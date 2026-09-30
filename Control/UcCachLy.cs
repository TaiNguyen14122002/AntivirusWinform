using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcCachLy : UserControl
    {
        private readonly ThreatDetectionRepository _threatRepository = new ThreatDetectionRepository();

        private sealed class QuarantineRow
        {
            public long DetectionID { get; set; }
            public string FileName { get; set; }
            public string OriginalPath { get; set; }
            public string ThreatName { get; set; }
            public DateTime? DetectedAt { get; set; }
            public long? FileSizeBytes { get; set; }
            public string FileMD5 { get; set; }
            public string FileSHA1 { get; set; }
            public string FileSHA256 { get; set; }
            public string ActionTaken { get; set; }
            public string Status { get; set; }
            public string QuarantinePath { get; set; }
        }

        public UcCachLy()
        {
            InitializeComponent();
            BackColor = Theme.PageBg;
            Theme.ScrollablePage(this, tableLayoutPanel1, 980, 620);
            Theme.StyleCard(grpQuarentineList, grpQuarantineInfo);

            btnRestore.Margin = btnRestoreAll.Margin = btnDeletePermanent.Margin =
                btnRefreshQuarantine.Margin = new Padding(0, 0, 8, 0);

            lblTotalFilesTitle.Font = Theme.PageSubFont;
            lblTotalFilesTitle.ForeColor = Theme.TextGray;
            lblTotalFilesValue.Font = Theme.TitleFont;
            lblTotalFilesValue.ForeColor = Theme.BlueDark;
            Theme.StyleGrid(dgvQuarantine);

            colPick.HeaderCell = new Theme.SelectAllHeaderCell(
                () => Theme.PickState(dgvQuarantine, colPick.Index));

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
                if (e.ColumnIndex != colPick.Index) return;
                bool any = dgvQuarantine.Rows.Cast<DataGridViewRow>()
                    .Any(r => r.Cells[colPick.Index].Value is bool b && b);
                Theme.PickAll(dgvQuarantine, colPick.Index, !any);
                SyncButtons();
            };

            Theme.StyleNeutralButtons(btnRefreshQuarantine);
            Theme.StyleButton(btnRestore, Theme.BtnRole.Action);
            Theme.StyleButton(btnRestoreAll, Theme.BtnRole.Action);
            Theme.StyleButton(btnDeletePermanent, Theme.BtnRole.Danger);

            btnRestore.Click += delegate { RestoreSelected(); };
            btnDeletePermanent.Click += delegate { DeleteSelected(); };
            btnRestoreAll.Click += delegate { RestoreAll(); };
            btnRefreshQuarantine.Click += delegate { RefreshData(); };
            dgvQuarantine.CellFormatting += Grid_CellFormatting;

            ScanEngine.QuarantineChanged += OnQuarantineChanged;
            RefreshData();
        }

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

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) RefreshData();
        }

        private void OnQuarantineChanged()
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((MethodInvoker)RefreshData);
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        public void RefreshData()
        {
            try
            {
                dgvQuarantine.Rows.Clear();
                DataTable table = _threatRepository.GetQuarantined();

                foreach (DataRow row in table.Rows)
                {
                    QuarantineRow item = CreateItem(row);
                    int index = dgvQuarantine.Rows.Add(
                        false,
                        string.IsNullOrWhiteSpace(item.FileName) ? "Không rõ" : item.FileName,
                        string.IsNullOrWhiteSpace(item.OriginalPath) ? "—" : item.OriginalPath,
                        string.IsNullOrWhiteSpace(item.ThreatName) ? "Không rõ" : item.ThreatName,
                        item.DetectedAt.HasValue ? item.DetectedAt.Value.ToString("dd/MM/yyyy HH:mm:ss") : "—",
                        FormatFileSize(item.FileSizeBytes));
                    dgvQuarantine.Rows[index].Tag = item;
                }

                lblTotalFilesValue.Text = table.Rows.Count.ToString();
                SyncButtons();
            }
            catch (Exception ex)
            {
                dgvQuarantine.Rows.Clear();
                lblTotalFilesValue.Text = "0";
                SyncButtons();
                MessageBox.Show("Không thể tải danh sách tệp cách ly từ SQL Server.\n\n" + ex.Message,
                    "Cách ly", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static QuarantineRow CreateItem(DataRow row)
        {
            return new QuarantineRow
            {
                DetectionID = GetInt64(row, "DetectionID", 0),
                FileName = GetString(row, "FileName"),
                OriginalPath = GetString(row, "OriginalPath"),
                ThreatName = GetString(row, "ThreatName"),
                DetectedAt = GetDateTime(row, "DetectedAt"),
                FileSizeBytes = GetNullableInt64(row, "FileSizeBytes"),
                FileMD5 = GetString(row, "FileMD5"),
                FileSHA1 = GetString(row, "FileSHA1"),
                FileSHA256 = GetString(row, "FileSHA256"),
                ActionTaken = GetString(row, "ActionTaken"),
                Status = GetString(row, "Status"),
                QuarantinePath = GetString(row, "QuarantinePath")
            };
        }

        private static string GetString(DataRow row, string name)
        {
            return !row.Table.Columns.Contains(name) || row[name] == DBNull.Value ? null : Convert.ToString(row[name]);
        }

        private static long GetInt64(DataRow row, string name, long fallback)
        {
            return !row.Table.Columns.Contains(name) || row[name] == DBNull.Value ? fallback : Convert.ToInt64(row[name]);
        }

        private static long? GetNullableInt64(DataRow row, string name)
        {
            return !row.Table.Columns.Contains(name) || row[name] == DBNull.Value ? (long?)null : Convert.ToInt64(row[name]);
        }

        private static DateTime? GetDateTime(DataRow row, string name)
        {
            return !row.Table.Columns.Contains(name) || row[name] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row[name]);
        }

        private static string FormatFileSize(long? bytes)
        {
            if (!bytes.HasValue || bytes.Value < 0) return "—";
            long v = bytes.Value;
            if (v < 1024) return v.ToString("N0") + " B";
            if (v < 1024L * 1024L) return (v / 1024.0).ToString("0.##") + " KB";
            if (v < 1024L * 1024L * 1024L) return (v / (1024.0 * 1024.0)).ToString("0.##") + " MB";
            return (v / (1024.0 * 1024.0 * 1024.0)).ToString("0.##") + " GB";
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
            List<QuarantineRow> items = CollectItems(TickedRows());
            if (items.Count == 0)
            {
                MessageBox.Show("Hãy tích ô Chọn ở dòng cần khôi phục.", "Khôi phục",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ConfirmRestore(items)) return;
            ProcessRestore(items, "Khôi phục");
        }

        private void RestoreAll()
        {
            List<QuarantineRow> items = CollectItems(dgvQuarantine.Rows);
            if (items.Count == 0) return;

            DialogResult answer = MessageBox.Show(
                "Khôi phục toàn bộ " + items.Count + " tệp về vị trí cũ?",
                "Khôi phục tất cả", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            if (!ConfirmRestore(items)) return;
            ProcessRestore(items, "Khôi phục tất cả");
        }

        private void ProcessRestore(List<QuarantineRow> items, string title)
        {
            int success = 0, failed = 0;
            foreach (QuarantineRow item in items)
            {
                if (ScanEngine.RestoreQuarantined(item.DetectionID)) success++;
                else failed++;
            }
            RefreshData();
            ShowOperationResult(title, success, failed);
        }

        // Quét lại file .qtn trước khi thả về máy. forceContent=true giúp kiểm tra
        // nội dung dù file cách ly đã đổi đuôi thành .qtn.
        private bool ConfirmRestore(List<QuarantineRow> items)
        {
            if (!FeatureFlags.FileRestoreGuard) return true;

            var warnings = new List<string>();
            foreach (QuarantineRow item in items)
            {
                if (string.IsNullOrWhiteSpace(item.QuarantinePath) || !File.Exists(item.QuarantinePath)) continue;
                ThreatFound threat = ScanEngine.EvaluateFile(item.QuarantinePath, true);
                if (threat != null)
                    warnings.Add((string.IsNullOrWhiteSpace(item.FileName) ? "Không rõ" : item.FileName) +
                        "\n" + (string.IsNullOrWhiteSpace(threat.Reason) ? "Vẫn được phát hiện là nguy hiểm." : threat.Reason));
            }

            if (warnings.Count == 0) return true;
            string details = string.Join("\n\n", warnings.Take(10));
            if (warnings.Count > 10) details += "\n\n... và " + (warnings.Count - 10) + " tệp khác.";

            return MessageBox.Show(
                "Cảnh báo — " + warnings.Count + " tệp vẫn được phát hiện là nguy hiểm:\n\n" +
                details + "\n\nVẫn khôi phục chúng về máy?",
                "Bảo vệ tệp: tệp cách ly còn nghi vấn",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void DeleteSelected()
        {
            List<QuarantineRow> items = CollectItems(TickedRows());
            if (items.Count == 0)
            {
                MessageBox.Show("Hãy tích ô Chọn ở dòng cần xóa vĩnh viễn.", "Xóa",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                "Xóa vĩnh viễn " + items.Count + " tệp đã cách ly?\n\nThao tác này không thể hoàn tác.",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int success = 0, failed = 0;
            foreach (QuarantineRow item in items)
            {
                if (ScanEngine.DeleteQuarantined(item.DetectionID, true)) success++;
                else failed++;
            }
            RefreshData();
            ShowOperationResult("Xóa vĩnh viễn", success, failed);
        }

        private static void ShowOperationResult(string title, int success, int failed)
        {
            MessageBox.Show(
                failed == 0 ? "Thành công: " + success + " tệp."
                            : "Thành công: " + success + "\nKhông thể xử lý: " + failed,
                title, MessageBoxButtons.OK,
                failed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private static List<QuarantineRow> CollectItems(IEnumerable rows)
        {
            var result = new List<QuarantineRow>();
            foreach (object obj in rows)
            {
                DataGridViewRow row = obj as DataGridViewRow;
                if (row == null) continue;
                QuarantineRow item = row.Tag as QuarantineRow;
                if (item != null && item.DetectionID > 0) result.Add(item);
            }
            return result;
        }

        private void tableLayoutPanel9_Paint(object sender, PaintEventArgs e) { }
        private void grpQuarentineList_Enter(object sender, EventArgs e) { }

        public static string QuarantineFolderLocation
        {
            get { return ScanEngine.QuarantineDir; }
        }
    }
}
