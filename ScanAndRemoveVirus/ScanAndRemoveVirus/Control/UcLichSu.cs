
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
    }
}
