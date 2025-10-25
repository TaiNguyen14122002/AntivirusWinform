using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    // Trang Cách ly: giao diện theo bảng, phân trang và cuộn riêng phần thân bảng.
    // Những thao tác Khôi phục/Xóa vẫn dùng UcCachLy và ScanEngine hiện có.
    internal sealed partial class Sketch20View
    {
        private static readonly Color QuarantineBorder = Color.FromArgb(217, 230, 247);
        private static readonly Color QuarantineHeader = Color.FromArgb(233, 244, 255);
        private static readonly Color QuarantineMuted = Color.FromArgb(115, 135, 169);
        private const int QuarantineRowHeight = 76;

        private void QuarantineRedesigned()
        {
            SortQuarantine(Model);
            int w = layoutWidth;
            bool compact = w < 940;
            bool veryCompact = w < 745;
            int bannerY = 10;
            int bannerH = veryCompact ? 92 : 82;
            Card("quarantine-message", 0, bannerY, w, bannerH,
                Color.FromArgb(238, 247, 255), QuarantineBorder, 11);
            Card("quarantine-message-icon", 18, bannerY + 17, 49, 49,
                Color.FromArgb(222, 239, 255), Color.Transparent, 10);
            Icon("info", 33, bannerY + 32, 19, Sketch20Style.Blue);
            Text("Các tệp trong khu vực cách ly đã bị vô hiệu hóa và không thể gây hại cho hệ thống.",
                83, bannerY + 9, Math.Max(120, w - 103), veryCompact ? 36 : 30,
                veryCompact ? 11.5f : 12.0f, Sketch20Style.Ink, true, veryCompact);
            Text("Bạn có thể khôi phục tệp nếu chắc chắn chúng an toàn, hoặc xóa vĩnh viễn nếu không cần thiết.",
                83, bannerY + (veryCompact ? 51 : 43), Math.Max(120, w - 103),
                veryCompact ? 31 : 25, 10.6f, QuarantineMuted, false, veryCompact);

            int listY = bannerY + bannerH + 14;
            int viewportAvailable = Math.Max(550, viewport.ClientSize.Height - AdvancedCanvasTop - 13);
            int actionH = veryCompact ? 116 : 83;
            int actionGap = 13;
            int listH = Math.Max(370, viewportAvailable - listY - actionH - actionGap - 9);
            int titleHeight = 79;
            int pagerH = compact ? 72 : 59;
            int headH = 46;
            int rowX = 16, rowW = Math.Max(360, w - 32);
            int headY = listY + titleHeight;
            int rowsY = headY + headH;
            int pagerY = listY + listH - pagerH;
            int rowsViewportHeight = Math.Max(90, pagerY - rowsY - 2);

            int total = Model.Quarantine.Count;
            int pageSize = quarantinePageSize == 20 || quarantinePageSize == 50 ? quarantinePageSize : 10;
            int pages = Math.Max(1, (total + pageSize - 1) / pageSize);
            int currentPage = Math.Min(pages, quarantineOffset / pageSize + 1);
            quarantineOffset = (currentPage - 1) * pageSize;
            List<Sketch20File> pageItems = Model.Quarantine.Skip(quarantineOffset).Take(pageSize).ToList();
            int bodyTotalH = pageItems.Count * QuarantineRowHeight;
            int maxScroll = Math.Max(0, bodyTotalH - rowsViewportHeight);
            quarantineRowsScrollPixels = Math.Max(0, Math.Min(quarantineRowsScrollPixels, maxScroll));
            int scrollWidth = maxScroll > 0 ? SystemInformation.VerticalScrollBarWidth : 0;
            int dataW = rowW - scrollWidth;

            Card("quarantine-main-card", 0, listY, w, listH, Color.White, QuarantineBorder, 11);
            Card("quarantine-title-icon", 19, listY + 13, 55, 55,
                Color.FromArgb(235, 245, 255), Color.Transparent, 10);
            Icon("cube", 33, listY + 27, 27, Sketch20Style.Blue);
            // Giữ đủ chiều rộng để hiển thị "Mới nhất trước" khi dùng Segoe UI.
            int sortWidth = compact ? 187 : 210;
            int sortX = w - sortWidth - 17;
            string listHeading = veryCompact ? "Tệp cách ly (" + total.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + ")"
                : "Danh sách tệp trong khu vực cách ly (" + total.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + ")";
            Text(listHeading, 86, listY + 15, Math.Max(175, sortX - 99), 39,
                veryCompact ? 15.0f : 18.0f, Sketch20Style.Ink, true);
            HeaderSelectBox("quarantine-sort", new[] { "Mới nhất trước", "Cũ nhất trước" },
                quarantineOldest ? 1 : 0,
                sortX, listY + 19, sortWidth, 38, 11.2f,
                choice =>
                {
                    quarantineOldest = choice == 1;
                    quarantineOffset = 0;
                    quarantineRowsScrollPixels = 0;
                    Render();
                });

            // Tiêu đề cột và từng dòng có cùng ranh giới tính theo dataW.
            Card("quarantine-header", rowX, headY, rowW, headH, QuarantineHeader, Color.Transparent, 4);
            int[] columns = QuarantineTableColumns(dataW);
            int checkW = columns[0], fileW = columns[1], severityW = columns[2], reasonW = columns[3], dateW = columns[4], actionW = columns[5];
            bool everythingSelected = total > 0 && Model.Quarantine.All(item => quarantineSelection.Contains(item.Index));
            var checkAll = Check("quarantine-select-all", "", rowX + 11, headY + 11, 27,
                everythingSelected, checkedValue =>
                {
                    quarantineSelection.Clear();
                    if (checkedValue)
                        foreach (Sketch20File item in Model.Quarantine) quarantineSelection.Add(item.Index);
                    if (Actions.SelectAllQuarantine != null) Actions.SelectAllQuarantine(checkedValue);
                    Render();
                });
            checkAll.Enabled = total > 0;
            int colX = rowX + checkW;
            QuarantineColumnCaption("TỆP TIN", colX, fileW, headY, headH, false); colX += fileW;
            QuarantineColumnCaption("MỨC ĐỘ", colX, severityW, headY, headH, true); colX += severityW;
            QuarantineColumnCaption("PHÁT HIỆN", colX, reasonW, headY, headH, true); colX += reasonW;
            QuarantineColumnCaption("NGÀY CÁCH LY", colX, dateW, headY, headH, true); colX += dateW;
            QuarantineColumnCaption("THAO TÁC", colX, actionW, headY, headH, true);
            Line(rowX, rowsY - 1, rowW);

            var rowsClip = new Panel {
                Name = "quarantine-rows-clip",
                BackColor = Color.White,
                Bounds = new Rectangle(rowX, rowsY + bodyOffset, dataW, rowsViewportHeight)
            };
            Theme.BatDoubleBuffer(rowsClip);
            Canvas.Controls.Add(rowsClip);
            Canvas.Nodes[rowsClip.Name] = rowsClip.Bounds;
            if (pageItems.Count == 0)
            {
                var empty = new Label {
                    Name = "quarantine-empty",
                    Text = string.IsNullOrWhiteSpace(Model.QuarantineNotice)
                        ? "Không có tệp nào trong khu vực cách ly."
                        : Model.QuarantineNotice,
                    ForeColor = QuarantineMuted,
                    BackColor = Color.White,
                    Font = new Font(Theme.UiFontFamily, 11.5f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Bounds = new Rectangle(12, Math.Max(8, (rowsViewportHeight - 55) / 2), Math.Max(40, dataW - 24), 55)
                };
                rowsClip.Controls.Add(empty);
            }
            for (int i = 0; i < pageItems.Count; i++)
            {
                Sketch20File file = pageItems[i];
                int modelIndex = file.Index;
                var row = new QuarantineListRow(file, dataW, QuarantineRowHeight,
                    quarantineSelection.Contains(modelIndex), columns, (quarantineOffset + i) % 2 != 0);
                row.Name = "quarantine-item-" + i;
                row.Tag = i;
                row.Location = new Point(0, i * QuarantineRowHeight - quarantineRowsScrollPixels);
                row.SelectionChanged += selected =>
                {
                    if (selected) quarantineSelection.Add(modelIndex);
                    else quarantineSelection.Remove(modelIndex);
                    if (Actions.SelectQuarantine != null) Actions.SelectQuarantine(modelIndex, selected);
                    Render();
                };
                row.DetailRequested += () => OpenDetail(modelIndex, true);
                rowsClip.Controls.Add(row);
                toolTips.SetToolTip(row, (file.Name ?? "") + "\n" + (file.Path ?? "") + "\n" + (file.Reason ?? ""));
            }
            if (maxScroll > 0)
            {
                var innerScroll = new VScrollBar {
                    Name = "quarantine-rows-scroll", AccessibleName = "Cuộn danh sách tệp cách ly",
                    Minimum = 0, Maximum = Math.Max(0, bodyTotalH - 1),
                    LargeChange = rowsViewportHeight, SmallChange = QuarantineRowHeight,
                    Value = quarantineRowsScrollPixels,
                    Bounds = new Rectangle(rowX + dataW, rowsY + bodyOffset, scrollWidth, rowsViewportHeight)
                };
                innerScroll.ValueChanged += (sender, args) =>
                {
                    quarantineRowsScrollPixels = innerScroll.Value;
                    foreach (QuarantineListRow item in rowsClip.Controls.OfType<QuarantineListRow>())
                        item.Top = Convert.ToInt32(item.Tag) * QuarantineRowHeight - quarantineRowsScrollPixels;
                };
                Canvas.Controls.Add(innerScroll);
            }

            // Thanh phân trang giữ cố định ở cuối card, độc lập với nội dung đang cuộn.
            Line(rowX, pagerY, rowW);
            int first = total == 0 ? 0 : quarantineOffset + 1;
            int last = total == 0 ? 0 : Math.Min(total, quarantineOffset + pageItems.Count);
            Text("Hiển thị " + first + " – " + last + " / " + total + " tệp",
                rowX + 6, pagerY + 8, Math.Max(140, w / 3), 34,
                11.2f, QuarantineMuted);
            QuarantinePager(pagerY + (compact ? 33 : 7), currentPage, pages, pageSize, compact);

            // Hàng thao tác dưới bảng (đúng như mẫu), kích hoạt dựa trên checkbox thực tế.
            int actionY = listY + listH + actionGap;
            Card("quarantine-actions", 0, actionY, w, actionH, Color.White, QuarantineBorder, 11);
            Card("quarantine-actions-icon", 18, actionY + 17, 49, 49,
                Color.FromArgb(235, 245, 255), Color.Transparent, 10);
            Icon("shield-check", 33, actionY + 32, 19, Sketch20Style.Blue);
            Text("Đã chọn " + quarantineSelection.Count + " tệp", 84, actionY + 12,
                Math.Max(130, w - 410), 31, 12.4f, Sketch20Style.Ink, true);
            Text("Khôi phục hoặc xóa vĩnh viễn các tệp đã chọn.", 84, actionY + 41,
                Math.Max(140, veryCompact ? w - 105 : w - 410), 24, 10.4f, QuarantineMuted);
            int buttonY = actionY + (veryCompact ? 65 : 20);
            int restoreX = Math.Max(10, w - 332);
            if (!veryCompact)
                Canvas.Drawing.Add(graphics =>
                {
                    using (var line = new Pen(QuarantineBorder))
                        graphics.DrawLine(line, restoreX - 18, actionY + 16, restoreX - 18, actionY + actionH - 16);
                });
            var restore = Button("restore-selected", "Khôi phục", restoreX, buttonY, 147,
                () => Command("restore"), "undo", false, false, 41);
            restore.SquareCorners = true;
            restore.Enabled = quarantineSelection.Count > 0;
            var delete = Button("delete-selected", "Xóa vĩnh viễn", restoreX + 156, buttonY, 163,
                () => Command("delete"), "trash", false, true, 41);
            delete.SquareCorners = true;
            delete.Enabled = quarantineSelection.Count > 0;
            ContentHeight = actionY + actionH + 8;
        }

        // Tổng các chiều rộng phải đúng bằng vùng bảng; dùng chung cho tiêu đề/dòng.
        private static int[] QuarantineTableColumns(int width)
        {
            const int check = 44;
            int available = Math.Max(280, width - check);
            bool compact = width < 930;
            int file = (int)(available * (compact ? .34 : .44));
            int severity = (int)(available * (compact ? .14 : .12));
            int reason = (int)(available * (compact ? .21 : .16));
            int date = (int)(available * (compact ? .15 : .13));
            int action = available - file - severity - reason - date;
            return new[] { check, file, severity, reason, date, action };
        }

        private void QuarantineColumnCaption(string name, int x, int width, int y, int height, bool center)
        {
            float textSize = layoutWidth < 900 ? 9.7f : 10.3f;
            int measured = Sketch20Style.Measure(name, textSize * ReferenceTextScale, true);
            int textX = center ? x + Math.Max(0, (width - measured) / 2) : x + 10;
            Text(name, textX, y, Math.Max(8, x + width - textX - 4), height,
                textSize, QuarantineMuted, true);
        }

        private void QuarantinePager(int y, int current, int totalPages, int size, bool compact)
        {
            int w = layoutWidth;
            int buttonW = compact ? 32 : 35;
            int gap = 5;
            List<int> pages = new List<int>();
            if (compact && w < 790) pages.Add(current);
            else if (totalPages <= 5)
                for (int i = 1; i <= totalPages; i++) pages.Add(i);
            else if (current <= 3) pages.AddRange(new[] { 1, 2, 3, -1, totalPages });
            else if (current >= totalPages - 2) pages.AddRange(new[] { 1, -1, totalPages - 2, totalPages - 1, totalPages });
            else pages.AddRange(new[] { 1, -1, current - 1, current, current + 1, -1, totalPages });
            int pagerWidth = (pages.Count + 2) * (buttonW + gap);
            int pagerX = Math.Max(16, w - 18 - pagerWidth);
            int controlsX = Math.Max(16, pagerX - (compact ? 171 : 206));
            Text("Số dòng/trang:", controlsX, y, compact ? 98 : 116, 36,
                10.6f, QuarantineMuted);
            SelectBox("quarantine-page-size", new[] { "10", "20", "50" },
                size == 20 ? 1 : size == 50 ? 2 : 0,
                controlsX + (compact ? 101 : 121), y, compact ? 65 : 78,
                selected =>
                {
                    quarantinePageSize = selected == 1 ? 20 : selected == 2 ? 50 : 10;
                    quarantineOffset = 0;
                    quarantineRowsScrollPixels = 0;
                    Render();
                }, 36);
            int x = pagerX;
            var prev = Button("quarantine-page-prev", "", x, y, buttonW,
                () => QuarantineGotoPage(current - 1), "chevron-left", false, false, 36);
            prev.Enabled = current > 1;
            prev.SquareCorners = true;
            x += buttonW + gap;
            foreach (int item in pages)
            {
                if (item == -1)
                    Text("…", x, y, buttonW, 36, 11.2f, QuarantineMuted);
                else
                {
                    int page = item;
                    var pageButton = Button("quarantine-page-" + page, page.ToString(), x, y,
                        buttonW, () => QuarantineGotoPage(page), "", page == current, false, 36);
                    pageButton.SquareCorners = true;
                }
                x += buttonW + gap;
            }
            var next = Button("quarantine-page-next", "Sau", x, y, buttonW,
                () => QuarantineGotoPage(current + 1), "", false, false, 36);
            next.TextPixels = 10.0f;
            next.Enabled = current < totalPages;
            next.SquareCorners = true;
        }

        private void QuarantineGotoPage(int number)
        {
            int count = Model.Quarantine.Count;
            int size = Math.Max(1, quarantinePageSize);
            int totalPages = Math.Max(1, (count + size - 1) / size);
            quarantineOffset = (Math.Max(1, Math.Min(totalPages, number)) - 1) * size;
            quarantineRowsScrollPixels = 0;
            Render();
        }
    }

    // Mỗi dòng vẽ riêng để khi chọn 20/50 dòng có thể cuộn mà không kéo theo tiêu đề.
    internal sealed class QuarantineListRow : System.Windows.Forms.Control
    {
        private readonly Sketch20File data;
        private readonly int[] widths;
        private readonly bool alternative;
        private bool selected;
        private readonly Rectangle checkArea;
        private readonly Rectangle detailArea;
        internal event Action<bool> SelectionChanged;
        internal event Action DetailRequested;

        internal QuarantineListRow(Sketch20File file, int width, int height,
            bool checkedValue, int[] columnWidths, bool alternate)
        {
            data = file;
            widths = columnWidths;
            alternative = alternate;
            selected = checkedValue;
            Size = new Size(width, height);
            AccessibleRole = AccessibleRole.ListItem;
            AccessibleName = (file.Name ?? "Tệp cách ly") + " - " + (file.Reason ?? "");
            BackColor = Color.White;
            TabStop = true;
            checkArea = new Rectangle(10, (height - 22) / 2, 22, 22);
            int actionStart = widths.Take(5).Sum();
            int actionButtonWidth = Math.Max(60, Math.Min(142, widths[5] - 10));
            detailArea = new Rectangle(actionStart + (widths[5] - actionButtonWidth) / 2,
                (height - 39) / 2, actionButtonWidth, 39);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                selected = !selected;
                if (SelectionChanged != null) SelectionChanged(selected);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (DetailRequested != null) DetailRequested();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = checkArea.Contains(e.Location) || detailArea.Contains(e.Location)
                ? Cursors.Hand : Cursors.Default;
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (checkArea.Contains(e.Location))
            {
                selected = !selected;
                if (SelectionChanged != null) SelectionChanged(selected);
            }
            else if (detailArea.Contains(e.Location) && DetailRequested != null)
                DetailRequested();
        }

        private static string CompactQuarantinePath(string path, int width, float fontSize)
        {
            path = path ?? "";
            if (path.Length == 0 || width <= 0 || Sketch20Style.Measure(path, fontSize) <= width)
                return path;
            string normalized = path.Replace('/', '\\');
            int archiveSep = normalized.IndexOf(" > ", StringComparison.Ordinal);
            string suffix, prefix;
            if (archiveSep >= 0)
            {
                string archive = normalized.Substring(0, archiveSep);
                string entry = normalized.Substring(archiveSep + 3);
                int slash = archive.LastIndexOf('\\');
                string archiveName = slash < 0 ? archive : archive.Substring(slash + 1);
                int entrySlash = entry.LastIndexOf('\\');
                string entryName = entrySlash < 0 ? entry : entry.Substring(entrySlash + 1);
                suffix = archiveName + " / " + entryName;
                prefix = archive.Length >= 3 && archive[1] == ':' ? archive.Substring(0, 3) : "";
            }
            else
            {
                var parts = normalized.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    suffix = parts[parts.Length - 2] + "\\" + parts[parts.Length - 1];
                else
                    suffix = normalized;
                prefix = normalized.Length >= 3 && normalized[1] == ':' ? normalized.Substring(0, 3) : "";
            }
            string preferred = prefix + "…\\" + suffix;
            if (Sketch20Style.Measure(preferred, fontSize) <= width) return preferred;
            preferred = "…\\" + suffix;
            if (Sketch20Style.Measure(preferred, fontSize) <= width) return preferred;
            int left = 0, right = suffix.Length;
            while (left < right)
            {
                int mid = (left + right + 1) / 2;
                string trial = "…" + suffix.Substring(suffix.Length - mid);
                if (Sketch20Style.Measure(trial, fontSize) <= width) left = mid;
                else right = mid - 1;
            }
            return "…" + suffix.Substring(Math.Max(0, suffix.Length - left));
        }

        private static string CompactQuarantineReason(string reason, int width, float fontSize)
        {
            string value = string.IsNullOrWhiteSpace(reason) ? "Không rõ" : reason.Trim();
            value = value.Replace("\r", " ").Replace("\n", " ");
            while (value.Contains("  ")) value = value.Replace("  ", " ");

            string[] colonParts = value.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (colonParts.Length >= 2)
            {
                string primary = colonParts[0].Trim() + ": " + colonParts[1].Trim();
                if (Sketch20Style.Measure(primary, fontSize) <= width) return primary;
            }

            int comma = value.IndexOf(',');
            if (comma > 0)
            {
                string primary = value.Substring(0, comma).Trim();
                if (Sketch20Style.Measure(primary, fontSize) <= width) return primary;
            }

            int semi = value.IndexOf(';');
            if (semi > 0)
            {
                string primary = value.Substring(0, semi).Trim();
                if (Sketch20Style.Measure(primary, fontSize) <= width) return primary;
            }

            if (Sketch20Style.Measure(value, fontSize) <= width) return value;
            int left = 0, right = value.Length;
            while (left < right)
            {
                int mid = (left + right + 1) / 2;
                string trial = value.Substring(0, mid).TrimEnd() + "…";
                if (Sketch20Style.Measure(trial, fontSize) <= width) left = mid;
                else right = mid - 1;
            }
            return left > 0 ? value.Substring(0, left).TrimEnd() + "…" : "…";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            int h = Height;
            Color ink = Sketch20Style.Ink;
            Color muted = Color.FromArgb(111, 132, 171);
            using (var background = new SolidBrush(alternative ? Color.FromArgb(246, 250, 255) : Color.White))
                g.FillRectangle(background, ClientRectangle);
            using (var border = new Pen(Color.FromArgb(221, 234, 249)))
                g.DrawLine(border, 0, h - 1, Width, h - 1);

            Rectangle tick = new Rectangle(checkArea.X + 2, checkArea.Y + 2, 18, 18);
            using (var edge = new Pen(selected ? Sketch20Style.Blue : Color.FromArgb(180, 193, 211), 1.5f))
            using (var fill = new SolidBrush(selected ? Sketch20Style.Blue : Color.White))
            {
                g.FillRectangle(fill, tick);
                g.DrawRectangle(edge, tick);
            }
            if (selected)
                Sketch20Style.Text(g, "✓", tick, 13.5f, Color.White, true, false, false, false, true);

            int[] x = new int[7];
            for (int i = 0; i < widths.Length; i++) x[i + 1] = x[i] + widths[i];
            string rawPath = data.Path ?? "";
            string ext = Path.GetExtension(data.Name ?? data.Path ?? "").TrimStart('.').ToUpperInvariant();
            if (ext.Length > 4) ext = ext.Substring(0, 4);
            if (string.IsNullOrWhiteSpace(ext)) ext = "FILE";
            Color iconFg = ext == "ZIP" ? Color.FromArgb(227, 129, 20) :
                ext == "PS1" ? Color.FromArgb(120, 69, 216) : Color.FromArgb(23, 123, 238);
            Color iconBg = ext == "ZIP" ? Color.FromArgb(255, 238, 213) :
                ext == "PS1" ? Color.FromArgb(240, 231, 255) : Color.FromArgb(231, 244, 255);
            int iconX = x[1] + 8, iconY = (h - 45) / 2;
            Sketch20Style.Round(g, new Rectangle(iconX, iconY, 45, 45), 8, iconBg, Color.Transparent);
            Sketch20Style.Icon(g, ext == "DLL" ? "settings" : "file", new Rectangle(iconX + 13, iconY + 6, 19, 19), iconFg);
            Sketch20Style.Text(g, ext, new Rectangle(iconX + 4, iconY + 25, 37, 16),
                ext.Length > 3 ? 8.7f : 10.1f, iconFg, true, false, false, false, true);
            int labelX = iconX + 57;
            int nameWidth = Math.Max(12, x[2] - labelX - 7);
            string displayPath = CompactQuarantinePath(rawPath, nameWidth, 10.9f);
            string displayReason = CompactQuarantineReason(data.Reason, Math.Max(8, widths[3] - 24), 11.0f);
            Sketch20Style.Text(g, data.Name ?? "Không rõ tên tệp",
                new Rectangle(labelX, 13, nameWidth, 26), 13.2f, ink, true);
            Sketch20Style.Text(g, displayPath, new Rectangle(labelX, 43, nameWidth, 22),
                10.9f, muted);

            string severity = string.IsNullOrWhiteSpace(data.Severity) ? "Không rõ" : data.Severity;
            bool high = severity.IndexOf("Cao", StringComparison.OrdinalIgnoreCase) >= 0 ||
                severity.IndexOf("Nguy", StringComparison.OrdinalIgnoreCase) >= 0;
            bool low = severity.IndexOf("Thấp", StringComparison.OrdinalIgnoreCase) >= 0;
            Color sevFg = high ? Color.FromArgb(227, 42, 75) : low ? Color.FromArgb(21, 154, 89) : Color.FromArgb(231, 143, 19);
            Color sevBg = high ? Color.FromArgb(255, 235, 240) : low ? Color.FromArgb(227, 250, 242) : Color.FromArgb(255, 245, 226);
            int badgeW = Math.Min(Math.Max(65, widths[2] - 15), Math.Max(76, Sketch20Style.Measure(severity, 11.5f, true) + 26));
            int badgeX = x[2] + Math.Max(2, (widths[2] - badgeW) / 2);
            Sketch20Style.Round(g, new Rectangle(badgeX, (h - 33) / 2, badgeW, 33), 14, sevBg, Color.Transparent);
            Sketch20Style.Text(g, severity, new Rectangle(badgeX + 2, (h - 33) / 2, badgeW - 4, 33),
                11.5f, sevFg, true, false, false, false, true);

            Sketch20Style.Text(g, displayReason,
                new Rectangle(x[3] + 12, 10, Math.Max(8, widths[3] - 24), h - 20),
                11.0f, ink);
            Sketch20Style.Text(g, data.Date ?? "—",
                new Rectangle(x[4] + 5, 16, Math.Max(4, widths[4] - 10), 24),
                11.6f, ink, false, false, false, false, true);
            Sketch20Style.Text(g, data.Time ?? "—",
                new Rectangle(x[4] + 5, 43, Math.Max(4, widths[4] - 10), 23),
                10.4f, muted, false, false, false, false, true);
            Sketch20Style.Round(g, detailArea, 5, Color.FromArgb(235, 246, 255), Color.Transparent);
            Sketch20Style.Text(g, "Xem chi tiết", new Rectangle(detailArea.X + 4, detailArea.Y,
                Math.Max(8, detailArea.Width - 8), detailArea.Height),
                10.7f, Sketch20Style.Blue, true, false, false, false, true);
        }
    }
}
