using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    // Bảng Mối đe dọa theo ảnh mẫu: có lọc/tìm kiếm, chọn nhiều hàng,
    // xử lý hàng loạt và phân trang. Không sử dụng dữ liệu demo trong ứng dụng thật.
    internal sealed partial class Sketch20View
    {
        private readonly HashSet<int> threatSelected = new HashSet<int>();
        private string threatSearchText = "";
        private int threatFilterIndex;
        private int threatSortMode; // 0: mới nhất, 1: cũ nhất, 2: tên A-Z, 3: mức độ
        private int threatRequestedRows = 10;
        // Thanh cuộn chỉ dành cho nội dung của trang hiện tại, không thay đổi phân trang.
        private int threatRowsScrollPixels;
        private int threatLastPageIndex = -1;
        private int threatLastPageSize = -1;
        private const int ThreatHeaderRowHeight = 42;
        private static readonly string[] ThreatFilters = { "Tất cả", "Mức độ cao", "Trung bình", "Thấp", "Heuristic" };
        private static readonly string[] ThreatPageChoices = { "10", "20", "50" };
        private static readonly Color ThreatTableBlue = Color.FromArgb(233, 244, 255);

        private List<Sketch20File> FilteredThreats()
        {
            IEnumerable<Sketch20File> query = Model.Threats.Where(f => f != null && !f.Resolved);
            string value = (threatSearchText ?? "").Trim();
            if (value.Length > 0)
                query = query.Where(f => new[] { f.Name, f.Path, f.Reason, f.Severity, ThreatCategory(f) }
                    .Any(s => (s ?? "").IndexOf(value, StringComparison.CurrentCultureIgnoreCase) >= 0));
            if (threatFilterIndex == 1) query = query.Where(f => f.Severity == "Cao");
            if (threatFilterIndex == 2) query = query.Where(f => f.Severity == "Trung bình");
            if (threatFilterIndex == 3) query = query.Where(f => f.Severity == "Thấp");
            if (threatFilterIndex == 4) query = query.Where(f =>
                (f.Reason ?? "").IndexOf("Heuristic", StringComparison.OrdinalIgnoreCase) >= 0);
            if (threatSortMode == 1) query = query.OrderBy(ThreatDate).ThenBy(f => f.Index);
            else if (threatSortMode == 2) query = query.OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(f => f.Index);
            else if (threatSortMode == 3) query = query.OrderBy(f => ThreatSeverityRank(f.Severity)).ThenByDescending(ThreatDate);
            else query = query.OrderByDescending(ThreatDate).ThenByDescending(f => f.Index);
            return query.ToList();
        }
        private static int ThreatSeverityRank(string severity)
        { return severity == "Cao" ? 0 : severity == "Trung bình" ? 1 : 2; }
        private static DateTime ThreatDate(Sketch20File f)
        {
            DateTime result;
            return DateTime.TryParseExact((f.Date ?? "") + " " + (f.Time ?? ""),
                new[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result) ? result : DateTime.MinValue;
        }
        private static string ThreatCategory(Sketch20File f)
        {
            string text = (f.Reason ?? "").Trim();
            if (text.StartsWith("Heuristic", StringComparison.OrdinalIgnoreCase)) return "Heuristic";
            if (text.StartsWith("Script", StringComparison.OrdinalIgnoreCase)) return "Script";
            if (text.StartsWith("VirusTotal", StringComparison.OrdinalIgnoreCase)) return "VirusTotal";
            int sep = text.IndexOf(':');
            if (sep > 0 && sep < 32) return text.Substring(0, sep).Trim();
            string[] names = { "Trojan", "Backdoor", "Spyware", "Ransomware", "Adware", "RiskWare", "Worm", "Dropper", "Packed" };
            foreach (string name in names)
                if (text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return name;
            return "Chưa phân loại";
        }
        private static string ThreatFileKind(Sketch20File f)
        {
            string file = f.Path ?? f.Name ?? "";
            int virtualEntry = file.LastIndexOf(" > ", StringComparison.Ordinal);
            if (virtualEntry >= 0) file = file.Substring(0, virtualEntry);
            string extension = System.IO.Path.GetExtension(file).TrimStart('.').ToUpperInvariant();
            if (extension.Length == 0 || extension.Length > 5) extension = "FILE";
            return extension;
        }

        private void RunThreatAction(string operation, bool all)
        {
            List<int> ids = all ? Model.Threats.Where(f => !f.Resolved).Select(f => f.Index).Distinct().ToList()
                : Model.Threats.Where(f => threatSelected.Contains(f.Index) && !f.Resolved)
                    .Select(f => f.Index).Distinct().ToList();
            if (ids.Count == 0)
            {
                AppDialog.Show("Hãy chọn ít nhất một tệp cần xử lý.", "Mối đe dọa",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (operation == "quarantine")
            {
                var choice = AppDialog.Show("Cách ly " + ids.Count + " mục đã " + (all ? "phát hiện" : "chọn") +
                    "?\nĐối với mục nằm trong ZIP, ứng dụng có thể cần cách ly toàn bộ tệp ZIP gốc.",
                    "Xác nhận cách ly", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (choice != DialogResult.Yes) return;
            }
            if (Actions.ProcessThreats != null) Actions.ProcessThreats(ids, operation);
            else if (Model.Sample)
            {
                foreach (Sketch20File file in Model.Threats.Where(f => ids.Contains(f.Index))) file.Resolved = true;
                Model.Threats.RemoveAll(f => f.Resolved);
                Model.ThreatCount = Model.Threats.Count;
            }
            else
            {
                AppDialog.Show("Chức năng xử lý tệp chưa được kết nối với bộ điều khiển.",
                    "Mối đe dọa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Chỉ loại những mục đã xử lý khỏi lựa chọn trong lần Render tiếp theo.
            // Nếu thao tác bị hủy hoặc xử lý thất bại, giữ mục đã chọn để thử lại.
            threatRowsScrollPixels = 0;
            RefreshModel(true);
            Render();
        }

        private void ThreatTableRedesigned(int y)
        {
            threatSelected.RemoveWhere(index => !Model.Threats.Any(f => f.Index == index && !f.Resolved));
            List<Sketch20File> data = FilteredThreats();
            int total = data.Count;
            bool compact = layoutWidth < 1180;
            bool narrow = layoutWidth < 750;
            bool stackedHeader = layoutWidth < 1130;
            int viewH = Math.Max(300, viewport.ClientSize.Height - AdvancedCanvasTop - y - 9);
            int titleH = stackedHeader ? 132 : 80;
            // Thanh hành động chuyển thành 2 hàng nếu bề ngang hẹp: hàng trên
            // chứa chọn tất cả, Cách ly tất cả, Làm mới; hàng dưới xử lý mục đã chọn.
            bool stackedActions = layoutWidth < 1050;
            int actionsH = stackedActions ? 104 : 66;
            int footerH = layoutWidth < 1030 ? 100 : 64;
            int headH = ThreatHeaderRowHeight;
            int minimumRowH = narrow ? 74 : compact ? 55 : 47;
            int fixedH = titleH + actionsH + headH + footerH + 8;

            // 10/20/50 là số mục của một TRANG; không tính theo chiều cao cửa sổ.
            int pageSize = threatRequestedRows;
            int pageCount = Math.Max(1, (total + pageSize - 1) / pageSize);
            int pageIndex = Math.Max(0, Math.Min(pageCount - 1, threatOffset / pageSize));
            threatOffset = pageIndex * pageSize;
            if (threatLastPageIndex != pageIndex || threatLastPageSize != pageSize)
            {
                threatRowsScrollPixels = 0;
                threatLastPageIndex = pageIndex;
                threatLastPageSize = pageSize;
            }
            List<Sketch20File> pageRows = data.Skip(threatOffset).Take(pageSize).ToList();
            int shown = pageRows.Count;
            // Khung DANH SÁCH luôn lấp đầy chiều cao viewport (kể cả khi 0 mục).
            // Chỉ nội dung bên trong thay đổi; phân trang luôn nằm dưới cùng khung.
            // Người dùng chọn 10/20/50 MỤC/TRANG; tối đa 10 dòng hiện cùng lúc,
            // 20/50 mục cuộn bên trong bảng và không đẩy footer ra ngoài màn hình.
            int rowsH = Math.Max(120, viewH - fixedH);
            // Ở cửa sổ cao, giãn chiều cao hàng cho vừa 10 dòng, tránh để 11+
            // dòng xuất hiện cùng lúc khi đang chọn 20/50 mục/trang.
            int rowH = Math.Max(minimumRowH, rowsH / 10);
            int maxVisibleByHeight = Math.Max(1, rowsH / rowH);
            int visibleRows = Math.Min(10, maxVisibleByHeight);
            int cardH = titleH + actionsH + headH + rowsH + footerH;
            Card("threat-panel", 0, y, layoutWidth, cardH, Color.White,
                Color.FromArgb(207, 225, 245), 0);

            // Tiêu đề DUY NHẤT của trang, với đúng biểu tượng khiên cảnh báo.
            Card("threat-title-icon", 16, y + 15, 44, 44, ThreatTableBlue, Color.Transparent, 0);
            Icon("shield-alert", 22, y + 21, 32, Sketch20Style.Blue);
            string heading = "Danh sách mối đe dọa (" + Model.Threats.Count(f => !f.Resolved).ToString("N0") + ")";
            int filterW = 190;
            int filterX = layoutWidth - filterW - 18;
            int searchW = stackedHeader ? Math.Max(160, filterX - 34) :
                Math.Min(405, Math.Max(240, layoutWidth / 3));
            int searchX = stackedHeader ? 18 : filterX - searchW - 14;
            int searchY = stackedHeader ? y + 75 : y + 19;
            Text(heading, 72, y + 15, stackedHeader ? layoutWidth - 94 : searchX - 85,
                40, stackedHeader ? 16.3f : 16.8f, Sketch20Style.Ink, true);
            DrawThreatSearch(searchX, searchY, searchW);
            SelectBox("threat-filter", ThreatFilters, threatFilterIndex, filterX, searchY,
                filterW, index => { threatFilterIndex = index; threatOffset = 0;
                    threatRowsScrollPixels = 0; Render(); }, 41);

            int toolbarY = y + titleH;
            Line(16, toolbarY, layoutWidth - 32);
            int bulkCount = threatSelected.Count;
            bool pageChecked = shown > 0 && pageRows.All(f => threatSelected.Contains(f.Index));
            // Ô chọn không được phủ lên viền trái nút Cách ly: ở chế độ rộng
            // nút bắt đầu tại x=195, trước đây checkbox bắt đầu x=22 rộng 174
            // (mép phải x=196) nên WinForms vẽ đè mất một đoạn viền nút.
            // Giữ cách tối thiểu 13 px khi hai control nằm cùng một hàng.
            Check("threat-check-all", "Đã chọn " + bulkCount + " mục", 22,
                toolbarY + 12, stackedActions ? 154 : 160, pageChecked, check =>
                {
                    foreach (Sketch20File f in pageRows)
                        if (check) threatSelected.Add(f.Index); else threatSelected.Remove(f.Index);
                    Render();
                });
            int actionX = stackedActions ? 22 : 195;
            int actionY = stackedActions ? toolbarY + 55 : toolbarY + 7;
            int actionBtnW = narrow ? 109 : 116;
            var quarantine = Button("threat-isolate-selected", "Cách ly", actionX, actionY,
                actionBtnW, () => RunThreatAction("quarantine", false), "shield-check", false, false, 42);
            quarantine.ReadableWhenDisabled = true;
            var delete = Button("threat-delete-selected", "Xóa", actionX + actionBtnW + 9,
                actionY, narrow ? 79 : 92, () => RunThreatAction("delete", false), "trash", false, false, 42);
            delete.ReadableWhenDisabled = true;
            var allow = Button("threat-allow-selected", "Cho phép",
                actionX + actionBtnW + (narrow ? 96 : 110), actionY,
                narrow ? 105 : 121, () => RunThreatAction("allow", false), "check", false, false, 42);
            allow.ReadableWhenDisabled = true;
            allow.ForeColor = Sketch20Style.Green;
            allow.BackColor = Sketch20Style.GreenSoft;

            // Hai nút toàn trang đứng cạnh nhau ở mép phải, cùng một hàng.
            // Màn hẹp tự tách thành hàng riêng để không đè vào nhóm xử lý đã chọn.
            const int refreshW = 132, isolateAllW = 170, rightGap = 10;
            int refreshX = layoutWidth - 18 - refreshW;
            int isolateAllX = refreshX - rightGap - isolateAllW;
            int rightActionsY = stackedActions ? toolbarY + 7 : actionY;
            var isolateAll = Button("isolate-all-threats", "Cách ly tất cả",
                isolateAllX, rightActionsY, isolateAllW,
                () => RunThreatAction("quarantine", true), "archive", true, false, 42);
            isolateAll.ReadableWhenDisabled = true;
            isolateAll.Enabled = Model.Threats.Any(f => f != null && !f.Resolved);
            var refresh = Button("threat-refresh", "Làm mới", refreshX, rightActionsY, refreshW,
                () => { threatSelected.Clear(); threatOffset = 0; threatRowsScrollPixels = 0;
                    RefreshModel(true); Render(); }, "refresh", false, false, 42);
            quarantine.Enabled = delete.Enabled = allow.Enabled = bulkCount > 0;

            // Tiêu đề bảng và vùng dòng là hai khu vực khác nhau: chỉ cuộn phần dòng.
            int rowLeft = 14, rowW = layoutWidth - 28;
            // Tiêu đề và dữ liệu PHẢI dùng cùng độ rộng cột. Khi có thanh cuộn,
            // trừ chiều rộng scrollbar ở cả hai bên để các nội dung thẳng cột.
            bool needsRowScroll = shown > visibleRows;
            int scrollW = needsRowScroll ? SystemInformation.VerticalScrollBarWidth : 0;
            int innerRowWidth = rowW - scrollW;
            int headerY = toolbarY + actionsH;
            Card("threat-table-header", rowLeft, headerY, rowW, headH,
                Color.FromArgb(230, 243, 255), Color.Transparent, 0);
            if (compact)
            {
                Text(pageChecked ? "☑" : "□", rowLeft + 12, headerY, 32, headH,
                    16.4f, Sketch20Style.Ink, true);
                Text("TỆP / ĐƯỜNG DẪN", rowLeft + 85, headerY,
                    Math.Max(65, rowW - 306), headH, 12.0f, Sketch20Style.Ink, true);
                Text("MỨC ĐỘ", rowLeft + rowW - 209, headerY, 98, headH, 11.7f, Sketch20Style.Ink, true);
                Text("THAO TÁC", rowLeft + rowW - 105, headerY, 102, headH, 11.7f, Sketch20Style.Ink, true);
            }
            else
            {
                int[] col = Sketch20ThreatGridRow.Columns(innerRowWidth);
                string[] headers = { "", "Loại", "Tên tệp", "Đường dẫn", "Loại mã độc", "Mức độ",
                    "Ngày phát hiện", "Trạng thái", "Thao tác" };
                int offset = rowLeft;
                for (int i = 0; i < headers.Length; i++)
                {
                    if (i == 0) Text(pageChecked ? "☑" : "□", offset + 12,
                        headerY, 26, headH, 17.5f, Sketch20Style.Ink);
                    else ThreatCenteredColumnTitle(headers[i], offset, headerY,
                        Math.Max(20, col[i]), headH);
                    offset += col[i];
                }
                HitArea("threat-sort-name", "Sắp xếp theo tên tệp", rowLeft + col[0] + col[1],
                    headerY, col[2], headH, () => { threatSortMode = 2; threatOffset = 0;
                        threatRowsScrollPixels = 0; Render(); });
                int dateX = rowLeft + col.Take(6).Sum();
                HitArea("threat-sort-date", "Đổi thứ tự ngày", dateX, headerY, col[6], headH,
                    () => { threatSortMode = threatSortMode == 1 ? 0 : 1; threatOffset = 0;
                        threatRowsScrollPixels = 0; Render(); });
                int severityX = rowLeft + col.Take(5).Sum();
                HitArea("threat-sort-severity", "Sắp xếp theo mức độ", severityX,
                    headerY, col[5], headH,
                    () => { threatSortMode = 3; threatOffset = 0; threatRowsScrollPixels = 0; Render(); });
            }
            HitArea("threat-select-page", "Chọn hoặc bỏ chọn các mục của trang", rowLeft + 4,
                headerY + 3, 37, headH - 6, () =>
                {
                    bool select = !pageRows.All(f => threatSelected.Contains(f.Index));
                    foreach (Sketch20File f in pageRows)
                        if (select) threatSelected.Add(f.Index); else threatSelected.Remove(f.Index);
                    Render();
                });

            int rowsTop = headerY + headH;
            var rowViewport = new Panel
            {
                Name = "threat-rows-viewport", BackColor = Color.White,
                AutoScroll = false, TabStop = false,
                Bounds = new Rectangle(rowLeft, rowsTop, innerRowWidth, rowsH)
            };
            Theme.BatDoubleBuffer(rowViewport);
            Canvas.Controls.Add(rowViewport);
            Canvas.Nodes[rowViewport.Name] = rowViewport.Bounds;
            if (shown == 0)
            {
                // Khi không có bản ghi, vẫn giữ bảng đầy chiều cao màn hình.
                // Căn nội dung trạng thái rỗng chính giữa khung và neo phân trang đáy.
                bool trulyEmpty = total == 0 && string.IsNullOrWhiteSpace(threatSearchText)
                    && threatFilterIndex == 0;
                var emptyTitle = new Label {
                    Name = "threat-empty-title", AutoSize = false,
                    Text = trulyEmpty ? "Không có mối đe dọa cần xử lý"
                        : "Không tìm thấy mối đe dọa phù hợp",
                    Font = Sketch20Style.Font(14.2f, true),
                    ForeColor = Sketch20Style.Ink,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.White
                };
                var emptyNote = new Label {
                    Name = "threat-empty-description", AutoSize = false,
                    Text = trulyEmpty
                        ? "Các mối đe dọa mới sẽ xuất hiện tại đây sau khi quét."
                        : "Thử thay đổi từ khóa hoặc bộ lọc để xem dữ liệu.",
                    Font = Sketch20Style.Font(11.5f),
                    ForeColor = Sketch20Style.Muted,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.White
                };
                Action layoutEmpty = () => {
                    int contentWidth = Math.Max(1, rowViewport.ClientSize.Width - 24);
                    int emptyTitleHeight = rowViewport.Height < 90 ? rowViewport.Height : 31;
                    int emptyNoteHeight = rowViewport.Height < 90 ? 0 : 28;
                    int emptyTop = Math.Max(0, (rowViewport.Height - emptyTitleHeight - emptyNoteHeight) / 2);
                    emptyTitle.SetBounds(12, emptyTop, contentWidth, emptyTitleHeight);
                    emptyNote.SetBounds(12, emptyTop + emptyTitleHeight, contentWidth, emptyNoteHeight);
                    emptyNote.Visible = emptyNoteHeight > 0;
                };
                rowViewport.Controls.Add(emptyTitle);
                rowViewport.Controls.Add(emptyNote);
                rowViewport.Resize += (sender, args) => layoutEmpty();
                layoutEmpty();
            }
            else
            {
                int maxScroll = Math.Max(0, shown * rowH - rowsH);
                threatRowsScrollPixels = Math.Max(0, Math.Min(threatRowsScrollPixels, maxScroll));
                for (int i = 0; i < shown; i++)
                {
                    Sketch20File file = pageRows[i];
                    int index = file.Index;
                    var row = new Sketch20ThreatGridRow(file, ThreatCategory(file), ThreatFileKind(file),
                        compact, threatSelected.Contains(index))
                    {
                        Name = "threat-grid-row-" + i,
                        Bounds = new Rectangle(0, i * rowH - threatRowsScrollPixels,
                            innerRowWidth, rowH),
                        BackColor = Color.White,
                        Tag = i
                    };
                    row.SelectRequested += () =>
                    {
                        if (!threatSelected.Add(index)) threatSelected.Remove(index);
                        Render();
                    };
                    row.DetailRequested += () => OpenDetail(index, false);
                    rowViewport.Controls.Add(row);
                    Canvas.Nodes[row.Name] = new Rectangle(rowLeft,
                        rowsTop + i * rowH - threatRowsScrollPixels, innerRowWidth, rowH);
                    toolTips.SetToolTip(row, file.Name + "\n" + file.Path + "\n" + file.Reason);
                }
                if (needsRowScroll)
                {
                    var bar = new VScrollBar
                    {
                        Name = "threat-rows-scroll", Minimum = 0,
                        Maximum = Math.Max(0, shown * rowH - 1),
                        LargeChange = Math.Max(1, rowsH), SmallChange = rowH,
                        Bounds = new Rectangle(rowLeft + innerRowWidth, rowsTop, scrollW, rowsH),
                        Value = threatRowsScrollPixels
                    };
                    bar.ValueChanged += (sender, args) =>
                    {
                        threatRowsScrollPixels = bar.Value;
                        foreach (Sketch20ThreatGridRow row in rowViewport.Controls.OfType<Sketch20ThreatGridRow>())
                            row.Top = (int)row.Tag * rowH - threatRowsScrollPixels;
                    };
                    Canvas.Controls.Add(bar);
                    Canvas.Nodes[bar.Name] = bar.Bounds;
                }
            }

            int pagerY = rowsTop + rowsH + 12;
            Line(16, pagerY - 2, layoutWidth - 32);
            int from = shown == 0 ? 0 : threatOffset + 1;
            int to = shown == 0 ? 0 : threatOffset + shown;
            string countText = "Hiển thị " + from + " – " + to + " / " + total + " mục";
            int pagerWidth = ThreatPaginationWidth(pageIndex, pageCount);
            int pagerX = Math.Max(14, layoutWidth - pagerWidth - 18);
            if (layoutWidth >= 1030)
            {
                Text(countText, 21, pagerY + 11, Math.Max(170, pagerX - 315), 31,
                    11.5f, Sketch20Style.Ink);
                Text("Số dòng/trang:", pagerX - 221, pagerY + 12, 129, 29,
                    11.1f, Sketch20Style.Ink);
                SelectBox("threat-page-size", ThreatPageChoices,
                    Array.IndexOf(new[] { 10, 20, 50 }, threatRequestedRows),
                    pagerX - 99, pagerY + 7, 84,
                    index => { threatRequestedRows = new[] { 10, 20, 50 }[index];
                        threatOffset = 0; threatRowsScrollPixels = 0; Render(); }, 38);
                DrawThreatPagination(pagerX, pagerY + 7, pageIndex, pageCount, pageSize);
            }
            else
            {
                Text(countText, 21, pagerY + 5, Math.Max(165, layoutWidth - 310), 28,
                    10.9f, Sketch20Style.Ink);
                Text("Số dòng/trang:", layoutWidth - 245, pagerY + 5, 134, 28,
                    10.7f, Sketch20Style.Ink);
                SelectBox("threat-page-size", ThreatPageChoices,
                    Array.IndexOf(new[] { 10, 20, 50 }, threatRequestedRows),
                    layoutWidth - 105, pagerY + 2, 83,
                    index => { threatRequestedRows = new[] { 10, 20, 50 }[index];
                        threatOffset = 0; threatRowsScrollPixels = 0; Render(); }, 37);
                DrawThreatPagination(pagerX, pagerY + 48, pageIndex, pageCount, pageSize);
            }
            ContentHeight = y + cardH;
        }

        // Vẽ tiêu đề căn giữa theo cả hai trục, bằng đúng ô cột của hàng dữ liệu.
        // Không dùng Text() mặc định vì hàm đó căn trái và có thể đặt lệch badge.
        private void ThreatCenteredColumnTitle(string value, int x, int y, int width, int height)
        {
            var bounds = new Rectangle(x, y + bodyOffset, width, height);
            float size = 11.6f * ReferenceTextScale;
            Canvas.TextRuns.Add(new Sketch20TextRun {
                Value = value, Bounds = bounds, Pixels = size, Bold = true
            });
            Canvas.Drawing.Add(g => Sketch20Style.Text(g, value, bounds, size,
                Sketch20Style.Ink, true, false, false, false, true));
        }

        // Trả về chính xác các nút phân trang cần vẽ, có dấu … trước trang cuối.
        private static List<int> ThreatVisiblePageTokens(int pageIndex, int pages)
        {
            var tokens = new List<int>();
            if (pages <= 5)
            {
                for (int i = 0; i < pages; i++) tokens.Add(i);
            }
            else if (pageIndex <= 2)
            {
                tokens.AddRange(new[] { 0, 1, 2, -1, pages - 1 });
            }
            else if (pageIndex >= pages - 3)
            {
                tokens.AddRange(new[] { 0, -1, pages - 3, pages - 2, pages - 1 });
            }
            else
            {
                tokens.AddRange(new[] { 0, -1, pageIndex - 1, pageIndex,
                    pageIndex + 1, -1, pages - 1 });
            }
            return tokens;
        }
        private static int ThreatPaginationWidth(int pageIndex, int pages)
        {
            const int step = 43, ellipsisW = 25;
            List<int> tokens = ThreatVisiblePageTokens(pageIndex, pages);
            return 38 + 7 + tokens.Sum(token => token == -1 ? ellipsisW + 5 : step) + 38;
        }
        private void DrawThreatPagination(int x, int y, int pageIndex, int pages, int pageSize)
        {
            const int pageW = 38, gap = 5;
            var prev = Button("threat-back-page", "", x, y, pageW,
                () => { threatOffset = Math.Max(0, (pageIndex - 1) * pageSize);
                    threatRowsScrollPixels = 0; Render(); },
                "chevron-left", false, false, 38);
            prev.Enabled = pageIndex > 0;
            prev.SquareCorners = true;
            int cursor = x + pageW + 7;
            foreach (int token in ThreatVisiblePageTokens(pageIndex, pages))
            {
                if (token == -1)
                {
                    Text("…", cursor, y, 25, 38, 14f, Sketch20Style.Ink, false);
                    cursor += 30;
                    continue;
                }
                int pageNumber = token;
                var btn = Button("threat-page-" + pageNumber, (pageNumber + 1).ToString(),
                    cursor, y, pageW,
                    () => { threatOffset = pageNumber * pageSize;
                        threatRowsScrollPixels = 0; Render(); },
                    "", pageNumber == pageIndex, false, 38);
                btn.SquareCorners = true;
                cursor += pageW + gap;
            }
            var next = Button("threat-next-page", "Sau", cursor, y, pageW,
                () => { threatOffset = Math.Min((pages - 1) * pageSize,
                    (pageIndex + 1) * pageSize); threatRowsScrollPixels = 0; Render(); },
                "", false, false, 38);
            next.TextPixels = 10.5f;
            next.Enabled = pageIndex < pages - 1;
            next.SquareCorners = true;
        }

        private void DrawThreatSearch(int x, int y, int width)
        {
            Card("threat-search-outline", x, y, width, 41, Color.White, Sketch20Style.Border, 4);
            Icon("search", x + 12, y + 11, 20, Sketch20Style.Ink);
            var input = new TextBox
            {
                Name = "threat-search",
                BorderStyle = BorderStyle.None,
                Font = Sketch20Style.Font(12),
                ForeColor = Sketch20Style.Ink,
                BackColor = Color.White,
                Text = threatSearchText,
                Bounds = new Rectangle(x + 43, y + 10, Math.Max(24, width - 54), 25)
            };
            input.TextChanged += delegate
            {
                threatSearchText = input.Text;
                threatOffset = 0;
                threatRowsScrollPixels = 0;
                pendingSearchCaret = input.SelectionStart;
                pendingSearchName = input.Name;
                searchRenderTimer.Stop(); searchRenderTimer.Start();
            };
            Canvas.Controls.Add(input);
            Canvas.Nodes[input.Name] = input.Bounds;
            SearchCue(input.Handle, 0x1501, IntPtr.Zero, "Tìm tệp hoặc mã độc");
        }
    }

    /// <summary>Một dòng bảng tự vẽ để giữ badge và checkbox đúng ảnh mẫu, không phụ thuộc theme DGV.</summary>
    internal sealed class Sketch20ThreatGridRow : Button
    {
        internal event Action SelectRequested, DetailRequested;
        private readonly Sketch20File file;
        private readonly string category, extension;
        private readonly bool compact, selected;
        private bool hover;
        internal Sketch20ThreatGridRow(Sketch20File item, string categoryText, string extensionText,
            bool compactLayout, bool isSelected)
        {
            file = item; category = categoryText; extension = extensionText;
            compact = compactLayout; selected = isSelected;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Text = ""; Cursor = Cursors.Hand;
            AccessibleName = "Mối đe dọa " + item.Name + ", " + item.Severity;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            MouseEnter += (s, e) => { hover = true; Invalidate(); };
            MouseLeave += (s, e) => { hover = false; Invalidate(); };
        }
        internal static int[] Columns(int width)
        {
            // Các cột cần chữ đầy đủ có kích thước tối thiểu; phần tăng thêm
            // ưu tiên tên tệp và đường dẫn. Chế độ gọn được dùng trước ngưỡng này.
            int[] cols = { 42, 65, 170, 184, 139, 118, 156, 124, 134 };
            int flex = Math.Max(0, width - cols.Sum());
            cols[2] += (int)(flex * .35);
            cols[3] += flex - (int)(flex * .35);
            return cols;
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            if (e.X <= 50) { if (SelectRequested != null) SelectRequested(); }
            else { if (DetailRequested != null) DetailRequested(); }
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        { using (var brush = new SolidBrush(Color.White)) e.Graphics.FillRectangle(brush, ClientRectangle); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(selected ? Color.FromArgb(235, 245, 255) :
                hover ? Color.FromArgb(246, 250, 255) : Color.White)) g.FillRectangle(brush, ClientRectangle);
            using (var p = new Pen(Color.FromArgb(236, 241, 247))) g.DrawLine(p, 1, Height - 1, Width - 1, Height - 1);
            int checkSize = 18;
            int checkY = (Height - checkSize) / 2;
            Sketch20Style.Round(g, new Rectangle(12, checkY, checkSize, checkSize), 0,
                selected ? Sketch20Style.Blue : Color.White,
                selected ? Sketch20Style.Blue : Color.FromArgb(69, 101, 152));
            if (selected) Sketch20Style.Icon(g, "check", new Rectangle(15, checkY + 3, 12, 12), Color.White);
            if (compact)
            {
                PaintCompact(g);
                return;
            }
            int[] cols = Columns(Width);
            int x = cols[0];
            int extW = Math.Min(54, cols[1] - 7);
            DrawExtension(g, x + (cols[1] - extW) / 2, (Height - 28) / 2, extW);
            x += cols[1];
            DrawFullCellText(g, file.Name, new Rectangle(x + 4, 0, cols[2] - 9, Height),
                12.5f, Sketch20Style.Ink, true);
            x += cols[2];
            var pathBounds = new Rectangle(x + 3, 0, cols[3] - 8, Height);
            Sketch20Style.Text(g, CompactThreatPath(file.Path, pathBounds.Width, 11.1f),
                pathBounds, 11.1f, Sketch20Style.Ink);
            x += cols[3];
            DrawFullCellText(g, category, new Rectangle(x + 4, 0, cols[4] - 8, Height),
                11.9f, Sketch20Style.Ink, false, true);
            x += cols[4];
            int severityWidth = Math.Min(cols[5] - 8,
                Math.Max(87, Sketch20Style.Measure(file.Severity ?? "Thấp", 11.5f, true) + 35));
            DrawSeverity(g, x + (cols[5] - severityWidth) / 2,
                (Height - 27) / 2, severityWidth);
            x += cols[5];
            DrawFullCellText(g, ((file.Date == "—" ? "" : file.Date) + " " +
                (file.Time == "—" ? "" : file.Time)).Trim(),
                new Rectangle(x + 3, 0, cols[6] - 6, Height),
                11.4f, Sketch20Style.Ink, false, true);
            x += cols[6];
            string status = file.Resolved ? file.Status : "Chưa xử lý";
            int statusW = Math.Min(cols[7] - 8,
                Math.Max(96, Sketch20Style.Measure(status, 11.7f, true) + 20));
            int statusX = x + (cols[7] - statusW) / 2;
            Sketch20Style.Round(g, new Rectangle(statusX, (Height - 28) / 2,
                statusW, 28), 0, Color.FromArgb(255, 239, 228), Color.Transparent);
            DrawFullCellText(g, status, new Rectangle(statusX + 4, (Height - 28) / 2,
                statusW - 8, 28), 11.7f, Color.FromArgb(187, 74, 20), true, true);
            x += cols[7];
            int actionW = Math.Min(cols[8] - 8, 128), actionY = (Height - 30) / 2;
            int actionX = x + (cols[8] - actionW) / 2;
            Sketch20Style.Round(g, new Rectangle(actionX, actionY, actionW, 30), 0,
                Color.FromArgb(228, 241, 255), Color.Transparent);
            DrawFullCellText(g, "Xem chi tiết", new Rectangle(actionX + 5, actionY,
                actionW - 10, 30), 11.7f, Sketch20Style.Blue, true, true);
        }
        // Chỉ đường dẫn được rút gọn bằng dấu …; giữ lại thư mục gốc và tên tệp cuối.
        private static string CompactThreatPath(string path, int width, float fontSize)
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
                int slash = normalized.LastIndexOf('\\');
                suffix = slash < 0 ? normalized : normalized.Substring(slash + 1);
                prefix = normalized.Length >= 3 && normalized[1] == ':' ? normalized.Substring(0, 3) : "";
            }
            string preferred = prefix + "…\\" + suffix;
            if (Sketch20Style.Measure(preferred, fontSize) <= width) return preferred;
            preferred = "…\\" + suffix;
            if (Sketch20Style.Measure(preferred, fontSize) <= width) return preferred;
            // Tên tệp cực dài: chỉ trong cột đường dẫn mới cho phép rút gọn.
            int left = 0, right = suffix.Length;
            while (left < right)
            {
                int mid = (left + right + 1) / 2;
                string trial = "…" + suffix.Substring(suffix.Length - mid);
                if (Sketch20Style.Measure(trial, fontSize) <= width) left = mid;
                else right = mid - 1;
            }
            return "…" + suffix.Substring(suffix.Length - left);
        }

        // Không tự sinh ba dấu chấm cho tên tệp, mức độ, loại, ngày và trạng thái.
        // Thu font một chút khi cần; nếu vẫn dài và đủ cao, cho phép xuống hai dòng.
        private static void DrawFullCellText(Graphics g, string value, Rectangle bounds,
            float fontSize, Color color, bool bold = false, bool center = false)
        {
            if (bounds.Width <= 1 || bounds.Height <= 1) return;
            string actual = value ?? "";
            float pixels = fontSize;
            while (pixels > 8.8f && Sketch20Style.Measure(actual, pixels, bold) > bounds.Width)
                pixels -= 0.35f;
            bool wrap = Sketch20Style.Measure(actual, pixels, bold) > bounds.Width && bounds.Height >= 38;
            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                TextFormatFlags.PreserveGraphicsClipping |
                TextFormatFlags.PreserveGraphicsTranslateTransform;
            if (wrap) flags |= TextFormatFlags.WordBreak;
            else flags |= TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
            if (center) flags |= TextFormatFlags.HorizontalCenter;
            using (var oldClip = g.Clip)
            {
                try
                {
                    g.SetClip(bounds, System.Drawing.Drawing2D.CombineMode.Intersect);
                    TextRenderer.DrawText(g, actual, Sketch20Style.Font(pixels, bold),
                        bounds, color, flags);
                }
                finally { g.SetClip(oldClip, System.Drawing.Drawing2D.CombineMode.Replace); }
            }
        }

        private void PaintCompact(Graphics g)
        {
            int y = Height <= 70 ? 5 : 8;
            DrawExtension(g, 48, y, 48);
            DrawFullCellText(g, file.Name,
                new Rectangle(108, y, Math.Max(90, Width - 330), 25),
                12.6f, Sketch20Style.Ink, true);
            var compactPath = new Rectangle(108, y + 23, Math.Max(90, Width - 328), 22);
            Sketch20Style.Text(g, CompactThreatPath(file.Path, compactPath.Width, 11.2f),
                compactPath, 11.2f, Sketch20Style.Muted);
            int sevX = Width - 208;
            DrawSeverity(g, sevX, (Height - 28) / 2, 95);
            Sketch20Style.Round(g, new Rectangle(Width - 106, (Height - 31) / 2, 101, 31),
                0, Sketch20Style.BlueSoft, Color.Transparent);
            DrawFullCellText(g, "Chi tiết",
                new Rectangle(Width - 101, (Height - 31) / 2, 93, 31),
                11.6f, Sketch20Style.Blue, true);
        }
        private void DrawExtension(Graphics g, int x, int y, int w)
        {
            Color color = extension == "EXE" ? Color.FromArgb(0, 116, 225) :
                extension == "PS1" ? Color.FromArgb(92, 43, 188) :
                extension == "ZIP" ? Color.FromArgb(190, 87, 14) : Color.FromArgb(18, 147, 98);
            Color soft = extension == "EXE" ? Color.FromArgb(217, 239, 255) :
                extension == "PS1" ? Color.FromArgb(239, 229, 255) :
                extension == "ZIP" ? Color.FromArgb(255, 235, 213) : Color.FromArgb(219, 249, 236);
            Sketch20Style.Round(g, new Rectangle(x, y, w, 28), 0, soft, Color.Transparent);
            DrawFullCellText(g, extension, new Rectangle(x + 1, y, w - 2, 28),
                11.8f, color, true, true);
        }
        private void DrawSeverity(Graphics g, int x, int y, int width)
        {
            string severity = file.Severity ?? "Thấp";
            bool high = severity == "Cao";
            bool medium = severity == "Trung bình";
            Color accent = high ? Color.FromArgb(213, 29, 43) : medium ? Color.FromArgb(216, 125, 8) : Color.FromArgb(174, 117, 17);
            Color soft = high ? Color.FromArgb(255, 237, 239) : Color.FromArgb(255, 246, 225);
            Sketch20Style.Round(g, new Rectangle(x, y, Math.Max(36, width), 27), 0, soft, Color.Transparent);
            int textW = Math.Min(width - 26, Sketch20Style.Measure(severity, 11.5f, true));
            int groupW = Math.Max(0, textW) + 14;
            int groupX = x + Math.Max(4, (width - groupW) / 2);
            using (var b = new SolidBrush(high ? Color.FromArgb(244, 30, 52) : Color.FromArgb(247, 166, 15)))
                g.FillEllipse(b, groupX, y + 9, 9, 9);
            DrawFullCellText(g, severity,
                new Rectangle(groupX + 14, y, Math.Max(1, width - (groupX - x) - 17), 27),
                11.5f, accent, true);
        }
    }
}
