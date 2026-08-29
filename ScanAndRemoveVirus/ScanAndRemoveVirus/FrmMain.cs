using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;

namespace ScanAndRemoveVirus
{
    public partial class FrmMain : Form
    {
        private readonly UcTongQuan ucTongQuan = new UcTongQuan();
        private readonly UcBaoVe ucBaoVe = new UcBaoVe();
        private readonly UcLichSu ucLichSu = new UcLichSu();
        private readonly UcCachLy ucCachLy = new UcCachLy();

        private void LoadContent(UserControl control)
        {
            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            control.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(control);
            pnlContent.ResumeLayout();
        }
        private void ResetSidebar()
        {
            Button[] buttons =
            {
                btnTongQuan,
                btnBaoVe,
                btnLichSu,
                btnCachLy
            };
            foreach(Button btn in buttons)
                Theme.StyleNav(btn, false);
        }
        private void ActiveSidebar(Button button)
        {
            ResetSidebar();
            Theme.StyleNav(button, true);
        }
        public FrmMain()
        {
            InitializeComponent();
            Theme.StyleNav(btnCaiDat, false); // nút Cài đặt ngoài danh sách điều hướng 4 tab

            LoadContent(ucTongQuan);
            ActiveSidebar(btnTongQuan);
            btnTongQuan.Click += btnTongQuan_Click;
            btnBaoVe.Click += btnBaoVe_Click;
            btnLichSu.Click += btnLichSu_Click;
            btnCachLy.Click += btnCachLy_Click;
            // Vùng Cài đặt (checkbox + nút Lưu) nằm trong tab Bảo vệ -> nút Cài đặt mở sang đó
            btnCaiDat.Click += (s, ev) => { LoadContent(ucBaoVe); ActiveSidebar(btnBaoVe); };

        }

        //API cho các UserControl điều hướng (vd: bấm số đếm cách ly ở tab Tổng quan)
        public void MoTabCachLy()
        {
            LoadContent(ucCachLy);
            ActiveSidebar(btnCachLy);
        }
        

        private void btnTongQuan_Click(object sender, EventArgs e)
        {
            LoadContent(ucTongQuan);
            ActiveSidebar(btnTongQuan);

        }

        private void btnBaoVe_Click(object sender, EventArgs e)
        {
            LoadContent(ucBaoVe);
            ActiveSidebar(btnBaoVe);
        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnLichSu_Click(object sender, EventArgs e)
        {
            LoadContent(ucLichSu);
            ActiveSidebar(btnLichSu);
        }

        private void btnCachLy_Click(object sender, EventArgs e)
        {
            LoadContent(ucCachLy);
            ActiveSidebar(btnCachLy);
        }
    }

}
