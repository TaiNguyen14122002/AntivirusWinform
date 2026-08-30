using System;
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
            Theme.StylePageHeader(lblSettingsTitle, lblSettingsSubtitle);
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

        private void LoadSettingsIntoUi()
        {
            var s = AppSettings.Load();
            suppressFlagEvents = true;
            try
            {
                chkAutoStart.Checked = AppSettings.IsAutoStartEnabled();
                chkAutoUpdate.Checked = FeatureFlags.AutoUpdateEnabled;   // một nguồn với hàng "Tự động cập nhật"
                chkSendSamples.Checked = s.SendSamples;
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

        // Bào "virus mock" chính chủ — 6 tệp VÔ HẠI phủ đúng 3 kỹ thuật để test toàn trình
        private void BtnSamples_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> files = TestSamples.Create();
                MessageBox.Show(
                    "Đã tạo " + files.Count + " tệp mẫu VÔ HẠI tại:\n" + TestSamples.FolderPath
                    + "\n\n• 3 tệp khớp kỹ thuật 1 (chữ ký prefix / hash SHA256 / tên 'eicar')"
                    + "\n• 2 tệp khớp kỹ thuật 2 (đuôi kép .pdf.exe / PowerShell độc)"
                    + "\n• 1 tệp sạch đối chứng (KHÔNG được báo)"
                    + "\n\nThử ngay: Tổng quan → Quét tùy chọn → Chọn thư mục → mở XVirus-Samples → Quét ngay.",
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
            try { BeginInvoke((MethodInvoker)(LoadSettingsIntoUi)); }
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
