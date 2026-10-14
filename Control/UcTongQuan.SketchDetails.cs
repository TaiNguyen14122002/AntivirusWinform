using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcTongQuan
    {
        private UiSelect sketchDetailPicker;
        private Label sketchDetailPath, sketchAnalysisReason, sketchAnalysisHint;
        private UiPill sketchDetailStatus, sketchAnalysisLevel;
        private UiButton sketchIsolateFile, sketchDeleteFile;
        private bool syncingSketchDetail;
        private readonly List<System.Windows.Forms.Control> sketchDetailPanels = new List<System.Windows.Forms.Control>();

        private void BuildSketchDetails()
        {
            var page = new SketchPage("", "") { Name = "sketchDetailsPage" };
            page.Title.Visible = page.Subtitle.Visible = false;
            pnlChiTietKetQua.BackColor = Theme.DashboardBg;
            pnlChiTietKetQua.Controls.Add(page);
            var header = DungHeaderChiTiet();
            page.Canvas.Controls.Add(header);
            var summary = SketchUi.Card("fileSummary");
            var icon = new PictureBox { Image = UiIcons.FileIcon(26, ".exe"), SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Theme.RedTint };
            sketchDetailPicker = new UiSelect { Name = "sketchDetailPicker",
                Font = Theme.BoldFont, BackColor = Color.White, ForeColor = Theme.TextDark };
            sketchDetailPicker.Font = Theme.BoldFont;
            sketchDetailPath = SketchUi.Label("", Theme.SmallFont, Theme.TextGray);
            sketchDetailStatus = new UiPill("Chưa chọn tệp", Theme.PillKind.Info);
            summary.Controls.AddRange(new System.Windows.Forms.Control[] { icon, sketchDetailPicker, sketchDetailPath, sketchDetailStatus });
            sketchDetailPicker.SelectedIndexChanged += delegate
            {
                if (!syncingSketchDetail && sketchDetailPicker.SelectedIndex >= 0) ChonDongChiTiet(sketchDetailPicker.SelectedIndex);
            };
            var list = SketchUi.Card("detailFileList");
            var grid = DungBangChiTiet();
            list.Controls.Add(grid);
            SketchUi.Grid(dgvChiTiet);
            // Bảng giữ menu xử lý từng dòng và chức năng sao chép hash.
            bool listVisible = false;
            var listButton = SketchUi.Button("Danh sách tệp", Theme.BtnRole.Neutral, delegate
            { listVisible = !listVisible; page.Reflow(); });
            summary.Controls.Add(listButton);
            var nativeTabs = (TabControl)DungTabChiTiet();
            nativeTabs.Visible = false;
            page.Canvas.Controls.Add(nativeTabs);
            // TabControl ẩn vẫn cần native handle để phát SelectedIndexChanged và nạp dữ liệu.
            IntPtr tabHandle = nativeTabs.Handle;
            var tabs = new SketchTabs("Thông tin chi tiết", "VirusTotal", "Hành vi", "Chuỗi ký tự", "Thông tin bổ sung");
            var body = SketchUi.Card("detailPanel");
            body.Padding = new Padding(12);
            var panels = sketchDetailPanels;
            foreach (TabPage tab in nativeTabs.TabPages)
            {
                var panel = tab.Controls[0];
                body.Controls.Add(panel);
                panel.Dock = DockStyle.Fill;
                panel.BackColor = Color.White;
                panels.Add(panel);
            }
            Action selectTab = delegate
            {
                for (int i = 0; i < panels.Count; i++) panels[i].Visible = i == nativeTabs.SelectedIndex;
                tabs.SelectedIndex = nativeTabs.SelectedIndex;
            };
            tabs.SelectedIndexChanged += delegate { nativeTabs.SelectedIndex = tabs.SelectedIndex; selectTab(); };
            nativeTabs.SelectedIndexChanged += delegate { selectTab(); };
            selectTab();
            page.Canvas.Controls.AddRange(new System.Windows.Forms.Control[] { summary, list, tabs, body });
            page.Arrange = delegate(int width)
            {
                SketchUi.Place(header, page.Canvas, 0, 0, width, 96);
                SketchUi.Place(summary, page.Canvas, 0, 108, width, 90);
                icon.SetBounds(18, 23, 40, 40);
                sketchDetailPicker.SetBounds(74, 16, Math.Max(140, width - 268), 30);
                sketchDetailPath.SetBounds(74, 52, Math.Max(140, width - 270), 27);
                sketchDetailStatus.Location = new Point(Math.Max(0, width - sketchDetailStatus.Width - 18), 14);
                listButton.SetBounds(width - 160, 48, 142, 30);
                int top = 216;
                list.Visible = listVisible;
                if (listVisible) { list.SetBounds(0, top, width, 188); top += 204; }
                tabs.SetBounds(0, top, width, 44);
                body.SetBounds(0, top + 58, width, 410);
                page.SetExtent(top + 484);
            };
            page.Reflow();
        }

        private System.Windows.Forms.Control DungCardKetLuan()
        {
            var card = new UiGroup { Text = "Kết quả phân tích", Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0) };
            Theme.StyleCard(card);
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            sketchAnalysisLevel = new UiPill("Mức độ", Theme.PillKind.Info);
            sketchAnalysisReason = SketchUi.Label("", Theme.BoldFont, Theme.TextDark);
            sketchAnalysisHint = SketchUi.Label("Kiểm tra nguồn gốc tệp trước khi chạy hoặc khôi phục.", Theme.BodyFont, Theme.TextGray);
            sketchIsolateFile = SketchUi.Button("Cách ly tệp", Theme.BtnRole.Primary, delegate { XuLyMotDong(detailRowIndex, 1); SyncSketchDetail(); });
            sketchDeleteFile = SketchUi.Button("Xóa tệp", Theme.BtnRole.Danger, delegate { XuLyMotDong(detailRowIndex, 2); SyncSketchDetail(); });
            inner.Controls.AddRange(new System.Windows.Forms.Control[] { sketchAnalysisLevel, sketchAnalysisReason, sketchAnalysisHint, sketchIsolateFile, sketchDeleteFile });
            inner.Resize += delegate
            {
                sketchAnalysisLevel.Location = new Point(0, 5);
                sketchAnalysisReason.SetBounds(0, 46, inner.Width, 80);
                sketchAnalysisHint.SetBounds(0, 134, inner.Width, 58);
                sketchIsolateFile.SetBounds(0, 214, Math.Max(90, (inner.Width - 12) / 2), 40);
                sketchDeleteFile.SetBounds((inner.Width + 12) / 2, 214, Math.Max(90, (inner.Width - 12) / 2), 40);
            };
            card.Controls.Add(inner);
            return card;
        }

        private void SyncSketchDetail()
        {
            if (sketchDetailPicker == null) return;
            syncingSketchDetail = true;
            sketchDetailPicker.Items.Clear();
            foreach (ThreatRow row in threatRows) sketchDetailPicker.Items.Add(Path.GetFileName(row.Path));
            if (detailRowIndex >= 0 && detailRowIndex < threatRows.Count)
            {
                ThreatRow row = threatRows[detailRowIndex];
                sketchDetailPicker.SelectedIndex = detailRowIndex;
                sketchDetailPath.Text = row.Path;
                sketchDetailStatus.Set(row.StatusText, row.Resolved ? Theme.PillKind.Warn : Theme.PillOf(row.Level));
                sketchAnalysisLevel.Set("Mức độ " + Theme.LevelText(row.Level).ToLowerInvariant(), Theme.PillOf(row.Level));
                sketchAnalysisReason.Text = row.ThreatText;
                sketchAnalysisHint.Text = row.Resolved ? "Tệp đã được xử lý. Hãy kiểm tra kỹ trước khi khôi phục từ cách ly." : "Khuyến nghị cách ly tệp và đối chiếu kết quả phân tích trước khi chạy.";
                sketchIsolateFile.Enabled = sketchDeleteFile.Enabled = !row.Resolved && File.Exists(row.Path);
            }
            syncingSketchDetail = false;
        }

        internal void OpenQuarantinedFileDetails(string path, string reason, long size, DateTime detectedAt,
            string md5, string sha1, string sha256)
        {
            threatRows.Clear();
            threatRows.Add(new ThreatRow { Path = path, ThreatText = reason, SizeBytes = size,
                Quarantined = true, Level = Theme.ThreatLevel.High, HashMd5 = md5, HashSha1 = sha1, HashSha256 = sha256 });
            lastScanTime = detectedAt;
            detailRowIndex = 0;
            MoChiTietKetQua();
        }
    }
}
