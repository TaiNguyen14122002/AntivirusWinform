using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Các "guard" thật cho từng hàng tính năng ở tab Bảo vệ:
    ///  - USB: timer phát hiện ổ removable mới cắm -> tự quét cả ổ.
    ///  - Tải xuống: watcher Downloads, tệp mang MOTW (Zone.Identifier từ Internet) -> quét nội dung đầy đủ.
    ///  - Hành vi: WMI giám sát tiến trình + FileSystemWatcher gom chuỗi thay đổi tài liệu.
    ///  - StartUp: watcher thư mục khởi động — tệp mới xuất hiện là dấu hiệu persistence.
    ///  - Khôi phục cách ly: hàm đánh giá lại tệp trước khi trả về máy.
    ///  - Tự cập nhật: khi quá 24h thì lấy metadata MalwareBazaar, thêm chữ ký thiếu rồi reload cache.
    /// Mọi phát hiện đi qua Raise(): ghi lịch sử + event ThreatDetected (UI pop-up nếu bật cảnh báo)
    /// + tự cách ly chữ ký xác nhận nếu "Tự động cách ly" đang bật.
    /// Cảnh báo heuristic/hành vi không tự cách ly vì có nguy cơ dương tính giả.
    /// </summary>
    public static class GuardService
    {
        public static event Action<ThreatFound> ThreatDetected;

        private static readonly object Sync = new object();
        private static FileSystemWatcher downloadWatcher;
        private static readonly List<FileSystemWatcher> fileWatchers = new List<FileSystemWatcher>();
        private static readonly SemaphoreSlim fileScanSlots = new SemaphoreSlim(2, 2);
        private static int pendingFileScans;
        private static int fileQueueOverflowReported;
        private static readonly ConcurrentDictionary<string, DateTime> recentFileChecks =
            new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> guardErrors =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<FileSystemWatcher> startupWatchers = new List<FileSystemWatcher>();
        private static Timer usbTimer;
        private static Timer signatureUpdateTimer;
        private static List<string> knownRemovable = new List<string>();
        private static ManagementEventWatcher procWatcher;
        private static readonly BehavioralActivityAnalyzer activityAnalyzer = new BehavioralActivityAnalyzer();
        private static readonly List<FileSystemWatcher> behaviorFileWatchers = new List<FileSystemWatcher>();

        private static readonly string[] OfficeParents =
            { "winword.exe", "excel.exe", "powerpnt.exe", "outlook.exe", "onenote.exe", "wordpad.exe" };
        private static readonly string[] ShellChildren =
            { "cmd.exe", "powershell.exe", "pwsh.exe", "wscript.exe", "cscript.exe",
              "mshta.exe", "rundll32.exe", "regsvr32.exe", "bash.exe" };
        private static readonly string[] SuspiciousCmdMarkers =
            { "-enc ", "-encodedcommand", "frombase64string", "iex(", "invoke-expression", "downloadstring", "msscriptcontrol" };

        // ==================== vòng đời ====================

        /// <summary>Gọi lúc app khởi động: bật các guard theo cờ đã lưu.</summary>
        public static void ApplyAll()
        {
            Configure("file", FeatureFlags.FileRestoreGuard);
            Configure("usb", FeatureFlags.UsbProtection);
            Configure("download", FeatureFlags.DownloadProtection);
            Configure("startup", FeatureFlags.StartupFoldersWatch);
            Configure("behavior", FeatureFlags.BehaviorWatch);
            lock (Sync)
            {
                if (signatureUpdateTimer == null)
                    signatureUpdateTimer = new Timer(delegate(object ignored)
                    {
                        try { EnsureDailyAutoUpdate(); }
                        catch (Exception ex) { System.Diagnostics.Trace.WriteLine("Lỗi bộ hẹn giờ cập nhật chữ ký: " + ex.Message); }
                    }, null, TimeSpan.FromSeconds(30), TimeSpan.FromHours(1));
            }
        }

        public static void Configure(string key, bool on)
        {
            switch (key)
            {
                case "file": SetFileProtection(on); break;
                case "usb": SetUsb(on); break;
                case "download": SetDownload(on); break;
                case "startup": SetStartup(on); break;
                case "behavior": SetBehavior(on); break;
            }
        }

        public static bool IsGuardRunning(string key)
        {
            lock (Sync)
            {
                switch (key)
                {
                    case "file": return fileWatchers.Count > 0;
                    case "usb": return usbTimer != null;
                    case "download": return downloadWatcher != null;
                    case "startup": return startupWatchers.Count > 0;
                    case "behavior": return procWatcher != null || behaviorFileWatchers.Count > 0;
                    default: return false;
                }
            }
        }

        /// <summary>Chẩn đoán mức độ hoạt động thực (không dựa vào cờ đã lưu).</summary>
        public static string GetGuardStatus(string key)
        {
            lock (Sync)
            {
                bool running = IsGuardRunning(key);
                string error;
                guardErrors.TryGetValue(key, out error);
                if (!running) return string.IsNullOrWhiteSpace(error) ? "Chưa chạy" : "Không hoạt động: " + error;
                if (key == "behavior" && procWatcher == null)
                    return "Hoạt động một phần: WMI tiến trình không khả dụng; vẫn theo dõi tệp";
                return string.IsNullOrWhiteSpace(error) ? "Đang giám sát" : "Đang giám sát (có cảnh báo: " + error + ")";
            }
        }

        /// <summary>Trả về các phạm vi thực sự được FileSystemWatcher giám sát.</summary>
        public static string[] GetWatchRoots(string key)
        {
            lock (Sync)
            {
                if (key == "file") return fileWatchers.Select(w => w.Path).ToArray();
                if (key == "download") return downloadWatcher == null ? new string[0] : new[] { downloadWatcher.Path };
                if (key == "startup") return startupWatchers.Select(w => w.Path).ToArray();
                if (key == "behavior") return behaviorFileWatchers.Select(w => w.Path).ToArray();
                if (key == "usb") return CurrentRemovable().ToArray();
                return new string[0];
            }
        }

        private static void RecordGuardError(string key, string message)
        {
            lock (Sync) guardErrors[key] = message;
            System.Diagnostics.Trace.WriteLine("Protection/" + key + ": " + message);
            ProtectionNotificationService.Publish("Bộ bảo vệ cần kiểm tra", message,
                ProtectionNoticeLevel.Warning, "guard-error:" + key, 90);
        }

        private static void SetFileProtection(bool on)
        {
            lock (Sync)
            {
                if (!on)
                {
                    foreach (var watcher in fileWatchers) { try { watcher.Dispose(); } catch (Exception) { } }
                    fileWatchers.Clear();
                    recentFileChecks.Clear();
                    Interlocked.Exchange(ref fileQueueOverflowReported, 0);
                    guardErrors.Remove("file");
                    return;
                }
                if (fileWatchers.Count > 0) return;
                guardErrors.Remove("file");
                // Best-effort: FileSystemWatcher theo dõi tệp được tạo/sửa, KHÔNG chặn mở tệp ở mức kernel.
                string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var roots = new[] {
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Path.Combine(user, "Downloads")
                }.Where(path => !string.IsNullOrWhiteSpace(path))
                 .Distinct(StringComparer.OrdinalIgnoreCase);
                foreach (string root in roots)
                {
                    if (!Directory.Exists(root)) continue;
                    try
                    {
                        FileSystemWatcher watcher = NewWatcher(root, true,
                            path => QueueFileCheck(path), true);
                        watcher.Error += (sender, e) => RecordGuardError("file",
                            "Watcher " + root + " bị gián đoạn: " + (e.GetException() == null ? "không rõ" : e.GetException().Message));
                        fileWatchers.Add(watcher);
                    }
                    catch (Exception ex) { RecordGuardError("file", root + ": " + ex.Message); }
                }
                if (fileWatchers.Count == 0)
                    RecordGuardError("file", "Không có thư mục người dùng truy cập được.");
            }
        }

        private static void QueueFileCheck(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            if (RealTimeProtection.IsRunning && !RealTimeProtection.IsDegraded &&
                RealTimeProtection.MonitorsPath(path)) return; // Realtime đã quét cùng phạm vi.
            DateTime now = DateTime.UtcNow;
            DateTime previous;
            if (recentFileChecks.TryGetValue(path, out previous) && (now - previous).TotalSeconds < 5) return;
            recentFileChecks[path] = now;
            if (recentFileChecks.Count > 4096)
                foreach (var item in recentFileChecks.Where(x => (now - x.Value).TotalMinutes > 2).Take(256))
                { DateTime removed; recentFileChecks.TryRemove(item.Key, out removed); }
            if (Interlocked.Increment(ref pendingFileScans) > 256)
            {
                Interlocked.Decrement(ref pendingFileScans);
                if (Interlocked.CompareExchange(ref fileQueueOverflowReported, 1, 0) == 0)
                    RecordGuardError("file", "Hàng đợi quá tải; một số thay đổi tệp chưa được quét.");
                LocalScanIssueStore.Record(path, "Bảo vệ tệp", "Hàng đợi kiểm tra quá tải.");
                return;
            }
            // WaitAsync không chiếm một ThreadPool worker trong lúc chờ suất quét.
            // Khi nhiều tệp thay đổi đồng thời, Wait() kiểu cũ giữ tối đa 256 thread
            // bị chặn và làm giao diện WinForms khựng, nhất là VM ít RAM.
            System.Threading.Tasks.Task.Run(async () =>
            {
                bool acquired = false;
                try
                {
                    await fileScanSlots.WaitAsync().ConfigureAwait(false);
                    acquired = true;
                    CheckUserFile(path);
                }
                finally
                {
                    if (acquired) fileScanSlots.Release();
                    if (Interlocked.Decrement(ref pendingFileScans) < 64)
                        Interlocked.Exchange(ref fileQueueOverflowReported, 0);
                }
            });
        }

        private static void CheckUserFile(string path)
        {
            try
            {
                // Giới hạn chờ khi tệp còn đang được ghi: không khẳng định đã quét nếu tệp chưa ổn định.
                long size = -1;
                DateTime changed = DateTime.MinValue;
                bool stable = false;
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(400);
                    if (!File.Exists(path) || !FeatureFlags.FileRestoreGuard || !IsGuardRunning("file")) return;
                    var info = new FileInfo(path);
                    if (info.Length == size && info.LastWriteTimeUtc == changed) { stable = true; break; }
                    size = info.Length; changed = info.LastWriteTimeUtc;
                }
                if (!stable)
                {
                    LocalScanIssueStore.Record(path, "Bảo vệ tệp", "Tệp liên tục thay đổi; chưa thể kiểm tra bản ổn định.");
                    return;
                }
                ThreatFound found = ScanEngine.EvaluateFile(path);
                if (found != null && IsGuardRunning("file")) Raise(found, "Bảo vệ tệp");
            }
            catch (Exception ex)
            {
                LocalScanIssueStore.Record(path, "Bảo vệ tệp", ex.GetType().Name + ": " + ex.Message);
                RecordGuardError("file", "Một số tệp chưa quét được (" + ex.GetType().Name + ")");
            }
        }

        /// <summary>Dừng timer và watcher khi ứng dụng đóng.</summary>
        public static void StopAll()
        {
            Configure("file", false);
            Configure("download", false);
            Configure("startup", false);
            Configure("behavior", false);
            Configure("usb", false);
            lock (Sync)
            {
                if (signatureUpdateTimer != null) { signatureUpdateTimer.Dispose(); signatureUpdateTimer = null; }
            }
        }

        // ==================== USB ====================

        private static void SetUsb(bool on)
        {
            lock (Sync)
            {
                if (on && usbTimer == null)
                {
                    knownRemovable = CurrentRemovable();
                    usbTimer = new Timer(delegate { UsbTick(); }, null, 5000, 5000);
                }
                else if (!on && usbTimer != null)
                {
                    usbTimer.Dispose();
                    usbTimer = null;
                    guardErrors.Remove("usb");
                }
            }
        }

        private static List<string> CurrentRemovable()
        {
            var list = new List<string>();
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                    if (d.DriveType == DriveType.Removable && d.IsReady) list.Add(d.Name);
            }
            catch (Exception) { }
            return list;
        }

        private static void UsbTick()
        {
            try
            {
                var now = CurrentRemovable();
                List<string> fresh;
                lock (Sync)
                {
                    fresh = DetectNewRemovable(knownRemovable, now);
                    knownRemovable = now;
                }
                foreach (string drive in fresh) QueueUsbScan(drive);
            }
            catch (Exception) { }
        }

        // Hàm thuần (test offline được): ổ xuất hiện trong current nhưng chưa từng biết
        public static List<string> DetectNewRemovable(ICollection<string> known, IEnumerable<string> current)
        {
            var fresh = new List<string>();
            foreach (string d in current)
                if (known == null || !known.Contains(d, StringComparer.OrdinalIgnoreCase)) fresh.Add(d);
            return fresh;
        }

        private static void QueueUsbScan(string driveRoot)
        {
            ThreadPool.QueueUserWorkItem(delegate { ScanDriveInto(driveRoot, "Bảo vệ USB"); });
        }

        // Công khai cho test + nút demo: giả lập "vừa cắm USB" trỏ vào một thư mục
        public static void SimulateUsbArrival(string folderRoot)
        {
            ThreadPool.QueueUserWorkItem(delegate { ScanDriveInto(folderRoot, "Bảo vệ USB"); });
        }

        private static void ScanDriveInto(string root, string source)
        {
            ProtectionNotificationService.Publish("Bảo vệ USB", "Đang kiểm tra thiết bị " + root,
                ProtectionNoticeLevel.Info, "usb-scan-start:" + root, 15);
            try
            {
                var r = ScanEngine.Scan(ScanType.Custom, root, CancellationToken.None);
                ScanHistoryStore.Add(source, root, r.CheckedObjects, r.Threats.Count, r.Duration.TotalSeconds);
                foreach (var issue in r.Issues)
                    LocalScanIssueStore.Record(issue.Path, source, issue.Reason);
                // Đã ghi một phiên tổng hợp ở trên: không đếm thêm mỗi phát hiện như phiên mới.
                foreach (ThreatFound t in r.Threats) Raise(t, source, false);
                ProtectionNotificationService.Publish("Đã kiểm tra USB",
                    "Đã quét " + r.CheckedObjects.ToString("N0") + " mục, phát hiện " + r.Threats.Count +
                    " mối đe dọa" + (r.Issues.Count > 0 ? "; " + r.Issues.Count + " mục chưa kiểm tra được." : "."),
                    r.Threats.Count > 0 || r.Issues.Count > 0 ? ProtectionNoticeLevel.Warning : ProtectionNoticeLevel.Success,
                    "usb-scan-done:" + root, 15);
            }
            catch (Exception ex)
            {
                LocalScanIssueStore.Record(root, source, "Quét ổ thất bại: " + ex.Message);
                RecordGuardError("usb", "Không quét được một số ổ: " + ex.GetType().Name);
            }
        }

        // ==================== Tải xuống (MOTW) ====================

        private static void SetDownload(bool on)
        {
            lock (Sync)
            {
                if (on && downloadWatcher == null)
                {
                    string dl = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    if (!Directory.Exists(dl)) { RecordGuardError("download", "Không tìm thấy thư mục Downloads."); return; }
                    try
                    {
                        downloadWatcher = NewWatcher(dl, true,
                            delegate (string path) { QueueDownloadCheck(path); });
                        guardErrors.Remove("download");
                        downloadWatcher.Error += (sender, e) => RecordGuardError("download",
                            "Theo dõi Downloads bị gián đoạn: " + (e.GetException() == null ? "không rõ" : e.GetException().Message));
                    }
                    catch (Exception ex) { RecordGuardError("download", ex.Message); }
                }
                else if (!on && downloadWatcher != null)
                {
                    downloadWatcher.Dispose();
                    downloadWatcher = null;
                    guardErrors.Remove("download");
                }
            }
        }

        private static void QueueDownloadCheck(string path)
        {
            ThreadPool.QueueUserWorkItem(delegate { CheckDownloadFile(path); });
        }

        // Công khai cho test: mô phỏng trình duyệt vừa lưu xong file vào Downloads
        public static void SimulateDownloadArrival(string path)
        {
            CheckDownloadFile(path);
        }

        private static void CheckDownloadFile(string path)
        {
            try
            {
                // Một số trình duyệt tạo tệp rỗng rồi ghi nhiều đợt. Chỉ quét khi ổn định.
                long lastSize = -1;
                DateTime lastWrite = DateTime.MinValue;
                bool settled = false;
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    Thread.Sleep(500);
                    if (!File.Exists(path) || !FeatureFlags.DownloadProtection || !IsGuardRunning("download")) return;
                    FileInfo info = new FileInfo(path);
                    if (info.Length == lastSize && info.LastWriteTimeUtc == lastWrite) { settled = true; break; }
                    lastSize = info.Length; lastWrite = info.LastWriteTimeUtc;
                }
                if (!settled)
                {
                    LocalScanIssueStore.Record(path, "Bảo vệ tải xuống", "Tệp tải xuống chưa ổn định để kiểm tra.");
                    return;
                }
                if (!HasZoneIdentifier(path)) return; // Có thể không có MOTW trên một số ổ/trình duyệt.
                ThreatFound t = ScanEngine.EvaluateFile(path, true);
                if (t != null && IsGuardRunning("download")) Raise(t, "Bảo vệ tải xuống");
            }
            catch (Exception ex)
            {
                LocalScanIssueStore.Record(path, "Bảo vệ tải xuống", ex.Message);
                RecordGuardError("download", "Không quét được một số tệp: " + ex.GetType().Name);
            }
        }

        // Đọc ADS "Zone.Identifier" mà trình duyệt đính kèm tệp tải về (ZoneId=3 = Internet).
        // Phải dùng CreateFileW trực tiếp: FileStream bị FileIOPermission chặn ký tự ':' của ADS.
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFileW(
            string name, uint access, uint share, IntPtr sa, uint disp, uint flags, IntPtr tmpl);

        public static bool HasZoneIdentifier(string path)
        {
            try
            {
                const uint GENERIC_READ = 0x80000000, OPEN_EXISTING = 3,
                         FILE_SHARE_ALL = 7, FILE_ATTRIBUTE_NORMAL = 0x80;
                using (var h = CreateFileW(path + ":Zone.Identifier", GENERIC_READ, FILE_SHARE_ALL,
                    IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero))
                using (var fs = new FileStream(h, FileAccess.Read))
                {
                    byte[] buffer = new byte[256];
                    int n = fs.Read(buffer, 0, buffer.Length);
                    return Encoding.ASCII.GetString(buffer, 0, n)
                        .IndexOf("ZoneId=3", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch (Exception) { return false; }
        }

        // ==================== StartUp folders ====================

        private static void SetStartup(bool on)
        {
            lock (Sync)
            {
                if (on && startupWatchers.Count == 0)
                {
                    string[] roots =
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
                    };
                    foreach (string root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;
                        try
                        {
                            FileSystemWatcher watcher = NewWatcher(root, false,
                                delegate (string path) { QueueStartupCheck(path); });
                            watcher.Error += (sender, e) => RecordGuardError("startup",
                                "Watcher " + root + " bị gián đoạn: " + (e.GetException() == null ? "không rõ" : e.GetException().Message));
                            startupWatchers.Add(watcher);
                        }
                        catch (Exception ex) { RecordGuardError("startup", root + ": " + ex.Message); }
                    }
                    if (startupWatchers.Count == 0) RecordGuardError("startup", "Không truy cập được thư mục Startup.");
                }
                if (!on)
                {
                    foreach (FileSystemWatcher w in startupWatchers) w.Dispose();
                    startupWatchers.Clear();
                    guardErrors.Remove("startup");
                }
            }
        }

        public static void SimulateStartupArrival(string path)
        {
            ThreadPool.QueueUserWorkItem(delegate { CheckStartupFile(path); });
        }

        private static void QueueStartupCheck(string path)
        {
            ThreadPool.QueueUserWorkItem(delegate { CheckStartupFile(path); });
        }

        private static void CheckStartupFile(string path)
        {
            try
            {
                Thread.Sleep(600);
                if (!File.Exists(path)) return;
                ThreatFound t = ScanEngine.EvaluateFile(path, true);
                if (t != null) Raise(t, "Bảo vệ thư mục khởi động");
            }
            catch (Exception) { }
        }

        // ==================== Hành vi tiến trình (WMI) ====================

        private static void SetBehavior(bool on)
        {
            lock (Sync)
            {
                if (on)
                {
                    // WMI và FileSystemWatcher độc lập: WMI lỗi vẫn giám sát tệp được.
                    if (procWatcher == null)
                    {
                        ManagementEventWatcher watcher = null;
                        try
                        {
                            var query = new EventQuery(
                                "SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_Process'");
                            watcher = new ManagementEventWatcher(new ManagementScope("root\\cimv2"), query);
                            watcher.EventArrived += OnProcessEvent;
                            watcher.Start();
                            procWatcher = watcher;
                        }
                        catch (Exception ex)
                        {
                            RecordGuardError("behavior", "WMI: " + ex.Message);
                            System.Diagnostics.Debug.WriteLine("Behavior WMI không hoạt động: " + ex.Message);
                            if (watcher != null) { try { watcher.Dispose(); } catch (Exception) { } }
                            procWatcher = null;
                        }
                    }
                    if (behaviorFileWatchers.Count == 0)
                    {
                        var roots = new[]
                        {
                            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                        };
                        foreach (string root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
                        {
                            try
                            {
                                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;
                                var w = new FileSystemWatcher(root);
                                w.IncludeSubdirectories = true;
                                w.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
                                w.InternalBufferSize = 32 * 1024;
                                w.Changed += OnBehaviorFileEvent;
                                w.Renamed += OnBehaviorRenamedEvent;
                                w.Deleted += OnBehaviorFileEvent;
                                w.Error += OnBehaviorWatchError;
                                w.EnableRaisingEvents = true;
                                behaviorFileWatchers.Add(w);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Behavior Watcher không hoạt động " + root + ": " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    if (procWatcher != null)
                    {
                        try { procWatcher.EventArrived -= OnProcessEvent; procWatcher.Stop(); procWatcher.Dispose(); }
                        catch (Exception) { }
                        procWatcher = null;
                    }
                    foreach (var w in behaviorFileWatchers)
                    {
                        try
                        {
                            w.EnableRaisingEvents = false;
                            w.Changed -= OnBehaviorFileEvent;
                            w.Renamed -= OnBehaviorRenamedEvent;
                            w.Deleted -= OnBehaviorFileEvent;
                            w.Error -= OnBehaviorWatchError;
                            w.Dispose();
                        }
                        catch (Exception) { }
                    }
                    behaviorFileWatchers.Clear();
                    activityAnalyzer.Reset();
                    guardErrors.Remove("behavior");
                }
            }
        }

        private static void OnBehaviorWatchError(object sender, ErrorEventArgs e)
        {
            RecordGuardError("behavior", "Watcher tệp bị gián đoạn: " +
                (e == null || e.GetException() == null ? "không rõ" : e.GetException().Message));
        }

        private static void OnBehaviorFileEvent(object sender, FileSystemEventArgs e)
        {
            ObserveBehaviorEvent(e.FullPath, null, e.ChangeType);
        }

        private static void OnBehaviorRenamedEvent(object sender, RenamedEventArgs e)
        {
            ObserveBehaviorEvent(e.FullPath, e.OldFullPath, WatcherChangeTypes.Renamed);
        }

        private static void ObserveBehaviorEvent(string path, string oldPath, WatcherChangeTypes change)
        {
            try
            {
                BehavioralFinding finding = activityAnalyzer.ObserveFileChange(path, oldPath, change, DateTime.UtcNow);
                if (finding == null) return;
                // Xử lý SQL/UI ngoài callback FileSystemWatcher để hạn chế mất sự kiện.
                EmitBehaviorFinding(finding, "Heuristic (hành vi tệp)", "Cảnh báo hành vi tệp");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Behavior event: " + ex.Message); }
        }

        private static void OnProcessEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var tp = e.NewEvent.GetPropertyValue("TargetInstance") as ManagementBaseObject;
                if (tp == null) return;
                string exe = tp["ExecutablePath"] as string;
                if (string.IsNullOrWhiteSpace(exe)) exe = tp["Name"] as string;
                string cmd = tp["CommandLine"] as string;
                string parent = null;
                try { parent = GetProcessName(Convert.ToUInt32(tp["ParentProcessId"])); }
                catch (Exception) { }
                BehavioralFinding finding = BehavioralActivityAnalyzer.AssessProcessStart(exe, cmd, parent);
                if (finding == null) return;
                EmitBehaviorFinding(finding, "Heuristic (hành vi tiến trình)", "Cảnh báo hành vi tiến trình");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Behavior process: " + ex.Message); }
        }

        private static void EmitBehaviorFinding(BehavioralFinding finding, string kind, string source)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    if (!IsGuardRunning("behavior")) return;
                    // Chỉ có bằng chứng hành vi; không tự động cách ly, không khẳng định có virus.
                    Raise(new ThreatFound
                    {
                        Kind = kind,
                        FilePath = finding.Scope,
                        Reason = "Nghi vấn " + finding.Score + "/100: " + finding.Evidence
                    }, source);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Behavior warning: " + ex.Message);
                }
            });
        }

        private static string GetProcessName(uint pid)
        {
            if (pid == 0) return null;
            using (var s = new ManagementObjectSearcher(
                "SELECT Name FROM Win32_Process WHERE ProcessId=" + pid))
                foreach (ManagementObject o in s.Get())
                {
                    object n = o["Name"];
                    return n as string;
                }
            return null;
        }

        // Luật hành vi — hàm thuần, test offline
        public static string BehaviorRuleHit(string exePath, string commandLine, string parentName)
        {
            string exe = (exePath ?? "").Trim();
            string cmd = (commandLine ?? "").ToLowerInvariant();
            string name = Path.GetFileName(exe).ToLowerInvariant();

            // (a) tiến trình chạy thẳng từ %TEMP% — dấu hiệu dropper
            string temp = Path.GetTempPath();
            if (exe.Length > 0 && name.Length > 0
                && exe.StartsWith(temp, StringComparison.OrdinalIgnoreCase))
                return "Tiến trình chạy từ thư mục Temp (dropper)";

            // (b) PowerShell mang cờ mã hóa/tải remote
            if (name == "powershell.exe" || name == "pwsh.exe" || name == "cmd.exe")
                foreach (string marker in SuspiciousCmdMarkers)
                    if (cmd.Contains(marker))
                        return "Dòng lệnh chứa chỉ dấu PowerShell độc (" + marker.Trim() + ")";

            // (c) ứng dụng văn phòng sinh tiến trình shell — chuỗi khai thác macro kinh điển
            string parent = (parentName ?? "").ToLowerInvariant();
            if (parent.Length > 0 && name.Length > 0
                && OfficeParents.Contains(parent) && ShellChildren.Contains(name))
                return parent + " khởi chạy shell " + name;

            return null;
        }

        // ==================== Khôi phục tệp cách ly (hàng "Bảo vệ tệp") ====================

        /// <summary>Trả về cảnh báo gộp nếu tệp trong khu cách ly VẪN khớp phát hiện; null nếu an toàn.</summary>
        public static string RestoreWarningFor(IEnumerable<string> storedIds)
        {
            var hits = new List<string>();
            foreach (string id in storedIds)
            {
                // Đường dẫn vật lý được lưu trong SQL, không phải id trần.
                long detectionId;
                if (!long.TryParse(id, out detectionId)) continue;
                string stored;
                try
                {
                    System.Data.DataRow record = new ThreatDetectionRepository().GetById(detectionId);
                    if (record == null || record["QuarantinePath"] == DBNull.Value) continue;
                    stored = Convert.ToString(record["QuarantinePath"]);
                }
                catch (Exception) { continue; }
                if (string.IsNullOrWhiteSpace(stored) || !File.Exists(stored)) continue;
                ThreatFound t = ScanEngine.EvaluateFile(stored, true);
                if (t != null) hits.Add(Path.GetFileName(t.FilePath) + ": " + t.Kind + " — " + t.Reason);
            }
            return hits.Count == 0 ? null : string.Join("\n", hits.ToArray());
        }

        // ==================== Tự cập nhật theo hạn ====================

        // Đồng bộ FULL CSV export tối đa một lần/24 giờ; không dùng get_recent=100.
        // Không mở cửa sổ mật khẩu cho lượt nền; khóa phải được cấu hình thủ công lần đầu.
        private static int autoUpdateScheduled;
        public static bool EnsureDailyAutoUpdate()
        {
            if (!FeatureFlags.AutoUpdateEnabled) return false;
            string key = MalwareBazaarSignatureUpdater.GetStoredAuthKey();
            if (string.IsNullOrWhiteSpace(key) ||
                string.IsNullOrWhiteSpace(MalwareBazaarSignatureUpdater.GetStoredFullExportUrl())) return false;
            DateTime last;
            bool has = ScanHistoryStore.TryGetLastSignatureUpdate(out last);
            if (has && (DateTime.Now - last).TotalHours < 24) return false;
            if (System.Threading.Interlocked.CompareExchange(ref autoUpdateScheduled, 1, 0) != 0) return false;
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var result = await MalwareBazaarSignatureUpdater.UpdateAsync(
                        key, System.Threading.CancellationToken.None).ConfigureAwait(false);
                    ScanHistoryStore.Add("Cập nhật CSDL", "MalwareBazaar full export: thêm " + result.Inserted +
                        " chữ ký; đã tồn tại " + result.Existing, 0, 0, 0);
                    ProtectionNotificationService.Publish("Cập nhật chữ ký hoàn tất",
                        "Đã bổ sung " + result.Inserted + " chữ ký mới; " + result.Existing + " đã tồn tại.",
                        ProtectionNoticeLevel.Success, "signature-update", 30);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Không cập nhật được MalwareBazaar: " + ex.Message);
                    ProtectionNotificationService.Publish("Không thể cập nhật chữ ký",
                        "Kiểm tra kết nối mạng và khóa API. " + ex.Message,
                        ProtectionNoticeLevel.Warning, "signature-update-error", 120);
                }
                finally { System.Threading.Interlocked.Exchange(ref autoUpdateScheduled, 0); }
            });
            return true;
        }

        // ==================== dùng chung ====================

        private static FileSystemWatcher NewWatcher(string root, bool recursive, Action<string> onArrive, bool includeChanges = false)
        {
            var w = new FileSystemWatcher(root);
            w.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
                | NotifyFilters.Size | NotifyFilters.CreationTime;
            w.IncludeSubdirectories = recursive;
            w.InternalBufferSize = 64 * 1024;
            w.Created += delegate(object s, FileSystemEventArgs e) { onArrive(e.FullPath); };
            w.Renamed += delegate(object s, RenamedEventArgs e) { onArrive(e.FullPath); };
            if (includeChanges) w.Changed += delegate(object s, FileSystemEventArgs e) { onArrive(e.FullPath); };
            w.EnableRaisingEvents = true;
            return w;
        }

        private static void Raise(ThreatFound t, string source, bool recordHistory = true)
        {
            if (recordHistory && ScanEngine.IsHeuristicOnly(t))
            {
                ScanHistoryStore.AddEntry(new HistoryEntry
                {
                    Time = DateTime.Now,
                    Type = source,
                    Scope = (t.FilePath ?? "") + " — " + (t.Reason ?? ""),
                    Files = 0,
                    Threats = 0,
                    ResultStatus = "Suspicious"
                });
            }
            else if (recordHistory)
                ScanHistoryStore.Add(source, t.FilePath ?? "", 0, 1, 0);
            if (RealTimeProtection.AutoQuarantine && !ScanEngine.IsHeuristicOnly(t) &&
                !string.IsNullOrEmpty(t.FilePath) && File.Exists(t.FilePath))
            {
                bool quarantined = ScanEngine.Quarantine(t.FilePath, t.Kind + ": " + t.Reason);
                if (quarantined)
                {
                    t.Reason += " (đã tự cách ly)";
                    ProtectionNotificationService.Publish("Đã cách ly tệp nguy hiểm",
                        "Bộ bảo vệ đã chuyển " + Path.GetFileName(t.FilePath) + " vào khu cách ly.",
                        ProtectionNoticeLevel.Success, "guard-quarantine:" + t.FilePath, 20);
                }
                else
                    ProtectionNotificationService.Publish("Chưa cách ly được tệp",
                        "Vui lòng kiểm tra tệp " + Path.GetFileName(t.FilePath) + " trong Mối đe dọa.",
                        ProtectionNoticeLevel.Warning, "guard-quarantine-error:" + t.FilePath, 20);
            }
            Action<ThreatFound> h = ThreatDetected;
            if (h != null) h(t);
        }
    }
}
