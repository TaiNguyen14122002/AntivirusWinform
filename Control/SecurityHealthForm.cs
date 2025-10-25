using System;
using System.Drawing;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public sealed class SecurityHealthForm : Form
    {
        readonly DataGridView grid = new DataGridView();
        readonly Button reload = new Button();
        readonly Label status = new Label();
        public SecurityHealthForm()
        {
            Text = "Kiểm tra tình trạng bảo mật Windows";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1020, 680);
            MinimumSize = new Size(750, 520);
            Font = new Font(Theme.UiFontFamily, 11.55F);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(18) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            Controls.Add(layout);
            var header = new FlowLayoutPanel { Dock = DockStyle.Fill };
            reload.Text = "Kiểm tra lại"; reload.Width = 150; reload.Height = 42;
            reload.Click += async (s, e) => await ReloadAsync();
            status.Width = 680; status.Height = 50;
            status.Text = "Kết quả chỉ mang tính kiểm tra cấu hình, không chứng nhận máy không có mã độc.";
            status.TextAlign = ContentAlignment.MiddleLeft;
            header.Controls.Add(reload); header.Controls.Add(status);
            layout.Controls.Add(header, 0, 0);
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowTemplate.Height = 48;
            layout.Controls.Add(grid, 0, 1);
            var footer = new Label { Dock = DockStyle.Fill, Text = "Cần chú ý hoặc Không xác định đều yêu cầu kiểm tra bổ sung, không mặc định kết luận lỗi bảo mật.",
                TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(footer, 0, 2);
            Shown += async (s, e) => await ReloadAsync();
        }
        async System.Threading.Tasks.Task ReloadAsync()
        {
            reload.Enabled = false; status.Text = "Đang kiểm tra cấu hình Windows...";
            try
            {
                var data = await SecurityHealthCheckService.RunAsync();
                if (IsDisposed) return;
                grid.DataSource = data;
                status.Text = "Đã kiểm tra " + data.Count + " hạng mục. Nhấn Kiểm tra lại để cập nhật.";
                await System.Threading.Tasks.Task.Run(() => SecurityHealthCheckService.SaveResults(data));
            }
            catch (Exception ex) { if (!IsDisposed) status.Text = "Kiểm tra thất bại: " + ex.Message; }
            finally { if (!IsDisposed) reload.Enabled = true; }
        }
    }
}
