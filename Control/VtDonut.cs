using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// Biểu đồ donut "X/72 công cụ phát hiện" của tab VirusTotal (tab Chi tiết kết quả quét).
    /// Vẽ thuần GDI+ (OnPaint) — không dùng thư viện chart ngoài.
    /// </summary>
    public sealed class VtDonut : System.Windows.Forms.Control
    {
        int malicious;
        int engines = 72;
        string caption = "Chưa tra VirusTotal";

        public VtDonut()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Theme.PageBg;
        }

        /// <summary>Nạp số liệu: số công cụ báo độc / tổng số công cụ đã phân tích.</summary>
        public void SetData(int maliciousCount, int engineCount, string captionText)
        {
            malicious = maliciousCount < 0 ? 0 : maliciousCount;
            engines = engineCount <= 0 ? 72 : engineCount;
            if (malicious > engines) malicious = engines;
            caption = captionText;
            Invalidate();
        }

        /// <summary>Nạp trạng thái chưa có số liệu (chưa tra / lỗi / không có mẫu).</summary>
        public void SetEmpty(string captionText)
        {
            malicious = 0;
            engines = 0;
            caption = captionText;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Math.Min(Width, Height) - 24;
            if (size < 40) size = Math.Max(24, Math.Min(Width, Height));
            var box = new Rectangle((Width - size) / 2, (Height - size) / 2 - 8, size, size);
            float thickness = size * 0.16f;

            using (var pen = new Pen(Theme.Line, thickness))
                e.Graphics.DrawArc(pen, box, -90f, 359.9f);

            if (engines > 0)
            {
                float sweep = 359.9f * malicious / engines;
                Color arc = malicious > 0 ? Theme.Red : Theme.Green;
                if (sweep > 0.05f)
                {
                    using (var pen = new Pen(arc, thickness))
                        e.Graphics.DrawArc(pen, box, -90f, sweep);
                }
            }

            Color valueColor = engines == 0 ? Theme.TextGray : (malicious > 0 ? Theme.Red : Theme.Green);
            string valueText = engines == 0 ? "—" : string.Format("{0}/{1}", malicious, engines);
            using (var valueFont = new Font("Segoe UI", Math.Max(11f, size * 0.20f), FontStyle.Bold))
            using (var brush = new SolidBrush(valueColor))
            using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                var inner = new RectangleF(box.X, box.Y + box.Height * 0.32f, box.Width, box.Height * 0.30f);
                e.Graphics.DrawString(valueText, valueFont, brush, inner, fmt);
            }
            if (!string.IsNullOrEmpty(caption))
            {
                using (var capFont = new Font("Segoe UI", 8.25f, FontStyle.Regular))
                using (var brush = new SolidBrush(Theme.TextGray))
                using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    var inner = new RectangleF(0, box.Bottom + 2, Width, 30);
                    e.Graphics.DrawString(caption, capFont, brush, inner, fmt);
                }
            }
        }
    }
}
