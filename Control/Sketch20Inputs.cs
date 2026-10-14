using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    internal sealed class Sketch20Mode : RadioButton
    {
        private readonly string description, icon;
        private bool hover;
        internal bool Compact, Centered;
        internal float TextScale = 1;
        internal Sketch20Mode(string title, string note, string symbol)
        {
            Text = title; description = note; icon = symbol; AutoCheck = false; AutoSize = false;
            Cursor = Cursors.Hand; BackColor = Sketch20Style.Background; AccessibleRole = AccessibleRole.RadioButton;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            MouseEnter += delegate { hover = true; Invalidate(); };
            MouseLeave += delegate { hover = false; Invalidate(); };
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        { Sketch20Style.ControlBackground(e.Graphics, this); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Sketch20Style.ControlBackground(e.Graphics, this);
            var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Color cardFill = Checked ? ColorTranslator.FromHtml("#f3f8ff") : hover ? ColorTranslator.FromHtml("#f9fbff") : Color.White;
            Color cardEdge = Checked ? ColorTranslator.FromHtml("#9fc7fb") : hover ? ColorTranslator.FromHtml("#c7ddf7") : ColorTranslator.FromHtml("#dce7f5");
            if (Compact)
            {
                Sketch20Style.Round(g, new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), 14, cardFill, cardEdge);
                Sketch20ReferenceAssets.FilledIcon(g, icon, new Rectangle(18, 17, 27, 27), Sketch20Style.Blue);
                Sketch20Style.Text(g, Text, new Rectangle(59, 9, Width - 74, 27), 11.5f * TextScale, Checked ? Sketch20Style.Blue : Sketch20Style.Ink, true);
                Sketch20Style.Text(g, description, new Rectangle(59, 40, Width - 74, 47), 10.3f * TextScale, Sketch20Style.Muted, false, true);
                var radio = new Rectangle(18, Height - 29, 16, 16);
                using (var p = new Pen(Checked ? Sketch20Style.Blue : Sketch20Style.Muted, 1.4f)) g.DrawEllipse(p, radio);
                if (Checked) using (var b = new SolidBrush(Sketch20Style.Blue)) g.FillEllipse(b, radio.X + 4, radio.Y + 4, 8, 8);
                return;
            }
            if (Centered)
            {
                Sketch20Style.Round(g, new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), 14, cardFill, cardEdge);
                Sketch20ReferenceAssets.FilledIcon(g, icon, new Rectangle((Width - 36) / 2, 23, 36, 36), Checked ? Sketch20Style.Blue : Sketch20Style.Muted);
                Sketch20Style.Text(g, Text, new Rectangle(17, 77, Width - 34, 30), 13 * TextScale, Checked ? Sketch20Style.Blue : Sketch20Style.Ink, true, center: true);
                Sketch20Style.Text(g, description, new Rectangle(17, 111, Width - 34, Height - 117), 11 * TextScale, Checked ? Sketch20Style.Blue : Sketch20Style.Muted, false, true, center: true);
                if (Checked) { using (var b = new SolidBrush(Sketch20Style.Blue)) g.FillEllipse(b, Width - 28, 10, 17, 17); Sketch20Style.Icon(g, "check", new Rectangle(Width - 25, 13, 11, 11), Color.White); }
                return;
            }
            Sketch20Style.Round(g, new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), 14, cardFill, cardEdge);
            // Mode card dùng bố cục dọc để tiêu đề không bị ellipsis ở cửa sổ nhỏ.
            int iconBox = Height >= 140 ? 49 : 36, iconX = 18;
            Sketch20Style.Round(g, new Rectangle(iconX, 14, iconBox, iconBox), 11, Sketch20Style.BlueSoft, Color.Transparent);
            Sketch20ReferenceAssets.FilledIcon(g, icon, new Rectangle(iconX + 10, 24, iconBox - 20, iconBox - 20), Sketch20Style.Blue);
            int titleY = Height >= 140 ? 72 : 50;
            Sketch20Style.Text(g, Text, new Rectangle(18, titleY, Width - 36, 44), 12.5f * TextScale, Sketch20Style.Ink, true, true);
            int descriptionY = titleY + 47;
            Sketch20Style.Text(g, description, new Rectangle(18, descriptionY, Width - 36, Height - descriptionY - 8), 10.5f * TextScale, Sketch20Style.Muted, false, true);
            var circle = new Rectangle(Width - 29, 12, 16, 16);
            if (Checked)
            {
                using (var brush = new SolidBrush(Sketch20Style.Blue)) g.FillEllipse(brush, circle);
                Sketch20Style.Icon(g, "check", new Rectangle(circle.X + 3, circle.Y + 3, 11, 11), Color.White);
            }
            else using (var pen = new Pen(Sketch20Style.Border)) g.DrawEllipse(pen, circle);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(5, 5, Width - 10, Height - 10));
        }
        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || base.IsInputKey(keyData);
        }
    }
    internal sealed class Sketch20Choice : Button
    {
        private readonly string[] choices;
        private int selected;
        internal event Action<int> Chosen;
        internal Sketch20Choice(string[] entries, int index)
        {
            choices = entries ?? new string[0];
            selected = choices.Length == 0 ? -1 : Math.Max(0, Math.Min(index, choices.Length - 1));
            AccessibleRole = AccessibleRole.ComboBox;
            Cursor = Cursors.Hand; BackColor = Sketch20Style.Background;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Click += delegate { OpenChoices(); };
        }
        private void OpenChoices()
        {
            if (choices.Length == 0) return;
            UiSelectPopup.ShowFor(this, choices, selected, delegate(int index)
            {
                if (IsDisposed) return;
                selected = index; Invalidate();
                if (Chosen != null) Chosen(index);
            }, Sketch20Style.Blue, Sketch20Style.BlueSoft);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        { Sketch20Style.ControlBackground(e.Graphics, this); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Sketch20Style.ControlBackground(e.Graphics, this);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Sketch20Style.Round(e.Graphics, new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), 10, Color.White, ColorTranslator.FromHtml("#d9e7f7"));
            int tileY = (Height - 28) / 2;
            Sketch20Style.Round(e.Graphics, new Rectangle(8, tileY, 28, 28), 8, Sketch20Style.BlueSoft, Color.Transparent);
            Sketch20Style.Icon(e.Graphics, "filter", new Rectangle(15, tileY + 7, 14, 14), Sketch20Style.Blue);
            string value = selected >= 0 && selected < choices.Length ? choices[selected] : string.Empty;
            Sketch20Style.Text(e.Graphics, value, new Rectangle(47, 0, Math.Max(1, Width - 90), Height), 11.5f, Sketch20Style.Ink, true);
            int cx = Width - 37;
            Sketch20Style.Round(e.Graphics, new Rectangle(cx, tileY, 28, 28), 8, ColorTranslator.FromHtml("#f1f6fd"), Color.Transparent);
            Sketch20Style.Icon(e.Graphics, "chevron-down", new Rectangle(cx + 8, tileY + 8, 12, 12), Sketch20Style.Muted);
        }
    }
    internal sealed class Sketch20Date : Button{
        private DateTime value;
        private readonly bool sample;
        private ContextMenuStrip activeMenu;
        internal event Action<DateTime> Chosen;
        internal Sketch20Date(DateTime date, bool isSample)
        {
            value = date; sample = isSample; AccessibleRole = AccessibleRole.ComboBox; Cursor = Cursors.Hand; BackColor = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Click += delegate { OpenCalendar(); };
        }
        private void OpenCalendar()
        {
            CloseActiveMenu();
            var menu = new ContextMenuStrip(); activeMenu = menu;
            var calendar = new MonthCalendar { MaxSelectionCount = 1 }; calendar.SetDate(value);
            menu.Items.Add(new ToolStripControlHost(calendar));
            calendar.DateSelected += delegate
            {
                if (IsDisposed) return;
                value = calendar.SelectionStart;
                if (Chosen != null) Chosen(value);
                if (activeMenu != null && !activeMenu.IsDisposed) activeMenu.Close();
                Invalidate();
            };
            menu.Closed += PopupClosed;
            if (!IsDisposed && IsHandleCreated)
            {
                Rectangle screenWork = Screen.FromControl(this).WorkingArea;
                Form form = FindForm();
                Rectangle appWork = form != null ? form.RectangleToScreen(form.ClientRectangle) : screenWork;
                Rectangle work = Rectangle.Intersect(screenWork, appWork);
                if (work.Width < 80 || work.Height < 80) work = screenWork;
                Size popup = menu.GetPreferredSize(Size.Empty);
                Point below = PointToScreen(new Point(0, Height + 4));
                int px = Math.Max(work.Left + 4, Math.Min(below.X, work.Right - popup.Width - 4));
                int py = below.Y;
                if (py + popup.Height > work.Bottom - 4)
                    py = Math.Max(work.Top + 4, PointToScreen(new Point(0, -popup.Height - 4)).Y);
                menu.Show(new Point(px, py));
            }
        }
        private void PopupClosed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            var menu = sender as ContextMenuStrip;
            if (ReferenceEquals(activeMenu, menu)) activeMenu = null;
            if (menu != null) { menu.Closed -= PopupClosed; if (!menu.IsDisposed) menu.Dispose(); }
        }
        private void CloseActiveMenu()
        {
            var menu = activeMenu; activeMenu = null;
            if (menu == null) return;
            menu.Closed -= PopupClosed;
            if (!menu.IsDisposed) { menu.Close(); menu.Dispose(); }
        }
        protected override void Dispose(bool disposing)
        { if (disposing) CloseActiveMenu(); base.Dispose(disposing); }
        protected override void OnPaintBackground(PaintEventArgs e)
        { Sketch20Style.ControlBackground(e.Graphics, this); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Sketch20Style.ControlBackground(e.Graphics, this);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Sketch20Style.Round(e.Graphics, ClientRectangle, 7, Color.White, Sketch20Style.Border);
            Sketch20Style.Icon(e.Graphics, "calendar", new Rectangle(12, (Height - 16) / 2, 16, 16), Sketch20Style.Muted);
            Sketch20Style.Text(e.Graphics, value.ToString("dd/MM/yyyy"), new Rectangle(39, 0, Width - 48, Height), 12);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(4, 4, Width - 8, Height - 8));
        }
    }
    internal sealed class Sketch20Password : Panel
    {
        internal readonly TextBox Entry;
        internal readonly Button Reveal;
        private readonly Label hint;
        private readonly ToolTip tip = new ToolTip();
        internal string Placeholder { set { hint.Text = value; } }
        internal Sketch20Password()
        {
            DoubleBuffered = true; BackColor = Color.White;
            Entry = new TextBox { BorderStyle = BorderStyle.None, UseSystemPasswordChar = true, Font = Sketch20Style.Font(13), BackColor = Color.White,
                ForeColor = Sketch20Style.Ink, Location = new Point(12, 12), AccessibleName = "API key VirusTotal" };
            hint = new Label { Text = "Nhập API key VirusTotal", Font = Sketch20Style.Font(13), ForeColor = Sketch20Style.Muted, BackColor = Color.White,
                Location = new Point(11, 9), Height = 24, TextAlign = ContentAlignment.MiddleLeft };
            Reveal = new Button { Name = "reveal-api-key", Text = "", Width = 36, Height = 32, Top = 4,
                FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand, AccessibleName = "Hiện API key" };
            Reveal.FlatAppearance.BorderSize = 0;
            Reveal.Paint += delegate(object sender, PaintEventArgs e)
            {
                var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new Pen(Sketch20Style.Muted, 1.5f))
                {
                    g.DrawBezier(pen, 8, 16, 13, 7, 23, 7, 28, 16);
                    g.DrawBezier(pen, 8, 16, 13, 25, 23, 25, 28, 16);
                    g.DrawEllipse(pen, 15, 13, 6, 6);
                    if (!Entry.UseSystemPasswordChar) g.DrawLine(pen, 8, 6, 28, 26);
                }
            };
            tip.SetToolTip(Reveal, Reveal.AccessibleName);
            Reveal.Click += delegate
            {
                int start = Entry.SelectionStart, length = Entry.SelectionLength;
                Entry.UseSystemPasswordChar = !Entry.UseSystemPasswordChar;
                Entry.Select(start, length);
                Reveal.AccessibleName = Entry.UseSystemPasswordChar ? "Hiện API key" : "Ẩn API key";
                tip.SetToolTip(Reveal, Reveal.AccessibleName); Reveal.Invalidate();
            };
            hint.Click += delegate { Entry.Focus(); };
            Entry.TextChanged += delegate { UpdateHint(); };
            Entry.GotFocus += delegate { UpdateHint(); }; Entry.LostFocus += delegate { UpdateHint(); };
            Controls.Add(Entry); Controls.Add(hint); Controls.Add(Reveal);
            Reveal.BringToFront(); hint.BringToFront();
            Resize += delegate { Entry.Width = Math.Max(1, Width - 64); hint.Width = Entry.Width; Reveal.Left = Width - 44; };
        }
        private void UpdateHint()
        { hint.Visible = Entry.Text.Length == 0; if (hint.Visible) hint.BringToFront(); }
        protected override void OnPaint(PaintEventArgs e)
        { base.OnPaint(e); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; Sketch20Style.Round(e.Graphics, ClientRectangle, 7, Color.Transparent, Sketch20Style.Border); }
        protected override void Dispose(bool disposing)
        { if (disposing) tip.Dispose(); base.Dispose(disposing); }
    }
}
