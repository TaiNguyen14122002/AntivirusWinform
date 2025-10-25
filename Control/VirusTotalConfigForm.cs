using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>Cấu hình API key trên máy này, lưu Windows DPAPI, không lưu SQL hoặc repo.</summary>
    public sealed class VirusTotalConfigForm : Form
    {
        private readonly TextBox keyInput;
        private readonly Label status;
        private readonly Button testButton;
        public VirusTotalConfigForm()
        {
            Text = "Cấu hình VirusTotal API";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(550, 298);
            Font = new Font(Theme.UiFontFamily, 10.65f);
            BackColor = Color.White;
            var headline = new Label { Left = 23, Top = 18, Width = 505, Height = 32,
                Text = "Kết nối VirusTotal API v3", Font = new Font(Theme.UiFontFamily, 15.4f, FontStyle.Bold) };
            var hint = new Label { Left = 24, Top = 57, Width = 503, Height = 45,
                Text = "Khóa API được mã hóa bằng Windows DPAPI theo tài khoản hiện tại. Không ghi vào mã nguồn hoặc SQL." };
            keyInput = new TextBox { Left = 25, Top = 112, Width = 498, Height = 27, UseSystemPasswordChar = true };
            status = new Label { Left = 25, Top = 153, Width = 498, Height = 45, Text = VirusTotalClient.IsConfigured
                ? "Đã lưu API key trên máy này. Bấm Kiểm tra để xác nhận kết nối." : "Chưa lưu API key." };
            var save = NewButton("Lưu API key", 25, 211, 114);
            testButton = NewButton("Kiểm tra", 147, 211, 110);
            var clear = NewButton("Gỡ API key", 265, 211, 110);
            var close = NewButton("Đóng", 383, 211, 140);
            save.Click += delegate
            {
                try
                {
                    VirusTotalClient.SaveApiKey(keyInput.Text);
                    keyInput.Clear(); status.Text = "Đã mã hóa và lưu khóa theo tài khoản Windows hiện tại.";
                }
                catch (Exception ex) { status.Text = "Lưu thất bại: " + ex.Message; }
            };
            testButton.Click += async delegate
            {
                testButton.Enabled = false;
                status.Text = "Đang kiểm tra kết nối API...";
                try { status.Text = await Task.Run(() => VirusTotalClient.TestApiKey()); }
                catch (Exception ex) { status.Text = "Không thể kiểm tra: " + ex.Message; }
                finally { if (!IsDisposed) testButton.Enabled = true; }
            };
            clear.Click += delegate
            {
                try { VirusTotalClient.ClearApiKey(); keyInput.Clear(); status.Text = "Đã gỡ khóa API lưu trên máy."; }
                catch (Exception ex) { status.Text = "Không gỡ được khóa: " + ex.Message; }
            };
            close.Click += delegate { Close(); };
            Controls.AddRange(new System.Windows.Forms.Control[] { headline, hint, keyInput, status, save, testButton, clear, close });
        }
        private static Button NewButton(string text, int left, int top, int width)
        { return new Button { Text = text, Left = left, Top = top, Width = width, Height = 38, FlatStyle = FlatStyle.System }; }
    }
}
