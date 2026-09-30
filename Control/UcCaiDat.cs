/*using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcCaiDat : UserControl
    {
        bool suppressFlagEvents;

        public UcCaiDat()
        {
            InitializeComponent();
            BackColor = Theme.PageBg;
            // Responsive: cửa sổ nhỏ -> cuộn thay vì cắt nội dung
            Theme.ScrollablePage(this, tableLayoutPanel1, 980, 680);
            //Theme.StylePageHeader(lblSettingsTitle, lblSettingsSubtitle);
            Theme.StyleCard(grpGeneral, grpGuards, grpVt, grpData);
            Theme.StyleButton(btnSaveSettings, Theme.BtnRole.Primary);
            Theme.StyleNeutralButtons(btnResetDefaults, btnSamples, btnOpenData, btnClearCache,
                btnSaveVtKey, btnClearVtKey);
            // footer chuẩn Lịch sử: hàng nút 40px, status trái - hành động phải
            btnSaveSettings.Margin = new Padding(2, 12, 2, 12);
            btnResetDefaults.Margin = new Padding(2, 12, 2, 12);
            // Ẩn UI nhập API key VirusTotal ở MỌI bản — chỉ thao tác qua tệp vtapikey.txt.
            // Người dùng chỉ thấy trạng thái (lblVtStatus), không đổi được key.
            txtVtKey.Visible = false;
            btnSaveVtKey.Visible = false;
            btnClearVtKey.Visible = false;
            tlpVt.RowStyles[2].Height = 0;
            tlpVt.RowStyles[3].Height = 0;
            lblVtHint.Text = "API key VirusTotal do quản trị viên cấu hình (tệp vtapikey.txt).";
            LoadSettingsIntoUi();
            btnSaveSettings.Click += BtnSaveSettings_Click;
            btnResetDefaults.Click += BtnResetDefaults_Click;
            btnSamples.Click += BtnSamples_Click;
            btnOpenData.Click += BtnOpenData_Click;
            btnClearCache.Click += BtnClearCache_Click;
            btnSaveVtKey.Click += BtnSaveVtKey_Click;
            btnClearVtKey.Click += BtnClearVtKey_Click;
            FeatureFlags.Changed += OnFlagsChanged;
        }

        // keepPendingEdits: giữ nguyên 2 ô CHỜ LƯU (AutoStart ở registry, SendSamples) —
        // chúng chỉ được ghi khi bấm "Lưu", nên nạp lại từ đĩa sẽ xóa mất thao tác người
        // dùng vừa tích. Các ô còn lại là công tắc sống, nạp lại luôn đúng.
        private void LoadSettingsIntoUi(bool keepPendingEdits = false)
        {
            var s = AppSettings.Load();
            suppressFlagEvents = true;
            try
            {
                if (!keepPendingEdits)
                {
                    chkAutoStart.Checked = AppSettings.IsAutoStartEnabled();
                    chkSendSamples.Checked = s.SendSamples;
                }
                chkAutoUpdate.Checked = FeatureFlags.AutoUpdateEnabled;   // một nguồn với hàng "Tự động cập nhật"
                chkShowNotification.Checked = FeatureFlags.ThreatAlerts;  // một nguồn với hàng "Cảnh báo mối đe dọa"
                chkVtAutoQuery.Checked = FeatureFlags.VtAutoQuery;
                chkUsbGuard.Checked = FeatureFlags.UsbProtection;
                chkDownloadGuard.Checked = FeatureFlags.DownloadProtection;
                chkBehaviorGuard.Checked = FeatureFlags.BehaviorWatch;
                chkStartupGuard.Checked = FeatureFlags.StartupFoldersWatch;
                chkRestoreGuard.Checked = FeatureFlags.FileRestoreGuard;
            }
            finally { suppressFlagEvents = false; }
            RefreshVtStatus();
            RefreshDataInfo();
        }

        // Công tắc cờ: ghi + áp dụng NGAY (giống bảng tính năng tab Bảo vệ — một nguồn dữ liệu)
        private void FlagToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressFlagEvents || !IsHandleCreated) return;
            FeatureFlags.AutoUpdateEnabled = chkAutoUpdate.Checked;
            FeatureFlags.ThreatAlerts = chkShowNotification.Checked;
            FeatureFlags.VtAutoQuery = chkVtAutoQuery.Checked;
            FeatureFlags.UsbProtection = chkUsbGuard.Checked;
            FeatureFlags.DownloadProtection = chkDownloadGuard.Checked;
            FeatureFlags.BehaviorWatch = chkBehaviorGuard.Checked;
            FeatureFlags.StartupFoldersWatch = chkStartupGuard.Checked;
            FeatureFlags.FileRestoreGuard = chkRestoreGuard.Checked;
            FeatureFlags.Persist();
            GuardService.ApplyAll();
            FeatureFlags.NotifyChanged();
            lblGeneralHint.Text = "Đã ghi & áp dụng ngay lúc " + DateTime.Now.ToString("HH:mm:ss");
            lblGeneralHint.ForeColor = Theme.Green;
        }

        // hai mục còn lại (autostart registry + send samples) cần bấm Lưu
        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            var f = AppSettings.Load(); // giữ nguyên các cờ guard đã live-persist
            f.AutoStart = chkAutoStart.Checked;
            f.AutoUpdate = chkAutoUpdate.Checked;
            f.SendSamples = chkSendSamples.Checked;
            f.ShowNotifications = chkShowNotification.Checked;
            try
            {
                AppSettings.Save(f);
                FeatureFlags.ThreatAlerts = f.ShowNotifications;
                FeatureFlags.AutoUpdateEnabled = f.AutoUpdate;
                FeatureFlags.VtAutoQuery = chkVtAutoQuery.Checked;
                FeatureFlags.Persist();
                FeatureFlags.NotifyChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được tệp cài đặt: " + ex.Message,
                    "Cài đặt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool startupOk = AppSettings.ApplyAutoStart(f.AutoStart);
            chkAutoStart.Checked = AppSettings.IsAutoStartEnabled(); // trung thực với kết quả registry
            string note = f.AutoStart
                ? (startupOk ? "\nĐã đăng ký chạy cùng Windows." : "\nKHÔNG đăng ký được chạy cùng Windows (registry từ chối).")
                : (startupOk ? "\nĐã bỏ đăng ký chạy cùng Windows." : "");
            lblSavedAt.Text = "Đã lưu lúc " + DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            MessageBox.Show("Cài đặt đã lưu." + note, "Cài đặt",
                MessageBoxButtons.OK,
                startupOk ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnResetDefaults_Click(object sender, EventArgs e)
        {
            var r = MessageBox.Show(
                "Khôi phục toàn bộ thiết lập về mặc định?\n\n"
                + "Bật: thông báo, guard USB/Tải về/Hành vi/Startup/Chặn-restore.\n"
                + "Tắt: tự cập nhật, gửi mẫu, tra VT tự động.\n"
                + "(Tự khởi động cùng Windows KHÔNG bị thay đổi.)",
                "Khôi phục mặc định", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
            FeatureFlags.AutoUpdateEnabled = false;
            FeatureFlags.ThreatAlerts = true;
            FeatureFlags.VtAutoQuery = false;
            FeatureFlags.UsbProtection = true;
            FeatureFlags.DownloadProtection = true;
            FeatureFlags.BehaviorWatch = true;
            FeatureFlags.StartupFoldersWatch = true;
            FeatureFlags.FileRestoreGuard = true;
            FeatureFlags.Persist();
            var f = AppSettings.Load();
            f.SendSamples = false;
            AppSettings.Save(f);
            GuardService.ApplyAll();
            FeatureFlags.NotifyChanged();
            LoadSettingsIntoUi();
            lblSavedAt.Text = "Đã khôi phục mặc định lúc " + DateTime.Now.ToString("HH:mm:ss");
        }

        // Bào "virus mock" chính chủ — 11 tệp VÔ HẠI phủ đúng 3 kỹ thuật để test toàn trình
        private void BtnSamples_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> files = TestSamples.Create();
                MessageBox.Show(
                    "Đã tạo " + files.Count + " tệp mẫu VÔ HẠI tại:\n" + TestSamples.FolderPath
                    + "\n\n• 4 tệp khớp kỹ thuật 1 (chữ ký prefix ×2 gồm cả .js / hash SHA256 / tên 'eicar')"
                    + "\n• 4 tệp khớp kỹ thuật 2 (đuôi kép / PowerShell độc / VBS downloader / exe ẩn mồi câu)"
                    + "\n• 3 tệp sạch đối chứng (mồi câu dưới ngưỡng, script lành, README — KHÔNG được báo)"
                    + "\n\nThử ngay: Tổng quan → Quét tùy chọn → Chọn thư mục → mở TestSamples → Quét ngay.",
                    "Bộ tệp mẫu kiểm thử", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tạo được tệp mẫu: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnOpenData_Click(object sender, EventArgs e)
        {
            try
            {
                string dir = Path.GetDirectoryName(ScanHistoryStore.LogPath);
                Directory.CreateDirectory(dir);
                Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không mở được thư mục dữ liệu: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnClearCache_Click(object sender, EventArgs e)
        {
            ScanEngine.ClearScanCache();
            try
            {
                if (File.Exists(ScanEngine.ScanCachePath))
                {
                    File.Delete(ScanEngine.ScanCachePath);
                    MessageBox.Show("Đã xóa cache kết quả quét.\nLần quét kế tiếp sẽ đọc lại toàn bộ tệp từ đĩa.",
                        "Xóa cache", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Không có cache nào cần xóa (sẽ tạo lại khi quét).\nCache trong phiên đã được giải phóng.",
                        "Xóa cache", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Xóa cache thất bại: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshVtStatus()
        {
            bool hasKey = VirusTotalClient.IsConfigured;
            lblVtStatus.Text = hasKey ? "API key: đã cấu hình — sẵn sàng tra cứu cloud"
                                      : "API key: chưa cấu hình — tính năng VT sẽ bỏ qua";
            lblVtStatus.ForeColor = hasKey ? Theme.Green : Theme.Amber;
        }

        private void BtnSaveVtKey_Click(object sender, EventArgs e)
        {
            string key = (txtVtKey.Text ?? "").Trim();
            if (key.Length == 0)
            {
                MessageBox.Show("Hãy dán API key vào ô trống trước đã.",
                    "VirusTotal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                VirusTotalClient.SaveApiKey(key);
                txtVtKey.Clear();
                RefreshVtStatus();
                lblSavedAt.Text = "API key VirusTotal đã lưu lúc " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được API key: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnClearVtKey_Click(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(VirusTotalClient.ApiKeyPath))
                    File.Delete(VirusTotalClient.ApiKeyPath);
                RefreshVtStatus();
                lblSavedAt.Text = "Đã gỡ API key VirusTotal";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không xóa được tệp key: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshDataInfo()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            lblAppVersion.Text = "Phiên bản ứng dụng: " + ver.ToString(3);
            DateTime when;
            lblDbUpdate.Text = ScanHistoryStore.TryGetLastSignatureUpdate(out when)
                ? "CSDL chữ ký: cập nhật lúc " + when.ToString("HH:mm dd/MM/yyyy")
                : "CSDL chữ ký: chưa ghi nhận lần cập nhật";
            lblQuarantined.Text = "Đang cách ly: " + ScanEngine.CountQuarantined() + " tệp";
            lblDataPath.Text = "Thư mục dữ liệu: " + Path.GetDirectoryName(ScanHistoryStore.LogPath);
        }

        // Lật cờ từ nơi khác (bảng tab Bảo vệ, auto-update nền) -> UI phản hồi theo
        private void OnFlagsChanged()
        {
            try { BeginInvoke((MethodInvoker)(() => LoadSettingsIntoUi(true))); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) LoadSettingsIntoUi();
        }
    }
}
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcCaiDat : UserControl
    {
        private bool suppressFlagEvents;

        public UcCaiDat()
        {
            InitializeComponent();

            BackColor = Theme.PageBg;

            // Responsive
            Theme.ScrollablePage(
                this,
                tableLayoutPanel1,
                980,
                680);

            Theme.StyleCard(
                grpGeneral,
                grpGuards,
                grpVt,
                grpData);

            Theme.StyleButton(
                btnSaveSettings,
                Theme.BtnRole.Primary);

            Theme.StyleNeutralButtons(
                btnResetDefaults,
                btnSamples,
                btnOpenData,
                btnClearCache,
                btnSaveVtKey,
                btnClearVtKey);

            btnSaveSettings.Margin =
                new Padding(2, 12, 2, 12);

            btnResetDefaults.Margin =
                new Padding(2, 12, 2, 12);

            // =====================================================
            // VIRUSTOTAL
            // =====================================================

            // API key VirusTotal vẫn lưu local trong vtapikey.txt.
            // Không đưa API key vào SQL vì CSDL hiện tại
            // không có cột tương ứng.
            txtVtKey.Visible = false;
            btnSaveVtKey.Visible = false;
            btnClearVtKey.Visible = false;

            if (tlpVt.RowStyles.Count > 2)
            {
                tlpVt.RowStyles[2].Height = 0;
            }

            if (tlpVt.RowStyles.Count > 3)
            {
                tlpVt.RowStyles[3].Height = 0;
            }

            lblVtHint.Text =
                "API key VirusTotal do quản trị viên cấu hình " +
                "(tệp vtapikey.txt).";

            // =====================================================
            // EVENTS
            // =====================================================

            btnSaveSettings.Click +=
                BtnSaveSettings_Click;

            btnResetDefaults.Click +=
                BtnResetDefaults_Click;

            btnSamples.Click +=
                BtnSamples_Click;

            btnOpenData.Click +=
                BtnOpenData_Click;

            btnClearCache.Click +=
                BtnClearCache_Click;

            btnSaveVtKey.Click +=
                BtnSaveVtKey_Click;

            btnClearVtKey.Click +=
                BtnClearVtKey_Click;

            FeatureFlags.Changed +=
                OnFlagsChanged;

            /*
             * QUAN TRỌNG:
             *
             * Không override Dispose() tại file này vì
             * UcCaiDat.Designer.cs đã có Dispose(bool).
             *
             * Dùng event Disposed để hủy đăng ký
             * FeatureFlags.Changed.
             */
            Disposed += UcCaiDat_Disposed;

            // =====================================================
            // LOAD
            // =====================================================

            LoadSettingsIntoUi();
        }

        // =========================================================
        // LOAD SETTINGS
        // =========================================================

        private void LoadSettingsIntoUi(
            bool keepPendingEdits = false)
        {
            SettingsFlags settings;

            try
            {
                settings = AppSettings.Load();
            }
            catch (Exception)
            {
                return;
            }

            if (settings == null)
            {
                return;
            }

            suppressFlagEvents = true;

            try
            {
                /*
                 * Hai mục này chỉ thay đổi khi người dùng
                 * bấm nút Lưu.
                 */
                if (!keepPendingEdits)
                {
                    // Registry là trạng thái AutoStart thực tế.
                    chkAutoStart.Checked =
                        AppSettings.IsAutoStartEnabled();

                    // SendSamples ánh xạ tới:
                    // dbo.Settings.SubmitSamples
                    chkSendSamples.Checked =
                        settings.SendSamples;
                }

                // Các cờ live lấy từ FeatureFlags.
                chkAutoUpdate.Checked =
                    FeatureFlags.AutoUpdateEnabled;

                chkShowNotification.Checked =
                    FeatureFlags.ThreatAlerts;

                chkVtAutoQuery.Checked =
                    FeatureFlags.VtAutoQuery;

                chkUsbGuard.Checked =
                    FeatureFlags.UsbProtection;

                chkDownloadGuard.Checked =
                    FeatureFlags.DownloadProtection;

                chkBehaviorGuard.Checked =
                    FeatureFlags.BehaviorWatch;

                chkStartupGuard.Checked =
                    FeatureFlags.StartupFoldersWatch;

                chkRestoreGuard.Checked =
                    FeatureFlags.FileRestoreGuard;
            }
            finally
            {
                suppressFlagEvents = false;
            }

            RefreshVtStatus();
            RefreshDataInfo();
        }

        // =========================================================
        // LIVE FEATURE FLAGS
        // =========================================================

        private void FlagToggle_CheckedChanged(
            object sender,
            EventArgs e)
        {
            if (suppressFlagEvents ||
                !IsHandleCreated)
            {
                return;
            }

            try
            {
                FeatureFlags.AutoUpdateEnabled =
                    chkAutoUpdate.Checked;

                FeatureFlags.ThreatAlerts =
                    chkShowNotification.Checked;

                FeatureFlags.VtAutoQuery =
                    chkVtAutoQuery.Checked;

                FeatureFlags.UsbProtection =
                    chkUsbGuard.Checked;

                FeatureFlags.DownloadProtection =
                    chkDownloadGuard.Checked;

                FeatureFlags.BehaviorWatch =
                    chkBehaviorGuard.Checked;

                FeatureFlags.StartupFoldersWatch =
                    chkStartupGuard.Checked;

                FeatureFlags.FileRestoreGuard =
                    chkRestoreGuard.Checked;

                /*
                 * FeatureFlags
                 *      ↓
                 * AppSettings
                 *      ↓
                 * SettingsRepository
                 *      ↓
                 * dbo.Settings
                 */
                FeatureFlags.Persist();

                GuardService.ApplyAll();

                FeatureFlags.NotifyChanged();

                lblGeneralHint.Text =
                    "Đã ghi & áp dụng ngay lúc " +
                    DateTime.Now.ToString("HH:mm:ss");

                lblGeneralHint.ForeColor =
                    Theme.Green;
            }
            catch (Exception ex)
            {
                lblGeneralHint.Text =
                    "Không thể áp dụng cài đặt: " +
                    ex.Message;

                lblGeneralHint.ForeColor =
                    Theme.Amber;
            }
        }

        // =========================================================
        // SAVE SETTINGS
        // =========================================================

        private void BtnSaveSettings_Click(
            object sender,
            EventArgs e)
        {
            SettingsFlags settings;

            try
            {
                /*
                 * Đọc lại cài đặt để giữ nguyên các guard
                 * đã được lưu trực tiếp trước đó.
                 */
                settings =
                    AppSettings.Load();

                if (settings == null)
                {
                    MessageBox.Show(
                        "Không đọc được cài đặt hiện tại.",
                        "Cài đặt",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                settings.AutoStart =
                    chkAutoStart.Checked;

                settings.AutoUpdate =
                    chkAutoUpdate.Checked;

                settings.SendSamples =
                    chkSendSamples.Checked;

                settings.ShowNotifications =
                    chkShowNotification.Checked;

                // Lưu Settings.
                AppSettings.Save(settings);

                // Đồng bộ FeatureFlags.
                FeatureFlags.ThreatAlerts =
                    settings.ShowNotifications;

                FeatureFlags.AutoUpdateEnabled =
                    settings.AutoUpdate;

                FeatureFlags.VtAutoQuery =
                    chkVtAutoQuery.Checked;

                FeatureFlags.Persist();

                FeatureFlags.NotifyChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không lưu được cài đặt:\n" +
                    ex.Message,
                    "Cài đặt",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // =====================================================
            // WINDOWS AUTO START
            // =====================================================

            bool startupOk =
                AppSettings.ApplyAutoStart(
                    settings.AutoStart);

            /*
             * Kiểm tra lại Registry để UI phản ánh
             * đúng trạng thái thực tế.
             */
            chkAutoStart.Checked =
                AppSettings.IsAutoStartEnabled();

            string note;

            if (settings.AutoStart)
            {
                note = startupOk
                    ? "\nĐã đăng ký chạy cùng Windows."
                    : "\nKHÔNG đăng ký được chạy cùng Windows.";
            }
            else
            {
                note = startupOk
                    ? "\nĐã bỏ đăng ký chạy cùng Windows."
                    : "";
            }

            lblSavedAt.Text =
                "Đã lưu lúc " +
                DateTime.Now.ToString(
                    "HH:mm:ss dd/MM/yyyy");

            MessageBox.Show(
                "Cài đặt đã lưu." + note,
                "Cài đặt",
                MessageBoxButtons.OK,
                startupOk
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Warning);

            RefreshDataInfo();
        }

        // =========================================================
        // RESET DEFAULTS
        // =========================================================

        private void BtnResetDefaults_Click(
            object sender,
            EventArgs e)
        {
            DialogResult result =
                MessageBox.Show(
                    "Khôi phục toàn bộ thiết lập về mặc định?\n\n" +
                    "Bật: thông báo, guard USB/Tải về/Hành vi/" +
                    "Startup/Chặn-restore.\n" +
                    "Tắt: tự cập nhật, gửi mẫu, tra VT tự động.\n" +
                    "(Tự khởi động cùng Windows KHÔNG bị thay đổi.)",
                    "Khôi phục mặc định",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // =================================================
                // FEATURE FLAGS
                // =================================================

                FeatureFlags.AutoUpdateEnabled =
                    false;

                FeatureFlags.ThreatAlerts =
                    true;

                FeatureFlags.VtAutoQuery =
                    false;

                FeatureFlags.UsbProtection =
                    true;

                FeatureFlags.DownloadProtection =
                    true;

                FeatureFlags.BehaviorWatch =
                    true;

                FeatureFlags.StartupFoldersWatch =
                    true;

                FeatureFlags.FileRestoreGuard =
                    true;

                FeatureFlags.Persist();

                // =================================================
                // SEND SAMPLES
                // =================================================

                SettingsFlags settings =
                    AppSettings.Load();

                if (settings != null)
                {
                    settings.SendSamples =
                        false;

                    AppSettings.Save(settings);
                }

                // =================================================
                // APPLY
                // =================================================

                GuardService.ApplyAll();

                FeatureFlags.NotifyChanged();

                LoadSettingsIntoUi();

                lblSavedAt.Text =
                    "Đã khôi phục mặc định lúc " +
                    DateTime.Now.ToString(
                        "HH:mm:ss");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không khôi phục được cài đặt:\n" +
                    ex.Message,
                    "Cài đặt",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // =========================================================
        // TEST SAMPLES
        // =========================================================

        private void BtnSamples_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                List<string> files =
                    TestSamples.Create();

                MessageBox.Show(
                    "Đã tạo " +
                    files.Count +
                    " tệp mẫu VÔ HẠI tại:\n" +
                    TestSamples.FolderPath +

                    "\n\n• 4 tệp kiểm tra kỹ thuật chữ ký" +
                    "\n• 4 tệp kiểm tra heuristic" +
                    "\n• 3 tệp sạch đối chứng" +

                    "\n\nThử ngay:" +
                    "\nTổng quan → Quét tùy chọn → " +
                    "Chọn thư mục TestSamples → Quét ngay.",

                    "Bộ tệp mẫu kiểm thử",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không tạo được tệp mẫu:\n" +
                    ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // =========================================================
        // OPEN LOCAL DATA
        // =========================================================

        private void BtnOpenData_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                /*
                 * ScanHistory đã chuyển sang SQL Server.
                 *
                 * Vì vậy không còn dùng:
                 *
                 * ScanHistoryStore.LogPath
                 *
                 * DataDir vẫn được dùng cho:
                 *
                 * - Quarantine
                 * - scancache.dat
                 * - settings.ini
                 * - vtapikey.txt
                 */
                string dataDirectory =
                    DataDir.Resolve("");

                if (string.IsNullOrWhiteSpace(
                    dataDirectory))
                {
                    throw new InvalidOperationException(
                        "Không xác định được thư mục dữ liệu.");
                }

                Directory.CreateDirectory(
                    dataDirectory);

                Process.Start(
                    "explorer.exe",
                    "\"" +
                    dataDirectory +
                    "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không mở được thư mục dữ liệu:\n" +
                    ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // =========================================================
        // CLEAR SCAN CACHE
        // =========================================================

        private void BtnClearCache_Click(
            object sender,
            EventArgs e)
        {
            /*
             * Xóa cache trong RAM trước.
             */
            ScanEngine.ClearScanCache();

            try
            {
                /*
                 * scancache.dat vẫn là cache local.
                 * Không chuyển cache này vào SQL Server.
                 */
                if (File.Exists(
                    ScanEngine.ScanCachePath))
                {
                    File.Delete(
                        ScanEngine.ScanCachePath);

                    MessageBox.Show(
                        "Đã xóa cache kết quả quét.\n" +
                        "Lần quét kế tiếp sẽ đọc lại toàn bộ " +
                        "tệp từ đĩa.",
                        "Xóa cache",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Không có cache nào cần xóa.\n" +
                        "Cache trong phiên đã được giải phóng.",
                        "Xóa cache",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Xóa cache thất bại:\n" +
                    ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            RefreshDataInfo();
        }

        // =========================================================
        // VIRUSTOTAL STATUS
        // =========================================================

        private void RefreshVtStatus()
        {
            bool hasKey =
                VirusTotalClient.IsConfigured;

            lblVtStatus.Text =
                hasKey
                    ? "API key: đã cấu hình — sẵn sàng tra cứu cloud"
                    : "API key: chưa cấu hình — tính năng VT sẽ bỏ qua";

            lblVtStatus.ForeColor =
                hasKey
                    ? Theme.Green
                    : Theme.Amber;
        }

        // =========================================================
        // VIRUSTOTAL KEY
        // =========================================================
        //
        // Các control này hiện bị ẩn nhưng vẫn giữ handler
        // để tương thích với Designer/code cũ.
        // =========================================================

        private void BtnSaveVtKey_Click(
            object sender,
            EventArgs e)
        {
            string key =
                (txtVtKey.Text ?? "")
                .Trim();

            if (key.Length == 0)
            {
                MessageBox.Show(
                    "Hãy nhập API key trước.",
                    "VirusTotal",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                VirusTotalClient.SaveApiKey(
                    key);

                txtVtKey.Clear();

                RefreshVtStatus();

                lblSavedAt.Text =
                    "API key VirusTotal đã lưu lúc " +
                    DateTime.Now.ToString(
                        "HH:mm:ss");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không lưu được API key:\n" +
                    ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void BtnClearVtKey_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                if (File.Exists(
                    VirusTotalClient.ApiKeyPath))
                {
                    File.Delete(
                        VirusTotalClient.ApiKeyPath);
                }

                RefreshVtStatus();

                lblSavedAt.Text =
                    "Đã gỡ API key VirusTotal";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không xóa được tệp key:\n" +
                    ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // =========================================================
        // DATA INFO
        // =========================================================

        private void RefreshDataInfo()
        {
            // =====================================================
            // APP VERSION
            // =====================================================

            Version version =
                Assembly
                    .GetExecutingAssembly()
                    .GetName()
                    .Version;

            if (version != null)
            {
                lblAppVersion.Text =
                    "Phiên bản ứng dụng: " +
                    version.ToString(3);
            }
            else
            {
                lblAppVersion.Text =
                    "Phiên bản ứng dụng: —";
            }

            // =====================================================
            // DATABASE UPDATE
            // =====================================================

            /*
             * TryGetLastSignatureUpdate()
             * hiện đọc:
             *
             * dbo.Settings.LastDatabaseUpdate
             *
             * Không còn dùng dbupdate.txt.
             */
            DateTime lastUpdate;

            if (ScanHistoryStore
                .TryGetLastSignatureUpdate(
                    out lastUpdate))
            {
                lblDbUpdate.Text =
                    "CSDL chữ ký: cập nhật lúc " +
                    lastUpdate.ToString(
                        "HH:mm dd/MM/yyyy");
            }
            else
            {
                lblDbUpdate.Text =
                    "CSDL chữ ký: chưa ghi nhận lần cập nhật";
            }

            // =====================================================
            // QUARANTINE
            // =====================================================

            lblQuarantined.Text =
                "Đang cách ly: " +
                ScanEngine.CountQuarantined() +
                " tệp";

            // =====================================================
            // LOCAL DATA
            // =====================================================

            try
            {
                lblDataPath.Text =
                    "Dữ liệu cục bộ: " +
                    DataDir.Resolve("");
            }
            catch
            {
                lblDataPath.Text =
                    "Dữ liệu cục bộ: không xác định";
            }
        }

        // =========================================================
        // FEATURE FLAGS CHANGED
        // =========================================================

        private void OnFlagsChanged()
        {
            if (IsDisposed ||
                Disposing)
            {
                return;
            }

            try
            {
                if (!IsHandleCreated)
                {
                    return;
                }

                BeginInvoke(
                    (MethodInvoker)
                    delegate
                    {
                        if (!IsDisposed &&
                            !Disposing)
                        {
                            LoadSettingsIntoUi(true);
                        }
                    });
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        // =========================================================
        // CONTROL VISIBLE
        // =========================================================

        protected override void OnVisibleChanged(
            EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible)
            {
                LoadSettingsIntoUi();
            }
        }

        // =========================================================
        // CONTROL DISPOSED
        // =========================================================

        /*
         * KHÔNG override Dispose(bool) tại đây.
         *
         * UcCaiDat.Designer.cs đã có Dispose(bool).
         *
         * Dùng event Disposed để hủy đăng ký static event.
         */
        private void UcCaiDat_Disposed(
            object sender,
            EventArgs e)
        {
            FeatureFlags.Changed -=
                OnFlagsChanged;

            Disposed -=
                UcCaiDat_Disposed;
        }
    }
}