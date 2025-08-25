using ScanAndRemoveVirus.Modal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcLichSu : UserControl
    {
        private List<ThreatDetection> threatDetections = new List<ThreatDetection>();
        private List<UpdateHistoryItem> updateHistory = new List<UpdateHistoryItem>();
        private void InitializeThreatFilter()
        {
            cboTimeFilter.Items.Clear();
            cboThreatTypeFilter.Items.Clear();
            cboStatusFilter.Items.Clear();

            // Khoảng thời gian
            cboTimeFilter.Items.AddRange(new object[]
            {
        "7 ngày qua",
        "30 ngày qua",
        "90 ngày qua",
        "Tất cả"
            });

            // Loại mối đe dọa (Category)
            cboThreatTypeFilter.Items.Add("Tất cả");

            foreach (string category in threatDetections
                .Select(x => x.Category)
                .Distinct()
                .OrderBy(x => x))
            {
                cboThreatTypeFilter.Items.Add(category);
            }

            // Trạng thái
            cboStatusFilter.Items.Add("Tất cả");

            foreach (string status in threatDetections
                .Select(x => x.Status)
                .Distinct())
            {
                cboStatusFilter.Items.Add(status);
            }

            cboTimeFilter.SelectedItem = "7 ngày qua";
            cboThreatTypeFilter.SelectedItem = "Tất cả";
            cboStatusFilter.SelectedItem = "Tất cả";
        }
        private void InitializeUpdateFilter()
        {
            cboUpdateTimeFilter.Items.Clear();
            cboUpdateStatusFilter.Items.Clear();

            cboUpdateTimeFilter.Items.AddRange(new object[]
            {
        "7 ngày qua",
        "30 ngày qua",
        "90 ngày qua",
        "Tất cả"
            });

            cboUpdateStatusFilter.Items.Add("Tất cả");

            foreach (string status in updateHistory
                .Select(x => x.Status)
                .Distinct())
            {
                cboUpdateStatusFilter.Items.Add(status);
            }

            cboUpdateTimeFilter.SelectedItem = "7 ngày qua";
            cboUpdateStatusFilter.SelectedItem = "Tất cả";
        }
        private void LoadDuLieuLichSuQuet()
        {
            dgvHistory.Rows.Clear();

            dgvHistory.Rows.Add(
                "20/08/2025 07:45",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 15 giây"
            );

            dgvHistory.Rows.Add(
                "19/08/2025 18:30",
                "Quét tùy chọn",
                @"C:\Downloads",
                "Phát hiện mối đe dọa",
                "1",
                "01 phút 42 giây"
            );

            dgvHistory.Rows.Add(
                "19/08/2025 15:10",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "An toàn",
                "0",
                "28 phút 35 giây"
            );

            dgvHistory.Rows.Add(
                "18/08/2025 09:20",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 08 giây"
            );

            dgvHistory.Rows.Add(
                "17/08/2025 21:40",
                "Quét tùy chọn",
                @"D:\Games",
                "Phát hiện mối đe dọa",
                "2",
                "08 phút 21 giây"
            );

            dgvHistory.Rows.Add(
                "16/08/2025 08:05",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "An toàn",
                "0",
                "31 phút 12 giây"
            );

            dgvHistory.Rows.Add(
                "15/08/2025 14:30",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 03 giây"
            );

            dgvHistory.Rows.Add(
                "14/08/2025 19:15",
                "Quét tùy chọn",
                @"C:\Users",
                "An toàn",
                "0",
                "05 phút 47 giây"
            );

            dgvHistory.Rows.Add(
                "13/08/2025 10:25",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "Phát hiện mối đe dọa",
                "3",
                "32 phút 08 giây"
            );

            dgvHistory.Rows.Add(
                "12/08/2025 16:50",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 11 giây"
            );
            dgvHistory.Rows.Add(
                "20/08/2025 07:45",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 15 giây"
            );

            dgvHistory.Rows.Add(
                "19/08/2025 18:30",
                "Quét tùy chọn",
                @"C:\Downloads",
                "Phát hiện mối đe dọa",
                "1",
                "01 phút 42 giây"
            );

            dgvHistory.Rows.Add(
                "19/08/2025 15:10",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "An toàn",
                "0",
                "28 phút 35 giây"
            );

            dgvHistory.Rows.Add(
                "18/08/2025 09:20",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 08 giây"
            );

            dgvHistory.Rows.Add(
                "17/08/2025 21:40",
                "Quét tùy chọn",
                @"D:\Games",
                "Phát hiện mối đe dọa",
                "2",
                "08 phút 21 giây"
            );

            dgvHistory.Rows.Add(
                "16/08/2025 08:05",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "An toàn",
                "0",
                "31 phút 12 giây"
            );

            dgvHistory.Rows.Add(
                "15/08/2025 14:30",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 03 giây"
            );

            dgvHistory.Rows.Add(
                "14/08/2025 19:15",
                "Quét tùy chọn",
                @"C:\Users",
                "An toàn",
                "0",
                "05 phút 47 giây"
            );

            dgvHistory.Rows.Add(
                "13/08/2025 10:25",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "Phát hiện mối đe dọa",
                "3",
                "32 phút 08 giây"
            );

            dgvHistory.Rows.Add(
                "12/08/2025 16:50",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 11 giây"
            );
            dgvHistory.Rows.Add(
                "20/08/2025 07:45",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 15 giây"
            );

            dgvHistory.Rows.Add(
                "19/08/2025 18:30",
                "Quét tùy chọn",
                @"C:\Downloads",
                "Phát hiện mối đe dọa",
                "1",
                "01 phút 42 giây"
            );

            dgvHistory.Rows.Add(
                "19/08/2025 15:10",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "An toàn",
                "0",
                "28 phút 35 giây"
            );

            dgvHistory.Rows.Add(
                "18/08/2025 09:20",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 08 giây"
            );

            dgvHistory.Rows.Add(
                "17/08/2025 21:40",
                "Quét tùy chọn",
                @"D:\Games",
                "Phát hiện mối đe dọa",
                "2",
                "08 phút 21 giây"
            );

            dgvHistory.Rows.Add(
                "16/08/2025 08:05",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "An toàn",
                "0",
                "31 phút 12 giây"
            );

            dgvHistory.Rows.Add(
                "15/08/2025 14:30",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 03 giây"
            );

            dgvHistory.Rows.Add(
                "14/08/2025 19:15",
                "Quét tùy chọn",
                @"C:\Users",
                "An toàn",
                "0",
                "05 phút 47 giây"
            );

            dgvHistory.Rows.Add(
                "13/08/2025 10:25",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "Phát hiện mối đe dọa",
                "3",
                "32 phút 08 giây"
            );

            dgvHistory.Rows.Add(
                "12/08/2025 16:50",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 11 giây"
            );
            dgvHistory.Rows.Add(
                "20/08/2025 07:45",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 15 giây"
            );

            dgvHistory.Rows.Add(
                "19/08/2025 18:30",
                "Quét tùy chọn",
                @"C:\Downloads",
                "Phát hiện mối đe dọa",
                "1",
                "01 phút 42 giây"
            );

            dgvHistory.Rows.Add(
                "19/08/2025 15:10",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "An toàn",
                "0",
                "28 phút 35 giây"
            );

            dgvHistory.Rows.Add(
                "18/08/2025 09:20",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 08 giây"
            );

            dgvHistory.Rows.Add(
                "17/08/2025 21:40",
                "Quét tùy chọn",
                @"D:\Games",
                "Phát hiện mối đe dọa",
                "2",
                "08 phút 21 giây"
            );

            dgvHistory.Rows.Add(
                "16/08/2025 08:05",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "An toàn",
                "0",
                "31 phút 12 giây"
            );

            dgvHistory.Rows.Add(
                "15/08/2025 14:30",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 03 giây"
            );

            dgvHistory.Rows.Add(
                "14/08/2025 19:15",
                "Quét tùy chọn",
                @"C:\Users",
                "An toàn",
                "0",
                "05 phút 47 giây"
            );

            dgvHistory.Rows.Add(
                "13/08/2025 10:25",
                "Quét toàn bộ",
                "Toàn bộ ổ đĩa",
                "Phát hiện mối đe dọa",
                "3",
                "32 phút 08 giây"
            );

            dgvHistory.Rows.Add(
                "12/08/2025 16:50",
                "Quét nhanh",
                "Khu vực hệ thống",
                "An toàn",
                "0",
                "02 phút 11 giây"
            );
        }
        private void LoadSampleThreatData()
        {
            threatDetections.Clear();

            threatDetections.Add(new ThreatDetection
            {
                DetectedTime = new DateTime(2025, 8, 20, 8, 25, 10),
                FileName = "eicar.com",
                ThreatName = "EICAR-Test-File",
                Category = "Test",
                OriginalPath = @"C:\Users\Admin\Downloads\eicar.com",
                Status = "Đã cách ly",
                FileSizeBytes = 68
            });

            threatDetections.Add(new ThreatDetection
            {
                DetectedTime = new DateTime(2025, 8, 19, 22, 10, 33),
                FileName = "setup_fake.exe",
                ThreatName = "Trojan.GenericKD.123456",
                Category = "Trojan",
                OriginalPath = @"D:\Setup\setup_fake.exe",
                Status = "Đã cách ly",
                FileSizeBytes = 2572288
            });

            threatDetections.Add(new ThreatDetection
            {
                DetectedTime = new DateTime(2025, 8, 18, 9, 15, 27),
                FileName = "keygen.zip",
                ThreatName = "HackTool.Keygen.8910",
                Category = "HackTool",
                OriginalPath = @"C:\Users\Admin\Desktop\keygen.zip",
                Status = "Đã cách ly",
                FileSizeBytes = 1174405
            });

            threatDetections.Add(new ThreatDetection
            {
                DetectedTime = new DateTime(2025, 8, 16, 14, 5, 2),
                FileName = "Invoice.scr",
                ThreatName = "Adware.InstallCore.2345",
                Category = "Adware",
                OriginalPath = @"C:\Users\Admin\Downloads\Invoice.scr",
                Status = "Đã cách ly",
                FileSizeBytes = 524288
            });

            DisplayThreatHistory(threatDetections);
        }
        private void LoadSampleUpdateHistory()
        {
            updateHistory.Clear();

            updateHistory.Add(new UpdateHistoryItem
            {
                UpdateID = 1,
                UpdateTime = new DateTime(2025, 8, 20, 8, 20, 45),
                DatabaseVersion = "VDB-2025.08.20.001",
                Size = "25.6 MB",
                UpdateSource = "Máy chủ chính",
                Status = "Thành công",
                Note = "Cập nhật tự động",
                UpdateMethod = "Tự động"
            });

            updateHistory.Add(new UpdateHistoryItem
            {
                UpdateID = 2,
                UpdateTime = new DateTime(2025, 8, 19, 21, 55, 12),
                DatabaseVersion = "VDB-2025.08.19.002",
                Size = "24.3 MB",
                UpdateSource = "Máy chủ chính",
                Status = "Thành công",
                Note = "Cập nhật tự động",
                UpdateMethod = "Tự động"
            });

            updateHistory.Add(new UpdateHistoryItem
            {
                UpdateID = 3,
                UpdateTime = new DateTime(2025, 8, 18, 8, 15, 33),
                DatabaseVersion = "VDB-2025.08.18.001",
                Size = "23.8 MB",
                UpdateSource = "Máy chủ dự phòng",
                Status = "Thành công",
                Note = "Cập nhật tự động",
                UpdateMethod = "Tự động"
            });

            updateHistory.Add(new UpdateHistoryItem
            {
                UpdateID = 4,
                UpdateTime = new DateTime(2025, 8, 17, 8, 10, 22),
                DatabaseVersion = "VDB-2025.08.17.001",
                Size = "24.1 MB",
                UpdateSource = "Máy chủ chính",
                Status = "Thành công",
                Note = "Cập nhật tự động",
                UpdateMethod = "Tự động"
            });

            updateHistory.Add(new UpdateHistoryItem
            {
                UpdateID = 5,
                UpdateTime = new DateTime(2025, 8, 16, 8, 5, 10),
                DatabaseVersion = "VDB-2025.08.16.001",
                Size = "23.5 MB",
                UpdateSource = "Máy chủ chính",
                Status = "Thành công",
                Note = "Cập nhật tự động",
                UpdateMethod = "Tự động"
            });

            updateHistory.Add(new UpdateHistoryItem
            {
                UpdateID = 6,
                UpdateTime = new DateTime(2025, 8, 15, 20, 30, 5),
                DatabaseVersion = "VDB-2025.08.15.002",
                Size = "22.9 MB",
                UpdateSource = "Máy chủ dự phòng",
                Status = "Thành công",
                Note = "Cập nhật thủ công",
                UpdateMethod = "Thủ công"
            });

            updateHistory.Add(new UpdateHistoryItem
            {
                UpdateID = 7,
                UpdateTime = new DateTime(2025, 8, 15, 8, 0, 0),
                DatabaseVersion = "VDB-2025.08.15.001",
                Size = "22.6 MB",
                UpdateSource = "Máy chủ chính",
                Status = "Thành công",
                Note = "Cập nhật tự động",
                UpdateMethod = "Tự động"
            });

            updateHistory.Add(new UpdateHistoryItem
            {
                UpdateID = 8,
                UpdateTime = new DateTime(2025, 8, 14, 19, 45, 32),
                DatabaseVersion = "VDB-2025.08.14.002",
                Size = "--",
                UpdateSource = "Máy chủ chính",
                Status = "Thất bại",
                Note = "Không thể kết nối máy chủ",
                UpdateMethod = "Tự động"
            });

            DisplayUpdateHistory(updateHistory);
        }
        private void DisplayUpdateHistory(List<UpdateHistoryItem> data)
        {
            dgvUpdateHistory.Rows.Clear();

            foreach (var item in data)
            {
                int row = dgvUpdateHistory.Rows.Add(
                    item.UpdateTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    item.DatabaseVersion,
                    item.Size,
                    item.UpdateSource,
                    item.Status,
                    item.Note
                );

                if (item.Status == "Thành công")
                    dgvUpdateHistory.Rows[row]
                        .Cells["colUpdateStatus"]
                        .Style.ForeColor = Color.Green;
                else
                    dgvUpdateHistory.Rows[row]
                        .Cells["colUpdateStatus"]
                        .Style.ForeColor = Color.Red;
            }

            UpdateUpdateInfo(data);
        }
        private void UpdateUpdateInfo(List<UpdateHistoryItem> data)
        {
            if (data.Count == 0)
                return;

            UpdateHistoryItem latest = data
                .OrderByDescending(x => x.UpdateTime)
                .First();

            lblCurrentVersionValue.Text = latest.DatabaseVersion;

            lblLastUpdateValue.Text =
                latest.UpdateTime.ToString("dd/MM/yyyy HH:mm:ss");

            lblLastUpdateValue.ForeColor = Color.Green;

            lblUpdateMethodValue.Text = latest.UpdateMethod;

            lblNextUpdateValue.Text =
                latest.UpdateTime.AddDays(1)
                .ToString("dd/MM/yyyy HH:mm:ss");
        }
        private void DisplayThreatHistory(List<ThreatDetection> data)
        {
            dgvThreatHistory.Rows.Clear();

            foreach (var item in data)
            {
                int row = dgvThreatHistory.Rows.Add(
                    item.DetectedTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    item.ThreatName,
                    item.Category,
                    item.OriginalPath,
                    item.Status,
                    "Xem chi tiết"
                );

                dgvThreatHistory.Rows[row]
                    .Cells["colStatus"]
                    .Style.ForeColor = Color.Green;
            }

            UpdateStatistics(data);
        }
        private void UpdateStatistics(List<ThreatDetection> data)
        {
            lblTotalThreatValue.Text = data.Count.ToString();

            lblQuarantinedValue.Text =
                data.Count(x => x.Status == "Đã cách ly").ToString();

            lblDeletedValue.Text =
                data.Count(x => x.Status == "Đã xóa").ToString();

            lblUnhandledValue.Text =
                data.Count(x => x.Status == "Chưa xử lý").ToString();
        }

        public UcLichSu()
        {
            InitializeComponent();
            LoadDuLieuLichSuQuet();
            LoadSampleThreatData();
            InitializeThreatFilter();
            btnApplyFilter.Click += btnApplyFilter_Click;
            LoadSampleUpdateHistory();
            InitializeUpdateFilter();
            btnApplyUpdateFilter.Click += btnApplyUpdateFilter_Click;
            btnCheckUpdateNow.Click += btnCheckUpdateNow_Click;
            dgvUpdateHistory.CellContentClick += dgvUpdateHistory_CellContentClick;
            dgvUpdateHistory.SelectionChanged += dgvUpdateHistory_SelectionChanged;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dgvHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btnViewDetail_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel9_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblTimeFilter_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel10_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
            IEnumerable<ThreatDetection> filtered = threatDetections;

            DateTime currentDate = new DateTime(2025, 8, 20, 23, 59, 59);

            switch (cboTimeFilter.Text)
            {
                case "7 ngày qua":
                    filtered = filtered.Where(x =>
                        x.DetectedTime >= currentDate.AddDays(-7));
                    break;

                case "30 ngày qua":
                    filtered = filtered.Where(x =>
                        x.DetectedTime >= currentDate.AddDays(-30));
                    break;

                case "90 ngày qua":
                    filtered = filtered.Where(x =>
                        x.DetectedTime >= currentDate.AddDays(-90));
                    break;
            }

            if (cboThreatTypeFilter.Text != "Tất cả")
            {
                filtered = filtered.Where(x =>
                    x.Category == cboThreatTypeFilter.Text);
            }

            if (cboStatusFilter.Text != "Tất cả")
            {
                filtered = filtered.Where(x =>
                    x.Status == cboStatusFilter.Text);
            }

            DisplayThreatHistory(filtered.ToList());

        }

        private void lblTotalThreatValue_Click(object sender, EventArgs e)
        {

        }

        private void grpUpdateInfo_Enter(object sender, EventArgs e)
        {

        }

        private void btnApplyUpdateFilter_Click(object sender, EventArgs e)
        {
            IEnumerable<UpdateHistoryItem> filtered = updateHistory;
            DateTime currentDate = new DateTime(2025, 8, 20, 23, 59, 59);
            switch (cboUpdateTimeFilter.Text)
            {
                case "7 ngày qua": filtered = filtered.Where(x => x.UpdateTime >= currentDate.AddDays(-7)); break;
                case "30 ngày qua": filtered = filtered.Where(x => x.UpdateTime >= currentDate.AddDays(-30)); break;
                case "90 ngày qua": filtered = filtered.Where(x => x.UpdateTime >= currentDate.AddDays(-90)); break;
            }
            if (cboUpdateStatusFilter.Text != "Tất cả")
                filtered = filtered.Where(x => x.Status == cboUpdateStatusFilter.Text);
            DisplayUpdateHistory(filtered.ToList());
        }

        private void btnCheckUpdateNow_Click(object sender, EventArgs e)
        {
            UpdateHistoryItem newest = new UpdateHistoryItem
            {
                UpdateID = updateHistory.Max(x => x.UpdateID) + 1,
                UpdateTime = DateTime.Now,
                DatabaseVersion = "VDB-" + DateTime.Now.ToString("yyyy.MM.dd") + ".003",
                Size = "26.1 MB",
                UpdateSource = "Máy chủ chính",
                Status = "Thành công",
                Note = "Kiểm tra cập nhật thủ công",
                UpdateMethod = "Thủ công"
            };
            updateHistory.Insert(0, newest);
            DisplayUpdateHistory(updateHistory);
            InitializeUpdateFilter();
            MessageBox.Show("Cơ sở dữ liệu virus đã được cập nhật thành công!", "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dgvUpdateHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvUpdateHistory.Columns[e.ColumnIndex].Name != "colUpdateDetail") return;
            MessageBox.Show("Phiên bản: " + dgvUpdateHistory.Rows[e.RowIndex].Cells["colDatabaseVersion"].Value +
            "Trạng thái: " + dgvUpdateHistory.Rows[e.RowIndex].Cells["colUpdateStatus"].Value +
            "Ghi chú: " + dgvUpdateHistory.Rows[e.RowIndex].Cells["colUpdateNote"].Value,
            "Chi tiết cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Bổ sung: khi chọn một dòng trong dgvUpdateHistory,
        // GroupBox "Thông tin" sẽ hiển thị đúng dữ liệu của dòng đó.
        private void dgvUpdateHistory_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvUpdateHistory == null ||
                dgvUpdateHistory.SelectedRows.Count == 0)
            {
                return;
            }

            DataGridViewRow row = dgvUpdateHistory.SelectedRows[0];

            // Kiểm tra dữ liệu cần thiết trước khi đọc.
            if (row.Cells["colDatabaseVersion"].Value == null ||
                row.Cells["colUpdateTime"].Value == null ||
                row.Cells["colUpdateStatus"].Value == null)
            {
                return;
            }

            string version = Convert.ToString(
                row.Cells["colDatabaseVersion"].Value);

            string updateTime = Convert.ToString(
                row.Cells["colUpdateTime"].Value);

            string status = Convert.ToString(
                row.Cells["colUpdateStatus"].Value);

            // lblUpdateMethodValue hiện có trên giao diện.
            // Theo dữ liệu mẫu hiện tại, Note chứa "Cập nhật tự động/thủ công".
            string methodOrNote = string.Empty;

            if (row.Cells["colUpdateNote"].Value != null)
            {
                methodOrNote = Convert.ToString(
                    row.Cells["colUpdateNote"].Value);
            }

            lblCurrentVersionValue.Text =
                string.IsNullOrWhiteSpace(version) ? "--" : version;

            lblLastUpdateValue.Text =
                string.IsNullOrWhiteSpace(updateTime) ? "--" : updateTime;

            lblUpdateMethodValue.Text =
                string.IsNullOrWhiteSpace(methodOrNote)
                    ? "--"
                    : methodOrNote;

            // Tính lần cập nhật tiếp theo dự kiến = 24 giờ sau lần cập nhật được chọn.
            DateTime parsedTime;

            if (DateTime.TryParse(updateTime, out parsedTime))
            {
                lblNextUpdateValue.Text =
                    parsedTime.AddDays(1).ToString("dd/MM/yyyy HH:mm:ss");
            }
            else
            {
                lblNextUpdateValue.Text = "--";
            }

            // Màu thời gian theo trạng thái của bản ghi được chọn.
            if (status == "Thành công")
            {
                lblLastUpdateValue.ForeColor = Color.Green;
            }
            else if (status == "Thất bại")
            {
                lblLastUpdateValue.ForeColor = Color.Red;
            }
            else
            {
                lblLastUpdateValue.ForeColor = Color.DarkOrange;
            }
        }

    }
}