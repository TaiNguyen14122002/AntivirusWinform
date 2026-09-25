using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// MÀN HÌNH (c) — "Chi tiết kết quả quét" của UcTongQuan (SPEC-UcTongQuan.md §3.3, §6.5, §9, §10).
    /// Bảng đầy đủ mọi đe dọa + panel 5 tab: Thông tin chi tiết / VirusTotal / Hành vi / Chuỗi ký tự /
    /// Thông tin bổ sung. Tab "Thông tin chi tiết" nạp ngay; 4 tab còn lại nạp LAZY khi mở.
    /// Hash MD5/SHA1/SHA256 chỉ tính khi tab cần, và tính trên luồng nền để không treo UI.
    /// </summary>
    public partial class UcTongQuan
    {
        // ---- header trang (c) ----
        private Button btnQuayLaiChiTiet;
        private Button btnXuatBaoCao;
        private Label lblChiTietTitle;
        private Label lblChiTietSub;
        private Label lblThoiGianQuet;
        private Label lblLoaiQuet;
        // ---- bảng đầy đủ ----
        private DataGridView dgvChiTiet;
        private DataGridViewTextBoxColumn colCtStt;
        private DataGridViewTextBoxColumn colCtTen;
        private DataGridViewTextBoxColumn colCtDuongDan;
        private DataGridViewTextBoxColumn colCtDeDoa;
        private DataGridViewTextBoxColumn colCtMucDo;
        private DataGridViewButtonColumn colCtHash;
        private DataGridViewButtonColumn colCtXem;
        private DataGridViewButtonColumn colCtChevron;
        // ---- 5 tab ----
        private TabControl tabChiTietDong;
        private TabPage tabThongTin, tabVirusTotalPage, tabHanhViPage, tabChuoiPage, tabBoSungPage;
        private Label lblTtTenTep, lblTtDuongDan, lblTtKichThuoc, lblTtLoaiTep, lblTtTao, lblTtSua;
        private Label lblTtMd5, lblTtSha1, lblTtSha256;
        private Button btnCopyMd5, btnCopySha1, btnCopySha256;
        private VtDonut donutVt;
        private Label lblVtKetLuan, lblVtLanPhanTich, lblVtLink;
        private Button btnCopyVtLink, btnMoVirusTotal, btnTraVtChiTiet;
        private DataGridView dgvVtVendors;
        private Label lblVtVendorNote;
        private TextBox txtHanhVi;
        private DataGridView dgvChuoiKyTu;
        private Label lblChuoiEmpty;
        private Label lblTtbsMotw, lblTtbsThuocTinh, lblTtbsTrangThai, lblTtbsTienTrinh, lblTtbsGhiChu;
        private ContextMenuStrip ctxDongChiTiet;
        private bool daDungChiTiet;
        private int tabDaNap = -1;          // tab nào đã nạp cho dòng đang chọn

        /// <summary>Chuỗi/marker đáng ngờ cho tab "Chuỗi ký tự" (khớp danh sách heuristic của ScanEngine).</summary>
        static readonly string[] MarkerDangNgo =
        {
            "iex(", "-enc ", "-EncodedCommand", "DownloadString", "FromBase64String", "Invoke-Expression",
            "cmd.exe", "powershell", "wscript", "cscript", "CreateObject", "eval(", "document.write",
            "ShellExecute", "reg add", "schtasks", "Start-Process"
        };

        // ================= DỰNG GIAO DIỆN TRANG (c) =================

        private void BuildChiTietView()
        {
            var root = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Name = "tlpChiTiet" };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));   // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 196F));  // bảng chi tiết
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // 5 tab
            root.Controls.Add(DungHeaderChiTiet(), 0, 0);
            root.Controls.Add(DungBangChiTiet(), 0, 1);
            root.Controls.Add(DungTabChiTiet(), 0, 2);
            pnlChiTietKetQua.Controls.Add(root);
        }

        // ---- tiện ích dựng control (giữ đúng Theme: mọi Button đi qua Theme.StyleButton) ----
        private static Label Nhan(string text, Font font, Color color, ContentAlignment align)
        {
            return new Label
            {
                Text = text, Font = font, ForeColor = color, Dock = DockStyle.Fill,
                TextAlign = align, Margin = new Padding(0), AutoEllipsis = true
            };
        }

        private static Button Nut(string text, Theme.BtnRole role, int width, int height = 32)
        {
            var b = new Button { Text = text, Width = width, Height = height, Dock = DockStyle.Right, Margin = new Padding(4, 0, 0, 0) };
            Theme.StyleButton(b, role);
            return b;
        }

        private System.Windows.Forms.Control DungHeaderChiTiet()
        {
            var head = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            head.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            btnQuayLaiChiTiet = Nut("Quay lại", Theme.BtnRole.Secondary, 118, 36);
            btnQuayLaiChiTiet.Dock = DockStyle.Left;
            btnQuayLaiChiTiet.Image = UiIcons.ArrowLeft(14, Theme.BlueDark);
            btnQuayLaiChiTiet.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnQuayLaiChiTiet.Click += delegate
            {
                HienThi(dgvActions.Rows.Count > 0 ? TongQuanView.PhatHienDeDoa : TongQuanView.AnToan);
            };

            var text = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            text.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            text.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            text.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblChiTietTitle = Nhan("Chi tiết kết quả quét", Theme.PageTitleFont, Theme.TextDark, ContentAlignment.MiddleLeft);
            lblChiTietSub = Nhan("Danh sách các mối đe dọa được phát hiện trong lần quét vừa rồi.",
                Theme.PageSubFont, Theme.TextGray, ContentAlignment.MiddleLeft);
            text.Controls.Add(lblChiTietTitle, 0, 0);
            text.Controls.Add(lblChiTietSub, 0, 1);

            var right = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblThoiGianQuet = Nhan("Thời gian quét: —", Theme.SmallFont, Theme.TextMid, ContentAlignment.MiddleRight);
            lblLoaiQuet = Nhan("Loại quét: —", Theme.SmallFont, Theme.TextMid, ContentAlignment.MiddleRight);
            btnXuatBaoCao = Nut("Xuất báo cáo", Theme.BtnRole.Secondary, 138);
            btnXuatBaoCao.Image = UiIcons.External(13, Theme.BlueDark);
            btnXuatBaoCao.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnXuatBaoCao.Click += BtnXuatBaoCao_Click;
            right.Controls.Add(lblThoiGianQuet, 0, 0);
            right.Controls.Add(lblLoaiQuet, 0, 1);
            right.Controls.Add(btnXuatBaoCao, 0, 2);

            head.Controls.Add(btnQuayLaiChiTiet, 0, 0);
            head.Controls.Add(text, 1, 0);
            head.Controls.Add(right, 2, 0);
            return head;
        }

        private System.Windows.Forms.Control DungBangChiTiet()
        {
            dgvChiTiet = new DataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 4),
                Name = "dgvChiTiet",
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ReadOnly = true,
                TabIndex = 0
            };
            colCtStt = new DataGridViewTextBoxColumn
            {
                HeaderText = "#", Name = "colCtStt", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 46,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colCtTen = new DataGridViewTextBoxColumn { HeaderText = "Tên tệp", Name = "colCtTen", FillWeight = 24F, MinimumWidth = 120 };
            colCtDuongDan = new DataGridViewTextBoxColumn { HeaderText = "Đường dẫn", Name = "colCtDuongDan", FillWeight = 34F, MinimumWidth = 160 };
            colCtDeDoa = new DataGridViewTextBoxColumn { HeaderText = "Mối đe dọa", Name = "colCtDeDoa", FillWeight = 30F, MinimumWidth = 160 };
            colCtMucDo = new DataGridViewTextBoxColumn
            {
                HeaderText = "Mức độ", Name = "colCtMucDo", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 92,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colCtHash = new DataGridViewButtonColumn
            {
                HeaderText = "Hash (SHA256)", Name = "colCtHash", Text = "Sao chép hash",
                UseColumnTextForButtonValue = false, AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 134, Resizable = DataGridViewTriState.False, SortMode = DataGridViewColumnSortMode.NotSortable,
                ToolTipText = "Bấm để sao chép SHA256 đầy đủ của tệp"
            };
            colCtXem = new DataGridViewButtonColumn
            {
                HeaderText = "Thao tác", Name = "colCtXem", Text = "Xem",
                UseColumnTextForButtonValue = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 64, Resizable = DataGridViewTriState.False, SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colCtChevron = new DataGridViewButtonColumn
            {
                HeaderText = "", Name = "colCtChevron", Text = "▾",
                UseColumnTextForButtonValue = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 40, Resizable = DataGridViewTriState.False, SortMode = DataGridViewColumnSortMode.NotSortable,
                ToolTipText = "Cách ly / Xóa / Tra VirusTotal cho dòng này"
            };
            dgvChiTiet.Columns.AddRange(new DataGridViewColumn[]
            {
                colCtStt, colCtTen, colCtDuongDan, colCtDeDoa, colCtMucDo, colCtHash, colCtXem, colCtChevron
            });
            Theme.StyleGrid(dgvChiTiet);
            dgvChiTiet.CellFormatting += DgvChiTiet_CellFormatting;
            dgvChiTiet.CellContentClick += DgvChiTiet_CellContentClick;
            dgvChiTiet.SelectionChanged += delegate
            {
                if (!daDungChiTiet || dgvChiTiet.CurrentRow == null) return;
                NapPanelChiTiet(dgvChiTiet.CurrentRow.Index);
            };

            ctxDongChiTiet = new ContextMenuStrip();
            var mCachLy = new ToolStripMenuItem("Cách ly tệp này");
            mCachLy.Click += delegate { XuLyMotDong(detailRowIndex, 1); };
            var mXoa = new ToolStripMenuItem("Xóa vĩnh viễn");
            mXoa.Click += delegate { XuLyMotDong(detailRowIndex, 2); };
            var mVt = new ToolStripMenuItem("Tra VirusTotal");
            mVt.Click += delegate { XuLyMotDong(detailRowIndex, 3); };
            ctxDongChiTiet.Items.AddRange(new ToolStripItem[] { mCachLy, mXoa, mVt });
            return dgvChiTiet;
        }

        private System.Windows.Forms.Control DungTabChiTiet()
        {
            tabChiTietDong = new TabControl { Dock = DockStyle.Fill, Margin = new Padding(0), Font = Theme.BodyFont };
            tabThongTin = new TabPage("Thông tin chi tiết") { BackColor = Theme.PageBg, Padding = new Padding(6) };
            tabVirusTotalPage = new TabPage("VirusTotal") { BackColor = Theme.PageBg, Padding = new Padding(6) };
            tabHanhViPage = new TabPage("Hành vi") { BackColor = Theme.PageBg, Padding = new Padding(6) };
            tabChuoiPage = new TabPage("Chuỗi ký tự") { BackColor = Theme.PageBg, Padding = new Padding(6) };
            tabBoSungPage = new TabPage("Thông tin bổ sung") { BackColor = Theme.PageBg, Padding = new Padding(6) };
            tabThongTin.Controls.Add(DungTabThongTin());
            tabVirusTotalPage.Controls.Add(DungTabVirusTotal());
            tabHanhViPage.Controls.Add(DungTabHanhVi());
            tabChuoiPage.Controls.Add(DungTabChuoiKyTu());
            tabBoSungPage.Controls.Add(DungTabBoSung());
            tabChiTietDong.TabPages.AddRange(new[]
            {
                tabThongTin, tabVirusTotalPage, tabHanhViPage, tabChuoiPage, tabBoSungPage
            });
            tabChiTietDong.SelectedIndexChanged += delegate
            {
                if (!daDungChiTiet || detailRowIndex < 0) return;
                NapTabChiTiet(detailRowIndex, tabChiTietDong.SelectedIndex);   // nạp lazy theo tab đang mở
            };
            return tabChiTietDong;
        }

        /// <summary>Tab 1 — card "Thông tin tệp" (trái) + card "VirusTotal" donut (phải).</summary>
        private System.Windows.Forms.Control DungTabThongTin()
        {
            var host = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0) };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));

            var grp = new GroupBox { Text = "Thông tin tệp", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) };
            Theme.StyleCard(grp);
            var t = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Fill, Padding = new Padding(6, 2, 6, 4) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38F));
            t.RowCount = 10;
            for (int i = 0; i < 9; i++) t.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            string[] ten = { "Tên tệp", "Đường dẫn", "Kích thước", "Loại tệp", "Thời gian tạo", "Thời gian sửa đổi", "MD5", "SHA1", "SHA256" };
            var giaTri = new Label[9];
            for (int i = 0; i < 9; i++)
            {
                t.Controls.Add(Nhan(ten[i], Theme.BoldFont, Theme.TextMid, ContentAlignment.MiddleLeft), 0, i);
                giaTri[i] = Nhan("—", Theme.SmallFont, Theme.TextDark, ContentAlignment.MiddleLeft);
                t.Controls.Add(giaTri[i], 1, i);
                if (i < 6) { t.Controls.Add(new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) }, 2, i); continue; }
                var copy = Nut("", Theme.BtnRole.Neutral, 30, 22);
                copy.Image = UiIcons.Copy(13, Theme.TextMid);
                copy.Dock = DockStyle.Fill;
                copy.Margin = new Padding(4, 0, 0, 0);
                copy.Tag = giaTri[i];
                copy.Click += BtnCopyHash_Click;
                t.Controls.Add(copy, 2, i);
            }
            lblTtTenTep = giaTri[0];
            lblTtDuongDan = giaTri[1];
            lblTtKichThuoc = giaTri[2];
            lblTtLoaiTep = giaTri[3];
            lblTtTao = giaTri[4];
            lblTtSua = giaTri[5];
            lblTtMd5 = giaTri[6];
            lblTtSha1 = giaTri[7];
            lblTtSha256 = giaTri[8];
            btnCopyMd5 = (Button)t.GetControlFromPosition(2, 6);
            btnCopySha1 = (Button)t.GetControlFromPosition(2, 7);
            btnCopySha256 = (Button)t.GetControlFromPosition(2, 8);
            grp.Controls.Add(t);
            host.Controls.Add(grp, 0, 0);
            host.Controls.Add(DungCardVirusTotal(), 1, 0);
            return host;
        }

        /// <summary>Card phải của tab 1: donut X/72 + kết luận + link + 3 CTA.</summary>
        private System.Windows.Forms.Control DungCardVirusTotal()
        {
            var grp = new GroupBox { Text = "VirusTotal", Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0) };
            Theme.StyleCard(grp);
            var t = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Padding = new Padding(6, 2, 6, 4) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            donutVt = new VtDonut { Dock = DockStyle.Fill, Margin = new Padding(0) };
            donutVt.SetEmpty("Chưa tra VirusTotal cho tệp này");
            lblVtKetLuan = Nhan("Chưa có kết quả — bấm “Tra VirusTotal” để tra hash SHA256.",
                Theme.SmallFont, Theme.TextMid, ContentAlignment.MiddleLeft);
            lblVtLanPhanTich = Nhan("Lần phân tích gần nhất: —", Theme.SmallFont, Theme.TextGray, ContentAlignment.MiddleLeft);
            lblVtLink = Nhan("Link: —", Theme.SmallFont, Theme.TextGray, ContentAlignment.MiddleLeft);
            var bar = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Fill, Margin = new Padding(0) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            btnTraVtChiTiet = Nut("Tra VirusTotal", Theme.BtnRole.Primary, 138, 34);
            btnTraVtChiTiet.Click += delegate { XuLyMotDong(detailRowIndex, 3); };
            btnCopyVtLink = Nut("Sao chép link", Theme.BtnRole.Neutral, 118, 34);
            btnCopyVtLink.Click += delegate
            {
                if (SaoChepVaoClipboard(lblVtLink.Tag as string)) NhayNutDaSaoChep(btnCopyVtLink);
            };
            btnMoVirusTotal = Nut("Mở trên VirusTotal", Theme.BtnRole.Secondary, 168, 34);
            btnMoVirusTotal.Image = UiIcons.External(13, Theme.BlueDark);
            btnMoVirusTotal.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnMoVirusTotal.Click += delegate { MoUrl(lblVtLink.Tag as string); };
            bar.Controls.Add(btnMoVirusTotal, 2, 0);
            bar.Controls.Add(btnCopyVtLink, 1, 0);
            bar.Controls.Add(btnTraVtChiTiet, 0, 0);

            t.Controls.Add(donutVt, 0, 0);
            t.Controls.Add(lblVtKetLuan, 0, 1);
            t.Controls.Add(lblVtLanPhanTich, 0, 2);
            t.Controls.Add(lblVtLink, 0, 3);
            t.Controls.Add(bar, 0, 4);
            grp.Controls.Add(t);
            return grp;
        }

        /// <summary>Tab 2 — VirusTotal: bảng tóm tắt số liệu API trả về (không có dữ liệu từng vendor).</summary>
        private System.Windows.Forms.Control DungTabVirusTotal()
        {
            var t = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblVtVendorNote = Nhan("API gói miễn phí chỉ trả về con số đồng thuận; xem kết quả từng hãng trên virustotal.com.",
                Theme.SmallFont, Theme.TextGray, ContentAlignment.MiddleLeft);
            dgvVtVendors = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, ReadOnly = true, Margin = new Padding(0),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, Name = "dgvVtVendors"
            };
            dgvVtVendors.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chỉ số", FillWeight = 40F });
            dgvVtVendors.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giá trị", FillWeight = 60F });
            Theme.StyleGrid(dgvVtVendors);
            t.Controls.Add(lblVtVendorNote, 0, 0);
            t.Controls.Add(dgvVtVendors, 0, 1);
            return t;
        }

        /// <summary>Tab 3 — Hành vi: lý do phát hiện + dấu vết đã gắn cho tệp (MOTW, thuộc tính ẩn…).</summary>
        private System.Windows.Forms.Control DungTabHanhVi()
        {
            txtHanhVi = new TextBox
            {
                Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.PageBg, ForeColor = Theme.TextDark,
                Font = Theme.BodyFont, Margin = new Padding(0), Name = "txtHanhVi"
            };
            return txtHanhVi;
        }

        /// <summary>Tab 4 — Chuỗi ký tự: marker đáng ngờ trích từ nội dung tệp.</summary>
        private System.Windows.Forms.Control DungTabChuoiKyTu()
        {
            var t = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblChuoiEmpty = Nhan("Không phát hiện chuỗi đáng ngờ (hoặc tệp không đọc được nội dung).",
                Theme.SmallFont, Theme.TextGray, ContentAlignment.MiddleLeft);
            dgvChuoiKyTu = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, ReadOnly = true, Margin = new Padding(0),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, Name = "dgvChuoiKyTu"
            };
            dgvChuoiKyTu.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "#", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50
            });
            dgvChuoiKyTu.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Chuỗi / marker đáng ngờ", FillWeight = 100F
            });
            Theme.StyleGrid(dgvChuoiKyTu);
            t.Controls.Add(lblChuoiEmpty, 0, 0);
            t.Controls.Add(dgvChuoiKyTu, 0, 1);
            return t;
        }

        /// <summary>Tab 5 — Thông tin bổ sung: MOTW, thuộc tính tệp, trạng thái xử lý, ghi chú.</summary>
        private System.Windows.Forms.Control DungTabBoSung()
        {
            var t = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(4) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowCount = 6;
            for (int i = 0; i < 6; i++) t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            lblTtbsMotw = Nhan("—", Theme.SmallFont, Theme.TextDark, ContentAlignment.MiddleLeft);
            lblTtbsThuocTinh = Nhan("—", Theme.SmallFont, Theme.TextDark, ContentAlignment.MiddleLeft);
            lblTtbsTrangThai = Nhan("—", Theme.SmallFont, Theme.TextDark, ContentAlignment.MiddleLeft);
            lblTtbsTienTrinh = Nhan("—", Theme.SmallFont, Theme.TextDark, ContentAlignment.MiddleLeft);
            lblTtbsGhiChu = Nhan("—", Theme.SmallFont, Theme.TextMid, ContentAlignment.MiddleLeft);
            t.Controls.Add(Nhan("Mark of the Web (MOTW)", Theme.BoldFont, Theme.TextMid, ContentAlignment.MiddleLeft), 0, 0);
            t.Controls.Add(lblTtbsMotw, 1, 0);
            t.Controls.Add(Nhan("Thuộc tính tệp", Theme.BoldFont, Theme.TextMid, ContentAlignment.MiddleLeft), 0, 1);
            t.Controls.Add(lblTtbsThuocTinh, 1, 1);
            t.Controls.Add(Nhan("Trạng thái xử lý", Theme.BoldFont, Theme.TextMid, ContentAlignment.MiddleLeft), 0, 2);
            t.Controls.Add(lblTtbsTrangThai, 1, 2);
            t.Controls.Add(Nhan("Tiến trình / nguồn sinh tệp", Theme.BoldFont, Theme.TextMid, ContentAlignment.MiddleLeft), 0, 3);
            t.Controls.Add(lblTtbsTienTrinh, 1, 3);
            t.Controls.Add(Nhan("Ghi chú", Theme.BoldFont, Theme.TextMid, ContentAlignment.MiddleLeft), 0, 4);
            t.Controls.Add(lblTtbsGhiChu, 1, 4);
            return t;
        }

        // ================= LOGIC TRANG (c) =================

        /// <summary>Dựng lại bảng đầy đủ từ danh sách threatRows (mọi dòng, kể cả đã cách ly/xóa).</summary>
        private void RefreshDetailGrid()
        {
            if (dgvChiTiet == null) return;
            daDungChiTiet = false;
            int keep = detailRowIndex;
            dgvChiTiet.Rows.Clear();
            for (int i = 0; i < threatRows.Count; i++)
            {
                ThreatRow r = threatRows[i];
                string deDoa = r.Resolved ? r.ThreatText + "   ·   " + r.StatusText : r.ThreatText;
                dgvChiTiet.Rows.Add((i + 1).ToString(), Path.GetFileName(r.Path), r.Path, deDoa,
                    Theme.LevelText(r.Level), RutGonHash(r), "Xem", "▾");
            }
            lblThoiGianQuet.Text = "Thời gian quét: " + (lastScanTime == DateTime.MinValue
                ? "—" : lastScanTime.ToString("dd/MM/yyyy HH:mm:ss"));
            lblLoaiQuet.Text = "Loại quét: " + (lastJob == null ? "—" : lastJob.DisplayName);
            daDungChiTiet = true;

            if (threatRows.Count == 0) { detailRowIndex = -1; tabDaNap = -1; return; }
            if (keep < 0 || keep >= threatRows.Count) keep = 0;
            ChonDongChiTiet(keep);
        }

        private static string RutGonHash(ThreatRow r)
        {
            if (string.IsNullOrEmpty(r.HashSha256)) return "Sao chép hash";
            return r.HashSha256.Length > 18 ? r.HashSha256.Substring(0, 16) + "…" : r.HashSha256;
        }

        /// <summary>Chọn 1 dòng trong bảng chi tiết và nạp panel bên dưới cho đúng dòng đó.</summary>
        private void ChonDongChiTiet(int index)
        {
            if (index < 0 || index >= dgvChiTiet.Rows.Count) return;
            daDungChiTiet = false;
            dgvChiTiet.ClearSelection();
            dgvChiTiet.Rows[index].Selected = true;
            if (dgvChiTiet.Rows[index].Cells.Count > 1)
                dgvChiTiet.CurrentCell = dgvChiTiet.Rows[index].Cells[1];
            daDungChiTiet = true;
            NapPanelChiTiet(index);
        }

        /// <summary>Mở trang (c) — không truyền dòng thì mở dòng đang chọn (hoặc dòng đầu).</summary>
        internal void MoChiTietKetQua(int actionRowIndex = -1)
        {
            if (threatRows.Count == 0)
            {
                MessageBox.Show("Chưa có kết quả quét nào để xem chi tiết. Hãy chạy một phiên quét trước.",
                    "Chi tiết kết quả quét", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int idx = detailRowIndex;
            if (actionRowIndex >= 0 && actionRowIndex < dgvActions.Rows.Count)
            {
                string path = dgvActions.Rows[actionRowIndex].Cells[colActionFile.Index].Value as string;
                int found = threatRows.FindIndex(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));
                if (found >= 0) idx = found;
            }
            if (idx < 0 || idx >= threatRows.Count) idx = 0;
            HienThi(TongQuanView.ChiTiet, false);
            RefreshDetailGrid();
            ChonDongChiTiet(idx);
        }

        /// <summary>Nạp panel 5 tab cho dòng đang chọn: tab 1 nạp ngay, 4 tab còn lại nạp lazy.</summary>
        private void NapPanelChiTiet(int index)
        {
            if (index < 0 || index >= threatRows.Count) return;
            detailRowIndex = index;
            tabDaNap = -1;
            ThreatRow r = threatRows[index];

            lblTtTenTep.Text = Path.GetFileName(r.Path);
            lblTtDuongDan.Text = r.Path;
            lblTtKichThuoc.Text = string.Format("{0} ({1:N0} bytes)",
                DirectorySizeCalculator.Format(r.SizeBytes), r.SizeBytes);
            lblTtLoaiTep.Text = MoTaLoaiTep(r.Path);
            lblTtTao.Text = r.CreatedAt == DateTime.MinValue ? "—" : r.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss");
            lblTtSua.Text = r.ModifiedAt == DateTime.MinValue ? "—" : r.ModifiedAt.ToString("dd/MM/yyyy HH:mm:ss");
            if (!string.IsNullOrEmpty(r.HashMd5)) lblTtMd5.Text = r.HashMd5;
            if (!string.IsNullOrEmpty(r.HashSha1)) lblTtSha1.Text = r.HashSha1;
            if (!string.IsNullOrEmpty(r.HashSha256)) lblTtSha256.Text = r.HashSha256;
            if (string.IsNullOrEmpty(r.HashMd5) || string.IsNullOrEmpty(r.HashSha1) || string.IsNullOrEmpty(r.HashSha256))
                TinhHashChoDongNen(index);

            CapNhatVtPanel(r);
            if (tabChiTietDong != null && tabChiTietDong.SelectedIndex != 0) tabChiTietDong.SelectedIndex = 0;
            NapTabChiTiet(index, 0);
        }

        /// <summary>Tính MD5/SHA1/SHA256 trên luồng nền (không đọc lại nhiều lần nếu đã có).</summary>
        private void TinhHashChoDongNen(int index)
        {
            ThreatRow r = threatRows[index];
            string path = r.Path;
            lblTtMd5.Text = lblTtSha1.Text = lblTtSha256.Text = "Đang tính…";
            Task.Run(() =>
            {
                string md5 = null, sha1 = null, sha256 = null;
                try
                {
                    if (File.Exists(path))
                    {
                        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                            FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan))
                        using (var m = MD5.Create())
                        using (var s1 = SHA1.Create())
                        using (var s2 = SHA256.Create())
                        {
                            byte[] buf = new byte[64 * 1024];
                            int read;
                            while ((read = fs.Read(buf, 0, buf.Length)) > 0)
                            {
                                m.TransformBlock(buf, 0, read, null, 0);
                                s1.TransformBlock(buf, 0, read, null, 0);
                                s2.TransformBlock(buf, 0, read, null, 0);
                            }
                            m.TransformFinalBlock(new byte[0], 0, 0);
                            s1.TransformFinalBlock(new byte[0], 0, 0);
                            s2.TransformFinalBlock(new byte[0], 0, 0);
                            md5 = ToHex(m.Hash);
                            sha1 = ToHex(s1.Hash);
                            sha256 = ToHex(s2.Hash);
                        }
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                try
                {
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        r.HashMd5 = md5;
                        r.HashSha1 = sha1;
                        if (r.HashSha256 == null) r.HashSha256 = sha256;
                        if (detailRowIndex != index) return;
                        lblTtMd5.Text = md5 ?? "Không đọc được tệp";
                        lblTtSha1.Text = sha1 ?? "Không đọc được tệp";
                        lblTtSha256.Text = sha256 ?? "Không đọc được tệp";
                        if (dgvChiTiet != null && index < dgvChiTiet.Rows.Count)
                            dgvChiTiet.Rows[index].Cells[colCtHash.Index].Value = RutGonHash(r);
                    }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            });
        }

        private static string ToHex(byte[] data)
        {
            if (data == null) return null;
            var sb = new StringBuilder(data.Length * 2);
            for (int i = 0; i < data.Length; i++) sb.Append(data[i].ToString("x2"));
            return sb.ToString();
        }

        /// <summary>Nạp card VirusTotal (donut + kết luận + link) theo dữ liệu đã tra của dòng.</summary>
        private void CapNhatVtPanel(ThreatRow r)
        {
            VirusTotalReport rep = r.VtReport;
            if (rep == null)
            {
                donutVt.SetEmpty("Chưa tra VirusTotal cho tệp này");
                lblVtKetLuan.Text = "Chưa có kết quả — bấm “Tra VirusTotal” để tra hash SHA256.";
                lblVtKetLuan.ForeColor = Theme.TextMid;
                lblVtLanPhanTich.Text = "App chưa tra VirusTotal cho tệp này.";
                lblVtLink.Text = "Link: —";
                lblVtLink.Tag = null;
                btnMoVirusTotal.Enabled = false;
                btnCopyVtLink.Enabled = false;
                return;
            }
            if (rep.Error != null)
            {
                donutVt.SetEmpty("Tra cứu lỗi");
                lblVtKetLuan.Text = "Lỗi tra cứu: " + rep.Error;
                lblVtKetLuan.ForeColor = Theme.Red;
            }
            else if (!rep.Found)
            {
                donutVt.SetEmpty("Mẫu chưa có trên VirusTotal");
                lblVtKetLuan.Text = "Chưa có mẫu này trên VirusTotal (app chỉ tra hash, không upload nội dung tệp).";
                lblVtKetLuan.ForeColor = Theme.Amber;
            }
            else
            {
                donutVt.SetData(rep.Malicious, rep.TotalEngines, "công cụ báo độc");
                lblVtKetLuan.Text = rep.Summary();
                lblVtKetLuan.ForeColor = rep.IsMalicious ? Theme.Red : (rep.IsSuspicious ? Theme.Amber : Theme.Green);
            }
            lblVtLanPhanTich.Text = r.VtQueriedAt == DateTime.MinValue
                ? "Lần phân tích gần nhất: API không trả về thời điểm"
                : "App tra lúc: " + r.VtQueriedAt.ToString("dd/MM/yyyy HH:mm:ss");
            lblVtLink.Text = "Link: " + (r.VtLink ?? "—");
            lblVtLink.Tag = r.VtLink;
            btnMoVirusTotal.Enabled = r.VtLink != null;
            btnCopyVtLink.Enabled = r.VtLink != null;
        }

        /// <summary>Nạp 1 trong 4 tab phụ (lazy) cho dòng đang chọn.</summary>
        private void NapTabChiTiet(int index, int tab)
        {
            if (index < 0 || index >= threatRows.Count) return;
            if (tabDaNap == tab) return;
            tabDaNap = tab;
            ThreatRow r = threatRows[index];
            switch (tab)
            {
                case 1: NapTabVtTongHop(r); break;
                case 2: txtHanhVi.Text = MoTaHanhVi(r); break;
                case 3: NapDanhSachChuoi(r); break;
                case 4: NapThongTinBoSung(r); break;
            }
        }

        /// <summary>Tab "VirusTotal": bảng tóm tắt con số API trả về cho dòng đang chọn.</summary>
        private void NapTabVtTongHop(ThreatRow r)
        {
            dgvVtVendors.Rows.Clear();
            VirusTotalReport rep = r.VtReport;
            if (rep == null)
            {
                dgvVtVendors.Rows.Add("Trạng thái", "Chưa tra VirusTotal cho tệp này");
                return;
            }
            dgvVtVendors.Rows.Add("Trạng thái", rep.Error != null ? "Lỗi: " + rep.Error
                : (!rep.Found ? "Chưa có mẫu trên VirusTotal" : "Đã có báo cáo"));
            dgvVtVendors.Rows.Add("Số công cụ đã phân tích", rep.Found ? rep.TotalEngines.ToString() : "—");
            dgvVtVendors.Rows.Add("Số công cụ báo độc", rep.Found ? rep.Malicious.ToString() : "—");
            dgvVtVendors.Rows.Add("Kết luận của app", rep.Error != null ? "không kết luận được"
                : (!rep.Found ? "chưa có mẫu (không đồng nghĩa an toàn)"
                : (rep.IsMalicious ? "ĐỘC HẠI" : (rep.IsSuspicious ? "nghi ngờ" : "không hãng nào báo độc"))));
            dgvVtVendors.Rows.Add("Báo cáo đầy đủ", r.VtLink ?? "—");
        }

        /// <summary>Tab "Hành vi": lý do phát hiện + dấu vết tĩnh; nói rõ khi chưa có dữ liệu runtime.</summary>
        private string MoTaHanhVi(ThreatRow r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Lý do phát hiện: " + r.ThreatText);
            sb.AppendLine("Mức độ đánh giá: " + Theme.LevelText(r.Level));
            sb.AppendLine("Trạng thái xử lý: " + r.StatusText);
            sb.AppendLine();
            if (r.Level == Theme.ThreatLevel.High && (r.ThreatText ?? string.Empty).StartsWith("Chữ ký"))
            {
                sb.AppendLine("• Khớp chữ ký cục bộ (prefix byte đầu / hash SHA256 trong bảng Signatures)");
                sb.AppendLine("• Đây là kết luận chắc chắn theo bảng chữ ký thử nghiệm của app.");
            }
            else if ((r.ThreatText ?? string.Empty).StartsWith("Heuristic"))
            {
                sb.AppendLine("• Chấm điểm đặc điểm nghi vấn (đuôi kép, mồi câu xã hội, tệp ẩn, marker script…)");
                sb.AppendLine("• Phát hiện theo heuristic có thể báo nhầm — nên tra VirusTotal để chốt.");
            }
            else if ((r.ThreatText ?? string.Empty).StartsWith("VirusTotal"))
            {
                sb.AppendLine("• Kết luận từ VirusTotal theo hash SHA256 (không upload nội dung tệp).");
            }
            string motw = DocMotw(r.Path);
            if (!string.IsNullOrEmpty(motw)) sb.AppendLine("• Mark of the Web: " + motw);
            string attr = MoTaThuocTinh(r.Path);
            if (!string.IsNullOrEmpty(attr)) sb.AppendLine("• Thuộc tính tệp: " + attr);
            sb.AppendLine();
            sb.AppendLine("Ghi chú: app chưa giám sát tiến trình đang chạy (cần WMI) — tab này chỉ hiển thị");
            sb.AppendLine("dấu vết tĩnh thu được khi quét, không bịa log hành vi runtime.");
            return sb.ToString();
        }

        /// <summary>Tab "Chuỗi ký tự": liệt kê marker đáng ngờ trích từ nội dung tệp (tối đa 4MB).</summary>
        private void NapDanhSachChuoi(ThreatRow r)
        {
            dgvChuoiKyTu.Rows.Clear();
            List<string> markers = TimChuoiDangNgo(r.Path);
            if (markers.Count == 0)
            {
                lblChuoiEmpty.Text = "Không phát hiện chuỗi đáng ngờ (hoặc tệp không đọc được nội dung).";
                lblChuoiEmpty.ForeColor = Theme.TextGray;
                return;
            }
            lblChuoiEmpty.Text = string.Format("Tìm thấy {0} chuỗi/marker đáng ngờ trong nội dung tệp.", markers.Count);
            lblChuoiEmpty.ForeColor = Theme.Amber;
            for (int i = 0; i < markers.Count; i++)
                dgvChuoiKyTu.Rows.Add((i + 1).ToString(), markers[i]);
        }

        /// <summary>Tab "Thông tin bổ sung": MOTW, thuộc tính, trạng thái, nguồn sinh tệp (nếu biết).</summary>
        private void NapThongTinBoSung(ThreatRow r)
        {
            string motw = DocMotw(r.Path);
            lblTtbsMotw.Text = string.IsNullOrEmpty(motw) ? "Không có (tệp không tải từ Internet/không đọc được ADS)"
                : motw;
            string attr = MoTaThuocTinh(r.Path);
            lblTtbsThuocTinh.Text = string.IsNullOrEmpty(attr) ? "Không đọc được thuộc tính tệp" : attr;
            lblTtbsTrangThai.Text = r.StatusText + (r.Quarantined
                ? " (tệp đã được dời vào khu cách ly của app)" : string.Empty);
            lblTtbsTienTrinh.Text = "Chưa theo dõi tiến trình sinh tệp (cần WMI — nằm trong lộ trình của app)";
            lblTtbsGhiChu.Text = string.Format("Phát hiện lúc {0} trong phiên “{1}”.",
                lastScanTime == DateTime.MinValue ? "—" : lastScanTime.ToString("dd/MM/yyyy HH:mm:ss"),
                lastJob == null ? "—" : lastJob.DisplayName);
        }

        // ================= TIỆN ÍCH ĐỌC THÔNG TIN TỆP (không bịa dữ liệu) =================

        /// <summary>Mô tả loại tệp theo phần mở rộng (dùng chung bảng phân loại với trang Quét nâng cao).</summary>
        internal static string MoTaLoaiTep(string path)
        {
            string ext = (Path.GetExtension(path) ?? string.Empty).ToLowerInvariant();
            if (ext.Length == 0) return "Không có phần mở rộng";
            if (ExecutableExtensions.Contains(ext)) return "Tệp thực thi / thư viện (" + ext + ")";
            if (ScriptExtensions.Contains(ext)) return "Tệp kịch bản (" + ext + ")";
            if (ArchiveExtensions.Contains(ext)) return "Tệp nén (" + ext + ")";
            if (DocumentExtensions.Contains(ext)) return "Tài liệu (" + ext + ")";
            return "Tệp khác (" + ext + ")";
        }

        /// <summary>Đọc Mark of the Web (ADS Zone.Identifier) — trả null nếu tệp không có/không đọc được.</summary>
        private static string DocMotw(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string ad = path + ":Zone.Identifier";
                if (!File.Exists(ad)) return null;
                string text;
                using (var sr = new StreamReader(ad))
                    text = sr.ReadToEnd();
                if (string.IsNullOrEmpty(text)) return null;
                string zone = null, host = null;
                foreach (string line in text.Split('\n'))
                {
                    string t = line.Trim('\r', ' ');
                    if (t.StartsWith("ZoneId=", StringComparison.OrdinalIgnoreCase)) zone = t.Substring(7).Trim();
                    else if (t.StartsWith("HostUrl=", StringComparison.OrdinalIgnoreCase)) host = t.Substring(8).Trim();
                }
                string name = zone == "3" ? "Vùng 3 (Internet)" : (zone == "2" ? "Vùng 2 (Intranet tin cậy)" : "Vùng " + (zone ?? "?"));
                return host == null ? name : name + " — nguồn: " + host;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        /// <summary>Mô tả thuộc tính tệp (Hidden/ReadOnly/System/ReparsePoint) — null nếu không đọc được.</summary>
        internal static string MoTaThuocTinh(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                FileAttributes a = File.GetAttributes(path);
                var ten = new List<string>();
                if ((a & FileAttributes.Hidden) != 0) ten.Add("Hidden (ẩn)");
                if ((a & FileAttributes.ReadOnly) != 0) ten.Add("ReadOnly (chỉ đọc)");
                if ((a & FileAttributes.System) != 0) ten.Add("System (hệ thống)");
                if ((a & FileAttributes.ReparsePoint) != 0) ten.Add("ReparsePoint (liên kết)");
                if ((a & FileAttributes.Archive) != 0) ten.Add("Archive");
                return ten.Count == 0 ? "Bình thường (không ẩn/chỉ đọc)" : string.Join(", ", ten.ToArray());
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        /// <summary>
        /// Trích các chuỗi/marker đáng ngờ từ nội dung tệp (tối đa 4MB, giống ngưỡng của ScanEngine).
        /// Trả về danh sách "marker — ngữ cảnh" đã loại trùng.
        /// </summary>
        internal static List<string> TimChuoiDangNgo(string path)
        {
            var ketQua = new List<string>();
            const long MaxBytes = 4L * 1024 * 1024;
            try
            {
                if (!File.Exists(path)) return ketQua;
                var info = new FileInfo(path);
                if (info.Length == 0 || info.Length > MaxBytes) return ketQua;
                string noiDung;
                using (var sr = new StreamReader(path, Encoding.UTF8, true))
                    noiDung = sr.ReadToEnd();
                if (noiDung.Length == 0) return ketQua;

                var daCo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string[] dong = noiDung.Split('\n');
                foreach (string marker in MarkerDangNgo)
                {
                    foreach (string raw in dong)
                    {
                        int at = raw.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                        if (at < 0) continue;
                        string nguCanh = raw.Trim();
                        if (nguCanh.Length > 120) nguCanh = nguCanh.Substring(0, 118) + "…";
                        string item = string.Format("{0}   →   {1}", marker, nguCanh);
                        if (daCo.Add(item)) ketQua.Add(item);
                        break;   // mỗi marker chỉ lấy 1 dòng tiêu biểu
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (DecoderFallbackException) { }
            return ketQua;
        }

        // ================= SỰ KIỆN BẢNG CHI TIẾT + HÀNH ĐỘNG TỪNG DÒNG =================

        private void DgvChiTiet_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colCtMucDo.Index) return;
            if (e.RowIndex < threatRows.Count) Theme.PaintBadgeCell(e, threatRows[e.RowIndex].Level);
        }

        private void DgvChiTiet_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= threatRows.Count) return;
            ThreatRow r = threatRows[e.RowIndex];
            if (e.ColumnIndex == colCtHash.Index)
            {
                string hash = r.HashSha256;
                if (string.IsNullOrEmpty(hash) && File.Exists(r.Path)) hash = ScanEngine.ComputeFileSha256(r.Path);
                if (string.IsNullOrEmpty(hash))
                {
                    MessageBox.Show("Chưa tính được SHA256 (tệp đã bị xóa hoặc không đọc được).",
                        "Sao chép hash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                r.HashSha256 = hash;
                if (SaoChepVaoClipboard(hash))
                    MessageBox.Show("Đã sao chép SHA256 đầy đủ vào clipboard.", "Sao chép hash",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (e.ColumnIndex == colCtXem.Index)
            {
                ChonDongChiTiet(e.RowIndex);
                return;
            }
            if (e.ColumnIndex == colCtChevron.Index)
            {
                if (r.Deleted)
                {
                    MessageBox.Show("Tệp này đã bị xóa khỏi máy nên không còn thao tác nào áp dụng được.",
                        "Thao tác theo dòng", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                ChonDongChiTiet(e.RowIndex);
                ctxDongChiTiet.Show(dgvChiTiet, dgvChiTiet.PointToClient(Cursor.Position));
            }
        }

        /// <summary>Hành động theo 1 dòng: 1 = cách ly, 2 = xóa vĩnh viễn, 3 = tra VirusTotal.</summary>
        private void XuLyMotDong(int index, int action)
        {
            if (index < 0 || index >= threatRows.Count) return;
            ThreatRow r = threatRows[index];
            if (action == 3) { TraVirusTotalTheoPath(r.Path); return; }

            if (action == 2)
            {
                DialogResult answer = MessageBox.Show(
                    "Xóa vĩnh viễn tệp này khỏi máy tính?\n" + r.Path + "\nHành động này không thể hoàn tác.",
                    "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes) return;
                try
                {
                    if (File.Exists(r.Path)) File.Delete(r.Path);
                    r.Deleted = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không xóa được tệp (có thể đang bị tiến trình khác giữ): " + ex.Message,
                        "Xóa tệp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                BoDongTrongBangDeDoa(r.Path);
                MessageBox.Show("Đã xóa vĩnh viễn tệp khỏi máy tính.", "Xóa tệp",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (File.Exists(r.Path))
                {
                    if (!ScanEngine.Quarantine(r.Path, r.ThreatText))
                    {
                        MessageBox.Show("Không cách ly được tệp (có thể đang bị khoá).", "Cách ly",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                r.Quarantined = true;   // tệp không còn ở vị trí cũ: coi như đã được xử lý
                BoDongTrongBangDeDoa(r.Path);
                MessageBox.Show("Đã cách ly tệp vào khu cách ly của app.", "Cách ly",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            UpdateThreatUi();
            RefreshDetailGrid();
            ChonDongChiTiet(index);
        }

        /// <summary>Bỏ dòng tương ứng khỏi bảng (b) sau khi tệp đã được cách ly/xóa từ trang (c).</summary>
        private void BoDongTrongBangDeDoa(string path)
        {
            foreach (DataGridViewRow row in dgvActions.Rows.Cast<DataGridViewRow>().ToList())
                if (Equals(row.Cells[colActionFile.Index].Value, path))
                    dgvActions.Rows.Remove(row);
        }

        /// <summary>Sao chép giá trị của Label gắn trong Tag (nút copy cạnh MD5/SHA1/SHA256).</summary>
        private void BtnCopyHash_Click(object sender, EventArgs e)
        {
            var nut = sender as Button;
            if (nut == null) return;
            var lbl = nut.Tag as Label;
            if (lbl == null) return;
            string gia = lbl.Text;
            if (string.IsNullOrEmpty(gia) || gia == "—" || gia == "Đang tính…" || gia.StartsWith("Không đọc"))
            {
                MessageBox.Show("Chưa có giá trị để sao chép.", "Sao chép",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (SaoChepVaoClipboard(gia)) NhayNutDaSaoChep(nut);
        }

        private static bool SaoChepVaoClipboard(string noiDung)
        {
            if (string.IsNullOrEmpty(noiDung)) return false;
            try { Clipboard.SetText(noiDung); return true; }
            catch (Exception)
            {
                MessageBox.Show("Không sao chép được vào clipboard (clipboard đang bị chiếm).",
                    "Sao chép", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        /// <summary>Đổi icon nút thành chấm xanh trong 1,5 giây để báo "đã sao chép".</summary>
        private static void NhayNutDaSaoChep(object nut)
        {
            var btn = nut as Button;
            if (btn == null) return;
            Image cu = btn.Image;
            btn.Image = UiIcons.Dot(12, Theme.Green);
            var t = new Timer { Interval = 1500 };
            t.Tick += delegate
            {
                t.Stop();
                t.Dispose();
                if (!btn.IsDisposed) btn.Image = cu;
            };
            t.Start();
        }

        private static void MoUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không mở được trình duyệt: " + ex.Message, "Mở liên kết",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ================= XUẤT BÁO CÁO 1 PHIÊN (khác "Xuất CSV" toàn bộ lịch sử ở tab Lịch sử) =================

        private void BtnXuatBaoCao_Click(object sender, EventArgs e)
        {
            if (threatRows.Count == 0)
            {
                MessageBox.Show("Chưa có kết quả quét nào để xuất báo cáo. Hãy chạy một phiên quét trước.",
                    "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Xuất báo cáo phiên quét";
                // Project không có thư viện PDF sẵn -> xuất TXT (đọc được bằng Notepad/Word), ghi rõ trong báo cáo
                dlg.Filter = "Báo cáo văn bản (*.txt)|*.txt";
                dlg.FileName = string.Format("BaoCao-{0:yyyyMMdd-HHmmss}.txt",
                    lastScanTime == DateTime.MinValue ? DateTime.Now : lastScanTime);
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dlg.FileName, TaoNoiDungBaoCao(), new UTF8Encoding(true));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không ghi được tệp báo cáo: " + ex.Message, "Xuất báo cáo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (MessageBox.Show("Đã xuất báo cáo:\n" + dlg.FileName + "\n\nMở tệp ngay?",
                        "Xuất báo cáo", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    MoUrl(dlg.FileName);
            }
        }

        /// <summary>Nội dung báo cáo: header phiên quét + toàn bộ đe dọa (hash đầy đủ, kết quả VT nếu có).</summary>
        private string TaoNoiDungBaoCao()
        {
            var sb = new StringBuilder();
            sb.AppendLine("BÁO CÁO PHIÊN QUÉT — ScanAndRemoveVirus");
            sb.AppendLine("============================================================");
            sb.AppendLine("Thời gian quét   : " + (lastScanTime == DateTime.MinValue ? "—" : lastScanTime.ToString("dd/MM/yyyy HH:mm:ss")));
            sb.AppendLine("Loại quét        : " + (lastJob == null ? "—" : lastJob.DisplayName));
            sb.AppendLine("Phạm vi          : " + (lastJob == null ? "—" : lastJob.ScopeText));
            sb.AppendLine("Tổng tệp đã quét : " + lastScannedFiles.ToString("N0"));
            sb.AppendLine("Số mối đe dọa    : " + threatRows.Count);
            sb.AppendLine();
            sb.AppendLine("DANH SÁCH MỐI ĐE DỌA");
            sb.AppendLine("------------------------------------------------------------");
            for (int i = 0; i < threatRows.Count; i++)
            {
                ThreatRow r = threatRows[i];
                string sha = r.HashSha256;
                if (string.IsNullOrEmpty(sha) && File.Exists(r.Path))
                {
                    try { if (new FileInfo(r.Path).Length <= 32L * 1024 * 1024) sha = ScanEngine.ComputeFileSha256(r.Path); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                sb.AppendLine(string.Format("{0}. {1}", i + 1, r.Path));
                sb.AppendLine("   Mối đe dọa : " + r.ThreatText);
                sb.AppendLine("   Mức độ     : " + Theme.LevelText(r.Level));
                sb.AppendLine("   Trạng thái : " + r.StatusText);
                sb.AppendLine("   SHA256     : " + (string.IsNullOrEmpty(sha) ? "(chưa tính)" : sha));
                if (r.VtReport != null)
                    sb.AppendLine("   VirusTotal : " + r.VtReport.Summary()
                        + (string.IsNullOrEmpty(r.VtLink) ? string.Empty : "  |  " + r.VtLink));
                sb.AppendLine();
            }
            sb.AppendLine("============================================================");
            sb.AppendLine("Ghi chú: báo cáo do app tạo từ dữ liệu thật của phiên quét (không phải số liệu mẫu).");
            sb.AppendLine("Định dạng TXT vì project chưa có thư viện xuất PDF.");
            return sb.ToString();
        }
    }
}












