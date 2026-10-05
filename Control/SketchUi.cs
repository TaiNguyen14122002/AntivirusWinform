using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    // Các màn dùng chung bố cục của bộ phác thảo; dữ liệu vẫn do services cung cấp.
    public sealed class SketchPage : Panel
    {
        public readonly Panel Canvas;
        public readonly Label Title;
        public readonly Label Subtitle;
        public Action<int> Arrange;
        private int extent = 640;
        private bool arranging;

        public SketchPage(string title, string subtitle)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.DashboardBg;
            AutoScroll = true;
            Canvas = new Panel { Name = "sketchCanvas", BackColor = Theme.DashboardBg };
            Theme.BatDoubleBuffer(Canvas);
            Controls.Add(Canvas);
            Title = SketchUi.Label(title, Theme.PageTitleFont, Theme.TextDark);
            Subtitle = SketchUi.Label(subtitle, Theme.BodyFont, Theme.TextGray);
            Canvas.Controls.Add(Title);
            Canvas.Controls.Add(Subtitle);
            Resize += delegate { Reflow(); };
        }

        public void SetExtent(int value)
        {
            extent = value;
            AutoScrollMinSize = new Size(0, extent);
            Canvas.Height = Math.Max(extent, ClientSize.Height);
        }

        public void Reflow()
        {
            if (arranging || IsDisposed) return;
            arranging = true;
            try
            {
                int width = Math.Max(240, ClientSize.Width - (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
                Canvas.Size = new Size(width, Math.Max(extent, ClientSize.Height));
                Title.SetBounds(0, 2, width, 36);
                Subtitle.SetBounds(0, 43, width, 26);
                if (Arrange != null) Arrange(width);
                // SetExtent có thể vừa làm xuất hiện scrollbar; xếp lại ngay để mép phải không bị che.
                int settledWidth = Math.Max(240, ClientSize.Width - (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
                if (settledWidth != width)
                {
                    Canvas.Width = settledWidth;
                    Title.Width = Subtitle.Width = settledWidth;
                    if (Arrange != null) Arrange(settledWidth);
                }
            }
            finally { arranging = false; }
        }
    }

    public sealed class UiSwitch : CheckBox
    {
        public UiSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            AutoSize = false;
            Size = new Size(44, 28);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.CheckButton;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var track = new Rectangle(3, 4, 36, 20);
            UiKit.Fill(e.Graphics, track, 10, Checked ? Theme.Blue : Theme.Line, Color.Transparent, 0);
            using (var b = new SolidBrush(Enabled ? Color.White : Theme.ChipGray))
                e.Graphics.FillEllipse(b, Checked ? 22 : 6, 7, 14, 14);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }

    public sealed class SketchTabButton : Button
    {
        public bool Selected;
        public SketchTabButton(string text)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            Text = text;
            Font = Theme.BodyFont;
            Height = 42;
            Width = TextRenderer.MeasureText(text, Theme.BoldFont).Width + 28;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Selected)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                UiKit.Fill(e.Graphics, ClientRectangle, 6, Theme.BlueFaint, Color.Transparent, 0);
                using (var pen = new Pen(Theme.Blue, 2)) e.Graphics.DrawLine(pen, 0, Height - 2, Width, Height - 2);
            }
            TextRenderer.DrawText(e.Graphics, Text, Selected ? Theme.BoldFont : Theme.BodyFont,
                ClientRectangle, Selected ? Theme.Blue : Theme.TextGray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(2, 2, Width - 5, Height - 5));
        }
    }

    public sealed class SketchTabs : Panel
    {
        private readonly SketchTabButton[] buttons;
        public event EventHandler SelectedIndexChanged;
        private int selected;
        public int SelectedIndex
        {
            get { return selected; }
            set
            {
                if (value < 0 || value >= buttons.Length) return;
                bool changed = selected != value;
                selected = value;
                for (int i = 0; i < buttons.Length; i++) { buttons[i].Selected = i == value; buttons[i].Invalidate(); }
                if (changed && SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }
        public SketchTabs(params string[] titles)
        {
            Height = 44;
            BackColor = Theme.DashboardBg;
            buttons = new SketchTabButton[titles.Length];
            int x = 0;
            for (int i = 0; i < titles.Length; i++)
            {
                int index = i;
                buttons[i] = new SketchTabButton(titles[i]) { Location = new Point(x, 0), Name = "sketchTab" + i };
                buttons[i].Click += delegate { SelectedIndex = index; };
                Controls.Add(buttons[i]);
                x += buttons[i].Width + 4;
            }
            SelectedIndex = 0;
        }
    }

    internal static class SketchUi
    {
        public static SketchPage Page(UserControl host, System.Windows.Forms.Control legacy, string title, string subtitle)
        {
            host.SuspendLayout();
            host.AutoScroll = false;
            host.AutoScrollMinSize = Size.Empty;
            host.BackColor = Theme.DashboardBg;
            if (legacy != null) { legacy.Visible = false; legacy.Dock = DockStyle.None; }
            var page = new SketchPage(title, subtitle) { Name = "sketchPage" };
            host.Controls.Add(page);
            page.BringToFront();
            host.ResumeLayout(true);
            return page;
        }

        public static Label Label(string text, Font font, Color color)
        {
            return new Label { Text = text, Font = font, ForeColor = color, BackColor = Color.Transparent,
                AutoSize = false, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        }

        public static UiCard Card(string name)
        {
            var card = new UiCard { Name = name, Padding = Padding.Empty, Radius = 12,
                FillColor = Color.White, BorderColor = Theme.CardBorder, ShowBorder = true };
            card.SetHeader("", null);
            return card;
        }

        public static void Place(System.Windows.Forms.Control control, System.Windows.Forms.Control parent, int x, int y, int width, int height)
        {
            if (control.Parent != parent) parent.Controls.Add(control);
            control.Dock = DockStyle.None;
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            control.Margin = Padding.Empty;
            control.SetBounds(x, y, Math.Max(1, width), Math.Max(1, height));
            control.BringToFront();
        }

        public static UiButton Button(string text, Theme.BtnRole role, EventHandler click)
        {
            var button = new UiButton { Text = text, Height = 40, Width = 146, Radius = 8 };
            Theme.StyleButton(button, role);
            if (click != null) button.Click += click;
            return button;
        }

        public static Label Note(Panel parent, string text)
        {
            var label = Label(text, Theme.SmallFont, Theme.TextGray);
            parent.Controls.Add(label);
            return label;
        }

        public static UiSwitch BindSwitch(CheckBox backing, string accessibleName)
        {
            var toggle = new UiSwitch { Checked = backing.Checked, AccessibleName = accessibleName, Name = "switch_" + backing.Name };
            toggle.CheckedChanged += delegate { if (backing.Checked != toggle.Checked) backing.Checked = toggle.Checked; };
            backing.CheckedChanged += delegate { if (!toggle.IsDisposed && toggle.Checked != backing.Checked) toggle.Checked = backing.Checked; };
            return toggle;
        }

        public static Panel SettingRow(CheckBox backing, string title, string note)
        {
            var row = new Panel { BackColor = Color.Transparent, Height = 78 };
            var heading = Label(title, Theme.BoldFont, Theme.TextDark);
            var hint = Label(note, Theme.SmallFont, Theme.TextGray);
            var toggle = BindSwitch(backing, title);
            row.Controls.AddRange(new System.Windows.Forms.Control[] { heading, hint, toggle });
            row.Resize += delegate
            {
                heading.SetBounds(0, 8, Math.Max(80, row.Width - 58), 24);
                hint.SetBounds(0, 34, Math.Max(80, row.Width - 58), 30);
                toggle.Location = new Point(Math.Max(0, row.Width - 44), 17);
            };
            row.Paint += delegate(object s, PaintEventArgs e)
            { using (var p = new Pen(Theme.Line)) e.Graphics.DrawLine(p, 0, row.Height - 1, row.Width, row.Height - 1); };
            return row;
        }

        public static void Grid(DataGridView grid)
        {
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = Color.White;
            grid.GridColor = Theme.Line;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.SidebarBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextGray;
            grid.ColumnHeadersDefaultCellStyle.Font = Theme.SmallFont;
            grid.ColumnHeadersHeight = 40;
            grid.DefaultCellStyle.Font = Theme.BodyFont;
            grid.DefaultCellStyle.Padding = new Padding(8, 3, 8, 3);
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Theme.BlueFaint;
            grid.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            grid.RowTemplate.Height = 56;
            foreach (DataGridViewRow row in grid.Rows) row.Height = 56;
        }
    }
}
