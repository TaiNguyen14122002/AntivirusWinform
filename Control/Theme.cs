using System;
using System.Drawing;
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

        // ---- Trung tính ----
        public static readonly Color TextDark = Color.FromArgb(31, 41, 55);
        public static readonly Color TextMid = Color.FromArgb(55, 65, 81);
        public static readonly Color TextGray = Color.FromArgb(107, 114, 128);
        public static readonly Color Line = Color.FromArgb(229, 231, 235);
        public static readonly Color ChipGray = Color.FromArgb(243, 244, 246);
        public static readonly Color ChipGrayText = Color.FromArgb(170, 174, 180);
        public static readonly Color PageBg = Color.White;

        public static readonly Font BodyFont = new Font("Segoe UI", 9.75F, FontStyle.Regular);
        public static readonly Font BoldFont = new Font("Segoe UI", 9.75F, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        // Ngôn ngữ thiết kế chuẩn (khởi đầu từ tab Lịch sử): header trang + card + lưới dày
        public static readonly Font PageTitleFont = new Font("Segoe UI", 18F, FontStyle.Bold);
        public static readonly Font PageSubFont = new Font("Segoe UI", 9.75F, FontStyle.Regular);
        public static readonly Font CardTitleFont = new Font("Segoe UI", 10.125F, FontStyle.Bold);
        public static readonly Font HintFont = new Font("Segoe UI", 8.25F, FontStyle.Italic);

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

        // Card/GroupBox chuẩn: chữ đậm Xanh brand (một nguồn, ghi đè designer)
        public static void StyleCard(params GroupBox[] cards)
        {
            foreach (var c in cards)
            {
                if (c == null) continue;
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
            g.CellPainting += StripFocusRing;
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
        public enum BtnRole { Primary, Cancel, Action, Neutral, Danger }
        public static readonly Font ButtonFont = new Font("Segoe UI", 9.75F, FontStyle.Bold);

        public static void StyleButton(Button b, BtnRole role)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.Font = ButtonFont;
            b.Cursor = Cursors.Hand;
            ApplyRole(b, role, false);
            bool[] hovered = { false };
            b.EnabledChanged += delegate { ApplyRole(b, role, hovered[0]); };
            b.MouseEnter += delegate { hovered[0] = true; ApplyRole(b, role, true); };
            b.MouseLeave += delegate { hovered[0] = false; ApplyRole(b, role, false); };
        }

        private static void ApplyRole(Button b, BtnRole role, bool hover)
        {
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
            b.Font = new Font("Segoe UI", 10.125F, active ? FontStyle.Bold : FontStyle.Regular);
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
                    return Green;
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
            root.MinimumSize = new Size(minWidth, minHeight);
        }
    }
}
