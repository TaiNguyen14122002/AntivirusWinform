using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ScanAndRemoveVirus.Database;

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

        private static readonly ThreatDetectionRepository ThreatRepository = new ThreatDetectionRepository();

        public static int CountQuarantined()
        {
            try { return ThreatRepository.CountQuarantined(); }
            catch { return 0; }
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

        // SQL là nguồn metadata; file vật lý vẫn nằm trong thư mục Quarantine dưới dạng DetectionID.qtn.
        public static bool Quarantine(string filePath, string reason, out string storedId)
        {
            storedId = null;
            long detectionId = 0;
            string originalPath = null;
            string quarantinePath = null;
            bool moved = false;

            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;
                Directory.CreateDirectory(QuarantineDir);

                FileInfo info = new FileInfo(filePath);
                originalPath = info.FullName;
                string sha256 = ComputeFileSha256(originalPath);

                detectionId = ThreatRepository.Add(null, null, info.Name, originalPath,
                    string.IsNullOrWhiteSpace(reason) ? "Không rõ" : reason,
                    info.Length, null, null, sha256);
                if (detectionId <= 0) return false;

                quarantinePath = Path.Combine(QuarantineDir, detectionId + ".qtn");
                if (File.Exists(quarantinePath)) return false;

                File.Move(originalPath, quarantinePath);
                moved = true;

                if (!ThreatRepository.MarkQuarantined(detectionId, quarantinePath))
                {
                    try
                    {
                        if (File.Exists(quarantinePath) && !File.Exists(originalPath))
                        {
                            File.Move(quarantinePath, originalPath);
                            moved = false;
                        }
                    }
                    catch { }
                    return false;
                }

                storedId = detectionId.ToString();
                CacheEntry removed;
                Cache.TryRemove(originalPath, out removed);
                RaiseQuarantineChanged();
                return true;
            }
            catch
            {
                if (moved && !string.IsNullOrWhiteSpace(quarantinePath) && !string.IsNullOrWhiteSpace(originalPath))
                {
                    try
                    {
                        if (File.Exists(quarantinePath) && !File.Exists(originalPath))
                            File.Move(quarantinePath, originalPath);
                    }
                    catch { }
                }
                return false;
            }
        }

        public static List<QuarantinedItem> ListQuarantined()
        {
            var result = new List<QuarantinedItem>();
            try
            {
                DataTable table = ThreatRepository.GetQuarantined();
                foreach (DataRow row in table.Rows)
                {
                    result.Add(new QuarantinedItem
                    {
                        Id = Convert.ToString(row["DetectionID"]),
                        OriginalPath = DbString(row, "OriginalPath"),
                        Name = DbString(row, "FileName"),
                        Threat = DbString(row, "ThreatName"),
                        DetectedTime = row["DetectedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["DetectedAt"]),
                        Size = row["FileSizeBytes"] == DBNull.Value ? null : Convert.ToString(row["FileSizeBytes"]),
                        QuarantinePath = DbString(row, "QuarantinePath")
                    });
                }
            }
            catch { }
            return result;
        }

        public class QuarantinedItem
        {
            public string Id { get; set; }
            public string OriginalPath { get; set; }
            public string Name { get; set; }
            public string Threat { get; set; }
            public DateTime DetectedTime { get; set; }
            public string Size { get; set; }
            public string QuarantinePath { get; set; }
        }

        public static bool RestoreQuarantined(string id)
        {
            long detectionId;
            return long.TryParse(id, out detectionId) && RestoreQuarantined(detectionId);
        }

        public static bool RestoreQuarantined(long detectionId)
        {
            try
            {
                DataRow row = ThreatRepository.GetById(detectionId);
                if (row == null || !string.Equals(DbString(row, "Status"), "Quarantined", StringComparison.OrdinalIgnoreCase)) return false;

                string originalPath = DbString(row, "OriginalPath");
                string quarantinePath = DbString(row, "QuarantinePath");
                if (string.IsNullOrWhiteSpace(originalPath) || string.IsNullOrWhiteSpace(quarantinePath) || !File.Exists(quarantinePath)) return false;

                string dir = Path.GetDirectoryName(originalPath);
                if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
                string dest = GetAvailableRestorePath(originalPath);
                File.Move(quarantinePath, dest);

                if (!ThreatRepository.MarkRestored(detectionId))
                {
                    try { if (File.Exists(dest) && !File.Exists(quarantinePath)) File.Move(dest, quarantinePath); } catch { }
                    return false;
                }

                CacheEntry removed;
                Cache.TryRemove(originalPath, out removed);
                Cache.TryRemove(dest, out removed);
                RaiseQuarantineChanged();
                return true;
            }
            catch { return false; }
        }

        public static bool DeleteQuarantined(string id) { return DeleteQuarantined(id, true); }
        public static bool DeleteQuarantined(string id, bool permanent)
        {
            long detectionId;
            return long.TryParse(id, out detectionId) && DeleteQuarantined(detectionId, permanent);
        }
        public static bool DeleteQuarantined(long detectionId) { return DeleteQuarantined(detectionId, true); }

        public static bool DeleteQuarantined(long detectionId, bool permanent)
        {
            try
            {
                DataRow row = ThreatRepository.GetById(detectionId);
                if (row == null || !string.Equals(DbString(row, "Status"), "Quarantined", StringComparison.OrdinalIgnoreCase)) return false;
                string quarantinePath = DbString(row, "QuarantinePath");

                if (!string.IsNullOrWhiteSpace(quarantinePath) && File.Exists(quarantinePath))
                {
                    if (permanent) File.Delete(quarantinePath);
                    else Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(quarantinePath,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }

                if (!ThreatRepository.MarkDeleted(detectionId)) return false;
                RaiseQuarantineChanged();
                return true;
            }
            catch { return false; }
        }

        private static string GetAvailableRestorePath(string originalPath)
        {
            if (!File.Exists(originalPath)) return originalPath;
            string dir = Path.GetDirectoryName(originalPath);
            string stem = Path.GetFileNameWithoutExtension(originalPath);
            string ext = Path.GetExtension(originalPath);
            for (int i = 1; i < 1000; i++)
            {
                string candidate = Path.Combine(dir, stem + " (" + i + ")" + ext);
                if (!File.Exists(candidate)) return candidate;
            }
            return Path.Combine(dir, stem + "_" + Guid.NewGuid().ToString("N") + ext);
        }

        private static string DbString(DataRow row, string columnName)
        {
            if (row == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value) return null;
            return Convert.ToString(row[columnName]);
        }

        private static void RaiseQuarantineChanged()
        {
            Action h = QuarantineChanged;
            if (h != null) try { h(); } catch { }
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
            try
            {
                foreach (WorkItem item in state.Work.GetConsumingEnumerable(state.Cancellation))
                {
                    try
                    {
                        state.Cancellation.ThrowIfCancellationRequested();

                        if (item.IsDirectory)
                            ScanDirectory(item.Path, state);
                        else if (item.Files != null)
                            ScanFileChunk(item.Files, state);
                    }
                    catch (OperationCanceledException) { }
                    catch (InvalidOperationException) { }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                    catch (Exception) { }
                    finally
                    {
                        try { state.Completion.Signal(); }
                        catch (InvalidOperationException) { }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { }
            catch (Exception) { }
        }

        /// <summary>
        /// Quét sâu một thư mục. Mỗi thư mục con được đưa trở lại hàng đợi nên
        /// engine có thể đi xuống không giới hạn số cấp. Lỗi ở một nhánh không
        /// làm dừng các nhánh còn lại.
        /// </summary>
        private static void ScanDirectory(string folder, ScanState state)
        {
            state.Cancellation.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(folder)) return;

            try
            {
                if (!Directory.Exists(folder)) return;
            }
            catch (Exception)
            {
                return;
            }

            // 1) Tìm và xếp hàng các thư mục con.
            // Tách riêng khỏi phần liệt kê file để lỗi ở một phần không làm bỏ phần kia.
            try
            {
                foreach (string subDirectory in Directory.EnumerateDirectories(folder))
                {
                    state.Cancellation.ThrowIfCancellationRequested();

                    try
                    {
                        FileAttributes attributes = File.GetAttributes(subDirectory);

                        // Bỏ junction/symbolic link để tránh vòng lặp vô hạn.
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                            continue;

                        state.Completion.AddCount();
                        try
                        {
                            state.Work.Add(new WorkItem
                            {
                                Path = subDirectory,
                                IsDirectory = true
                            }, state.Cancellation);
                        }
                        catch
                        {
                            // AddCount đã tăng nhưng Work.Add thất bại -> phải trả count lại.
                            try { state.Completion.Signal(); }
                            catch (InvalidOperationException) { }
                            throw;
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                    catch (Exception) { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            catch (Exception) { }

            // 2) Liệt kê file trong chính thư mục hiện tại và chia thành từng chunk.
            try
            {
                var chunk = new List<FileInfo>(FileChunkSize);

                foreach (string filePath in Directory.EnumerateFiles(folder))
                {
                    state.Cancellation.ThrowIfCancellationRequested();

                    try
                    {
                        chunk.Add(new FileInfo(filePath));

                        if (chunk.Count >= FileChunkSize)
                        {
                            QueueFileChunk(chunk, state);
                            chunk = new List<FileInfo>(FileChunkSize);
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                    catch (Exception) { }
                }

                if (chunk.Count > 0)
                    QueueFileChunk(chunk, state);
            }
            catch (OperationCanceledException) { throw; }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            catch (Exception) { }
        }

        /// <summary>
        /// Đưa một nhóm file vào hàng đợi quét. Nếu thêm thất bại thì hoàn lại
        /// CountdownEvent để phiên quét không bị chờ vô hạn.
        /// </summary>
        private static void QueueFileChunk(List<FileInfo> files, ScanState state)
        {
            if (files == null || files.Count == 0) return;

            state.Cancellation.ThrowIfCancellationRequested();

            FileInfo[] array = files.ToArray();
            state.Completion.AddCount();

            try
            {
                state.Work.Add(new WorkItem
                {
                    Files = array,
                    IsDirectory = false
                }, state.Cancellation);
            }
            catch
            {
                try { state.Completion.Signal(); }
                catch (InvalidOperationException) { }
                throw;
            }
        }

        private static void ScanFileChunk(FileInfo[] files, ScanState state)
        {
            if (files == null) return;

            for (int i = 0; i < files.Length; i++)
            {
                state.Cancellation.ThrowIfCancellationRequested();

                FileInfo file = files[i];
                if (file == null) continue;

                try
                {
                    InspectOneFile(file, state);
                }
                catch (OperationCanceledException) { throw; }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
                catch (Exception) { }
            }
        }

        private static void InspectOneFile(FileInfo file, ScanState state)
        {
            state.Cancellation.ThrowIfCancellationRequested();
            if (file == null) return;

            string fullPath;
            try
            {
                fullPath = file.FullName;
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                string kind, reason;
                if (Evaluate(file, out kind, out reason))
                {
                    state.Threats.Enqueue(new ThreatFound
                    {
                        FilePath = fullPath,
                        Kind = kind,
                        Reason = reason
                    });
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                // Một file lỗi không được làm dừng phần còn lại của chunk.
            }
            finally
            {
                // File đã được engine thử xử lý, kể cả khi file bị khóa/lỗi đọc.
                int scanned = Interlocked.Increment(ref state.Scanned);

                if (state.Progress != null && scanned % ProgressEveryNFiles == 0)
                {
                    try { state.Progress(scanned, Path.GetFileName(fullPath)); }
                    catch (Exception) { }
                }
            }
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
