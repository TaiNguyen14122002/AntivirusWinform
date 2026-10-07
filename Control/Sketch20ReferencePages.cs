using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    internal sealed partial class Sketch20View
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, EntryPoint = "SendMessageW")]
        private static extern IntPtr SearchCue(IntPtr handle, int message, IntPtr parameter, string text);
        private void ResetPageScroll()
        {
            viewport.AutoScrollPosition = Point.Empty; PositionSettingsShortcut();
            if (IsHandleCreated) BeginInvoke((Action)(() => { if (IsDisposed) return; viewport.AutoScrollPosition = Point.Empty; PositionSettingsShortcut(); }));
        }
        private static DateTime QuarantineDate(Sketch20File file)
        {
            DateTime result;
            return DateTime.TryParseExact((file.Date ?? "") + " " + (file.Time ?? ""), new[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result) ? result : DateTime.MinValue;
        }
        private void SortQuarantine(Sketch20Model model)
        { model.Quarantine = quarantineOldest ? model.Quarantine.OrderBy(QuarantineDate).ThenBy(f => f.Index).ToList() : model.Quarantine.OrderByDescending(QuarantineDate).ThenBy(f => f.Index).ToList(); }
        private static void OpenFirewallSettings()
        {
            try { System.Diagnostics.Process.Start("control.exe", "/name Microsoft.WindowsFirewall"); }
            catch (Exception error) { MessageBox.Show("Không mở được cài đặt tường lửa: " + error.Message, "Tường lửa", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        private void AddTableScroll(string name, int x, int y, int height, int count, int page, int offset, Action<int> changed)
        {
            if (count <= page) return;
            var bar = new VScrollBar { Name = name, AccessibleName = name == "strings-scroll" ? "Cuộn chuỗi ký tự" : name == "history-scroll" ? "Cuộn lịch sử" : name == "quarantine-scroll" ? "Cuộn tệp cách ly" : "Cuộn kết quả VirusTotal", Bounds = new Rectangle(x, y, 15, height), Minimum = 0, Maximum = count - 1, LargeChange = page, SmallChange = 1, Value = offset };
            bar.Scroll += delegate(object sender, ScrollEventArgs e) { changed(e.NewValue); if (e.Type != ScrollEventType.ThumbTrack) BeginInvoke((Action)(() => { if (IsDisposed) return; Render(); var next = Canvas.Controls[name]; if (next != null) next.Focus(); })); }; Canvas.Controls.Add(bar); Canvas.Nodes[name] = bar.Bounds;
        }
        private void FilledIcon(string name, int x, int y, int size, Color color)
        {
            var bounds = new Rectangle(x, y + bodyOffset, size, size);
            Canvas.Drawing.Add(g => Sketch20ReferenceAssets.FilledIcon(g, name, bounds, color));
        }
        private void ReferenceProtection()
        {
            const int heroY = 81, heroH = 184;
            GradientCard("protection-summary", 0, heroY, layoutWidth, heroH, ColorTranslator.FromHtml("#eef7ff"), ColorTranslator.FromHtml("#def1ff"), Sketch20Style.Border);
            Artwork("protection-art", 14, heroY + 12, 150, 161, Model.Running ? "reference-protection" : "reference-off");
            int copyX = 174, rightW = layoutWidth < 850 ? 218 : 232, rightX = layoutWidth - rightW - 22, copyW = rightX - copyX - 26;
            int leadWidth = Sketch20Style.Measure("Bảo vệ thời gian thực", 17.5f * ReferenceTextScale, true);
            Text("Bảo vệ thời gian thực", copyX, heroY + 24, leadWidth + 2, 32, 17.5f, Sketch20Style.Ink, true);
            Text(Model.Running ? "đang bật" : "đang tắt", copyX + leadWidth + 6, heroY + 24, copyW - leadWidth - 6, 32, 17.5f, Model.Running ? Sketch20Style.Green : Sketch20Style.Amber, true);
            Text(Model.Running ? "Thiết bị của bạn đang được bảo vệ toàn diện." : "Bật bảo vệ để theo dõi các thay đổi trên máy.", copyX, heroY + 62, copyW, 25, 11.5f, Sketch20Style.Muted);
            string[] bullets = { "Hệ thống được giám sát 24/7", "Tự động ngăn chặn các mối đe dọa", "Hiệu suất tối ưu, không làm chậm máy" };
            for (int i = 0; i < bullets.Length; i++)
            {
                int yy = heroY + 98 + i * 24;
                Canvas.Drawing.Add(g => { using (var b = new SolidBrush(Sketch20Style.Green)) g.FillEllipse(b, copyX, yy + 4, 15, 15); });
                Icon("check", copyX + 3, yy + 7, 9, Color.White);
                Text(bullets[i], copyX + 23, yy, copyW - 23, 22, 10.5f, Sketch20Style.Muted);
            }
            Canvas.Drawing.Add(g => { using (var p = new Pen(Sketch20Style.Border)) g.DrawLine(p, rightX - 22, heroY + 25, rightX - 22, heroY + heroH - 24); });
            Text("Cơ sở dữ liệu chữ ký", rightX, heroY + 39, rightW, 25, 10.5f, Sketch20Style.Muted);
            Text(Model.Signature, rightX, heroY + 67, rightW - 78, 26, 11.5f, Sketch20Style.Ink, true);
            Badge("Mới nhất", rightX + rightW - 72, heroY + 67, "safe", 72);
            Button("update-signatures", "Cập nhật chữ ký", rightX, heroY + 110, rightW, () => Command("update"), "refresh", true, false, 45);
            int sectionY = heroY + heroH + 13, gridY = sectionY + 39;
            Text("Các tính năng bảo vệ", 0, sectionY, layoutWidth - 190, 38, 19, Sketch20Style.Ink, true);
            Button("manage-modules", "Quản lý module →", layoutWidth - 165, sectionY, 165, () => { SettingsTab = 1; Go("settings"); }, "", false, false, 31, true);
            string[] icons = { "shield-check", "file", "usb", "download", "monitor", "power", "cube", "globe" };
            string[] notes = {
                "Liên tục giám sát hệ thống, ngăn chặn virus, mã độc và các mối đe dọa ngay khi chúng xuất hiện.",
                "Quét và kiểm tra các tệp khi mở, sao chép hoặc chỉnh sửa để phát hiện mối đe dọa.",
                "Tự động quét các thiết bị USB khi kết nối, ngăn chặn virus lây lan qua thiết bị lưu trữ ngoài.",
                "Kiểm tra các tệp tải xuống từ Internet, ngăn chặn tệp độc hại trước khi chúng được mở.",
                "Phát hiện và ngăn chặn các hành vi đáng ngờ của ứng dụng theo thời gian thực.",
                "Ngăn chặn các chương trình độc hại tự động khởi động cùng Windows.",
                "Tự động đưa các tệp nghi ngờ vào vùng cách ly an toàn, tránh lây nhiễm.",
                "Kiểm tra liên kết và tệp tin với VirusTotal để phát hiện mối đe dọa trực tuyến." };
            Color[] soft = { Sketch20Style.GreenSoft, Sketch20Style.BlueSoft, ColorTranslator.FromHtml("#f1ebff"), ColorTranslator.FromHtml("#e8faff"), Sketch20Style.BlueSoft, Sketch20Style.RedSoft, Sketch20Style.AmberSoft, Sketch20Style.BlueSoft };
            Color[] ink = { Sketch20Style.Green, Sketch20Style.Blue, ColorTranslator.FromHtml("#7c35ff"), Sketch20Style.Blue, Sketch20Style.Blue, Sketch20Style.Red, Sketch20Style.Amber, Sketch20Style.Blue };
            const int gap = 13;
            int cardW = (layoutWidth - gap) / 2, cardH = layoutWidth < 850 ? 146 : 125;
            for (int i = 0; i < 8; i++)
            {
                int x = i % 2 * (cardW + gap), y = gridY + i / 2 * (cardH + gap), index = i;
                string key = FeatureKeys[i]; bool on = Model.Flag(key), needsKey = key == "vtq" && !Model.KeyConfigured;
                Card("feature-" + key, x, y, cardW, cardH);
                string[] tiles = { "rt", "file", "usb", "download", "behavior", "startup", "aq", "web" };
                Artwork("feature-icon-" + i, x + 14, y + 14, 50, 51, "reference-icon-feature-" + tiles[i]);
                Text(FeatureNames[i], x + 78, y + Sketch20Style.RowPaddingY, cardW - 187, Sketch20Style.TitleLineHeight, 12, Sketch20Style.Ink, true, true);
                Badge(on ? "Đang bật" : needsKey ? "Chưa cấu hình" : "Đang tắt", x + 78, y + 42, on ? "safe" : "warn", needsKey ? 108 : 79);
                Text(notes[i], x + 78, y + 72, cardW - 94, cardH - 80, 11, Sketch20Style.Muted, false, true);
                Button("feature-settings-" + key, "", x + cardW - 94, y + 18, 31, () => { SettingsTab = index == 7 ? 2 : 1; Go("settings"); }, "settings", false, false, 31);
                Switch(key, FeatureNames[i], x + cardW - 49, y + 21);
            }
            ContentHeight = gridY + 4 * cardH + 3 * gap + 8;
        }
        private void ReferenceQuarantine()
        {
            Card("quarantine-notice", 0, 81, layoutWidth, 66, Sketch20Style.BlueSoft, Sketch20Style.Border, 11);
            Canvas.Drawing.Add(g => { using (var b = new SolidBrush(Sketch20Style.Blue)) g.FillEllipse(b, 19, 101, 25, 25); });
            Icon("info", 21, 103, 21, Color.White);
            Text("Các tệp trong khu vực cách ly đã bị vô hiệu hóa và không thể gây hại cho hệ thống.", 58, 87, layoutWidth - 104, 27, 11.5f, Sketch20Style.Ink);
            Text("Bạn có thể khôi phục tệp nếu chắc chắn chúng an toàn, hoặc xóa vĩnh viễn nếu không cần thiết.", 58, 115, layoutWidth - 104, 23, 10.5f, Sketch20Style.Muted);
            SortQuarantine(Model);
            int rows = Model.Quarantine.Count, rowH = 80, listY = 163;
            int visible = Math.Min(rows, Math.Max(2, Math.Min(6, (viewport.Height - 410) / rowH)));
            quarantineOffset = Math.Max(0, Math.Min(quarantineOffset, Math.Max(0, rows - visible)));
            int listH = 94 + Math.Max(1, visible) * rowH + 8;
            Card("quarantine-list", 0, listY, layoutWidth, listH);
            Text("Khu vực cách ly", 18, listY + 10, layoutWidth - 280, 38, 19, Sketch20Style.Ink, true);
            Text("Tổng " + rows + " tệp", layoutWidth - 291, listY + 13, 105, 27, 11, Sketch20Style.Muted, false, false, true);
            SelectBox("quarantine-sort", new[] { "Mới nhất trước", "Cũ nhất trước" }, quarantineOldest ? 1 : 0, layoutWidth - 178, listY + 10, 160, v => { quarantineOffset = 0; quarantineOldest = v == 1; SortQuarantine(Model); Render(); }, 35);
            int head = listY + 55;
            Card("quarantine-table-header", 14, head, layoutWidth - 28, 34, Sketch20Style.GridHeader, Color.Transparent, 7);
            int severityX = (int)(layoutWidth * .34), reasonX = (int)(layoutWidth * .49), dateX = (int)(layoutWidth * .72), actionX = (int)(layoutWidth * .87);
            Check("quarantine-all", "", 20, head + 4, 25, rows > 0 && quarantineSelection.Count == rows, selected => { quarantineSelection.Clear(); if (selected) foreach (var f in Model.Quarantine) quarantineSelection.Add(f.Index); if (Actions.SelectAllQuarantine != null) Actions.SelectAllQuarantine(selected); Render(); });
            Text("Tệp tin", 65, head, severityX - 75, 34, 10.5f, Sketch20Style.Muted);
            Text("Độ nghiêm trọng", severityX, head, reasonX - severityX - 10, 34, 10.5f, Sketch20Style.Muted);
            Text("Lý do phát hiện", reasonX, head, dateX - reasonX - 12, 34, 10.5f, Sketch20Style.Muted);
            Text("Ngày cách ly", dateX, head, actionX - dateX - 8, 34, 10.5f, Sketch20Style.Muted);
            Text("Thao tác", actionX, head, layoutWidth - actionX - 16, 34, 10.5f, Sketch20Style.Muted);
            AddTableScroll("quarantine-scroll", layoutWidth - 17, head + 35, visible * rowH, rows, visible, quarantineOffset, value => quarantineOffset = value);
            for (int i = 0; i < visible; i++)
            {
                var f = Model.Quarantine[quarantineOffset + i]; int top = head + 35 + i * rowH, index = f.Index;
                Card("quarantine-row-" + i, 14, top, layoutWidth - 28, rowH - 1, i % 2 == 0 ? Color.White : Sketch20Style.GridAlt, Sketch20Style.Border, 0);
                Check("quarantine-pick-" + i, "", 20, top + 27, 25, quarantineSelection.Contains(index), selected => { if (selected) quarantineSelection.Add(index); else quarantineSelection.Remove(index); if (Actions.SelectQuarantine != null) Actions.SelectQuarantine(index, selected); Render(); });
                Color fg = Kind(f.Severity) == "risk" ? Sketch20Style.Red : Sketch20Style.Blue;
                FilledIcon("file", 64, top + 26, 27, fg);
                TwoLineText(f.Name, Model.Sample ? f.Path : System.IO.Path.GetDirectoryName(f.Path ?? ""), 107, top + 12, severityX - 122, 12, 10.4f, Sketch20Style.Ink, Sketch20Style.Muted, true, true);
                Badge(f.Severity, severityX, top + 27, Kind(f.Severity), Math.Min(110, reasonX - severityX - 13));
                Text(f.Reason, reasonX, top + 21, dateX - reasonX - 14, 43, 11, Sketch20Style.Muted, false, true);
                TwoLineText(f.Date, f.Time, dateX, top + 16, actionX - dateX - 10, 10.7f, 10.5f, Sketch20Style.Muted, Sketch20Style.Muted, false);
                Button("quarantine-detail-" + i, "Chi tiết", actionX, top + 25, layoutWidth - actionX - 18, () => OpenDetail(index, true), "chevron-right", false, false, 33, true);
            }
            if (rows == 0) Text(string.IsNullOrWhiteSpace(Model.QuarantineNotice) ? "Không có tệp nào trong khu vực cách ly." : Model.QuarantineNotice, 22, head + 55, layoutWidth - 44, 55, 12.5f, Sketch20Style.Muted, false, true);
            int footer = listY + listH + 18;
            Card("quarantine-actions", 0, footer, layoutWidth, 84);
            FilledIcon("shield-check", 18, footer + 27, 24, Sketch20Style.Blue);
            Text("Đã chọn " + quarantineSelection.Count + " tệp", 54, footer + 16, layoutWidth - 406, 26, 12, Sketch20Style.Ink, true);
            Text("Bạn có thể khôi phục hoặc xóa vĩnh viễn các tệp đã chọn.", 54, footer + 44, layoutWidth - 406, 24, 10.5f, Sketch20Style.Muted);
            var restore = Button("restore-selected", "Khôi phục", layoutWidth - 321, footer + 21, 143, () => Command("restore"), "undo", false, false, 44); restore.Selected = true; restore.Enabled = quarantineSelection.Count > 0;
            var delete = Button("delete-selected", "Xóa vĩnh viễn", layoutWidth - 165, footer + 21, 147, () => Command("delete"), "trash", true, true, 44); delete.Enabled = quarantineSelection.Count > 0;
            ContentHeight = footer + 96;
        }
        private void ReferenceDetailTabs(int y, string[] names, string[] icons, int[] indices, int selected)
        {
            if (names.Length == 4) Card("detail-tabs", 0, y, layoutWidth, 47, Color.White, Sketch20Style.Border, 8);
            int x = 0, gap = 4, available = layoutWidth - gap * (names.Length - 1);
            int[] widths = names.Select(t => Sketch20Style.Measure(t, 12) + 46).ToArray();
            int sum = widths.Sum();
            for (int i = 0; i < names.Length; i++)
            {
                int index = indices[i], width = i == names.Length - 1 ? layoutWidth - x : widths[i] * available / sum;
                var b = new Sketch20Button(names[i], icons[i]) { Name = "tab-" + i, Tab = true, UnderlineOnly = true, Selected = i == selected, TextPixels = 12 * ReferenceTextScale, IconPixels = 17, Bounds = new Rectangle(x, y, width, 46) };
                b.Click += delegate { DetailTab = index; if (Actions.SelectDetailTab != null) Actions.SelectDetailTab(index); RefreshModel(true); Render(); ResetPageScroll(); };
                Canvas.Controls.Add(b); Canvas.Nodes[b.Name] = b.Bounds; x += width + gap;
            }
            Line(0, y + 46, layoutWidth); Canvas.Nodes["tabs"] = new Rectangle(0, y, layoutWidth, 47);
        }
        private void ReferenceDetails()
        {
            var f = Model.Detail;
            bool analysis = DetailTab == 2 || DetailTab == 3, extra = DetailTab == 4;
            if (analysis)
            {
                Button("details-back", "Quay lại danh sách", 0, 1, 170, () => Go(DetailReturnPage), "arrow-left", false, false, 29, true);
                Card("file-summary", 0, 46, layoutWidth, 137, Sketch20Style.Background, Color.Transparent);
                Card("file-summary-icon", 6, 52, 79, 81);
                FilledIcon("file", 26, 68, 41, Sketch20Style.Blue);
                Text(f == null ? "Chưa chọn tệp" : f.Name, 107, 58, layoutWidth - 168, 36, 22, Sketch20Style.Ink, true);
                Text(f == null ? "Chọn tệp từ danh sách để xem chi tiết." : f.Path, 107, 98, layoutWidth - 145, 25, 12.5f, Sketch20Style.Muted);
                Badge(f == null ? "Chưa có dữ liệu" : "Đáng ngờ", 107, 137, "risk", 108);
                Badge(f == null ? "—" : f.Size, 228, 137, "info", 110);
                Badge(f == null ? "—" : System.IO.Path.GetExtension(f.Name ?? "").TrimStart('.').ToUpperInvariant(), 351, 137, "info", 68);
                Badge(f == null ? "—" : f.Date + ", " + f.Time, 432, 137, "info", 159);
                ReferenceDetailTabs(190, new[] { "Tổng quan", "Hành vi", "Chuỗi ký tự", "Chi tiết", "Ngày ký", "Độ tin cậy" }, new[] { "house", "activity", "files", "file", "clock", "shield-check" }, new[] { 0, 2, 3, 4, 4, 1 }, DetailTab == 2 ? 1 : 2);
                detailBodyY = 253;
            }
            else if (extra)
            {
                Button("details-back", "", 0, 2, 31, () => Go(DetailReturnPage), "arrow-left", false, false, 32, true);
                Text("Chi tiết tệp", 45, 0, layoutWidth - 230, 44, 27, Sketch20Style.Ink, true);
                Text("Xem thông tin chi tiết và trạng thái của tệp được cách ly.", 45, 38, layoutWidth - 230, 27, 12.5f, Sketch20Style.Muted);
                Card("file-summary", 0, 83, layoutWidth, 117);
                Card("file-summary-icon", 17, 100, 72, 80, Sketch20Style.BlueSoft, Color.Transparent, 14);
                FilledIcon("file", 36, 123, 32, Sketch20Style.Blue);
                int divider = (int)(layoutWidth * .6);
                Text(f == null ? "Chưa chọn tệp" : f.Name, 107, 96, divider - 130, 34, 21, Sketch20Style.Ink, true);
                Badge(f == null ? "Chưa có dữ liệu" : f.Severity, 107, 136, f == null ? "info" : Kind(f.Severity), 110);
                Text(f == null ? "—" : f.Path, 107, 166, divider - 127, 25, 10.8f, Sketch20Style.Muted);
                Canvas.Drawing.Add(g => { using (var p = new Pen(Sketch20Style.Border)) g.DrawLine(p, divider, 103, divider, 179); });
                Text("Kích thước", divider + 20, 101, 120, 27, 12, Sketch20Style.Muted);
                Text(f == null ? "—" : f.Size, divider + 150, 101, layoutWidth - divider - 174, 27, 12, Sketch20Style.Ink);
                Text("Ngày phát hiện", divider + 20, 140, 120, 27, 12, Sketch20Style.Muted);
                Text(f == null ? "—" : f.Date + ", " + f.Time, divider + 150, 140, layoutWidth - divider - 174, 27, 11.5f, Sketch20Style.Ink);
                ReferenceDetailTabs(212, new[] { "Tổng quan", "Phân tích", "Hoạt động", "Thông tin bổ sung" }, new[] { "house", "sigma", "activity", "shield-check" }, new[] { 0, 1, 2, 4 }, 3);
                detailBodyY = 276;
            }
            else
            {
                GradientCard("file-summary", 0, 114, layoutWidth, 129, f == null ? Color.White : ColorTranslator.FromHtml("#fff0f4"), ColorTranslator.FromHtml("#fff5f7"), Sketch20Style.Border);
                Artwork("file-summary-icon", 23, 136, 77, 81, "reference-icon-file-gear");
                int rightW = 227, rightX = layoutWidth - rightW - 23;
                Text(f == null ? "Chưa chọn tệp" : f.Name, 111, 128, rightX - 131, 36, 21, Sketch20Style.Ink, true);
                Text(f == null ? "Chọn tệp từ danh sách để xem chi tiết." : f.Path, 111, 166, rightX - 132, 26, 11.5f, Sketch20Style.Muted);
                Icon("file", 111, 207, 16, Sketch20Style.Muted); Text(f == null ? "—" : f.Size, 139, 200, 96, 29, 11, Sketch20Style.Muted);
                Icon("clock", 253, 207, 16, Sketch20Style.Muted); Text(f == null ? "—" : "Sửa đổi: " + f.Date + ", " + f.Time, 279, 200, Math.Max(100, rightX - 291), 29, 11, Sketch20Style.Muted);
                Badge(f == null ? "Chưa có dữ liệu" : f.Status, rightX, 138, f == null ? "info" : "risk", rightW);
                Text(f == null ? "Thông tin sẽ hiển thị khi chọn tệp." : f.Resolved ? "Tệp này đã được cách ly\nđể ngăn nguy cơ lây nhiễm." : "Kiểm tra thông tin và kết quả\nphân tích trước khi sử dụng.", rightX, 179, rightW, 48, 11, Sketch20Style.Muted, false, true);
                ReferenceDetailTabs(258, new[] { "Thông tin chi tiết", "VirusTotal", "Hoạt động", "Thông tin bổ sung" }, new[] { "file", "sigma", "activity", "clock" }, new[] { 0, 1, 2, 4 }, DetailTab);
                detailBodyY = 322;
            }
            int height;
            if (DetailTab == 0) { height = 466; DetailInfo(f, height); }
            else if (DetailTab == 1) { height = VirusTotalHeight(); DetailVirusTotal(f, height); }
            else if (DetailTab == 2) { height = Math.Max(364, 94 + Math.Max(1, Model.Behavior.Count) * 84); DetailBehavior(f, height); }
            else if (DetailTab == 3) { height = Math.Max(320, viewport.Height - detailBodyY - 48); DetailStrings(height); }
            else { height = 460; DetailExtra(f, height); }
            ContentHeight = detailBodyY + height + 12;
        }
    }
}
