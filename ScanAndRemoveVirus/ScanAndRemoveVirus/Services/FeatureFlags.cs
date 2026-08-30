using System;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Công tắc cho 10 tính năng ở tab Bảo vệ — lưu/đọc qua settings.ini
    /// để nhớ giữa các lần mở app. GuardService lắng nghe Changed để bật/tắt cơ chế thật.
    /// </summary>
    public static class FeatureFlags
    {
        public static event Action Changed;

        public static bool RealTimeOn { get; set; }
        public static bool FileRestoreGuard { get; set; }        // "Bảo vệ tệp": chặn khôi phục tệp còn độc
        public static bool UsbProtection { get; set; }           // USB: quét ổ removable mới cắm
        public static bool DownloadProtection { get; set; }      // Tải xuống: quét tệp có MOTW trong Downloads
        public static bool BehaviorWatch { get; set; }           // Hành vi: WMI giám sát tiến trình
        public static bool StartupFoldersWatch { get; set; }     // StartUp folders: quét tệp mới rơi vào
        public static bool ThreatAlerts { get; set; }            // Pop-up cảnh báo (chung checkbox Cài đặt)
        public static bool AutoUpdateEnabled { get; set; }       // Tự cập nhật khi mở app (>24h)
        public static bool VtAutoQuery { get; set; }             // Web/Cloud: heuristic -> tự tra VT

        static FeatureFlags()
        {
            // Mặc định: guard chủ động BẬT; autostart/auto-update/VT-auto TẮT
            // (VT auto cần API key; auto-update nên người dùng chủ động chọn)
            FileRestoreGuard = true;
            UsbProtection = true;
            DownloadProtection = true;
            BehaviorWatch = true;
            StartupFoldersWatch = true;
            ThreatAlerts = true;
            LoadFromStore();
        }

        public static void LoadFromStore()
        {
            var s = AppSettings.Load();
            FileRestoreGuard = s.FileRestoreGuard;
            UsbProtection = s.UsbProtection;
            DownloadProtection = s.DownloadProtection;
            BehaviorWatch = s.BehaviorWatch;
            StartupFoldersWatch = s.StartupFoldersWatch;
            ThreatAlerts = s.ShowNotifications;
            AutoUpdateEnabled = s.AutoUpdate;
            VtAutoQuery = s.VtAutoQuery;
            RealTimeOn = s.RealTimeOnPersist;
        }

        public static void Persist()
        {
            var s = AppSettings.Load();
            s.FileRestoreGuard = FileRestoreGuard;
            s.UsbProtection = UsbProtection;
            s.DownloadProtection = DownloadProtection;
            s.BehaviorWatch = BehaviorWatch;
            s.StartupFoldersWatch = StartupFoldersWatch;
            s.ShowNotifications = ThreatAlerts;
            s.AutoUpdate = AutoUpdateEnabled;
            s.VtAutoQuery = VtAutoQuery;
            s.RealTimeOnPersist = RealTimeOn;
            AppSettings.Save(s);
        }

        public static void NotifyChanged()
        {
            Action h = Changed;
            if (h != null) h();
        }
    }
}
