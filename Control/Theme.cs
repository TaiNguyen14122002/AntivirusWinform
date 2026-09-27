using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// BẢNG MÀU DUY NHẤT của toàn app (single source of truth).
    /// Brand blue #0A56D8 + 4 nấc phụ; ngữ nghĩa:
    ///  Xanh dương = hành động chính / Xanh lá = an toàn·Bật /
    ///  Đỏ = đe dọa·Tắt·xóa / Hổ phách = cần lưu ý / Xám = phụ trợ.
    /// Mọi file designer + code-behind PHẢI tham chiếu Theme.X, không hard-code RGB.
    /// </summary>
    public static class Theme
    {
        // ---- Brand blue scale (#0A56D8) ----
        public static readonly Color Blue = Color.FromArgb(10, 86, 216);        // primary
        public static readonly Color BlueDark = Color.FromArgb(10, 62, 140);    // header text / brand title
        public static readonly Color BlueSoft = Color.FromArgb(205, 224, 250);  // border / hover
        public static readonly Color BlueFaint = Color.FromArgb(232, 241, 252); // sidebar idle / grid header bg
        public static readonly Color BlueTint = Color.FromArgb(240, 246, 254);  // chip nền xanh nhạt

        // ---- Trạng thái ----
        public static readonly Color Green = Color.FromArgb(22, 163, 74);
        public static readonly Color Red = Color.FromArgb(220, 38, 38);
        public static readonly Color RedText = Color.FromArgb(185, 28, 28);
        public static readonly Color RedSoft = Color.FromArgb(252, 210, 210);
        public static readonly Color RedTint = Color.FromArgb(254, 243, 243);
        public static readonly Color Amber = Color.FromArgb(217, 119, 6);

        // ---- Token bổ sung cho UcTongQuan (SPEC-UcTongQuan.md §2) ----
        public static readonly Color GreenSoft = Color.FromArgb(209, 240, 215);   // viền chip xanh
        public static readonly Color GreenTint = Color.FromArgb(240, 253, 244);  // nền chip xanh nhạt
        public static readonly Color AmberSoft = Color.FromArgb(250, 231, 199);   // viền chip hổ phách
        public static readonly Color AmberTint = Color.FromArgb(255, 250, 240);  // nền chip hổ phách
        public static readonly Color WarningMedium = Color.FromArgb(245, 166, 35); // badge "Trung bình"
        public static readonly Color WarningLow = Color.FromArgb(241, 196, 15);    // badge "Thấp"

        // ---- Trung tính ----
        public static readonly Color TextDark = Color.FromArgb(31, 41, 55);
        public static readonly Color TextMid = Color.FromArgb(55, 65, 81);
        public static readonly Color TextGray = Color.FromArgb(107, 114, 128);
        public static readonly Color Line = Color.FromArgb(229, 231, 235);
        public static readonly Color ChipGray = Color.FromArgb(243, 244, 246);
        // (170,174,180) trên nền ChipGray chỉ đạt ~2:1 — chữ nút bị vô hiệu gần như tàng hình
        // (thấy rõ ở 3 nút "Khôi phục/Khôi phục tất cả/Xóa vĩnh viễn" khi chưa chọn dòng nào).
        // Hạ xuống mức đọc được mà vẫn rõ là đang tắt.
        public static readonly Color ChipGrayText = Color.FromArgb(148, 153, 161);
        public static readonly Color PageBg = Color.White;

        public static readonly Font BodyFont = new Font("Segoe UI", 9.75F, FontStyle.Regular);
        public static readonly Font BodyBigFont = new Font("Segoe UI", 11.25F, FontStyle.Regular);  // hàng danh sách thoáng (Hoạt động gần đây)
        public static readonly Font BoldFont = new Font("Segoe UI", 9.75F, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        // Ngôn ngữ thiết kế chuẩn (khởi đầu từ tab Lịch sử): header trang + card + lưới dày
        public static readonly Font PageTitleFont = new Font("Segoe UI", 18F, FontStyle.Bold);
        public static readonly Font PageSubFont = new Font("Segoe UI", 9.75F, FontStyle.Regular);
        public static readonly Font CardTitleFont = new Font("Segoe UI", 10.125F, FontStyle.Bold);
        public static readonly Font HintFont = new Font("Segoe UI", 8.25F, FontStyle.Italic);
        // Cỡ chữ dùng cho các màn Tổng quan mới (hero, số liệu lớn, khoá tab)
        public static readonly Font HeroTitleFont = new Font("Segoe UI", 15.75F, FontStyle.Bold);
        public static readonly Font StatValueFont = new Font("Segoe UI", 18F, FontStyle.Bold);
        public static readonly Font BadgeFont = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.25F, FontStyle.Regular);

        /// <summary>Mức độ đe dọa — suy ra từ loại phát hiện (SPEC-UcTongQuan.md §6.3).</summary>
        public enum ThreatLevel { High, Medium, Low }

        /// <summary>Nhãn hiển thị của badge mức độ: Cao / Trung bình / Thấp.</summary>
        public static string LevelText(ThreatLevel level)
        {
            switch (level)
            {
                case ThreatLevel.High: return "Cao";
                case ThreatLevel.Medium: return "Trung bình";
                default: return "Thấp";
            }
        }

        /// <summary>Cặp màu (nền, chữ/viền) của badge mức độ — một nguồn duy nhất.</summary>
        public static void LevelColors(ThreatLevel level, out Color soft, out Color strong)
        {
            switch (level)
            {
                case ThreatLevel.High: soft = RedTint; strong = RedText; break;
                case ThreatLevel.Medium: soft = AmberTint; strong = WarningMedium; break;
                default: soft = ChipGray; strong = Amber; break;
            }
        }

        /// <summary>Tô ô "Mức độ" trong bảng theo enum ThreatLevel.</summary>
        public static void PaintBadgeCell(DataGridViewCellFormattingEventArgs e, ThreatLevel level)
        {
            Color soft, strong;
            LevelColors(level, out soft, out strong);
            e.CellStyle.BackColor = soft;
            e.CellStyle.ForeColor = strong;
            e.CellStyle.Font = BadgeFont;
            e.CellStyle.SelectionBackColor = soft;
            e.CellStyle.SelectionForeColor = strong;
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        /// <summary>Label dạng "pill" (badge) theo mức độ đe dọa.</summary>
        public static void StyleBadge(Label lbl, ThreatLevel level)
        {
            Color soft, strong;
            LevelColors(level, out soft, out strong);
            lbl.AutoSize = false;
            lbl.Font = BadgeFont;
            lbl.BackColor = soft;
            lbl.ForeColor = strong;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Text = LevelText(level);
        }

        /// <summary>Thẻ chọn chế độ quét (radio dạng card) — viền/nền đổi theo trạng thái chọn.</summary>
        public static void StyleRadioCard(RadioButton card, bool selected)
        {
            card.FlatStyle = FlatStyle.Flat;
            card.UseVisualStyleBackColor = false;
            card.Appearance = Appearance.Button;
            card.TextAlign = ContentAlignment.MiddleLeft;
            card.Font = ButtonFont;
            card.Cursor = Cursors.Hand;
            card.BackColor = selected ? BlueTint : PageBg;
            card.ForeColor = selected ? BlueDark : TextMid;
            card.FlatAppearance.BorderSize = selected ? 2 : 1;
            card.FlatAppearance.BorderColor = selected ? Blue : Line;
            card.FlatAppearance.CheckedBackColor = BlueTint;
            card.FlatAppearance.MouseOverBackColor = BlueTint;
            card.Padding = new Padding(10, 0, 6, 0);
        }

        // =====================================================================
        // TIỆN ÍCH CHỐNG LỖI VẼ (một nguồn duy nhất — dùng cho mọi tab)
        // =====================================================================

        /// <summary>PropertyInfo của <c>Control.DoubleBuffered</c> (protected) — tra MỘT lần.
        /// Viết đủ <c>System.Windows.Forms.Control</c> vì trong namespace này tên "Control" bị chính
        /// namespace <c>ScanAndRemoveVirus.Control</c> che mất.</summary>
        static readonly PropertyInfo pDoubleBuffered =
            typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>Phương thức protected <c>Control.SetStyle</c> — dùng làm đường dự phòng.</summary>
        static readonly MethodInfo mSetStyle =
            typeof(System.Windows.Forms.Control).GetMethod("SetStyle",
                BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Bật Double Buffered cho control KHÔNG thuộc quyền sửa kiểu (Panel/DataGridView do Designer
        /// tạo, hoặc UserControl do tab khác sở hữu). <c>DoubleBuffered</c> là thuộc tính protected của
        /// Control nên chỉ chạm được qua reflection; ở đây KHÔNG đổi kiểu control, KHÔNG sửa file
        /// Designer, chỉ đọc/ghi đúng thuộc tính đó (kèm đường dự phòng gọi thẳng <c>SetStyle</c>).
        /// CHỈ gọi cho khung chứa bị vẽ lại liên tục (lưới dữ liệu cuộn, vùng đổi trang, dải có viền
        /// vẽ tay) — mỗi buffer tốn thêm một bitmap bằng kích thước control nên không bật tràn lan.
        /// </summary>
        public static void BatDoubleBuffer(System.Windows.Forms.Control c)
        {
            if (c == null) return;
            try
            {
                if (pDoubleBuffered != null && !(bool)pDoubleBuffered.GetValue(c, null))
                {
                    pDoubleBuffered.SetValue(c, true, null);
                    return;
                }
                if (pDoubleBuffered != null) return;
            }
            catch (Exception)
            {
                // Ghi thuộc tính không được -> thử đường dự phòng bên dưới.
            }
            try
            {
                if (mSetStyle != null)
                    mSetStyle.Invoke(c, new object[]
                    {
                        ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true
                    });
            }
            catch (Exception)
            {
                // Nền tảng không cho ghi -> bỏ qua: giao diện vẫn chạy, chỉ còn nhấp nháy như trước.
            }
        }

        /// <summary>Đánh dấu control đã được gắn handler vẽ — để gọi lại Style* không cộng dồn handler.</summary>
        static readonly ConditionalWeakTable<System.Windows.Forms.Control, object> daGanHandler =
            new ConditionalWeakTable<System.Windows.Forms.Control, object>();

        /// <summary>Gắn handler MỘT LẦN cho control (gọi lại trên cùng control là no-op).</summary>
        static bool GanLanDau(System.Windows.Forms.Control c)
        {
            object da;
            if (daGanHandler.TryGetValue(c, out da)) return false;
            daGanHandler.Add(c, new object());
            return true;
        }

        /// <summary>Viền 1px quanh hộp info-box / dải loading — dùng chung cho cả hai Style* bên dưới.</summary>
        static void VeVienNhat(System.Windows.Forms.Control host, PaintEventArgs e)
        {
            using (var pen = new Pen(BlueSoft))
                e.Graphics.DrawRectangle(pen, 0, 0, host.Width - 1, host.Height - 1);
        }

        /// <summary>Hộp "lưu ý / info-box" (nền xanh nhạt + viền xanh) quanh 1 Label nội dung.</summary>
        public static void StyleInfoBox(Panel host, Label text)
        {
            if (host == null) return;
            host.BackColor = BlueTint;
            host.Padding = new Padding(10, 8, 10, 8);
            // Chỉ gắn MỘT lần: gọi lại StyleInfoBox trên cùng panel mà cộng dồn handler thì viền
            // được vẽ 2–3 lần (nét đậm bất thường) và mỗi lần vẽ lại chạy thừa một lượt.
            if (GanLanDau(host)) host.Paint += delegate(object s, PaintEventArgs e) { VeVienNhat(host, e); };
            BatDoubleBuffer(host);
            if (text == null) return;
            // Lưu ý thứ tự: khối này ghi đè Font/ForeColor của nhãn — tiêu đề nào cần đậm thì
            // phải gán lại SAU lời gọi này (xem UcTongQuan.QuetNangCao.cs, hộp "Lưu ý").
            text.BackColor = BlueTint;
            text.ForeColor = TextMid;
            text.Font = SmallFont;
        }

        /// <summary>
        /// (25/09/2026, lần 9) **Dải loading quét** ở tab Tổng quan: nền xanh rất nhạt + viền xanh
        /// mảnh (kiểu info-box), chữ trạng thái xanh đậm/xám và vòng xoay đồng màu thương hiệu.
        /// Ở đây cùng với các `Style*` khác để màu sắc chỉ có MỘT nguồn (không set rải trong Designer).
        /// </summary>
        public static void StyleLoadingStrip(Panel strip, LoadingSpinner spinner, Label title, Label detail, Button cancel)
        {
            if (strip != null)
            {
                strip.BackColor = BlueTint;
                // Cùng cơ chế với StyleInfoBox: gắn viền MỘT lần (gọi lại không cộng dồn handler).
                if (GanLanDau(strip)) strip.Paint += delegate(object s, PaintEventArgs e) { VeVienNhat(strip, e); };
                // Dải này co giãn theo cửa sổ trong lúc quét -> tô nền + viền vẽ tay rất dễ nhấp nháy.
                BatDoubleBuffer(strip);
            }
            if (spinner != null)
            {
                spinner.BackColor = BlueTint;
                spinner.MauVong = BlueSoft;
                spinner.MauQuay = Blue;
            }
            if (title != null)
            {
                title.BackColor = BlueTint;
                title.ForeColor = BlueDark;
                title.Font = BoldFont;
            }
            if (detail != null)
            {
                detail.BackColor = BlueTint;
                detail.ForeColor = TextGray;
                detail.Font = SmallFont;
            }
            if (cancel != null) StyleButton(cancel, BtnRole.Cancel);
        }

        /// <summary>LinkLabel đồng bộ tông xanh brand và luôn có con trỏ bàn tay.</summary>
        public static void StyleLinkLabel(LinkLabel lnk)
        {
            lnk.LinkColor = Blue;
            lnk.ActiveLinkColor = BlueDark;
            lnk.VisitedLinkColor = Blue;
            lnk.LinkBehavior = LinkBehavior.HoverUnderline;
            lnk.Cursor = Cursors.Hand;
            lnk.Font = BoldFont;
        }

        // Trang chuẩn hóa: tiêu đề 18B TextDark + phụ đề xám (mọi tab theo tab Lịch sử)
        public static void StylePageHeader(Label title, Label subtitle)
        {
            title.Font = PageTitleFont;
            title.ForeColor = TextDark;
            if (subtitle != null)
            {
                subtitle.Font = PageSubFont;
                subtitle.ForeColor = TextGray;
            }
        }

        // ================= TRANG LỊCH SỬ (ảnh tham chiếu 26/09/2026 — README §3) =================
        // CHỈ dùng cho UcLichSu: tiêu đề lớn hơn chuẩn chung (18pt) theo ảnh = 25pt đậm + phụ đề 10.5;
        // lưới 7 cột với header/hàng 48px và đường kẻ ngang xanh xám (không sọc xen kẽ).
        // Màu của ảnh được ánh xạ về token sẵn có: #F4F8FE -> BlueTint, #D8E2F1 -> BlueSoft,
        // #172338 -> TextDark, #52627C -> TextGray, #07843C -> Green, #E11D20 -> Red, #D98200 -> Amber.
        public static readonly Font PageTitleLargeFont = new Font("Segoe UI", 25F, FontStyle.Bold);
        public static readonly Font PageSubLargeFont = new Font("Segoe UI", 10.5F, FontStyle.Regular);

        /// <summary>Header trang Lịch sử: tiêu đề 25B TextDark + phụ đề 10.5 xám xanh.</summary>
        public static void StyleHistoryHeader(Label title, Label subtitle)
        {
            if (title != null)
            {
                title.Font = PageTitleLargeFont;
                title.ForeColor = TextDark;
            }
            if (subtitle != null)
            {
                subtitle.Font = PageSubLargeFont;
                subtitle.ForeColor = TextGray;
            }
        }

        /// <summary>Nhãn nhỏ phía trên mỗi điều khiển trong hàng bộ lọc (Từ ngày / Đến ngày / Loại quét).</summary>
        public static void StyleFilterLabel(Label lbl)
        {
            if (lbl == null) return;
            lbl.Font = SmallFont;
            lbl.ForeColor = TextGray;
        }

        /// <summary>Điều khiển trong hàng bộ lọc: nền trắng, chữ TextDark, combo kiểu DropDownList phẳng.</summary>
        public static void StyleFilterInput(System.Windows.Forms.Control c)
        {
            if (c == null) return;
            c.BackColor = PageBg;
            c.ForeColor = TextDark;
            c.Font = BodyFont;
            var combo = c as ComboBox;
            if (combo != null)
            {
                combo.FlatStyle = FlatStyle.Flat;
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        /// <summary>
        /// Lưới tab Lịch sử: nền trắng, header 48px (xanh rất nhạt + chữ đậm), hàng 48px,
        /// đường kẻ ngang xanh xám mảnh và KHÔNG sọc xen kẽ như các bảng khác (ảnh 26/09/2026).
        /// </summary>
        public static void StyleHistoryGrid(DataGridView g)
        {
            StyleGrid(g);
            g.ColumnHeadersHeight = 48;
            g.RowTemplate.Height = 48;
            foreach (DataGridViewRow row in g.Rows) row.Height = 48;
            g.GridColor = BlueSoft;
            g.AlternatingRowsDefaultCellStyle.BackColor = PageBg;
            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = PageBg;
            g.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextDark;
            g.DefaultCellStyle.SelectionBackColor = PageBg;
            g.DefaultCellStyle.SelectionForeColor = TextDark;
            g.ScrollBars = ScrollBars.Both; // cửa sổ hẹp -> cuộn ngang thay vì cắt chữ (README §3.6)
        }

        /// <summary>Cột nút "Xem" của lưới Lịch sử: nút phẳng, nền trắng (viền/bo góc vẽ trong UcLichSu).</summary>
        public static void StyleHistoryGridButton(DataGridViewButtonColumn col)
        {
            if (col == null) return;
            col.FlatStyle = FlatStyle.Flat;
            col.DefaultCellStyle.BackColor = PageBg;
            col.DefaultCellStyle.ForeColor = BlueDark;
            col.DefaultCellStyle.SelectionBackColor = PageBg;
            col.DefaultCellStyle.SelectionForeColor = BlueDark;
        }

        /// <summary>
        /// Đường bo góc cho chỗ VẼ TAY (nút/nhãn) — giữ nguyên hình học đang dùng ở lưới Lịch sử,
        /// nhưng thân hàm nay gọi thuật toán DUY NHẤT <see cref="UiKit.Round"/>: trước đây đây là bản
        /// sao AddArc thứ hai (khác 1px và khác cách kẹp bán kính) nên rất dễ lệch nhau khi chỉ sửa một
        /// bên. Không còn chỗ nào tự AddArc cho hình bo góc.
        /// Vì sao lùi 1px ở phải/dưới: với hình chữ nhật (x, y, W, H), path đặt đúng ở "x + W" /
        /// "y + H" là nằm NGOÀI vùng vẽ (pixel cuối cùng là x + W - 1) nên nửa nét bút 1px ở hai cạnh
        /// đó bị cắt -> viền mảnh hơn ở phải/dưới. Lùi 1px (giống hệt cách <see cref="UiKit.Fill"/> tự
        /// lùi trước khi vẽ nền + viền) để cả 4 cạnh đều có viền 1px đủ nét như nhau.
        /// </summary>
        public static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            return UiKit.Round(new Rectangle(r.X, r.Y,
                Math.Max(1, r.Width - 1), Math.Max(1, r.Height - 1)), radius);
        }

        // Card/GroupBox chuẩn: chữ đậm Xanh brand (một nguồn, ghi đè designer)
        public static void StyleCard(params GroupBox[] cards)
        {
            foreach (var c in cards)
            {
                if (c == null) continue;
                // UiGroup tự vẽ tiêu đề -> chỉ cần giao đúng font/màu cho phần vẽ đó.
                // Trước đây hàm này gán thẳng c.Font: với GroupBox thì đó là font tiêu đề,
                // nhưng vì Font là thứ control CON thừa hưởng nên mọi nhãn bên trong thẻ
                // "Thông tin bảo vệ" bị đổi thành đậm theo.
                var ug = c as UiGroup;
                if (ug != null) { ug.HeadFont = CardHeadFont; ug.HeadColor = TextDark; continue; }
                c.Font = CardTitleFont;
                c.ForeColor = BlueDark;
            }
        }

        // ==== COMMON CHO TOÀN BỘ TABLE (một nguồn duy nhất cho mọi DataGridView) ====
        // Header xanh nhạt chữ xanh đậm brand + dòng xen kẽ nhạt + hành vi fill/full-row:
        // mọi bảng trong app PHẢI đi qua hàm này — designer chỉ giữ bố cục và cột.
        public static void StyleGrid(DataGridView g)
        {
            g.EnableHeadersVisualStyles = false;
            g.Font = BodyFont;
            g.ColumnHeadersDefaultCellStyle.BackColor = BlueFaint;
            g.ColumnHeadersDefaultCellStyle.ForeColor = BlueDark;
            g.ColumnHeadersDefaultCellStyle.Font = BoldFont;
            // Ô header của CỘT ĐANG CHỌN được vẽ bằng màu Selection*, mặc định là
            // SystemColors.Highlight (xanh đặc) + HighlightText (chữ trắng) — đó là lý do
            // duy nhất cột đầu của bảng Bảo vệ xanh lè còn các cột kia thì không.
            // Ép về đúng màu header để mọi cột trông như nhau.
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = BlueFaint;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = BlueDark;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 40;
            g.BackgroundColor = PageBg;
            g.BorderStyle = BorderStyle.None;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.RowHeadersVisible = false;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.AllowUserToOrderColumns = false;
            g.RowTemplate.DefaultCellStyle.ForeColor = TextDark;
            g.RowTemplate.Height = 34;
            // các hàng đã nạp trước khi StyleGrid chạy cũng phải theo đúng chiều cao chung
            foreach (DataGridViewRow row in g.Rows)
                row.Height = 34;
            AlternatingRows(g);
            g.GridColor = Line;
            // BỎ hiệu ứng chọn dòng: bấm vào dòng không đổi màu nền/chữ (chọn vẫn hoạt động ngầm)
            g.DefaultCellStyle.SelectionBackColor = PageBg;
            g.DefaultCellStyle.SelectionForeColor = TextDark;
            // và xóa cả KHUNG FOCUS nét đứt quanh ô vừa bấm — artifact highlight cuối cùng
            // Chỉ gắn MỘT lần: StyleHistoryGrid/StyleGrid có thể chạy lại trên cùng lưới, cộng dồn
            // handler thì mỗi ô được vẽ lại thêm một lượt.
            if (GanLanDau(g)) g.CellPainting += StripFocusRing;
            // Bảng cuộn tay từng dòng/đổi trang liên tục -> không double buffer là nhấp nháy thấy rõ.
            BatDoubleBuffer(g);
        }

        // Vẽ lại ô bình thường nhưng bỏ phần Focus (khung nét đứt trên ô current cell)
        static void StripFocusRing(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if ((e.PaintParts & DataGridViewPaintParts.Focus) == 0) return;
            e.Paint(e.ClipBounds, e.PaintParts & ~DataGridViewPaintParts.Focus);
            e.Handled = true;
        }

        // ==== CỘT "CHỌN" (CHECKBOX) DÙNG CHUNG CHO MỌI TABLE ====
        // Header ô vuông + tick hàng loạt, giống chuẩn tab Lịch sử: các bảng Hành động,
        // Cách ly, Lịch sử đều dùng chung một nguồn duy nhất tại đây.
        public static readonly Font PickGlyphFont = new Font("Segoe MDL2 Assets", 11F, FontStyle.Regular, GraphicsUnit.Point);
        const string PickBoxEmpty = "\uE739";    // CheckBox rỗng
        const string PickBoxChecked = "\uE73A";  // CheckBox đã bật hết
        const string PickBoxPartial = "\uE73D";  // bật một phần (indeterminate)

        // Trạng thái tick của bảng: 0 = không dòng nào, 1 = một phần, 2 = tất cả
        public static int PickState(DataGridView g, int pickCol)
        {
            int n = g.Rows.Count;
            if (n == 0) return 0;
            int p = 0;
            foreach (DataGridViewRow r in g.Rows)
                if (r.Cells[pickCol].Value is bool b && b) p++;
            return p == 0 ? 0 : (p == n ? 2 : 1);
        }

        // Tick / bỏ tick TOÀN BỘ dòng + vẽ lại header icon
        public static void PickAll(DataGridView g, int pickCol, bool on)
        {
            foreach (DataGridViewRow r in g.Rows) r.Cells[pickCol].Value = on;
            InvalidatePickHeader(g);
        }

        // Vẽ lại vùng header để icon ô vuông cập nhật theo trạng thái tick
        public static void InvalidatePickHeader(DataGridView g)
        {
            g.Invalidate(new Rectangle(0, 0, g.Width, g.ColumnHeadersHeight));
        }

        // Header ô vuông: vẽ glyph theo trạng thái tick (click header do từng bảng tự xử lý)
        public sealed class SelectAllHeaderCell : DataGridViewColumnHeaderCell
        {
            readonly Func<int> state;
            public SelectAllHeaderCell(Func<int> state) { this.state = state; }

            protected override void Paint(Graphics g, Rectangle clipBounds, Rectangle cellBounds,
                int rowIndex, DataGridViewElementStates cellState, object formattedValue, object value,
                string errorText, DataGridViewCellStyle cellStyle,
                DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
            {
                base.Paint(g, clipBounds, cellBounds, rowIndex, cellState, formattedValue, value,
                    errorText, cellStyle, advancedBorderStyle,
                    paintParts & ~(DataGridViewPaintParts.ContentForeground | DataGridViewPaintParts.Focus));
                int st = state();
                TextRenderer.DrawText(g, st == 2 ? PickBoxChecked : st == 1 ? PickBoxPartial : PickBoxEmpty,
                    PickGlyphFont, cellBounds, st == 0 ? TextGray : Blue,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        static void AlternatingRows(DataGridView g)
        {
            // tô xen kẽ theo màu nền hiện hành của dòng mặc định;
            // dòng được CHỌN cũng giữ nguyên nền xen kẽ -> bấm không đổi màu gì
            Color tint = Color.FromArgb(250, 251, 252);
            g.AlternatingRowsDefaultCellStyle.BackColor = tint;
            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = tint;
            g.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextDark;
        }

        // ==== HỆ THỐNG NÚT THỐNG NHẤT ====
        // Mọi nút trong app dùng chung hình hài (font, border, cursor);
        // chỉ khác nhau theo VAI TRÒ semantics.
        public enum BtnRole { Primary, Secondary, Cancel, Action, Neutral, Danger }
        public static readonly Font ButtonFont = new Font("Segoe UI", 9.75F, FontStyle.Bold);

        /// <summary>
        /// Trạng thái đang áp của một nút (vai trò + đang rê chuột?) — lưu theo nút để
        /// <see cref="StyleButton"/> gọi lại chỉ ĐỔI VAI TRÒ chứ không đăng ký thêm sự kiện.
        /// </summary>
        sealed class BtnState
        {
            public BtnRole Role;
            public bool Hover;
            public bool Hooked;
        }

        // ConditionalWeakTable: khoá là nút, KHÔNG giữ nút sống (nút bị Dispose là mục tự mất).
        static readonly ConditionalWeakTable<Button, BtnState> nutStates =
            new ConditionalWeakTable<Button, BtnState>();

        public static void StyleButton(Button b, BtnRole role)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.Font = ButtonFont;
            b.Cursor = Cursors.Hand;

            // Một nút chỉ ĐĂNG KÝ SỰ KIỆN MỘT LẦN. Trước đây mỗi lời gọi cộng thêm 1 handler
            // EnabledChanged + 2 handler chuột: UpdateThreatUi() gọi lại 4 nút sau MỖI thay đổi của
            // bảng đe dọa, sau vài chục lượt là hàng trăm handler cho một nút — mỗi lần rê chuột
            // chạy hết chừng đó lượt áp style (giật + nhấp nháy). Nay vai trò được LƯU lại, handler
            // đọc state tại thời điểm chạy nên luôn áp đúng vai trò mới nhất.
            BtnState st = nutStates.GetOrCreateValue(b);
            st.Role = role;
            if (!st.Hooked)
            {
                st.Hooked = true;
                b.EnabledChanged += delegate { ApplyRole(b, st); };
                b.MouseEnter += delegate { st.Hover = true; ApplyRole(b, st); };
                b.MouseLeave += delegate { st.Hover = false; ApplyRole(b, st); };
            }
            ApplyRole(b, st);
        }

        static void ApplyRole(Button b, BtnState st)
        {
            ApplyRole(b, st.Role, st.Hover);
        }

        private static void ApplyRole(Button b, BtnRole role, bool hover)
        {
            // Nút bo góc (UiButton) tự vẽ theo Variant. Nếu chạy tiếp phần FlatAppearance
            // bên dưới, nút sẽ bị tô thêm một nền VUÔNG đè lên hình bo góc — và vì
            // StyleButton không hề chạm tới UiButton.Kind, nút phụ sẽ giữ nguyên màu
            // Primary (xanh) của mặc định. Chuyển vai trò rồi dừng.
            var ub = b as UiButton;
            if (ub != null) { UiKit.Apply(ub, role); return; }

            if (!b.Enabled)
            {
                b.BackColor = ChipGray;
                b.ForeColor = ChipGrayText;
                b.FlatAppearance.BorderColor = Line;
                b.FlatAppearance.BorderSize = 1;
                return;
            }
            Color bg, fg, border;
            switch (role)
            {
                case BtnRole.Primary: // CTA chính: nền xanh brand, chữ trắng
                    bg = hover ? Color.FromArgb(9, 74, 186) : Blue; fg = Color.White; border = bg; break;
                case BtnRole.Secondary: // CTA phụ: nền trắng, viền + chữ xanh brand
                    bg = hover ? BlueTint : PageBg; fg = BlueDark; border = hover ? Blue : BlueSoft; break;
                case BtnRole.Cancel: // hành động dừng/hủy: nền đỏ, chữ trắng
                    bg = hover ? Color.FromArgb(196, 32, 32) : Red; fg = Color.White; border = bg; break;
                case BtnRole.Action: // thao tác khẳng định (cách ly/khôi phục): chip xanh nhạt
                    bg = hover ? BlueSoft : BlueTint; fg = BlueDark; border = BlueSoft; break;
                case BtnRole.Danger: // hủy destructive (xóa vĩnh viễn): chip đỏ nhạt
                    bg = hover ? Color.FromArgb(253, 226, 226) : RedTint; fg = Red; border = RedSoft; break;
                default: // Neutral: nút phụ xám
                    bg = hover ? Color.FromArgb(233, 235, 238) : ChipGray; fg = TextMid; border = Line; break;
            }
            b.BackColor = bg;
            b.ForeColor = fg;
            b.FlatAppearance.BorderColor = border; // solid: border==bg -> liền mạch không viền đôi
            b.FlatAppearance.BorderSize = 1;
        }

        public static void StyleNeutralButtons(params Button[] buttons)
        {
            foreach (Button b in buttons) StyleButton(b, BtnRole.Neutral);
        }

        // Nút điều hướng sidebar: cùng họ với hệ thống nút (Hand cursor, borderless phẳng)
        public static void StyleNav(Button b, bool active)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderSize = 0;
            b.Cursor = Cursors.Hand;
            // Token chung: trước đây new Font(...) mỗi lời gọi -> mỗi lượt là một GDI font mới
            // không bao giờ được giải phóng.
            b.Font = active ? NavBoldFont : NavFont;
            b.BackColor = active ? Blue : BlueFaint;
            b.ForeColor = active ? Color.White : Blue;
        }

        // Tô màu ô "Trạng thái/Kết quả" theo từ khóa ngữ nghĩa
        public static void PaintStatusCell(DataGridViewCellFormattingEventArgs e, string value)
        {
            Color? c = StatusColor(value);
            if (c == null) return;
            e.CellStyle.ForeColor = c.Value;
            e.CellStyle.Font = BoldFont;
            e.CellStyle.SelectionForeColor = c.Value;
        }

        public static Color? StatusColor(string value)
        {
            switch (value)
            {
                case "Bật":
                case "An toàn":
                case "Đã cập nhật":
                case "Không phát hiện": // kết quả phiên quét sạch của tab Lịch sử (ảnh 26/09/2026)
                    return Green;
                case "Đã cách ly": // chỉ hiển thị khi có bằng chứng thật trong sổ cách ly (README §3.3)
                    return Amber;
                case "Tắt":
                case "Phát hiện mối đe dọa":
                case "Cần xử lý":
                    return Red;
                case "Thành công":
                    return Blue;
                default:
                    return null;
            }
        }

        // ==== RESPONSIVE ====
        // Trang co giãn theo cửa sổ; khi nhỏ hơn kích thước tối thiểu thì CUỘN thay vì cắt nội dung
        public static void ScrollablePage(UserControl page, System.Windows.Forms.Control root, int minWidth, int minHeight)
        {
            page.AutoScroll = true;
            // Nhấp nháy khi chuyển tab / kéo cửa sổ: cả trang là một khung lớn nhiều control con,
            // để WinForms xoá nền rồi vẽ lại từng lớp là thấy rõ. Double buffer của CHÍNH trang
            // (UserControl thừa hưởng protected DoubleBuffered -> gọi qua Theme.BatDoubleBuffer).
            // Mọi tab đều đi qua hàm này nên chỉ cần bật ở MỘT chỗ.
            BatDoubleBuffer(page);
            // AutoScrollMinSize mới là thứ tạo ra thanh cuộn: nó nâng DisplayRectangle của
            // trang lên tối thiểu (minWidth × minHeight), và control Dock=Fill được xếp theo
            // DisplayRectangle nên nội dung giãn đủ chỗ rồi cuộn.
            // Chỉ đặt root.MinimumSize (cách cũ) KHÔNG có tác dụng: root đang Dock=Fill nên
            // luôn được ép bằng đúng vùng nhìn — thanh cuộn không bao giờ xuất hiện và phần
            // đáy của MỌI tab bị cắt (đo được: Tổng quan tràn +143px, Cài đặt +99px ở cửa sổ nhỏ).
            page.AutoScrollMinSize = new Size(minWidth, minHeight);
            root.MinimumSize = new Size(minWidth, minHeight);

            // Chiều cao vùng cuộn phải đủ cho phần nội dung KHÔNG co được (thẻ Cài đặt có 5
            // hàng cố định 40px). Đo lại mỗi lần xếp layout vì độ cao đó phụ thuộc bề rộng.
            Action sync = delegate
            {
                var want = new Size(minWidth, Math.Max(minHeight, MinContentHeight(root)));
                if (page.AutoScrollMinSize != want) page.AutoScrollMinSize = want;
            };
            root.Layout += delegate { sync(); };
            sync();
        }

        /// <summary>
        /// Chiều cao TỐI THIỂU mà nội dung thật sự cần — chỉ tính phần cứng:
        /// hàng Absolute của TableLayoutPanel cộng dồn, hàng Percent/AutoSize thì đi xuống
        /// control con xem bên trong có gì cứng không. Bảng và nhãn co giãn được nên góp 0.
        /// KHÔNG dùng PreferredSize ở đây: với hàng phần trăm nó trả về chính chiều cao hiện
        /// tại của bảng, nên nuôi số đó vào AutoScrollMinSize sẽ tự đẩy trang cao dần.
        /// </summary>
        static int MinContentHeight(System.Windows.Forms.Control c)
        {
            var t = c as TableLayoutPanel;
            if (t == null)
            {
                int inner = 0;
                foreach (System.Windows.Forms.Control k in c.Controls)
                    inner = Math.Max(inner, MinContentHeight(k));
                // 0 = control lá (bảng, nhãn...) co giãn được -> không đòi thêm chỗ
                return inner == 0 ? 0 : inner + c.Padding.Vertical;
            }
            int fixedSum = 0;      // hàng Absolute/AutoSize: cộng thẳng
            float weightSum = 0f;  // tổng trọng số các hàng Percent
            float worst = 0f;      // max(nhu cầu / trọng số) — hàng đòi nhiều nhất

            for (int r = 0; r < t.RowCount; r++)
            {
                int need = 0;
                foreach (System.Windows.Forms.Control k in t.Controls)
                    if (t.GetRow(k) == r) need = Math.Max(need, MinContentHeight(k) + k.Margin.Vertical);

                RowStyle rs = r < t.RowStyles.Count ? t.RowStyles[r] : null;
                if (rs == null || rs.SizeType != SizeType.Percent)
                {
                    fixedSum += Math.Max(rs == null ? 0 : (int)rs.Height, need);
                    continue;
                }
                // Hàng Percent KHÔNG cộng dồn: hai hàng 50% cùng đòi 286px thì bảng chỉ cần
                // 572px, không phải 286. Cộng thẳng sẽ ra số nhỏ hơn thực tế và thẻ vẫn bị bóp.
                weightSum += rs.Height;
                if (rs.Height > 0f) worst = Math.Max(worst, need / rs.Height);
            }
            return fixedSum + (int)Math.Ceiling(worst * weightSum) + t.Padding.Vertical;
        }

        // =====================================================================
        // NGÔN NGỮ THIẾT KẾ MỚI (theo mockup Design/*.jpg) — bổ sung, không thay thế
        // Các token dưới đây phục vụ bộ control vẽ tay ở UiKit.cs.
        // =====================================================================

        // ---- Nền & viền ----
        public static readonly Color SidebarBg = Color.FromArgb(248, 250, 252);   // nền sidebar
        public static readonly Color NavActiveBg = Color.FromArgb(232, 240, 254);  // mục sidebar đang chọn
        public static readonly Color NavHoverBg = Color.FromArgb(241, 245, 249);   // mục sidebar rê chuột
        public static readonly Color CardBorder = Color.FromArgb(229, 231, 235);   // viền card 1px
        public static readonly Color Divider = Color.FromArgb(241, 245, 249);      // đường kẻ mảnh trong card
        public static readonly Color InputBg = Color.White;
        public static readonly Color InputBorder = Color.FromArgb(209, 213, 219);
        public static readonly Color HeaderBg = Color.FromArgb(249, 250, 251);     // header bảng

        // ---- Bán kính bo góc (một nguồn duy nhất) ----
        public const int RadiusCard = 10;
        public const int RadiusButton = 8;
        public const int RadiusInput = 8;
        public const int RadiusPill = 999;   // bo tròn hoàn toàn

        // ---- Typography theo mockup ----
        public static readonly Font H1Font = new Font("Segoe UI", 21.75F, FontStyle.Bold);   // "Chi tiết kết quả quét"
        public static readonly Font H2Font = new Font("Segoe UI", 15F, FontStyle.Bold);      // tiêu đề khối lớn
        public static readonly Font HeroBigFont = new Font("Segoe UI", 19.5F, FontStyle.Bold); // "Máy tính của bạn được bảo vệ"
        public static readonly Font StatBigFont = new Font("Segoe UI", 23.25F, FontStyle.Bold); // số liệu lớn
        public static readonly Font CardHeadFont = new Font("Segoe UI", 11.25F, FontStyle.Bold); // tiêu đề card
        public static readonly Font FieldFont = new Font("Segoe UI", 9.75F, FontStyle.Regular);  // nhãn/giá trị
        public static readonly Font MonoFont = new Font("Consolas", 9.75F, FontStyle.Regular);
        // Mục điều hướng sidebar: bản thường + bản đậm (mục đang chọn). Một nguồn cho cả
        // Theme.StyleNav (nút sidebar kiểu cũ) lẫn UiKit.UiNavItem (sidebar hiện tại) — trước đây
        // mỗi bên tự new Font(...), riêng UiNavItem còn new Font(Font, Bold) MỖI LẦN VẼ.
        public static readonly Font NavFont = new Font("Segoe UI", 10.125F, FontStyle.Regular);
        public static readonly Font NavBoldFont = new Font("Segoe UI", 10.125F, FontStyle.Bold);

        /// <summary>Màu chữ của một "pill" theo ngữ nghĩa (nền = bản *Tint*, chữ = bản *Text*).</summary>
        public enum PillKind { Danger, Warn, Low, Ok, Info, Neutral }

        /// <summary>Cặp (nền, chữ) của pill — một nguồn duy nhất cho mọi badge trong app.</summary>
        public static void PillColors(PillKind kind, out Color bg, out Color fg)
        {
            switch (kind)
            {
                case PillKind.Danger: bg = RedTint; fg = RedText; break;
                case PillKind.Warn: bg = AmberTint; fg = WarningMedium; break;
                case PillKind.Low: bg = Color.FromArgb(254, 249, 231); fg = Amber; break;
                case PillKind.Ok: bg = GreenTint; fg = Green; break;
                case PillKind.Info: bg = BlueTint; fg = BlueDark; break;
                default: bg = ChipGray; fg = TextMid; break;
            }
        }

        /// <summary>Pill cho mức độ đe dọa (Cao/Trung bình/Thấp) — dùng chung với LevelColors.</summary>
        public static PillKind PillOf(ThreatLevel level)
        {
            switch (level)
            {
                case ThreatLevel.High: return PillKind.Danger;
                case ThreatLevel.Medium: return PillKind.Warn;
                default: return PillKind.Low;
            }
        }
    }
}
