using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

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

        /// <summary>
        /// Huy hiệu hero của tab Tổng quan (mockup): đĩa tròn màu rất nhạt + icon ở giữa.
        /// Tỉ lệ lấy đúng mockup — đĩa 100%, icon ~52% đường kính đĩa.
        /// </summary>
        public static Bitmap HeroBadge(int size, bool ok)
        {
            return Get("hero-badge-" + (ok ? "ok" : "warn"), size, ok ? Theme.Green : Theme.Red,
                delegate(Graphics g, int s, Color c)
                {
                    using (var b = new SolidBrush(Lighten(c, 0.90f)))
                        g.FillEllipse(b, 0, 0, s - 1, s - 1);
                    int inner = (int)(s * 0.52f);
                    Bitmap ico = ok ? ShieldOk(inner) : AlertCircle(inner);
                    g.DrawImage(ico, (s - inner) / 2, (s - inner) / 2, inner, inner);
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
                    // Lỗ trục răng: phải ghi đè bằng pixel trong suốt (SourceCopy) vì nền bitmap
                    // vốn trong suốt nên vẽ chồng bằng SourceOver không xóa được gì.
                    // KHÔNG dùng SetClip(..., Exclude) quanh đường tròn này: vùng Exclude là phần
                    // NGOÀI đường tròn, nên lệnh xóa trúng toàn bộ thân + răng và chỉ chừa lại
                    // lòng trục — bánh răng biến thành một chấm nhỏ (lỗi cũ).
                    g.CompositingMode = CompositingMode.SourceCopy;
                    using (var clear = new SolidBrush(Color.Transparent))
                        g.FillEllipse(clear, cx - s * 0.14f, cy - s * 0.14f, s * 0.28f, s * 0.28f);
                    g.CompositingMode = CompositingMode.SourceOver;
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

        // =====================================================================
        // BỘ ICON CHO GIAO DIỆN MỚI (theo mockup Design/*.jpg)
        // =====================================================================

        static readonly Dictionary<string, Bitmap> recolorCache = new Dictionary<string, Bitmap>();

        /// <summary>
        /// Đổi màu một bitmap icon đã vẽ (giữ nguyên độ trong suốt). UiNavItem dùng để tô icon
        /// theo trạng thái chọn/mặc định mà không phải vẽ lại hình. Có cache theo (bitmap, màu).
        /// </summary>
        public static Bitmap Recolor(Bitmap src, Color color)
        {
            if (src == null) return null;
            string k = src.GetHashCode() + "|" + color.ToArgb() + "|" + src.Width;
            Bitmap hit;
            if (recolorCache.TryGetValue(k, out hit)) return hit;
            var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            bmp.SetResolution(96, 96);
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    Color p = src.GetPixel(x, y);
                    bmp.SetPixel(x, y, Color.FromArgb(p.A, color));
                }
            recolorCache[k] = bmp;
            return bmp;
        }

        // ---- Icon điều hướng sidebar ----

        /// <summary>Ngôi nhà (Tổng quan).</summary>
        public static Bitmap Home(int size, Color color)
        {
            return Get("home", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.5f, s * 0.095f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 20f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(2.6f * u, 8.6f * u),
                        new PointF(10f * u, 2.6f * u),
                        new PointF(17.4f * u, 8.6f * u)
                    });
                    g.DrawLines(pen, new[]
                    {
                        new PointF(4.6f * u, 8.6f * u),
                        new PointF(4.6f * u, 17f * u),
                        new PointF(15.4f * u, 17f * u),
                        new PointF(15.4f * u, 8.6f * u)
                    });
                }
            });
        }

        /// <summary>Kính lúp + khiên (Bảo vệ).</summary>
        public static Bitmap ShieldSearch(int size, Color color)
        {
            return Get("shield-search", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.5f, s * 0.095f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 20f;
                    // thân khiên
                    g.DrawLines(pen, new[]
                    {
                        new PointF(10f * u, 2.4f * u),
                        new PointF(16.4f * u, 5f * u),
                        new PointF(16.4f * u, 9.4f * u),
                        new PointF(10f * u, 17.6f * u),
                        new PointF(3.6f * u, 9.4f * u),
                        new PointF(3.6f * u, 5f * u),
                        new PointF(10f * u, 2.4f * u)
                    });
                    // kính lúp
                    g.DrawEllipse(pen, 7.2f * u, 6.6f * u, 5.6f * u, 5.6f * u);
                    g.DrawLine(pen, 12f * u, 11.4f * u, 14.6f * u, 14f * u);
                }
            });
        }

        /// <summary>Khay/hộp lưu trữ (Cách ly).</summary>
        public static Bitmap Tray(int size, Color color)
        {
            return Get("tray", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.5f, s * 0.095f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 20f;
                    g.DrawRectangle(pen, 2.8f * u, 3.4f * u, 14.4f * u, 13.2f * u);
                    g.DrawLine(pen, 2.8f * u, 11.4f * u, 7f * u, 11.4f * u);
                    g.DrawLine(pen, 13f * u, 11.4f * u, 17.2f * u, 11.4f * u);
                    g.DrawLines(pen, new[]
                    {
                        new PointF(7f * u, 11.4f * u),
                        new PointF(8.4f * u, 14f * u),
                        new PointF(11.6f * u, 14f * u),
                        new PointF(13f * u, 11.4f * u)
                    });
                }
            });
        }

        /// <summary>Đồng hồ (Lịch sử).</summary>
        public static Bitmap Clock(int size, Color color)
        {
            return Get("clock", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.5f, s * 0.095f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 20f;
                    g.DrawEllipse(pen, 2.6f * u, 2.6f * u, 14.8f * u, 14.8f * u);
                    g.DrawLine(pen, 10f * u, 6f * u, 10f * u, 10.4f * u);
                    g.DrawLine(pen, 10f * u, 10.4f * u, 13.2f * u, 12.2f * u);
                }
            });
        }

        /// <summary>Logo thương hiệu ở đầu sidebar: khiên xanh đặc + dấu ✓ trắng.</summary>
        public static Bitmap BrandShield(int size)
        {
            return Get("brand-shield", size, Theme.Blue, delegate(Graphics g, int s, Color c)
            {
                using (GraphicsPath p = ShieldPath(s))
                using (var brush = new LinearGradientBrush(new Rectangle(0, 0, s, s), Lighten(c, 0.22f), c, 90f))
                    g.FillPath(brush, p);
                using (var pen = new Pen(Color.White, s * 0.105f))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 24f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(7.4f * u, 12.2f * u),
                        new PointF(10.6f * u, 15.4f * u),
                        new PointF(17f * u, 8.4f * u)
                    });
                }
            });
        }

        // ---- Icon nhỏ dùng chung ----

        /// <summary>Mũi tên ▾ (mở menu/combobox).</summary>
        public static Bitmap ChevronDown(int size, Color color)
        {
            return Get("chev-down", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.11f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 16f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(4f * u, 6.4f * u),
                        new PointF(8f * u, 10.4f * u),
                        new PointF(12f * u, 6.4f * u)
                    });
                }
            });
        }

        /// <summary>Mũi tên › (đi tiếp).</summary>
        public static Bitmap ChevronRight(int size, Color color)
        {
            return Get("chev-right", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.11f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 16f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(6.4f * u, 4f * u),
                        new PointF(10.4f * u, 8f * u),
                        new PointF(6.4f * u, 12f * u)
                    });
                }
            });
        }

        /// <summary>Kính lúp (tìm kiếm).</summary>
        public static Bitmap Search(int size, Color color)
        {
            return Get("search", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.105f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 16f;
                    g.DrawEllipse(pen, 2.6f * u, 2.6f * u, 8.4f * u, 8.4f * u);
                    g.DrawLine(pen, 10.4f * u, 10.4f * u, 13.4f * u, 13.4f * u);
                }
            });
        }

        /// <summary>Mũi tên tải lên ⬆ (vùng kéo-thả tệp).</summary>
        public static Bitmap Upload(int size, Color color)
        {
            return Get("upload", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.5f, s * 0.10f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 20f;
                    g.DrawLine(pen, 10f * u, 15.4f * u, 10f * u, 4.4f * u);
                    g.DrawLines(pen, new[]
                    {
                        new PointF(6f * u, 8.4f * u),
                        new PointF(10f * u, 4.4f * u),
                        new PointF(14f * u, 8.4f * u)
                    });
                    g.DrawLines(pen, new[]
                    {
                        new PointF(4f * u, 13.4f * u),
                        new PointF(4f * u, 16.4f * u),
                        new PointF(16f * u, 16.4f * u),
                        new PointF(16f * u, 13.4f * u)
                    });
                }
            });
        }

        /// <summary>Thư mục (chọn thư mục / đường dẫn loại "Thư mục").</summary>
        public static Bitmap Folder(int size, Color color)
        {
            return Get("folder", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.095f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 20f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(2.6f * u, 16f * u),
                        new PointF(2.6f * u, 4.6f * u),
                        new PointF(8f * u, 4.6f * u),
                        new PointF(9.6f * u, 7f * u),
                        new PointF(17.4f * u, 7f * u),
                        new PointF(17.4f * u, 16f * u),
                        new PointF(2.6f * u, 16f * u)
                    });
                }
            });
        }

        /// <summary>Màn hình máy tính (card "Thông tin hệ thống").</summary>
        public static Bitmap Monitor(int size, Color color)
        {
            return Get("monitor", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.095f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 20f;
                    g.DrawRectangle(pen, 2.6f * u, 4f * u, 14.8f * u, 10f * u);
                    g.DrawLine(pen, 7.4f * u, 17f * u, 12.6f * u, 17f * u);
                    g.DrawLine(pen, 10f * u, 14f * u, 10f * u, 17f * u);
                }
            });
        }

        /// <summary>Quả địa cầu (kết nối mạng).</summary>
        public static Bitmap Globe(int size, Color color)
        {
            return Get("globe", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.095f)))
                {
                    float u = s / 20f;
                    g.DrawEllipse(pen, 2.6f * u, 2.6f * u, 14.8f * u, 14.8f * u);
                    g.DrawEllipse(pen, 7f * u, 2.6f * u, 6f * u, 14.8f * u);
                    g.DrawLine(pen, 2.6f * u, 10f * u, 17.4f * u, 10f * u);
                }
            });
        }

        /// <summary>Thùng rác (xóa).</summary>
        public static Bitmap Trash(int size, Color color)
        {
            return Get("trash", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.095f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 20f;
                    g.DrawLine(pen, 3f * u, 5.6f * u, 17f * u, 5.6f * u);
                    g.DrawLines(pen, new[]
                    {
                        new PointF(5.4f * u, 5.6f * u),
                        new PointF(5.4f * u, 17f * u),
                        new PointF(14.6f * u, 17f * u),
                        new PointF(14.6f * u, 5.6f * u)
                    });
                    g.DrawLines(pen, new[]
                    {
                        new PointF(7.8f * u, 5.6f * u),
                        new PointF(7.8f * u, 3.4f * u),
                        new PointF(12.2f * u, 3.4f * u),
                        new PointF(12.2f * u, 5.6f * u)
                    });
                    g.DrawLine(pen, 8.6f * u, 8.6f * u, 8.6f * u, 14f * u);
                    g.DrawLine(pen, 11.4f * u, 8.6f * u, 11.4f * u, 14f * u);
                }
            });
        }

        /// <summary>Dấu cộng (thêm vị trí quét).</summary>
        public static Bitmap Plus(int size, Color color)
        {
            return Get("plus", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.5f, s * 0.12f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 16f;
                    g.DrawLine(pen, 8f * u, 3.4f * u, 8f * u, 12.6f * u);
                    g.DrawLine(pen, 3.4f * u, 8f * u, 12.6f * u, 8f * u);
                }
            });
        }

        /// <summary>Dấu ✕ (đóng/xóa).</summary>
        public static Bitmap Close(int size, Color color)
        {
            return Get("close", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.4f, s * 0.11f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    float u = s / 16f;
                    g.DrawLine(pen, 4.4f * u, 4.4f * u, 11.6f * u, 11.6f * u);
                    g.DrawLine(pen, 11.6f * u, 4.4f * u, 4.4f * u, 11.6f * u);
                }
            });
        }

        /// <summary>Dấu ✓ (đã chọn / hợp lệ).</summary>
        public static Bitmap Check(int size, Color color)
        {
            return Get("check", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.5f, s * 0.13f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 16f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(3.4f * u, 8.4f * u),
                        new PointF(6.6f * u, 11.6f * u),
                        new PointF(12.6f * u, 4.6f * u)
                    });
                }
            });
        }

        // ---- Icon theo loại tệp (bảng danh sách tệp/đe dọa) ----

        /// <summary>Icon tệp theo phần mở rộng: trang giấy + dải màu nhận diện loại tệp.</summary>
        public static Bitmap FileIcon(int size, string ext)
        {
            string e = (ext ?? "").TrimStart('.').ToLowerInvariant();
            Color tone;
            switch (e)
            {
                case "exe": case "dll": case "sys": case "msi": tone = Color.FromArgb(37, 99, 235); break;
                case "zip": case "rar": case "7z": case "gz": tone = Color.FromArgb(180, 130, 40); break;
                case "pdf": tone = Color.FromArgb(220, 38, 38); break;
                case "doc": case "docx": tone = Color.FromArgb(43, 87, 154); break;
                case "ps1": case "bat": case "cmd": case "vbs": tone = Color.FromArgb(112, 66, 176); break;
                default: tone = Theme.TextGray; break;
            }
            return Get("file-" + e, size, tone, delegate(Graphics g, int s, Color c)
            {
                float u = s / 20f;
                // trang giấy có góc gấp
                using (var path = new GraphicsPath())
                {
                    path.AddLines(new[]
                    {
                        new PointF(4.4f * u, 2.4f * u),
                        new PointF(11.6f * u, 2.4f * u),
                        new PointF(15.6f * u, 6.4f * u),
                        new PointF(15.6f * u, 17.6f * u),
                        new PointF(4.4f * u, 17.6f * u)
                    });
                    path.CloseFigure();
                    using (var b = new SolidBrush(Color.FromArgb(38, c))) g.FillPath(b, path);
                    using (var pen = new Pen(c, Math.Max(1.2f, s * 0.085f))) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, path); }
                }
                // nếp gấp góc trên phải
                using (var pen = new Pen(c, Math.Max(1.2f, s * 0.085f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(11.6f * u, 2.4f * u),
                        new PointF(11.6f * u, 6.4f * u),
                        new PointF(15.6f * u, 6.4f * u)
                    });
                }
            });
        }

        /// <summary>
        /// Icon nhà cung cấp (Microsoft/Kaspersky/ESET/...): ô vuông bo góc mang màu nhận diện
        /// + chữ cái đầu. Không sao chép logo thật — chỉ là ký hiệu nhận biết trong bảng.
        /// </summary>
        public static Bitmap VendorIcon(int size, string name)
        {
            string n = string.IsNullOrEmpty(name) ? "?" : name;
            Color tone = VendorColor(n);
            return Get("vendor-" + n.ToLowerInvariant(), size, tone, delegate(Graphics g, int s, Color c)
            {
                using (GraphicsPath p = UiKit.Round(new Rectangle(0, 0, s - 1, s - 1), Math.Max(3, s / 5)))
                using (var b = new SolidBrush(c)) g.FillPath(b, p);
                string letter = n.Substring(0, 1).ToUpperInvariant();
                using (var f = new Font("Segoe UI", s * 0.56f, FontStyle.Bold, GraphicsUnit.Pixel))
                    TextRenderer.DrawText(g, letter, f, new Rectangle(0, 0, s, s), Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            });
        }

        static Color VendorColor(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "microsoft": return Color.FromArgb(0, 120, 212);
                case "kaspersky": return Color.FromArgb(0, 108, 68);
                case "eset": return Color.FromArgb(16, 122, 176);
                case "bitdefender": return Color.FromArgb(214, 30, 40);
                case "avast": return Color.FromArgb(255, 120, 0);
                case "avira": return Color.FromArgb(200, 30, 30);
                case "mcafee": return Color.FromArgb(196, 24, 30);
                case "symantec": return Color.FromArgb(252, 178, 22);
                case "trend micro": return Color.FromArgb(208, 40, 46);
                case "fortinet": return Color.FromArgb(218, 41, 28);
                case "google": return Color.FromArgb(66, 133, 244);
                case "clamav": return Color.FromArgb(38, 84, 168);
                case "sophos": return Color.FromArgb(0, 82, 155);
                case "panda": return Color.FromArgb(0, 122, 190);
                default: return Theme.TextGray;
            }
        }

        // ---- Icon hành vi (timeline "Phân tích hành vi") ----

        /// <summary>Icon hành vi theo khoá: process / registry / file / network / security.</summary>
        public static Bitmap Behavior(int size, string key)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "registry":
                    return Get("bh-registry", size, Theme.Amber, delegate(Graphics g, int s, Color c)
                    {
                        using (var pen = new Pen(c, Math.Max(1.4f, s * 0.095f)))
                        {
                            pen.LineJoin = LineJoin.Round;
                            float u = s / 20f;
                            g.DrawRectangle(pen, 3.4f * u, 3.4f * u, 13.2f * u, 13.2f * u);
                            g.DrawLine(pen, 3.4f * u, 8.2f * u, 16.6f * u, 8.2f * u);
                            g.DrawLine(pen, 3.4f * u, 12.4f * u, 16.6f * u, 12.4f * u);
                            g.DrawLine(pen, 10f * u, 8.2f * u, 10f * u, 16.6f * u);
                        }
                    });
                case "file":
                    return FileIcon(size, "dll");
                case "network":
                    return Globe(size, Theme.Blue);
                case "security":
                    return Get("bh-security", size, Theme.Red, delegate(Graphics g, int s, Color c)
                    {
                        using (var pen = new Pen(c, Math.Max(1.4f, s * 0.095f)))
                        {
                            pen.LineJoin = LineJoin.Round;
                            float u = s / 20f;
                            g.DrawLines(pen, new[]
                            {
                                new PointF(10f * u, 2.4f * u),
                                new PointF(16.4f * u, 5f * u),
                                new PointF(16.4f * u, 9.4f * u),
                                new PointF(10f * u, 17.6f * u),
                                new PointF(3.6f * u, 9.4f * u),
                                new PointF(3.6f * u, 5f * u),
                                new PointF(10f * u, 2.4f * u)
                            });
                            g.DrawLine(pen, 10f * u, 7f * u, 10f * u, 11.6f * u);
                            g.DrawLine(pen, 10f * u, 13.8f * u, 10f * u, 14.2f * u);
                        }
                    });
                default: // process
                    return Get("bh-process", size, Theme.Blue, delegate(Graphics g, int s, Color c)
                    {
                        using (var pen = new Pen(c, Math.Max(1.4f, s * 0.095f)))
                        {
                            pen.LineJoin = LineJoin.Round;
                            float u = s / 20f;
                            g.DrawRectangle(pen, 2.6f * u, 4f * u, 14.8f * u, 12f * u);
                            g.DrawLine(pen, 2.6f * u, 7.6f * u, 17.4f * u, 7.6f * u);
                            g.DrawLine(pen, 5.4f * u, 5.8f * u, 6.6f * u, 5.8f * u);
                        }
                    });
            }
        }

        // ================= ICON TRANG LỊCH SỬ (ảnh tham chiếu 26/09/2026 — README §3.3/§3.4) =================
        // Giữ lại từ nhánh main gốc: UcLichSu.cs gọi trực tiếp các icon này.

        /// <summary>Mũi tên → (nút "trang sau" ở chân trang Lịch sử).</summary>
        public static Bitmap ArrowRight(int size, Color color)
        {
            return Get("arrow-right", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.6f, s * 0.12f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 16f;
                    g.DrawLine(pen, 3.2f * u, 8f * u, 12.8f * u, 8f * u);
                    g.DrawLines(pen, new[]
                    {
                        new PointF(8.6f * u, 3.6f * u),
                        new PointF(13f * u, 8f * u),
                        new PointF(8.6f * u, 12.4f * u)
                    });
                }
            });
        }
        /// <summary>Ba chấm ngang ••• (nút "thêm thao tác" của trang Lịch sử).</summary>
        public static Bitmap More(int size, Color color)
        {
            return Get("more", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var brush = new SolidBrush(c))
                {
                    float d = Math.Max(2.4f, s * 0.17f);
                    for (int i = 0; i < 3; i++)
                        g.FillEllipse(brush, s * 0.5f - d / 2f + (i - 1) * s * 0.30f, s * 0.5f - d / 2f, d, d);
                }
            });
        }
        /// <summary>Màn hình máy tính (loại quét toàn bộ).</summary>
        public static Bitmap Desktop(int size, Color color)
        {
            return Get("desktop", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.2f, s * 0.10f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 16f;
                    g.DrawRectangle(pen, 1.8f * u, 2.6f * u, 12.4f * u, 8.4f * u);
                    g.DrawLine(pen, 5.4f * u, 13.4f * u, 10.6f * u, 13.4f * u);
                    g.DrawLine(pen, 8f * u, 11f * u, 8f * u, 13.4f * u);
                }
            });
        }
        /// <summary>Tia sét (loại quét nhanh).</summary>
        public static Bitmap Bolt(int size, Color color)
        {
            return Get("bolt", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var brush = new SolidBrush(c))
                {
                    float u = s / 16f;
                    g.FillPolygon(brush, new[]
                    {
                        new PointF(9.6f * u, 1.4f * u),
                        new PointF(3.6f * u, 9.2f * u),
                        new PointF(7.5f * u, 9.2f * u),
                        new PointF(6.1f * u, 14.6f * u),
                        new PointF(12.4f * u, 6.4f * u),
                        new PointF(8.5f * u, 6.4f * u)
                    });
                }
            });
        }
        /// <summary>Tờ tệp (quét tệp).</summary>
        public static Bitmap Doc(int size, Color color)
        {
            return Get("doc", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.2f, s * 0.10f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    float u = s / 16f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(3.4f * u, 1.8f * u), new PointF(9.4f * u, 1.8f * u),
                        new PointF(12.6f * u, 5.0f * u), new PointF(12.6f * u, 14.2f * u),
                        new PointF(3.4f * u, 14.2f * u), new PointF(3.4f * u, 1.8f * u)
                    });
                    g.DrawLines(pen, new[]
                    {
                        new PointF(9.4f * u, 1.8f * u), new PointF(9.4f * u, 5.0f * u),
                        new PointF(12.6f * u, 5.0f * u)
                    });
                }
            });
        }
        /// <summary>Khiên rỗng (cảnh báo "Bảo vệ thời gian thực").</summary>
        public static Bitmap Shield(int size, Color color)
        {
            return Get("shield", size, color, delegate(Graphics g, int s, Color c)
            {
                using (var pen = new Pen(c, Math.Max(1.2f, s * 0.10f)))
                {
                    pen.LineJoin = LineJoin.Round;
                    using (GraphicsPath p = ShieldPath(s))
                        g.DrawPath(pen, p);
                }
            });
        }
    }
}
