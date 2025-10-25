using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Bảo vệ thời gian thực theo hàng đợi hữu hạn. Watcher KHÔNG bao giờ quét trong callback.
    /// Overflow/lỗi watcher được công bố rõ, chạy quét bù thay vì tuyên bố máy an toàn.
    /// Chỉ hoạt động khi tiến trình đang chạy, KHÔNG phải Windows Service.
    /// </summary>
    public static class RealTimeProtection
    {
        private const int MaxPending = 2048;
        private const int Workers = 2;
        private static readonly object Sync = new object();
        private static readonly List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();
        private static readonly ConcurrentDictionary<string, byte> Pending =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, string> Processed =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, byte> Recovering =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private static BlockingCollection<string> queue;
        private static CancellationTokenSource stop;
        private static int droppedEvents;
        private static long scannedEvents;
        private static string lastError;

        public static bool AutoQuarantine { get; set; }
        public static event Action<ThreatFound> ThreatDetected;
        public static event Action<bool> StatusChanged;
        public static string LastWatcherError { get { return Volatile.Read(ref lastError); } }
        public static int PendingCount { get { return Pending.Count; } }
        public static int DroppedEvents { get { return Volatile.Read(ref droppedEvents); } }
        public static long ScannedEvents { get { return Interlocked.Read(ref scannedEvents); } }
        public static bool IsDegraded { get { return !string.IsNullOrWhiteSpace(LastWatcherError) || DroppedEvents > 0; } }
        public static bool IsRunning { get { lock (Sync) return Watchers.Count > 0; } }
        /// <summary>Tránh quét trùng khi File Guard và bảo vệ realtime cùng theo dõi một tệp.</summary>
        public static bool MonitorsPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            lock (Sync)
                return Watchers.Any(w => path.StartsWith(w.Path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));
        }

        public static void Start()
        {
            Start(ScanEngine.GetScanRoots(ScanType.Quick, null));
        }

        public static void Start(IEnumerable<string> roots)
        {
            if (roots == null) return;
            bool changed = false;
            lock (Sync)
            {
                if (Watchers.Count > 0) return;
                stop = new CancellationTokenSource();
                queue = new BlockingCollection<string>(new ConcurrentQueue<string>(), MaxPending);
                Pending.Clear();
                Processed.Clear();
                Recovering.Clear();
                Interlocked.Exchange(ref droppedEvents, 0);
                Volatile.Write(ref lastError, null);
                foreach (string root in roots)
                {
                    if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;
                    try
                    {
                        var watcher = CreateWatcher(root);
                        try { watcher.EnableRaisingEvents = true; }
                        catch { watcher.Dispose(); throw; }
                        Watchers.Add(watcher);
                        changed = true;
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                        ex is ArgumentException || ex is System.Security.SecurityException)
                    {
                        Volatile.Write(ref lastError, "Không theo dõi được " + root + ": " + ex.GetType().Name);
                    }
                }
                if (changed)
                {
                    CancellationToken token = stop.Token;
                    BlockingCollection<string> localQueue = queue;
                    for (int i = 0; i < Workers; i++)
                        ThreadPool.QueueUserWorkItem(_ => Worker(localQueue, token));
                }
                else
                {
                    queue.Dispose(); queue = null;
                    stop.Dispose(); stop = null;
                    Volatile.Write(ref lastError, "Không có thư mục nào có thể giám sát.");
                }
            }
            RaiseStatus();
        }

        private static FileSystemWatcher CreateWatcher(string root)
        {
            var watcher = new FileSystemWatcher(root)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
                    NotifyFilters.Size | NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
                InternalBufferSize = 64 * 1024
            };
            watcher.Created += OnEvent;
            watcher.Changed += OnEvent;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;
            return watcher;
        }

        public static void Stop()
        {
            CancellationTokenSource previous = null;
            lock (Sync)
            {
                foreach (var watcher in Watchers)
                {
                    try
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.Created -= OnEvent;
                        watcher.Changed -= OnEvent;
                        watcher.Renamed -= OnRenamed;
                        watcher.Error -= OnError;
                        watcher.Dispose();
                    }
                    catch (Exception) { }
                }
                Watchers.Clear();
                previous = stop;
                stop = null;
                if (queue != null)
                {
                    try { queue.CompleteAdding(); } catch (InvalidOperationException) { }
                    queue = null; // worker vẫn giữ tham chiếu; worker tự thoát theo token
                }
                Pending.Clear();
                Processed.Clear();
            }
            if (previous != null) previous.Cancel(); // không dispose ngay vì worker dùng token
            RaiseStatus();
        }

        private static void OnEvent(object sender, FileSystemEventArgs e) { if (e != null) Enqueue(e.FullPath); }
        private static void OnRenamed(object sender, RenamedEventArgs e) { if (e != null) Enqueue(e.FullPath); }

        private static void Enqueue(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            BlockingCollection<string> work = queue;
            if (work == null || work.IsAddingCompleted || !Pending.TryAdd(path, 0)) return;
            bool queued = false;
            try { queued = work.TryAdd(path); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            if (!queued)
            {
                byte ignored; Pending.TryRemove(path, out ignored);
                Interlocked.Increment(ref droppedEvents);
                Volatile.Write(ref lastError, "Hàng đợi giám sát quá tải; đang yêu cầu quét bù.");
                var watcher = FindWatcher(path);
                if (watcher != null) QueueRecovery(watcher.Path, watcher);
            }
        }

        private static FileSystemWatcher FindWatcher(string path)
        {
            lock (Sync)
                foreach (var w in Watchers)
                    if (path.StartsWith(w.Path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) return w;
            return null;
        }

        private static void Worker(BlockingCollection<string> work, CancellationToken cancellation)
        {
            try
            {
                foreach (string path in work.GetConsumingEnumerable(cancellation))
                {
                    try
                    {
                        // Ghi ổn định trong tối đa 3 lần; tránh hash tệp vẫn đang ghi.
                        string version = null;
                        bool settled = false;
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            cancellation.ThrowIfCancellationRequested();
                            Thread.Sleep(350 * (attempt + 1));
                            if (!File.Exists(path)) break;
                            FileInfo info = new FileInfo(path);
                            string current = info.Length + "|" + info.LastWriteTimeUtc.Ticks;
                            if (version != null && version == current) { version = current; settled = true; break; }
                            version = current;
                        }
                        if (cancellation.IsCancellationRequested || !File.Exists(path)) continue;
                        if (!settled)
                        {
                            LocalScanIssueStore.Record(path, "Realtime", "Tệp thay đổi liên tục; chưa xác minh được phiên bản ổn định.");
                            continue;
                        }
                        string previous;
                        if (Processed.TryGetValue(path, out previous) && previous == version) continue;
                        ThreatFound hit = ScanEngine.EvaluateFile(path);
                        if (cancellation.IsCancellationRequested) break;
                        Interlocked.Increment(ref scannedEvents);
                        // Chỉ cache sau khi EvaluateFile không báo lỗi; không bỏ qua việc phát hiện mới.
                        if (Processed.Count > 20000) Processed.Clear();
                        Processed[path] = version;
                        if (hit == null) continue;
                        ScanHistoryStore.Add("Bảo vệ thời gian thực", path, 1, 1, 0);
                        SafeNotify(hit);
                        if (AutoQuarantine && !ScanEngine.IsHeuristicOnly(hit) && File.Exists(path))
                        {
                            if (ScanEngine.Quarantine(path, hit.Kind + ": " + hit.Reason))
                                ProtectionNotificationService.Publish("Đã tự động cách ly",
                                    "Tệp " + System.IO.Path.GetFileName(path) + " đã chuyển vào khu cách ly.",
                                    ProtectionNoticeLevel.Success, "auto-quarantine:" + path, 30);
                            else
                                ProtectionNotificationService.Publish("Cách ly chưa thành công",
                                    "Phát hiện nguy hiểm ở " + System.IO.Path.GetFileName(path) +
                                    "; vui lòng kiểm tra trang Mối đe dọa.",
                                    ProtectionNoticeLevel.Warning, "auto-quarantine-failed:" + path, 30);
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        Volatile.Write(ref lastError, "Có tệp chưa được kiểm tra: " + ex.GetType().Name);
                        LocalScanIssueStore.Record(path, "Realtime", ex.GetType().Name);
                    }
                    finally { byte ignored; Pending.TryRemove(path, out ignored); }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            var watcher = sender as FileSystemWatcher;
            if (watcher == null) return;
            var ex = e == null ? null : e.GetException();
            Volatile.Write(ref lastError, "Gián đoạn FileSystemWatcher: " +
                (ex == null ? "không rõ" : ex.GetType().Name));
            ProtectionNotificationService.Publish("Bảo vệ thời gian thực gián đoạn",
                "Đang thử khôi phục giám sát: " + watcher.Path,
                ProtectionNoticeLevel.Warning, "rt-watcher-error:" + watcher.Path, 90);
            QueueRecovery(watcher.Path, watcher);
        }

        private static void QueueRecovery(string root, FileSystemWatcher original)
        {
            if (!Recovering.TryAdd(root, 0)) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { Recover(root, original); }
                finally { byte ignored; Recovering.TryRemove(root, out ignored); }
            });
        }

        private static void Recover(string root, FileSystemWatcher original)
        {
            if (original != null)
            {
                lock (Sync)
                    if (Watchers.Contains(original))
                        try { original.EnableRaisingEvents = false; } catch (Exception) { }
                bool replaced = false;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    Thread.Sleep(1000 * (1 << attempt));
                    if (!Directory.Exists(root)) continue;
                    try
                    {
                        var replacement = CreateWatcher(root);
                        lock (Sync)
                        {
                            int index = Watchers.IndexOf(original);
                            if (index < 0) { replacement.Dispose(); return; }
                            replacement.EnableRaisingEvents = true;
                            Watchers[index] = replacement;
                            try { original.Dispose(); } catch (Exception) { }
                        }
                        replaced = true;
                        break;
                    }
                    catch (Exception ex) { Volatile.Write(ref lastError, "Lỗi khởi động lại watcher: " + ex.GetType().Name); }
                }
                if (!replaced)
                {
                    lock (Sync) { Watchers.Remove(original); try { original.Dispose(); } catch (Exception) { } }
                    RaiseStatus();
                    return;
                }
            }
            // Sau khoảng hở sự kiện, quét bù toàn bộ root để tránh mất file.
            // Quá tải lặp liên tiếp chỉ chạy một lượt quét bù cho mỗi thư mục.
            try
            {
                if (!IsRunning || !Directory.Exists(root)) return;
                ScanResult result = ScanEngine.Scan(ScanType.Custom, root, CancellationToken.None);
                foreach (var hit in result.Threats)
                {
                    SafeNotify(hit);
                    if (AutoQuarantine && !ScanEngine.IsHeuristicOnly(hit) && File.Exists(hit.FilePath))
                        ScanEngine.Quarantine(hit.FilePath, hit.Kind + ": " + hit.Reason);
                }
                foreach (var issue in result.Issues)
                    LocalScanIssueStore.Record(issue.Path, "Recovery", issue.Reason);
                if (result.Issues.Count == 0)
                {
                    Interlocked.Exchange(ref droppedEvents, 0);
                    Volatile.Write(ref lastError, null);
                }
            }
            catch (Exception ex)
            {
                Volatile.Write(ref lastError, "Quét bù chưa hoàn thành: " + ex.GetType().Name);
                LocalScanIssueStore.Record(root, "Recovery", ex.GetType().Name);
            }
        }

        private static void SafeNotify(ThreatFound hit)
        {
            Action<ThreatFound> handlers = ThreatDetected;
            if (handlers == null) return;
            foreach (Action<ThreatFound> handler in handlers.GetInvocationList())
                try { handler(hit); } catch (Exception) { }
        }

        private static void RaiseStatus()
        {
            var handlers = StatusChanged;
            if (handlers == null) return;
            foreach (Action<bool> handler in handlers.GetInvocationList())
                try { handler(IsRunning); } catch (Exception) { }
        }
    }
}
