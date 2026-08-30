using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    public enum ScanType { Quick, Full, Custom }

    public class ThreatFound
    {
        public string FilePath { get; set; }
        public string Reason { get; set; }
        /// <summary>Loại phát hiện: "Chữ ký" (kỹ thuật 1) hoặc "Heuristic" (kỹ thuật 2).</summary>
        public string Kind { get; set; }
    }

    public class ScanResult
    {
        public int FilesScanned { get; set; }
        public TimeSpan Duration { get; set; }
        private readonly List<ThreatFound> threats = new List<ThreatFound>();
        public List<ThreatFound> Threats { get { return threats; } }
        public bool HasThreats { get { return threats.Count > 0; } }
    }

    /// <summary>
    /// Engine quét 3 lớp:
    ///  (1) CHỮ KÝ: tên chứa "eicar", byte đầu khớp chuỗi thử nghiệm, hash SHA256 khớp bảng chữ ký.
    ///  (2) HEURISTIC: chấm điểm đặc điểm nghi vấn (đuôi kép giả mạo, tệp ẩn, mồi câu, script độc).
    ///  (3) HÀNH VI/REALTIME: xem RealTimeProtection — FileSystemWatcher quét tức thì khi tệp mới/đổi.
    /// Tốc độ: chỉ mở tệp diện nghi vấn + cache kết quả theo (mtime, kích thước).
    /// </summary>
    public static class ScanEngine
    {
        // ---- Kỹ thuật 1: bảng chữ ký thử nghiệm ----
        // ponytail: chưa phải dữ liệu virus thật. Nâng cấp: nạp từ bảng VirusSignatures (AntivirusDB.sql).
        public const string TestSignature = "XVIRUS-TEST-SIGNATURE::";
        // Nội dung "mẫu virus giả lập" — hash SHA256 của đúng chuỗi này nằm trong bảng chữ ký bên dưới.
        public const string SignatureSampleContent = "SIM-MALWARE-SIGNATURE::A57C1F37::PAYLOAD";

        public sealed class SignatureEntry
        {
            public string Name;
            public string Sha256Hex;   // khớp toàn bộ nội dung tệp (hash chữ ký)
            public string Prefix;      // khớp byte đầu tệp (chuỗi chữ ký)
        }

        private static readonly List<SignatureEntry> Signatures = new List<SignatureEntry>
        {
            new SignatureEntry { Name = "XSignature.Test", Prefix = TestSignature },
            // File thử nghiệm chuẩn EICAR (SHA256 công khai của 68 byte chuỗi EICAR chuẩn):
            new SignatureEntry { Name = "EICAR-Test-File", Sha256Hex = "275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538bbb857a133b" },
            // SHA256 của SignatureSampleContent (tính bằng PowerShell, ASCII không BOM)
            new SignatureEntry { Name = "Malsim.Sample.Hash", Sha256Hex = "ea9958fec77e564d36bd910ca32d15004e41fdb21bfb7c1dca414da943ee3eeb" },
        };

        private const long MaxContentScanBytes = 4 * 1024 * 1024;
        private const int ProgressEveryNFiles = 2000;
        private const int MaxParallelWorkers = 64;
        private const int FileChunkSize = 1024;
        private const int MaxCacheEntries = 500000;
        private static readonly byte[] SignatureBytes = Encoding.ASCII.GetBytes(TestSignature);
        private static readonly byte[] EmptyBytes = new byte[0];
        private static readonly ThreadLocal<byte[]> SignatureBuffer =
            new ThreadLocal<byte[]>(() => new byte[SignatureBytes.Length]);
        private static readonly ThreadLocal<byte[]> ReadBuffer =
            new ThreadLocal<byte[]>(() => new byte[65536]);

        // Chỉ tệp thuộc diện này mới được MỞ ra đọc nội dung (kỹ thuật 1 phần hash/byte đầu):
        private static readonly HashSet<string> ContentScanExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".exe", ".dll", ".sys", ".scr", ".com", ".pif",
                ".bat", ".cmd", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".ps1", ".msi", ".hta",
                ".txt", ".ini", ".log", ".csv", ".lnk"
            };

        // ---- Kỹ thuật 2: heuristic ----
        private const int HeuristicThreshold = 60;
        private static readonly HashSet<string> ExecutableExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".exe", ".scr", ".com", ".pif", ".bat", ".cmd", ".vbs", ".vbe", ".ps1", ".js", ".jse", ".wsf", ".hta", ".msi" };
        private static readonly HashSet<string> DocumentLureExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png", ".gif", ".txt", ".zip", ".rar", ".mp3", ".csv", ".iso" };
        private static readonly string[] LureKeywords =
            { "invoice", "receipt", "payment", "hoa don", "thanh toan", "crack", "keygen", "serial", "resum", "screenshot", "wallet", "ho so vay", "thong bao" };
        private static readonly HashSet<string> ScriptContentExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".ps1", ".bat", ".cmd", ".vbs", ".hta" };

        // ---- Cache kỹ thuật 1 (mtime + size -> đã kiểm nội dung sạch/nhiễm) ----
        private struct CacheEntry
        {
            public long Ticks;
            public long Length;
            public bool Threat;
            public CacheEntry(long ticks, long length, bool threat)
            {
                Ticks = ticks;
                Length = length;
                Threat = threat;
            }
        }
        private static readonly ConcurrentDictionary<string, CacheEntry> Cache =
            new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private static bool cacheLoaded;
        private static readonly object CacheLoadLock = new object();

        public static string QuarantineDir
        {
            get { return DataDir.Resolve("Quarantine"); }
        }

        public static string ScanCachePath
        {
            get { return DataDir.Resolve("scancache.dat"); }
        }

        public static void ClearScanCache()
        {
            lock (CacheLoadLock)
            {
                Cache.Clear();
                cacheLoaded = false;
            }
        }

        public static int CountQuarantined()
        {
            return Directory.Exists(QuarantineDir) ? Directory.GetFiles(QuarantineDir).Length : 0;
        }

        public static bool Quarantine(string filePath)
        {
            string id;
            return Quarantine(filePath, null, out id);
        }

        public static bool Quarantine(string filePath, string reason)
        {
            string id;
            return Quarantine(filePath, reason, out id);
        }

        // Cách ly 1 đe dọa: dời tệp vào Quarantine (đổi tên GUID.qtn) và ghi sổ để khôi phục/xóa sau này.
        public static bool Quarantine(string filePath, string reason, out string storedId)
        {
            storedId = null;
            try
            {
                Directory.CreateDirectory(QuarantineDir);
                string id = Guid.NewGuid().ToString("N") + ".qtn";
                string dest = Path.Combine(QuarantineDir, id);
                File.Move(filePath, dest);
                storedId = id;
                string name = Path.GetFileName(filePath);
                string size = new FileInfo(dest).Length.ToString();
                QuarantineLedger.Record(id, filePath, DateTime.Now, reason ?? "", name, size);
                Action h = QuarantineChanged;
                if (h != null) h();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static List<QuarantinedItem> ListQuarantined()
        {
            return QuarantineLedger.ReadAll();
        }

        public class QuarantinedItem
        {
            public string Id { get; set; }
            public string OriginalPath { get; set; }
            public string Name { get; set; }
            public string Threat { get; set; }
            public DateTime DetectedTime { get; set; }
            public string Size { get; set; }
        }

        /// <summary>Khôi phục: trả tệp về đường dẫn gốc rồi gỡ khỏi sổ. Trả về false nếu tệp đã mất.</summary>
        public static bool RestoreQuarantined(string id)
        {
            string stored = Path.Combine(QuarantineDir, id);
            if (!File.Exists(stored)) return false;
            QuarantinedItem item = null;
            foreach (var q in QuarantineLedger.ReadAll())
                if (q.Id == id) { item = q; break; }
            try
            {
                if (item != null && !string.IsNullOrEmpty(item.OriginalPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(item.OriginalPath));
                    // Không đè tệp gốc hiện có: thêm hậu tố " (n)"
                    string dest = item.OriginalPath;
                    string dir = Path.GetDirectoryName(dest);
                    string stem = Path.GetFileNameWithoutExtension(dest);
                    string ext = Path.GetExtension(dest);
                    int n = 1;
                    while (File.Exists(dest) && n < 1000)
                    {
                        dest = Path.Combine(dir, stem + " (" + n + ")" + ext);
                        n++;
                    }
                    File.Move(stored, dest);
                }
                else
                {
                    File.Delete(stored);
                }
                QuarantineLedger.Remove(id);
                Action h = QuarantineChanged;
                if (h != null) h();
                return true;
            }
            catch (Exception) { return false; }
        }

        /// <summary>Xóa vĩnh viễn tệp đang cách ly.</summary>
        public static bool DeleteQuarantined(string id)
        {
            string stored = Path.Combine(QuarantineDir, id);
            try
            {
                if (File.Exists(stored)) File.Delete(stored);
                QuarantineLedger.Remove(id);
                Action h = QuarantineChanged;
                if (h != null) h();
                return true;
            }
            catch (Exception) { return false; }
        }

        // Báo cho UI (tab Tổng quan + tab Cách ly) khi danh sách cách ly thay đổi.
        public static event Action QuarantineChanged;

        public static IEnumerable<string> GetScanRoots(ScanType type, string customPath)
        {
            if (type == ScanType.Custom)
                return string.IsNullOrEmpty(customPath) || (!Directory.Exists(customPath) && !File.Exists(customPath))
                    ? Enumerable.Empty<string>()
                    : Enumerable.Repeat(customPath, 1);

            if (type == ScanType.Full)
                return DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .Select(d => d.Name);

            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new[]
            {
                Path.Combine(profile, "Desktop"),
                Path.Combine(profile, "Downloads"),
                Path.GetTempPath()
            }.Where(Directory.Exists);
        }

        /// <param name="progress">callback (số tệp đã quét, tệp hiện tại) — có thể được gọi từ nhiều thread nền</param>
        /// <exception cref="OperationCanceledException">bị hủy giữa chừng</exception>
        public static ScanResult Scan(ScanType type, string customPath, CancellationToken ct,
            Action<int, string> progress = null)
        {
            ct.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            var result = new ScanResult();
            EnsureCacheLoaded();

            // Quét tùy chọn trỏ vào đúng 1 tệp:
            if (type == ScanType.Custom && File.Exists(customPath))
            {
                ct.ThrowIfCancellationRequested();
                ThreatFound single = EvaluateFile(customPath);
                if (single != null) result.Threats.Add(single);
                result.FilesScanned = 1;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                if (progress != null) progress(1, customPath);
                ct.ThrowIfCancellationRequested();
                SaveCache();
                return result;
            }

            string[] roots = GetScanRoots(type, customPath).Where(Directory.Exists).ToArray();

            if (roots.Length > 0)
            {
                // Không Dispose CountdownEvent/BlockingCollection: worker vẫn có thể chạm
                // vào chúng sau khi Wait trả về; để GC tự thu dọn.
                var completion = new CountdownEvent(1);
                var work = new BlockingCollection<WorkItem>();
                var state = new ScanState
                {
                    Threats = new ConcurrentQueue<ThreatFound>(),
                    Work = work,
                    Completion = completion,
                    Progress = progress,
                    Cancellation = ct
                };

                int workerCount = Math.Max(8, Math.Min(MaxParallelWorkers, Environment.ProcessorCount * 4));
                int minWorker, minIocp;
                ThreadPool.GetMinThreads(out minWorker, out minIocp);
                ThreadPool.SetMinThreads(Math.Max(minWorker, workerCount), Math.Max(minIocp, workerCount));

                for (int i = 0; i < workerCount; i++)
                    ThreadPool.QueueUserWorkItem(delegate { RunWorker(state); });

                foreach (string root in roots)
                {
                    completion.AddCount();
                    work.Add(new WorkItem { Path = root, IsDirectory = true });
                }

                completion.Signal();
                try
                {
                    completion.Wait(ct);
                }
                finally
                {
                    work.CompleteAdding();
                }

                foreach (ThreatFound threat in state.Threats)
                    result.Threats.Add(threat);
                result.FilesScanned = state.Scanned;
                SaveCache();
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            if (progress != null) progress(result.FilesScanned, null);
            ct.ThrowIfCancellationRequested();
            return result;
        }

        // Điểm vào cho RealTimeProtection + quét 1 tệp. Trả về null nếu tệp sạch.
        public static ThreatFound EvaluateFile(string path)
        {
            return EvaluateFile(path, false);
        }

        /// <summary>forceContent=true: bỏ qua cửa "diện nghi vấn" (guard đặc chủng: USB/startup/MOTW/khôi phục).</summary>
        public static ThreatFound EvaluateFile(string path, bool forceContent)
        {
            FileInfo file;
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                file = new FileInfo(path);
            }
            catch (Exception) { return null; }

            string kind, reason;
            if (Evaluate(file, out kind, out reason, forceContent))
                return new ThreatFound { FilePath = file.FullName, Kind = kind, Reason = reason };
            return null;
        }

        private static void RunWorker(ScanState state)
        {
            // Worker chạy trên ThreadPool: MỌI exception lọt ra ngoài sẽ giết cả app.
            // Khi hủy giữa chừng, main thread có thể đã CompleteAdding() trong khi worker
            // đang giữa ScanDirectory -> Work.Add bắn InvalidOperationException: phải nuốt ở đây.
            try
            {
                foreach (WorkItem item in state.Work.GetConsumingEnumerable(state.Cancellation))
                {
                    try
                    {
                        if (item.IsDirectory)
                            ScanDirectory(item.Path, state);
                        else
                            ScanFileChunk(item.Files, state);
                    }
                    catch (OperationCanceledException) { }
                    catch (InvalidOperationException) { } // CompleteAdding/ODE (ODE là subclass)
                    finally
                    {
                        try { state.Completion.Signal(); }
                        catch (InvalidOperationException) { } // event đã đạt 0 (vừa hủy xong)
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { } // swallowing ODE: ObjectDisposedException phái sinh từ nó
        }

        private static void ScanDirectory(string folder, ScanState state)
        {
            try
            {
                var dir = new DirectoryInfo(folder);
                foreach (var sub in dir.EnumerateDirectories())
                {
                    if ((sub.Attributes & FileAttributes.ReparsePoint) != 0) continue; // bỏ junction, tránh lặp vô hạn
                    // Check hủy TRƯỚC khi nhận việc mới: giảm cửa sổ race với CompleteAdding()
                    state.Cancellation.ThrowIfCancellationRequested();
                    state.Completion.AddCount();
                    state.Work.Add(new WorkItem { Path = sub.FullName, IsDirectory = true });
                }

                // DirectoryInfo nạp WIN32_FIND_DATA thẳng vào FileInfo:
                // Name/Length/LastWriteTime/Attributes không tốn thêm syscall.
                FileInfo[] chunk = null;
                int chunkFill = 0;
                foreach (FileInfo file in dir.EnumerateFiles())
                {
                    state.Cancellation.ThrowIfCancellationRequested();
                    if (chunk == null) chunk = new FileInfo[FileChunkSize];
                    chunk[chunkFill++] = file;
                    if (chunkFill == FileChunkSize)
                    {
                        state.Completion.AddCount();
                        state.Work.Add(new WorkItem { Files = chunk });
                        chunk = null;
                        chunkFill = 0;
                    }
                }
                if (chunkFill > 0)
                {
                    if (chunkFill < FileChunkSize) Array.Resize(ref chunk, chunkFill);
                    state.Completion.AddCount();
                    state.Work.Add(new WorkItem { Files = chunk });
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        private static void ScanFileChunk(FileInfo[] files, ScanState state)
        {
            for (int i = 0; i < files.Length; i++)
                InspectOneFile(files[i], state);
        }

        private static void InspectOneFile(FileInfo file, ScanState state)
        {
            state.Cancellation.ThrowIfCancellationRequested();
            string kind, reason;
            if (Evaluate(file, out kind, out reason))
                state.Threats.Enqueue(new ThreatFound { FilePath = file.FullName, Kind = kind, Reason = reason });
            int scanned = Interlocked.Increment(ref state.Scanned);
            if (state.Progress != null && scanned % ProgressEveryNFiles == 0)
                state.Progress(scanned, file.Name);
        }

        /// <summary>
        /// Pipeline 2 lớp (kỹ thuật 1 + kỹ thuật 2). Trả về true nếu đe dọa.
        /// forceContent: guard đặc chủng bỏ qua cửa "diện nghi vấn" để soi mọi đuôi/file.
        /// </summary>
        private static bool Evaluate(FileInfo file, out string kind, out string reason,
            bool forceContent = false)
        {
            // --- Kỹ thuật 1a: tên trùng chuẩn EICAR ---
            if (file.Name.IndexOf("eicar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                kind = "Chữ ký";
                reason = "Tên tệp chứa 'eicar' (trên chuẩn kiểm thử ngành)";
                return true;
            }

            // --- Kỹ thuật 1b: chữ ký byte đầu + hash SHA256 (chỉ với tệp diện nghi vấn) ---
            long length = file.Length;
            bool contentGate = forceContent || (ContentScanExtensions.Contains(file.Extension)
                && length >= SignatureBytes.Length && length <= MaxContentScanBytes);
            if (contentGate)
            {
                CacheEntry prev;
                long ticks = SafeTicks(file);
                if (ticks >= 0 && Cache.TryGetValue(file.FullName, out prev)
                    && prev.Length == length && prev.Ticks == ticks)
                {
                    if (prev.Threat)
                    {
                        kind = "Chữ ký";
                        reason = "Trùng chữ ký/hash (kết quả cache của lần quét trước)";
                        return true;
                    }
                }
                else
                {
                    string contentReason;
                    bool contentThreat = CheckContent(file.FullName, out contentReason);
                    Remember(file, contentThreat);
                    if (contentThreat)
                    {
                        kind = "Chữ ký";
                        reason = contentReason;
                        return true;
                    }
                }
            }

            // --- Kỹ thuật 2: chấm điểm heuristic (metadata + nội dung script nhỏ) ---
            List<string> hits;
            int score = HeuristicScore(file, out hits);
            if (score >= HeuristicThreshold)
            {
                kind = "Heuristic";
                reason = string.Format("Nghi vấn {0}/100: {1}", score, string.Join("; ", hits));
                return true;
            }

            kind = null;
            reason = null;
            return false;
        }

        // Mở tệp MỘT lần: kiểm byte đầu theo chữ ký prefix, rồi tính SHA256 toàn bộ so bảng chữ ký.
        private static bool CheckContent(string path, out string reason)
        {
            reason = null;
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan))
                {
                    if (stream.Length == 0 || stream.Length > MaxContentScanBytes) return false;

                    byte[] head = SignatureBuffer.Value;
                    int headRead = stream.Read(head, 0, head.Length);
                    if (headRead == head.Length)
                    {
                        foreach (SignatureEntry sig in Signatures)
                        {
                            if (sig.Prefix == null) continue;
                            byte[] p = Encoding.ASCII.GetBytes(sig.Prefix);
                            if (p.Length != head.Length) continue;
                            bool match = true;
                            for (int i = 0; i < p.Length; i++)
                                if (head[i] != p[i]) { match = false; break; }
                            if (match) { reason = "Byte đầu tệp khớp chữ ký " + sig.Name; return true; }
                        }
                    }

                    using (var sha = SHA256.Create())
                    {
                        sha.TransformBlock(head, 0, headRead, null, 0);
                        byte[] buffer = ReadBuffer.Value;
                        long total = headRead;
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            total += read;
                            if (total > MaxContentScanBytes) return false;
                            sha.TransformBlock(buffer, 0, read, null, 0);
                        }
                        sha.TransformFinalBlock(EmptyBytes, 0, 0);
                        string hex = ToHexLower(sha.Hash);
                        foreach (SignatureEntry sig in Signatures)
                        {
                            if (sig.Sha256Hex != null && string.Equals(sig.Sha256Hex, hex, StringComparison.OrdinalIgnoreCase))
                            {
                                reason = "SHA256 nội dung khớp chữ ký " + sig.Name;
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch (Exception)
            {
                return false; // ponytail: tệp bị khoá/không đọc được thì bỏ qua, không dừng phiên quét
            }
        }

        private static string ToHexLower(byte[] hash)
        {
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        // SHA256 toàn bộ tệp (bất kỳ kích thước) — phục vụ tra cứu VirusTotal/cloud.
        public static string ComputeFileSha256(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.SequentialScan))
                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(stream);
                    return ToHexLower(hash);
                }
            }
            catch (Exception) { return null; }
        }

        // --- Kỹ thuật 2: bộ chấm điểm heuristic ---
        private static int HeuristicScore(FileInfo file, out List<string> hits)
        {
            hits = new List<string>();
            int score = 0;
            string name = file.Name;
            string ext = file.Extension;

            bool isExec = ext.Length > 0 && ExecutableExtensions.Contains(ext);

            // 1) Đuôi kép giả mạo tài liệu: "invoice.pdf.exe"
            string stem = Path.GetFileNameWithoutExtension(name);
            if (isExec && stem != null)
            {
                string prevExt = Path.GetExtension(stem);
                if (prevExt.Length > 0 && DocumentLureExtensions.Contains(prevExt))
                {
                    score += 70;
                    hits.Add("đuôi kép giả mạo tài liệu (" + prevExt.TrimStart('.') + ext + ")");
                }
            }

            // 2) Tệp thực thi bị ẩn thuộc tính Hidden
            if (isExec && (file.Attributes & FileAttributes.Hidden) != 0)
            {
                score += 40;
                hits.Add("tệp thực thi bị ẩn");
            }

            // 3) Tên mồi câu xã hội + đuôi thực thi
            if (isExec)
            {
                string lower = name.ToLowerInvariant();
                foreach (string word in LureKeywords)
                    if (lower.IndexOf(word, StringComparison.Ordinal) >= 0)
                    {
                        score += 30;
                        hits.Add("tên mồi câu \"" + word + "\"");
                        break;
                    }
            }

            // 4) Tệp thực thi nhỏ nằm trong thư mục Temp (dấu hiệu dropper)
            if (isExec && file.Length < 2048)
            {
                string temp = Path.GetTempPath();
                if (file.FullName.StartsWith(temp, StringComparison.OrdinalIgnoreCase))
                {
                    score += 40;
                    hits.Add("thực thi nhỏ trong thư mục Temp");
                }
            }

            // 5) Mã lệnh độc điển hình trong script (PowerShell/VBS/BAT)
            bool isScript = ext.Length > 0 && ScriptContentExtensions.Contains(ext);
            if (isScript)
            {
                string text;
                if (TryReadScriptText(file.FullName, out text))
                {
                    int markerHits = 0;
                    CheckMarker(text, "iex(", ref score, ref markerHits, hits, "PowerShell iex()");
                    CheckMarker(text, "invoke-expression", ref score, ref markerHits, hits, "Invoke-Expression");
                    CheckMarker(text, "-enc ", ref score, ref markerHits, hits, "PowerShell -EncodedCommand");
                    CheckMarker(text, "frombase64string", ref score, ref markerHits, hits, "giải mã Base64");
                    CheckMarker(text, "downloadstring", ref score, ref markerHits, hits, "tải mã từ xa");
                    CheckMarker(text, "msscriptcontrol", ref score, ref markerHits, hits, "MS ScriptControl");
                }
            }

            return score;
        }

        private static void CheckMarker(string text, string marker, ref int score, ref int markerHits,
            List<string> hits, string label)
        {
            if (markerHits >= 3) return; // chặn điểm phình khi script chỉ nhiều hơn là độc
            if (text.IndexOf(marker, StringComparison.Ordinal) >= 0)
            {
                markerHits++;
                score += 30;
                hits.Add(label);
            }
        }

        private static bool TryReadScriptText(string path, out string text)
        {
            text = null;
            try
            {
                byte[] buffer = new byte[4096];
                int n;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan))
                    n = fs.Read(buffer, 0, buffer.Length);
                if (n <= 0) return false;
                text = Encoding.ASCII.GetString(buffer, 0, n).ToLowerInvariant();
                return true;
            }
            catch (Exception) { return false; }
        }

        private static long SafeTicks(FileInfo file)
        {
            try { return file.LastWriteTimeUtc.Ticks; }
            catch (IOException) { return -1; }
        }

        private static void Remember(FileInfo file, bool threat)
        {
            if (Cache.Count >= MaxCacheEntries) return;
            long ticks = SafeTicks(file);
            if (ticks < 0) return;
            Cache[file.FullName] = new CacheEntry(ticks, file.Length, threat);
        }

        // format dòng cache: "threat|length|ticks|đường dẫn" (đường dẫn NTFS không chứa '|')
        private static void EnsureCacheLoaded()
        {
            if (cacheLoaded) return;
            lock (CacheLoadLock)
            {
                if (cacheLoaded) return;
                try
                {
                    if (File.Exists(ScanCachePath))
                    {
                        foreach (string line in File.ReadLines(ScanCachePath))
                        {
                            string[] parts = line.Split(new[] { '|' }, 4);
                            if (parts.Length != 4) continue;
                            long length, ticks;
                            if (!long.TryParse(parts[1], out length)) continue;
                            if (!long.TryParse(parts[2], out ticks)) continue;
                            Cache[parts[3]] = new CacheEntry(
                                ticks, length, parts[0].Length > 0 && parts[0][0] == '1');
                        }
                    }
                }
                catch (Exception) { } // cache hỏng -> quét lại từ đầu
                cacheLoaded = true;
            }
        }

        private static void SaveCache()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScanCachePath));
                using (var w = new StreamWriter(ScanCachePath, false, new UTF8Encoding(false)))
                {
                    foreach (KeyValuePair<string, CacheEntry> kv in Cache)
                    {
                        w.Write(kv.Value.Threat ? '1' : '0');
                        w.Write('|');
                        w.Write(kv.Value.Length);
                        w.Write('|');
                        w.Write(kv.Value.Ticks);
                        w.Write('|');
                        w.WriteLine(kv.Key);
                    }
                }
            }
            catch (Exception) { } // cache là best-effort
        }

        private sealed class WorkItem
        {
            public string Path;
            public FileInfo[] Files;
            public bool IsDirectory;
        }

        private sealed class ScanState
        {
            public int Scanned;
            public ConcurrentQueue<ThreatFound> Threats;
            public BlockingCollection<WorkItem> Work;
            public CountdownEvent Completion;
            public Action<int, string> Progress;
            public CancellationToken Cancellation;
        }
    }
}
