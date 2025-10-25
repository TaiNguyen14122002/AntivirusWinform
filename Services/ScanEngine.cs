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
        /// <summary>Hash toàn tệp, CDC hoặc phân tích heuristic tĩnh dự phòng.</summary>
        public string Kind { get; set; }
        public long? SignatureID { get; set; }
        /// <summary>Đường dẫn vật lý của archive khi FilePath là đường dẫn entry ảo.</summary>
        public string PhysicalPath { get; set; }
        public string ArchiveEntryPath { get; set; }
    }

    public sealed class ScanFileIssue
    {
        public string Path { get; set; }
        public string Reason { get; set; }
    }

    public class ScanResult
    {
        public int FilesScanned { get; set; }
        /// <summary>
        /// Số tệp thực sự kiểm tra thành công (tệp vật lý + các entry tệp nén).
        /// Không tính tệp vật lý không đọc được hoặc entry chưa kiểm tra.
        /// Dùng CHUNG cho giao diện, popup hoàn tất và nhật ký quét.
        /// </summary>
        public int CheckedObjects
        {
            get { return Math.Max(0, FilesScanned - ReadErrorCount) + ArchiveEntriesScanned; }
        }
        /// <summary>Số tệp đã thử quét nhưng không đọc/đối chiếu được do lỗi I/O hoặc quyền.</summary>
        public int ReadErrorCount { get; set; }
        /// <summary>Thư mục vật lý đã duyệt, không tính trùng đường dẫn.</summary>
        public int DirectoriesScanned { get; set; }
        /// <summary>Thư mục không truy cập được hoặc liên kết thư mục được bỏ qua.</summary>
        public int DirectoriesSkipped { get; set; }
        /// <summary>Lỗi liệt kê tệp/thư mục (khác với lỗi đọc nội dung tệp).</summary>
        public int EnumerationErrors { get; set; }
        /// <summary>Phạm vi tệp/thư mục chọn đã không còn tồn tại lúc bắt đầu quét.</summary>
        public int MissingTargets { get; set; }
        public int ArchiveEntriesScanned { get; set; }
        public int ArchiveEntriesUnchecked { get; set; }
        private readonly List<ScanFileIssue> issues = new List<ScanFileIssue>();
        public List<ScanFileIssue> Issues { get { return issues; } }
        public TimeSpan Duration { get; set; }
        /// <summary>Non-empty if SQL signatures could not be loaded.</summary>
        public string SignatureLoadWarning { get; set; }
        public string SignatureSource { get; set; }
        public DateTime SignatureCreatedAtUtc { get; set; }
        public bool SignatureIsDegraded { get; set; }
        private readonly List<ThreatFound> threats = new List<ThreatFound>();
        public List<ThreatFound> Threats { get { return threats; } }
        public bool HasThreats { get { return threats.Count > 0; } }
    }

    /// <summary>
    /// Số liệu tiến trình dùng chung cho tất cả vị trí trong một phiên quét.
    /// Các trường được cập nhật từ worker; UI chỉ đọc snapshot bất biến.
    /// ETA chỉ hiển thị sau khi liệt kê xong thư mục và có đủ dữ liệu.
    /// </summary>
    public sealed class ScanTelemetry
    {
        private readonly Stopwatch clock = Stopwatch.StartNew();
        // Byte tệp vật lý và byte đọc của entry giải nén PHẢI tách riêng:
        // dung lượng entry có thể gấp nhiều lần kích thước ZIP trên đĩa.
        private long bytesDiscovered, physicalBytesRead, archiveBytesRead, skippedSecondPassBytes;
        private int filesDiscovered, filesCompleted, physicalFilesChecked, archiveEntriesCompleted, threatsFound;
        private int directoriesPending, enumerationComplete, clockStarted, ioErrors;
        // Đếm các archive vật lý đã biết, chưa hoàn thành quét sâu.
        // Cho đến khi hoàn tất, KHÔNG có mẫu số đáng tin cậy cho ETA/percentage.
        private int archivesDiscovered, archivesCompleted;
        private int passFactor = 1;
        private string currentPath;

        public sealed class Reading
        {
            public int FilesDiscovered, FilesCompleted, ArchiveEntriesCompleted, CheckedObjects, ThreatsFound, Errors, Percent;
            public int ArchivesDiscovered, ArchivesCompleted, ArchivesPending;
            public long BytesDiscovered, BytesRead, ArchiveBytesRead;
            public double MiBPerSecond;
            public TimeSpan? Remaining;
            public bool EnumerationComplete;
            public string CurrentPath;
        }

        internal void Configure(HybridSignatureScanner.Snapshot snapshot)
        {
            if (Interlocked.CompareExchange(ref clockStarted, 1, 0) == 0) clock.Restart();
            passFactor = snapshot != null && snapshot.CdcGroupCount > 0 &&
                snapshot.WholeFileSignatureCount > 0 ? 2 : 1;
        }
        internal void BeginDirectories(int roots)
        {
            Interlocked.Exchange(ref directoriesPending, roots);
            Interlocked.Exchange(ref enumerationComplete, roots == 0 ? 1 : 0);
        }
        internal void AddDirectory()
        {
            Interlocked.Increment(ref directoriesPending);
            Interlocked.Exchange(ref enumerationComplete, 0);
        }
        internal void DirectoryFinished()
        {
            if (Interlocked.Decrement(ref directoriesPending) == 0)
                Interlocked.Exchange(ref enumerationComplete, 1);
        }
        internal void Discover(FileInfo[] files)
        {
            long bytes = 0;
            int archives = 0;
            foreach (FileInfo file in files)
            {
                if (file == null) continue;
                try
                {
                    bytes += Math.Max(0, file.Length);
                    if (ArchiveScanService.IsSupportedArchive(file.Name)) archives++;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (System.Security.SecurityException) { }
            }
            // Công bố số archive trước để UI không tính ETA khi ZIP vừa được phát hiện.
            if (archives > 0) Interlocked.Add(ref archivesDiscovered, archives);
            Interlocked.Add(ref bytesDiscovered, bytes);
            Interlocked.Add(ref filesDiscovered, files.Length);
        }
        internal void ReadBytes(long count)
        {
            if (count > 0) Interlocked.Add(ref physicalBytesRead, count);
        }
        internal void ReadArchiveBytes(long count)
        {
            if (count > 0) Interlocked.Add(ref archiveBytesRead, count);
        }
        internal void FinishArchive()
        {
            Interlocked.Increment(ref archivesCompleted);
        }
        internal void SkipCdcPass(long bytes)
        {
            if (passFactor == 2 && bytes > 0)
                Interlocked.Add(ref skippedSecondPassBytes, bytes);
        }
        internal void StartFile(string path) { Interlocked.Exchange(ref currentPath, path); }
        internal void FinishFile(bool successfullyChecked)
        {
            if (successfullyChecked) Interlocked.Increment(ref physicalFilesChecked);
            Interlocked.Increment(ref filesCompleted);
        }
        internal void FinishArchiveEntry() { Interlocked.Increment(ref archiveEntriesCompleted); }
        /// <summary>Mỗi phát hiện (hash, CDC hoặc heuristic) được báo ngay từ worker.</summary>
        internal void RecordThreat() { Interlocked.Increment(ref threatsFound); }
        internal void RecordError() { Interlocked.Increment(ref ioErrors); }

        public Reading GetReading()
        {
            int total = Volatile.Read(ref filesDiscovered), done = Volatile.Read(ref filesCompleted);
            int entriesDone = Volatile.Read(ref archiveEntriesCompleted);
            int physicalChecked = Volatile.Read(ref physicalFilesChecked);
            int foundArchives = Volatile.Read(ref archivesDiscovered);
            int finishedArchives = Volatile.Read(ref archivesCompleted);
            int pendingArchives = Math.Max(0, foundArchives - finishedArchives);
            long totalBytes = Interlocked.Read(ref bytesDiscovered);
            long physicalRead = Interlocked.Read(ref physicalBytesRead);
            long entryRead = Interlocked.Read(ref archiveBytesRead);
            bool ready = Volatile.Read(ref enumerationComplete) == 1;
            double elapsed = Math.Max(0.001, clock.Elapsed.TotalSeconds);
            double filePart = total == 0 ? 0 : Math.Min(1d, (double)done / total);
            double planned = Math.Max(1d, (double)totalBytes * passFactor - Interlocked.Read(ref skippedSecondPassBytes));
            double bytePart = totalBytes == 0 ? filePart : Math.Min(1d, physicalRead / planned);
            double fraction = totalBytes == 0 ? filePart : 0.25d * filePart + 0.75d * bytePart;
            // Trong khi ZIP/TAR/GZ chưa quét sâu xong, số entry và số byte giải nén
            // chưa thể dự đoán trước. Dùng thanh marquee thay vì báo 99% ảo.
            bool canCalculate = ready && pendingArchives == 0;
            var reading = new Reading
            {
                FilesDiscovered = total, FilesCompleted = done,
                ArchiveEntriesCompleted = entriesDone, CheckedObjects = physicalChecked + entriesDone,
                ThreatsFound = Volatile.Read(ref threatsFound),
                ArchivesDiscovered = foundArchives, ArchivesCompleted = finishedArchives,
                ArchivesPending = pendingArchives,
                BytesDiscovered = totalBytes, BytesRead = physicalRead + entryRead,
                ArchiveBytesRead = entryRead, Errors = Volatile.Read(ref ioErrors),
                EnumerationComplete = ready,
                CurrentPath = Interlocked.CompareExchange(ref currentPath, null, null),
                MiBPerSecond = (physicalRead + entryRead) / (1024d * 1024d * elapsed),
                Percent = canCalculate ? Math.Max(0, Math.Min(done < total ? 98 : 99,
                    (int)Math.Floor(fraction * 100))) : -1
            };
            // Chỉ ước lượng nếu đã thống kê xong và không còn archive chưa kiểm tra.
            // Gần cuối có quá ít số liệu để tính ETA chính xác: ưu tiên thông báo
            // 'Đang hoàn tất' thay vì 'còn ~1 giây' trong khi worker vẫn bận.
            if (canCalculate && total > 0 && elapsed >= 5 && fraction >= 0.05 &&
                fraction < 0.90 && done < total)
            {
                double seconds = elapsed * (1d - fraction) / fraction;
                if (!double.IsNaN(seconds) && !double.IsInfinity(seconds) && seconds >= 15)
                    reading.Remaining = TimeSpan.FromSeconds(Math.Min(7 * 86400, seconds));
            }
            return reading;
        }

        public static string DescribeRemaining(Reading reading)
        {
            if (reading == null || !reading.EnumerationComplete) return "Đang thống kê tệp…";
            if (reading.ArchivesPending > 0) return "Đang quét sâu tệp nén…";
            if (reading.Remaining.HasValue) return FormatRemaining(reading.Remaining);
            if (reading.FilesDiscovered > 0 && reading.FilesCompleted >= reading.FilesDiscovered)
                return "Đang hoàn tất…";
            return "Đang ước tính…";
        }
        public static string FormatRemaining(TimeSpan? remaining)
        {
            if (!remaining.HasValue) return "Đang ước tính…";
            TimeSpan value = remaining.Value;
            if (value.TotalHours >= 1) return string.Format("~{0} giờ {1} phút", (int)value.TotalHours, value.Minutes);
            if (value.TotalMinutes >= 1) return string.Format("~{0} phút", (int)Math.Ceiling(value.TotalMinutes));
            if (value.TotalSeconds < 15) return "Đang hoàn tất…";
            return "~" + ((int)Math.Ceiling(value.TotalSeconds / 10d) * 10) + " giây";
        }
    }

    /// <summary>
    /// Thứ tự phát hiện bắt buộc:
    ///  1) MD5/SHA-1/SHA-256 toàn tệp (OR); trùng thì dừng.
    ///  2) Sliding Window + Rolling Hash cắt CDC, SHA-256 chunk; trùng thì dừng.
    ///  3) Heuristic tĩnh dự phòng, chỉ báo NGHI VẤN; không xác nhận mã độc.
    /// Không thực thi nội dung tệp khi phân tích.
    /// </summary>
    public static class ScanEngine
    {
        // Giữ chuỗi mẫu để tạo tệp demo cũ; KHÔNG được nhận dạng theo tên/prefix.
        // Muốn mẫu demo được phát hiện, cần nạp hash SHA-256 của mẫu vào VirusSignatures.
        public const string TestSignature = "XVIRUS-TEST-SIGNATURE::";
        public const string SignatureSampleContent = "SIM-MALWARE-SIGNATURE::A57C1F37::PAYLOAD";
        private const int ProgressEveryNFiles = 100;
        private const int MaxParallelWorkers = 4;
        private const int FileChunkSize = 1024;

        // Cache thuộc giao diện bảo trì cũ; không được dùng để bỏ qua xác minh hai phương pháp băm.
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

        /// <summary>Heuristic (kể cả cảnh báo hành vi) chỉ là nghi vấn, cần người dùng xác nhận.</summary>
        public static bool IsHeuristicOnly(ThreatFound hit)
        {
            return hit != null && !string.IsNullOrWhiteSpace(hit.Kind) &&
                hit.Kind.StartsWith("Heuristic", StringComparison.OrdinalIgnoreCase);
        }

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
                if (string.IsNullOrWhiteSpace(sha256)) return false;

                detectionId = ThreatRepository.Add(null, null, info.Name, originalPath,
                    string.IsNullOrWhiteSpace(reason) ? "Không rõ" : reason,
                    info.Length, null, null, sha256);
                if (detectionId <= 0) return false;

                quarantinePath = Path.Combine(QuarantineDir, detectionId + ".qtn");
                if (File.Exists(quarantinePath)) return false;

                // Nhật ký bền vững ghi TRƯỚC khi thay đổi NTFS; không cần bảng SQL mới.
                string journalId = QuarantineRecoveryJournal.Begin("Quarantine", detectionId,
                    originalPath, quarantinePath, sha256);
                File.Move(originalPath, quarantinePath);
                moved = true;
                QuarantineRecoveryJournal.Stage(journalId, "FileMoved");

                if (!ThreatRepository.MarkQuarantined(detectionId, quarantinePath))
                {
                    try
                    {
                        if (File.Exists(quarantinePath) && !File.Exists(originalPath))
                        {
                            File.Move(quarantinePath, originalPath);
                            moved = false;
                            QuarantineRecoveryJournal.Stage(journalId, "RolledBack");
                        }
                    }
                    catch { }
                    return false;
                }

                try { QuarantineRecoveryJournal.Stage(journalId, "Completed"); }
                catch (Exception ex) { System.Diagnostics.Trace.WriteLine("Journal hoàn tất: " + ex.Message); }
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
            string error;
            return RestoreQuarantined(detectionId, out error);
        }

        /// <summary>
        /// Khoi phuc tu kho cach ly va thong bao ly do khi that bai.
        /// Khong thay doi bao ve cua Windows Defender, khong vo hieu hoa anti-virus.
        /// </summary>
        public static bool RestoreQuarantined(long detectionId, out string error)
        {
            error = null;
            string originalPath = null;
            string quarantinePath = null;
            string restoredPath = null;
            bool fileWasMoved = false;

            try
            {
                if (detectionId <= 0)
                {
                    error = "Mã bản ghi cách ly không hợp lệ.";
                    return false;
                }

                DataRow row = ThreatRepository.GetById(detectionId);
                if (row == null)
                {
                    error = "Không tìm thấy bản ghi DetectionID " + detectionId + " trong SQL Server.";
                    return false;
                }
                if (!string.Equals(DbString(row, "Status"), "Quarantined", StringComparison.OrdinalIgnoreCase))
                {
                    error = "Trạng thái SQL không còn là Quarantined; hãy làm mới danh sách.";
                    return false;
                }

                originalPath = DbString(row, "OriginalPath");
                quarantinePath = DbString(row, "QuarantinePath");
                if (string.IsNullOrWhiteSpace(originalPath))
                {
                    error = "SQL thiếu OriginalPath (đường dẫn gốc).";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(quarantinePath))
                {
                    error = "SQL thiếu QuarantinePath (vị trí tệp cách ly).";
                    return false;
                }
                if (!File.Exists(quarantinePath))
                {
                    error = "Không tìm thấy tệp vật lý trong kho cách ly: " + quarantinePath +
                        ". Có thể tệp đã bị Windows Security hoặc chương trình khác di chuyển/xóa. " +
                        "Kiểm tra Lịch sử bảo vệ của Windows Security.";
                    return false;
                }

                // F05: xác minh byte vật lý trước khi di chuyển; không tin metadata/trạng thái SQL đơn thuần.
                string storedSha256 = DbString(row, "FileSHA256");
                if (string.IsNullOrWhiteSpace(storedSha256) || storedSha256.Length != 64)
                {
                    error = "Bản ghi cách ly thiếu SHA-256 đối chiếu; cần xác minh thủ công trước khi khôi phục.";
                    return false;
                }
                string currentSha256 = ComputeFileSha256(quarantinePath);
                if (string.IsNullOrWhiteSpace(currentSha256) ||
                    !string.Equals(storedSha256.Trim(), currentSha256, StringComparison.OrdinalIgnoreCase))
                {
                    error = "SHA-256 tệp trong kho cách ly đã khác lúc lưu; không khôi phục để tránh mất an toàn dữ liệu.";
                    return false;
                }

                string directory = Path.GetDirectoryName(originalPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    error = "Đường dẫn gốc không hợp lệ: " + originalPath;
                    return false;
                }
                // Thu muc goc co the da mat (USB/drive Z: bi ngat ket noi).
                // De exception duoc phan loai thay vi nuot boi catch rong.
                Directory.CreateDirectory(directory);
                restoredPath = GetAvailableRestorePath(originalPath);
                string restoreJournalId = QuarantineRecoveryJournal.Begin("Restore", detectionId,
                    restoredPath, quarantinePath, storedSha256);
                File.Move(quarantinePath, restoredPath);
                fileWasMoved = true;
                QuarantineRecoveryJournal.Stage(restoreJournalId, "FileMoved");

                // Mot antivirus khac co the dua tep vua khoi phuc vao cach ly.
                // Khong danh dau Restored trong SQL neu tep da bien mat ngay sau Move.
                if (!File.Exists(restoredPath))
                {
                    error = "Tệp đã biến mất ngay sau khi khôi phục về: " + restoredPath +
                        ". Kiểm tra Lịch sử bảo vệ của Windows Security.";
                    return false;
                }

                if (!ThreatRepository.MarkRestored(detectionId))
                {
                    error = "Đã chuyển tệp nhưng SQL Server không cập nhật được trạng thái Restored.";
                    error += AttemptRestoreRollback(restoredPath, quarantinePath);
                    if (File.Exists(quarantinePath) && !File.Exists(restoredPath))
                        QuarantineRecoveryJournal.Stage(restoreJournalId, "RolledBack");
                    return false;
                }
                try { QuarantineRecoveryJournal.Stage(restoreJournalId, "Completed"); }
                catch (Exception ex) { System.Diagnostics.Trace.WriteLine("Journal khôi phục: " + ex.Message); }

                CacheEntry removed;
                Cache.TryRemove(originalPath, out removed);
                Cache.TryRemove(restoredPath, out removed);
                RaiseQuarantineChanged();
                return true;
            }
            catch (Exception ex)
            {
                error = DescribeRestoreFailure(ex, originalPath, quarantinePath);
                if (fileWasMoved && !string.IsNullOrWhiteSpace(restoredPath) &&
                    !string.IsNullOrWhiteSpace(quarantinePath))
                    error += AttemptRestoreRollback(restoredPath, quarantinePath);
                System.Diagnostics.Debug.WriteLine("[RestoreQuarantined] " + detectionId + ": " + ex);
                return false;
            }
        }

        private static string AttemptRestoreRollback(string restoredPath, string quarantinePath)
        {
            try
            {
                if (File.Exists(restoredPath) && !File.Exists(quarantinePath))
                {
                    File.Move(restoredPath, quarantinePath);
                    return " Đã chuyển tệp trở lại kho cách ly.";
                }
                if (!File.Exists(restoredPath) && !File.Exists(quarantinePath))
                    return " Không còn thấy tệp ở cả hai vị trí; kiểm tra Windows Security.";
                return " Kiểm tra lại trạng thái và đường dẫn tệp.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[RestoreQuarantined.Rollback] " + ex);
                return " Không thể đưa tệp về kho cách ly: " + ex.Message;
            }
        }

        private static string DescribeRestoreFailure(Exception ex, string originalPath, string quarantinePath)
        {
            if (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
                return "Bị từ chối quyền truy cập khi khôi phục tệp. Kiểm tra quyền ghi thư mục gốc và " +
                       "Lịch sử bảo vệ của Windows Security. Chi tiết: " + ex.Message;
            if (ex is DriveNotFoundException || ex is DirectoryNotFoundException)
                return "Ổ đĩa hoặc thư mục gốc không còn truy cập được: " + originalPath + ". " + ex.Message;
            if (ex is FileNotFoundException)
                return "Tệp trong kho cách ly không còn tồn tại: " + quarantinePath + ". " + ex.Message;
            if (ex is System.Data.SqlClient.SqlException)
                return "SQL Server không phản hồi hoặc từ chối cập nhật: " + ex.Message;
            if (ex is IOException)
                return "Lỗi I/O khi chuyển tệp (tệp đang được sử dụng, ổ đĩa không sẵn sàng hoặc " +
                       "Windows Security can thiệp). Chi tiết: " + ex.Message;
            return "Khôi phục thất bại (" + ex.GetType().Name + "): " + ex.Message;
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

        // One discovery policy for the scan engine and both scan selection views.
        // Snapshot ready drives; a disconnected device must not hide the other drives.
        public static DriveInfo[] GetFullScanDrives()
        {
            var drives = new List<DriveInfo>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType != DriveType.NoRootDirectory &&
                        drive.DriveType != DriveType.Unknown && drive.IsReady)
                        drives.Add(drive);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (System.Security.SecurityException) { }
            }
            return drives.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static IEnumerable<string> GetScanRoots(ScanType type, string customPath)
        {
            if (type == ScanType.Custom)
                return string.IsNullOrEmpty(customPath) || (!Directory.Exists(customPath) && !File.Exists(customPath))
                    ? Enumerable.Empty<string>()
                    : Enumerable.Repeat(customPath, 1);

            if (type == ScanType.Full)
                return GetFullScanDrives().Select(d => d.Name);

            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new[]
            {
                Path.Combine(profile, "Desktop"),
                Path.Combine(profile, "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.GetTempPath()
            }.Where(Directory.Exists);
        }

        /// <param name="progress">callback (số tệp đã quét, tệp hiện tại) — có thể được gọi từ nhiều thread nền</param>
        /// <exception cref="OperationCanceledException">bị hủy giữa chừng</exception>
        public static ScanResult Scan(ScanType type, string customPath, CancellationToken ct,
            Action<int, string> progress = null)
        {
            return Scan(type, customPath, ct, progress, null, null);
        }

        // Snapshot truyền từ phiên quét giúp chọn nhiều thư mục chỉ tải SQL đúng MỘT lần.
        public static ScanResult Scan(ScanType type, string customPath, CancellationToken ct,
            Action<int, string> progress, ScanTelemetry telemetry,
            HybridSignatureScanner.Snapshot suppliedSnapshot)
        {
            return Scan(type, customPath, ct, progress, telemetry, suppliedSnapshot, null);
        }

        // Các thư mục và tệp người dùng chọn được quét đồng thời trên MỘT hàng đợi.
        public static ScanResult Scan(ScanType type, string customPath, CancellationToken ct,
            Action<int, string> progress, ScanTelemetry telemetry,
            HybridSignatureScanner.Snapshot suppliedSnapshot, IEnumerable<string> selectedTargets)
        {
            ct.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            var result = new ScanResult();
            var signatureSnapshot = suppliedSnapshot ?? HybridSignatureScanner.GetSnapshot(true);
            if (telemetry != null) telemetry.Configure(signatureSnapshot);
            result.SignatureLoadWarning = signatureSnapshot.Error;
            result.SignatureSource = signatureSnapshot.SignatureSource;
            result.SignatureCreatedAtUtc = signatureSnapshot.SignatureCreatedAtUtc;
            result.SignatureIsDegraded = signatureSnapshot.IsDegraded;

            // Quét tùy chọn trỏ vào đúng 1 tệp:
            if (selectedTargets == null && type == ScanType.Custom && File.Exists(customPath))
            {
                ct.ThrowIfCancellationRequested();
                bool failedToRead;
                if (telemetry != null)
                {
                    telemetry.BeginDirectories(0);
                    telemetry.Discover(new[] { new FileInfo(customPath) });
                    telemetry.StartFile(customPath);
                }
                ThreatFound single = EvaluateFileWithSnapshot(customPath, false, signatureSnapshot, ct, out failedToRead, telemetry); 
                if (telemetry != null && failedToRead) telemetry.RecordError();
                if (single != null)
                {
                    result.Threats.Add(single);
                    if (telemetry != null) telemetry.RecordThreat();
                }
                result.FilesScanned = 1;
                result.ReadErrorCount = failedToRead ? 1 : 0;
                if (failedToRead)
                    result.Issues.Add(new ScanFileIssue { Path = customPath, Reason = "Tệp không đọc được." });
                // Dù hash của ZIP ngoài khớp, vẫn quét từng tệp bên trong để trả về entry cụ thể.
                if (ArchiveScanService.IsUnsupportedArchive(customPath))
                {
                    result.ArchiveEntriesUnchecked++;
                    result.Issues.Add(new ScanFileIssue { Path = customPath,
                        Reason = "Định dạng nén chưa hỗ trợ kiểm tra nội dung entry." });
                }
                if (ArchiveScanService.IsSupportedArchive(customPath))
                {
                    try
                    {
                        ArchiveScanService.Outcome archive = ArchiveScanService.Scan(customPath,
                            (staged, virtualPath) =>
                            {
                                if (telemetry != null) telemetry.StartFile(virtualPath);
                                bool entryError;
                                ThreatFound hit = EvaluateFileWithSnapshot(staged, false, signatureSnapshot,
                                    ct, out entryError, telemetry, true);
                                if (!entryError && telemetry != null) telemetry.FinishArchiveEntry();
                                if (hit != null)
                                {
                                    hit.FilePath = virtualPath;
                                    hit.PhysicalPath = customPath;
                                    hit.ArchiveEntryPath = virtualPath.Substring(customPath.Length + 3);
                                    result.Threats.Add(hit);
                                    if (telemetry != null) telemetry.RecordThreat();
                                }
                                return !entryError;
                            },
                            (virtualPath, why) => result.Issues.Add(new ScanFileIssue { Path = virtualPath, Reason = why }), ct);
                        result.ArchiveEntriesScanned = archive.Examined;
                        result.ArchiveEntriesUnchecked = archive.Unchecked;
                    }
                    finally { if (telemetry != null) telemetry.FinishArchive(); }
                }
                if (telemetry != null) telemetry.FinishFile(!failedToRead);
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                if (progress != null) progress(1, customPath);
                ct.ThrowIfCancellationRequested();
                return result;
            }

            var locations = selectedTargets == null
                ? GetScanRoots(type, customPath).ToArray()
                : selectedTargets.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            string[] roots = locations.Where(Directory.Exists).ToArray();
            FileInfo[] directFiles = locations.Where(File.Exists).Select(p => new FileInfo(p)).ToArray();
            result.MissingTargets = locations.Length - roots.Length - directFiles.Length;

            if (roots.Length > 0 || directFiles.Length > 0)
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
                    Cancellation = ct,
                    Signatures = signatureSnapshot,
                    Telemetry = telemetry,
                    SeenFiles = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase),
                    SeenDirectories = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase),
                    Issues = new ConcurrentQueue<ScanFileIssue>()
                };

                // Cho phép 2 worker trên máy 2 CPU; tối đa 4 để tránh nghẽn đĩa.
                int workerCount = Math.Max(1, Math.Min(MaxParallelWorkers, Environment.ProcessorCount));
                if (telemetry != null) telemetry.BeginDirectories(roots.Length);

                foreach (string root in roots)
                {
                    completion.AddCount();
                    work.Add(new WorkItem { Path = root, IsDirectory = true });
                }

                // Các đường dẫn trỏ thẳng tới tệp cũng tham gia hàng đợi này.
                for (int start = 0; start < directFiles.Length; start += FileChunkSize)
                {
                    FileInfo[] batch = directFiles.Skip(start).Take(FileChunkSize).ToArray();
                    QueueFileChunk(new List<FileInfo>(batch), state);
                }
                completion.Signal();
                for (int i = 0; i < workerCount; i++)
                    ThreadPool.QueueUserWorkItem(delegate { RunWorker(state); });
                try
                {
                    completion.Wait(ct);
                }
                finally
                {
                    work.CompleteAdding();
                }

                // Danh sách hiển thị và lịch sử chỉ nhận đường dẫn của TỪNG TỆP,
                // không gom các kết quả thành một dòng tương ứng với thư mục gốc.
                foreach (ThreatFound threat in state.Threats.OrderBy(t => t.FilePath, StringComparer.OrdinalIgnoreCase))
                    result.Threats.Add(threat);
                result.FilesScanned = state.Scanned;
                result.ReadErrorCount = state.ReadErrors;
                result.DirectoriesScanned = state.DirectoriesScanned;
                result.DirectoriesSkipped = state.DirectoriesSkipped;
                result.EnumerationErrors = state.EnumerationErrors;
                result.ArchiveEntriesScanned = state.ArchiveEntriesScanned;
                result.ArchiveEntriesUnchecked = state.ArchiveEntriesUnchecked;
                foreach (ScanFileIssue item in state.Issues.Take(2000)) result.Issues.Add(item);
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
            return EvaluateFileWithSnapshot(path, forceContent,
                HybridSignatureScanner.GetSnapshot(false), CancellationToken.None);
        }

        private static ThreatFound EvaluateFileWithSnapshot(string path, bool forceContent,
            HybridSignatureScanner.Snapshot snapshot, CancellationToken cancellation)
        {
            bool ignored;
            return EvaluateFileWithSnapshot(path, forceContent, snapshot, cancellation, out ignored);
        }

        private static ThreatFound EvaluateFileWithSnapshot(string path, bool forceContent,
            HybridSignatureScanner.Snapshot snapshot, CancellationToken cancellation, out bool readError,
            ScanTelemetry telemetry = null, bool archiveEntry = false)
        {
            readError = false;
            FileInfo file;
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    readError = true;
                    return null;
                }
                file = new FileInfo(path);
            }
            catch (Exception)
            {
                readError = true;
                return null;
            }

            string kind, reason;
            long signatureId;
            if (Evaluate(file, out kind, out reason, out signatureId, out readError, forceContent,
                snapshot, cancellation, telemetry, archiveEntry))
                return new ThreatFound { FilePath = file.FullName, Kind = kind, Reason = reason,
                    SignatureID = signatureId > 0 ? (long?)signatureId : null };
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
                        if (item.IsDirectory && state.Telemetry != null)
                            state.Telemetry.DirectoryFinished();
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
            if (ArchiveScanService.IsStagingDirectory(folder)) return;

            try
            {
                if (!Directory.Exists(folder))
                {
                    Interlocked.Increment(ref state.DirectoriesSkipped);
                    state.Issues.Enqueue(new ScanFileIssue { Path = folder, Reason = "Thư mục không tồn tại hoặc không truy cập được." });
                    return;
                }
                // Đường dẫn thư mục bị chọn lặp hoặc nằm trong một thư mục khác
                // chỉ quét một lần để tránh báo cùng một tệp nhiều lần.
                if (!state.SeenDirectories.TryAdd(Path.GetFullPath(folder), 0)) return;
                if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
                {
                    Interlocked.Increment(ref state.DirectoriesSkipped);
                    state.Issues.Enqueue(new ScanFileIssue { Path = folder, Reason = "Liên kết thư mục được bỏ qua để tránh quét lặp." });
                    return;
                }
                Interlocked.Increment(ref state.DirectoriesScanned);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                ex is System.Security.SecurityException || ex is ArgumentException)
            {
                Interlocked.Increment(ref state.DirectoriesSkipped);
                Interlocked.Increment(ref state.EnumerationErrors);
                state.Issues.Enqueue(new ScanFileIssue { Path = folder, Reason = "Không thể duyệt thư mục: " + ex.GetType().Name });
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
                        {
                            Interlocked.Increment(ref state.DirectoriesSkipped);
                            continue;
                        }

                        state.Completion.AddCount();
                        if (state.Telemetry != null) state.Telemetry.AddDirectory();
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
                            if (state.Telemetry != null) state.Telemetry.DirectoryFinished();
                            throw;
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (UnauthorizedAccessException) { Interlocked.Increment(ref state.DirectoriesSkipped); Interlocked.Increment(ref state.EnumerationErrors); }
                    catch (IOException) { Interlocked.Increment(ref state.DirectoriesSkipped); Interlocked.Increment(ref state.EnumerationErrors); }
                    catch (Exception) { Interlocked.Increment(ref state.DirectoriesSkipped); Interlocked.Increment(ref state.EnumerationErrors); }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (UnauthorizedAccessException) { Interlocked.Increment(ref state.EnumerationErrors); }
            catch (IOException) { Interlocked.Increment(ref state.EnumerationErrors); }
            catch (Exception) { Interlocked.Increment(ref state.EnumerationErrors); }

            // 2) Liệt kê file trong chính thư mục hiện tại và chia thành từng chunk.
            // Nếu bộ liệt kê gặp lỗi giữa chừng, vẫn quét tất cả các tệp
            // đã lấy được trước đó (không làm mất nhóm cuối chưa đủ 1024).
            var chunk = new List<FileInfo>(FileChunkSize);
            try
            {
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
                    catch (UnauthorizedAccessException) { Interlocked.Increment(ref state.EnumerationErrors); }
                    catch (IOException) { Interlocked.Increment(ref state.EnumerationErrors); }
                    catch (Exception) { Interlocked.Increment(ref state.EnumerationErrors); }
                }

            }
            catch (OperationCanceledException) { throw; }
            catch (UnauthorizedAccessException) { Interlocked.Increment(ref state.EnumerationErrors); }
            catch (IOException) { Interlocked.Increment(ref state.EnumerationErrors); }
            catch (Exception) { Interlocked.Increment(ref state.EnumerationErrors); }
            finally
            {
                // Nếu hủy, không thêm công việc mới. Trong các trường hợp còn
                // lại, không bỏ sót nhóm tệp cuối khi việc liệt kê thất bại.
                if (!state.Cancellation.IsCancellationRequested && chunk.Count > 0)
                    QueueFileChunk(chunk, state);
            }
        }

        /// <summary>
        /// Đưa một nhóm file vào hàng đợi quét. Nếu thêm thất bại thì hoàn lại
        /// CountdownEvent để phiên quét không bị chờ vô hạn.
        /// </summary>
        private static void QueueFileChunk(List<FileInfo> files, ScanState state)
        {
            if (files == null || files.Count == 0) return;

            state.Cancellation.ThrowIfCancellationRequested();

            // Một tệp có thể xuất hiện ở nhiều phạm vi chồng nhau
            // (VD: D:\Data và D:\Data\Sub). Quét đúng một lần mỗi đường dẫn.
            var unique = new List<FileInfo>(files.Count);
            foreach (FileInfo info in files)
            {
                if (info != null && state.SeenFiles.TryAdd(info.FullName, 0))
                    unique.Add(info);
            }
            if (unique.Count == 0) return;
            FileInfo[] array = unique.ToArray();
            if (state.Telemetry != null) state.Telemetry.Discover(array);
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

            bool successfullyChecked = false;
            try
            {
                if (state.Telemetry != null) state.Telemetry.StartFile(fullPath);
                string kind, reason;
                long signatureId;
                bool readError;
                bool detected = Evaluate(file, out kind, out reason, out signatureId, out readError,
                    false, state.Signatures, state.Cancellation, state.Telemetry);
                successfullyChecked = !readError;
                if (readError)
                {
                    Interlocked.Increment(ref state.ReadErrors);
                    state.Issues.Enqueue(new ScanFileIssue { Path = fullPath, Reason = "Không thể đọc/đối chiếu tệp." });
                    if (state.Telemetry != null) state.Telemetry.RecordError();
                }
                if (ArchiveScanService.IsUnsupportedArchive(fullPath))
                {
                    Interlocked.Increment(ref state.ArchiveEntriesUnchecked);
                    state.Issues.Enqueue(new ScanFileIssue { Path = fullPath,
                        Reason = "Chưa hỗ trợ kiểm tra tệp thành phần bên trong định dạng nén này." });
                }
                if (ArchiveScanService.IsSupportedArchive(fullPath))
                {
                    try
                    {
                        ArchiveScanService.Outcome archive = ArchiveScanService.Scan(fullPath,
                            (staged, virtualPath) =>
                            {
                                if (state.Telemetry != null) state.Telemetry.StartFile(virtualPath);
                                bool entryReadError;
                                ThreatFound entryHit = EvaluateFileWithSnapshot(staged, false,
                                    state.Signatures, state.Cancellation, out entryReadError, state.Telemetry, true);
                                if (!entryReadError && state.Telemetry != null)
                                    state.Telemetry.FinishArchiveEntry();
                                if (entryHit != null)
                                {
                                    entryHit.FilePath = virtualPath;
                                    entryHit.PhysicalPath = fullPath;
                                    entryHit.ArchiveEntryPath = virtualPath.Substring(fullPath.Length + 3);
                                    state.Threats.Enqueue(entryHit);
                                    if (state.Telemetry != null) state.Telemetry.RecordThreat();
                                }
                                return !entryReadError;
                            },
                            (virtualPath, why) => state.Issues.Enqueue(new ScanFileIssue { Path = virtualPath, Reason = why }),
                            state.Cancellation);
                        Interlocked.Add(ref state.ArchiveEntriesScanned, archive.Examined);
                        Interlocked.Add(ref state.ArchiveEntriesUnchecked, archive.Unchecked);
                    }
                    finally { if (state.Telemetry != null) state.Telemetry.FinishArchive(); }
                }
                if (detected)
                {
                    if (state.Telemetry != null && !string.IsNullOrEmpty(reason) &&
                        reason.StartsWith("Hash toàn tệp", StringComparison.Ordinal))
                    {
                        try { state.Telemetry.SkipCdcPass(file.Length); } catch (Exception) { }
                    }
                    state.Threats.Enqueue(new ThreatFound
                    {
                        FilePath = fullPath,
                        Kind = kind,
                        Reason = reason,
                        SignatureID = signatureId > 0 ? (long?)signatureId : null
                    });
                    if (state.Telemetry != null) state.Telemetry.RecordThreat();
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                // Ghi nhận lỗi quét thay vì âm thầm coi tệp là không khớp chữ ký.
                successfullyChecked = false;
                Interlocked.Increment(ref state.ReadErrors);
                state.Issues.Enqueue(new ScanFileIssue { Path = fullPath, Reason = "Lỗi xử lý tệp (quyền hoặc I/O)." });
                if (state.Telemetry != null) state.Telemetry.RecordError();
            }
            finally
            {
                if (state.Telemetry != null) state.Telemetry.FinishFile(successfullyChecked);
                // File đã được engine thử xử lý, kể cả khi file bị khóa/lỗi đọc.
                int scanned = Interlocked.Increment(ref state.Scanned);

                if (state.Progress != null && (scanned == 1 || scanned % ProgressEveryNFiles == 0))
                {
                    try { state.Progress(scanned, fullPath); }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// Hash toàn tệp -> CDC -> heuristic tĩnh. forceContent giữ tương thích API.
        /// </summary>
        private static bool Evaluate(FileInfo file, out string kind, out string reason,
            out long signatureId, out bool readError, bool forceContent,
            HybridSignatureScanner.Snapshot snapshot, CancellationToken cancellation,
            ScanTelemetry telemetry = null, bool archiveEntry = false)
        {
            readError = false;
            signatureId = 0;
            kind = null;
            reason = null;
            cancellation.ThrowIfCancellationRequested();
            Action<long> countRead = telemetry == null ? (Action<long>)null :
                (archiveEntry ? (Action<long>)telemetry.ReadArchiveBytes : telemetry.ReadBytes);
            try
            {
                // Phương pháp 1 và 2: khớp chữ ký là xác nhận và DỪNG NGAY.
                if (snapshot != null && snapshot.HasSignatures)
                {
                    HybridSignatureScanner.Match hit = HybridSignatureScanner.ScanFile(file.FullName, snapshot, cancellation,
                        countRead);
                    if (hit != null)
                    {
                        kind = "Chữ ký";
                        signatureId = hit.SignatureID;
                        reason = hit.Technique + ": " + hit.Evidence;
                        return true;
                    }
                }

                // Phương pháp 3: chỉ thực hiện khi cả hai lớp hash đều không khớp.
                // Heuristic KHÔNG xác nhận mẫu mã độc: chỉ tạo cảnh báo nghi vấn có bằng chứng.
                StaticHeuristicAnalyzer.Finding suspicious = StaticHeuristicAnalyzer.Analyze(
                    file.FullName, cancellation, countRead);
                if (suspicious == null) return false;
                // Ngoại lệ được giới hạn ở heuristic, không can thiệp phát hiện hash/CDC.
                if (ScanExclusionService.IsHeuristicExcluded(file.FullName)) return false;
                kind = "Heuristic";
                reason = "Nghi vấn " + suspicious.Score + "/100: " + suspicious.Evidence;
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (IOException) { readError = true; return false; }
            catch (UnauthorizedAccessException) { readError = true; return false; }
            catch (System.Security.SecurityException) { readError = true; return false; }
        }

        private static string ToHexLower(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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

        // Định dạng cache cũ giữ để tương thích giao diện. Engine không dựa vào cache để phát hiện.
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
            public int ReadErrors;
            public int DirectoriesScanned;
            public int DirectoriesSkipped;
            public int EnumerationErrors;
            public int ArchiveEntriesScanned;
            public int ArchiveEntriesUnchecked;
            public ConcurrentQueue<ScanFileIssue> Issues;
            public ConcurrentDictionary<string, byte> SeenFiles;
            public ConcurrentDictionary<string, byte> SeenDirectories;
            public ConcurrentQueue<ThreatFound> Threats;
            public BlockingCollection<WorkItem> Work;
            public CountdownEvent Completion;
            public Action<int, string> Progress;
            public CancellationToken Cancellation;
            public HybridSignatureScanner.Snapshot Signatures;
            public ScanTelemetry Telemetry;
        }
    }
}
