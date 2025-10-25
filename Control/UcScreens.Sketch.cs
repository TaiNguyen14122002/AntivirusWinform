using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcBaoVe
    {
        private readonly Dictionary<string, UiSwitch> sketchSwitches = new Dictionary<string, UiSwitch>();
        private readonly Dictionary<string, Label> sketchFeatureStates = new Dictionary<string, Label>();
        private Label sketchProtectionTitle, sketchProtectionHint;
        private UiPill sketchProtectionBadge;
        private bool syncingSketchProtection;

        private void BuildSketchProtection()
        {
            var page = SketchUi.Page(this, tableLayoutPanel1, "Bảo vệ", "Quản lý các tính năng bảo vệ chủ động.");
            var summary = SketchUi.Card("protectionSummary");
            var shield = new PictureBox { Image = UiIcons.BrandShield(30), SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Theme.GreenTint };
            sketchProtectionTitle = SketchUi.Label("", Theme.BoldFont, Theme.TextDark);
            sketchProtectionHint = SketchUi.Label("", Theme.SmallFont, Theme.TextGray);
            var status = new UiPill("Bảo vệ", Theme.PillKind.Ok);
            sketchProtectionBadge = status;
            summary.Controls.AddRange(new System.Windows.Forms.Control[] { shield, sketchProtectionTitle, sketchProtectionHint, status });
            page.Canvas.Controls.Add(summary);
            var update = SketchUi.Button("Làm mới trạng thái", Theme.BtnRole.Neutral, delegate
            {
                try { SyncProtectionStats(); SyncSketchProtection(); }
                catch (Exception ex) { AppDialog.Show(ex.Message, "Bảo vệ", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            });
            page.Canvas.Controls.Add(update);
            var cards = new List<UiCard>();
            var headings = new List<Label>();
            var notes = new List<Label>();
            var icons = new List<PictureBox>();
            string[] descriptions = {
                "Giám sát tệp mới hoặc thay đổi trong máy.", "Kiểm tra lại tệp trước khi khôi phục từ cách ly.",
                "Quét ổ USB ngay khi được kết nối.", "Kiểm tra tệp tải về từ Internet.",
                "Theo dõi tiến trình có hành vi đáng ngờ.", "Kiểm tra tệp mới trong thư mục Startup.",
                "Chuyển tệp nguy hiểm vào khu vực cách ly.", "Thông báo khi phát hiện mối đe dọa.",
                "Tự kiểm tra cập nhật chữ ký khi mở ứng dụng.", "Tra cứu tệp nghi ngờ bằng mã băm." };
            for (int i = 0; i < Catalog.GetLength(0); i++)
            {
                string key = Catalog[i, 0];
                var card = SketchUi.Card("feature_" + key);
                var icon = new PictureBox { Image = UiIcons.ShieldSearch(18, Theme.Blue), SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Theme.BlueFaint };
                var title = SketchUi.Label(Catalog[i, 1], Theme.BoldFont, Theme.TextDark);
                var note = SketchUi.Label(descriptions[i], Theme.SmallFont, Theme.TextGray);
                var state = SketchUi.Label("", Theme.SmallFont, Theme.Green);
                var toggle = new UiSwitch { Name = "switch_" + key, AccessibleName = Catalog[i, 1], Tag = key };
                toggle.CheckedChanged += delegate(object sender, EventArgs e)
                {
                    if (syncingSketchProtection) return;
                    var selected = (UiSwitch)sender;
                    string feature = (string)selected.Tag;
                    if (feature == "vtq" && selected.Checked && !VirusTotalClient.IsConfigured)
                    {
                        syncingSketchProtection = true;
                        selected.Checked = false;
                        syncingSketchProtection = false;
                        var main = FindForm() as FrmMain;
                        if (main != null) main.MoCaiDatVirusTotal();
                        return;
                    }
                    SetState(feature, selected.Checked);
                    SyncRealFeaturesUi();
                    SyncProtectionStats();
                };
                sketchSwitches[key] = toggle;
                sketchFeatureStates[key] = state;
                card.Controls.AddRange(new System.Windows.Forms.Control[] { icon, title, note, state, toggle });
                page.Canvas.Controls.Add(card);
                cards.Add(card); headings.Add(title); notes.Add(note); icons.Add(icon);
            }
            var footer = SketchUi.Note(page.Canvas, "Thiết lập bảo vệ được áp dụng ngay và đồng bộ với Cài đặt.");
            page.Arrange = delegate(int width)
            {
                int columns = width >= 620 ? 2 : 1;
                int cardWidth = (width - (columns - 1) * 14) / columns;
                page.Title.Width = Math.Max(140, width - 180);
                SketchUi.Place(update, page.Canvas, Math.Max(0, width - 162), 4, 162, 40);
                SketchUi.Place(summary, page.Canvas, 0, 86, width, 86);
                shield.SetBounds(18, 17, 50, 50);
                sketchProtectionTitle.SetBounds(84, 15, Math.Max(100, width - 208), 27);
                sketchProtectionHint.SetBounds(84, 45, Math.Max(100, width - 208), 26);
                status.Location = new Point(Math.Max(0, width - status.Width - 20), 29);
                for (int i = 0; i < cards.Count; i++)
                {
                    SketchUi.Place(cards[i], page.Canvas, (i % columns) * (cardWidth + 14), 188 + (i / columns) * 120, cardWidth, 106);
                    icons[i].SetBounds(16, 16, 30, 30);
                    headings[i].SetBounds(58, 13, Math.Max(120, cardWidth - 118), 27);
                    notes[i].SetBounds(58, 43, Math.Max(120, cardWidth - 80), 31);
                    sketchFeatureStates[Catalog[i, 0]].SetBounds(58, 77, Math.Max(120, cardWidth - 80), 20);
                    sketchSwitches[Catalog[i, 0]].Location = new Point(cardWidth - 56, 14);
                }
                int end = 188 + ((cards.Count + columns - 1) / columns) * 120;
                footer.SetBounds(0, end + 2, width, 30);
                page.SetExtent(end + 38);
            };
            page.Reflow();
            SyncSketchProtection();
        }

        private void SyncSketchProtection()
        {
            if (sketchProtectionTitle == null) return;
            syncingSketchProtection = true;
            int onCount = 0;
            foreach (var pair in sketchSwitches)
            {
                bool on = IsOn(pair.Key);
                if (on) onCount++;
                pair.Value.Checked = on;
                sketchFeatureStates[pair.Key].Text = on ? "Đang bật" : (pair.Key == "vtq" && !VirusTotalClient.IsConfigured ? "Chưa có API key · Cần cấu hình" : "Đang tắt");
                sketchFeatureStates[pair.Key].ForeColor = on ? Theme.Green : Theme.TextGray;
            }
            syncingSketchProtection = false;
            sketchProtectionTitle.Text = RealTimeProtection.IsDegraded ? "Bảo vệ suy giảm - cần kiểm tra" :
                RealTimeProtection.IsRunning ? "Bảo vệ thời gian thực đang bật" : "Bảo vệ thời gian thực đang tắt";
            sketchProtectionBadge.Set(RealTimeProtection.IsRunning && !RealTimeProtection.IsDegraded ? "Đang bảo vệ" : "Cần chú ý",
                RealTimeProtection.IsRunning && !RealTimeProtection.IsDegraded ? Theme.PillKind.Ok : Theme.PillKind.Warn);
            sketchProtectionHint.Text = onCount + "/" + sketchSwitches.Count + " tính năng hoạt động · " + lblDatabaseValue2.Text;
        }
    }

    public partial class UcLichSu
    {
        private void BuildSketchHistory()
        {
            var page = SketchUi.Page(this, tlpPage, "Lịch sử", "Theo dõi các lần quét và cảnh báo bảo vệ.");
            var export = SketchUi.Button("Xuất CSV", Theme.BtnRole.Neutral, MiXuatCsv_Click);
            var card = SketchUi.Card("historyList");
            var footer = SketchUi.Note(page.Canvas, "Nhấp vào Xem để mở chi tiết của phiên quét. Menu mở rộng có cảnh báo và nhật ký cập nhật.");
            page.Canvas.Controls.AddRange(new System.Windows.Forms.Control[] { export, card });
            SketchUi.Grid(dgvHistory);
            page.Arrange = delegate(int width)
            {
                page.Title.Width = Math.Max(140, width - 190);
                SketchUi.Place(export, page.Canvas, width - 164, 4, 118, 40);
                SketchUi.Place(btnMore, page.Canvas, width - 38, 4, 38, 40);
                SketchUi.Place(card, page.Canvas, 0, 86, width, 478);
                int fieldWidth = Math.Max(110, (width - 156) / 3);
                SketchUi.Place(tlpTuNgay, card, 18, 16, fieldWidth - 12, 64);
                SketchUi.Place(tlpDenNgay, card, 18 + fieldWidth, 16, fieldWidth - 12, 64);
                SketchUi.Place(tlpLoaiQuet, card, 18 + fieldWidth * 2, 16, fieldWidth - 12, 64);
                SketchUi.Place(btnLoc, card, width - 104, 36, 86, 38);
                Theme.StyleButton(btnLoc, Theme.BtnRole.Primary);
                SketchUi.Place(pnlGrid, card, 1, 94, width - 2, 320);
                SketchUi.Place(lblTotal, card, 18, 426, Math.Max(100, width - 210), 34);
                SketchUi.Place(btnPrev, card, width - 170, 424, 34, 36);
                SketchUi.Place(lblPage, card, width - 128, 424, 68, 36);
                SketchUi.Place(btnNext, card, width - 52, 424, 34, 36);
                footer.SetBounds(0, 578, width, 36);
                page.SetExtent(620);
                PositionEmptyLabel();
            };
            page.Reflow();
        }
    }

    public partial class UcCachLy
    {
        private Label sketchQuarantineStatus;
        private Label sketchQuarantineEmpty;

        private void BuildSketchQuarantine()
        {
            var page = SketchUi.Page(this, tableLayoutPanel1, "Cách ly", "Các tệp được giữ riêng để bảo vệ máy tính.");
            var info = SketchUi.Card("quarantineNotice");
            info.FillColor = Theme.BlueFaint; info.ShowBorder = false;
            var message = SketchUi.Label("Tệp cách ly không thể chạy. Chỉ khôi phục khi bạn chắc chắn tệp an toàn.", Theme.BodyFont, Theme.TextGray);
            info.Controls.Add(message);
            var card = SketchUi.Card("quarantineList");
            var title = SketchUi.Label("Tệp đang cách ly", Theme.CardHeadFont, Theme.TextDark);
            sketchQuarantineStatus = SketchUi.Label("", Theme.SmallFont, Theme.TextGray);
            sketchQuarantineEmpty = SketchUi.Label("Chưa có tệp trong khu vực cách ly.", Theme.BodyFont, Theme.TextGray);
            sketchQuarantineEmpty.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.AddRange(new System.Windows.Forms.Control[] { title, sketchQuarantineEmpty });
            page.Canvas.Controls.AddRange(new System.Windows.Forms.Control[] { info, card, sketchQuarantineStatus });
            SketchUi.Grid(dgvQuarantine);
            colPick.Width = 38;
            colFileName.FillWeight = 25;
            colOriginaPath.FillWeight = 30;
            colThreatName.FillWeight = 24;
            colDetectedTime.FillWeight = 22;
            colFileSize.FillWeight = 12;
            var details = new DataGridViewButtonColumn { Name = "colQuarantineDetails", HeaderText = "",
                Text = "Chi tiết", UseColumnTextForButtonValue = true, FillWeight = 14, FlatStyle = FlatStyle.Flat };
            dgvQuarantine.Columns.Add(details);
            dgvQuarantine.CellContentClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0 || e.ColumnIndex != details.Index) return;
                var item = dgvQuarantine.Rows[e.RowIndex].Tag as QuarantineRow;
                if (item == null) return;
                using (var dialog = new Form { Text = "Chi tiết tệp cách ly", Size = new Size(1120, 790), MinimumSize = new Size(1000, 680),
                    StartPosition = FormStartPosition.CenterParent, BackColor = Theme.DashboardBg, Padding = new Padding(24), Font = Theme.DefaultUiFont })
                {
                    var view = new UcTongQuan { Dock = DockStyle.Fill };
                    dialog.Controls.Add(view);
                    view.OpenQuarantinedFileDetails(item.OriginalPath, item.ThreatName, item.FileSizeBytes ?? 0,
                        item.DetectedAt ?? DateTime.MinValue, item.FileMD5, item.FileSHA1, item.FileSHA256);
                    dialog.ShowDialog(FindForm());
                }
            };
            page.Arrange = delegate(int width)
            {
                page.Title.Width = Math.Max(140, width - 140);
                SketchUi.Place(btnRefreshQuarantine, page.Canvas, width - 120, 4, 120, 40);
                SketchUi.Place(info, page.Canvas, 0, 86, width, 50);
                message.SetBounds(17, 7, width - 34, 36);
                SketchUi.Place(card, page.Canvas, 0, 154, width, 432);
                title.SetBounds(18, 13, Math.Max(100, width - 180), 32);
                SketchUi.Place(lblTotalFilesValue, card, width - 76, 15, 58, 30);
                lblTotalFilesValue.TextAlign = ContentAlignment.MiddleRight;
                lblTotalFilesValue.Font = Theme.BoldFont;
                SketchUi.Place(dgvQuarantine, card, 1, 58, width - 2, 302);
                sketchQuarantineEmpty.SetBounds(20, 176, width - 40, 40);
                sketchQuarantineEmpty.BringToFront();
                SketchUi.Place(btnRestore, card, width - 445, 377, 118, 38);
                SketchUi.Place(btnRestoreAll, card, width - 319, 377, 154, 38);
                SketchUi.Place(btnDeletePermanent, card, width - 157, 377, 139, 38);
                sketchQuarantineStatus.SetBounds(0, 599, width, 42);
                page.SetExtent(651);
            };
            page.Reflow();
            SyncSketchQuarantine();
        }

        private void SyncSketchQuarantine()
        {
            if (sketchQuarantineStatus == null) return;
            sketchQuarantineEmpty.Visible = dgvQuarantine.Rows.Count == 0;
            sketchQuarantineStatus.Text = dgvQuarantine.Rows.Count == 0 ? "Danh sách cách ly sẽ cập nhật sau khi xử lý tệp được phát hiện." : "Tệp được kiểm tra lại trước khi khôi phục.";
        }
    }

    public partial class UcCaiDat
    {
        private SketchPage sketchSettingsPage;
        private readonly List<UiCard> sketchSettingsCards = new List<UiCard>();

        private void BuildSketchSettings()
        {
            var page = SketchUi.Page(this, tableLayoutPanel1, "Cài đặt", "Cá nhân hóa ứng dụng và các cơ chế bảo vệ.");
            sketchSettingsPage = page;
            string[] headings = { "Khởi động & đồng bộ", "Dữ liệu & công cụ" };
            string[] notes = { "Tùy chỉnh cách Antivirus hoạt động trên máy tính.", "Thông tin ứng dụng, dữ liệu quét và các công cụ kiểm thử." };
            for (int i = 0; i < 2; i++)
            {
                var card = SketchUi.Card("settingsPanel" + i);
                var title = SketchUi.Label(headings[i], Theme.CardHeadFont, Theme.TextDark);
                var note = SketchUi.Label(notes[i], Theme.SmallFont, Theme.TextGray);
                card.Controls.AddRange(new System.Windows.Forms.Control[] { title, note });
                card.Resize += delegate(object sender, EventArgs e)
                {
                    var c = (UiCard)sender;
                    title.SetBounds(22, 15, c.Width - 44, 28);
                    note.SetBounds(22, 44, c.Width - 44, 26);
                };
                page.Canvas.Controls.Add(card);
                sketchSettingsCards.Add(card);
            }
            CheckBox[] checks = { chkAutoStart, chkAutoUpdate, chkShowNotification, chkSendSamples };
            string[] titles = { "Khởi động cùng Windows", "Tự động cập nhật chữ ký", "Hiển thị thông báo", "Gửi mẫu tệp nghi ngờ" };
            string[] hints = { "Tự mở ứng dụng sau khi đăng nhập.", "Kiểm tra cập nhật khi mở ứng dụng.", "Thông báo khi phát hiện mối đe dọa.", "Cho phép gửi mẫu để hỗ trợ phân tích." };
            var general = sketchSettingsCards[0];
            for (int i = 0; i < checks.Length; i++)
            {
                var row = SketchUi.SettingRow(checks[i], titles[i], hints[i]);
                row.Location = new Point(22, 82 + i * 78);
                general.Controls.Add(row);
                general.Resize += delegate { row.Width = general.Width - 44; };
            }
            // API key VirusTotal được cấu hình trực tiếp trong .env, không hiển thị trên UI.
            txtVtKey.Visible = btnSaveVtKey.Visible = btnClearVtKey.Visible = false;
            lblVtStatus.Visible = lblVtHint.Visible = chkVtAutoQuery.Visible = false;
            var data = sketchSettingsCards[1];
            data.Resize += delegate
            {
                Label[] values = { lblAppVersion, lblDbUpdate, lblQuarantined, lblDataPath };
                for (int i = 0; i < values.Length; i++)
                {
                    values[i].Font = Theme.BodyFont;
                    SketchUi.Place(values[i], data, 22, 86 + i * 60, data.Width - 44, 46);
                }
                SketchUi.Place(btnOpenData, data, 22, 346, 184, 40);
                SketchUi.Place(btnClearCache, data, 218, 346, 162, 40);
                SketchUi.Place(btnSamples, data, 392, 346, 164, 40);
            };
            // Công cụ bảo mật bổ sung: mọi nút mở tính năng thực tế, không phải placeholder.
            var tools = SketchUi.Card("settingsExtraTools");
            var toolsTitle = SketchUi.Label("Công cụ bảo mật và quản trị", Theme.CardHeadFont, Theme.TextDark);
            var toolsHint = SketchUi.Label("Quản lý chữ ký, ngoại lệ, lập lịch quét và kiểm tra Windows.", Theme.SmallFont, Theme.TextGray);
            tools.Controls.Add(toolsTitle);
            tools.Controls.Add(toolsHint);
            string[] toolNames = { "Quản lý chữ ký", "Lịch quét tự động", "Ngoại lệ heuristic", "Kiểm tra Windows", "Xuất báo cáo HTML", "Tệp chưa kiểm tra", "Đối soát cách ly", "Đồng bộ lịch sử", "Phân tích EXE/DLL" };
            Action[] toolActions = {
                () => { using (var f = new SignatureManagementForm()) f.ShowDialog(FindForm()); },
                () => ExactOpenScheduleManager(),
                () => { using (var f = new ScanExclusionForm()) f.ShowDialog(FindForm()); },
                () => { using (var f = new SecurityHealthForm()) f.ShowDialog(FindForm()); },
                () => ExportHtmlReport(),
                () => { using (var f = new ScanIssuesForm()) f.ShowDialog(FindForm()); },
                () => { using (var f = new RecoveryJournalForm()) f.ShowDialog(FindForm()); },
                () => {
                    var owner = FindForm();
                    System.Threading.Tasks.Task.Run(() => Services.OfflineHistoryQueue.Replay(100))
                        .ContinueWith(t => {
                            if (owner == null || owner.IsDisposed || !owner.IsHandleCreated) return;
                            try { owner.BeginInvoke((MethodInvoker)(() => AppDialog.Show(owner,
                                "Đã đồng bộ: " + (t.IsFaulted ? 0 : t.Result) +
                                " bản ghi. Còn chờ: " + Services.OfflineHistoryQueue.PendingCount +
                                ". Nếu vẫn còn chờ, kiểm tra kết nối SQL Server.", "Đồng bộ lịch sử",
                                MessageBoxButtons.OK, MessageBoxIcon.Information))); }
                            catch (Exception) { }
                        });
                },
                () => { using (var f = new PeInspectForm()) f.ShowDialog(FindForm()); }
            };
            var toolButtons = new Button[toolNames.Length];
            for (int i = 0; i < toolNames.Length; i++)
            {
                int index = i;
                var button = new Button {
                    Text = toolNames[i], FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                    BackColor = Color.White, ForeColor = Theme.TextDark,
                    Font = Theme.BoldFont, Height = 46
                };
                button.FlatAppearance.BorderColor = Theme.CardBorder;
                button.Click += delegate { toolActions[index](); };
                tools.Controls.Add(button);
                toolButtons[i] = button;
            }
            tools.Resize += delegate
            {
                int w = Math.Max(180, tools.Width - 44);
                toolsTitle.SetBounds(22, 13, w, 32);
                toolsHint.SetBounds(22, 46, w, 28);
                int cols = w >= 700 ? 4 : w >= 450 ? 2 : 1;
                int gap = 14;
                int cell = Math.Max(1, (w - (cols - 1) * gap) / cols);
                for (int i = 0; i < toolButtons.Length; i++)
                    toolButtons[i].SetBounds(22 + (i % cols) * (cell + gap),
                        94 + (i / cols) * 59, cell, 46);
            };
            page.Canvas.Controls.Add(tools);
            sketchSettingsCards.Add(tools);
            page.Arrange = delegate(int width)
            {
                if (sketchSettingsCards.Count > 0) sketchSettingsCards[0].SetBounds(0, 88, width, 418);
                if (sketchSettingsCards.Count > 1) sketchSettingsCards[1].SetBounds(0, 524, width, 418);
                if (sketchSettingsCards.Count > 2)
                    sketchSettingsCards[2].SetBounds(0, 960, width,
                        width >= 744 ? 288 : width >= 494 ? 452 : 684);
                ArrangeSketchSettings();
            };
            page.Reflow();
        }

        private void ExportHtmlReport()
        {
            using (var dialog = new SaveFileDialog {
                Title = "Xuất báo cáo lịch sử quét HTML", Filter = "Trang HTML (*.html)|*.html",
                FileName = "BaoCaoQuet_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html" })
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                bool paths = AppDialog.Show(FindForm(),
                    "Có xuất đường dẫn tệp/thư mục riêng tư vào báo cáo không?\nChọn Không để ẩn đường dẫn.",
                    "Quyền riêng tư báo cáo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                try
                {
                    int count = Services.ScanReportExporter.ExportRecentHtml(dialog.FileName, paths);
                    AppDialog.Show(FindForm(), "Đã xuất " + count + " phiên quét vào tệp HTML.",
                        "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    AppDialog.Show(FindForm(), "Không xuất được báo cáo: " + ex.Message,
                        "Lỗi báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>Mở cùng hộp quản lý lịch cho trang Cài đặt và Tổng quan.</summary>
        internal void ExactOpenScheduleManager()
        {
            // Lịch quét và công cụ thuộc trang Cài đặt không hỏi đăng nhập quản trị.
            using (var f = new ScanScheduleForm()) f.ShowDialog(FindForm());
        }

        private void ArrangeSketchSettings()
        {
            if (sketchSettingsPage == null || sketchSettingsCards.Count < 2) return;
            sketchSettingsCards[0].Visible = true;
            sketchSettingsCards[1].Visible = true;
            int end = sketchSettingsCards.Count > 2 ? 960 + sketchSettingsCards[2].Height : 524 + sketchSettingsCards[1].Height;
            int width = sketchSettingsPage.Canvas.Width;
            SketchUi.Place(lblSavedAt, sketchSettingsPage.Canvas, 0, end + 20, Math.Max(120, width - 370), 42);
            lblSavedAt.Font = Theme.SmallFont;
            SketchUi.Place(btnResetDefaults, sketchSettingsPage.Canvas, width - 352, end + 20, 194, 40);
            SketchUi.Place(btnSaveSettings, sketchSettingsPage.Canvas, width - 146, end + 20, 146, 40);
            sketchSettingsPage.SetExtent(end + 84);
        }

        internal void OpenVirusTotalSettings()
        {
            ArrangeSketchSettings();
        }
    }
}
