using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    // Một renderer dùng cho cả ứng dụng và chế độ xem 20 phác thảo.
    // Chế độ xem cung cấp DTO riêng; ứng dụng đọc DTO từ controller/service hiện có.
    internal sealed class Sketch20View : UserControl
    {
        internal static readonly string[] ScreenNames = { "Tổng quan", "Bảo vệ", "Lịch sử", "Cách ly", "Cài đặt · Chung", "Quét toàn bộ hệ thống", "Chi tiết · Thông tin", "Chi tiết · VirusTotal", "Chi tiết · Hành vi", "Chi tiết · Chuỗi ký tự", "Chi tiết · Thông tin bổ sung", "Cài đặt · Bảo vệ", "Cài đặt · VirusTotal", "Cài đặt · Dữ liệu", "Quét thư mục", "Quét tệp", "Quét tùy chỉnh", "Đang quét", "Phát hiện mối đe dọa", "Bảo vệ tắt" };
        private readonly Panel sidebar, viewport;
        internal readonly Sketch20Surface Canvas;
        internal readonly Sketch20Actions Actions;
        private readonly Func<Sketch20Model> readModel;
        private readonly Timer update;
        internal Sketch20Model Model;
        internal string Page = "overview";
        internal string DetailReturnPage = "quarantine";
        internal int SettingsTab, DetailTab, AdvancedMode;
        internal int ContentHeight;
        private string previousStamp;
        private int layoutWidth;
        private int bodyOffset;
        private readonly ToolTip toolTips = new ToolTip();
        private bool rendering;
        private bool needsScrollbar;
        private readonly HashSet<int> quarantineSelection = new HashSet<int>();
        private List<string> folders = new List<string>(), files = new List<string>(), customFolders = new List<string>();
        private bool[] customLocations = { true, true, true, false };
        private bool includeSubfolders = true;
        private DateTime from = new DateTime(2026, 10, 1), to = new DateTime(2026, 10, 4);
        private string scanFilter = "Tất cả", typedKey = "";

        internal Sketch20View(Sketch20Model initial, Sketch20Actions actions, Func<Sketch20Model> source = null)
        {
            Model = initial; Actions = actions ?? new Sketch20Actions(); readModel = source;
            if (!initial.Sample) { from = DateTime.Today.AddDays(-30); to = DateTime.Today; }
            AutoScaleMode = AutoScaleMode.None; Dock = DockStyle.Fill; BackColor = Sketch20Style.Background;
            sidebar = new Panel { Dock = DockStyle.Left, Width = 208, BackColor = Sketch20Style.Sidebar };
            Theme.BatDoubleBuffer(sidebar); sidebar.Paint += DrawSidebar;
            viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Sketch20Style.Background };
            Canvas = new Sketch20Surface { Name = "Sketch20Canvas", Location = new Point(26, 29) };
            viewport.Controls.Add(Canvas); Controls.Add(viewport); Controls.Add(sidebar);
            Resize += delegate { Render(); }; viewport.Resize += delegate { if (!rendering) Render(); };
            update = new Timer { Interval = 250 };
            update.Tick += delegate { RefreshModel(); };
            if (source != null) update.Start();
            BuildSidebarButtons(); Render();
            Disposed += delegate { update.Stop(); update.Dispose(); toolTips.Dispose(); };
        }

        internal int ScreenNumber
        {
            get
            {
                if (Page == "protection") return 2;
                if (Page == "history") return 3;
                if (Page == "quarantine") return 4;
                if (Page == "settings") return SettingsTab == 0 ? 5 : 11 + SettingsTab;
                if (Page == "advanced") return AdvancedMode == 0 ? 6 : 14 + AdvancedMode;
                if (Page == "details") return 7 + DetailTab;
                return Model.Scanning ? 18 : Model.ThreatCount > 0 ? 19 : !Model.Running ? 20 : 1;
            }
        }
        internal void SelectScreen(int number)
        {
            if (number < 1 || number > 20) throw new ArgumentOutOfRangeException("number");
            if (Model.Sample) Model = Sketch20Samples.Create(number);
            SettingsTab = number == 12 ? 1 : number == 13 ? 2 : number == 14 ? 3 : 0;
            DetailTab = number >= 7 && number <= 11 ? number - 7 : 0;
            AdvancedMode = number >= 15 && number <= 17 ? number - 14 : 0;
            Page = number == 2 ? "protection" : number == 3 ? "history" : number == 4 ? "quarantine" : number == 5 || number >= 12 && number <= 14 ? "settings" : number == 6 || number >= 15 && number <= 17 ? "advanced" : number >= 7 && number <= 11 ? "details" : "overview";
            viewport.AutoScrollPosition = Point.Empty; NotifyPage(); Render();
        }
        internal void Go(string page)
        {
            Page = page; viewport.AutoScrollPosition = Point.Empty; NotifyPage(); RefreshModel(true); Render();
        }
        private void NotifyPage()
        { if (Actions.Navigate != null) Actions.Navigate(Page); }
        private void Command(string command)
        {
            if (Actions.Command != null) Actions.Command(command);
            if (Model.Sample)
            {
                if (command == "scan") { Model.Scanning = true; Model.ScannedFiles = 1310; Model.Progress = 64; Page = "overview"; }
                if (command == "cancel") { Model.Scanning = false; Model.Status = "Đã hủy phiên quét"; }
                if (command == "enable") { Model.Running = true; Model.Flags["rt"] = true; }
                if (command == "finish") { Model.Scanning = false; Model.ThreatCount = 3; Model.Threats = Model.Quarantine.Take(3).ToList(); }
                if (command == "isolate") { Model.ThreatCount = 0; foreach (var f in Model.Threats) f.Resolved = true; Model.Threats.Clear(); }
                if (command == "clear-key") Model.KeyConfigured = false;
                if (command == "restore" || command == "delete") { Model.Quarantine.RemoveAll(f => quarantineSelection.Contains(f.Index)); quarantineSelection.Clear(); }
                if (command == "defaults") foreach (string key in Model.Flags.Keys.ToList()) Model.Flags[key] = key != "vtq" && key != "samples";
            }
            RefreshModel(true); Render();
        }
        internal void RefreshModel(bool force = false)
        {
            if (readModel != null)
            {
                var next = readModel();
                if (next == null) return;
                string stamp = next.Stamp();
                if (!force && stamp == previousStamp) return;
                if (string.Join("|", Model.Quarantine.Select(f => f.Id)) != string.Join("|", next.Quarantine.Select(f => f.Id)))
                {
                    quarantineSelection.Clear();
                    if (Actions.SelectAllQuarantine != null) Actions.SelectAllQuarantine(false);
                }
                Model = next; previousStamp = stamp;
            }
            if (!Canvas.Controls.OfType<TextBox>().Any(c => c.Focused) && !ContainsFocusedTextBox(Canvas)) Render();
        }
        private static bool ContainsFocusedTextBox(System.Windows.Forms.Control parent)
        { return parent.Controls.Cast<System.Windows.Forms.Control>().Any(c => c is TextBox && c.Focused || ContainsFocusedTextBox(c)); }

        private void BuildSidebarButtons()
        {
            string[] names = { "Tổng quan", "Bảo vệ", "Cách ly", "Lịch sử" };
            string[] pages = { "overview", "protection", "quarantine", "history" };
            for (int i = 0; i < names.Length; i++)
            {
                string page = pages[i];
                var nav = new Button { Name = "nav-" + page, Text = names[i], AccessibleName = names[i], FlatStyle = FlatStyle.Flat,
                    BackColor = Sketch20Style.Sidebar, ForeColor = Color.Transparent, Cursor = Cursors.Hand, TabStop = true,
                    Bounds = new Rectangle(15, 131 + i * 53, 178, 46) };
                nav.FlatAppearance.BorderSize = 0;
                nav.Paint += delegate(object sender, PaintEventArgs e) { DrawNav(e.Graphics, (Button)sender, page); };
                nav.Click += delegate { Go(page); };
                sidebar.Controls.Add(nav);
            }
        }
        private void DrawNav(Graphics g, Button button, string page)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bool active = page == Page || page == "overview" && Page == "advanced" || page == "quarantine" && Page == "details";
            Sketch20Style.Round(g, button.ClientRectangle, 9, active ? Sketch20Style.BlueSoft : Sketch20Style.Sidebar, Color.Transparent);
            Color ink = active ? Sketch20Style.Blue : Sketch20Style.Ink;
            string icon = page == "overview" ? "house" : page == "protection" ? "shield" : page == "quarantine" ? "archive" : page == "history" ? "history" : "settings";
            Sketch20Style.Icon(g, icon, new Rectangle(14, 13, 19, 19), active ? Sketch20Style.Blue : Sketch20Style.Muted);
            Sketch20Style.Text(g, button.Text, new Rectangle(45, 0, 116, 46), 14, ink, active);
            if (page == "quarantine")
            {
                Sketch20Style.Round(g, new Rectangle(143, 15, 20, 19), 5, Sketch20Style.Track, Color.Transparent);
                Sketch20Style.Text(g, Model.Quarantine.Count.ToString(), new Rectangle(148, 15, 13, 19), 11, Sketch20Style.Muted);
            }
            if (button.Focused) ControlPaint.DrawFocusRectangle(g, new Rectangle(4, 4, 169, 37));
        }
        private void DrawSidebar(object sender, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var pen = new Pen(Sketch20Style.Border)) g.DrawLine(pen, 207, 0, 207, sidebar.Height);
            Sketch20Style.Icon(g, "shield-check", new Rectangle(24, 42, 35, 35), Sketch20Style.Blue);
            Sketch20Style.Text(g, "Antivirus", new Rectangle(68, 28, 135, 35), 23, Sketch20Style.Ink, true);
            Sketch20Style.Text(g, "Bảo vệ máy tính của\nbạn", new Rectangle(68, 60, 130, 35), 11, Sketch20Style.Muted, false, true);
            int bottom = sidebar.Height - 64;
            Color state = Model.Running ? Sketch20Style.Green : Sketch20Style.Amber;
            using (var brush = new SolidBrush(state)) g.FillEllipse(brush, 23, bottom + 5, 6, 6);
            Sketch20Style.Text(g, Model.Running ? "Bảo vệ đang hoạt động" : "Bảo vệ thời gian thực tắt", new Rectangle(36, bottom, 167, 18), 11, state);
            Sketch20Style.Text(g, "Phiên bản " + Model.Version, new Rectangle(23, bottom + 22, 178, 18), 11, Sketch20Style.Muted);
        }

        internal void Render()
        {
            if (rendering || IsDisposed || viewport.ClientSize.Width < 90) return;
            rendering = true;
            var scroll = viewport.AutoScrollPosition;
            try
            {
                // Xóa DisplayRectangle cũ trước khi đổi kích thước hoặc trang; Windows
                // có thể giữ cặp scrollbar do chiều rộng của trang trước.
                viewport.AutoScroll = false;
                Canvas.Location = new Point(26, 29);
                Canvas.SuspendLayout();
                toolTips.RemoveAll();
                if (Canvas.ContextMenuStrip != null) { Canvas.ContextMenuStrip.Dispose(); Canvas.ContextMenuStrip = null; }
                foreach (System.Windows.Forms.Control c in Canvas.Controls.Cast<System.Windows.Forms.Control>().ToArray()) c.Dispose();
                Canvas.Drawing.Clear(); Canvas.BackgroundDrawing.Clear(); Canvas.Nodes.Clear(); Canvas.TextRuns.Clear();
                layoutWidth = Math.Max(500, viewport.Width - 52 - (needsScrollbar ? SystemInformation.VerticalScrollBarWidth : 0));
                Canvas.Width = layoutWidth;
                bodyOffset = 0;
                PageTools();
                // Nội dung bắt đầu ngay dưới hàng công cụ, không giữ khoảng trống
                // của tiêu đề/mô tả cũ (81px, hoặc 114px ở màn có nút quay lại).
                bodyOffset = 56 - (Page == "advanced" || Page == "details" ? 114 : 81);
                if (Page == "protection") Protection();
                else if (Page == "history") History();
                else if (Page == "quarantine") Quarantine();
                else if (Page == "settings") Settings();
                else if (Page == "advanced") Advanced();
                else if (Page == "details") Details();
                else Overview();
                Canvas.Height = ContentHeight;
                // Chiều rộng tự theo viewport. Ép MinSize.Width bằng Width có thể giữ
                // cả hai scrollbar khi đổi từ màn dài sang màn ngắn trên Windows.
                viewport.AutoScrollMinSize = new Size(0, ContentHeight + 45);
                viewport.AutoScroll = true;
                viewport.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
                Canvas.ResumeLayout(); Canvas.Invalidate(); sidebar.Invalidate();
                foreach (System.Windows.Forms.Control c in sidebar.Controls) c.Invalidate();
            }
            finally { rendering = false; }
            bool needs = ContentHeight + 45 > viewport.Height;
            if (needsScrollbar != needs) { needsScrollbar = needs; Render(); }
        }
        private new void Text(string value, int x, int y, int width, int height = 20, float pixels = 13, Color? color = null, bool bold = false, bool wrap = false, bool right = false, bool mono = false)
        {
            var bounds = new Rectangle(x, y + bodyOffset, Math.Max(1, width), height);
            Canvas.TextRuns.Add(new Sketch20TextRun { Value = value ?? "—", Bounds = bounds, Pixels = pixels, Bold = bold, Wrap = wrap, Right = right, Mono = mono });
            Canvas.Drawing.Add(g => Sketch20Style.Text(g, value, bounds, pixels, color, bold, wrap, right, mono));
        }
        private void Line(int x, int y, int width)
        { y += bodyOffset; Canvas.Drawing.Add(g => { using (var pen = new Pen(Sketch20Style.Border)) g.DrawLine(pen, x, y, x + width, y); }); }
        private void Icon(string name, int x, int y, int size = 16, Color? color = null)
        { y += bodyOffset; Canvas.Drawing.Add(g => Sketch20Style.Icon(g, name, new Rectangle(x, y, size, size), color ?? Sketch20Style.Blue)); }
        private void Card(string name, int x, int y, int width, int height, Color? fill = null, Color? edge = null, int radius = 12)
        {
            var bounds = new Rectangle(x, y + bodyOffset, width, height); Canvas.Nodes[name] = bounds;
            Action<Graphics> paint = g => Sketch20Style.Round(g, bounds, radius, fill ?? Color.White, edge ?? Sketch20Style.Border);
            Canvas.Drawing.Add(paint); Canvas.BackgroundDrawing.Add(paint);
        }
        private void Badge(string caption, int x, int y, string kind = "safe", int width = 0)
        {
            Color ink = kind == "risk" ? Sketch20Style.Red : kind == "warn" ? Sketch20Style.Amber : kind == "info" ? Sketch20Style.Blue : Sketch20Style.Green;
            Color bg = kind == "risk" ? Sketch20Style.RedSoft : kind == "warn" ? Sketch20Style.AmberSoft : kind == "info" ? Sketch20Style.BlueSoft : Sketch20Style.GreenSoft;
            int w = width > 0 ? width : Sketch20Style.Measure(caption, 11, true) + 16;
            Card("badge-" + x + "-" + y, x, y, w, 24, bg, Color.Transparent, 5);
            Text(caption, x + 8, y, w - 12, 24, 11, ink, true);
        }
        private Sketch20Button Button(string name, string caption, int x, int y, int width, Action click, string icon = "", bool primary = false, bool danger = false, int height = 40, bool link = false)
        {
            var b = new Sketch20Button(caption, icon) { Name = name, Primary = primary, Danger = danger, Link = link,
                Bounds = new Rectangle(x, y + bodyOffset, width, height), TextPixels = height <= 33 ? 12 : 13 };
            b.Click += delegate { click(); }; Canvas.Controls.Add(b); Canvas.Nodes[name] = b.Bounds; return b;
        }
        private void Switch(string key, string caption, int x, int y)
        {
            var c = new Sketch20Switch { Name = "switch-" + key, AccessibleName = caption, Bounds = new Rectangle(x, y + bodyOffset, 34, 25), Checked = Model.Flag(key) };
            c.CheckedChanged += delegate
            {
                if (key == "vtq" && c.Checked && !Model.KeyConfigured) { SettingsTab = 2; Go("settings"); return; }
                if (Actions.SetFlag != null) Actions.SetFlag(key, c.Checked);
                if (Model.Sample) { Model.Flags[key] = c.Checked; if (key == "rt") Model.Running = c.Checked; }
                RefreshModel(true);
            };
            Canvas.Controls.Add(c); Canvas.Nodes[c.Name] = c.Bounds;
        }
        private Sketch20Check Check(string name, string caption, int x, int y, int width, bool value, Action<bool> change)
        {
            var c = new Sketch20Check(caption) { Name = name, Bounds = new Rectangle(x, y + bodyOffset, width, 25), Checked = value };
            c.CheckedChanged += delegate { change(c.Checked); }; Canvas.Controls.Add(c); Canvas.Nodes[name] = c.Bounds; return c;
        }
        private void PageTools()
        {
            var settings = Button("nav-settings", "", layoutWidth - 40, 0, 40, () => Go("settings"), "settings");
            settings.IconPixels = 20; settings.Selected = Page == "settings";
            settings.AccessibleName = "Cài đặt"; settings.AccessibleDescription = "Mở cài đặt ứng dụng";
            toolTips.SetToolTip(settings, "Cài đặt");
            int right = layoutWidth - 52;
            if (Page == "protection") Button("update-signatures", "Cập nhật chữ ký", right - 159, 0, 159, () => Command("update"), "refresh");
            else if (Page == "history") Button("export-csv", "Xuất CSV", right - 129, 0, 129, () => Command("export"), "download");
            else if (Page == "quarantine") Button("refresh-quarantine", "Làm mới", right - 110, 0, 110, () => Command("refresh"), "refresh");
            else if (Page == "advanced") Button("advanced-back", "Về Tổng quan", 0, 7, 104, () => Go("overview"), "arrow-left", false, false, 25, true);
            else if (Page == "details") Button("details-back", "Về danh sách tệp", 0, 7, 132, () => Go(DetailReturnPage), "arrow-left", false, false, 25, true);
            else if (Page == "overview" && Model.Sample)
            {
                int state = Model.Scanning ? 2 : Model.ThreatCount > 0 ? 3 : !Model.Running ? 1 : 0;
                int labelWidth = Sketch20Style.Measure("Trạng thái mẫu", 12);
                Text("Trạng thái mẫu", right - 169 - labelWidth, 0, labelWidth, 40, 12, Sketch20Style.Muted);
                SelectBox("sample-state", new[] { "An toàn", "Bảo vệ tắt", "Đang quét", "Có mối đe dọa" }, state, right - 161, 0, 161, index => SelectScreen(new[] { 1, 20, 18, 19 }[index]), 40);
            }
        }
        private void Footer(int y, string left, string right)
        {
            Text(left, 0, y, layoutWidth / 2, 18, 11, Sketch20Style.Muted);
            Text(right, layoutWidth / 2, y, layoutWidth / 2, 18, 11, Sketch20Style.Muted, false, false, true);
            ContentHeight = y + bodyOffset + 18;
        }
        private void Tabs(int y, string[] titles, int selected, Action<int> choose)
        {
            int x = 0;
            for (int i = 0; i < titles.Length; i++)
            {
                int index = i, width = Sketch20Style.Measure(titles[i], 13, i == selected) + 26;
                var b = new Sketch20Button(titles[i]) { Name = "tab-" + i, Tab = true, Selected = i == selected, Bounds = new Rectangle(x, y + bodyOffset, width, 43) };
                b.Click += delegate { choose(index); }; Canvas.Controls.Add(b); Canvas.Nodes[b.Name] = b.Bounds;
                x += width + 5;
            }
            Line(0, y + 43, layoutWidth); Canvas.Nodes["tabs"] = new Rectangle(0, y + bodyOffset, layoutWidth, 44);
        }
        private void InfoRow(string name, string value, int x, int y, int width, int labelWidth = 145, int height = 45, bool last = false, bool mono = false, string kind = null, string note = null)
        {
            Text(name, x, y + 7, labelWidth, 30, 12, Sketch20Style.Muted);
            if (kind == null) Text(value, x + labelWidth + 14, y + 7, width - labelWidth - 14, height - 14, 12, mono ? Sketch20Style.Muted : Sketch20Style.Ink, false, false, false, mono);
            else
            {
                Badge(value, x + labelWidth + 14, y + 10, kind);
                if (note != null) Text(note, x + labelWidth + 14, y + 38, width - labelWidth - 14, 20, 12, Sketch20Style.Muted);
            }
            if (!last) Line(x, y + height, width);
        }
        private void Callout(int x, int y, int width, string value, bool warn = false, int height = 44)
        {
            Card("callout-" + y, x, y, width, height, warn ? Sketch20Style.AmberSoft : Sketch20Style.BlueSoft, Color.Transparent, 9);
            Icon(warn ? "alert" : "info", x + 16, y + (height - 16) / 2);
            Text(value, x + 41, y + 7, width - 57, height - 14, 12, warn ? Sketch20Style.Amber : Sketch20Style.Muted, false, true);
        }

        private void Overview()
        {
            int state = Model.Scanning ? 2 : Model.ThreatCount > 0 ? 3 : !Model.Running ? 1 : 0;
            string[] titles = { "Máy tính của bạn được bảo vệ", "Bảo vệ thời gian thực đang tắt", "Đang kiểm tra máy tính", Model.ThreatCount + " tệp cần được xử lý" };
            string[] notes = { "Không phát hiện mối đe dọa trong lần quét gần nhất.", "Bật lại để tự động giám sát các tệp mới và thay đổi.", "Quét nhanh các vị trí thường xuất hiện mối đe dọa.", "Cách ly các tệp được phát hiện để bảo vệ máy tính." };
            string[] eyebrows = { "BẢO VỆ ĐANG HOẠT ĐỘNG", "CẦN CHÚ Ý", "ĐANG QUÉT", "PHÁT HIỆN MỐI ĐE DỌA" };
            Color[] colors = { Sketch20Style.Green, Sketch20Style.Amber, Sketch20Style.Blue, Sketch20Style.Red };
            Color[] fills = { Sketch20Style.GreenSoft, Sketch20Style.AmberSoft, Sketch20Style.BlueSoft, Sketch20Style.RedSoft };
            Color[] edges = { Sketch20Style.GreenBorder, Sketch20Style.AmberBorder, Sketch20Style.Border, Sketch20Style.RedBorder };
            int heroHeight = state == 2 ? 256 : 208;
            Card("hero", 0, 81, layoutWidth, heroHeight, fills[state], edges[state], 13);
            Icon("circle-check", 27, 109, 14, colors[state]); Text(eyebrows[state], 49, 104, layoutWidth - 170, 25, 11, colors[state], true);
            Text(titles[state], 27, 130, layoutWidth - 175, 40, 27, Sketch20Style.Ink, true);
            Text(notes[state], 27, 174, layoutWidth - 175, 22, 13, Sketch20Style.Muted);
            int emblemY = 81 + (heroHeight - 94) / 2;
            int emblemPaintY = emblemY + bodyOffset;
            Canvas.Drawing.Add(g => { using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, layoutWidth - 121, emblemPaintY, 94, 94); });
            Icon(state == 2 ? "scan" : state == 0 ? "shield-check" : "shield-alert", layoutWidth - 100, emblemY + 21, 51, colors[state]);
            int buttonsY = 221;
            if (state == 2)
            {
                int progressWidth = layoutWidth - 167;
                Card("progress-track", 27, 216, progressWidth, 7, Sketch20Style.Track, Color.Transparent, 4);
                int progress = Model.Progress < 0 ? 35 : Math.Min(100, Model.Progress);
                Card("progress-fill", 27, 216, Math.Max(4, progressWidth * progress / 100), 7, Sketch20Style.Blue, Color.Transparent, 4);
                Text(Model.Sample ? "1.310 / 2.048 tệp" : Model.ScannedFiles.ToString("N0") + " tệp đã kiểm tra", 27, 226, progressWidth - 70, 18, 11, Sketch20Style.Muted);
                Text(Model.Progress < 0 ? "Đang quét…" : Model.Progress + "%", 27, 226, progressWidth, 18, 11, Sketch20Style.Muted, false, false, true);
                buttonsY += 48;
                Button("scan-results", Model.Sample ? "Xem kết quả mẫu" : "Xem kết quả", 27, buttonsY, 168, () => { if (Model.Sample) Command("finish"); else Go("details"); }, "list-checks", true);
                Button("cancel-scan", "Hủy quét", 204, buttonsY, 108, () => Command("cancel"), "x");
            }
            else if (state == 1)
            {
                Button("enable-protection", "Bật bảo vệ", 27, buttonsY, 125, () => Command("enable"), "shield-check", true);
                Button("quick-scan", "Quét ngay", 161, buttonsY, 117, () => Command("scan"), "scan");
            }
            else if (state == 3)
            {
                Button("show-details", "Xem chi tiết", 27, buttonsY, 130, () => OpenDetail(0, false), "file-search", true);
                Button("quick-scan", "Quét lại", 166, buttonsY, 104, () => Command("scan"), "scan");
            }
            else
            {
                Button("quick-scan", "Quét ngay", 27, buttonsY, 117, () => Command("scan"), "scan", true);
                Button("advanced-scan", "Quét nâng cao", 153, buttonsY, 144, () => Go("advanced"), "chevron-down");
            }
            int statsY = 81 + heroHeight + 18;
            int statWidth = (layoutWidth - 26) / 3;
            string[] labels = { "Mối đe dọa", "Tệp đã quét", "Lần quét gần nhất" };
            string[] values = { Model.ThreatCount.ToString(), Model.ScannedFiles.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Model.LastDate };
            string[] hints = { state == 3 ? "Cần xử lý" : "Trong lần quét gần nhất", state == 2 ? "Trong phiên hiện tại" : "Tổng số tệp đã kiểm tra", Model.LastTime + " · " + Model.LastType };
            for (int i = 0; i < 3; i++)
            {
                int x = i * (statWidth + 13), width = i == 2 ? layoutWidth - x : statWidth;
                Card("stat-" + i, x, statsY, width, 118);
                Icon(i == 0 ? "shield-alert" : i == 1 ? "files" : "clock", x + 18, statsY + 20, 15, Sketch20Style.Muted);
                Text(labels[i], x + 40, statsY + 17, width - 54, 20, 12, Sketch20Style.Muted);
                Text(values[i], x + 18, statsY + 41, width - 36, 38, i == 2 ? 20 : 28, i == 0 ? state == 3 ? Sketch20Style.Red : Sketch20Style.Green : i == 1 ? Sketch20Style.Blue : Sketch20Style.Ink, true);
                Text(hints[i], x + 18, statsY + 80, width - 36, 20, 11, Sketch20Style.Muted);
            }
            int listY = statsY + 137;
            if (state == 3) ThreatTable(listY);
            else Activity(listY);
        }
        private void Activity(int y)
        {
            int rows = Model.Activities.Count, height = 66 + (rows == 0 ? 125 : Math.Min(rows, 5) * 67);
            Card("activity", 0, y, layoutWidth, height);
            Icon("history", 20, y + 23, 18); Text("Hoạt động gần đây", 48, y + 17, layoutWidth - 200, 31, 17, Sketch20Style.Ink, true);
            Button("open-history", "Xem lịch sử  ›", layoutWidth - 118, y + 17, 98, () => Go("history"), "", false, false, 31, true);
            Line(0, y + 66, layoutWidth);
            if (rows == 0) Text("Chưa có hoạt động quét. Nhấn Quét ngay để bắt đầu.", 20, y + 90, layoutWidth - 40, 30, 13, Sketch20Style.Muted);
            for (int i = 0; i < Math.Min(rows, 5); i++)
            {
                var row = Model.Activities[i]; int top = y + 66 + i * 67;
                Color bg = row[0] == "check" ? Sketch20Style.GreenSoft : row[0] == "archive" ? Sketch20Style.AmberSoft : Sketch20Style.BlueSoft;
                Color fg = row[0] == "check" ? Sketch20Style.Green : row[0] == "archive" ? Sketch20Style.Amber : Sketch20Style.Blue;
                Card("activity-icon-" + i, 20, top + 16, 34, 34, bg, Color.Transparent, 8); Icon(row[0], 29, top + 25, 16, fg);
                Text(row[1], 66, top + 9, layoutWidth - 155, 27); Text(row[2], 66, top + 33, layoutWidth - 155, 20, 11, Sketch20Style.Muted);
                Text(row[3], layoutWidth - 95, top + 22, 75, 22, 11, Sketch20Style.Muted, false, false, true);
                if (i < rows - 1) Line(0, top + 67, layoutWidth);
            }
            Footer(y + height + 18, Model.Scanning ? "Đang quét: " + Model.ScanPath : Model.Status, "CSDL chữ ký: " + Model.Signature);
        }
        private void ThreatTable(int y)
        {
            int count = Model.Threats.Count, rows = Math.Min(count, 8), height = 64 + 39 + rows * 66 + 62;
            Card("threat-table", 0, y, layoutWidth, height); Icon("shield-alert", 20, y + 22, 18);
            Text("Mối đe dọa được phát hiện", 48, y + 16, layoutWidth - 150, 31, 17, Sketch20Style.Ink, true);
            Badge(count + " tệp", layoutWidth - 63, y + 20, "risk"); TableHeader(y + 64, new[] { "Tệp", "Mức độ", "Chi tiết" }, new[] { 0f, .47f, .77f });
            for (int i = 0; i < rows; i++)
            {
                var f = Model.Threats[i]; int top = y + 103 + i * 66, index = f.Index;
                Text(f.Name, 14, top + 10, (int)(layoutWidth * .45), 22, 12, Sketch20Style.Ink, true); Text(f.Reason, 14, top + 33, (int)(layoutWidth * .45), 20, 11, Sketch20Style.Muted);
                Badge(f.Severity, (int)(layoutWidth * .47) + 14, top + 21, Kind(f.Severity));
                Button("threat-detail-" + i, "Xem  ›", (int)(layoutWidth * .77) + 10, top + 17, 65, () => OpenDetail(index, false), "", false, false, 33, true);
                Line(0, top + 66, layoutWidth);
            }
            int foot = y + 103 + rows * 66;
            Text("Chưa xử lý", 18, foot + 19, layoutWidth - 200, 22, 12, Sketch20Style.Muted);
            Button("isolate-all", "Cách ly tất cả", layoutWidth - 144, foot + 14, 126, () => Command("isolate"), "archive", true, false, 33);
            Footer(y + height + 18, Model.Status, "CSDL chữ ký: " + Model.Signature);
        }

        internal static readonly string[] FeatureKeys = { "rt", "restore", "usb", "download", "behavior", "startup", "aq", "vtq" };
        internal static readonly string[] FeatureNames = { "Bảo vệ thời gian thực", "Bảo vệ tệp", "Bảo vệ USB", "Bảo vệ tải xuống", "Giám sát hành vi", "Bảo vệ thư mục khởi động", "Tự động cách ly", "Bảo vệ web · VirusTotal" };
        internal static readonly string[] FeatureNotes = { "Giám sát tệp mới hoặc thay đổi trong máy.", "Kiểm tra lại tệp trước khi khôi phục từ cách ly.", "Quét ổ USB ngay khi được kết nối.", "Kiểm tra tệp tải về từ Internet.", "Theo dõi tiến trình có hành vi đáng ngờ.", "Kiểm tra tệp mới trong thư mục Startup.", "Chuyển tệp nguy hiểm vào khu vực cách ly.", "Tra cứu tệp nghi ngờ bằng mã băm." };
        private void Protection()
        {
            Card("protection-summary", 0, 81, layoutWidth, 95);
            Card("protection-mark", 21, 102, 51, 51, Sketch20Style.GreenSoft, Color.Transparent); Icon("shield-check", 33, 114, 27, Sketch20Style.Green);
            Text(Model.Running ? "Bảo vệ thời gian thực đang bật" : "Bảo vệ thời gian thực đang tắt", 90, 99, layoutWidth - 220, 29, 17, Sketch20Style.Ink, true);
            Text(FeatureKeys.Count(Model.Flag) + "/8 tính năng đang hoạt động · CSDL cập nhật lúc " + (Model.Sample ? "09:10" : Model.Signature), 90, 128, layoutWidth - 220, 21, 12, Sketch20Style.Muted);
            Badge(Model.Running ? "Đang bảo vệ" : "Cần chú ý", layoutWidth - 106, 117, Model.Running ? "safe" : "warn");
            string[] icons = { "shield-check", "file-check", "usb", "download", "activity", "power", "archive", "globe" };
            int cw = (layoutWidth - 12) / 2;
            for (int i = 0; i < 8; i++)
            {
                int x = i % 2 * (cw + 12), y = 196 + i / 2 * 116;
                int h = i >= 6 && !Model.KeyConfigured ? 115 : 104;
                Card("feature-" + FeatureKeys[i], x, y, cw, h);
                Card("feature-icon-" + i, x + 17, y + 17, 30, 30, Sketch20Style.BlueSoft, Color.Transparent, 7); Icon(icons[i], x + 24, y + 24, 16);
                Text(FeatureNames[i], x + 59, y + 13, cw - 117, 25, 14, Sketch20Style.Ink, true);
                Text(FeatureNotes[i], x + 59, y + 40, cw - 90, 20, 12, Sketch20Style.Muted);
                string key = FeatureKeys[i];
                int statusWidth = key == "vtq" && !Model.KeyConfigured ? 140 : cw - 86;
                Text(Model.Flag(key) ? "Đang bật" : key == "vtq" && !Model.KeyConfigured ? "Chưa cấu hình API key ·" : "Đang tắt", x + 59, y + 67, statusWidth, 19, 11, Model.Flag(key) ? Sketch20Style.Green : Sketch20Style.Muted);
                if (key == "vtq" && !Model.KeyConfigured) Button("configure-vt", "Cấu hình  ›", x + 207, y + 65, 86, () => { SettingsTab = 2; Go("settings"); }, "", false, false, 23, true);
                Switch(key, FeatureNames[i], x + cw - 51, y + 17);
            }
            Footer(677, "Giám sát gần nhất: " + Model.LastDate + " · " + (Model.Sample ? "09:25" : Model.LastTime), Model.ScannedFiles.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " tệp đã quét · " + Model.Quarantine.Count + " tệp đã cách ly");
        }
        private static string Kind(string severity)
        { return severity == "Cao" ? "risk" : severity == "Trung bình" ? "warn" : "info"; }

        private void TableHeader(int y, string[] names, float[] starts, int height = 39)
        {
            int paintY = y + bodyOffset;
            Canvas.Drawing.Add(g => { using (var b = new SolidBrush(Sketch20Style.Sidebar)) g.FillRectangle(b, 1, paintY, layoutWidth - 2, height); });
            Line(0, y, layoutWidth); Line(0, y + height, layoutWidth);
            for (int i = 0; i < names.Length; i++)
            {
                int x = (int)(layoutWidth * starts[i]), end = i == names.Length - 1 ? layoutWidth : (int)(layoutWidth * starts[i + 1]);
                Text(names[i], x + 14, y, end - x - 28, height, 11, Sketch20Style.Muted);
            }
        }
        private void History()
        {
            int rows = Model.History.Count, height = 109 + 39 + (rows == 0 ? 132 : rows * 66) + 62;
            Card("history-table", 0, 81, layoutWidth, height);
            int filterW = layoutWidth - 39 - 78 - 33;
            int dateW = (int)(filterW / 3.15), typeW = filterW - 2 * dateW;
            Text("Từ ngày", 20, 100, dateW, 23, 12, Sketch20Style.Muted); Text("Đến ngày", 31 + dateW, 100, dateW, 23, 12, Sketch20Style.Muted); Text("Loại quét", 42 + 2 * dateW, 100, typeW, 23, 12, Sketch20Style.Muted);
            DateBox("history-from", from, 20, 129, dateW, d => from = d);
            DateBox("history-to", to, 31 + dateW, 129, dateW, d => to = d);
            string[] types = { "Tất cả", "Quét nhanh", "Quét thư mục", "Quét tệp", "Quét toàn bộ", "Quét tùy chỉnh", "Thời gian thực" };
            SelectBox("history-filter", types, Math.Max(0, Array.IndexOf(types, scanFilter)), 42 + 2 * dateW, 129, typeW, i => scanFilter = types[i]);
            Button("filter-history", "Lọc", layoutWidth - 98, 132, 78, () =>
            {
                if (Actions.FilterHistory != null) Actions.FilterHistory(from, to, scanFilter);
                if (Model.Sample) Model.History = Sketch20Samples.Create(3).History.Where(h => scanFilter == "Tất cả" || h.Type == scanFilter).ToList();
                RefreshModel(true); Render();
            }, "filter", true);
            float[] starts = { 0, .153f, .334f, .55f, .67f, .73f, .86f };
            TableHeader(190, new[] { "Thời gian", "Loại quét", "Kết quả", "Tệp quét", "Đe dọa", "Thời lượng", "" }, starts);
            if (rows == 0) Text("Không có lịch sử quét phù hợp", 20, 271, layoutWidth - 40, 30, 13, Sketch20Style.Muted);
            for (int i = 0; i < rows; i++)
            {
                var row = Model.History[i]; int y = 229 + i * 66, index = row.Index;
                Text(row.Date, 14, y + 8, (int)(layoutWidth * .15) - 28, 24, 12, Sketch20Style.Ink, true); Text(row.Time, 14, y + 32, 125, 20, 11, Sketch20Style.Muted);
                Text(row.Type, (int)(layoutWidth * starts[1]) + 14, y + 18, (int)(layoutWidth * .17) - 20, 28, 12);
                int resultWidth = Math.Min(Sketch20Style.Measure(row.Result, 11, true) + 16, (int)(layoutWidth * (starts[3] - starts[2])) - 26);
                Badge(row.Result, (int)(layoutWidth * starts[2]) + 14, y + 22, row.Risk ? "warn" : "safe", resultWidth);
                Text(row.Files, (int)(layoutWidth * .55), y + 18, (int)(layoutWidth * .06), 28, 12, null, false, false, true);
                Text(row.Threats, (int)(layoutWidth * .67), y + 18, (int)(layoutWidth * .06), 28, 12, row.Risk ? Sketch20Style.Red : Sketch20Style.Ink, false, false, true);
                Text(row.Duration, (int)(layoutWidth * .73) + 14, y + 18, (int)(layoutWidth * .13) - 12, 28, 12);
                Button("history-detail-" + i, "Xem  ›", (int)(layoutWidth * .86) + 14, y + 17, 63, () => { if (Actions.Command != null) Actions.Command("history-detail:" + index); RefreshModel(true); if (Model.Sample && Model.Detail != null) Go("details"); }, "", false, false, 33, true);
                Line(0, y + 66, layoutWidth);
            }
            int footer = 229 + (rows == 0 ? 132 : rows * 66);
            Text("Tổng cộng: " + (Model.Sample ? Model.History.Count : Model.HistoryTotal) + " lần quét", 20, footer + 18, layoutWidth - 200, 25, 12, Sketch20Style.Muted);
            var prev = Button("history-prev", "", layoutWidth - 133, footer + 14, 38, () => Command("history-prev"), "chevron-left", false, false, 33); prev.Enabled = Model.HistoryPrev;
            Text(Model.Sample ? (Model.History.Count == 0 ? "0 / 0" : "1 / 1") : Model.HistoryPage, layoutWidth - 88, footer + 14, 40, 33, 11, Sketch20Style.Muted);
            var next = Button("history-next", "", layoutWidth - 57, footer + 14, 38, () => Command("history-next"), "chevron-right", false, false, 33); next.Enabled = Model.HistoryNext;
            Footer(81 + height + 18, "Hiển thị các phiên quét và cảnh báo thời gian thực", Model.Sample ? "Dữ liệu mẫu · 01–04/10/2026" : from.ToString("dd/MM/yyyy") + " – " + to.ToString("dd/MM/yyyy"));
        }
        private void Quarantine()
        {
            Callout(0, 81, layoutWidth, "Tệp cách ly không thể chạy. Chỉ khôi phục khi bạn xác định tệp an toàn.");
            int rows = Model.Quarantine.Count, height = 64 + 44 + (rows == 0 ? 142 : rows * 72) + 62;
            Card("quarantine-table", 0, 144, layoutWidth, height); Icon("archive", 20, 166, 18); Text("Tệp đang cách ly", 48, 159, layoutWidth - 150, 33, 17, Sketch20Style.Ink, true);
            Badge(rows + " tệp", layoutWidth - 64, 164, "warn");
            float[] starts = { 0f, .049f, .417f, .664f, .843f };
            TableHeader(208, new[] { "", "Tệp & đường dẫn", "Phát hiện", "Thời gian cách ly", "" }, starts, 44);
            Check("quarantine-all", "", 15, 216, 25, rows > 0 && quarantineSelection.Count == rows, selected =>
            {
                quarantineSelection.Clear(); if (selected) foreach (var f in Model.Quarantine) quarantineSelection.Add(f.Index);
                if (Actions.SelectAllQuarantine != null) Actions.SelectAllQuarantine(selected); Render();
            });
            if (rows == 0) Text(string.IsNullOrWhiteSpace(Model.QuarantineNotice) ? "Không có tệp nào trong khu vực cách ly." : Model.QuarantineNotice, 20, 280, layoutWidth - 40, 86, 13, Sketch20Style.Muted, false, true);
            for (int i = 0; i < rows; i++)
            {
                var f = Model.Quarantine[i]; int y = 252 + i * 72, index = f.Index;
                Check("quarantine-pick-" + i, "", 15, y + 22, 25, quarantineSelection.Contains(index), selected => { if (selected) quarantineSelection.Add(index); else quarantineSelection.Remove(index); if (Actions.SelectQuarantine != null) Actions.SelectQuarantine(index, selected); Render(); });
                Icon("file-warning", (int)(layoutWidth * .049) + 14, y + 26, 20, Sketch20Style.Muted);
                int tx = (int)(layoutWidth * .049) + 45;
                Text(f.Name, tx, y + 12, (int)(layoutWidth * .417) - tx - 10, 24, 12, Sketch20Style.Ink, true); Text(Model.Sample ? f.Path : Path.GetDirectoryName(f.Path ?? ""), tx, y + 36, (int)(layoutWidth * .417) - tx - 10, 20, 11, Sketch20Style.Muted);
                Badge(f.Severity, (int)(layoutWidth * .417) + 14, y + 14, Kind(f.Severity)); Text(f.Reason, (int)(layoutWidth * .417) + 14, y + 38, (int)(layoutWidth * .245) - 28, 20, 11, Sketch20Style.Muted);
                Text(f.Date, (int)(layoutWidth * .664) + 14, y + 12, (int)(layoutWidth * .17) - 15, 24, 12); Text(f.Time + " · " + f.Size, (int)(layoutWidth * .664) + 14, y + 36, (int)(layoutWidth * .17) - 15, 20, 11, Sketch20Style.Muted);
                Button("quarantine-detail-" + i, "Chi tiết  ›", (int)(layoutWidth * .843) + 14, y + 19, 86, () => OpenDetail(index, true), "", false, false, 33, true); Line(0, y + 72, layoutWidth);
            }
            int footer = 252 + (rows == 0 ? 142 : rows * 72);
            Text("Đã chọn " + quarantineSelection.Count + " tệp", 20, footer + 19, layoutWidth - 300, 25, 12, Sketch20Style.Muted);
            var restore = Button("restore-selected", "Khôi phục", layoutWidth - 254, footer + 14, 105, () => Command("restore"), "undo", false, false, 33); restore.Enabled = quarantineSelection.Count > 0;
            var delete = Button("delete-selected", "Xóa vĩnh viễn", layoutWidth - 141, footer + 14, 123, () => Command("delete"), "trash", false, true, 33); delete.Enabled = quarantineSelection.Count > 0;
            Footer(144 + height + 18, "Tệp được kiểm tra lại trước khi khôi phục", Model.Sample ? "Thao tác trên dữ liệu mẫu" : "Danh sách từ cơ sở dữ liệu");
        }

        private void Settings()
        {
            Tabs(81, new[] { "Chung", "Bảo vệ chủ động", "VirusTotal", "Dữ liệu & công cụ" }, SettingsTab, index => { SettingsTab = index; Render(); });
            int cardHeight = SettingsTab == 0 ? 403 : SettingsTab == 1 ? 481 : SettingsTab == 2 ? 329 : 349;
            Card("settings-content", 0, 144, layoutWidth, cardHeight);
            string[] titles = { "Khởi động & đồng bộ", "Bảo vệ chủ động", "Phân tích qua VirusTotal", "Dữ liệu & công cụ" };
            string[] notes = { "Tùy chỉnh cách Antivirus hoạt động trên máy tính.", "Thiết lập được đồng bộ với màn Bảo vệ.", "Tra cứu kết quả phân tích tệp bằng API key của bạn.", "Thông tin ứng dụng, dữ liệu quét và các công cụ kiểm thử." };
            Text(titles[SettingsTab], 22, 161, layoutWidth - 44, 29, 17, Sketch20Style.Ink, true); Text(notes[SettingsTab], 22, 190, layoutWidth - 44, 23, 12, Sketch20Style.Muted);
            if (SettingsTab <= 1)
            {
                string[] keys = SettingsTab == 0 ? new[] { "autostart", "update", "alerts", "samples" } : FeatureKeys.Skip(1).Take(5).ToArray();
                string[] names = SettingsTab == 0 ? new[] { "Khởi động cùng Windows", "Tự động cập nhật chữ ký", "Hiển thị thông báo", "Gửi mẫu tệp nghi ngờ" } : FeatureNames.Skip(1).Take(5).ToArray();
                string[] rowNotes = SettingsTab == 0 ? new[] { "Tự mở ứng dụng sau khi đăng nhập.", "Kiểm tra cập nhật khi mở ứng dụng.", "Thông báo khi phát hiện mối đe dọa.", "Cho phép gửi mẫu để hỗ trợ phân tích." } : FeatureNotes.Skip(1).Take(5).ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    int y = 212 + i * 78;
                    Text(names[i], 22, y + 9, layoutWidth - 125, 27, 14, Sketch20Style.Ink, true); Text(rowNotes[i], 22, y + 37, layoutWidth - 125, 23, 12, Sketch20Style.Muted);
                    Switch(keys[i], names[i], layoutWidth - 56, y + 27); if (i < keys.Length - 1) Line(22, y + 78, layoutWidth - 44);
                }
            }
            else if (SettingsTab == 2)
            {
                Text(FeatureNames[7], 22, 221, layoutWidth - 125, 27, 14, Sketch20Style.Ink, true); Text(FeatureNotes[7], 22, 249, layoutWidth - 125, 23, 12, Sketch20Style.Muted);
                Switch("vtq", FeatureNames[7], layoutWidth - 56, 239); Line(22, 290, layoutWidth - 44);
                Text("API key", 22, 307, layoutWidth - 44, 26, 12, Sketch20Style.Muted); PasswordBox(22, 335, layoutWidth - 44);
                Button("save-api-key", "Lưu API key", 22, 387, 129, () => { if (Actions.SaveKey != null) Actions.SaveKey(typedKey); if (Model.Sample) Model.KeyConfigured = !string.IsNullOrWhiteSpace(typedKey); typedKey = ""; RefreshModel(true); Render(); }, "key", true);
                Button("clear-api-key", "Gỡ API key", 159, 387, 100, () => { typedKey = ""; Command("clear-key"); });
                Text(Model.KeyConfigured ? "Đã cấu hình API key" : "Chưa cấu hình API key", 22, 429, layoutWidth - 44, 23, 12, Sketch20Style.Muted);
            }
            else
            {
                InfoRow("Phiên bản ứng dụng", Model.Version, 22, 227, layoutWidth - 44);
                InfoRow("CSDL chữ ký", Model.Signature, 22, 272, layoutWidth - 44);
                InfoRow("Đang cách ly", Model.Quarantine.Count + " tệp", 22, 317, layoutWidth - 44);
                InfoRow("Thư mục dữ liệu", Model.DataFolder, 22, 362, layoutWidth - 44, 145, 45, true, true);
                Button("open-data", "Mở thư mục dữ liệu", 22, 430, 181, () => Command("open-data"), "folder-open");
                Button("clear-cache", "Xóa cache quét", 211, 430, 149, () => Command("clear-cache"), "trash");
                Button("create-samples", "Tạo tệp mẫu", 368, 430, 137, () => Command("samples"), "file-plus");
            }
            int buttonsY = 144 + cardHeight + 18;
            Text("Công tắc bảo vệ được áp dụng ngay.", 0, buttonsY + 5, layoutWidth - 350, 30, 11, Sketch20Style.Muted);
            Button("restore-defaults", "Khôi phục mặc định", layoutWidth - 304, buttonsY, 159, () => Command("defaults"));
            Button("save-settings", "Lưu thiết lập", layoutWidth - 137, buttonsY, 137, () => Command("save-settings"), "check", true);
            Footer(buttonsY + 58, "Thiết lập bảo vệ được lưu trên máy", "Phiên bản " + Model.Version);
        }

        private void Advanced()
        {
            string[] names = { "Quét toàn bộ hệ thống", "Quét thư mục", "Quét tệp", "Quét tùy chỉnh" };
            string[] notes = { "Kiểm tra tất cả ổ đĩa và thư mục.", "Chọn một thư mục cần kiểm tra.", "Kiểm tra một hoặc nhiều tệp cụ thể.", "Chọn các vị trí phù hợp với bạn." };
            string[] icons = { "monitor", "folder-search", "file-search", "sliders" };
            int cw = (layoutWidth - 13) / 2;
            for (int i = 0; i < 4; i++)
            {
                int index = i, x = i % 2 * (cw + 13), y = 114 + i / 2 * 101;
                var mode = new Sketch20Mode(names[i], notes[i], icons[i]) { Name = "mode-" + i, Bounds = new Rectangle(x, y + bodyOffset, cw, 88), Checked = AdvancedMode == i };
                mode.Click += delegate { AdvancedMode = index; Render(); };
                mode.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) { AdvancedMode = (AdvancedMode + (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up ? 3 : 1)) % 4; Render(); Canvas.Controls["mode-" + AdvancedMode].Focus(); e.Handled = true; } };
                Canvas.Controls.Add(mode); Canvas.Nodes[mode.Name] = mode.Bounds;
            }
            int height = AdvancedMode == 3 ? 284 : 256;
            Card("scan-scope", 0, 323, layoutWidth, height); Text(names[AdvancedMode], 22, 340, layoutWidth - 44, 29, 17, Sketch20Style.Ink, true); Text("Phạm vi quét", 22, 370, layoutWidth - 44, 23, 12, Sketch20Style.Muted);
            if (AdvancedMode < 3)
            {
                Card("scan-path", 22, 408, layoutWidth - 44, 54, Sketch20Style.Background, Sketch20Style.Border, 8);
                Icon(AdvancedMode == 0 ? "hard-drive" : AdvancedMode == 1 ? "folder" : "files", 38, 427, 16);
                string path = AdvancedMode == 0 ? Model.Sample ? "Ổ đĩa C:\\ và D:\\ · Bao gồm thư mục con" : FullDriveText() : AdvancedMode == 1 ? Model.Sample && folders.Count == 0 ? @"C:\Users\User\Downloads" : folders.Count == 0 ? "Chưa chọn thư mục" : string.Join(" · ", folders) : Model.Sample && files.Count == 0 ? "invoice_update.exe · setup_script.ps1" : files.Count == 0 ? "Chưa chọn tệp" : string.Join(" · ", files.Select(Path.GetFileName));
                Text(path, 64, 421, AdvancedMode == 0 ? layoutWidth / 2 - 66 : layoutWidth - 218, 28, 12);
                if (AdvancedMode == 0) Badge("Toàn bộ", layoutWidth / 2 + 10, 422, "safe", layoutWidth / 2 - 48);
                else Button("choose-scope", AdvancedMode == 1 ? "Chọn thư mục" : "Chọn tệp", layoutWidth - 142, 418, 104, () => ChooseScope(AdvancedMode == 1), "", false, false, 33);
                if (AdvancedMode == 1) Check("include-subfolders", "Quét cả thư mục con", 22, 478, layoutWidth - 44, includeSubfolders, value => includeSubfolders = value);
                else Text(AdvancedMode == 0 ? "Thời gian quét tùy thuộc vào số lượng tệp trên máy tính." : Model.Sample && files.Count == 0 ? "Đã chọn 2 tệp · 260 KB" : "Đã chọn " + files.Count + " tệp", 22, 476, layoutWidth - 44, 27, 12, Sketch20Style.Muted);
            }
            else
            {
                string[] labels = { "Desktop", "Downloads", "Thư mục Temp", "Thư mục Startup" };
                for (int i = 0; i < 4; i++) { int index = i; Check("custom-location-" + i, labels[i], 22 + i % 2 * ((layoutWidth - 44) / 2 + 6), 409 + i / 2 * 34, (layoutWidth - 68) / 2, customLocations[i], value => customLocations[index] = value); }
                Button("add-custom-folder", "Thêm thư mục", 22, 486, 146, () => ChooseScope(true), "folder-plus");
                if (customFolders.Count > 0) Text(string.Join(" · ", customFolders), 179, 486, layoutWidth - 201, 40, 12, Sketch20Style.Muted);
            }
            int actionY = 323 + height - 63;
            Text("Sử dụng chữ ký và phân tích heuristic.", 22, actionY + 8, layoutWidth - 210, 24, 11, Sketch20Style.Muted);
            var start = Button("start-advanced", "Bắt đầu quét", layoutWidth - 158, actionY, 136, StartAdvanced, "scan", true);
            start.Enabled = !Model.Scanning && (Model.Sample || AdvancedMode == 0 || AdvancedMode == 1 && folders.Count > 0 || AdvancedMode == 2 && files.Count > 0 || AdvancedMode == 3 && (customLocations.Any(b => b) || customFolders.Count > 0));
            Footer(323 + height + 18, "Sẵn sàng bắt đầu quét", Model.Sample ? "CSDL chữ ký đã cập nhật" : "CSDL chữ ký: " + Model.Signature);
        }
        private static string FullDriveText()
        {
            try { string[] drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed).Select(d => d.Name).ToArray(); return drives.Length == 0 ? "Các ổ đĩa sẵn sàng · Bao gồm thư mục con" : "Ổ đĩa " + string.Join(" và ", drives) + " · Bao gồm thư mục con"; }
            catch { return "Các ổ đĩa sẵn sàng · Bao gồm thư mục con"; }
        }
        private void ChooseScope(bool folder)
        {
            if (Model.Sample) { if (AdvancedMode == 3) customFolders.Add(@"C:\Users\User\Documents"); else if (folder) folders = new List<string> { @"C:\Users\User\Documents" }; else files = new List<string> { "eicar-test.txt" }; Render(); return; }
            if (folder)
            {
                using (var dialog = new FolderBrowserDialog { Description = "Chọn thư mục cần quét" })
                    if (dialog.ShowDialog(FindForm()) == DialogResult.OK) { if (AdvancedMode == 3) customFolders.Add(dialog.SelectedPath); else folders = new List<string> { dialog.SelectedPath }; }
            }
            else using (var dialog = new OpenFileDialog { Title = "Chọn tệp cần quét", Multiselect = true }) if (dialog.ShowDialog(FindForm()) == DialogResult.OK) files = dialog.FileNames.ToList();
            Render();
        }
        private void StartAdvanced()
        {
            var targets = AdvancedMode == 1 ? folders.ToList() : AdvancedMode == 2 ? files.ToList() : customFolders.ToList();
            if (AdvancedMode == 3 && !Model.Sample)
            {
                string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] paths = { Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Path.Combine(user, "Downloads"), Path.GetTempPath(), Environment.GetFolderPath(Environment.SpecialFolder.Startup) };
                for (int i = 0; i < paths.Length; i++) if (customLocations[i] && Directory.Exists(paths[i])) targets.Add(paths[i]);
            }
            if (Actions.StartAdvanced != null) Actions.StartAdvanced(AdvancedMode, targets, includeSubfolders);
            if (Model.Sample) Command("scan");
            Go("overview");
        }

        private void OpenDetail(int index, bool quarantine)
        {
            if (Actions.OpenFile != null) Actions.OpenFile(index, quarantine);
            DetailReturnPage = quarantine ? "quarantine" : "overview";
            if (Model.Sample) Model.Detail = (quarantine ? Model.Quarantine : Model.Threats).FirstOrDefault(f => f.Index == index) ?? Model.Quarantine.FirstOrDefault();
            DetailTab = 0; Go("details");
        }
        private void Details()
        {
            var f = Model.Detail;
            Card("file-summary", 0, 114, layoutWidth, 97);
            Card("file-summary-icon", 20, 135, 49, 55, Sketch20Style.RedSoft, Color.Transparent, 10); Icon("file-warning", 30, 148, 29, Sketch20Style.Red);
            Text(f == null ? "Chưa chọn tệp" : f.Name, 84, 134, layoutWidth - 245, 31, 20, Sketch20Style.Ink, true);
            Text(f == null ? "Chọn Xem/Chi tiết từ danh sách tệp." : (Model.Sample ? f.Path : Path.GetDirectoryName(f.Path ?? "")) + " · " + f.Size, 84, 165, layoutWidth - 245, 23, 11, Sketch20Style.Muted);
            string status = f == null ? "Chưa có dữ liệu" : f.Status;
            Badge(status, layoutWidth - Sketch20Style.Measure(status, 11, true) - 36, 151, f != null && f.Resolved ? "warn" : "risk");
            Tabs(231, new[] { "Thông tin chi tiết", "VirusTotal", "Hành vi", "Chuỗi ký tự", "Thông tin bổ sung" }, DetailTab, index => { DetailTab = index; if (Actions.SelectDetailTab != null) Actions.SelectDetailTab(index); RefreshModel(true); Render(); });
            int height;
            if (DetailTab == 0) { height = 309; DetailInfo(f, height); }
            else if (DetailTab == 1) { height = VirusTotalHeight(); DetailVirusTotal(f, height); }
            else if (DetailTab == 2) { height = 390; DetailBehavior(f, height); }
            else if (DetailTab == 3) { height = Math.Max(175, 75 + Math.Min(100, Model.Strings.Count) * 46); DetailStrings(height); }
            else { height = 413; DetailExtra(f, height); }
            Footer(294 + height + 18, Model.Sample ? "Phân tích tệp mẫu" : "Phân tích tệp được chọn", "Chi tiết · VirusTotal · Hành vi · Chuỗi · Metadata");
        }
        private void DetailInfo(Sketch20File f, int height)
        {
            Card("detail-info", 0, 294, layoutWidth, height);
            int left = (int)((layoutWidth - 68) * 1.15 / 2.15), rightX = 22 + left + 24, rightWidth = layoutWidth - rightX - 22;
            Text("Thông tin tệp", 22, 311, left, 29, 17, Sketch20Style.Ink, true); Text("Kết quả phân tích", rightX, 311, rightWidth, 29, 17, Sketch20Style.Ink, true);
            string[] labels = { "Tên tệp", "Loại tệp", "Kích thước", "Phát hiện lúc", "Nguồn phát hiện" };
            string[] values = { f == null ? "—" : f.Name, f == null ? "—" : f.FileType, f == null ? "—" : f.Size, f == null ? "—" : f.Date + " · " + f.Time, f == null ? "—" : f.Source };
            for (int i = 0; i < 5; i++) InfoRow(labels[i], values[i], 22, 354 + i * 45, left, 100, 45, i == 4);
            Card("analysis-result", rightX, 356, rightWidth, 133, Sketch20Style.Background, Color.Transparent, 9);
            Badge(f == null ? "Chưa đánh giá" : "Mức độ " + (f.Severity ?? "").ToLowerInvariant(), rightX + 17, 373, f == null ? "info" : Kind(f.Severity));
            Text(f == null ? "Chưa có kết quả" : f.Reason, rightX + 17, 407, rightWidth - 34, 23, 14, Sketch20Style.Ink, true);
            Text("Tệp có các dấu hiệu đáng ngờ trong cấu trúc và nội dung. Cần kiểm tra trước khi chạy hoặc khôi phục.", rightX + 17, 433, rightWidth - 34, 48, 12, Sketch20Style.Muted, false, true);
            Callout(rightX, 502, rightWidth, f != null && f.Resolved ? "Khuyến nghị giữ tệp trong khu vực cách ly." : "Khuyến nghị cách ly tệp trước khi tiếp tục.", true);
            if (!Model.Sample && f != null && !f.Resolved)
            {
                Button("isolate-file", "Cách ly tệp", rightX, 561, (rightWidth - 8) / 2, () => Command("isolate-file"), "archive", true, false, 33);
                Button("delete-file", "Xóa tệp", rightX + (rightWidth + 8) / 2, 561, (rightWidth - 8) / 2, () => Command("delete-file"), "trash", false, true, 33);
            }
        }
        private int VirusTotalHeight()
        {
            int rows = Math.Min(10, Model.Engines.Count);
            return 251 + (rows == 0 ? 88 : 32 + rows * 32);
        }
        private void DetailVirusTotal(Sketch20File f, int height)
        {
            Card("detail-virustotal", 0, 294, layoutWidth, height);
            bool hasCounts = Model.VtDetections.HasValue && Model.VtEngineCount.HasValue;
            bool hasReport = hasCounts && Model.VtEngineCount.Value > 0;
            bool needsKey = !Model.Sample && !Model.KeyConfigured;
            Color resultColor = hasReport ? Model.VtDetections.Value > 0 ? Sketch20Style.Red : Sketch20Style.Green : Sketch20Style.Muted;
            string summary = f == null ? "Chọn một tệp để xem kết quả phân tích."
                : hasReport ? Model.VtDetections.Value + " trong " + Model.VtEngineCount.Value + " công cụ đánh dấu tệp có dấu hiệu độc hại."
                : hasCounts ? "Kết quả này chưa có công cụ phân tích."
                : needsKey ? "Thêm API key trong Cài đặt → VirusTotal để bắt đầu tra cứu."
                : "Chưa có kết quả. Bấm Tra cứu VirusTotal để kiểm tra tệp.";
            Text("Kết quả phân tích", 22, 311, layoutWidth - 222, 29, 17, Sketch20Style.Ink, true);
            var query = Button("query-virustotal", needsKey ? "Cấu hình API key" : hasReport ? "Tra cứu lại" : "Tra cứu VirusTotal",
                layoutWidth - 184, 310, 162, () => { if (needsKey) { SettingsTab = 2; Go("settings"); } else Command("vt-query"); }, "", true, false, 36);
            query.Enabled = needsKey || f != null;
            Text(summary, 22, 351, layoutWidth - 44, 25, 13, resultColor);
            string[] labels = { "Công cụ cảnh báo", "Tổng công cụ phân tích", "Lần tra cứu" };
            string[] values = { hasCounts ? Model.VtDetections.Value.ToString() : "—", hasCounts ? Model.VtEngineCount.Value.ToString() : "—", Model.VtTime };
            int cw = (layoutWidth - 68) / 3;
            for (int i = 0; i < 3; i++)
            {
                int x = 22 + i * (cw + 12), width = i == 2 ? layoutWidth - 22 - x : cw;
                Card("vt-stat-" + i, x, 386, width, 74, Sketch20Style.Background, Sketch20Style.Border, 9);
                Text(labels[i], x + 15, 398, width - 30, 22, 12, Sketch20Style.Muted);
                Text(values[i], x + 15, 422, width - 30, 36, i == 2 ? 13 : 25, i == 0 ? resultColor : Sketch20Style.Ink, i < 2, i == 2);
            }
            int rows = Math.Min(10, Model.Engines.Count), tableHeight = rows == 0 ? 88 : 32 + rows * 32;
            Text("Chi tiết theo công cụ", 22, 466, layoutWidth - 260, 25, 14, Sketch20Style.Ink, true);
            Text(rows + (Model.Engines.Count > rows ? " / " + Model.Engines.Count : "") + " kết quả hiển thị", layoutWidth - 222, 466, 200, 25, 11, Sketch20Style.Muted, false, false, true);
            Card("vt-engine-table", 22, 501, layoutWidth - 44, tableHeight, Color.White, Sketch20Style.Border, 8);
            int split = 22 + (layoutWidth - 44) / 3;
            Text("Công cụ phân tích", 36, 501, split - 50, 32, 12, Sketch20Style.Muted, true);
            Text("Nhận diện / kết quả", split + 14, 501, layoutWidth - split - 50, 32, 12, Sketch20Style.Muted, true);
            Line(22, 533, layoutWidth - 44);
            for (int i = 0; i < rows; i++)
            {
                var row = Model.Engines[i]; int y = 533 + i * 32;
                Text(row.Length > 0 ? row[0] : "—", 36, y + 2, split - 50, 28, 12, Sketch20Style.Ink, true);
                Text(row.Length > 1 ? row[1] : "Chưa có kết quả", split + 14, y + 2, layoutWidth - split - 50, 28, 12, Sketch20Style.Ink);
                if (i < rows - 1) Line(22, y + 32, layoutWidth - 44);
            }
            if (rows == 0) Text(hasReport ? "Kết quả này chưa kèm danh sách từng công cụ." : "Chưa có dữ liệu. Kết quả sẽ xuất hiện sau khi tra cứu.", 36, 540, layoutWidth - 72, 42, 12, Sketch20Style.Muted, false, true);
            Text(Model.Sample ? "Dữ liệu minh họa. Tra cứu thật sẽ dùng mã băm của tệp." : "Công cụ cảnh báo: công cụ đánh dấu tệp có dấu hiệu độc hại.", 22, 501 + tableHeight + 10, layoutWidth - 44, 24, 12, Sketch20Style.Muted, false, true);
        }
        private void DetailBehavior(Sketch20File f, int height)
        {
            Card("detail-behavior", 0, 294, layoutWidth, height); Text("Dấu hiệu hành vi", 22, 311, layoutWidth - 44, 29, 17, Sketch20Style.Ink, true);
            Text(Model.Sample ? "Các chỉ dấu minh họa từ quá trình phân tích tệp." : "Các chỉ dấu tĩnh thu được từ quá trình phân tích tệp.", 22, 340, layoutWidth - 44, 23, 12, Sketch20Style.Muted);
            int y = 380;
            for (int i = 0; i < Model.Behavior.Count; i++)
            {
                var row = Model.Behavior[i]; int h = row.Length > 2 ? 74 : 45;
                InfoRow(row[0], row[1], 22, y, layoutWidth - 44, 145, h, i == Model.Behavior.Count - 1, false, row.Length > 2 ? row[2] : null, row.Length > 3 ? row[3] : null); y += h;
            }
            if (Model.Behavior.Count == 0) Text("Chưa có tệp được chọn để phân tích.", 22, y, layoutWidth - 44, 40, 12, Sketch20Style.Muted);
        }
        private void DetailStrings(int height)
        {
            Card("detail-strings", 0, 294, layoutWidth, height); Text("Chuỗi ký tự trích xuất", 22, 311, layoutWidth - 180, 29, 17, Sketch20Style.Ink, true);
            Badge(Model.Strings.Count + (Model.Sample ? " chuỗi mẫu" : " chuỗi"), layoutWidth - 106, 317);
            for (int i = 0; i < Math.Min(100, Model.Strings.Count); i++)
            {
                int y = 352 + i * 46;
                Text((i + 1).ToString("000"), 22, y + 8, 33, 29, 11, Sketch20Style.Muted); Text(Model.Strings[i], 70, y + 8, layoutWidth - 92, 29, 12, Sketch20Style.Ink, false, false, false, true);
                if (i < Model.Strings.Count - 1) Line(22, y + 46, layoutWidth - 44);
            }
            if (Model.Strings.Count == 0) Text("Không có chuỗi ký tự được trích xuất.", 22, 370, layoutWidth - 44, 35, 12, Sketch20Style.Muted);
        }
        private void DetailExtra(Sketch20File f, int height)
        {
            Card("detail-extra", 0, 294, layoutWidth, height); Text("Thông tin bổ sung", 22, 311, layoutWidth - 44, 29, 17, Sketch20Style.Ink, true);
            InfoRow("Đường dẫn gốc", f == null ? "—" : f.Path + (Model.Sample ? "\\" + f.Name : ""), 22, 354, layoutWidth - 44, 145, 47, false, true);
            InfoRow("SHA-256", f == null ? "—" : f.Sha256, 22, 401, layoutWidth - 44, 145, 47, false, true);
            InfoRow("MD5", f == null ? "—" : f.Md5, 22, 448, layoutWidth - 44, 145, 47, false, true);
            InfoRow("Chữ ký số", "Chưa xác thực", 22, 495, layoutWidth - 44, 145, 51, false, false, "warn");
            InfoRow("Ngày tạo tệp", f == null ? "—" : f.Created, 22, 546, layoutWidth - 44);
            InfoRow("Trạng thái", f == null ? "—" : f.Status, 22, 591, layoutWidth - 44);
            InfoRow("Mã mục cách ly", f == null ? "—" : f.Id, 22, 636, layoutWidth - 44, 145, 45, true);
            var menu = new ContextMenuStrip();
            menu.Font = Sketch20Style.Font(13);
            menu.Items.Add("Sao chép đường dẫn gốc", null, delegate { if (f != null && !string.IsNullOrWhiteSpace(f.Path)) Clipboard.SetText(f.Path + (Model.Sample ? "\\" + f.Name : "")); });
            menu.Items.Add("Sao chép SHA-256", null, delegate { if (f != null && !string.IsNullOrWhiteSpace(f.Sha256)) Clipboard.SetText(f.Sha256); });
            menu.Items.Add("Sao chép MD5", null, delegate { if (f != null && !string.IsNullOrWhiteSpace(f.Md5)) Clipboard.SetText(f.Md5); });
            Canvas.ContextMenuStrip = menu;
        }

        private void SelectBox(string name, string[] choices, int selected, int x, int y, int width, Action<int> changed, int height = 43)
        {
            var control = new Sketch20Choice(choices, selected) { Name = name, Bounds = new Rectangle(x, y + bodyOffset, width, height) };
            control.Chosen += changed; Canvas.Controls.Add(control); Canvas.Nodes[name] = control.Bounds;
        }
        private void DateBox(string name, DateTime date, int x, int y, int width, Action<DateTime> changed)
        {
            var control = new Sketch20Date(date, Model.Sample) { Name = name, Bounds = new Rectangle(x, y + bodyOffset, width, 43) };
            control.Chosen += changed; Canvas.Controls.Add(control); Canvas.Nodes[name] = control.Bounds;
        }
        private void PasswordBox(int x, int y, int width)
        {
            var input = new Sketch20Password { Name = "api-key", Bounds = new Rectangle(x, y + bodyOffset, width, 40) };
            input.Entry.Text = typedKey; input.Entry.TextChanged += delegate { typedKey = input.Entry.Text; }; Canvas.Controls.Add(input); Canvas.Nodes[input.Name] = input.Bounds;
        }
    }
}
