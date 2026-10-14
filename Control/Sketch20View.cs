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
        private readonly Timer layoutRenderTimer, searchRenderTimer;
        private string pendingSearchName;
        private int pendingSearchCaret;
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
        private bool rendering;
        private bool needsScrollbar;
        private int detailBodyY = 306;
        private int sidebarStatusTop;
        private bool CompactSidebar { get { return ClientSize.Width < 1180; } }
        private DateTime? scanningSince;
        private DateTime lastScanRenderUtc = DateTime.MinValue;
        private int stringsOffset, engineOffset, historyOffset, quarantineOffset, threatOffset;
        private bool quarantineOldest, historyOldest;
        private readonly HashSet<int> quarantineSelection = new HashSet<int>();
        private List<string> folders = new List<string>(), files = new List<string>(), customFolders = new List<string>();
        private bool[] customLocations = { true, true, false, false };
        private bool includeSubfolders = true;
        private readonly HashSet<string> excludedDrives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string engineSearch = "", stringSearch = "";
        private DateTime from = DateTime.Today.AddDays(-30), to = DateTime.Today;
        private string scanFilter = "Tất cả", typedKey = "";
        private bool historyFilterInitialized;

        private sealed class NavigationState
        {
            internal string Page, DetailReturnPage;
            internal int SettingsTab, DetailTab, AdvancedMode;
        }
        private readonly Stack<NavigationState> navigation = new Stack<NavigationState>();
        private bool restoringNavigation;

        internal Sketch20View(Sketch20Model initial, Sketch20Actions actions, Func<Sketch20Model> source = null)
        {
            Model = initial; Actions = actions ?? new Sketch20Actions(); readModel = source;
            if (!initial.Sample) { from = DateTime.Today.AddDays(-30); to = DateTime.Today; }
            AutoScaleMode = AutoScaleMode.None; Dock = DockStyle.Fill; BackColor = Sketch20Style.Background;
            sidebar = new Panel { Dock = DockStyle.Left, Width = 208, BackColor = Sketch20Style.Sidebar };
            Theme.BatDoubleBuffer(this);
            Theme.BatDoubleBuffer(sidebar); sidebar.Paint += DrawSidebar;
            sidebar.Resize += delegate { LayoutSidebarBottom(); sidebar.Invalidate(); };
            viewport = new Panel { Name = "Sketch20Viewport", Dock = DockStyle.Fill, AutoScroll = true, BackColor = Sketch20Style.Background };
            Theme.BatDoubleBuffer(viewport);
            Canvas = new Sketch20Surface { Name = "Sketch20Canvas", Location = new Point(26, 29) };
            viewport.Controls.Add(Canvas); Controls.Add(viewport); Controls.Add(sidebar);
            AllowDrop = viewport.AllowDrop = Canvas.AllowDrop = true;
            DragEnter += ExternalDragEnter; viewport.DragEnter += ExternalDragEnter; Canvas.DragEnter += ExternalDragEnter;
            DragDrop += ExternalDragDrop; viewport.DragDrop += ExternalDragDrop; Canvas.DragDrop += ExternalDragDrop;
            MouseWheel += TableMouseWheel; sidebar.MouseWheel += TableMouseWheel; viewport.MouseWheel += TableMouseWheel; Canvas.MouseWheel += TableMouseWheel;
            Canvas.MouseClick += ThreatListMouseClick;
            Canvas.MouseMove += ThreatListMouseMove;
            layoutRenderTimer = new Timer { Interval = 55 };
            layoutRenderTimer.Tick += delegate { layoutRenderTimer.Stop(); if (!rendering && !IsDisposed) Render(); };
            searchRenderTimer = new Timer { Interval = 130 };
            searchRenderTimer.Tick += delegate
            {
                searchRenderTimer.Stop();
                if (rendering || IsDisposed) return;
                var current = !string.IsNullOrEmpty(pendingSearchName) ? Canvas.Controls[pendingSearchName] as TextBox : null;
                bool restoreFocus = current != null && current.Focused;
                string name = pendingSearchName; int caret = pendingSearchCaret;
                Render();
                if (restoreFocus && !string.IsNullOrEmpty(name))
                {
                    var next = Canvas.Controls[name] as TextBox;
                    if (next != null) { next.Focus(); next.SelectionStart = Math.Min(caret, next.TextLength); }
                }
            };
            Resize += delegate { UpdateResponsiveSidebar(); ScheduleLayoutRender(); };
            viewport.Resize += delegate { if (!rendering) ScheduleLayoutRender(); };
            update = new Timer { Interval = 500 };
            update.Tick += delegate { RefreshModel(); };
            if (source != null) update.Start();
            BuildSidebarButtons(); UpdateResponsiveSidebar(); Render();
            Disposed += delegate
            {
                update.Stop(); update.Dispose();
                layoutRenderTimer.Stop(); layoutRenderTimer.Dispose();
                searchRenderTimer.Stop(); searchRenderTimer.Dispose();
                toolTips.Dispose();
            };
        }

        private void ScheduleLayoutRender()
        {
            if (rendering || IsDisposed) return;
            layoutRenderTimer.Stop();
            layoutRenderTimer.Start();
        }

        internal int ScreenNumber
        {
            get
            {
                if (Page == "protection") return 2;
                if (Page == "history") return 3;
                if (Page == "quarantine") return 4;
                if (Page == "settings") return SettingsTab == 0 ? 5 : 14;
                if (Page == "advanced") return AdvancedMode == 0 ? 6 : 14 + AdvancedMode;
                if (Page == "details") return 7 + DetailTab;
                return Model.Scanning ? 18 : Model.ThreatCount > 0 ? 19 : !Model.Running ? 20 : 1;
            }
        }
        internal void SelectScreen(int number)
        {
            if (number < 1 || number > 20) throw new ArgumentOutOfRangeException("number");
            navigation.Clear();
            if (Model.Sample) { Model = Sketch20Samples.Create(number); if (number >= 7 && number <= 10) DetailReturnPage = "overview"; if (number == 11) DetailReturnPage = "quarantine"; if (number == 4) { quarantineSelection.Clear(); quarantineSelection.Add(0); quarantineSelection.Add(1); } }
            SettingsTab = number == 14 ? 1 : 0;
            DetailTab = number >= 7 && number <= 11 ? number - 7 : 0;
            AdvancedMode = number >= 15 && number <= 17 ? number - 14 : 0;
            Page = number == 2 ? "protection" : number == 3 ? "history" : number == 4 ? "quarantine" : number == 5 || number >= 12 && number <= 14 ? "settings" : number == 6 || number >= 15 && number <= 17 ? "advanced" : number >= 7 && number <= 11 ? "details" : "overview";
            stringsOffset = engineOffset = historyOffset = quarantineOffset = threatOffset = 0; viewport.AutoScrollPosition = Point.Empty; NotifyPage(); Render(); ResetPageScroll();
        }
        private NavigationState CurrentNavigationState()
        {
            return new NavigationState
            {
                Page = Page, DetailReturnPage = DetailReturnPage, SettingsTab = SettingsTab,
                DetailTab = DetailTab, AdvancedMode = AdvancedMode
            };
        }

        private void RestoreNavigationState(NavigationState state)
        {
            if (state == null) return;
            Page = state.Page; DetailReturnPage = state.DetailReturnPage;
            SettingsTab = state.SettingsTab; DetailTab = state.DetailTab; AdvancedMode = state.AdvancedMode;
            viewport.AutoScrollPosition = Point.Empty;
            NotifyPage(); Render(); ResetPageScroll();
            if (readModel != null && IsHandleCreated)
                BeginInvoke((Action)(() => { if (!IsDisposed) RefreshModel(true); }));
        }

        private void NavigateBack()
        {
            if (restoringNavigation) return;
            restoringNavigation = true;
            try
            {
                if (navigation.Count > 0) RestoreNavigationState(navigation.Pop());
                else if (Page != "overview") RestoreNavigationState(new NavigationState { Page = "overview", DetailReturnPage = "overview" });
            }
            finally { restoringNavigation = false; }
        }

        internal void Go(string page)
        {
            if (!restoringNavigation && !string.Equals(Page, page, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(page, "overview", StringComparison.OrdinalIgnoreCase))
                    navigation.Clear();
                else
                    navigation.Push(CurrentNavigationState());
            }

            Page = page;
            if (page == "history") historyOffset = 0;
            if (page == "quarantine") quarantineOffset = 0;
            if (page == "threats") threatOffset = 0;
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
            if (!ContainsFocusedEditor(Canvas)) Render();
        }
        private static bool ContainsFocusedEditor(System.Windows.Forms.Control parent)
        {
            return parent.Controls.Cast<System.Windows.Forms.Control>().Any(c =>
                ((c is TextBox || c is UiSelect || c is DateTimePicker) && c.Focused) || ContainsFocusedEditor(c));
        }

        private void TableMouseWheel(object sender, MouseEventArgs e)
        {
            if (Model == null || Canvas == null || Canvas.IsDisposed) return;
            Point mouse = Canvas.PointToClient(System.Windows.Forms.Control.MousePosition);
            VScrollBar bar = null;
            foreach (VScrollBar candidate in Canvas.Controls.OfType<VScrollBar>())
            {
                // Toàn bộ bề ngang của table, cùng dải Y với scrollbar, đều nhận wheel.
                var tableArea = new Rectangle(0, candidate.Top, Math.Max(1, layoutWidth), candidate.Height);
                if (tableArea.Contains(mouse)) { bar = candidate; break; }
            }
            if (bar == null) return;

            int max = Math.Max(0, bar.Maximum - bar.LargeChange + 1);
            if (max <= 0) return;
            int step = SystemInformation.MouseWheelScrollLines <= 0 ? 1 : Math.Min(3, SystemInformation.MouseWheelScrollLines);
            int next = Math.Max(0, Math.Min(max, bar.Value + (e.Delta < 0 ? step : -step)));
            if (next == bar.Value) return;

            SetTableOffset(bar.Name, next);
            var handled = e as HandledMouseEventArgs;
            if (handled != null) handled.Handled = true;
            Render();
        }

        private void SetTableOffset(string name, int value)
        {
            if (name == "quarantine-scroll") quarantineOffset = value;
            else if (name == "history-scroll") historyOffset = value;
            else if (name == "threat-scroll") threatOffset = value;
            else if (name == "engine-scroll") engineOffset = value;
            else if (name == "strings-scroll") stringsOffset = value;
        }

        private void WireTableMouseWheel(System.Windows.Forms.Control parent)
        {
            foreach (System.Windows.Forms.Control child in parent.Controls)
            {
                if (!(child is VScrollBar))
                {
                    child.MouseWheel -= TableMouseWheel;
                    child.MouseWheel += TableMouseWheel;
                }
                if (child.HasChildren) WireTableMouseWheel(child);
            }
        }

        private int ThreatRowAt(Point point)
        {
            if (Page != "threats") return -1;
            for (int i = 0; ; i++)
            {
                Rectangle bounds;
                if (!Canvas.Nodes.TryGetValue("threat-list-row-" + i, out bounds)) break;
                if (bounds.Contains(point)) return threatOffset + i;
            }
            return -1;
        }

        private void ThreatListMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            int row = ThreatRowAt(e.Location);
            if (row < 0 || row >= Model.Threats.Count) return;
            OpenDetail(Model.Threats[row].Index, false);
        }

        private void ThreatListMouseMove(object sender, MouseEventArgs e)
        {
            Canvas.Cursor = ThreatRowAt(e.Location) >= 0 ? Cursors.Hand : Cursors.Default;
        }

        private void BuildSidebarButtons()
        {
            string[] names = { "Tổng quan", "Bảo vệ", "Cách ly", "Lịch sử", "Cài đặt" };
            string[] pages = { "overview", "protection", "quarantine", "history", "settings" };
            for (int i = 0; i < names.Length; i++)
            {
                string page = pages[i];
                var nav = new Button { Name = "nav-" + page, Text = names[i], AccessibleName = names[i], FlatStyle = FlatStyle.Flat,
                    BackColor = Sketch20Style.Sidebar, ForeColor = Color.Transparent, Cursor = Cursors.Hand, TabStop = false,
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
            // Thông tin thiết bị hiển thị trực tiếp ở đáy sidebar, không cần click mở popup.
            var deviceInfo = Model.Sample ? new[] { "DESKTOP-7H3K9P2", "Windows 11 Pro 23H2", "Intel Core i7-12700H", "16 GB", "64-bit", "Dell Precision 5540" } : DeviceInformation;
            var device = new Sketch20SidebarStatus("Thông tin thiết bị", "monitor")
            {
                Name = "sidebar-device", Note = deviceInfo[0], Details = deviceInfo
            };
            sidebar.Controls.Add(device);
            LayoutSidebarBottom();
            string[] sectionNames = { "Chung", "Dữ liệu & công cụ" };
            string[] sectionIcons = { "settings", "hard-drive" };
            for (int i = 0; i < sectionNames.Length; i++)
            {
                int index = i;
                var item = new Sketch20Button(sectionNames[i], sectionIcons[i]) { Name = "settings-section-" + i, Tab = true, TextPixels = 12, Bounds = new Rectangle(10, 347 + i * 46, 188, 40), Visible = false };
                item.Click += delegate { SettingsTab = index; Go("settings"); };
                sidebar.Controls.Add(item);
            }
            BringSidebarStatusToFront();
        }

        private void UpdateResponsiveSidebar()
        {
            int targetWidth = CompactSidebar ? 76 : 208;
            if (sidebar.Width != targetWidth) sidebar.Width = targetWidth;
            string[] pages = { "overview", "protection", "quarantine", "history", "settings" };
            for (int i = 0; i < pages.Length; i++)
            {
                var nav = sidebar.Controls["nav-" + pages[i]] as Button;
                if (nav == null) continue;
                nav.Bounds = CompactSidebar
                    ? new Rectangle(8, 91 + i * 51, 60, 44)
                    : new Rectangle(10, 91 + i * 51, 188, 44);
                toolTips.SetToolTip(nav, nav.Text);
            }
            var device = sidebar.Controls["sidebar-device"];
            if (device != null && CompactSidebar) toolTips.SetToolTip(device, "Thông tin thiết bị · " + Environment.MachineName);
            LayoutSidebarBottom();
            sidebar.Invalidate();
        }

        private void LayoutSidebarBottom()
        {
            const int menuBottom = 339;
            if (CompactSidebar)
            {
                const int compactCardHeight = 68;
                int footerTop = sidebar.ClientSize.Height - 12;
                sidebarStatusTop = Math.Max(menuBottom + 8, footerTop - compactCardHeight - 12);
                var compact = sidebar.Controls["sidebar-device"];
                if (compact != null)
                {
                    compact.Bounds = new Rectangle(10, sidebarStatusTop + 8, 56, 50);
                    compact.Visible = true;
                }
                BringSidebarStatusToFront();
                return;
            }

            int normalFooterTop = Math.Max(0, sidebar.ClientSize.Height - 52);
            const int cardHeight = 220;
            int desiredTop = normalFooterTop - cardHeight - 16;
            sidebarStatusTop = Math.Max(menuBottom + 11, desiredTop);
            if (sidebarStatusTop + cardHeight > normalFooterTop - 8)
                sidebarStatusTop = Math.Max(menuBottom + 4, normalFooterTop - cardHeight - 8);

            var item = sidebar.Controls["sidebar-device"];
            if (item != null)
            {
                item.Bounds = new Rectangle(16, sidebarStatusTop + 8, 176, 204);
                item.Visible = true;
            }
            BringSidebarStatusToFront();
        }

        private void BringSidebarStatusToFront()
        {
            var item = sidebar.Controls["sidebar-device"];
            if (item != null) item.BringToFront();
        }

        private void DrawNav(Graphics g, Button button, string page)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bool active = page == Page || page == "overview" && (Page == "advanced" || Page == "threats" || Page == "details" && (DetailReturnPage == "overview" || DetailReturnPage == "threats")) || page == "quarantine" && Page == "details" && DetailReturnPage == "quarantine" || page == "history" && Page == "details" && DetailReturnPage == "history";
            Sketch20Style.Round(g, button.ClientRectangle, 10, active ? Sketch20Style.BlueSoft : Sketch20Style.Sidebar, Color.Transparent);
            if (active) using (var b = new SolidBrush(Sketch20Style.Blue)) g.FillRectangle(b, 0, 9, 3, 28);
            Color ink = active ? Sketch20Style.Blue : Sketch20Style.Ink;
            string icon = page == "overview" ? "house" : page == "protection" ? "shield" : page == "quarantine" ? "archive" : page == "history" ? "history" : "settings";
            int iconX = CompactSidebar ? (button.Width - 20) / 2 : 17;
            Sketch20ReferenceAssets.FilledIcon(g, page == "protection" ? "shield-check" : page == "quarantine" ? "cube" : page == "history" ? "clock" : icon, new Rectangle(iconX, 12, 20, 20), active ? Sketch20Style.Blue : Sketch20Style.Muted);
            if (!CompactSidebar)
                Sketch20Style.Text(g, button.Text, new Rectangle(52, 0, 115, 44), 14, ink, active);
            if (page == "quarantine" && Model.Quarantine.Count > 0)
            {
                string countText = Model.Quarantine.Count.ToString();
                int badgeW = Math.Max(18, Sketch20Style.Measure(countText, CompactSidebar ? 8.5f : 10.5f, true) + 8);
                int badgeX = CompactSidebar ? button.Width - badgeW - 3 : 166 - badgeW;
                int badgeY = CompactSidebar ? 3 : 14;
                int badgeH = CompactSidebar ? 17 : 20;
                Sketch20Style.Round(g, new Rectangle(badgeX, badgeY, badgeW, badgeH), badgeH / 2, active ? Color.White : Sketch20Style.BlueSoft, Color.Transparent);
                Sketch20Style.Text(g, countText, new Rectangle(badgeX, badgeY, badgeW, badgeH), CompactSidebar ? 8.5f : 10.5f, Sketch20Style.Blue, true, false, false, false, true);
            }
        }

        private void DrawSidebar(object sender, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(Sketch20Style.Sidebar)) g.FillRectangle(bg, e.ClipRectangle);
            using (var pen = new Pen(Sketch20Style.Border)) g.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);

            if (CompactSidebar)
            {
                Sketch20ReferenceAssets.Logo(g, new Rectangle(24, 26, 27, 32));
                Sketch20Style.Round(g, new Rectangle(8, sidebarStatusTop, 60, 68), 13, Color.White, Sketch20Style.Border);
                return;
            }

            Sketch20ReferenceAssets.Logo(g, new Rectangle(27, 26, 27, 32));
            Sketch20Style.Text(g, "Antivirus", new Rectangle(65, 25, 131, 34), 20, Sketch20Style.Ink, true);
            Sketch20Style.Round(g, new Rectangle(12, sidebarStatusTop, 184, 220), 13, Color.White, Sketch20Style.Border);
            using (var pen = new Pen(Sketch20Style.Border))
                g.DrawLine(pen, 24, sidebar.Height - 52, 184, sidebar.Height - 52);
            Sketch20Style.Text(g, "Phiên bản " + Model.Version, new Rectangle(26, sidebar.Height - 43, 156, 24), 10.5f, Sketch20Style.Muted);
        }



        internal void Render()
        {
            if (rendering || IsDisposed || viewport.ClientSize.Width < 90) return;
            rendering = true;
            bool fixedScanViewport = (Page == "overview" && Model != null && Model.Scanning) || Page == "settings";
            bool fixedProtectionViewport = Page == "protection";
            bool fixedViewport = fixedScanViewport || fixedProtectionViewport;
            var scroll = fixedViewport ? Point.Empty : viewport.AutoScrollPosition;
            try
            {
                // Tổng quan khi quét là viewport cố định: không tạo scrollbar và không
                // toggle AutoScroll qua lại theo từng nhịp progress.
                viewport.AutoScroll = false;
                UpdateResponsiveSidebar();
                int pageMargin = CompactSidebar ? 16 : 26;
                Canvas.Location = new Point(pageMargin, 29);
                Canvas.SuspendLayout();
                toolTips.RemoveAll();
                if (Canvas.ContextMenuStrip != null) { Canvas.ContextMenuStrip.Dispose(); Canvas.ContextMenuStrip = null; }
                foreach (System.Windows.Forms.Control c in Canvas.Controls.Cast<System.Windows.Forms.Control>().ToArray()) c.Dispose();
                Canvas.Drawing.Clear(); Canvas.BackgroundDrawing.Clear(); Canvas.Nodes.Clear(); Canvas.TextRuns.Clear();
                int availableWidth = viewport.Width - pageMargin * 2 - (!fixedViewport && needsScrollbar ? SystemInformation.VerticalScrollBarWidth : 0);
                layoutWidth = Math.Max(620, availableWidth);
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
                else if (Page == "threats") ThreatTable(114);
                else if (Page == "details") ReferenceDetails();
                else Overview();
                if (WindowForm != null) Sketch20Chrome.AddControls(WindowForm, this, layoutWidth);
                Canvas.Height = fixedViewport ? Math.Max(1, viewport.ClientSize.Height - Canvas.Top) : ContentHeight;
                // Tổng quan khi quét và tab Bảo vệ là viewport cố định: không sinh scrollbar.
                int horizontalMin = layoutWidth > availableWidth ? layoutWidth + pageMargin * 2 : 0;
                viewport.AutoScrollMinSize = fixedViewport ? Size.Empty : new Size(horizontalMin, ContentHeight + (Page == "settings" ? 45 : 31));
                viewport.AutoScroll = !fixedViewport;
                if (!fixedViewport) viewport.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
                WireTableMouseWheel(this);
                LayoutSidebarBottom();
                Canvas.ResumeLayout(); Canvas.Invalidate();
                var sideDevice = sidebar.Controls["sidebar-device"] as Sketch20SidebarStatus;
                if (sideDevice != null)
                {
                    var info = Model.Sample ? new[] { "DESKTOP-7H3K9P2", "Windows 11 Pro 23H2", "Intel Core i7-12700H", "16 GB", "64-bit", "Dell Precision 5540" } : DeviceInformation;
                    sideDevice.Note = info[0]; sideDevice.Details = info; sideDevice.StatusColor = Sketch20Style.Blue;
                }
                foreach (System.Windows.Forms.Control c in sidebar.Controls)
                {
                    if (c.Name.StartsWith("settings-section-"))
                    {
                        c.Visible = false;
                        ((Sketch20Button)c).Selected = c.Name == "settings-section-" + SettingsTab;
                    }
                    c.Invalidate();
                }
                BringSidebarStatusToFront();
                sidebar.Invalidate();
            }
            finally { rendering = false; }
            bool needs = !fixedScanViewport && ContentHeight + (Page == "settings" ? 45 : 31) > viewport.Height;
            if (needsScrollbar != needs) { needsScrollbar = needs; Render(); }
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
        private void Card(string name, int x, int y, int width, int height, Color? fill = null, Color? edge = null, int radius = 14)
        {
            var bounds = new Rectangle(x, y + bodyOffset, width, height); Canvas.Nodes[name] = bounds;
            Action<Graphics> paint = g =>
            {
                Color actualFill = fill ?? Color.White;
                Color actualEdge = edge ?? ColorTranslator.FromHtml("#dce7f5");
                Sketch20Style.Round(g, bounds, radius, actualFill, actualEdge);
            };
            Canvas.Drawing.Add(paint); Canvas.BackgroundDrawing.Add(paint);
        }
        private void GradientCard(string name, int x, int y, int width, int height, Color start, Color end, Color edge, int radius = 14)
        {
            var bounds = new Rectangle(x, y + bodyOffset, width, height); Canvas.Nodes[name] = bounds;
            Action<Graphics> paint = g =>
            {
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
            bool indicator = Page != "settings" && (new[] { "Cao", "Trung bình", "Nguy hiểm", "Đáng ngờ", "Đang cách ly", "Đã cách ly", "An toàn" }.Contains(caption) || (kind == "risk" || kind == "safe") && (caption ?? "").EndsWith(" tệp", StringComparison.OrdinalIgnoreCase));
            const float badgeText = 10.8f;
            int natural = Sketch20Style.PillWidth(caption, badgeText * ReferenceTextScale, indicator);
            // Badge luôn ôm nội dung. Tham số width cũ chỉ còn giữ tương thích lời gọi,
            // không được kéo giãn hoặc cắt chữ theo độ rộng cột.
            int w = natural;
            Card("badge-" + x + "-" + y, x, y, w, 24, bg, Color.Transparent, 12);
            if (indicator)
            {
                int py = y + bodyOffset;
                Canvas.Drawing.Add(g => { using (var brush = new SolidBrush(ink)) g.FillEllipse(brush, x + 8, py + 6, 12, 12); });
                Icon(kind == "safe" ? "check" : "alert", x + 10, y + 8, 8, Color.White);
            }
            Text(caption, x + (indicator ? 26 : 8), y, Math.Max(1, w - (indicator ? 34 : 16)), 24, badgeText, ink, true);
        }
        private Sketch20Button Button(string name, string caption, int x, int y, int width, Action click, string icon = "", bool primary = false, bool danger = false, int height = 40, bool link = false)
        {
            var b = new Sketch20Button(caption, icon) { Name = name, Primary = primary, Danger = danger, Link = link,
                Bounds = new Rectangle(x, y + bodyOffset, width, height), TextPixels = (height <= 33 ? 12 : 13) * ReferenceTextScale };
            b.Click += delegate { click(); }; Canvas.Controls.Add(b); Canvas.Nodes[name] = b.Bounds; return b;
        }
        private void HitArea(string name, string accessibleName, int x, int y, int width, int height, Action click)
        {
            var hit = new Sketch20HitArea
            {
                Name = name, AccessibleName = accessibleName,
                Bounds = new Rectangle(x, y + bodyOffset, width, height)
            };
            hit.Click += delegate { click(); };
            Canvas.Controls.Add(hit); Canvas.Nodes[name] = hit.Bounds;
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
            string title = Page == "overview" ? "Tổng quan" : Page == "protection" ? "Bảo vệ" : Page == "history" ? "Lịch sử" : Page == "quarantine" ? "Cách ly" : Page == "settings" ? "Cài đặt" : Page == "advanced" ? "Quét virus" : Page == "threats" ? "Mối đe dọa cần xử lý" : "Chi tiết tệp";
            string subtitle = Page == "overview" ? "Theo dõi trạng thái bảo vệ và hoạt động của máy tính." :
                Page == "protection" ? "Các mô-đun bảo vệ giúp giữ an toàn cho thiết bị của bạn." :
                Page == "history" ? "Xem lại các lần quét, phát hiện và hành động đã thực hiện." :
                Page == "quarantine" ? "Các tệp có khả năng gây hại đã được cách ly để đảm bảo an toàn cho thiết bị." :
                Page == "settings" ? "Tùy chỉnh hoạt động và các tính năng bảo vệ theo nhu cầu của bạn." :
                Page == "advanced" ? "Chọn chế độ quét phù hợp để kiểm tra và loại bỏ các mối đe dọa." :
                Page == "threats" ? "Chọn một tệp trong danh sách để xem chi tiết trước khi cách ly." :
                "Xem thông tin chi tiết và kết quả phân tích của tệp được phát hiện.";

            // 5 tab cấp 1 của sidebar không có Back. Chỉ màn con mới hiển thị Back.
            bool primaryPage = Page == "overview" || Page == "protection" || Page == "quarantine" || Page == "history" || Page == "settings";
            if (!primaryPage)
            {
                var back = Button("page-back", "", 0, 6, 32, NavigateBack, "arrow-left", false, false, 32, true);
                back.AccessibleName = "Quay lại màn trước";
                Text(title, 44, 0, layoutWidth - (Page == "threats" ? 250 : 74), 44, 27, Sketch20Style.Ink, true);
                Text(subtitle, 44, 39, layoutWidth - (Page == "threats" ? 270 : 94), 25, 12.5f, Sketch20Style.Muted);
                if (Page == "threats" && Model.Threats.Count > 0)
                    Button("isolate-all-threats", "Cách ly tất cả", layoutWidth - 166, 2, 166, () => Command("isolate"), "archive", true, false, 40);
            }
            else
            {
                // Các màn cấp 1 chỉ giữ tiêu đề chữ, không dùng ô icon trước tiêu đề.
                Text(title, 0, 6, layoutWidth - 230, 36, 24, Sketch20Style.Ink, true);
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
            Card("tabs-shell", 0, y, layoutWidth, 48, Color.White, ColorTranslator.FromHtml("#dce7f5"), 14);
            string[] icons = Page == "settings" ? new[] { "settings", "shield-check", "hard-drive" } : new[] { "file", "sigma", "activity", "files", "shield-check" };
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
            Canvas.Nodes["tabs"] = new Rectangle(0, y + bodyOffset, layoutWidth, 48);
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
            int textWidth = width - 57;
            bool singleLine = value != null && value.IndexOf('\n') < 0 && Sketch20Style.Measure(value, 12) <= textWidth;
            if (singleLine)
                Text(value, x + 41, y, textWidth, height, 12, warn ? Sketch20Style.Amber : Sketch20Style.Muted);
            else
                Text(value, x + 41, y + 7, textWidth, height - 14, 12, warn ? Sketch20Style.Amber : Sketch20Style.Muted, false, true);
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
                string prefix = state == 1 ? "Bảo vệ thời gian thực" : Model.ThreatCount.ToString();
                string suffix = state == 1 ? "đang tắt" : "tệp cần được xử lý";
                // TextRenderer vẫn chừa một chút side-bearing quanh glyph dù NoPadding.
                // Trừ theo cỡ font để phần số/chữ màu và phần tiêu đề kế tiếp nhìn như một câu liền.
                int prefixW = Math.Max(1, Sketch20Style.Measure(prefix, titlePixels, true) - (int)Math.Round(titlePixels * 0.45f));
                const int splitTitleGap = 7;
                Text(prefix, 26, heroY + 25, prefixW, titleH, titlePixels, state == 3 ? Sketch20Style.Red : Sketch20Style.Ink, true);
                Text(suffix, 26 + prefixW + splitTitleGap, heroY + 25, Math.Max(1, titleW - prefixW - splitTitleGap), titleH, titlePixels, state == 1 ? Sketch20Style.Amber : Sketch20Style.Ink, true);
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
                Button("show-details", "Xem chi tiết", 26, actionY, 149, () => Go("threats"), "file-search", true, true);
                Button("quick-scan", "Quét lại", 187, actionY, 125, () => Command("scan"), "refresh");
            }
            else
            {
                int scanY = Math.Max(121, noteY + noteH + 16);
                int progressW = Math.Max(195, copyW - 123);
                Card("progress-track", 26, heroY + scanY, progressW, 10, Sketch20Style.Track, Color.Transparent, 5);
                if (Model.Progress >= 0)
                {
                    int progress = Math.Min(100, Model.Progress);
                    Card("progress-fill", 26, heroY + scanY, Math.Max(6, progressW * progress / 100), 10, Sketch20Style.Blue, Color.Transparent, 5);
                    Text(progress + "%", 38 + progressW, heroY + scanY - 9, 108, 28, 12, Sketch20Style.Blue, true);
                }
                else
                {
                    // Không biết trước tổng số tệp nếu quét cây thư mục. Dùng thanh hoạt động
                    // chạy theo số tệp THẬT đã xử lý thay vì vẽ cố định 35% giả lập.
                    int segment = Math.Max(36, progressW / 4);
                    int travel = Math.Max(1, progressW - segment);
                    int x = 26 + (Model.ScannedFiles * 7 % travel);
                    Card("progress-fill", x, heroY + scanY, segment, 10, Sketch20Style.Blue, Color.Transparent, 5);
                    Text("Đang quét", 38 + progressW, heroY + scanY - 9, 108, 28, 12, Sketch20Style.Blue, true);
                }
                Text(Model.ScannedFiles.ToString("N0") + " tệp · " + Model.ThreatCount + " mối đe dọa", 26, heroY + scanY + 15, copyW, 24, 10.5f, Sketch20Style.Muted);
                // Giữ block đường dẫn nằm trọn phía trên hàng nút; trước đây dòng path chạm/đè button.
                Text("Đang kiểm tra:", 26, heroY + scanY + 43, copyW, 19, 11, Sketch20Style.Ink);
                Text(Model.ScanPath, 26, heroY + scanY + 62, copyW, 17, 10.5f, Sketch20Style.Muted);
                // Trong lúc đang quét chưa có kết quả cuối cùng: chỉ cho phép hủy phiên quét.
                int scanActionY = actionY + 8;
                Button("cancel-scan", "Hủy quét", 26, scanActionY, 134, () => Command("cancel"), "x", false, true);
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
                Text(labels[i], x + 82, statsY + 13, w - 96, 24, 11.5f, Sketch20Style.Ink);
                Text(values[i], x + 82, statsY + 37, w - 92, 33, i == 2 ? 17.5f : 22, i == 0 && state == 3 ? Sketch20Style.Red : Sketch20Style.Ink, true);
                Text(hints[i], x + 82, statsY + 75, w - 92, 21, 10.2f, Sketch20Style.Muted);
            }
            int lowerY = statsY + 119;
            int lowerH = state == 2
                ? Math.Max(164, Math.Min(360, viewport.Height - lowerY - 45))
                : Math.Max(342, Math.Min(452, viewport.Height - lowerY - 62));
            OverviewLower(lowerY, lowerH);
            ContentHeight = state == 2 ? Math.Max(1, viewport.ClientSize.Height - 30) : lowerY + lowerH + 14;
        }
        private void MiniStatus(string title, string note, int x, int y, Color color, int width)
        {
            Canvas.Drawing.Add(g => { using (var b = new SolidBrush(color)) g.FillEllipse(b, x, y + 4, 19, 19); });
            Icon(note == "Đang tắt" ? "alert" : "check", x + 3, y + 7, 13, Color.White);
            Text(title, x + 29, y, width - 29, 26, 11.5f, Sketch20Style.Ink, true);
            Text(note, x + 29, y + 25, width - 29, 22, 10.5f, note == "Đang tắt" ? Sketch20Style.Red : Sketch20Style.Muted);
        }
        private void OverviewLower(int y, int height)
        {
            Card("activity-card", 0, y, layoutWidth, height, Color.White, ColorTranslator.FromHtml("#dce7f5"), 16);
            Card("activity-title-icon", 17, y + 10, 36, 36, ColorTranslator.FromHtml("#edf5ff"), Color.Transparent, 10);
            Icon("history", 27, y + 20, 16, Sketch20Style.Blue);
            Text("Hoạt động gần đây", 66, y + 8, layoutWidth - 205, 32, 17, Sketch20Style.Ink, true);
            Button("open-history", "Xem tất cả  →", layoutWidth - 126, y + 11, 109, () => Go("history"), "", false, false, 28, true);
            const int activityListTop = 60;
            const int activityBottomPadding = 12;
            const int activityRowGap = 8;
            int availableRowsH = Math.Max(58, height - activityListTop - activityBottomPadding);
            int maxRows = Math.Max(1, Math.Min(5, (availableRowsH + activityRowGap) / (58 + activityRowGap)));
            int actRows = Math.Min(maxRows, Model.Activities.Count);
            int cardH = actRows == 0 ? 0 : Math.Min(76, Math.Max(58, (availableRowsH - activityRowGap * (actRows - 1)) / actRows));
            int rowsStartY = y + activityListTop;
            if (actRows == 0)
            {
                int emptyY = y + Math.Max(62, (height - 82) / 2);
                Card("activity-empty-icon", (layoutWidth - 48) / 2, emptyY, 48, 48, Sketch20Style.GreenSoft, Color.Transparent, 24);
                Icon("shield-check", (layoutWidth - 22) / 2, emptyY + 13, 22, Sketch20Style.Green);
                Text("Chưa có hoạt động gần đây.", 20, emptyY + 58, layoutWidth - 40, 30, 12.5f, Sketch20Style.Muted, true, false, true);
            }
            for (int i = 0; i < actRows; i++)
            {
                var a = Model.Activities[i]; int top = rowsStartY + i * (cardH + activityRowGap), center = top + cardH / 2;
                Color fg = a[0] == "check" ? Sketch20Style.Green : a[0] == "archive" ? Sketch20Style.Red : Sketch20Style.Blue;
                Color bg = a[0] == "check" ? Sketch20Style.GreenSoft : a[0] == "archive" ? Sketch20Style.RedSoft : Sketch20Style.BlueSoft;
                Card("activity-row-" + i, 12, top, layoutWidth - 24, cardH, Color.White, ColorTranslator.FromHtml("#e5edf8"), 11);
                Card("activity-icon-" + i, 22, center - 19, 38, 38, bg, Color.Transparent, 19);
                FilledIcon(a[0] == "check" ? "shield-check" : a[0], 32, center - 9, 18, fg);
                if (string.IsNullOrWhiteSpace(a[2]))
                    Text(a[1], 72, center - 15, layoutWidth - 235, 30, 11.6f, Sketch20Style.Ink, true);
                else
                    TwoLineText(a[1], a[2], 72, center - 22, layoutWidth - 235, 11.6f, 9.7f);
                Text(a[3], layoutWidth - 150, center - 15, 126, 30, 9.5f, Sketch20Style.Muted, false, false, true);
            }
        }
        private static readonly string[] DeviceInformation = ReadDeviceInformation();
        private static string[] ReadDeviceInformation()
        {
            string cpu = "Máy tính Windows", os = Environment.OSVersion.VersionString, memory = "—", model = "Máy tính Windows";
            try
            {
                cpu = Convert.ToString(Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", cpu)).Trim();
                os = Convert.ToString(Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", os));
                if (Environment.OSVersion.Version.Build >= 22000) os = os.Replace("Windows 10", "Windows 11");
                memory = Math.Round(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1073741824.0) + " GB";
                string manufacturer = Convert.ToString(Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer", "")).Replace(" Inc.", "").Trim();
                string product = Convert.ToString(Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName", "")).Trim();
                model = (manufacturer + " " + product).Trim();
                if (string.IsNullOrWhiteSpace(model)) model = "Máy tính Windows";
            }
            catch { }
            return new[] { Environment.MachineName, os, cpu, memory, Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit", model };
        }
        private void ShowDeviceInformation()
        {
            var vals = Model.Sample ? new[] { "DESKTOP-7H3K9P2", "Windows 11 Pro 23H2", "Intel Core i7-12700H", "16 GB", "64-bit", "Dell Precision 5540" } : DeviceInformation;
            AppDialog.Show("Thiết bị: " + vals[0] + "\nHệ điều hành: " + vals[1] + "\nBộ xử lý: " + vals[2] + "\nBộ nhớ: " + vals[3], "Thông tin thiết bị", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                int center = top + 32;
                Icon(row[0], 26, center - 9, 18, fg);
                if (string.IsNullOrWhiteSpace(row[2]))
                    Text(row[1], 58, center - 15, layoutWidth - 175, 30, 13, Sketch20Style.Ink, true);
                else
                    TwoLineText(row[1], row[2], 58, top + 8, layoutWidth - 175, 13, 11);
                Text(row[3], layoutWidth - 101, center - 15, 75, 30, 11, Sketch20Style.Muted, false, false, true);
            }
            Footer(y + height + 18, Model.Scanning ? "Đang quét: " + Model.ScanPath : Model.Status, "CSDL chữ ký: " + Model.Signature);
        }
        private void ThreatTable(int y)
        {
            const int rowH = Sketch20Style.TableRowHeight, rowGap = Sketch20Style.TableRowGap;
            int count = Model.Threats.Count;
            int visible = Math.Min(count, Math.Max(3, Math.Min(7, (viewport.Height - y - 238) / (rowH + rowGap))));
            threatOffset = Math.Max(0, Math.Min(threatOffset, Math.Max(0, count - Math.Max(1, visible))));
            int rowsHeight = count == 0 ? 104 : visible * (rowH + rowGap) - rowGap;
            int listH = 79 + Sketch20Style.TableHeaderHeight + Sketch20Style.TableOuterPaddingY + rowsHeight + Sketch20Style.TableOuterPaddingY + 44;

            Card("threat-list", 0, y, layoutWidth, listH, Color.White, ColorTranslator.FromHtml("#dce7f5"), 16);

            // Header thoáng, cùng ngôn ngữ card với các màn còn lại.
            Card("threat-title-icon", 22, y + 17, 46, 46, Sketch20Style.RedSoft, Color.Transparent, 12);
            Icon("shield-alert", 34, y + 29, 22, Sketch20Style.Red);
            Text("Danh sách tệp cần cách ly", 82, y + 12, layoutWidth - 280, 32, 19, Sketch20Style.Ink, true);
            Text("Kiểm tra từng tệp trước khi thực hiện cách ly.", 82, y + 43, layoutWidth - 300, 24, 11, Sketch20Style.Muted);
            int countW = Sketch20Style.PillWidth(count + " tệp", 10.8f * ReferenceTextScale, true);
            Badge(count + " tệp", layoutWidth - countW - 22, y + 27, count > 0 ? "risk" : "safe", countW);

            int head = y + 79;
            int rowX = 18;
            bool hasScroll = count > visible;
            int rowW = layoutWidth - rowX - (hasScroll ? 37 : 18);
            // Tệp là cột flex; mức độ/phát hiện/thao tác có minimum riêng.
            // Cột thao tác phải luôn đủ chỗ cho toàn bộ chữ "Xem chi tiết" + chevron.
            // Khi hẹp, ưu tiên co cột tệp/nội dung thay vì rút gọn action thành "Xem chi...".
            int[] threatCols = ResponsiveTableWidths(rowW, 0, 312, 108, 168, 154);
            int fileW = threatCols[0], severityW = threatCols[1], reasonW = threatCols[2], actionW = threatCols[3];
            int severityX = rowX + fileW, reasonX = severityX + severityW, actionX = reasonX + reasonW;
            Card("threat-table-header", rowX, head, rowW, Sketch20Style.TableHeaderHeight, ColorTranslator.FromHtml("#f7faff"), Color.Transparent, 9);
            Text("TỆP TIN", rowX + 18, head, fileW - 28, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            Text("MỨC ĐỘ", severityX, head, severityW - 12, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            Text("PHÁT HIỆN", reasonX, head, reasonW - 12, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            Text("THAO TÁC", actionX, head, actionW - 8, Sketch20Style.TableHeaderHeight, 9.0f, Sketch20Style.Muted, true);

            int rowsY = head + Sketch20Style.TableHeaderHeight + Sketch20Style.TableOuterPaddingY;
            if (count == 0)
            {
                Card("threat-empty", rowX, rowsY, rowW, rowsHeight, Sketch20Style.Background, Color.Transparent, 12);
                Card("threat-empty-icon", rowX + 22, rowsY + 22, 46, 46, Sketch20Style.GreenSoft, Color.Transparent, 23);
                Icon("shield-check", rowX + 34, rowsY + 34, 22, Sketch20Style.Green);
                Text("Không còn tệp nào đang chờ xử lý", rowX + 84, rowsY + 19, rowW - 110, 30, 14, Sketch20Style.Ink, true);
                Text("Thiết bị hiện không có mối đe dọa cần cách ly.", rowX + 84, rowsY + 48, rowW - 110, 24, 10.8f, Sketch20Style.Muted);
                Button("threats-empty-back", "Về Tổng quan", rowX + rowW - 154, rowsY + 31, 132, () => Go("overview"), "arrow-left", false, false, 36);
            }
            else
            {
                if (hasScroll)
                    AddTableScroll("threat-scroll", layoutWidth - 17, rowsY, rowsHeight, count, Math.Max(1, visible), threatOffset, value => threatOffset = value);
                for (int i = 0; i < visible; i++)
                {
                    var f = Model.Threats[threatOffset + i];
                    int top = rowsY + i * (rowH + rowGap), index = f.Index;
                    var row = new Sketch20ThreatRow(f, severityX - rowX, reasonX - rowX, actionX - rowX, false)
                    {
                        Name = "threat-list-row-" + i,
                        Bounds = new Rectangle(rowX, top, rowW, rowH)
                    };
                    row.Click += delegate { OpenDetail(index, false); };
                    Canvas.Controls.Add(row); Canvas.Nodes[row.Name] = row.Bounds;
                }
            }

            int footerY = rowsY + rowsHeight + Sketch20Style.TableOuterPaddingY;
            Icon("info", 26, footerY + 14, 16, Sketch20Style.Blue);
            Text("Nhấn vào một dòng để xem chi tiết trước khi xử lý.", 51, footerY + 7, layoutWidth - 78, 30, 10.5f, Sketch20Style.Muted);
            ContentHeight = y + listH + 18;
        }

        internal static readonly string[] FeatureKeys = { "rt", "restore", "usb", "download", "behavior", "startup", "aq", "vtq" };
        internal static readonly string[] FeatureNames = { "Bảo vệ thời gian thực", "Bảo vệ tệp", "Bảo vệ USB", "Bảo vệ tải xuống", "Giám sát hành vi", "Bảo vệ thư mục khởi động", "Tự động cách ly", "Bảo vệ web · VirusTotal" };
        internal static readonly string[] FeatureNotes = { "Giám sát tệp mới hoặc thay đổi trong máy.", "Kiểm tra lại tệp trước khi khôi phục từ cách ly.", "Quét ổ USB ngay khi được kết nối.", "Kiểm tra tệp tải về từ Internet.", "Theo dõi tiến trình có hành vi đáng ngờ.", "Kiểm tra tệp mới trong thư mục Startup.", "Chuyển tệp nguy hiểm vào khu vực cách ly.", "Tra cứu tệp nghi ngờ bằng mã băm." };
        private static string Kind(string severity)
        { return severity == "Cao" ? "risk" : severity == "Trung bình" ? "warn" : "info"; }

        // Phân bổ cột dùng chung cho các table custom. Các cột giữ minimum rõ ràng;
        // cột flex nhận toàn bộ phần dư để resize không làm cột khác mất chữ.
        private static int[] ResponsiveTableWidths(int totalWidth, int flexIndex, params int[] minimums)
        {
            var widths = (int[])minimums.Clone();
            int sum = widths.Sum();
            if (totalWidth >= sum)
            {
                widths[Math.Max(0, Math.Min(flexIndex, widths.Length - 1))] += totalWidth - sum;
                return widths;
            }

            // Trường hợp cực hẹp: ưu tiên giữ các cột trạng thái/thao tác; co cột flex trước.
            int flex = Math.Max(0, Math.Min(flexIndex, widths.Length - 1));
            int deficit = sum - totalWidth;
            int flexFloor = Math.Min(widths[flex], 96);
            int take = Math.Min(deficit, Math.Max(0, widths[flex] - flexFloor));
            widths[flex] -= take; deficit -= take;
            for (int i = 0; i < widths.Length && deficit > 0; i++)
            {
                if (i == flex) continue;
                int floor = Math.Min(widths[i], 64);
                take = Math.Min(deficit, Math.Max(0, widths[i] - floor));
                widths[i] -= take; deficit -= take;
            }
            return widths;
        }

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
            if (!historyFilterInitialized && Model != null && !Model.Sample && (Model.HistoryTotal > 0 || Model.History.Count > 0))
            {
                from = Model.HistoryFrom.Date;
                to = Model.HistoryTo.Date;
                scanFilter = NormalizeHistoryFilter(Model.HistoryFilter);
                historyFilterInitialized = true;
            }
            const int filterY = 81, filterH = 152, controlY = 176, controlH = 40;
            GradientCard("history-filter-card", 0, filterY, layoutWidth, filterH,
                Color.White, ColorTranslator.FromHtml("#f7fbff"), ColorTranslator.FromHtml("#c7d5e6"), 16);
            Card("history-filter-icon",18,filterY+11,52,52,ColorTranslator.FromHtml("#e8f2ff"),Color.Transparent,15);
            Icon("filter",33,filterY+26,22,Sketch20Style.Blue);
            Text("Bộ lọc lịch sử",84,filterY+10,layoutWidth-320,32,18.8f,ColorTranslator.FromHtml("#101f38"),true);
            Text("Thu hẹp kết quả theo thời gian và loại quét.",84,filterY+42,layoutWidth-330,22,10.8f,ColorTranslator.FromHtml("#667895"));

            int refreshW=112, exportW=136, filterBtnW=102, gap=12;
            int available=layoutWidth-36-refreshW-exportW-filterBtnW-gap*5;
            int dateW=(int)(available*0.28), typeW=available-dateW*2;
            if (typeW > 264) { int extra = typeW - 264; typeW = 264; dateW += extra / 2; }
            int date2X=18+dateW+gap, filterTypeX=date2X+dateW+gap;
            int labelY=filterY+74;
            Text("Từ ngày",18,labelY,dateW,18,10.1f,ColorTranslator.FromHtml("#536684"),true);
            Text("Đến ngày",date2X,labelY,dateW,18,10.1f,ColorTranslator.FromHtml("#536684"),true);
            Text("Loại quét",filterTypeX,labelY,typeW,18,10.1f,ColorTranslator.FromHtml("#536684"),true);
            DateBox("history-from",from,18,controlY,dateW,d=>from=d);
            DateBox("history-to",to,date2X,controlY,dateW,d=>to=d);
            string[] types={"Tất cả","Quét nhanh","Quét thư mục","Quét tệp","Quét toàn bộ","Quét tùy chỉnh"};
            SelectBox("history-filter",types,Math.Max(0,Array.IndexOf(types,scanFilter)),filterTypeX,controlY,typeW,i=>scanFilter=types[i],controlH);
            int filterX=filterTypeX+typeW+gap;
            Button("filter-history","Lọc",filterX,controlY,filterBtnW,()=>{ historyOffset=0; if(Actions.FilterHistory!=null) Actions.FilterHistory(from,to,scanFilter); if(Model.Sample) Model.History=Sketch20Samples.Create(3).History.Where(h=>scanFilter=="Tất cả"||h.Type==scanFilter).ToList(); RefreshModel(true); },"search",true,false,controlH);
            int refreshX=filterX+filterBtnW+gap;
            Button("history-refresh","Làm mới",refreshX,controlY,refreshW,()=>{ historyOffset=0; Command("history-refresh"); },"refresh",false,false,controlH);
            Button("export-csv","Xuất CSV",refreshX+refreshW+gap,controlY,exportW,()=>Command("export"),"download",false,false,controlH);

            const int rowH=Sketch20Style.TableRowHeight,rowGap=Sketch20Style.TableRowGap;
            int rows=Model.History.Count,listY=248;
            int visible=Math.Min(rows,Math.Max(2,Math.Min(7,(viewport.Height-listY-205)/(rowH+rowGap))));
            historyOffset=Math.Max(0,Math.Min(historyOffset,Math.Max(0,rows-Math.Max(1,visible))));
            int rowsHeight=rows==0?100:visible*(rowH+rowGap)-rowGap;
            int listH=72+Sketch20Style.TableHeaderHeight+Sketch20Style.TableOuterPaddingY+rowsHeight+Sketch20Style.TableOuterPaddingY+44;
            Card("history-list",0,listY,layoutWidth,listH,Color.White,ColorTranslator.FromHtml("#dce7f5"),16);
            Card("history-title-icon",20,listY+15,42,42,Sketch20Style.BlueSoft,Color.Transparent,11);
            Icon("history",31,listY+26,20,Sketch20Style.Blue);
            Text("Nhật ký hoạt động",76,listY+10,layoutWidth-410,31,18.5f,Sketch20Style.Ink,true);
            Text("Theo dõi các lần quét và kết quả xử lý gần đây.",76,listY+39,layoutWidth-430,22,10.7f,Sketch20Style.Muted);
            Text("Tổng "+(Model.Sample?rows:Model.HistoryTotal)+" lần quét",layoutWidth-324,listY+20,132,27,10.7f,Sketch20Style.Muted,false,false,true);
            HeaderSelectBox("history-sort",new[]{"Mới nhất trước","Cũ nhất trước"},historyOldest?1:0,layoutWidth-184,listY+20,162,27,10.7f,value=>
            {
                historyOldest=value==1; historyOffset=0;
                if(Actions.SortHistory!=null) Actions.SortHistory(historyOldest);
                if(Model.Sample) Model.History=historyOldest?Model.History.OrderBy(h=>h.Date+h.Time).ToList():Model.History.OrderByDescending(h=>h.Date+h.Time).ToList();
                RefreshModel(true);
            });

            int head=listY+72,rowX=16;
            bool hasScroll=rows>visible;
            int rowW=layoutWidth-rowX-(hasScroll?37:16);

            // Responsive theo chiều rộng THỰC của row. Cột loại quét nhận toàn bộ phần dư;
            // các cột thời gian/thống kê/kết quả/thao tác giữ đủ chỗ để không cắt chữ.
            bool compactHistory = rowW < 900;
            int timeW = compactHistory ? 102 : 116;
            int statW = compactHistory ? 184 : 205;
            int resultW = compactHistory ? 148 : 156;
            int actionW = compactHistory ? 94 : 104;
            int scanTypeW = Math.Max(230, rowW - timeW - statW - resultW - actionW - 24);
            int typeX = rowX + timeW;
            int statX = typeX + scanTypeW;
            int resultX = statX + statW;
            int actionX = rowX + rowW - actionW;

            Card("history-table-header",rowX,head,rowW,Sketch20Style.TableHeaderHeight,ColorTranslator.FromHtml("#f7faff"),Color.Transparent,9);
            Text("THỜI GIAN",rowX+14,head,timeW-20,Sketch20Style.TableHeaderHeight,9.4f,Sketch20Style.Muted,true);
            Text("LOẠI QUÉT",typeX,head,scanTypeW-14,Sketch20Style.TableHeaderHeight,9.4f,Sketch20Style.Muted,true);
            Text("THỐNG KÊ",statX,head,statW-12,Sketch20Style.TableHeaderHeight,9.4f,Sketch20Style.Muted,true);
            Text("KẾT QUẢ",resultX,head,resultW-8,Sketch20Style.TableHeaderHeight,9.4f,Sketch20Style.Muted,true);
            Text("THAO TÁC",actionX,head,actionW-4,Sketch20Style.TableHeaderHeight,9.0f,Sketch20Style.Muted,true);
            int rowsY=head+34+Sketch20Style.TableOuterPaddingY;
            if(hasScroll) AddTableScroll("history-scroll",layoutWidth-17,rowsY,rowsHeight,rows,Math.Max(1,visible),historyOffset,value=>historyOffset=value);

            for(int i=0;i<visible;i++)
            {
                var row=Model.History[historyOffset+i]; int top=rowsY+i*(rowH+rowGap),index=row.Index,centerY=top+rowH/2;
                Card("history-row-"+i,rowX,top,rowW,rowH,Color.White,ColorTranslator.FromHtml("#e5edf8"),11);
                TwoLineText(row.Date,row.Time,rowX+14,top+(rowH-(Sketch20Style.TitleLineHeight+Sketch20Style.TextStackGap+Sketch20Style.BodyLineHeight))/2,timeW-26,11.3f,9.6f,Sketch20Style.Ink,Sketch20Style.Muted,true);

                Card("history-type-icon-"+i,typeX,top+(rowH-42)/2,42,42,Sketch20Style.BlueSoft,Color.Transparent,10);
                FilledIcon(row.Type.IndexOf("toàn",StringComparison.OrdinalIgnoreCase)>=0?"house":row.Type.IndexOf("thư",StringComparison.OrdinalIgnoreCase)>=0?"folder":row.Type.IndexOf("tệp",StringComparison.OrdinalIgnoreCase)>=0?"file":"bolt",typeX+11,top+(rowH-20)/2,20,Sketch20Style.Blue);
                int typeTextX = typeX + 54;
                int typeTextW = Math.Max(150, scanTypeW - 64);
                int typeTextY = top + (rowH - (25 + Sketch20Style.TextStackGap + 21)) / 2;
                string scope = !string.IsNullOrEmpty(row.Scope)?row.Scope:row.Type.IndexOf("toàn",StringComparison.OrdinalIgnoreCase)>=0?"Tất cả ổ đĩa và thư mục":"Vị trí được lựa chọn";
                Text(row.Type,typeTextX,typeTextY,typeTextW,25,compactHistory?10.9f:11.5f,Sketch20Style.Ink,true);
                Text(scope,typeTextX,typeTextY+25+Sketch20Style.TextStackGap,typeTextW,21,compactHistory?8.9f:9.4f,Sketch20Style.Muted);

                Color threatInk=row.Risk?Sketch20Style.Red:Sketch20Style.Green;
                int durationX = statX + statW - 68;
                Text(row.Files+" tệp",statX,centerY-22,Math.Max(72,durationX-statX-8),22,10.2f,Sketch20Style.Ink,true);
                Text(row.Threats+" mối đe dọa",statX,centerY+1,Math.Max(105,statW-12),22,9.7f,threatInk,true);
                Icon("clock",durationX,centerY-8,14,Sketch20Style.Muted);
                Text(row.Duration,durationX+18,centerY-12,48,24,9.3f,Sketch20Style.Muted);

                string result=row.Result!=null&&row.Result.IndexOf("phát hiện",StringComparison.OrdinalIgnoreCase)>=0?"Phát hiện":row.Result;
                Badge(result,resultX,top+(rowH-24)/2,row.Risk?"risk":"safe");

                int deleteX = actionX + 2;
                int viewX = actionX + 38;
                var deleteHistory=Button("history-delete-"+i,"",deleteX,top+(rowH-32)/2,30,()=>{if(Actions.Command!=null)Actions.Command("history-delete:"+index);RefreshModel(true);Render();},"trash",false,true,32,true);
                deleteHistory.AccessibleName="Xóa nhật ký";
                Button("history-detail-"+i,"Xem",viewX,top+(rowH-32)/2,Math.Max(50,rowX+rowW-viewX-4),()=>{if(Actions.Command!=null)Actions.Command("history-detail:"+index);RefreshModel(true);if(Model.Sample&&Model.Detail!=null)Go("details");},"",false,false,32,true);
            }
            if(rows==0)
            {
                Card("history-empty",rowX,rowsY,rowW,rowsHeight,Sketch20Style.Background,Color.Transparent,12);
                Card("history-empty-icon",rowX+22,rowsY+26,46,46,Sketch20Style.BlueSoft,Color.Transparent,23);
                Icon("history",rowX+34,rowsY+38,22,Sketch20Style.Blue);
                Text("Không có lịch sử quét phù hợp.",rowX+84,rowsY+24,rowW-110,50,12.5f,Sketch20Style.Muted);
            }

            int footer=rowsY+rowsHeight+Sketch20Style.TableOuterPaddingY;
            Text("Trang "+(Model.Sample?(rows==0?"0 / 0":"1 / 1"):Model.HistoryPage),26,footer+17,layoutWidth-180,24,10.5f,Sketch20Style.Muted);
            var prev=Button("history-prev","",layoutWidth-110,footer+12,34,()=>{historyOffset=0;Command("history-prev");},"chevron-left",false,false,32);prev.Enabled=Model.HistoryPrev;
            var next=Button("history-next","",layoutWidth-62,footer+12,34,()=>{historyOffset=0;Command("history-next");},"chevron-right",false,false,32);next.Enabled=Model.HistoryNext;
            ContentHeight=listY+listH+18;
        }

        private void Settings()
        {
            SettingsTab = 0;
            const int top = 81;
            const int gap = 14;
            const int generalH = 246;

            Card("settings-general", 0, top, layoutWidth, generalH, Color.White, ColorTranslator.FromHtml("#dce7f5"), 16);
            Card("settings-general-icon", 20, top + 16, 42, 42, Sketch20Style.BlueSoft, Color.Transparent, 12);
            Icon("settings", 30, top + 26, 22, Sketch20Style.Blue);
            Text("Thiết lập chung", 76, top + 9, layoutWidth - 100, 30, 18.5f, Sketch20Style.Ink, true);
            Text("Các tùy chọn cơ bản của ứng dụng.", 76, top + 39, layoutWidth - 100, 23, 10.8f, Sketch20Style.Muted);

            string[] keys = { "autostart", "update", "alerts", "samples" };
            string[] names = { "Khởi động cùng Windows", "Tự động cập nhật chữ ký", "Hiển thị thông báo", "Gửi mẫu tệp nghi ngờ" };
            string[] notes = { "Tự động mở ứng dụng khi bật máy tính.", "Kiểm tra cập nhật chữ ký khi khởi động.", "Nhận cảnh báo về mối đe dọa và kết quả quét.", "Cho phép gửi mẫu tệp nghi ngờ để hỗ trợ phân tích." };
            string[] icons = { "monitor", "refresh", "bell", "globe" };
            Color[] colors = { Sketch20Style.Blue, Sketch20Style.Green, Sketch20Style.Red, Sketch20Style.Amber };
            Color[] soft = { Sketch20Style.BlueSoft, Sketch20Style.GreenSoft, Sketch20Style.RedSoft, Sketch20Style.AmberSoft };

            int gridX = 18, gridY = top + 72, cellGap = 12;
            int cellW = (layoutWidth - gridX * 2 - cellGap) / 2;
            const int cellH = 76, cellStepY = 82;
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2, row = i / 2;
                int x = gridX + col * (cellW + cellGap);
                int y = gridY + row * cellStepY;
                Card("setting-row-" + i, x, y, cellW, cellH, ColorTranslator.FromHtml("#fbfdff"), ColorTranslator.FromHtml("#e5edf8"), 12);
                Card("setting-icon-" + i, x + 12, y + 17, 40, 40, soft[i], Color.Transparent, 10);
                Icon(icons[i], x + 22, y + 27, 20, colors[i]);
                int textW = Math.Max(120, cellW - 118);
                TwoLineText(names[i], notes[i], x + 64, y + 10, textW, 11.8f, 9.5f, Sketch20Style.Ink, Sketch20Style.Muted, true, true);
                Switch(keys[i], names[i], x + cellW - 48, y + 25);
            }

            int lowerY = top + generalH + gap;
            const int lowerH = 262;
            int lowerGap = 14;
            int leftW = (layoutWidth - lowerGap) / 2;
            int rightX = leftW + lowerGap;
            int rightW = layoutWidth - rightX;

            Card("data-info", 0, lowerY, leftW, lowerH, Color.White, ColorTranslator.FromHtml("#dce7f5"), 16);
            Card("data-info-icon", 18, lowerY + 16, 40, 40, Sketch20Style.GreenSoft, Color.Transparent, 11);
            Icon("hard-drive", 28, lowerY + 26, 20, Sketch20Style.Green);
            Text("Thông tin ứng dụng", 70, lowerY + 10, leftW - 88, 28, 15.5f, Sketch20Style.Ink, true);
            Text("Dữ liệu và trạng thái hiện tại.", 70, lowerY + 37, leftW - 88, 22, 10.2f, Sketch20Style.Muted);
            int infoY = lowerY + 66;
            InfoRow("Phiên bản", Model.Version, 20, infoY, leftW - 40, 120, 42);
            InfoRow("CSDL chữ ký", Model.Signature, 20, infoY + 42, leftW - 40, 120, 42);
            InfoRow("Đang cách ly", Model.Quarantine.Count + " tệp", 20, infoY + 84, leftW - 40, 120, 42);
            InfoRow("Thư mục dữ liệu", Model.DataFolder, 20, infoY + 126, leftW - 78, 120, 42, true);
            Button("copy-data-folder", "", leftW - 52, infoY + 132, 28, () => { if (!string.IsNullOrWhiteSpace(Model.DataFolder)) Clipboard.SetText(Model.DataFolder); }, "files", false, false, 28, true);

            Card("quick-tools", rightX, lowerY, rightW, lowerH, Color.White, ColorTranslator.FromHtml("#dce7f5"), 16);
            Card("quick-tools-icon", rightX + 18, lowerY + 16, 40, 40, Sketch20Style.BlueSoft, Color.Transparent, 11);
            Icon("bolt", rightX + 28, lowerY + 26, 20, Sketch20Style.Blue);
            Text("Thao tác nhanh", rightX + 70, lowerY + 10, rightW - 88, 28, 15.5f, Sketch20Style.Ink, true);
            Text("Công cụ quản lý dữ liệu ứng dụng.", rightX + 70, lowerY + 37, rightW - 88, 22, 10.2f, Sketch20Style.Muted);

            string[] toolTitles = { "Mở thư mục dữ liệu", "Xóa cache quét", "Tạo tệp mẫu" };
            string[] toolNotes = { "Mở vị trí lưu dữ liệu.", "Xóa dữ liệu tạm của các lần quét.", "Tạo mẫu thử để kiểm tra phát hiện." };
            string[] toolIcons = { "folder-open", "trash", "file-plus" };
            string[] toolButtons = { "Mở", "Xóa", "Tạo" };
            Color[] toolColors = { Sketch20Style.Blue, Sketch20Style.Red, Sketch20Style.Green };
            Color[] toolSoft = { Sketch20Style.BlueSoft, Sketch20Style.RedSoft, Sketch20Style.GreenSoft };
            Action[] toolActions = { () => Command("open-data"), () => Command("clear-cache"), () => Command("samples") };
            int toolY = lowerY + 68;
            for (int i = 0; i < 3; i++)
            {
                int y = toolY + i * 60;
                Card("data-action-" + i, rightX + 16, y, rightW - 32, 54, ColorTranslator.FromHtml("#fbfdff"), ColorTranslator.FromHtml("#e5edf8"), 11);
                Card("data-action-icon-" + i, rightX + 28, y + 9, 36, 36, toolSoft[i], Color.Transparent, 9);
                Icon(toolIcons[i], rightX + 37, y + 18, 18, toolColors[i]);
                Text(toolTitles[i], rightX + 76, y + 5, Math.Max(90, rightW - 196), 23, 11.2f, Sketch20Style.Ink, true);
                Text(toolNotes[i], rightX + 76, y + 27, Math.Max(90, rightW - 196), 19, 9.2f, Sketch20Style.Muted);
                Button("data-action-button-" + i, toolButtons[i], rightX + rightW - 104, y + 11, 76, toolActions[i], toolIcons[i], false, false, 32);
            }

            int footerY = lowerY + lowerH + 12;
            Text("Thay đổi chỉ được áp dụng sau khi lưu.", 4, footerY + 13, Math.Max(160, layoutWidth - 380), 36, 10.2f, Sketch20Style.Muted);
            Button("restore-defaults", "Khôi phục mặc định", layoutWidth - 370, footerY + 7, 190, () => Command("defaults"), "undo", false, false, 40);
            Button("save-settings", "Lưu thiết lập", layoutWidth - 168, footerY + 7, 168, () => Command("save-settings"), "save", true, false, 40);
            ContentHeight = footerY + 54;
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
        private void ExternalDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }
        private void ExternalDragDrop(object sender, DragEventArgs e)
        {
            if (Model.Sample || e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var dropped = ((string[])e.Data.GetData(DataFormats.FileDrop) ?? new string[0])
                .Where(p => File.Exists(p) || Directory.Exists(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (dropped.Count == 0) return;
            var dirs = dropped.Where(Directory.Exists).ToList();
            var pickedFiles = dropped.Where(File.Exists).ToList();
            if (dirs.Count > 0 && pickedFiles.Count == 0)
            {
                AdvancedMode = 1; folders = dirs;
            }
            else if (pickedFiles.Count > 0 && dirs.Count == 0)
            {
                AdvancedMode = 2; files = files.Concat(pickedFiles).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            else
            {
                AdvancedMode = 3; customFolders = customFolders.Concat(dropped).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            if (Page != "advanced") Go("advanced"); else Render();
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
            string sourcePage = Page;
            if (Actions.OpenFile != null) Actions.OpenFile(index, quarantine);
            if (Page != "details") DetailReturnPage = quarantine ? "quarantine" : sourcePage == "threats" ? "threats" : "overview";
            if (Model.Sample) Model.Detail = (quarantine ? Model.Quarantine : Model.Threats).FirstOrDefault(f => f.Index == index) ?? Model.Quarantine.FirstOrDefault();
            else RefreshModel(true);
            DetailTab = 0; Go("details");
        }
        private void DetailInfoStacked(Sketch20File f)
        {
            int mainY = detailBodyY;
            const int fileH = 458;
            Card("detail-info", 0, mainY, layoutWidth, fileH);
            Text("Thông tin tệp", 18, mainY + 12, layoutWidth - 36, 34, 18, Sketch20Style.Ink, true);
            string[] labels = { "Tên tệp", "Đường dẫn đầy đủ", "Kích thước", "Loại tệp", "Mô tả", "Nhà phát hành", "Phiên bản", "Ngày tạo", "Nguồn phát hiện", "Chữ ký số", "Băm SHA-256" };
            string[] values = { f == null ? "—" : f.Name, f == null ? "—" : f.Path, f == null ? "—" : f.Size, f == null ? "—" : f.FileType, "Chưa có thông tin", "Chưa xác minh", "Chưa có thông tin", f == null ? "—" : f.Created, f == null ? "—" : f.Source, "Chưa xác minh", f == null ? "—" : f.Sha256 };
            for (int i = 0; i < labels.Length; i++)
            {
                int yy = mainY + 58 + i * 35;
                InfoRow(labels[i], values[i], 18, yy, layoutWidth - 36, 155, 35, i == labels.Length - 1, i == 10);
            }

            int riskY = mainY + fileH + 14;
            const int riskH = 448;
            Card("detail-risk", 0, riskY, layoutWidth, riskH);
            Text("Đánh giá rủi ro", 18, riskY + 12, layoutWidth - 170, 34, 18, Sketch20Style.Ink, true);
            int badgeW = Sketch20Style.PillWidth(f == null ? "Chưa xác định" : f.Severity, 10.8f * ReferenceTextScale, true);
            Badge(f == null ? "Chưa xác định" : f.Severity, layoutWidth - badgeW - 20, riskY + 17, f == null ? "info" : Kind(f.Severity));

            int bannerY = riskY + 63;
            Card("risk-card", 16, bannerY, layoutWidth - 32, 108, Sketch20Style.RedSoft, Color.Transparent, 11);
            Card("risk-icon", 30, bannerY + 20, 42, 43, Color.White, Color.Transparent, 10);
            Icon("alert", 41, bannerY + 31, 21, Sketch20Style.Red);
            Text(f == null ? "Chưa có đánh giá" : "Tệp có dấu hiệu đáng ngờ", 84, bannerY + 10, layoutWidth - 116, 32, 13.5f, Sketch20Style.Red, true);
            Text(f == null ? "Chọn tệp để xem kết quả." : "Đối chiếu các chỉ dấu, nguồn gốc và kết quả phân tích trước khi sử dụng tệp.", 84, bannerY + 47, layoutWidth - 116, 50, 11.5f, Sketch20Style.Muted, false, true);

            int yyRisk = bannerY + 124;
            InfoRow("Mức độ rủi ro", f == null ? "—" : f.Severity, 18, yyRisk, layoutWidth - 36, 150, 39);
            InfoRow("Phát hiện bởi", Model.VtEngineCount.HasValue ? Model.VtDetections + "/" + Model.VtEngineCount + " công cụ" : "Chưa tra cứu", 18, yyRisk + 39, layoutWidth - 36, 150, 39);
            InfoRow("Loại mối đe dọa", f == null ? "—" : f.Reason, 18, yyRisk + 78, layoutWidth - 36, 150, 39);
            InfoRow("Lần quét gần nhất", f == null ? "—" : f.Date + " " + f.Time, 18, yyRisk + 117, layoutWidth - 36, 150, 39, true);

            int noteY = riskY + riskH - 84;
            Card("recommendation", 16, noteY, layoutWidth - 32, 68, Sketch20Style.AmberSoft, Color.Transparent, 10);
            Icon("info", 31, noteY + 15, 19, Sketch20Style.Amber);
            Text("Khuyến nghị", 63, noteY + 6, layoutWidth - 95, 25, 12.5f, Sketch20Style.Ink, true);
            Text(f != null && f.Resolved ? "Giữ tệp trong vùng cách ly nếu chưa chắc chắn về nguồn gốc." : "Nên cách ly tệp trước khi tiếp tục sử dụng.", 63, noteY + 31, layoutWidth - 95, 28, 11, Sketch20Style.Muted, false, true);
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
        private int VirusTotalHeight() { return 323 + Math.Min(Math.Max(1, FilteredEngines().Count), Math.Max(4, (viewport.Height - detailBodyY - 430) / 46)) * 46 + 66; }
        private void DetailVirusTotal(Sketch20File f, int height)
        {
            int mainY = detailBodyY;
            Card("detail-virustotal", 0, mainY, layoutWidth, height);
            bool hasCounts = Model.VtDetections.HasValue && Model.VtEngineCount.HasValue, hasReport = hasCounts && Model.VtEngineCount.Value > 0, needsKey = !Model.Sample && !Model.KeyConfigured;
            int bad = hasCounts ? Model.VtDetections.Value : 0, total = hasCounts ? Model.VtEngineCount.Value : 0, clean = Math.Max(0, total - bad);
            Text("Kết quả phân tích trên VirusTotal", 22, mainY + 11, layoutWidth - (layoutWidth >= 850 ? 435 : 224), 34, 18, Sketch20Style.Ink, true);
            if (layoutWidth >= 850 && hasReport)
            {
                int updateX = Math.Max(360, layoutWidth - 430);
                int updateW = Math.Max(120, layoutWidth - 228 - updateX);
                Text("Cập nhật: " + Model.VtTime, updateX, mainY + 19, updateW, 22, 9.5f, Sketch20Style.Muted, false, false, true);
            }
            var query = Button("query-virustotal", needsKey ? "Thiếu API key (.env)" : hasReport ? "Tra cứu lại" : "Tra cứu VirusTotal", layoutWidth - 206, mainY + 11, 184, () => { if (!needsKey) Command("vt-query"); }, "refresh", true, false, 37);
            query.Enabled = !needsKey && f != null;
            Card("vt-alert", 22, mainY + 62, layoutWidth - 44, 82, hasReport && bad > 0 ? Sketch20Style.RedSoft : Sketch20Style.BlueSoft, Color.Transparent, 10);
            Icon(hasReport && bad > 0 ? "sigma" : "info", 40, mainY + 84, 33, hasReport && bad > 0 ? Sketch20Style.Red : Sketch20Style.Blue);
            string summary = f == null ? "Chọn một tệp để xem kết quả phân tích." : hasReport ? bad + " trong " + total + " công cụ đánh dấu tệp có dấu hiệu độc hại" : needsKey ? "Thêm VIRUSTOTAL_API_KEY vào file .env rồi mở lại ứng dụng." : "Chưa có kết quả. Hãy bấm Tra cứu VirusTotal.";
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
            int rowX = 22;
            int engineRowW = layoutWidth - 44;
            // Loại phát hiện là cột flex vì nội dung engine có độ dài biến thiên.
            int[] engineCols = ResponsiveTableWidths(engineRowW - 32, 2, 205, 136, 260, 120);
            int c1 = rowX + 16;
            int c2 = c1 + engineCols[0];
            int c3 = c2 + engineCols[1];
            int c4 = c3 + engineCols[2];
            Card("engine-header", rowX, head, engineRowW, Sketch20Style.TableHeaderHeight, ColorTranslator.FromHtml("#f7faff"), Color.Transparent, 9);
            Text("CÔNG CỤ", c1, head, engineCols[0] - 10, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            Text("KẾT QUẢ", c2, head, engineCols[1] - 12, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            Text("LOẠI PHÁT HIỆN", c3, head, engineCols[2] - 12, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            Text("CẬP NHẬT", c4, head, engineCols[3] - 6, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            var all = FilteredEngines();
            const int engineRowH = Sketch20Style.TableCompactRowHeight, engineGap = Sketch20Style.TableRowGap;
            int page = Math.Max(4, (height - 400) / (engineRowH + engineGap));
            engineOffset = Math.Max(0, Math.Min(engineOffset, Math.Max(0, all.Count - page)));
            var rows = all.Skip(engineOffset).Take(page).ToList();
            int rowsY = head + Sketch20Style.TableHeaderHeight + Sketch20Style.TableOuterPaddingY;
            int rowsHeight = Math.Max(engineRowH, page * (engineRowH + engineGap) - engineGap);
            AddTableScroll("engine-scroll", layoutWidth - 37, rowsY, rowsHeight, all.Count, page, engineOffset, value => engineOffset = value);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; int yy = rowsY + i * (engineRowH + engineGap);
                string result = row.Length > 1 ? row[1] : "Chưa có kết quả";
                bool good = result.IndexOf("undetected", StringComparison.OrdinalIgnoreCase) >= 0 || result.IndexOf("không phát hiện", StringComparison.OrdinalIgnoreCase) >= 0;
                Card("vt-engine-row-" + i, rowX, yy, engineRowW, engineRowH, Color.White, ColorTranslator.FromHtml("#e5edf8"), 10);
                Text(row.Length == 0 ? "—" : row[0], c1, yy, engineCols[0] - 10, engineRowH, 11.2f, Sketch20Style.Ink, true);
                string statusText = good ? "Undetected" : "Malicious";
                Color statusInk = good ? Sketch20Style.Green : Sketch20Style.Red;
                int statusW = Math.Min(Math.Max(1, engineCols[1] - 16), Sketch20Style.PillWidth(statusText, 10.2f * ReferenceTextScale, true));
                int statusY = yy + (engineRowH - 24) / 2;
                Card("vt-engine-status-" + i, c2, statusY, statusW, 24, good ? Sketch20Style.GreenSoft : Sketch20Style.RedSoft, Color.Transparent, 12);
                Canvas.Drawing.Add(g => { using (var dot = new SolidBrush(statusInk)) g.FillEllipse(dot, c2 + 8, statusY + 6 + bodyOffset, 12, 12); });
                Icon(good ? "check" : "alert", c2 + 10, statusY + 8, 8, Color.White);
                Text(statusText, c2 + 26, statusY, Math.Max(1, statusW - 34), 24, 10.2f, statusInk, true);
                Text(good ? "—" : result, c3, yy, engineCols[2] - 12, engineRowH, 10.2f, Sketch20Style.Ink);
                Text((Model.VtTime ?? "—").Split(new[] { '·', ',' })[0].Trim(), c4, yy, engineCols[3] - 8, engineRowH, 9.8f, Sketch20Style.Muted);
            }
            if (rows.Count == 0)
            {
                Card("engine-empty", rowX, rowsY, layoutWidth - 44, 76, Sketch20Style.Background, Color.Transparent, 12);
                Text("Chưa có kết quả phù hợp để hiển thị.", 40, rowsY + 20, layoutWidth - 80, 36, 12, Sketch20Style.Muted);
            }
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
            int rowX = 22;
            int stringsRowW = layoutWidth - 44;
            int[] stringCols = ResponsiveTableWidths(stringsRowW - 32, 1, 42, 430, 150);
            int numberX = rowX + 16;
            int stringX = numberX + stringCols[0];
            int classX = stringX + stringCols[1];
            Card("strings-header", rowX, head, stringsRowW, Sketch20Style.TableHeaderHeight, ColorTranslator.FromHtml("#f7faff"), Color.Transparent, 9);
            Text("#", numberX, head, stringCols[0] - 8, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            Text("CHUỖI KÝ TỰ", stringX, head, stringCols[1] - 12, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            Text("PHÂN LOẠI", classX, head, stringCols[2] - 8, Sketch20Style.TableHeaderHeight, 9.4f, Sketch20Style.Muted, true);
            var all = FilteredStrings();
            const int stringRowH = Sketch20Style.TableCompactRowHeight, stringGap = Sketch20Style.TableRowGap;
            int page = Math.Max(1, (height - 154) / (stringRowH + stringGap));
            stringsOffset = Math.Max(0, Math.Min(stringsOffset, Math.Max(0, all.Count - page)));
            var rows = all.Skip(stringsOffset).Take(page).ToList();
            int rowsY = head + Sketch20Style.TableHeaderHeight + Sketch20Style.TableOuterPaddingY;
            int rowsHeight = Math.Max(stringRowH, page * (stringRowH + stringGap) - stringGap);
            AddTableScroll("strings-scroll", layoutWidth - 37, rowsY, rowsHeight, all.Count, page, stringsOffset, value => stringsOffset = value);
            for (int i = 0; i < rows.Count; i++)
            {
                string value = rows[i]; int yy = rowsY + i * (stringRowH + stringGap);
                Card("string-row-" + i, rowX, yy, stringsRowW, stringRowH, Color.White, ColorTranslator.FromHtml("#e5edf8"), 10);
                Text((i + stringsOffset + 1).ToString("00"), numberX, yy, stringCols[0] - 8, stringRowH, 10.3f, Sketch20Style.Muted, true);
                Text(value, stringX, yy, stringCols[1] - 12, stringRowH, 11.2f, Sketch20Style.Ink, false, false, false, true);
                bool technical = new[] { "CreateRemoteThread", "VirtualAllocEx", "LoadLibraryA", "CreateProcessW", "WinExec", "WriteProcessMemory" }.Contains(value);
                bool registry = value.IndexOf(@"CurrentVersion\Run", StringComparison.OrdinalIgnoreCase) >= 0 || value.StartsWith("HKEY_", StringComparison.OrdinalIgnoreCase) || value.StartsWith(@"HKLM\", StringComparison.OrdinalIgnoreCase) || value.StartsWith(@"HKCU\", StringComparison.OrdinalIgnoreCase);
                string kind = technical ? "technical" : value.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("powershell", StringComparison.OrdinalIgnoreCase) >= 0 ? "risk" : registry ? "warn" : value.IndexOf("dll", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0 ? "info" : "neutral";
                Badge(kind == "technical" ? "Kỹ thuật" : kind == "risk" ? "Đáng ngờ" : kind == "warn" ? "Registry" : kind == "info" ? "Hệ thống" : "Tệp nhị phân", classX, yy + (stringRowH - 24) / 2, kind);
            }
            if (rows.Count == 0)
            {
                Card("strings-empty", rowX, rowsY, layoutWidth - 44, 76, Sketch20Style.Background, Color.Transparent, 12);
                Text("Không có chuỗi ký tự phù hợp.", 40, rowsY + 20, layoutWidth - 80, 36, 12, Sketch20Style.Muted);
            }
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
                pendingSearchCaret = input.SelectionStart; pendingSearchName = name;
                change(input.Text);
                searchRenderTimer.Stop(); searchRenderTimer.Start();
            };
            Canvas.Controls.Add(input); Canvas.Nodes[name] = input.Bounds;
            SearchCue(input.Handle, 0x1501, IntPtr.Zero, name == "engine-search" ? "Tìm tên công cụ..." : "Tìm kiếm chuỗi ký tự...");
        }

        private sealed class SketchSelectPopup : Form
        {
            readonly string[] choices;
            readonly Action<int> picked;
            readonly Color accent, accentSoft, border;
            readonly int rowHeight = 34, pad = 6, visible;
            int selected, hover = -1, offset;

            internal SketchSelectPopup(string[] values, int current, int popupWidth, int maxHeight, bool risk, Action<int> onPicked)
            {
                choices = values ?? new string[0]; picked = onPicked;
                selected = Math.Max(0, Math.Min(current, Math.Max(0, choices.Length - 1)));
                accent = risk ? ColorTranslator.FromHtml("#d74d68") : Sketch20Style.Blue;
                accentSoft = risk ? ColorTranslator.FromHtml("#fff0f4") : ColorTranslator.FromHtml("#edf5ff");
                border = risk ? ColorTranslator.FromHtml("#eadce1") : ColorTranslator.FromHtml("#d9e7f7");
                visible = Math.Max(1, Math.Min(choices.Length, Math.Min(8, Math.Max(1, (maxHeight - pad * 2) / rowHeight))));
                offset = Math.Max(0, Math.Min(selected, Math.Max(0, choices.Length - visible)));
                FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
                BackColor = Color.White; KeyPreview = true; DoubleBuffered = true; AutoScaleMode = AutoScaleMode.Dpi;
                ClientSize = new Size(Math.Max(120, popupWidth), pad * 2 + visible * rowHeight);
                Deactivate += delegate { Close(); };
                KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
                Shown += delegate { UpdateShape(); };
                Resize += delegate { UpdateShape(); };
            }

            void UpdateShape()
            {
                if (Width < 2 || Height < 2) return;
                int r = 12, d = r * 2;
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddArc(0, 0, d, d, 180, 90); path.AddArc(Width - d - 1, 0, d, d, 270, 90);
                    path.AddArc(Width - d - 1, Height - d - 1, d, d, 0, 90); path.AddArc(0, Height - d - 1, d, d, 90, 90);
                    path.CloseFigure(); Region = new Region(path);
                }
            }

            int RowAt(Point p)
            {
                if (p.Y < pad || p.Y >= pad + visible * rowHeight) return -1;
                int row = (p.Y - pad) / rowHeight, index = offset + row;
                return index >= 0 && index < choices.Length ? index : -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e); int next = RowAt(e.Location);
                if (next != hover) { hover = next; Invalidate(); }
            }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }
            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                if (choices.Length <= visible) return;
                int next = Math.Max(0, Math.Min(choices.Length - visible, offset + (e.Delta < 0 ? 1 : -1)));
                if (next != offset) { offset = next; hover = -1; Invalidate(); }
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e); int index = RowAt(e.Location); if (index < 0) return;
                selected = index; Close(); if (picked != null) picked(index);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Sketch20Style.Round(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), 12, Color.White, border);
                for (int row = 0; row < visible; row++)
                {
                    int index = offset + row; if (index >= choices.Length) break;
                    int y = pad + row * rowHeight; bool active = index == selected, hot = index == hover;
                    if (active || hot)
                        Sketch20Style.Round(e.Graphics, new Rectangle(6, y + 2, Width - 12, rowHeight - 4), 8,
                            active ? accentSoft : ColorTranslator.FromHtml("#f7faff"), Color.Transparent);
                    Sketch20Style.Text(e.Graphics, choices[index], new Rectangle(16, y, Math.Max(30, Width - 50), rowHeight),
                        10.6f, active ? accent : Sketch20Style.Ink, active);
                    if (active) Sketch20Style.Icon(e.Graphics, "check", new Rectangle(Width - 31, y + 9, 16, 16), accent);
                }
                if (choices.Length > visible)
                {
                    float track = Height - pad * 2f - 8, thumb = Math.Max(18, track * visible / choices.Length);
                    float maxOffset = Math.Max(1, choices.Length - visible), top = pad + 4 + (track - thumb) * offset / maxOffset;
                    using (var b = new SolidBrush(ColorTranslator.FromHtml("#cbd8e8"))) e.Graphics.FillRectangle(b, Width - 5, top, 2, thumb);
                }
            }
        }

        private void MenuSelectBox(string name, string[] choices, int selected, int x, int y, int width, int height, float pixels, Action<int> changed, string leadingIcon = "filter", bool risk = false)
        {
            // Mọi dropdown phải nằm hoàn toàn trong vùng nội dung; luôn chừa mép phải để không có cảm giác tràn app.
            const int outerMargin = 10;
            int right = Math.Max(outerMargin + 1, layoutWidth - outerMargin);
            x = Math.Max(outerMargin, Math.Min(x, right - 1));
            width = Math.Max(1, Math.Min(width, right - x));
            selected = Math.Max(0, Math.Min(selected, Math.Max(0, choices.Length - 1)));
            string value = choices.Length == 0 ? "" : choices[selected];
            bool hover = false, rich = risk || height >= 38;
            Color accent = risk ? ColorTranslator.FromHtml("#d74d68") : Sketch20Style.Blue;
            Color accentSoft = risk ? ColorTranslator.FromHtml("#fff0f4") : Sketch20Style.BlueSoft;
            Color normalEdge = risk ? ColorTranslator.FromHtml("#eadce1") : ColorTranslator.FromHtml("#c7d5e6");
            Color hoverEdge = risk ? ColorTranslator.FromHtml("#efb4c1") : ColorTranslator.FromHtml("#8fbcf7");
            Color hoverFill = risk ? ColorTranslator.FromHtml("#fff9fb") : ColorTranslator.FromHtml("#f7fbff");
            var button = new Sketch20SelectSurface
            {
                Name = name, AccessibleName = value, Bounds = new Rectangle(x, y + bodyOffset, width, height)
            };
            button.MouseEnter += delegate { hover = true; button.Invalidate(); };
            button.MouseLeave += delegate { hover = false; button.Invalidate(); };
            button.Painter = delegate(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color fill = hover ? hoverFill : Color.White;
                Color edge = hover ? hoverEdge : normalEdge;
                Sketch20Style.Round(e.Graphics, new Rectangle(0, 0, Math.Max(1, width - 1), Math.Max(1, height - 1)), rich ? 10 : 9, fill, edge);
                if (rich)
                {
                    int tileSize = height <= 34 ? 24 : 28;
                    int tileY = (height - tileSize) / 2;
                    int iconSize = tileSize <= 24 ? 12 : 14;
                    int iconPad = (tileSize - iconSize) / 2;
                    int textX = 8 + tileSize + 9;
                    int chevronX = width - tileSize - 8;
                    Sketch20Style.Round(e.Graphics, new Rectangle(8, tileY, tileSize, tileSize), 7, accentSoft, Color.Transparent);
                    Sketch20Style.Icon(e.Graphics, leadingIcon, new Rectangle(8 + iconPad, tileY + iconPad, iconSize, iconSize), accent);
                    Sketch20Style.Text(e.Graphics, value, new Rectangle(textX, 0, Math.Max(1, chevronX - textX - 6), height), pixels, Sketch20Style.Ink, true);
                    Sketch20Style.Round(e.Graphics, new Rectangle(chevronX, tileY, tileSize, tileSize), 7,
                        hover ? (risk ? ColorTranslator.FromHtml("#ffe3ea") : ColorTranslator.FromHtml("#deecff"))
                              : (risk ? ColorTranslator.FromHtml("#fff4f6") : ColorTranslator.FromHtml("#f1f6fd")), Color.Transparent);
                    Sketch20Style.Icon(e.Graphics, "chevron-down", new Rectangle(chevronX + (tileSize - 12) / 2, tileY + (tileSize - 12) / 2, 12, 12), hover ? accent : Sketch20Style.Muted);
                }
                else
                {
                    Sketch20Style.Text(e.Graphics, value, new Rectangle(12, 0, Math.Max(1, width - 36), height), pixels, Sketch20Style.Ink);
                    Sketch20Style.Icon(e.Graphics, "chevron-down", new Rectangle(width - 22, (height - 12) / 2, 12, 12), hover ? Sketch20Style.Blue : Sketch20Style.Muted);
                }
            };
            Rectangle screenWork = Screen.FromControl(button).WorkingArea;
            Rectangle appWork = viewport.RectangleToScreen(viewport.ClientRectangle);
            Rectangle work = Rectangle.Intersect(screenWork, appWork);
            if (work.Width < 80 || work.Height < 80) work = screenWork;
            int naturalMenuW = Math.Max(150, width);
            for (int k = 0; k < choices.Length; k++)
                naturalMenuW = Math.Max(naturalMenuW, Sketch20Style.Measure(choices[k], 10.6f, k == selected) + 48);
            int popupW = Math.Min(naturalMenuW, Math.Min(320, Math.Max(120, work.Width - 16)));
            button.Click += delegate
            {
                var popup = new SketchSelectPopup(choices, selected, popupW, Math.Max(60, work.Height - 20), risk, index =>
                {
                    value = choices[index]; selected = index; button.AccessibleName = value; button.Invalidate();
                    if (changed != null) changed(index);
                });
                Point below = button.PointToScreen(new Point(0, button.Height + 4));
                int px = Math.Max(work.Left + 4, Math.Min(below.X, work.Right - popup.Width - 4));
                int py = below.Y;
                if (py + popup.Height > work.Bottom - 4)
                    py = Math.Max(work.Top + 4, button.PointToScreen(new Point(0, -popup.Height - 4)).Y);
                popup.Location = new Point(px, py);
                Form owner = WindowForm ?? FindForm();
                if (owner == null) popup.Show(); else popup.Show(owner);
            };
            Canvas.Controls.Add(button); Canvas.Nodes[name] = button.Bounds;
        }

        private void HeaderSelectBox(string name, string[] choices, int selected, int x, int y, int width, int height, float pixels, Action<int> changed)
        { MenuSelectBox(name, choices, selected, x, y, width, height, pixels, changed); }

        private void SelectBox(string name, string[] choices, int selected, int x, int y, int width, Action<int> changed, int height = 43)
        { MenuSelectBox(name, choices, selected, x, y, width, height, 11.5f, changed); }

        private void ThreatSelectBox(string name, string[] choices, int selected, int x, int y, int width, Action<int> changed)
        { MenuSelectBox(name, choices, selected, x, y, width, 32, 10.6f, changed, "alert", true); }

        private void DateBox(string name, DateTime date, int x, int y, int width, Action<DateTime> changed)
        {
            const int height = 42, outerMargin = 10;
            int right = Math.Max(outerMargin + 1, layoutWidth - outerMargin);
            x = Math.Max(outerMargin, Math.Min(x, right - 1));
            width = Math.Max(1, Math.Min(width, right - x));
            DateTime value = date.Date;
            bool hover = false;
            var button = new Sketch20SelectSurface
            {
                Name = name, AccessibleName = value.ToString("dd/MM/yyyy"), Bounds = new Rectangle(x, y + bodyOffset, width, height)
            };
            button.MouseEnter += delegate { hover = true; button.Invalidate(); };
            button.MouseLeave += delegate { hover = false; button.Invalidate(); };
            button.Painter = delegate(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color fill = hover ? ColorTranslator.FromHtml("#f7fbff") : Color.White;
                Color edge = hover ? ColorTranslator.FromHtml("#8fbcf7") : ColorTranslator.FromHtml("#c7d5e6");
                Sketch20Style.Round(e.Graphics, new Rectangle(0, 0, Math.Max(1, width - 1), height - 1), 11, fill, edge);
                int tileY = (height - 28) / 2;
                Sketch20Style.Round(e.Graphics, new Rectangle(8, tileY, 28, 28), 8, Sketch20Style.BlueSoft, Color.Transparent);
                Sketch20Style.Icon(e.Graphics, "calendar", new Rectangle(15, tileY + 7, 14, 14), Sketch20Style.Blue);
                Sketch20Style.Text(e.Graphics, value.ToString("dd/MM/yyyy"), new Rectangle(47, 0, Math.Max(1, width - 91), height), 11.5f, Sketch20Style.Ink, true);
                int chevronX = width - 37;
                Sketch20Style.Round(e.Graphics, new Rectangle(chevronX, tileY, 28, 28), 8,
                    hover ? ColorTranslator.FromHtml("#deecff") : ColorTranslator.FromHtml("#f1f6fd"), Color.Transparent);
                Sketch20Style.Icon(e.Graphics, "chevron-down", new Rectangle(chevronX + 8, tileY + 8, 12, 12), hover ? Sketch20Style.Blue : Sketch20Style.Muted);
            };
            var calendar = new MonthCalendar { MaxSelectionCount = 1, SelectionStart = value, SelectionEnd = value, ShowToday = true };
            var host = new ToolStripControlHost(calendar) { AutoSize = false, Size = calendar.Size, Margin = Padding.Empty, Padding = Padding.Empty };
            var menu = new ContextMenuStrip { ShowImageMargin = false, Padding = Padding.Empty };
            menu.Items.Add(host);
            calendar.DateSelected += delegate(object sender, DateRangeEventArgs e)
            {
                value = e.Start.Date; button.AccessibleName = value.ToString("dd/MM/yyyy"); button.Invalidate();
                if (changed != null) changed(value); menu.Close();
            };
            button.Click += delegate
            {
                calendar.SetDate(value);
                Rectangle screenWork = Screen.FromControl(button).WorkingArea;
                Rectangle appWork = viewport.RectangleToScreen(viewport.ClientRectangle);
                Rectangle work = Rectangle.Intersect(screenWork, appWork);
                if (work.Width < 80 || work.Height < 80) work = screenWork;
                Size popup = menu.GetPreferredSize(Size.Empty);
                Point below = button.PointToScreen(new Point(0, button.Height + 4));
                int px = Math.Max(work.Left + 4, Math.Min(below.X, work.Right - popup.Width - 4));
                int py = below.Y;
                if (py + popup.Height > work.Bottom - 4)
                    py = Math.Max(work.Top + 4, button.PointToScreen(new Point(0, -popup.Height - 4)).Y);
                menu.Show(new Point(px, py));
            };
            button.Disposed += delegate { menu.Dispose(); };
            button.ContextMenuStrip = menu;
            Canvas.Controls.Add(button); Canvas.Nodes[name] = button.Bounds;
        }
        private static string NormalizeHistoryFilter(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Tất cả";
            string[] known = { "Tất cả", "Quét nhanh", "Quét thư mục", "Quét tệp", "Quét toàn bộ", "Quét tùy chỉnh" };
            foreach (string item in known)
                if (string.Equals(value, item, StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith(item, StringComparison.OrdinalIgnoreCase)) return item;
            return "Tất cả";
        }

        private void PasswordBox(int x, int y, int width)
        {
            var input = new Sketch20Password { Name = "api-key", Bounds = new Rectangle(x, y + bodyOffset, width, 40) };
            input.Placeholder = Model.KeyConfigured ? "Đã lưu API key · Nhập key mới để thay thế" : "Nhập API key VirusTotal";
            input.Entry.Text = typedKey; input.Entry.TextChanged += delegate { typedKey = input.Entry.Text; }; Canvas.Controls.Add(input); Canvas.Nodes[input.Name] = input.Bounds;
        }
    }
}
