using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcCachLy : UserControl
    {
        public UcCachLy()
        {
            InitializeComponent();
            BackColor = Theme.PageBg;
            // Responsive: cửa sổ nhỏ -> cuộn thay vì cắt nội dung
            Theme.ScrollablePage(this, tableLayoutPanel1, 980, 620);
            Theme.StylePageHeader(lblQuarantineTitle, lblQuarantineSubtitle);
            Theme.StyleCard(grpQuarentineList, grpQuarantineInfo);
            // compact chuẩn Lịch sử: nút 40px thay vì band 49-61px
            btnRestore.Margin = btnRestoreAll.Margin = btnDeletePermanent.Margin =
                btnRefreshQuarantine.Margin = new Padding(2, 8, 2, 8);
            lblTotalFilesTitle.Font = Theme.PageSubFont;
            lblTotalFilesTitle.ForeColor = Theme.TextGray;
            lblTotalFilesValue.Font = Theme.TitleFont;
            lblTotalFilesValue.ForeColor = Theme.BlueDark;
            Theme.StyleGrid(dgvQuarantine);
            // Cột "Chọn" (checkbox) dùng chung: header ô vuông + commit tức thì
            colPick.HeaderCell = new Theme.SelectAllHeaderCell(() => Theme.PickState(dgvQuarantine, colPick.Index));
            dgvQuarantine.CurrentCellDirtyStateChanged += delegate
            {
                if (dgvQuarantine.IsCurrentCellDirty)
                    dgvQuarantine.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvQuarantine.CellValueChanged += (s, e) =>
            {
                if (e.ColumnIndex == colPick.Index) SyncButtons();
            };
            dgvQuarantine.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.ColumnIndex == colPick.Index)
                {
                    bool any = dgvQuarantine.Rows.Cast<DataGridViewRow>()
                        .Any(r => r.Cells[colPick.Index].Value is bool b && b);
                    Theme.PickAll(dgvQuarantine, colPick.Index, !any);
                    SyncButtons();
                }
            };
            Theme.StyleNeutralButtons(btnRefreshQuarantine);
            btnRestore.Click += delegate { RestoreSelected(); };
            btnDeletePermanent.Click += delegate { DeleteSelected(); };
            btnRestoreAll.Click += delegate { RestoreAll(); };
            Theme.StyleButton(btnRestore, Theme.BtnRole.Action);
            Theme.StyleButton(btnRestoreAll, Theme.BtnRole.Action);
            Theme.StyleButton(btnDeletePermanent, Theme.BtnRole.Danger);
            btnRefreshQuarantine.Click += delegate { RefreshData(); };
            dgvQuarantine.CellFormatting += Grid_CellFormatting;
            ScanEngine.QuarantineChanged += OnQuarantineChanged;
            RefreshData();
        }

        // Cột Mối đe dọa đỏ đậm; ô "Thời gian phát hiện" xám phụ
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            if (e.ColumnIndex == colThreatName.Index)
            {
                e.CellStyle.ForeColor = Theme.RedText;
                e.CellStyle.Font = Theme.BoldFont;
                e.CellStyle.SelectionForeColor = Theme.RedText;
            }
            else if (e.ColumnIndex == colDetectedTime.Index || e.ColumnIndex == colFileSize.Index)
                e.CellStyle.ForeColor = Theme.TextGray;
        }

        // Tab được FrmMain dựng sẵn 1 lần -> tự làm mới mỗi khi người dùng mở tab
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) RefreshData();
        }

        private void OnQuarantineChanged()
        {
            try { BeginInvoke((MethodInvoker)RefreshData); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { } // handle chưa tạo (startup) - RefreshData cuối ctor lo
        }

        public void RefreshData()
        {
            dgvQuarantine.Rows.Clear();
            List<ScanEngine.QuarantinedItem> items = ScanEngine.ListQuarantined();
            foreach (ScanEngine.QuarantinedItem item in items)
            {
                int r = dgvQuarantine.Rows.Add(
                    false,
                    item.Name,
                    item.OriginalPath,
                    string.IsNullOrEmpty(item.Threat) ? "Không rõ" : item.Threat,
                    item.DetectedTime == DateTime.MinValue ? "—" : item.DetectedTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    item.Size);
                dgvQuarantine.Rows[r].Tag = item;
            }
            lblTotalFilesValue.Text = items.Count.ToString();
            SyncButtons();
        }

        private IEnumerable<DataGridViewRow> TickedRows()
        {
            return dgvQuarantine.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Cells[colPick.Index].Value is bool b && b);
        }

        private void SyncButtons()
        {
            bool hasPick = TickedRows().Any();
            bool hasRows = dgvQuarantine.Rows.Count > 0;
            btnRestore.Enabled = hasRows && hasPick;
            btnDeletePermanent.Enabled = hasRows && hasPick;
            btnRestoreAll.Enabled = hasRows;
            Theme.InvalidatePickHeader(dgvQuarantine);
        }

        private void RestoreSelected()
        {
            var ids = CollectIds(TickedRows());
            if (ids.Count == 0)
            {
                MessageBox.Show("Hãy tích ô Chọn ở dòng cần khôi phục.",
                    "Khôi phục", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ConfirmRestore(ids)) return;
            int failed = 0;
            foreach (string id in ids)
                if (!ScanEngine.RestoreQuarantined(id)) failed++;
            if (failed > 0)
                MessageBox.Show(failed + " tệp không khôi phục được (đã bị xóa khỏi khu cách ly hoặc bị khóa).",
                    "Khôi phục", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // Hàng "Bảo vệ tệp": quét lại tệp cách ly TRƯỚC khi thả về máy — chặn malware quay ngược vào hệ thống
        private bool ConfirmRestore(List<string> ids)
        {
            if (!FeatureFlags.FileRestoreGuard) return true;
            string warning = GuardService.RestoreWarningFor(ids);
            if (warning == null) return true;
            var answer = MessageBox.Show(
                "Cảnh báo — các tệp vẫn khớp phát hiện nguy hiểm:\n\n" + warning
                + "\n\nVẫn khôi phục chúng về máy?",
                "Bảo vệ tệp: tệp cách ly còn nghi vấn",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            return answer == DialogResult.Yes;
        }

        private void DeleteSelected()
        {
            var ids = CollectIds(TickedRows());
            if (ids.Count == 0)
            {
                MessageBox.Show("Hãy tích ô Chọn ở dòng cần xóa vĩnh viễn.", "Xóa",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var answer = MessageBox.Show(
                "Xóa vĩnh viễn " + ids.Count + " tệp đã cách ly? Không thể hoàn tác.",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
            foreach (string id in ids) ScanEngine.DeleteQuarantined(id, true);
        }

        private void RestoreAll()
        {
            var ids = CollectIds(dgvQuarantine.Rows);
            if (ids.Count == 0) return;
            var answer = MessageBox.Show(
                "Khôi phục toàn bộ " + ids.Count + " tệp về vị trí cũ?",
                "Khôi phục tất cả", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            if (!ConfirmRestore(ids)) return;
            foreach (string id in ids) ScanEngine.RestoreQuarantined(id);
        }

        // Bắt buộc tách id ra danh sách riêng: mỗi lần Restore/Delete bắn QuarantineChanged ->
        // RefreshData() clear grid, nếu lặp trực tiếp trên Rows/SelectedRows sẽ lỗi collection đã thay đổi.
        private static List<string> CollectIds(System.Collections.IEnumerable rows)
        {
            var ids = new List<string>();
            foreach (DataGridViewRow row in rows)
            {
                var item = row.Tag as ScanEngine.QuarantinedItem;
                if (item != null) ids.Add(item.Id);
            }
            return ids;
        }

        private void tableLayoutPanel9_Paint(object sender, PaintEventArgs e)
        {

        }

        private void grpQuarentineList_Enter(object sender, EventArgs e)
        {

        }

        // Tham khảo: hiển thị thư mục cách ly thật khi cần kiểm tra thủ công
        public static string QuarantineFolderLocation
        {
            get { return ScanEngine.QuarantineDir; }
        }
    }
}
