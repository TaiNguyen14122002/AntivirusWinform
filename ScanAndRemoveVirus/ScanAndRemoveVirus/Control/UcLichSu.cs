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
        public UcLichSu()
        {
            InitializeComponent();
            BackColor = Theme.PageBg;
            Theme.StyleGrid(dgvHistory);
            Theme.StyleNeutralButtons(btnViewDetail, btnRefreshHistory, btnExportReport);
            btnRefreshHistory.Click += delegate { LoadRealData(); };
            btnExportReport.Click += ExportReport;
            tabHistory.SelectedIndexChanged += delegate { FillGrid(); }; // btnViewDetail đã gắn trong Designer
            dgvHistory.CellFormatting += Grid_CellFormatting;
            LoadRealData();
        }

        // Cột Kết quả: An toàn xanh / Phát hiện đỏ / Thành công xanh dương
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
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) LoadRealData();
        }

        private List<HistoryEntry> all = new List<HistoryEntry>();

        // Lịch sử quét THẬT từ %AppData%\ScanAndRemoveVirus\scanhistory.log
        private void LoadRealData()
        {
            all = ScanHistoryStore.Entries();
            FillGrid();
        }

        private void FillGrid()
        {
            dgvHistory.Rows.Clear();
            int tab = tabHistory.SelectedIndex; // 0: quét | 1: đe dọa | 2: cập nhật
            foreach (HistoryEntry e in all)
            {
                bool isUpdate = e.Type == "Cập nhật CSDL";
                bool keep;
                if (tab == 2) keep = isUpdate;
                else if (tab == 1) keep = !isUpdate && e.Threats > 0;
                else keep = !isUpdate;
                if (!keep) continue;

                string duration = isUpdate ? "—" : FormatDuration(e.Seconds);
                dgvHistory.Rows.Add(
                    e.Time.ToString("dd/MM/yyyy HH:mm"),
                    e.Type,
                    e.Scope,
                    isUpdate ? "Thành công" : e.Result,
                    isUpdate ? "—" : e.Threats.ToString(),
                    duration);
                dgvHistory.Rows[dgvHistory.Rows.Count - 1].Tag = e;
            }
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds < 1) return "dưới 1 giây";
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalMinutes < 1) return string.Format("{0} giây", (int)t.TotalSeconds);
            return string.Format("{0} phút {1:00} giây", (int)t.TotalMinutes, t.Seconds);
        }

        // btnViewDetail được Designer gắn sẵn -> hiển thị đầy đủ thông tin dòng đang chọn
        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            if (dgvHistory.SelectedRows.Count == 0)
            {
                MessageBox.Show("Hãy chọn một dòng trong lịch sử.", "Chi tiết",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var entry = dgvHistory.SelectedRows[0].Tag as HistoryEntry;
            if (entry == null) return;
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

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dgvHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
