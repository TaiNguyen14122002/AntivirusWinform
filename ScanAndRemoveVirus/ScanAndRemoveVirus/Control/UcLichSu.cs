using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcLichSu : UserControl
    {
        enum HistView { Scans, Threats, Updates }
        HistView view = HistView.Scans;
        bool oldestFirst;   // đảo chiều sắp xếp: false = mới nhất trước (mặc định)

        public UcLichSu()
        {
            InitializeComponent();
            BackColor = Theme.PageBg;
            Theme.StyleGrid(dgvHistory);
            colPick.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHistory.CurrentCellDirtyStateChanged += delegate
            {
                if (dgvHistory.IsCurrentCellDirty)
                    dgvHistory.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvHistory.CellValueChanged += Grid_CellValueChanged;
            dgvHistory.ColumnHeaderMouseClick += Grid_HeaderClick;
            dgvHistory.MouseMove += Grid_MouseMove;   // header bấm được -> Hand
            dgvHistory.CellDoubleClick += delegate { OpenDetail(); };
            dgvHistory.SelectionChanged += delegate { UpdatePickSummary(); };

            Theme.StyleButton(btnFilterAll, Theme.BtnRole.Action);
            Theme.StyleButton(btnFilterThreats, Theme.BtnRole.Danger);
            Theme.StyleButton(btnFilterUpdates, Theme.BtnRole.Primary);
            btnFilterAll.Click += delegate { SetView(HistView.Scans); };
            btnFilterThreats.Click += delegate { SetView(HistView.Threats); };
            btnFilterUpdates.Click += delegate { SetView(HistView.Updates); };

            Theme.StyleNeutralButtons(btnViewDetail, btnRefreshHistory, btnExportReport);
            Theme.StyleButton(btnDeleteHistory, Theme.BtnRole.Danger);
            btnRefreshHistory.Click += delegate { LoadRealData(); };
            btnExportReport.Click += ExportReport;
            btnDeleteHistory.Click += BtnDeleteHistory_Click;
            btnViewDetail.Click += delegate { OpenDetail(); };

            // HeaderText/ToolTipText của cột ĐƯỢC LƯU TRONG HeaderCell -> thay cell phải gán lại
            colPick.HeaderCell = new IconHeaderCell(this, true);
            colPick.HeaderText = "";
            colPick.ToolTipText = "Nhấp để chọn / bỏ chọn tất cả";
            colTime.HeaderCell = new IconHeaderCell(this, false);
            colTime.HeaderText = "Thời gian";
            colTime.ToolTipText = "Nhấp để đổi chiều sắp xếp theo thời gian";
            LoadRealData();
        }

        private void SetView(HistView v)
        {
            view = v;
            FillGrid();
        }

        // Cột Kết quả: An toàn xanh / Phát hiện đỏ / Thành công xanh dương;
        // dòng được TICK chọn nền xanh nhạt để phân biệt với dòng đang highlight
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            if (e.ColumnIndex == colResult.Index)
                Theme.PaintStatusCell(e, e.Value.ToString());
            else if (e.ColumnIndex == colThreatCount.Index)
            {
                int n;
                if (int.TryParse(e.Value.ToString(), out n) && n > 0)
                {
                    e.CellStyle.ForeColor = Theme.Red;
                    e.CellStyle.Font = Theme.BoldFont;
                    e.CellStyle.SelectionForeColor = Theme.Red;
                }
            }
            else if (e.ColumnIndex == colTime.Index
                     && dgvHistory.Rows[e.RowIndex].Cells[colPick.Index].Value is bool b && b)
            {
                e.CellStyle.BackColor = Theme.BlueTint;
                e.CellStyle.SelectionBackColor = Theme.BlueTint;
                e.CellStyle.Font = Theme.BoldFont;
            }
        }

        // ==== HEADER ICONS (Segoe MDL2 Assets) ====
        static readonly Font GlyphFont = new Font("Segoe MDL2 Assets", 11F, FontStyle.Regular, GraphicsUnit.Point);
        static readonly Font GlyphSmall = new Font("Segoe MDL2 Assets", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
        const string BoxEmpty = "";      // CheckBox rỗng
        const string BoxChecked = "";    // CheckBox đã bật hết
        const string BoxPartial = "";    // bật một phần (indeterminate)
        const string ArrowUp = "";       // ChevronUp: đang cũ -> mới
        const string ArrowDown = "";     // ChevronDown: đang mới -> cũ

        private sealed class IconHeaderCell : DataGridViewColumnHeaderCell
        {
            readonly UcLichSu owner;
            readonly bool pickMode;
            public IconHeaderCell(UcLichSu owner, bool pickMode)
            {
                this.owner = owner;
                this.pickMode = pickMode;
            }

            protected override void Paint(Graphics g, Rectangle clipBounds, Rectangle cellBounds,
                int rowIndex, DataGridViewElementStates cellState, object formattedValue, object value,
                string errorText, DataGridViewCellStyle cellStyle,
                DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
            {
                base.Paint(g, clipBounds, cellBounds, rowIndex, cellState, formattedValue, value,
                    errorText, cellStyle, advancedBorderStyle,
                    paintParts & ~(DataGridViewPaintParts.ContentForeground | DataGridViewPaintParts.Focus));
                var hs = owner.dgvHistory.ColumnHeadersDefaultCellStyle;
                if (pickMode)
                {
                    int st = owner.PickState();
                    string gly = st == 2 ? BoxChecked : st == 1 ? BoxPartial : BoxEmpty;
                    TextRenderer.DrawText(g, gly, GlyphFont, cellBounds,
                        st == 0 ? Theme.TextGray : Theme.Blue,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                else
                {
                    string headerText = OwningColumn.HeaderText ?? "";
                    int h = cellBounds.Height;
                    Size ts = TextRenderer.MeasureText(headerText, hs.Font);
                    Point pt = new Point(cellBounds.X + 6, cellBounds.Y + (h - ts.Height) / 2);
                    TextRenderer.DrawText(g, headerText, hs.Font, pt, hs.ForeColor);
                    TextRenderer.DrawText(g, owner.oldestFirst ? ArrowUp : ArrowDown,
                        GlyphSmall, new Rectangle(pt.X + ts.Width + 4, cellBounds.Y, 26, h), Theme.Blue,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colPick.Index) return;
            dgvHistory.InvalidateRow(e.RowIndex);
            UpdatePickSummary();
        }

        // 0 = không dòng nào tick, 1 = một phần, 2 = tất cả
        private int PickState()
        {
            int n = dgvHistory.Rows.Count;
            if (n == 0) return 0;
            int p = PickedCount();
            return p == 0 ? 0 : (p == n ? 2 : 1);
        }

        private void InvalidateHeaderIcons()
        {
            dgvHistory.Invalidate(new Rectangle(0, 0, dgvHistory.Width, dgvHistory.ColumnHeadersHeight));
        }

        // Bấm header: ô vuông -> chọn/bỏ TẤT CẢ; Thời gian -> đảo chiều sắp xếp
        private void Grid_HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == colPick.Index)
            {
                bool any = dgvHistory.Rows.Cast<DataGridViewRow>()
                    .Any(r => r.Cells[colPick.Index].Value is bool b && b);
                foreach (DataGridViewRow r in dgvHistory.Rows)
                    r.Cells[colPick.Index].Value = !any;
                dgvHistory.Invalidate();
                UpdatePickSummary();
            }
            else if (e.ColumnIndex == colTime.Index)
            {
                ToggleSort();
            }
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            var hit = dgvHistory.HitTest(e.X, e.Y);
            if (hit.Type == DataGridViewHitTestType.ColumnHeader
                && (hit.ColumnIndex == colPick.Index || hit.ColumnIndex == colTime.Index))
                dgvHistory.Cursor = Cursors.Hand;
            else
                dgvHistory.Cursor = Cursors.Default;
        }

        private int PickedCount()
        {
            return dgvHistory.Rows.Cast<DataGridViewRow>()
                .Count(r => r.Cells[colPick.Index].Value is bool b && b);
        }

        private void UpdatePickSummary()
        {
            int n = PickedCount();
            if (n == 0)
            {
                lblSelection.Text = "Chưa chọn dòng nào — tích ô vuông đầu dòng (bấm ô trên header để chọn tất cả).";
                lblSelection.ForeColor = Theme.TextGray;
            }
            else
            {
                lblSelection.Text = "Đã chọn " + n + " dòng — sẵn sàng xóa.";
                lblSelection.ForeColor = Theme.Red;
            }
            btnDeleteHistory.Enabled = n > 0;
            InvalidateHeaderIcons();   // icon header đổi theo trạng thái tick
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) LoadRealData();
        }

        private List<HistoryEntry> all = new List<HistoryEntry>();

        // Lịch sử quét THẬT từ scanhistory.log (một nguồn duy nhất qua DataDir)
        private void LoadRealData()
        {
            all = ScanHistoryStore.Entries();
            FillGrid();
        }

        private void FillGrid()
        {
            dgvHistory.Rows.Clear();
            for (int pass = 0; pass < all.Count; pass++)
            {
                HistoryEntry e = all[oldestFirst ? all.Count - 1 - pass : pass];
                bool isUpdate = e.Type == "Cập nhật CSDL";
                bool keep;
                if (view == HistView.Updates) keep = isUpdate;
                else if (view == HistView.Threats) keep = !isUpdate && e.Threats > 0;
                else keep = !isUpdate;
                if (!keep) continue;

                string duration = isUpdate ? "—" : FormatDuration(e.Seconds);
                int ix = dgvHistory.Rows.Add(
                    false,
                    e.Time.ToString("dd/MM/yyyy HH:mm"),
                    e.Type,
                    e.Scope,
                    isUpdate ? "Thành công" : e.Result,
                    isUpdate ? "—" : e.Threats.ToString(),
                    duration);
                dgvHistory.Rows[ix].Tag = e;
            }
            string label = view == HistView.Updates ? "Cập nhật CSDL"
                : view == HistView.Threats ? "phiên có đe dọa" : "mọi phiên quét";
            lblStats.Text = "Đang xem: " + label + " — " + dgvHistory.Rows.Count
                + "/" + all.Count + " dòng · " + (oldestFirst ? "cũ → mới ↑" : "mới → cũ ↓");
            UpdatePickSummary();
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds < 1) return "dưới 1 giây";
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalMinutes < 1) return string.Format("{0} giây", (int)t.TotalSeconds);
            return string.Format("{0} phút {1:00} giây", (int)t.TotalMinutes, t.Seconds);
        }

        // Sort nằm ở header "Thời gian" (mũi tên kế chữ) — test gọi qua reflection
        private void ToggleSort()
        {
            oldestFirst = !oldestFirst;
            FillGrid();
        }

        // XÓA THEO TICK: đọc trực tiếp giá trị cột Chọn — không phụ thuộc selection
        private void BtnDeleteHistory_Click(object sender, EventArgs e)
        {
            var victims = new List<HistoryEntry>();
            foreach (DataGridViewRow row in dgvHistory.Rows)
            {
                if (!(row.Cells[colPick.Index].Value is bool b) || !b) continue;
                var entry = row.Tag as HistoryEntry;
                if (entry != null) victims.Add(entry);
            }
            if (victims.Count == 0)
            {
                MessageBox.Show("Chưa có dòng nào được tích ở cột chọn (ô vuông đầu dòng).",
                    "Xóa lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var ask = MessageBox.Show(
                "Xóa " + victims.Count + " dòng đã chọn khỏi lịch sử?\n\nHành động này không thể hoàn tác.",
                "Xóa lịch sử", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (ask != DialogResult.Yes) return;
            int removed = 0;
            foreach (HistoryEntry entry in victims)
                if (ScanHistoryStore.Remove(entry)) removed++;
            LoadRealData();
            lblHistorySubtitle.Text = "Đã xóa " + removed + " dòng khỏi lịch sử (lúc "
                + DateTime.Now.ToString("HH:mm:ss") + ").";
        }

        private void OpenDetail()
        {
            HistoryEntry entry = null;
            if (dgvHistory.SelectedRows.Count > 0)
                entry = dgvHistory.SelectedRows[0].Tag as HistoryEntry;
            if (entry == null)
            {
                // người dùng tick dòng thay vì chọn dòng -> chi tiết theo tick đầu tiên
                var row = dgvHistory.Rows.Cast<DataGridViewRow>()
                    .FirstOrDefault(r => r.Cells[colPick.Index].Value is bool b && b);
                entry = row == null ? null : row.Tag as HistoryEntry;
            }
            if (entry == null)
            {
                MessageBox.Show("Hãy chọn một dòng trong lịch sử.", "Chi tiết",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MessageBox.Show(string.Format(
                "Thời gian: {0:dd/MM/yyyy HH:mm:ss}\r\nHành động: {1}\r\nPhạm vi: {2}\r\n"
                + "Tệp đã quét: {3:N0}\r\nMối đe dọa: {4}\r\nKết quả: {5}\r\nThời lượng: {6:0.0} giây",
                entry.Time, entry.Type, entry.Scope, entry.Files,
                entry.Threats, entry.Result, entry.Seconds),
                "Chi tiết lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportReport(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Xuất báo cáo lịch sử quét";
                dlg.Filter = "Tệp CSV (*.csv)|*.csv";
                dlg.FileName = "lich-su-quet-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".csv";
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    ScanHistoryStore.ExportCsv(dlg.FileName);
                    MessageBox.Show("Đã xuất báo cáo:\n" + dlg.FileName, "Xuất báo cáo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không xuất được báo cáo: " + ex.Message, "Xuất báo cáo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
