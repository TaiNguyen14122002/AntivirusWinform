using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    internal static class AppDialog
    {
        internal static DialogResult Show(string text)
        { return Show(null, text, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); }

        internal static DialogResult Show(string text, string caption)
        { return Show(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information); }

        internal static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        { return Show(null, text, caption, buttons, MessageBoxIcon.None); }

        internal static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        { return Show(null, text, caption, buttons, icon); }

        internal static DialogResult Show(IWin32Window owner, string text)
        { return Show(owner, text, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); }

        internal static DialogResult Show(IWin32Window owner, string text, string caption)
        { return Show(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information); }

        internal static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons)
        { return Show(owner, text, caption, buttons, MessageBoxIcon.None); }

        internal static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (var dlg = new AppDialogForm(text, caption, buttons, icon))
            {
                IWin32Window actualOwner = owner ?? Form.ActiveForm;
                return actualOwner == null ? dlg.ShowDialog() : dlg.ShowDialog(actualOwner);
            }
        }

        internal static string Prompt(IWin32Window owner, string caption, string text, string placeholder = "", bool password = false)
        {
            using (var dlg = new AppInputDialogForm(caption, text, placeholder, password))
            {
                IWin32Window actualOwner = owner ?? Form.ActiveForm;
                DialogResult result = actualOwner == null ? dlg.ShowDialog() : dlg.ShowDialog(actualOwner);
                return result == DialogResult.OK ? dlg.Value.Trim() : null;
            }
        }
    }

    internal sealed class AppDialogForm : Form
    {
        readonly string message;
        readonly MessageBoxButtons buttons;
        readonly MessageBoxIcon icon;
        readonly Panel body;
        readonly FlowLayoutPanel footer;
        readonly Panel messageHost;
        readonly Label messageLabel;
        readonly AppDialogIcon iconView;
        DialogResult escapeResult;

        static string CompactMessage(string value)
        {
            string normalized = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split('\n');
            var compact = new System.Collections.Generic.List<string>(lines.Length);
            foreach (string line in lines)
            {
                string clean = (line ?? string.Empty).TrimEnd();
                if (string.IsNullOrWhiteSpace(clean)) continue;
                compact.Add(clean);
            }

            const int maxLines = 18;
            if (compact.Count > maxLines)
            {
                int hidden = compact.Count - (maxLines - 1);
                compact = compact.Take(maxLines - 1).ToList();
                compact.Add("… và " + hidden + " dòng khác.");
            }

            string result = string.Join(Environment.NewLine, compact.ToArray());
            const int maxChars = 2200;
            if (result.Length > maxChars)
                result = result.Substring(0, maxChars).TrimEnd() + Environment.NewLine + "… nội dung đã được rút gọn.";
            return result;
        }

        internal AppDialogForm(string text, string caption, MessageBoxButtons buttonSet, MessageBoxIcon messageIcon)
        {
            message = CompactMessage(text ?? string.Empty);
            buttons = buttonSet;
            icon = messageIcon;

            Text = string.IsNullOrWhiteSpace(caption) ? "Thông báo" : caption;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            KeyPreview = true;
            BackColor = Color.FromArgb(246, 249, 253);
            AutoScroll = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.BodyFont;
            Padding = new Padding(2);
            MinimumSize = new Size(360, 168);

            Rectangle work = Screen.PrimaryScreen == null
                ? new Rectangle(0, 0, 1280, 900)
                : Screen.PrimaryScreen.WorkingArea;
            int maxDialogW = Math.Max(430, Math.Min(680, work.Width - 80));
            int dialogW = message.Length > 650 ? Math.Min(650, maxDialogW)
                : message.Length > 260 ? Math.Min(550, maxDialogW)
                : Math.Min(430, maxDialogW);
            int textWidth = Math.Max(250, dialogW - 96);
            int textHeight = TextRenderer.MeasureText(message, Theme.BodyFont, new Size(textWidth, 5000),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + 4;
            int dialogH = 46 + 56 + 26 + Math.Max(36, textHeight);
            MaximumSize = new Size(maxDialogW, Math.Max(520, work.Height - 60));
            ClientSize = new Size(dialogW, Math.Max(174, dialogH));

            var header = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(242, 247, 253), Padding = new Padding(18, 0, 8, 0) };
            var title = new Label { Dock = DockStyle.Fill, Text = Text, Font = new Font(Theme.UiFontFamily, 12.05F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 33, 58), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            var close = new Button { Dock = DockStyle.Right, Width = 34, Text = "×", Font = new Font(Theme.UiFontFamily, 14.4F), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(242, 247, 253), ForeColor = Color.FromArgb(84, 103, 132), TabStop = false, Cursor = Cursors.Hand };
            close.FlatAppearance.BorderSize = 0;
            close.FlatAppearance.MouseOverBackColor = Theme.BlueTint;
            close.Click += delegate { DialogResult = escapeResult; Close(); };
            header.Controls.Add(title); header.Controls.Add(close);
            var headerLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(207, 220, 237) };
            header.Controls.Add(headerLine); headerLine.BringToFront();

            body = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 249, 253), Padding = new Padding(18, 10, 18, 8) };
            iconView = new AppDialogIcon { Location = new Point(18, 14), Size = new Size(42, 42), Icon = icon };
            messageHost = new Panel { BackColor = Color.FromArgb(246, 249, 253), AutoScroll = false };
            messageLabel = new Label
            {
                AutoSize = false,
                BackColor = Color.FromArgb(246, 249, 253),
                ForeColor = Color.FromArgb(31, 48, 73),
                Font = Theme.BodyFont,
                Text = message,
                TextAlign = ContentAlignment.TopLeft
            };
            messageHost.Controls.Add(messageLabel);
            body.Controls.Add(iconView); body.Controls.Add(messageHost);

            footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(14, 9, 14, 9),
                BackColor = Color.FromArgb(237, 244, 252)
            };
Controls.Add(body); Controls.Add(footer); Controls.Add(header);
            BuildButtons();

            Resize += delegate { LayoutBody(); UpdateRegion(); };
            Shown += delegate { LayoutBody(); UpdateRegion(); ActivateDefault(); };
            Paint += delegate(object sender, PaintEventArgs e)
            {
                using (var path = UiKit.Round(new Rectangle(1, 1, Width - 3, Height - 3), 16))
                using (var pen = new Pen(Theme.BorderStrong, 1.6f))
                {
                    pen.Alignment = PenAlignment.Inset;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            };
            KeyDown += DialogKeyDown;
        }

        void LayoutBody()
        {
            int top = 14;
            int x = 76;
            int right = 18;
            iconView.Location = new Point(18, top);
            int hostW = Math.Max(120, body.ClientSize.Width - x - right);
            int hostH = Math.Max(42, body.ClientSize.Height - top - 8);
            messageHost.SetBounds(x, top, hostW, hostH);
            int textH = TextRenderer.MeasureText(message, Theme.BodyFont, new Size(Math.Max(80, hostW - 6), 4000),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + 6;
            messageLabel.SetBounds(0, 0, Math.Max(80, hostW - 6), Math.Max(hostH, textH));
        }

        void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (var path = UiKit.Round(new Rectangle(0, 0, Width - 1, Height - 1), 16))
                Region = new Region(path);
        }

        void AddButton(string text, DialogResult result, Theme.BtnRole role, bool defaultButton = false)
        {
            var button = new Button
            {
                Text = text,
                DialogResult = result,
                Width = Math.Max(94, TextRenderer.MeasureText(text, Theme.ButtonFont).Width + 34),
                Height = 36,
                Margin = new Padding(8, 0, 0, 0),
                TabStop = true
            };
            Theme.StyleButton(button, role);
            footer.Controls.Add(button);
            if (defaultButton) AcceptButton = button;
        }

        void BuildButtons()
        {
            switch (buttons)
            {
                case MessageBoxButtons.OKCancel:
                    AddButton("Hủy", DialogResult.Cancel, Theme.BtnRole.Secondary); escapeResult = DialogResult.Cancel;
                    AddButton("OK", DialogResult.OK, Theme.BtnRole.Primary, true); break;
                case MessageBoxButtons.YesNo:
                    AddButton("Không", DialogResult.No, Theme.BtnRole.Secondary); escapeResult = DialogResult.No;
                    AddButton("Có", DialogResult.Yes, Theme.BtnRole.Primary, true); break;
                case MessageBoxButtons.YesNoCancel:
                    AddButton("Hủy", DialogResult.Cancel, Theme.BtnRole.Secondary); escapeResult = DialogResult.Cancel;
                    AddButton("Không", DialogResult.No, Theme.BtnRole.Secondary);
                    AddButton("Có", DialogResult.Yes, Theme.BtnRole.Primary, true); break;
                case MessageBoxButtons.RetryCancel:
                    AddButton("Hủy", DialogResult.Cancel, Theme.BtnRole.Secondary); escapeResult = DialogResult.Cancel;
                    AddButton("Thử lại", DialogResult.Retry, Theme.BtnRole.Primary, true); break;
                case MessageBoxButtons.AbortRetryIgnore:
                    AddButton("Bỏ qua", DialogResult.Ignore, Theme.BtnRole.Secondary); escapeResult = DialogResult.Ignore;
                    AddButton("Thử lại", DialogResult.Retry, Theme.BtnRole.Secondary);
                    AddButton("Dừng", DialogResult.Abort, Theme.BtnRole.Primary, true); break;
                default:
                    escapeResult = DialogResult.OK;
                    AddButton("OK", DialogResult.OK, Theme.BtnRole.Primary, true); break;
            }
            CancelButton = footer.Controls.OfType<Button>().FirstOrDefault(b => b.DialogResult == escapeResult);
        }

        void ActivateDefault()
        {
            var b = AcceptButton as Button;
            if (b != null) { b.Focus(); b.Select(); }
        }

        void DialogKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Escape) return;
            DialogResult = escapeResult;
            Close();
            e.Handled = true;
        }
    }

    internal sealed class AppInputDialogForm : Form
    {
        readonly TextBox input;
        internal string Value { get { return input.Text ?? string.Empty; } }

        internal AppInputDialogForm(string caption, string text, string placeholder, bool password)
        {
            Text = string.IsNullOrWhiteSpace(caption) ? "Nhập thông tin" : caption;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            KeyPreview = true;
            BackColor = Color.FromArgb(246, 249, 253);
            AutoScroll = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.DefaultUiFont;
            ClientSize = new Size(470, 224);
            MinimumSize = new Size(400, 210);
            MaximumSize = new Size(580, 320);

            var header = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(242, 247, 253), Padding = new Padding(18, 0, 8, 0) };
            var title = new Label { Dock = DockStyle.Fill, Text = Text, Font = new Font(Theme.UiFontFamily, 12.05F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 33, 58), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            var close = new Button { Dock = DockStyle.Right, Width = 34, Text = "×", Font = new Font(Theme.UiFontFamily, 14.4F), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(242, 247, 253), ForeColor = Color.FromArgb(84, 103, 132), TabStop = false, Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };
            close.FlatAppearance.BorderSize = 0;
            close.FlatAppearance.MouseOverBackColor = Theme.BlueTint;
            header.Controls.Add(title); header.Controls.Add(close);
            var headerLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(207, 220, 237) };
            header.Controls.Add(headerLine); headerLine.BringToFront();

            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 249, 253), Padding = new Padding(18, 12, 18, 8) };
            var iconView = new AppDialogIcon { Icon = MessageBoxIcon.Information, Location = new Point(18, 14), Size = new Size(42, 42) };
            var message = new Label { Text = text ?? string.Empty, Font = Theme.BodyFont, ForeColor = Color.FromArgb(31, 48, 73), BackColor = Color.FromArgb(246, 249, 253), AutoSize = false, Location = new Point(76, 13), Size = new Size(360, 48) };
            input = new TextBox { Font = Theme.BodyFont, BackColor = Color.White, ForeColor = Theme.TextDark, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = password, Location = new Point(76, 70), Size = new Size(360, 32) };
            if (!string.IsNullOrEmpty(placeholder)) input.Text = placeholder;
            body.Controls.Add(iconView); body.Controls.Add(message); body.Controls.Add(input);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(14, 9, 14, 9), BackColor = Color.FromArgb(237, 244, 252) };
var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Width = 94, Height = 36, Margin = new Padding(8, 0, 0, 0) };
            var ok = new Button { Text = password ? "Xác nhận" : "Lưu", DialogResult = DialogResult.OK, Width = 104, Height = 36, Margin = new Padding(8, 0, 0, 0) };
            Theme.StyleButton(cancel, Theme.BtnRole.Secondary);
            Theme.StyleButton(ok, Theme.BtnRole.Primary);
            footer.Controls.Add(cancel); footer.Controls.Add(ok);
            AcceptButton = ok; CancelButton = cancel;

            Controls.Add(body); Controls.Add(footer); Controls.Add(header);
            Resize += delegate
            {
                message.Width = Math.Max(180, body.ClientSize.Width - 94);
                input.Width = Math.Max(180, body.ClientSize.Width - 94);
                UpdateRegion();
            };
            Shown += delegate { UpdateRegion(); input.Focus(); input.SelectAll(); };
            Paint += delegate(object sender, PaintEventArgs e)
            {
                using (var path = UiKit.Round(new Rectangle(1, 1, Width - 3, Height - 3), 16))
                using (var pen = new Pen(Theme.BorderStrong, 1.6f))
                {
                    pen.Alignment = PenAlignment.Inset;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            };
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode != Keys.Escape) return;
                DialogResult = DialogResult.Cancel; Close(); e.Handled = true;
            };
        }

        void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (var path = UiKit.Round(new Rectangle(0, 0, Width - 1, Height - 1), 16)) Region = new Region(path);
        }
    }

    internal sealed class AppDialogIcon : System.Windows.Forms.Control
    {
        internal MessageBoxIcon Icon;
        internal AppDialogIcon()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.FromArgb(246, 249, 253);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color ink, soft;
            string glyph;
            switch (Icon)
            {
                case MessageBoxIcon.Error: ink = Theme.Red; soft = Theme.RedTint; glyph = "x"; break;
                case MessageBoxIcon.Warning: ink = Theme.Amber; soft = Theme.AmberTint; glyph = "alert"; break;
                case MessageBoxIcon.Question: ink = Theme.Blue; soft = Theme.BlueTint; glyph = "info"; break;
                case MessageBoxIcon.None: ink = Theme.TextGray; soft = Theme.ChipGray; glyph = "info"; break;
                default: ink = Theme.Blue; soft = Theme.BlueTint; glyph = "info"; break;
            }
            using (var brush = new SolidBrush(soft)) e.Graphics.FillEllipse(brush, 0, 0, Width - 1, Height - 1);
            Sketch20Style.Icon(e.Graphics, glyph, new Rectangle(13, 13, 22, 22), ink);
        }
    }
}
