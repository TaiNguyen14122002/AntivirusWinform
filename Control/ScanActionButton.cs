using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    public class ScanActionButton : System.Windows.Forms.Control
    {
        private bool isHover;
        private bool isPressed;

        public bool IsPrimary { get; set; } = true;

        public int CornerRadius { get; set; } = 12;

        public Color PrimaryColor { get; set; } =
            Color.FromArgb(10, 86, 216);

        public Color BorderColor { get; set; } =
            Color.FromArgb(210, 220, 235);

        public ScanActionButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true
            );

            DoubleBuffered = true;

            Size = new Size(250, 56);

            Font = new Font(
                "Segoe UI",
                10,
                FontStyle.Bold
            );

            Cursor = Cursors.Hand;

            TabStop = true;
        }

        private GraphicsPath CreateRoundedPath(
            Rectangle rect,
            int radius)
        {
            GraphicsPath path = new GraphicsPath();

            int diameter = Math.Min(
                radius * 2,
                Math.Min(rect.Width, rect.Height)
            );

            if (diameter <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(
                rect.Left,
                rect.Top,
                diameter,
                diameter,
                180,
                90
            );

            path.AddArc(
                rect.Right - diameter,
                rect.Top,
                diameter,
                diameter,
                270,
                90
            );

            path.AddArc(
                rect.Right - diameter,
                rect.Bottom - diameter,
                diameter,
                diameter,
                0,
                90
            );

            path.AddArc(
                rect.Left,
                rect.Bottom - diameter,
                diameter,
                diameter,
                90,
                90
            );

            path.CloseFigure();

            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color background =
                Parent != null
                    ? Parent.BackColor
                    : Color.White;

            g.Clear(background);

            Rectangle rect = new Rectangle(
                1,
                1,
                Width - 3,
                Height - 3
            );

            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            Color fill;
            Color border;
            Color textColor;

            if (IsPrimary)
            {
                fill = isPressed
                    ? Color.FromArgb(5, 60, 160)
                    : isHover
                        ? Color.FromArgb(8, 75, 190)
                        : PrimaryColor;

                border = fill;
                textColor = Color.White;
            }
            else
            {
                fill = isPressed
                    ? Color.FromArgb(225, 235, 250)
                    : isHover
                        ? Color.FromArgb(240, 246, 255)
                        : Color.White;

                border = BorderColor;
                textColor = PrimaryColor;
            }

            using (GraphicsPath path =
                CreateRoundedPath(rect, CornerRadius))
            using (SolidBrush brush =
                new SolidBrush(fill))
            using (Pen pen =
                new Pen(border, 1))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                rect,
                textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis
            );

            if (Focused && ShowFocusCues)
            {
                Rectangle focusRect = rect;
                focusRect.Inflate(-5, -5);

                ControlPaint.DrawFocusRectangle(
                    g,
                    focusRect
                );
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            isHover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            isHover = false;
            isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(
            MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                Focus();
                Invalidate();
            }
        }

        protected override void OnMouseUp(
            MouseEventArgs e)
        {
            base.OnMouseUp(e);

            isPressed = false;
            Invalidate();
        }

        protected override void OnKeyDown(
            KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Space ||
                e.KeyCode == Keys.Enter)
            {
                isPressed = true;
                Invalidate();
                e.Handled = true;
            }
        }

        protected override void OnKeyUp(
            KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode == Keys.Space ||
                e.KeyCode == Keys.Enter)
            {
                isPressed = false;
                Invalidate();

                OnClick(EventArgs.Empty);

                e.Handled = true;
            }
        }
    }
}