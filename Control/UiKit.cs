using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// BỘ CONTROL VẼ TAY dùng chung cho giao diện mới (theo mockup Design/*.jpg).
    /// Toàn bộ app trước đây chỉ dùng control mặc định của WinForms (vuông, không bo góc,
    /// không badge) nên không thể khớp mockup. File này cung cấp các "viên gạch" còn thiếu:
    ///  UiCard (thẻ bo góc) · UiPill (badge) · UiButton (nút bo góc) ·
    ///  UiNavItem (mục sidebar) · UiRadioCard (thẻ chọn chế độ quét) · UiFieldGrid (bảng nhãn/giá trị).
    /// Mọi màu/font lấy từ Theme — không hard-code RGB ở đây.
    /// </summary>
    public static class UiKit
    {
        /// <summary>Đường bo góc dùng chung cho mọi control (một nguồn duy nhất).</summary>
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            if (radius <= 0 || r.Width <= 0 || r.Height <= 0) { p.AddRectangle(r); return p; }
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>Vẽ một hình bo góc có nền + viền. Viền 0 = không vẽ viền.</summary>
        public static void Fill(Graphics g, Rectangle r, int radius, Color fill, Color border, float borderWidth)
        {
            r.Width -= 1; r.Height -= 1;                 // chừa 1px cho viền không bị cắt
            using (GraphicsPath p = Round(r, radius))
            {
                if (fill.A > 0)
                    using (var b = new SolidBrush(fill)) g.FillPath(b, p);
                if (borderWidth > 0 && border.A > 0)
                    using (var pen = new Pen(border, borderWidth)) g.DrawPath(pen, p);
            }
        }

        /// <summary>Đường kẻ mảnh 1px ngang (dùng giữa các khối trong card).</summary>
        public static Panel Sep(int height)
        {
            return new Panel { Height = height, BackColor = Theme.Divider, Dock = DockStyle.Top };
        }

        /// <summary>
        /// Áp vai trò nút của Theme lên UiButton — cầu nối duy nhất giữa hệ vai trò cũ
        /// (`Theme.StyleButton`, dùng ở mọi tab) và bộ nút bo góc mới. Nhờ vậy code nghiệp vụ
        /// vẫn đổi vai trò nút giữa chừng (vd "Quét ngay" ⇄ "Hủy quét") mà không cần biết UiButton.
        /// </summary>
        public static void Apply(UiButton b, Theme.BtnRole role)
        {
            if (b == null) return;
            switch (role)
            {
                case Theme.BtnRole.Primary: b.Kind = UiButton.Variant.Primary; break;
                case Theme.BtnRole.Cancel: b.Kind = UiButton.Variant.Cancel; break;
                case Theme.BtnRole.Danger: b.Kind = UiButton.Variant.Danger; break;
                case Theme.BtnRole.Neutral: b.Kind = UiButton.Variant.OutlineGray; break;
                case Theme.BtnRole.Action: b.Kind = UiButton.Variant.Outline; break;
                default: b.Kind = UiButton.Variant.Outline; break;   // Secondary
            }
            // UiButton tự vẽ chữ bằng Font của chính nó, mà InitializeComponent của mấy
            // designer cũ vẫn gán "Segoe UI 9.75 Regular" lên nút — chạy SAU hàm dựng nên
            // đè mất Theme.ButtonFont. Kết quả: nút tab Cách ly/Lịch sử nhạt, nút tab Cài
            // đặt đậm. Ép lại một nguồn duy nhất ở đây.
            b.Font = Theme.ButtonFont;
            b.Invalidate();
        }
    }

    // =====================================================================
    // UiCard — thẻ nền trắng, bo góc, viền 1px, có header (icon + tiêu đề + slot phải)
    // =====================================================================
    public class UiCard : Panel
    {
        readonly Panel header;
        readonly PictureBox headIcon;
        readonly Label headTitle;
        readonly FlowLayoutPanel headRight;

        public Color FillColor = Color.White;
        public Color BorderColor = Theme.CardBorder;
        public int Radius = Theme.RadiusCard;
        public bool ShowBorder = true;
        public Font HeadFont = Theme.CardHeadFont;

        public UiCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            Padding = new Padding(20, 16, 20, 16);

            header = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
            headIcon = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(18, 18), Location = new Point(0, 6), BackColor = Color.Transparent };
            headTitle = new Label { AutoSize = true, Location = new Point(26, 5), Font = Theme.CardHeadFont, ForeColor = Theme.TextDark, BackColor = Color.Transparent };
            headRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, AutoSize = true, BackColor = Color.Transparent,
                Padding = new Padding(0), Margin = new Padding(0)
            };
            header.Controls.Add(headTitle);
            header.Controls.Add(headIcon);
            header.Controls.Add(headRight);
            Controls.Add(header);
        }

        /// <summary>Đặt tiêu đề card kèm icon (icon = null thì chỉ có chữ).</summary>
        public UiCard SetHeader(string title, Bitmap icon)
        {
            headTitle.Text = title ?? "";
            headTitle.Font = HeadFont;
            if (icon != null) { headIcon.Image = icon; headIcon.Visible = true; }
            else headIcon.Visible = false;
            headTitle.Left = icon != null ? 26 : 0;
            header.Visible = !string.IsNullOrEmpty(title) || icon != null;
            return this;
        }

        /// <summary>Thêm control vào góc phải header (nút, link...).</summary>
        public UiCard AddHeaderAction(System.Windows.Forms.Control c)
        {
            c.Margin = new Padding(8, 0, 0, 0);
            headRight.Controls.Add(c);
            headRight.BringToFront();
            return this;
        }

        Panel body;

        /// <summary>
        /// Thân card — nơi chứa nội dung; header nằm trên, cách thân 12px.
        /// Gọi nhiều lần trả về CÙNG một panel (nếu tạo mới mỗi lần thì lần gọi sau sẽ chồng
        /// một panel rỗng lên nội dung vừa thêm).
        /// </summary>
        public Panel Body()
        {
            if (body == null)
            {
                body = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 12, 0, 0) };
                Controls.Add(body);
            }
            body.BringToFront();
            return body;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            UiKit.Fill(e.Graphics, new Rectangle(0, 0, Width, Height), Radius, FillColor, BorderColor, ShowBorder ? 1f : 0f);
        }
    }

    // =====================================================================
    // UiGroup — GroupBox vẽ lại thành THẺ BO GÓC, đúng ngôn ngữ của UiCard.
    // GroupBox gốc luôn vẽ khung vuông khắc chìm + tiêu đề đè LÊN đường viền; không
    // thuộc tính nào đổi được (không có FlatStyle cho GroupBox) nên 4 tab còn lại
    // trông khác hẳn tab Tổng quan. Muốn đồng bộ thì phải tự vẽ.
    // Vẫn kế thừa GroupBox (không phải Panel) để designer và code-behind hiện có giữ
    // nguyên .Text/.Controls/.Font — chỉ đổi tên kiểu, không đổi cách dùng.
    // =====================================================================
    public class UiGroup : GroupBox
    {
        public Color FillColor = Color.White;
        public Color BorderColor = Theme.CardBorder;
        public int Radius = Theme.RadiusCard;
        public bool ShowBorder = true;
        public Font HeadFont = Theme.CardHeadFont;
        public Color HeadColor = Theme.TextDark;

        // Lề trong: 20 ngang · 62 trên (16 lề + 34 tiêu đề + 12 khe, đúng như UiCard) · 16 dưới
        public static readonly Padding CardPadding = new Padding(20, 62, 20, 16);

        public UiGroup()
        {
            // SupportsTransparentBackColor: GroupBox mặc định không nhận nền trong suốt,
            // thiếu cờ này BackColor=Transparent sẽ bị vẽ thành ĐEN và bo góc thành vô nghĩa.
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Padding = CardPadding;
            // KHÔNG đặt Font ở đây: Font của GroupBox là thứ các control con THỪA HƯỞNG
            // (nhãn trong thẻ "Thông tin bảo vệ" không tự đặt Font). Gán Font tiêu đề lên
            // đây sẽ biến toàn bộ nội dung thẻ thành đậm — tiêu đề đã có HeadFont riêng.
        }

        /// <summary>
        /// GroupBox gốc chừa thêm ~18px trên đỉnh DisplayRectangle để đặt dòng tiêu đề.
        /// UiGroup vẽ tiêu đề BÊN TRONG phần Padding (62px) rồi, nên khoản chừa đó là thừa:
        /// nó ăn mất 18px chiều cao nội dung của MỌI thẻ (đo được: thẻ "Thông tin" ở tab
        /// Cách ly chỉ còn 34px cho 2 nhãn cần 44px). Trả DisplayRectangle về đúng Padding.
        /// </summary>
        public override Rectangle DisplayRectangle
        {
            get
            {
                Rectangle r = ClientRectangle;
                r.X += Padding.Left;
                r.Y += Padding.Top;
                r.Width -= Padding.Left + Padding.Right;
                r.Height -= Padding.Top + Padding.Bottom;
                return r;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            UiKit.Fill(e.Graphics, new Rectangle(0, 0, Width, Height), Radius, FillColor, BorderColor,
                ShowBorder ? 1f : 0f);
            if (string.IsNullOrEmpty(Text)) return;
            // Tiêu đề nằm BÊN TRONG thẻ (không đè lên viền) — cùng vị trí/cỡ chữ với UiCard.
            TextRenderer.DrawText(e.Graphics, Text, HeadFont,
                new Rectangle(20, 16, Math.Max(0, Width - 40), 34), HeadColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
        }
    }

    // =====================================================================
    // UiPill — badge bo tròn hoàn toàn (Cao / Trung bình / Thấp / Phát hiện ...)
    // =====================================================================
    public class UiPill : System.Windows.Forms.Control
    {
        string text;
        Theme.PillKind kind;

        public UiPill(string text, Theme.PillKind kind)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.BadgeFont;
            Set(text, kind);
        }

        public void Set(string newText, Theme.PillKind newKind)
        {
            text = newText ?? "";
            kind = newKind;
            Size = GetPreferredSize(Size.Empty);
            Invalidate();
        }

        public override Size GetPreferredSize(Size proposed)
        {
            Size t = TextRenderer.MeasureText(text, Font);
            return new Size(t.Width + 18, Math.Max(22, t.Height + 6));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg, fg;
            Theme.PillColors(kind, out bg, out fg);
            UiKit.Fill(e.Graphics, new Rectangle(0, 0, Width, Height), Theme.RadiusPill, bg, bg, 0f);
            TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(0, 0, Width, Height), fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    // =====================================================================
    // UiButton — nút bo góc, có biến thể + icon tuỳ chọn (thay Theme.StyleButton cho UI mới)
    // =====================================================================
    public class UiButton : Button
    {
        public enum Variant { Primary, Outline, OutlineGray, Danger, Cancel, Ghost }

        public Variant Kind = Variant.Primary;
        public Bitmap Icon;
        public int Radius = Theme.RadiusButton;
        public int IconGap = 8;

        bool hover, down;

        public UiButton()
        {
            // SupportsTransparentBackColor: Button không hỗ trợ nền trong suốt mặc định,
            // thiếu cờ này BackColor=Transparent sẽ bị vẽ thành đen.
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Font = Theme.ButtonFont;
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        public UiButton With(string label, Variant kind, Bitmap icon)
        {
            Text = label; Kind = kind; Icon = icon;
            return this;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

        void Palette(out Color bg, out Color fg, out Color border)
        {
            if (!Enabled) { bg = Theme.ChipGray; fg = Theme.ChipGrayText; border = Theme.Line; return; }
            switch (Kind)
            {
                case Variant.Primary:
                    bg = down ? Color.FromArgb(9, 68, 172) : hover ? Color.FromArgb(9, 74, 186) : Theme.Blue;
                    fg = Color.White; border = bg; break;
                case Variant.Outline:
                    bg = hover ? Theme.BlueTint : Color.White; fg = Theme.BlueDark;
                    border = hover ? Theme.Blue : Theme.BlueSoft; break;
                case Variant.Danger:
                    bg = hover ? Color.FromArgb(253, 226, 226) : Color.White; fg = Theme.Red;
                    border = Theme.RedSoft; break;
                case Variant.Cancel: // nền đỏ đặc — hành động dừng/hủy (tương ứng Theme.BtnRole.Cancel)
                    bg = down ? Color.FromArgb(176, 28, 28) : hover ? Color.FromArgb(196, 32, 32) : Theme.Red;
                    fg = Color.White; border = bg; break;
                case Variant.Ghost:
                    bg = hover ? Theme.ChipGray : Color.Transparent; fg = Theme.TextMid;
                    border = Color.Transparent; break;
                default: // OutlineGray — nút phụ viền xám
                    bg = hover ? Theme.ChipGray : Color.White; fg = Theme.TextMid;
                    border = Theme.InputBorder; break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg, fg, border;
            Palette(out bg, out fg, out border);
            UiKit.Fill(e.Graphics, new Rectangle(0, 0, Width, Height), Radius, bg, border, 1f);

            // Bố cục: [icon] khoảng cách [chữ] — cả cụm canh giữa theo chiều ngang
            Size ts = TextRenderer.MeasureText(Text, Font);
            int iw = Icon != null ? Icon.Width : 0;
            int total = ts.Width + (iw > 0 ? iw + IconGap : 0);
            int x = Math.Max(6, (Width - total) / 2);
            if (iw > 0)
            {
                e.Graphics.DrawImage(Icon, x, (Height - Icon.Height) / 2, Icon.Width, Icon.Height);
                x += iw + IconGap;
            }
            TextRenderer.DrawText(e.Graphics, Text, Font,
                new Rectangle(x, 0, Math.Max(0, Width - x - 4), Height), fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
        }
    }

    // =====================================================================
    // UiNavItem — mục sidebar: icon + nhãn, trạng thái chọn (nền xanh nhạt) / rê chuột
    // =====================================================================
    public class UiNavItem : Button
    {
        public Bitmap Icon;
        public bool Active;

        bool hover;

        public UiNavItem(string text, Bitmap icon)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = Color.Transparent;
            Text = text;
            Icon = icon;
            Height = 44;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 10.125F, FontStyle.Regular);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg = Active ? Theme.NavActiveBg : hover ? Theme.NavHoverBg : Color.Transparent;
            if (bg.A > 0)
                UiKit.Fill(e.Graphics, new Rectangle(0, 0, Width, Height), Theme.RadiusButton, bg, bg, 0f);

            Color fg = Active ? Theme.BlueDark : Theme.TextMid;
            // Icon đổi màu theo trạng thái: vẽ lại bitmap cùng hình dạng nhưng màu khác
            Bitmap ico = Icon;
            if (Icon != null && Active) ico = UiIcons.Recolor(Icon, Theme.Blue);
            int ix = 14;
            if (ico != null) e.Graphics.DrawImage(ico, ix, (Height - ico.Height) / 2, ico.Width, ico.Height);

            var font = Active ? new Font(Font, FontStyle.Bold) : Font;
            TextRenderer.DrawText(e.Graphics, Text, font,
                new Rectangle(ix + 26, 0, Width - ix - 30, Height), fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
            if (font != Font) font.Dispose();
        }
    }

    // =====================================================================
    // UiRadioCard — thẻ chọn chế độ quét (Quét toàn bộ / thư mục / tệp / tuỳ chỉnh)
    //
    // Kế thừa RadioButton (KHÔNG phải Control trần) là chủ ý: thẻ phải giữ nguyên
    // ngữ nghĩa radio — Checked/CheckedChanged, điều hướng bằng phím mũi tên, TabStop,
    // và vai trò "radio button" mà trình đọc màn hình đọc được. Ta chỉ thay phần VẼ:
    // OnPaint được override và KHÔNG gọi base nên RadioButton không vẽ ô tròn mặc định.
    //
    // Trước đây thẻ được ghép từ 1 RadioButton (hàng tiêu đề) + 1 Label (hàng mô tả)
    // nằm trong một TableLayoutPanel. Hệ quả: viền thẻ do RadioButton vẽ nên chỉ ôm
    // hàng tiêu đề, còn dòng mô tả rơi ra NGOÀI viền; thẻ cũng không có icon và không
    // có nút radio. Một control ôm trọn thẻ sửa cả ba lỗi cùng lúc.
    // =====================================================================
    public class UiRadioCard : RadioButton
    {
        public Bitmap Icon;
        public string Desc = "";

        bool hover;

        public UiRadioCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            // Appearance.Normal + FlatStyle.Flat: tắt hết phần vẽ chuẩn của RadioButton.
            Appearance = Appearance.Normal;
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = Theme.BoldFont;
            Height = 104;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = Checked ? Theme.BlueTint : hover ? Color.FromArgb(252, 253, 255) : Color.White;
            Color border = Checked ? Theme.Blue : Theme.CardBorder;
            UiKit.Fill(e.Graphics, new Rectangle(0, 0, Width, Height), Theme.RadiusCard,
                fill, border, Checked ? 1.6f : 1f);

            // Ô icon tròn — cách lề trái 45px như mockup (đo trên Design/179033025*.jpg)
            int cx = 45 + 17, cy = Height / 2;
            using (var b = new SolidBrush(Checked ? Theme.BlueFaint : Theme.ChipGray))
                e.Graphics.FillEllipse(b, cx - 17, cy - 17, 34, 34);
            if (Icon != null)
                e.Graphics.DrawImage(Icon, cx - Icon.Width / 2, cy - Icon.Height / 2, Icon.Width, Icon.Height);

            // Khối chữ: tiêu đề 1 dòng + mô tả 2 dòng, canh giữa theo chiều dọc
            int tx = 90, textW = Math.Max(0, Width - tx - 46);
            const int caoTieuDe = 22, caoMoTa = 34, khe = 2;
            int top = Math.Max(4, (Height - (caoTieuDe + khe + caoMoTa)) / 2);
            TextRenderer.DrawText(e.Graphics, Text, Theme.BoldFont,
                new Rectangle(tx, top, textW, caoTieuDe), Checked ? Theme.BlueDark : Theme.TextDark,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
            // WordBreak: mô tả dài ("Kiểm tra tất cả ổ đĩa và tệp trên máy tính của bạn.")
            // xuống 2 dòng như mockup — cắt bằng EndEllipsis thì mất nửa câu.
            TextRenderer.DrawText(e.Graphics, Desc, Theme.SmallFont,
                new Rectangle(tx, top + caoTieuDe + khe, textW, caoMoTa), Theme.TextGray,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak
                | TextFormatFlags.NoPadding);

            // Nút radio — cách lề phải 30px
            int rx = Width - 30, ry = cy;
            using (var pen = new Pen(Checked ? Theme.Blue : Theme.InputBorder, 1.6f))
                e.Graphics.DrawEllipse(pen, rx - 9, ry - 9, 18, 18);
            if (Checked)
                using (var b = new SolidBrush(Theme.Blue))
                    e.Graphics.FillEllipse(b, rx - 5, ry - 5, 10, 10);
        }
    }

    // =====================================================================
    // UiIconBadge — ô tròn nền xanh nhạt chứa icon, dùng làm khối nhận diện
    // ở đầu mỗi thẻ nội dung chế độ quét ("Quét thư mục", "Quét tệp", …).
    // =====================================================================
    public class UiIconBadge : System.Windows.Forms.Control
    {
        readonly Bitmap icon;
        public Color CircleColor = Theme.BlueFaint;

        public UiIconBadge(Bitmap icon, int size)
        {
            this.icon = icon;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(size, size);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(CircleColor))
                e.Graphics.FillEllipse(b, 0, 0, Width - 1, Height - 1);
            if (icon == null) return;
            e.Graphics.DrawImage(icon, (Width - icon.Width) / 2, (Height - icon.Height) / 2,
                icon.Width, icon.Height);
        }
    }

    // =====================================================================
    // UiGridHost — khung ôm SÁT bảng dữ liệu theo số dòng thật.
    //
    // Bảng nằm trực tiếp trong hàng Percent 100 sẽ cao bằng toàn bộ phần còn lại của
    // thẻ. Với bảng RỖNG (chưa chọn thư mục/tệp nào) nó thành một khoảng trắng lớn và
    // đẩy mục "Tuỳ chọn quét" xuống tận đáy thẻ — mockup đặt mục đó NGAY DƯỚI bảng.
    // Khung này tự đo lại chiều cao mỗi khi thêm/bớt dòng, chặn trên ở MaxRows để bảng
    // nhiều dòng vẫn cuộn bên trong thay vì kéo dài thẻ vô hạn.
    // =====================================================================
    public class UiGridHost : Panel
    {
        readonly DataGridView grid;
        public int MaxRows = 6;
        public int RowHeight = 30;

        public UiGridHost(DataGridView grid)
        {
            this.grid = grid;
            Dock = DockStyle.Top;
            Margin = new Padding(0);
            BackColor = Color.Transparent;
            Controls.Add(grid);
            grid.RowsAdded += delegate { Fit(); };
            grid.RowsRemoved += delegate { Fit(); };
            Fit();
        }

        int TinhCao()
        {
            int n = Math.Max(grid.Rows.Count, 1);          // bảng rỗng vẫn chừa 1 dòng trống
            return grid.ColumnHeadersHeight + Math.Min(n, MaxRows) * RowHeight + 4;
        }

        void Fit()
        {
            int h = TinhCao();
            if (Height != h) Height = h;
        }

        public override Size GetPreferredSize(Size proposed)
        {
            return new Size(proposed.Width, TinhCao());
        }
    }

    // =====================================================================
    // UiFieldGrid — bảng 2 cột "nhãn : giá trị" dùng trong mọi card thông tin chi tiết
    // =====================================================================
    public class UiFieldGrid : TableLayoutPanel
    {
        int labelWidth = 132;

        public UiFieldGrid()
        {
            ColumnCount = 2;
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.Transparent;
            Margin = new Padding(0);
            Padding = new Padding(0);
        }

        /// <summary>Thêm một dòng nhãn/giá trị; trả về Label giá trị để cập nhật sau.</summary>
        public Label Add(string label, string value)
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Font = Theme.FieldFont,
                ForeColor = Theme.TextGray,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 5, 8, 5)
            };
            var val = new Label
            {
                Text = value ?? "",
                AutoSize = true,
                Font = Theme.FieldFont,
                ForeColor = Theme.TextDark,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 5, 0, 5)
            };
            Controls.Add(lbl, 0, RowCount);
            Controls.Add(val, 1, RowCount);
            RowCount++;
            return val;
        }

        /// <summary>Thêm một dòng có control tuỳ biến ở cột giá trị (nút sao chép, pill...).</summary>
        public void AddCustom(string label, System.Windows.Forms.Control valueControl)
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Font = Theme.FieldFont,
                ForeColor = Theme.TextGray,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 5, 8, 5)
            };
            valueControl.Margin = new Padding(0, 5, 0, 5);
            Controls.Add(lbl, 0, RowCount);
            Controls.Add(valueControl, 1, RowCount);
            RowCount++;
        }
    }
}
