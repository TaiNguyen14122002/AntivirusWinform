
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus
{
    public partial class FrmMain : Form
    {
        // =====================================================
        // 1. USER CONTROLS
        // =====================================================

        private readonly UcTongQuan ucTongQuan = new UcTongQuan();
        private readonly UcBaoVe ucBaoVe = new UcBaoVe();
        private readonly UcLichSu ucLichSu = new UcLichSu();
        private readonly UcCachLy ucCachLy = new UcCachLy();
        private readonly UcCaiDat ucCaiDat = new UcCaiDat();
        private readonly UcQuanTri ucQuanTri = new UcQuanTri();

        private UserControl currentControl;
        private Label lblSidebarState;
        private System.Windows.Forms.Timer adminSessionMonitor;

        // =====================================================
        // 2. SIDEBAR
        // =====================================================

        private UiNavItem btnTongQuan;
        private UiNavItem btnBaoVe;
        private UiNavItem btnCachLy;
        private UiNavItem btnLichSu;
        private UiNavItem btnCaiDat;
        private UiNavItem btnQuanTri;

        private readonly List<UiNavItem> navItems =
            new List<UiNavItem>();

        private const int NavStep = 50;
        private const int NavHeight = 44;
        private const int NavMargin = 14;

        private bool sidebarInitialized;
        private bool isNavigating;

        // =====================================================
        // 3. CONSTRUCTOR
        // =====================================================

        public FrmMain() : this(false)
        {
        }

        public FrmMain(bool launchedFromWindows)
        {
            // Khi Windows tự mở chương trình, bỏ qua hộp thoại SQL yêu cầu OK.
            // Nếu SQL chưa sẵn sàng, các thành phần tiếp tục dùng xử lý lỗi sẵn có.
            // Mở ứng dụng thủ công vẫn giữ bước kiểm tra SQL ban đầu.
            if (!launchedFromWindows)
                KiemTraKetNoiSqlTruocKhiMoGiaoDien();

            InitializeComponent();
            // Cơ sở kế thừa cho các điều khiển không có font khai báo riêng.
            Font = Theme.DefaultUiFont;

            // Bật DoubleBuffered cho Form.
            DoubleBuffered = true;

            // Hạn chế nhấp nháy cho các panel chính.
            Theme.BatDoubleBuffer(pnlSidebar);
            Theme.BatDoubleBuffer(pnlContent);

            // Tạm dừng bố cục trong quá trình khởi tạo.
            SuspendLayout();
            pnlSidebar.SuspendLayout();
            pnlContent.SuspendLayout();

            try
            {
                BuildSidebar();

                // Tải cấu hình bảo vệ đã lưu.
                InitializeProtection();

                // Hiển thị trang Tổng quan.
                Navigate(btnTongQuan);
            }
            finally
            {
                pnlContent.ResumeLayout(true);
                pnlSidebar.ResumeLayout(true);
                ResumeLayout(true);
            }

            // Đăng ký sự kiện thay đổi kích thước.
            // Không đăng ký trùng nếu Designer đã đăng ký.
            Resize -= FrmMain_Resize;
            Resize += FrmMain_Resize;

            // Cập nhật bố cục ban đầu.
            UpdateResponsiveLayout();
            InitializeSketch20();
            InitializeProtectionNotifications();
            // Khi phiên hết hạn, không để trang quản trị nhạy cảm tiếp tục hiển thị.
            // Timer chỉ KIỂM TRA, không gia hạn session.
            adminSessionMonitor = new System.Windows.Forms.Timer { Interval = 3000 };
            adminSessionMonitor.Tick += (sender, e) =>
            {
                if (IsDisposed || AdminSecurity.IsAuthenticated) return;
                if (exactView != null && exactView.Page == "admin")
                    exactView.Go("overview");
                else if (currentControl == ucQuanTri && btnTongQuan != null)
                    Navigate(btnTongQuan);
            };
            adminSessionMonitor.Start();
            FormClosed += (sender, e) =>
            {
                if (adminSessionMonitor != null) { adminSessionMonitor.Stop(); adminSessionMonitor.Dispose(); }
                try { GuardService.StopAll(); } catch (Exception ex) { Debug.WriteLine("Guard stop: " + ex.Message); }
                try { RealTimeProtection.Stop(); } catch (Exception ex) { Debug.WriteLine("Realtime stop: " + ex.Message); }
                AdminSecurity.Shutdown();
            };
        }

        // =====================================================
        // 3A. KIỂM TRA KẾT NỐI SQL TRƯỚC KHI MỞ GIAO DIỆN CHÍNH
        // =====================================================

        private static void KiemTraKetNoiSqlTruocKhiMoGiaoDien()
        {
            try
            {
                // Lấy chuỗi kết nối từ Database/DbConnection.cs:
                // localhost\SQLEXPRESS, AntivirusDB, Windows Authentication.
                using (SqlConnection conn = DbConnection.Create())
                {
                    conn.Open();

                    string thongTinChuKy;
                    try
                    {
                        // COUNT_BIG trả về bigint, tránh tràn số nếu bảng có nhiều bản ghi.
                        const string sql = "SELECT COUNT_BIG(*) FROM dbo.VirusSignatures";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            long soChuKyVirus = Convert.ToInt64(cmd.ExecuteScalar());
                            thongTinChuKy = "Bảng: VirusSignatures" + Environment.NewLine +
                                            "Tổng số chữ ký virus: " + soChuKyVirus.ToString("N0");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Kết nối đã thành công, nhưng bảng có thể không tồn tại
                        // hoặc tài khoản Windows không có quyền SELECT.
                        thongTinChuKy = "Không thể đọc bảng VirusSignatures." +
                                        Environment.NewLine + "Chi tiết: " + ex.Message;
                    }

                    MessageBox.Show(
                        "KẾT NỐI SQL SERVER THÀNH CÔNG!" + Environment.NewLine + Environment.NewLine +
                        "Server: " + conn.DataSource + Environment.NewLine +
                        "Database: " + conn.Database + Environment.NewLine + Environment.NewLine +
                        thongTinChuKy + Environment.NewLine + Environment.NewLine +
                        "Nhấn OK để mở giao diện chính.",
                        "Kiểm tra cơ sở dữ liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "KHÔNG THỂ KẾT NỐI SQL SERVER!" + Environment.NewLine + Environment.NewLine +
                    "Lỗi: " + ex.Message + Environment.NewLine + Environment.NewLine +
                    "Nhấn OK để tiếp tục mở ứng dụng. Một số chức năng CSDL có thể không hoạt động.",
                    "Kiểm tra cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =====================================================
        // 4. KHỞI TẠO CÁC CHỨC NĂNG BẢO VỆ
        // =====================================================

        private void InitializeProtection()
        {
            try
            {
                FeatureFlags.LoadFromStore();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Không thể tải cấu hình bảo vệ: " +
                    ex.ToString()
                );
            }

            try
            {
                GuardService.ApplyAll();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Không thể khởi tạo GuardService: " +
                    ex.ToString()
                );
            }

            if (FeatureFlags.RealTimeOn &&
                !RealTimeProtection.IsRunning)
            {
                try
                {
                    RealTimeProtection.Start();
                }
                catch (Exception ex)
                {
                    FeatureFlags.RealTimeOn = false;

                    Debug.WriteLine(
                        "Không thể khởi động bảo vệ thời gian thực: " +
                        ex.ToString()
                    );
                }
            }
        }

        // =====================================================
        // 5. LOAD USER CONTROL
        // =====================================================

        private void LoadContent(UserControl control)
        {
            if (control == null || control.IsDisposed)
                return;

            if (currentControl == control &&
                pnlContent.Controls.Contains(control))
            {
                return;
            }

            pnlContent.SuspendLayout();

            try
            {
                // Chỉ gỡ các control khỏi panel.
                // Không Dispose vì các trang được tái sử dụng.
                pnlContent.Controls.Clear();

                control.Dock = DockStyle.Fill;
                control.Margin = Padding.Empty;
                control.Visible = true;

                pnlContent.Controls.Add(control);
                control.BringToFront();

                currentControl = control;
            }
            finally
            {
                pnlContent.ResumeLayout(true);
            }
        }

        // =====================================================
        // 6. KHỞI TẠO SIDEBAR
        // =====================================================

        private void BuildSidebar()
        {
            if (sidebarInitialized)
                return;

            pnlNavHost.SuspendLayout();
            pnlSidebarBottom.SuspendLayout();

            try
            {
                // Logo thương hiệu.
                picBrand.Image = UiIcons.BrandShield(32);
                lblBrandName.ForeColor = Theme.TextDark;
                lblBrandTag.AutoSize = false;
                lblBrandTag.Size = new Size(134, 34);
                lblSidebarState = SketchUi.Label("", Theme.SmallFont, Theme.Green);
                lblSidebarState.SetBounds(22, 20, 176, 30);
                pnlSidebarBottom.Controls.Add(lblSidebarState);
                lblVersion.Location = new Point(22, 56);
                RealTimeProtection.StatusChanged += SidebarProtectionChanged;
                Shown += delegate { SidebarProtectionChanged(RealTimeProtection.IsRunning); };
                FormClosed += delegate { RealTimeProtection.StatusChanged -= SidebarProtectionChanged; };

                // Tạo các mục điều hướng.
                btnTongQuan = MakeNav(
                    "Tổng quan",
                    UiIcons.Home(18, Theme.TextGray)
                );

                btnBaoVe = MakeNav(
                    "Bảo vệ",
                    UiIcons.ShieldSearch(
                        18,
                        Theme.TextGray
                    )
                );

                btnCachLy = MakeNav(
                    "Cách ly",
                    UiIcons.Tray(18, Theme.TextGray)
                );

                btnLichSu = MakeNav(
                    "Lịch sử",
                    UiIcons.Clock(18, Theme.TextGray)
                );

                // Đặt các nút điều hướng.
                int y = 8;

                UiNavItem[] mainItems =
                {
                    btnTongQuan,
                    btnBaoVe,
                    btnCachLy,
                    btnLichSu
                };

                foreach (UiNavItem item in mainItems)
                {
                    PlaceNav(
                        item,
                        pnlNavHost,
                        y
                    );

                    y += NavStep;
                }

                btnQuanTri = MakeNav("Quản trị", UiIcons.ShieldSearch(18, Theme.TextGray));
                PlaceNav(btnQuanTri, pnlNavHost, y);

                // Nút Cài đặt nằm ở cuối sidebar.
                btnCaiDat = MakeNav(
                    "Cài đặt",
                    UiIcons.Gear(18, Theme.TextGray)
                );

                PlaceNav(
                    btnCaiDat,
                    pnlNavHost,
                    290
                );

                sidebarInitialized = true;
            }
            finally
            {
                pnlSidebarBottom.ResumeLayout(true);
                pnlNavHost.ResumeLayout(true);
            }
        }

        // =====================================================
        // 7. TẠO NÚT ĐIỀU HƯỚNG
        // =====================================================

        private UiNavItem MakeNav(
            string text,
            Bitmap icon)
        {
            UiNavItem item = new UiNavItem(
                text,
                icon
            );

            item.Click += delegate
            {
                Navigate(item);
            };

            navItems.Add(item);

            return item;
        }

        // =====================================================
        // 8. ĐỊNH VỊ NÚT SIDEBAR
        // =====================================================

        private static void PlaceNav(
            UiNavItem item,
            System.Windows.Forms.Control host,
            int top)
        {
            if (item == null || host == null)
                return;

            host.SuspendLayout();

            try
            {
                item.Dock = DockStyle.None;

                item.Location = new Point(
                    NavMargin,
                    top
                );

                item.Size = new Size(
                    Math.Max(
                        100,
                        host.ClientSize.Width -
                        NavMargin * 2
                    ),
                    NavHeight
                );

                item.Anchor =
                    AnchorStyles.Top |
                    AnchorStyles.Left |
                    AnchorStyles.Right;

                host.Controls.Add(item);
            }
            finally
            {
                host.ResumeLayout(true);
            }
        }

        // =====================================================
        // 9. CẬP NHẬT TRẠNG THÁI SIDEBAR
        // =====================================================

        private void ActiveSidebar(UiNavItem selectedItem)
        {
            foreach (UiNavItem item in navItems)
            {
                if (item == null || item.IsDisposed)
                    continue;

                bool shouldBeActive =
                    item == selectedItem;

                if (item.Active != shouldBeActive)
                {
                    item.Active = shouldBeActive;
                    item.Invalidate();
                }
            }
        }

        // =====================================================
        // 10. ĐIỀU HƯỚNG
        // =====================================================

        private void Navigate(UiNavItem item)
        {
            if (isNavigating)
                return;

            isNavigating = true;

            try
            {
                UserControl target;

                if (item == btnBaoVe)
                {
                    target = ucBaoVe;
                }
                else if (item == btnCachLy)
                {
                    target = ucCachLy;
                }
                else if (item == btnLichSu)
                {
                    target = ucLichSu;
                }
                else if (item == btnCaiDat)
                {
                    target = ucCaiDat;
                }
                else if (item == btnQuanTri)
                {
                    if (!AdminSecurity.EnsureLogin(this)) return;
                    target = ucQuanTri;
                    ucQuanTri.Activated();
                }
                else
                {
                    target = ucTongQuan;
                    item = btnTongQuan;
                }

                LoadContent(target);
                ActiveSidebar(item);
            }
            finally
            {
                isNavigating = false;
            }
        }

        // =====================================================
        // 11. API ĐIỀU HƯỚNG
        // =====================================================

        public void MoCaiDatVirusTotal()
        {
            if (exactView != null) { exactView.SettingsTab = 0; exactView.Go("settings"); return; }
            Navigate(btnCaiDat);
        }

        private void SidebarProtectionChanged(bool running)
        {
            if (lblSidebarState == null || IsDisposed) return;
            if (InvokeRequired)
            {
                if (IsHandleCreated) BeginInvoke((MethodInvoker)(() => SidebarProtectionChanged(running)));
                return;
            }
            lblSidebarState.Text = running ? "● Bảo vệ đang hoạt động" : "● Bảo vệ thời gian thực đang tắt";
            lblSidebarState.ForeColor = running ? Theme.Green : Theme.Amber;
        }

        public void MoTabCachLy()
        {
            if (exactView != null) { exactView.Go("quarantine"); return; }
            Navigate(btnCachLy);
        }

        public void MoTabLichSu()
        {
            if (exactView != null) { exactView.Go("history"); return; }
            Navigate(btnLichSu);
        }

        // =====================================================
        // 12. CHẾ ĐỘ CHỤP GIAO DIỆN
        // =====================================================

        internal void ShowForShot(string view)
        {
            string v = (view ?? "")
                .Trim()
                .ToLowerInvariant();

            if (exactView != null)
            {
                if (v.StartsWith("nangcao", StringComparison.Ordinal))
                { exactView.AdvancedMode = v.Contains("full") ? 0 : v.Contains("file") ? 2 : 1; exactView.Go("advanced"); }
                else exactView.Go(v == "baove" ? "protection" : v == "cachly" ? "quarantine" : v == "lichsu" ? "history" : v == "caidat" ? "settings" : v == "chitiet" ? "details" : "overview");
                return;
            }

            switch (v)
            {
                case "baove":
                    Navigate(btnBaoVe);
                    return;

                case "cachly":
                    Navigate(btnCachLy);
                    return;

                case "lichsu":
                    Navigate(btnLichSu);
                    return;

                case "caidat":
                    Navigate(btnCaiDat);
                    return;
            }

            Navigate(btnTongQuan);

            if (v == "chitiet")
            {
                ucTongQuan.MoChiTietKetQua(0);
                return;
            }

            if (!v.StartsWith(
                "nangcao",
                StringComparison.Ordinal))
            {
                return;
            }

            CheDoQuetNangCao mode =
                CheDoQuetNangCao.Folder;

            if (v.EndsWith(
                ".full",
                StringComparison.Ordinal))
            {
                mode = CheDoQuetNangCao.FullSystem;
            }
            else if (v.EndsWith(
                ".files",
                StringComparison.Ordinal))
            {
                mode = CheDoQuetNangCao.Files;
            }
            else if (v.EndsWith(
                ".custom",
                StringComparison.Ordinal))
            {
                mode = CheDoQuetNangCao.Custom;
            }

            ucTongQuan.MoQuetNangCao(mode);
        }

        // =====================================================
        // 13. RESPONSIVE LAYOUT
        // =====================================================

        private void FrmMain_Resize(
            object sender,
            EventArgs e)
        {
            UpdateResponsiveLayout();
        }

        private void UpdateResponsiveLayout()
        {
            if (pnlContent == null ||
                pnlContent.IsDisposed)
            {
                return;
            }

            int padding =
                ClientSize.Width < 1180
                    ? 20
                    : 26;

            Padding newPadding = new Padding(
                padding,
                28,
                padding,
                22
            );

            if (pnlContent.Padding != newPadding)
            {
                pnlContent.Padding = newPadding;
            }

            UpdateSidebarLayout();
        }

        // =====================================================
        // 14. RESPONSIVE SIDEBAR
        // =====================================================

        private void UpdateSidebarLayout()
        {
            if (!sidebarInitialized)
                return;

            UpdateNavHost(
                pnlNavHost,
                new UiNavItem[]
                {
                    btnTongQuan,
                    btnBaoVe,
                    btnCachLy,
                    btnLichSu,
                    btnQuanTri
                }
            );

            UpdateNavHost(
                pnlSidebarBottom,
                new UiNavItem[]
                {
                    btnCaiDat
                }
            );
        }

        private static void UpdateNavHost(
            System.Windows.Forms.Control host,
            IEnumerable<UiNavItem> items)
        {
            if (host == null || host.IsDisposed)
                return;

            int width = Math.Max(
                100,
                host.ClientSize.Width -
                NavMargin * 2
            );

            host.SuspendLayout();

            try
            {
                foreach (UiNavItem item in items)
                {
                    if (item == null || item.IsDisposed)
                        continue;

                    if (item.Width != width)
                    {
                        item.Width = width;
                    }
                }
            }
            finally
            {
                host.ResumeLayout(true);
            }
        }

        private void pnlContent_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
