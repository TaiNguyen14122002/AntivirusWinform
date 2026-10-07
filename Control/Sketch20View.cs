using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    // Một renderer dùng cho cả ứng dụng và chế độ xem 20 phác thảo.
    // Chế độ xem cung cấp DTO riêng; ứng dụng đọc DTO từ controller/service hiện có.
    internal sealed partial class Sketch20View : UserControl
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
        internal Form WindowForm;
        private string previousStamp;
        private int layoutWidth;
        private int bodyOffset;
        private readonly ToolTip toolTips = new ToolTip();
        private Sketch20Button settingsShortcut;
        private bool rendering;
        private bool needsScrollbar;
        private int detailBodyY = 306;
        private DateTime? scanningSince;
        private DateTime lastScanRenderUtc = DateTime.MinValue;
        private int stringsOffset, engineOffset, historyOffset, quarantineOffset;
        private bool quarantineOldest;
        private readonly HashSet<int> quarantineSelection = new HashSet<int>();
        private List<string> folders = new List<string>(), files = new List<string>(), customFolders = new List<string>();
        private bool[] customLocations = { true, true, false, false };
        private bool includeSubfolders = true;
        private readonly HashSet<string> excludedDrives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string engineSearch = "", stringSearch = "";
        private DateTime from = new DateTime(2024, 11, 1), to = new DateTime(2024, 11, 30);
        private string scanFilter = "Tất cả", typedKey = "";

        internal Sketch20View(Sketch20Model initial, Sketch20Actions actions, Func<Sketch20Model> source = null)
        {
            Model = initial; Actions = actions ?? new Sketch20Actions(); readModel = source;
            if (!initial.Sample) { from = DateTime.Today.AddDays(-30); to = DateTime.Today; }
            AutoScaleMode = AutoScaleMode.None; Dock = DockStyle.Fill; BackColor = Sketch20Style.Background;
            sidebar = new Panel { Dock = DockStyle.Left, Width = 208, BackColor = Sketch20Style.Sidebar };
            Theme.BatDoubleBuffer(this);
            Theme.BatDoubleBuffer(sidebar); sidebar.Paint += DrawSidebar;
            viewport = new Panel { Name = "Sketch20Viewport", Dock = DockStyle.Fill, AutoScroll = true, BackColor = Sketch20Style.Background };
            Theme.BatDoubleBuffer(viewport);
            Canvas = new Sketch20Surface { Name = "Sketch20Canvas", Location = new Point(26, 29) };
            viewport.Controls.Add(Canvas); Controls.Add(viewport); Controls.Add(sidebar);
            Canvas.LocationChanged += delegate { PositionSettingsShortcut(); };
            viewport.Scroll += delegate { PositionSettingsShortcut(); };
            Resize += delegate { Render(); }; viewport.Resize += delegate { if (!rendering) Render(); };
            update = new Timer { Interval = 500 };
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
            if (Model.Sample) { Model = Sketch20Samples.Create(number); if (number >= 7 && number <= 10) DetailReturnPage = "overview"; if (number == 11) DetailReturnPage = "quarantine"; if (number == 4) { quarantineSelection.Clear(); quarantineSelection.Add(0); quarantineSelection.Add(1); } }
            SettingsTab = number == 12 ? 1 : number == 13 ? 2 : number == 14 ? 3 : 0;
            DetailTab = number >= 7 && number <= 11 ? number - 7 : 0;
            AdvancedMode = number >= 15 && number <= 17 ? number - 14 : 0;
            Page = number == 2 ? "protection" : number == 3 ? "history" : number == 4 ? "quarantine" : number == 5 || number >= 12 && number <= 14 ? "settings" : number == 6 || number >= 15 && number <= 17 ? "advanced" : number >= 7 && number <= 11 ? "details" : "overview";
            stringsOffset = engineOffset = historyOffset = quarantineOffset = 0; viewport.AutoScrollPosition = Point.Empty; NotifyPage(); Render(); ResetPageScroll();
        }
        internal void Go(string page)
        {
            Page = page;
            if (page == "history") historyOffset = 0;
            if (page == "quarantine") quarantineOffset = 0;
            viewport.AutoScrollPosition = Point.Empty;
            // Chuyển trang trước để click phản hồi ngay. Backend/controller cũ được
            // đồng bộ sau frame đầu, tránh giữ UI thread 1-3 giây trước khi đổi màn.
            NotifyPage();
            Render();
            ResetPageScroll();
            if (readModel != null && IsHandleCreated)
                BeginInvoke((Action)(() => { if (!IsDisposed) RefreshModel(true); }));
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
                SortQuarantine(next);
                if (next.Scanning) { if (!scanningSince.HasValue || !Model.Scanning) scanningSince = DateTime.UtcNow; next.ElapsedSeconds = Math.Max(0, (int)(DateTime.UtcNow - scanningSince.Value).TotalSeconds); } else scanningSince = null;
                string stamp = next.Stamp();
                if (!force && stamp == previousStamp) return;
                if (string.Join("|", Model.Quarantine.Select(f => f.Id)) != string.Join("|", next.Quarantine.Select(f => f.Id)))
                {
                    quarantineSelection.Clear();
                    if (Actions.SelectAllQuarantine != null) Actions.SelectAllQuarantine(false);
                }
                bool wasScanning = Model != null && Model.Scanning;
                Model = next; previousStamp = stamp;
                // Trong lúc quét, dữ liệu engine thay đổi rất dày. Dựng lại toàn bộ cây
                // control mỗi 500ms gây rung/flicker; giới hạn full render còn ~1 lần/giây.
                if (!force && Page == "overview" && next.Scanning && wasScanning)
                {
                    DateTime now = DateTime.UtcNow;
                    if ((now - lastScanRenderUtc).TotalMilliseconds < 950) return;
                    lastScanRenderUtc = now;
                }
                else if (next.Scanning != wasScanning) lastScanRenderUtc = DateTime.UtcNow;
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
                    Bounds = new Rectangle(10, 91 + i * 51, 188, 44) };
                nav.FlatAppearance.BorderSize = 0;
                // Navbar không đổi nền khi hover/nhấn; chỉ trạng thái active mới được tô màu.
                nav.FlatAppearance.MouseOverBackColor = Sketch20Style.Sidebar;
                nav.FlatAppearance.MouseDownBackColor = Sketch20Style.Sidebar;
                nav.UseVisualStyleBackColor = false;
                nav.Paint += delegate(object sender, PaintEventArgs e) { DrawNav(e.Graphics, (Button)sender, page); };
                nav.Click += delegate { Go(page); };
                sidebar.Controls.Add(nav);
            }
            string[] sectionNames = { "Chung", "Bảo vệ chủ động", "VirusTotal", "Dữ liệu & công cụ" };
            string[] sectionIcons = { "settings", "shield-check", "sigma", "hard-drive" };
            for (int i = 0; i < sectionNames.Length; i++)
            {
                int index = i;
                var item = new Sketch20Button(sectionNames[i], sectionIcons[i]) { Name = "settings-section-" + i, Tab = true, TextPixels = 12, Bounds = new Rectangle(10, 347 + i * 46, 188, 40), Visible = false };
                item.Click += delegate { SettingsTab = index; Go("settings"); };
                sidebar.Controls.Add(item);
            }
        }

        private void DrawNav(Graphics g, Button button, string page)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bool active = page == Page || page == "overview" && (Page == "advanced" || Page == "details" && DetailReturnPage == "overview") || page == "quarantine" && Page == "details" && DetailReturnPage == "quarantine" || page == "history" && Page == "details" && DetailReturnPage == "history";
            Sketch20Style.Round(g, button.ClientRectangle, 10, active ? Sketch20Style.BlueSoft : Sketch20Style.Sidebar, Color.Transparent);
            if (active) using (var b = new SolidBrush(Sketch20Style.Blue)) g.FillRectangle(b, 0, 9, 3, 28);
            Color ink = active ? Sketch20Style.Blue : Sketch20Style.Ink;
            string icon = page == "overview" ? "house" : page == "protection" ? "shield" : page == "quarantine" ? "archive" : page == "history" ? "history" : "settings";
            if (Page == "settings") Sketch20Style.Icon(g, icon, new Rectangle(17, 12, 20, 20), active ? Sketch20Style.Blue : Sketch20Style.Muted);
            else Sketch20ReferenceAssets.FilledIcon(g, page == "protection" ? "shield-check" : page == "quarantine" ? "cube" : page == "history" ? "clock" : icon, new Rectangle(17, 12, 20, 20), active ? Sketch20Style.Blue : Sketch20Style.Muted);
            Sketch20Style.Text(g, button.Text, new Rectangle(52, 0, 115, 44), 14, ink, active);
            if (Page == "settings" && page == "quarantine" && Model.Quarantine.Count > 0)
            {
                Sketch20Style.Round(g, new Rectangle(145, 14, 21, 20), 7, active ? Color.White : Sketch20Style.BlueSoft, Color.Transparent);
                Sketch20Style.Text(g, Model.Quarantine.Count.ToString(), new Rectangle(145, 14, 21, 20), 10.5f, Sketch20Style.Blue, true, false, true);
            }
            if (button.Focused) ControlPaint.DrawFocusRectangle(g, new Rectangle(5, 5, 166, 36));
        }

        private void DrawSidebar(object sender, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var pen = new Pen(Sketch20Style.Border)) g.DrawLine(pen, 207, 0, 207, sidebar.Height);
            if (Page == "settings") Sketch20Artwork.Draw(g, new Rectangle(22, 21, 39, 44), "safe");
            else Sketch20ReferenceAssets.Logo(g, new Rectangle(27, 26, 27, 32));
            Sketch20Style.Text(g, "Antivirus", new Rectangle(65, 25, 131, 34), 20, Sketch20Style.Ink, true);

            // Sidebar giữ điều hướng và phiên bản.
            using (var pen = new Pen(Sketch20Style.Border)) g.DrawLine(pen, 24, sidebar.Height - 52, 184, sidebar.Height - 52);
            Sketch20Style.Text(g, "Phiên bản " + Model.Version, new Rectangle(26, sidebar.Height - 43, 156, 24), 10.5f, Sketch20Style.Muted);
        }


        internal void Render()
        {
            if (rendering || IsDisposed || viewport.ClientSize.Width < 90) return;
            rendering = true;
            bool fixedScanViewport = Page == "overview" && Model != null && Model.Scanning;
            var scroll = fixedScanViewport ? Point.Empty : viewport.AutoScrollPosition;
            try
            {
                // Tổng quan khi quét là viewport cố định: không tạo scrollbar và không
                // toggle AutoScroll qua lại theo từng nhịp progress.
                viewport.AutoScroll = false;
                Canvas.Location = new Point(26, 29);
                Canvas.SuspendLayout();
                toolTips.RemoveAll();
                if (Canvas.ContextMenuStrip != null) { Canvas.ContextMenuStrip.Dispose(); Canvas.ContextMenuStrip = null; }
                foreach (System.Windows.Forms.Control c in Canvas.Controls.Cast<System.Windows.Forms.Control>().ToArray()) c.Dispose();
                Canvas.Drawing.Clear(); Canvas.BackgroundDrawing.Clear(); Canvas.Nodes.Clear(); Canvas.TextRuns.Clear();
                layoutWidth = Math.Max(500, viewport.Width - 52 - (!fixedScanViewport && needsScrollbar ? SystemInformation.VerticalScrollBarWidth : 0));
                Canvas.Width = layoutWidth;
                bodyOffset = 0;
                PageTools();
                // Nội dung bắt đầu ngay dưới hàng công cụ, không giữ khoảng trống
                // của tiêu đề/mô tả cũ (81px, hoặc 114px ở màn có nút quay lại).
                bodyOffset = 0;
                if (Page == "protection") ReferenceProtection();
                else if (Page == "history") History();
                else if (Page == "quarantine") ReferenceQuarantine();
                else if (Page == "settings") Settings();
                else if (Page == "advanced") Advanced();
                else if (Page == "details") ReferenceDetails();
                else Overview();
                if (WindowForm != null) Sketch20Chrome.AddControls(WindowForm, this, layoutWidth);
                settingsShortcut = Button("nav-settings", "", layoutWidth - 42, 0, 36, () => Go("settings"), "settings", false, false, 32);
                settingsShortcut.AccessibleName = "Cài đặt";
                settingsShortcut.IconPixels = 22;
                settingsShortcut.Selected = Page == "settings";
                toolTips.SetToolTip(settingsShortcut, "Cài đặt");
                settingsShortcut.BringToFront();
                Canvas.Height = fixedScanViewport ? Math.Max(1, viewport.ClientSize.Height - Canvas.Top) : ContentHeight;
                // Khi đang quét, tuyệt đối không sinh scrollbar. Các màn còn lại giữ
                // hành vi cuộn cũ.
                viewport.AutoScrollMinSize = fixedScanViewport ? Size.Empty : new Size(0, ContentHeight + (Page == "settings" ? 45 : 31));
                viewport.AutoScroll = !fixedScanViewport;
                if (!fixedScanViewport) viewport.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
                PositionSettingsShortcut();
                Canvas.ResumeLayout(); Canvas.Invalidate(); sidebar.Invalidate();
                foreach (System.Windows.Forms.Control c in sidebar.Controls)
                {
                    if (c.Name.StartsWith("settings-section-"))
                    {
                        c.Visible = false;
                        ((Sketch20Button)c).Selected = c.Name == "settings-section-" + SettingsTab;
                    }
                    c.Invalidate();
                }
            }
            finally { rendering = false; }
            bool needs = !fixedScanViewport && ContentHeight + (Page == "settings" ? 45 : 31) > viewport.Height;
            if (needsScrollbar != needs) { needsScrollbar = needs; Render(); }
        }
        private void PositionSettingsShortcut()
        {
            if (settingsShortcut == null || settingsShortcut.IsDisposed) return;
            // Compensate for the scrolling canvas so settings stays in the window corner.
            settingsShortcut.Location = new Point(layoutWidth - 42, Math.Max(0, -viewport.AutoScrollPosition.Y));
            Canvas.Nodes[settingsShortcut.Name] = settingsShortcut.Bounds;
        }
        private float ReferenceTextScale { get { return Page == "settings" ? 1f : Math.Min(1.17f, Math.Max(1.02f, layoutWidth / 820f)); } }
        private new void Text(string value, int x, int y, int width, int height = 20, float pixels = 13, Color? color = null, bool bold = false, bool wrap = false, bool right = false, bool mono = false)
        {
            if (pixels <= 20) pixels *= ReferenceTextScale;
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
            Action<Graphics> paint = g =>
            {
                if ((fill ?? Color.White) == Color.White)
                    Sketch20Style.Round(g, new Rectangle(bounds.X + 1, bounds.Y + 3, bounds.Width, bounds.Height), radius, Sketch20Style.Shadow, Color.Transparent);
                Sketch20Style.Round(g, bounds, radius, fill ?? Color.White, edge ?? Sketch20Style.Border);
            };
            Canvas.Drawing.Add(paint); Canvas.BackgroundDrawing.Add(paint);
        }
        private void GradientCard(string name, int x, int y, int width, int height, Color start, Color end, Color edge, int radius = 14)
        {
            var bounds = new Rectangle(x, y + bodyOffset, width, height); Canvas.Nodes[name] = bounds;
            Action<Graphics> paint = g =>
            {
                Sketch20Style.Round(g, new Rectangle(bounds.X + 1, bounds.Y + 4, bounds.Width, bounds.Height), radius, Sketch20Style.Shadow, Color.Transparent);
                using (var path = UiKit.Round(new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1), radius))
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(bounds, start, end, System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                using (var pen = new Pen(edge)) { g.FillPath(brush, path); g.DrawPath(pen, path); }
            };
            Canvas.Drawing.Add(paint); Canvas.BackgroundDrawing.Add(paint);
        }

        private void Artwork(string name, int x, int y, int width, int height, string kind)
        {
            var bounds = new Rectangle(x, y + bodyOffset, width, height);
            Canvas.Nodes[name] = bounds;
            Canvas.Drawing.Add(g => { if (kind.StartsWith("reference-")) Sketch20ReferenceAssets.Draw(g, bounds, kind.Substring(10)); else Sketch20Artwork.Draw(g, bounds, kind); });
        }

        private void Badge(string caption, int x, int y, string kind = "safe", int width = 0)
        {
            Color ink = kind == "technical" ? ColorTranslator.FromHtml("#8840f8") : kind == "neutral" ? Sketch20Style.Muted : kind == "risk" ? Sketch20Style.Red : kind == "warn" ? Sketch20Style.Amber : kind == "info" ? Sketch20Style.Blue : Sketch20Style.Green;
            Color bg = kind == "technical" ? ColorTranslator.FromHtml("#f0e9ff") : kind == "neutral" ? ColorTranslator.FromHtml("#edf2f8") : kind == "risk" ? Sketch20Style.RedSoft : kind == "warn" ? Sketch20Style.AmberSoft : kind == "info" ? Sketch20Style.BlueSoft : Sketch20Style.GreenSoft;
            int w = width > 0 ? width : Sketch20Style.Measure(caption, 11, true) + 16;
            Card("badge-" + x + "-" + y, x, y, w, 24, bg, Color.Transparent, 5);
            bool indicator = Page != "settings" && new[] { "Cao", "Trung bình", "Nguy hiểm", "Đáng ngờ", "Đang cách ly", "Đã cách ly", "An toàn" }.Contains(caption);
            if (indicator)
            {
                int py = y + bodyOffset;
                Canvas.Drawing.Add(g => { using (var brush = new SolidBrush(ink)) g.FillEllipse(brush, x + 8, py + 5, 14, 14); });
                Icon(kind == "safe" ? "check" : "alert", x + 10, y + 7, 10, Color.White);
            }
            Text(caption, x + (indicator ? 28 : 8), y, w - (indicator ? 32 : 12), 24, 11, ink, true);
        }
        private Sketch20Button Button(string name, string caption, int x, int y, int width, Action click, string icon = "", bool primary = false, bool danger = false, int height = 40, bool link = false)
        {
            var b = new Sketch20Button(caption, icon) { Name = name, Primary = primary, Danger = danger, Link = link,
                Bounds = new Rectangle(x, y + bodyOffset, width, height), TextPixels = (height <= 33 ? 12 : 13) * ReferenceTextScale };
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
            var c = new Sketch20Check(caption) { TextPixels = 13 * ReferenceTextScale, Name = name, Bounds = new Rectangle(x, y + bodyOffset, width, 25), Checked = value };
            c.CheckedChanged += delegate { change(c.Checked); }; Canvas.Controls.Add(c); Canvas.Nodes[name] = c.Bounds; return c;
        }
        private void PageTools()
        {
            string title = Page == "overview" ? "Tổng quan" : Page == "protection" ? "Bảo vệ" : Page == "history" ? "Lịch sử" : Page == "quarantine" ? "Cách ly" : Page == "settings" ? "Cài đặt" : Page == "advanced" ? "Quét virus" : "Chi tiết tệp";
            string subtitle = Page == "overview" ? "Theo dõi trạng thái bảo vệ và hoạt động của máy tính." :
                Page == "protection" ? "Các mô-đun bảo vệ giúp giữ an toàn cho thiết bị của bạn." :
                Page == "history" ? "Xem lại các lần quét, phát hiện và hành động đã thực hiện." :
                Page == "quarantine" ? "Các tệp có khả năng gây hại đã được cách ly để đảm bảo an toàn cho thiết bị." :
                Page == "settings" ? "Tùy chỉnh hoạt động và các tính năng bảo vệ theo nhu cầu của bạn." :
                Page == "advanced" ? "Chọn chế độ quét phù hợp để kiểm tra và loại bỏ các mối đe dọa." :
                "Xem thông tin chi tiết và kết quả phân tích của tệp được phát hiện.";

            if (Page == "details" && DetailTab >= 2) return;
            if (Page == "details")
            {
                Button("details-back", "Quay lại", 0, 0, 92, () => Go(DetailReturnPage), "arrow-left", false, false, 25, true);
                Text(title, 0, 28, layoutWidth - 200, 40, 25, Sketch20Style.Ink, true);
                Text(subtitle, 0, 65, layoutWidth - 210, 24, 12, Sketch20Style.Muted);
            }
            else if (Page == "advanced")
            {
                var back = Button("advanced-back", "", 0, 6, 32, () => Go("overview"), "arrow-left", false, false, 32, true);
                back.AccessibleName = "Quay lại Tổng quan";
                Text(title, 44, 0, layoutWidth - 274, 44, 27, Sketch20Style.Ink, true);
                Text(subtitle, 44, 39, layoutWidth - 294, 25, 12.5f, Sketch20Style.Muted);
            }
            else
            {
                Text(title, 0, 0, layoutWidth - 230, 44, 27, Sketch20Style.Ink, true);
                Text(subtitle, 0, 39, layoutWidth - 250, 25, 12.5f, Sketch20Style.Muted);
            }

            int right = layoutWidth;
        }

        private void Footer(int y, string left, string right)
        {
            Text(left, 0, y, layoutWidth / 2, 18, 11, Sketch20Style.Muted);
            Text(right, layoutWidth / 2, y, layoutWidth / 2, 18, 11, Sketch20Style.Muted, false, false, true);
            ContentHeight = y + bodyOffset + 18;
        }
        private void Tabs(int y, string[] titles, int selected, Action<int> choose)
        {
            string[] icons = Page == "settings" ? new[] { "settings", "shield-check", "sigma", "hard-drive" } : new[] { "file", "sigma", "activity", "files", "shield-check" };
            float pixels = layoutWidth < 850 ? 11.5f : 13;
            int[] widths = titles.Select((t, i) => Sketch20Style.Measure(t, pixels, i == selected) + 50).ToArray();
            int total = widths.Sum() + 5 * (titles.Length - 1);
            int x = 0;
            for (int i = 0; i < titles.Length; i++)
            {
                int index = i, width = total > layoutWidth ? widths[i] * (layoutWidth - 5 * (titles.Length - 1)) / (total - 5 * (titles.Length - 1)) : widths[i];
                var b = new Sketch20Button(titles[i], icons[i]) { Name = "tab-" + i, Tab = true, Selected = i == selected, TextPixels = pixels, IconPixels = 16, Bounds = new Rectangle(x, y + bodyOffset, width, 43) };
                b.Click += delegate { choose(index); }; Canvas.Controls.Add(b); Canvas.Nodes[b.Name] = b.Bounds;
                x += width + 5;
            }
            Line(0, y + 43, layoutWidth); Canvas.Nodes["tabs"] = new Rectangle(0, y + bodyOffset, layoutWidth, 44);
        }
        private void TwoLineText(string title, string note, int x, int y, int width, float titleSize = 12, float noteSize = 10.5f, Color? titleColor = null, Color? noteColor = null, bool titleBold = true, bool wrapNote = false)
        {
            Text(title, x, y, width, Sketch20Style.TitleLineHeight, titleSize, titleColor ?? Sketch20Style.Ink, titleBold);
            Text(note, x, y + Sketch20Style.TitleLineHeight + Sketch20Style.TextStackGap, width, Sketch20Style.BodyLineHeight, noteSize, noteColor ?? Sketch20Style.Muted, false, wrapNote);
        }
        private void InfoRow(string name, string value, int x, int y, int width, int labelWidth = 145, int height = 45, bool last = false, bool mono = false, string kind = null, string note = null)
        {
            int lineH = Sketch20Style.TitleLineHeight;
            int lineY = y + Math.Max(0, (height - lineH) / 2);
            Text(name, x, lineY, labelWidth, lineH, 12, Sketch20Style.Muted);
            if (kind == null)
            {
                Text(value, x + labelWidth + Sketch20Style.RowPaddingX, lineY, width - labelWidth - Sketch20Style.RowPaddingX, lineH, 12, mono ? Sketch20Style.Muted : Sketch20Style.Ink, false, false, false, mono);
            }
            else
            {
                int badgeY = y + Math.Max(0, (height - 25) / 2);
                Badge(value, x + labelWidth + Sketch20Style.RowPaddingX, badgeY, kind);
                if (note != null) Text(note, x + labelWidth + Sketch20Style.RowPaddingX, lineY + lineH + Sketch20Style.TextStackGap, width - labelWidth - Sketch20Style.RowPaddingX, Sketch20Style.BodyLineHeight, 11, Sketch20Style.Muted);
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
            string[] titles = { "Máy tính của bạn đang được bảo vệ", "Bảo vệ thời gian thực đang tắt", "Đang kiểm tra máy tính", Model.ThreatCount + " tệp cần được xử lý" };
            string[] notes = { "Tất cả các tính năng bảo vệ đang hoạt động bình thường.\nChúng tôi luôn theo dõi và giữ cho thiết bị của bạn an toàn.", "Thiết bị của bạn hiện không được bảo vệ theo thời gian thực.\nHãy bật tính năng này để ngăn chặn các mối đe dọa ngay lập tức.", "Vui lòng không tắt máy hoặc đóng ứng dụng trong quá trình quét.", "Chúng tôi đã phát hiện các tệp có dấu hiệu là phần mềm độc hại.\nVui lòng xử lý ngay để giữ an toàn cho thiết bị của bạn." };
            Color[] start = { ColorTranslator.FromHtml("#edf7ff"), ColorTranslator.FromHtml("#fff9e8"), ColorTranslator.FromHtml("#eaf5ff"), ColorTranslator.FromHtml("#fff1f4") };
            Color[] end = { ColorTranslator.FromHtml("#d5eaff"), ColorTranslator.FromHtml("#fff0c7"), ColorTranslator.FromHtml("#d7efff"), ColorTranslator.FromHtml("#ffdee7") };
            const int heroY = 81;
            bool compact = layoutWidth < 850;
            int statusW = compact ? 178 : 215, statusX = layoutWidth - statusW - 22;
            int artW = state == 2 ? (int)(layoutWidth * .32) : compact ? 180 : 246, artX = state == 2 ? layoutWidth - artW - 19 : statusX - artW - 8;
            int copyW = Math.Max(290, artX - 26);
            int titleW = Math.Min(statusX - 38, copyW + 100);
            float titlePixels = compact ? 21 : 23;
            int titleH = Math.Max(38, TextRenderer.MeasureText(titles[state], Sketch20Style.Font(titlePixels, true), new Size(titleW, 32767), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Height + 2);
            int noteY = 25 + titleH + 5;
            string note = notes[state];
            int noteH = TextRenderer.MeasureText(note, Sketch20Style.Font(12 * ReferenceTextScale), new Size(copyW, 32767), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Height + 3;
            int heroH = state == 2 ? Math.Max(260, noteY + noteH + 166) : Math.Max(220, noteY + noteH + 70);
            GradientCard("hero", 0, heroY, layoutWidth, heroH, start[state], end[state], Sketch20Style.Border, 14);
            var heroBounds = new Rectangle(0, heroY, layoutWidth, heroH);
            Canvas.Drawing.Add(g => Sketch20Artwork.Waves(g, heroBounds, state == 1 ? Sketch20Style.Amber : state == 3 ? Sketch20Style.Red : Sketch20Style.Blue));
            Artwork("hero-art", artX, heroY + 13, artW, heroH - 20, state == 1 ? "reference-off" : state == 2 ? "reference-scan" : state == 3 ? "reference-risk" : "reference-safe");
            if (state == 1 || state == 3)
            {
                string prefix = state == 1 ? "Bảo vệ thời gian thực " : Model.ThreatCount + " ";
                string suffix = state == 1 ? "đang tắt" : "tệp cần được xử lý";
                int prefixW = Sketch20Style.Measure(prefix, titlePixels, true);
                Text(prefix.TrimEnd(), 26, heroY + 25, prefixW, titleH, titlePixels, state == 3 ? Sketch20Style.Red : Sketch20Style.Ink, true);
                Text(suffix, 26 + prefixW, heroY + 25, Math.Max(1, titleW - prefixW), titleH, titlePixels, state == 1 ? Sketch20Style.Amber : Sketch20Style.Ink, true);
            }
            else Text(titles[state], 26, heroY + 25, titleW, titleH, titlePixels, Sketch20Style.Ink, true, true);
            Text(note, 26, heroY + noteY, copyW, noteH, 12, Sketch20Style.Muted, false, true);
            if (state != 2)
            {
            MiniStatus("Bảo vệ thời gian thực", Model.Running ? "Đang hoạt động" : "Đang tắt", statusX, heroY + 35, Model.Running ? Sketch20Style.Green : Sketch20Style.Red, statusW);
            MiniStatus("Cập nhật chữ ký", "Mới nhất", statusX, heroY + 93, Sketch20Style.Green, statusW);
            MiniStatus("Tường lửa", Model.Sample ? "Đang bật" : "Xem trong hệ thống", statusX, heroY + 151, Sketch20Style.Green, statusW);
            }
            int actionY = heroY + heroH - 57;
            if (state == 0)
            {
                Button("quick-scan", "Quét nhanh", 26, actionY, 147, () => Command("scan"), "search", true);
                Button("advanced-scan", "Quét nâng cao", 185, actionY, 165, () => Go("advanced"), "settings");
            }
            else if (state == 1)
            {
                Button("enable-protection", "Bật bảo vệ", 26, actionY, 141, () => Command("enable"), "play", true, false, 40).Accent = ColorTranslator.FromHtml("#ff9300");
                Button("quick-scan", "Quét ngay", 179, actionY, 133, () => Command("scan"), "scan");
            }
            else if (state == 3)
            {
                Button("show-details", "Xem chi tiết", 26, actionY, 149, () => OpenDetail(Model.Threats.Count > 0 ? Model.Threats[0].Index : 0, false), "file-search", true, true);
                Button("quick-scan", "Quét lại", 187, actionY, 125, () => Command("scan"), "refresh");
            }
            else
            {
                int scanY = Math.Max(121, noteY + noteH + 16);
                int progressW = Math.Max(195, copyW - 123), progress = Model.Progress < 0 ? 35 : Math.Min(100, Model.Progress);
                Card("progress-track", 26, heroY + scanY, progressW, 10, Sketch20Style.Track, Color.Transparent, 5);
                Card("progress-fill", 26, heroY + scanY, Math.Max(6, progressW * progress / 100), 10, Sketch20Style.Blue, Color.Transparent, 5);
                Text(Model.Progress < 0 ? "Đang quét" : progress + "%", 38 + progressW, heroY + scanY - 9, 108, 28, 12, Sketch20Style.Blue, true);
                Text(Model.ScannedFiles.ToString("N0") + " tệp · " + Model.ThreatCount + " mối đe dọa", 26, heroY + scanY + 15, copyW, 24, 10.5f, Sketch20Style.Muted);
                // Giữ block đường dẫn nằm trọn phía trên hàng nút; trước đây dòng path chạm/đè button.
                Text("Đang kiểm tra:", 26, heroY + scanY + 43, copyW, 19, 11, Sketch20Style.Ink);
                Text(Model.ScanPath, 26, heroY + scanY + 62, copyW, 17, 10.5f, Sketch20Style.Muted);
                Button("scan-results", "Xem kết quả", 26, actionY, 166, () => { if (Model.Sample) Command("finish"); else if (Model.Threats.Count > 0) OpenDetail(Model.Threats[0].Index, false); else Go("history"); }, "eye");
                Button("cancel-scan", "Hủy quét", 207, actionY, 134, () => Command("cancel"), "x", false, true);
            }
            int statsY = heroY + heroH + 14, gap = 14, statW = (layoutWidth - gap * 2) / 3;
            string[] labels = { "Mối đe dọa", "Tệp đã quét", state == 2 ? "Thời gian đã qua" : "Lần quét gần nhất" };
            string[] values = { Model.ThreatCount.ToString(), Model.ScannedFiles.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), state == 2 ? TimeSpan.FromSeconds(Model.ElapsedSeconds).ToString(@"hh\:mm\:ss") : Model.LastDate == "—" ? "Chưa có" : Model.LastDate };
            string[] hints = { Model.ThreatCount > 0 ? "Cần được xử lý ngay" : "Không phát hiện mối đe dọa", state == 2 ? "Trong phiên hiện tại" : "Tổng số tệp đã kiểm tra", state == 2 ? "Theo dõi tiến trình quét" : Model.Sample ? "Không phát hiện mối đe dọa nào" : Model.LastTime + " · " + Model.LastType };
            string[] icons = { "shield-alert", "file", "clock" };
            Color[] colors = { Sketch20Style.Red, Sketch20Style.Blue, ColorTranslator.FromHtml("#7740f6") };
            Color[] soft = { Sketch20Style.RedSoft, Sketch20Style.BlueSoft, ColorTranslator.FromHtml("#f1ebff") };
            for (int i = 0; i < 3; i++)
            {
                int x = i * (statW + gap), w = i == 2 ? layoutWidth - x : statW;
                Card("stat-" + i, x, statsY, w, 105);
                Artwork("stat-icon-" + i, x + 16, statsY + 17, 51, 56, "reference-icon-stat-" + (i == 0 ? "risk" : i == 1 ? "file" : "clock"));
                Text(labels[i], x + 82, statsY + 13, w - 110, 24, 11.5f, Sketch20Style.Ink);
                Text(values[i], x + 82, statsY + 37, w - 105, 33, i == 2 ? 17.5f : 22, i == 0 && state == 3 ? Sketch20Style.Red : Sketch20Style.Ink, true);
                Text(hints[i], x + 82, statsY + 75, w - 96, 21, 10.2f, Sketch20Style.Muted);
                Icon("chevron-right", x + w - 24, statsY + 17, 12, Sketch20Style.Muted);
            }
            int lowerY = statsY + 119;
            int lowerH = state == 3 ? 435 : state == 2
                ? Math.Max(164, Math.Min(360, viewport.Height - lowerY - 45))
                : Math.Max(342, Math.Min(452, viewport.Height - lowerY - 62));
            OverviewLower(lowerY, lowerH, state == 3);
            ContentHeight = state == 2 ? Math.Max(1, viewport.ClientSize.Height - 30) : lowerY + lowerH + (state == 3 ? 0 : 14);
        }
        private void MiniStatus(string title, string note, int x, int y, Color color, int width)
        {
            Canvas.Drawing.Add(g => { using (var b = new SolidBrush(color)) g.FillEllipse(b, x, y + 4, 19, 19); });
            Icon(note == "Đang tắt" ? "alert" : "check", x + 3, y + 7, 13, Color.White);
            Text(title, x + 29, y, width - 29, 26, 11.5f, Sketch20Style.Ink, true);
            Text(note, x + 29, y + 25, width - 29, 22, 10.5f, note == "Đang tắt" ? Sketch20Style.Red : Sketch20Style.Muted);
        }
        private void OverviewLower(int y, int height, bool threats)
        {
            const int gap = 14;
            if (threats)
            {
                const int threatH = 174;
                Card("threat-list", 0, y, layoutWidth, threatH);
                Text("Các mối đe dọa đã phát hiện (" + Model.Threats.Count + ")", 18, y + 9, layoutWidth - 210, 31, 16, Sketch20Style.Ink, true);
                Button("isolate-all", "Cách ly tất cả", layoutWidth - 166, y + 8, 148, () => Command("isolate"), "archive", true, true, 34);
                for (int i = 0; i < Math.Min(3, Model.Threats.Count); i++)
                {
                    var f = Model.Threats[i]; int top = y + 45 + i * 40, index = f.Index;
                    Card("threat-row-" + i, 12, top, layoutWidth - 24, 36, Color.White, Sketch20Style.Border, 8);
                    Card("threat-mini-icon-" + i, 20, top + 3, 30, 30, Sketch20Style.RedSoft, Color.Transparent, 7);
                    Icon("file-warning", 28, top + 10, 15, Sketch20Style.Red);
                    Text(f.Name, 60, top + 1, layoutWidth - 424, 18, 11, Sketch20Style.Ink, true);
                    Text(f.Path, 60, top + 18, layoutWidth - 424, 17, 9, Sketch20Style.Muted);
                    Badge(f.Severity, layoutWidth - 339, top + 6, Kind(f.Severity), 79);
                    Text(f.Time, layoutWidth - 249, top + 7, 70, 22, 9.5f, Sketch20Style.Muted, false, false, true);
                    Button("threat-detail-" + i, "Chi tiết", layoutWidth - 170, top + 3, 78, () => OpenDetail(index, false), "", false, false, 29, true);
                    Button("isolate-one-" + i, "Cách ly", layoutWidth - 84, top + 3, 68, () => { OpenDetail(index, false); Command("isolate-file"); }, "", false, false, 29);
                }
                y += threatH + gap; height -= threatH + gap;
            }
            int leftW = (int)(layoutWidth * .59), rightX = leftW + gap, rightW = layoutWidth - rightX;
            Card("activity-card", 0, y, leftW, height);
            Text("Hoạt động gần đây", 17, y + 10, leftW - 150, 30, 17, Sketch20Style.Ink, true);
            Button("open-history", "Xem tất cả  →", leftW - 126, y + 11, 109, () => Go("history"), "", false, false, 28, true);
            bool scanning = Model.Scanning;
            int maxActivityRows = scanning ? (height < 210 ? 1 : height < 300 ? 2 : 4) : threats ? 3 : 5;
            int actRows = Math.Min(maxActivityRows, Model.Activities.Count);
            int rowH = Math.Min(72, Math.Max(48, (height - 53) / Math.Max(1, actRows)));
            if (actRows == 0) Text("Chưa có hoạt động gần đây.", 20, y + 70, leftW - 40, 30, 13, Sketch20Style.Muted);
            for (int i = 0; i < actRows; i++)
            {
                var a = Model.Activities[i]; int top = y + 47 + i * rowH, center = top + (rowH - 6) / 2;
                Color fg = a[0] == "check" ? Sketch20Style.Green : a[0] == "archive" ? Sketch20Style.Red : Sketch20Style.Blue;
                Color bg = a[0] == "check" ? Sketch20Style.GreenSoft : a[0] == "archive" ? Sketch20Style.RedSoft : Sketch20Style.BlueSoft;
                Card("activity-row-" + i, 12, top, leftW - 24, rowH - 6, Color.White, Sketch20Style.Border, 9);
                Card("activity-icon-" + i, 22, center - 19, 38, 38, bg, Color.Transparent, 19);
                FilledIcon(a[0] == "check" ? "shield-check" : a[0], 32, center - 9, 18, fg);
                TwoLineText(a[1], a[2], 72, center - 22, leftW - 205, 11.6f, 9.7f);
                Text(a[3], leftW - 138, center - 22, 118, 25, 9.5f, Sketch20Style.Muted, false, false, true);
            }
            bool compactLower = threats;
            int deviceH = compactLower ? 114 : 164;
            Card("device-card", rightX, y, rightW, deviceH);
            Icon("monitor", rightX + 18, y + 18, 16, Sketch20Style.Ink);
            Text("Thiết bị này", rightX + 44, y + 9, rightW - 126, 32, 14.5f, Sketch20Style.Ink, true);
            Button("device-details", "Chi tiết →", rightX + rightW - 90, y + 10, 74, ShowDeviceInformation, "", false, false, 29, true);
            string[] labels = { "Tên thiết bị", "Hệ điều hành", "Bộ xử lý", "Bộ nhớ (RAM)" };
            string[] vals = Model.Sample ? new[] { "DESKTOP-7H3K9P2", "Windows 11 Pro 23H2", "Intel Core i7-12700H", "16 GB" } : DeviceInformation;
            for (int i = 0; i < 4; i++)
            {
                Icon(i == 0 ? "laptop" : i == 1 ? "windows" : i == 2 ? "cpu" : "ram", rightX + 18, y + (compactLower ? 44 : 51) + i * (compactLower ? 18 : 26), 14, Sketch20Style.Muted);
                Text(labels[i], rightX + 42, y + (compactLower ? 38 : 44) + i * (compactLower ? 18 : 26), 108, compactLower ? 22 : 26, 10.6f, Sketch20Style.Muted);
                Text(vals[i], rightX + 151, y + (compactLower ? 38 : 44) + i * (compactLower ? 18 : 26), rightW - 170, compactLower ? 22 : 26, 10.6f, Sketch20Style.Ink);
            }
            string[] names = { "Bảo vệ thời gian thực", "Cập nhật chữ ký", "Khu vực cách ly", "Bảo vệ web" };
            string[] hints = { Model.Running ? "Liên tục theo dõi và ngăn chặn mối đe dọa." : "Đang tắt. Thiết bị của bạn có thể gặp rủi ro.", Model.Signature, Model.Quarantine.Count + " tệp đang được cách ly", Model.KeyConfigured ? "Tra cứu tệp qua VirusTotal" : "Cấu hình API key để tra cứu tệp" };
            string[] icons = { "shield-check", "refresh", "bulb", "shield-check" };
            if (!Model.Running) { names[2] = "Tường lửa"; hints[2] = "Đang bảo vệ các kết nối mạng của bạn."; icons[2] = "shield-check"; names[3] = "Khuyến nghị"; hints[3] = "Bật bảo vệ thời gian thực để giữ an toàn."; icons[3] = "bulb"; }
            if (compactLower) { names[2] = "Khuyến nghị"; hints[2] = Model.Scanning ? "Một số cài đặt có thể được tối ưu." : "Hãy xử lý các mối đe dọa đã phát hiện."; icons[2] = "bulb"; }
            int quickCount = compactLower ? 3 : scanning ? (height < 300 ? 0 : 3) : 4;
            int quickH = quickCount == 0 ? 0 : compactLower ? 36 : Math.Max(42, Math.Min(55, (height - deviceH - 12) / Math.Max(1, quickCount) - 5));
            for (int i = 0; i < quickCount; i++)
            {
                int top = y + deviceH + (compactLower ? 8 : 10) + i * (quickH + 4);
                Card("device-action-" + i, rightX, top, rightW, quickH, i == 0 && !Model.Running ? Sketch20Style.RedSoft : Color.White);
                Icon(icons[i], rightX + 17, top + (quickH - 20) / 2, 20, i == 0 ? Model.Running ? Sketch20Style.Green : Sketch20Style.Red : i == 2 ? Sketch20Style.Amber : Sketch20Style.Blue);
                if (compactLower)
                {
                    Text(names[i], rightX + 49, top + 1, rightW - 112, 18, 10.5f, Sketch20Style.Ink, true);
                    Text(hints[i], rightX + 49, top + 18, rightW - 112, 16, 8.1f, Sketch20Style.Muted);
                }
                else TwoLineText(names[i], hints[i], rightX + 49, top + 4, rightW - 112, 11.2f, 9.1f);
                if (i == 0) Switch("rt", names[i], rightX + rightW - 53, top + 8);
                else if (i == 1) Button("device-update", "", rightX + rightW - 47, top + 7, 29, () => Command("update"), "chevron-right", false, false, 29, true);
                else if (i == 2) Button("device-quarantine", "", rightX + rightW - 47, top + 7, 29, () => { if (!Model.Running) OpenFirewallSettings(); else Go("quarantine"); }, "chevron-right", false, false, 29, true);
                else Button("device-web", "", rightX + rightW - 47, top + 7, 29, () => { if (!Model.Running) Command("enable"); else { SettingsTab = 2; Go("settings"); } }, "chevron-right", false, false, 29, true);
            }
        }
        private static readonly string[] DeviceInformation = ReadDeviceInformation();
        private static string[] ReadDeviceInformation()
        {
            string cpu = "Máy tính Windows", os = Environment.OSVersion.VersionString, memory = "—";
            try
            {
                cpu = Convert.ToString(Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", cpu)).Trim();
                os = Convert.ToString(Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", os));
                if (Environment.OSVersion.Version.Build >= 22000) os = os.Replace("Windows 10", "Windows 11");
                memory = Math.Round(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1073741824.0) + " GB";
            }
            catch { }
            return new[] { Environment.MachineName, os, cpu, memory };
        }
        private void ShowDeviceInformation()
        {
            var vals = Model.Sample ? new[] { "DESKTOP-7H3K9P2", "Windows 11 Pro 23H2", "Intel Core i7-12700H", "16 GB" } : DeviceInformation;
            MessageBox.Show("Thiết bị: " + vals[0] + "\nHệ điều hành: " + vals[1] + "\nBộ xử lý: " + vals[2] + "\nBộ nhớ: " + vals[3], "Thông tin thiết bị", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Activity(int y)
        {
            const int headerHeight = 56, rowStep = 72;
            int rows = Math.Min(Model.Activities.Count, 5);
            int height = headerHeight + (rows == 0 ? 100 : rows * rowStep + 8);
            Card("activity", 0, y, layoutWidth, height);
            Icon("history", 20, y + 19, 18);
            Text("Hoạt động gần đây", 48, y + 12, layoutWidth - 200, 31, 17, Sketch20Style.Ink, true);
            Button("open-history", "Xem lịch sử  ›", layoutWidth - 118, y + 12, 98, () => Go("history"), "", false, false, 31, true);
            if (rows == 0) Text("Chưa có hoạt động quét. Nhấn Quét ngay để bắt đầu.", 20, y + 82, layoutWidth - 40, 30, 13, Sketch20Style.Muted);
            for (int i = 0; i < rows; i++)
            {
                var row = Model.Activities[i]; int top = y + headerHeight + i * rowStep;
                Card("activity-row-" + i, 12, top, layoutWidth - 24, 64, Sketch20Style.Background, Color.Transparent, 7);
                Color fg = row[0] == "check" ? Sketch20Style.Green : row[0] == "archive" ? Sketch20Style.Amber : Sketch20Style.Blue;
                Icon(row[0], 26, top + 23, 18, fg);
                TwoLineText(row[1], row[2], 58, top + 8, layoutWidth - 175, 13, 11);
                Text(row[3], layoutWidth - 101, top + 20, 75, 24, 11, Sketch20Style.Muted, false, false, true);
            }
            Footer(y + height + 18, Model.Scanning ? "Đang quét: " + Model.ScanPath : Model.Status, "CSDL chữ ký: " + Model.Signature);
        }
        private void ThreatTable(int y)
        {
            int count = Model.Threats.Count, rows = Math.Min(count, 8), height = 72 + (rows == 0 ? 108 : rows * 76) + 64;
            Card("threat-list", 0, y, layoutWidth, height, Color.White, Sketch20Style.Border, 14);
            Card("threat-title-icon", 18, y + 16, 34, 34, Sketch20Style.RedSoft, Color.Transparent, 9);
            Icon("shield-alert", 26, y + 24, 18, Sketch20Style.Red);
            Text("Mối đe dọa được phát hiện", 64, y + 12, layoutWidth - 190, 28, 17, Sketch20Style.Ink, true);
            Text("Kiểm tra từng tệp trước khi cách ly hoặc xóa.", 64, y + 38, layoutWidth - 210, 20, 11, Sketch20Style.Muted);
            Badge(count + " tệp", layoutWidth - 82, y + 21, "risk", 64);
            if (rows == 0) Text("Không có mối đe dọa đang chờ xử lý.", 22, y + 92, layoutWidth - 44, 30, 13, Sketch20Style.Muted);
            for (int i = 0; i < rows; i++)
            {
                var f = Model.Threats[i]; int top = y + 70 + i * 76, index = f.Index;
                Card("threat-row-" + i, 16, top, layoutWidth - 32, 66, i % 2 == 0 ? Color.White : Sketch20Style.GridAlt, Sketch20Style.Border, 10);
                Card("threat-file-icon-" + i, 28, top + 15, 36, 36, Sketch20Style.RedSoft, Color.Transparent, 9);
                Icon("file-warning", 37, top + 24, 18, Sketch20Style.Red);
                TwoLineText(f.Name, f.Reason, 78, top + 8, layoutWidth - 340, 13, 11);
                Badge(f.Severity, layoutWidth - 230, top + 21, Kind(f.Severity), 86);
                Button("threat-detail-" + i, "Chi tiết", layoutWidth - 126, top + 17, 92, () => OpenDetail(index, false), "chevron-right", false, false, 34, true);
            }
            int foot = y + 70 + (rows == 0 ? 108 : rows * 76);
            Text("Ưu tiên cách ly trước, chỉ xóa khi đã xác minh.", 20, foot + 18, layoutWidth - 210, 24, 11, Sketch20Style.Muted);
            Button("isolate-all", "Cách ly tất cả", layoutWidth - 152, foot + 12, 132, () => Command("isolate"), "archive", true, false, 36);
            Footer(y + height + 18, Model.Status, "CSDL chữ ký: " + Model.Signature);
        }

        internal static readonly string[] FeatureKeys = { "rt", "restore", "usb", "download", "behavior", "startup", "aq", "vtq" };
        internal static readonly string[] FeatureNames = { "Bảo vệ thời gian thực", "Bảo vệ tệp", "Bảo vệ USB", "Bảo vệ tải xuống", "Giám sát hành vi", "Bảo vệ thư mục khởi động", "Tự động cách ly", "Bảo vệ web · VirusTotal" };
        internal static readonly string[] FeatureNotes = { "Giám sát tệp mới hoặc thay đổi trong máy.", "Kiểm tra lại tệp trước khi khôi phục từ cách ly.", "Quét ổ USB ngay khi được kết nối.", "Kiểm tra tệp tải về từ Internet.", "Theo dõi tiến trình có hành vi đáng ngờ.", "Kiểm tra tệp mới trong thư mục Startup.", "Chuyển tệp nguy hiểm vào khu vực cách ly.", "Tra cứu tệp nghi ngờ bằng mã băm." };
        private static string Kind(string severity)
        { return severity == "Cao" ? "risk" : severity == "Trung bình" ? "warn" : "info"; }

        private void TableHeader(int y, string[] names, float[] starts, int height = 39)
        {
            int paintY = y + bodyOffset;
            Canvas.Drawing.Add(g => { using (var b = new SolidBrush(Sketch20Style.GridHeader)) g.FillRectangle(b, 1, paintY, layoutWidth - 2, height); });
            Line(0, y, layoutWidth); Line(0, y + height, layoutWidth);
            for (int i = 0; i < names.Length; i++)
            {
                int x = (int)(layoutWidth * starts[i]), end = i == names.Length - 1 ? layoutWidth : (int)(layoutWidth * starts[i + 1]);
                Text(names[i], x + 14, y, end - x - 28, height, 12, Sketch20Style.Ink, true);
            }
        }
        private void History()
        {
            int rows=Model.History.Count, rowH=66, listY=178;
            int visible=Math.Min(rows,Math.Max(3,Math.Min(8,(viewport.Height-listY-154)/rowH)));
            historyOffset=Math.Max(0,Math.Min(historyOffset,Math.Max(0,rows-visible)));
            Card("history-filter-card",0,81,layoutWidth,82,Color.White,Sketch20Style.Border,14);
            int exportW=116, filterBtnW=104, gap=10;
            int available=layoutWidth-40-exportW-filterBtnW-gap*4;
            int dateW=(int)(available*0.27), typeW=available-dateW*2;
            Text("Từ ngày",20,87,dateW,20,10.5f,Sketch20Style.Muted,true);
            Text("Đến ngày",30+dateW,87,dateW,20,10.5f,Sketch20Style.Muted,true);
            Text("Loại quét",40+dateW*2,87,typeW,20,10.5f,Sketch20Style.Muted,true);
            DateBox("history-from",from,20,108,dateW,d=>from=d);
            DateBox("history-to",to,30+dateW,108,dateW,d=>to=d);
            string[] types={"Tất cả","Quét nhanh","Quét thư mục","Quét tệp","Quét toàn bộ","Quét tùy chỉnh","Thời gian thực"};
            SelectBox("history-filter",types,Math.Max(0,Array.IndexOf(types,scanFilter)),40+dateW*2,108,typeW,i=>scanFilter=types[i]);
            int filterX=50+dateW*2+typeW;
            Button("filter-history","Lọc",filterX,111,filterBtnW,()=>{ historyOffset=0; if(Actions.FilterHistory!=null) Actions.FilterHistory(from,to,scanFilter); if(Model.Sample) Model.History=Sketch20Samples.Create(3).History.Where(h=>scanFilter=="Tất cả"||h.Type==scanFilter).ToList(); RefreshModel(true); Render(); },"search",true,false,36);
            Button("export-csv","Xuất CSV",filterX+filterBtnW+gap,111,exportW,()=>Command("export"),"download",false,false,36);

            int listH=57+Math.Max(1,visible)*rowH+48;
            Card("history-list",0,listY,layoutWidth,listH,Color.White,Sketch20Style.Border,14);
            Text("Nhật ký hoạt động",18,listY+10,layoutWidth-280,29,17,Sketch20Style.Ink,true);
            Text("Tổng "+(Model.Sample?rows:Model.HistoryTotal)+" lần quét",layoutWidth-307,listY+13,125,24,10.5f,Sketch20Style.Muted,false,false,true);
            Card("history-sort",layoutWidth-164,listY+8,146,33,Sketch20Style.Background,Sketch20Style.Border,8);
            Text("Mới nhất trước",layoutWidth-153,listY+8,117,33,10.5f,Sketch20Style.Ink);
            Icon("chevron-down",layoutWidth-38,listY+18,13,Sketch20Style.Muted);
            if(rows==0) Text("Không có lịch sử quét phù hợp.",20,listY+74,layoutWidth-40,35,12.5f,Sketch20Style.Muted);
            string[] rowIcons={"monitor","folder","file","scan"};
            AddTableScroll("history-scroll",layoutWidth-17,listY+48,visible*rowH,rows,visible,historyOffset,value=>historyOffset=value);
            for(int i=0;i<visible;i++)
            {
                var row=Model.History[historyOffset+i]; int top=listY+48+i*rowH,index=row.Index;
                int visualH=rowH-7;
                int centerY=top+visualH/2;
                int twoLineTop=centerY-21;
                Card("history-row-"+i,16,top,layoutWidth-32,visualH,Color.White,Sketch20Style.Border,9);

                // Date/time block shares the exact same vertical center as the rest of the row.
                TwoLineText(row.Date,row.Time,28,centerY-(Sketch20Style.TitleLineHeight+Sketch20Style.TextStackGap+Sketch20Style.BodyLineHeight)/2,100,11.5f,9.8f);

                // Scan type and compact statistics share one visual center line.
                int statX=layoutWidth-551;
                int typeIconTop=centerY-16;
                Card("history-type-icon-"+i,132,typeIconTop,32,32,Sketch20Style.BlueSoft,Color.Transparent,8);
                FilledIcon(row.Type.IndexOf("toàn",StringComparison.OrdinalIgnoreCase)>=0?"house":row.Type.IndexOf("thư",StringComparison.OrdinalIgnoreCase)>=0?"file":row.Type.IndexOf("tệp",StringComparison.OrdinalIgnoreCase)>=0?"file":"bolt",138,centerY-10,20,Sketch20Style.Blue);
                int typeTextW=Math.Max(60,statX-189);
                TwoLineText(row.Type,!string.IsNullOrEmpty(row.Scope) ? row.Scope : row.Type.IndexOf("toàn",StringComparison.OrdinalIgnoreCase)>=0 ? "Quét tất cả ổ đĩa và thư mục" : "Các vị trí quan trọng trong hệ thống",177,centerY-(Sketch20Style.TitleLineHeight+Sketch20Style.TextStackGap+Sketch20Style.BodyLineHeight)/2,typeTextW,11.5f,9.5f);
                int statY=centerY-14;
                Card("history-files-icon-"+i,statX,statY,28,28,Sketch20Style.BlueSoft,Color.Transparent,8);
                FilledIcon("file",statX+7,statY+7,14,Sketch20Style.Blue);
                Text(row.Files+" tệp",statX+35,centerY-12,86,24,10.3f,Sketch20Style.Ink,true);
                int threatX=statX+125;
                Color threatInk=row.Risk?Sketch20Style.Red:Sketch20Style.Green;
                Color threatSoft=row.Risk?Sketch20Style.RedSoft:Sketch20Style.GreenSoft;
                Card("history-threat-icon-"+i,threatX,statY,28,28,threatSoft,Color.Transparent,8);
                FilledIcon(row.Risk?"bolt":"shield-check",threatX+7,statY+7,14,threatInk);
                Text(row.Threats+" mối đe dọa",threatX+35,centerY-12,91,24,9.7f,threatInk,true);
                int durationX=statX+255;
                Icon("clock",durationX,centerY-8,16,Sketch20Style.Muted);
                Text(row.Duration,durationX+22,centerY-12,70,24,9.8f,Sketch20Style.Ink);
                Badge(row.Result!=null&&row.Result.IndexOf("phát hiện",StringComparison.OrdinalIgnoreCase)>=0?"Phát hiện":row.Result,layoutWidth-200,centerY-12,row.Risk?"risk":"safe",76);
                var deleteHistory=Button("history-delete-"+i,"",layoutWidth-116,centerY-16,32,()=>{ if(Actions.Command!=null) Actions.Command("history-delete:"+index); RefreshModel(true); Render(); },"trash",false,true,32,true);
                deleteHistory.AccessibleName="Xóa nhật ký";
                Button("history-detail-"+i,"Xem",layoutWidth-76,centerY-16,52,()=>{ if(Actions.Command!=null) Actions.Command("history-detail:"+index); RefreshModel(true); if(Model.Sample&&Model.Detail!=null) Go("details"); },"",false,false,32,true);
            }
            int footer=listY+50+Math.Max(1,visible)*rowH;
            Text("Trang "+(Model.Sample?(rows==0?"0 / 0":"1 / 1"):Model.HistoryPage),20,footer+8,layoutWidth-180,24,10.5f,Sketch20Style.Muted);
            var prev=Button("history-prev","",layoutWidth-110,footer+3,34,()=>{historyOffset=0;Command("history-prev");},"chevron-left",false,false,32); prev.Enabled=Model.HistoryPrev;
            var next=Button("history-next","",layoutWidth-62,footer+3,34,()=>{historyOffset=0;Command("history-next");},"chevron-right",false,false,32); next.Enabled=Model.HistoryNext;
            ContentHeight=listY+listH+18;
        }

        private void Settings()
        {
            Tabs(81, new[] { "Chung", "Bảo vệ chủ động", "VirusTotal", "Dữ liệu & công cụ" }, SettingsTab, index => { SettingsTab = index; Render(); });
            if (SettingsTab == 2) { VirusTotalSettings(); return; }
            if (SettingsTab == 3) { DataToolsSettings(); return; }
            int rows = SettingsTab == 0 ? 4 : 5, cardH = 110 + rows * 86 + (SettingsTab == 0 ? 64 : 0) + 60;
            Card("settings-content", 0, 144, layoutWidth, cardH);
            Text(SettingsTab == 0 ? "Thiết lập chung" : "Bảo vệ chủ động", 22, 163, layoutWidth - 44, 33, 20, Sketch20Style.Ink, true);
            Text(SettingsTab == 0 ? "Các tùy chọn giúp ứng dụng hoạt động hiệu quả và phù hợp với nhu cầu của bạn." : "Theo dõi và ngăn chặn các mối đe dọa theo thời gian thực.", 22, 198, layoutWidth - 44, 26, 12, Sketch20Style.Muted);
            string[] keys = SettingsTab == 0 ? new[] { "autostart", "update", "alerts", "samples" } : FeatureKeys.Skip(1).Take(5).ToArray();
            string[] names = SettingsTab == 0 ? new[] { "Khởi động cùng Windows", "Tự động cập nhật chữ ký", "Hiển thị thông báo", "Gửi mẫu tệp nghi ngờ" } : FeatureNames.Skip(1).Take(5).ToArray();
            string[] notes = SettingsTab == 0 ? new[] { "Tự động mở ứng dụng khi bật máy tính để luôn được bảo vệ.", "Kiểm tra các bản cập nhật chữ ký khi khởi động ứng dụng.", "Nhận thông báo về mối đe dọa, kết quả quét và thông tin quan trọng.", "Cho phép gửi mẫu tệp nghi ngờ để hỗ trợ phân tích." } : FeatureNotes.Skip(1).Take(5).ToArray();
            string[] icons = SettingsTab == 0 ? new[] { "monitor", "refresh", "bell", "globe" } : new[] { "file", "usb", "download", "activity", "folder" };
            Color[] colors = { Sketch20Style.Blue, Sketch20Style.Green, Sketch20Style.Red, Sketch20Style.Blue, Sketch20Style.Amber };
            Color[] soft = { Sketch20Style.BlueSoft, Sketch20Style.GreenSoft, Sketch20Style.RedSoft, Sketch20Style.BlueSoft, Sketch20Style.AmberSoft };
            for (int i = 0; i < rows; i++)
            {
                int y = 234 + i * 86;
                Card("setting-row-" + i, 17, y, layoutWidth - 34, 76);
                Card("setting-icon-" + i, 32, y + 13, 49, 50, soft[i], Color.Transparent, 12);
                Icon(icons[i], 44, y + 26, 25, colors[i]);
                TwoLineText(names[i], notes[i], 99, y + Sketch20Style.RowPaddingY, layoutWidth - 187, 13.5f, 11.5f, Sketch20Style.Ink, Sketch20Style.Muted, true, true);
                Switch(keys[i], names[i], layoutWidth - 65, y + 25);
            }
            if (SettingsTab == 0) Callout(17, 234 + rows * 86 + 2, layoutWidth - 34, "Các tùy chọn được lưu trên máy và đồng bộ với màn Bảo vệ.", false, 52);
            int actionY = 144 + cardH - 60;
            Button("restore-defaults", "Khôi phục mặc định", layoutWidth - 400, actionY, 214, () => Command("defaults"), "undo");
            Button("save-settings", "Lưu thiết lập", layoutWidth - 174, actionY, 152, () => Command("save-settings"), "save", true);
            ContentHeight = 144 + cardH + 16;
        }
        private void DataToolsSettings()
        {
            GradientCard("data-hero", 0, 144, layoutWidth, 164, ColorTranslator.FromHtml("#e9f5ff"), ColorTranslator.FromHtml("#cde8ff"), Sketch20Style.Border);
            Text("Dữ liệu & công cụ", 31, 173, layoutWidth - 290, 44, 27, Sketch20Style.Ink, true);
            Text("Quản lý dữ liệu ứng dụng, bộ nhớ đệm, mẫu tệp và các công cụ hỗ trợ khác.", 31, 226, layoutWidth - 310, 55, 12, Sketch20Style.Muted, false, true);
            Artwork("data-art", layoutWidth - 246, 147, 213, 154, "data");
            Card("data-info", 0, 326, layoutWidth, 251);
            Icon("info", 23, 346, 24); Text("Thông tin ứng dụng", 60, 338, layoutWidth - 90, 38, 17, Sketch20Style.Ink, true);
            InfoRow("Phiên bản ứng dụng", Model.Version, 22, 384, layoutWidth - 44, 205, 43);
            InfoRow("CSDL chữ ký", Model.Signature, 22, 427, layoutWidth - 44, 205, 43);
            InfoRow("Đang cách ly", Model.Quarantine.Count + " tệp", 22, 470, layoutWidth - 44, 205, 43);
            InfoRow("Thư mục dữ liệu", Model.DataFolder, 22, 513, layoutWidth - 88, 205, 43, true);
            Button("copy-data-folder", "", layoutWidth - 55, 520, 31, () => { if (!string.IsNullOrWhiteSpace(Model.DataFolder)) Clipboard.SetText(Model.DataFolder); }, "files", false, false, 31);
            Card("tools-card", 0, 594, layoutWidth, 173);
            Icon("settings", 23, 616, 24); Text("Công cụ & thao tác dữ liệu", 60, 604, layoutWidth - 90, 34, 17, Sketch20Style.Ink, true);
            Text("Quản lý dữ liệu, giải phóng bộ nhớ đệm và kiểm tra khả năng phát hiện.", 60, 640, layoutWidth - 90, 24, 11.3f, Sketch20Style.Muted);
            int gap = 12, w = (layoutWidth - 44 - gap * 2) / 3;
            Button("open-data", "Mở thư mục dữ liệu", 22, 681, w, () => Command("open-data"), "folder-open", false, false, 57);
            Button("clear-cache", "Xóa cache quét", 22 + w + gap, 681, w, () => Command("clear-cache"), "trash", false, false, 57);
            Button("create-samples", "Tạo tệp mẫu", 22 + (w + gap) * 2, 681, w, () => Command("samples"), "file-plus", false, false, 57);
            ContentHeight = 785;
        }
        private void VirusTotalSettings()
        {
            GradientCard("vt-hero", 0, 144, layoutWidth, 164, ColorTranslator.FromHtml("#e9f5ff"), ColorTranslator.FromHtml("#cde8ff"), Sketch20Style.Border);
            Text("VirusTotal", 31, 175, layoutWidth - 300, 44, 28, Sketch20Style.Ink, true);
            Text("Tích hợp VirusTotal giúp kiểm tra tệp và đường dẫn đáng ngờ dựa trên dữ liệu cộng đồng bảo mật toàn cầu.", 31, 226, layoutWidth - 310, 59, 12, Sketch20Style.Muted, false, true);
            Artwork("vt-art", layoutWidth - 240, 146, 207, 155, "vt");
            Card("settings-content", 0, 326, layoutWidth, 290);
            Card("vt-key-icon", 22, 346, 48, 50, Sketch20Style.BlueSoft, Color.Transparent, 12);
            Icon("key", 35, 359, 23);
            Text("API key VirusTotal", 89, 344, layoutWidth - 130, 34, 18, Sketch20Style.Ink, true);
            Text("Nhập API key để tra cứu tệp, URL và thông tin mối đe dọa.", 89, 384, layoutWidth - 122, 29, 12, Sketch20Style.Muted);
            Button("get-api-key", "Lấy API key từ tài khoản VirusTotal ↗", 85, 415, 327, () => System.Diagnostics.Process.Start("https://www.virustotal.com/gui/my-apikey"), "", false, false, 28, true);
            PasswordBox(89, 455, layoutWidth - 111);
            var save = Button("save-api-key", Model.KeyConfigured ? "Lưu API key mới" : "Lưu API key", 89, 511, 200, () => { if (Actions.SaveKey != null) Actions.SaveKey(typedKey); if (Model.Sample) Model.KeyConfigured = !string.IsNullOrWhiteSpace(typedKey); typedKey = ""; RefreshModel(true); Render(); }, "save", true, false, 44);
            save.Enabled = !string.IsNullOrWhiteSpace(typedKey);
            var entry = ((Sketch20Password)Canvas.Controls["api-key"]).Entry;
            entry.TextChanged += delegate { save.Enabled = !string.IsNullOrWhiteSpace(entry.Text); };
            var clear = Button("clear-api-key", "Gỡ key", 302, 511, 171, () => { typedKey = ""; Command("clear-key"); }, "trash", false, true, 44);
            clear.Enabled = Model.KeyConfigured;
            Text(Model.KeyConfigured ? "API key đã được lưu trên máy." : "API key được che khi nhập và lưu trên máy của bạn.", 89, 570, layoutWidth - 122, 25, 11, Sketch20Style.Muted);
            Card("vt-auto-query", 0, 634, layoutWidth, 159);
            Card("vt-auto-icon", 22, 653, 48, 50, Sketch20Style.BlueSoft, Color.Transparent, 12); Icon("refresh", 35, 666, 23);
            Text("Tra cứu tự động", 89, 650, layoutWidth - 178, 32, 17, Sketch20Style.Ink, true);
            Text("Tự động tra cứu mã băm của tệp nghi ngờ khi quét hoặc phát hiện theo thời gian thực.", 89, 688, layoutWidth - 178, 39, 11.8f, Sketch20Style.Muted, false, true);
            Switch("vtq", "Tra cứu tự động", layoutWidth - 58, 665);
            Callout(89, 737, layoutWidth - 111, "Tra cứu bằng mã băm giúp kiểm tra tệp và giữ nội dung tệp trên máy.", false, 41);
            ContentHeight = 810;
        }

        private void Advanced()
        {
            string[] names = { "Quét toàn bộ hệ thống", "Quét thư mục", "Quét tệp", "Quét tùy chỉnh" };
            string[] notes = { "Kiểm tra tất cả ổ đĩa, tệp và khu vực hệ thống.", "Chọn thư mục cụ thể để quét.", "Quét một hoặc nhiều tệp riêng lẻ.", "Chọn các vị trí và thư mục cần kiểm tra." };
            string[] icons = { "monitor", "folder", "file", "sliders" };
            int modeY = 81, modeH = 174;
            int[] modeIds = { 0, 1, 2, 3 };
            int gap = 12, cw = (layoutWidth - gap * 3) / 4;
            for (int i = 0; i < 4; i++)
            {
                int index = modeIds[i];
                var mode = new Sketch20Mode(names[i], notes[i], icons[i]) { Name = "mode-" + i, Bounds = new Rectangle(i * (cw + gap), modeY, cw, modeH), Checked = AdvancedMode == index, Compact = false, Centered = false };
                mode.TextScale = ReferenceTextScale;
                mode.Click += delegate { if (index < 0) { Command("scan"); Go("overview"); } else { AdvancedMode = index; Render(); } };
                Canvas.Controls.Add(mode); Canvas.Nodes[mode.Name] = mode.Bounds;
            }
            int mainY = modeY + modeH + 17;
            if (AdvancedMode == 0) { FullScanScope(mainY); return; }
            int fileRows = Math.Max(1, Math.Min(8, Model.Sample && files.Count == 0 ? 2 : files.Count));
            int extraRows = Math.Min(5, customFolders.Count);
            int mainH = AdvancedMode == 1 ? 316 : AdvancedMode == 2 ? 233 + fileRows * 65 : 495 + extraRows * 55;
            Card("scan-scope", 0, mainY, layoutWidth, mainH);
            Text(AdvancedMode == 3 ? "Chọn vị trí cần quét" : AdvancedMode == 1 ? "Quét thư mục" : "Quét tệp", 22, mainY + 15, layoutWidth - 44, 37, 21, Sketch20Style.Ink, true);
            if (AdvancedMode == 1)
            {
                Text("Thư mục cần quét", 22, mainY + 61, layoutWidth - 44, 27, 12.5f, Sketch20Style.Ink, true);
                Card("scan-path", 22, mainY + 99, layoutWidth - 225, 46);
                Icon("folder", 39, mainY + 112, 21, Sketch20Style.Amber);
                Text(Model.Sample && folders.Count == 0 ? @"C:\Users\Admin\Downloads" : folders.Count == 0 ? "Chưa chọn thư mục" : folders[0], 76, mainY + 105, layoutWidth - 298, 33, 12, Sketch20Style.Ink);
                Button("choose-scope", "Chọn thư mục", layoutWidth - 191, mainY + 99, 169, () => ChooseScope(true), "folder-open", false, false, 46);
                Check("include-subfolders", "Quét cả thư mục con", 22, mainY + 173, layoutWidth - 44, includeSubfolders, v => includeSubfolders = v);
                Text("Bao gồm các tệp và thư mục bên trong vị trí đã chọn.", 50, mainY + 207, layoutWidth - 74, 28, 11.5f, Sketch20Style.Muted);
            }
            else if (AdvancedMode == 2)
            {
                Text("Các tệp cần quét", 22, mainY + 60, layoutWidth - 200, 30, 12.5f, Sketch20Style.Ink, true);
                Button("choose-scope", "Chọn tệp", layoutWidth - 145, mainY + 55, 123, () => ChooseScope(false), "file-plus", false, false, 37);
                for (int i = 0; i < fileRows; i++)
                {
                    int index = i, top = mainY + 105 + i * 65;
                    string path = Model.Sample && files.Count == 0 ? i == 0 ? @"C:\Users\Admin\Downloads\invoice_update.exe" : @"D:\Projects\setup_script.ps1" : files.Count > i ? files[i] : "";
                    Card("file-pick-" + i, 22, top, layoutWidth - 44, 61);
                    Card("file-pick-icon-" + i, 34, top + 12, 36, 37, Sketch20Style.BlueSoft, Color.Transparent, 8);
                    Icon("file", 43, top + 20, 19);
                    TwoLineText(path.Length == 0 ? "Chưa chọn tệp" : Path.GetFileName(path), path, 85, top + 7, layoutWidth - 269, 12, 10.5f);
                    string size = Model.Sample ? i == 0 ? "12.5 MB" : "8.3 KB" : FileSize(path);
                    Text(size, layoutWidth - 158, top + 18, 75, 26, 11, Sketch20Style.Muted, false, false, true);
                    if (files.Count > i) Button("remove-file-" + i, "", layoutWidth - 70, top + 14, 31, () => { files.RemoveAt(index); Render(); }, "x", false, false, 32, true);
                }
                Text((Model.Sample && files.Count == 0 ? 2 : files.Count) + " tệp đã chọn", 22, mainY + 113 + fileRows * 65, layoutWidth - 44, 29, 12, Sketch20Style.Blue, true);
            }
            else
            {
                Text("Chọn các thư mục và vị trí bạn muốn kiểm tra.", 22, mainY + 58, layoutWidth - 44, 29, 12, Sketch20Style.Muted);
                string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] labels = { "Desktop", "Downloads", "Thư mục Temp", "Thư mục Startup" };
                string[] paths = Model.Sample ? new[] { @"C:\Users\Admin\Desktop", @"C:\Users\Admin\Downloads", @"C:\Users\Admin\AppData\Local\Temp", @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup" } : new[] { Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Path.Combine(user, "Downloads"), Path.GetTempPath(), Environment.GetFolderPath(Environment.SpecialFolder.Startup) };
                for (int i = 0; i < 4; i++)
                {
                    int index = i, top = mainY + 98 + i * 57;
                    Card("custom-row-" + i, 22, top, layoutWidth - 44, 53);
                    Check("custom-location-" + i, "", 34, top + 14, 24, customLocations[i], v => customLocations[index] = v);
                    Icon(i == 0 ? "monitor" : i == 1 ? "download" : "folder", 76, top + 15, 22, i < 2 ? Sketch20Style.Blue : Sketch20Style.Muted);
                    TwoLineText(labels[i], paths[i], 115, top + 4, layoutWidth - 150, 12, 10.6f);
                }
                for (int i = 0; i < extraRows; i++)
                {
                    int index = i, top = mainY + 326 + i * 55;
                    Card("extra-folder-" + i, 22, top, layoutWidth - 44, 50);
                    Icon("folder", 40, top + 15, 20);
                    Text(customFolders[i], 76, top + 8, layoutWidth - 157, 34, 12);
                    Button("remove-folder-" + i, "", layoutWidth - 67, top + 10, 30, () => { customFolders.RemoveAt(index); Render(); }, "x", false, false, 30, true);
                }
                Button("add-custom-folder", "Thêm thư mục", 22, mainY + 326 + extraRows * 55, layoutWidth - 44, () => ChooseScope(true), "folder-plus", false, false, 57);
                Text("Chọn thêm thư mục hoặc ổ đĩa khác để quét.", 22, mainY + 388 + extraRows * 55, layoutWidth - 44, 23, 11, Sketch20Style.Muted);
            }
            int actionY = mainY + mainH - 65;
            Callout(22, actionY - 1, layoutWidth - 216, AdvancedMode == 2 ? "Bạn có thể chọn nhiều tệp cùng lúc để kiểm tra." : "Quét chữ ký và phân tích các dấu hiệu đáng ngờ trong phạm vi đã chọn.", false, 46);
            var start = Button("start-advanced", "Bắt đầu quét", layoutWidth - 178, actionY, 156, StartAdvanced, "search", true, false, 45);
            start.Enabled = !Model.Scanning && (Model.Sample || AdvancedMode == 1 && folders.Count > 0 || AdvancedMode == 2 && files.Count > 0 || AdvancedMode == 3 && (customLocations.Any(b => b) || customFolders.Count > 0));
            ContentHeight = mainY + mainH + 17;
        }
        private static string FileSize(string path)
        {
            try { long bytes = new FileInfo(path).Length; return bytes >= 1048576 ? (bytes / 1048576.0).ToString("0.#") + " MB" : bytes >= 1024 ? (bytes / 1024.0).ToString("0.#") + " KB" : bytes + " B"; }
            catch { return "—"; }
        }
        private void FullScanScope(int mainY)
        {
            DriveInfo[] drives = Model.Sample ? new DriveInfo[0] : ScanEngine.GetFullScanDrives();
            string[] roots = Model.Sample ? new[] { @"C:\", @"D:\", @"E:\" } : drives.Select(d => d.Name).ToArray();
            int count = roots.Length, mainH = 311 + Math.Max(1, count) * 60;
            Card("scan-scope", 0, mainY, layoutWidth, mainH);
            Text("Ổ đĩa có thể quét", 22, mainY + 16, layoutWidth - 210, 34, 21, Sketch20Style.Ink, true);
            Text("Chọn các ổ đĩa bạn muốn kiểm tra.", 22, mainY + 53, layoutWidth - 210, 29, 12, Sketch20Style.Muted);
            Check("all-drives", "Chọn tất cả", layoutWidth - 146, mainY + 23, 124, count > 0 && roots.All(r => !excludedDrives.Contains(r)), on => { foreach (string root in roots) { if (on) excludedDrives.Remove(root); else excludedDrives.Add(root); } Render(); });
            for (int i = 0; i < count; i++)
            {
                int top = mainY + 97 + i * 60; string root = roots[i];
                Card("drive-row-" + i, 22, top, layoutWidth - 44, 54);
                Check("drive-pick-" + i, "", 34, top + 15, 24, !excludedDrives.Contains(root), on => { if (on) excludedDrives.Remove(root); else excludedDrives.Add(root); Render(); });
                Icon("hard-drive", 81, top + 15, 25, Sketch20Style.Blue);
                string label = Model.Sample ? i == 0 ? "Ổ đĩa hệ thống (C:)" : i == 1 ? "Ổ đĩa dữ liệu (D:)" : "Ổ đĩa công việc (E:)" : DriveLabel(drives[i]) + " (" + root.TrimEnd('\\') + ")";
                Text(label, 124, top + 11, layoutWidth - 438, 31, 12.6f, Sketch20Style.Ink);
                int bx = layoutWidth - 320, bw = 132;
                double ratio = .45 + .2 * (i % 2); string capacity = i == 0 ? "125.4 GB / 250 GB" : i == 1 ? "320 GB / 500 GB" : "282.1 GB / 1 TB";
                if (!Model.Sample)
                {
                    try { ratio = (drives[i].TotalSize - drives[i].TotalFreeSpace) / (double)Math.Max(1, drives[i].TotalSize); capacity = ((drives[i].TotalSize - drives[i].TotalFreeSpace) / 1073741824.0).ToString("0.#") + " / " + (drives[i].TotalSize / 1073741824.0).ToString("0.#") + " GB"; }
                    catch { capacity = "Sẵn sàng"; }
                }
                Card("drive-track-" + i, bx, top + 25, bw, 8, Sketch20Style.Track, Color.Transparent, 4);
                Card("drive-fill-" + i, bx, top + 25, Math.Max(4, (int)(bw * ratio)), 8, Sketch20Style.Blue, Color.Transparent, 4);
                Text(capacity, layoutWidth - 179, top + 14, 147, 28, 10.3f, Sketch20Style.Muted, false, false, true);
            }
            if (count == 0) Text("Không có ổ đĩa sẵn sàng để quét.", 22, mainY + 115, layoutWidth - 44, 35, 13, Sketch20Style.Muted);
            int scopeY = mainY + 111 + Math.Max(1, count) * 60;
            Text("Phạm vi quét", 22, scopeY, layoutWidth - 44, 31, 17, Sketch20Style.Ink, true);
            string[] scopes = { "Kiểm tra tệp và thư mục", "Đối chiếu chữ ký virus", "Phân tích dấu hiệu đáng ngờ", "Quét toàn bộ thư mục con", "Phát hiện phần mở rộng kép", "Theo dõi tiến trình quét" };
            for (int i = 0; i < 6; i++) { Icon("circle-check", 24 + i % 2 * (layoutWidth / 2), scopeY + 42 + i / 2 * 29, 16); Text(scopes[i], 50 + i % 2 * (layoutWidth / 2), scopeY + 36 + i / 2 * 29, layoutWidth / 2 - 77, 28, 11.8f, Sketch20Style.Muted); }
            var start = Button("start-advanced", "Quét các ổ đĩa đã chọn", 22, mainY + mainH - 64, layoutWidth - 44, StartAdvanced, "search", true, false, 45);
            start.Enabled = !Model.Scanning && roots.Any(r => !excludedDrives.Contains(r));
            ContentHeight = mainY + mainH + 18;
        }

        private static string DriveLabel(DriveInfo drive)
        {
            if (drive.DriveType == DriveType.Removable) return "USB / Ổ di động";
            if (drive.DriveType == DriveType.Network) return "Ổ mạng";
            if (drive.DriveType == DriveType.CDRom) return "Đĩa có dữ liệu";
            if (drive.DriveType == DriveType.Ram) return "Ổ RAM";
            string system = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            return string.Equals(drive.Name, system, StringComparison.OrdinalIgnoreCase) ? "Ổ hệ thống Windows" : "Ổ dữ liệu";
        }
        private static string FullDriveText()
        {
            return "Ổ đĩa " + string.Join(" · ", ScanEngine.GetFullScanDrives().Select(d => d.Name)) + " · Bao gồm thư mục con";
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
            int requestedMode = AdvancedMode;
            if (AdvancedMode == 0 && !Model.Sample)
            {
                var drives = ScanEngine.GetFullScanDrives();
                targets = drives.Where(d => !excludedDrives.Contains(d.Name)).Select(d => d.Name).ToList();
                if (targets.Count == 0) return;
                if (targets.Count < drives.Length) requestedMode = 3;
            }
            if (Actions.StartAdvanced != null) Actions.StartAdvanced(requestedMode, targets, includeSubfolders);
            if (Model.Sample) Command("scan");
            else
            {
                // ExactStartAdvanced đặt engine vào trạng thái quét đồng bộ trước await.
                // Phản ánh ngay vào model để frame đầu của Tổng quan đã là màn quét,
                // tránh nháy màn thường/scrollbar trước tick model kế tiếp.
                Model.Scanning = true;
                Model.Progress = -1;
                lastScanRenderUtc = DateTime.UtcNow;
            }
            Go("overview");
        }

        private void OpenDetail(int index, bool quarantine)
        {
            if (Actions.OpenFile != null) Actions.OpenFile(index, quarantine);
            DetailReturnPage = quarantine ? "quarantine" : "overview";
            if (Model.Sample) Model.Detail = (quarantine ? Model.Quarantine : Model.Threats).FirstOrDefault(f => f.Index == index) ?? Model.Quarantine.FirstOrDefault();
            else RefreshModel(true);
            DetailTab = 0; Go("details");
        }
        private void DetailInfo(Sketch20File f, int height)
        {
            int leftW = (layoutWidth - 14) / 2, rightX = leftW + 14, rightW = layoutWidth - rightX, mainY = detailBodyY;
            Card("detail-info", 0, mainY, leftW, height);
            Card("detail-risk", rightX, mainY, rightW, height);
            Text("Thông tin tệp", 18, mainY + 12, leftW - 36, 34, 18, Sketch20Style.Ink, true);
            Text("Đánh giá rủi ro", rightX + 18, mainY + 12, rightW - 135, 34, 18, Sketch20Style.Ink, true);
            Badge(f == null ? "Chưa xác định" : f.Severity, rightX + rightW - 112, mainY + 17, f == null ? "info" : Kind(f.Severity), 93);
            string[] labels = { "Tên tệp", "Đường dẫn đầy đủ", "Kích thước", "Loại tệp", "Mô tả", "Nhà phát hành", "Phiên bản", "Ngày tạo", "Nguồn phát hiện", "Chữ ký số", "Băm SHA-256" };
            string[] values = { f == null ? "—" : f.Name, f == null ? "—" : f.Path, f == null ? "—" : f.Size, f == null ? "—" : f.FileType, "Chưa có thông tin", "Chưa xác minh", "Chưa có thông tin", f == null ? "—" : f.Created, f == null ? "—" : f.Source, "Chưa xác minh", f == null ? "—" : f.Sha256 };
            for (int i = 0; i < labels.Length; i++)
            {
                int yy = mainY + 58 + i * 35;
                InfoRow(labels[i], values[i], 18, yy, leftW - 36, leftW < 425 ? 107 : 135, 35, i == labels.Length - 1, i == 10);
            }
            int bannerY = mainY + 63;
            Card("risk-card", rightX + 16, bannerY, rightW - 32, 114, Sketch20Style.RedSoft, Color.Transparent, 11);
            Card("risk-icon", rightX + 30, bannerY + 20, 42, 43, Color.White, Color.Transparent, 10); Icon("alert", rightX + 41, bannerY + 31, 21, Sketch20Style.Red);
            Text(f == null ? "Chưa có đánh giá" : "Tệp có dấu hiệu đáng ngờ", rightX + 84, bannerY + 10, rightW - 116, 32, 13.5f, Sketch20Style.Red, true);
            Text(f == null ? "Chọn tệp để xem kết quả." : "Đối chiếu các chỉ dấu, nguồn gốc và kết quả phân tích trước khi sử dụng tệp.", rightX + 84, bannerY + 48, rightW - 116, 54, 11.5f, Sketch20Style.Muted, false, true);
            int yyRisk = bannerY + 130;
            InfoRow("Mức độ rủi ro", f == null ? "—" : f.Severity, rightX + 18, yyRisk, rightW - 36, 133, 39);
            InfoRow("Phát hiện bởi", Model.VtEngineCount.HasValue ? Model.VtDetections + "/" + Model.VtEngineCount + " công cụ" : "Chưa tra cứu", rightX + 18, yyRisk + 39, rightW - 36, 133, 39);
            InfoRow("Loại mối đe dọa", f == null ? "—" : f.Reason, rightX + 18, yyRisk + 78, rightW - 36, 133, 39);
            InfoRow("Lần quét gần nhất", f == null ? "—" : f.Date + " " + f.Time, rightX + 18, yyRisk + 117, rightW - 36, 133, 39, true);
            int noteY = mainY + height - 97;
            Card("recommendation", rightX + 16, noteY, rightW - 32, 78, Sketch20Style.AmberSoft, Color.Transparent, 10);
            Icon("info", rightX + 31, noteY + 17, 19, Sketch20Style.Amber);
            Text("Khuyến nghị", rightX + 63, noteY + 8, rightW - 95, 27, 12.5f, Sketch20Style.Ink, true);
            Text(f != null && f.Resolved ? "Giữ tệp trong vùng cách ly nếu chưa chắc chắn về nguồn gốc." : "Nên cách ly tệp trước khi tiếp tục sử dụng.", rightX + 31, noteY + 39, rightW - 63, 34, 11, Sketch20Style.Muted, false, true);
        }
        private List<string[]> FilteredEngines()
        { return Model.Engines.Where(e => engineSearch.Length == 0 || string.Join(" ", e).IndexOf(engineSearch, StringComparison.OrdinalIgnoreCase) >= 0).Take(100).ToList(); }
        private List<string> FilteredStrings()
        { return Model.Strings.Where(v => stringSearch.Length == 0 || v.IndexOf(stringSearch, StringComparison.OrdinalIgnoreCase) >= 0).Take(100).ToList(); }
        private int VirusTotalHeight() { return 323 + Math.Min(Math.Max(1, FilteredEngines().Count), Math.Max(4, (viewport.Height - detailBodyY - 420) / 33)) * 33 + 60; }
        private void DetailVirusTotal(Sketch20File f, int height)
        {
            int mainY = detailBodyY;
            Card("detail-virustotal", 0, mainY, layoutWidth, height);
            bool hasCounts = Model.VtDetections.HasValue && Model.VtEngineCount.HasValue, hasReport = hasCounts && Model.VtEngineCount.Value > 0, needsKey = !Model.Sample && !Model.KeyConfigured;
            int bad = hasCounts ? Model.VtDetections.Value : 0, total = hasCounts ? Model.VtEngineCount.Value : 0, clean = Math.Max(0, total - bad);
            Text("Kết quả phân tích trên VirusTotal", 22, mainY + 11, layoutWidth - (layoutWidth >= 850 ? 435 : 224), 34, 18, Sketch20Style.Ink, true);
            if (layoutWidth >= 850 && hasReport) Text("Cập nhật: " + Model.VtTime, layoutWidth - 394, mainY + 19, 207, 22, 9.5f, Sketch20Style.Muted, false, false, true);
            var query = Button("query-virustotal", needsKey ? "Cấu hình API key" : hasReport ? "Tra cứu lại" : "Tra cứu VirusTotal", layoutWidth - 176, mainY + 11, 154, () => { if (needsKey) { SettingsTab = 2; Go("settings"); } else Command("vt-query"); }, "refresh", true, false, 37);
            query.Enabled = needsKey || f != null;
            Card("vt-alert", 22, mainY + 62, layoutWidth - 44, 82, hasReport && bad > 0 ? Sketch20Style.RedSoft : Sketch20Style.BlueSoft, Color.Transparent, 10);
            Icon(hasReport && bad > 0 ? "sigma" : "info", 40, mainY + 84, 33, hasReport && bad > 0 ? Sketch20Style.Red : Sketch20Style.Blue);
            string summary = f == null ? "Chọn một tệp để xem kết quả phân tích." : hasReport ? bad + " trong " + total + " công cụ đánh dấu tệp có dấu hiệu độc hại" : needsKey ? "Thêm API key trong Cài đặt → VirusTotal để bắt đầu tra cứu." : "Chưa có kết quả. Hãy bấm Tra cứu VirusTotal.";
            Text(summary, 92, mainY + 69, layoutWidth - 136, 33, 13.5f, hasReport && bad > 0 ? Sketch20Style.Red : Sketch20Style.Ink, true);
            Text(hasReport ? "Tệp được phân tích trên nền tảng VirusTotal. Đối chiếu kết quả từ các công cụ bảo mật." : Model.VtSummary, 92, mainY + 107, layoutWidth - 136, 32, 11, Sketch20Style.Muted, false, true);
            string[] labels = { "Phát hiện độc hại", "Không phát hiện", "Nghi ngờ", "Tổng số công cụ" }, vals = { hasCounts ? bad.ToString() : "—", hasCounts ? clean.ToString() : "—", "—", hasCounts ? total.ToString() : "—" }, icons = { "shield-alert", "circle-check", "info", "list-checks" };
            Color[] colors = { Sketch20Style.Red, Sketch20Style.Green, Sketch20Style.Muted, Sketch20Style.Blue };
            int gap = 11, cw = (layoutWidth - 44 - gap * 3) / 4;
            for (int i = 0; i < 4; i++)
            {
                int x = 22 + i * (cw + gap), yy = mainY + 158;
                Card("vt-stat-" + i, x, yy, cw, 76);
                Icon(icons[i], x + 15, yy + 23, 25, colors[i]);
                Text(i == 0 && hasCounts ? vals[i] + " / " + total : i == 2 && Model.Sample ? "0" : vals[i], x + 52, yy + 8, cw - 63, 35, 22, colors[i], true);
                Text(labels[i], x + 52, yy + 46, cw - 60, 22, 10, Sketch20Style.Muted);
            }
            int listY = mainY + 246;
            Text("Kết quả từ các công cụ bảo mật", 22, listY, layoutWidth - 288, 33, 15, Sketch20Style.Ink, true);
            SearchBox("engine-search", engineSearch, layoutWidth - 252, listY + 1, 230, value => { engineSearch = value; engineOffset = 0; });
            int head = listY + 46;
            Card("engine-header", 22, head, layoutWidth - 44, 31, Sketch20Style.GridHeader, Color.Transparent, 7);
            int c1 = 34, c2 = (int)(layoutWidth * .26), c3 = (int)(layoutWidth * .52), c4 = (int)(layoutWidth * .81);
            Text("Công cụ", c1, head, c2 - c1 - 10, 31, 10.5f, Sketch20Style.Muted, true);
            Text("Kết quả", c2, head, c3 - c2 - 12, 31, 10.5f, Sketch20Style.Muted, true);
            Text("Loại phát hiện", c3, head, c4 - c3 - 12, 31, 10.5f, Sketch20Style.Muted, true);
            Text("Cập nhật", c4, head, layoutWidth - c4 - 32, 31, 10.5f, Sketch20Style.Muted, true);
            var all = FilteredEngines();
            int page = Math.Max(4, (height - 383) / 33); engineOffset = Math.Max(0, Math.Min(engineOffset, Math.Max(0, all.Count - page)));
            var rows = all.Skip(engineOffset).Take(page).ToList();
            AddTableScroll("engine-scroll", layoutWidth - 37, head + 31, page * 33, all.Count, page, engineOffset, value => engineOffset = value);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; int yy = head + 31 + i * 33;
                string result = row.Length > 1 ? row[1] : "Chưa có kết quả";
                bool good = result.IndexOf("undetected", StringComparison.OrdinalIgnoreCase) >= 0 || result.IndexOf("không phát hiện", StringComparison.OrdinalIgnoreCase) >= 0;
                Card("vt-engine-row-" + i, 22, yy, layoutWidth - 44, 32, i % 2 == 0 ? Color.White : Sketch20Style.GridAlt, Sketch20Style.Border, 0);
                Text(row.Length == 0 ? "—" : row[0], c1, yy, c2 - c1 - 10, 32, 11.5f, Sketch20Style.Ink, true);
                Icon(good ? "circle-check" : "alert", c2, yy + 9, 14, good ? Sketch20Style.Green : Sketch20Style.Red);
                Text(good ? "Undetected" : "Malicious", c2 + 22, yy, c3 - c2 - 34, 32, 11, good ? Sketch20Style.Green : Sketch20Style.Red);
                Text(good ? "—" : result, c3, yy, c4 - c3 - 12, 32, 10.5f, Sketch20Style.Ink);
                Text((Model.VtTime ?? "—").Split(new[] { '·', ',' })[0].Trim(), c4, yy, layoutWidth - c4 - 55, 32, 10, Sketch20Style.Muted);
            }
            if (rows.Count == 0) Text("Chưa có kết quả phù hợp để hiển thị.", 34, head + 39, layoutWidth - 68, 35, 12, Sketch20Style.Muted);
            var report = Button("open-virustotal-report", hasReport ? "Xem tất cả " + total + " kết quả →" : "Xem báo cáo trên VirusTotal →", 22, mainY + height - 42, 255,
                () => System.Diagnostics.Process.Start("https://www.virustotal.com/gui/file/" + f.Sha256), "", false, false, 30, true);
            report.Enabled = hasReport && f != null && !string.IsNullOrEmpty(f.Sha256) && f.Sha256.Length == 64 && f.Sha256.All(Uri.IsHexDigit);
        }

        private void DetailBehavior(Sketch20File f, int height)
        {
            Card("detail-behavior",0,detailBodyY,layoutWidth,height,Color.White,Sketch20Style.Border,14); Icon("activity",22,detailBodyY+25,27); Text("Chỉ số hành vi",70,detailBodyY+10,layoutWidth-92,40,20,Sketch20Style.Ink,true); Text("Phân tích các hành vi quan sát được trong quá trình thực thi tệp.",70,detailBodyY+49,layoutWidth-92,27,12,Sketch20Style.Muted);
            string[] icons={"settings","power","activity","globe","alert"}; Color[] colors={Sketch20Style.Blue,Sketch20Style.Green,ColorTranslator.FromHtml("#7c3aed"),Sketch20Style.Amber,Sketch20Style.Red}; Color[] soft={Sketch20Style.BlueSoft,Sketch20Style.GreenSoft,ColorTranslator.FromHtml("#f0eaff"),Sketch20Style.AmberSoft,Sketch20Style.RedSoft};
            int count=Model.Behavior.Count;
            if(count==0){Text("Chưa có tệp được chọn để phân tích.",22,detailBodyY+83,layoutWidth-44,40,12,Sketch20Style.Muted);return;}
            for(int i=0;i<count;i++)
            {
                var row=Model.Behavior[i];int y=detailBodyY+90+i*84; Card("behavior-row-"+i,18,y,layoutWidth-36,75,Sketch20Style.Background,Sketch20Style.Border,9); Card("behavior-icon-"+i,30,y+14,47,47,soft[i%soft.Length],Color.Transparent,9); Icon(icons[i%icons.Length],42,y+26,23,colors[i%colors.Length]);
                Text(row[0],94,y+12,190,27,13,Sketch20Style.Ink,true); Text(row.Length>3?row[3]:"Chỉ dấu được ghi nhận",94,y+43,Math.Max(150,layoutWidth-590),23,10.5f,Sketch20Style.Muted);
                string val=row.Length>1?row[1]:"—"; Text(val,layoutWidth-390,y+17,210,48,11.5f,Sketch20Style.Ink,false,true);
                string kind=row.Length>2&&(row[2]=="risk"||row[2]=="warn"||row[2]=="safe")?row[2]:val.IndexOf("chưa",StringComparison.OrdinalIgnoreCase)>=0?"safe":val.IndexOf("có",StringComparison.OrdinalIgnoreCase)>=0||val.IndexOf("đáng",StringComparison.OrdinalIgnoreCase)>=0?"risk":"warn"; Badge(kind=="risk"?"Đáng ngờ":kind=="safe"?"An toàn":"Cần kiểm tra",layoutWidth-160,y+24,kind,116); Icon("chevron-right",layoutWidth-34,y+29,16,Sketch20Style.Muted);
            }
        }

        private void DetailStrings(int height)
        {
            int mainY = detailBodyY;
            Card("detail-strings", 0, mainY, layoutWidth, height);
            Card("strings-icon", 22, mainY + 16, 43, 44, Sketch20Style.BlueSoft, Color.Transparent, 10);
            Icon("file", 33, mainY + 27, 21);
            Text("Các chuỗi ký tự được trích xuất", 83, mainY + 9, layoutWidth - 245, 34, 18, Sketch20Style.Ink, true);
            Text("Danh sách chuỗi ký tự có thể đọc được từ tệp nhị phân.", 83, mainY + 43, layoutWidth - 374, 43, 11.5f, Sketch20Style.Muted);
            Badge(Model.Strings.Count + " chuỗi", layoutWidth - 119, mainY + 17, "info", 97);
            SearchBox("string-search", stringSearch, layoutWidth - 269, mainY + 54, 247, value => { stringSearch = value; stringsOffset = 0; });
            int head = mainY + 88;
            Card("strings-header", 22, head, layoutWidth - 44, 31, Sketch20Style.GridHeader, Color.Transparent, 7);
            Text("#", 36, head, 33, 31, 10.5f, Sketch20Style.Muted, true);
            Text("Chuỗi ký tự", 80, head, layoutWidth - 288, 31, 10.5f, Sketch20Style.Muted, true);
            Text("Phân loại", layoutWidth - 189, head, 142, 31, 10.5f, Sketch20Style.Muted, true);
            var all = FilteredStrings();
            int page = Math.Max(1, (height - 132) / 25); stringsOffset = Math.Max(0, Math.Min(stringsOffset, Math.Max(0, all.Count - page)));
            var rows = all.Skip(stringsOffset).Take(page).ToList();
            AddTableScroll("strings-scroll", layoutWidth - 37, head + 31, page * 25, all.Count, page, stringsOffset, value => stringsOffset = value);
            for (int i = 0; i < rows.Count; i++)
            {
                string value = rows[i]; int yy = head + 31 + i * 25;
                Text((i + stringsOffset + 1).ToString("00"), 36, yy, 32, 25, 10.5f, Sketch20Style.Muted);
                Text(value, 80, yy, layoutWidth - 299, 25, 11.5f, Sketch20Style.Ink, false, false, false, true);
                bool technical = new[] { "CreateRemoteThread", "VirtualAllocEx", "LoadLibraryA", "CreateProcessW", "WinExec", "WriteProcessMemory" }.Contains(value);
                bool registry = value.IndexOf(@"CurrentVersion\Run", StringComparison.OrdinalIgnoreCase) >= 0 || value.StartsWith("HKEY_", StringComparison.OrdinalIgnoreCase) || value.StartsWith(@"HKLM\", StringComparison.OrdinalIgnoreCase) || value.StartsWith(@"HKCU\", StringComparison.OrdinalIgnoreCase);
                string kind = technical ? "technical" : value.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("powershell", StringComparison.OrdinalIgnoreCase) >= 0 ? "risk" : registry ? "warn" : value.IndexOf("dll", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0 ? "info" : "neutral";
                Badge(kind == "technical" ? "Kỹ thuật" : kind == "risk" ? "Đáng ngờ" : kind == "warn" ? "Registry" : kind == "info" ? "Hệ thống" : "Tệp nhị phân", layoutWidth - 189, yy, kind, 125);
                if (i < rows.Count - 1) Line(34, yy + 24, layoutWidth - 68);
            }
            if (rows.Count == 0) Text("Không có chuỗi ký tự phù hợp.", 34, head + 43, layoutWidth - 68, 35, 12, Sketch20Style.Muted);
        }
        private void DetailExtra(Sketch20File f, int height)
        {
            int mainY = detailBodyY;
            Card("detail-extra", 0, mainY, layoutWidth, height);
            Icon("file-plus", 23, mainY + 22, 24); Text("Thông tin bổ sung", 64, mainY + 12, layoutWidth - 88, 37, 20, Sketch20Style.Ink, true);
            Line(22, mainY + 59, layoutWidth - 44);
            string[] labels = { "Đường dẫn gốc", "SHA-256", "MD5", "Chữ ký số", "Ngày tạo tệp", "Trạng thái", "Mã mục cách ly" };
            string[] values = { f == null ? "—" : f.Path, f == null ? "—" : f.Sha256, f == null ? "—" : f.Md5, "Chưa xác minh chữ ký số", f == null ? "—" : f.Created, f == null ? "—" : f.Status, f == null ? "—" : f.Id };
            string[] icons = { "folder", "settings", "settings", "file-warning", "clock", "archive", "info" };
            var menu = new ContextMenuStrip();
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i, yy = mainY + 67 + i * 53;
                Card("extra-icon-" + i, 22, yy + 8, 35, 36, Sketch20Style.Background, Color.Transparent, 10);
                Icon(icons[i], 31, yy + 17, 17, Sketch20Style.Muted);
                Text(labels[i], 75, yy + 3, 138, 44, 12, Sketch20Style.Ink, true);
                if (i == 3) Badge(values[i], 230, yy + 14, "warn", 225);
                else if (i == 5) Badge(values[i], 230, yy + 14, f != null && f.Resolved ? "risk" : "info", 170);
                else Text(values[i], 230, yy + 3, layoutWidth - 302, 44, i == 1 || i == 2 ? 11 : 12, Sketch20Style.Ink, false, false, false, i == 1 || i == 2);
                if (i < 6) Line(22, yy + 51, layoutWidth - 44);
                if (i == 0 || i == 1 || i == 2 || i == 6)
                {
                    Action copy = () => { if (f != null && !string.IsNullOrWhiteSpace(values[index])) Clipboard.SetText(values[index]); };
                    Button("copy-extra-" + i, "", layoutWidth - 58, yy + 9, 34, copy, "files", false, false, 33);
                    var item = new ToolStripMenuItem("Sao chép " + labels[i].ToLowerInvariant()); item.Click += delegate { copy(); }; menu.Items.Add(item);
                }
            }
            Canvas.ContextMenuStrip = menu;
        }
        private void SearchBox(string name, string value, int x, int y, int width, Action<string> change)
        {
            Card(name + "-border", x, y, width, 34);
            Icon("search", x + 10, y + 10, 15, Sketch20Style.Muted);
            var input = new TextBox { Name = name, AccessibleName = name == "engine-search" ? "Tìm kiếm công cụ bảo mật" : "Tìm kiếm chuỗi ký tự", BorderStyle = BorderStyle.None, BackColor = Color.White, ForeColor = Sketch20Style.Ink, Font = Sketch20Style.Font(12), Text = value, Bounds = new Rectangle(x + 35, y + 9, width - 47, 22) };
            input.TextChanged += delegate
            {
                int caret = input.SelectionStart; string text = input.Text;
                change(text); Render();
                var next = Canvas.Controls[name] as TextBox;
                if (next != null) { next.Focus(); next.SelectionStart = Math.Min(caret, next.TextLength); }
            };
            Canvas.Controls.Add(input); Canvas.Nodes[name] = input.Bounds;
            SearchCue(input.Handle, 0x1501, IntPtr.Zero, name == "engine-search" ? "Tìm tên công cụ..." : "Tìm kiếm chuỗi ký tự...");
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
            input.Placeholder = Model.KeyConfigured ? "Đã lưu API key · Nhập key mới để thay thế" : "Nhập API key VirusTotal";
            input.Entry.Text = typedKey; input.Entry.TextChanged += delegate { typedKey = input.Entry.Text; }; Canvas.Controls.Add(input); Canvas.Nodes[input.Name] = input.Bounds;
        }
    }
}
