using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>Trung tâm tệp chưa kiểm tra được, không dùng bảng SQL mới.</summary>
    public sealed class ScanIssuesForm : Form
    {
        readonly DataGridView grid = new DataGridView();
        readonly Label status = new Label();
        readonly Button retry = new Button();
        public ScanIssuesForm()
        {
            Text = "Tệp chưa kiểm tra / sự kiện bảo vệ";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(780, 500);
            Size = new Size(1060, 660);
            Font = new Font(Theme.UiFontFamily, 10.65F);
            var header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(12) };
            var reload = new Button { Text = "Làm mới", Width = 115, Height = 36 };
            retry.Text = "Quét lại tệp đã chọn"; retry.Width = 180; retry.Height = 36;
            var clear = new Button { Text = "Xóa nhật ký cục bộ", Width = 170, Height = 36 };
            reload.Click += (s, e) => Reload();
            retry.Click += async (s, e) => await RetryAsync();
            clear.Click += (s, e) =>
            {
                if (MessageBox.Show(this, "Chỉ xóa nhật ký lỗi cục bộ, không xóa tệp hoặc dữ liệu SQL. Tiếp tục?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                { LocalScanIssueStore.Clear(); Reload(); }
            };
            header.Controls.Add(reload); header.Controls.Add(retry); header.Controls.Add(clear);
            status.Dock = DockStyle.Bottom; status.Height = 55; status.Padding = new Padding(14);
            status.Text = "Tệp không thể kiểm tra không được coi là an toàn.";
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            Controls.Add(grid); Controls.Add(status); Controls.Add(header);
            Shown += (s, e) => Reload();
        }
        private void Reload()
        {
            var items = LocalScanIssueStore.Recent();
            grid.DataSource = items;
            status.Text = items.Count + " sự kiện gần nhất. Đường dẫn ZIP > entry chỉ để hiển thị; hãy quét lại tệp nén gốc.";
        }
        private async Task RetryAsync()
        {
            if (grid.CurrentRow == null) return;
            var issue = grid.CurrentRow.DataBoundItem as LocalScanIssueStore.Issue;
            if (issue == null) return;
            string path = issue.Path ?? "";
            int boundary = path.IndexOf(" > ", StringComparison.Ordinal);
            if (boundary >= 0) path = path.Substring(0, boundary);
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                MessageBox.Show(this, "Không còn tìm thấy tệp/thư mục này.", "Quét lại"); return;
            }
            retry.Enabled = false;
            status.Text = "Đang quét lại, vui lòng không đóng cửa sổ...";
            try
            {
                var result = await Task.Run(() => ScanEngine.Scan(ScanType.Custom, path, CancellationToken.None));
                foreach (var item in result.Issues) LocalScanIssueStore.Record(item.Path, "Retry", item.Reason);
                status.Text = "Đã thử: " + result.FilesScanned + " tệp; phát hiện: " + result.Threats.Count +
                    "; chưa kiểm tra: " + result.Issues.Count + ". Kết quả không đồng nghĩa tệp sạch tuyệt đối.";
                if (result.Threats.Count > 0) MessageBox.Show(this, "Phát hiện " + result.Threats.Count +
                    " dấu hiệu/mối đe dọa. Hãy kiểm tra chi tiết trước khi xử lý.", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Reload();
            }
            catch (Exception ex) { status.Text = "Quét lại lỗi: " + ex.Message; }
            finally { if (!IsDisposed) retry.Enabled = true; }
        }
    }
}
