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
            viewport.AutoScrollPosition = Point.Empty;
            if (IsHandleCreated) BeginInvoke((Action)(() => { if (IsDisposed) return; viewport.AutoScrollPosition = Point.Empty; }));
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
            catch (Exception error) { AppDialog.Show("Không mở được cài đặt tường lửa: " + error.Message, "Tường lửa", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        private void AddTableScroll(string name, int x, int y, int height, int count, int page, int offset, Action<int> changed)
        {
            if (count <= page) return;
            int maxOffset = Math.Max(0, count - page);
            offset = Math.Max(0, Math.Min(offset, maxOffset));
            var bar = new VScrollBar
            {
                Name = name,
                AccessibleName = name == "strings-scroll" ? "Cuộn chuỗi ký tự" : name == "history-scroll" ? "Cuộn lịch sử" : name == "quarantine-scroll" ? "Cuộn tệp cách ly" : name == "threat-scroll" ? "Cuộn danh sách mối đe dọa" : "Cuộn kết quả VirusTotal",
                Bounds = new Rectangle(x, y, 15, height), Minimum = 0, Maximum = count - 1,
                LargeChange = Math.Max(1, page), SmallChange = 1, Value = offset, TabStop = false
            };
            bar.Scroll += delegate(object sender, ScrollEventArgs e)
            {
                int value = Math.Max(0, Math.Min(maxOffset, e.NewValue));
                changed(value);
                if (e.Type != ScrollEventType.ThumbTrack)
                    BeginInvoke((Action)(() => { if (!IsDisposed) Render(); }));
            };
            // Một số theme Windows chỉ phát ThumbTrack khi kéo; MouseUp đảm bảo dữ liệu
            // luôn render theo vị trí cuối cùng sau khi thả thumb.
            bar.MouseUp += delegate { changed(Math.Max(0, Math.Min(maxOffset, bar.Value))); BeginInvoke((Action)(() => { if (!IsDisposed) Render(); })); };
            Canvas.Controls.Add(bar); Canvas.Nodes[name] = bar.Bounds;
        }
        private void FilledIcon(string name, int x, int y, int size, Color color)
        {
            var bounds = new Rectangle(x, y + bodyOffset, size, size);
            Canvas.Drawing.Add(g => Sketch20ReferenceAssets.FilledIcon(g, name, bounds, color));
        }
        private void ReferenceProtection()
        {
            const int heroY = 81;
            int viewportBottom = Math.Max(520, viewport.ClientSize.Height - Canvas.Top - 8);
            int availableH = Math.Max(430, viewportBottom - heroY);
            int heroH = Math.Max(126, Math.Min(144, (int)(availableH * 0.23f)));
            GradientCard("protection-summary", 0, heroY, layoutWidth, heroH, ColorTranslator.FromHtml("#eef7ff"), ColorTranslator.FromHtml("#def1ff"), Sketch20Style.Border);
            int artW = Math.Min(118, heroH - 18);
            Artwork("protection-art", 14, heroY + 8, artW, heroH - 16, Model.Running ? "reference-protection" : "reference-off");
            int copyX = artW + 32, copyW = layoutWidth - copyX - 24;
            int leadWidth = Sketch20Style.Measure("Bảo vệ thời gian thực", 16.5f * ReferenceTextScale, true);
            Text("Bảo vệ thời gian thực", copyX, heroY + 14, leadWidth + 2, 28, 16.5f, Sketch20Style.Ink, true);
            Text(Model.Running ? "đang bật" : "đang tắt", copyX + leadWidth + 6, heroY + 14, copyW - leadWidth - 6, 28, 16.5f, Model.Running ? Sketch20Style.Green : Sketch20Style.Amber, true);
            Text(Model.Running ? "Thiết bị của bạn đang được bảo vệ toàn diện." : "Bật bảo vệ để theo dõi các thay đổi trên máy.", copyX, heroY + 44, copyW, 22, 10.7f, Sketch20Style.Muted);
            string summary = "Giám sát 24/7  •  Tự động ngăn chặn  •  Tối ưu hiệu suất";
            int summaryY = heroY + heroH - 38;
            Card("protection-summary-dot", copyX, summaryY + 2, 18, 18, Sketch20Style.GreenSoft, Color.Transparent, 9);
            Icon("check", copyX + 5, summaryY + 7, 8, Sketch20Style.Green);
            Text(summary, copyX + 26, summaryY, copyW - 26, 22, 10.1f, Sketch20Style.Muted);

            int sectionY = heroY + heroH + 8, gridY = sectionY + 36;
            Card("protection-section-icon", 0, sectionY, 30, 30, ColorTranslator.FromHtml("#edf5ff"), Color.Transparent, 9);
            Icon("shield-check", 8, sectionY + 8, 14, Sketch20Style.Blue);
            Text("Các tính năng bảo vệ", 40, sectionY - 2, layoutWidth - 205, 32, 16.5f, Sketch20Style.Ink, true);
            Button("manage-modules", "Quản lý module →", layoutWidth - 150, sectionY, 150, () => { SettingsTab = 1; Go("settings"); }, "", false, false, 28, true);

            string[] notes = {
                "Giám sát hệ thống và ngăn chặn mối đe dọa theo thời gian thực.",
                "Kiểm tra tệp khi mở, sao chép hoặc chỉnh sửa.",
                "Tự động quét thiết bị USB khi kết nối.",
                "Kiểm tra tệp tải xuống trước khi được mở.",
                "Theo dõi các hành vi ứng dụng đáng ngờ.",
                "Kiểm tra chương trình tự khởi động cùng Windows.",
                "Tự động chuyển tệp nghi ngờ vào vùng cách ly.",
                "Đối chiếu liên kết và tệp tin bằng VirusTotal." };
            string[] tiles = { "rt", "file", "usb", "download", "behavior", "startup", "aq", "web" };
            const int gap = 8;
            int cardW = (layoutWidth - gap) / 2;
            int availableCardsH = Math.Max(228, viewportBottom - gridY - 8);
            int cardH = Math.Max(54, Math.Min(68, (availableCardsH - gap * 3) / 4));
            for (int i = 0; i < 8; i++)
            {
                int x = i % 2 * (cardW + gap), y = gridY + i / 2 * (cardH + gap), index = i;
                string key = FeatureKeys[i]; bool on = Model.Flag(key), needsKey = key == "vtq" && !Model.KeyConfigured;
                Card("feature-" + key, x, y, cardW, cardH, Color.White, ColorTranslator.FromHtml("#dce7f5"), 12);
                Artwork("feature-icon-" + i, x + 10, y + 9, 36, 37, "reference-icon-feature-" + tiles[i]);
                Text(FeatureNames[i], x + 56, y + 6, cardW - 149, 21, 10.8f, Sketch20Style.Ink, true);
                string status = on ? "Đang bật" : needsKey ? "Chưa cấu hình" : "Đang tắt";
                Color statusColor = on ? Sketch20Style.Green : Sketch20Style.Amber;
                Canvas.Drawing.Add(g => { using (var b = new SolidBrush(statusColor)) g.FillEllipse(b, x + 57, y + 31 + bodyOffset, 6, 6); });
                Text(status, x + 69, y + 25, needsKey ? 96 : 64, 18, 8.9f, statusColor, true);
                Text(notes[i], x + 56, y + cardH - 21, Math.Max(80, cardW - 68), 16, 8.9f, Sketch20Style.Muted);
                if (index != 7) Button("feature-settings-" + key, "", x + cardW - 83, y + 8, 27, () => { SettingsTab = 1; Go("settings"); }, "settings", false, false, 27);
                Switch(key, FeatureNames[i], x + cardW - 47, y + 10);
            }
            ContentHeight = Math.Min(viewportBottom, gridY + 4 * cardH + 3 * gap + 6);
        }
        private void ReferenceQuarantine()
        {
            // Banner thông tin: rộng, thoáng và nhẹ hơn để tách khỏi card nội dung.
            Card("quarantine-notice", 0, 78, layoutWidth, 74, ColorTranslator.FromHtml("#eef6ff"), ColorTranslator.FromHtml("#d9e9fb"), 14);
            Card("quarantine-notice-icon", 18, 94, 42, 42, ColorTranslator.FromHtml("#dcecff"), Color.Transparent, 21);
            Icon("info", 28, 104, 22, Sketch20Style.Blue);
            Text("Các tệp trong khu vực cách ly đã bị vô hiệu hóa và không thể gây hại cho hệ thống.", 74, 88, layoutWidth - 100, 28, 12.1f, Sketch20Style.Ink, true);
            Text("Bạn có thể khôi phục tệp nếu chắc chắn chúng an toàn, hoặc xóa vĩnh viễn nếu không cần thiết.", 74, 116, layoutWidth - 100, 24, 10.6f, Sketch20Style.Muted);

            SortQuarantine(Model);
            const int rowH = Sketch20Style.TableRowHeight, rowGap = Sketch20Style.TableRowGap;
            int rows = Model.Quarantine.Count, listY = 170;
            int visible = Math.Min(rows, Math.Max(2, Math.Min(6, (viewport.Height - 455) / (rowH + rowGap))));
            quarantineOffset = Math.Max(0, Math.Min(quarantineOffset, Math.Max(0, rows - Math.Max(1, visible))));
            int rowsHeight = rows == 0 ? 164 : visible * (rowH + rowGap) - rowGap;
            int listH = 138 + rowsHeight + Sketch20Style.TableOuterPaddingY * 2;

            // Card chính.
            Card("quarantine-list", 0, listY, layoutWidth, listH, Color.White, ColorTranslator.FromHtml("#dce7f5"), 18);
            Card("quarantine-title-icon", 22, listY + 20, 54, 54, ColorTranslator.FromHtml("#edf5ff"), Color.Transparent, 15);
            Icon("archive", 37, listY + 35, 24, Sketch20Style.Blue);
            Text("Khu vực cách ly", 92, listY + 16, layoutWidth - 500, 35, 22, Sketch20Style.Ink, true);
            Text("Các tệp đã được vô hiệu hóa an toàn.", 92, listY + 52, layoutWidth - 520, 24, 11.1f, Sketch20Style.Muted);
            // Chỉ giữ bộ sắp xếp ở góc phải; bỏ cụm tổng số tệp.
            int sortW = 176, sortX = layoutWidth - sortW - 22;
            int headerY = listY + 24;
            HeaderSelectBox("quarantine-sort", new[] { "Mới nhất trước", "Cũ nhất trước" }, quarantineOldest ? 1 : 0, sortX, headerY, sortW, 34, 10.8f, v => { quarantineOffset = 0; quarantineOldest = v == 1; SortQuarantine(Model); Render(); });

            int head = listY + 96, rowX = 22;
            bool hasScroll = rows > visible;
            int rowW = layoutWidth - rowX - (hasScroll ? 43 : 22);
            // Luôn chừa đủ vùng thao tác để hiển thị trọn "Xem chi tiết" + chevron.
            // Khi hẹp, co cột tệp/phát hiện trước thay vì cắt chữ action bằng dấu ba chấm.
            int[] quarantineCols = ResponsiveTableWidths(rowW - 38, 0, 266, 108, 164, 122, 148);
            int fileW = quarantineCols[0], severityW = quarantineCols[1], reasonW = quarantineCols[2], dateW = quarantineCols[3], actionW = quarantineCols[4];
            int severityX = rowX + 38 + fileW;
            int reasonX = severityX + severityW;
            int dateX = reasonX + reasonW;
            int actionX = dateX + dateW;

            Card("quarantine-table-header", rowX, head, rowW, Sketch20Style.TableHeaderHeight, ColorTranslator.FromHtml("#f5f8fd"), Color.Transparent, 11);
            Check("quarantine-all", "", rowX + 8, head + (Sketch20Style.TableHeaderHeight - 25) / 2, 25, rows > 0 && quarantineSelection.Count == rows, selected => { quarantineSelection.Clear(); if (selected) foreach (var f in Model.Quarantine) quarantineSelection.Add(f.Index); if (Actions.SelectAllQuarantine != null) Actions.SelectAllQuarantine(selected); Render(); });
            Text("TỆP TIN", rowX + 50, head, fileW - 12, Sketch20Style.TableHeaderHeight, 9.6f, ColorTranslator.FromHtml("#687793"), true);
            Text("MỨC ĐỘ", severityX, head, severityW - 10, Sketch20Style.TableHeaderHeight, 9.6f, ColorTranslator.FromHtml("#687793"), true);
            Text("PHÁT HIỆN", reasonX, head, reasonW - 12, Sketch20Style.TableHeaderHeight, 9.6f, ColorTranslator.FromHtml("#687793"), true);
            Text("NGÀY CÁCH LY", dateX, head, dateW - 8, Sketch20Style.TableHeaderHeight, 9.6f, ColorTranslator.FromHtml("#687793"), true);
            Text("THAO TÁC", actionX, head, actionW - 6, Sketch20Style.TableHeaderHeight, 9.4f, ColorTranslator.FromHtml("#687793"), true);

            int rowsY = head + Sketch20Style.TableHeaderHeight + Sketch20Style.TableOuterPaddingY;
            if (hasScroll) AddTableScroll("quarantine-scroll", layoutWidth - 19, rowsY, rowsHeight, rows, Math.Max(1, visible), quarantineOffset, value => quarantineOffset = value);
            for (int i = 0; i < visible; i++)
            {
                var f = Model.Quarantine[quarantineOffset + i]; int top = rowsY + i * (rowH + rowGap), index = f.Index;
                Card("quarantine-row-" + i, rowX, top, rowW, rowH, Color.White, ColorTranslator.FromHtml("#e2eaf5"), 12);
                Check("quarantine-pick-" + i, "", rowX + 8, top + (rowH - 25) / 2, 25, quarantineSelection.Contains(index), selected => { if (selected) quarantineSelection.Add(index); else quarantineSelection.Remove(index); if (Actions.SelectQuarantine != null) Actions.SelectQuarantine(index, selected); Render(); });
                Color fg = Kind(f.Severity) == "risk" ? Sketch20Style.Red : Kind(f.Severity) == "warn" ? Sketch20Style.Amber : Sketch20Style.Blue;
                Color soft = Kind(f.Severity) == "risk" ? Sketch20Style.RedSoft : Kind(f.Severity) == "warn" ? Sketch20Style.AmberSoft : Sketch20Style.BlueSoft;
                Card("quarantine-file-icon-" + i, rowX + 44, top + 14, 44, 44, soft, Color.Transparent, 11);
                Icon("file", rowX + 57, top + 27, 18, fg);
                TwoLineText(f.Name, Model.Sample ? f.Path : System.IO.Path.GetDirectoryName(f.Path ?? ""), rowX + 102, top + (rowH - (Sketch20Style.TitleLineHeight + Sketch20Style.TextStackGap + Sketch20Style.BodyLineHeight)) / 2, Math.Max(90, fileW - 84), 12.5f, 9.8f, Sketch20Style.Ink, Sketch20Style.Muted, true, false);
                Badge(f.Severity, severityX, top + (rowH - 24) / 2, Kind(f.Severity));
                Text(string.IsNullOrWhiteSpace(f.Reason) ? "Không rõ" : f.Reason, reasonX, top + Sketch20Style.TableOuterPaddingY, reasonW - 14, rowH - Sketch20Style.TableOuterPaddingY * 2, 10.4f, Sketch20Style.Ink, false, false);
                TwoLineText(f.Date, f.Time, dateX, top + (rowH - (Sketch20Style.TitleLineHeight + Sketch20Style.TextStackGap + Sketch20Style.BodyLineHeight)) / 2, dateW - 10, 10.4f, 9.7f, Sketch20Style.Ink, Sketch20Style.Muted, false);
                Button("quarantine-detail-" + i, "Xem chi tiết", actionX, top + (rowH - 32) / 2, Math.Max(138, actionW - 4), () => OpenDetail(index, true), "chevron-right", false, false, 32, true);
            }

            if (rows == 0)
            {
                Card("quarantine-empty", rowX, rowsY, rowW, rowsHeight, ColorTranslator.FromHtml("#fbfdff"), ColorTranslator.FromHtml("#edf2f8"), 14);
                int centerX = rowX + rowW / 2;
                int artY = rowsY + 29;
                Card("quarantine-empty-icon", centerX - 36, artY, 72, 72, ColorTranslator.FromHtml("#eaf3ff"), Color.Transparent, 36);
                Icon("shield-check", centerX - 18, artY + 18, 36, Sketch20Style.Blue);
                Canvas.Drawing.Add(g =>
                {
                    using (var b = new SolidBrush(ColorTranslator.FromHtml("#8bb7ff")))
                    {
                        g.FillEllipse(b, centerX - 82, artY + 12, 6, 6);
                        g.FillEllipse(b, centerX + 74, artY + 3, 5, 5);
                    }
                });
                string emptyText = string.IsNullOrWhiteSpace(Model.QuarantineNotice) ? "Không có tệp nào trong khu vực cách ly." : Model.QuarantineNotice;
                Text(emptyText, rowX + 40, artY + 82, rowW - 80, 34, 13.2f, ColorTranslator.FromHtml("#52617d"), true, false, true);
            }

            int footer = listY + listH + 16;
            Card("quarantine-actions", 0, footer, layoutWidth, 92, Color.White, ColorTranslator.FromHtml("#dce7f5"), 16);
            Card("quarantine-action-icon", 22, footer + 20, 52, 52, ColorTranslator.FromHtml("#edf5ff"), Color.Transparent, 15);
            FilledIcon("shield-check", 37, footer + 35, 22, Sketch20Style.Blue);
            Text("Đã chọn " + quarantineSelection.Count + " tệp", 92, footer + 17, Math.Max(180, layoutWidth - 540), 28, 12.8f, Sketch20Style.Ink, true);
            Text("Khôi phục hoặc xóa vĩnh viễn các tệp đã chọn.", 92, footer + 47, Math.Max(180, layoutWidth - 540), 24, 10.7f, Sketch20Style.Muted);

            int footerActionX = layoutWidth - 344;
            Canvas.Drawing.Add(g => { using (var p = new Pen(ColorTranslator.FromHtml("#e1e9f4"))) g.DrawLine(p, footerActionX - 24, footer + 20, footerActionX - 24, footer + 72); });
            var restore = Button("restore-selected", "Khôi phục", footerActionX, footer + 24, 150, () => Command("restore"), "undo", false, false, 44); restore.Selected = true; restore.Enabled = quarantineSelection.Count > 0;
            var delete = Button("delete-selected", "Xóa vĩnh viễn", footerActionX + 164, footer + 24, 158, () => Command("delete"), "trash", true, true, 44); delete.Enabled = quarantineSelection.Count > 0;
            ContentHeight = footer + 108;
        }
        private void ReferenceDetailTabs(int y, string[] names, string[] icons, int[] indices, int selected)
        {
            if (names.Length == 4) Card("detail-tabs", 0, y, layoutWidth, 48, Color.White, ColorTranslator.FromHtml("#dce7f5"), 14);
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
            Canvas.Nodes["tabs"] = new Rectangle(0, y, layoutWidth, 47);
        }
        private void ReferenceDetails()
        {
            var f = Model.Detail;
            // Một khung duy nhất cho mọi tab. Trước đây tab Hoạt động/Thông tin bổ sung
            // đổi sang layout khác nên người dùng có cảm giác bị chuyển sang trang lạ.
            GradientCard("file-summary", 0, 114, layoutWidth, 129, f == null ? Color.White : ColorTranslator.FromHtml("#fff0f4"), ColorTranslator.FromHtml("#fff5f7"), Sketch20Style.Border);
            Artwork("file-summary-icon", 23, 136, 77, 81, "reference-icon-file-gear");
            int rightW = 227, rightX = layoutWidth - rightW - 23;
            Text(f == null ? "Chưa chọn tệp" : f.Name, 111, 128, rightX - 131, 36, 21, Sketch20Style.Ink, true);
            Text(f == null ? "Chọn tệp từ danh sách để xem chi tiết." : f.Path, 111, 166, rightX - 132, 26, 11.5f, Sketch20Style.Muted);
            Icon("file", 111, 207, 16, Sketch20Style.Muted); Text(f == null ? "—" : f.Size, 139, 200, 96, 29, 11, Sketch20Style.Muted);
            Icon("clock", 253, 207, 16, Sketch20Style.Muted); Text(f == null ? "—" : "Phát hiện: " + f.Date + ", " + f.Time, 279, 200, Math.Max(100, rightX - 291), 29, 11, Sketch20Style.Muted);
            Badge(f == null ? "Chưa có dữ liệu" : f.Status, rightX, 138, f == null ? "info" : "risk", rightW);
            Text(f == null ? "Thông tin sẽ hiển thị khi chọn tệp." : f.Resolved ? "Tệp này đã được xử lý hoặc cách ly." : "Kiểm tra thông tin và kết quả phân tích trước khi sử dụng.", rightX, 179, rightW, 48, 11, Sketch20Style.Muted, false, true);

            int tabsY = 258;
            if (Model.Threats.Count > 1)
            {
                const int threatLabelW = 170;
                Text("Mối đe dọa đang xem", 8, 254, threatLabelW, 32, 10.6f, Sketch20Style.Muted, true);
                int selected = Math.Max(0, Model.Threats.FindIndex(t => f != null && t.Index == f.Index));
                int pickerX = 8 + threatLabelW + 10;
                int pickerW = Math.Min(245, Math.Max(135, layoutWidth - pickerX - 18));
                ThreatSelectBox("detail-threat-select", Model.Threats.Select(t => t.Name).ToArray(), selected, pickerX, 254, pickerW, value =>
                {
                    if (value >= 0 && value < Model.Threats.Count) OpenDetail(Model.Threats[value].Index, false);
                });
                tabsY = 296;
            }
            ReferenceDetailTabs(tabsY, new[] { "Thông tin chi tiết", "VirusTotal", "Hoạt động", "Thông tin bổ sung" }, new[] { "file", "sigma", "activity", "clock" }, new[] { 0, 1, 2, 4 }, DetailTab == 3 ? 2 : DetailTab);
            detailBodyY = tabsY + 64;

            int height;
            if (DetailTab == 0)
            {
                if (layoutWidth < 980) { height = 934; DetailInfoStacked(f); }
                else { height = 466; DetailInfo(f, height); }
            }
            else if (DetailTab == 1) { height = VirusTotalHeight(); DetailVirusTotal(f, height); }
            else if (DetailTab == 2 || DetailTab == 3) { height = Math.Max(364, 94 + Math.Max(1, Model.Behavior.Count) * 84); DetailBehavior(f, height); }
            else { height = 460; DetailExtra(f, height); }
            ContentHeight = detailBodyY + height + 12;
        }
    }
}
