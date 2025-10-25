using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    internal sealed class UiSelectItemCollection
    {
        private readonly UiSelect owner;
        private readonly List<object> items = new List<object>();
        internal UiSelectItemCollection(UiSelect owner) { this.owner = owner; }
        public int Count { get { return items.Count; } }
        public object this[int index]
        {
            get { return items[index]; }
            set { items[index] = value; owner.OnItemsChanged(); }
        }
        public int Add(object value) { items.Add(value); owner.OnItemsChanged(); return items.Count - 1; }
        public void AddRange(object[] values) { if (values != null) items.AddRange(values); owner.OnItemsChanged(); }
        public void Clear() { items.Clear(); owner.OnItemsChanged(); }
        public bool Contains(object value) { return items.Contains(value); }
        public int IndexOf(object value) { return items.IndexOf(value); }
        internal string[] Texts() { return items.Select(v => Convert.ToString(v) ?? string.Empty).ToArray(); }
    }

    internal class UiSelect : System.Windows.Forms.Control
    {
        private int selectedIndex = -1;
        private bool hover;
        internal readonly UiSelectItemCollection Items;
        internal int DropDownWidth { get; set; }
        internal bool ShowCalendarGlyph { get; set; }
        internal bool SquareCorners { get; set; }
        internal Color AccentBackColor { get; set; } = Color.White;
        internal Color AccentHoverColor { get; set; } = Color.FromArgb(248, 251, 255);
        internal Color AccentBorderColor { get; set; } = Color.FromArgb(217, 231, 247);
        internal Color AccentFocusedBorderColor { get; set; } = Color.FromArgb(147, 190, 244);
        internal event EventHandler SelectedIndexChanged;

        internal UiSelect()
        {
            Items = new UiSelectItemCollection(this);
            BackColor = Color.White;
            ForeColor = Theme.TextDark;
            Font = Theme.BodyFont;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.ComboBox;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        }

        internal int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                int next = Items.Count == 0 ? -1 : Math.Max(-1, Math.Min(value, Items.Count - 1));
                if (selectedIndex == next) return;
                selectedIndex = next;
                Text = SelectedItem == null ? string.Empty : Convert.ToString(SelectedItem);
                Invalidate();
                var handler = SelectedIndexChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        internal object SelectedItem
        {
            get { return selectedIndex >= 0 && selectedIndex < Items.Count ? Items[selectedIndex] : null; }
            set { SelectedIndex = value == null ? -1 : Items.IndexOf(value); }
        }

        internal int FindString(string text)
        {
            if (string.IsNullOrEmpty(text)) return -1;
            for (int i = 0; i < Items.Count; i++)
                if ((Convert.ToString(Items[i]) ?? string.Empty).StartsWith(text, StringComparison.CurrentCultureIgnoreCase)) return i;
            return -1;
        }

        internal void OnItemsChanged()
        {
            if (selectedIndex >= Items.Count) selectedIndex = Items.Count - 1;
            Text = SelectedItem == null ? string.Empty : Convert.ToString(SelectedItem);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { base.OnClick(e); OpenPopup(); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.Down && e.Alt)
            { OpenPopup(); e.Handled = true; return; }
            if (e.KeyCode == Keys.Down && Items.Count > 0) { SelectedIndex = Math.Min(Items.Count - 1, Math.Max(0, selectedIndex + 1)); e.Handled = true; }
            else if (e.KeyCode == Keys.Up && Items.Count > 0) { SelectedIndex = Math.Max(0, selectedIndex - 1); e.Handled = true; }
            base.OnKeyDown(e);
        }

        private void OpenPopup()
        {
            if (!Enabled || Items.Count == 0) return;
            UiSelectPopup.ShowFor(this, Items.Texts(), selectedIndex, delegate(int index) { SelectedIndex = index; }, Theme.Blue, Theme.BlueTint, DropDownWidth, SquareCorners);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color edge = hover || Focused ? AccentFocusedBorderColor : AccentBorderColor;
            Color fill = hover ? AccentHoverColor : AccentBackColor;
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            if (SquareCorners)
            {
                using (var brush = new SolidBrush(fill)) e.Graphics.FillRectangle(brush, bounds);
                using (var pen = new Pen(edge)) e.Graphics.DrawRectangle(pen, bounds);
            }
            else
            {
                using (var path = Rounded(bounds, Math.Min(10, Math.Max(5, Height / 3))))
                using (var brush = new SolidBrush(fill))
                using (var pen = new Pen(edge))
                { e.Graphics.FillPath(brush, path); e.Graphics.DrawPath(pen, path); }
            }

            int arrowBox = Math.Min(28, Math.Max(21, Height - 8));
            int arrowX = Width - arrowBox - 5;
            Rectangle arrowBounds = new Rectangle(arrowX, (Height - arrowBox) / 2, arrowBox, arrowBox);
            Color arrowFill = hover || Focused ? Color.FromArgb(227, 230, 235) : Color.FromArgb(236, 239, 243);
            if (SquareCorners)
            {
                using (var brush = new SolidBrush(arrowFill)) e.Graphics.FillRectangle(brush, arrowBounds);
            }
            else
            {
                using (var path = Rounded(arrowBounds, 7))
                using (var brush = new SolidBrush(arrowFill)) e.Graphics.FillPath(brush, path);
            }

            int textLeft = 12;
            if (ShowCalendarGlyph)
            {
                DrawCalendarGlyph(e.Graphics, new Rectangle(13, Math.Max(0, (Height - 20) / 2), 18, 18), ForeColor);
                textLeft = 38;
            }
            TextRenderer.DrawText(e.Graphics, SelectedItem == null ? string.Empty : Convert.ToString(SelectedItem), Font,
                new Rectangle(textLeft, 0, Math.Max(1, arrowX - textLeft - 6), Height), ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            using (var pen = new Pen(hover || Focused ? ForeColor : Theme.TextGray, 1.5f))
            {
                int cx = arrowX + arrowBox / 2, cy = Height / 2;
                e.Graphics.DrawLines(pen, new[] { new Point(cx - 4, cy - 2), new Point(cx, cy + 2), new Point(cx + 4, cy - 2) });
            }
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(3, 3, Math.Max(1, Width - 6), Math.Max(1, Height - 6)));
        }

        private static void DrawCalendarGlyph(Graphics g, Rectangle rect, Color color)
        {
            using (var pen = new Pen(color, 1.6f))
            using (var brush = new SolidBrush(color))
            {
                g.DrawRectangle(pen, rect.Left + 1, rect.Top + 4, rect.Width - 3, rect.Height - 6);
                g.DrawLine(pen, rect.Left + 1, rect.Top + 8, rect.Right - 2, rect.Top + 8);
                g.DrawLine(pen, rect.Left + 5, rect.Top + 1, rect.Left + 5, rect.Top + 7);
                g.DrawLine(pen, rect.Right - 6, rect.Top + 1, rect.Right - 6, rect.Top + 7);
                g.FillRectangle(brush, rect.Left + 4, rect.Top + 11, 3, 3);
                g.FillRectangle(brush, rect.Left + 9, rect.Top + 11, 3, 3);
                g.FillRectangle(brush, rect.Left + 4, rect.Top + 15, 3, 3);
            }
        }

        internal static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure();
            return p;
        }
    }

    internal sealed class UiSelectPopup : Form
    {
        private static UiSelectPopup current;
        private readonly Color accent, soft;
        private readonly bool squareCorners;

        private UiSelectPopup(Color accentColor, Color softColor, bool square)
        {
            accent = accentColor; soft = softColor; squareCorners = square;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            KeyPreview = true;
            Padding = new Padding(5);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.DefaultUiFont;
            Deactivate += delegate { Close(); };
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
            Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (squareCorners)
                {
                    using (var pen = new Pen(Color.FromArgb(218, 229, 243)))
                        e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
                }
                else
                {
                    using (var p = UiSelect.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 11))
                    using (var pen = new Pen(Color.FromArgb(218, 229, 243))) e.Graphics.DrawPath(pen, p);
                }
            };
            FormClosed += delegate { if (ReferenceEquals(current, this)) current = null; };
        }

        internal static void ShowFor(System.Windows.Forms.Control owner, string[] choices, int selected, Action<int> chosen, Color accent, Color soft, int requestedWidth = 0, bool squareCorners = false)
        {
            if (owner == null || owner.IsDisposed || choices == null || choices.Length == 0) return;
            if (current != null && !current.IsDisposed) current.Close();

            Rectangle screenWork = Screen.FromControl(owner).WorkingArea;
            Form parent = owner.FindForm();
            Rectangle appWork = parent != null ? parent.RectangleToScreen(parent.ClientRectangle) : screenWork;
            Rectangle work = Rectangle.Intersect(screenWork, appWork);
            if (work.Width < 80 || work.Height < 80) work = screenWork;

            int natural = Math.Max(owner.Width, requestedWidth);
            using (Graphics g = owner.CreateGraphics())
                for (int i = 0; i < choices.Length; i++) natural = Math.Max(natural, (int)Math.Ceiling(g.MeasureString(choices[i], owner.Font).Width) + 56);
            int popupW = Math.Min(Math.Max(120, natural), Math.Min(360, Math.Max(120, work.Width - 16)));
            int rowH = 34, wantedH = choices.Length * rowH + 10;
            int popupH = Math.Min(wantedH, Math.Max(44, work.Height - 16));

            var popup = new UiSelectPopup(accent, soft, squareCorners) { ClientSize = new Size(popupW, popupH) };
            current = popup;
            var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = wantedH > popupH, Padding = new Padding(0) };
            popup.Controls.Add(host);
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                var item = new UiSelectPopupItem(choices[i], i == selected, accent, soft, squareCorners)
                { Left = 0, Top = i * rowH, Width = popupW - (wantedH > popupH ? SystemInformation.VerticalScrollBarWidth : 0) - 10, Height = rowH };
                item.Click += delegate { if (chosen != null) chosen(index); popup.Close(); };
                host.Controls.Add(item);
            }

            Point below = owner.PointToScreen(new Point(0, owner.Height + 4));
            int px = Math.Max(work.Left + 4, Math.Min(below.X, work.Right - popupW - 4));
            int py = below.Y;
            if (py + popupH > work.Bottom - 4) py = Math.Max(work.Top + 4, owner.PointToScreen(new Point(0, -popupH - 4)).Y);
            popup.Location = new Point(px, py);
            if (parent != null) popup.Show(parent); else popup.Show();
            popup.Activate();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!squareCorners)
                using (var p = UiSelect.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 11)) Region = new Region(p);
        }
    }

    internal sealed class UiSelectPopupItem : System.Windows.Forms.Control
    {
        private readonly bool selected;
        private readonly Color accent, soft;
        private readonly bool squareCorners;
        private bool hover;
        internal UiSelectPopupItem(string text, bool active, Color accentColor, Color softColor, bool square)
        {
            Text = text; selected = active; accent = accentColor; soft = softColor;
            squareCorners = square;
            Cursor = Cursors.Hand; BackColor = Color.White; Font = Theme.BodyFont;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (hover || selected)
            {
                using (var b = new SolidBrush(selected ? soft : Color.FromArgb(246, 250, 255)))
                {
                    if (squareCorners) e.Graphics.FillRectangle(b, 2, 2, Math.Max(1, Width - 4), Math.Max(1, Height - 4));
                    else using (var p = UiSelect.Rounded(new Rectangle(2, 2, Width - 4, Height - 4), 8))
                        e.Graphics.FillPath(b, p);
                }
            }
            TextRenderer.DrawText(e.Graphics, Text, selected ? Theme.BoldFont : Theme.BodyFont,
                new Rectangle(11, 0, Math.Max(1, Width - 42), Height), selected ? accent : Theme.TextDark,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (selected)
            {
                using (var pen = new Pen(accent, 1.7f))
                {
                    int x = Width - 24, y = Height / 2;
                    e.Graphics.DrawLines(pen, new[] { new Point(x - 4, y), new Point(x - 1, y + 3), new Point(x + 5, y - 4) });
                }
            }
        }
    }
}

namespace ScanAndRemoveVirus.Control
{
    internal sealed class UiActionEntry
    {
        internal string Title, Subtitle;
        internal Image Icon;
        internal bool Checked, Danger, Enabled = true;
        internal Action Click;
        internal UiActionEntry(string title, Action click, string subtitle = null, Image icon = null)
        { Title = title ?? string.Empty; Subtitle = subtitle ?? string.Empty; Click = click; Icon = icon; }
    }

    internal sealed class UiActionPopup : Form
    {
        private static UiActionPopup current;
        private UiActionPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            Padding = new Padding(5);
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.DefaultUiFont;
            Deactivate += delegate { Close(); };
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
            Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = UiSelect.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 11))
                using (var pen = new Pen(Color.FromArgb(218, 229, 243))) e.Graphics.DrawPath(pen, p);
            };
            FormClosed += delegate { if (ReferenceEquals(current, this)) current = null; };
        }

        internal static void ShowFor(System.Windows.Forms.Control owner, UiActionEntry[] entries, int requestedWidth = 0)
        {
            if (owner == null || owner.IsDisposed || entries == null || entries.Length == 0) return;
            if (current != null && !current.IsDisposed) current.Close();
            Rectangle screenWork = Screen.FromControl(owner).WorkingArea;
            Form parent = owner.FindForm();
            Rectangle appWork = parent != null ? parent.RectangleToScreen(parent.ClientRectangle) : screenWork;
            Rectangle work = Rectangle.Intersect(screenWork, appWork);
            if (work.Width < 80 || work.Height < 80) work = screenWork;

            int natural = Math.Max(owner.Width, requestedWidth);
            using (Graphics g = owner.CreateGraphics())
                foreach (UiActionEntry e in entries)
                    natural = Math.Max(natural, (int)Math.Ceiling(g.MeasureString(e.Title, Theme.BodyFont).Width) + 76);
            int popupW = Math.Min(Math.Max(170, natural), Math.Min(380, Math.Max(170, work.Width - 16)));
            int wantedH = 10;
            for (int i = 0; i < entries.Length; i++) wantedH += string.IsNullOrWhiteSpace(entries[i].Subtitle) ? 38 : 54;
            int popupH = Math.Min(wantedH, Math.Max(48, work.Height - 16));

            var popup = new UiActionPopup { ClientSize = new Size(popupW, popupH) };
            current = popup;
            var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = wantedH > popupH };
            popup.Controls.Add(host);
            int top = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                UiActionEntry entry = entries[i];
                int rowH = string.IsNullOrWhiteSpace(entry.Subtitle) ? 38 : 54;
                var item = new UiActionPopupItem(entry)
                {
                    Left = 0, Top = top, Width = popupW - (wantedH > popupH ? SystemInformation.VerticalScrollBarWidth : 0) - 10, Height = rowH
                };
                item.Click += delegate
                {
                    if (!entry.Enabled) return;
                    popup.Close();
                    if (entry.Click != null) entry.Click();
                };
                host.Controls.Add(item);
                top += rowH;
            }

            Point below = owner.PointToScreen(new Point(0, owner.Height + 4));
            int px = Math.Max(work.Left + 4, Math.Min(below.X, work.Right - popupW - 4));
            int py = below.Y;
            if (py + popupH > work.Bottom - 4) py = Math.Max(work.Top + 4, owner.PointToScreen(new Point(0, -popupH - 4)).Y);
            popup.Location = new Point(px, py);
            if (parent != null) popup.Show(parent); else popup.Show();
            popup.Activate();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            using (var p = UiSelect.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 11)) Region = new Region(p);
        }
    }

    internal sealed class UiActionPopupItem : System.Windows.Forms.Control
    {
        private readonly UiActionEntry entry;
        private bool hover;
        internal UiActionPopupItem(UiActionEntry value)
        {
            entry = value;
            Cursor = value.Enabled ? Cursors.Hand : Cursors.Default;
            BackColor = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnMouseEnter(EventArgs e) { if (entry.Enabled) hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (hover || entry.Checked)
                using (var p = UiSelect.Rounded(new Rectangle(2, 2, Width - 4, Height - 4), 8))
                using (var b = new SolidBrush(entry.Checked ? Theme.BlueTint : Color.FromArgb(246, 250, 255))) e.Graphics.FillPath(b, p);
            Color titleColor = !entry.Enabled ? Theme.TextGray : entry.Danger ? Theme.Red : entry.Checked ? Theme.Blue : Theme.TextDark;
            int textX = 12;
            if (entry.Icon != null)
            {
                int s = 18, y = (Height - s) / 2;
                e.Graphics.DrawImage(entry.Icon, new Rectangle(12, y, s, s));
                textX = 42;
            }
            int rightPad = entry.Checked ? 34 : 12;
            if (string.IsNullOrWhiteSpace(entry.Subtitle))
            {
                TextRenderer.DrawText(e.Graphics, entry.Title, entry.Checked ? Theme.BoldFont : Theme.BodyFont,
                    new Rectangle(textX, 0, Math.Max(1, Width - textX - rightPad), Height), titleColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, entry.Title, Theme.BoldFont,
                    new Rectangle(textX, 7, Math.Max(1, Width - textX - rightPad), 21), titleColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(e.Graphics, entry.Subtitle, Theme.SmallFont,
                    new Rectangle(textX, 28, Math.Max(1, Width - textX - rightPad), 18), entry.Enabled ? Theme.TextGray : Color.FromArgb(170, 175, 185),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            if (entry.Checked)
            {
                using (var pen = new Pen(Theme.Blue, 1.7f))
                {
                    int x = Width - 22, y = Height / 2;
                    e.Graphics.DrawLines(pen, new[] { new Point(x - 4, y), new Point(x - 1, y + 3), new Point(x + 5, y - 4) });
                }
            }
        }
    }
}
