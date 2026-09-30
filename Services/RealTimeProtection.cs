using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Kỹ thuật 3 — giám sát hành vi/bảo vệ thời gian thực:
    /// FileSystemWatcher theo dõi các khu vực nóng; mỗi khi tệp mới xuất hiện/ghi thay đổi,
    /// quét ngay (chữ ký + heuristic) mà không đợi người dùng bấm Quét.
    /// </summary>
    /*public static class RealTimeProtection
    {
        private static readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private static readonly object Sync = new object();
        // path -> "length|ticks": chặn thông báo lặp khi FileSystemWatcher bắn nhiều event cho 1 lần ghi
        private static readonly ConcurrentDictionary<string, string> Processed =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static bool AutoQuarantine { get; set; }
        public static event Action<ThreatFound> ThreatDetected;
        public static event Action<bool> StatusChanged;

        public static bool IsRunning
        {
            get { lock (Sync) { return watchers.Count > 0; } }
        }

        // Mặc định giám sát đúng phạm vi quét nhanh: Desktop, Downloads, Temp.
        public static void Start()
        {
            Start(ScanEngine.GetScanRoots(ScanType.Quick, null));
        }

        /// <exception cref="IOException">không tạo được watcher (mất quyền truy cập thư mục)</exception>
        public static void Start(IEnumerable<string> roots)
        {
            lock (Sync)
            {
                if (watchers.Count > 0) return;
                foreach (string root in roots)
                {
                    if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                    var w = new FileSystemWatcher(root);
                    w.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
                        | NotifyFilters.Size | NotifyFilters.CreationTime;
                    w.IncludeSubdirectories = true;
                    w.InternalBufferSize = 64 * 1024; // tránh tràn bộ đệm khi nhiều tệp đổi cùng lúc
                    w.Created += OnFileSystemEvent;
                    w.Changed += OnFileSystemEvent;
                    w.Renamed += OnRenamedEvent;
                    w.EnableRaisingEvents = true;
                    watchers.Add(w);
                }
            }
            RaiseStatus();
        }

        public static void Stop()
        {
            lock (Sync)
            {
                foreach (var w in watchers)
                {
                    w.EnableRaisingEvents = false;
                    w.Dispose();
                }
                watchers.Clear();
                Processed.Clear();
            }
            RaiseStatus();
        }

        private static void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            Handle(e.FullPath);
        }

        private static void OnRenamedEvent(object sender, RenamedEventArgs e)
        {
            Handle(e.FullPath);
        }

        private static void Handle(string path)
        {
            // Đợi bên ghi xong file rồi mới mở (tránh đọc tệp đang bị khóa giữa chừng)
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    Thread.Sleep(700);
                    if (!IsRunning) return;
                    ThreatFound hit = ScanEngine.EvaluateFile(path);
                    if (hit == null) return;

                    string sig;
                    try
                    {
                        var fi = new FileInfo(path);
                        sig = fi.Length + "|" + fi.LastWriteTimeUtc.Ticks;
                    }
                    catch (Exception) { return; } // tệp biến mất giữa chừng (antivirus khác/tự xóa)

                    string prev;
                    if (Processed.TryGetValue(path, out prev) && prev == sig) return; // cùng 1 phiên bản tệp
                    if (Processed.Count > 100000) Processed.Clear();
                    Processed[path] = sig;

                    // Ghi lịch sử thật cho hành vi phát hiện tức thì (tab Lịch sử đọc từ đây)
                    ScanHistoryStore.Add("Bảo vệ thời gian thực", path, 0, 1, 0);

                    Action<ThreatFound> handler = ThreatDetected;
                    if (handler != null) handler(hit);
                    if (AutoQuarantine) ScanEngine.Quarantine(path, hit.Kind + ": " + hit.Reason);
                }
                catch (Exception) { } // watcher nền không bao giờ được làm chết app
            });
        }

        private static void RaiseStatus()
        {
            Action<bool> handler = StatusChanged;
            if (handler != null) handler(IsRunning);
        }
    }*/
    /// <summary>
    /// Bảo vệ thời gian thực.
    ///
    /// Theo dõi các thư mục quan trọng bằng FileSystemWatcher.
    /// Khi tệp được tạo mới, thay đổi hoặc đổi tên:
    ///
    /// 1. Đợi quá trình ghi tệp hoàn tất.
    /// 2. Gọi ScanEngine.EvaluateFile() để kiểm tra.
    /// 3. Nếu phát hiện mối đe dọa:
    ///    - Ghi lịch sử qua ScanHistoryStore.
    ///    - Phát sự kiện ThreatDetected cho giao diện.
    ///    - Tự động cách ly nếu AutoQuarantine = true.
    ///
    /// Class này KHÔNG truy cập SQL Server trực tiếp.
    /// Việc lưu dữ liệu được thực hiện thông qua:
    ///
    /// ScanHistoryStore -> ScanHistoryRepository -> dbo.ScanHistory
    ///
    /// ScanEngine -> VirusSignatureRepository -> dbo.VirusSignatures
    ///
    /// ScanEngine.Quarantine -> ThreatDetectionRepository
    ///                       -> dbo.ThreatDetections
    /// </summary>
    public static class RealTimeProtection
    {
        // =========================================================
        // WATCHERS
        // =========================================================

        private static readonly List<FileSystemWatcher> watchers =
            new List<FileSystemWatcher>();

        private static readonly object Sync =
            new object();

        /*
         * Lưu trạng thái file đã xử lý:
         *
         * path -> "length|LastWriteTimeUtc.Ticks"
         *
         * FileSystemWatcher có thể phát nhiều event
         * cho cùng một lần ghi file.
         *
         * Dictionary này giúp tránh cảnh báo trùng.
         */
        private static readonly ConcurrentDictionary<string, string>
            Processed =
                new ConcurrentDictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

        // =========================================================
        // SETTINGS
        // =========================================================

        /// <summary>
        /// Nếu true, tệp được phát hiện sẽ tự động được
        /// chuyển vào khu cách ly.
        /// </summary>
        public static bool AutoQuarantine { get; set; }

        // =========================================================
        // EVENTS
        // =========================================================

        /// <summary>
        /// Phát khi bảo vệ thời gian thực phát hiện mối đe dọa.
        /// </summary>
        public static event Action<ThreatFound> ThreatDetected;

        /// <summary>
        /// Phát khi trạng thái Real-time Protection thay đổi.
        /// true = đang chạy.
        /// false = đã dừng.
        /// </summary>
        public static event Action<bool> StatusChanged;

        // =========================================================
        // STATUS
        // =========================================================

        public static bool IsRunning
        {
            get
            {
                lock (Sync)
                {
                    return watchers.Count > 0;
                }
            }
        }

        // =========================================================
        // START
        // =========================================================

        /// <summary>
        /// Bật bảo vệ thời gian thực với phạm vi mặc định.
        ///
        /// Phạm vi mặc định giống Quick Scan của ScanEngine.
        /// </summary>
        public static void Start()
        {
            IEnumerable<string> roots =
                ScanEngine.GetScanRoots(
                    ScanType.Quick,
                    null);

            Start(roots);
        }

        /// <summary>
        /// Bật bảo vệ thời gian thực trên các thư mục được truyền vào.
        /// </summary>
        public static void Start(
            IEnumerable<string> roots)
        {
            if (roots == null)
                return;

            bool changed = false;

            lock (Sync)
            {
                // Đã chạy rồi thì không tạo watcher lần nữa.
                if (watchers.Count > 0)
                    return;

                foreach (string root in roots)
                {
                    if (string.IsNullOrWhiteSpace(root))
                        continue;

                    try
                    {
                        if (!Directory.Exists(root))
                            continue;

                        FileSystemWatcher watcher =
                            new FileSystemWatcher(root);

                        watcher.NotifyFilter =
                            NotifyFilters.FileName |
                            NotifyFilters.LastWrite |
                            NotifyFilters.Size |
                            NotifyFilters.CreationTime;

                        watcher.IncludeSubdirectories = true;

                        /*
                         * FileSystemWatcher sử dụng bộ đệm
                         * không phân trang của Windows.
                         *
                         * 64 KB đủ lớn để hạn chế mất event,
                         * nhưng không nên tăng quá mức.
                         */
                        watcher.InternalBufferSize =
                            64 * 1024;

                        watcher.Created +=
                            OnFileSystemEvent;

                        watcher.Changed +=
                            OnFileSystemEvent;

                        watcher.Renamed +=
                            OnRenamedEvent;

                        watcher.EnableRaisingEvents =
                            true;

                        watchers.Add(watcher);

                        changed = true;
                    }
                    catch (IOException)
                    {
                        // Không tạo được watcher cho root này.
                        // Tiếp tục thử root khác.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Không có quyền theo dõi thư mục.
                        // Tiếp tục root khác.
                    }
                    catch (ArgumentException)
                    {
                        // Đường dẫn không hợp lệ.
                    }
                }
            }

            if (changed)
            {
                RaiseStatus();
            }
        }

        // =========================================================
        // STOP
        // =========================================================

        /// <summary>
        /// Dừng toàn bộ bảo vệ thời gian thực.
        /// </summary>
        public static void Stop()
        {
            bool changed = false;

            lock (Sync)
            {
                if (watchers.Count > 0)
                {
                    changed = true;
                }

                foreach (
                    FileSystemWatcher watcher
                    in watchers)
                {
                    try
                    {
                        watcher.EnableRaisingEvents =
                            false;

                        watcher.Created -=
                            OnFileSystemEvent;

                        watcher.Changed -=
                            OnFileSystemEvent;

                        watcher.Renamed -=
                            OnRenamedEvent;

                        watcher.Dispose();
                    }
                    catch (Exception)
                    {
                        // Không để một watcher lỗi
                        // làm gián đoạn việc dừng các watcher khác.
                    }
                }

                watchers.Clear();

                Processed.Clear();
            }

            if (changed)
            {
                RaiseStatus();
            }
        }

        // =========================================================
        // FILE SYSTEM EVENTS
        // =========================================================

        private static void OnFileSystemEvent(
            object sender,
            FileSystemEventArgs e)
        {
            if (e == null)
                return;

            Handle(e.FullPath);
        }

        private static void OnRenamedEvent(
            object sender,
            RenamedEventArgs e)
        {
            if (e == null)
                return;

            Handle(e.FullPath);
        }

        // =========================================================
        // HANDLE FILE
        // =========================================================

        private static void Handle(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            /*
             * Không xử lý trực tiếp trong event của
             * FileSystemWatcher.
             *
             * Nếu quét ngay trong event, watcher có thể
             * bị block khi ScanEngine mất nhiều thời gian.
             */
            ThreadPool.QueueUserWorkItem(
                delegate
                {
                    try
                    {
                        /*
                         * Đợi ứng dụng/trình duyệt hoàn tất
                         * quá trình ghi file.
                         */
                        Thread.Sleep(700);

                        // Real-time đã bị tắt trong lúc chờ.
                        if (!IsRunning)
                            return;

                        // File có thể đã bị xóa.
                        if (!File.Exists(path))
                            return;

                        // =====================================
                        // QUÉT FILE
                        // =====================================

                        ThreatFound hit =
                            ScanEngine.EvaluateFile(path);

                        if (hit == null)
                            return;

                        // =====================================
                        // TẠO CHỮ KÝ PHIÊN BẢN FILE
                        // =====================================

                        string fileVersion;

                        try
                        {
                            FileInfo fileInfo =
                                new FileInfo(path);

                            fileVersion =
                                fileInfo.Length +
                                "|" +
                                fileInfo.LastWriteTimeUtc.Ticks;
                        }
                        catch (IOException)
                        {
                            return;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            return;
                        }
                        catch (Exception)
                        {
                            return;
                        }

                        // =====================================
                        // CHỐNG EVENT TRÙNG
                        // =====================================

                        string previousVersion;

                        if (Processed.TryGetValue(
                                path,
                                out previousVersion))
                        {
                            if (previousVersion ==
                                fileVersion)
                            {
                                return;
                            }
                        }

                        /*
                         * Không để dictionary tăng vô hạn
                         * trong trường hợp ứng dụng chạy lâu.
                         */
                        if (Processed.Count > 100000)
                        {
                            Processed.Clear();
                        }

                        Processed[path] =
                            fileVersion;

                        // =====================================
                        // GHI LỊCH SỬ
                        // =====================================

                        /*
                         * KHÔNG ghi SQL trực tiếp tại đây.
                         *
                         * Sau khi ScanHistoryStore được chuyển
                         * sang Repository, lời gọi này sẽ:
                         *
                         * ScanHistoryStore
                         *      ↓
                         * ScanHistoryRepository
                         *      ↓
                         * dbo.ScanHistory
                         */
                        ScanHistoryStore.Add(
                            "Bảo vệ thời gian thực",
                            path,
                            0,
                            1,
                            0);

                        // =====================================
                        // THÔNG BÁO CHO UI
                        // =====================================

                        Action<ThreatFound> handler =
                            ThreatDetected;

                        if (handler != null)
                        {
                            handler(hit);
                        }

                        // =====================================
                        // TỰ ĐỘNG CÁCH LY
                        // =====================================

                        if (AutoQuarantine)
                        {
                            /*
                             * ScanEngine.Quarantine() sẽ được
                             * chuyển sang ThreatDetections.
                             *
                             * RealTimeProtection không cần biết
                             * SQL Server hoạt động như thế nào.
                             */
                            if (File.Exists(path))
                            {
                                ScanEngine.Quarantine(
                                    path,
                                    hit.Kind +
                                    ": " +
                                    hit.Reason);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        /*
                         * Đây là thread nền.
                         *
                         * Một file bị lỗi không được phép
                         * làm ứng dụng bị crash.
                         */
                    }
                });
        }

        // =========================================================
        // STATUS EVENT
        // =========================================================

        private static void RaiseStatus()
        {
            Action<bool> handler =
                StatusChanged;

            if (handler != null)
            {
                handler(IsRunning);
            }
        }
    }
}
