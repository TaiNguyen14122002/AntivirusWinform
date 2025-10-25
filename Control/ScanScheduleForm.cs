using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// Trang đặt lịch quét: bố cục co giãn, nhập liệu có ràng buộc và sử dụng
    /// Task Scheduler hiện có. Không yêu cầu đăng nhập Quản trị.
    /// </summary>
    public sealed class ScanScheduleForm : Form
    {
        private static readonly Color Surface = Color.White;
        private static readonly Color PageBackground = Color.FromArgb(245, 248, 253);
        private static readonly Color Border = Color.FromArgb(214, 226, 241);
        private static readonly Color Ink = Color.FromArgb(28, 43, 68);
        private static readonly Color Muted = Color.FromArgb(98, 115, 139);
        private static readonly Color Blue = Color.FromArgb(20, 109, 229);
        private static readonly Color Red = Color.FromArgb(191, 55, 65);

        private readonly DataGridView table = new DataGridView();
        private readonly TextBox name = new TextBox();
        private readonly TextBox path = new TextBox();
        private readonly ComboBox type = new ComboBox();
        private readonly ComboBox frequency = new ComboBox();
        private readonly DateTimePicker when = new DateTimePicker();
        private readonly Label hint = new Label();
        private readonly Label validationHint = new Label();
        private readonly Label clockLabel = new Label();
        private readonly Label countLabel = new Label();
        private readonly ToolTip toolTip = new ToolTip();
        private readonly ErrorProvider errors = new ErrorProvider();
        private readonly Timer clockTimer = new Timer();
        private readonly TableLayoutPanel input = new TableLayoutPanel();
        private readonly Button save, clear, toggle, run, delete, reload, chooseFile, chooseFolder, editTargets;
        private readonly List<string> scanTargets = new List<string>();
        private long selectedId;
        private bool changingSelection;
        private bool inputIsValid;

        public ScanScheduleForm()
        {
            Text = "Đặt thời gian quét virus tự động";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1170, 740);
            MinimumSize = new Size(940, 640);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font(Theme.UiFontFamily, 10.2f);
            BackColor = PageBackground;
            DoubleBuffered = true;
            KeyPreview = true;

            errors.ContainerControl = this;
            errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            var root = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                BackColor = PageBackground, Padding = new Padding(16, 12, 16, 10),
                Margin = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 244));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            Controls.Add(root);

            var heading = new Panel { Dock = DockStyle.Fill, BackColor = PageBackground };
            heading.Controls.Add(new Label {
                Text = "Thiết lập lịch quét tự động", Font = new Font(Theme.UiFontFamily, 16.1f, FontStyle.Bold),
                ForeColor = Ink, Dock = DockStyle.Top, Height = 34
            });
            heading.Controls.Add(new Label {
                Text = "Đặt thời gian để Windows tự khởi chạy chương trình quét virus.",
                ForeColor = Muted, Dock = DockStyle.Bottom, Height = 24,
                Font = new Font(Theme.UiFontFamily, 9.6f)
            });
            clockLabel.ForeColor = Muted;
            clockLabel.Font = new Font(Theme.UiFontFamily, 9.5f);
            clockLabel.TextAlign = ContentAlignment.MiddleRight;
            clockLabel.SetBounds(760, 3, 355, 28);
            clockLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            heading.Controls.Add(clockLabel);
            clockLabel.BringToFront();
            heading.Resize += (s, e) => clockLabel.SetBounds(
                Math.Max(400, heading.ClientSize.Width - 355), 2, 350, 28);
            root.Controls.Add(heading, 0, 0);

            var settingsCard = Card();
            settingsCard.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(settingsCard, 0, 1);
            var editor = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                Margin = Padding.Empty, BackColor = Surface
            };
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            settingsCard.Controls.Add(editor);
            editor.Controls.Add(new Label {
                Text = "Thông tin lịch quét", Dock = DockStyle.Fill, ForeColor = Ink,
                Font = new Font(Theme.UiFontFamily, 12.3f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            input.Dock = DockStyle.Fill;
            input.Margin = Padding.Empty;
            input.ColumnCount = 3;
            input.RowCount = 4;
            input.BackColor = Surface;
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 206));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
            for (int i = 0; i < 4; i++) input.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            editor.Controls.Add(input, 0, 1);

            AddLabel("Tên lịch", 0);
            name.MaxLength = 150;
            SetupTextBox(name);
            name.TextChanged += (s, e) => RefreshValidation();
            input.Controls.Add(name, 1, 0);
            input.SetColumnSpan(name, 2);
            toolTip.SetToolTip(name, "Tên lịch dài 3–150 ký tự, không được trùng với lịch khác.");

            AddLabel("Chế độ quét", 1);
            type.Items.AddRange(new object[] { "Quét nhanh", "Quét toàn bộ hệ thống", "Quét tệp và thư mục (nhiều mục tiêu)" });
            SetupCombo(type);
            type.SelectedIndex = 0;
            input.Controls.Add(type, 1, 1);
            input.SetColumnSpan(type, 2);

            AddLabel("Danh sách mục tiêu", 2);
            SetupTextBox(path);
            path.ReadOnly = true;
            path.Cursor = Cursors.Hand;
            path.Click += (s, e) => EditTargets();
            input.Controls.Add(path, 1, 2);
            toolTip.SetToolTip(path, "Nhấn vào ô này hoặc Danh sách để xem, thêm và xóa mục tiêu quét.");
            var browse = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
            browse.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            browse.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            browse.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            chooseFile = Btn("Thêm tệp");
            chooseFolder = Btn("Thêm thư mục");
            editTargets = Btn("Danh sách");
            browse.Controls.Add(chooseFile, 0, 0);
            browse.Controls.Add(chooseFolder, 1, 0);
            browse.Controls.Add(editTargets, 2, 0);
            input.Controls.Add(browse, 2, 2);
            chooseFile.Click += (s, e) => PickFile();
            chooseFolder.Click += (s, e) => PickFolder();
            editTargets.Click += (s, e) => EditTargets();

            AddLabel("Bắt đầu và tần suất", 3);
            when.Format = DateTimePickerFormat.Custom;
            when.CustomFormat = "dd/MM/yyyy HH:mm";
            when.ShowUpDown = false;
            when.Dock = DockStyle.Fill;
            when.Margin = new Padding(4, 5, 8, 5);
            when.CalendarFont = new Font(Theme.UiFontFamily, 10f);
            when.Value = DefaultStartTime();
            when.ValueChanged += (s, e) => RefreshValidation();
            input.Controls.Add(when, 1, 3);
            frequency.Items.AddRange(new object[] { "Hằng ngày", "Hằng tuần", "Hằng tháng", "Một lần" });
            SetupCombo(frequency);
            frequency.SelectedIndex = 0;
            frequency.SelectedIndexChanged += (s, e) => RefreshValidation();
            input.Controls.Add(frequency, 2, 3);
            type.SelectedIndexChanged += (s, e) => UpdateTargetMode();
            path.TextChanged += (s, e) => RefreshValidation();

            validationHint.Dock = DockStyle.Fill;
            validationHint.TextAlign = ContentAlignment.MiddleLeft;
            validationHint.Font = new Font(Theme.UiFontFamily, 9.1f);
            validationHint.ForeColor = Muted;
            editor.Controls.Add(validationHint, 0, 2);

            var listCard = Card();
            listCard.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(listCard, 0, 2);
            var listLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = Padding.Empty };
            listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            listCard.Controls.Add(listLayout);
            var listHeader = new Panel { Dock = DockStyle.Fill };
            countLabel.Text = "Các lịch đã tạo (0)";
            countLabel.Dock = DockStyle.Left;
            countLabel.Width = 400;
            countLabel.TextAlign = ContentAlignment.MiddleLeft;
            countLabel.Font = new Font(Theme.UiFontFamily, 12.3f, FontStyle.Bold);
            countLabel.ForeColor = Ink;
            listHeader.Controls.Add(countLabel);
            var note = new Label {
                Text = "Chọn một lịch để sửa hoặc thực hiện thao tác",
                Dock = DockStyle.Right, AutoSize = false, Width = 400,
                TextAlign = ContentAlignment.MiddleRight, ForeColor = Muted,
                Font = new Font(Theme.UiFontFamily, 9.2f)
            };
            listHeader.Controls.Add(note);
            listLayout.Controls.Add(listHeader, 0, 0);
            SetupTable();
            listLayout.Controls.Add(table, 0, 1);
            table.SelectionChanged += (s, e) => {
                if (changingSelection) return;
                if (table.SelectedRows.Count == 0 || table.CurrentRow == null)
                {
                    selectedId = 0;
                    UpdateSelectionButtons();
                }
                else CopySelected();
            };
            table.DataBindingComplete += (s, e) => UpdateGridTooltips();
            table.CellFormatting += TableCellFormatting;

            var actions = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1, Margin = Padding.Empty,
                BackColor = PageBackground
            };
            foreach (float width in new[] { 22f, 16f, 18f, 14f, 14f, 16f })
                actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(actions, 0, 3);
            save = Btn("Lưu / cập nhật lịch");
            clear = Btn("Tạo lịch mới");
            toggle = Btn("Bật / tắt lịch");
            run = Btn("Chạy ngay");
            delete = Btn("Xóa lịch");
            reload = Btn("Tải lại");
            Button[] buttons = { save, clear, toggle, run, delete, reload };
            for (int i = 0; i < buttons.Length; i++) actions.Controls.Add(buttons[i], i, 0);
            save.BackColor = Blue;
            save.ForeColor = Color.White;
            save.FlatAppearance.BorderColor = Blue;
            delete.ForeColor = Red;
            delete.FlatAppearance.BorderColor = Color.FromArgb(239, 201, 207);
            save.Click += (s, e) => Save();
            clear.Click += (s, e) => Reset();
            toggle.Click += (s, e) => Toggle();
            run.Click += (s, e) => RunNow();
            delete.Click += (s, e) => Delete();
            reload.Click += (s, e) => RefreshData();

            hint.Dock = DockStyle.Fill;
            hint.TextAlign = ContentAlignment.MiddleLeft;
            hint.ForeColor = Muted;
            hint.Font = new Font(Theme.UiFontFamily, 9.4f);
            hint.Text = "Lịch chạy qua Windows Task Scheduler bằng tài khoản đang đăng nhập (không cần mật khẩu). " +
                        "Máy cần bật và tài khoản còn đăng nhập; kết quả được lưu ở Lịch sử.";
            root.Controls.Add(hint, 0, 4);

            Resize += (s, e) => UpdateResponsive();
            Shown += (s, e) => { UpdateResponsive(); RefreshData(); Reset(); };
            UpdateTargetMode();
            UpdateSelectionButtons();
            clockTimer.Interval = 30000;
            clockTimer.Tick += (s, e) => { UpdateClock(); RefreshValidation(); };
            clockTimer.Start();
            FormClosed += (s, e) => { clockTimer.Stop(); clockTimer.Dispose(); errors.Dispose(); toolTip.Dispose(); };
            UpdateClock();
        }

        private static DateTime DefaultStartTime()
        {
            // Không đặt lịch cũ khi tạo mới vào cuối ngày hoặc gần nửa đêm.
            return DateTime.Now.Date.AddDays(1).AddHours(22);
        }

        private static Panel Card()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 10), BackColor = Surface };
            panel.Paint += (s, e) => {
                using (var pen = new Pen(Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, panel.Width - 1), Math.Max(0, panel.Height - 1));
            };
            return panel;
        }

        private static Button Btn(string text)
        {
            var button = new Button {
                Text = text, Dock = DockStyle.Fill, Margin = new Padding(4, 6, 4, 6),
                FlatStyle = FlatStyle.Flat, BackColor = Surface, ForeColor = Ink,
                Font = new Font(Theme.UiFontFamily, 9.6f, FontStyle.Bold),
                Cursor = Cursors.Hand, AutoSize = false, UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            return button;
        }

        private void AddLabel(string text, int row)
        {
            var label = new Label { Text = text, Dock = DockStyle.Fill, AutoSize = false,
                ForeColor = Ink, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Theme.UiFontFamily, 10.0f, FontStyle.Bold), Margin = new Padding(4, 3, 4, 3) };
            input.Controls.Add(label, 0, row);
        }

        private static void SetupTextBox(TextBox field)
        {
            field.Dock = DockStyle.Fill;
            field.AutoSize = false;
            field.BorderStyle = BorderStyle.FixedSingle;
            field.BackColor = Surface;
            field.ForeColor = Ink;
            field.Font = new Font(Theme.UiFontFamily, 10.5f);
            field.Margin = new Padding(4, 5, 8, 5);
        }

        private static void SetupCombo(ComboBox field)
        {
            field.Dock = DockStyle.Fill;
            field.DropDownStyle = ComboBoxStyle.DropDownList;
            field.FlatStyle = FlatStyle.Flat;
            field.Font = new Font(Theme.UiFontFamily, 10.4f);
            field.ForeColor = Ink;
            field.BackColor = Surface;
            field.Margin = new Padding(4, 5, 8, 5);
        }

        private void SetupTable()
        {
            table.Dock = DockStyle.Fill;
            table.BorderStyle = BorderStyle.None;
            table.BackgroundColor = Surface;
            table.EnableHeadersVisualStyles = false;
            table.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            table.RowHeadersVisible = false;
            table.AllowUserToAddRows = false;
            table.AllowUserToDeleteRows = false;
            table.AllowUserToResizeRows = false;
            table.ReadOnly = true;
            table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            table.MultiSelect = false;
            table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            table.ColumnHeadersHeight = 42;
            table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            table.RowTemplate.Height = 37;
            table.GridColor = Color.FromArgb(232, 238, 246);
            table.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            table.DefaultCellStyle.Font = new Font(Theme.UiFontFamily, 9.4f);
            table.ColumnHeadersDefaultCellStyle.Font = new Font(Theme.UiFontFamily, 9.6f, FontStyle.Bold);
            table.ColumnHeadersDefaultCellStyle.ForeColor = Ink;
            table.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(236, 244, 254);
            table.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            table.ColumnHeadersDefaultCellStyle.Padding = new Padding(7, 0, 4, 0);
            table.DefaultCellStyle.ForeColor = Ink;
            table.DefaultCellStyle.BackColor = Surface;
            table.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 235, 255);
            table.DefaultCellStyle.SelectionForeColor = Ink;
            table.DefaultCellStyle.Padding = new Padding(7, 3, 4, 3);
            table.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 251, 255);
            table.ScrollBars = ScrollBars.Both;
        }

        private void UpdateResponsive()
        {
            bool compact = ClientSize.Width < 1070;
            input.ColumnStyles[0].Width = compact ? 176 : 206;
            input.ColumnStyles[2].Width = compact ? 300 : 330;
            chooseFile.Font = chooseFolder.Font = editTargets.Font = new Font(Theme.UiFontFamily, compact ? 8.4f : 9.0f, FontStyle.Bold);
            save.Font = new Font(Theme.UiFontFamily, compact ? 8.9f : 9.6f, FontStyle.Bold);
            clear.Font = toggle.Font = run.Font = delete.Font = reload.Font =
                new Font(Theme.UiFontFamily, compact ? 9.0f : 9.6f, FontStyle.Bold);
        }

        private void UpdateClock()
        {
            clockLabel.Text = "Hiện tại: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        }

        private void UpdateTargetMode()
        {
            bool custom = type.SelectedIndex == 2;
            path.Enabled = custom;
            path.BackColor = custom ? Surface : Color.FromArgb(247, 249, 252);
            chooseFile.Enabled = chooseFolder.Enabled = editTargets.Enabled = custom;
            UpdateTargetSummary();
            RefreshValidation();
        }

        private void UpdateTargetSummary()
        {
            if (type.SelectedIndex != 2)
            {
                path.Text = "Không áp dụng cho chế độ này";
                return;
            }
            int count = scanTargets.Count;
            if (count == 0) path.Text = "Chưa chọn tệp hoặc thư mục";
            else if (count == 1) path.Text = scanTargets[0];
            else
            {
                int folders = scanTargets.Count(Directory.Exists);
                path.Text = count + " mục tiêu (" + (count - folders) + " tệp, " + folders + " thư mục)";
            }
            toolTip.SetToolTip(path, count == 0
                ? "Thêm tệp hoặc thư mục cần quét. Có thể kết hợp nhiều loại."
                : string.Join(Environment.NewLine, scanTargets.Take(20)) +
                    (count > 20 ? Environment.NewLine + "... và " + (count - 20) + " mục khác" : ""));
        }

        private static void AddPathsTo(List<string> destination, IEnumerable<string> added)
        {
            var all = ScanScheduleService.NormalizeTargets(destination.Concat(added));
            destination.Clear();
            destination.AddRange(all);
        }

        private bool PickFilesInto(List<string> destination, IWin32Window owner = null)
        {
            using (var dialog = new OpenFileDialog {
                Title = "Chọn một hoặc nhiều tệp quét tự động",
                Filter = "Tất cả tệp (*.*)|*.*", CheckFileExists = true, Multiselect = true
            })
            {
                if (dialog.ShowDialog(owner ?? this) != DialogResult.OK) return false;
                AddPathsTo(destination, dialog.FileNames);
                return true;
            }
        }

        private bool PickFolderInto(List<string> destination, IWin32Window owner = null)
        {
            using (var dialog = new FolderBrowserDialog { Description =
                "Chọn một thư mục; nhấn Thêm thư mục lần nữa để bổ sung thư mục khác." })
            {
                if (dialog.ShowDialog(owner ?? this) != DialogResult.OK) return false;
                AddPathsTo(destination, new[] { dialog.SelectedPath });
                return true;
            }
        }

        private void PickFile()
        {
            if (type.SelectedIndex != 2) return;
            try { if (PickFilesInto(scanTargets)) UpdateTargetSummary(); }
            catch (ArgumentException ex) { MessageBox.Show(this, ex.Message, "Mục tiêu quét", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void PickFolder()
        {
            if (type.SelectedIndex != 2) return;
            try { if (PickFolderInto(scanTargets)) UpdateTargetSummary(); }
            catch (ArgumentException ex) { MessageBox.Show(this, ex.Message, "Mục tiêu quét", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void EditTargets()
        {
            if (type.SelectedIndex != 2) return;
            var draft = new List<string>(scanTargets);
            using (var dialog = new Form {
                Text = "Danh sách tệp và thư mục cần quét",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(850, 515), MinimumSize = new Size(620, 390),
                BackColor = PageBackground, Font = new Font(Theme.UiFontFamily, 10f),
                AutoScaleMode = AutoScaleMode.Dpi
            })
            {
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14),
                    ColumnCount = 1, RowCount = 5, BackColor = PageBackground };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 53));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                dialog.Controls.Add(layout);
                var info = new Label { Dock = DockStyle.Fill, ForeColor = Ink,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Text = "Chọn nhiều tệp cùng lúc, thêm nhiều thư mục hoặc kết hợp cả hai.\n" +
                           "Những tệp trùng hoặc nằm trong thư mục đã chọn chỉ được quét một lần.",
                    Font = new Font(Theme.UiFontFamily, 9.4f) };
                layout.Controls.Add(info, 0, 0);
                var list = new ListBox { Dock = DockStyle.Fill,
                    SelectionMode = SelectionMode.MultiExtended,
                    HorizontalScrollbar = true, IntegralHeight = false,
                    Font = new Font(Theme.UiFontFamily, 10.1f),
                    BorderStyle = BorderStyle.FixedSingle };
                layout.Controls.Add(list, 0, 1);
                var count = new Label { Dock = DockStyle.Fill, ForeColor = Muted,
                    TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Theme.UiFontFamily, 9.3f) };
                layout.Controls.Add(count, 0, 2);
                Action repaint = () => {
                    list.BeginUpdate();
                    try { list.Items.Clear(); foreach (string target in draft) list.Items.Add(target); }
                    finally { list.EndUpdate(); }
                    count.Text = "Đã chọn " + draft.Count + " mục tiêu (" +
                        draft.Count(File.Exists) + " tệp, " + draft.Count(Directory.Exists) + " thư mục).";
                };
                var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
                for (int i = 0; i < 4; i++)
                    buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                var addFiles = Btn("Thêm tệp");
                var addFolder = Btn("Thêm thư mục");
                var remove = Btn("Xóa mục đã chọn");
                var removeAll = Btn("Xóa tất cả");
                buttons.Controls.Add(addFiles, 0, 0);
                buttons.Controls.Add(addFolder, 1, 0);
                buttons.Controls.Add(remove, 2, 0);
                buttons.Controls.Add(removeAll, 3, 0);
                layout.Controls.Add(buttons, 0, 3);
                addFiles.Click += (s, e) => {
                    try { if (PickFilesInto(draft, dialog)) repaint(); }
                    catch (ArgumentException ex) { MessageBox.Show(dialog, ex.Message); }
                };
                addFolder.Click += (s, e) => {
                    try { if (PickFolderInto(draft, dialog)) repaint(); }
                    catch (ArgumentException ex) { MessageBox.Show(dialog, ex.Message); }
                };
                remove.Click += (s, e) => {
                    foreach (string target in list.SelectedItems.Cast<string>().ToArray()) draft.Remove(target);
                    repaint();
                };
                removeAll.Click += (s, e) => { draft.Clear(); repaint(); };
                var footer = new FlowLayoutPanel { Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var accept = Btn("Áp dụng danh sách");
                var cancel = Btn("Hủy");
                accept.Dock = cancel.Dock = DockStyle.None;
                accept.Size = new Size(170, 40);
                cancel.Size = new Size(105, 40);
                accept.BackColor = Blue; accept.ForeColor = Color.White;
                accept.FlatAppearance.BorderColor = Blue;
                accept.Click += (s, e) => {
                    try
                    {
                        ScanScheduleService.EncodeTargets(draft); // giới hạn NVARCHAR(1000)
                        dialog.DialogResult = DialogResult.OK;
                        dialog.Close();
                    }
                    catch (ArgumentException ex) {
                        MessageBox.Show(dialog, ex.Message, "Mục tiêu quét", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };
                cancel.Click += (s, e) => { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); };
                footer.Controls.Add(accept);
                footer.Controls.Add(cancel);
                layout.Controls.Add(footer, 0, 4);
                repaint();
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    scanTargets.Clear(); scanTargets.AddRange(draft);
                    UpdateTargetSummary();
                }
            }
        }

        /// <summary>Thông báo tại chỗ; Save vẫn kiểm tra lại trong lớp dịch vụ.</summary>
        private void RefreshValidation()
        {
            if (IsDisposed || validationHint == null) return;
            errors.SetError(name, "");
            errors.SetError(path, "");
            errors.SetError(when, "");
            string problem = null;
            string newName = (name.Text ?? "").Trim();
            if (newName.Length < 3 || newName.Length > 150 || newName.Contains("\n") || newName.Contains("\r"))
            {
                problem = "Tên lịch cần từ 3 đến 150 ký tự, không được xuống dòng.";
                errors.SetError(name, problem);
            }
            else if (type.SelectedIndex == 2)
            {
                try
                {
                    if (scanTargets.Count == 0)
                        throw new ArgumentException("Chọn ít nhất một tệp hoặc thư mục cần quét.");
                    ScanScheduleService.EncodeTargets(scanTargets);
                    string unavailable = scanTargets.FirstOrDefault(p => !File.Exists(p) && !Directory.Exists(p));
                    if (unavailable != null)
                        throw new ArgumentException("Mục tiêu quét không còn tồn tại: " + unavailable);
                }
                catch (ArgumentException ex)
                {
                    problem = ex.Message;
                    errors.SetError(path, problem);
                }
            }
            if (problem == null && when.Value <= DateTime.Now.AddMinutes(2))
            {
                problem = "Thời gian bắt đầu phải muộn hơn hiện tại ít nhất 2 phút.";
                errors.SetError(when, problem);
            }
            else if (problem == null && when.Value > DateTime.Now.AddYears(5))
            {
                problem = "Chỉ đặt lịch trong vòng 5 năm tới.";
                errors.SetError(when, problem);
            }

            inputIsValid = problem == null;
            validationHint.ForeColor = inputIsValid ? Muted : Red;
            if (problem != null) { validationHint.Text = "⚠ " + problem; return; }
            if (frequency.SelectedIndex == 2 && when.Value.Day >= 29)
                validationHint.Text = "Lưu ý: lịch hằng tháng vào ngày " + when.Value.Day +
                    " sẽ bỏ qua những tháng không có ngày này.";
            else
                validationHint.Text = "Lần chạy đầu tiên: " + when.Value.ToString("dd/MM/yyyy HH:mm") +
                    "   •   " + (frequency.SelectedItem ?? "Hằng ngày");
        }

        private ScanSchedule BuildSchedule()
        {
            return new ScanSchedule {
                Id = selectedId,
                Name = (name.Text ?? "").Trim(),
                ScanType = new[] { "Quick", "Full", "Custom" }[Math.Max(0, type.SelectedIndex)],
                TargetPath = type.SelectedIndex == 2 ? ScanScheduleService.EncodeTargets(scanTargets) : null,
                Frequency = new[] { "Daily", "Weekly", "Monthly", "Once" }[Math.Max(0, frequency.SelectedIndex)],
                StartTime = when.Value
            };
        }

        private void RefreshData()
        {
            try
            {
                long keep = selectedId;
                changingSelection = true;
                DataTable data = ScanScheduleService.List();
                table.DataSource = data;
                ConfigureColumns();
                countLabel.Text = "Các lịch đã tạo (" + data.Rows.Count.ToString("N0") + ")";
                table.ClearSelection();
                table.CurrentCell = null;
                bool found = false;
                if (keep > 0 && table.Columns.Contains("ScheduleID"))
                {
                    foreach (DataGridViewRow row in table.Rows)
                    {
                        if (Convert.ToInt64(row.Cells["ScheduleID"].Value) != keep) continue;
                        row.Selected = true;
                        table.CurrentCell = row.Cells["Name"];
                        found = true;
                        break;
                    }
                }
                if (keep > 0 && !found) selectedId = 0;
                UpdateGridTooltips();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không tải được danh sách lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { changingSelection = false; UpdateSelectionButtons(); }
        }

        private void ConfigureColumns()
        {
            string[] keys = { "ScheduleID", "Name", "ScanType", "TargetPath", "Frequency", "StartTime", "LastRunAt", "LastOutcome", "IsEnabled" };
            string[] titles = { "Mã lịch", "Tên lịch", "Kiểu quét", "Tệp / thư mục", "Tần suất", "Bắt đầu", "Lần chạy gần nhất", "Kết quả", "Đang bật" };
            float[] weights = { 1f, 120f, 78f, 175f, 80f, 112f, 112f, 200f, 62f };
            for (int i = 0; i < keys.Length; i++)
            {
                if (!table.Columns.Contains(keys[i])) continue;
                DataGridViewColumn column = table.Columns[keys[i]];
                column.HeaderText = titles[i];
                column.FillWeight = weights[i];
                column.MinimumWidth = keys[i] == "IsEnabled" ? 70 : keys[i] == "LastOutcome" ? 125 : 84;
                column.SortMode = DataGridViewColumnSortMode.Automatic;
            }
            if (table.Columns.Contains("ScheduleID")) table.Columns["ScheduleID"].Visible = false;
            if (table.Columns.Contains("StartTime")) table.Columns["StartTime"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            if (table.Columns.Contains("LastRunAt")) table.Columns["LastRunAt"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            if (table.Columns.Contains("IsEnabled"))
                table.Columns["IsEnabled"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void TableCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string key = table.Columns[e.ColumnIndex].Name;
            if (key == "ScanType")
            {
                string value = Convert.ToString(e.Value);
                e.Value = value == "Quick" ? "Quét nhanh" : value == "Full" ? "Toàn bộ" : "Tùy chọn";
                e.FormattingApplied = true;
            }
            else if (key == "Frequency")
            {
                string value = Convert.ToString(e.Value);
                e.Value = value == "Daily" ? "Hằng ngày" : value == "Weekly" ? "Hằng tuần" :
                    value == "Monthly" ? "Hằng tháng" : "Một lần";
                e.FormattingApplied = true;
            }
            else if (key == "TargetPath" && e.Value != null && e.Value != DBNull.Value)
            {
                e.Value = ScanScheduleService.DescribeTargets(Convert.ToString(e.Value));
                e.FormattingApplied = true;
            }
            else if (key == "LastRunAt" && (e.Value == null || e.Value == DBNull.Value))
            {
                e.Value = "Chưa chạy";
                e.FormattingApplied = true;
            }
            else if ((key == "LastOutcome" || key == "TargetPath") &&
                     (e.Value == null || e.Value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(e.Value))))
            {
                e.Value = "—";
                e.FormattingApplied = true;
            }
        }

        private void UpdateGridTooltips()
        {
            if (table.DataSource == null) return;
            foreach (DataGridViewRow row in table.Rows)
            {
                foreach (string key in new[] { "TargetPath", "LastOutcome", "Name" })
                {
                    if (!table.Columns.Contains(key)) continue;
                    var cell = row.Cells[key];
                    cell.ToolTipText = cell.Value == DBNull.Value ? "" :
                        key == "TargetPath" ? string.Join(Environment.NewLine,
                            ScanScheduleService.DecodeTargets(Convert.ToString(cell.Value))) : Convert.ToString(cell.Value);
                }
            }
        }

        private void CopySelected()
        {
            if (changingSelection || table.SelectedRows.Count == 0 || table.CurrentRow == null ||
                !table.Columns.Contains("ScheduleID") || table.CurrentRow.Cells["ScheduleID"].Value == null) return;
            try
            {
                changingSelection = true;
                DataGridViewRow row = table.CurrentRow;
                selectedId = Convert.ToInt64(row.Cells["ScheduleID"].Value);
                name.Text = Convert.ToString(row.Cells["Name"].Value);
                type.SelectedIndex = Math.Max(0, Array.IndexOf(new[] { "Quick", "Full", "Custom" }, Convert.ToString(row.Cells["ScanType"].Value)));
                scanTargets.Clear();
                scanTargets.AddRange(ScanScheduleService.DecodeTargets(row.Cells["TargetPath"].Value == DBNull.Value
                    ? null : Convert.ToString(row.Cells["TargetPath"].Value)));
                UpdateTargetSummary();
                frequency.SelectedIndex = Math.Max(0, Array.IndexOf(new[] { "Daily", "Weekly", "Monthly", "Once" }, Convert.ToString(row.Cells["Frequency"].Value)));
                DateTime start = Convert.ToDateTime(row.Cells["StartTime"].Value);
                when.Value = start < when.MinDate ? when.MinDate : start > when.MaxDate ? when.MaxDate : start;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không đọc được thông tin lịch: " + ex.Message, "Lịch quét",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { changingSelection = false; UpdateSelectionButtons(); RefreshValidation(); }
        }

        private void UpdateSelectionButtons()
        {
            bool selected = selectedId > 0 && table.SelectedRows.Count > 0;
            toggle.Enabled = run.Enabled = delete.Enabled = selected;
            if (selected && table.CurrentRow != null && table.Columns.Contains("IsEnabled"))
            {
                bool enabled = table.CurrentRow.Cells["IsEnabled"].Value != DBNull.Value &&
                    Convert.ToBoolean(table.CurrentRow.Cells["IsEnabled"].Value);
                toggle.Text = enabled ? "Tắt lịch" : "Bật lịch";
                run.Enabled = enabled;
            }
            else toggle.Text = "Bật / tắt lịch";
        }

        private void Reset()
        {
            changingSelection = true;
            try
            {
                selectedId = 0;
                string proposed = "Quét tự động";
                if (table.Columns.Contains("Name"))
                {
                    int suffix = 2;
                    while (table.Rows.Cast<DataGridViewRow>().Any(row =>
                        string.Equals(Convert.ToString(row.Cells["Name"].Value), proposed, StringComparison.OrdinalIgnoreCase)))
                        proposed = "Quét tự động " + suffix++;
                }
                name.Text = proposed;
                scanTargets.Clear();
                type.SelectedIndex = 0;
                UpdateTargetSummary();
                frequency.SelectedIndex = 0;
                when.Value = DefaultStartTime();
                table.ClearSelection();
                table.CurrentCell = null;
            }
            finally { changingSelection = false; UpdateSelectionButtons(); RefreshValidation(); }
            name.Focus();
        }

        private void Save()
        {
            ScanSchedule schedule = null;
            try
            {
                // Kiểm tra tại form để hướng dẫn người dùng; dịch vụ luôn kiểm tra lại
                // một lần khi lưu, kể cả thao tác không đi qua giao diện này.
                RefreshValidation();
                if (!inputIsValid)
                    throw new ArgumentException(validationHint.Text.TrimStart('⚠', ' '));
                schedule = BuildSchedule();
                if (schedule.Frequency == "Monthly" && schedule.StartTime.Day >= 29 &&
                    MessageBox.Show(this,
                        "Lịch vào ngày " + schedule.StartTime.Day + " sẽ không chạy trong các tháng thiếu ngày này.\nTiếp tục lưu?",
                        "Xác nhận lịch hằng tháng", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                selectedId = ScanScheduleService.Save(schedule);
                RefreshData();
                MessageBox.Show(this, "Lịch đã lưu và đăng ký thành công với Windows Task Scheduler.\n" +
                    "Lần bắt đầu: " + schedule.StartTime.ToString("dd/MM/yyyy HH:mm"),
                    "Lưu lịch thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ArgumentException ex)
            {
                RefreshValidation();
                MessageBox.Show(this, ex.Message, "Kiểm tra thông tin lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                // Dịch vụ có thể đã ghi lịch ở trạng thái Tắt trước khi Task Scheduler trả lỗi.
                if (schedule != null && schedule.Id > 0) selectedId = schedule.Id;
                RefreshData();
                MessageBox.Show(this, ex.Message + "\n\nNếu lịch có trong danh sách nhưng đang Tắt, " +
                    "hãy chọn lịch đó và thử Bật lịch lại sau khi khắc phục lỗi Windows Task Scheduler.",
                    "Không đăng ký được lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Toggle()
        {
            if (selectedId <= 0) return;
            try
            {
                ScanSchedule schedule = ScanScheduleService.Get(selectedId);
                if (!schedule.Enabled && schedule.Frequency == "Once" && schedule.StartTime <= DateTime.Now)
                    throw new ArgumentException("Lịch chạy một lần đã qua thời điểm bắt đầu. Hãy sửa sang thời điểm trong tương lai.");
                ScanScheduleService.SetEnabled(selectedId, !schedule.Enabled);
                RefreshData();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thể bật / tắt lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void RunNow()
        {
            if (selectedId <= 0) return;
            try
            {
                ScanScheduleService.RunNow(selectedId);
                MessageBox.Show(this, "Đã yêu cầu Windows chạy lịch quét. Kết quả sẽ có trong Lịch sử.",
                    "Chạy ngay", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thể chạy lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void Delete()
        {
            if (selectedId <= 0) return;
            if (MessageBox.Show(this, "Xóa lịch đang chọn khỏi Windows Task Scheduler và cơ sở dữ liệu?",
                "Xác nhận xóa lịch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                ScanScheduleService.Remove(selectedId);
                selectedId = 0;
                RefreshData();
                Reset();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thể xóa lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}
