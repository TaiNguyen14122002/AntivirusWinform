using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public sealed class PeInspectForm : Form
    {
        readonly TextBox result = new TextBox();
        readonly Button choose = new Button();
        public PeInspectForm()
        {
            Text = "Phân tích EXE/DLL và chữ ký số";
            MinimumSize = new Size(650, 410); Size = new Size(840, 510);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font(Theme.UiFontFamily, 11.55F);
            choose.Text = "Chọn tệp EXE/DLL để phân tích";
            choose.Dock = DockStyle.Top; choose.Height = 52;
            choose.Click += async (s, e) => await ChooseAsync();
            result.Dock = DockStyle.Fill; result.Multiline = true; result.ReadOnly = true;
            result.ScrollBars = ScrollBars.Vertical; result.BorderStyle = BorderStyle.None;
            result.Text = "Không thực thi tệp. Chữ ký số hợp lệ không có nghĩa tệp không chứa mã độc.";
            Controls.Add(result); Controls.Add(choose);
        }
        private async Task ChooseAsync()
        {
            using (var picker = new OpenFileDialog { Filter = "Windows executables (*.exe;*.dll;*.sys)|*.exe;*.dll;*.sys|Tất cả tệp (*.*)|*.*" })
            {
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                choose.Enabled = false;
                result.Text = "Đang phân tích metadata và xác minh chữ ký số...";
                try
                {
                    var item = await Task.Run(() => PortableExecutableInspector.Inspect(picker.FileName));
                    if (IsDisposed) return;
                    var text = new StringBuilder();
                    text.AppendLine("Tệp: " + picker.FileName);
                    text.AppendLine("PE hợp lệ: " + (item.IsPe ? "Có" : "Không / không đọc được header"));
                    text.AppendLine("Kiến trúc: " + item.Machine);
                    text.AppendLine("Số section: " + item.SectionCount);
                    text.AppendLine("Có chứng chỉ đi kèm: " + (item.IsSigned ? "Có" : "Chưa xác định"));
                    text.AppendLine("Nhà phát hành khai báo: " + (item.Signer ?? "Không có"));
                    text.AppendLine("Authenticode: " + (item.SignatureResult ?? "Không xác định"));
                    text.AppendLine("Kết quả: " + (item.SignatureTrusted ? "Chữ ký được Windows chấp nhận" : "Chưa xác minh được chữ ký"));
                    text.AppendLine().AppendLine(item.Note);
                    result.Text = text.ToString();
                }
                catch (Exception ex) { if (!IsDisposed) result.Text = "Lỗi phân tích: " + ex.Message; }
                finally { if (!IsDisposed) choose.Enabled = true; }
            }
        }
    }
}
