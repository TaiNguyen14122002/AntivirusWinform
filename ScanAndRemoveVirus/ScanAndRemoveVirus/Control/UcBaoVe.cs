using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcBaoVe : UserControl
    {
        private const string FeatureRealTime = "Bảo vệ thời gian thực";
        private const string FeatureAutoQuarantine = "Tự động cách ly";

        private void LoadDuLieuBaoVe()
        {
            dgvProtecctionFeatures.Rows.Clear();

            dgvProtecctionFeatures.Rows.Add(
                "Bảo vệ thời gian thực",
                "Tự động giám sát tệp và chương trình đang được mở hoặc thực thi",
                "Bật",
                "Tắt"
            );

            dgvProtecctionFeatures.Rows.Add(
                "Bảo vệ tệp",
                "Kiểm tra tệp khi người dùng mở, sao chép hoặc tải xuống",
                "Bật",
                "Tắt"
            );

            dgvProtecctionFeatures.Rows.Add(
                "Bảo vệ USB",
                "Kiểm tra thiết bị USB và các tệp khi kết nối với máy tính",
                "Bật",
                "Tắt"
            );

            dgvProtecctionFeatures.Rows.Add(
                "Bảo vệ tải xuống",
                "Kiểm tra các tệp được tải xuống từ Internet",
                "Bật",
                "Tắt"
            );

            dgvProtecctionFeatures.Rows.Add(
                "Phát hiện hành vi đáng ngờ",
                "Phân tích hành vi của chương trình để phát hiện hoạt động bất thường",
                "Bật",
                "Tắt"
            );

            dgvProtecctionFeatures.Rows.Add(
                "Bảo vệ thư mục hệ thống",
                "Giám sát các thư mục quan trọng của Windows",
                "Bật",
                "Tắt"
            );

            dgvProtecctionFeatures.Rows.Add(
                "Tự động cách ly",
                "Tự động đưa tệp nguy hiểm vào khu vực cách ly",
                "Bật",
                "Tắt"
            );

            dgvProtecctionFeatures.Rows.Add(
                "Cảnh báo mối đe dọa",
                "Hiển thị thông báo khi phát hiện virus hoặc hoạt động đáng ngờ",
                "Bật",
                "Tắt"
            );

            dgvProtecctionFeatures.Rows.Add(
                "Tự động cập nhật",
                "Tự động cập nhật cơ sở dữ liệu virus",
                "Bật",
                "Tắt"
            );

            dgvProtecctionFeatures.Rows.Add(
                "Bảo vệ trình duyệt",
                "Kiểm tra các trang web và nội dung tải xuống có nguy cơ gây hại",
                "Tắt",
                "Bật"
            );
        }
        public UcBaoVe()
        {
            InitializeComponent();
            BackColor = Theme.PageBg;
            Theme.StyleNeutralButtons(btnSaveSettings);
            LoadDuLieuBaoVe();
            Theme.StyleGrid(dgvProtecctionFeatures);
            // Chỉ 2 hàng dưới đây điều khiển tính năng THẬT; các hàng khác là hiển thị mẫu
            dgvProtecctionFeatures.CellClick += Grid_CellClick;
            dgvProtecctionFeatures.CellFormatting += Grid_CellFormatting;
            RealTimeProtection.StatusChanged += OnRealTimeStatus;
            RealTimeProtection.ThreatDetected += OnThreatDetected;
            SyncRealFeaturesUi();
            SyncProtectionStats();
            // Cài đặt: nạp flags đã lưu + nút Lưu ghi thật (tự khởi động = vào registry Run)
            LoadSettingsIntoUi();
            btnSaveSettings.Click += BtnSaveSettings_Click;
        }

        private void LoadSettingsIntoUi()
        {
            var s = AppSettings.Load();
            chkAutoStart.Checked = AppSettings.IsAutoStartEnabled();
            chkAutoUpdate.Checked = s.AutoUpdate;
            chkSendSamples.Checked = s.SendSamples;
            chkShowNotification.Checked = s.ShowNotifications;
        }

        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            var f = new SettingsFlags
            {
                AutoStart = chkAutoStart.Checked,
                AutoUpdate = chkAutoUpdate.Checked,
                SendSamples = chkSendSamples.Checked,
                ShowNotifications = chkShowNotification.Checked
            };
            try
            {
                AppSettings.Save(f);
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
            MessageBox.Show("Cài đặt đã lưu." + note, "Cài đặt",
                MessageBoxButtons.OK,
                startupOk ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        // Panel "Trạng thái bảo vệ": mọi số liệu lấy từ source thật (không còn text mock 2025)
        private void SyncProtectionStats()
        {
            bool on = RealTimeProtection.IsRunning;
            lblStatusValue.Text = on
                ? "Tất cả các tính năng đang hoạt động tốt"
                : "Bảo vệ thời gian thực đang tắt";
            lblStatusValue.ForeColor = on ? Theme.Green : Theme.Red;

            lblDatabaseVersionValue.Text = System.Reflection.Assembly
                .GetExecutingAssembly().GetName().Version.ToString(3);

            DateTime last;
            if (ScanHistoryStore.TryGetLastSignatureUpdate(out last))
            {
                lblDatabaseValue2.Text = "Đã cập nhật";
                lblDatabaseValue2.ForeColor = Theme.Green;
                lblUpdateDateValue.Text = last.ToString("dd/MM/yyyy HH:mm");
            }
            else
            {
                lblDatabaseValue2.Text = "Chưa cập nhật";
                lblDatabaseValue2.ForeColor = Theme.Amber;
                lblUpdateDateValue.Text = "—";
            }

            HistoryEntry lastRt = ScanHistoryStore.LatestOfType("Bảo vệ thời gian thực");
            lblRealtimeScanValue.Text = lastRt == null
                ? "Chưa có quét thời gian thực"
                : lastRt.Time.ToString("dd/MM/yyyy HH:mm");

            lblScannedFilesValue.Text = ScanHistoryStore.TotalFilesScanned().ToString("N0");
            lblBlockedThreaetsValue.Text = ScanHistoryStore.TotalThreatsDetected().ToString("N0");
        }

        // Mở lại tab -> refresh (số liệu có thể đổi do quét/RT ở tab khác)
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) { SyncProtectionStats(); SyncRealFeaturesUi(); LoadSettingsIntoUi(); }
        }

        // Cột Trạng thái: Bật xanh lá / Tắt đỏ
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            if (e.ColumnIndex == colStatus.Index)
                Theme.PaintStatusCell(e, e.Value.ToString());
            else if (e.ColumnIndex == colAction.Index)
            {
                e.CellStyle.ForeColor = Theme.Blue;
                e.CellStyle.Font = Theme.BoldFont;
                e.CellStyle.SelectionForeColor = Theme.Blue;
            }
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colAction.Index) return;
            string feature = Convert.ToString(dgvProtecctionFeatures.Rows[e.RowIndex].Cells[colFeature.Index].Value);
            if (feature == FeatureRealTime)
            {
                if (RealTimeProtection.IsRunning) RealTimeProtection.Stop();
                else RealTimeProtection.Start();
                SyncRealFeaturesUi();
            }
            else if (feature == FeatureAutoQuarantine)
            {
                RealTimeProtection.AutoQuarantine = !RealTimeProtection.AutoQuarantine;
                SyncRealFeaturesUi();
            }
            else
            {
                MessageBox.Show("Tính năng \"" + feature + "\" là hiển thị mẫu — bản mô phỏng chưa triển khai.",
                    "Chưa khả dụng", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Đồng bộ ô "Trạng thái" của 2 tính năng thật theo đúng state trong engine
        private void SyncRealFeaturesUi()
        {
            foreach (DataGridViewRow row in dgvProtecctionFeatures.Rows)
            {
                string feature = Convert.ToString(row.Cells[colFeature.Index].Value);
                if (feature == FeatureRealTime)
                    SetFeatureState(row, RealTimeProtection.IsRunning);
                else if (feature == FeatureAutoQuarantine)
                    SetFeatureState(row, RealTimeProtection.AutoQuarantine);
            }
        }

        private void SetFeatureState(DataGridViewRow row, bool on)
        {
            row.Cells[colStatus.Index].Value = on ? "Bật" : "Tắt";
            row.Cells[colAction.Index].Value = on ? "Tắt" : "Bật";
        }

        private void OnRealTimeStatus(bool running)
        {
            try
            {
                BeginInvoke((MethodInvoker)(() => { SyncRealFeaturesUi(); SyncProtectionStats(); }));
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        // Kỹ thuật 3 phát hiện đe dọa: hỏi người dùng, hoặc cách ly thẳng nếu "Tự động cách ly" đang bật
        private void OnThreatDetected(ThreatFound threat)
        {
            try
            {
                BeginInvoke((MethodInvoker)(() =>
                {
                    if (RealTimeProtection.AutoQuarantine)
                    {
                        MessageBox.Show(
                            "Bảo vệ thời gian thực đã cách ly:\n" + threat.FilePath
                            + "\n\nPhát hiện [" + threat.Kind + "]: " + threat.Reason,
                            "Mối đe dọa bị chặn",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    var answer = MessageBox.Show(
                        threat.FilePath + "\n\nPhát hiện [" + threat.Kind + "]: " + threat.Reason
                        + "\n\nCách ly tệp này ngay?",
                        "Bảo vệ thời gian thực: mối đe dọa mới",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer == DialogResult.Yes && ScanEngine.Quarantine(threat.FilePath))
                        MessageBox.Show("Đã chuyển tệp vào khu cách ly.", "Cách ly",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void chkAutoStart_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
