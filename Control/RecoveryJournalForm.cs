using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public sealed class RecoveryJournalForm : Form
    {
        private readonly DataGridView grid = new DataGridView();
        private readonly TextBox detail = new TextBox();
        public RecoveryJournalForm()
        {
            Text = "Đối soát giao dịch cách ly / khôi phục";
            Size = new Size(1080, 650); MinimumSize = new Size(750, 500);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font(Theme.UiFontFamily, 10.65F);
            var reload = new Button { Text = "Kiểm tra lại", Dock = DockStyle.Top, Height = 42 };
            reload.Click += (s, e) => LoadData();
            detail.Dock = DockStyle.Bottom; detail.Multiline = true; detail.ReadOnly = true;
            detail.Height = 110; detail.ScrollBars = ScrollBars.Vertical;
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.SelectionChanged += (s, e) =>
            {
                var item = grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as QuarantineRecoveryJournal.Operation;
                detail.Text = item == null ? "" : QuarantineRecoveryJournal.Diagnose(item) + Environment.NewLine +
                    "ID " + item.Id + "; trạng thái " + item.State + "; lỗi " + item.Error +
                    Environment.NewLine + "Chỉ đối soát, không tự khôi phục hoặc xóa tệp nghi ngờ.";
            };
            Controls.Add(grid); Controls.Add(detail); Controls.Add(reload);
            Shown += (s, e) => LoadData();
        }
        private void LoadData()
        {
            grid.DataSource = QuarantineRecoveryJournal.Outstanding();
            if (grid.Rows.Count == 0) detail.Text = "Không có giao dịch chưa hoàn tất được ghi nhận trong nhật ký cục bộ.";
        }
    }
}
