using ScanAndRemoveVirus.form;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;

namespace ScanAndRemoveVirus
{
    public partial class FrmMain : Form
    {
        private readonly UcTongQuan ucTongQuan = new UcTongQuan();
        private readonly UcBaoVe ucBaoVe = new UcBaoVe();
        private readonly UcLichSu ucLichSu = new UcLichSu();

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
                btnLichSu
            };
            foreach(Button btn in buttons)
            {
                btn.BackColor = Color.FromArgb(247, 248, 250);
                btn.ForeColor = Color.FromArgb(37, 99, 235);
                btn.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            }
        }
        private void ActiveSidebar(Button button)
        {
            ResetSidebar();

            button.BackColor = Color.FromArgb(10, 86, 216);
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        }
        public FrmMain()
        {
            InitializeComponent();

            LoadContent(ucTongQuan);
            ActiveSidebar(btnTongQuan);
            btnTongQuan.Click += btnTongQuan_Click;
            btnBaoVe.Click += btnBaoVe_Click;
            btnLichSu.Click += btnLichSu_Click;

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
    }

}
