using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Các "guard" thật cho từng hàng tính năng ở tab Bảo vệ:
    ///  - USB: timer phát hiện ổ removable mới cắm -> tự quét cả ổ.
    ///  - Tải xuống: watcher Downloads, tệp mang MOTW (Zone.Identifier từ Internet) -> quét nội dung đầy đủ.
    ///  - Hành vi: WMI giám sát tiến trình mới + luật (chạy từ Temp, PowerShell mã hóa, Office sinh shell).
    ///  - StartUp: watcher thư mục khởi động — tệp mới xuất hiện là dấu hiệu persistence.
    ///  - Khôi phục cách ly: hàm đánh giá lại tệp trước khi trả về máy.
    ///  - Tự cập nhật: tem CSDL quá 24h -> đóng dấu mới + xóa cache khi mở app.
    /// Mọi phát hiện đi qua Raise(): ghi lịch sử + event ThreatDetected (UI pop-up nếu bật cảnh báo)
    /// + tự cách ly nếu "Tự động cách ly" đang bật.
    /// </summary>
    public static class GuardService
    {
        public static event Action<ThreatFound> ThreatDetected;

        private static readonly object Sync = new object();
        private static FileSystemWatcher downloadWatcher;
        private static readonly List<FileSystemWatcher> startupWatchers = new List<FileSystemWatcher>();
        private static Timer usbTimer;
        private static List<string> knownRemovable = new List<string>();
        private static ManagementEventWatcher procWatcher;

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
            Configure("usb", FeatureFlags.UsbProtection);
            Configure("download", FeatureFlags.DownloadProtection);
            Configure("startup", FeatureFlags.StartupFoldersWatch);
            Configure("behavior", FeatureFlags.BehaviorWatch);
        }

        public static void Configure(string key, bool on)
        {
            switch (key)
            {
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
                    case "usb": return usbTimer != null;
                    case "download": return downloadWatcher != null;
                    case "startup": return startupWatchers.Count > 0;
                    case "behavior": return procWatcher != null;
                    default: return false;
                }
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
            try
            {
                var r = ScanEngine.Scan(ScanType.Custom, root, CancellationToken.None);
                ScanHistoryStore.Add(source, root, r.FilesScanned, r.Threats.Count, r.Duration.TotalSeconds);
                foreach (ThreatFound t in r.Threats) Raise(t, source);
            }
            catch (Exception) { }
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
                    if (!Directory.Exists(dl)) return;
                    downloadWatcher = NewWatcher(dl, true,
                        delegate (string path) { QueueDownloadCheck(path); });
                }
                else if (!on && downloadWatcher != null)
                {
                    downloadWatcher.Dispose();
                    downloadWatcher = null;
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
                Thread.Sleep(900); // chờ bên tải ghi xong
                if (!File.Exists(path)) return;
                if (!HasZoneIdentifier(path)) return; // không phải tệp tải từ Internet — lớp RT nền đã phủ
                ThreatFound t = ScanEngine.EvaluateFile(path, true);
                if (t != null) Raise(t, "Bảo vệ tải xuống");
            }
            catch (Exception) { }
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
                    foreach (string root in roots)
                        if (Directory.Exists(root))
                            startupWatchers.Add(NewWatcher(root, false,
                                delegate (string path) { QueueStartupCheck(path); }));
                    if (startupWatchers.Count == 0) return;
                }
                if (!on)
                {
                    foreach (FileSystemWatcher w in startupWatchers) w.Dispose();
                    startupWatchers.Clear();
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
                if (on && procWatcher == null)
                {
                    try
                    {
                        var query = new EventQuery(
                            "SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_Process'");
                        procWatcher = new ManagementEventWatcher(
                            new ManagementScope("root\\cimv2"), query);
                        procWatcher.EventArrived += OnProcessEvent;
                        procWatcher.Start();
                    }
                    catch (Exception) { procWatcher = null; } // WMI bị khóa trên máy -> coi như guard off
                }
                else if (!on && procWatcher != null)
                {
                    try { procWatcher.EventArrived -= OnProcessEvent; procWatcher.Stop(); procWatcher.Dispose(); }
                    catch (Exception) { }
                    procWatcher = null;
                }
            }
        }

        private static void OnProcessEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var tp = e.NewEvent.GetPropertyValue("TargetInstance") as ManagementBaseObject;
                if (tp == null) return;
                string exe = tp["ExecutablePath"] as string;
                string cmd = tp["CommandLine"] as string;
                string parent = null;
                try { parent = GetProcessName(Convert.ToUInt32(tp["ParentProcessId"])); } catch { }
                string rule = BehaviorRuleHit(exe, cmd, parent);
                if (rule == null) return;
                string shown = string.IsNullOrEmpty(cmd) ? exe : cmd;
                Raise(new ThreatFound
                {
                    Kind = "Heuristic (hành vi)",
                    FilePath = string.IsNullOrEmpty(exe) ? "(tiến trình hệ thống)" : exe,
                    Reason = rule + " — " + shown
                }, "Phát hiện hành vi đáng ngờ");
            }
            catch (Exception) { } // watcher nền không bao giờ làm chết app
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
                string stored = Path.Combine(ScanEngine.QuarantineDir, id);
                if (!File.Exists(stored)) continue;
                ThreatFound t = ScanEngine.EvaluateFile(stored, true);
                if (t != null) hits.Add(Path.GetFileName(t.FilePath) + ": " + t.Kind + " — " + t.Reason);
            }
            return hits.Count == 0 ? null : string.Join("\n", hits.ToArray());
        }

        // ==================== Tự cập nhật theo hạn ====================

        /// <summary>Cờ "Tự động cập nhật" bật mà tem quá 24h (hoặc thiếu) -> đóng tem mới + xóa cache. Trả về true nếu vừa chạy.</summary>
        public static bool EnsureDailyAutoUpdate()
        {
            if (!FeatureFlags.AutoUpdateEnabled) return false;
            DateTime last;
            bool has = ScanHistoryStore.TryGetLastSignatureUpdate(out last);
            if (has && (DateTime.Now - last).TotalHours < 24) return false;
            ScanHistoryStore.MarkSignatureUpdated(DateTime.Now);
            ScanEngine.ClearScanCache();
            ScanHistoryStore.Add("Cập nhật CSDL", "Tự động (theo hạn 24h)", 0, 0, 0);
            return true;
        }

        // ==================== dùng chung ====================

        private static FileSystemWatcher NewWatcher(string root, bool recursive, Action<string> onArrive)
        {
            var w = new FileSystemWatcher(root);
            w.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
                | NotifyFilters.Size | NotifyFilters.CreationTime;
            w.IncludeSubdirectories = recursive;
            w.InternalBufferSize = 64 * 1024;
            w.Created += delegate(object s, FileSystemEventArgs e) { onArrive(e.FullPath); };
            w.Renamed += delegate(object s, RenamedEventArgs e) { onArrive(e.FullPath); };
            w.EnableRaisingEvents = true;
            return w;
        }

        private static void Raise(ThreatFound t, string source)
        {
            ScanHistoryStore.Add(source, t.FilePath ?? "", 0, 1, 0);
            if (RealTimeProtection.AutoQuarantine && !string.IsNullOrEmpty(t.FilePath) && File.Exists(t.FilePath))
            {
                ScanEngine.Quarantine(t.FilePath, t.Kind + ": " + t.Reason);
                t.Reason += " (đã tự cách ly)";
            }
            Action<ThreatFound> h = ThreatDetected;
            if (h != null) h(t);
        }
    }
}
