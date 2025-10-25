using System;
using System.Drawing;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public sealed class SignatureManagementForm : Form
    {
        private readonly TextBox search = new TextBox();
        private readonly ComboBox state = new ComboBox();
        private readonly DataGridView grid = new DataGridView();
        private readonly Label status = new Label();

        public SignatureManagementForm()
        {
            Text = "Quản lý chữ ký mã độc";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1120, 730); MinimumSize = new Size(840, 570);
            Font = new Font(Theme.UiFontFamily, 11.55F);
            BackColor = Color.FromArgb(247, 250, 254);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(18) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            Controls.Add(root);
            var header = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            search.Width = 410; search.Font = Font;
            search.PlaceholderTextCompat("Tìm tên mã độc, họ mã độc, SHA256");
            state.DropDownStyle = ComboBoxStyle.DropDownList;
            state.Width = 190;
            state.Items.AddRange(new object[] { "All", "PendingReview", "Verified", "Legacy", "Rejected", "Disabled" });
            state.SelectedIndex = 0;
            var reload = Button("Tìm và tải lại");
            reload.Click += (s, e) => Reload();
            header.Controls.Add(search); header.Controls.Add(state); header.Controls.Add(reload);
            root.Controls.Add(header, 0, 0);
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowTemplate.Height = 40;
            grid.BackgroundColor = Color.White;
            root.Controls.Add(grid, 0, 1);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            foreach (var pair in new[] { new[] { "Duyệt và kích hoạt", "Approve" }, new[] { "Từ chối", "Reject" },
                new[] { "Vô hiệu hóa", "Disable" }, new[] { "Chờ xác minh", "PendingReview" } })
            {
                string choice = pair[1];
                var button = Button(pair[0]);
                button.Click += (s, e) => Change(choice);
                actions.Controls.Add(button);
            }
            var audit = Button("Lịch sử thay đổi");
            audit.Click += (s, e) => ShowAudit();
            actions.Controls.Add(audit);
            root.Controls.Add(actions, 0, 2);
            status.Dock = DockStyle.Fill; status.ForeColor = Color.FromArgb(90, 110, 135);
            status.Text = "Mẫu chỉ tham gia phát hiện khi IsActive = 1. Mọi quyết định đều ghi nhật ký SQL.";
            root.Controls.Add(status, 0, 3);
            Shown += (s, e) => Reload();
        }
        private static Button Button(string title)
        {
            return new Button { Text = title, AutoSize = true, MinimumSize = new Size(125, 42),
                FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = new Font(Theme.UiFontFamily, 10.65F, FontStyle.Bold) };
        }
        private long CurrentId()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Cells["SignatureID"].Value == null)
                throw new InvalidOperationException("Hãy chọn một chữ ký.");
            return Convert.ToInt64(grid.CurrentRow.Cells["SignatureID"].Value);
        }
        private void Reload()
        {
            try { grid.DataSource = SignatureVerificationService.Search(search.Text, Convert.ToString(state.SelectedItem));
                status.Text = "Đang hiển thị tối đa 300 chữ ký. Có thể tìm theo mã SHA-256."; }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không tải được dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void Change(string action)
        {
            try
            {
                long id = CurrentId();
                string reason = Microsoft.VisualBasic.Interaction.InputBox("Nhập lý do và bằng chứng xác minh cho thao tác " + action,
                    "Lý do thay đổi", "");
                if (string.IsNullOrWhiteSpace(reason)) return;
                if (MessageBox.Show(this, "Xác nhận " + action + " chữ ký " + id + "?", "Quản lý chữ ký",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                SignatureVerificationService.ChangeStatus(id, action, reason);
                Reload();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thể thay đổi", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void ShowAudit()
        {
            try
            {
                long id = CurrentId();
                using (var dialog = new Form { Text = "Lịch sử chữ ký #" + id, Size = new Size(900, 540), StartPosition = FormStartPosition.CenterParent, Font = Theme.DefaultUiFont })
                {
                    var log = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, DataSource = SignatureVerificationService.Audit(id) };
                    dialog.Controls.Add(log); dialog.ShowDialog(this);
                }
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không mở được nhật ký"); }
        }
    }
    internal static class TextBoxCompatibility
    {
        // .NET Framework 4.7.2 chưa hỗ trợ PlaceholderText của WinForms mới.
        public static void PlaceholderTextCompat(this TextBox box, string text)
        {
            box.AccessibleDescription = text;
        }
    }
}
