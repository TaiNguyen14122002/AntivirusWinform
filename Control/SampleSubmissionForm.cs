using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>Popup local sample submission. No sample bytes are sent to any cloud service.</summary>
    internal sealed class SampleSubmissionForm : Form
    {
        private static readonly Color Ink = Color.FromArgb(30, 45, 70);
        private static readonly Color Blue = Color.FromArgb(28, 103, 220);
        private static readonly Color Muted = Color.FromArgb(105, 120, 145);
        private readonly List<string> files = new List<string>();
        private readonly ListBox selected;
        private readonly TextBox details;
        private readonly Label status;
        private readonly Label counter;
        private readonly CheckBox verified;
        private readonly Button choose, remove, start, cancel;
        private readonly ProgressBar bar;
        private CancellationTokenSource cancellation;
        private bool running;

        internal static void Open(IWin32Window owner)
        {
            using (var form = new SampleSubmissionForm())
            {
                IWin32Window parent = owner ?? Form.ActiveForm;
                if (parent == null) form.ShowDialog();
                else form.ShowDialog(parent);
            }
        }

        private static Button MakeButton(string label, bool primary)
        {
            var button = new Button
            {
                Text = label,
                Height = 46,
                Width = 168,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(Theme.UiFontFamily, 12.55F, FontStyle.Bold),
                BackColor = primary ? Blue : Color.White,
                ForeColor = primary ? Color.White : Ink,
                Margin = new Padding(0),
                AutoSize = false,
                AutoEllipsis = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(206, 218, 234);
            return button;
        }

        // Đo độ rộng thật của chữ theo font/DPI hiện tại; không rút gọn thành ba chấm.
        private static int WidthForButton(Button button, int minimum)
        {
            if (button == null) return minimum;
            Size measured = TextRenderer.MeasureText(button.Text ?? string.Empty, button.Font,
                new Size(10000, 200), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            return Math.Max(minimum, measured.Width + 54);
        }

        private void FitActionButtons()
        {
            var selectionLayout = choose == null ? null : choose.Parent as TableLayoutPanel;
            if (selectionLayout != null && selectionLayout.ColumnStyles.Count >= 5)
            {
                // Cột 1 là khoảng cách cố định, không nằm trong vùng bấm của hai nút.
                selectionLayout.ColumnStyles[0].Width = WidthForButton(choose, 192);
                selectionLayout.ColumnStyles[2].Width = WidthForButton(remove, 208);
            }

            var actionLayout = start == null ? null : start.Parent as TableLayoutPanel;
            if (actionLayout != null && actionLayout.ColumnStyles.Count >= 4)
            {
                actionLayout.ColumnStyles[1].Width = WidthForButton(start, 208);
                actionLayout.ColumnStyles[3].Width = WidthForButton(cancel, 172);
            }
        }

        private static Label MakeLabel(string text, float size, bool bold = false)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = Ink,
                Font = new Font(Theme.UiFontFamily, size, bold ? FontStyle.Bold : FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private SampleSubmissionForm()
        {
            Text = "Gửi mẫu tệp nghi ngờ";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(980, 760);
            Size = new Size(1120, 810);
            BackColor = Color.FromArgb(248, 251, 255);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font(Theme.UiFontFamily, 11.55F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(0);

            var outer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 24, 30, 24),
                ColumnCount = 1,
                RowCount = 8,
                BackColor = BackColor
            };
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            Controls.Add(outer);

            var title = MakeLabel("GỬI MẪU TỆP NGHI NGỜ", 23F, true);
            title.Margin = new Padding(0, 0, 0, 8);
            outer.Controls.Add(title, 0, 0);

            var note = MakeLabel(
                "Nhập danh sách hash (.csv.zip, .csv, .tsv, .jsonl, .json) hoặc mẫu thực (.zip, .exe, .js và nhiều định dạng khác). " +
                "Dữ liệu được kiểm tra trùng và lưu vào AntivirusDB; không thực thi tệp.", 12.5F);
            note.ForeColor = Muted;
            note.Margin = new Padding(0, 4, 0, 12);
            outer.Controls.Add(note, 0, 1);

            var pickerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                Margin = new Padding(0, 0, 0, 12)
            };
            pickerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 192));
            pickerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24)); // Khoảng cách giữa hai nút.
            pickerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 208));
            pickerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
            pickerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            choose = MakeButton("Chọn tệp", true);
            remove = MakeButton("Xóa lựa chọn", false);
            counter = MakeLabel("0 tệp đã chọn", 12F, true);
            counter.AutoSize = false;
            counter.Margin = new Padding(0);
            counter.Padding = new Padding(0, 0, 0, 2);

            pickerTable.Controls.Add(choose, 0, 0);
            pickerTable.Controls.Add(remove, 2, 0);
            pickerTable.Controls.Add(counter, 4, 0);
            outer.Controls.Add(pickerTable, 0, 2);

            selected = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.UiFontFamily, 11.55F),
                BackColor = Color.White,
                HorizontalScrollbar = true,
                SelectionMode = SelectionMode.MultiExtended,
                IntegralHeight = false
            };
            selected.Margin = new Padding(0, 0, 0, 12);
            outer.Controls.Add(selected, 0, 3);

            verified = new CheckBox
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = "Đã xác minh mẫu, cho phép kích hoạt chữ ký (IsActive = 1)",
                Checked = false,
                ForeColor = Ink,
                Font = new Font(Theme.UiFontFamily, 13.4F, FontStyle.Bold),
                Margin = new Padding(0, 4, 0, 8)
            };
            outer.Controls.Add(verified, 0, 4);

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 0, 12) };
            bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            status = MakeLabel("Mẫu nghi ngờ mặc định: IsActive = 0. Chỉ bật sau khi có bằng chứng xác minh.", 11.5F);
            status.ForeColor = Muted;
            status.Margin = new Padding(0, 0, 0, 6);
            bottom.Controls.Add(status, 0, 0);
            details = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font(Theme.UiFontFamily, 11.55F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Text = "Chọn tệp để bắt đầu. Các tệp gốc không bị thực thi, xóa hay gửi lên Internet."
            };
            bottom.Controls.Add(details, 0, 1);
            outer.Controls.Add(bottom, 0, 5);

            bar = new ProgressBar { Dock = DockStyle.Fill, Height = 18, Style = ProgressBarStyle.Blocks, Maximum = 100, Margin = new Padding(0) };
            outer.Controls.Add(bar, 0, 6);

            var buttonRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                Margin = new Padding(0, 12, 0, 0)
            };
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 208));
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14));
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 172));
            cancel = MakeButton("Đóng", false);
            start = MakeButton("Kiểm tra và lưu", true);
            buttonRow.Controls.Add(start, 1, 0);
            buttonRow.Controls.Add(cancel, 3, 0);
            outer.Controls.Add(buttonRow, 0, 7);

            // Cập nhật kích thước các nút khi form được mở, đổi kích thước hoặc đổi DPI.
            Shown += (sender, args) => FitActionButtons();
            Resize += (sender, args) => FitActionButtons();
            FitActionButtons();

            choose.Click += Choose_Click;
            remove.Click += Remove_Click;
            start.Click += Start_Click;
            cancel.Click += Cancel_Click;
            FormClosing += (sender, args) =>
            {
                if (!running) return;
                cancellation.Cancel();
                args.Cancel = true;
                status.Text = "Đang yêu cầu dừng, chờ thao tác hiện tại hoàn tất";
            };
        }

        private void Choose_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "Chọn mẫu tệp nghi ngờ",
                Filter = "Tất cả định dạng (*.*)|*.*|Danh sách chữ ký (*.csv.zip;*.csv;*.tsv;*.json;*.jsonl;*.gz)|*.csv.zip;*.csv;*.tsv;*.json;*.jsonl;*.ndjson;*.csv.gz;*.tsv.gz;*.jsonl.gz;*.sha256;*.sha1;*.md5|Mẫu tệp và ZIP (*.zip;*.exe;*.dll;*.js;*.ps1;*.bat;*.vbs;*.scr;*.com)|*.zip;*.exe;*.dll;*.js;*.ps1;*.bat;*.vbs;*.scr;*.com|Tệp nén (*.zip;*.gz)|*.zip;*.gz",
                Multiselect = true,
                CheckFileExists = true,
                RestoreDirectory = true
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                foreach (string path in dialog.FileNames)
                {
                    if (files.Count >= 500) break;
                    if (!files.Any(s => string.Equals(s, path, StringComparison.OrdinalIgnoreCase)))
                        files.Add(path);
                }
                UpdateSelected();
            }
        }

        private void Remove_Click(object sender, EventArgs e)
        {
            foreach (string item in selected.SelectedItems.Cast<string>().ToList()) files.Remove(item);
            UpdateSelected();
        }

        private void UpdateSelected()
        {
            selected.BeginUpdate();
            try
            {
                selected.Items.Clear();
                foreach (string path in files) selected.Items.Add(path);
            }
            finally { selected.EndUpdate(); }
            counter.Text = files.Count.ToString("N0") + " tệp đã chọn";
        }

        private async void Start_Click(object sender, EventArgs e)
        {
            if (running) return;
            if (files.Count == 0) { AppDialog.Show(this, "Bạn chưa chọn tệp.", "Gửi mẫu"); return; }
            if (verified.Checked && AppDialog.Show(this,
                "Bạn xác nhận đã kiểm chứng các tệp chọn là mã độc?\r\n\r\n" +
                "Chữ ký sẽ được kích hoạt (IsActive = 1) và được dùng để phát hiện / cách ly tệp khớp hash.\r\n" +
                "Không chọn Có nếu đây chỉ là tệp nghi ngờ hoặc tệp thử nghiệm chưa xác minh.",
                "Xác nhận kích hoạt chữ ký", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            bool confirmed = verified.Checked;
            string[] snapshot = files.ToArray();
            running = true;
            cancellation = new CancellationTokenSource();
            choose.Enabled = false; remove.Enabled = false; start.Enabled = false; verified.Enabled = false;
            cancel.Text = "Hủy xử lý";
            FitActionButtons();
            bar.Style = ProgressBarStyle.Marquee; bar.MarqueeAnimationSpeed = 30;
            details.Text = "Đang đọc danh sách chữ ký hoặc tính hash mẫu và đối chiếu SQL Server";
            var uiProgress = new Progress<SuspiciousSampleImporter.Progress>(p =>
            {
                if (IsDisposed) return;
                status.Text = "Tệp " + p.SelectedIndex + "/" + p.SelectedTotal + ": " + p.Current;
                details.Text = "Đã phân tích " + p.Processed + " | Thêm " + p.Inserted +
                    " | Đã có " + p.Duplicate + " | Lỗi " + p.Failed;
            });
            try
            {
                SuspiciousSampleImporter.Report result = await Task.Run(() =>
                    SuspiciousSampleImporter.Import(snapshot, confirmed, uiProgress, cancellation.Token));
                status.Text = "Đã hoàn tất: " + result.Inserted + " mới, " + result.Duplicate + " trùng, " + result.Failed + " lỗi, " + result.Skipped + " bỏ qua.";
                details.Text = "TỔNG KẾT\r\n" +
                    "Đã phân tích: " + result.Processed + "\r\n" +
                    "Thêm mới: " + result.Inserted + "\r\n" +
                    "Đã tồn tại: " + result.Duplicate + "\r\n" +
                    "Lỗi: " + result.Failed + "\r\n" +
                    "Bỏ qua: " + result.Skipped + "\r\n" +
                    "Chữ ký mới kích hoạt: " + result.Activated + "\r\n\r\n" +
                    string.Join("\r\n", result.Messages);
            }
            catch (OperationCanceledException)
            {
                status.Text = "Đã hủy theo yêu cầu. Những bản ghi đã lưu vẫn được giữ; chạy lại sẽ bỏ qua hash trùng.";
            }
            catch (Exception ex)
            {
                status.Text = "Không thể nhập mẫu: " + ex.Message;
                details.Text = "LỖI: " + ex.ToString();
                AppDialog.Show(this, "Nhập mẫu thất bại: " + ex.Message, "Gửi mẫu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                bar.MarqueeAnimationSpeed = 0; bar.Style = ProgressBarStyle.Blocks;
                cancel.Text = "Đóng"; cancel.Enabled = true; running = false;
                FitActionButtons();
                choose.Enabled = true; remove.Enabled = true; start.Enabled = true; verified.Enabled = true;
                cancellation.Dispose(); cancellation = null;
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            if (running) { cancellation.Cancel(); cancel.Enabled = false; status.Text = "Đang dừng"; }
            else Close();
        }
    }
}
