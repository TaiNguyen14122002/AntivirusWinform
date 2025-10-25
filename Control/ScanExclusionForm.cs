using System;
using System.Drawing;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public sealed class ScanExclusionForm : Form
    {
        readonly DataGridView table = new DataGridView();
        readonly Label hint = new Label();
        public ScanExclusionForm()
        {
            Text = "Ngoại lệ có kiểm soát";
            Size = new Size(1000, 640); MinimumSize = new Size(740, 490);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font(Theme.UiFontFamily, 11.55F);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(18) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            Controls.Add(root);
            hint.Dock = DockStyle.Fill; hint.Text = "Ngoại lệ chỉ giảm cảnh báo heuristic cho đúng SHA-256 đã chọn, không bỏ qua mã độc khớp chữ ký. Tự hết hạn tối đa 90 ngày.";
            hint.ForeColor = Color.FromArgb(85, 99, 119);
            root.Controls.Add(hint, 0, 0);
            table.Dock = DockStyle.Fill; table.ReadOnly = true; table.AllowUserToAddRows = false;
            table.SelectionMode = DataGridViewSelectionMode.FullRowSelect; table.MultiSelect = false;
            table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            table.RowTemplate.Height = 38;
            root.Controls.Add(table, 0, 1);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var add = Btn("Thêm ngoại lệ theo tệp");
            var disable = Btn("Hủy ngoại lệ đã chọn");
            var reload = Btn("Làm mới");
            actions.Controls.Add(add); actions.Controls.Add(disable); actions.Controls.Add(reload);
            root.Controls.Add(actions, 0, 2);
            add.Click += (s, e) => Add();
            disable.Click += (s, e) => Disable();
            reload.Click += (s, e) => RefreshData();
            Shown += (s, e) => RefreshData();
        }
        static Button Btn(string text) { return new Button { Text = text, AutoSize = true, MinimumSize = new Size(165, 43), FlatStyle = FlatStyle.Flat }; }
        void RefreshData()
        {
            try { table.DataSource = ScanExclusionService.List(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thể tải ngoại lệ"); }
        }
        void Add()
        {
            using (var chooser = new OpenFileDialog { Title = "Chọn tệp cần giảm cảnh báo heuristic", Filter = "Tất cả tệp|*.*" })
            {
                if (chooser.ShowDialog(this) != DialogResult.OK) return;
                string reason = Microsoft.VisualBasic.Interaction.InputBox("Nhập lý do ngoại lệ (bắt buộc):", "Lý do", "");
                if (string.IsNullOrWhiteSpace(reason)) return;
                string days = Microsoft.VisualBasic.Interaction.InputBox("Hết hạn sau bao nhiêu ngày (1-90)?", "Thời hạn", "7");
                int value;
                if (!int.TryParse(days, out value)) return;
                if (MessageBox.Show(this, "Chỉ giảm cảnh báo heuristic cho đúng phiên bản tệp đã chọn. Tiếp tục?",
                    "Ngoại lệ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                try { ScanExclusionService.AddHeuristicExclusion(chooser.FileName, reason, value); RefreshData(); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thể thêm ngoại lệ"); }
            }
        }
        void Disable()
        {
            try
            {
                if (table.CurrentRow == null) return;
                long id = Convert.ToInt64(table.CurrentRow.Cells["ExclusionID"].Value);
                if (MessageBox.Show(this, "Hủy ngoại lệ " + id + "?", "Ngoại lệ", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                ScanExclusionService.Disable(id); RefreshData();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thể hủy ngoại lệ"); }
        }
    }
}
