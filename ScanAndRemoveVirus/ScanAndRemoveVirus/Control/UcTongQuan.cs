using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcTongQuan : UserControl
    {
        private string customScanPath = string.Empty;
        private bool isChangingScanType = false;

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

            // Nút chọn thư mục
            btnChooseCustomPath.Click += btnChooseCustomPath_Click;

            // Theo dõi kiểu quét
            rdoQuickScan.CheckedChanged += ScanType_CheckedChanged;
            rdoFullScan.CheckedChanged += ScanType_CheckedChanged;
            rdoCustomScan.CheckedChanged += ScanType_CheckedChanged;

            // Mặc định Quét nhanh
            rdoQuickScan.Checked = true;

            UpdateCustomScanControls();
        }

        private void lblQuarantineCount_Click(object sender, EventArgs e)
        {

        }

        // =========================================================
        // THAY ĐỔI KIỂU QUÉT
        // =========================================================

        private void ScanType_CheckedChanged(object sender, EventArgs e)
        {
            if (isChangingScanType)
                return;

            RadioButton selectedRadio = sender as RadioButton;

            if (selectedRadio == null || !selectedRadio.Checked)
                return;

            isChangingScanType = true;

            try
            {
                if (selectedRadio == rdoQuickScan)
                {
                    rdoFullScan.Checked = false;
                    rdoCustomScan.Checked = false;
                }
                else if (selectedRadio == rdoFullScan)
                {
                    rdoQuickScan.Checked = false;
                    rdoCustomScan.Checked = false;
                }
                else if (selectedRadio == rdoCustomScan)
                {
                    rdoQuickScan.Checked = false;
                    rdoFullScan.Checked = false;
                }
            }
            finally
            {
                isChangingScanType = false;
            }

            UpdateCustomScanControls();
        }

        // =========================================================
        // BẬT / TẮT PHẦN "TÙY CHỌN QUÉT"
        // =========================================================

        private void UpdateCustomScanControls()
        {
            bool enableCustomScan = rdoCustomScan.Checked;

            grpCustomInfo.Enabled = enableCustomScan;

            if (enableCustomScan)
            {
                if (string.IsNullOrWhiteSpace(customScanPath))
                {
                    txtCustomPath.Text = "Chưa chọn vị trí";

                    lblCustomPathHint.Text =
                        "Chọn thư mục hoặc ổ đĩa cần quét.";
                }
                else
                {
                    txtCustomPath.Text = customScanPath;

                    lblCustomPathHint.Text =
                        "Đã chọn vị trí để quét.";
                }
            }
            else
            {
                lblCustomPathHint.Text =
                    "Chọn Quét tùy chọn để chọn vị trí.";
            }
        }

        // =========================================================
        // CHỌN THƯ MỤC / Ổ ĐĨA
        // =========================================================

        private void btnChooseCustomPath_Click(object sender, EventArgs e)
        {
            // Chỉ cho phép thao tác khi chọn Quét tùy chọn
            if (!rdoCustomScan.Checked)
            {
                MessageBox.Show(
                    "Vui lòng chọn \"Quét tùy chọn\" trước.",
                    "Tùy chọn quét",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description =
                    "Chọn thư mục hoặc ổ đĩa cần quét.";

                dialog.ShowNewFolderButton = false;

                // Nếu trước đó đã chọn vị trí thì mở lại tại vị trí cũ
                if (!string.IsNullOrWhiteSpace(customScanPath) &&
                    Directory.Exists(customScanPath))
                {
                    dialog.SelectedPath = customScanPath;
                }

                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                // Lưu đường dẫn
                customScanPath = dialog.SelectedPath;

                // Hiển thị đường dẫn
                txtCustomPath.Text = customScanPath;

                // Cập nhật hướng dẫn
                lblCustomPathHint.Text =
                    "Đã chọn vị trí để quét.";

                // Đưa con trỏ về cuối TextBox
                txtCustomPath.SelectionStart =
                    txtCustomPath.Text.Length;

                txtCustomPath.ScrollToCaret();
            }
        }
    }
}