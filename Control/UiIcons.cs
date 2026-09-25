using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// KHO ICON DUY NHẤT của app: vẽ bằng GDI+ (System.Drawing) — không thêm thư viện ngoài,
    /// không phụ thuộc font biểu tượng của Windows. Icon được cache theo (tên, cỡ, màu) nên
    /// mỗi biến thể chỉ vẽ một lần. Dùng cho hero trạng thái (khiên xanh / tròn đỏ),
    /// các nút có icon, và chấm màu ở danh sách "Hoạt động gần đây".
    /// </summary>
    public static class UiIcons
    {
        static readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();

        static Bitmap Get(string key, int size, Color color, Action<Graphics, int, Color> draw)
        {
            string k = key + "|" + size + "|" + color.ToArgb();
            Bitmap bmp;
            if (cache.TryGetValue(k, out bmp)) return bmp;
            bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            bmp.SetResolution(96, 96);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                draw(g, size, color);
            }
            cache[k] = bmp;
            return bmp;
        }

        static Color Lighten(Color c, float amount)
        {
            return Color.FromArgb(c.A,
                (int)Math.Min(255, c.R + (255 - c.R) * amount),
                (int)Math.Min(255, c.G + (255 - c.G) * amount),
                (int)Math.Min(255, c.B + (255 - c.B) * amount));
        }

        // ================= HERO TRẠNG THÁI =================

        /// <summary>Khiên xanh + dấu ✓ trắng (trạng thái an toàn của tab Tổng quan).</summary>
        public static Bitmap ShieldOk(int size)
        {
            return Get("shield-ok", size, Theme.Green, delegate(Graphics g, int s, Color c)
            {
                using (GraphicsPath p = ShieldPath(s))
                using (var brush = new LinearGradientBrush(new Rectangle(0, 0, s, s), Lighten(c, 0.18f), c, 90f))
                    g.FillPath(brush, p);
                using (var pen = new Pen(Color.White, s * 0.10f))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 24f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(7.4f * u, 12.4f * u),
                        new PointF(10.6f * u, 15.6f * u),
                        new PointF(16.9f * u, 8.6f * u)
                    });
                }
            });
        }

        /// <summary>Hình tròn đỏ + dấu ! trắng (trạng thái phát hiện đe dọa).</summary>
        public static Bitmap AlertCircle(int size)
        {
            return Get("alert", size, Theme.Red, delegate(Graphics g, int s, Color c)
            {
                using (var brush = new LinearGradientBrush(new Rectangle(0, 0, s, s), Lighten(c, 0.20f), c, 90f))
                    g.FillEllipse(brush, 1, 1, s - 2, s - 2);
                using (var pen = new Pen(Color.White, s * 0.115f))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 24f;
                    g.DrawLine(pen, 12f * u, 6.4f * u, 12f * u, 13.6f * u);
                    g.DrawLine(pen, 12f * u, 17.1f * u, 12f * u, 17.6f * u);
                }
            });
        }

        static GraphicsPath ShieldPath(int s)
        {
            var p = new GraphicsPath();
            p.AddBezier(0.5f * s, 0.04f * s, 0.72f * s, 0.12f * s, 0.87f * s, 0.17f * s, 0.95f * s, 0.21f * s);
            p.AddBezier(0.95f * s, 0.21f * s, 0.95f * s, 0.62f * s, 0.66f * s, 0.88f * s, 0.5f * s, 0.97f * s);
            p.AddBezier(0.5f * s, 0.97f * s, 0.34f * s, 0.88f * s, 0.05f * s, 0.62f * s, 0.05f * s, 0.21f * s);
            p.AddBezier(0.05f * s, 0.21f * s, 0.13f * s, 0.17f * s, 0.28f * s, 0.12f * s, 0.5f * s, 0.04f * s);
            p.CloseFigure();
            return p;
        }

        // ================= ICON NÚT / NHÃN NHỎ =================

        /// <summary>Tam giác ▶ (bắt đầu quét).</summary>
        public static Bitmap Play(int size, Color color)
        {
            return Get("play", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var brush = new SolidBrush(c))
                    g.FillPolygon(brush, new[]
                    {
                        new PointF(0.24f * s, 0.14f * s),
                        new PointF(0.84f * s, 0.50f * s),
                        new PointF(0.24f * s, 0.86f * s)
                    });
            });
        }

        /// <summary>Ô vuông ⏹ (hủy phiên quét).</summary>
        public static Bitmap Stop(int size, Color color)
        {
            return Get("stop", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var brush = new SolidBrush(c))
                    g.FillRectangle(brush, 0.22f * s, 0.22f * s, 0.56f * s, 0.56f * s);
            });
        }

        /// <summary>Bánh răng ⚙ (mở trang Quét nâng cao).</summary>
        public static Bitmap Gear(int size, Color color)
        {
            return Get("gear", size, color, delegate(Graphics g, int s, Color c)
            {
                float cx = s / 2f, cy = s / 2f;
                using (var brush = new SolidBrush(c))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        g.ResetTransform();
                        g.TranslateTransform(cx, cy);
                        g.RotateTransform(i * 45f);
                        g.FillRectangle(brush, -s * 0.085f, -s * 0.47f, s * 0.17f, s * 0.26f);
                    }
                    g.ResetTransform();
                    g.FillEllipse(brush, cx - s * 0.33f, cy - s * 0.33f, s * 0.66f, s * 0.66f);
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(cx - s * 0.14f, cy - s * 0.14f, s * 0.28f, s * 0.28f);
                        g.SetClip(path, CombineMode.Exclude);
                        g.CompositingMode = CompositingMode.SourceCopy;   // xóa hẳn pixel để lỗ răng trong suốt
                        using (var clear = new SolidBrush(Color.Transparent))
                            g.FillRectangle(clear, 0, 0, s, s);
                        g.CompositingMode = CompositingMode.SourceOver;
                        g.ResetClip();
                    }
                }
            });
        }

        /// <summary>Mũi tên ← (quay lại).</summary>
        public static Bitmap ArrowLeft(int size, Color color)
        {
            return Get("arrow-left", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.6f, s * 0.12f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 16f;
                    g.DrawLine(pen, 14f * u, 8f * u, 3.2f * u, 8f * u);
                    g.DrawLines(pen, new[]
                    {
                        new PointF(7.4f * u, 3.6f * u),
                        new PointF(3f * u, 8f * u),
                        new PointF(7.4f * u, 12.4f * u)
                    });
                }
            });
        }

        /// <summary>Vòng lặp ⟳ (quét lại).</summary>
        public static Bitmap Refresh(int size, Color color)
        {
            return Get("refresh", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.6f, s * 0.12f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float m = s * 0.20f;
                    g.DrawArc(pen, m, m, s - 2 * m, s - 2 * m, 45f, 275f);
                    float r = (s - 2 * m) / 2f;
                    var tip = new PointF(m + r + r * (float)Math.Cos(45 * Math.PI / 180),
                                         m + r + r * (float)Math.Sin(45 * Math.PI / 180));
                    using (var brush = new SolidBrush(c))
                        g.FillPolygon(brush, new[]
                        {
                            new PointF(tip.X + s * 0.05f, tip.Y - s * 0.17f),
                            new PointF(tip.X - s * 0.17f, tip.Y - s * 0.02f),
                            new PointF(tip.X + s * 0.11f, tip.Y + s * 0.11f)
                        });
                }
            });
        }

        /// <summary>Hai tờ giấy (sao chép giá trị).</summary>
        public static Bitmap Copy(int size, Color color)
        {
            return Get("copy", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.10f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 16f;
                    g.DrawRectangle(pen, 2f * u, 2f * u, 8f * u, 9f * u);
                    g.DrawRectangle(pen, 6f * u, 5.5f * u, 8f * u, 8.5f * u);
                }
            });
        }

        /// <summary>Mũi tên mở ra ngoài ↗ (mở báo cáo trên trình duyệt).</summary>
        public static Bitmap External(int size, Color color)
        {
            return Get("external", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.10f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 16f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(3f * u, 13f * u),
                        new PointF(3f * u, 3f * u),
                        new PointF(6.5f * u, 3f * u)
                    });
                    g.DrawLines(pen, new[]
                    {
                        new PointF(9f * u, 4f * u),
                        new PointF(13f * u, 4f * u),
                        new PointF(13f * u, 8f * u)
                    });
                    g.DrawLine(pen, 12.6f * u, 4.4f * u, 7f * u, 10f * u);
                }
            });
        }

        /// <summary>Vòng tròn chữ i (chú thích ⓘ cho tuỳ chọn quét).</summary>
        public static Bitmap Info(int size, Color color)
        {
            return Get("info", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.3f, s * 0.10f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    g.DrawEllipse(pen, s * 0.10f, s * 0.10f, s * 0.80f, s * 0.80f);
                    float u = s / 16f;
                    g.DrawLine(pen, 10f * u, 7.4f * u, 10f * u, 11.6f * u);
                    g.DrawLine(pen, 10f * u, 4.6f * u, 10f * u, 5.1f * u);
                }
            });
        }

        /// <summary>Chấm tròn (icon dòng "Hoạt động gần đây").</summary>
        public static Bitmap Dot(int size, Color color)
        {
            return Get("dot", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var brush = new SolidBrush(c))
                    g.FillEllipse(brush, s * 0.18f, s * 0.18f, s * 0.64f, s * 0.64f);
            });
        }
    }
}

