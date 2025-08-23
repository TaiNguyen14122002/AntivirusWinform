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
    public partial class UcTongQuan : UserControl
    {
        private void LoadDuLieuHoatDongGanDay()
        {
            dgvRecentActivity.Rows.Clear();

            dgvRecentActivity.Rows.Add(
                "20/08/2025 07:45",
                "Quét nhanh",
                "Đã quét 125.430 tệp, không phát hiện mối đe dọa"
            );

            dgvRecentActivity.Rows.Add(
                "20/08/2025 07:32",
                "Cập nhật cơ sở dữ liệu",
                "Cơ sở dữ liệu virus đã được cập nhật thành công"
            );

            dgvRecentActivity.Rows.Add(
                "19/08/2025 18:20",
                "Phát hiện mối đe dọa",
                @"Phát hiện Trojan.Win32.Generic tại C:\Downloads\setup.exe"
            );

            dgvRecentActivity.Rows.Add(
                "19/08/2025 18:21",
                "Cách ly tệp",
                @"Đã cách ly C:\Downloads\setup.exe để ngăn chặn nguy cơ"
            );

            dgvRecentActivity.Rows.Add(
                "19/08/2025 15:10",
                "Quét tùy chọn",
                @"Đã quét thư mục C:\Users\TaiNguyen\Downloads, không phát hiện mối đe dọa"
            );

            dgvRecentActivity.Rows.Add(
                "18/08/2025 09:15",
                "Bảo vệ thời gian thực",
                "Bảo vệ thời gian thực đã được bật"
            );

            dgvRecentActivity.Rows.Add(
                "17/08/2025 21:40",
                "Phát hiện mối đe dọa",
                "Phát hiện Malware.Generic trong một tệp tải xuống"
            );

            dgvRecentActivity.Rows.Add(
                "17/08/2025 21:41",
                "Cách ly tệp",
                "Đã cách ly tệp nguy hiểm thành công"
            );

            dgvRecentActivity.Rows.Add(
                "16/08/2025 08:05",
                "Quét toàn bộ",
                "Đã quét toàn bộ ổ đĩa, không phát hiện mối đe dọa"
            );

            dgvRecentActivity.Rows.Add(
                "15/08/2025 14:30",
                "Cập nhật cơ sở dữ liệu",
                "Cập nhật cơ sở dữ liệu virus thành công"
            );
        }
        public UcTongQuan()
        {
            InitializeComponent();
            LoadDuLieuHoatDongGanDay();
        }

        private void lblQuarantineCount_Click(object sender, EventArgs e)
        {

        }
    }
}
