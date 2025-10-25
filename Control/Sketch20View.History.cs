using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    // Lịch sử: bố cục theo ảnh tham chiếu, dữ liệu từ UcLichSu / ScanHistoryStore.
    // Một trang gồm 10/20/50 bản ghi; chiều cao bảng tự co giãn và cuộn độc lập.
    internal sealed partial class Sketch20View
    {
        private int historyRowsScrollPixels;
        private int historyLastRemotePage = -1;
        private int historyLastPageSize = -1;
        private static readonly string[] HistoryPageChoices = { "10", "20", "50" };
        private static readonly Color HistoryEdge = Color.FromArgb(219, 231, 246);
        private static readonly Color HistoryPale = Color.FromArgb(232, 243, 255);

        private void HistoryRedesigned()
        {
            if (!historyFilterInitialized && Model != null && !Model.Sample)
            {
                from = Model.HistoryFrom.Date;
                to = Model.HistoryTo.Date;
                scanFilter = NormalizeHistoryFilter(Model.HistoryFilter);
                historyFilterInitialized = true;
            }

            int w = layoutWidth;
            bool narrow = w < 790;
            bool medium = w < 1090;
            int titleY = 12;
            int filterH = narrow ? 267 : medium ? 206 : 178;
            GradientCard("history-main-filter", 0, titleY, w, filterH,
                Color.White, Color.FromArgb(239, 248, 255), HistoryEdge, 7);
            Card("history-title-icon", 20, titleY + 12, 52, 52,
                HistoryPale, Color.Transparent, 8);
            Icon("history", 33, titleY + 25, 26, Sketch20Style.Blue);
            Text("Lịch sử", 88, titleY + 15, w - 108, 29,
                21.8f, Sketch20Style.Ink, true);
            Text("Xem lại lịch sử quét và các hoạt động xử lý mối đe dọa trên máy tính.",
                88, titleY + 48, w - 110, 23, 11.6f, Sketch20Style.Muted);
            Line(16, titleY + 77, w - 32);

            const int gap = 12;
            int labelY = titleY + 85;
            int inputY = titleY + 110;
            int dateX = 20, dateW, secondX, typeX, typeW, actionsX, actionY;
            const int applyW = 108, refreshW = 116, exportW = 126;
            int actionsWidth = applyW + refreshW + exportW + 2 * gap;
            if (!medium)
            {
                int fieldWidth = w - 40 - actionsWidth - 16;
                dateW = Math.Max(150, (fieldWidth - 2 * gap) / 3);
                typeW = Math.Max(146, fieldWidth - 2 * dateW - 2 * gap);
                secondX = dateX + dateW + gap;
                typeX = secondX + dateW + gap;
                actionsX = typeX + typeW + 16;
                actionY = inputY;
            }
            else if (!narrow)
            {
                dateW = Math.Max(120, (w - 64) / 3);
                secondX = dateX + dateW + gap;
                typeX = secondX + dateW + gap;
                typeW = Math.Max(105, w - typeX - 20);
                actionsX = Math.Max(dateX, w - 20 - actionsWidth);
                actionY = titleY + 158;
            }
            else
            {
                dateW = Math.Max(80, (w - 52) / 2);
                secondX = dateX + dateW + gap;
                typeX = dateX;
                typeW = w - 40;
                actionsX = Math.Max(12, w - 16 - actionsWidth);
                actionY = titleY + 217;
            }
            Text("Từ ngày", dateX, labelY, dateW, 20, 11.3f, Sketch20Style.Muted, true);
            Text("Đến ngày", secondX, labelY, dateW, 20, 11.3f, Sketch20Style.Muted, true);
            DateBox("history-from", from, dateX, inputY, dateW, d => from = d.Date);
            DateBox("history-to", to, secondX, inputY, dateW, d => to = d.Date);
            if (narrow)
            {
                Text("Loại quét", typeX, titleY + 152, typeW, 20, 11.3f, Sketch20Style.Muted, true);
                inputY = titleY + 173;
            }
            else Text("Loại quét", typeX, labelY, typeW, 20, 11.3f, Sketch20Style.Muted, true);
            string[] types = { "Tất cả", "Quét nhanh", "Quét thư mục", "Quét tệp", "Quét toàn bộ", "Quét tùy chỉnh" };
            SelectBox("history-filter", types, Math.Max(0, Array.IndexOf(types, scanFilter)),
                typeX, inputY, typeW, i => { scanFilter = types[i]; }, 42);
            var apply = Button("filter-history", "Lọc", actionsX, actionY, applyW,
                () =>
                {
                    if (from.Date > to.Date)
                    {
                        AppDialog.Show("Từ ngày không được sau Đến ngày.", "Lọc lịch sử",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    historyRowsScrollPixels = 0;
                    historyLastRemotePage = -1;
                    historyOffset = 0;
                    if (Actions.FilterHistory != null) Actions.FilterHistory(from, to, scanFilter);
                    if (Model.Sample)
                        Model.History = Model.History.Where(h => scanFilter == "Tất cả" || h.Type == scanFilter).ToList();
                    RefreshModel(true);
                }, "search", true, false, 42);
            apply.SquareCorners = true;
            var refresh = Button("history-refresh", "Làm mới", actionsX + applyW + gap,
                actionY, refreshW, () => { historyRowsScrollPixels = 0; Command("history-refresh"); },
                "refresh", false, false, 42);
            refresh.SquareCorners = true;
            var export = Button("export-csv", "Xuất CSV", actionsX + applyW + refreshW + 2 * gap,
                actionY, exportW, () => Command("export"), "download", false, false, 42);
            export.SquareCorners = true;

            int sectionY = titleY + filterH + 15;
            int viewportH = Math.Max(290, viewport.ClientSize.Height - AdvancedCanvasTop - sectionY - 14);
            Card("history-journal-card", 0, sectionY, w, viewportH, Color.White, HistoryEdge, 8);
            Card("history-journal-icon", 20, sectionY + 15, 52, 52, HistoryPale, Color.Transparent, 8);
            Icon("history", 33, sectionY + 28, 26, Sketch20Style.Blue);
            Text("Nhật ký hoạt động", 88, sectionY + 17,
                Math.Max(155, w - (narrow ? 305 : medium ? 290 : 468)), 31, 19.8f, Sketch20Style.Ink, true);
            if (!narrow)
                Text("Theo dõi các lần quét và kết quả xử lý gần đây.", 88, sectionY + 50,
                    Math.Max(150, w - (medium ? 305 : 465)), 24, 11f, Sketch20Style.Muted);
            int total = Model.Sample ? Model.History.Count : Math.Max(Model.History.Count, Model.HistoryTotal);
            if (!medium)
                Text("Tổng " + total.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " lần quét",
                    w - 365, sectionY + 22, 166, 37, 11.8f, Sketch20Style.Muted, false, false, true);
            HeaderSelectBox("history-sort", new[] { "Mới nhất trước", "Cũ nhất trước" },
                historyOldest ? 1 : 0, w - (narrow ? 191 : 186), sectionY + 23,
                narrow ? 173 : 164, 41, 11.2f, choice =>
                {
                    historyOldest = choice == 1;
                    historyRowsScrollPixels = 0;
                    if (Actions.SortHistory != null) Actions.SortHistory(historyOldest);
                    if (Model.Sample)
                        Model.History = historyOldest
                            ? Model.History.OrderBy(h => h.Date + h.Time).ToList()
                            : Model.History.OrderByDescending(h => h.Date + h.Time).ToList();
                    RefreshModel(true);
                });

            int rowX = 20, rowW = w - 40;
            int headerY = sectionY + (narrow ? 79 : 92);
            const int tableHeaderH = 45, rowH = 75;
            Card("history-header-strip", rowX, headerY, rowW, tableHeaderH,
                HistoryPale, Color.Transparent, 3);
            HistoryDrawColumnHeaders(rowX, rowW, headerY, tableHeaderH);
            int rowsY = headerY + tableHeaderH;
            int footerY = sectionY + viewportH - 58;
            int rowsHeight = Math.Max(85, footerY - rowsY - 7);
            int rowCount = Model.History.Count;
            int pageSize = Math.Max(1, Model.HistoryPageSize);
            int current = 1;
            string[] parts = (Model.HistoryPage ?? "1 / 1").Split('/');
            if (parts.Length > 0) int.TryParse(parts[0].Trim(), out current);
            current = Math.Max(1, current);
            if (Model.Sample)
            {
                // DTO mẫu không có backend phân trang: chỉ hiển thị phần đầu hợp lệ.
                rowCount = Math.Min(rowCount, pageSize);
                current = 1;
            }
            if (historyLastRemotePage != current || historyLastPageSize != pageSize)
            {
                historyRowsScrollPixels = 0;
                historyLastRemotePage = current;
                historyLastPageSize = pageSize;
            }
            int scrollTotal = rowCount * rowH;
            int maxScroll = Math.Max(0, scrollTotal - rowsHeight);
            historyRowsScrollPixels = Math.Max(0, Math.Min(historyRowsScrollPixels, maxScroll));
            int scrollW = maxScroll > 0 ? SystemInformation.VerticalScrollBarWidth : 0;
            var clip = new Panel
            {
                Name = "history-row-viewport", BackColor = Color.White,
                Bounds = new Rectangle(rowX, rowsY + bodyOffset, rowW - scrollW, rowsHeight)
            };
            Theme.BatDoubleBuffer(clip);
            Canvas.Controls.Add(clip);
            for (int i = 0; i < rowCount; i++)
            {
                Sketch20History h = Model.History[i];
                int modelIndex = h.Index;
                var row = new HistoryJournalRow(h, rowW - scrollW, rowH, w < 975);
                row.Name = "history-entry-" + i;
                row.Tag = i;
                row.Location = new Point(0, i * rowH - historyRowsScrollPixels);
                row.DetailRequested += () =>
                {
                    if (Actions.Command != null) Actions.Command("history-detail:" + modelIndex);
                    RefreshModel(true);
                    if (Model.Sample && Model.Detail != null) Go("details");
                };
                clip.Controls.Add(row);
                toolTips.SetToolTip(row, h.Type + "\n" + h.Scope + "\n" + h.Files + " tệp — " + h.Threats + " mối đe dọa");
            }
            if (rowCount == 0)
            {
                var emptyTitle = new Label
                {
                    Name = "history-empty-message",
                    Text = "Không có lịch sử quét phù hợp",
                    ForeColor = Sketch20Style.Muted,
                    BackColor = Color.White,
                    Font = new Font(Theme.UiFontFamily, 13.0f, FontStyle.Bold),
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Bounds = new Rectangle(12, Math.Max(0, (rowsHeight - 42) / 2),
                        Math.Max(80, clip.Width - 24), 42)
                };
                clip.Controls.Add(emptyTitle);
            }
            if (maxScroll > 0)
            {
                var bar = new VScrollBar
                {
                    Name = "history-rows-scroll", Minimum = 0,
                    Maximum = Math.Max(0, scrollTotal - 1),
                    LargeChange = rowsHeight, SmallChange = rowH,
                    Bounds = new Rectangle(rowX + rowW - scrollW, rowsY + bodyOffset, scrollW, rowsHeight),
                    Value = historyRowsScrollPixels
                };
                bar.ValueChanged += (sender, args) =>
                {
                    historyRowsScrollPixels = bar.Value;
                    foreach (HistoryJournalRow row in clip.Controls.OfType<HistoryJournalRow>())
                        row.Top = (int)row.Tag * rowH - historyRowsScrollPixels;
                };
                Canvas.Controls.Add(bar);
            }
            Line(rowX, footerY - 5, rowW);
            int start = rowCount == 0 ? 0 : (current - 1) * pageSize + 1;
            int end = rowCount == 0 ? 0 : Math.Min(total, start + rowCount - 1);
            Text("Hiển thị " + start + "–" + end + " / " + total + " bản ghi",
                rowX + 5, footerY + 7, Math.Min(rowW / 2, 250), 36,
                11.8f, Sketch20Style.Muted);
            HistoryDrawPager(w, footerY + 7, current, Math.Max(1, (total + pageSize - 1) / pageSize), pageSize);
            ContentHeight = sectionY + viewportH + 6;
        }

        private void HistoryDrawColumnHeaders(int x, int width, int y, int h)
        {
            float[] ends = width < 975 ? new float[] { .16f, .50f, .68f, .84f, 1f }
                : new float[] { .14f, .43f, .60f, .73f, .87f, 1f };
            string[] names = width < 975
                ? new[] { "THỜI GIAN", "LOẠI QUÉT", "THỐNG KÊ / THỜI GIAN", "KẾT QUẢ", "THAO TÁC" }
                : new[] { "THỜI GIAN", "LOẠI QUÉT", "THỐNG KÊ", "THỜI GIAN QUÉT", "KẾT QUẢ", "THAO TÁC" };
            int previous = 0;
            for (int i = 0; i < names.Length; i++)
            {
                int next = i == names.Length - 1 ? width : (int)(ends[i] * width);
                // Thời gian / Loại quét căn trái; các cột số liệu, kết quả và thao tác căn giữa.
                bool centerColumn = i >= 2;
                int cellWidth = next - previous;
                if (centerColumn)
                {
                    // Đặt cả nhãn ở giữa cột (Text() chỉ có tham số căn phải).
                    float fontPixels = 10.5f * ReferenceTextScale;
                    int natural = Sketch20Style.Measure(names[i], fontPixels, true);
                    int offset = Math.Max(0, (cellWidth - natural) / 2);
                    Text(names[i], x + previous + offset, y,
                        Math.Max(8, cellWidth - offset), h, 10.5f, Sketch20Style.Muted, true);
                }
                else
                    Text(names[i], x + previous + 10, y,
                        Math.Max(8, cellWidth - 13), h, 10.5f, Sketch20Style.Muted, true);
                previous = next;
            }
        }

        private void HistoryDrawPager(int width, int y, int currentPage, int totalPages, int size)
        {
            bool narrow = width < 970;
            int btnW = narrow ? 36 : 39, gap = 5;
            List<int> tokens = new List<int>();
            if (narrow) tokens.Add(currentPage);
            else if (totalPages <= 5)
                for (int n = 1; n <= totalPages; n++) tokens.Add(n);
            else if (currentPage <= 3)
                tokens.AddRange(new[] { 1, 2, 3, -1, totalPages });
            else if (currentPage >= totalPages - 2)
                tokens.AddRange(new[] { 1, -1, totalPages - 2, totalPages - 1, totalPages });
            else
                tokens.AddRange(new[] { 1, -1, currentPage - 1, currentPage, currentPage + 1, -1, totalPages });
            int pageWidth = (tokens.Count + 2) * (btnW + gap) + 8;
            int left = Math.Max(16, width - 18 - pageWidth);
            if (width > 750)
            {
                Text("Số dòng/trang:", Math.Max(228, left - 224), y + 3, 126, 36,
                    11.5f, Sketch20Style.Muted);
                SelectBox("history-size", HistoryPageChoices,
                    Math.Max(0, Array.IndexOf(new[] { 10, 20, 50 }, size)),
                    Math.Max(340, left - 99), y, 80,
                    i => { historyRowsScrollPixels = 0; Command("history-size:" + HistoryPageChoices[i]); }, 38);
            }
            else
            {
                Text("Dòng/trang:", Math.Max(274, left - 180), y + 3, 95, 36,
                    10.8f, Sketch20Style.Muted);
                SelectBox("history-size", HistoryPageChoices,
                    Math.Max(0, Array.IndexOf(new[] { 10, 20, 50 }, size)),
                    left - 83, y, 70,
                    i => { historyRowsScrollPixels = 0; Command("history-size:" + HistoryPageChoices[i]); }, 38);
            }
            int x = left;
            var prev = Button("history-prev", "", x, y, btnW,
                () => { historyRowsScrollPixels = 0; Command("history-prev"); },
                "chevron-left", false, false, 38);
            prev.Enabled = currentPage > 1 && totalPages > 1;
            prev.SquareCorners = true;
            x += btnW + gap;
            foreach (int token in tokens)
            {
                if (token == -1)
                    Text("…", x, y, btnW, 38, 12f, Sketch20Style.Muted, false, false, true);
                else
                {
                    int page = token;
                    var btn = Button("history-page-" + page, page.ToString(), x, y, btnW,
                        () => { historyRowsScrollPixels = 0; Command("history-page:" + page); },
                        "", page == currentPage, false, 38);
                    btn.SquareCorners = true;
                    btn.TextPixels = 12f * ReferenceTextScale;
                }
                x += btnW + gap;
            }
            var next = Button("history-next", "Sau", x, y, btnW,
                () => { historyRowsScrollPixels = 0; Command("history-next"); },
                "", false, false, 38);
            next.TextPixels = 10.5f;
            next.Enabled = currentPage < totalPages;
            next.SquareCorners = true;
        }
    }

    // Dòng lịch sử dùng control riêng, để nội dung cuộn mà tiêu đề và chân trang vẫn cố định.
    internal sealed class HistoryJournalRow : System.Windows.Forms.Control
    {
        private readonly Sketch20History data;
        private readonly bool compact;
        private readonly Rectangle detailsArea;
        internal event Action DetailRequested;
        private static readonly Color Border = Color.FromArgb(228, 237, 248);
        private static readonly Color TextColor = Sketch20Style.Ink;
        internal HistoryJournalRow(Sketch20History entry, int width, int height, bool compactMode)
        {
            data = entry;
            compact = compactMode;
            Size = new Size(width, height);
            BackColor = Color.White;
            Cursor = Cursors.Default;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            // Cột thao tác chỉ còn Xem; căn giữa nút trong toàn bộ bề rộng cột.
            int actionStart = (int)(width * (compact ? .84f : .87f));
            int actionWidth = Math.Max(1, width - actionStart);
            int buttonWidth = Math.Min(105, Math.Max(30, actionWidth - 16));
            detailsArea = new Rectangle(actionStart + (actionWidth - buttonWidth) / 2,
                (height - 33) / 2, buttonWidth, 33);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = detailsArea.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (detailsArea.Contains(e.Location) && DetailRequested != null)
                DetailRequested();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            paintGraphics = g;
            int w = Width, h = Height;
            using (var pen = new Pen(Border)) g.DrawLine(pen, 0, h - 1, w, h - 1);
            float[] ends = compact ? new float[] { .16f, .50f, .68f, .84f, 1f }
                : new float[] { .14f, .43f, .60f, .73f, .87f, 1f };
            int[] col = new int[ends.Length + 1];
            for (int i = 0; i < ends.Length; i++) col[i + 1] = i == ends.Length - 1 ? w : (int)(ends[i] * w);
            Draw(data.Date, col[0] + 10, 12, col[1] - col[0] - 15, 25, 12.2f, true);
            Draw(data.Time, col[0] + 10, 39, col[1] - col[0] - 15, 22, 10.8f, false, Sketch20Style.Muted);

            int ix = col[1] + 8;
            Rectangle icon = new Rectangle(ix, (h - 43) / 2, 43, 43);
            using (var b = new SolidBrush(Sketch20Style.BlueSoft)) g.FillRectangle(b, icon);
            string iconName = (data.Type ?? "").IndexOf("thư", StringComparison.OrdinalIgnoreCase) >= 0 ? "folder"
                : (data.Type ?? "").IndexOf("tệp", StringComparison.OrdinalIgnoreCase) >= 0 ? "file"
                : "bolt";
            Sketch20Style.Icon(g, iconName, new Rectangle(ix + 11, icon.Y + 11, 21, 21), Sketch20Style.Blue);
            int tx = ix + 53, tw = Math.Max(18, col[2] - tx - 8);
            Draw(data.Type, tx, 11, tw, 26, 12f, true);
            string scope = string.IsNullOrWhiteSpace(data.Scope) ? "Vị trí được lựa chọn" : data.Scope;
            Draw(scope, tx, 39, tw, 23, 10.6f, false, Sketch20Style.Muted);

            // Căn giữa cả hai dòng thống kê trong cột tương ứng.
            int statsX = col[2], statsW = Math.Max(1, col[3] - statsX);
            Color risk = data.Risk ? Sketch20Style.Red : Sketch20Style.Green;
            if (compact)
            {
                // Khi thu hẹp, cột gộp thống kê / thời gian vẫn hiển thị đủ 3 dòng.
                DrawCenter(data.Files + " tệp", statsX, 3, statsW, 23, 11.5f, true);
                DrawCenter(data.Threats + " mối đe dọa", statsX, 26, statsW, 22, 11.2f, true, risk);
                DrawCenter(data.Duration, statsX, 49, statsW, 21, 10.9f, false, Sketch20Style.Muted);
            }
            else
            {
                DrawCenter(data.Files + " tệp", statsX, 11, statsW, 25, 11.8f, true);
                DrawCenter(data.Threats + " mối đe dọa", statsX, 39, statsW, 23, 11.5f, true, risk);
                // Căn giữa toàn bộ cụm icon đồng hồ + thời lượng.
                int durationX = col[3], durationW = Math.Max(1, col[4] - col[3]);
                string duration = string.IsNullOrWhiteSpace(data.Duration) ? "—" : data.Duration;
                int textWidth = Sketch20Style.Measure(duration, 11.4f);
                int groupWidth = Math.Min(durationW - 4, 17 + 6 + textWidth);
                int groupX = durationX + Math.Max(0, (durationW - groupWidth) / 2);
                Sketch20Style.Icon(g, "clock", new Rectangle(groupX, 28, 17, 17), Sketch20Style.Muted);
                Draw(duration, groupX + 23, 21, Math.Max(1, groupWidth - 23), 30,
                    11.4f, false, Sketch20Style.Muted);
            }
            int resultColumn = compact ? 3 : 4;
            string result = data.Result ?? (data.Risk ? "Phát hiện" : "An toàn");
            if (result.IndexOf("phát hiện", StringComparison.OrdinalIgnoreCase) >= 0) result = "Phát hiện";
            bool quarantined = result.IndexOf("cách ly", StringComparison.OrdinalIgnoreCase) >= 0;
            bool safe = !data.Risk || quarantined;
            Color badgeBg = safe ? Color.FromArgb(232, 249, 240) : Color.FromArgb(255, 239, 226);
            Color badgeInk = safe ? Color.FromArgb(0, 148, 83) : Color.FromArgb(220, 112, 0);
            int resultWidth = Math.Max(1, col[resultColumn + 1] - col[resultColumn]);
            int badgeWidth = Math.Min(132, Math.Max(30, resultWidth - 16));
            Rectangle badge = new Rectangle(col[resultColumn] + (resultWidth - badgeWidth) / 2,
                (h - 30) / 2, badgeWidth, 30);
            using (var brush = new SolidBrush(badgeBg)) g.FillRectangle(brush, badge);
            Sketch20Style.Text(g, result, new Rectangle(badge.X + 6, badge.Y, badge.Width - 12, 30),
                11.3f, badgeInk, true, false, false, false, true);

            using (var brush = new SolidBrush(Color.FromArgb(235, 245, 255))) g.FillRectangle(brush, detailsArea);
            Sketch20Style.Text(g, "Xem", detailsArea, 11.5f, Sketch20Style.Blue, true,
                false, false, false, true);
        }
        private void Draw(string value, int x, int y, int w, int h, float size, bool bold,
            Color? color = null)
        {
            Sketch20Style.Text(paintGraphics, value, new Rectangle(x, y, w, h),
                size, color ?? TextColor, bold);
        }
        private void DrawCenter(string value, int x, int y, int w, int h, float size, bool bold,
            Color? color = null)
        {
            Sketch20Style.Text(paintGraphics, value, new Rectangle(x, y, Math.Max(1, w), h),
                size, color ?? TextColor, bold, false, false, false, true);
        }
        private Graphics paintGraphics;
    }
}
