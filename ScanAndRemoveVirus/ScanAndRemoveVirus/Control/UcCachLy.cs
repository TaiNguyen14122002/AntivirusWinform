using System;
using System.Collections.Generic;
using System.Drawing;
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
            Theme.StylePageHeader(lblQuarantineTitle, lblQuarantineSubtitle);
            Theme.StyleCard(grpQuarentineList, grpQuarantineInfo);
            // compact chuẩn Lịch sử: nút 40px thay vì band 49-61px
            btnRestore.Margin = btnRestoreAll.Margin = btnDeletePermanent.Margin =
                btnDeleteAll.Margin = btnRefreshQuarantine.Margin = new Padding(2, 8, 2, 8);
            lblTotalFilesTitle.Font = Theme.PageSubFont;
            lblTotalFilesTitle.ForeColor = Theme.TextGray;
            lblTotalFilesValue.Font = Theme.TitleFont;
            lblTotalFilesValue.ForeColor = Theme.BlueDark;
            Theme.StyleGrid(dgvQuarantine);
            Theme.StyleNeutralButtons(btnRefreshQuarantine);
            btnRestore.Click += delegate { RestoreSelected(); };
            btnDeletePermanent.Click += delegate { DeleteSelected(); };
            btnRestoreAll.Click += delegate { RestoreAll(); };
            btnDeleteAll.Click += delegate { DeleteAll(); };
            Theme.StyleButton(btnRestore, Theme.BtnRole.Action);
            Theme.StyleButton(btnRestoreAll, Theme.BtnRole.Action);
            Theme.StyleButton(btnDeletePermanent, Theme.BtnRole.Danger);
            Theme.StyleButton(btnDeleteAll, Theme.BtnRole.Danger);
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
                    item.Name,
                    item.OriginalPath,
                    string.IsNullOrEmpty(item.Threat) ? "Không rõ" : item.Threat,
                    item.DetectedTime == DateTime.MinValue ? "—" : item.DetectedTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    item.Size);
                dgvQuarantine.Rows[r].Tag = item;
            }
            lblTotalFilesValue.Text = items.Count.ToString();
        }

        private void RestoreSelected()
        {
            var ids = CollectIds(dgvQuarantine.SelectedRows);
            if (ids.Count == 0)
            {
                MessageBox.Show("Hãy chọn dòng cần khôi phục (bấm vào dòng trong danh sách).",
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
            var ids = CollectIds(dgvQuarantine.SelectedRows);
            if (ids.Count == 0)
            {
                MessageBox.Show("Hãy chọn dòng cần xóa vĩnh viễn.", "Xóa",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var answer = MessageBox.Show(
                "Xóa vĩnh viễn " + ids.Count + " tệp đã cách ly? Không thể hoàn tác.",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
            foreach (string id in ids) ScanEngine.DeleteQuarantined(id);
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

        private void DeleteAll()
        {
            var ids = CollectIds(dgvQuarantine.Rows);
            if (ids.Count == 0) return;
            var answer = MessageBox.Show(
                "XÓA VĨNH VIỄN toàn bộ " + ids.Count + " tệp đã cách ly? Không thể hoàn tác.",
                "Xóa tất cả", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
            foreach (string id in ids) ScanEngine.DeleteQuarantined(id);
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
