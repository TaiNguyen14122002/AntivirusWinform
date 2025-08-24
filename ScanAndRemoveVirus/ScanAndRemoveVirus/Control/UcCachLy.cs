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
    public partial class UcCachLy : UserControl
    {
        public UcCachLy()
        {
            InitializeComponent();
            LoadSampleQuarantineData();
        }
        private void LoadSampleQuarantineData()
        {
            dgvQuarantine.Rows.Clear();

            dgvQuarantine.Rows.Add(
                "eicar.com",
                @"C:\Users\Admin\Downloads\eicar.com",
                "EICAR-Test-File (Not a Virus)",
                "20/08/2025 08:25:10",
                "68 B");

            dgvQuarantine.Rows.Add(
                "setup_fake.exe",
                @"D:\Setup\setup_fake.exe",
                "Trojan.GenericKD.123456",
                "19/08/2025 22:10:33",
                "2.45 MB");

            dgvQuarantine.Rows.Add(
                "keygen.zip",
                @"C:\Users\Admin\Desktop\keygen.zip",
                "HackTool.Keygen.8910",
                "18/08/2025 09:15:27",
                "1.12 MB");

            dgvQuarantine.Rows.Add(
                "Invoice.scr",
                @"C:\Users\Admin\Downloads\Invoice.scr",
                "Adware.InstallCore.2345",
                "16/08/2025 14:05:02",
                "512 KB");

            lblTotalFilesValue.Text = dgvQuarantine.Rows.Count.ToString();
        }

        private void tableLayoutPanel9_Paint(object sender, PaintEventArgs e)
        {

        }

        private void grpQuarentineList_Enter(object sender, EventArgs e)
        {

        }
    }
}
