using System;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Công tắc cho 10 tính năng ở tab Bảo vệ — lưu/đọc qua settings.ini
    /// để nhớ giữa các lần mở app. GuardService lắng nghe Changed để bật/tắt cơ chế thật.
    /// </summary>
    //public static class FeatureFlags
    //{
    /*public static event Action Changed;

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
}*/
    /// <summary>
    /// Quản lý trạng thái các tính năng bảo vệ của ứng dụng.
    ///
    /// FeatureFlags KHÔNG truy cập SQL Server trực tiếp.
    /// Việc đọc/ghi được thực hiện thông qua AppSettings.
    ///
    /// Các thiết lập có trong SQL Server:
    /// - RealTimeOn            -> Settings.RealTimeProtection
    /// - FileRestoreGuard      -> Settings.FileProtection
    /// - UsbProtection         -> Settings.USBProtection
    /// - DownloadProtection    -> Settings.DownloadProtection
    /// - ThreatAlerts          -> Settings.ShowNotifications
    /// - AutoUpdateEnabled     -> Settings.AutoUpdate
    ///
    /// Các thiết lập chưa có cột tương ứng trong CSDL:
    /// - BehaviorWatch
    /// - StartupFoldersWatch
    /// - VtAutoQuery
    ///
    /// Các thiết lập trên tiếp tục được AppSettings lưu local.
    ///
    /// GuardService lắng nghe sự kiện Changed để áp dụng
    /// trạng thái bật/tắt vào các cơ chế bảo vệ thực tế.
    /// </summary>
    public static class FeatureFlags
    {
        // =========================================================
        // EVENT
        // =========================================================

        public static event Action Changed;

        // =========================================================
        // REAL-TIME PROTECTION
        // =========================================================

        /// <summary>
        /// Bảo vệ thời gian thực.
        /// SQL: Settings.RealTimeProtection
        /// </summary>
        public static bool RealTimeOn { get; set; }

        // =========================================================
        // FILE PROTECTION
        // =========================================================

        /// <summary>
        /// Bảo vệ tệp.
        /// Dùng để ngăn khôi phục tệp vẫn còn được xác định là độc hại.
        /// SQL: Settings.FileProtection
        /// </summary>
        public static bool FileRestoreGuard { get; set; }

        // =========================================================
        // USB PROTECTION
        // =========================================================

        /// <summary>
        /// Tự động kiểm tra thiết bị USB/removable mới được kết nối.
        /// SQL: Settings.USBProtection
        /// </summary>
        public static bool UsbProtection { get; set; }

        // =========================================================
        // DOWNLOAD PROTECTION
        // =========================================================

        /// <summary>
        /// Theo dõi và kiểm tra các tệp tải xuống.
        /// SQL: Settings.DownloadProtection
        /// </summary>
        public static bool DownloadProtection { get; set; }

        // =========================================================
        // BEHAVIOR WATCH
        // =========================================================

        /// <summary>
        /// Theo dõi hành vi tiến trình.
        ///
        /// Hiện chưa có cột tương ứng trong CSDL gốc,
        /// vì vậy AppSettings tiếp tục lưu giá trị này local.
        /// </summary>
        public static bool BehaviorWatch { get; set; }

        // =========================================================
        // STARTUP FOLDER WATCH
        // =========================================================

        /// <summary>
        /// Theo dõi các thư mục Startup của Windows.
        ///
        /// Hiện chưa có cột tương ứng trong CSDL gốc,
        /// vì vậy AppSettings tiếp tục lưu giá trị này local.
        /// </summary>
        public static bool StartupFoldersWatch { get; set; }

        // =========================================================
        // THREAT NOTIFICATIONS
        // =========================================================

        /// <summary>
        /// Cho phép hiển thị thông báo khi phát hiện mối đe dọa.
        /// SQL: Settings.ShowNotifications
        /// </summary>
        public static bool ThreatAlerts { get; set; }

        // =========================================================
        // AUTO UPDATE
        // =========================================================

        /// <summary>
        /// Cho phép tự động kiểm tra/cập nhật CSDL virus.
        /// SQL: Settings.AutoUpdate
        /// </summary>
        public static bool AutoUpdateEnabled { get; set; }

        // =========================================================
        // VIRUSTOTAL AUTO QUERY
        // =========================================================

        /// <summary>
        /// Tự động truy vấn VirusTotal khi cần.
        ///
        /// Hiện chưa có cột tương ứng trong CSDL gốc,
        /// vì vậy AppSettings tiếp tục lưu giá trị này local.
        /// </summary>
        public static bool VtAutoQuery { get; set; }

        // =========================================================
        // STATIC CONSTRUCTOR
        // =========================================================

        static FeatureFlags()
        {
            /*
             * Giá trị mặc định.
             *
             * Nếu SQL Server hoặc settings.ini chưa có dữ liệu,
             * các giá trị này được dùng làm trạng thái ban đầu.
             */

            RealTimeOn = false;

            FileRestoreGuard = true;

            UsbProtection = true;

            DownloadProtection = true;

            BehaviorWatch = true;

            StartupFoldersWatch = true;

            ThreatAlerts = true;

            AutoUpdateEnabled = false;

            VtAutoQuery = false;

            // Sau khi đặt mặc định,
            // đọc trạng thái đã lưu.
            LoadFromStore();
        }

        // =========================================================
        // LOAD
        // =========================================================

        /// <summary>
        /// Đọc trạng thái các tính năng thông qua AppSettings.
        ///
        /// AppSettings sẽ tự quyết định dữ liệu nào lấy từ SQL Server
        /// và dữ liệu nào lấy từ file local.
        /// </summary>
        public static void LoadFromStore()
        {
            try
            {
                SettingsFlags settings =
                    AppSettings.Load();

                if (settings == null)
                    return;

                RealTimeOn =
                    settings.RealTimeOnPersist;

                FileRestoreGuard =
                    settings.FileRestoreGuard;

                UsbProtection =
                    settings.UsbProtection;

                DownloadProtection =
                    settings.DownloadProtection;

                BehaviorWatch =
                    settings.BehaviorWatch;

                StartupFoldersWatch =
                    settings.StartupFoldersWatch;

                ThreatAlerts =
                    settings.ShowNotifications;

                AutoUpdateEnabled =
                    settings.AutoUpdate;

                VtAutoQuery =
                    settings.VtAutoQuery;
            }
            catch (Exception)
            {
                /*
                 * Nếu SQL Server hoặc file local gặp lỗi,
                 * giữ nguyên trạng thái hiện tại/mặc định.
                 *
                 * Không để lỗi cài đặt làm ứng dụng crash.
                 */
            }
        }

        // =========================================================
        // PERSIST
        // =========================================================

        /// <summary>
        /// Lưu trạng thái hiện tại.
        ///
        /// FeatureFlags chỉ gửi dữ liệu cho AppSettings.
        /// AppSettings chịu trách nhiệm:
        ///
        /// SQL Server:
        /// - RealTimeOn
        /// - FileRestoreGuard
        /// - UsbProtection
        /// - DownloadProtection
        /// - ThreatAlerts
        /// - AutoUpdateEnabled
        ///
        /// Local:
        /// - BehaviorWatch
        /// - StartupFoldersWatch
        /// - VtAutoQuery
        /// </summary>
        public static void Persist()
        {
            try
            {
                /*
                 * Đọc trước để giữ nguyên những setting
                 * mà FeatureFlags không quản lý.
                 *
                 * Ví dụ:
                 * AutoStart
                 * SendSamples
                 */
                SettingsFlags settings =
                    AppSettings.Load();

                if (settings == null)
                    settings = new SettingsFlags();

                // -----------------------------
                // SQL SERVER
                // -----------------------------

                settings.RealTimeOnPersist =
                    RealTimeOn;

                settings.FileRestoreGuard =
                    FileRestoreGuard;

                settings.UsbProtection =
                    UsbProtection;

                settings.DownloadProtection =
                    DownloadProtection;

                settings.ShowNotifications =
                    ThreatAlerts;

                settings.AutoUpdate =
                    AutoUpdateEnabled;

                // -----------------------------
                // LOCAL SETTINGS
                // -----------------------------

                settings.BehaviorWatch =
                    BehaviorWatch;

                settings.StartupFoldersWatch =
                    StartupFoldersWatch;

                settings.VtAutoQuery =
                    VtAutoQuery;

                // AppSettings tự xử lý SQL + local.
                AppSettings.Save(settings);
            }
            catch (Exception)
            {
                /*
                 * Không để lỗi lưu setting
                 * làm ứng dụng bị dừng.
                 */
            }
        }

        // =========================================================
        // NOTIFY
        // =========================================================

        /// <summary>
        /// Thông báo cho GuardService rằng trạng thái tính năng
        /// đã thay đổi để GuardService áp dụng lại cấu hình.
        /// </summary>
        public static void NotifyChanged()
        {
            Action handler = Changed;

            if (handler != null)
            {
                handler();
            }
        }
    }
}



