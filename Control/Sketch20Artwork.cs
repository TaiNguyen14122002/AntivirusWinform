using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ScanAndRemoveVirus.Control
{
    // Artwork stays vector based so it remains sharp at every window size.
    internal static class Sketch20Artwork
    {
        private static Color C(string value) { return ColorTranslator.FromHtml(value); }
        private static void Fill(Graphics g, GraphicsPath path, Color top, Color bottom, RectangleF bounds)
        {
            using (var brush = new LinearGradientBrush(bounds, top, bottom, 55f)) g.FillPath(brush, path);
        }
        private static GraphicsPath Shield(float inset = 0)
        {
            var p = new GraphicsPath();
            p.AddBezier(108, 18 + inset, 82, 40, 68, 43, 34 + inset, 49);
            p.AddBezier(34 + inset, 49, 30 + inset, 134, 50, 181, 108, 213 - inset);
            p.AddBezier(108, 213 - inset, 164, 180, 184 - inset, 135, 182 - inset, 49);
            p.AddBezier(182 - inset, 49, 150, 43, 132, 35, 108, 18 + inset);
            p.CloseFigure(); return p;
        }
        internal static void Draw(Graphics graphics, Rectangle bounds, string kind)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            var state = graphics.Save();
            try
            {
                graphics.SetClip(bounds, CombineMode.Intersect);
                graphics.TranslateTransform(bounds.X, bounds.Y);
                graphics.ScaleTransform(bounds.Width / 240f, bounds.Height / 240f);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                var g = graphics;
                using (var glow = new SolidBrush(Color.FromArgb(75, 255, 255, 255))) g.FillEllipse(glow, 18, 26, 212, 198);
                using (var orbit = new Pen(Color.FromArgb(190, 255, 255, 255), 1.8f))
                {
                    g.DrawArc(orbit, 4, 62, 232, 122, 150, 215);
                    g.DrawArc(orbit, 38, 12, 165, 216, 15, 174);
                }
                if (kind == "data") { Database(g); }
                else if (kind == "vt") { VirusTotal(g); }
                else
                {
                    bool amber = kind == "off", red = kind == "risk";
                    if (kind == "scan" || red) Laptop(g, red);
                    if (kind == "scan" || red) { g.TranslateTransform(34, -9); g.ScaleTransform(.83f, .83f); }
                    using (var outer = Shield())
                    {
                        Fill(g, outer, amber ? C("#fff2b7") : red ? C("#ffd4df") : C("#d0f3ff"), amber ? C("#ffaf18") : red ? C("#ff4970") : C("#2b8dff"), new RectangleF(30, 18, 156, 198));
                        using (var rim = new Pen(Color.White, 2.5f)) g.DrawPath(rim, outer);
                    }
                    using (var inner = Shield(12))
                    {
                        Fill(g, inner, amber ? C("#ffcd21") : red ? C("#ff4a6e") : C("#109aff"), amber ? C("#f58500") : red ? C("#ee003e") : C("#0034ce"), new RectangleF(44, 30, 125, 175));
                        using (var rim = new Pen(Color.FromArgb(180, 235, 253, 255), 1.8f)) g.DrawPath(rim, inner);
                    }
                    using (var half = new GraphicsPath())
                    {
                        half.AddBezier(108, 31, 84, 49, 65, 53, 46, 56);
                        half.AddBezier(46, 56, 45, 122, 60, 165, 108, 198);
                        half.CloseFigure(); Fill(g, half, Color.FromArgb(90, 120, 217, 255), Color.FromArgb(8, 255, 255, 255), new RectangleF(44, 30, 67, 170));
                    }
                    if (amber || red)
                    {
                        using (var pen = new Pen(Color.White, 10f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLine(pen, 108, 91, 108, 134);
                        using (var brush = new SolidBrush(Color.White)) g.FillEllipse(brush, 102, 151, 12, 12);
                    }
                    else using (var pen = new Pen(Color.White, 13f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) g.DrawLines(pen, new[] { new PointF(77, 116), new PointF(100, 139), new PointF(143, 91) });
                    if (kind == "scan")
                    {
                        using (var brush = new SolidBrush(Color.FromArgb(175, 209, 243, 255))) g.FillEllipse(brush, 119, 136, 50, 50);
                        using (var pen = new Pen(Color.White, 7)) { g.DrawEllipse(pen, 119, 136, 50, 50); g.DrawLine(pen, 160, 177, 185, 207); }
                    }
                    if (amber)
                    {
                        Sketch20Style.Round(g, new Rectangle(110, 176, 77, 37), 18, C("#ff9700"), Color.White);
                        using (var brush = new SolidBrush(Color.White)) g.FillEllipse(brush, 115, 180, 28, 28);
                    }
                }
                using (var star = new Pen(Color.White, 2))
                { g.DrawLine(star, 208, 32, 208, 46); g.DrawLine(star, 201, 39, 215, 39); g.DrawLine(star, 22, 155, 22, 166); g.DrawLine(star, 17, 160, 27, 160); }
            }
            finally { graphics.Restore(state); }
        }
        private static void Laptop(Graphics g, bool red)
        {
            var screen = new Rectangle(22, 63, 159, 129);
            Sketch20Style.Round(g, screen, 12, red ? C("#ff567b") : C("#458eff"), Color.White);
            Sketch20Style.Round(g, new Rectangle(29, 72, 144, 108), 5, red ? C("#ffd5df") : C("#0751cd"), C("#d6f3ff"));
            using (var keyboard = new GraphicsPath())
            {
                keyboard.AddPolygon(new[] { new PointF(22, 191), new PointF(181, 191), new PointF(221, 217), new PointF(48, 225), new PointF(7, 210) });
                Fill(g, keyboard, Color.White, C("#94caff"), new RectangleF(7, 191, 214, 34));
            }
        }
        private static void VirusTotal(Graphics g)
        {
            Sketch20Style.Round(g, new Rectangle(48, 44, 140, 162), 23, C("#379cff"), C("#b0e8ff"));
            Sketch20Style.Round(g, new Rectangle(53, 48, 130, 154), 20, C("#0870f4"), Color.White);
            using (var p = new Pen(Color.White, 11) { LineJoin = LineJoin.Miter }) g.DrawLines(p, new[] { new PointF(154, 88), new PointF(90, 88), new PointF(121, 123), new PointF(88, 159), new PointF(154, 159) });
            Sketch20Style.Round(g, new Rectangle(185, 139, 29, 42), 5, Color.FromArgb(225, 240, 255, 255), Color.White);
        }
        private static void Database(Graphics g)
        {
            for (int i = 2; i >= 0; i--)
            {
                int y = 48 + i * 44;
                using (var brush = new LinearGradientBrush(new Rectangle(46, y, 128, 56), C("#69caff"), C("#0555e4"), 0f)) g.FillRectangle(brush, 46, y, 128, 49);
                using (var brush = new SolidBrush(C("#0a72ed"))) g.FillEllipse(brush, 46, y + 29, 128, 39);
                using (var brush = new LinearGradientBrush(new Rectangle(46, y - 10, 128, 40), C("#c7f2ff"), C("#469ffe"), 90f)) g.FillEllipse(brush, 46, y - 10, 128, 40);
                using (var pen = new Pen(Color.White, 2)) g.DrawEllipse(pen, 46, y - 10, 128, 40);
            }
            using (var brush = new SolidBrush(C("#3991f4"))) g.FillEllipse(brush, 131, 145, 79, 79);
            using (var pen = new Pen(Color.White, 3)) g.DrawEllipse(pen, 131, 145, 79, 79);
            Sketch20Style.Icon(g, "settings", new Rectangle(148, 161, 46, 46), Color.White);
        }
        internal static void Waves(Graphics g, Rectangle bounds, Color color)
        {
            var state = g.Save();
            try
            {
                g.SetClip(bounds, CombineMode.Intersect);
                using (var wave = new GraphicsPath())
                {
                    wave.AddBezier(bounds.X + bounds.Width / 3, bounds.Bottom, bounds.X + bounds.Width * .5f, bounds.Y + bounds.Height * .3f, bounds.X + bounds.Width * .65f, bounds.Bottom + 60, bounds.Right, bounds.Y + bounds.Height * .45f);
                    wave.AddLine(bounds.Right, bounds.Bottom, bounds.X + bounds.Width / 3, bounds.Bottom); wave.CloseFigure();
                    using (var brush = new SolidBrush(Color.FromArgb(24, color))) g.FillPath(brush, wave);
                }
            }
            finally { g.Restore(state); }
        }
    }
}
