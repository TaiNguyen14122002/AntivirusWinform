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
    public static class RealTimeProtection
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
    }
}
