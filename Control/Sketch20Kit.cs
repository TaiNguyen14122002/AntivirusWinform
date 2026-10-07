using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    // Kích thước là CSS pixel của bộ 20 phác thảo (96 DPI), không lấy cỡ chữ từ designer cũ.
    internal static class Sketch20Style
    {
        internal static readonly Color Background = ColorTranslator.FromHtml("#f7fbff"), Sidebar = ColorTranslator.FromHtml("#f2f8ff"),
            Ink = ColorTranslator.FromHtml("#090e2c"), Muted = ColorTranslator.FromHtml("#647397"), Border = ColorTranslator.FromHtml("#e1ecfa"),
            Blue = ColorTranslator.FromHtml("#0068ff"), BlueSoft = ColorTranslator.FromHtml("#e7f2ff"), Track = ColorTranslator.FromHtml("#dfe9ff"),
            Green = ColorTranslator.FromHtml("#00a651"), GreenSoft = ColorTranslator.FromHtml("#e8faf0"), GreenBorder = ColorTranslator.FromHtml("#d9f0e5"),
            Red = ColorTranslator.FromHtml("#ff174a"), RedSoft = ColorTranslator.FromHtml("#ffedf2"), RedBorder = ColorTranslator.FromHtml("#ffd7e1"),
            Amber = ColorTranslator.FromHtml("#ec8500"), AmberSoft = ColorTranslator.FromHtml("#fff5df"), AmberBorder = ColorTranslator.FromHtml("#ffe8b5");
        internal static readonly Color GridHeader = ColorTranslator.FromHtml("#f4f8fe");
        internal static readonly Color Shadow = Color.FromArgb(7, 41, 114, 195);
        internal static readonly Color GridAlt = ColorTranslator.FromHtml("#fbfcfe");
        internal const float TypographyScale = 1.08f;
        // Nhịp dọc dùng chung cho toàn bộ Sketch20: tránh mỗi màn tự đặt line-height/padding khác nhau.
        internal const int ColumnPadding = 18;
        internal const int RowPaddingX = 14;
        internal const int RowPaddingY = 10;
        internal const int TitleLineHeight = 24;
        internal const int BodyLineHeight = 20;
        internal const int TextStackGap = 3;
        internal const int RowGap = 10;
        internal const int SectionGap = 16;
        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();
        // Chỉ bật trong bộ chẩn đoán: ghi lại vùng chữ của cả control tự vẽ.
        internal static Action<Graphics, Sketch20TextRun> TraceText;
        internal static string BodyFamily = ResolveFamily(Environment.OSVersion.Platform == PlatformID.Unix
            ? new[] { "Arial", "Liberation Sans", "Nimbus Sans", "DejaVu Sans" } : new[] { "Segoe UI", "Arial", "Tahoma" }, FontFamily.GenericSansSerif.Name);
        internal static string MonoFamily = ResolveFamily(new[] { "Consolas", "DejaVu Sans Mono", "Liberation Mono", "Courier New" }, FontFamily.GenericMonospace.Name);
        private static string ResolveFamily(string[] preferred, string fallback)
        {
            using (var installed = new System.Drawing.Text.InstalledFontCollection())
                foreach (string name in preferred)
                    if (installed.Families.Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))) return name;
            return fallback;
        }
        internal static Font Font(float pixels, bool bold = false, bool mono = false)
        {
            string family = mono ? MonoFamily : BodyFamily;
            float actualPixels = Math.Max(1f, pixels * TypographyScale);
            string key = family + "/" + actualPixels + "/" + bold;
            Font font;
            if (!Fonts.TryGetValue(key, out font))
            {
                font = new Font(family, actualPixels, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
                Fonts.Add(key, font);
            }
            return font;
        }
        internal static void Round(Graphics g, Rectangle bounds, int radius, Color fill, Color border)
        {
            if (bounds.Width < 2 || bounds.Height < 2) return;
            using (var path = UiKit.Round(new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1), radius))
            {
                if (fill.A != 0) using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                if (border.A != 0) using (var pen = new Pen(border, 1)) g.DrawPath(pen, path);
            }
        }
        internal static void Text(Graphics g, string value, Rectangle bounds, float size = 13, Color? color = null, bool bold = false,
            bool wrap = false, bool right = false, bool mono = false, bool center = false)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            // Windows vẽ lại nền của control trong suốt bằng Graphics đã dịch tọa độ.
            // TextRenderer dùng GDI: phải giữ phép dịch như các hình vẽ GDI+ để tránh chữ nền lọt vào button/tab.
            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix
                | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.PreserveGraphicsTranslateTransform;
            // Một dòng căn giữa theo chiều dọc để thẳng hàng với icon. Nội dung nhiều dòng
            // luôn căn từ trên xuống để không bị "trôi" baseline hoặc đè lên hàng kế tiếp.
            if (wrap) flags |= TextFormatFlags.WordBreak;
            else flags |= TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
            if (right) flags |= TextFormatFlags.Right;
            if (center) flags |= TextFormatFlags.HorizontalCenter;
            // Chỉ đổi clip. Save/Restore trên Graphics đã dịch làm sai viewport GDI
            // của TextRenderer trong .NET Framework; kiểm thử pixel Windows bao phủ trường hợp này.
            using (var originalClip = g.Clip)
            {
            try
            {
                g.SetClip(bounds, CombineMode.Intersect);
                Font font = Font(size, bold, mono);
                string display = value ?? "—";
                if (TraceText != null) TraceText(g, new Sketch20TextRun { Value = display, Bounds = bounds, Pixels = size, Bold = bold, Wrap = wrap, Right = right, Mono = mono });
                if (!wrap) display = FitLine(g, display.Replace('\r', ' ').Replace('\n', ' '), font, bounds.Width);
                TextRenderer.DrawText(g, display, font, bounds, color ?? Ink, flags);
            }
            finally { g.SetClip(originalClip, CombineMode.Replace); }
            }
        }
        internal static int Measure(string value, float size = 13, bool bold = false, bool mono = false)
        { return TextRenderer.MeasureText(value ?? "", Font(size, bold, mono), Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width; }
        internal static int Measure(Graphics graphics, string value, float size, bool bold = false)
        { return TextRenderer.MeasureText(graphics, value ?? "", Font(size, bold), Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width; }
        private static string FitLine(Graphics graphics, string value, Font font, int width)
        {
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            Func<string, int> measure = text => TextRenderer.MeasureText(graphics, text, font, new Size(32767, 32767), flags).Width;
            if (measure(value) <= width) return value;
            const string mark = "…";
            if (measure(mark) > width) return "";
            int[] elements = System.Globalization.StringInfo.ParseCombiningCharacters(value);
            int low = 0, high = elements.Length;
            while (low < high)
            {
                int count = (low + high + 1) / 2;
                string candidate = value.Substring(0, count == elements.Length ? value.Length : elements[count]) + mark;
                if (measure(candidate) <= width) low = count; else high = count - 1;
            }
            return value.Substring(0, low == elements.Length ? value.Length : elements[low]) + mark;
        }
        internal static void ControlBackground(Graphics graphics, System.Windows.Forms.Control control)
        {
            var surface = control.Parent as Sketch20Surface;
            if (surface != null) surface.PaintControlBackground(graphics, control.Bounds);
            else using (var brush = new SolidBrush(control.Parent == null ? Background : control.Parent.BackColor))
                graphics.FillRectangle(brush, control.ClientRectangle);
        }

        // Nét 1.7 px và viewBox 24 px tương ứng hệ icon Lucide của phác thảo.
        internal static void Icon(Graphics g, string name, Rectangle box, Color color)
        {
            if (string.IsNullOrEmpty(name)) return;
            var state = g.Save();
            g.TranslateTransform(box.X, box.Y); g.ScaleTransform(box.Width / 24f, box.Height / 24f);
            using (var p = new Pen(color, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                Action<float, float, float, float> line = (a, b, c, d) => g.DrawLine(p, a, b, c, d);
                Action<PointF[]> lines = a => g.DrawLines(p, a);
                switch (name)
                {
                    case "cube": lines(new[] { new PointF(12, 2), new PointF(21, 7), new PointF(21, 18), new PointF(12, 23), new PointF(3, 18), new PointF(3, 7), new PointF(12, 2) }); lines(new[] { new PointF(3, 7), new PointF(12, 12), new PointF(21, 7) }); line(12, 12, 12, 23); break;
                    case "windows": using (var b = new SolidBrush(color)) { g.FillRectangle(b, 2, 3, 8, 8); g.FillRectangle(b, 12, 2, 10, 9); g.FillRectangle(b, 2, 13, 8, 8); g.FillRectangle(b, 12, 13, 10, 9); } break;
                    case "cpu": g.DrawRectangle(p, 6, 6, 12, 12); g.DrawRectangle(p, 9, 9, 6, 6); for (int i = 7; i <= 17; i += 5) { line(i, 2, i, 6); line(i, 18, i, 22); line(2, i, 6, i); line(18, i, 22, i); } break;
                    case "ram": g.DrawRectangle(p, 2, 6, 20, 11); for (int i = 4; i <= 16; i += 6) g.DrawRectangle(p, i, 9, 4, 5); for (int i = 4; i <= 20; i += 4) line(i, 17, i, 21); break;
                    case "laptop": g.DrawRectangle(p, 5, 3, 14, 12); lines(new[] { new PointF(5, 15), new PointF(1, 20), new PointF(23, 20), new PointF(19, 15) }); break;
                    case "bulb": g.DrawEllipse(p, 5, 2, 14, 14); line(8, 15, 8, 19); line(16, 15, 16, 19); line(8, 19, 16, 19); line(10, 22, 14, 22); break;
                    case "calendar": g.DrawRectangle(p, 3, 5, 18, 17); line(3, 10, 21, 10); line(7, 2, 7, 7); line(17, 2, 17, 7); break;
                    case "search": g.DrawEllipse(p, 3, 2, 14, 14); line(15, 15, 22, 22); break;
                    case "play": using (var b = new SolidBrush(color)) g.FillPolygon(b, new[] { new PointF(5, 2), new PointF(21, 12), new PointF(5, 22) }); break;
                    case "eye": lines(new[] { new PointF(1, 12), new PointF(7, 5), new PointF(17, 5), new PointF(23, 12), new PointF(17, 19), new PointF(7, 19), new PointF(1, 12) }); g.DrawEllipse(p, 8, 8, 8, 8); break;
                    case "shield": case "shield-check": case "shield-alert":
                        using (var path = new GraphicsPath())
                        { path.AddLines(new[] { new PointF(12, 3), new PointF(21, 7), new PointF(20, 15) });
                          path.AddBezier(20, 15, 19, 19, 15, 21, 12, 22); path.AddBezier(12, 22, 9, 21, 5, 19, 4, 15);
                          path.AddLines(new[] { new PointF(4, 15), new PointF(3, 7), new PointF(12, 3) }); g.DrawPath(p, path); }
                        if (name == "shield-check") lines(new[] { new PointF(8, 12), new PointF(11, 15), new PointF(16, 10) });
                        if (name == "shield-alert") { line(12, 9, 12, 13); using (var brush = new SolidBrush(color)) g.FillEllipse(brush, 11.3f, 16, 1.4f, 1.4f); }
                        break;
                    case "house": lines(new[] { new PointF(3, 10), new PointF(12, 3), new PointF(21, 10), new PointF(21, 21), new PointF(15, 21), new PointF(15, 14), new PointF(9, 14), new PointF(9, 21), new PointF(3, 21), new PointF(3, 10) }); break;
                    case "archive": g.DrawRectangle(p, 3, 3, 18, 5); g.DrawRectangle(p, 5, 8, 14, 13); line(10, 12, 14, 12); break;
                    case "file": case "file-warning": case "file-search": case "file-check": case "file-plus":
                        lines(new[] { new PointF(5, 3), new PointF(15, 3), new PointF(20, 8), new PointF(20, 21), new PointF(5, 21), new PointF(5, 3) });
                        lines(new[] { new PointF(15, 3), new PointF(15, 8), new PointF(20, 8) });
                        if (name == "file-warning") { line(12, 11, 12, 15); line(12, 18, 12.1f, 18); }
                        if (name == "file-search") { g.DrawEllipse(p, 8, 10, 6, 6); line(13, 15, 16, 18); }
                        if (name == "file-check") lines(new[] { new PointF(8, 14), new PointF(11, 17), new PointF(16, 12) });
                        if (name == "file-plus") { line(9, 14, 15, 14); line(12, 11, 12, 17); }
                        break;
                    case "files": g.DrawRectangle(p, 8, 6, 12, 15); lines(new[] { new PointF(15, 6), new PointF(15, 2), new PointF(3, 2), new PointF(3, 17), new PointF(8, 17) }); break;
                    case "folder": case "folder-open": case "folder-search": case "folder-plus":
                        lines(new[] { new PointF(3, 6), new PointF(9, 6), new PointF(12, 9), new PointF(21, 9), new PointF(21, 20), new PointF(3, 20), new PointF(3, 6) });
                        if (name == "folder-search") { g.DrawEllipse(p, 10, 11, 5, 5); line(14, 15, 17, 18); }
                        if (name == "folder-plus") { line(9, 14, 15, 14); line(12, 11, 12, 17); }
                        if (name == "folder-open") line(4, 13, 20, 13);
                        break;
                    case "monitor": g.DrawRectangle(p, 2, 3, 20, 14); line(12, 17, 12, 21); line(8, 21, 16, 21); break;
                    case "hard-drive": g.DrawRectangle(p, 3, 12, 18, 8); lines(new[] { new PointF(3, 12), new PointF(6, 4), new PointF(18, 4), new PointF(21, 12) }); line(16, 16, 16.1f, 16); line(19, 16, 19.1f, 16); break;
                    case "scan": line(3, 8, 3, 3); line(3, 3, 8, 3); line(16, 3, 21, 3); line(21, 3, 21, 8); line(21, 16, 21, 21); line(21, 21, 16, 21); line(8, 21, 3, 21); line(3, 21, 3, 16); line(7, 12, 17, 12); break;
                    case "clock": case "history": g.DrawEllipse(p, 3, 3, 18, 18); line(12, 7, 12, 12); line(12, 12, 16, 14); if (name == "history") { line(2, 3, 2, 9); line(2, 9, 7, 9); } break;
                    case "refresh": g.DrawArc(p, 3, 3, 18, 18, 20, 145); g.DrawArc(p, 3, 3, 18, 18, 200, 145); lines(new[] { new PointF(21, 3), new PointF(21, 8), new PointF(16, 8) }); lines(new[] { new PointF(3, 21), new PointF(3, 16), new PointF(8, 16) }); break;
                    case "check": lines(new[] { new PointF(4, 12), new PointF(10, 18), new PointF(20, 6) }); break;
                    case "circle-check": g.DrawEllipse(p, 3, 3, 18, 18); lines(new[] { new PointF(8, 12), new PointF(11, 15), new PointF(16, 10) }); break;
                    case "x": line(6, 6, 18, 18); line(6, 18, 18, 6); break;
                    case "minimize": line(5, 12, 19, 12); break;
                    case "maximize": g.DrawRectangle(p, 5, 5, 14, 14); break;
                    case "sigma": lines(new[] { new PointF(20, 4), new PointF(5, 4), new PointF(12, 12), new PointF(5, 20), new PointF(20, 20) }); break;
                    case "bell": g.DrawArc(p, 6, 3, 12, 15, 180, 180); line(6, 10, 6, 18); line(18, 10, 18, 18); line(4, 18, 20, 18); g.DrawArc(p, 9, 18, 6, 5, 0, 180); break;
                    case "save": g.DrawRectangle(p, 4, 3, 16, 18); g.DrawRectangle(p, 8, 3, 8, 6); g.DrawRectangle(p, 8, 14, 8, 7); break;
                    case "bolt": lines(new[] { new PointF(14, 2), new PointF(5, 13), new PointF(11, 13), new PointF(9, 22), new PointF(20, 9), new PointF(13, 9), new PointF(14, 2) }); break;
                    case "chevron-right": lines(new[] { new PointF(9, 5), new PointF(16, 12), new PointF(9, 19) }); break;
                    case "chevron-left": lines(new[] { new PointF(15, 5), new PointF(8, 12), new PointF(15, 19) }); break;
                    case "chevron-down": lines(new[] { new PointF(5, 9), new PointF(12, 16), new PointF(19, 9) }); break;
                    case "arrow-left": line(4, 12, 21, 12); lines(new[] { new PointF(10, 5), new PointF(3, 12), new PointF(10, 19) }); break;
                    case "download": line(12, 3, 12, 15); lines(new[] { new PointF(7, 10), new PointF(12, 15), new PointF(17, 10) }); lines(new[] { new PointF(3, 15), new PointF(3, 21), new PointF(21, 21), new PointF(21, 15) }); break;
                    case "power": g.DrawArc(p, 3, 3, 18, 18, -50, 280); line(12, 2, 12, 12); break;
                    case "activity": lines(new[] { new PointF(1, 12), new PointF(6, 12), new PointF(9, 3), new PointF(14, 21), new PointF(17, 12), new PointF(23, 12) }); break;
                    case "globe": g.DrawEllipse(p, 2, 2, 20, 20); g.DrawEllipse(p, 7, 2, 10, 20); line(2, 12, 22, 12); break;
                    case "info": g.DrawEllipse(p, 3, 3, 18, 18); line(12, 11, 12, 17); line(12, 7, 12.1f, 7); break;
                    case "alert": lines(new[] { new PointF(12, 3), new PointF(22, 21), new PointF(2, 21), new PointF(12, 3) }); line(12, 10, 12, 14); line(12, 18, 12.1f, 18); break;
                    case "trash": line(3, 6, 21, 6); line(8, 3, 16, 3); lines(new[] { new PointF(5, 6), new PointF(7, 21), new PointF(17, 21), new PointF(19, 6) }); line(10, 10, 10, 17); line(14, 10, 14, 17); break;
                    case "undo": lines(new[] { new PointF(7, 3), new PointF(2, 8), new PointF(7, 13) }); line(2, 8, 14, 8); g.DrawArc(p, 9, 8, 12, 12, -90, 260); break;
                    case "key": g.DrawEllipse(p, 12, 2, 10, 10); lines(new[] { new PointF(13, 11), new PointF(3, 21), new PointF(1, 19), new PointF(3, 17), new PointF(5, 19), new PointF(7, 17), new PointF(5, 15) }); break;
                    case "filter": line(3, 6, 21, 6); line(6, 12, 18, 12); line(10, 18, 14, 18); break;
                    case "sliders": line(3, 6, 21, 6); line(3, 12, 21, 12); line(3, 18, 21, 18); line(9, 3, 9, 9); line(15, 9, 15, 15); line(7, 15, 7, 21); break;
                    case "usb": line(12, 21, 12, 3); lines(new[] { new PointF(9, 6), new PointF(12, 3), new PointF(15, 6) }); lines(new[] { new PointF(12, 17), new PointF(6, 13), new PointF(6, 10) }); lines(new[] { new PointF(12, 13), new PointF(18, 9), new PointF(18, 6) }); g.DrawRectangle(p, 16, 3, 4, 4); g.DrawEllipse(p, 4, 7, 4, 4); break;
                    case "settings": g.DrawEllipse(p, 5, 5, 14, 14); g.DrawEllipse(p, 9, 9, 6, 6); for (int i = 0; i < 8; i++) { double a = i * Math.PI / 4; line((float)(12 + 7 * Math.Cos(a)), (float)(12 + 7 * Math.Sin(a)), (float)(12 + 10 * Math.Cos(a)), (float)(12 + 10 * Math.Sin(a))); } break;
                    case "list-checks": lines(new[] { new PointF(2, 6), new PointF(4, 8), new PointF(7, 4) }); line(11, 6, 22, 6); line(2, 14, 4, 16); line(4, 16, 7, 12); line(11, 14, 22, 14); break;
                    case "crown": lines(new[] { new PointF(3, 8), new PointF(7, 16), new PointF(17, 16), new PointF(21, 8), new PointF(16, 12), new PointF(12, 5), new PointF(8, 12), new PointF(3, 8) }); line(6, 19, 18, 19); break;
                    default: g.DrawEllipse(p, 4, 4, 16, 16); break;
                }
            }
            g.Restore(state);
        }
    }

    internal sealed class Sketch20Button : Button
    {
        internal string IconName;
        internal bool Primary, Danger, Link, Tab, Selected, Chrome, UnderlineOnly;
        internal Color? Accent;
        internal float TextPixels = 13;
        internal int IconPixels = 16;
        private bool hover;
        internal Sketch20Button(string text, string icon = "")
        {
            Text = text; IconName = icon; Font = Sketch20Style.Font(13, true); Cursor = Cursors.Hand;
            BackColor = Color.Transparent; AccessibleName = text; FlatStyle = FlatStyle.Flat;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw, true);
            MouseEnter += delegate { hover = true; Invalidate(); }; MouseLeave += delegate { hover = false; Invalidate(); };
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        { Sketch20Style.ControlBackground(e.Graphics, this); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Sketch20Style.ControlBackground(e.Graphics, this);
            var s = Sketch20Style.Ink; var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fg = Primary ? Color.White : Danger ? Sketch20Style.Red : Link || Selected ? Sketch20Style.Blue : s;
            if (Chrome) fg = Sketch20Style.Muted;
            Color bg = Primary ? (Accent ?? (Danger ? Sketch20Style.Red : Sketch20Style.Blue)) : Tab ? Selected && !UnderlineOnly ? Sketch20Style.BlueSoft : Color.Transparent : Link ? Color.Transparent : Selected ? Sketch20Style.BlueSoft : Color.White;
            if (hover && !Primary && !Tab && !Link) bg = Sketch20Style.BlueSoft;
            if (!Enabled) { fg = Color.FromArgb(170, 181, 197); bg = Primary ? Sketch20Style.Track : Color.White; }
            Color edge = Link || Tab ? Color.Transparent : Primary ? bg : Danger ? Sketch20Style.RedBorder : Sketch20Style.Border;
            Sketch20Style.Round(g, ClientRectangle, Tab ? 6 : 8, bg, edge);
            if (Tab && Selected) using (var pen = new Pen(Sketch20Style.Blue, 2)) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            int gap = string.IsNullOrEmpty(IconName) || string.IsNullOrEmpty(Text) ? 0 : 8;
            int iconWidth = string.IsNullOrEmpty(IconName) ? 0 : IconPixels;
            bool bold = !Tab || Selected;
            int tw = Sketch20Style.Measure(g, Text, TextPixels, bold);
            int x = Math.Max(0, (Width - iconWidth - gap - tw) / 2);
            if (iconWidth > 0) Sketch20Style.Icon(g, IconName, new Rectangle(x, (Height - iconWidth) / 2, iconWidth, iconWidth), fg);
            if (!string.IsNullOrEmpty(Text)) Sketch20Style.Text(g, Text, new Rectangle(x + iconWidth + gap, 0, Math.Max(1, Width - x - iconWidth - gap), Height), TextPixels, Tab && !Selected ? Sketch20Style.Muted : fg, bold);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(3, 3, Width - 6, Height - 6));
        }
    }

    internal sealed class Sketch20Switch : CheckBox
    {
        internal Sketch20Switch()
        {
            Size = new Size(34, 25); AutoSize = false; Cursor = Cursors.Hand; BackColor = Color.Transparent;
            AccessibleRole = AccessibleRole.CheckButton;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        { Sketch20Style.ControlBackground(e.Graphics, this); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Sketch20Style.ControlBackground(e.Graphics, this);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Sketch20Style.Round(e.Graphics, new Rectangle(0, 2, 34, 21), 10, Checked ? Sketch20Style.Blue : Sketch20Style.Track, Color.Transparent);
            using (var brush = new SolidBrush(Color.White)) e.Graphics.FillEllipse(brush, Checked ? 16 : 3, 5, 15, 15);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }
    }

    internal sealed class Sketch20Check : CheckBox
    {
        internal float TextPixels = 13;
        internal Sketch20Check(string text)
        {
            Text = text; Height = 25; AutoSize = false; BackColor = Color.Transparent; Font = Sketch20Style.Font(13); Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        { Sketch20Style.ControlBackground(e.Graphics, this); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Sketch20Style.ControlBackground(e.Graphics, this);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var box = new Rectangle(2, (Height - 15) / 2, 15, 15);
            Sketch20Style.Round(e.Graphics, box, 2, Checked ? Sketch20Style.Blue : Color.White, Checked ? Sketch20Style.Blue : Color.FromArgb(195, 195, 195));
            if (Checked) Sketch20Style.Icon(e.Graphics, "check", box, Color.White);
            if (!string.IsNullOrEmpty(Text) && Width > 26)
                Sketch20Style.Text(e.Graphics, Text, new Rectangle(26, 0, Width - 26, Height), TextPixels);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }
    }

    internal sealed class Sketch20Surface : Panel
    {
        internal readonly List<Action<Graphics>> Drawing = new List<Action<Graphics>>();
        internal readonly List<Action<Graphics>> BackgroundDrawing = new List<Action<Graphics>>();
        internal readonly List<Sketch20TextRun> TextRuns = new List<Sketch20TextRun>();
        internal readonly Dictionary<string, Rectangle> Nodes = new Dictionary<string, Rectangle>();
        private readonly ToolTip tooltip = new ToolTip { AutoPopDelay = 20000, InitialDelay = 450, ReshowDelay = 100, ShowAlways = true };
        private string previousTip;
        internal Sketch20Surface() { DoubleBuffered = true; BackColor = Sketch20Style.Background; }
        internal void PaintControlBackground(Graphics graphics, Rectangle bounds)
        {
            // Không để Windows giả lập transparency bằng cách in lại cây control cũ phía dưới.
            // Chỉ dựng nền/card của renderer hiện tại tại tọa độ của control.
            using (var brush = new SolidBrush(BackColor)) graphics.FillRectangle(brush, new Rectangle(Point.Empty, bounds.Size));
            var state = graphics.Save();
            try
            {
                graphics.TranslateTransform(-bounds.X, -bounds.Y);
                foreach (var paint in BackgroundDrawing) paint(graphics);
            }
            finally { graphics.Restore(state); }
        }
        internal string TooltipAt(Point location)
        {
            var text = TextRuns.LastOrDefault(t => t.Bounds.Contains(location));
            return text != null && !text.Wrap && (text.Mono || Sketch20Style.Measure(text.Value, text.Pixels, text.Bold, text.Mono) > text.Bounds.Width) ? text.Value : "";
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            string value = TooltipAt(e.Location);
            if (value != previousTip) { tooltip.SetToolTip(this, value); previousTip = value; }
        }
        protected override void Dispose(bool disposing)
        { if (disposing) tooltip.Dispose(); base.Dispose(disposing); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var draw in Drawing) draw(e.Graphics);
        }
    }

    // Vùng chữ và font thực tế để kiểm tra tràn/đè chữ trên renderer native.
    internal sealed class Sketch20TextRun
    {
        internal string Value;
        internal Rectangle Bounds;
        internal float Pixels;
        internal bool Bold, Wrap, Right, Mono;
    }

    internal sealed class Sketch20File
    {
        internal string Id, Name, Path, Reason, Severity, Size, Date, Time, Status, Sha256, Md5, Created, FileType, Source;
        internal int Index;
        internal bool Resolved;
    }
    internal sealed class Sketch20History
    {
        internal string Date, Time, Type, Result, Files, Threats, Duration, Scope;
        internal int Index;
        internal bool Risk;
    }
    internal sealed class Sketch20Model
    {
        internal bool Sample, Running, Scanning, KeyConfigured;
        internal int ThreatCount, ScannedFiles, Progress = -1, ElapsedSeconds;
        internal int? VtDetections, VtEngineCount;
        internal int HistoryTotal;
        internal bool HistoryPrev, HistoryNext;
        internal string HistoryPage = "0 / 0", QuarantineNotice = "";
        internal string Version = "1.0.0", LastDate = "—", LastTime = "—", LastType = "—", Signature = "Chưa ghi nhận cập nhật", Status = "Ứng dụng sẵn sàng", DataFolder = "—", ScanPath = "—", VtSummary = "Chưa tra cứu VirusTotal", VtTime = "—", VtScore = "—";
        internal Dictionary<string, bool> Flags = new Dictionary<string, bool>();
        internal List<Sketch20File> Threats = new List<Sketch20File>(), Quarantine = new List<Sketch20File>();
        internal List<Sketch20History> History = new List<Sketch20History>();
        internal List<string[]> Activities = new List<string[]>(), Engines = new List<string[]>(), Behavior = new List<string[]>();
        internal List<string> Strings = new List<string>();
        internal Sketch20File Detail;
        internal bool Flag(string key) { bool value; return Flags.TryGetValue(key, out value) && value; }
        internal string Stamp()
        {
            return string.Join("|", Running, Scanning, ThreatCount, ScannedFiles, Progress, ElapsedSeconds, KeyConfigured, Status, VtSummary, VtTime, VtScore, VtDetections, VtEngineCount,
                Signature, LastDate, LastTime, LastType, HistoryTotal, HistoryPage, HistoryPrev, HistoryNext, QuarantineNotice,
                string.Join(",", Flags.OrderBy(f => f.Key).Select(f => f.Key + f.Value)),
                string.Join(",", Quarantine.Select(f => f.Id + f.Path + f.Status)), string.Join(",", History.Select(h => h.Date + h.Time + h.Type + h.Result + h.Files)),
                Detail == null ? "" : Detail.Path + Detail.Status + Detail.Sha256,
                string.Join(",", Activities.Select(a => string.Join("/", a))),
                string.Join(",", Strings), string.Join(",", Engines.Select(e => string.Join("/", e))), string.Join(",", Behavior.Select(b => string.Join("/", b))));
        }
    }
    internal sealed class Sketch20Actions
    {
        internal Action<string> Navigate, Command;
        internal Action<string, bool> SetFlag;
        internal Action<string> SaveKey;
        internal Action<int, bool> SelectQuarantine;
        internal Action<bool> SelectAllQuarantine;
        internal Action<int, bool> OpenFile;
        internal Action<int> SelectDetailTab;
        internal Action<DateTime, DateTime, string> FilterHistory;
        internal Action<int, List<string>, bool> StartAdvanced;
    }
}
