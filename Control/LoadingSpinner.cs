using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// Vòng xoay "đang tải" của **dải loading quét** ở tab Tổng quan (25/09/2026, lần 9).
    /// Vẽ thuần GDI+ (`OnPaint`) + `Timer` nội bộ — cùng kiểu với `VtDonut`, không dùng thư viện ngoài.
    /// Vòng xoay tự chạy khi control được hiện và tự dừng khi bị ẩn (`OnVisibleChanged`), nên
    /// `UcTongQuan` chỉ cần bật/tắt `Visible` của dải loading là animation đi theo — không có
    /// Timer nào chạy nền lúc máy rảnh.
    /// </summary>
    public sealed class LoadingSpinner : System.Windows.Forms.Control
    {
        private readonly Timer timer;
        private int goc;                    // góc quay hiện tại (0..330, mỗi nhịp +30°)

        /// <summary>Màu vòng nền (đường tròn mờ phía sau cung đang quay) — `Theme.StyleLoadingStrip` gán.</summary>
        public Color MauVong = Theme.BlueSoft;

        /// <summary>Màu cung đang quay — `Theme.StyleLoadingStrip` gán.</summary>
        public Color MauQuay = Theme.Blue;

        /// <summary>Độ dày nét vẽ (px).</summary>
        public float DoDay = 3F;

        public LoadingSpinner()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Theme.BlueTint;
            timer = new Timer();
            timer.Interval = 60;            // ~16 vòng/giây ở bước 30° -> mắt thấy quay liền mạch
            timer.Tick += delegate
            {
                goc = (goc + 30) % 360;
                Invalidate();
            };
        }

        /// <summary>
        /// Góc quay hiện tại (độ). `Tests\UiEndToEnd.cs` đọc thuộc tính này 2 lần để chứng minh
        /// vòng xoay **đang quay thật** (giá trị đổi sau vài nhịp Timer), chứ không chỉ là ảnh tĩnh.
        /// </summary>
        public int Goc { get { return goc; } }

        /// <summary>Bật animation (gọi khi dải loading hiện ra).</summary>
        public void BatDau()
        {
            if (!timer.Enabled) timer.Enabled = true;
        }

        /// <summary>Dừng animation (gọi khi phiên quét kết thúc).</summary>
        public void Dung()
        {
            if (timer.Enabled) timer.Enabled = false;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) BatDau(); else Dung();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int canh = Math.Min(Width, Height);
            if (canh < 8) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var box = new Rectangle((Width - canh) / 2 + 2, (Height - canh) / 2 + 2, canh - 4, canh - 4);
            using (var nen = new Pen(MauVong, DoDay))
                e.Graphics.DrawArc(nen, box, 0F, 359.9F);
            using (var pen = new Pen(MauQuay, DoDay))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                e.Graphics.DrawArc(pen, box, -90F + goc, 100F);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && timer != null) timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
