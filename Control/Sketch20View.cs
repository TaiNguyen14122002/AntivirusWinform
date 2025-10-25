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
        internal static readonly string[] ScreenNames = { "Tổng quan", "Bảo vệ", "Lịch sử", "Cách ly", "Cài đặt · Chung", "Quét toàn bộ hệ thống", "Chi tiết · Thông tin", "Chi tiết · VirusTotal", "Chi tiết · Hành vi", "Chi tiết · Chuỗi ký tự", "Chi tiết · Thông tin bổ sung", "Cài đặt · Bảo vệ", "Cài đặt · VirusTotal", "Cài đặt · Dữ liệu", "Quét thư mục", "Quét tệp", "Quét nâng cao", "Đang quét", "Phát hiện mối đe dọa", "Bảo vệ tắt" };
        private readonly Panel sidebar, viewport;
        private UcQuanTri adminPage;
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
        // Quét nâng cao không dùng thanh cuộn hệ thống. Khi màn hình nhỏ hoặc
        // danh sách dài, lăn chuột sẽ dịch nội dung mà không hiện thanh cuộn.
        private int advancedScrollOffset;
        // Quét nâng cao, Cách ly và Lịch sử cuộn bằng con lăn khi cửa sổ quá thấp,
        // nhưng không vẽ scrollbar WinForms bên ngoài.
        private int quarantinePageScrollOffset;
        // Trang Chi tiết tệp không dùng thanh cuộn toàn trang; màn nhỏ cuộn bằng con lăn.
        private int detailPageScrollOffset;
        private const int AdvancedCanvasTop = 29;
        private int detailBodyY = 306;
        // Kích thước tập trung tại một nơi để sidebar dễ bảo trì / responsive.
        private const int ExpandedSidebarWidth = 264;
        private const int CollapsedSidebarWidth = 80;
        private const int SidebarNavStart = 144;
        private const int SidebarNavStep = 57;
        private const int SidebarNavHeight = 48;
        private const int SidebarNavCount = 7;
        private const int SidebarFooterHeight = 56;
        private int sidebarStatusTop;
        private int sidebarStatusHeight;
        private Button hoveredSidebarNav;
        private bool CompactSidebar { get { return ClientSize.Width < 1180; } }
        private int CurrentSidebarNavStart { get { return CompactSidebar ? 124 : SidebarNavStart; } }
        private DateTime? scanningSince;
        private DateTime lastScanRenderUtc = DateTime.MinValue;
        // Chỉ cập nhật sidebar khi thực sự đổi chế độ hoặc trang; tránh vẽ lại cả sidebar
        // trong từng lần cập nhật tiến trình quét.
        private int appliedSidebarWidth = -1;
        private string paintedSidebarState;
        // Control này tồn tại xuyên qua các lần dựng lại trang (~1 giây/lần).
        // Hoạt ảnh chỉ vẽ lại một dải nhỏ, không làm mới toàn bộ cây WinForms.
        private readonly ScanSweepProgressBar scanSweepProgress;
        private int stringsOffset, engineOffset, historyOffset, quarantineOffset, threatOffset;
        private int quarantinePageSize = 10, quarantineRowsScrollPixels;
        private bool quarantineOldest, historyOldest;
        private readonly HashSet<int> quarantineSelection = new HashSet<int>();
        private List<string> folders = new List<string>(), files = new List<string>(), customFolders = new List<string>();
        private bool[] customLocations = { true, true, false, false };
        private int folderListPage;
        private int fileListPage;
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
            Font = Theme.DefaultUiFont;
            sidebar = new Panel { Dock = DockStyle.Left, Width = ExpandedSidebarWidth, BackColor = Sketch20Style.Sidebar };
            Theme.BatDoubleBuffer(this);
            Theme.BatDoubleBuffer(sidebar); sidebar.Paint += DrawSidebar;
            sidebar.Resize += delegate { LayoutSidebarBottom(); sidebar.Invalidate(); };
            viewport = new Panel { Name = "Sketch20Viewport", Dock = DockStyle.Fill, AutoScroll = true, BackColor = Sketch20Style.Background };
            Theme.BatDoubleBuffer(viewport);
            Canvas = new Sketch20Surface { Name = "Sketch20Canvas", Location = new Point(26, 29) };
            scanSweepProgress = new ScanSweepProgressBar { Name = "scan-sweep-progress", Visible = false };
            Canvas.Controls.Add(scanSweepProgress);
            viewport.Controls.Add(Canvas); Controls.Add(viewport); Controls.Add(sidebar);
            AllowDrop = viewport.AllowDrop = Canvas.AllowDrop = true;
            DragEnter += ExternalDragEnter; viewport.DragEnter += ExternalDragEnter; Canvas.DragEnter += ExternalDragEnter;
            DragDrop += ExternalDragDrop; viewport.DragDrop += ExternalDragDrop; Canvas.DragDrop += ExternalDragDrop;
            MouseWheel += TableMouseWheel; sidebar.MouseWheel += TableMouseWheel; viewport.MouseWheel += TableMouseWheel; Canvas.MouseWheel += TableMouseWheel;
            layoutRenderTimer = new Timer { Interval = 55 };
            layoutRenderTimer.Tick += delegate { layoutRenderTimer.Stop(); if (!rendering && !IsDisposed) Render(); };
            searchRenderTimer = new Timer { Interval = 230 };
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
            // Model vẫn cập nhật theo thời gian thực nhưng không phá rồi tạo lại hàng trăm
            // control mỗi 350 ms. Quét có animation riêng; không cần full render 3 fps.
            update = new Timer { Interval = 1000 };
            update.Tick += delegate
            {
                if (IsDisposed || !Visible) return;
                Form owner = FindForm();
                if (owner != null && owner.WindowState == FormWindowState.Minimized) return;
                int interval = Model != null && Model.Scanning && Page == "overview" ? 750 : 1200;
                if (update.Interval != interval) update.Interval = interval;
                RefreshModel();
            };
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

        internal void SetAdminPage(UcQuanTri page)
        {
            adminPage = page;
            if (page == null) return;
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            viewport.Controls.Add(page);
            page.BringToFront();
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
            AdvancedMode = number >= 15 && number <= 16 ? number - 14 : 0;
            Page = number == 2 ? "protection" : number == 3 ? "history" : number == 4 ? "quarantine" : number == 5 || number >= 12 && number <= 14 ? "settings" : number == 6 || number >= 15 && number <= 17 ? "advanced" : number >= 7 && number <= 11 ? "details" : "overview";
            advancedScrollOffset = 0;
            quarantinePageScrollOffset = 0;
            detailPageScrollOffset = 0;
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
            if (state.Page == "admin" && !AdminSecurity.EnsureLogin(FindForm() ?? (IWin32Window)this)) return;
            Page = state.Page; DetailReturnPage = state.DetailReturnPage;
            advancedScrollOffset = 0;
            quarantinePageScrollOffset = 0;
            detailPageScrollOffset = 0;
            SettingsTab = state.SettingsTab; DetailTab = state.DetailTab; AdvancedMode = Math.Max(0, Math.Min(2, state.AdvancedMode));
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
            if (string.Equals(page, "admin", StringComparison.OrdinalIgnoreCase) &&
                !AdminSecurity.EnsureLogin(FindForm() ?? (IWin32Window)this)) return;
            if (!restoringNavigation && !string.Equals(Page, page, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(page, "overview", StringComparison.OrdinalIgnoreCase))
                    navigation.Clear();
                else
                    navigation.Push(CurrentNavigationState());
            }

            Page = page;
            if (page == "admin" && adminPage != null) adminPage.Activated();
            advancedScrollOffset = 0;
            quarantinePageScrollOffset = 0;
            detailPageScrollOffset = 0;
            if (page == "history") { historyOffset = 0; historyRowsScrollPixels = 0; }
            if (page == "quarantine") quarantineOffset = 0;
            if (page == "threats") { threatOffset = 0; threatRowsScrollPixels = 0; threatSelected.Clear(); }
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
        // Khi xuất hiện bản ghi lịch sử mới, ưu tiên đầu danh sách
        // kể cả khi người dùng đang xem trang con của 10 bản ghi SQL.
        internal void ResetHistoryDisplayPage()
        {
            historyOffset = 0;
            historyRowsScrollPixels = 0;
        }

        internal void RefreshModel(bool force = false)
        {
            if (Page == "admin") return;
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
                // control liên tục gây rung/flicker; giới hạn full render ở ~350ms khi đang quét.
                if (!force && Page == "overview" && next.Scanning && wasScanning)
                {
                    DateTime now = DateTime.UtcNow;
                    if ((now - lastScanRenderUtc).TotalMilliseconds < 650) return;
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
            if (Page == "history")
            {
                Point pointer = Canvas.PointToClient(System.Windows.Forms.Control.MousePosition);
                var historyBar = Canvas.Controls.OfType<VScrollBar>()
                    .FirstOrDefault(c => c.Name == "history-rows-scroll");
                if (historyBar != null && new Rectangle(14, historyBar.Top, layoutWidth - 25, historyBar.Height).Contains(pointer))
                {
                    int historyMax = Math.Max(0, historyBar.Maximum - historyBar.LargeChange + 1);
                    historyBar.Value = Math.Max(0, Math.Min(historyMax, historyBar.Value + (e.Delta < 0 ? 72 : -72)));
                    var handledHistory = e as HandledMouseEventArgs;
                    if (handledHistory != null) handledHistory.Handled = true;
                }
                return;
            }
            if (Page == "details")
            {
                Point pointer = Canvas.PointToClient(System.Windows.Forms.Control.MousePosition);
                var inner = Canvas.Controls.OfType<VScrollBar>().FirstOrDefault(control =>
                    control.Name == "engine-scroll" || control.Name == "strings-scroll");
                bool overList = inner != null &&
                    new Rectangle(0, inner.Top, Math.Max(1, layoutWidth), inner.Height).Contains(pointer);
                int innerMax = inner == null ? 0 : Math.Max(0, inner.Maximum - inner.LargeChange + 1);
                if (overList && innerMax > 0)
                {
                    int step = SystemInformation.MouseWheelScrollLines <= 0 ? 1 :
                        Math.Min(3, SystemInformation.MouseWheelScrollLines);
                    int detailNextOffset = Math.Max(0, Math.Min(innerMax, inner.Value + (e.Delta < 0 ? step : -step)));
                    if (detailNextOffset != inner.Value)
                    {
                        SetTableOffset(inner.Name, detailNextOffset);
                        Render();
                    }
                }
                else
                {
                    // Chỉ dành cho cửa sổ quá thấp hoặc tab Thông tin xếp một cột.
                    // Không xuất hiện thanh cuộn ngoài, nhưng không làm mất thông tin.
                    int maxScroll = Math.Max(0, ContentHeight + AdvancedCanvasTop + 6 - viewport.ClientSize.Height);
                    int step = Math.Max(48, 28 * Math.Max(1, SystemInformation.MouseWheelScrollLines));
                    int delta = e.Delta < 0 ? step : -step;
                    detailPageScrollOffset = Math.Max(0, Math.Min(maxScroll, detailPageScrollOffset + delta));
                    Canvas.Top = AdvancedCanvasTop - detailPageScrollOffset;
                }
                var handledDetail = e as HandledMouseEventArgs;
                if (handledDetail != null) handledDetail.Handled = true;
                return;
            }
            if (Page == "advanced" || Page == "quarantine")
            {
                // Không hiển thị thanh cuộn ngoài ở Quét nâng cao/Cách ly/Lịch sử.
                // Màn hình quá thấp vẫn sử dụng con lăn để tới mọi nút bên dưới.
                Point pointer = viewport.PointToClient(System.Windows.Forms.Control.MousePosition);
                if (!viewport.ClientRectangle.Contains(pointer)) return;
                int maxScroll = Math.Max(0, ContentHeight + AdvancedCanvasTop + 6 - viewport.ClientSize.Height);
                int wheelLines = SystemInformation.MouseWheelScrollLines;
                int pageWheelStep = wheelLines < 0 ? viewport.ClientSize.Height / 2 : 28 * Math.Max(1, wheelLines);
                int move = -(int)Math.Round((double)e.Delta / 120 * Math.Max(32, pageWheelStep));
                if (Page == "quarantine")
                {
                    // Ưu tiên cuộn các dòng cách ly trong bảng; giữ nguyên tiêu đề và phân trang.
                    Point canvasPointer = Canvas.PointToClient(System.Windows.Forms.Control.MousePosition);
                    var quarantineInnerBar = Canvas.Controls.OfType<VScrollBar>()
                        .FirstOrDefault(control => control.Name == "quarantine-rows-scroll");
                    Rectangle rowsArea;
                    bool isOverRows = Canvas.Nodes.TryGetValue("quarantine-rows-clip", out rowsArea) &&
                        rowsArea.Contains(canvasPointer);
                    if (quarantineInnerBar != null && isOverRows && quarantineInnerBar.Enabled)
                    {
                        int quarantineInnerMax = Math.Max(0, quarantineInnerBar.Maximum - quarantineInnerBar.LargeChange + 1);
                        quarantineInnerBar.Value = Math.Max(0, Math.Min(quarantineInnerMax,
                            quarantineInnerBar.Value + (e.Delta < 0 ? 3 * 70 : -3 * 70)));
                    }
                    else
                    {
                        quarantinePageScrollOffset = Math.Max(0, Math.Min(maxScroll, quarantinePageScrollOffset + move));
                        Canvas.Top = AdvancedCanvasTop - quarantinePageScrollOffset;
                    }
                }
                else
                {
                    advancedScrollOffset = Math.Max(0, Math.Min(maxScroll, advancedScrollOffset + move));
                    Canvas.Top = AdvancedCanvasTop - advancedScrollOffset;
                }
                var handledWheel = e as HandledMouseEventArgs;
                if (handledWheel != null) handledWheel.Handled = true;
                return;
            }
            Point mouse = Canvas.PointToClient(System.Windows.Forms.Control.MousePosition);
            VScrollBar bar = null;
            foreach (VScrollBar candidate in Canvas.Controls.OfType<VScrollBar>())
            {
                var tableArea = new Rectangle(0, candidate.Top, Math.Max(1, layoutWidth), candidate.Height);
                if (tableArea.Contains(mouse)) { bar = candidate; break; }
            }
            if (bar == null) return;

            int max = Math.Max(0, bar.Maximum - bar.LargeChange + 1);
            if (max <= 0) return;
            int tableWheelStep = SystemInformation.MouseWheelScrollLines <= 0 ? 1 : Math.Min(3, SystemInformation.MouseWheelScrollLines);
            // VScrollBar của Mối đe dọa dùng đơn vị pixel; cuộn theo hàng để tránh
            // chỉ nhích vài pixel sau mỗi nấc con lăn.
            int wheelStep = bar.Name == "threat-rows-scroll" ? tableWheelStep * 48 : tableWheelStep;
            int next = Math.Max(0, Math.Min(max, bar.Value + (e.Delta < 0 ? wheelStep : -wheelStep)));
            if (next == bar.Value) return;

            // Mối đe dọa: cuộn danh sách BÊN TRONG trang hiện tại, không đổi trang
            // và không rebuild cây control ở mỗi nấc con lăn.
            if (bar.Name == "threat-rows-scroll")
                bar.Value = next;
            else
            {
                SetTableOffset(bar.Name, next);
                Render();
            }
            var handled = e as HandledMouseEventArgs;
            if (handled != null) handled.Handled = true;
        }

        private void SetTableOffset(string name, int value)
        {
            if (name == "quarantine-scroll") quarantineOffset = value;
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
            string[] names = { "Tổng quan", "Bảo vệ", "Cách ly", "Mối đe dọa", "Lịch sử", "Quản trị", "Cài đặt" };
            string[] pages = { "overview", "protection", "quarantine", "threats", "history", "admin", "settings" };
            for (int i = 0; i < names.Length; i++)
            {
                string page = pages[i];
                var nav = new Button { Name = "nav-" + page, Text = names[i], AccessibleName = names[i], FlatStyle = FlatStyle.Flat,
                    BackColor = Sketch20Style.Sidebar, ForeColor = Color.Transparent, Cursor = Cursors.Hand, TabStop = false,
                    Bounds = new Rectangle(14, SidebarNavStart + i * SidebarNavStep, ExpandedSidebarWidth - 28, SidebarNavHeight) };
                nav.FlatAppearance.BorderSize = 0;
                // Navbar không đổi nền khi hover/nhấn; chỉ trạng thái active mới được tô màu.
                nav.FlatAppearance.MouseOverBackColor = Sketch20Style.Sidebar;
                nav.FlatAppearance.MouseDownBackColor = Sketch20Style.Sidebar;
                nav.UseVisualStyleBackColor = false;
                nav.Paint += delegate(object sender, PaintEventArgs e) { DrawNav(e.Graphics, (Button)sender, page); };
                nav.MouseEnter += delegate { hoveredSidebarNav = nav; nav.Invalidate(); };
                nav.MouseLeave += delegate { if (hoveredSidebarNav == nav) hoveredSidebarNav = null; nav.Invalidate(); };
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
                var item = new Sketch20Button(sectionNames[i], sectionIcons[i]) { Name = "settings-section-" + i, Tab = true, SquareCorners = true, TextPixels = 12, Bounds = new Rectangle(14, SidebarNavStart + SidebarNavCount * SidebarNavStep + 20 + i * 46, ExpandedSidebarWidth - 28, 40), Visible = false };
                item.Click += delegate { SettingsTab = index; Go("settings"); };
                sidebar.Controls.Add(item);
            }
            BringSidebarStatusToFront();
        }

        private void UpdateResponsiveSidebar()
        {
            int targetWidth = CompactSidebar ? CollapsedSidebarWidth : ExpandedSidebarWidth;
            // Render() được gọi cả khi tiến trình quét cập nhật. Không đặt lại
            // Bounds/ToolTip của sidebar nếu chế độ responsive không đổi.
            if (appliedSidebarWidth == targetWidth && sidebar.Width == targetWidth) return;
            appliedSidebarWidth = targetWidth;
            if (sidebar.Width != targetWidth) sidebar.Width = targetWidth;
            string[] pages = { "overview", "protection", "quarantine", "threats", "history", "admin", "settings" };
            for (int i = 0; i < pages.Length; i++)
            {
                var nav = sidebar.Controls["nav-" + pages[i]] as Button;
                if (nav == null) continue;
                nav.Bounds = CompactSidebar
                    ? new Rectangle(9, CurrentSidebarNavStart + i * SidebarNavStep, targetWidth - 18, SidebarNavHeight)
                    : new Rectangle(14, SidebarNavStart + i * SidebarNavStep, targetWidth - 28, SidebarNavHeight);
                toolTips.SetToolTip(nav, nav.Text);
            }
            var device = sidebar.Controls["sidebar-device"];
            if (device != null)
                toolTips.SetToolTip(device, CompactSidebar ? "Thiết bị: " + Environment.MachineName : null);
            LayoutSidebarBottom();
            sidebar.Invalidate();
        }

        private void LayoutSidebarBottom()
        {
            int menuBottom = CurrentSidebarNavStart + (SidebarNavCount - 1) * SidebarNavStep + SidebarNavHeight;
            var device = sidebar.Controls["sidebar-device"];
            if (device == null) return;

            if (CompactSidebar)
            {
                sidebarStatusHeight = 66;
                sidebarStatusTop = Math.Max(menuBottom + 12, sidebar.ClientSize.Height - sidebarStatusHeight - 24);
                device.Visible = sidebarStatusTop + sidebarStatusHeight <= sidebar.ClientSize.Height - 12;
                device.Bounds = new Rectangle(13, sidebarStatusTop + 8, CollapsedSidebarWidth - 26, 50);
            }
            else
            {
                // Thẻ thiết bị có lề hai bên bằng nhau và chiều cao thích ứng.
                // Khi không đủ chỗ, đổi sang bản tóm tắt để tránh đè lên menu.
                int available = sidebar.ClientSize.Height - SidebarFooterHeight - (menuBottom + 42);
                sidebarStatusHeight = available < 238 ? 114 : Math.Min(296, available - 10);
                int bottom = sidebar.ClientSize.Height - SidebarFooterHeight - 12;
                sidebarStatusTop = Math.Max(menuBottom + 30, bottom - sidebarStatusHeight);
                bool enoughSpace = sidebarStatusTop + sidebarStatusHeight <=
                    sidebar.ClientSize.Height - SidebarFooterHeight - 8;
                device.Visible = enoughSpace;
                // Viền thẻ: x=14 và right=14. Nội dung: thêm 8 px mỗi bên,
                // thêm 12 px trên/dưới để thông số không sát đường viền.
                device.Bounds = new Rectangle(22, sidebarStatusTop + 12,
                    ExpandedSidebarWidth - 44, sidebarStatusHeight - 24);
            }
            BringSidebarStatusToFront();
        }

        private void BringSidebarStatusToFront()
        {
            var item = sidebar.Controls["sidebar-device"];
            // Không thực hiện Z-order operation ở mỗi frame nếu đã đứng trên cùng.
            if (item != null && sidebar.Controls.GetChildIndex(item) != 0)
                item.BringToFront();
        }

        private void DrawNav(Graphics g, Button button, string page)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bool active = page == Page || page == "overview" && (Page == "advanced" || Page == "details" && DetailReturnPage == "overview") || page == "threats" && Page == "details" && DetailReturnPage == "threats" || page == "quarantine" && Page == "details" && DetailReturnPage == "quarantine" || page == "history" && Page == "details" && DetailReturnPage == "history";
            // Giữ góc vuông của tất cả nút; hover nhẹ, active xanh rõ ràng.
            Color background = active ? Sketch20Style.BlueSoft :
                button == hoveredSidebarNav ? ColorTranslator.FromHtml("#e8f2ff") : Sketch20Style.Sidebar;
            using (var brush = new SolidBrush(background))
                g.FillRectangle(brush, button.ClientRectangle);
            if (active)
                using (var b = new SolidBrush(Sketch20Style.Blue))
                    g.FillRectangle(b, 0, 10, 4, button.Height - 20);
            Color ink = active ? Sketch20Style.Blue : Sketch20Style.Ink;
            string icon = page == "overview" ? "house" : page == "protection" ? "shield" : page == "quarantine" ? "archive" : page == "threats" ? "shield-alert" : page == "history" ? "history" : page == "admin" ? "shield-check" : "settings";
            int iconSize = 21;
            int iconX = CompactSidebar ? (button.Width - iconSize) / 2 : 19;
            int iconY = (button.Height - iconSize) / 2;
            Sketch20ReferenceAssets.FilledIcon(g, page == "protection" ? "shield-check" : page == "quarantine" ? "cube" : page == "history" ? "clock" : icon,
                new Rectangle(iconX, iconY, iconSize, iconSize), active ? Sketch20Style.Blue : Sketch20Style.Muted);
            int badgeW = 0;
            string countText = page == "quarantine" && Model.Quarantine.Count > 0 ? Model.Quarantine.Count.ToString() :
                page == "threats" && Model.Threats.Count > 0 ? Model.Threats.Count.ToString("N0") : null;
            if (countText != null)
                badgeW = Math.Max(20, Sketch20Style.Measure(countText, CompactSidebar ? 8.5f : 10.3f, true) + 10);
            if (!CompactSidebar)
                Sketch20Style.Text(g, button.Text,
                    new Rectangle(58, 0, Math.Max(50, button.Width - 70 - (badgeW > 0 ? badgeW + 10 : 0)), button.Height),
                    14.5f, ink, active);
            if (badgeW > 0)
            {
                int badgeH = CompactSidebar ? 18 : 21;
                int badgeX = button.Width - badgeW - (CompactSidebar ? 2 : 13);
                int badgeY = CompactSidebar ? 3 : (button.Height - badgeH) / 2;
                Sketch20Style.Round(g, new Rectangle(badgeX, badgeY, badgeW, badgeH), badgeH / 2,
                    active ? Color.White : Sketch20Style.BlueSoft, Color.Transparent);
                Sketch20Style.Text(g, countText, new Rectangle(badgeX, badgeY, badgeW, badgeH),
                    CompactSidebar ? 8.5f : 10.3f, Sketch20Style.Blue, true, false, false, false, true);
            }
        }

        private void DrawSidebar(object sender, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(Sketch20Style.Sidebar)) g.FillRectangle(bg, e.ClipRectangle);
            using (var pen = new Pen(Sketch20Style.Border))
                g.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);

            if (CompactSidebar)
            {
                Sketch20ReferenceAssets.Logo(g, new Rectangle(25, 27, 29, 34));
                using (var pen = new Pen(Sketch20Style.Border))
                    g.DrawLine(pen, 13, 102, sidebar.Width - 13, 102);
                var deviceCard = sidebar.Controls["sidebar-device"];
                if (deviceCard != null && deviceCard.Visible)
                    Sketch20Style.Round(g, new Rectangle(9, sidebarStatusTop, sidebar.Width - 18,
                        sidebarStatusHeight), 12, Color.White, Sketch20Style.Border);
                return;
            }

            // Header rõ ràng, phân nhóm menu, thẻ thiết bị tách biệt với điều hướng.
            Sketch20ReferenceAssets.Logo(g, new Rectangle(25, 25, 33, 39));
            Sketch20Style.Text(g, "Antivirus", new Rectangle(74, 23, sidebar.Width - 91, 38),
                21f, Sketch20Style.Ink, true);
            Sketch20Style.Text(g, "BẢO VỆ MÁY TÍNH", new Rectangle(75, 57, sidebar.Width - 96, 19),
                9.5f, Sketch20Style.Muted, true);
            using (var pen = new Pen(Sketch20Style.Border))
            {
                g.DrawLine(pen, 22, 100, sidebar.Width - 22, 100);
                g.DrawLine(pen, 22, sidebar.Height - SidebarFooterHeight, sidebar.Width - 22,
                    sidebar.Height - SidebarFooterHeight);
            }
            Sketch20Style.Text(g, "ĐIỀU HƯỚNG", new Rectangle(26, 113, sidebar.Width - 52, 20),
                10.3f, Sketch20Style.Muted, true);

            var card = sidebar.Controls["sidebar-device"];
            if (card != null && card.Visible)
            {
                if (sidebarStatusTop >= SidebarNavStart + (SidebarNavCount - 1) * SidebarNavStep + SidebarNavHeight + 24)
                    Sketch20Style.Text(g, "THIẾT BỊ CỦA BẠN",
                        new Rectangle(26, sidebarStatusTop - 24, sidebar.Width - 52, 19),
                        10.1f, Sketch20Style.Muted, true);
                Sketch20Style.Round(g, new Rectangle(14, sidebarStatusTop, sidebar.Width - 28,
                    sidebarStatusHeight), 13, Color.White, Sketch20Style.Border);
            }
            Sketch20Style.Text(g, "Phiên bản " + Model.Version,
                new Rectangle(26, sidebar.Height - SidebarFooterHeight + 13, sidebar.Width - 52, 26),
                10.7f, Sketch20Style.Muted);
        }

        internal void Render()
        {
            if (rendering || IsDisposed || viewport.ClientSize.Width < 90) return;
            if (Page == "admin" && !AdminSecurity.IsAuthenticated)
            {
                Page = "overview";
                if (adminPage != null) adminPage.Visible = false;
            }
            if (Page == "admin" && adminPage != null)
            {
                viewport.AutoScroll = false;
                Canvas.Visible = false;
                adminPage.Visible = true;
                adminPage.BringToFront();
                UpdateResponsiveSidebar();
                LayoutSidebarBottom();
                foreach (System.Windows.Forms.Control c in sidebar.Controls) c.Invalidate();
                sidebar.Invalidate();
                return;
            }
            if (adminPage != null) adminPage.Visible = false;
            Canvas.Visible = true;
            rendering = true;
            // Cho phép cuộn cả lúc đang quét nếu cửa sổ nhỏ; tránh cắt progress và nút Hủy.
            // Những kích thước lớn vẫn không hiện thanh cuộn khi nội dung đủ chỗ.
            bool fixedScanViewport = false;
            // Bình thường Mối đe dọa chỉ cuộn bên trong bảng và giữ phân trang.
            // Với cửa sổ quá thấp, cho phép cuộn trang để không cắt các nút và footer.
            bool threatWindowTooShort = Page == "threats" && viewport.ClientSize.Height <
                (viewport.ClientSize.Width < 1080 ? 550 : 480);
            bool fixedViewport = fixedScanViewport || Page == "history" ||
                (Page == "threats" && !threatWindowTooShort);
            bool hideAdvancedScrollbar = Page == "advanced";
            bool hideQuarantineScrollbar = Page == "quarantine";
            bool hideDetailScrollbar = Page == "details";
            bool manualPageScroll = hideAdvancedScrollbar || hideQuarantineScrollbar || hideDetailScrollbar;
            var scroll = fixedViewport || manualPageScroll ? Point.Empty : viewport.AutoScrollPosition;
            try
            {
                // Tạm tắt cuộn khi dựng giao diện, sau đó bật lại khi nội dung
                // vượt vùng hiển thị; tránh cắt nút Hủy trên cửa sổ nhỏ.
                viewport.AutoScroll = false;
                UpdateResponsiveSidebar();
                int pageMargin = CompactSidebar ? 16 : 26;
                Canvas.Location = new Point(pageMargin, AdvancedCanvasTop);
                Canvas.SuspendLayout();
                toolTips.RemoveAll();
                // RemoveAll() cũng xóa tooltip của sidebar. Khôi phục nhãn ngắn,
                // nhưng KHÔNG tính lại vị trí các nút trong mỗi lượt Render().
                foreach (string navPage in new[] { "overview", "protection", "quarantine", "threats", "history", "admin", "settings" })
                {
                    var nav = sidebar.Controls["nav-" + navPage] as Button;
                    if (nav != null) toolTips.SetToolTip(nav, nav.Text);
                }
                var sidebarDevice = sidebar.Controls["sidebar-device"];
                if (sidebarDevice != null && CompactSidebar)
                    toolTips.SetToolTip(sidebarDevice, "Thiết bị: " + Environment.MachineName);
                if (Canvas.ContextMenuStrip != null) { Canvas.ContextMenuStrip.Dispose(); Canvas.ContextMenuStrip = null; }
                // Giữ nguyên progress control để hoạt ảnh không bị reset mỗi lần Render.
                foreach (System.Windows.Forms.Control c in Canvas.Controls.Cast<System.Windows.Forms.Control>().ToArray())
                    if (!ReferenceEquals(c, scanSweepProgress)) c.Dispose();
                Canvas.Drawing.Clear(); Canvas.BackgroundDrawing.Clear(); Canvas.Nodes.Clear(); Canvas.TextRuns.Clear();
                int availableWidth = viewport.ClientSize.Width - pageMargin * 2 -
                    (!fixedViewport && !manualPageScroll && needsScrollbar ? SystemInformation.VerticalScrollBarWidth : 0);
                // Trên màn hẹp, trang Bảo vệ chuyển sang một cột thay vì bị cắt
                // ngang bởi ngưỡng 620px dùng cho những trang khác.
                layoutWidth = Math.Max(Page == "protection" ? 340 : Page == "overview" ? 470 : Page == "threats" ? 520 : 620, availableWidth);
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
                else if (Page == "threats") ThreatTable(10);
                else if (Page == "details") ReferenceDetails();
                else Overview();
                if (WindowForm != null) Sketch20Chrome.AddControls(WindowForm, this, layoutWidth);
                bool scanBarVisible = Page == "overview" && Model != null && Model.Scanning;
                scanSweepProgress.SetActive(scanBarVisible);
                if (scanBarVisible) scanSweepProgress.BringToFront();
                Canvas.Height = fixedViewport ? Math.Max(1, viewport.ClientSize.Height - Canvas.Top) :
                    manualPageScroll ? Math.Max(ContentHeight, viewport.ClientSize.Height - AdvancedCanvasTop) : ContentHeight;
                // Quét nâng cao và Cách ly còn hỗ trợ lăn chuột ẩn trên cửa sổ thấp.
                // Lịch sử cố định cả hai trục và điều hướng bằng phân trang.
                int horizontalMin = layoutWidth > availableWidth ? layoutWidth + pageMargin * 2 : 0;
                viewport.AutoScrollMinSize = fixedViewport || manualPageScroll ? Size.Empty :
                    new Size(horizontalMin, ContentHeight + (Page == "settings" ? 8 : 31));
                viewport.AutoScroll = !fixedViewport && !manualPageScroll;
                if (manualPageScroll)
                {
                    int maxScroll = Math.Max(0, ContentHeight + AdvancedCanvasTop + 6 - viewport.ClientSize.Height);
                    if (hideQuarantineScrollbar)
                    {
                        quarantinePageScrollOffset = Math.Max(0, Math.Min(quarantinePageScrollOffset, maxScroll));
                        Canvas.Location = new Point(pageMargin, AdvancedCanvasTop - quarantinePageScrollOffset);
                    }
                    else if (hideDetailScrollbar)
                    {
                        detailPageScrollOffset = Math.Max(0, Math.Min(detailPageScrollOffset, maxScroll));
                        Canvas.Location = new Point(pageMargin, AdvancedCanvasTop - detailPageScrollOffset);
                    }
                    else
                    {
                        advancedScrollOffset = Math.Max(0, Math.Min(advancedScrollOffset, maxScroll));
                        Canvas.Location = new Point(pageMargin, AdvancedCanvasTop - advancedScrollOffset);
                    }
                }
                else if (!fixedViewport)
                    viewport.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
                WireTableMouseWheel(this);
                LayoutSidebarBottom();
                Canvas.ResumeLayout(); Canvas.Invalidate();
                var sideDevice = sidebar.Controls["sidebar-device"] as Sketch20SidebarStatus;
                if (sideDevice != null)
                {
                    var info = Model.Sample ? new[] { "DESKTOP-7H3K9P2", "Windows 11 Pro 23H2", "Intel Core i7-12700H", "16 GB", "64-bit", "Dell Precision 5540" } : DeviceInformation;
                    sideDevice.Note = info[0]; sideDevice.Details = info; sideDevice.StatusColor = Sketch20Style.Blue;
                }
                // Invalidate sidebar một lần khi đổi trang/nhánh điều hướng.
                // Trước đây mọi lần RefreshModel (kể cả khi quét) đều vẽ lại
                // toàn bộ nav và thẻ thiết bị dù chúng không đổi.
                string sidebarState = Page + "|" + DetailReturnPage + "|" + SettingsTab + "|" + CompactSidebar;
                if (!string.Equals(paintedSidebarState, sidebarState, StringComparison.Ordinal))
                {
                    paintedSidebarState = sidebarState;
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
            }
            finally { rendering = false; }
            bool needs = !fixedViewport && !manualPageScroll &&
                ContentHeight + (Page == "settings" ? 8 : 31) > viewport.Height;
            if (needsScrollbar != needs) { needsScrollbar = needs; Render(); }
        }
        // Trang Cài đặt trước đây khóa tỷ lệ ở 1.0, khiến chữ nhỏ hơn các trang khác.
        // Tăng riêng lên 1.19 để chữ mô tả, giá trị, nút thao tác dễ đọc hơn;
        // không phóng to các màn hình khác và không phụ thuộc DPI của Windows.
        private float ReferenceTextScale { get { return Page == "settings" ? 1.19f : Math.Min(1.17f, Math.Max(1.02f, layoutWidth / 820f)); } }
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
                Sketch20Style.Round(g, bounds, Page == "threats" ? 0 : radius, actualFill, actualEdge);
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
                Bounds = new Rectangle(x, y + bodyOffset, width, height),
                SquareCorners = Page == "threats",
                TextPixels = (height <= 33 ? 12 : 13) * ReferenceTextScale };
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
            bool currentValue = Model.Flag(key);
            var c = new Sketch20Switch { Name = "switch-" + key, AccessibleName = caption,
                Bounds = new Rectangle(x, y + bodyOffset, 34, 25), Checked = currentValue };
            bool revertingMaintenanceClick = false;
            c.CheckedChanged += delegate
            {
                if (revertingMaintenanceClick) return;
                // Các trang ngoài Quản trị không cần mật khẩu Admin.
                // Khi bật tra cứu VirusTotal mà thiếu key, mở cấu hình tại chỗ.
                if (key == "vtq" && c.Checked && !Model.KeyConfigured)
                {
                    if (Actions.Command != null) Actions.Command("vt-config");
                    Model.KeyConfigured = VirusTotalClient.IsConfigured;
                    if (!Model.KeyConfigured)
                    {
                        revertingMaintenanceClick = true;
                        try { c.Checked = currentValue; }
                        finally { revertingMaintenanceClick = false; }
                        return;
                    }
                }
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
            // Trang Mối đe dọa chỉ sử dụng tiêu đề bên trong danh sách;
            // không dựng banner thứ hai hay nút Cách ly tất cả ở hàng trên.
            if (Page == "threats") return;
            string title = Page == "overview" ? "Tổng quan" : Page == "protection" ? "Bảo vệ" : Page == "history" ? "Lịch sử" : Page == "quarantine" ? "Cách ly" : Page == "settings" ? "Cài đặt" : Page == "admin" ? "Quản trị" : Page == "advanced" ? "Quét virus" : Page == "threats" ? "Mối đe dọa cần xử lý" : "Chi tiết tệp";
            string subtitle = Page == "overview" ? "Theo dõi trạng thái bảo vệ và hoạt động của máy tính." :
                Page == "protection" ? "Các mô-đun bảo vệ giúp giữ an toàn cho thiết bị của bạn." :
                Page == "history" ? "Xem lại các lần quét, phát hiện và hành động đã thực hiện." :
                Page == "quarantine" ? "Các tệp có khả năng gây hại đã được cách ly để đảm bảo an toàn cho thiết bị." :
                Page == "settings" ? "Tùy chỉnh hoạt động và các tính năng bảo vệ theo nhu cầu của bạn." :
                Page == "admin" ? "Theo dõi và quản lý hệ thống bằng WinForms." :
                Page == "advanced" ? "Chọn chế độ quét phù hợp để kiểm tra và loại bỏ các mối đe dọa." :
                Page == "threats" ? "Chọn một hoặc nhiều tệp để xem chi tiết và xử lý (cách ly, xóa hoặc cho phép)." :
                "Xem thông tin chi tiết và kết quả phân tích của tệp được phát hiện.";

            // 5 tab cấp 1 của sidebar không có Back. Chỉ màn con mới hiển thị Back.
            bool primaryPage = Page == "overview" || Page == "protection" || Page == "quarantine" ||
                Page == "history" || Page == "settings" || Page == "admin";
            if (!primaryPage)
            {
                var back = Button("page-back", "", 0, 6, 32, NavigateBack,
                    "arrow-left", false, false, 32, true);
                back.AccessibleName = "Quay lại màn trước";
                Text(title, 44, 0, layoutWidth - 74, 44, 27, Sketch20Style.Ink, true);
                Text(subtitle, 44, 39, layoutWidth - 94, 25, 12.5f, Sketch20Style.Muted);
            }
            else
            {
                // Các trang có banner/card nội dung lớn không lặp lại tiêu đề ở đầu trang.
                if (Page != "overview" && Page != "protection" && Page != "quarantine" &&
                    Page != "history" && Page != "settings")
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

        // Nút quét của trang Tổng quan luôn là góc vuông, tự tăng kích cỡ
        // theo chiều cao thực tế. Không ảnh hưởng tới nút trên các trang khác.
        private Sketch20Button OverviewActionButton(string name, string caption, int x, int y, int width,
            Action click, string icon, bool primary = false, bool danger = false, int height = 72)
        {
            Sketch20Button button = Button(name, caption, x, y, width, click, icon, primary, danger, height);
            button.SquareCorners = true;
            button.TextPixels = (height >= 84 ? 20.5f : height >= 68 ? 17.5f : 15f) * ReferenceTextScale;
            button.IconPixels = height >= 84 ? 30 : height >= 68 ? 25 : 21;
            return button;
        }

        // Khung đường dẫn luôn cao cố định trong một phiên quét. Không lấy chiều cao
        // từ số dòng của ScanPath vì mỗi tệp có đường dẫn khác nhau sẽ làm cả cột nhảy.
        private const int OverviewScanPathHeight = 112;

        private int MeasureOverviewScanDetailsHeight(int width)
        {
            return 143 + OverviewScanPathHeight;
        }

        // Giữ tối đa ba dòng trong khung cố định. Đường dẫn quá dài được rút gọn
        // ở giữa để vẫn thấy ổ đĩa/thư mục đầu và tên tệp cuối đường dẫn.
        private string FormatOverviewScanPath(string value, int width, int height)
        {
            string original = (value ?? "—").Replace('\r', ' ').Replace('\n', ' ');
            Font font = Sketch20Style.Font(10.6f * ReferenceTextScale);
            var flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
            Size limit = new Size(Math.Max(80, width), 32767);
            Func<string, bool> fits = text => TextRenderer.MeasureText(text, font, limit, flags).Height <= height;
            if (fits(original)) return original;

            int low = 0, high = original.Length;
            string result = "…";
            while (low <= high)
            {
                int count = low + (high - low) / 2;
                int prefix = count / 3;
                int suffix = count - prefix;
                string candidate = original.Substring(0, prefix) + "…" +
                    original.Substring(original.Length - suffix);
                if (fits(candidate)) { result = candidate; low = count + 1; }
                else high = count - 1;
            }
            return result;
        }

        private void OverviewScanProgressPanel(int x, int y, int width)
        {
            int panelHeight = MeasureOverviewScanDetailsHeight(width);
            int innerX = x + 18, innerWidth = Math.Max(170, width - 36);
            int pathY = y + 127;
            int pathH = OverviewScanPathHeight;

            // Bảng thông tin đồng nhất với nền xanh của khối quét.
            Card("scan-progress-shell", x, y, width, panelHeight,
                ColorTranslator.FromHtml("#f9fcff"), ColorTranslator.FromHtml("#cbdcf5"), 15);
            Card("scan-live-indicator", innerX, y + 16, 9, 9,
                ColorTranslator.FromHtml("#1684f6"), Color.Transparent, 5);
            Text("TIẾN TRÌNH ĐANG QUÉT", innerX + 17, y + 9, innerWidth - 18, 26,
                10.6f, Sketch20Style.Ink, true);
            // Thanh quét có Timer riêng (~30 FPS), không phụ thuộc số tệp trong engine.
            scanSweepProgress.Bounds = new Rectangle(innerX, y + 40 + bodyOffset, innerWidth, 36);
            scanSweepProgress.SetTarget(Model.Progress);
            Canvas.Nodes[scanSweepProgress.Name] = scanSweepProgress.Bounds;

            string scanned = Model.ScannedFiles.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
            bool showEta = !string.IsNullOrEmpty(Model.EstimatedRemaining);
            int leftTextW = showEta && innerWidth >= 350 ? innerWidth / 2 : innerWidth;
            Text("Đã kiểm tra " + scanned + " tệp", innerX, y + 85, leftTextW, 20,
                10.8f, Sketch20Style.Ink, true);
            Text("Phát hiện " + Model.ThreatCount + " mối đe dọa", innerX, y + 106, leftTextW, 19,
                10.1f, Model.ThreatCount > 0 ? Sketch20Style.Red : Sketch20Style.Muted);
            if (showEta)
            {
                if (innerWidth >= 350)
                {
                    // Chỉ gắn nhãn "Còn lại" với thời gian dự kiến thực sự.
                    string etaLabel = Model.EstimatedRemaining.StartsWith("~", StringComparison.Ordinal)
                        ? "Còn lại: " + Model.EstimatedRemaining : Model.EstimatedRemaining;
                    Text(etaLabel, innerX + leftTextW, y + 85,
                        innerWidth - leftTextW, 20, 10.8f, Sketch20Style.Blue, true, false, true);
                    Text("Tốc độ: " + Model.ScanSpeed, innerX + leftTextW, y + 106,
                        innerWidth - leftTextW, 19, 10.1f, Sketch20Style.Muted, false, false, true);
                }
                // Trên cửa sổ hẹp, ETA vẫn hiển thị trong dòng trạng thái quét
                // phía trên; không chồng vào hai dòng thống kê tại đây.
            }

            Card("scan-progress-path", innerX, pathY, innerWidth, pathH,
                Color.White, ColorTranslator.FromHtml("#e1ecfc"), 12);
            Text("TỆP ĐANG KIỂM TRA", innerX + 15, pathY + 10,
                innerWidth - 30, 21, 10.1f, Sketch20Style.Muted, true);
            int pathTextWidth = innerWidth - 30;
            int pathTextHeight = pathH - 43;
            string originalPath = Model.ScanPath ?? "—";
            string displayPath = FormatOverviewScanPath(originalPath, pathTextWidth, pathTextHeight);
            Text(displayPath, innerX + 15, pathY + 33,
                pathTextWidth, pathTextHeight, 10.6f, Sketch20Style.Ink, false, true);
            // Di chuột vào đường dẫn rút gọn để xem toàn bộ nội dung qua tooltip.
            if (displayPath != originalPath)
                Canvas.TextRuns.Add(new Sketch20TextRun {
                    Value = originalPath,
                    Bounds = new Rectangle(innerX + 15, pathY + 33 + bodyOffset,
                        pathTextWidth, pathTextHeight),
                    Pixels = 10.6f * ReferenceTextScale, Wrap = false
                });
        }

        // Thanh loading tự vẽ liên tục, chỉ invalidate control nhỏ (không giật theo Render).
        private sealed class ScanSweepProgressBar : System.Windows.Forms.Control
        {
            private readonly Timer animation = new Timer { Interval = 50 };
            private readonly System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
            private bool active;
            private int target = -1;
            private double displayed;
            private double sweep;
            private long previousFrame;

            internal ScanSweepProgressBar()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
                BackColor = ColorTranslator.FromHtml("#f9fcff");
                AccessibleName = "Tiến trình quét virus";
                AccessibleRole = AccessibleRole.ProgressBar;
                TabStop = false;
                animation.Tick += AdvanceFrame;
            }

            internal void SetActive(bool value)
            {
                if (active == value) return;
                active = value;
                Visible = value;
                if (value)
                {
                    clock.Restart();
                    previousFrame = 0;
                    displayed = target >= 0 ? target : 0;
                    sweep = 0;
                    animation.Start();
                }
                else
                {
                    animation.Stop();
                    clock.Reset();
                    target = -1;
                    displayed = 0;
                    sweep = 0;
                }
                Invalidate();
            }

            internal void SetTarget(int percent)
            {
                int next = percent < 0 ? -1 : Math.Max(0, Math.Min(100, percent));
                if (target == next) return;
                if (!active || target < 0 || next < 0 || next < displayed - 4)
                    displayed = Math.Max(0, next);
                target = next;
                AccessibleDescription = next < 0 ? "Đang quét, chưa xác định tổng số tệp" :
                    "Đã hoàn thành " + next + " phần trăm";
                Invalidate();
            }

            private void AdvanceFrame(object sender, EventArgs e)
            {
                if (!active || IsDisposed || !Visible) return;
                if (FindForm() != null && FindForm().WindowState == FormWindowState.Minimized)
                {
                    previousFrame = clock.ElapsedMilliseconds;
                    return;
                }
                long now = clock.ElapsedMilliseconds;
                double delta = Math.Max(.001, Math.Min(.07, (now - previousFrame) / 1000.0));
                previousFrame = now;
                sweep += delta;
                if (target >= 0)
                {
                    // Ease-out theo thời gian: model báo kết quả 0.5-1 giây/lần nhưng thanh vẫn di chuyển mượt.
                    displayed += (target - displayed) * (1 - Math.Exp(-delta * 6.7));
                    if (Math.Abs(target - displayed) < .04) displayed = target;
                }
                Invalidate();
            }

            private static System.Drawing.Drawing2D.GraphicsPath Rounded(RectangleF box, float radius)
            {
                float r = Math.Max(1, Math.Min(radius, Math.Min(box.Width, box.Height) / 2f));
                float d = r * 2;
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(box.X, box.Y, d, d, 180, 90);
                path.AddArc(box.Right - d, box.Y, d, d, 270, 90);
                path.AddArc(box.Right - d, box.Bottom - d, d, d, 0, 90);
                path.AddArc(box.X, box.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                if (Width < 150 || Height < 20) return;
                int labelW = Width < 265 ? 70 : 82;
                float trackW = Math.Max(68, Width - labelW - 14);
                var track = new RectangleF(1, 5, trackW - 2, 25);
                using (var shape = Rounded(track, 12))
                using (var background = new SolidBrush(ColorTranslator.FromHtml("#e9f2ff")))
                using (var outline = new Pen(ColorTranslator.FromHtml("#bed5f6"), 1.1f))
                {
                    g.FillPath(background, shape);
                    g.DrawPath(outline, shape);
                    var original = g.Save();
                    g.SetClip(shape);
                    try
                    {
                        if (target >= 0)
                        {
                            float fill = track.Width * (float)Math.Max(0, Math.Min(100, displayed)) / 100f;
                            if (fill > .5f)
                            {
                                var area = new RectangleF(track.X, track.Y, Math.Max(.5f, fill), track.Height);
                                using (var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                                    new PointF(track.Left, track.Top),
                                    new PointF(Math.Max(track.Left + 1, track.Right), track.Top),
                                    ColorTranslator.FromHtml("#4fb7ff"), ColorTranslator.FromHtml("#075cf4")))
                                    g.FillRectangle(gradient, area);
                                // Dải sáng chỉ di chuyển trong phần đã hoàn tất, không giả lập % cao hơn thực tế.
                                float head = track.X + ((float)(sweep * 145) % (fill + 65)) - 36;
                                var glow = new RectangleF(head, track.Y, 42, track.Height);
                                var filledClip = g.Save();
                                try
                                {
                                    g.SetClip(area, System.Drawing.Drawing2D.CombineMode.Intersect);
                                    using (var gleam = new System.Drawing.Drawing2D.LinearGradientBrush(glow,
                                        Color.FromArgb(0, 255, 255, 255), Color.FromArgb(100, 255, 255, 255),
                                        System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                                        g.FillRectangle(gleam, glow);
                                }
                                finally { g.Restore(filledClip); }
                            }
                        }
                        else
                        {
                            // Marquee đi theo thời gian (không dùng ScannedFiles làm pha).
                            float size = Math.Max(72, track.Width * .42f);
                            float period = track.Width + size;
                            float head = track.Left - size + (float)((sweep * 170) % period);
                            var area = new RectangleF(head, track.Y, size, track.Height);
                            using (var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(area,
                                Color.FromArgb(0, 42, 145, 255), ColorTranslator.FromHtml("#0865f5"),
                                System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                            {
                                gradient.InterpolationColors = new System.Drawing.Drawing2D.ColorBlend
                                {
                                    Colors = new[] { Color.FromArgb(0, 26, 132, 253),
                                        Color.FromArgb(200, 86, 188, 255),
                                        Color.FromArgb(255, 10, 108, 247),
                                        Color.FromArgb(0, 8, 101, 240) },
                                    Positions = new[] { 0f, .32f, .67f, 1f }
                                };
                                g.FillRectangle(gradient, area);
                            }
                        }
                        using (var highlight = new SolidBrush(Color.FromArgb(70, Color.White)))
                            g.FillRectangle(highlight, track.X + 3, track.Y + 2, track.Width - 6, 4);
                    }
                    finally { g.Restore(original); }
                }

                var labelBounds = new Rectangle((int)(track.Right + 12), 1,
                    Math.Max(42, Width - (int)(track.Right + 12)), 33);
                using (var pill = Rounded(new RectangleF(labelBounds.X, 2,
                    Math.Max(40, labelBounds.Width - 1), 31), 12))
                using (var bg = new SolidBrush(ColorTranslator.FromHtml("#e8f2ff")))
                    g.FillPath(bg, pill);
                string label = target < 0 ? "ĐANG QUÉT" : target + "%";
                using (var font = new Font(Theme.UiFontFamily, target < 0 ? 8.6f : 11.1f, FontStyle.Bold))
                using (var ink = new SolidBrush(ColorTranslator.FromHtml("#075ad8")))
                using (var format = new StringFormat { Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                    g.DrawString(label, font, ink, labelBounds, format);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) { animation.Stop(); animation.Dispose(); clock.Stop(); }
                base.Dispose(disposing);
            }
        }

        private Sketch20Button AdvancedSquareButton(string name, string caption, int x, int y, int width,
            Action click, string icon, bool primary = false, bool danger = false, int height = 44, bool iconOnly = false)
        {
            Sketch20Button button = Button(name, caption, x, y, width, click, icon, primary, danger, height, iconOnly);
            button.SquareCorners = true;
            if (!iconOnly)
            {
                button.TextPixels = (height >= 52 ? 15f : height >= 44 ? 13.2f : 12f) * ReferenceTextScale;
                button.IconPixels = height >= 52 ? 20 : 18;
            }
            return button;
        }

        // Bố cục khối quét: màn rộng có ba cột [2 nút] [khiên] [trạng thái].
        // Màn trung bình hai cột (khiên + 2 nút | trạng thái), màn nhỏ xếp dọc.
        // Không tạo bất kỳ module hay thẻ bổ sung nào trong trang Tổng quan.
        private void Overview()
        {
            bool signatureWarning = !Model.Scanning && !string.IsNullOrWhiteSpace(Model.Status) &&
                Model.Status.IndexOf("chữ ký chưa đầy đủ", StringComparison.OrdinalIgnoreCase) >= 0;
            int state = Model.Scanning ? 2 : Model.ThreatCount > 0 ? 3 : !Model.Running ? 1 : 0;
            string[] titles = { signatureWarning ? "Chưa thể xác nhận trạng thái bảo vệ" : "Máy tính của bạn đang được bảo vệ", "Bảo vệ thời gian thực đang tắt",
                "Đang kiểm tra máy tính", Model.ThreatCount + " tệp cần được xử lý" };
            string[] notes = { signatureWarning
                    ? "Chữ ký mã độc chưa sẵn sàng đầy đủ.\nKiểm tra SQL Server và bật những chữ ký đã xác minh."
                    : "Tất cả các tính năng bảo vệ đang hoạt động bình thường.\nChúng tôi luôn theo dõi và giữ cho thiết bị của bạn an toàn.",
                "Thiết bị của bạn hiện không được bảo vệ theo thời gian thực.\nHãy bật tính năng này để ngăn chặn các mối đe dọa ngay lập tức.",
                "Vui lòng không tắt máy hoặc đóng ứng dụng trong quá trình quét.",
                "Chúng tôi đã phát hiện các tệp có dấu hiệu là phần mềm độc hại.\nVui lòng xử lý ngay để giữ an toàn cho thiết bị của bạn." };
            Color[] start = { ColorTranslator.FromHtml("#edf7ff"), ColorTranslator.FromHtml("#fff9e8"),
                ColorTranslator.FromHtml("#eaf5ff"), ColorTranslator.FromHtml("#fff1f4") };
            Color[] end = { ColorTranslator.FromHtml("#d5eaff"), ColorTranslator.FromHtml("#fff0c7"),
                ColorTranslator.FromHtml("#d7efff"), ColorTranslator.FromHtml("#ffdee7") };

            const int heroY = 15;
            const int blockGap = 16;
            const int bottomPadding = 16;
            bool wide = layoutWidth >= 1200;
            bool medium = layoutWidth >= 780 && !wide;
            bool verticalStats = layoutWidth < 900;
            int availableHeight = Math.Max(1, viewport.ClientSize.Height - Canvas.Top - bottomPadding - 4);
            // Thẻ thống kê cao hơn để các dòng chữ thoáng; cả 3 dùng cùng chiều cao.
            // Khi chỉ còn 1 cột, giữ chiều cao cố định để số tệp tăng không đổi bố cục.
            int statH = verticalStats ? 152 : Math.Max(148, Math.Min(188, (int)(availableHeight * 0.22)));
            int totalStatsH = verticalStats ? 3 * statH + 24 : statH;
            // Dành chỗ cho nút Đặt lịch dưới hai nút quét ở mọi DPI/kích thước.
            int minimumHeroH = wide ? (state == 2 ? 540 : 525) :
                medium ? (state == 2 ? 650 : 680) :
                layoutWidth < 580 ? (state == 2 ? 960 : 1100) : (state == 2 ? 880 : 990);
            int heroH = Math.Max(minimumHeroH, availableHeight - heroY - blockGap - totalStatsH);
            GradientCard("hero", 0, heroY, layoutWidth, heroH, start[state], end[state], Sketch20Style.Border, 17);
            Rectangle heroBounds = new Rectangle(0, heroY, layoutWidth, heroH);
            Canvas.Drawing.Add(g => Sketch20Artwork.Waves(g, heroBounds,
                state == 1 ? Sketch20Style.Amber : state == 3 ? Sketch20Style.Red : Sketch20Style.Blue));

            int sideInset = wide ? Math.Max(32, Math.Min(58, layoutWidth / 26)) : medium ? 30 : 26;
            int gap = wide ? Math.Max(18, Math.Min(46, layoutWidth / 32)) : 25;
            int leftX = sideInset, leftW;
            int artX, artW, artY, artH;
            int infoX, infoW;
            if (wide)
            {
                if (state == 2)
                {
                    // Khi quét: tăng chiều ngang bảng tiến trình từ ~28% lên ~34.5%.
                    // Cân lại phần minh họa và nút Hủy, vẫn giữ lề phải bằng lề trái.
                    leftW = (int)(layoutWidth * 0.30f);
                    infoW = Math.Min(620, Math.Max(415, (int)Math.Round(layoutWidth * 0.345f)));
                    infoX = layoutWidth - sideInset - infoW;

                    // Đặt khiên trong khoảng trống THẬT giữa nút Hủy và bảng tiến trình.
                    // Tính theo chiều rộng nút Hủy để không bị đè khi thu nhỏ cửa sổ.
                    int cancelWidth = Math.Min(320, leftW - 8);
                    int actionRight = leftX + (leftW + cancelWidth) / 2;
                    int availableArtX = actionRight + 22;
                    int availableArtRight = infoX - 24;
                    int artSpace = Math.Max(140, availableArtRight - availableArtX);
                    artW = Math.Min(Math.Min(390, (int)(layoutWidth * 0.24f)), artSpace);
                    artX = availableArtX + (artSpace - artW) / 2;
                }
                else
                {
                    // Giữ nguyên bố cục khi chưa quét.
                    leftW = (int)(layoutWidth * 0.36f);
                    artX = (int)(layoutWidth * 0.455f);
                    artW = Math.Min(390, (int)(layoutWidth * 0.24f));
                    infoX = (int)(layoutWidth * 0.72f);
                    infoW = layoutWidth - sideInset - infoX;
                }
                artH = Math.Min(Math.Min(heroH - 90, 440), (int)(artW * 1.16f));
                artY = heroY + (heroH - artH) / 2;
            }
            else if (medium)
            {
                // Với màn hình trung bình, ưu tiên thêm khoảng 25-50px cho tiến trình.
                leftW = Math.Max(285, (int)((layoutWidth - 2 * sideInset - gap) *
                    (state == 2 ? 0.42f : 0.46f)));
                infoX = leftX + leftW + gap;
                infoW = layoutWidth - sideInset - infoX;
                artW = Math.Min(leftW - 22, 268);
                artH = Math.Min(292, (int)(artW * 1.08f));
                artX = leftX + (leftW - artW) / 2;
                artY = heroY + 48;
            }
            else
            {
                leftW = layoutWidth - sideInset * 2;
                infoX = sideInset;
                infoW = leftW;
                artW = Math.Min(leftW - 28, 258);
                artH = Math.Min(262, (int)(artW * 1.02f));
                artX = (layoutWidth - artW) / 2;
                artY = heroY + 336;
            }

            string note = notes[state];
            float titlePixels = wide ? 26 : medium ? 23 : 22;
            int titleH = Math.Max(44, TextRenderer.MeasureText(titles[state], Sketch20Style.Font(titlePixels, true),
                new Size(infoW, 32767), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Height + 6);
            int noteH = Math.Max(44, TextRenderer.MeasureText(note, Sketch20Style.Font(13 * ReferenceTextScale),
                new Size(infoW, 32767), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Height + 5);
            // Trạng thái có 3 dòng (cao 66 px); khi đang quét dùng khối tiến trình + đường dẫn lớn hơn.
            int detailsH = state == 2 ? MeasureOverviewScanDetailsHeight(infoW) : 3 * 69;
            int infoGroupH = titleH + 20 + noteH + 27 + detailsH;
            int infoTop;
            if (wide || medium)
                infoTop = heroY + Math.Max(28, (heroH - infoGroupH) / 2);
            else
                infoTop = heroY + 24;
            int noteY = infoTop + titleH + 20;
            int detailsY = noteY + noteH + 27;

            Text(titles[state], infoX, infoTop, infoW, titleH, titlePixels,
                state == 3 ? Sketch20Style.Red : Sketch20Style.Ink, true, true);
            Text(note, infoX, noteY, infoW, noteH, 13, Sketch20Style.Muted, false, true);
            if (state == 2)
            {
                // Tiến trình quét được đặt trong một khối riêng để loading bar và đường dẫn rõ ràng hơn.
                OverviewScanProgressPanel(infoX, detailsY, infoW);
            }
            else
            {
                MiniStatus("Bảo vệ thời gian thực", Model.Running ? "Đang hoạt động" : "Đang tắt",
                    infoX, detailsY, Model.Running ? Sketch20Style.Green : Sketch20Style.Red, infoW);
                MiniStatus("Cập nhật chữ ký", signatureWarning ? "Cần kiểm tra" : "Mới nhất",
                    infoX, detailsY + 69, signatureWarning ? Sketch20Style.Amber : Sketch20Style.Green, infoW);
                MiniStatus("Tường lửa", Model.Sample ? "Đang bật" : "Xem trong hệ thống",
                    infoX, detailsY + 138, Sketch20Style.Green, infoW);
            }

            // Đặt hình khiên sau các nhãn nhưng trước các nút trong thứ tự vẽ.
            // Ba trạng thái bảo vệ sử dụng cùng vị trí hình minh họa.
            if (!wide && !medium)
            {
                int imageSpaceEnd = heroY + heroH - 194;
                int imageSpaceTop = Math.Max(detailsY + detailsH + 10, heroY + 290);
                artH = Math.Max(132, Math.Min(artH, imageSpaceEnd - imageSpaceTop - 8));
                artY = imageSpaceTop + Math.Max(0, (imageSpaceEnd - imageSpaceTop - artH) / 2);
            }
            Artwork("hero-art", artX, artY, artW, artH,
                state == 1 ? "reference-off" : state == 2 ? "reference-scan" :
                state == 3 ? "reference-risk" : "reference-safe");

            int buttonHeight = wide ? Math.Max(70, Math.Min(98, heroH / 7)) : medium ? 72 : 64;
            int buttonGap = wide ? 20 : 16;
            // Nút Hủy đủ lớn để thao tác, nhưng không chiếm chỗ của thanh tiến trình.
            int buttonWidth = wide ? Math.Min(state == 2 ? 320 : 388, leftW - 4) :
                medium ? Math.Min(320, leftW - 8) : Math.Min(360, leftW - 10);
            int buttonX = leftX + (leftW - buttonWidth) / 2;
            // Giữ nguyên kích cỡ 2 nút quét và thêm nút phụ ngắn hơn + thông tin lịch.
            int scheduleHeight = wide ? 57 : 53;
            int scheduleGap = wide ? 15 : 12;
            int scheduleNoteH = 29;
            int fullActionHeight = 2 * buttonHeight + buttonGap + scheduleGap + scheduleHeight + scheduleNoteH;
            int buttonY = state == 2
                ? (wide ? heroY + (heroH - buttonHeight) / 2 : heroY + heroH - 30 - buttonHeight)
                : wide ? heroY + (heroH - fullActionHeight) / 2
                : heroY + heroH - 30 - fullActionHeight;

            if (state == 0)
            {
                OverviewActionButton("quick-scan", "Quét nhanh", buttonX, buttonY, buttonWidth,
                    () => Command("scan"), "search", true, false, buttonHeight);
                OverviewActionButton("advanced-scan", "Quét nâng cao", buttonX, buttonY + buttonHeight + buttonGap,
                    buttonWidth, () => Go("advanced"), "settings", false, false, buttonHeight);
            }
            else if (state == 1)
            {
                OverviewActionButton("enable-protection", "Bật bảo vệ", buttonX, buttonY, buttonWidth,
                    () => Command("enable"), "play", true, false, buttonHeight).Accent = ColorTranslator.FromHtml("#ff9300");
                OverviewActionButton("quick-scan", "Quét ngay", buttonX, buttonY + buttonHeight + buttonGap,
                    buttonWidth, () => Command("scan"), "search", false, false, buttonHeight);
            }
            else if (state == 3)
            {
                OverviewActionButton("show-details", "Xem chi tiết", buttonX, buttonY, buttonWidth,
                    () => Go("threats"), "file-search", true, true, buttonHeight);
                OverviewActionButton("quick-scan", "Quét lại", buttonX, buttonY + buttonHeight + buttonGap,
                    buttonWidth, () => Command("scan"), "refresh", false, false, buttonHeight);
            }
            else
            {
                // Khi đang quét chỉ xuất hiện nút Hủy quét, không thêm nút thao tác giả.
                OverviewActionButton("cancel-scan", "Hủy quét", buttonX,
                    buttonY, buttonWidth, () => Command("cancel"), "x", false, true, buttonHeight);
            }

            // Nút lập lịch vẫn hiện khi bảo vệ tắt hoặc có mối đe dọa;
            // chỉ ẩn trong lúc quét để không cạnh tranh với thao tác Hủy quét.
            if (state != 2)
            {
                int scheduleY = buttonY + 2 * buttonHeight + buttonGap + scheduleGap;
                var setSchedule = OverviewActionButton("schedule-scan", "Đặt lịch quét tự động",
                    buttonX, scheduleY, buttonWidth, () => Command("schedule"),
                    "calendar", false, false, scheduleHeight);
                setSchedule.TextPixels = 13.8f * ReferenceTextScale;
                setSchedule.IconPixels = 21;
                string summary = string.IsNullOrWhiteSpace(Model.NextAutomaticScan)
                    ? "Chưa có thông tin lịch quét" : Model.NextAutomaticScan;
                // Căn giữa thông báo theo chính nút "Đặt lịch quét tự động",
                // kể cả khi thay đổi kích thước cửa sổ hoặc nội dung lịch quét.
                // Dòng trạng thái luôn hiển thị một dòng, tự rút gọn nếu quá dài.
                float summaryFontPixels = 10.3f * ReferenceTextScale;
                int summaryWidth = Math.Min(buttonWidth,
                    Math.Max(1, Sketch20Style.Measure(summary, summaryFontPixels) + 8));
                int summaryX = buttonX + (buttonWidth - summaryWidth) / 2;
                var summaryBounds = new Rectangle(summaryX,
                    scheduleY + scheduleHeight + 4 + bodyOffset,
                    summaryWidth, scheduleNoteH - 4);
                Canvas.TextRuns.Add(new Sketch20TextRun {
                    Value = summary, Bounds = summaryBounds, Pixels = summaryFontPixels
                });
                Canvas.Drawing.Add(g => Sketch20Style.Text(g, summary, summaryBounds,
                    summaryFontPixels, Sketch20Style.Muted, center: true));
            }

            int statsY = heroY + heroH + blockGap;
            string[] labels = { "Mối đe dọa", "Tệp đã quét", state == 2 ? "Thời gian đã qua" : "Lần quét gần nhất" };
            string[] values = { Model.ThreatCount.ToString(), Model.ScannedFiles.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")),
                state == 2 ? TimeSpan.FromSeconds(Model.ElapsedSeconds).ToString(@"hh\:mm\:ss") :
                Model.LastDate == "—" ? "Chưa có" : Model.LastDate };
            string[] hints = { Model.ThreatCount > 0 ? "Cần được xử lý ngay" : "Không phát hiện mối đe dọa",
                state == 2 ? "Trong phiên hiện tại" : "Tổng số tệp đã kiểm tra",
                state == 2 ? "Theo dõi tiến trình quét" :
                Model.Sample ? "Không phát hiện mối đe dọa nào" : Model.LastTime + " · " + Model.LastType };
            const int statSideInset = 12;
            int statGap = verticalStats ? 12 : 18;
            int cardWidth = verticalStats ? layoutWidth - 2 * statSideInset :
                (layoutWidth - 2 * statSideInset - 2 * statGap) / 3;
            int cardsWidth = verticalStats ? cardWidth : cardWidth * 3 + statGap * 2;
            int firstCardX = (layoutWidth - cardsWidth) / 2;

            // Cùng một cách căn giữa cho cả ba thẻ: Mối đe dọa, Tệp đã quét,
            // Lần quét gần nhất (hoặc Thời gian đã qua lúc đang quét).
            // Tất cả vị trí được tính từ kích thước THẺ, không phụ thuộc độ dài số liệu.
            const int statTitleHeight = 26;
            const int statHintHeight = 36;
            float statScale = ReferenceTextScale;
            int idealTextWidth = Math.Max(232, Math.Min(290,
                Math.Max(
                    Sketch20Style.Measure("14/10/2026", 17.5f * statScale, true),
                    Math.Max(Sketch20Style.Measure("Lần quét gần nhất", 11.5f * statScale),
                        Sketch20Style.Measure("Không phát hiện mối đe dọa", 10.2f * statScale))) + 20));

            for (int i = 0; i < 3; i++)
            {
                int x = verticalStats ? firstCardX : firstCardX + i * (cardWidth + statGap);
                int y = verticalStats ? statsY + i * (statH + statGap) : statsY;
                Card("stat-" + i, x, y, cardWidth, statH, Color.White,
                    ColorTranslator.FromHtml("#dce7f5"), 15);

                // 2 lề ngoài bằng nhau; icon và cột chữ luôn đi cùng thành một nhóm
                // căn giữa, kể cả lúc thẻ rộng toàn màn hình hoặc khi thay đổi DPI.
                int sidePadding = cardWidth < 340 ? 15 : Math.Min(32, Math.Max(22, cardWidth / 18));
                int iconSize = statH >= 170 ? 70 : statH >= 144 ? 64 : 56;
                int contentGap = cardWidth < 345 ? 17 : cardWidth < 430 ? 23 : 30;
                int maxTextWidth = Math.Max(100, cardWidth - 2 * sidePadding - iconSize - contentGap);
                int textW = Math.Min(idealTextWidth, maxTextWidth);
                int groupWidth = iconSize + contentGap + textW;
                int groupX = x + (cardWidth - groupWidth) / 2;
                int iconY = y + (statH - iconSize) / 2;
                int statTextX = groupX + iconSize + contentGap;

                Artwork("stat-icon-" + i, groupX, iconY, iconSize, iconSize,
                    "reference-icon-stat-" + (i == 0 ? "risk" : i == 1 ? "file" : "clock"));

                // Ba dòng có khoảng hở thực (title → value → hint), cả khối được
                // căn giữa theo chiều dọc; ngày quét và số tệp dài không đẩy nhau.
                int rowGap = statH >= 160 ? 12 : 9;
                int valueHeight = statH >= 160 ? 42 : 38;
                int textStackHeight = statTitleHeight + rowGap + valueHeight + rowGap + statHintHeight;
                int textTop = y + (statH - textStackHeight) / 2;
                int valueY = textTop + statTitleHeight + rowGap;
                int hintY = valueY + valueHeight + rowGap;
                float valueSize = i == 2 ? 17.5f : 23f;
                // Giữ nguyên vùng chữ, chỉ hạ cỡ nếu giá trị thực sự vượt quá chiều rộng.
                float effectiveValueSize = valueSize <= 20 ? valueSize * statScale : valueSize;
                if (Sketch20Style.Measure(values[i], effectiveValueSize, true) > textW - 2)
                    valueSize = i == 2 ? 15.5f : 19.5f;

                Text(labels[i], statTextX, textTop, textW, statTitleHeight,
                    11.5f, Sketch20Style.Ink);
                Text(values[i], statTextX, valueY, textW, valueHeight,
                    valueSize, i == 0 && state == 3 ? Sketch20Style.Red : Sketch20Style.Ink, true);
                Text(hints[i], statTextX, hintY, textW, statHintHeight,
                    10.2f, Sketch20Style.Muted, false, true);
            }
            ContentHeight = statsY + totalStatsH + bottomPadding;
        }

        private void MiniStatus(string title, string note, int x, int y, Color color, int width)
        {
            // Biểu tượng và hai dòng chữ thống nhất hàng lối ở mọi bề rộng màn hình.
            Canvas.Drawing.Add(g => { using (var b = new SolidBrush(color)) g.FillEllipse(b, x, y + 5, 29, 29); });
            Icon(note == "Đang tắt" ? "alert" : "check", x + 6, y + 11, 17, Color.White);
            Text(title, x + 44, y, width - 44, 30, 13.2f, Sketch20Style.Ink, true);
            Text(note, x + 44, y + 30, width - 44, 28, 11.6f,
                note == "Đang tắt" ? Sketch20Style.Red : Sketch20Style.Muted);
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
            Button("open-history", "Xem lịch sử", layoutWidth - 118, y + 12, 98, () => Go("history"), "", false, false, 31, true);
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
            ThreatTableRedesigned(y);
        }

        internal static readonly string[] FeatureKeys = { "rt", "restore", "usb", "download", "behavior", "startup", "aq", "vtq" };
        internal static readonly string[] FeatureNames = { "Bảo vệ thời gian thực", "Bảo vệ tệp", "Bảo vệ USB", "Bảo vệ tải xuống", "Giám sát hành vi", "Bảo vệ thư mục khởi động", "Tự động cách ly", "Bảo vệ web · VirusTotal" };
        internal static readonly string[] FeatureNotes = { "Giám sát tệp mới hoặc thay đổi trong máy.", "Kiểm tra lại tệp trước khi khôi phục từ cách ly.", "Quét ổ USB ngay khi được kết nối.", "Kiểm tra tệp tải về từ Internet.", "Theo dõi tiến trình và thay đổi hàng loạt tệp trong thời gian thực.", "Kiểm tra tệp mới trong thư mục Startup.", "Chuyển tệp nguy hiểm vào khu vực cách ly.", "Tra cứu tệp nghi ngờ bằng mã băm." };
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
            HistoryRedesigned();
        }

        // Góc vuông chỉ áp dụng cho nút hành động ở trang Cài đặt.
        // Công tắc bật/tắt vẫn giữ thiết kế riêng để dễ nhận biết trạng thái.
        private Sketch20Button SettingsSquareButton(string name, string caption, int x, int y,
            int width, Action action, string icon, bool primary = false, bool danger = false,
            int height = 44, bool iconOnly = false)
        {
            Sketch20Button button = Button(name, caption, x, y, width, action,
                icon, primary, danger, height, iconOnly);
            button.SquareCorners = true;
            button.TextPixels = (height >= 46 ? 14.2f : 12.8f) * ReferenceTextScale;
            button.IconPixels = height >= 46 ? 19 : 17;
            return button;
        }

        // Dòng thông tin trên trang Cài đặt: chữ và giá trị lớn hơn một chút,
        // vẫn rút gọn đường dẫn quá dài mà không đè lên nút Sao chép.
        private void SettingsInfoRow(string label, string value, int x, int y,
            int width, int labelWidth, int height, bool last = false)
        {
            // Giá trị và nhãn sử dụng chữ >= 17 px thực tế ở mức zoom mặc định.
            // Căn giữa theo chiều cao hàng để không đè lên đường phân cách.
            int lineH = Math.Min(height - 6, 36);
            int lineY = y + Math.Max(0, (height - lineH) / 2);
            Text(label, x, lineY, labelWidth, lineH, 13.3f, Sketch20Style.Muted);
            int valueWidth = Math.Max(36, width - labelWidth - Sketch20Style.RowPaddingX);
            // Đường dẫn thư mục có thể dài: cho phép xuống dòng trong hàng cuối,
            // tránh hiện dấu ba chấm khi tăng cỡ chữ.
            bool wrapValue = last && Sketch20Style.Measure(value, 13.6f * ReferenceTextScale) > valueWidth;
            int valueHeight = wrapValue ? Math.Min(height - 4, 48) : lineH;
            int valueY = wrapValue ? y + Math.Max(0, (height - valueHeight) / 2) : lineY;
            Text(value, x + labelWidth + Sketch20Style.RowPaddingX, valueY,
                valueWidth, valueHeight, 13.6f, Sketch20Style.Ink, false, wrapValue);
            if (!last) Line(x, y + height, width);
        }

        private void Settings()
        {
            SettingsTab = 0;
            const int top = 18;
            const int gap = 16;
            const int footerGap = 14;
            bool singleColumn = layoutWidth < 870;
            bool stackLower = layoutWidth < 900;
            bool compactFooter = layoutWidth < 790;
            int generalColumns = singleColumn ? 1 : 2;
            int generalRows = singleColumn ? 2 : 1;
            // Dành chiều cao tối thiểu đủ cho hai dòng mô tả phóng to.
            int minGeneralH = singleColumn ? 330 : 226;
            int minInfoH = stackLower ? 312 : 310;
            int minToolsH = stackLower ? 440 : 420;
            int minLowerH = stackLower ? minInfoH + gap + minToolsH : Math.Max(minInfoH, minToolsH);
            int footerH = compactFooter ? 102 : 65;

            // Chia không gian theo viewport: màn hình lớn giãn đều các thẻ; màn hình
            // thấp vẫn giữ kích thước tối thiểu và dùng AutoScroll của trang.
            int viewportBottom = Math.Max(0, viewport.ClientSize.Height - AdvancedCanvasTop - 21);
            int requiredH = top + minGeneralH + gap + minLowerH + footerGap + footerH;
            int extra = Math.Max(0, viewportBottom - requiredH);
            int generalGrow = (int)(extra * 0.35f);
            int generalH = minGeneralH + generalGrow;
            int lowerAreaH = minLowerH + extra - generalGrow;

            Card("settings-general", 0, top, layoutWidth, generalH, Color.White,
                ColorTranslator.FromHtml("#dce7f5"), 16);
            Card("settings-general-icon", 22, top + 17, 48, 48,
                Sketch20Style.BlueSoft, Color.Transparent, 13);
            Icon("settings", 34, top + 29, 24, Sketch20Style.Blue);
            Text("Thiết lập chung", 84, top + 11, layoutWidth - 112, 36,
                22f, Sketch20Style.Ink, true);
            Text("Các tùy chọn cơ bản của ứng dụng.", 84, top + 49,
                layoutWidth - 112, 28, 12.1f, Sketch20Style.Muted);

            // Chính sách chữ ký và gửi mẫu được quản trị trong UcQuanTri, không ở Cài đặt người dùng.
            string[] keys = { "autostart", "alerts" };
            string[] names = { "Khởi động cùng Windows", "Hiển thị thông báo" };
            string[] notes = { "Tự động mở ứng dụng khi bật máy tính.", "Nhận cảnh báo về mối đe dọa và kết quả quét." };
            string[] icons = { "monitor", "bell" };
            Color[] colors = { Sketch20Style.Blue, Sketch20Style.Red };
            Color[] soft = { Sketch20Style.BlueSoft, Sketch20Style.RedSoft };

            const int gridInset = 18, cellGap = 14, gridHeaderH = 84;
            int gridY = top + gridHeaderH;
            int cellW = (layoutWidth - gridInset * 2 - cellGap * (generalColumns - 1)) / generalColumns;
            int cellH = Math.Max(90,
                (generalH - gridHeaderH - 18 - cellGap * (generalRows - 1)) / generalRows);
            for (int i = 0; i < keys.Length; i++)
            {
                int col = i % generalColumns, row = i / generalColumns;
                int x = gridInset + col * (cellW + cellGap);
                int y = gridY + row * (cellH + cellGap);
                Card("setting-row-" + i, x, y, cellW, cellH,
                    ColorTranslator.FromHtml("#fbfdff"), ColorTranslator.FromHtml("#e5edf8"), 12);
                int iconSize = cellH >= 120 ? 54 : 48;
                int iconY = y + (cellH - iconSize) / 2;
                Card("setting-icon-" + i, x + 16, iconY, iconSize, iconSize,
                    soft[i], Color.Transparent, 12);
                Icon(icons[i], x + 16 + (iconSize - 24) / 2,
                    iconY + (iconSize - 24) / 2, 24, colors[i]);
                int textX = x + iconSize + 32;
                int textW = Math.Max(100, cellW - (textX - x) - 66);
                const int textBlockH = 81;
                int textY = y + (cellH - textBlockH) / 2;
                Text(names[i], textX, textY, textW, 32, 14.4f, Sketch20Style.Ink, true);
                Text(notes[i], textX, textY + 35, textW, 44,
                    11.8f, Sketch20Style.Muted, false, true);
                Switch(keys[i], names[i], x + cellW - 50, y + (cellH - 25) / 2);
            }

            int lowerY = top + generalH + gap;
            int leftW = stackLower ? layoutWidth : (layoutWidth - gap) / 2;
            int rightX = stackLower ? 0 : leftW + gap;
            int rightW = layoutWidth - rightX;
            int remainingH = stackLower ? lowerAreaH - gap : lowerAreaH;
            int infoH = stackLower ? minInfoH + Math.Max(0, remainingH - minInfoH - minToolsH) / 2 : lowerAreaH;
            int toolsY = stackLower ? lowerY + infoH + gap : lowerY;
            int toolsH = stackLower ? lowerAreaH - gap - infoH : lowerAreaH;

            Card("data-info", 0, lowerY, leftW, infoH, Color.White,
                ColorTranslator.FromHtml("#dce7f5"), 16);
            Card("data-info-icon", 20, lowerY + 17, 46, 46,
                Sketch20Style.GreenSoft, Color.Transparent, 12);
            Icon("hard-drive", 32, lowerY + 29, 22, Sketch20Style.Green);
            Text("Thông tin ứng dụng", 82, lowerY + 11, leftW - 104, 34,
                18.4f, Sketch20Style.Ink, true);
            Text("Dữ liệu và trạng thái hiện tại.", 82, lowerY + 46,
                leftW - 104, 28, 12.0f, Sketch20Style.Muted);

            int infoY = lowerY + 85;
            int infoRowH = Math.Max(48, (infoH - 85 - 19) / 4);
            int infoValueWidth = leftW >= 540 ? 155 : 127;
            int infoX = 22;
            int infoW = leftW - 44;
            SettingsInfoRow("Phiên bản", Model.Version, infoX, infoY, infoW, infoValueWidth, infoRowH);
            SettingsInfoRow("CSDL chữ ký", Model.Signature, infoX, infoY + infoRowH,
                infoW, infoValueWidth, infoRowH);
            SettingsInfoRow("Đang cách ly", Model.Quarantine.Count + " tệp", infoX,
                infoY + 2 * infoRowH, infoW, infoValueWidth, infoRowH);
            SettingsInfoRow("Thư mục dữ liệu", Model.DataFolder, infoX,
                infoY + 3 * infoRowH, infoW - 38, infoValueWidth, infoRowH, true);
            var copyButton = SettingsSquareButton("copy-data-folder", "", leftW - 55,
                infoY + 3 * infoRowH + (infoRowH - 36) / 2, 34,
                () => { if (!string.IsNullOrWhiteSpace(Model.DataFolder)) Clipboard.SetText(Model.DataFolder); },
                "files", false, false, 36, true);
            toolTips.SetToolTip(copyButton, "Sao chép đường dẫn thư mục dữ liệu");

            Card("quick-tools", rightX, toolsY, rightW, toolsH, Color.White,
                ColorTranslator.FromHtml("#dce7f5"), 16);
            Card("quick-tools-icon", rightX + 20, toolsY + 17, 46, 46,
                Sketch20Style.BlueSoft, Color.Transparent, 12);
            Icon("bolt", rightX + 32, toolsY + 29, 22, Sketch20Style.Blue);
            Text("Thao tác nhanh", rightX + 82, toolsY + 11,
                rightW - 104, 34, 18.4f, Sketch20Style.Ink, true);
            Text("Công cụ quản lý dữ liệu ứng dụng.", rightX + 82,
                toolsY + 46, rightW - 104, 28, 12.0f, Sketch20Style.Muted);

            string[] toolTitles = { "Mở thư mục dữ liệu", "Xóa cache quét", "Tạo tệp mẫu" };
            string[] toolNotes = { "Mở vị trí lưu dữ liệu.", "Xóa dữ liệu tạm của các lần quét.", "Tạo mẫu thử để kiểm tra phát hiện." };
            string[] toolIcons = { "folder-open", "trash", "file-plus" };
            string[] toolButtons = { "Mở", "Xóa", "Tạo" };
            Color[] toolColors = { Sketch20Style.Blue, Sketch20Style.Red, Sketch20Style.Green };
            Color[] toolSoft = { Sketch20Style.BlueSoft, Sketch20Style.RedSoft, Sketch20Style.GreenSoft };
            Action[] toolActions = { () => Command("open-data"), () => Command("clear-cache"), () => Command("samples") };
            const int toolGap = 12;
            int toolY = toolsY + 83;
            int toolRowH = Math.Max(64, (toolsH - 83 - 18 - toolGap * 2) / 3);
            for (int i = 0; i < toolTitles.Length; i++)
            {
                int y = toolY + i * (toolRowH + toolGap);
                Card("data-action-" + i, rightX + 18, y, rightW - 36, toolRowH,
                    ColorTranslator.FromHtml("#fbfdff"), ColorTranslator.FromHtml("#e5edf8"), 11);
                int iconSize = toolRowH >= 86 ? 46 : 42;
                int iconY = y + (toolRowH - iconSize) / 2;
                Card("data-action-icon-" + i, rightX + 33, iconY,
                    iconSize, iconSize, toolSoft[i], Color.Transparent, 11);
                Icon(toolIcons[i], rightX + 33 + (iconSize - 22) / 2,
                    iconY + (iconSize - 22) / 2, 22, toolColors[i]);
                int textX = rightX + iconSize + 48;
                int textW = Math.Max(100, rightW - (textX - rightX) - 134);
                // Tăng cả tiêu đề lẫn chú thích và cho phép chú thích xuống 2 dòng.
                const int toolTextH = 78;
                int textY = y + (toolRowH - toolTextH) / 2;
                Text(toolTitles[i], textX, textY, textW, 33,
                    14.2f, Sketch20Style.Ink, true);
                Text(toolNotes[i], textX, textY + 35, textW, 42,
                    11.8f, Sketch20Style.Muted, false, true);
                SettingsSquareButton("data-action-button-" + i, toolButtons[i],
                    rightX + rightW - 118, y + (toolRowH - 42) / 2, 94,
                    toolActions[i], toolIcons[i], false, false, 42);
            }

            int footerY = lowerY + lowerAreaH + footerGap;
            if (compactFooter)
            {
                Text("Thay đổi chỉ được áp dụng sau khi lưu.", 3, footerY + 4,
                    layoutWidth - 6, 30, 12.1f, Sketch20Style.Muted);
                int restoreW = (layoutWidth - 12) / 2;
                int saveW = layoutWidth - 12 - restoreW;
                SettingsSquareButton("restore-defaults", "Khôi phục mặc định", 0,
                    footerY + 38, restoreW, () => Command("defaults"),
                    "undo", false, false, 48);
                SettingsSquareButton("save-settings", "Lưu thiết lập", restoreW + 12,
                    footerY + 38, saveW, () => Command("save-settings"),
                    "save", true, false, 48);
            }
            else
            {
                const int restoreW = 220, saveW = 188, footerButtonGap = 12;
                Text("Thay đổi chỉ được áp dụng sau khi lưu.", 4, footerY + 14,
                    layoutWidth - restoreW - saveW - footerButtonGap - 30,
                    36, 12.1f, Sketch20Style.Muted);
                int restoreX = layoutWidth - restoreW - saveW - footerButtonGap;
                SettingsSquareButton("restore-defaults", "Khôi phục mặc định",
                    restoreX, footerY + 5, restoreW, () => Command("defaults"),
                    "undo", false, false, 48);
                SettingsSquareButton("save-settings", "Lưu thiết lập",
                    restoreX + restoreW + footerButtonGap, footerY + 5, saveW,
                    () => Command("save-settings"), "save", true, false, 48);
            }
            ContentHeight = footerY + footerH + 3;
        }

        // Danh sách nhiều thư mục: thêm nối tiếp (không thay thế), không quét lặp đường dẫn.
        // FolderBrowserDialog của WinForms chọn một thư mục/lần; người dùng có thể
        // nhấn Thêm nhiều lần hoặc kéo thả nhiều thư mục một lượt từ Explorer.
        private void AddScanFolders(IEnumerable<string> selectedPaths, bool toCustom)
        {
            List<string> target = toCustom ? customFolders : folders;
            bool changed = false;
            foreach (string selected in selectedPaths ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(selected)) continue;
                string path;
                try
                {
                    path = Path.GetFullPath(selected.Trim());
                    string root = Path.GetPathRoot(path);
                    if (!string.Equals(path, root, StringComparison.OrdinalIgnoreCase))
                        path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                catch { continue; }
                if (!Model.Sample && !Directory.Exists(path)) continue;
                if (target.Any(existing => string.Equals(existing, path, StringComparison.OrdinalIgnoreCase)))
                    continue;
                target.Add(path);
                changed = true;
            }
            // Luôn chuyển đến trang cuối để thư mục vừa chọn xuất hiện ngay.
            if (changed && !toCustom) folderListPage = int.MaxValue;
        }

        private void FolderScanScope(int mainY)
        {
            bool twoColumns = layoutWidth >= 900;
            int minimumHeight = twoColumns ? 485 : 620;
            int mainH = Math.Max(minimumHeight,
                viewport.ClientSize.Height - AdvancedCanvasTop - mainY - 30);
            int innerMargin = 22, gap = 16;
            Card("scan-scope", 0, mainY, layoutWidth, mainH, Color.White,
                ColorTranslator.FromHtml("#dce7f5"), 17);

            Text("Quét thư mục", innerMargin, mainY + 17, layoutWidth - innerMargin * 2, 36,
                21, Sketch20Style.Ink, true);
            Text("Thêm nhiều thư mục vào danh sách và quét trong một lần.",
                innerMargin, mainY + 55, layoutWidth - innerMargin * 2, 27,
                12f, Sketch20Style.Muted);

            int listX = innerMargin, contentY = mainY + 96;
            int actionH = 52;
            int actionY = mainY + mainH - actionH - 20;
            int contentH = actionY - contentY - 17;
            int listW = twoColumns ? Math.Max(440, (layoutWidth - innerMargin * 2 - gap) * 63 / 100)
                                   : layoutWidth - innerMargin * 2;
            int sideX = twoColumns ? listX + listW + gap : listX;
            int sideW = twoColumns ? layoutWidth - innerMargin - sideX : listW;
            int sideH = twoColumns ? contentH : 166;
            int listH = twoColumns ? contentH : contentH - sideH - gap;
            int sideY = twoColumns ? contentY : contentY + listH + gap;

            Card("scan-folder-list", listX, contentY, listW, listH,
                ColorTranslator.FromHtml("#fbfdff"), ColorTranslator.FromHtml("#dce7f5"), 13);
            Text("Danh sách thư mục", listX + 17, contentY + 17,
                Math.Max(120, listW - 230), 28, 14, Sketch20Style.Ink, true);
            var addButton = AdvancedSquareButton("add-scan-folder", "Thêm thư mục",
                listX + listW - 186, contentY + 13, 170,
                () => ChooseScope(true), "folder-plus", true, false, 43);
            toolTips.SetToolTip(addButton, "Chọn thêm một thư mục. Có thể chọn nhiều lần.");
            Text(folders.Count == 0 ? "Chưa chọn thư mục" : folders.Count + " thư mục đã chọn",
                listX + 17, contentY + 51, listW - 34, 22, 10.8f, Sketch20Style.Muted);

            const int rowH = 61;
            const int rowGap = 9;
            int startY = contentY + 82;
            int bottomReserve = 47;
            int rowsPerPage = Math.Max(1, (listH - 82 - bottomReserve + rowGap) / (rowH + rowGap));
            int totalPages = Math.Max(1, (folders.Count + rowsPerPage - 1) / rowsPerPage);
            folderListPage = Math.Max(0, Math.Min(folderListPage, totalPages - 1));
            if (folders.Count == 0)
            {
                // Giữa khung danh sách, nhưng không chạm khu vực phân trang bên dưới.
                int usableRowArea = Math.Max(112, listH - 82 - bottomReserve);
                int iconTop = startY + Math.Max(0, (usableRowArea - 112) / 2);
                Card("scan-folder-empty-icon", listX + (listW - 50) / 2,
                    iconTop, 50, 50, Sketch20Style.BlueSoft, Color.Transparent, 12);
                Icon("folder", listX + (listW - 22) / 2, iconTop + 14,
                    22, Sketch20Style.Blue);
                Canvas.Drawing.Add(g => Sketch20Style.Text(g, "Chưa có thư mục nào",
                    new Rectangle(listX + 24, iconTop + 52 + bodyOffset, listW - 48, 27),
                    12.2f * ReferenceTextScale, Sketch20Style.Ink, true,
                    false, false, false, true));
                Canvas.Drawing.Add(g => Sketch20Style.Text(g,
                    "Nhấn Thêm thư mục hoặc kéo thư mục từ Explorer vào cửa sổ.",
                    new Rectangle(listX + 25, iconTop + 79 + bodyOffset, listW - 50, 33),
                    10.8f * ReferenceTextScale, Sketch20Style.Muted,
                    false, true, false, false, true));
            }
            else
            {
                for (int indexOnPage = 0; indexOnPage < rowsPerPage; indexOnPage++)
                {
                    int index = folderListPage * rowsPerPage + indexOnPage;
                    if (index >= folders.Count) break;
                    string path = folders[index];
                    int y = startY + indexOnPage * (rowH + rowGap);
                    int x = listX + 15, rowW = listW - 30;
                    Card("scan-folder-row-" + index, x, y, rowW, rowH,
                        Color.White, ColorTranslator.FromHtml("#e3ecf8"), 9);
                    Card("scan-folder-icon-" + index, x + 10, y + 11, 40, 40,
                        Sketch20Style.BlueSoft, Color.Transparent, 9);
                    Icon("folder", x + 20, y + 21, 20, Sketch20Style.Blue);
                    string name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (string.IsNullOrWhiteSpace(name)) name = path;
                    int textW = Math.Max(80, rowW - 114);
                    Text(name, x + 61, y + 7, textW, 23, 12f, Sketch20Style.Ink, true);
                    Text(path, x + 61, y + 32, textW, 21, 10f, Sketch20Style.Muted);
                    int removableIndex = index;
                    var remove = AdvancedSquareButton("remove-scan-folder-" + index, "",
                        x + rowW - 47, y + 12, 34,
                        () => { folders.RemoveAt(removableIndex); Render(); },
                        "x", false, true, 36, true);
                    remove.AccessibleName = "Xóa thư mục " + path;
                    toolTips.SetToolTip(remove, "Xóa khỏi danh sách: " + path);
                }
            }

            // Phân trang khi có nhiều thư mục: không tạo thanh cuộn ngang/dọc.
            int pagerY = contentY + listH - 38;
            if (totalPages > 1)
            {
                int prevX = listX + listW - 198;
                var prev = AdvancedSquareButton("folder-page-prev", "", prevX, pagerY, 35,
                    () => { folderListPage--; Render(); }, "chevron-left", false, false, 31, true);
                prev.Enabled = folderListPage > 0;
                var next = AdvancedSquareButton("folder-page-next", "Sau", listX + listW - 48,
                    pagerY, 35, () => { folderListPage++; Render(); },
                    "", false, false, 31, true);
                next.TextPixels = 10.2f;
                next.Enabled = folderListPage + 1 < totalPages;
                Text("Trang " + (folderListPage + 1) + "/" + totalPages,
                    listX + listW - 155, pagerY + 4, 103, 24, 10.5f, Sketch20Style.Muted,
                    false, false, true);
            }
            else
            {
                Text("Có thể chọn thêm nhiều thư mục.", listX + 17, pagerY + 5,
                    listW - 34, 24, 10.2f, Sketch20Style.Muted);
            }

            Card("scan-folder-options", sideX, sideY, sideW, sideH,
                ColorTranslator.FromHtml("#f8fbff"), ColorTranslator.FromHtml("#dce7f5"), 13);
            Text("Tùy chọn quét", sideX + 18, sideY + 16,
                sideW - 36, 30, 14, Sketch20Style.Ink, true);
            // Bắt buộc quét đệ quy ở chế độ thư mục, không cho tắt để bỏ sót tệp.
            Icon("check-circle", sideX + 18, sideY + 66, 19, Sketch20Style.Green);
            Text("Luôn quét tất cả thư mục con", sideX + 44, sideY + 61,
                Math.Max(100, sideW - 61), 31, 11.2f, Sketch20Style.Ink, true);
            Text("Mỗi tệp được phân tích và thông báo riêng theo đường dẫn.",
                sideX + 43, sideY + 100, Math.Max(90, sideW - 64),
                twoColumns ? 49 : 35, 10.5f, Sketch20Style.Muted, false, true);
            if (twoColumns)
            {
                int infoY = sideY + sideH - 132;
                Card("scan-folder-summary", sideX + 16, infoY, sideW - 32, 111,
                    Color.White, ColorTranslator.FromHtml("#dce7f5"), 10);
                Text("PHẠM VI ĐÃ CHỌN", sideX + 31, infoY + 16,
                    sideW - 62, 24, 10.5f, Sketch20Style.Muted, true);
                Text(folders.Count.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " thư mục",
                    sideX + 31, infoY + 45, sideW - 62, 36, 20, Sketch20Style.Blue, true);
            }

            // Footer ở cuối vùng nội dung, không chạy lên/xuống khi tăng số thư mục.
            int buttonW = Math.Min(235, Math.Max(190, layoutWidth / 5));
            int buttonX = layoutWidth - innerMargin - buttonW;
            Callout(innerMargin, actionY, Math.Max(140, buttonX - innerMargin - 14),
                folders.Count == 0 ? "Hãy chọn ít nhất một thư mục để bắt đầu." :
                "Sẽ quét toàn bộ " + folders.Count + " thư mục đã chọn.", false, actionH);
            var start = AdvancedSquareButton("start-advanced", "Bắt đầu quét", buttonX, actionY,
                buttonW, StartAdvanced, "search", true, false, actionH);
            start.Enabled = !Model.Scanning && folders.Count > 0;
            ContentHeight = mainY + mainH + 18;
        }

        private void Advanced()
        {
            // Chỉ còn ba chế độ. Chế độ cũ (3) không được mở từ giao diện hay preview.
            if (AdvancedMode < 0 || AdvancedMode > 2) AdvancedMode = 0;
            string[] names = { "Quét toàn bộ hệ thống", "Quét thư mục", "Quét tệp" };
            string[] notes = {
                "Kiểm tra tất cả ổ đĩa, tệp và khu vực hệ thống.",
                "Chọn một hoặc nhiều thư mục cần quét.",
                "Quét một hoặc nhiều tệp riêng lẻ."
            };
            string[] icons = { "monitor", "folder", "file" };

            // Responsive: cửa sổ rộng hiển thị 3 thẻ bằng nhau;
            // cửa sổ hẹp dùng hai thẻ trên và một thẻ trải toàn hàng dưới.
            bool stackedModes = layoutWidth < 780;
            int modeY = 81;
            int gap = 14;
            int rowH = stackedModes ? 162 :
                Math.Max(174, Math.Min(196, (int)(viewport.ClientSize.Height * 0.19f)));
            int modeBlockH = stackedModes ? rowH * 2 + gap : rowH;
            int firstRowWidth = stackedModes ? (layoutWidth - gap) / 2 :
                (layoutWidth - gap * 2) / 3;
            int lastWidth = stackedModes ? layoutWidth :
                layoutWidth - 2 * (firstRowWidth + gap);
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                int x = stackedModes ? (i == 1 ? firstRowWidth + gap : 0) :
                    i * (firstRowWidth + gap);
                int y = modeY + (stackedModes && i == 2 ? rowH + gap : 0);
                int w = i == 2 ? lastWidth : firstRowWidth;
                var mode = new Sketch20Mode(names[i], notes[i], icons[i])
                {
                    Name = "mode-" + i,
                    Bounds = new Rectangle(x, y, w, rowH),
                    Checked = AdvancedMode == index,
                    Compact = false,
                    Centered = false
                };
                mode.TextScale = ReferenceTextScale;
                mode.Click += delegate { AdvancedMode = index; Render(); };
                Canvas.Controls.Add(mode);
                Canvas.Nodes[mode.Name] = mode.Bounds;
            }

            int mainY = modeY + modeBlockH + 17;
            if (AdvancedMode == 0) { FullScanScope(mainY); return; }
            if (AdvancedMode == 1) { FolderScanScope(mainY); return; }

            FileScanScope(mainY);
        }

        private static string FileSize(string path)
        {
            try { long bytes = new FileInfo(path).Length; return bytes >= 1048576 ? (bytes / 1048576.0).ToString("0.#") + " MB" : bytes >= 1024 ? (bytes / 1024.0).ToString("0.#") + " KB" : bytes + " B"; }
            catch { return "—"; }
        }
        // Bố cục Quét tệp: danh sách luôn chiếm phần lớn vùng nội dung và phân trang
        // khi có quá nhiều mục; phần tổng quan chiếm phần còn lại thay vì để trắng.
        private void FileScanScope(int mainY)
        {
            bool twoColumns = layoutWidth >= 900;
            int mainH = Math.Max(twoColumns ? 555 : 700,
                viewport.ClientSize.Height - AdvancedCanvasTop - mainY - 30);
            const int margin = 22, gap = 16, actionH = 52;
            int actionY = mainY + mainH - actionH - 20;
            int contentY = mainY + 99;
            int contentH = actionY - contentY - 16;
            int listW = twoColumns ? (layoutWidth - margin * 2 - gap) * 63 / 100 : layoutWidth - margin * 2;
            int sideX = twoColumns ? margin + listW + gap : margin;
            int sideW = twoColumns ? layoutWidth - margin - sideX : listW;
            int sideH = twoColumns ? contentH : 174;
            int listH = twoColumns ? contentH : contentH - sideH - gap;
            int sideY = twoColumns ? contentY : contentY + listH + gap;

            Card("scan-scope", 0, mainY, layoutWidth, mainH, Color.White,
                ColorTranslator.FromHtml("#dce7f5"), 17);
            Text("Quét tệp", margin, mainY + 17, layoutWidth - margin * 2, 35,
                21, Sketch20Style.Ink, true);
            Text("Chọn một hoặc nhiều tệp cần kiểm tra trong cùng một lần quét.",
                margin, mainY + 54, layoutWidth - margin * 2, 27,
                12, Sketch20Style.Muted);

            Card("scan-file-list", margin, contentY, listW, listH,
                ColorTranslator.FromHtml("#fbfdff"), ColorTranslator.FromHtml("#dce7f5"), 13);
            Text("Danh sách tệp", margin + 17, contentY + 16,
                Math.Max(110, listW - 180), 29, 14, Sketch20Style.Ink, true);
            var choose = AdvancedSquareButton("choose-scope", "Chọn tệp", margin + listW - 145,
                contentY + 13, 130, () => ChooseScope(false), "file-plus", true, false, 42);
            toolTips.SetToolTip(choose, "Chọn nhiều tệp từ Windows Explorer.");

            bool hasSampleFiles = Model.Sample && files.Count == 0;
            string[] samplePaths = { @"C:\Users\Admin\Downloads\invoice_update.exe", @"D:\Projects\setup_script.ps1" };
            int selectedCount = hasSampleFiles ? samplePaths.Length : files.Count;
            Text(selectedCount == 0 ? "Chưa chọn tệp" :
                selectedCount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " tệp đã chọn",
                margin + 17, contentY + 52, listW - 34, 22, 10.8f, Sketch20Style.Muted);

            const int rowH = 66, rowGap = 9, headerH = 85, bottomH = 47;
            int rowsPerPage = Math.Max(1, (listH - headerH - bottomH + rowGap) / (rowH + rowGap));
            int pages = Math.Max(1, (selectedCount + rowsPerPage - 1) / rowsPerPage);
            fileListPage = Math.Max(0, Math.Min(fileListPage, pages - 1));
            int rowStart = contentY + headerH;
            if (selectedCount > 0)
            {
                // Ít tệp: căn giữa danh sách trong khoảng trống khả dụng.
                // Nhiều tệp: dùng phân trang, không kéo dài chiều cao dòng.
                int visibleRows = Math.Min(rowsPerPage, selectedCount - fileListPage * rowsPerPage);
                int occupied = visibleRows * rowH + Math.Max(0, visibleRows - 1) * rowGap;
                int space = Math.Max(0, listH - headerH - bottomH);
                rowStart += Math.Max(0, (space - occupied) / 2);
            }
            if (selectedCount == 0)
            {
                int emptyY = rowStart + Math.Max(10, (listH - headerH - bottomH - 116) / 2);
                Card("scan-file-empty", margin + (listW - 52) / 2, emptyY, 52, 52,
                    Sketch20Style.BlueSoft, Color.Transparent, 12);
                Icon("file", margin + (listW - 20) / 2, emptyY + 16, 20, Sketch20Style.Blue);
                string emptyCaption = "Chưa có tệp nào được chọn";
                int captionW = Math.Min(listW - 48, Sketch20Style.Measure(emptyCaption, 12.2f, true) + 20);
                Text(emptyCaption, margin + (listW - captionW) / 2, emptyY + 58,
                    captionW, 27, 12.2f, Sketch20Style.Ink, true);
                string emptyNote = "Nhấn Chọn tệp hoặc kéo tệp từ Explorer vào cửa sổ.";
                int noteW = Math.Min(listW - 48, Sketch20Style.Measure(emptyNote, 10.6f) + 20);
                Text(emptyNote, margin + (listW - noteW) / 2, emptyY + 86, noteW, 35,
                    10.6f, Sketch20Style.Muted, false, true);
            }
            else
            {
                for (int row = 0; row < rowsPerPage; row++)
                {
                    int index = fileListPage * rowsPerPage + row;
                    if (index >= selectedCount) break;
                    string path = hasSampleFiles ? samplePaths[index] : files[index];
                    int top = rowStart + row * (rowH + rowGap);
                    int rx = margin + 14, rw = listW - 28;
                    Card("file-pick-" + index, rx, top, rw, rowH,
                        Color.White, ColorTranslator.FromHtml("#e4edf9"), 10);
                    int iconY = top + (rowH - 40) / 2;
                    Card("file-pick-icon-" + index, rx + 10, iconY, 40, 40,
                        Sketch20Style.BlueSoft, Color.Transparent, 10);
                    Icon("file", rx + 20, iconY + 10, 20, Sketch20Style.Blue);
                    int infoX = rx + 62;
                    int infoW = Math.Max(80, rw - 190);
                    string name = Path.GetFileName(path);
                    Text(string.IsNullOrEmpty(name) ? path : name,
                        infoX, top + 9, infoW, 24, 12, Sketch20Style.Ink, true);
                    Text(path, infoX, top + 36, infoW, 22, 10, Sketch20Style.Muted);
                    string size = hasSampleFiles ? (index == 0 ? "12.5 MB" : "8.3 KB") : FileSize(path);
                    Text(size, rx + rw - 119, top + 18, 70, 29,
                        10.7f, Sketch20Style.Muted, false, false, true);
                    if (!hasSampleFiles)
                    {
                        int removeIndex = index;
                        var remove = AdvancedSquareButton("remove-file-" + index, "",
                            rx + rw - 43, top + 16, 30,
                            () => { files.RemoveAt(removeIndex); Render(); },
                            "x", false, true, 34, true);
                        remove.AccessibleName = "Xóa tệp " + name;
                        toolTips.SetToolTip(remove, "Xóa khỏi danh sách: " + path);
                    }
                }
            }

            int pagerY = contentY + listH - 38;
            if (pages > 1)
            {
                var prev = AdvancedSquareButton("file-page-prev", "", margin + listW - 198,
                    pagerY, 35, () => { fileListPage--; Render(); },
                    "chevron-left", false, false, 31, true);
                prev.Enabled = fileListPage > 0;
                Text("Trang " + (fileListPage + 1) + "/" + pages,
                    margin + listW - 157, pagerY + 4, 103, 24,
                    10.5f, Sketch20Style.Muted, false, false, true);
                var next = AdvancedSquareButton("file-page-next", "Sau", margin + listW - 48,
                    pagerY, 35, () => { fileListPage++; Render(); },
                    "", false, false, 31, true);
                next.TextPixels = 10.2f;
                next.Enabled = fileListPage + 1 < pages;
            }
            else
                Text("Có thể chọn nhiều tệp trong một lần quét.",
                    margin + 17, pagerY + 4, listW - 34, 24, 10.2f, Sketch20Style.Muted);

            Card("scan-file-info", sideX, sideY, sideW, sideH,
                ColorTranslator.FromHtml("#f7fbff"), ColorTranslator.FromHtml("#dce7f5"), 13);
            Text("Thông tin lựa chọn", sideX + 18, sideY + 17,
                sideW - 36, 31, 14, Sketch20Style.Ink, true);
            if (twoColumns)
            {
                Card("scan-file-count", sideX + 16, sideY + 64, sideW - 32, 112,
                    Color.White, ColorTranslator.FromHtml("#e2ecfa"), 11);
                Text("TỆP ĐÃ CHỌN", sideX + 32, sideY + 79,
                    sideW - 64, 23, 10.6f, Sketch20Style.Muted, true);
                Text(selectedCount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")),
                    sideX + 32, sideY + 108, sideW - 64, 46, 26, Sketch20Style.Blue, true);
                int guidanceTop = sideY + 200;
                string[] guidance = {
                    "Chọn nhiều tệp cùng lúc để quét.",
                    "Có thể kéo thả tệp từ Explorer.",
                    "Loại bỏ tệp không muốn quét bằng dấu X."
                };
                int itemHeight = Math.Max(47, (sideH - 218) / guidance.Length);
                for (int i = 0; i < guidance.Length; i++)
                {
                    int top = guidanceTop + i * itemHeight;
                    Icon("circle-check", sideX + 21, top + 11, 17, Sketch20Style.Blue);
                    Text(guidance[i], sideX + 48, top + 3,
                        sideW - 68, itemHeight - 6, 10.8f, Sketch20Style.Muted, false, true);
                }
            }
            else
            {
                Text(selectedCount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " tệp đã chọn",
                    sideX + 18, sideY + 57, sideW - 36, 35, 21, Sketch20Style.Blue, true);
                Text("Hỗ trợ chọn nhiều tệp cùng lúc hoặc kéo thả từ Explorer.",
                    sideX + 18, sideY + 103, sideW - 36, 43,
                    10.7f, Sketch20Style.Muted, false, true);
            }
            int btnWidth = Math.Min(235, Math.Max(185, layoutWidth / 5));
            int btnX = layoutWidth - margin - btnWidth;
            Callout(margin, actionY, Math.Max(140, btnX - margin - 14),
                selectedCount == 0 ? "Hãy chọn ít nhất một tệp để bắt đầu." :
                "Sẽ quét " + selectedCount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " tệp đã chọn.",
                false, actionH);
            var start = AdvancedSquareButton("start-advanced", "Bắt đầu quét", btnX, actionY,
                btnWidth, StartAdvanced, "search", true, false, actionH);
            start.Enabled = !Model.Scanning && (Model.Sample || files.Count > 0);
            ContentHeight = mainY + mainH + 18;
        }

        // Quét toàn bộ hệ thống: danh sách ổ đĩa và phạm vi quét được chia vùng,
        // giãn đều theo kích thước cửa sổ thay vì dồn về nửa phía trên.
        private void FullScanScope(int mainY)
        {
            DriveInfo[] drives = Model.Sample ? new DriveInfo[0] : ScanEngine.GetFullScanDrives();
            string[] roots = Model.Sample ? new[] { @"C:\", @"D:\" } :
                drives.Select(d => d.Name).ToArray();
            int count = roots.Length;
            bool twoColumns = layoutWidth >= 900;
            int minimumH = twoColumns ? Math.Max(550, 294 + Math.Max(1, count) * 62 + Math.Max(0, count - 1) * 11) :
                Math.Max(710, 552 + Math.Max(1, count) * 62 + Math.Max(0, count - 1) * 11);
            int mainH = Math.Max(minimumH,
                viewport.ClientSize.Height - AdvancedCanvasTop - mainY - 30);
            const int margin = 22, gap = 16, actionH = 52;
            int actionY = mainY + mainH - actionH - 20;
            int contentY = mainY + 98;
            int contentH = actionY - contentY - 16;
            int leftW = twoColumns ? (layoutWidth - margin * 2 - gap) * 56 / 100 :
                layoutWidth - margin * 2;
            int rightX = twoColumns ? margin + leftW + gap : margin;
            int rightW = twoColumns ? layoutWidth - margin - rightX : leftW;
            int rightH = twoColumns ? contentH : 242;
            int leftH = twoColumns ? contentH : contentH - rightH - gap;
            int rightY = twoColumns ? contentY : contentY + leftH + gap;

            Card("scan-scope", 0, mainY, layoutWidth, mainH,
                Color.White, ColorTranslator.FromHtml("#dce7f5"), 17);
            Text("Quét toàn bộ hệ thống", margin, mainY + 17,
                layoutWidth - margin * 2, 36, 21, Sketch20Style.Ink, true);
            Text("Chọn các ổ đĩa bạn muốn kiểm tra.",
                margin, mainY + 56, layoutWidth - margin * 2, 26,
                12, Sketch20Style.Muted);

            Card("scan-drives-panel", margin, contentY, leftW, leftH,
                ColorTranslator.FromHtml("#fbfdff"), ColorTranslator.FromHtml("#dce7f5"), 13);
            Text("Ổ đĩa có thể quét", margin + 17, contentY + 17,
                Math.Max(120, leftW - 190), 30, 14, Sketch20Style.Ink, true);
            Check("all-drives", "Chọn tất cả", margin + leftW - 142,
                contentY + 21, 128, count > 0 && roots.All(r => !excludedDrives.Contains(r)),
                on => { foreach (string root in roots)
                    { if (on) excludedDrives.Remove(root); else excludedDrives.Add(root); } Render(); });
            Text(count == 0 ? "Chưa có ổ đĩa" :
                roots.Count(r => !excludedDrives.Contains(r)) + "/" + count + " ổ đĩa được chọn",
                margin + 17, contentY + 53, leftW - 34, 23,
                10.8f, Sketch20Style.Muted);

            const int rowGap = 11;
            int rowsTop = contentY + 91;
            int rowsArea = Math.Max(65, leftH - 108);
            int rowH = Math.Max(62, Math.Min(100,
                (rowsArea - rowGap * Math.Max(0, count - 1)) / Math.Max(1, count)));
            int usedH = count == 0 ? 0 : count * rowH + (count - 1) * rowGap;
            int firstY = rowsTop + Math.Max(0, (rowsArea - usedH) / 2);
            for (int i = 0; i < count; i++)
            {
                int top = firstY + i * (rowH + rowGap);
                string root = roots[i];
                int rx = margin + 14, rw = leftW - 28;
                Card("drive-row-" + i, rx, top, rw, rowH,
                    Color.White, ColorTranslator.FromHtml("#e4edf8"), 10);
                Check("drive-pick-" + i, "", rx + 12, top + (rowH - 24) / 2,
                    24, !excludedDrives.Contains(root),
                    on => { if (on) excludedDrives.Remove(root); else excludedDrives.Add(root); Render(); });
                Icon("hard-drive", rx + 50, top + (rowH - 22) / 2,
                    22, Sketch20Style.Blue);
                string label = Model.Sample ? i == 0 ? "Ổ hệ thống Windows (C:)" : "Ổ mạng (Z:)" :
                    DriveLabel(drives[i]) + " (" + root.TrimEnd('\\') + ")";
                double ratio = .45 + .2 * (i % 2);
                string capacity = i == 0 ? "86.4 / 254.5 GB" : "116 / 238.4 GB";
                if (!Model.Sample)
                {
                    try
                    {
                        ratio = (drives[i].TotalSize - drives[i].TotalFreeSpace) /
                            (double)Math.Max(1, drives[i].TotalSize);
                        capacity = ((drives[i].TotalSize - drives[i].TotalFreeSpace) / 1073741824.0).ToString("0.#") +
                            " / " + (drives[i].TotalSize / 1073741824.0).ToString("0.#") + " GB";
                    }
                    catch { capacity = "Sẵn sàng"; }
                }
                int textX = rx + 83;
                int textW = Math.Max(90, rw - 220);
                int labelY = top + (rowH - 47) / 2;
                Text(label, textX, labelY, textW, 25,
                    11.9f, Sketch20Style.Ink, true);
                Text(capacity, rx + rw - 134, labelY, 122, 24,
                    9.5f, Sketch20Style.Muted, false, false, true);
                int progressW = rw - 108;
                int trackY = labelY + 32;
                Card("drive-track-" + i, textX, trackY, progressW, 9,
                    Sketch20Style.Track, Color.Transparent, 4);
                Card("drive-fill-" + i, textX, trackY,
                    Math.Max(4, (int)(progressW * Math.Max(0, Math.Min(1, ratio)))),
                    9, Sketch20Style.Blue, Color.Transparent, 4);
            }
            if (count == 0)
                Text("Không có ổ đĩa sẵn sàng để quét.", margin + 24,
                    contentY + leftH / 2, leftW - 48, 34, 12, Sketch20Style.Muted,
                    false, false, true);

            Card("scan-scope-options", rightX, rightY, rightW, rightH,
                ColorTranslator.FromHtml("#f8fbff"), ColorTranslator.FromHtml("#dce7f5"), 13);
            Text("Phạm vi quét", rightX + 18, rightY + 16,
                rightW - 36, 32, 14, Sketch20Style.Ink, true);
            string[] scopes = { "Kiểm tra tệp và thư mục", "Đối chiếu chữ ký virus",
                "Phân tích dấu hiệu đáng ngờ", "Quét toàn bộ thư mục con",
                "Phát hiện phần mở rộng kép", "Theo dõi tiến trình quét" };
            if (twoColumns)
            {
                int scopeAreaH = rightH - 81;
                int itemH = Math.Max(42, (scopeAreaH - 5 * 9) / 6);
                for (int i = 0; i < scopes.Length; i++)
                {
                    int y = rightY + 65 + i * (itemH + 9);
                    Card("scope-option-" + i, rightX + 15, y, rightW - 30, itemH,
                        Color.White, ColorTranslator.FromHtml("#e5eef9"), 10);
                    Icon("circle-check", rightX + 29, y + (itemH - 18) / 2,
                        18, Sketch20Style.Blue);
                    Text(scopes[i], rightX + 55, y + (itemH - 26) / 2,
                        rightW - 80, 26, 11f, Sketch20Style.Muted);
                }
            }
            else
            {
                int spaceX = rightW / 2;
                for (int i = 0; i < scopes.Length; i++)
                {
                    int column = i % 2, row = i / 2;
                    int x = rightX + 15 + column * spaceX;
                    int y = rightY + 66 + row * 52;
                    Card("scope-option-" + i, x, y, spaceX - 20, 45,
                        Color.White, ColorTranslator.FromHtml("#e5eef9"), 9);
                    Icon("circle-check", x + 10, y + 13, 17, Sketch20Style.Blue);
                    Text(scopes[i], x + 34, y + 6,
                        spaceX - 56, 34, 10.6f, Sketch20Style.Muted, false, true);
                }
            }
            var start = AdvancedSquareButton("start-advanced", "Quét các ổ đĩa đã chọn",
                margin, actionY, layoutWidth - margin * 2,
                StartAdvanced, "search", true, false, actionH);
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
                AdvancedMode = 1; AddScanFolders(dirs, false);
            }
            else if (pickedFiles.Count > 0 && dirs.Count == 0)
            {
                AdvancedMode = 2; files = files.Concat(pickedFiles).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            else
            {
                AppDialog.Show("Không thể trộn tệp và thư mục trong cùng một lần kéo thả. " +
                    "Hãy thêm thư mục ở Quét thư mục, hoặc thêm tệp ở Quét tệp.",
                    "Chọn chế độ quét", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (Page != "advanced") Go("advanced"); else Render();
        }

        private void ChooseScope(bool folder)
        {
            if (Model.Sample)
            {
                if (folder)
                {
                    string[] sampleFolders = { @"C:\Users\User\Documents", @"C:\Users\User\Downloads", @"D:\Projects", @"D:\Backup" };
                    string sample = sampleFolders.FirstOrDefault(path => !folders.Contains(path, StringComparer.OrdinalIgnoreCase));
                    if (sample != null) AddScanFolders(new[] { sample }, false);
                }
                else files = new List<string> { "eicar-test.txt" };
                Render(); return;
            }
            if (folder)
            {
                using (var dialog = new FolderBrowserDialog { Description = "Chọn thư mục cần quét" })
                    if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                        AddScanFolders(new[] { dialog.SelectedPath }, false);
            }
            else using (var dialog = new OpenFileDialog { Title = "Chọn tệp cần quét", Multiselect = true }) if (dialog.ShowDialog(FindForm()) == DialogResult.OK) files = dialog.FileNames.ToList();
            Render();
        }
        private void StartAdvanced()
        {
            // Không còn chế độ Quét tùy chỉnh trong UI. Chế độ 3 vẫn được engine
            // sử dụng nội bộ khi chỉ chọn một phần các ổ đĩa để quét toàn bộ.
            if (AdvancedMode < 0 || AdvancedMode > 2) return;
            var targets = AdvancedMode == 1 ? folders.ToList() :
                AdvancedMode == 2 ? files.ToList() : new List<string>();
            int requestedMode = AdvancedMode;
            if (AdvancedMode == 0 && !Model.Sample)
            {
                var drives = ScanEngine.GetFullScanDrives();
                targets = drives.Where(d => !excludedDrives.Contains(d.Name)).Select(d => d.Name).ToList();
                if (targets.Count == 0) return;
                if (targets.Count < drives.Length) requestedMode = 3;
            }
            if (Actions.StartAdvanced != null) Actions.StartAdvanced(requestedMode, targets, true);
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
        private void DetailInfoStacked(Sketch20File f, int totalHeight)
        {
            int mainY = detailBodyY;
            int fileH = Math.Max(458, (totalHeight - 14) / 2);
            int riskH = Math.Max(462, totalHeight - fileH - 14);
            Card("detail-info", 0, mainY, layoutWidth, fileH);
            Text("Thông tin tệp", 20, mainY + 15, layoutWidth - 40, 35, 19, Sketch20Style.Ink, true);
            string[] labels = { "Tên tệp", "Đường dẫn đầy đủ", "Kích thước", "Loại tệp", "Mô tả", "Nhà phát hành", "Phiên bản", "Ngày tạo", "Nguồn phát hiện", "Chữ ký số", "Băm SHA-256" };
            string[] values = { f == null ? "—" : f.Name, f == null ? "—" : f.Path, f == null ? "—" : f.Size, f == null ? "—" : f.FileType, "Chưa có thông tin", "Chưa xác minh", "Chưa có thông tin", f == null ? "—" : f.Created, f == null ? "—" : f.Source, "Chưa xác minh", f == null ? "—" : f.Sha256 };
            int rowStart = mainY + 59;
            int rowHeight = Math.Max(35, (fileH - 78) / labels.Length);
            int labelW = Math.Max(116, Math.Min(167, layoutWidth / 4));
            for (int i = 0; i < labels.Length; i++)
                InfoRow(labels[i], values[i], 20, rowStart + i * rowHeight,
                    layoutWidth - 40, labelW, rowHeight, i == labels.Length - 1, i == 10);

            int riskY = mainY + fileH + 14;
            Card("detail-risk", 0, riskY, layoutWidth, riskH);
            Text("Đánh giá rủi ro", 20, riskY + 14, layoutWidth - 178, 35, 19, Sketch20Style.Ink, true);
            int badgeW = Sketch20Style.PillWidth(f == null ? "Chưa xác định" : f.Severity,
                10.8f * ReferenceTextScale, true);
            Badge(f == null ? "Chưa xác định" : f.Severity,
                layoutWidth - badgeW - 20, riskY + 19, f == null ? "info" : Kind(f.Severity));

            int bannerY = riskY + 63;
            Card("risk-card", 18, bannerY, layoutWidth - 36, 110, Sketch20Style.RedSoft, Color.Transparent, 11);
            Card("risk-icon", 32, bannerY + 20, 42, 43, Color.White, Color.Transparent, 10);
            Icon("alert", 43, bannerY + 31, 21, Sketch20Style.Red);
            Text(f == null ? "Chưa có đánh giá" : "Tệp có dấu hiệu đáng ngờ",
                86, bannerY + 12, layoutWidth - 122, 32, 13.5f, Sketch20Style.Red, true);
            Text(f == null ? "Chọn tệp để xem kết quả." :
                    "Đối chiếu các chỉ dấu, nguồn gốc và kết quả phân tích trước khi sử dụng tệp.",
                86, bannerY + 49, layoutWidth - 122, 52, 11.5f, Sketch20Style.Muted, false, true);

            int noteY = riskY + riskH - 94;
            int riskRowStart = bannerY + 123;
            int riskRowH = Math.Max(39, (noteY - 18 - riskRowStart) / 4);
            InfoRow("Mức độ rủi ro", f == null ? "—" : f.Severity,
                20, riskRowStart, layoutWidth - 40, labelW, riskRowH);
            InfoRow("Phát hiện bởi", Model.VtEngineCount.HasValue ?
                Model.VtDetections + "/" + Model.VtEngineCount + " công cụ" : "Chưa tra cứu",
                20, riskRowStart + riskRowH, layoutWidth - 40, labelW, riskRowH);
            InfoRow("Loại mối đe dọa", f == null ? "—" : f.Reason,
                20, riskRowStart + 2 * riskRowH, layoutWidth - 40, labelW, riskRowH);
            InfoRow("Lần quét gần nhất", f == null ? "—" : f.Date + " " + f.Time,
                20, riskRowStart + 3 * riskRowH, layoutWidth - 40, labelW, riskRowH, true);

            Card("recommendation", 18, noteY, layoutWidth - 36, 76, Sketch20Style.AmberSoft, Color.Transparent, 10);
            Icon("info", 33, noteY + 16, 19, Sketch20Style.Amber);
            Text("Khuyến nghị", 65, noteY + 9, layoutWidth - 101, 25, 12.5f, Sketch20Style.Ink, true);
            Text(f != null && f.Resolved ?
                    "Giữ tệp trong vùng cách ly nếu chưa chắc chắn về nguồn gốc." :
                    "Nên cách ly tệp trước khi tiếp tục sử dụng.",
                65, noteY + 37, layoutWidth - 101, 34, 11, Sketch20Style.Muted, false, true);
        }

        private void DetailInfo(Sketch20File f, int height)
        {
            int gap = 16;
            int leftW = (layoutWidth - gap) / 2;
            int rightX = leftW + gap;
            int rightW = layoutWidth - rightX;
            int mainY = detailBodyY;
            Card("detail-info", 0, mainY, leftW, height);
            Card("detail-risk", rightX, mainY, rightW, height);
            Text("Thông tin tệp", 20, mainY + 16, leftW - 40, 34, 19, Sketch20Style.Ink, true);
            Text("Đánh giá rủi ro", rightX + 20, mainY + 16, rightW - 157, 34, 19, Sketch20Style.Ink, true);
            Badge(f == null ? "Chưa xác định" : f.Severity,
                rightX + rightW - 120, mainY + 19,
                f == null ? "info" : Kind(f.Severity), 100);
            string[] labels = { "Tên tệp", "Đường dẫn đầy đủ", "Kích thước", "Loại tệp", "Mô tả", "Nhà phát hành", "Phiên bản", "Ngày tạo", "Nguồn phát hiện", "Chữ ký số", "Băm SHA-256" };
            string[] values = { f == null ? "—" : f.Name, f == null ? "—" : f.Path, f == null ? "—" : f.Size, f == null ? "—" : f.FileType, "Chưa có thông tin", "Chưa xác minh", "Chưa có thông tin", f == null ? "—" : f.Created, f == null ? "—" : f.Source, "Chưa xác minh", f == null ? "—" : f.Sha256 };
            int rowStart = mainY + 61;
            int rowH = Math.Max(35, (height - 80) / labels.Length);
            int labelW = leftW < 550 ? 131 : 148;
            for (int i = 0; i < labels.Length; i++)
                InfoRow(labels[i], values[i], 20, rowStart + i * rowH,
                    leftW - 40, labelW, rowH, i == labels.Length - 1, i == 10);

            int bannerY = mainY + 63;
            Card("risk-card", rightX + 18, bannerY, rightW - 36, 114, Sketch20Style.RedSoft, Color.Transparent, 11);
            Card("risk-icon", rightX + 32, bannerY + 21, 42, 43, Color.White, Color.Transparent, 10);
            Icon("alert", rightX + 43, bannerY + 32, 21, Sketch20Style.Red);
            Text(f == null ? "Chưa có đánh giá" : "Tệp có dấu hiệu đáng ngờ",
                rightX + 86, bannerY + 12, rightW - 124, 32, 13.5f, Sketch20Style.Red, true);
            Text(f == null ? "Chọn tệp để xem kết quả." :
                    "Đối chiếu các chỉ dấu, nguồn gốc và kết quả phân tích trước khi sử dụng tệp.",
                rightX + 86, bannerY + 49, rightW - 124, 54, 11.5f, Sketch20Style.Muted, false, true);
            int noteY = mainY + height - 94;
            int riskRowStart = bannerY + 128;
            int riskRowH = Math.Max(39, (noteY - 20 - riskRowStart) / 4);
            InfoRow("Mức độ rủi ro", f == null ? "—" : f.Severity,
                rightX + 20, riskRowStart, rightW - 40, 136, riskRowH);
            InfoRow("Phát hiện bởi", Model.VtEngineCount.HasValue ?
                Model.VtDetections + "/" + Model.VtEngineCount + " công cụ" : "Chưa tra cứu",
                rightX + 20, riskRowStart + riskRowH, rightW - 40, 136, riskRowH);
            InfoRow("Loại mối đe dọa", f == null ? "—" : f.Reason,
                rightX + 20, riskRowStart + 2 * riskRowH, rightW - 40, 136, riskRowH);
            InfoRow("Lần quét gần nhất", f == null ? "—" : f.Date + " " + f.Time,
                rightX + 20, riskRowStart + 3 * riskRowH, rightW - 40, 136, riskRowH, true);

            Card("recommendation", rightX + 18, noteY, rightW - 36, 77, Sketch20Style.AmberSoft, Color.Transparent, 10);
            Icon("info", rightX + 34, noteY + 16, 19, Sketch20Style.Amber);
            Text("Khuyến nghị", rightX + 65, noteY + 9, rightW - 103, 25, 12.5f, Sketch20Style.Ink, true);
            Text(f != null && f.Resolved ?
                    "Giữ tệp trong vùng cách ly nếu chưa chắc chắn về nguồn gốc." :
                    "Nên cách ly tệp trước khi tiếp tục sử dụng.",
                rightX + 65, noteY + 38, rightW - 103, 32, 11, Sketch20Style.Muted, false, true);
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
            int bad = hasCounts ? Model.VtDetections.Value : 0, total = hasCounts ? Model.VtEngineCount.Value : 0,
                clean = Model.VtClean ?? (Model.Sample ? Math.Max(0, total - bad) : 0),
                suspicious = Model.VtSuspicious ?? 0;
            Text("Kết quả phân tích trên VirusTotal", 22, mainY + 11,
                Math.Max(200, layoutWidth - 270), 34,
                layoutWidth < 900 ? 16.5f : 18f, Sketch20Style.Ink, true);
            if (layoutWidth >= 850 && hasReport)
            {
                int updateX = Math.Max(360, layoutWidth - 430);
                int updateW = Math.Max(120, layoutWidth - 228 - updateX);
                Text("Cập nhật: " + Model.VtTime, updateX, mainY + 19, updateW, 22, 9.5f, Sketch20Style.Muted, false, false, true);
            }
            string queryText = needsKey ? "Thiết lập API key" : Model.VtQuerying ? "Đang tra cứu..." :
                Model.VtLookedUp ? "Tra cứu lại" : "Tra cứu VirusTotal";
            var query = Button("query-virustotal", queryText, layoutWidth - 206, mainY + 11, 184,
                () => { Command(needsKey ? "vt-config" : "vt-query"); }, "refresh", true, false, 37);
            query.Enabled = f != null && !Model.VtQuerying;
            bool vtWarning = Model.VtHasError || Model.VtNotFound;
            bool compactAlert = height < (layoutWidth < 900 ? 640 : 600);
            Card("vt-alert", 22, mainY + (compactAlert ? 58 : 62), layoutWidth - 44,
                compactAlert ? 69 : 82,
                hasReport && bad > 0 ? Sketch20Style.RedSoft : vtWarning ? Sketch20Style.AmberSoft : Sketch20Style.BlueSoft,
                Color.Transparent, 10);
            Icon(hasReport && bad > 0 ? "sigma" : "info", 40, mainY + (compactAlert ? 77 : 84), 33,
                hasReport && bad > 0 ? Sketch20Style.Red : vtWarning ? Sketch20Style.Amber : Sketch20Style.Blue);
            string summary = f == null ? "Chọn một tệp để xem kết quả phân tích." :
                needsKey ? "Chưa cấu hình API key VirusTotal." :
                Model.VtQuerying ? "Đang tự động tra cứu SHA-256 trên VirusTotal..." :
                Model.VtUploaded ? "Đã gửi mẫu, VirusTotal đang chờ phân tích. Tra lại sau ít phút." :
                Model.VtNotFound ? "Chưa có báo cáo trên VirusTotal (không đồng nghĩa an toàn)." :
                Model.VtHasError ? "Không thể tra cứu VirusTotal. Kiểm tra thông báo lỗi bên dưới." :
                hasReport ? bad + " trong " + total + " công cụ đánh dấu tệp có dấu hiệu độc hại" :
                Model.VtLookedUp ? "Đã nhận báo cáo; chưa có số liệu phân tích từ các công cụ." :
                "Ứng dụng sẽ tự động tra cứu khi mở chi tiết tệp.";
            Text(summary, 92, mainY + (compactAlert ? 62 : 69), layoutWidth - 136,
                compactAlert ? 29 : 33, compactAlert ? 12.3f : 13.5f, hasReport && bad > 0 ? Sketch20Style.Red : Sketch20Style.Ink, true);
            Text(hasReport ? "Tệp được phân tích trên nền tảng VirusTotal. Đối chiếu kết quả từ các công cụ bảo mật." : Model.VtSummary,
                92, mainY + (compactAlert ? 98 : 107), layoutWidth - 136,
                compactAlert ? 24 : 32, compactAlert ? 10.2f : 11f, Sketch20Style.Muted, false, true);
            string[] labels = { "Phát hiện độc hại", "Không phát hiện", "Nghi ngờ", "Tổng số công cụ" }, vals = { hasCounts ? bad.ToString() : "—", hasCounts ? clean.ToString() : "—", hasCounts ? suspicious.ToString() : "—", hasCounts ? total.ToString() : "—" }, icons = { "shield-alert", "circle-check", "info", "list-checks" };
            Color[] colors = { Sketch20Style.Red, Sketch20Style.Green, Sketch20Style.Muted, Sketch20Style.Blue };
            int gap = 11;
            int statColumns = layoutWidth < 900 ? 2 : 4;
            int statRows = 4 / statColumns;
            // Khi chiều cao cửa sổ không đủ, các thống kê tự thu lại để dành chỗ
            // cho bảng kết quả, thay vì ép cả trang cao 650px và bật scrollbar ngoài.
            bool vtCompactHeight = height < (statColumns == 2 ? 640 : 600);
            int statH = vtCompactHeight ? (statColumns == 2 ? 62 : 64) : (statColumns == 2 ? 80 : 76);
            int statTop = vtCompactHeight ? 134 : 158;
            int statGap = vtCompactHeight ? 8 : 12;
            int cw = (layoutWidth - 44 - gap * (statColumns - 1)) / statColumns;
            for (int i = 0; i < 4; i++)
            {
                int col = i % statColumns, row = i / statColumns;
                int x = 22 + col * (cw + gap);
                int yy = mainY + statTop + row * (statH + statGap);
                Card("vt-stat-" + i, x, yy, cw, statH);
                Icon(icons[i], x + 15, yy + (statH - 25) / 2, 25, colors[i]);
                Text(i == 0 && hasCounts ? vals[i] + " / " + total : i == 2 && Model.Sample ? "0" : vals[i],
                    x + 52, yy + (vtCompactHeight ? 5 : 9), cw - 63,
                    vtCompactHeight ? 29 : 35, vtCompactHeight ? 18f : 22f, colors[i], true);
                Text(labels[i], x + 52, yy + statH - (vtCompactHeight ? 25 : 30),
                    cw - 60, 22, 10.3f, Sketch20Style.Muted);
            }
            int listY = mainY + statTop + statRows * statH + (statRows - 1) * statGap + (vtCompactHeight ? 9 : 12);
            Text("Kết quả từ các công cụ bảo mật", 22, listY, layoutWidth - 288, 33, 15, Sketch20Style.Ink, true);
            SearchBox("engine-search", engineSearch, layoutWidth - 252, listY + 1, 230, value => { engineSearch = value; engineOffset = 0; });
            int head = listY + (vtCompactHeight ? 42 : 46);
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
            int engineRowH = vtCompactHeight ? 40 : Sketch20Style.TableCompactRowHeight;
            int engineGap = vtCompactHeight ? 5 : Sketch20Style.TableRowGap;
            int rowsY = head + Sketch20Style.TableHeaderHeight + (vtCompactHeight ? 6 : Sketch20Style.TableOuterPaddingY);
            // Không còn hai nút ở cuối bảng: dành toàn bộ chiều cao còn lại
            // (chỉ giữ lề đáy 14px) cho kết quả từ các công cụ bảo mật.
            int rowsAvailable = Math.Max(0, mainY + height - 14 - rowsY);
            int page = Math.Max(1, (rowsAvailable + engineGap) / (engineRowH + engineGap));
            engineOffset = Math.Max(0, Math.Min(engineOffset, Math.Max(0, all.Count - page)));
            var rows = all.Skip(engineOffset).Take(page).ToList();
            int rowsHeight = Math.Max(engineRowH, page * (engineRowH + engineGap) - engineGap);
            AddTableScroll("engine-scroll", layoutWidth - 37, rowsY, rowsHeight, all.Count, page, engineOffset, value => engineOffset = value);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; int yy = rowsY + i * (engineRowH + engineGap);
                string category = row.Length > 1 ? row[1] : "unknown";
                string result = row.Length > 2 ? row[2] : "—";
                string updateDate = row.Length > 3 ? row[3] : "—";
                bool good = category == "undetected" || category == "harmless";
                bool badEngine = category == "malicious";
                bool suspiciousEngine = category == "suspicious";
                Card("vt-engine-row-" + i, rowX, yy, engineRowW, engineRowH, Color.White, ColorTranslator.FromHtml("#e5edf8"), 10);
                Text(row.Length == 0 ? "—" : row[0], c1, yy, engineCols[0] - 10, engineRowH, 11.2f, Sketch20Style.Ink, true);
                string statusText = good ? "Không phát hiện" : badEngine ? "Độc hại" : suspiciousEngine ? "Nghi ngờ" : category;
                Color statusInk = good ? Sketch20Style.Green : badEngine ? Sketch20Style.Red : Sketch20Style.Amber;
                int statusW = Math.Min(Math.Max(1, engineCols[1] - 16), Sketch20Style.PillWidth(statusText, 10.2f * ReferenceTextScale, true));
                int statusY = yy + (engineRowH - 24) / 2;
                Card("vt-engine-status-" + i, c2, statusY, statusW, 24, good ? Sketch20Style.GreenSoft : badEngine ? Sketch20Style.RedSoft : Sketch20Style.AmberSoft, Color.Transparent, 12);
                Canvas.Drawing.Add(g => { using (var dot = new SolidBrush(statusInk)) g.FillEllipse(dot, c2 + 8, statusY + 6 + bodyOffset, 12, 12); });
                Icon(good ? "check" : "alert", c2 + 10, statusY + 8, 8, Color.White);
                Text(statusText, c2 + 26, statusY, Math.Max(1, statusW - 34), 24, 10.2f, statusInk, true);
                Text(good ? "—" : result, c3, yy, engineCols[2] - 12, engineRowH, 10.2f, Sketch20Style.Ink);
                Text(updateDate, c4, yy, engineCols[3] - 8, engineRowH, 9.8f, Sketch20Style.Muted);
            }
            if (rows.Count == 0)
            {
                int emptyH = Math.Max(36, Math.Min(68, rowsAvailable));
                Card("engine-empty", rowX, rowsY, layoutWidth - 44, emptyH, Sketch20Style.Background, Color.Transparent, 8);
                Text("Chưa có kết quả phù hợp để hiển thị.", 40, rowsY + 5, layoutWidth - 80,
                    Math.Max(25, emptyH - 10), 11.5f, Sketch20Style.Muted);
            }

        }

        private void DetailBehavior(Sketch20File f, int height)
        {
            Card("detail-behavior", 0, detailBodyY, layoutWidth, height,
                Color.White, Sketch20Style.Border, 14);
            Icon("activity", 22, detailBodyY + 25, 27);
            Text("Chỉ số hành vi", 70, detailBodyY + 10, layoutWidth - 92,
                40, 20, Sketch20Style.Ink, true);
            Text("Phân tích các hành vi quan sát được trong quá trình thực thi tệp.",
                70, detailBodyY + 49, layoutWidth - 92, 27, 12, Sketch20Style.Muted);
            string[] icons = { "settings", "power", "activity", "globe", "alert" };
            Color[] colors = { Sketch20Style.Blue, Sketch20Style.Green,
                ColorTranslator.FromHtml("#7c3aed"), Sketch20Style.Amber, Sketch20Style.Red };
            Color[] soft = { Sketch20Style.BlueSoft, Sketch20Style.GreenSoft,
                ColorTranslator.FromHtml("#f0eaff"), Sketch20Style.AmberSoft, Sketch20Style.RedSoft };
            int count = Model.Behavior.Count;
            if (count == 0)
            {
                string emptyText = "Chưa có tệp được chọn để phân tích.";
                int emptyW = Math.Min(layoutWidth - 44,
                    Math.Max(340, Sketch20Style.Measure(emptyText, 12) + 28));
                Text(emptyText, (layoutWidth - emptyW) / 2,
                    detailBodyY + Math.Max(90, height / 2 - 18),
                    emptyW, 40, 12, Sketch20Style.Muted);
                return;
            }
            bool compact = layoutWidth < 900;
            int behaviorGap = 12;
            int behaviorRowH = compact ? 112 :
                Math.Max(75, Math.Min(96, (height - 120 - (count - 1) * behaviorGap) / Math.Max(1, count)));
            int behaviorGroupH = count * behaviorRowH + (count - 1) * behaviorGap;
            int behaviorStartY = detailBodyY + 90 + Math.Max(0,
                (height - 112 - behaviorGroupH) / 2);
            for (int i = 0; i < count; i++)
            {
                var row = Model.Behavior[i];
                int y = behaviorStartY + i * (behaviorRowH + behaviorGap);
                Card("behavior-row-" + i, 18, y, layoutWidth - 36, behaviorRowH,
                    Sketch20Style.Background, Sketch20Style.Border, 9);
                Card("behavior-icon-" + i, 30, y + (behaviorRowH - 47) / 2,
                    47, 47, soft[i % soft.Length], Color.Transparent, 9);
                Icon(icons[i % icons.Length], 42, y + (behaviorRowH - 23) / 2,
                    23, colors[i % colors.Length]);

                string value = row.Length > 1 ? row[1] : "—";
                string kind = row.Length > 2 && (row[2] == "risk" || row[2] == "warn" || row[2] == "safe") ? row[2] :
                    value.IndexOf("chưa", StringComparison.OrdinalIgnoreCase) >= 0 ? "safe" :
                    value.IndexOf("có", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("đáng", StringComparison.OrdinalIgnoreCase) >= 0 ? "risk" : "warn";
                if (compact)
                {
                    Text(row.Length > 0 ? row[0] : "—", 94, y + 10,
                        layoutWidth - 290, 27, 13, Sketch20Style.Ink, true);
                    Text(row.Length > 3 ? row[3] : "Chỉ dấu được ghi nhận",
                        94, y + 40, layoutWidth - 270, 24, 10.5f, Sketch20Style.Muted);
                    Text(value, 94, y + 70, layoutWidth - 200, 30,
                        11.5f, Sketch20Style.Ink, false, true);
                }
                else
                {
                    int labelY = y + (behaviorRowH - 64) / 2 + 7;
                    Text(row.Length > 0 ? row[0] : "—", 94, labelY, 190, 27, 13, Sketch20Style.Ink, true);
                    Text(row.Length > 3 ? row[3] : "Chỉ dấu được ghi nhận", 94,
                        labelY + 31, Math.Max(150, layoutWidth - 590), 23, 10.5f, Sketch20Style.Muted);
                    Text(value, layoutWidth - 390, y + (behaviorRowH - 48) / 2,
                        210, 48, 11.5f, Sketch20Style.Ink, false, true);
                }
                Badge(kind == "risk" ? "Đáng ngờ" : kind == "safe" ? "An toàn" : "Cần kiểm tra",
                    layoutWidth - 160, y + (behaviorRowH - 24) / 2, kind, 116);
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
            int extraRowH=Math.Max(53,(height-85)/labels.Length);
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i, yy = mainY + 67 + i * extraRowH;
                int innerY = yy + Math.Max(3,(extraRowH-44)/2);
                Card("extra-icon-" + i, 22, innerY + 4, 35, 36, Sketch20Style.Background, Color.Transparent, 10);
                Icon(icons[i], 31, innerY + 13, 17, Sketch20Style.Muted);
                Text(labels[i], 75, innerY, 138, 44, 12, Sketch20Style.Ink, true);
                if (i == 3) Badge(values[i], 230, innerY + 11, "warn", 225);
                else if (i == 5) Badge(values[i], 230, innerY + 11, f != null && f.Resolved ? "risk" : "info", 170);
                else Text(values[i], 230, innerY, layoutWidth - 302, 44, i == 1 || i == 2 ? 11 : 12, Sketch20Style.Ink, false, false, false, i == 1 || i == 2);
                if (i < 6) Line(22, yy + extraRowH - 2, layoutWidth - 44);
                if (i == 0 || i == 1 || i == 2 || i == 6)
                {
                    Action copy = () => { if (f != null && !string.IsNullOrWhiteSpace(values[index])) Clipboard.SetText(values[index]); };
                    Button("copy-extra-" + i, "", layoutWidth - 58, innerY + 5, 34, copy, "files", false, false, 33);
                    var item = new ToolStripMenuItem("Sao chép " + labels[i].ToLowerInvariant()); item.Click += delegate { copy(); }; menu.Items.Add(item);
                }
            }
            Canvas.ContextMenuStrip = menu;
        }
        private void SearchBox(string name, string value, int x, int y, int width, Action<string> change)
        {
            // Tìm kiếm ở tab VirusTotal dùng góc vuông; các màn hình khác giữ nguyên.
            Card(name + "-border", x, y, width, 34, Color.White, null,
                name == "engine-search" ? 0 : 14);
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
            readonly bool squareCorners;
            readonly int rowHeight = 34, pad = 6, visible;
            int selected, hover = -1, offset;

            internal SketchSelectPopup(string[] values, int current, int popupWidth, int maxHeight,
                bool risk, Action<int> onPicked, bool square = false)
            {
                squareCorners = square;
                choices = values ?? new string[0]; picked = onPicked;
                selected = Math.Max(0, Math.Min(current, Math.Max(0, choices.Length - 1)));
                accent = risk ? ColorTranslator.FromHtml("#d74d68") : Sketch20Style.Blue;
                accentSoft = risk ? ColorTranslator.FromHtml("#fff0f4") : ColorTranslator.FromHtml("#edf5ff");
                border = risk ? ColorTranslator.FromHtml("#eadce1") : ColorTranslator.FromHtml("#d9e7f7");
                visible = Math.Max(1, Math.Min(choices.Length, Math.Min(8, Math.Max(1, (maxHeight - pad * 2) / rowHeight))));
                offset = Math.Max(0, Math.Min(selected, Math.Max(0, choices.Length - visible)));
                FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
                BackColor = Color.White; KeyPreview = true; DoubleBuffered = true; AutoScaleMode = AutoScaleMode.Dpi;
                Font = Theme.DefaultUiFont;
                ClientSize = new Size(Math.Max(120, popupWidth), pad * 2 + visible * rowHeight);
                Deactivate += delegate { Close(); };
                KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
                Shown += delegate { UpdateShape(); };
                Resize += delegate { UpdateShape(); };
            }

            void UpdateShape()
            {
                if (Width < 2 || Height < 2) return;
                if (squareCorners) { Region = null; return; }
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
                Sketch20Style.Round(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1),
                    squareCorners ? 0 : 12, Color.White, border);
                for (int row = 0; row < visible; row++)
                {
                    int index = offset + row; if (index >= choices.Length) break;
                    int y = pad + row * rowHeight; bool active = index == selected, hot = index == hover;
                    if (active || hot)
                        Sketch20Style.Round(e.Graphics, new Rectangle(6, y + 2, Width - 12, rowHeight - 4),
                            squareCorners ? 0 : 8,
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
            bool square = Page == "threats" || name == "quarantine-sort";
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
                Sketch20Style.Round(e.Graphics, new Rectangle(0, 0, Math.Max(1, width - 1), Math.Max(1, height - 1)),
                    square ? 0 : (rich ? 10 : 9), fill, edge);
                if (rich)
                {
                    int tileSize = height <= 34 ? 24 : 28;
                    int tileY = (height - tileSize) / 2;
                    int iconSize = tileSize <= 24 ? 12 : 14;
                    int iconPad = (tileSize - iconSize) / 2;
                    int textX = 8 + tileSize + 9;
                    int chevronX = width - tileSize - 8;
                    Sketch20Style.Round(e.Graphics, new Rectangle(8, tileY, tileSize, tileSize),
                        square ? 0 : 7, accentSoft, Color.Transparent);
                    Sketch20Style.Icon(e.Graphics, leadingIcon, new Rectangle(8 + iconPad, tileY + iconPad, iconSize, iconSize), accent);
                    Sketch20Style.Text(e.Graphics, value, new Rectangle(textX, 0, Math.Max(1, chevronX - textX - 6), height), pixels, Sketch20Style.Ink, true);
                    Sketch20Style.Round(e.Graphics, new Rectangle(chevronX, tileY, tileSize, tileSize),
                        square ? 0 : 7,
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
                }, square);
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
