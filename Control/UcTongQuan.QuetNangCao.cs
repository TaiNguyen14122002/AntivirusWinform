using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>4 chế độ của dropdown "Quét nâng cao" (SPEC-UcTongQuan.md §3.4).</summary>
    public enum CheDoQuetNangCao { FullSystem, Folder, Files, Custom }

    /// <summary>Loại tệp quét ở chế độ "Quét tùy chỉnh" (SPEC §5.5).</summary>
    public enum LoaiTepQuet { TatCa, ThucThi, Nen, TaiLieu, TuyChinh }

    /// <summary>
    /// MÀN HÌNH (d) — "Quét nâng cao" của UcTongQuan (SPEC-UcTongQuan.md §3.4, §5, §6.2, §8).
    /// Cột trái: 4 thẻ chọn chế độ (toàn bộ hệ thống / thư mục / tệp / tùy chỉnh);
    /// cột phải: nội dung chi tiết của chế độ đang chọn (đổi ngay, giữ nguyên dữ liệu đã nhập);
    /// cuối trang: hộp "Lưu ý" theo chế độ + nút "Bắt đầu quét".
    /// </summary>
    public partial class UcTongQuan
    {
        // ---- phân loại phần mở rộng dùng chung với trang Chi tiết ----
        internal static readonly HashSet<string> ExecutableExtensions = new HashSet<string>(
            new[] { ".exe", ".dll", ".sys", ".bat", ".cmd", ".scr", ".com", ".msi" }, StringComparer.OrdinalIgnoreCase);
        internal static readonly HashSet<string> ScriptExtensions = new HashSet<string>(
            new[] { ".ps1", ".vbs", ".js", ".jse", ".wsf", ".hta", ".sh" }, StringComparer.OrdinalIgnoreCase);
        internal static readonly HashSet<string> ArchiveExtensions = new HashSet<string>(
            new[] { ".zip", ".rar", ".7z", ".tar", ".gz" }, StringComparer.OrdinalIgnoreCase);
        internal static readonly HashSet<string> DocumentExtensions = new HashSet<string>(
            new[] { ".doc", ".docx", ".pdf", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf" }, StringComparer.OrdinalIgnoreCase);

        // ---- trạng thái trang (d) ----
        private CheDoQuetNangCao cheDo = CheDoQuetNangCao.FullSystem;
        private RadioButton[] theCheDo;
        private Label[] theCheDoMoTa;
        private bool dangChonTheCheDo;          // chặn chạy lại khi tự đồng bộ trạng thái 4 thẻ chế độ
        private Label lblCheDoTomTat;
        private Button btnQuayLaiNangCao;
        private TableLayoutPanel pnlNoiDungPhai;
        private Panel pnlFullSystem, pnlFolder, pnlFiles, pnlCustom;
        private Panel pnlLuuY;
        private Label lblLuuY;
        private Button btnBatDauQuet;
        private ToolTip tips;

        // ---- BỐ CỤC RESPONSIVE CỦA TRANG (d) (25/09/2026, lần 7) ----
        // Trang (d) tự xếp lại theo BỀ RỘNG THẬT của vùng trang — KHÔNG dựng lại control nên 4 thẻ chế độ
        // giữ nguyên trạng thái chọn và dữ liệu đã nhập của từng chế độ:
        //   • >= 1000px: 2 cột — 4 thẻ xếp dọc (300px) bên trái, nội dung bên phải (đúng thiết kế cũ);
        //   • 800..999px: 4 thẻ nằm 1 HÀNG ngang trên đầu, nội dung chiếm trọn bề ngang bên dưới;
        //   • < 800px   : 4 thẻ về LƯỚI 2x2, nội dung bên dưới.
        // Nhãn "Đang chọn: …" và nút "Bắt đầu quét" cũng xuống hàng riêng khi hẹp để không bị cắt.
        private TableLayoutPanel tlpTrangNangCao;    // root: hàng 0 header · hàng 1 thân · hàng 2 chân trang
        private TableLayoutPanel tlpHeaderNangCao;
        private TableLayoutPanel tlpThanNangCao;     // thân: 2 cột (rộng) hay 1 cột xếp dọc (hẹp)
        private TableLayoutPanel pnlCheDoTrai;       // lưới 4 thẻ: dọc (rộng) / 1 hàng ngang / lưới 2x2
        private TableLayoutPanel tlpChanNangCao;
        private TableLayoutPanel[] oTheCheDo;        // khung từng thẻ (hàng 0 = tiêu đề, hàng 1 = mô tả)
        private bool dangXepBoCuc;                   // chặn đệ quy khi tự xếp lại bố cục
        private bool dangXepDoc;                     // bố cục đang áp: true = thẻ trên / nội dung dưới
        private bool dangTheMotHang;                 // (chỉ khi xếp dọc) true = 4 thẻ 1 hàng, false = lưới 2x2

        private const int RongNhanTomTat = 420;      // bề rộng nhãn "Đang chọn: …" khi đủ chỗ (SPEC §3.4)
        private const float CaoHeaderRong = 62F;     // hàng header khi 2 cột (nút · tiêu đề · nhãn tóm tắt cùng hàng)
        private const float CaoHeaderHep = 82F;      // hàng header khi hẹp: 52 (nút + tiêu đề) + 26 (tóm tắt) + lề 4
        private const float CaoChanRong = 66F;       // hàng chân trang khi 2 cột: hộp "Lưu ý" | nút "Bắt đầu quét"
        private const float CaoChanHep = 112F;       // hàng chân trang khi hẹp: "Lưu ý" (60) + 6 + nút (42) + lề 4
        private const float CaoTheDoc = 86F;         // thẻ chế độ khi xếp dọc (như thiết kế cũ)
        private const float CaoTheNgang = 74F;       // thẻ khi 4 thẻ nằm 1 hàng ngang
        private const float CaoTheNho = 74F;         // thẻ trong lưới 2x2 (2 hàng = 148)
        private const float RongCotCheDo = 300F;     // cột trái (4 thẻ xếp dọc) của bố cục 2 cột
        private const int NguongHaiCot = 1000;       // >= 1000px: 2 cột — nhỏ hơn: thẻ lên trên, nội dung xuống dưới
        private const int NguongMotHang = 800;       // >= 800px (khi đã xếp dọc): 4 thẻ 1 hàng — nhỏ hơn: lưới 2x2

        /// <summary>
        /// Bề rộng tối thiểu của vùng Tổng quan khi mở trang (d): (d) tự xếp lại (2 cột -> 1 hàng -> 2x2)
        /// nên cần ít chỗ hơn ngưỡng 980 của (a)/(b)/(c) — xem UcTongQuan.HienThi/CapNhatRongToiThieu.
        /// </summary>
        internal const int RongToiThieuTrangNangCao = 420;

        // ---- chế độ FullSystem ----
        private CheckBox[] chkFull;
        private DataGridView dgvODia;
        private DataGridViewCheckBoxColumn colODiaChon;

        // ---- chế độ Folder ----
        private TextBox txtThuMuc;
        private Button btnChonThuMuc, btnXoaThuMuc;
        private DataGridView dgvThuMuc;
        private CheckBox[] chkFolder;
        private readonly List<string> dsThuMuc = new List<string>();
        private readonly Dictionary<string, CancellationTokenSource> sizeJobs = new Dictionary<string, CancellationTokenSource>();

        // ---- chế độ Files ----
        private Panel pnlKeoTha;
        private Button btnChonTep, btnXoaTep;
        private DataGridView dgvTep;
        private CheckBox[] chkFiles;
        private readonly List<string> dsTep = new List<string>();

        // ---- chế độ Custom ----
        private DataGridView dgvViTri;
        private Button btnThemViTri, btnXoaDongViTri, btnXoaTatCaViTri;
        private RadioButton[] rdoLoai;
        private TextBox txtPhanMoRong;
        private CheckBox[] chkCustom;
        private CheckBox chkBoQuaTepLon;
        private NumericUpDown numBoQuaTepLon;
        private ComboBox cboDonVi;
        private readonly List<ScanTarget> dsViTri = new List<ScanTarget>();

        /// <summary>Một vị trí quét của chế độ tùy chỉnh (thư mục hoặc tệp lẻ).</summary>
        internal sealed class ScanTarget
        {
            public string Path;
            public bool IsDirectory;
        }

        // ================= TIỆN ÍCH DỰNG CONTROL =================

        /// <summary>CheckBox tuỳ chọn kèm icon ⓘ + ToolTip giải thích (SPEC §3.4 mục 4.1).</summary>
        private System.Windows.Forms.Control OChkTuyChon(string text, string tooltip, bool initial)
        {
            var host = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0) };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var chk = new CheckBox
            {
                Text = text,
                Checked = initial,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                ForeColor = Theme.TextMid,
                Cursor = Cursors.Hand
            };
            chk.CheckedChanged += delegate { CapNhatTrangThaiNutBatDauQuet(); };
            var info = new Label
            {
                Image = UiIcons.Info(14, Theme.TextGray),
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                ImageAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            if (tips == null) tips = new ToolTip();
            tips.SetToolTip(info, tooltip);
            tips.SetToolTip(chk, tooltip);
            host.Controls.Add(chk, 0, 0);
            host.Controls.Add(info, 1, 0);
            return host;
        }

        private static TableLayoutPanel LuoiTuyChon(System.Windows.Forms.Control[] oChk, int soCot)
        {
            var t = new TableLayoutPanel { ColumnCount = soCot, Dock = DockStyle.Fill, Margin = new Padding(0) };
            for (int c = 0; c < soCot; c++)
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / soCot));
            int soHang = (oChk.Length + soCot - 1) / soCot;
            t.RowCount = soHang;
            for (int r = 0; r < soHang; r++) t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            for (int i = 0; i < oChk.Length; i++)
                t.Controls.Add(oChk[i], i % soCot, i / soCot);
            return t;
        }

        private static CheckBox LayCheckBox(System.Windows.Forms.Control host)
        {
            var t = host as TableLayoutPanel;
            return t == null ? null : t.Controls.OfType<CheckBox>().FirstOrDefault();
        }

        private static Label NhanNangCao(string text, Font font, Color color, ContentAlignment align)
        {
            return new Label
            {
                Text = text, Font = font, ForeColor = color, Dock = DockStyle.Fill,
                TextAlign = align, Margin = new Padding(0), AutoEllipsis = true
            };
        }

        private static DataGridView LuoiMoi(int chieuCaoHang)
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                BackgroundColor = Theme.PageBg
            };
            Theme.StyleGrid(g);
            if (chieuCaoHang > 0) foreach (DataGridViewRow r in g.Rows) r.Height = chieuCaoHang;
            return g;
        }

        // ================= DỰNG GIAO DIỆN TRANG (d) =================

        /// <summary>
        /// Dựng ruột trang (d): header · thân (thẻ chế độ + nội dung) · chân trang ("Lưu ý" + nút).
        /// Bố cục ở đây là bố cục RỘNG (2 cột) như thiết kế; bố cục hẹp do XepBoCucNangCao() xếp lại
        /// ngay sau đó và mỗi lần bề rộng vùng trang đổi (resize cửa sổ).
        /// </summary>
        private void BuildQuetNangCaoView()
        {
            var root = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Name = "tlpQuetNangCao" };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoHeaderRong));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoChanRong));
            tlpTrangNangCao = root;
            root.Controls.Add(DungHeaderNangCao(), 0, 0);

            var body = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0), Name = "tlpThanNangCao" };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RongCotCheDo));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpThanNangCao = body;
            body.Controls.Add(DungCotCheDo(), 0, 0);

            pnlNoiDungPhai = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Name = "pnlNoiDungPhai" };
            pnlNoiDungPhai.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlNoiDungPhai.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlFullSystem = DungFullSystem();
            pnlFolder = DungFolder();
            pnlFiles = DungFiles();
            pnlCustom = DungCustom();
            pnlNoiDungPhai.Controls.Add(pnlCustom, 0, 0);
            pnlNoiDungPhai.Controls.Add(pnlFiles, 0, 0);
            pnlNoiDungPhai.Controls.Add(pnlFolder, 0, 0);
            pnlNoiDungPhai.Controls.Add(pnlFullSystem, 0, 0);
            foreach (Panel p in new[] { pnlFullSystem, pnlFolder, pnlFiles, pnlCustom }) p.Visible = false;
            body.Controls.Add(pnlNoiDungPhai, 1, 0);
            root.Controls.Add(body, 0, 1);
            root.Controls.Add(DungChanTrang(), 0, 2);
            // RESPONSIVE (lần 7): bề rộng vùng trang đổi (kéo cửa sổ, hiện/ẩn thanh cuộn) -> xếp lại bố cục
            pnlQuetNangCao.Resize += delegate { XepBoCucNangCao(); };
            pnlQuetNangCao.Controls.Add(root);
            XepBoCucNangCao();   // áp bố cục đúng với bề rộng hiện tại ngay từ lần dựng đầu
        }

        // ================= RESPONSIVE: XẾP LẠI BỐ CỤC TRANG (d) =================

        /// <summary>
        /// Xếp lại bố cục trang (d) theo bề rộng của pnlQuetNangCao (2 cột / 1 hàng / lưới 2x2).
        /// Chỉ ghi khi bố cục ĐỔI nên kéo cửa sổ liên tục cũng không giật; mọi thay đổi chỉ là vị trí +
        /// kích thước ô của TableLayoutPanel, KHÔNG tạo lại control (giữ trạng thái chọn + dữ liệu).
        /// </summary>
        private void XepBoCucNangCao()
        {
            if (pnlCheDoTrai == null || tlpThanNangCao == null || tlpTrangNangCao == null || dangXepBoCuc) return;
            int rong = pnlQuetNangCao == null ? 0 : pnlQuetNangCao.ClientSize.Width;
            if (rong <= 0) return;                       // trang chưa có bề rộng thật -> lần Resize/Layout sau xếp tiếp
            bool xepDoc = rong < NguongHaiCot;           // hẹp: thẻ lên trên, nội dung xuống dưới
            bool theMotHang = rong >= NguongMotHang;     // (chỉ có nghĩa khi xếp dọc) 1 hàng ngang thay vì lưới 2x2
            if (xepDoc == dangXepDoc && theMotHang == dangTheMotHang) return;
            dangXepBoCuc = true;
            try
            {
                dangXepDoc = xepDoc;
                dangTheMotHang = theMotHang;
                XepTheCheDo(xepDoc, theMotHang);
                XepThanTrangNangCao(xepDoc, theMotHang);
                XepHeaderNangCao(xepDoc);
                XepChanTrangNangCao(xepDoc);
                // 3 hàng của root: header · thân (giãn hết phần còn lại) · chân trang — cao theo bố cục đang áp
                DatHang(tlpTrangNangCao,
                    new[] { xepDoc ? CaoHeaderHep : CaoHeaderRong, 100F, xepDoc ? CaoChanHep : CaoChanRong },
                    new[] { SizeType.Absolute, SizeType.Percent, SizeType.Absolute });
            }
            finally { dangXepBoCuc = false; }
        }

        /// <summary>
        /// 4 thẻ chế độ: xếp dọc (bố cục 2 cột) · 1 hàng ngang (vừa) · lưới 2x2 (hẹp).
        /// Di chuyển con TRƯỚC rồi mới đổi lưới để không bao giờ đặt con ra ngoài phạm vi cột/hàng.
        /// </summary>
        private void XepTheCheDo(bool xepDoc, bool theMotHang)
        {
            if (pnlCheDoTrai == null || oTheCheDo == null) return;
            NoiLuoi(pnlCheDoTrai, 4, 5);   // nới tạm đủ chỗ cho mọi vị trí bên dưới
            if (!xepDoc)
            {
                // 2 cột: 4 thẻ xếp dọc + hàng cuối giãn hết phần còn lại (như thiết kế cũ)
                for (int i = 0; i < oTheCheDo.Length; i++) DatOViTri(oTheCheDo[i], 0, i, new Padding(0, 0, 0, 8));
                DatCot(pnlCheDoTrai, new[] { 100F }, new[] { SizeType.Percent });
                DatHang(pnlCheDoTrai, new[] { CaoTheDoc, CaoTheDoc, CaoTheDoc, CaoTheDoc, 100F },
                    new[] { SizeType.Absolute, SizeType.Absolute, SizeType.Absolute, SizeType.Absolute, SizeType.Percent });
            }
            else if (theMotHang)
            {
                // 1 hàng ngang: 4 thẻ chia đều bề ngang trên đầu vùng nội dung
                for (int i = 0; i < oTheCheDo.Length; i++)
                    DatOViTri(oTheCheDo[i], i, 0, new Padding(0, 0, i == oTheCheDo.Length - 1 ? 0 : 8, 0));
                DatCot(pnlCheDoTrai, new[] { 25F, 25F, 25F, 25F },
                    new[] { SizeType.Percent, SizeType.Percent, SizeType.Percent, SizeType.Percent });
                DatHang(pnlCheDoTrai, new[] { 100F }, new[] { SizeType.Percent });
            }
            else
            {
                // Lưới 2x2: mỗi thẻ ~nửa bề ngang -> tiêu đề + mô tả vẫn đủ chỗ đọc
                for (int i = 0; i < oTheCheDo.Length; i++)
                    DatOViTri(oTheCheDo[i], i % 2, i / 2, new Padding(0, 0, i % 2 == 0 ? 8 : 0, 8));
                DatCot(pnlCheDoTrai, new[] { 50F, 50F }, new[] { SizeType.Percent, SizeType.Percent });
                DatHang(pnlCheDoTrai, new[] { CaoTheNho, CaoTheNho }, new[] { SizeType.Absolute, SizeType.Absolute });
            }
        }

        /// <summary>Thân trang: 2 cột (rộng) hay 1 cột — hàng thẻ ở trên, nội dung giãn hết ở dưới (hẹp).</summary>
        private void XepThanTrangNangCao(bool xepDoc, bool theMotHang)
        {
            if (tlpThanNangCao == null) return;
            if (xepDoc)
            {
                DatOViTri(pnlCheDoTrai, 0, 0, new Padding(0));
                DatOViTri(pnlNoiDungPhai, 0, 1, new Padding(0, 8, 0, 0));
                DatCot(tlpThanNangCao, new[] { 100F }, new[] { SizeType.Percent });
                DatHang(tlpThanNangCao, new[] { CaoHangThe(theMotHang), 100F },
                    new[] { SizeType.Absolute, SizeType.Percent });
            }
            else
            {
                DatOViTri(pnlCheDoTrai, 0, 0, new Padding(0));
                DatOViTri(pnlNoiDungPhai, 1, 0, new Padding(8, 0, 0, 0));
                DatCot(tlpThanNangCao, new[] { RongCotCheDo, 100F }, new[] { SizeType.Absolute, SizeType.Percent });
                DatHang(tlpThanNangCao, new[] { 100F }, new[] { SizeType.Percent });
            }
        }

        /// <summary>Chiều cao hàng thẻ của thân trang khi xếp dọc (1 hàng ngang 74px / lưới 2x2 = 2 x 74px).</summary>
        private static float CaoHangThe(bool theMotHang)
        {
            return theMotHang ? CaoTheNgang : CaoTheNho * 2F;
        }

        /// <summary>
        /// Header: hẹp thì nhãn "Đang chọn: …" xuống hàng dưới và trải hết bề ngang
        /// (bỏ bề rộng cứng 420px vốn làm tràn chữ khi cửa sổ hẹp).
        /// </summary>
        private void XepHeaderNangCao(bool xepDoc)
        {
            if (tlpHeaderNangCao == null) return;
            if (xepDoc)
            {
                if (lblCheDoTomTat != null)
                {
                    tlpHeaderNangCao.SetColumnSpan(lblCheDoTomTat, 3);
                    DatOViTri(lblCheDoTomTat, 0, 1, new Padding(0, 0, 0, 4));
                    if (lblCheDoTomTat.Dock != DockStyle.Fill) lblCheDoTomTat.Dock = DockStyle.Fill;
                    if (lblCheDoTomTat.TextAlign != ContentAlignment.MiddleLeft)
                        lblCheDoTomTat.TextAlign = ContentAlignment.MiddleLeft;
                }
                DatHang(tlpHeaderNangCao, new[] { 52F, 26F }, new[] { SizeType.Absolute, SizeType.Absolute });
            }
            else
            {
                if (lblCheDoTomTat != null)
                {
                    tlpHeaderNangCao.SetColumnSpan(lblCheDoTomTat, 1);
                    DatOViTri(lblCheDoTomTat, 2, 0, new Padding(0));
                    if (lblCheDoTomTat.Dock != DockStyle.None) lblCheDoTomTat.Dock = DockStyle.None;
                    if (lblCheDoTomTat.TextAlign != ContentAlignment.MiddleRight)
                        lblCheDoTomTat.TextAlign = ContentAlignment.MiddleRight;
                    if (lblCheDoTomTat.Width != RongNhanTomTat) lblCheDoTomTat.Width = RongNhanTomTat;
                }
                DatHang(tlpHeaderNangCao, new[] { 100F }, new[] { SizeType.Percent });
            }
        }

        /// <summary>
        /// Chân trang: hẹp thì hộp "Lưu ý" ở trên và nút "Bắt đầu quét" giãn HẾT bề ngang ở dưới
        /// (nút to, dễ bấm trên màn hình nhỏ) — đủ rộng thì trở lại "Lưu ý" | nút như thiết kế.
        /// </summary>
        private void XepChanTrangNangCao(bool xepDoc)
        {
            if (tlpChanNangCao == null) return;
            if (xepDoc)
            {
                DatOViTri(pnlLuuY, 0, 0, new Padding(0, 0, 0, 6));
                DatOViTri(btnBatDauQuet, 0, 1, new Padding(0));
                DatCot(tlpChanNangCao, new[] { 100F }, new[] { SizeType.Percent });
                DatHang(tlpChanNangCao, new[] { 100F, 42F }, new[] { SizeType.Percent, SizeType.Absolute });
                if (btnBatDauQuet != null && btnBatDauQuet.Dock != DockStyle.Fill) btnBatDauQuet.Dock = DockStyle.Fill;
            }
            else
            {
                DatOViTri(pnlLuuY, 0, 0, new Padding(0, 0, 8, 0));
                DatOViTri(btnBatDauQuet, 1, 0, new Padding(4, 0, 0, 0));
                DatCot(tlpChanNangCao, new[] { 100F, 0F }, new[] { SizeType.Percent, SizeType.AutoSize });
                DatHang(tlpChanNangCao, new[] { 100F }, new[] { SizeType.Percent });
                if (btnBatDauQuet != null && btnBatDauQuet.Dock != DockStyle.Right) btnBatDauQuet.Dock = DockStyle.Right;
            }
        }

        /// <summary>Nới tạm lưới của TableLayoutPanel đủ chỗ cho mọi vị trí sắp xếp (tránh đặt con ra ngoài lưới).</summary>
        private static void NoiLuoi(TableLayoutPanel t, int soCot, int soHang)
        {
            while (t.ColumnStyles.Count < soCot) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / soCot));
            while (t.RowStyles.Count < soHang) t.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        }

        /// <summary>Đặt lưới cột của TableLayoutPanel — luôn gọi SAU khi đã di chuyển con (cột thừa thì cắt bớt).</summary>
        private static void DatCot(TableLayoutPanel t, float[] kichThuoc, SizeType[] kieu)
        {
            while (t.ColumnStyles.Count < kichThuoc.Length)
                t.ColumnStyles.Add(new ColumnStyle(kieu[t.ColumnStyles.Count], kichThuoc[t.ColumnStyles.Count]));
            while (t.ColumnStyles.Count > kichThuoc.Length) t.ColumnStyles.RemoveAt(t.ColumnStyles.Count - 1);
            for (int i = 0; i < kichThuoc.Length; i++)
            {
                if (t.ColumnStyles[i].SizeType != kieu[i]) t.ColumnStyles[i].SizeType = kieu[i];
                if (Math.Abs(t.ColumnStyles[i].Width - kichThuoc[i]) > 0.5F) t.ColumnStyles[i].Width = kichThuoc[i];
            }
        }

        /// <summary>Đặt lưới hàng của TableLayoutPanel — luôn gọi SAU khi đã di chuyển con (hàng thừa thì cắt bớt).</summary>
        private static void DatHang(TableLayoutPanel t, float[] kichThuoc, SizeType[] kieu)
        {
            while (t.RowStyles.Count < kichThuoc.Length)
                t.RowStyles.Add(new RowStyle(kieu[t.RowStyles.Count], kichThuoc[t.RowStyles.Count]));
            while (t.RowStyles.Count > kichThuoc.Length) t.RowStyles.RemoveAt(t.RowStyles.Count - 1);
            for (int i = 0; i < kichThuoc.Length; i++)
            {
                if (t.RowStyles[i].SizeType != kieu[i]) t.RowStyles[i].SizeType = kieu[i];
                if (Math.Abs(t.RowStyles[i].Height - kichThuoc[i]) > 0.5F) t.RowStyles[i].Height = kichThuoc[i];
            }
        }

        /// <summary>Đặt 1 con vào ô (cột, hàng) của lưới cha + lề — chỉ ghi khi khác để khỏi xếp lại vô ích.</summary>
        private static void DatOViTri(System.Windows.Forms.Control o, int cot, int hang, Padding le)
        {
            var t = o == null ? null : o.Parent as TableLayoutPanel;
            if (t == null) return;
            var viTri = new TableLayoutPanelCellPosition(cot, hang);
            if (t.GetCellPosition(o) != viTri) t.SetCellPosition(o, viTri);
            if (o.Margin != le) o.Margin = le;
        }

        private System.Windows.Forms.Control DungHeaderNangCao()
        {
            var head = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            head.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpHeaderNangCao = head;

            btnQuayLaiNangCao = Nut("Quay lại", Theme.BtnRole.Secondary, 118, 36);
            btnQuayLaiNangCao.Dock = DockStyle.Left;
            btnQuayLaiNangCao.Image = UiIcons.ArrowLeft(14, Theme.BlueDark);
            btnQuayLaiNangCao.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnQuayLaiNangCao.Click += delegate
            {
                HienThi(dgvActions.Rows.Count > 0 ? TongQuanView.PhatHienDeDoa : TongQuanView.AnToan);
            };

            var text = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            text.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            text.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            text.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            text.Controls.Add(NhanNangCao("Quét nâng cao", Theme.PageTitleFont, Theme.TextDark, ContentAlignment.MiddleLeft), 0, 0);
            text.Controls.Add(NhanNangCao("Chọn chế độ quét phù hợp với nhu cầu của bạn để kiểm tra và phát hiện các mối đe dọa.",
                Theme.PageSubFont, Theme.TextGray, ContentAlignment.MiddleLeft), 0, 1);

            lblCheDoTomTat = NhanNangCao(string.Empty, Theme.SmallFont, Theme.TextMid, ContentAlignment.MiddleRight);
            lblCheDoTomTat.AutoSize = false;
            lblCheDoTomTat.Width = RongNhanTomTat;   // bố cục hẹp sẽ chuyển nhãn này xuống hàng riêng (XepHeaderNangCao)

            head.Controls.Add(btnQuayLaiNangCao, 0, 0);
            head.Controls.Add(text, 1, 0);
            head.Controls.Add(lblCheDoTomTat, 2, 0);
            return head;
        }

        /// <summary>Cột trái: 4 thẻ chọn chế độ (radio dạng card) — chọn thẻ nào thì cột phải đổi ngay.</summary>
        private System.Windows.Forms.Control DungCotCheDo()
        {
            var t = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0), Name = "pnlCheDoTrai" };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowCount = 5;
            for (int i = 0; i < 4; i++) t.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoTheDoc));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlCheDoTrai = t;

            string[] tieuDe = { "Quét toàn bộ hệ thống", "Quét thư mục", "Quét tệp", "Quét tùy chỉnh" };
            string[] moTa = {
                "Kiểm tra tất cả ổ đĩa và tệp",
                "Chọn thư mục để quét",
                "Chọn một hoặc nhiều tệp để quét",
                "Cấu hình vị trí và loại tệp quét"
            };
            theCheDo = new RadioButton[4];
            theCheDoMoTa = new Label[4];
            oTheCheDo = new TableLayoutPanel[4];
            for (int i = 0; i < 4; i++)
            {
                CheDoQuetNangCao mode = (CheDoQuetNangCao)i;
                var o = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };
                o.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                o.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
                o.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                var rdo = new RadioButton
                {
                    Text = tieuDe[i], Dock = DockStyle.Fill, Margin = new Padding(0),
                    Name = "rdoCheDo" + mode
                };
                CheDoQuetNangCao chotMode = mode;
                RadioButton chotThe = rdo;
                // 4 thẻ nằm trong 4 container riêng -> WinForms KHÔNG tự bỏ chọn thẻ cũ (radio chỉ loại
                // trừ trong cùng parent), nên MỌI thay đổi trạng thái đều phải đi qua ChonTheCheDo().
                rdo.CheckedChanged += delegate(object s, EventArgs ev)
                {
                    if (((RadioButton)s).Checked) ChonTheCheDo(chotMode);
                };
                // Bấm vào bất kỳ đâu trên thẻ (dòng tiêu đề hoặc dòng mô tả) cũng chọn thẻ đó.
                EventHandler chonThe = delegate
                {
                    if (chotThe.Enabled && !chotThe.Checked) chotThe.Checked = true;
                };
                // Bàn phím: ↑/↓ (và ←/→) đi qua lại giữa 4 thẻ. Mũi tên là "dialog key" nên phải khai
                // báo IsInputKey ở PreviewKeyDown thì KeyDown mới nhận được.
                int chiSo = i;
                rdo.PreviewKeyDown += delegate(object s, PreviewKeyDownEventArgs ev)
                {
                    if (ev.KeyCode == Keys.Up || ev.KeyCode == Keys.Down
                        || ev.KeyCode == Keys.Left || ev.KeyCode == Keys.Right)
                        ev.IsInputKey = true;
                };
                rdo.KeyDown += delegate(object s, KeyEventArgs ev)
                {
                    int buoc = (ev.KeyCode == Keys.Up || ev.KeyCode == Keys.Left) ? -1
                             : (ev.KeyCode == Keys.Down || ev.KeyCode == Keys.Right) ? 1 : 0;
                    if (buoc == 0 || theCheDo == null) return;
                    int ke = (chiSo + buoc + theCheDo.Length) % theCheDo.Length;
                    if (theCheDo[ke] != null && !theCheDo[ke].Checked) theCheDo[ke].Checked = true;
                    if (theCheDo[ke] != null) theCheDo[ke].Focus();
                    ev.Handled = true;
                    ev.SuppressKeyPress = true;
                };
                Theme.StyleRadioCard(rdo, false);
                var sub = NhanNangCao(moTa[i], Theme.SmallFont, Theme.TextGray, ContentAlignment.TopLeft);
                sub.Padding = new Padding(12, 0, 0, 0);
                sub.Cursor = Cursors.Hand;
                sub.Click += chonThe;
                o.Click += chonThe;
                o.Controls.Add(rdo, 0, 0);
                o.Controls.Add(sub, 0, 1);
                theCheDo[i] = rdo;
                theCheDoMoTa[i] = sub;
                oTheCheDo[i] = o;
                t.Controls.Add(o, 0, i);
            }
            return t;
        }

        /// <summary>Chân trang: hộp "Lưu ý" theo chế độ + nút "Bắt đầu quét" (disable khi chưa hợp lệ).</summary>
        private System.Windows.Forms.Control DungChanTrang()
        {
            var chan = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0), Name = "tlpChanNangCao" };
            chan.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            chan.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            chan.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpChanNangCao = chan;

            pnlLuuY = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
            lblLuuY = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            Theme.StyleInfoBox(pnlLuuY, lblLuuY);
            pnlLuuY.Controls.Add(lblLuuY);

            btnBatDauQuet = Nut("Bắt đầu quét", Theme.BtnRole.Primary, 176, 42);
            btnBatDauQuet.Image = UiIcons.Play(13, Color.White);
            btnBatDauQuet.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnBatDauQuet.Dock = DockStyle.Right;
            btnBatDauQuet.Click += delegate { BatDauQuetNangCao(); };

            chan.Controls.Add(pnlLuuY, 0, 0);
            chan.Controls.Add(btnBatDauQuet, 1, 0);
            return chan;
        }

        // ================= CHẾ ĐỘ 1: QUÉT TOÀN BỘ HỆ THỐNG =================

        private Panel DungFullSystem()
        {
            var p = new Panel { Dock = DockStyle.Fill, Name = "pnlFullSystem" };
            var t = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowCount = 5;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));   // hộp lưu ý
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));   // "Tuỳ chọn quét"
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));  // 7 checkbox (4 hàng x 2 cột)
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));   // "Ổ đĩa sẽ quét"
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // bảng ổ đĩa

            var info = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
            var infoText = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Chế độ quét toàn bộ hệ thống sẽ kiểm tra tất cả ổ đĩa và có thể mất nhiều thời gian. "
                     + "Bạn vẫn có thể sử dụng máy tính trong khi quét."
            };
            Theme.StyleInfoBox(info, infoText);
            info.Controls.Add(infoText);

            var oChk = new System.Windows.Forms.Control[7];
            oChk[0] = OChkTuyChon("Quét sâu (kiểm tra chi tiết từng tệp)", "Mở và soi nội dung từng tệp diện nghi vấn thay vì chỉ kiểm tra metadata.", false);
            oChk[1] = OChkTuyChon("Quét file nén (ZIP, RAR, 7Z…)", "Kiểm tra cả tệp nén và liệt kê tệp bên trong nếu đọc được.", true);
            oChk[2] = OChkTuyChon("Kiểm tra bộ nhớ đang hoạt động", "Đối chiếu tiến trình đang chạy với bảng chữ ký (cần quyền đọc bộ nhớ).", false);
            oChk[3] = OChkTuyChon("Tìm kiếm chương trình không mong muốn (PUA)", "Bật heuristic nhạy hơn cho công cụ adware/riskware.", false);
            oChk[4] = OChkTuyChon("Quét khu vực hệ thống (Windows)", "Ưu tiên thư mục Windows, System32 và thư mục khởi động.", true);
            oChk[5] = OChkTuyChon("Sử dụng phát hiện dựa trên hành vi", "Chấm điểm đặc điểm hành vi (script độc, tệp ẩn, mồi câu xã hội).", true);
            oChk[6] = OChkTuyChon("Tự động cách ly khi phát hiện mối đe dọa", "Tệp bị phát hiện sẽ được dời vào khu cách ly ngay, không chờ bạn bấm.", false);
            chkFull = new CheckBox[7];
            for (int i = 0; i < 7; i++) chkFull[i] = LayCheckBox(oChk[i]);

            dgvODia = LuoiMoi(0);
            dgvODia.Name = "dgvODia";
            dgvODia.ReadOnly = false;
            colODiaChon = new DataGridViewCheckBoxColumn
            {
                HeaderText = "", Name = "colODiaChon", AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 48, Resizable = DataGridViewTriState.False, SortMode = DataGridViewColumnSortMode.NotSortable
            };
            dgvODia.Columns.Add(colODiaChon);
            dgvODia.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ổ đĩa", Name = "colODiaTen", FillWeight = 18F });
            dgvODia.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Loại", Name = "colODiaLoai", FillWeight = 26F });
            dgvODia.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tổng dung lượng", Name = "colODiaTong", FillWeight = 28F });
            dgvODia.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dung lượng trống", Name = "colODiaTrong", FillWeight = 28F });
            foreach (DataGridViewColumn c in dgvODia.Columns) if (c.Index > 0) c.ReadOnly = true;
            dgvODia.CurrentCellDirtyStateChanged += delegate
            {
                if (dgvODia.IsCurrentCellDirty) dgvODia.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvODia.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == colODiaChon.Index) CapNhatTrangThaiNutBatDauQuet();
            };

            t.Controls.Add(info, 0, 0);
            t.Controls.Add(NhanNangCao("Tuỳ chọn quét", Theme.BoldFont, Theme.TextDark, ContentAlignment.MiddleLeft), 0, 1);
            t.Controls.Add(LuoiTuyChon(oChk, 2), 0, 2);
            t.Controls.Add(NhanNangCao("Ổ đĩa sẽ quét (bỏ tick ổ bạn không muốn kiểm tra)", Theme.BoldFont, Theme.TextDark, ContentAlignment.MiddleLeft), 0, 3);
            t.Controls.Add(dgvODia, 0, 4);
            p.Controls.Add(t);
            return p;
        }

        /// <summary>Nạp danh sách ổ đĩa CỐ ĐỊNH thật của máy (DriveInfo), mặc định tick hết.</summary>
        private void NapDanhSachODia()
        {
            if (dgvODia == null) return;
            dgvODia.Rows.Clear();
            string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            try
            {
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    if (d.DriveType != DriveType.Fixed) continue;
                    string loai = string.Equals(d.Name, systemDrive, StringComparison.OrdinalIgnoreCase)
                        ? "Hệ thống (ổ cài đặt Windows)"
                        : "Ổ đĩa dữ liệu";
                    string tong = "—", trong = "—";
                    try
                    {
                        if (d.IsReady)
                        {
                            tong = DirectorySizeCalculator.Format(d.TotalSize);
                            trong = DirectorySizeCalculator.Format(d.TotalFreeSpace);
                        }
                    }
                    catch (IOException) { }
                    dgvODia.Rows.Add(true, d.Name, loai, tong, trong);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            if (dgvODia.Rows.Count == 0)
                dgvODia.Rows.Add(false, "(không đọc được ổ đĩa)", "—", "—", "—");
            CapNhatTrangThaiNutBatDauQuet();
        }

        // ================= CHẾ ĐỘ 2: QUÉT THƯ MỤC =================

        private Panel DungFolder()
        {
            var p = new Panel { Dock = DockStyle.Fill, Name = "pnlFolder" };
            var t = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowCount = 5;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));

            var dong1 = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0) };
            dong1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dong1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            dong1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            txtThuMuc = new TextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BackColor = Theme.ChipGray, ForeColor = Theme.TextMid,
                Font = Theme.BodyFont, Margin = new Padding(0, 4, 8, 4),
                Text = "(chọn thư mục cần quét — có thể thêm nhiều thư mục)"
            };
            btnChonThuMuc = Nut("Chọn thư mục", Theme.BtnRole.Secondary, 148, 34);
            btnChonThuMuc.Click += delegate { ChonThemThuMuc(); };
            dong1.Controls.Add(txtThuMuc, 0, 0);
            dong1.Controls.Add(btnChonThuMuc, 1, 0);

            var dong2 = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0) };
            dong2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dong2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            dong2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            dong2.Controls.Add(NhanNangCao("Danh sách thư mục đã chọn (0)", Theme.BoldFont, Theme.TextDark, ContentAlignment.MiddleLeft), 0, 0);
            btnXoaThuMuc = Nut("Xóa tất cả", Theme.BtnRole.Danger, 118, 26);
            btnXoaThuMuc.Click += delegate { XoaTatCaThuMuc(); };
            dong2.Controls.Add(btnXoaThuMuc, 1, 0);

            dgvThuMuc = LuoiMoi(0);
            dgvThuMuc.Name = "dgvThuMuc";
            dgvThuMuc.ReadOnly = false;
            dgvThuMuc.AllowDrop = true;
            dgvThuMuc.DragEnter += LuoiHoacPanel_DragEnter;
            dgvThuMuc.DragDrop += DgvThuMuc_DragDrop;
            dgvThuMuc.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "STT", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50
            });
            dgvThuMuc.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đường dẫn thư mục", FillWeight = 60F });
            dgvThuMuc.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Kích thước", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 130
            });
            dgvThuMuc.Columns.Add(new DataGridViewButtonColumn
            {
                HeaderText = "Thao tác", Text = "Xóa", UseColumnTextForButtonValue = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 86,
                Resizable = DataGridViewTriState.False, SortMode = DataGridViewColumnSortMode.NotSortable
            });
            foreach (DataGridViewColumn c in dgvThuMuc.Columns) if (c.Index != 3) c.ReadOnly = true;
            dgvThuMuc.CellContentClick += DgvThuMuc_CellContentClick;

            var oChk = new System.Windows.Forms.Control[6];
            oChk[0] = OChkTuyChon("Quét sâu", "Kiểm tra chi tiết nội dung từng tệp diện nghi vấn.", false);
            oChk[1] = OChkTuyChon("Quét trí nhớ tạm", "Kiểm tra thêm thư mục temp/cache của người dùng.", true);
            oChk[2] = OChkTuyChon("Quét file nén (ZIP, RAR, 7Z)", "Soi cả tệp nén nếu đọc được nội dung.", true);
            oChk[3] = OChkTuyChon("Tìm PUA", "Bật heuristic nhạy hơn cho phần mềm không mong muốn.", false);
            oChk[4] = OChkTuyChon("Sử dụng phát hiện dựa trên hành vi", "Chấm điểm đặc điểm hành vi nghi vấn.", true);
            oChk[5] = OChkTuyChon("Tự động cách ly khi phát hiện mối đe dọa", "Dời tệp bị phát hiện vào khu cách ly ngay.", false);
            chkFolder = new CheckBox[6];
            for (int i = 0; i < 6; i++) chkFolder[i] = LayCheckBox(oChk[i]);

            t.Controls.Add(dong1, 0, 0);
            t.Controls.Add(dong2, 0, 1);
            t.Controls.Add(dgvThuMuc, 0, 2);
            t.Controls.Add(NhanNangCao("Tuỳ chọn quét", Theme.BoldFont, Theme.TextDark, ContentAlignment.MiddleLeft), 0, 3);
            t.Controls.Add(LuoiTuyChon(oChk, 2), 0, 4);
            p.Controls.Add(t);
            return p;
        }

        // ================= CHẾ ĐỘ 3: QUÉT TỆP (có kéo–thả) =================

        private Panel DungFiles()
        {
            var p = new Panel { Dock = DockStyle.Fill, Name = "pnlFiles" };
            var t = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowCount = 5;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));   // vùng kéo–thả
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));   // nút chọn tệp
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));   // tiêu đề danh sách
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // bảng tệp
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));   // 3 tuỳ chọn

            pnlKeoTha = new Panel { Dock = DockStyle.Fill, AllowDrop = true, Margin = new Padding(0, 0, 0, 6), Name = "pnlKeoTha" };
            pnlKeoTha.BackColor = Theme.BlueTint;
            pnlKeoTha.AllowDrop = true;
            pnlKeoTha.DragEnter += LuoiHoacPanel_DragEnter;
            pnlKeoTha.DragLeave += delegate { pnlKeoTha.BackColor = Theme.BlueTint; };
            pnlKeoTha.DragDrop += PnlKeoTha_DragDrop;
            pnlKeoTha.Paint += delegate(object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Theme.BlueSoft))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlKeoTha.Width - 1, pnlKeoTha.Height - 1);
            };
            var lblKeoThaLocal = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextMid,
                Font = Theme.BodyFont,
                Text = "Kéo và thả tệp vào đây\nHoặc chọn tệp bằng nút bên dưới"
            };
            pnlKeoTha.Controls.Add(lblKeoThaLocal);

            var dong2 = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Fill, Margin = new Padding(0) };
            dong2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            dong2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dong2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            dong2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            btnChonTep = Nut("Chọn tệp", Theme.BtnRole.Secondary, 148, 34);
            btnChonTep.Click += delegate { ChonThemTep(); };
            dong2.Controls.Add(btnChonTep, 0, 0);
            dong2.Controls.Add(NhanNangCao("Danh sách tệp đã chọn (0)", Theme.BoldFont, Theme.TextDark, ContentAlignment.MiddleLeft), 1, 0);
            btnXoaTep = Nut("Xóa tất cả", Theme.BtnRole.Danger, 118, 26);
            btnXoaTep.Click += delegate { XoaTatCaTep(); };
            dong2.Controls.Add(btnXoaTep, 2, 0);

            dgvTep = LuoiMoi(0);
            dgvTep.Name = "dgvTep";
            dgvTep.ReadOnly = false;
            dgvTep.AllowDrop = true;
            dgvTep.DragEnter += LuoiHoacPanel_DragEnter;
            dgvTep.DragDrop += PnlKeoTha_DragDrop;
            dgvTep.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "STT", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50
            });
            dgvTep.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên tệp", FillWeight = 40F });
            dgvTep.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đường dẫn (rút gọn)", FillWeight = 45F });
            dgvTep.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Kích thước", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 110
            });
            dgvTep.Columns.Add(new DataGridViewButtonColumn
            {
                HeaderText = "Thao tác", Text = "Xóa", UseColumnTextForButtonValue = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 86,
                Resizable = DataGridViewTriState.False, SortMode = DataGridViewColumnSortMode.NotSortable
            });
            foreach (DataGridViewColumn c in dgvTep.Columns) if (c.Index != 4) c.ReadOnly = true;
            dgvTep.CellContentClick += DgvTep_CellContentClick;

            var oChk = new System.Windows.Forms.Control[3];
            oChk[0] = OChkTuyChon("Sử dụng phát hiện dựa trên hành vi", "Chấm điểm đặc điểm hành vi nghi vấn của tệp.", true);
            oChk[1] = OChkTuyChon("Kiểm tra tệp nén (ZIP, RAR, 7Z…)", "Soi cả tệp nén nếu đọc được nội dung.", true);
            oChk[2] = OChkTuyChon("Tự động cách ly khi phát hiện mối đe dọa", "Dời tệp bị phát hiện vào khu cách ly ngay.", false);
            chkFiles = new CheckBox[3];
            for (int i = 0; i < 3; i++) chkFiles[i] = LayCheckBox(oChk[i]);

            t.Controls.Add(pnlKeoTha, 0, 0);
            t.Controls.Add(dong2, 0, 1);
            t.Controls.Add(dgvTep, 0, 3);
            t.Controls.Add(LuoiTuyChon(oChk, 2), 0, 4);
            p.Controls.Add(t);
            return p;
        }

        // ================= CHẾ ĐỘ 4: QUÉT TÙY CHỈNH =================

        private Panel DungCustom()
        {
            var p = new Panel { Dock = DockStyle.Fill, Name = "pnlCustom" };
            var t = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowCount = 7;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));   // nút vị trí
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // bảng vị trí
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));   // "Loại tệp quét"
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));   // 5 radio loại tệp
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));   // ô phần mở rộng
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));   // 5 tuỳ chọn
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));   // bỏ qua tệp lớn

            var dong1 = new TableLayoutPanel { ColumnCount = 4, Dock = DockStyle.Fill, Margin = new Padding(0) };
            dong1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            dong1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            dong1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dong1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            dong1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            btnThemViTri = Nut("+ Thêm vị trí", Theme.BtnRole.Secondary, 138, 32);
            btnThemViTri.Click += BtnThemViTri_Click;
            btnXoaDongViTri = Nut("Xóa dòng đang chọn", Theme.BtnRole.Neutral, 176, 32);
            btnXoaDongViTri.Click += delegate { XoaDongViTriDangChon(); };
            btnXoaTatCaViTri = Nut("Xóa tất cả", Theme.BtnRole.Danger, 118, 26);
            btnXoaTatCaViTri.Click += delegate { XoaTatCaViTri(); };
            dong1.Controls.Add(btnThemViTri, 0, 0);
            dong1.Controls.Add(btnXoaDongViTri, 1, 0);
            dong1.Controls.Add(NhanNangCao("Vị trí quét (0)", Theme.BoldFont, Theme.TextDark, ContentAlignment.MiddleLeft), 2, 0);
            dong1.Controls.Add(btnXoaTatCaViTri, 3, 0);

            dgvViTri = LuoiMoi(0);
            dgvViTri.Name = "dgvViTri";
            dgvViTri.ReadOnly = false;
            dgvViTri.AllowDrop = true;
            dgvViTri.DragEnter += LuoiHoacPanel_DragEnter;
            dgvViTri.DragDrop += DgvViTri_DragDrop;
            DungCotBangViTri();

            t.Controls.Add(dong1, 0, 0);
            t.Controls.Add(dgvViTri, 0, 1);
            t.Controls.Add(NhanNangCao("Loại tệp quét", Theme.BoldFont, Theme.TextDark, ContentAlignment.MiddleLeft), 0, 2);
            t.Controls.Add(DungRadioLoaiTep(), 0, 3);
            t.Controls.Add(DungOTuyChinhPhanMoRong(), 0, 4);
            t.Controls.Add(DungTuyChonCustom(), 0, 5);
            t.Controls.Add(DungDongBoQuaTepLon(), 0, 6);
            p.Controls.Add(t);
            return p;
        }

        /// <summary>Cột của bảng "Vị trí quét" của chế độ tùy chỉnh.</summary>
        private void DungCotBangViTri()
        {
            dgvViTri.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "STT", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50
            });
            dgvViTri.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đường dẫn", FillWeight = 70F });
            dgvViTri.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Loại", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 90
            });
            dgvViTri.Columns.Add(new DataGridViewButtonColumn
            {
                HeaderText = "Thao tác", Text = "Xóa", UseColumnTextForButtonValue = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 86,
                Resizable = DataGridViewTriState.False, SortMode = DataGridViewColumnSortMode.NotSortable
            });
            foreach (DataGridViewColumn c in dgvViTri.Columns) if (c.Index != 3) c.ReadOnly = true;
            dgvViTri.CellContentClick += DgvViTri_CellContentClick;
        }

        /// <summary>5 lựa chọn "Loại tệp quét" — chỉ "Tùy chỉnh phần mở rộng" mới bật ô nhập.</summary>
        private System.Windows.Forms.Control DungRadioLoaiTep()
        {
            string[] loaiTep = { "Tất cả tệp", "Chỉ tệp thực thi (.exe, .dll, .sys…)", "Tệp nén (.zip, .rar, .7z…)",
                "Tệp tài liệu (.doc, .docx, .pdf…)", "Tùy chỉnh phần mở rộng tệp" };
            var luoi = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0) };
            luoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            luoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            luoi.RowCount = 3;
            for (int i = 0; i < 3; i++) luoi.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
            rdoLoai = new RadioButton[5];
            for (int i = 0; i < 5; i++)
            {
                var rdo = new RadioButton
                {
                    Text = loaiTep[i], AutoSize = true, Margin = new Padding(0),
                    Cursor = Cursors.Hand, ForeColor = Theme.TextMid, Font = Theme.BodyFont,
                    Checked = i == 0, Name = "rdoLoaiTep" + i
                };
                rdo.CheckedChanged += delegate { CapNhatODienPhanMoRong(); };
                rdoLoai[i] = rdo;
                luoi.Controls.Add(rdo, i % 2, i / 2);
            }
            return luoi;
        }

        private System.Windows.Forms.Control DungOTuyChinhPhanMoRong()
        {
            txtPhanMoRong = new TextBox
            {
                Dock = DockStyle.Fill, Enabled = false, Font = Theme.BodyFont,
                Margin = new Padding(0, 3, 0, 3), Text = "exe,dll,sys",
                BackColor = Theme.ChipGray
            };
            if (tips == null) tips = new ToolTip();
            tips.SetToolTip(txtPhanMoRong, "Nhập các phần mở rộng cách nhau bằng dấu phẩy, ví dụ: exe, dll, sys");
            txtPhanMoRong.TextChanged += delegate { CapNhatTrangThaiNutBatDauQuet(); };
            return txtPhanMoRong;
        }

        private System.Windows.Forms.Control DungTuyChonCustom()
        {
            var oChk = new System.Windows.Forms.Control[5];
            oChk[0] = OChkTuyChon("Quét sâu", "Kiểm tra chi tiết nội dung từng tệp diện nghi vấn.", false);
            oChk[1] = OChkTuyChon("Sử dụng phát hiện dựa trên hành vi", "Chấm điểm đặc điểm hành vi nghi vấn.", true);
            oChk[2] = OChkTuyChon("Kiểm tra tệp nén", "Soi cả tệp nén nếu đọc được nội dung.", true);
            oChk[3] = OChkTuyChon("Phát hiện phần mềm không mong muốn (PUA)", "Bật heuristic nhạy hơn cho adware/riskware.", false);
            oChk[4] = OChkTuyChon("Tự động cách ly khi phát hiện mối đe dọa", "Dời tệp bị phát hiện vào khu cách ly ngay.", false);
            chkCustom = new CheckBox[5];
            for (int i = 0; i < 5; i++) chkCustom[i] = LayCheckBox(oChk[i]);
            return LuoiTuyChon(oChk, 2);
        }

        private System.Windows.Forms.Control DungDongBoQuaTepLon()
        {
            var dong = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Fill, Margin = new Padding(0) };
            dong.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            dong.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            dong.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            dong.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            chkBoQuaTepLon = new CheckBox
            {
                Text = "Bỏ qua các tệp lớn hơn", AutoSize = true, Dock = DockStyle.Left,
                Margin = new Padding(0, 6, 10, 0), TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                ForeColor = Theme.TextMid, Font = Theme.BodyFont, Cursor = Cursors.Hand
            };
            chkBoQuaTepLon.CheckedChanged += delegate { CapNhatTrangThaiNutBatDauQuet(); };
            numBoQuaTepLon = new NumericUpDown
            {
                Minimum = 1, Maximum = 100000, Value = 100, Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 6, 3), Font = Theme.BodyFont
            };
            numBoQuaTepLon.ValueChanged += delegate { CapNhatTrangThaiNutBatDauQuet(); };
            cboDonVi = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 6, 3), Font = Theme.BodyFont
            };
            cboDonVi.Items.AddRange(new object[] { "KB", "MB", "GB" });
            cboDonVi.SelectedIndex = 1;
            dong.Controls.Add(chkBoQuaTepLon, 0, 0);
            dong.Controls.Add(numBoQuaTepLon, 1, 0);
            dong.Controls.Add(cboDonVi, 2, 0);
            return dong;
        }

        // ================= ĐIỀU PHỐI TRANG (d): CHỌN CHẾ ĐỘ, LƯU Ý, VALIDATE =================

        /// <summary>Mở trang (d) với chế độ chỉ định (từ dropdown "Quét nâng cao ▾" ở header trang).</summary>
        internal void MoQuetNangCao(CheDoQuetNangCao mode)
        {
            HienThi(TongQuanView.QuetNangCao, false);
            ChonTheCheDo(mode);
        }

        /// <summary>HienThi(QuetNangCao) gọi vào đây: bảo đảm dữ liệu nền (ổ đĩa) đã có sẵn.</summary>
        private void OnMoTrangQuetNangCao()
        {
            if (dgvODia != null && dgvODia.Rows.Count == 0) NapDanhSachODia();
            if (theCheDo != null && !theCheDo[(int)cheDo].Checked) ChonTheCheDo(cheDo);
            CapNhatLuuY();
            CapNhatTrangThaiNutBatDauQuet();
            // RESPONSIVE (lần 7): áp bố cục đúng với bề rộng hiện tại của cửa sổ ngay khi mở trang
            XepBoCucNangCao();
        }

        /// <summary>Khoá các lựa chọn phạm vi khi phiên quét đang chạy (SPEC §8: chặn double click).</summary>
        private void OnSetScanningAdvanced(bool scanning)
        {
            if (btnBatDauQuet != null) btnBatDauQuet.Enabled = !scanning;
            if (theCheDo == null) return;
            for (int i = 0; i < theCheDo.Length; i++)
            {
                if (theCheDo[i] != null) theCheDo[i].Enabled = !scanning;
                if (theCheDoMoTa != null && theCheDoMoTa[i] != null) theCheDoMoTa[i].Enabled = !scanning;
            }
        }

        /// <summary>
        /// Chọn chế độ quét: bảo đảm ĐÚNG 1 trong 4 thẻ được chọn rồi mới đổi nội dung cột phải.
        /// 4 thẻ nằm trong 4 container riêng nên WinForms không tự bỏ chọn thẻ cũ (radio chỉ loại trừ
        /// trong cùng parent) — nếu không tự đồng bộ thì sau khi bấm sang thẻ khác, bấm LẠI thẻ đầu
        /// sẽ không có CheckedChanged và cột phải "kẹt" ở chế độ vừa chọn (không qua lại được).
        /// </summary>
        private void ChonTheCheDo(CheDoQuetNangCao mode)
        {
            cheDo = mode;
            if (dangChonTheCheDo) return;               // thẻ vừa bật trong lúc đồng bộ -> không chạy lại
            dangChonTheCheDo = true;
            try
            {
                if (theCheDo != null)
                    for (int i = 0; i < theCheDo.Length; i++)
                        if (theCheDo[i] != null && theCheDo[i].Checked != (i == (int)mode))
                            theCheDo[i].Checked = i == (int)mode;   // bật thẻ đang chọn, bỏ 3 thẻ còn lại
            }
            finally { dangChonTheCheDo = false; }
            HienThiNoiDungCheDo(mode);
        }

        /// <summary>Đổi nội dung cột phải NGAY trong trang (không đổi trang, giữ dữ liệu đã nhập).</summary>
        private void HienThiNoiDungCheDo(CheDoQuetNangCao mode)
        {
            cheDo = mode;
            if (pnlFullSystem == null) return;
            pnlFullSystem.Visible = mode == CheDoQuetNangCao.FullSystem;
            pnlFolder.Visible = mode == CheDoQuetNangCao.Folder;
            pnlFiles.Visible = mode == CheDoQuetNangCao.Files;
            pnlCustom.Visible = mode == CheDoQuetNangCao.Custom;
            for (int i = 0; i < 4; i++)
            {
                bool chon = i == (int)mode;
                if (theCheDo != null && theCheDo[i] != null) Theme.StyleRadioCard(theCheDo[i], chon);
                // Tô cả dòng mô tả theo trạng thái chọn -> nhìn là biết ngay đang ở chế độ nào.
                if (theCheDoMoTa == null || theCheDoMoTa[i] == null) continue;
                theCheDoMoTa[i].ForeColor = chon ? Theme.BlueDark : Theme.TextGray;
                theCheDoMoTa[i].BackColor = chon ? Theme.BlueTint : Theme.PageBg;
            }
            CapNhatLuuY();
            CapNhatTrangThaiNutBatDauQuet();
        }

        /// <summary>Hộp "Lưu ý" đổi câu chữ theo chế độ (README mục d).</summary>
        private void CapNhatLuuY()
        {
            if (lblLuuY == null) return;
            switch (cheDo)
            {
                case CheDoQuetNangCao.FullSystem:
                    lblLuuY.Text = "Lưu ý: thời gian quét phụ thuộc vào dung lượng và số lượng tệp.\n"
                        + "Bạn có thể tiếp tục sử dụng máy tính trong khi quét.";
                    break;
                case CheDoQuetNangCao.Folder:
                    lblLuuY.Text = "Lưu ý: thời gian quét phụ thuộc vào dung lượng và số lượng tệp trong thư mục.\n"
                        + "Bạn có thể tiếp tục sử dụng máy tính trong khi quét.";
                    break;
                case CheDoQuetNangCao.Files:
                    lblLuuY.Text = "Lưu ý: bạn có thể chọn nhiều tệp cùng lúc để quét (hoặc kéo–thả vào khung).\n"
                        + "Hỗ trợ các định dạng phổ biến: exe, dll, doc, pdf, zip, rar…";
                    break;
                default:
                    lblLuuY.Text = "Lưu ý: bạn có thể thêm nhiều thư mục và tệp cùng lúc để quét.\n"
                        + "Thời gian quét phụ thuộc vào dung lượng dữ liệu và tùy chọn bạn chọn.";
                    break;
            }
            if (lblCheDoTomTat != null)
                lblCheDoTomTat.Text = string.Format("Đang chọn: {0}  ·  {1} vị trí",
                    theCheDo != null && theCheDo[(int)cheDo] != null ? theCheDo[(int)cheDo].Text : "—",
                    DemSoViTri());
        }

        private int DemSoViTri()
        {
            switch (cheDo)
            {
                case CheDoQuetNangCao.Folder: return dsThuMuc.Count;
                case CheDoQuetNangCao.Files: return dsTep.Count;
                case CheDoQuetNangCao.Custom: return dsViTri.Count;
                default: return DemODiaDuocChon();
            }
        }

        /// <summary>Validate theo SPEC §5.6 — không hợp lệ thì DISABLE nút (không dùng MessageBox chặn).</summary>
        private void CapNhatTrangThaiNutBatDauQuet()
        {
            if (btnBatDauQuet == null) return;
            bool hopLe;
            switch (cheDo)
            {
                case CheDoQuetNangCao.FullSystem:
                    hopLe = DemODiaDuocChon() > 0;
                    break;
                case CheDoQuetNangCao.Folder:
                    hopLe = dsThuMuc.Count > 0;
                    break;
                case CheDoQuetNangCao.Files:
                    hopLe = dsTep.Count > 0;
                    break;
                default:
                    string loi;
                    bool tuyChinh = rdoLoai != null && rdoLoai[4] != null && rdoLoai[4].Checked;
                    hopLe = dsViTri.Count > 0 && (!tuyChinh || ParsePhanMoRong(out loi).Count > 0);
                    break;
            }
            btnBatDauQuet.Enabled = hopLe && !isScanning;
        }

        private int DemODiaDuocChon()
        {
            if (dgvODia == null) return 0;
            int n = 0;
            foreach (DataGridViewRow r in dgvODia.Rows)
                if (r.Cells[colODiaChon.Index].Value is bool b && b) n++;
            return n;
        }

        /// <summary>Ô "Tùy chỉnh phần mở rộng" chỉ bật khi chọn radio tương ứng (SPEC §5.5).</summary>
        private void CapNhatODienPhanMoRong()
        {
            if (txtPhanMoRong == null || rdoLoai == null || rdoLoai[4] == null) return;
            bool tuyChinh = rdoLoai[4].Checked;
            txtPhanMoRong.Enabled = tuyChinh;
            txtPhanMoRong.BackColor = tuyChinh ? Theme.PageBg : Theme.ChipGray;
            if (tuyChinh) txtPhanMoRong.Focus();
            CapNhatTrangThaiNutBatDauQuet();
        }

        /// <summary>Chuẩn hoá danh sách phần mở rộng: tách phẩy/khoảng trắng, bỏ dấu chấm, lowercase, loại trùng.</summary>
        private HashSet<string> ParsePhanMoRong(out string loi)
        {
            loi = null;
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (txtPhanMoRong == null) return set;
            string[] phan = txtPhanMoRong.Text.Split(
                new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string p in phan)
            {
                string s = p.Trim().TrimStart('.').ToLowerInvariant();
                if (s.Length == 0) continue;
                set.Add("." + s);
            }
            if (set.Count == 0) loi = "Nhập ít nhất 1 phần mở rộng, cách nhau bằng dấu phẩy.";
            return set;
        }

        // ================= CHẾ ĐỘ THƯ MỤC: THÊM / XÓA / TÍNH KÍCH THƯỚC =================

        private void ChonThemThuMuc()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Chọn thư mục cần quét (có thể thêm nhiều thư mục)";
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                txtThuMuc.Text = dlg.SelectedPath;
                if (!ThemThuMuc(dlg.SelectedPath))
                    txtThuMuc.Text = dsThuMuc.Count > 0 ? dsThuMuc[dsThuMuc.Count - 1] : "(chưa có thư mục nào)";
            }
        }

        /// <summary>
        /// Thêm thư mục theo luật dedupe của SPEC §5.3: trùng thì bỏ qua, là con của thư mục đã có thì bỏ qua,
        /// là cha của các thư mục đã có thì gộp lại chỉ giữ thư mục cha (tránh quét trùng 2 lần).
        /// </summary>
        private bool ThemThuMuc(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return false;
            string full = path.TrimEnd(Path.DirectorySeparatorChar);
            foreach (string co in dsThuMuc)
            {
                string coF = co.TrimEnd(Path.DirectorySeparatorChar);
                if (string.Equals(coF, full, StringComparison.OrdinalIgnoreCase)) return false;
                if (full.StartsWith(coF + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(string.Format("Thư mục này đã nằm trong {0} rồi.", coF),
                        "Quét thư mục", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
            }
            for (int i = dsThuMuc.Count - 1; i >= 0; i--)
            {
                string coF = dsThuMuc[i].TrimEnd(Path.DirectorySeparatorChar);
                if (coF.StartsWith(full + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    dsThuMuc.RemoveAt(i);   // thư mục con của thư mục cha mới -> bỏ để không quét trùng
            }
            dsThuMuc.Add(path);
            LamMoiBangThuMuc();
            return true;
        }

        private void LamMoiBangThuMuc()
        {
            dgvThuMuc.Rows.Clear();
            for (int i = 0; i < dsThuMuc.Count; i++)
            {
                dgvThuMuc.Rows.Add((i + 1).ToString(), dsThuMuc[i], "Đang tính…", "Xóa");
                TinhKichThuocThuMuc(dsThuMuc[i]);
            }
            CapNhatLuuY();
            CapNhatTrangThaiNutBatDauQuet();
        }

        /// <summary>Tính kích thước thư mục ở luồng nền (hủy được khi dòng bị xóa trước lúc tính xong).</summary>
        private void TinhKichThuocThuMuc(string path)
        {
            CancellationTokenSource old;
            if (sizeJobs.TryGetValue(path, out old)) old.Cancel();
            var ctsSize = new CancellationTokenSource();
            sizeJobs[path] = ctsSize;
            CancellationToken token = ctsSize.Token;
            Task.Run(() =>
            {
                long bytes = 0;
                try { bytes = DirectorySizeCalculator.GetSize(path, token); }
                catch (OperationCanceledException) { return; }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                try
                {
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        int viTri = dsThuMuc.FindIndex(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
                        if (viTri < 0 || viTri >= dgvThuMuc.Rows.Count) return;
                        dgvThuMuc.Rows[viTri].Cells[2].Value = DirectorySizeCalculator.Format(bytes);
                    }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            });
        }

        private void XoaTatCaThuMuc()
        {
            if (dsThuMuc.Count == 0) return;
            if (dsThuMuc.Count >= 3 &&
                MessageBox.Show(string.Format("Xóa toàn bộ {0} thư mục khỏi danh sách?", dsThuMuc.Count),
                    "Xóa tất cả", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            foreach (CancellationTokenSource c in sizeJobs.Values) c.Cancel();
            sizeJobs.Clear();
            dsThuMuc.Clear();
            LamMoiBangThuMuc();
        }

        private void DgvThuMuc_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 3) return;
            if (e.RowIndex >= dsThuMuc.Count) return;
            string path = dsThuMuc[e.RowIndex];
            CancellationTokenSource c;
            if (sizeJobs.TryGetValue(path, out c)) { c.Cancel(); sizeJobs.Remove(path); }
            dsThuMuc.RemoveAt(e.RowIndex);
            LamMoiBangThuMuc();
        }

        private void LuoiHoacPanel_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            if (pnlKeoTha != null && sender == pnlKeoTha) pnlKeoTha.BackColor = Theme.BlueSoft;
        }

        private static string[] LayDuongDanKeoTha(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return null;
            return (string[])e.Data.GetData(DataFormats.FileDrop);
        }

        private void DgvThuMuc_DragDrop(object sender, DragEventArgs e)
        {
            string[] paths = LayDuongDanKeoTha(e);
            if (paths == null) return;
            int them = 0;
            foreach (string p in paths)
            {
                if (Directory.Exists(p)) { if (ThemThuMuc(p)) them++; }
                else if (File.Exists(p))
                    MessageBox.Show("Chế độ Quét thư mục chỉ nhận thư mục. Dùng chế độ Quét tệp nếu muốn thêm tệp.",
                        "Quét thư mục", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (them > 0) txtThuMuc.Text = dsThuMuc[dsThuMuc.Count - 1];
        }

        // ================= CHẾ ĐỘ TỆP: THÊM / XÓA / KÉO–THẢ =================

        private void ChonThemTep()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Chọn tệp cần quét";
                dlg.Filter = "Tất cả tệp (*.*)|*.*";
                dlg.Multiselect = true;
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                ThemTep(dlg.FileNames);
            }
        }

        /// <summary>Thêm nhiều tệp, dedupe theo đường dẫn đầy đủ (giữ nguyên toàn bộ đường dẫn trên lưới).</summary>
        private int ThemTep(string[] paths)
        {
            if (paths == null) return 0;
            int them = 0;
            foreach (string p in paths)
            {
                if (string.IsNullOrEmpty(p) || !File.Exists(p)) continue;
                string full = Path.GetFullPath(p);
                bool daCo = dsTep.Any(x => string.Equals(x, full, StringComparison.OrdinalIgnoreCase));
                if (daCo) continue;
                dsTep.Add(full);
                them++;
            }
            if (them > 0) LamMoiBangTep();
            return them;
        }

        private void LamMoiBangTep()
        {
            dgvTep.Rows.Clear();
            for (int i = 0; i < dsTep.Count; i++)
            {
                string path = dsTep[i];
                string kichThuoc = "—";
                try
                {
                    if (File.Exists(path)) kichThuoc = DirectorySizeCalculator.Format(new FileInfo(path).Length);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                dgvTep.Rows.Add((i + 1).ToString(), Path.GetFileName(path), RutGonDuongDan(path), kichThuoc, "Xóa");
            }
            CapNhatLuuY();
            CapNhatTrangThaiNutBatDauQuet();
        }

        /// <summary>Rút gọn đường dẫn ở GIỮA để vẫn thấy phần phân biệt (SPEC §3.2).</summary>
        private static string RutGonDuongDan(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= 58) return path;
            string file = Path.GetFileName(path);
            string dir = Path.GetDirectoryName(path) ?? string.Empty;
            int conLai = 58 - file.Length - 6;
            if (conLai < 8) return "…" + path.Substring(path.Length - 55);
            return dir.Substring(0, Math.Min(conLai, dir.Length)) + "…" + Path.DirectorySeparatorChar + file;
        }

        private void XoaTatCaTep()
        {
            if (dsTep.Count == 0) return;
            if (dsTep.Count >= 3 &&
                MessageBox.Show(string.Format("Xóa toàn bộ {0} tệp khỏi danh sách quét?", dsTep.Count),
                    "Xóa tất cả", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            dsTep.Clear();
            LamMoiBangTep();
        }

        private void DgvTep_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 4) return;
            if (e.RowIndex >= dsTep.Count) return;
            dsTep.RemoveAt(e.RowIndex);
            LamMoiBangTep();
        }

        /// <summary>Kéo–thả vào vùng "Kéo và thả tệp vào đây" hoặc vào bảng danh sách tệp.</summary>
        private void PnlKeoTha_DragDrop(object sender, DragEventArgs e)
        {
            if (pnlKeoTha != null) pnlKeoTha.BackColor = Theme.BlueTint;
            string[] paths = LayDuongDanKeoTha(e);
            if (paths == null) return;
            var tep = new List<string>();
            foreach (string p in paths)
            {
                if (File.Exists(p)) { tep.Add(p); continue; }
                if (!Directory.Exists(p)) continue;   // tệp đã bị xóa ngoài Explorer: bỏ qua im lặng
                MessageBox.Show("Chế độ Quét tệp chỉ nhận tệp (không nhận thư mục). "
                    + "Dùng chế độ Quét thư mục nếu bạn muốn quét cả thư mục.",
                    "Quét tệp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            int them = ThemTep(tep.ToArray());
            if (them == 0 && tep.Count > 0)
                MessageBox.Show("Các tệp này đã có trong danh sách (không thêm trùng).",
                    "Quét tệp", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ================= CHẾ ĐỘ TÙY CHỈNH: VỊ TRÍ + LOẠI TỆP =================

        private void BtnThemViTri_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            var mThuMuc = new ToolStripMenuItem("Thêm thư mục…");
            mThuMuc.Click += delegate { ChonThemThuMucChoViTri(); };
            var mTep = new ToolStripMenuItem("Thêm tệp…");
            mTep.Click += delegate { ChonThemTepChoViTri(); };
            menu.Items.AddRange(new ToolStripItem[] { mThuMuc, mTep });
            menu.Show(btnThemViTri, new Point(0, btnThemViTri.Height + 2));
        }

        private void ChonThemThuMucChoViTri()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Thêm thư mục vào danh sách vị trí quét";
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                ThemViTri(dlg.SelectedPath, true);
            }
        }

        private void ChonThemTepChoViTri()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Thêm tệp vào danh sách vị trí quét";
                dlg.Filter = "Tất cả tệp (*.*)|*.*";
                dlg.Multiselect = true;
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                foreach (string f in dlg.FileNames) ThemViTri(f, false);
            }
        }

        /// <summary>Thêm 1 vị trí (thư mục/tệp) với luật dedupe giống chế độ thư mục.</summary>
        private bool ThemViTri(string path, bool isDirectory)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (isDirectory ? !Directory.Exists(path) : !File.Exists(path)) return false;
            string full = path.TrimEnd(Path.DirectorySeparatorChar);
            foreach (ScanTarget t in dsViTri)
            {
                string coF = t.Path.TrimEnd(Path.DirectorySeparatorChar);
                if (string.Equals(coF, full, StringComparison.OrdinalIgnoreCase)) return false;
                if (isDirectory && full.StartsWith(coF + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return false;  // đã nằm trong thư mục đang có
            }
            if (isDirectory)
            {
                for (int i = dsViTri.Count - 1; i >= 0; i--)
                {
                    string coF = dsViTri[i].Path.TrimEnd(Path.DirectorySeparatorChar);
                    if (coF.StartsWith(full + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        dsViTri.RemoveAt(i);   // gộp: giữ thư mục cha
                }
            }
            dsViTri.Add(new ScanTarget { Path = path, IsDirectory = isDirectory });
            LamMoiBangViTri();
            return true;
        }

        private void LamMoiBangViTri()
        {
            dgvViTri.Rows.Clear();
            for (int i = 0; i < dsViTri.Count; i++)
                dgvViTri.Rows.Add((i + 1).ToString(), dsViTri[i].Path,
                    dsViTri[i].IsDirectory ? "Thư mục" : "Tệp", "Xóa");
            CapNhatLuuY();
            CapNhatTrangThaiNutBatDauQuet();
        }

        private void XoaDongViTriDangChon()
        {
            if (dgvViTri.CurrentRow == null || dgvViTri.CurrentRow.Index >= dsViTri.Count)
            {
                MessageBox.Show("Hãy bấm chọn 1 dòng trong bảng vị trí quét trước.",
                    "Xóa dòng", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            dsViTri.RemoveAt(dgvViTri.CurrentRow.Index);
            LamMoiBangViTri();
        }

        private void XoaTatCaViTri()
        {
            if (dsViTri.Count == 0) return;
            if (dsViTri.Count >= 3 &&
                MessageBox.Show(string.Format("Xóa toàn bộ {0} vị trí quét?", dsViTri.Count),
                    "Xóa tất cả", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            dsViTri.Clear();
            LamMoiBangViTri();
        }

        private void DgvViTri_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 3) return;
            if (e.RowIndex >= dsViTri.Count) return;
            dsViTri.RemoveAt(e.RowIndex);
            LamMoiBangViTri();
        }

        private void DgvViTri_DragDrop(object sender, DragEventArgs e)
        {
            string[] paths = LayDuongDanKeoTha(e);
            if (paths == null) return;
            foreach (string p in paths)
            {
                if (Directory.Exists(p)) ThemViTri(p, true);
                else if (File.Exists(p)) ThemViTri(p, false);
            }
        }

        // ================= "BẮT ĐẦU QUÉT" TỪ TRANG (d) =================

        private async void BatDauQuetNangCao()
        {
            if (isScanning) return;
            ScanJob job = TaoJobTuTrangQuetNangCao();
            if (job == null) return;
            btnBatDauQuet.Enabled = false;
            await ChayPhienQuet(job);
            CapNhatTrangThaiNutBatDauQuet();
        }

        /// <summary>
        /// Dựng ScanJob theo chế độ đang chọn. Vì ScanEngine chỉ nhận 1 CustomPath/lần,
        /// mỗi vị trí được quét tuần tự bằng ScanType.Custom rồi gộp kết quả (xem ChayPhienQuet).
        /// "Loại tệp quét" và "Bỏ qua tệp lớn hơn" được áp ở tầng UI sau khi có kết quả
        /// (engine hiện không có tham số lọc theo phần mở rộng/kích thước).
        /// </summary>
        private ScanJob TaoJobTuTrangQuetNangCao()
        {
            var job = new ScanJob { FromAdvanced = true };
            switch (cheDo)
            {
                case CheDoQuetNangCao.FullSystem:
                {
                    var roots = new List<string>();
                    foreach (DataGridViewRow r in dgvODia.Rows)
                        if (r.Cells[colODiaChon.Index].Value is bool b && b)
                            roots.Add(Convert.ToString(r.Cells[1].Value));
                    if (roots.Count == 0)
                    {
                        MessageBox.Show("Hãy tick ít nhất 1 ổ đĩa cần quét.", "Quét toàn bộ hệ thống",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return null;
                    }
                    job.Targets = roots;
                    job.DisplayName = "Quét toàn bộ";
                    job.ScopeText = "Ổ đĩa: " + string.Join(", ", roots.ToArray());
                    job.AutoQuarantine = chkFull[6].Checked;
                    break;
                }
                case CheDoQuetNangCao.Folder:
                {
                    if (dsThuMuc.Count == 0)
                    {
                        MessageBox.Show("Hãy thêm ít nhất 1 thư mục cần quét.", "Quét thư mục",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return null;
                    }
                    job.Targets = new List<string>(dsThuMuc);
                    job.DisplayName = "Quét thư mục";
                    job.ScopeText = string.Format("{0} thư mục: {1}", dsThuMuc.Count, string.Join(", ", dsThuMuc.ToArray()));
                    job.AutoQuarantine = chkFolder[5].Checked;
                    break;
                }
                case CheDoQuetNangCao.Files:
                {
                    if (dsTep.Count == 0)
                    {
                        MessageBox.Show("Hãy chọn ít nhất 1 tệp cần quét.", "Quét tệp",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return null;
                    }
                    job.Targets = new List<string>(dsTep);
                    job.DisplayName = "Quét tệp";
                    job.ScopeText = string.Format("{0} tệp đã chọn", dsTep.Count);
                    job.AutoQuarantine = chkFiles[2].Checked;
                    break;
                }
                default:
                {
                    if (dsViTri.Count == 0)
                    {
                        MessageBox.Show("Hãy thêm ít nhất 1 vị trí (thư mục hoặc tệp) cần quét.", "Quét tùy chỉnh",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return null;
                    }
                    job.Targets = dsViTri.Select(t => t.Path).ToList();
                    job.DisplayName = "Quét tùy chỉnh";
                    job.ScopeText = string.Format("{0} vị trí đã chọn", dsViTri.Count);
                    job.AutoQuarantine = chkCustom[4].Checked;

                    if (rdoLoai[1].Checked) job.ExtensionFilter = ExecutableExtensions;
                    else if (rdoLoai[2].Checked) job.ExtensionFilter = ArchiveExtensions;
                    else if (rdoLoai[3].Checked) job.ExtensionFilter = DocumentExtensions;
                    else if (rdoLoai[4].Checked)
                    {
                        string loi;
                        HashSet<string> set = ParsePhanMoRong(out loi);
                        if (set.Count == 0)
                        {
                            MessageBox.Show(loi, "Loại tệp quét", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtPhanMoRong.Focus();
                            return null;
                        }
                        job.ExtensionFilter = set;
                    }
                    if (chkBoQuaTepLon.Checked)
                        job.SkipLargerThanBytes = (long)numBoQuaTepLon.Value * HeSoDonVi();
                    break;
                }
            }
            return job;
        }

        private long HeSoDonVi()
        {
            switch (cboDonVi.SelectedIndex)
            {
                case 0: return 1024L;                       // KB
                case 2: return 1024L * 1024L * 1024L;       // GB
                default: return 1024L * 1024L;              // MB
            }
        }
    }
}
