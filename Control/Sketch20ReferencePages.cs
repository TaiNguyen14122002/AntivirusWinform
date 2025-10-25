using System;
using ScanAndRemoveVirus.Services;
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
            // Trang Bảo vệ dùng banner làm phần mở đầu, không để khoảng trống 81px
            // cho tiêu đề "Bảo vệ" đã được ẩn trong PageTools().
            const int heroY = 12;
            const int sectionGap = 18;
            const int cardGap = 14;
            // Phần đệm bên dưới đảm bảo các thẻ không chạm sát status bar.
            int viewportBottom = Math.Max(0, viewport.ClientSize.Height - Canvas.Top - 20);
            int usableHeight = Math.Max(0, viewportBottom - heroY);
            bool narrow = layoutWidth < 600;
            int columns = layoutWidth >= 760 ? 2 : 1;
            int rows = (8 + columns - 1) / columns;

            // Ở màn hình lớn, banner giãn cùng chiều cao cửa sổ; ở màn hình nhỏ
            // giữ kích thước tối thiểu để các dòng thông báo không bị che.
            int heroH = Math.Max(narrow ? 200 : 170,
                Math.Min(232, (int)(Math.Max(560, usableHeight) * 0.255f)));
            GradientCard("protection-summary", 0, heroY, layoutWidth, heroH,
                ColorTranslator.FromHtml("#eef7ff"), ColorTranslator.FromHtml("#def1ff"), Sketch20Style.Border);

            int artW = narrow ? Math.Min(96, heroH - 104) : Math.Min(178, heroH - 28);
            int artX = narrow ? 14 : 22;
            Artwork("protection-art", artX, heroY + (heroH - artW) / 2,
                artW, artW, Model.Running ? "reference-protection" : "reference-off");
            int copyX = artX + artW + (narrow ? 12 : 26);
            int copyW = Math.Max(96, layoutWidth - copyX - 24);
            string statusTitle = Model.Running ? "đang bật" : "đang tắt";
            Color activeColor = Model.Running ? Sketch20Style.Green : Sketch20Style.Amber;
            string description = Model.Running
                ? "Đang theo dõi các thư mục được cấu hình khi phần mềm đang chạy."
                : "Bật bảo vệ để theo dõi các thay đổi trong những thư mục được hỗ trợ.";
            if (narrow)
            {
                int top = heroY + 22;
                Text("Bảo vệ thời gian thực", copyX, top, copyW, 42,
                    12.2f, Sketch20Style.Ink, true, true);
                Text(statusTitle, copyX, top + 43, copyW, 25, 13f, activeColor, true);
                Text(description, copyX, top + 74, copyW, 47,
                    10f, Sketch20Style.Muted, false, true);
            }
            else
            {
                int titleY = heroY + Math.Max(26, (heroH - 154) / 2);
                int leadWidth = Sketch20Style.Measure("Bảo vệ thời gian thực", 16.5f * ReferenceTextScale, true);
                Text("Bảo vệ thời gian thực", copyX, titleY, leadWidth + 4, 34, 16.5f, Sketch20Style.Ink, true);
                Text(statusTitle, copyX + leadWidth + 14, titleY,
                    Math.Max(90, copyW - leadWidth - 14), 34, 16.5f, activeColor, true);
                Text(description, copyX, titleY + 45, copyW, 38,
                    11.4f, Sketch20Style.Muted, false, true);
            }

            int summaryY = heroY + heroH - (narrow ? 37 : 47);
            Card("protection-summary-dot", copyX, summaryY + 1, 19, 19,
                Sketch20Style.GreenSoft, Color.Transparent, 10);
            Icon("check", copyX + 5, summaryY + 6, 9, Sketch20Style.Green);
            Text(narrow ? "Giám sát và ngăn chặn 24/7"
                    : "Giám sát 24/7  •  Tự động ngăn chặn  •  Tối ưu hiệu suất",
                copyX + 27, summaryY, Math.Max(50, copyW - 27), 26,
                narrow ? 9.2f : 10.5f, Sketch20Style.Muted, false, true);

            int sectionY = heroY + heroH + sectionGap;
            int gridY = sectionY + 49;
            Card("protection-section-icon", 0, sectionY + 1, 33, 33,
                ColorTranslator.FromHtml("#edf5ff"), Color.Transparent, 9);
            Icon("shield-check", 8, sectionY + 9, 17, Sketch20Style.Blue);
            Text("Các tính năng bảo vệ", 47, sectionY,
                Math.Max(140, layoutWidth - 47), 35, 17f, Sketch20Style.Ink, true);

            string[] notes = {
                "Giám sát hệ thống và ngăn chặn mối đe dọa theo thời gian thực.",
                "Quét tệp khi được tạo hoặc sửa trong các thư mục người dùng.",
                "Tự động quét thiết bị USB khi kết nối.",
                "Quét tệp tải về có dấu nguồn Internet (MOTW) sau khi lưu.",
                "Theo dõi các hành vi ứng dụng đáng ngờ.",
                "Quét tệp mới xuất hiện trong các thư mục Startup của Windows.",
                "Tự động chuyển tệp nghi ngờ vào vùng cách ly.",
                "Đối chiếu liên kết và tệp tin bằng VirusTotal." };
            string[] tiles = { "rt", "file", "usb", "download", "behavior", "startup", "aq", "web" };
            int cardW = (layoutWidth - (columns - 1) * cardGap) / columns;
            int availableCardsH = Math.Max(0, viewportBottom - gridY);
            // Chia đúng phần chiều cao còn lại cho bốn hàng. Khi cửa sổ thấp
            // hoặc cột bị xếp dọc, dùng chiều cao tối thiểu và cuộn thay vì đè chữ.
            int minimumCardH = columns == 2 ? 112 : 119;
            int cardH = Math.Max(minimumCardH, (availableCardsH - cardGap * (rows - 1)) / rows);
            for (int i = 0; i < 8; i++)
            {
                int x = (i % columns) * (cardW + cardGap);
                int y = gridY + (i / columns) * (cardH + cardGap);
                string key = FeatureKeys[i];
                bool on = Model.Flag(key);
                bool needsKey = key == "vtq" && !Model.KeyConfigured;
                Card("feature-" + key, x, y, cardW, cardH,
                    Color.White, ColorTranslator.FromHtml("#dce7f5"), 12);

                bool spacious = cardH >= 154;
                int iconSize = spacious ? 54 : 44;
                int padX = spacious ? 20 : 16;
                int topY = y + (spacious ? 23 : 15);
                int iconY = topY + (spacious ? 2 : 0);
                int textX = x + padX + iconSize + 16;
                int titleW = Math.Max(90, cardW - (textX - x) - 102);
                Artwork("feature-icon-" + i, x + padX, iconY,
                    iconSize, iconSize, "reference-icon-feature-" + tiles[i]);
                Text(FeatureNames[i], textX, topY, titleW, 28,
                    spacious ? 12.3f : 11.1f, Sketch20Style.Ink, true);

                string guardMessage;
                Model.GuardMessages.TryGetValue(key, out guardMessage);
                bool requested;
                Model.GuardDesired.TryGetValue(key, out requested);
                bool issue = (requested && !on) || (on && !string.IsNullOrEmpty(guardMessage) &&
                    (guardMessage.StartsWith("Hoạt động một phần", StringComparison.Ordinal) ||
                     guardMessage.IndexOf("cảnh báo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     (key == "rt" && Model.Running && guardMessage.StartsWith("Giám sát bị", StringComparison.Ordinal))));
                string status = issue ? "Cần kiểm tra" : on ? "Đang bật" : needsKey ? "Chưa cấu hình" : "Đang tắt";
                Color statusColor = issue ? Sketch20Style.Amber : on ? Sketch20Style.Green : Sketch20Style.Amber;
                int statusY = topY + (spacious ? 36 : 29);
                int dotX = textX;
                Canvas.Drawing.Add(g =>
                {
                    using (var brush = new SolidBrush(statusColor))
                        g.FillEllipse(brush, dotX, statusY + 7 + bodyOffset, 7, 7);
                });
                Text(status, textX + 13, statusY,
                    Math.Max(80, cardW - (textX - x) - 18), 20,
                    10f, statusColor, true);

                // Dòng mô tả luôn đặt trên mép dưới một khoảng đồng nhất.
                // Phần tiêu đề/trạng thái có vùng riêng, không va đường ngăn.
                int lineY = y + cardH - (spacious ? 54 : 43);
                Line(x + 18, lineY, cardW - 36);
                Text(notes[i], x + 20, lineY + 9, cardW - 40,
                    spacious ? 38 : 28, spacious ? 10.5f : 9.8f,
                    Sketch20Style.Muted, false, true);

                int actionsY = topY + 1;
                if (i != 7)
                    Button("feature-settings-" + key, "", x + cardW - 83, actionsY,
                        27, () => { if (Actions.Command != null) Actions.Command("protection-config:" + key); },
                        "settings", false, false, 27);
                Switch(key, FeatureNames[i], x + cardW - 47, actionsY + 1);
            }
            ContentHeight = gridY + rows * cardH + (rows - 1) * cardGap + 4;
        }
        private void ReferenceQuarantine()
        {
            // Bố cục Cách ly mới dùng bảng cố định tiêu đề/chân trang và cuộn nội bộ.
            QuarantineRedesigned();
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
                var b = new Sketch20Button(names[i], icons[i]) { Name = "tab-" + i, Tab = true, UnderlineOnly = true, Selected = index == selected, TextPixels = 12 * ReferenceTextScale, IconPixels = 17, Bounds = new Rectangle(x, y, width, 46) };
                b.Click += delegate {
                    DetailTab = index;
                    detailPageScrollOffset = 0;
                    if (Actions.SelectDetailTab != null) Actions.SelectDetailTab(index);
                    RefreshModel(true); Render(); ResetPageScroll();
                };
                Canvas.Controls.Add(b); Canvas.Nodes[b.Name] = b.Bounds; x += width + gap;
            }
            Canvas.Nodes["tabs"] = new Rectangle(0, y, layoutWidth, 47);
        }
        private void ReferenceDetails()
        {
            var f = Model.Detail;
            // Banner luôn nằm dưới header, còn nội dung bên dưới giãn theo viewport.
            bool compact = layoutWidth < 840;
            int summaryY = 76;
            // Chừa một hàng riêng cho thao tác cách ly tệp đang xem.
            // Không đặt nút lên chữ trạng thái/đường dẫn khi cửa sổ thu nhỏ.
            int summaryH = compact ? 177 : 153;
            int rightW = compact ? 171 : 231;
            int rightX = layoutWidth - rightW - 19;
            int textX = compact ? 95 : 113;
            int textW = Math.Max(180, rightX - textX - 18);
            GradientCard("file-summary", 0, summaryY, layoutWidth, summaryH,
                f == null ? Color.White : ColorTranslator.FromHtml("#fff0f4"),
                ColorTranslator.FromHtml("#fff5f7"), Sketch20Style.Border);
            int iconSize = compact ? 62 : 80;
            Artwork("file-summary-icon", 20, summaryY + (summaryH - iconSize) / 2,
                iconSize, iconSize, "reference-icon-file-gear");
            Text(f == null ? "Chưa chọn tệp" : f.Name, textX, summaryY + 19,
                textW, 32, compact ? 18.5f : 20.5f, Sketch20Style.Ink, true);
            Text(f == null ? "Chọn tệp từ danh sách để xem chi tiết." : f.Path,
                textX, summaryY + 54, textW, compact ? 47 : 35,
                compact ? 10.5f : 11.5f, Sketch20Style.Muted, false, true);
            // Metadata nằm trên hàng riêng để tên tệp/đường dẫn không va badge.
            int metadataY = summaryY + (compact ? 118 : 103);
            Icon("file", textX, metadataY + 5, 16, Sketch20Style.Muted);
            Text(f == null ? "—" : f.Size, textX + 22, metadataY,
                compact ? 60 : 96, 28, 10.8f, Sketch20Style.Muted);
            if (compact)
            {
                Icon("clock", textX + 82, metadataY + 5, 16, Sketch20Style.Muted);
                Text(f == null ? "—" : f.Date + " " + f.Time,
                    textX + 102, metadataY, Math.Max(85, textW - 104), 28,
                    10.2f, Sketch20Style.Muted);
            }
            else
            {
                Icon("clock", textX + 140, metadataY + 5, 16, Sketch20Style.Muted);
                Text(f == null ? "—" : "Phát hiện: " + f.Date + ", " + f.Time,
                    textX + 164, metadataY, Math.Max(80, textW - 165), 29,
                    10.8f, Sketch20Style.Muted);
            }
            bool quarantined = f != null &&
                (string.Equals(f.Status, "Đang cách ly", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(f.Status, "Đã cách ly", StringComparison.OrdinalIgnoreCase));
            bool canQuarantine = f != null && !f.Resolved && !quarantined &&
                !string.IsNullOrWhiteSpace(f.Path);
            Badge(f == null ? "Chưa có dữ liệu" : f.Status,
                rightX + 6, summaryY + 16,
                f == null ? "info" : quarantined ? "safe" : f.Resolved ? "info" : "risk",
                rightW - 18);
            Text(f == null ? "Chọn tệp để xem thông tin." :
                    quarantined ? "Tệp đã nằm trong khu vực cách ly." :
                    f.Resolved ? "Tệp đã được xử lý; không thể cách ly lại." :
                    "Cách ly tệp nghi ngờ để ngăn nguy cơ gây hại.",
                rightX + 7, summaryY + 49, rightW - 21,
                compact ? 64 : 52, 10.5f, Sketch20Style.Muted, false, true);
            // Thao tác này chỉ xử lý đúng dòng đang được chọn trong trang Chi tiết.
            // Phần kiểm tra tệp thực tế và xác nhận nằm tại controller UcTongQuan.
            var isolateDetail = Button("detail-isolate-file",
                quarantined ? "Đã cách ly" : f != null && f.Resolved ? "Đã xử lý" : "Cách ly tệp",
                rightX + 6, summaryY + summaryH - 49, rightW - 18,
                () => Command("isolate-file"), quarantined ? "check" : "archive",
                true, false, 40);
            isolateDetail.SquareCorners = true;
            isolateDetail.Enabled = canQuarantine;
            isolateDetail.AccessibleName = canQuarantine ? "Cách ly tệp " + f.Name :
                quarantined ? "Tệp đã cách ly" : "Cách ly không khả dụng";

            int tabsY;
            int summaryBottom = summaryY + summaryH;
            if (Model.Threats.Count > 1)
            {
                int pickerY = summaryBottom + 11;
                const int threatLabelW = 169;
                Text("Mối đe dọa đang xem", 8, pickerY + 1, threatLabelW, 30,
                    10.5f, Sketch20Style.Muted, true);
                int selected = Math.Max(0, Model.Threats.FindIndex(t => f != null && t.Index == f.Index));
                int pickerX = 8 + threatLabelW + 10;
                int pickerW = Math.Min(286, Math.Max(135, layoutWidth - pickerX - 18));
                ThreatSelectBox("detail-threat-select", Model.Threats.Select(t => t.Name).ToArray(),
                    selected, pickerX, pickerY, pickerW, value =>
                    {
                        if (value >= 0 && value < Model.Threats.Count)
                            OpenDetail(Model.Threats[value].Index, false);
                    });
                tabsY = pickerY + 40;
            }
            else tabsY = summaryBottom + 12;

            ReferenceDetailTabs(tabsY,
                new[] { "Thông tin chi tiết", "VirusTotal", "Hoạt động", "Thông tin bổ sung" },
                new[] { "file", "sigma", "activity", "clock" },
                new[] { 0, 1, 2, 4 }, DetailTab == 3 ? 2 : DetailTab);
            detailBodyY = tabsY + 55;

            // Ưu tiên lấp đầy chiều cao thực tế, không tạo scrollbar ngoài.
            // Nếu thiết bị quá thấp, chỉ cuộn bằng con lăn mà vẫn truy cập được dữ liệu.
            int availableHeight = Math.Max(0,
                viewport.ClientSize.Height - AdvancedCanvasTop - detailBodyY - 8);
            int height;
            if (DetailTab == 0)
            {
                if (layoutWidth < 980)
                {
                    height = Math.Max(948, availableHeight);
                    DetailInfoStacked(f, height);
                }
                else
                {
                    height = Math.Max(476, availableHeight);
                    DetailInfo(f, height);
                }
            }
            else if (DetailTab == 1)
            {
                height = Math.Max(layoutWidth < 900 ? 508 : 462, availableHeight);
                DetailVirusTotal(f, height);
            }
            else if (DetailTab == 2 || DetailTab == 3)
            {
                height = Math.Max(Math.Max(380, 112 + Math.Max(1, Model.Behavior.Count) * (layoutWidth < 900 ? 124 : 88)), availableHeight);
                DetailBehavior(f, height);
            }
            else
            {
                height = Math.Max(468, availableHeight);
                DetailExtra(f, height);
            }
            ContentHeight = detailBodyY + height + 6;
        }

    }
}
