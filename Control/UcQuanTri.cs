using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// Admin-only technical operations. No end-user scan history, quarantine, or
    /// user file management is exposed in this page. Existing SQL schema is untouched.
    /// </summary>
    public sealed partial class UcQuanTri : UserControl
    {
        private static readonly Color Background = Color.FromArgb(246, 249, 253);
        private static readonly Color Ink = Color.FromArgb(32, 49, 75);
        private static readonly Color Muted = Color.FromArgb(105, 122, 147);
        private static readonly Color Primary = Color.FromArgb(33, 103, 217);
        private static readonly string[] Titles = {
            "Tổng quan quản trị", "Quản lý chữ ký", "Kiểm duyệt chữ ký", "Nguồn dữ liệu mã độc",
            "Cấu hình bộ máy", "Quy tắc phát hiện", "Chất lượng dữ liệu", "Giám sát dịch vụ",
            "Giám sát hiệu năng", "Kiểm thử antivirus", "Quản lý cập nhật", "Nhật ký quản trị",
            "Sao lưu và phục hồi", "Chẩn đoán hệ thống", "Quản lý VirusTotal" };
        private static readonly string[] Descriptions = {
            "Tình trạng dữ liệu, bộ máy và kết nối nội bộ. Không chứa lịch sử cá nhân.",
            "Tìm kiếm, bổ sung/kiểm tra và bật/tắt chữ ký đã xác minh.",
            "Kiểm tra bằng chứng và phê duyệt kích hoạt chữ ký, có nhật ký thay đổi.",
            "Điều hành dữ liệu MalwareBazaar, CSV, nhập mẫu và chính sách gửi mẫu.",
            "Điều chỉnh các công tắc bộ máy thực sự được hỗ trợ; không giả lập hiệu lực.",
            "Xem phân bố chữ ký và chính sách phát hiện. Không áp ngưỡng không được engine hỗ trợ.",
            "Phát hiện hash sai định dạng, trùng lặp hoặc chữ ký thiếu thông tin.",
            "Theo dõi trạng thái của các thành phần bảo vệ Windows và dịch vụ SQL.",
            "Xem RAM, số luồng, thời gian CPU và tình trạng tài nguyên tiến trình.",
            "Thực hiện kiểm thử an toàn hash và chẩn đoán thành phần đã cài.",
            "Điều khiển tự động cập nhật chữ ký, nhập nguồn dữ liệu mới.",
            "Xem nhật ký quản trị mã hóa bằng DPAPI cho tài khoản Windows hiện tại.",
            "Sao lưu database và xác minh tệp backup trên máy chủ SQL; phục hồi có xác nhận.",
            "Kiểm tra SQL, kho cấu hình, Task Scheduler, API và quyền thư mục.",
            "Quản lý API key, kiểm tra kết nối, tự tra cứu hash và việc gửi mẫu." };
        private readonly FlowLayoutPanel navigation = new FlowLayoutPanel();
        private readonly Label heading = new Label();
        private readonly Label description = new Label();
        private readonly Label status = new Label();
        private readonly FlowLayoutPanel actions = new FlowLayoutPanel();
        private readonly DataGridView grid = new DataGridView();
        private readonly TextBox search = new TextBox();
        private readonly Button refresh = new Button();
        // Trang quản trị chính sử dụng bố cục thẻ/công tắc giống trang Cài đặt.
        // Các module quản trị cũ được giữ dưới mục Công cụ nâng cao.
        private readonly Panel homeHost = new Panel();
        private Panel adminToolbar;
        private Label adminToolbarTitle;
        private TableLayoutPanel adminHomeGrid;
        private TableLayoutPanel adminBottomGrid;
        private bool homeLayoutBusy;
        private bool adminBottomStacked;
        private System.Windows.Forms.Control detailHost;
        private Panel policiesCard;
        private Panel virusTotalCard;
        private Panel signaturesCard;
        private Label updateStatus;
        private Label samplesStatus;
        private Label vtStatus;
        private Label signatureStatus;
        private Button updateButton;
        private Button samplesButton;
        private Button vtAutoButton;
        private Button adminLogoutButton;
        private bool renderingHome;
        private int homeDataSequence;
        private int selected;
        private int sequence;
        private bool loading;
        private bool dirty;
        public Action<string> NavigateRequested { get; set; }

        public UcQuanTri()
        {
            Name = "UcQuanTri"; Dock = DockStyle.Fill; BackColor = Background;
            Font = new Font(Theme.UiFontFamily, 10.35f); AutoScaleMode = AutoScaleMode.Font;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 218));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);
            detailHost = root;
            navigation.Dock = DockStyle.Fill; navigation.FlowDirection = FlowDirection.TopDown;
            navigation.WrapContents = false; navigation.AutoScroll = true; navigation.BackColor = Color.White;
            navigation.Padding = new Padding(5, 8, 5, 7); navigation.Resize += (s,e) => ResizeNav();
            root.Controls.Add(navigation, 0, 0);
            for (int i = 0; i < Titles.Length; i++)
            {
                int module = i;
                var item = MakeButton(Titles[i], () => Choose(module));
                item.Tag = i; item.Height = 42; item.Margin = new Padding(2, 2, 2, 3);
                navigation.Controls.Add(item);
            }
            var back = MakeButton("← Về trung tâm quản trị", ShowHome);
            back.Height = 42; back.BackColor = Color.FromArgb(232, 243, 255);
            navigation.Controls.Add(back);
            navigation.Controls.SetChildIndex(back, 0);
            var exit = MakeButton("Đăng xuất quản trị", () => { AdminSecurity.Lock(); if (NavigateRequested != null) NavigateRequested("overview"); });
            exit.Height = 42; exit.BackColor = Color.FromArgb(254, 240, 240);
            navigation.Controls.Add(exit);
            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                Padding = new Padding(15, 4, 5, 3) };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 89));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.Controls.Add(main, 1, 0);
            var top = new Panel { Dock = DockStyle.Fill };
            heading.Text = Titles[0]; heading.Font = new Font(Theme.UiFontFamily, 20.25f, FontStyle.Bold);
            heading.ForeColor = Ink; heading.SetBounds(0, 4, 820, 43);
            heading.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            description.ForeColor = Muted; description.SetBounds(0, 50, 840, 36);
            description.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            top.Controls.Add(heading); top.Controls.Add(description); main.Controls.Add(top, 0, 0);
            var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 103));
            actions.Dock = DockStyle.Fill; actions.AutoScroll = true; actions.WrapContents = false;
            actions.Margin = new Padding(0, 0, 4, 0);
            toolbar.Controls.Add(actions, 0, 0);
            search.Dock = DockStyle.Fill; search.Margin = new Padding(3, 8, 3, 11);
            search.AccessibleDescription = "Tìm tên hoặc SHA-256, nhấn Enter";
            search.KeyDown += (s,e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; RefreshData(); } };
            toolbar.Controls.Add(search, 1, 0);
            refresh.Text = "Làm mới"; refresh.Dock = DockStyle.Fill; refresh.Margin = new Padding(5, 7, 0, 10);
            refresh.Click += (s,e) => RefreshData(); toolbar.Controls.Add(refresh, 2, 0);
            main.Controls.Add(toolbar, 0, 1);
            grid.Dock = DockStyle.Fill; grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.None;
            grid.ReadOnly = true; grid.MultiSelect = false; grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowTemplate.Height = 31; grid.ColumnHeadersHeight = 37;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(237, 244, 254);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Ink;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(215, 231, 252);
            grid.DefaultCellStyle.SelectionForeColor = Ink;
            main.Controls.Add(grid, 0, 2);
            status.Dock = DockStyle.Fill; status.ForeColor = Muted;
            status.TextAlign = ContentAlignment.MiddleLeft; main.Controls.Add(status, 0, 3);
            Disposed += (s,e) => sequence++;
            ResizeNav();
            BuildAdminHome();
            ShowHome();
        }
        // Giao diện Quản trị được sắp xếp theo các thẻ giống trang Cài đặt:
        // hai công tắc chính, VirusTotal, dữ liệu chữ ký và công cụ bảo trì.
        private void BuildAdminHome()
        {
            homeHost.Dock = DockStyle.Fill;
            homeHost.AutoScroll = false; // Trang quản trị co giãn theo kích thước thực, không có thanh cuộn.
            homeHost.BackColor = Background;
            homeHost.Padding = new Padding(0);
            homeHost.Resize += (s, e) => LayoutHome();
            Controls.Add(homeHost);

            // Không hiển thị tiêu đề và mô tả "Trung tâm quản trị".
            // Bộ lọc ngày chuyển vào thẻ Thống kê hệ thống.

            policiesCard = HomeCard("Chính sách quản trị", "Hai thiết lập quan trọng: giữ nguyên cơ chế lưu và xử lý hiện có.");
            updateStatus = HomeStatus();
            samplesStatus = HomeStatus();
            updateButton = HomeToggleButton(() => { Safe(ToggleAutoUpdate); RefreshHomeState(); });
            samplesButton = HomeToggleButton(() => { Safe(ToggleSamples); RefreshHomeState(); });
            AddSettingLine(policiesCard, "Tự động cập nhật chữ ký",
                "Tự động kiểm tra và nhập dữ liệu chữ ký theo chính sách đang sử dụng.", 84,
                updateStatus, updateButton);
            AddSettingLine(policiesCard, "Gửi mẫu tệp nghi ngờ",
                "Quản lý quyền gửi mẫu để phân tích; khi tải lên VirusTotal vẫn yêu cầu xác nhận riêng.", 165,
                samplesStatus, samplesButton);

            virusTotalCard = HomeCard("Quản lý VirusTotal", "Cấu hình API v3 và điều khiển tra cứu SHA-256 từ ứng dụng.");
            vtStatus = HomeStatus();
            var apiTitle = AddText(virusTotalCard, "Khóa API", 23, 91, 130, 28, 11.4f, true, Ink);
            apiTitle.Name = "vt-api-title";
            vtStatus.SetBounds(155, 91, 320, 30);
            virusTotalCard.Controls.Add(vtStatus);
            AddHomeButton(virusTotalCard, "Cấu hình API", () => {
                Safe(() => Dialog(new VirusTotalConfigForm(), "vt-config")); RefreshHomeState();
            }, 23, 137, 160);
            AddHomeButton(virusTotalCard, "Thông tin VirusTotal", () => Choose(14), 195, 137, 188);
            vtAutoButton = HomeToggleButton(() => { Safe(ToggleAutoVT); RefreshHomeState(); });
            AddSettingLine(virusTotalCard, "Tự động tra cứu SHA-256",
                "Chỉ dùng hash khi tra cứu; không tự tải lên nội dung tệp.", 187,
                new Label { Visible = false }, vtAutoButton);

            signaturesCard = HomeCard("Dữ liệu chữ ký & cập nhật", "Kiểm soát nguồn dữ liệu, kiểm duyệt và cập nhật chữ ký mã độc.");
            signatureStatus = HomeStatus();
            signatureStatus.SetBounds(23, 78, 630, 29);
            signaturesCard.Controls.Add(signatureStatus);
            AddHomeButton(signaturesCard, "Quản lý chữ ký", () => Choose(1), 23, 119, 169);
            AddHomeButton(signaturesCard, "Kiểm duyệt", () => Choose(2), 204, 119, 150);
            AddHomeButton(signaturesCard, "Cập nhật thủ công", () => Safe(() => {
                SignatureUpdateProgressForm.Open(FindForm());
                AdminAudit.Write("signature-update", "Mở trình cập nhật thủ công");
            }), 366, 119, 185);
            AddHomeButton(signaturesCard, "Kiểm tra dữ liệu", () => Choose(6), 23, 178, 169);
            AddHomeButton(signaturesCard, "Nguồn dữ liệu", () => Choose(3), 204, 178, 150);
            // Nhập chữ ký từ file/hash vào AntivirusDB bằng công cụ đã có.
            // Khác với công tắc "Gửi mẫu tệp nghi ngờ" (chính sách gửi mẫu):
            // thao tác ở đây xử lý tại máy, không tự upload lên VirusTotal.
            var importSamples = AddHomeButton(signaturesCard, "Thêm chữ ký từ tệp", () => Safe(() =>
            {
                AdminAudit.Write("sample-import-open", "Mở công cụ nhập chữ ký từ tệp nghi ngờ");
                SampleSubmissionForm.Open(FindForm());
                RefreshHomeState();
            }), 366, 178, 185);
            importSamples.Name = "admin-home-import-sample";
            importSamples.Font = new Font(Theme.UiFontFamily, 10.75f, FontStyle.Bold);
            importSamples.ForeColor = Primary;

            // Thanh công cụ trên cùng chứa mọi thao tác: ngày, cập nhật, làm mới, đăng xuất.
            // Panel tự vẽ viền, có DoubleBuffer + ResizeRedraw để viền cũ
            // không còn lưu lại thành các đường sọc bên trong thẻ khi resize.
            adminToolbar = new AdminBorderPanel { Name = "admin-top-toolbar", BackColor = Color.White };
            homeHost.Controls.Add(adminToolbar);
            adminToolbarTitle = new Label {
                Name = "admin-toolbar-title",
                AutoSize = false,
                Text = "Thống kê hệ thống",
                ForeColor = Ink,
                BackColor = Color.White,
                Font = new Font(Theme.UiFontFamily, 17.45f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            adminToolbar.Controls.Add(adminToolbarTitle);
            adminLogoutButton = AddHomeButton(adminToolbar, "Đăng xuất quản trị", () => {
                AdminSecurity.Lock();
                if (NavigateRequested != null) NavigateRequested("overview");
            }, 0, 0, 182);
            adminLogoutButton.Name = "admin-home-logout";
            adminLogoutButton.BackColor = Color.FromArgb(253, 238, 239);
            adminLogoutButton.ForeColor = Color.FromArgb(160, 55, 65);
            adminLogoutButton.FlatAppearance.BorderColor = Color.FromArgb(244, 212, 217);
            adminLogoutButton.Font = new Font(Theme.UiFontFamily, 10.85f, FontStyle.Bold);

            BuildDashboardCard();
            // Tái sử dụng đúng các control và sự kiện hiện có: không tạo bộ lọc mới.
            adminToolbar.Controls.Add(dashboardPeriod);
            adminToolbar.Controls.Add(dashboardUpdatedLabel);
            adminToolbar.Controls.Add(dashboardRefresh);
            adminToolbar.Controls.Add(dashboardDetails);
            // Cấu trúc thật thay cho các SetBounds ở cấp màn hình: mỗi phần có một ô riêng.
            // Không cho biểu đồ, thanh lọc và các thẻ điều khiển chồng lên nhau khi đổi DPI.
            adminHomeGrid = new TableLayoutPanel {
                Name = "admin-home-responsive-grid", Dock = DockStyle.Fill,
                ColumnCount = 1, RowCount = 3, BackColor = Background,
                Padding = new Padding(6), Margin = new Padding(0),
                AutoScroll = false, AutoSize = false
            };
            adminHomeGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            adminHomeGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
            adminHomeGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            adminHomeGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 300f));
            adminToolbar.Dock = DockStyle.Fill;
            adminToolbar.Margin = new Padding(6, 0, 6, 9);
            dashboardCard.Dock = DockStyle.Fill;
            dashboardCard.Margin = new Padding(6, 0, 6, 10);
            adminHomeGrid.Controls.Add(adminToolbar, 0, 0);
            adminHomeGrid.Controls.Add(dashboardCard, 0, 1);

            adminBottomGrid = new TableLayoutPanel {
                Name = "admin-bottom-responsive-grid", Dock = DockStyle.Fill,
                ColumnCount = 3, RowCount = 1, BackColor = Background,
                Padding = new Padding(0), Margin = new Padding(0),
                AutoScroll = false
            };
            adminBottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            adminBottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            adminBottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334f));
            adminBottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            foreach (Panel card in new[] { policiesCard, virusTotalCard, signaturesCard })
            {
                card.Dock = DockStyle.Fill;
                card.Margin = new Padding(4, 0, 4, 0);
                card.Resize += (sender, e) => LayoutAdminCards();
                // Bố trí nội dung bên trong qua LayoutAdminCards; không neo rộng 2 lần.
                foreach (var label in card.Controls.OfType<Label>())
                    label.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }
            adminBottomGrid.Controls.Add(policiesCard, 0, 0);
            adminBottomGrid.Controls.Add(virusTotalCard, 1, 0);
            adminBottomGrid.Controls.Add(signaturesCard, 2, 0);
            adminBottomGrid.Resize += (sender, e) => LayoutAdminCards();
            adminHomeGrid.Controls.Add(adminBottomGrid, 0, 2);
            homeHost.Controls.Add(adminHomeGrid);
            adminHomeGrid.BringToFront();
            adminToolbar.Resize += (sender, e) => LayoutAdminToolbar();
            dashboardCard.Resize += (sender, e) => LayoutDashboardContent();
            LayoutHome();
        }

        // Không dùng Panel.Paint thường để vẽ viền: sau một đợt thay đổi
        // kích thước, nét viền cũ có thể không bị xoá sạch (đường sọc dọc).
        private sealed class AdminBorderPanel : Panel
        {
            public AdminBorderPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                UpdateStyles();
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                Invalidate(true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Rectangle border = ClientRectangle;
                if (border.Width < 2 || border.Height < 2) return;
                using (var pen = new Pen(Color.FromArgb(219, 231, 246)))
                    e.Graphics.DrawRectangle(pen, border.Left, border.Top,
                        border.Width - 1, border.Height - 1);
            }
        }

        private Panel HomeCard(string title, string info)
        {
            var card = new AdminBorderPanel {
                BackColor = Color.White,
                Margin = new Padding(0),
            };
            var icon = new Label {
                Name = "home-card-icon",
                Text = "●", ForeColor = Primary,
                Font = new Font(Theme.UiFontFamily, 13.4f), TextAlign = ContentAlignment.MiddleCenter
            };
            icon.SetBounds(20, 13, 24, 34);
            var cardTitle = AddText(card, title, 50, 12, 1000, 36, 18.0f, true, Ink);
            cardTitle.Name = "home-card-title";
            cardTitle.UseMnemonic = false; // Hiển thị dấu & trong "Dữ liệu chữ ký & cập nhật".
            cardTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            var cardSubtitle = AddText(card, info, 22, 50, 1030, 28, 10.9f, false, Muted);
            cardSubtitle.Name = "home-card-subtitle";
            cardSubtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(icon);
            return card;
        }

        private static Label AddText(System.Windows.Forms.Control parent, string text,
            int x, int y, int width, int height, float size, bool bold, Color color)
        {
            var label = new Label { Text = text, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = color, Font = new Font(Theme.UiFontFamily, Theme.ReadableFontSize(size), bold ? FontStyle.Bold : FontStyle.Regular) };
            label.SetBounds(x, y, width, height);
            parent.Controls.Add(label);
            return label;
        }

        private Label HomeStatus()
        {
            return new Label { TextAlign = ContentAlignment.MiddleLeft, AutoSize = false,
                ForeColor = Muted, Font = new Font(Theme.UiFontFamily, 11.25f) };
        }

        private Button HomeToggleButton(Action click)
        {
            var button = MakeButton("Đang đọc...", click);
            button.Width = 101;
            button.Height = 38;
            button.Font = new Font(Theme.UiFontFamily, 11.85f, FontStyle.Bold);
            return button;
        }

        private void AddSettingLine(Panel card, string name, string details, int y,
            Label state, Button button)
        {
            var header = AddText(card, name, 23, y, 700, 30, 13.1f, true, Ink);
            var note = AddText(card, details, 23, y + 31, 700, 42, 10.3f, false, Muted);
            header.Name = "policy-title";
            note.Name = "policy-note";
            state.Name = "policy-state";
            card.Controls.Add(state);
            card.Controls.Add(button);
            button.Top = y + 13;
            button.Name = "policy-toggle";
            state.Top = y + 46;
        }

        private Button AddHomeButton(Panel parent, string label, Action click, int x, int y, int width)
        {
            var button = MakeButton(label, click);
            button.Font = new Font(Theme.UiFontFamily, 11.25f, FontStyle.Regular);
            button.SetBounds(x, y, width, 44);
            button.FlatAppearance.BorderColor = Color.FromArgb(205, 223, 244);
            parent.Controls.Add(button);
            return button;
        }

        // Một hàng công cụ chung: ngày | thời điểm cập nhật | làm mới / xem lỗi | đăng xuất.
        // Mọi control đều có cùng Parent để tránh lệch hàng khi Resize / thay đổi DPI.
        private void LayoutAdminToolbar()
        {
            if (adminToolbar == null || dashboardPeriod == null || adminLogoutButton == null) return;
            int width = adminToolbar.ClientSize.Width;
            int height = adminToolbar.ClientSize.Height;
            if (width <= 0 || height <= 0) return;

            bool compact = width < 1030;
            int margin = compact ? 9 : 15;
            int gap = compact ? 6 : 10;
            int controlHeight = Math.Min(44, Math.Max(34, height - 10));
            int top = (height - controlHeight) / 2;
            bool narrow = width < 620;
            int logoutWidth = narrow ? 116 : (compact ? 147 : 184);
            int refreshWidth = narrow ? 32 : (compact ? 36 : 42);
            int errorWidth = dashboardDetails.Visible ? (narrow ? 68 : (compact ? 75 : 89)) : 0;
            int periodWidth = narrow ? 156 : (compact ? 196 : 248);
            int gapCount = dashboardDetails.Visible ? 4 : 3;
            int available = Math.Max(0, width - 2 * margin - gapCount * gap -
                logoutWidth - refreshWidth - errorWidth - periodWidth);
            if (available < 160)
            {
                int reduce = Math.Min(periodWidth - 136, 160 - available);
                periodWidth -= reduce;
                available += reduce;
            }
            int updatedWidth = Math.Min(compact ? 250 : 320, Math.Max(70, available));
            int groupWidth = periodWidth + updatedWidth + refreshWidth + logoutWidth +
                errorWidth + gapCount * gap;
            // Bên trái là tiêu đề thống kê, bên phải là nhóm điều khiển.
            int x = Math.Max(margin + 210, width - margin - groupWidth);
            if (adminToolbarTitle != null)
            {
                int titleWidth = Math.Max(170, x - margin - 10);
                adminToolbarTitle.SetBounds(margin + 10, top - 1, titleWidth, controlHeight + 2);
                adminToolbarTitle.Font = new Font(Theme.UiFontFamily, compact ? 16.6f : 17.45f, FontStyle.Bold);
            }
            dashboardPeriod.SetBounds(x, top, periodWidth, controlHeight);
            x += periodWidth + gap;
            dashboardUpdatedLabel.SetBounds(x, top, updatedWidth, controlHeight);
            dashboardUpdatedLabel.TextAlign = ContentAlignment.MiddleRight;
            dashboardUpdatedLabel.AutoEllipsis = compact;
            x += updatedWidth + gap;
            if (dashboardDetails.Visible)
            {
                dashboardDetails.SetBounds(x, top, errorWidth, controlHeight);
                x += errorWidth + gap;
            }
            else dashboardDetails.SetBounds(0, 0, 0, 0);
            dashboardRefresh.SetBounds(x, top, refreshWidth, controlHeight);
            x += refreshWidth + gap;
            adminLogoutButton.SetBounds(x, top, logoutWidth, controlHeight);
            adminLogoutButton.Text = narrow ? "Đăng xuất" : (compact ? "Đăng xuất" : "Đăng xuất quản trị");

            // Ở kích thước hẹp, hiển thị thời điểm rút gọn và giữ giá trị đầy đủ trong tooltip.
            string full = dashboardUpdatedLabel.Tag as string ?? dashboardUpdatedLabel.Text;
            if (compact && dashboardLastSuccess.HasValue && full.StartsWith("Cập nhật lần cuối:", StringComparison.Ordinal))
                dashboardUpdatedLabel.Text = "Cập nhật: " + dashboardLastSuccess.Value.ToString("dd/MM HH:mm");
            else if (dashboardUpdatedLabel.Tag is string)
                dashboardUpdatedLabel.Text = full;
            adminToolbar.Invalidate();
        }

        // Bố cục trang quản trị luôn vừa vùng làm việc: không kéo chiều cao tối thiểu,
        // không bật scrollbar. Ở cửa sổ thấp, ưu tiên thẻ thao tác và thu biểu đồ theo tỉ lệ.
        private void LayoutHome()
        {
            if (homeLayoutBusy || homeHost.IsDisposed || adminHomeGrid == null ||
                dashboardCard == null || policiesCard == null) return;
            int width = homeHost.ClientSize.Width;
            int height = homeHost.ClientSize.Height;
            if (width <= 0 || height <= 0) return;
            homeLayoutBusy = true;
            try
            {
                adminHomeGrid.SuspendLayout();
                adminHomeGrid.MinimumSize = Size.Empty;
                adminHomeGrid.Dock = DockStyle.Fill;
                adminHomeGrid.Padding = new Padding(width < 750 ? 3 : 4);
                // Không xếp 3 card thành 3 hàng khi chiều cao cửa sổ không đủ.
                // Chế độ dọc chỉ dùng khi cửa sổ thực sự cao và hẹp.
                bool stacked = width < 760 && height >= 1040;
                int toolbarHeight = height < 580 ? 44 : 58;
                int bottomHeight = stacked
                    ? Math.Min(675, (int)(height * .61f))
                    : Math.Min(245, Math.Max(195, (int)(height * .32f)));
                // Dashboard dùng hàng % còn lại, không được khiến trang vượt viewport.
                adminHomeGrid.RowStyles[0].Height = toolbarHeight;
                adminHomeGrid.RowStyles[1].SizeType = SizeType.Percent;
                adminHomeGrid.RowStyles[1].Height = 100f;
                adminHomeGrid.RowStyles[2].SizeType = SizeType.Absolute;
                adminHomeGrid.RowStyles[2].Height = bottomHeight;
                if (adminBottomStacked != stacked)
                {
                    adminBottomStacked = stacked;
                    adminBottomGrid.SuspendLayout();
                    adminBottomGrid.Controls.Clear();
                    adminBottomGrid.ColumnStyles.Clear();
                    adminBottomGrid.RowStyles.Clear();
                    if (stacked)
                    {
                        adminBottomGrid.ColumnCount = 1;
                        adminBottomGrid.RowCount = 3;
                        adminBottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                        for (int i = 0; i < 3; i++)
                            adminBottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3f));
                        adminBottomGrid.Controls.Add(policiesCard, 0, 0);
                        adminBottomGrid.Controls.Add(virusTotalCard, 0, 1);
                        adminBottomGrid.Controls.Add(signaturesCard, 0, 2);
                    }
                    else
                    {
                        adminBottomGrid.ColumnCount = 3;
                        adminBottomGrid.RowCount = 1;
                        for (int i = 0; i < 3; i++)
                            adminBottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,
                                i == 2 ? 33.334f : 33.333f));
                        adminBottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                        adminBottomGrid.Controls.Add(policiesCard, 0, 0);
                        adminBottomGrid.Controls.Add(virusTotalCard, 1, 0);
                        adminBottomGrid.Controls.Add(signaturesCard, 2, 0);
                    }
                    adminBottomGrid.ResumeLayout(true);
                }
                adminHomeGrid.ResumeLayout(true);
                LayoutAdminToolbar();
                LayoutDashboardContent();
                LayoutAdminCards();
            }
            finally { homeLayoutBusy = false; }
        }

        private void LayoutDashboardContent()
        {
            if (dashboardCard == null || dashboardCard.IsDisposed || dashboardCard.Width < 1) return;
            // Dashboard nằm bên trong một ô TableLayoutPanel, không tự đổi vị trí của card.
            LayoutDashboard(0, 0, dashboardCard.ClientSize.Width, dashboardCard.ClientSize.Height);
        }

        private void LayoutAdminCards()
        {
            if (policiesCard == null || signaturesCard == null || virusTotalCard == null ||
                policiesCard.Width < 1 || virusTotalCard.Width < 1 || signaturesCard.Width < 1) return;

            // Các thẻ dưới có font tiêu đề riêng để không cắt chữ khi thu hẹp cửa sổ.
            foreach (Panel card in new[] { policiesCard, virusTotalCard, signaturesCard })
            {
                var title = card.Controls.Find("home-card-title", false).OfType<Label>().FirstOrDefault();
                var subtitle = card.Controls.Find("home-card-subtitle", false).OfType<Label>().FirstOrDefault();
                if (title != null)
                {
                    title.Width = Math.Max(80, card.ClientSize.Width - 65);
                    title.Font = new Font(Theme.UiFontFamily, card.Width < 340 ? 13.5f : 16.1f, FontStyle.Bold);
                    title.AutoEllipsis = true;
                }
                if (subtitle != null)
                {
                    subtitle.SetBounds(22, 51, Math.Max(80, card.ClientSize.Width - 44), 29);
                    subtitle.Font = new Font(Theme.UiFontFamily, card.Width < 340 ? 9.65f : 10.75f);
                    subtitle.AutoEllipsis = true;
                    subtitle.Visible = card.ClientSize.Height >= 205;
                }
            }

            // Hai công tắc: vùng chữ và vùng nút không giao nhau theo trục X.
            int policyWidth = policiesCard.ClientSize.Width;
            int toggleWidth = policyWidth < 350 ? 78 : 100;
            int toggleLeft = Math.Max(24, policyWidth - toggleWidth - 17);
            int policyTextWidth = Math.Max(25, toggleLeft - 35);
            int policyHeight = policiesCard.ClientSize.Height;
            bool compactPolicyHeight = policyHeight < 235;
            int secondY = compactPolicyHeight ? 141 : 151;
            var policyTitles = policiesCard.Controls.OfType<Label>()
                .Where(l => l.Name == "policy-title").OrderBy(l => l.Top).ToList();
            var policyNotes = policiesCard.Controls.OfType<Label>()
                .Where(l => l.Name == "policy-note").OrderBy(l => l.Top).ToList();
            var policyStates = policiesCard.Controls.OfType<Label>()
                .Where(l => l.Name == "policy-state").OrderBy(l => l.Top).ToList();
            var policyToggles = policiesCard.Controls.OfType<Button>()
                .Where(b => b.Name == "policy-toggle").OrderBy(b => b.Top).ToList();
            for (int i = 0; i < Math.Min(2, policyTitles.Count); i++)
            {
                int rowY = i == 0 ? (compactPolicyHeight ? 79 : 82) : secondY;
                policyTitles[i].SetBounds(22, rowY, policyTextWidth, 27);
                policyTitles[i].Font = new Font(Theme.UiFontFamily, policyWidth < 350 ? 10.25f : 12.55f, FontStyle.Bold);
                policyTitles[i].AutoEllipsis = false;
                if (i < policyNotes.Count)
                {
                    policyNotes[i].SetBounds(22, rowY + 27, policyTextWidth,
                        compactPolicyHeight ? 24 : Math.Min(37, Math.Max(0, policyHeight - rowY - 32)));
                    policyNotes[i].Font = new Font(Theme.UiFontFamily, policyWidth < 350 ? 9.55f : 10.45f);
                    policyNotes[i].AutoEllipsis = false;
                }
                if (i < policyToggles.Count)
                    policyToggles[i].SetBounds(toggleLeft, rowY + 4, toggleWidth, compactPolicyHeight ? 31 : 34);
                if (i < policyStates.Count)
                {
                    policyStates[i].SetBounds(toggleLeft, rowY + 39, toggleWidth, 22);
                    policyStates[i].Font = new Font(Theme.UiFontFamily, policyWidth < 350 ? 9.65f : 10.45f);
                    policyStates[i].Visible = !compactPolicyHeight;
                    policyStates[i].TextAlign = ContentAlignment.MiddleCenter;
                }
            }

            // VirusTotal: dòng khóa API tự đổi bố cục theo chiều rộng, không để
            // trạng thái dài chèn lên chữ 'Khóa API' hoặc nút bấm.
            int vtWidth = virusTotalCard.ClientSize.Width;
            int vtHeight = virusTotalCard.ClientSize.Height;
            bool compactVt = vtWidth < 380;
            bool shortVt = vtHeight < 232;
            var apiLabel = virusTotalCard.Controls.Find("vt-api-title", false)
                .OfType<Label>().FirstOrDefault();
            if (apiLabel != null)
            {
                apiLabel.SetBounds(22, shortVt ? 79 : 82,
                    compactVt ? 76 : 100, 25);
                apiLabel.Font = new Font(Theme.UiFontFamily, vtWidth < 350 ? 10.85f : 12.35f, FontStyle.Bold);
            }
            if (vtStatus != null)
            {
                if (compactVt)
                    vtStatus.SetBounds(22, shortVt ? 101 : 104,
                        Math.Max(45, vtWidth - 44), 22);
                else
                    vtStatus.SetBounds(128, shortVt ? 79 : 82,
                        Math.Max(45, vtWidth - 150), 25);
                vtStatus.Font = new Font(Theme.UiFontFamily, vtWidth < 350 ? 9.55f : 10.45f);
                vtStatus.AutoEllipsis = false;
                dashboardToolTip.SetToolTip(vtStatus, vtStatus.Text);
            }
            int vtControlY = compactVt ? (shortVt ? 126 : 129) : (shortVt ? 113 : 114);
            int vtButtonHeight = shortVt ? 30 : 33;
            int vtGap = 14;
            int vtButtonWidth = Math.Max(24, (vtWidth - 44 - vtGap) / 2);
            var vtButtons = virusTotalCard.Controls.OfType<Button>()
                .Where(b => b.Name != "policy-toggle").ToList();
            if (vtButtons.Count >= 2)
            {
                vtButtons[0].SetBounds(22, vtControlY, vtButtonWidth, vtButtonHeight);
                vtButtons[1].SetBounds(22 + vtButtonWidth + vtGap, vtControlY, vtButtonWidth, vtButtonHeight);
                foreach (Button b in vtButtons)
                {
                    b.Font = new Font(Theme.UiFontFamily, vtWidth < 350 ? 9.65f : 10.75f);
                    b.AutoEllipsis = false;
                    dashboardToolTip.SetToolTip(b, b.Text);
                }
            }
            int vtRow = vtControlY + vtButtonHeight + 9;
            var vtHeader = virusTotalCard.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Name == "policy-title");
            var vtNote = virusTotalCard.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Name == "policy-note");
            int vtSwitchWidth = vtWidth < 350 ? 67 : 84;
            int vtSwitchLeft = Math.Max(24, vtWidth - vtSwitchWidth - 17);
            int vtTextWidth = Math.Max(25, vtSwitchLeft - 32);
            if (vtHeader != null)
            {
                vtHeader.SetBounds(22, vtRow, vtTextWidth, 24);
                vtHeader.Font = new Font(Theme.UiFontFamily, vtWidth < 350 ? 9.95f : 11.65f, FontStyle.Bold);
                vtHeader.AutoEllipsis = false;
                dashboardToolTip.SetToolTip(vtHeader, vtHeader.Text);
            }
            if (vtNote != null)
            {
                vtNote.SetBounds(22, vtRow + 24, vtTextWidth,
                    Math.Max(0, Math.Min(32, vtHeight - vtRow - 27)));
                vtNote.Font = new Font(Theme.UiFontFamily, vtWidth < 350 ? 9.45f : 10.35f);
                vtNote.AutoEllipsis = false;
                vtNote.Visible = !shortVt && vtHeight >= vtRow + 50;
            }
            if (vtAutoButton != null)
                vtAutoButton.SetBounds(vtSwitchLeft, vtRow + 3, vtSwitchWidth, shortVt ? 30 : 33);

            // 6 công cụ chữ ký: bảng 2 cột 3 dòng, chia đều chiều cao thực tế.
            int sigWidth = signaturesCard.ClientSize.Width;
            int sigHeight = signaturesCard.ClientSize.Height;
            signatureStatus.SetBounds(22, sigHeight < 225 ? 78 : 81,
                Math.Max(35, sigWidth - 44), 25);
            signatureStatus.Font = new Font(Theme.UiFontFamily, sigWidth < 350 ? 9.85f : 10.55f);
            signatureStatus.AutoEllipsis = false;
            var sigButtons = signaturesCard.Controls.OfType<Button>().ToList();
            int sx = 22, gap = 7;
            int cellWidth = Math.Max(70, (sigWidth - sx * 2 - gap) / 2);
            int top = sigHeight < 225 ? 108 : 111;
            int rowHeight = Math.Min(35, Math.Max(25, (sigHeight - top - 9 - 2 * gap) / 3));
            for (int i = 0; i < sigButtons.Count; i++)
            {
                sigButtons[i].SetBounds(sx + (i % 2) * (cellWidth + gap),
                    top + (i / 2) * (rowHeight + gap), cellWidth, rowHeight);
                sigButtons[i].Font = new Font(Theme.UiFontFamily, sigWidth < 350 ? 9.65f : 10.75f,
                    sigButtons[i].Name == "admin-home-import-sample" ? FontStyle.Bold : FontStyle.Regular);
                sigButtons[i].AutoEllipsis = false;
                dashboardToolTip.SetToolTip(sigButtons[i], sigButtons[i].Text);
            }
            // Buộc sơn lại toàn bộ các thẻ sau thay đổi bố cục/DPI,
            // xoá mọi pixel viền cũ còn sót trên nền trắng.
            policiesCard.Invalidate(true);
            virusTotalCard.Invalidate(true);
            signaturesCard.Invalidate(true);
        }

        private static void SetToggleAppearance(Button button, bool on)
        {
            if (button == null) return;
            button.Text = on ? "BẬT   ✓" : "TẮT   ○";
            button.BackColor = on ? Color.FromArgb(224, 247, 234) : Color.FromArgb(242, 246, 251);
            button.ForeColor = on ? Color.FromArgb(15, 127, 68) : Color.FromArgb(110, 123, 141);
        }

        private void RefreshHomeState()
        {
            if (homeHost.IsDisposed || !AdminSecurity.IsAuthenticated) return;
            SetToggleAppearance(updateButton, FeatureFlags.AutoUpdateEnabled);
            updateStatus.Text = FeatureFlags.AutoUpdateEnabled ? "Đang bật" : "Đang tắt";
            try
            {
                bool send = AppSettings.Load().SendSamples;
                samplesButton.Enabled = true;
                SetToggleAppearance(samplesButton, send);
                samplesStatus.Text = send ? "Cho phép gửi" : "Không cho phép";
            }
            catch (Exception ex)
            {
                SetToggleAppearance(samplesButton, false);
                samplesButton.Enabled = false;
                samplesStatus.Text = "Không đọc được cấu hình";
                samplesStatus.Tag = ex.Message;
            }
            vtStatus.Text = VirusTotalClient.IsConfigured ? "API đã cấu hình (khóa được mã hóa)" : "Chưa cấu hình API key";
            SetToggleAppearance(vtAutoButton, FeatureFlags.VtAutoQuery);
            RefreshDashboardAsync();
            // Kiểm tra số chữ ký trên worker, tránh khóa UI khi SQL chậm hoặc bị mất kết nối.
            int token = ++homeDataSequence;
            signatureStatus.Text = "Đang lấy số lượng chữ ký từ SQL Server...";
            Task.Run(() =>
            {
                try
                {
                    var counts = SqlTable("SELECT COUNT_BIG(*) AS Total, SUM(CASE WHEN IsActive=1 THEN CAST(1 AS BIGINT) ELSE CAST(0 AS BIGINT) END) AS Active FROM dbo.VirusSignatures");
                    if (counts.Rows.Count == 0) return "Chưa có dữ liệu chữ ký.";
                    long total = Convert.ToInt64(counts.Rows[0][0]);
                    long active = counts.Rows[0][1] == DBNull.Value ? 0 : Convert.ToInt64(counts.Rows[0][1]);
                    return string.Format("Tổng {0:N0} chữ ký  •  Đang kích hoạt {1:N0}", total, active);
                }
                catch { return "Không kết nối được SQL Server; có thể kiểm tra tại Chẩn đoán hệ thống."; }
            }).ContinueWith(t =>
            {
                if (homeHost.IsDisposed || !homeHost.IsHandleCreated || token != homeDataSequence) return;
                try { homeHost.BeginInvoke((Action)(() => {
                    if (!IsDisposed && token == homeDataSequence && AdminSecurity.IsAuthenticated)
                        signatureStatus.Text = t.IsFaulted ? "Không lấy được dữ liệu chữ ký." : t.Result;
                })); } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
            });
        }

        private void ShowHome()
        {
            if (!RequireAdmin()) return;
            ++sequence;
            renderingHome = true;
            detailHost.Visible = false;
            homeHost.Visible = true;
            homeHost.BringToFront();
            LayoutHome();
            RefreshHomeState();
        }

        private void ResizeNav()
        {
            foreach (System.Windows.Forms.Control item in navigation.Controls)
                item.Width = Math.Max(165, navigation.ClientSize.Width - 30);
        }
        private Button MakeButton(string name, Action action)
        {
            // Các nút tại trang Quản trị đều là hình chữ nhật, góc vuông (không bo góc).
            var button = new Button { Text = name, AutoSize = false,
                Width = 145, Height = 40, FlatStyle = FlatStyle.Flat,
                BackColor = Color.White, ForeColor = Ink, Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Font = new Font(Theme.UiFontFamily, 9.75f, FontStyle.Regular),
                Margin = new Padding(1, 3, 6, 3) };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(213, 227, 245);
            button.Click += (s,e) => { if (RequireAdmin()) action(); };
            return button;
        }
        private bool RequireAdmin()
        {
            if (AdminSecurity.IsAuthenticated) return true;
            if (NavigateRequested != null) NavigateRequested("overview");
            return false;
        }
        public void Activated() { if (RequireAdmin()) ShowHome(); }
        public void ShowModule(int module) { if (RequireAdmin() && module >= 0 && module < Titles.Length) Choose(module); }
        private void Choose(int module)
        {
            if (!RequireAdmin()) return;
            renderingHome = false;
            homeHost.Visible = false;
            detailHost.Visible = true;
            detailHost.BringToFront();
            selected = module; ++sequence;
            heading.Text = Titles[module]; description.Text = Descriptions[module];
            search.Visible = module == 1 || module == 2 || module == 6;
            foreach (Button nav in navigation.Controls.OfType<Button>())
            {
                bool current = nav.Tag is int && (int)nav.Tag == module;
                nav.BackColor = current ? Color.FromArgb(228, 239, 255) : Color.White;
                nav.ForeColor = current ? Primary : Ink;
                nav.Font = new Font(Theme.UiFontFamily, 9.75f, current ? FontStyle.Bold : FontStyle.Regular);
            }
            BuildActions(module);
            RefreshData();
        }
        private void Action(string label, Action invoke) { actions.Controls.Add(MakeButton(label, invoke)); }
        private void Dialog(Form dialog, string operation)
        {
            using (dialog) dialog.ShowDialog(FindForm());
            AdminAudit.Write(operation, "Đã mở công cụ quản trị: " + dialog.Text);
            RefreshData();
        }
        private void Safe(Action callback)
        {
            if (!RequireAdmin()) return;
            try { callback(); }
            catch (Exception ex) { MessageBox.Show(FindForm(), ex.Message, "Thao tác quản trị", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void BuildActions(int module)
        {
            actions.Controls.Clear();
            switch (module)
            {
                case 0: Action("Chẩn đoán", () => Choose(13));
                    Action("Đổi mật khẩu Admin", () => Safe(ChangeAdminPassword));
                    Action("Kiểm tra SQL", () => Safe(() => { using (var cn = DbConnection.Create()) MessageBox.Show(cn.Database + " • " + cn.DataSource, "Cấu hình SQL"); })); break;
                case 1: Action("Thêm chữ ký", () => Safe(AddSignature));
                    Action("Phân loại chữ ký", () => Safe(ManageUnclassifiedSignatures));
                    Action("Sửa tên/mức độ", () => Safe(EditSignature));
                    Action("Kích hoạt", () => Safe(() => SetSignatureState(true)));
                    Action("Vô hiệu hóa", () => Safe(() => SetSignatureState(false)));
                    Action("Công cụ chữ ký", () => Safe(() => Dialog(new SignatureManagementForm(), "signature-management"))); break;
                case 2: Action("Phân loại chữ ký", () => Safe(ManageUnclassifiedSignatures));
                    Action("Duyệt chữ ký", () => Safe(() => SetSignatureState(true)));
                    Action("Chờ xác minh", () => Safe(() => SetSignatureState(false)));
                    Action("Công cụ kiểm duyệt", () => Safe(() => Dialog(new SignatureManagementForm(), "signature-review"))); break;
                case 3: Action("Nhập mẫu tệp", () => Safe(() => { SampleSubmissionForm.Open(FindForm()); AdminAudit.Write("sample-import", "Mở công cụ nhập dữ liệu mẫu"); }));
                    Action("Quản lý VirusTotal", () => Choose(14));
                    Action("Gửi mẫu: Bật/Tắt", () => Safe(ToggleSamples)); break;
                case 4: Action("Bảo vệ thời gian thực", () => Safe(() => ToggleFeature("realtime")));
                    Action("Bảo vệ USB", () => Safe(() => ToggleFeature("usb")));
                    Action("Bảo vệ tải xuống", () => Safe(() => ToggleFeature("download"))); break;
                case 5: Action("Kiểm duyệt chữ ký", () => Choose(2)); Action("Ngoại lệ heuristic", () => Safe(() => Dialog(new ScanExclusionForm(), "heuristic-exclusions"))); break;
                case 6: Action("Kiểm tra trùng / sai", () => RefreshData()); Action("Quản lý chữ ký", () => Choose(1)); break;
                case 7: Action("Kiểm tra Windows", () => Safe(() => Dialog(new SecurityHealthForm(), "windows-security")));
                    Action("Khôi phục watcher", () => Safe(() => { FeatureFlags.NotifyChanged(); AdminAudit.Write("watcher-refresh", "Yêu cầu áp dụng lại trạng thái guard"); RefreshData(); })); break;
                case 8: Action("Cập nhật hiệu năng", RefreshData); break;
                case 9: Action("Chạy bộ kiểm thử an toàn", () => Safe(RunSafeTests)); Action("Kiểm tra PE", () => Safe(() => Dialog(new PeInspectForm(), "pe-inspection"))); break;
                case 10: Action("Bật/Tắt tự cập nhật", () => Safe(ToggleAutoUpdate));
                    Action("Cập nhật thủ công", () => Safe(() => { SignatureUpdateProgressForm.Open(FindForm()); AdminAudit.Write("signature-update", "Mở trình cập nhật thủ công"); RefreshData(); })); break;
                case 11: Action("Làm mới nhật ký", RefreshData); break;
                case 12: Action("Sao lưu .bak", () => Safe(BackupDatabase));
                    Action("Xác minh .bak", () => Safe(() => VerifyBackup(false)));
                    Action("Phục hồi .bak", () => Safe(() => VerifyBackup(true))); break;
                case 13: Action("Chạy chẩn đoán", RefreshData);
                    Action("Kiểm tra Windows", () => Safe(() => Dialog(new SecurityHealthForm(), "security-check"))); break;
                case 14: Action("Thiết lập API key", () => Safe(() => Dialog(new VirusTotalConfigForm(), "vt-config")));
                    Action("Tự tra hash: Bật/Tắt", () => Safe(ToggleAutoVT));
                    Action("Gửi mẫu: Bật/Tắt", () => Safe(ToggleSamples)); break;
            }
        }
        private void ChangeAdminPassword()
        {
            string first = AppDialog.Prompt(FindForm(), "Đổi mật khẩu quản trị", "Nhập mật khẩu mới (tối thiểu 12 ký tự):", "", true);
            if (first == null) return;
            string second = AppDialog.Prompt(FindForm(), "Xác nhận mật khẩu", "Nhập lại mật khẩu mới:", "", true);
            if (second == null) return;
            if (first != second) throw new InvalidOperationException("Hai mật khẩu mới không khớp.");
            string error;
            if (!AdminSecurity.ChangePassword(first, out error)) throw new InvalidOperationException(error);
            MessageBox.Show(FindForm(), "Đã đổi mật khẩu quản trị cho tài khoản Windows hiện tại. Hãy dùng mật khẩu mới từ lần vào tiếp theo.", "Thành công");
        }
        private void AddSignature()
        {
            string hash = Microsoft.VisualBasic.Interaction.InputBox("Nhập SHA-256 của mẫu đã được kiểm chứng (64 ký tự hex):", "Thêm chữ ký mã độc", "").Trim().ToLowerInvariant();
            if (hash.Length == 0) return;
            if (hash.Length != 64 || hash.Any(c => !Uri.IsHexDigit(c))) throw new InvalidOperationException("SHA-256 phải có đúng 64 ký tự hex.");
            string name = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên mẫu / họ mã độc (cần nguồn xác minh):", "Tên mã độc", "").Trim();
            if (name.Length == 0 || name.Length > 200) throw new InvalidOperationException("Tên mẫu phải dài từ 1 đến 200 ký tự.");
            string reason = Microsoft.VisualBasic.Interaction.InputBox("Ghi nguồn và bằng chứng xác minh (không được để trống):", "Nguồn chữ ký", "").Trim();
            if (reason.Length == 0) throw new InvalidOperationException("Chưa có nguồn kiểm chứng nên không thêm chữ ký.");
            if (MessageBox.Show(FindForm(), "Thêm chữ ký ở trạng thái TẮT để kiểm duyệt trước khi sử dụng?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            long id;
            using (var cn = DbConnection.Create())
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        using (var check = new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.VirusSignatures WITH (UPDLOCK,HOLDLOCK) WHERE SHA256=@hash", cn, tx))
                        {
                            check.Parameters.Add("@hash", SqlDbType.Char, 64).Value = hash;
                            if (Convert.ToInt64(check.ExecuteScalar()) != 0) throw new InvalidOperationException("Hash đã tồn tại trong cơ sở dữ liệu.");
                        }
                        using (var cmd = new SqlCommand(@"INSERT INTO dbo.VirusSignatures
                          (MalwareName,Category,SignatureType,SHA256,Severity,Description,RecommendedAction,IsActive)
                          OUTPUT INSERTED.SignatureID
                          VALUES(@name,NULL,'Hash',@hash,'Medium',@reason,'Review',0)", cn, tx))
                        {
                            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;
                            cmd.Parameters.Add("@hash", SqlDbType.Char, 64).Value = hash;
                            cmd.Parameters.Add("@reason", SqlDbType.NVarChar, 1000).Value = reason.Length > 1000 ? reason.Substring(0,1000) : reason;
                            id = Convert.ToInt64(cmd.ExecuteScalar());
                        }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
            AdminAudit.Write("signature-add", "ID=" + id + "; SHA256=" + hash + "; Nguồn=" + reason);
            MessageBox.Show(FindForm(), "Đã thêm chữ ký #" + id + " ở trạng thái chờ kiểm duyệt (IsActive=0).", "Thêm chữ ký");
            RefreshData();
        }
        private void EditSignature()
        {
            long id = SelectedSignature();
            string name = Microsoft.VisualBasic.Interaction.InputBox("Tên mã độc mới cho chữ ký #" + id + ":", "Chỉnh sửa metadata", "").Trim();
            if (name.Length == 0 || name.Length > 200) return;
            string severity = Microsoft.VisualBasic.Interaction.InputBox("Mức nguy hiểm: Low / Medium / High / Critical", "Mức độ", "Medium").Trim();
            if (!new[] { "Low", "Medium", "High", "Critical" }.Any(v => string.Equals(v,severity,StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Mức nguy hiểm chỉ được là Low, Medium, High hoặc Critical.");
            string reason = Microsoft.VisualBasic.Interaction.InputBox("Lý do điều chỉnh metadata:", "Nhật ký thay đổi", "").Trim();
            if (reason.Length == 0) return;
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand("UPDATE dbo.VirusSignatures SET MalwareName=@name,Severity=@sev,UpdatedAt=SYSDATETIME() WHERE SignatureID=@id",cn))
            {
                cmd.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;
                cmd.Parameters.Add("@sev", SqlDbType.VarChar, 20).Value = severity;
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
                cn.Open(); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Không tìm thấy chữ ký.");
            }
            HybridSignatureScanner.Invalidate();
            AdminAudit.Write("signature-edit", "ID=" + id + "; MalwareName=" + name + "; Severity=" + severity + "; Lý do=" + reason);
            RefreshData();
        }
        private long SelectedSignature()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Cells.Count == 0) throw new InvalidOperationException("Chọn một chữ ký ở bảng bên dưới.");
            return Convert.ToInt64(grid.CurrentRow.Cells[0].Value);
        }
        private void SetSignatureState(bool enabled)
        {
            long id = SelectedSignature();
            string action = enabled ? "kích hoạt" : "tạm ngừng";
            string reason = Microsoft.VisualBasic.Interaction.InputBox("Nhập lý do/bằng chứng để " + action + " chữ ký #" + id,
                "Kiểm duyệt chữ ký", "");
            if (string.IsNullOrWhiteSpace(reason)) return;
            if (MessageBox.Show(FindForm(), "Xác nhận " + action + " chữ ký #" + id + "?", "Quản trị",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand("UPDATE dbo.VirusSignatures SET IsActive=@on, UpdatedAt=SYSDATETIME() WHERE SignatureID=@id", cn))
            {
                cmd.Parameters.Add("@on", SqlDbType.Bit).Value = enabled;
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
                cn.Open(); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Không tìm thấy chữ ký hoặc không được phép sửa.");
            }
            HybridSignatureScanner.Invalidate();
            AdminAudit.Write("signature-state", "SignatureID=" + id + ", IsActive=" + enabled + "; Lý do: " + reason);
            RefreshData();
        }
        private void ToggleAutoUpdate()
        {
            bool next = !FeatureFlags.AutoUpdateEnabled;
            if (MessageBox.Show(FindForm(), (next ? "Bật" : "Tắt") + " tự động cập nhật chữ ký?",
                "Chính sách cập nhật (Admin)", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            FeatureFlags.AutoUpdateEnabled = next;
            FeatureFlags.Persist(); FeatureFlags.NotifyChanged();
            AdminAudit.Write("auto-update", "AutoUpdate=" + next); RefreshData();
        }
        private void ToggleSamples()
        {
            bool next = !AppSettings.Load().SendSamples;
            if (MessageBox.Show(FindForm(), (next ? "Cho phép" : "Ngừng cho phép") +
                " gửi/nhập mẫu nghi ngờ? Không tự tải tệp lên VirusTotal.", "Chính sách gửi mẫu (Admin)",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            new SettingsRepository().UpdateSubmitSamples(next);
            AdminAudit.Write("sample-policy", "SubmitSamples=" + next); RefreshData();
        }
        private void ToggleAutoVT()
        {
            bool next = !FeatureFlags.VtAutoQuery;
            if (next && !VirusTotalClient.IsConfigured) throw new InvalidOperationException("Cần cấu hình VirusTotal API key trước.");
            if (MessageBox.Show(FindForm(), "Đổi chế độ tự tra VirusTotal sang " + (next ? "BẬT" : "TẮT") + "?",
                "Quản lý VirusTotal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            FeatureFlags.VtAutoQuery = next; FeatureFlags.Persist(); FeatureFlags.NotifyChanged();
            AdminAudit.Write("vt-auto-query", "VtAutoQuery=" + next); RefreshData();
        }
        private void ToggleFeature(string feature)
        {
            bool next;
            if (feature == "realtime") { next = !FeatureFlags.RealTimeOn; FeatureFlags.RealTimeOn = next; }
            else if (feature == "usb") { next = !FeatureFlags.UsbProtection; FeatureFlags.UsbProtection = next; }
            else { next = !FeatureFlags.DownloadProtection; FeatureFlags.DownloadProtection = next; }
            if (MessageBox.Show(FindForm(), "Áp dụng trạng thái " + (next ? "BẬT" : "TẮT") + " cho " + feature + "?",
                "Điều khiển dịch vụ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                if (feature == "realtime") FeatureFlags.RealTimeOn = !next;
                else if (feature == "usb") FeatureFlags.UsbProtection = !next;
                else FeatureFlags.DownloadProtection = !next;
                return;
            }
            FeatureFlags.Persist(); FeatureFlags.NotifyChanged();
            AdminAudit.Write("feature-toggle", feature + "=" + next); RefreshData();
        }
        private void RunSafeTests()
        {
            const string sample = "abc";
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.ASCII.GetBytes(sample))).Replace("-", "").ToLowerInvariant();
            bool ok = hash == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
            AdminAudit.Write("self-test", "SHA256 test=" + (ok ? "PASS" : "FAIL"));
            MessageBox.Show(FindForm(), (ok ? "PASS" : "FAIL") + ": kiểm thử SHA-256 chuẩn trên dữ liệu vô hại.\n" +
                "Đây không phải đánh giá tỷ lệ phát hiện mã độc trên tập kiểm thử thực tế.", "Kiểm thử an toàn",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        private void BackupDatabase()
        {
            using (var file = new SaveFileDialog { Filter = "SQL Server backup (*.bak)|*.bak", FileName = "AntivirusDB_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".bak",
                Title = "Chọn đường dẫn trên MÁY CHỦ SQL (dịch vụ SQL phải có quyền ghi)" })
            {
                if (file.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (MessageBox.Show(FindForm(), "SQL Server sẽ ghi tệp trên máy chủ SQL, không nhất thiết là máy chạy WinForms. Tiếp tục?",
                    "Sao lưu AntivirusDB", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;
                using (var cn = DbConnection.Create())
                using (var cmd = new SqlCommand("BACKUP DATABASE [AntivirusDB] TO DISK=@path WITH COPY_ONLY, INIT, CHECKSUM", cn))
                {
                    cmd.CommandTimeout = 180; cmd.Parameters.Add("@path", SqlDbType.NVarChar, 4000).Value = file.FileName;
                    cn.Open(); cmd.ExecuteNonQuery();
                }
                AdminAudit.Write("backup", "Đã yêu cầu SQL backup tại " + file.FileName);
                MessageBox.Show(FindForm(), "SQL Server báo sao lưu thành công. Hãy xác minh bản .bak trước khi dùng phục hồi.", "Sao lưu");
            }
        }
        private void VerifyBackup(bool offerRestore)
        {
            using (var file = new OpenFileDialog { Filter = "SQL backup (*.bak)|*.bak", Title = "Chọn file .bak nằm trên máy chủ SQL" })
            {
                if (file.ShowDialog(FindForm()) != DialogResult.OK) return;
                using (var cn = DbConnection.Create())
                using (var cmd = new SqlCommand("RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM", cn))
                {
                    cmd.CommandTimeout = 180; cmd.Parameters.Add("@path", SqlDbType.NVarChar, 4000).Value = file.FileName;
                    cn.Open(); cmd.ExecuteNonQuery();
                }
                AdminAudit.Write("backup-verify", "Đã xác minh .bak: " + file.FileName);
                if (!offerRestore) { MessageBox.Show(FindForm(), "SQL Server xác minh backup thành công.", "Xác minh .bak"); return; }
                MessageBox.Show(FindForm(), "Backup đã xác minh. Để phục hồi an toàn cần dừng mọi phiên quét, kiểm tra quyền SQL và " +
                    "thực hiện RESTORE từ SQL Server Management Studio. Trang quản trị không tự ghi đè database đang dùng.",
                    "Phục hồi được kiểm soát", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void RefreshData()
        {
            if (IsDisposed || !AdminSecurity.IsAuthenticated) return;
            if (renderingHome) { RefreshHomeState(); return; }
            if (loading) { dirty = true; return; }
            int current = ++sequence;
            int module = selected;
            string term = search.Text.Trim();
            loading = true; refresh.Enabled = false; status.Text = "Đang thu thập dữ liệu quản trị...";
            var ignored = LoadAsync(current, module, term);
        }
        private async Task LoadAsync(int seq, int module, string term)
        {
            try
            {
                DataTable result = await Task.Run(() => GetData(module, term));
                if (IsDisposed || seq != sequence || !AdminSecurity.IsAuthenticated) return;
                grid.DataSource = result;
                status.Text = "Đang hiển thị " + result.Rows.Count.ToString("N0") +
                    " bản ghi • Chỉ những thao tác Admin được xác thực mới có quyền chỉnh sửa.";
            }
            catch (Exception ex)
            {
                if (IsDisposed || seq != sequence) return;
                grid.DataSource = null; status.Text = "Không tải được: " + ex.Message;
            }
            finally
            {
                if (!IsDisposed)
                {
                    loading = false; refresh.Enabled = true;
                    if (dirty) { dirty = false; RefreshData(); }
                }
            }
        }
        private static DataTable NewTable(string first = "Thành phần", string second = "Trạng thái", string third = "Ghi chú")
        {
            var table = new DataTable(); table.Columns.Add(first); table.Columns.Add(second); table.Columns.Add(third);
            return table;
        }
        private static void Row(DataTable table, string first, object second, object third)
        { table.Rows.Add(first, Convert.ToString(second), Convert.ToString(third)); }
        private static DataTable SqlTable(string sql, string filter = "")
        {
            var table = new DataTable();
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(sql, cn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                cmd.CommandTimeout = 12;
                cmd.Parameters.Add("@term", SqlDbType.NVarChar, 200).Value = filter;
                adapter.Fill(table);
            }
            return table;
        }
        private static DataTable GetData(int module, string term)
        {
            if (module == 1 || module == 2)
            {
                string sql = "SELECT TOP (200) SignatureID AS [ID], MalwareName AS [Tên mã độc], " +
                    "MalwareFamily AS [Họ mã độc], SignatureType AS [Kiểu], Severity AS [Mức độ], " +
                    "SHA256, IsActive AS [Kích hoạt], UpdatedAt AS [Sửa lúc] " +
                    "FROM dbo.VirusSignatures WHERE (@term='' OR MalwareName LIKE '%' + @term + '%' OR SHA256 LIKE @term + '%') " +
                    (module == 2 ? " AND IsActive=0 " : "") + "ORDER BY SignatureID DESC";
                return SqlTable(sql, term);
            }
            if (module == 6)
            {
                return SqlTable(@"SELECT TOP (200) SignatureID AS [ID], MalwareName AS [Tên mã độc],
                    SHA256, SignatureType AS [Kiểu], IsActive AS [Kích hoạt],
                    CASE WHEN SHA256 IS NOT NULL AND (LEN(RTRIM(SHA256))<>64 OR SHA256 COLLATE Latin1_General_BIN2 LIKE '%[^0-9a-fA-F]%')
                         THEN N'SHA256 không hợp lệ'
                         WHEN MD5 IS NULL AND SHA1 IS NULL AND SHA256 IS NULL THEN N'Thiếu hash'
                         WHEN NULLIF(LTRIM(RTRIM(MalwareName)),'') IS NULL THEN N'Thiếu tên'
                         ELSE N'Cần kiểm tra thủ công' END AS [Vấn đề]
                    FROM dbo.VirusSignatures
                    WHERE (SHA256 IS NOT NULL AND (LEN(RTRIM(SHA256))<>64 OR SHA256 COLLATE Latin1_General_BIN2 LIKE '%[^0-9a-fA-F]%'))
                    OR (MD5 IS NULL AND SHA1 IS NULL AND SHA256 IS NULL)
                    OR NULLIF(LTRIM(RTRIM(MalwareName)),'') IS NULL
                    OR (SHA256 IS NOT NULL AND SHA256 IN
                      (SELECT SHA256 FROM dbo.VirusSignatures WHERE SHA256 IS NOT NULL GROUP BY SHA256 HAVING COUNT(*)>1))
                    ORDER BY SignatureID DESC", term);
            }
            if (module == 11) return AdminAudit.Read();
            var table = NewTable();
            if (module == 0)
            {
                try
                {
                    DataTable counts = SqlTable("SELECT (SELECT COUNT_BIG(*) FROM dbo.VirusSignatures) AS Total, " +
                        "(SELECT COUNT_BIG(*) FROM dbo.VirusSignatures WHERE IsActive=1) AS Active, " +
                        "(SELECT COUNT_BIG(*) FROM dbo.VirusSignatures WHERE IsActive=0) AS Disabled, " +
                        "(SELECT MAX(CreatedAt) FROM dbo.VirusSignatures) AS LastAdded");
                    if (counts.Rows.Count > 0)
                    {
                        var c = counts.Rows[0];
                        Row(table, "SQL Server", "Đã kết nối", DbConnection.Create().DataSource);
                        Row(table, "Tổng chữ ký mã độc", c[0], "dbo.VirusSignatures");
                        Row(table, "Chữ ký đang kích hoạt", c[1], "Sử dụng khi phát hiện");
                        Row(table, "Chữ ký vô hiệu hóa/chờ duyệt", c[2], "Chưa sử dụng để so khớp");
                        Row(table, "Ngày nhập gần nhất", c[3], "Theo cột CreatedAt");
                    }
                }
                catch (Exception ex) { Row(table, "SQL Server", "Không truy cập", ex.Message); }
                Row(table, "VirusTotal API", VirusTotalClient.IsConfigured ? "Đã cấu hình" : "Chưa có khóa", "DPAPI/Windows account");
                Row(table, "Tự động cập nhật chữ ký", FeatureFlags.AutoUpdateEnabled ? "Đang bật" : "Đang tắt", "Chính sách do Admin quản lý");
                Row(table, "Bảo vệ thời gian thực", FeatureFlags.RealTimeOn ? "Đang bật" : "Đang tắt", "Guard / FileSystemWatcher");
                return table;
            }
            if (module == 3)
            {
                Row(table, "MalwareBazaar", "Full CSV Export", "Nhập metadata/chữ ký qua công cụ cập nhật đã tích hợp");
                Row(table, "VirusTotal", VirusTotalClient.IsConfigured ? "API đã cấu hình" : "Chưa cấu hình", "Tra hash mặc định; upload riêng theo xác nhận");
                Row(table, "Mẫu cục bộ", "Nhập thủ công", "Công cụ nhập mẫu lưu metadata vào database");
                try { Row(table, "Chính sách gửi mẫu", AppSettings.Load().SendSamples ? "Cho phép" : "Không cho phép", "dbo.Settings.SubmitSamples"); }
                catch (Exception ex) { Row(table, "Chính sách gửi mẫu", "Không đọc được", ex.Message); }
            }
            if (module == 4)
            {
                Row(table, "SHA-256 toàn tệp", "Có", "Đối chiếu chữ ký hash");
                Row(table, "Chunk Hash / CDC", "Có", "Theo dữ liệu chunk đã xác minh");
                Row(table, "Sliding Window", "Có", "Đối chiếu vùng dữ liệu đặc trưng");
                Row(table, "Heuristic", "Có", "Kết quả nghi ngờ cần xác minh");
                Row(table, "Realtime", FeatureFlags.RealTimeOn ? "Bật" : "Tắt", "Điều khiển GuardService");
                Row(table, "USB Guard", FeatureFlags.UsbProtection ? "Bật" : "Tắt", "Điều khiển GuardService");
                Row(table, "Download Guard", FeatureFlags.DownloadProtection ? "Bật" : "Tắt", "Điều khiển GuardService");
                Row(table, "YARA", "Chưa tích hợp bắt buộc", "Không trình bày như engine hoạt động khi chưa có runtime/rule");
            }
            if (module == 5)
            {
                Row(table, "SHA-256", "Trùng toàn bộ hash", "Độ tin cậy cao khi chữ ký chính xác");
                Row(table, "Chunk / Sliding", "Dùng chữ ký kiểm chứng", "Không tự cách ly chỉ bởi một đoạn trùng ngẫu nhiên");
                Row(table, "Heuristic", "Cảnh báo nghi vấn", "Có thể báo nhầm, cần phân tích bổ sung");
                Row(table, "VirusTotal", FeatureFlags.VtAutoQuery ? "Tự tra hash: Bật" : "Tự tra hash: Tắt", "API có giới hạn lượt truy vấn");
                Row(table, "Ngưỡng heuristic", "Theo engine hiện tại", "Chưa có API điều chỉnh an toàn, không tạo công tắc giả");
            }
            if (module == 7)
            {
                Row(table, "Realtime Watcher", FeatureFlags.RealTimeOn ? "Bật theo cấu hình" : "Tắt theo cấu hình", "Cần kiểm tra log để xác nhận watcher thực tế");
                Row(table, "USB Guard", FeatureFlags.UsbProtection ? "Bật" : "Tắt", "Cấu hình cục bộ/SQL");
                Row(table, "Download Guard", FeatureFlags.DownloadProtection ? "Bật" : "Tắt", "Cấu hình cục bộ/SQL");
                Row(table, "Behavior Monitor", FeatureFlags.BehaviorWatch ? "Bật" : "Tắt", "WMI/process events");
                Row(table, "Task Scheduler", "Kiểm tra tại Windows", "Dịch vụ Schedule; xem taskschd.msc để xác nhận");
                try { using (var cn = DbConnection.Create()) { cn.Open(); Row(table, "SQL Server", "Kết nối OK", cn.DataSource); } }
                catch (Exception ex) { Row(table, "SQL Server", "Không kết nối", ex.Message); }
            }
            if (module == 8)
            {
                using (var process = Process.GetCurrentProcess())
                {
                    process.Refresh();
                    Row(table, "Bộ nhớ làm việc", (process.WorkingSet64 / (1024.0 * 1024)).ToString("N1") + " MiB", "RAM tiến trình hiện tại");
                    Row(table, "Bộ nhớ private", (process.PrivateMemorySize64 / (1024.0 * 1024)).ToString("N1") + " MiB", "Memory private");
                    Row(table, "Số luồng", process.Threads.Count, "Windows threads");
                    Row(table, "Thời gian CPU", process.TotalProcessorTime.ToString(), "Tổng CPU time, không phải % CPU tức thời");
                    Row(table, "Thời điểm khởi chạy", process.StartTime.ToString("dd/MM/yyyy HH:mm:ss"), "WinForms process");
                }
            }
            if (module == 9)
            {
                Row(table, "Kiểm thử SHA-256", "Có thể chạy", "Dữ liệu kiểm thử 'abc' vô hại, so sánh digest chuẩn");
                Row(table, "Kiểm thử chữ ký PE", "Công cụ phân tích", "Phân tích file EXE/DLL cục bộ");
                Row(table, "Kiểm thử phát hiện thực tế", "Cần mẫu an toàn và ground truth", "Không tự khẳng định TPR/FPR nếu chưa có tập nhãn");
            }
            if (module == 10)
            {
                Row(table, "Tự động cập nhật chữ ký", FeatureFlags.AutoUpdateEnabled ? "Bật" : "Tắt", "Được quản lý tại trang Admin");
                Row(table, "Cập nhật thủ công", "Sẵn sàng", "Sử dụng công cụ nhập Full CSV MalwareBazaar");
                Row(table, "VirusTotal", VirusTotalClient.IsConfigured ? "Kết nối đã cấu hình" : "Chưa có khóa", "Không phải nguồn chữ ký được tự phê duyệt");
            }
            if (module == 12)
            {
                Row(table, "SQL BACKUP", "COPY_ONLY + CHECKSUM", "Ghi trên máy chạy dịch vụ SQL Server");
                Row(table, "RESTORE VERIFYONLY", "Có", "Xác minh khả năng đọc .bak trên SQL Server");
                Row(table, "Phục hồi", "Bước kiểm soát bằng SSMS", "Không ghi đè CSDL đang chạy khi chưa dừng engine và sao lưu dữ liệu hiện tại");
            }
            if (module == 13)
            {
                try { using (var cn = DbConnection.Create()) { cn.Open(); Row(table, "Kết nối SQL", "OK", cn.DataSource); } }
                catch (Exception ex) { Row(table, "Kết nối SQL", "LỖI", ex.Message); }
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScanAndRemoveVirus");
                Row(table, "Thư mục cấu hình", Directory.Exists(dir) ? "Tồn tại" : "Chưa tạo", dir);
                Row(table, "Windows Task Scheduler", Environment.OSVersion.Platform == PlatformID.Win32NT ? "Hệ điều hành Windows" : "Chưa hỗ trợ", "Dùng taskschd.msc để xem trạng thái dịch vụ");
                Row(table, "VirusTotal key", VirusTotalClient.IsConfigured ? "Đã cấu hình" : "Chưa lưu", "Lưu DPAPI theo tài khoản Windows");
                Row(table, "Nhật ký quản trị", File.Exists(AdminAudit.PathName) ? "Đã tạo" : "Chưa có sự kiện", "Chỉ đọc được dưới tài khoản Windows này");
            }
            if (module == 14)
            {
                Row(table, "API v3", VirusTotalClient.IsConfigured ? "Có khóa" : "Chưa cấu hình", "Không hiển thị giá trị API key");
                Row(table, "Tự động tra SHA-256", FeatureFlags.VtAutoQuery ? "Bật" : "Tắt", "Giới hạn API áp dụng theo client");
                try { Row(table, "Cho phép gửi mẫu", AppSettings.Load().SendSamples ? "Bật" : "Tắt", "Không tự upload file; luôn yêu cầu xác nhận"); }
                catch (Exception ex) { Row(table, "Cho phép gửi mẫu", "Không đọc được", ex.Message); }
                Row(table, "Giới hạn mặc định", "4 lần gọi/phút", "Hạn mức thật do VirusTotal cấp cho tài khoản");
            }
            return table;
        }
    }
}
