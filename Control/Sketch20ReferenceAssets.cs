using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScanAndRemoveVirus.Control
{
    // These assets are unchanged regions of the supplied mockups. Only artwork is
    // reused; all text, status, lists, inputs and actions remain native controls.
    internal static class Sketch20ReferenceAssets
    {
        private static readonly Dictionary<string, Bitmap> Images = new Dictionary<string, Bitmap>();
        private static Bitmap Get(string name)
        {
            Bitmap image;
            if (Images.TryGetValue(name, out image)) return image;
            using (var stream = typeof(Sketch20ReferenceAssets).Assembly.GetManifestResourceStream("ReferenceUI." + name + ".png"))
            {
                if (stream == null) throw new InvalidOperationException("Missing reference artwork: " + name);
                using (var source = new Bitmap(stream))
                {
                    image = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(image)) g.DrawImageUnscaled(source, 0, 0);
                }
            }
            // Blend only the edge of the source rectangle into the live card.
            // The shield/laptop pixels stay intact; this avoids a rectangular seam.
            var bits = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var pixels = new byte[bits.Stride * bits.Height]; Marshal.Copy(bits.Scan0, pixels, 0, pixels.Length);
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
            {
                float distance = Math.Min(Math.Min(x, image.Width - 1 - x), Math.Min(y, image.Height - 1 - y));
                float opacity = name.StartsWith("icon-") ? 1f : Math.Min(1f, distance / 9f);
                if (name == "risk" && x < 37 && y > 43 && y < 88) opacity = 0;
                pixels[y * bits.Stride + x * 4 + 3] = (byte)(255 * opacity);
            }
            Marshal.Copy(pixels, 0, bits.Scan0, pixels.Length); image.UnlockBits(bits);
            Images.Add(name, image); return image;
        }
        internal static void Draw(Graphics graphics, Rectangle bounds, string name)
        {
            var state = graphics.Save();
            try { graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; graphics.DrawImage(Get(name), bounds); }
            finally { graphics.Restore(state); }
        }
        internal static void Logo(Graphics graphics, Rectangle bounds)
        {
            var state = graphics.Save();
            try
            {
                graphics.TranslateTransform(bounds.X, bounds.Y); graphics.ScaleTransform(bounds.Width / 24f, bounds.Height / 28f);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = new GraphicsPath())
                {
                    path.AddPolygon(new[] { new PointF(12, 0), new PointF(23, 5), new PointF(22, 17), new PointF(18, 23), new PointF(12, 28), new PointF(6, 23), new PointF(2, 17), new PointF(1, 5) });
                    using (var b = new LinearGradientBrush(new Rectangle(0, 0, 24, 28), Color.FromArgb(0, 173, 255), Sketch20Style.Blue, 0f)) graphics.FillPath(b, path);
                }
                using (var b = new SolidBrush(Color.FromArgb(30, 0, 0, 100))) graphics.FillPolygon(b, new[] { new PointF(12, 0), new PointF(23, 5), new PointF(22, 17), new PointF(18, 23), new PointF(12, 28) });
                using (var p = new Pen(Color.White, 2.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) graphics.DrawLines(p, new[] { new PointF(6, 13), new PointF(10, 17), new PointF(18, 8) });
            }
            finally { graphics.Restore(state); }
        }
        internal static void FilledIcon(Graphics graphics, string name, Rectangle bounds, Color color)
        {
            if (name != "shield-check" && name != "house" && name != "file" && name != "bolt") { Sketch20Style.Icon(graphics, name, bounds, color); return; }
            var state = graphics.Save();
            try
            {
                graphics.TranslateTransform(bounds.X, bounds.Y); graphics.ScaleTransform(bounds.Width / 24f, bounds.Height / 24f);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(color))
                {
                    if (name == "shield-check") graphics.FillPolygon(b, new[] { new PointF(12, 1), new PointF(22, 5), new PointF(21, 15), new PointF(17, 20), new PointF(12, 23), new PointF(7, 20), new PointF(3, 15), new PointF(2, 5) });
                    else if (name == "house") graphics.FillPolygon(b, new[] { new PointF(1, 11), new PointF(12, 1), new PointF(23, 11), new PointF(20, 11), new PointF(20, 22), new PointF(15, 22), new PointF(15, 14), new PointF(9, 14), new PointF(9, 22), new PointF(4, 22), new PointF(4, 11) });
                    else if (name == "bolt") graphics.FillPolygon(b, new[] { new PointF(11, 0), new PointF(4, 13), new PointF(10, 13), new PointF(7, 24), new PointF(21, 9), new PointF(14, 9), new PointF(17, 0) });
                    else graphics.FillPolygon(b, new[] { new PointF(4, 1), new PointF(14, 1), new PointF(21, 8), new PointF(21, 23), new PointF(4, 23) });
                }
                if (name == "shield-check") using (var p = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) graphics.DrawLines(p, new[] { new PointF(7, 11), new PointF(11, 15), new PointF(17, 8) });
                if (name == "file") using (var p = new Pen(Color.White, 1.2f)) { graphics.DrawLine(p, 14, 1, 14, 8); graphics.DrawLine(p, 14, 8, 21, 8); graphics.DrawLine(p, 7, 12, 17, 12); graphics.DrawLine(p, 7, 16, 17, 16); graphics.DrawLine(p, 7, 19, 17, 19); }
            }
            finally { graphics.Restore(state); }
        }
    }
}
