using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    internal sealed class Sketch20Mode : RadioButton
    {
        private readonly string description, icon;
        internal Sketch20Mode(string title, string note, string symbol)
        {
            Text = title; description = note; icon = symbol; AutoCheck = false; AutoSize = false;
            Cursor = Cursors.Hand; BackColor = Sketch20Style.Background; AccessibleRole = AccessibleRole.RadioButton;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        { Sketch20Style.ControlBackground(e.Graphics, this); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Sketch20Style.ControlBackground(e.Graphics, this);
            var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Sketch20Style.Round(g, ClientRectangle, 11, Checked ? Sketch20Style.BlueSoft : Color.White, Checked ? Sketch20Style.Blue : Sketch20Style.Border);
            Sketch20Style.Round(g, new Rectangle(21, 21, 38, 38), 9, Sketch20Style.Background, Color.Transparent);
            Sketch20Style.Icon(g, icon, new Rectangle(29, 29, 22, 22), Sketch20Style.Blue);
            Sketch20Style.Text(g, Text, new Rectangle(72, 16, Width - 131, 28), 14, Sketch20Style.Ink, true);
            Sketch20Style.Text(g, description, new Rectangle(72, 44, Width - 106, 27), 12, Sketch20Style.Muted);
            var circle = new Rectangle(Width - 38, 24, 17, 17);
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
            choices = entries; selected = Math.Max(0, Math.Min(index, entries.Length - 1)); AccessibleRole = AccessibleRole.ComboBox;
            Cursor = Cursors.Hand; BackColor = Sketch20Style.Background;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Click += delegate { OpenChoices(); };
        }
        private void OpenChoices()
        {
            var menu = new ContextMenuStrip { Font = Sketch20Style.Font(13) };
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                var item = new ToolStripMenuItem(choices[i]) { Checked = i == selected };
                item.Click += delegate { selected = index; Invalidate(); if (Chosen != null) Chosen(index); };
                menu.Items.Add(item);
            }
            menu.Closed += delegate { menu.Dispose(); };
            menu.Show(this, new Point(0, Height));
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        { Sketch20Style.ControlBackground(e.Graphics, this); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Sketch20Style.ControlBackground(e.Graphics, this);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Sketch20Style.Round(e.Graphics, ClientRectangle, 7, Color.FromArgb(247, 248, 250), Sketch20Style.Border);
            Sketch20Style.Text(e.Graphics, choices[selected], new Rectangle(12, 0, Width - 40, Height));
            Sketch20Style.Icon(e.Graphics, "chevron-down", new Rectangle(Width - 25, (Height - 13) / 2, 13, 13), Sketch20Style.Ink);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(4, 4, Width - 8, Height - 8));
        }
    }
    internal sealed class Sketch20Date : Button
    {
        private DateTime value;
        private readonly bool sample;
        internal event Action<DateTime> Chosen;
        internal Sketch20Date(DateTime date, bool isSample)
        {
            value = date; sample = isSample; AccessibleRole = AccessibleRole.ComboBox; Cursor = Cursors.Hand; BackColor = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Click += delegate
            {
                var menu = new ContextMenuStrip();
                var calendar = new MonthCalendar { MaxSelectionCount = 1 }; calendar.SetDate(value);
                menu.Items.Add(new ToolStripControlHost(calendar));
                calendar.DateSelected += delegate { value = calendar.SelectionStart; if (Chosen != null) Chosen(value); menu.Close(); Invalidate(); };
                menu.Closed += delegate { menu.Dispose(); };
                menu.Show(this, new Point(0, Height));
            };
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        { Sketch20Style.ControlBackground(e.Graphics, this); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Sketch20Style.ControlBackground(e.Graphics, this);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Sketch20Style.Round(e.Graphics, ClientRectangle, 7, Color.White, Sketch20Style.Border);
            Sketch20Style.Text(e.Graphics, value.ToString(sample ? "MM/dd/yyyy" : "dd/MM/yyyy"), new Rectangle(12, 0, Width - 24, Height));
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(4, 4, Width - 8, Height - 8));
        }
    }
    internal sealed class Sketch20Password : Panel
    {
        internal readonly TextBox Entry;
        private readonly Label hint;
        internal Sketch20Password()
        {
            DoubleBuffered = true; BackColor = Color.White;
            Entry = new TextBox { BorderStyle = BorderStyle.None, UseSystemPasswordChar = true, Font = Sketch20Style.Font(13), BackColor = Color.White,
                ForeColor = Sketch20Style.Ink, Location = new Point(12, 12), AccessibleName = "API key VirusTotal" };
            hint = new Label { Text = "Nhập API key VirusTotal", Font = Sketch20Style.Font(13), ForeColor = Color.FromArgb(155, 155, 155), BackColor = Color.White,
                Location = new Point(11, 9), Height = 24, TextAlign = ContentAlignment.MiddleLeft };
            hint.Click += delegate { Entry.Focus(); };
            Entry.TextChanged += delegate { UpdateHint(); };
            Entry.GotFocus += delegate { UpdateHint(); }; Entry.LostFocus += delegate { UpdateHint(); };
            Controls.Add(Entry); Controls.Add(hint);
            hint.BringToFront();
            Resize += delegate { Entry.Width = Width - 24; hint.Width = Width - 24; };
        }
        private void UpdateHint()
        { hint.Visible = Entry.Text.Length == 0 && !Entry.Focused; if (hint.Visible) hint.BringToFront(); }
        protected override void OnPaint(PaintEventArgs e)
        { base.OnPaint(e); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; Sketch20Style.Round(e.Graphics, ClientRectangle, 7, Color.Transparent, Sketch20Style.Border); }
    }
}
