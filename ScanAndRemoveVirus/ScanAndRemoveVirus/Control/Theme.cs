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

        // Header xanh nhạt chữ xanh đậm brand + dòng xen kẽ nhạt: áp cho mọi DataGridView
        public static void StyleGrid(DataGridView g)
        {
            g.EnableHeadersVisualStyles = false;
            g.Font = BodyFont;
            g.ColumnHeadersDefaultCellStyle.BackColor = BlueFaint;
            g.ColumnHeadersDefaultCellStyle.ForeColor = BlueDark;
            g.ColumnHeadersDefaultCellStyle.Font = BoldFont;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.BackgroundColor = PageBg;
            g.BorderStyle = BorderStyle.None;
            g.RowTemplate.DefaultCellStyle.ForeColor = TextDark;
            AlternatingRows(g);
            g.GridColor = Line;
            g.DefaultCellStyle.SelectionBackColor = BlueSoft;
            g.DefaultCellStyle.SelectionForeColor = TextDark;
        }

        static void AlternatingRows(DataGridView g)
        {
            // tô xen kẽ theo màu nền hiện hành của dòng mặc định
            g.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(250, 251, 252);
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
    }
}
