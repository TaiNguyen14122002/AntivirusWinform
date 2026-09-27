using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus
{
    public partial class FrmMain : Form
    {
        private readonly UcTongQuan ucTongQuan = new UcTongQuan();
        private readonly UcBaoVe ucBaoVe = new UcBaoVe();
        private readonly UcLichSu ucLichSu = new UcLichSu();
        private readonly UcCachLy ucCachLy = new UcCachLy();
        private readonly UcCaiDat ucCaiDat = new UcCaiDat();

        // Mục điều hướng sidebar (mockup: Tổng quan · Bảo vệ · Cách ly · Lịch sử, Cài đặt ở đáy)
        private UiNavItem btnTongQuan;
        private UiNavItem btnBaoVe;
        private UiNavItem btnCachLy;
        private UiNavItem btnLichSu;
        private UiNavItem btnCaiDat;
        private readonly List<UiNavItem> navItems = new List<UiNavItem>();

        private void LoadContent(UserControl control)
        {
            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            control.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(control);
            pnlContent.ResumeLayout();
        }

        private void ActiveSidebar(UiNavItem item)
        {
            foreach (UiNavItem it in navItems) { it.Active = false; it.Invalidate(); }
            if (item == null) return;
            item.Active = true;
            item.Invalidate();
        }

        /// <summary>Dựng sidebar bằng code: logo thương hiệu ở designer, các mục điều hướng ở đây.</summary>
        private void BuildSidebar()
        {
            picBrand.Image = UiIcons.BrandShield(36);

            btnTongQuan = MakeNav("Tổng quan", UiIcons.Home(18, Theme.TextGray));
            btnBaoVe = MakeNav("Bảo vệ", UiIcons.ShieldSearch(18, Theme.TextGray));
            btnCachLy = MakeNav("Cách ly", UiIcons.Tray(18, Theme.TextGray));
            btnLichSu = MakeNav("Lịch sử", UiIcons.Clock(18, Theme.TextGray));

            // Xếp mục điều hướng bằng toạ độ tuyệt đối: TableLayoutPanel + AutoSize + Dock=Top
            // co hàng không đúng như khai báo, còn cách này thì luôn chính xác.
            int y = 8;
            foreach (UiNavItem it in new[] { btnTongQuan, btnBaoVe, btnCachLy, btnLichSu })
            {
                PlaceNav(it, pnlNavHost, y);
                y += NavStep;
            }

            // Cài đặt nằm ở khối đáy, ngay trên dòng phiên bản
            btnCaiDat = MakeNav("Cài đặt", UiIcons.Gear(18, Theme.TextGray));
            PlaceNav(btnCaiDat, pnlSidebarBottom, 12);
        }

        const int NavStep = 50;   // bước nhảy giữa hai mục sidebar

        static void PlaceNav(UiNavItem it, System.Windows.Forms.Control host, int top)
        {
            it.Location = new Point(14, top);
            it.Size = new Size(host.Width - 28, 44);
            it.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            host.Controls.Add(it);
        }

        private UiNavItem MakeNav(string text, Bitmap icon)
        {
            var item = new UiNavItem(text, icon);
            item.Click += delegate { Navigate(item); };
            navItems.Add(item);
            return item;
        }

        private void Navigate(UiNavItem item)
        {
            if (item == btnBaoVe) LoadContent(ucBaoVe);
            else if (item == btnCachLy) LoadContent(ucCachLy);
            else if (item == btnLichSu) LoadContent(ucLichSu);
            else if (item == btnCaiDat) LoadContent(ucCaiDat);
            else LoadContent(ucTongQuan);
            ActiveSidebar(item);
        }

        /// <summary>
        /// Đưa giao diện về một trang cụ thể để chụp ảnh nghiệm thu (--shot-view &lt;tên&gt;).
        /// Chỉ phục vụ việc đối chiếu giao diện với bản thiết kế — không phải luồng người dùng.
        /// Tên hợp lệ: baove · cachly · lichsu · caidat · chitiet · nangcao[.full|.folder|.files|.custom]
        /// </summary>
        internal void ShowForShot(string view)
        {
            string v = (view ?? "").Trim().ToLowerInvariant();
            if (v == "baove") { Navigate(btnBaoVe); return; }
            if (v == "cachly") { Navigate(btnCachLy); return; }
            if (v == "lichsu") { Navigate(btnLichSu); return; }
            if (v == "caidat") { Navigate(btnCaiDat); return; }

            Navigate(btnTongQuan);
            if (v == "chitiet") { ucTongQuan.MoChiTietKetQua(0); return; }
            if (!v.StartsWith("nangcao")) return;

            CheDoQuetNangCao mode = CheDoQuetNangCao.Folder;   // mockup 6 vẽ sẵn chế độ "Quét thư mục"
            if (v.EndsWith(".full")) mode = CheDoQuetNangCao.FullSystem;
            else if (v.EndsWith(".files")) mode = CheDoQuetNangCao.Files;
            else if (v.EndsWith(".custom")) mode = CheDoQuetNangCao.Custom;
            ucTongQuan.MoQuetNangCao(mode);
        }

        public FrmMain()
        {
            InitializeComponent();
            BuildSidebar();

            // Khởi động các guard theo cờ đã lưu (USB/Tải xuống/Hành vi/StartUp) + khôi phục RT
            FeatureFlags.LoadFromStore();
            GuardService.ApplyAll();
            if (FeatureFlags.RealTimeOn && !RealTimeProtection.IsRunning)
            {
                try { RealTimeProtection.Start(); } catch (Exception) { FeatureFlags.RealTimeOn = false; }
            }

            LoadContent(ucTongQuan);
            ActiveSidebar(btnTongQuan);
        }

        // API điều hướng mở thẳng tab Cách ly — ngang hàng MoTabLichSu
        public void MoTabCachLy()
        {
            LoadContent(ucCachLy);
            ActiveSidebar(btnCachLy);
        }

        // API cho các UserControl điều hướng (vd: link "Mở tab Lịch sử" ở thẻ Hoạt động gần đây)
        public void MoTabLichSu()
        {
            LoadContent(ucLichSu);
            ActiveSidebar(btnLichSu);
        }

        // ==== RESPONSIVE ====
        // Padding vùng nội dung co theo bề rộng cửa sổ (30px khi rộng, 16px khi hẹp)
        private void FrmMain_Resize(object sender, EventArgs e)
        {
            int pad = Width < 1180 ? 16 : 30;
            if (pnlContent.Padding.Left != pad)
                pnlContent.Padding = new Padding(pad, 20, pad, 20);
        }
    }
}
