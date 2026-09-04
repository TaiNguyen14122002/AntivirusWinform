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
        // 10 tính năng — mỗi hàng có key điều khiển cơ chế THẬT tương ứng (row.Tag = key)
        private static readonly string[,] Catalog = {
            { "rt",       "Bảo vệ thời gian thực",       "Tự động giám sát và quét tệp mới/đổi trong Desktop, Downloads, Temp" },
            { "restore",  "Bảo vệ tệp",                  "Kiểm tra lại tệp cách ly trước khi khôi phục về máy" },
            { "usb",      "Bảo vệ USB",                  "Tự động quét toàn bộ ổ removable ngay khi được cắm vào" },
            { "download", "Bảo vệ tải xuống",            "Quét đầy đủ nội dung tệp có nguồn gốc Internet (MOTW) về thư mục Downloads" },
            { "behavior", "Phát hiện hành vi đáng ngờ",  "Giám sát tiến trình qua WMI: chạy từ Temp, PowerShell mã hóa, Office sinh shell" },
            { "startup",  "Bảo vệ thư mục khởi động",    "Quét tệp mới xuất hiện trong StartUp — điểm cài persistence của malware" },
            { "aq",       "Tự động cách ly",             "Tự động đưa tệp nguy hiểm vào khu vực cách ly, không hỏi" },
            { "alerts",   "Cảnh báo mối đe dọa",         "Hiển thị pop-up khi phát hiện; tắt thì chỉ ghi lịch sử" },
            { "update",   "Tự động cập nhật",            "Mở app khi chữ ký quá 24h: đóng dấu cập nhật + xóa cache để quét lại" },
            { "vtq",      "Bảo vệ web (VirusTotal)",     "Tự tra VirusTotal theo hash cho dòng heuristic nghi vấn (cần API key)" },
        };

        private void LoadDuLieuBaoVe()
        {
            dgvProtecctionFeatures.Rows.Clear();
            for (int i = 0; i < Catalog.GetLength(0); i++)
            {
                int r = dgvProtecctionFeatures.Rows.Add(Catalog[i, 1], Catalog[i, 2], "—", "—");
                dgvProtecctionFeatures.Rows[r].Tag = Catalog[i, 0];
            }
        }

        private static string NameForKey(string key)
        {
            for (int i = 0; i < Catalog.GetLength(0); i++)
                if (Catalog[i, 0] == key) return Catalog[i, 1];
            return key;
        }

        // Trạng thái HIỆN HÀNH của từng tính năng — một nguồn sự thật duy nhất
        private static bool IsOn(string key)
        {
            switch (key)
            {
                case "rt": return RealTimeProtection.IsRunning;
                case "aq": return RealTimeProtection.AutoQuarantine;
                case "restore": return FeatureFlags.FileRestoreGuard;
                case "usb": return FeatureFlags.UsbProtection;
                case "download": return FeatureFlags.DownloadProtection;
                case "behavior": return FeatureFlags.BehaviorWatch;
                case "startup": return FeatureFlags.StartupFoldersWatch;
                case "alerts": return FeatureFlags.ThreatAlerts;
                case "update": return FeatureFlags.AutoUpdateEnabled;
                case "vtq": return FeatureFlags.VtAutoQuery;
                default: return false;
            }
        }

        private static void SetState(string key, bool on)
        {
            switch (key)
            {
                case "rt":
                    try
                    {
                        if (on && !RealTimeProtection.IsRunning) RealTimeProtection.Start();
                        if (!on && RealTimeProtection.IsRunning) RealTimeProtection.Stop();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không bật được bảo vệ thời gian thực: " + ex.Message,
                            "Bảo vệ thời gian thực", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    FeatureFlags.RealTimeOn = RealTimeProtection.IsRunning;
                    break;
                case "aq":
                    RealTimeProtection.AutoQuarantine = on;
                    break;
                case "restore":  FeatureFlags.FileRestoreGuard = on; break;
                case "usb":      FeatureFlags.UsbProtection = on; GuardService.Configure("usb", on); break;
                case "download": FeatureFlags.DownloadProtection = on; GuardService.Configure("download", on); break;
                case "behavior": FeatureFlags.BehaviorWatch = on; GuardService.Configure("behavior", on); break;
                case "startup":  FeatureFlags.StartupFoldersWatch = on; GuardService.Configure("startup", on); break;
                case "alerts":   FeatureFlags.ThreatAlerts = on; break;
                case "update":   FeatureFlags.AutoUpdateEnabled = on; break;
                case "vtq":      FeatureFlags.VtAutoQuery = on; break;
            }
            FeatureFlags.Persist();
            FeatureFlags.NotifyChanged();
        }
        public UcBaoVe()
        {
            InitializeComponent();
            BackColor = Theme.PageBg;
            // Responsive: cửa sổ nhỏ -> cuộn thay vì cắt nội dung
            Theme.ScrollablePage(this, tableLayoutPanel1, 980, 640);
            Theme.StylePageHeader(lblProtectionTitle, lblProtectionSubtitle);
            Theme.StyleCard(grpProtectionFeatures, grpProtectionInfo);
            // StyleGrid phải chạy TRƯỚC khi nạp hàng để mọi dòng theo đúng RowTemplate chung
            Theme.StyleGrid(dgvProtecctionFeatures);
            LoadDuLieuBaoVe();
            dgvProtecctionFeatures.CellClick += Grid_CellClick;
            dgvProtecctionFeatures.CellFormatting += Grid_CellFormatting;
            RealTimeProtection.StatusChanged += OnRealTimeStatus;
            RealTimeProtection.ThreatDetected += OnThreatDetected;
            GuardService.ThreatDetected += OnThreatDetected;
            FeatureFlags.Changed += OnFlagsChanged;
            SyncRealFeaturesUi();
            SyncProtectionStats();
        }

        private void OnFlagsChanged()
        {
            try { BeginInvoke((MethodInvoker)(() => { SyncRealFeaturesUi(); SyncProtectionStats(); })); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
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
            if (Visible) { SyncProtectionStats(); SyncRealFeaturesUi(); }
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
            DataGridViewRow row = dgvProtecctionFeatures.Rows[e.RowIndex];
            string key = row.Tag as string;
            if (string.IsNullOrEmpty(key)) return;
            SetState(key, !IsOn(key));
            SyncRealFeaturesUi();
        }

        // Đồng bộ ô Trạng thái/Hành động của MỌI hàng theo đúng state trong Services
        private void SyncRealFeaturesUi()
        {
            foreach (DataGridViewRow row in dgvProtecctionFeatures.Rows)
            {
                string key = row.Tag as string;
                if (string.IsNullOrEmpty(key)) continue;
                SetFeatureState(row, IsOn(key));
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

        // Kỹ thuật 3 phát hiện đe dọa: pop-up xác nhận cách ly (bỏ qua nếu tắt "Cảnh báo")
        private void OnThreatDetected(ThreatFound threat)
        {
            try
            {
                BeginInvoke((MethodInvoker)(() =>
                {
                    if (!FeatureFlags.ThreatAlerts) return; // chế độ im lặng — lịch sử vẫn ghi ở tầng service
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
    }
}
