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
    public partial class UcBaoVe : UserControl
    {
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
            LoadDuLieuBaoVe();
        }

        private void chkAutoStart_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
