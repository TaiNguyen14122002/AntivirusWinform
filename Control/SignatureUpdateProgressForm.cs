using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// Hiển thị tiến trình thật: MalwareBazaar API → kiểm tra trùng → SQL → reload cache.
    /// Không tải hay thực thi mẫu mã độc, chỉ nhận metadata và chữ ký băm.
    /// </summary>
    internal sealed class SignatureUpdateProgressForm : Form
    {
        private static readonly Color Blue = Color.FromArgb(20, 102, 235);
        private static readonly Color Navy = Color.FromArgb(23, 40, 68);
        private static readonly Color Muted = Color.FromArgb(105, 124, 154);
        private static readonly Color Pale = Color.FromArgb(239, 246, 255);
        private static readonly Color Green = Color.FromArgb(29, 170, 93);

        private static readonly string[] Tasks =
        {
            "Kiểm tra kết nối SQL Server...",
            "Tải bản xuất TOÀN BỘ hash từ MalwareBazaar...",
            "Chuẩn hóa metadata và SHA-256/MD5/SHA-1...",
            "Đối chiếu trùng và thêm chữ ký mới vào SQL...",
            "Tải lại cơ sở dữ liệu chữ ký dùng để quét...",
            "Hoàn tất cập nhật chữ ký."
        };

        private readonly Label[] markers = new Label[Tasks.Length];
        private readonly Label[] clockLabels = new Label[Tasks.Length];
        private readonly Label[] taskLabels = new Label[Tasks.Length];
        private readonly Label titleLabel;
        private readonly Label subtitleLabel;
        private readonly Label detailLabel;
        private readonly Label percentageLabel;
        private readonly Panel progressTrack;
        private readonly Panel progressFill;
        private readonly Button cancelButton;
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();
        private readonly string authKey;
        private bool finished;
        private int runningStep = -1;

        private sealed class StepUpdate
        {
            internal int Step;
            internal int Percent;
            internal bool Completed;
            internal string Detail;
        }

        internal static void Open(IWin32Window owner)
        {
            IWin32Window actualOwner = owner ?? Form.ActiveForm;
            string key = MalwareBazaarSignatureUpdater.GetStoredAuthKey();
            if (string.IsNullOrWhiteSpace(key))
            {
                key = AppDialog.Prompt(actualOwner, "Kết nối MalwareBazaar",
                    "Nhập Auth-Key MalwareBazaar (chỉ cần nhập lần đầu; khóa sẽ được mã hóa cho tài khoản Windows):", "", true);
                if (key == null) return;
                if (string.IsNullOrWhiteSpace(key))
                {
                    AppDialog.Show(actualOwner, "Chưa nhập Auth-Key MalwareBazaar.", "Cập nhật chữ ký");
                    return;
                }
                try { MalwareBazaarSignatureUpdater.SaveAuthKey(key); }
                catch (Exception ex)
                {
                    AppDialog.Show(actualOwner, "Không lưu được Auth-Key: " + ex.Message, "Cập nhật chữ ký");
                    return;
                }
            }
            string fullExportUrl = MalwareBazaarSignatureUpdater.GetStoredFullExportUrl();
            if (string.IsNullOrWhiteSpace(fullExportUrl))
            {
                fullExportUrl = AppDialog.Prompt(actualOwner, "Full Export MalwareBazaar",
                    "Đăng nhập https://bazaar.abuse.ch/export/ và sao chép liên kết FULL CSV HASH EXPORT (không chọn ZIP mẫu mã độc).\nDán liên kết xuất dữ liệu ở đây:", "", true);
                if (fullExportUrl == null) return;
                try { MalwareBazaarSignatureUpdater.SaveFullExportUrl(fullExportUrl); }
                catch (Exception ex)
                {
                    AppDialog.Show(actualOwner, "Liên kết export không hợp lệ: " + ex.Message, "Cập nhật chữ ký");
                    return;
                }
            }
            using (var dialog = new SignatureUpdateProgressForm(key))
            {
                if (actualOwner == null) dialog.ShowDialog();
                else dialog.ShowDialog(actualOwner);
            }
        }

        private SignatureUpdateProgressForm(string key)
        {
            authKey = key;
            Text = "Cập nhật cơ sở dữ liệu chữ ký";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(245, 248, 253);
            ShowInTaskbar = false;
            MinimizeBox = MaximizeBox = false;
            ClientSize = new Size(720, 475);
            MinimumSize = new Size(590, 475);
            Font = new Font(Theme.UiFontFamily, 10.65F);
            KeyPreview = true;

            var header = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White };
            var emblem = new Label
            {
                Text = "⟳", Location = new Point(26, 24), Size = new Size(56, 56),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Symbol", 27F, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 165, 91),
                BackColor = Color.FromArgb(222, 248, 234)
            };
            titleLabel = new Label
            {
                Text = "Đang cập nhật cơ sở dữ liệu chữ ký", Location = new Point(102, 21),
                Size = new Size(525, 32), Font = new Font(Theme.UiFontFamily, 17.25F, FontStyle.Bold),
                ForeColor = Navy, AutoEllipsis = true
            };
            subtitleLabel = new Label
            {
                Text = "Đang đồng bộ toàn bộ chữ ký từ Full CSV Export vào SQL Server...",
                Location = new Point(102, 61), Size = new Size(555, 27),
                Font = new Font(Theme.UiFontFamily, 11.15F), ForeColor = Muted, AutoEllipsis = true
            };
            var close = new Button
            {
                Text = "×", Font = new Font(Theme.UiFontFamily, 20.25F),
                FlatStyle = FlatStyle.Flat, Location = new Point(662, 14),
                Size = new Size(38, 38), ForeColor = Muted,
                BackColor = Color.White, TabStop = false, Cursor = Cursors.Hand
            };
            close.FlatAppearance.BorderSize = 0;
            close.Click += delegate { Close(); };
            header.Controls.Add(emblem);
            header.Controls.Add(titleLabel);
            header.Controls.Add(subtitleLabel);
            header.Controls.Add(close);
            header.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(216, 228, 241) });

            var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 250, 254) };
            progressTrack = new Panel { BackColor = Color.FromArgb(220, 231, 248), Location = new Point(27, 15), Height = 16, Width = 590 };
            progressFill = new Panel { BackColor = Blue, Location = new Point(0, 0), Size = new Size(0, 16) };
            progressTrack.Controls.Add(progressFill);
            percentageLabel = new Label
            {
                Text = "0%", Location = new Point(628, 8), Size = new Size(65, 30),
                Font = new Font(Theme.UiFontFamily, 12.55F, FontStyle.Bold), ForeColor = Blue,
                TextAlign = ContentAlignment.MiddleRight
            };

            var tasksPanel = new Panel
            {
                Location = new Point(27, 53), Size = new Size(666, 207),
                BackColor = Pale
            };
            for (int i = 0; i < Tasks.Length; i++)
            {
                int y = 10 + i * 32;
                markers[i] = new Label
                {
                    Location = new Point(14, y + 2), Size = new Size(25, 25),
                    Text = "○", TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI Symbol", 15F), ForeColor = Muted
                };
                taskLabels[i] = new Label
                {
                    Text = Tasks[i], Location = new Point(48, y + 3), Size = new Size(480, 26),
                    Font = new Font(Theme.UiFontFamily, 10.85F), ForeColor = Muted,
                    TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
                };
                clockLabels[i] = new Label
                {
                    Text = "--:--:--", Location = new Point(537, y + 3), Size = new Size(110, 25),
                    ForeColor = Muted, Font = new Font(Theme.UiFontFamily, 10.15F),
                    TextAlign = ContentAlignment.MiddleRight
                };
                tasksPanel.Controls.Add(markers[i]);
                tasksPanel.Controls.Add(taskLabels[i]);
                tasksPanel.Controls.Add(clockLabels[i]);
            }
            detailLabel = new Label
            {
                Text = "Chỉ tải thông tin mã băm và metadata; không tải tệp mã độc.",
                Location = new Point(27, 262), Size = new Size(666, 43),
                Font = new Font(Theme.UiFontFamily, 10.15F), ForeColor = Muted,
                AutoEllipsis = false
            };
            content.Controls.Add(progressTrack);
            content.Controls.Add(percentageLabel);
            content.Controls.Add(tasksPanel);
            content.Controls.Add(detailLabel);

            var footer = new Panel
            {
                Dock = DockStyle.Bottom, Height = 70,
                BackColor = Color.FromArgb(237, 244, 253)
            };
            cancelButton = new Button
            {
                Text = "Hủy", Size = new Size(158, 44),
                Location = new Point(534, 13), Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Navy,
                Cursor = Cursors.Hand, Font = new Font(Theme.UiFontFamily, 11.15F, FontStyle.Bold)
            };
            cancelButton.FlatAppearance.BorderColor = Color.FromArgb(193, 211, 234);
            cancelButton.Click += delegate { if (finished) Close(); else { cancel.Cancel(); cancelButton.Enabled = false; detailLabel.Text = "Đang hủy sau bước xử lý hiện tại..."; } };
            footer.Controls.Add(cancelButton);

            Resize += delegate { RoundCorners(this, 18); };
            tasksPanel.Resize += delegate { RoundCorners(tasksPanel, 12); };
            emblem.Resize += delegate { RoundCorners(emblem, 13); };
            progressTrack.Resize += delegate { RoundCorners(progressTrack, 8); };
            progressFill.Resize += delegate { RoundCorners(progressFill, 8); };
            RoundCorners(this, 18);
            RoundCorners(tasksPanel, 12);
            RoundCorners(emblem, 13);
            RoundCorners(progressTrack, 8);

            Controls.Add(content);
            Controls.Add(footer);
            Controls.Add(header);
            header.Resize += delegate
            {
                close.Left = header.ClientSize.Width - close.Width - 16;
                titleLabel.Width = Math.Max(220, close.Left - 112);
                subtitleLabel.Width = Math.Max(230, header.ClientSize.Width - 130);
            };
            content.Resize += delegate
            {
                int w = content.ClientSize.Width;
                percentageLabel.Left = w - 92;
                progressTrack.Width = Math.Max(100, percentageLabel.Left - 39);
                tasksPanel.Width = Math.Max(300, w - 54);
                detailLabel.Width = tasksPanel.Width;
                for (int i = 0; i < Tasks.Length; i++)
                {
                    clockLabels[i].Left = tasksPanel.Width - 126;
                    taskLabels[i].Width = Math.Max(200, clockLabels[i].Left - 54);
                }
                SetBarWidth();
            };
            Shown += async delegate { await RunOnOpenAsync(); };
            FormClosing += delegate { cancel.Cancel(); };
        }

        private static void RoundCorners(System.Windows.Forms.Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            int d = Math.Min(radius * 2, Math.Min(control.Width, control.Height));
            if (d <= 1) return;
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, d, d, 180, 90);
                path.AddArc(control.Width - d, 0, d, d, 270, 90);
                path.AddArc(control.Width - d, control.Height - d, d, d, 0, 90);
                path.AddArc(0, control.Height - d, d, d, 90, 90);
                path.CloseFigure();
                Region old = control.Region;
                control.Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        private void SetBarWidth()
        {
            int percent;
            if (!int.TryParse(percentageLabel.Text.TrimEnd('%'), out percent)) percent = 0;
            progressFill.Width = Math.Max(0, progressTrack.ClientSize.Width * percent / 100);
        }

        private void RenderStep(StepUpdate update)
        {
            if (IsDisposed || Disposing) return;
            runningStep = update.Step;
            int step = update.Step;
            if (step >= 0 && step < Tasks.Length)
            {
                markers[step].Text = update.Completed ? "✓" : "◉";
                markers[step].ForeColor = update.Completed ? Green : Blue;
                taskLabels[step].ForeColor = update.Completed ? Navy : Blue;
                if (update.Completed) clockLabels[step].Text = DateTime.Now.ToString("HH:mm:ss");
            }
            percentageLabel.Text = update.Percent + "%";
            SetBarWidth();
            if (!string.IsNullOrWhiteSpace(update.Detail)) detailLabel.Text = update.Detail;
        }

        private async Task RunOnOpenAsync()
        {
            var reported = new Progress<MalwareBazaarSignatureUpdater.ProgressInfo>(update =>
            {
                RenderStep(new StepUpdate
                {
                    Step = update.Stage,
                    Percent = update.Percent,
                    Completed = update.Done,
                    Detail = update.Message
                });
            });
            try
            {
                // Không khóa UI khi truy vấn HTTP hoặc SQL Server.
                MalwareBazaarSignatureUpdater.UpdateResult result = await Task.Run(
                    () => MalwareBazaarSignatureUpdater.UpdateAsync(authKey, cancel.Token, reported), cancel.Token);
                if (IsDisposed || Disposing) return;
                finished = true;
                titleLabel.Text = "Hoàn tất cập nhật chữ ký virus";
                subtitleLabel.Text = "Đã nhập toàn bộ chữ ký khả dụng trong Full CSV Export vào SQL Server.";
                detailLabel.Text = "Nhận: " + result.Received.ToString("N0") + " | Thêm mới: " + result.Inserted.ToString("N0") +
                    " | Đã tồn tại: " + result.Existing.ToString("N0") + " | Không hợp lệ: " + result.Invalid.ToString("N0") +
                    " | Hash hiện có: " + result.Active.ToString("N0") + ".";
                cancelButton.Text = "Đóng";
                cancelButton.Enabled = true;
            }
            catch (OperationCanceledException)
            {
                if (IsDisposed || Disposing) return;
                finished = true;
                titleLabel.Text = "Đã hủy cập nhật chữ ký";
                detailLabel.Text = "Đã hủy trước khi hoàn tất. Các thay đổi SQL đã commit (nếu có) sẽ không bị xóa.";
                cancelButton.Text = "Đóng";
                cancelButton.Enabled = true;
            }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                finished = true;
                titleLabel.Text = "Không thể cập nhật chữ ký virus";
                subtitleLabel.Text = "Kiểm tra kết nối Internet, Auth-Key MalwareBazaar và SQL Server.";
                if (runningStep >= 0 && runningStep < markers.Length)
                {
                    markers[runningStep].Text = "!";
                    markers[runningStep].ForeColor = Color.Firebrick;
                    taskLabels[runningStep].ForeColor = Color.Firebrick;
                }
                if (ex is UnauthorizedAccessException)
                {
                    detailLabel.Text = "MalwareBazaar từ chối quyền tải export. Kiểm tra Auth-Key và liên kết Full CSV Export trong tài khoản abuse.ch.";
                }
                else detailLabel.Text = "Lỗi: " + ex.Message;
                cancelButton.Text = "Đóng";
                cancelButton.Enabled = true;
            }
        }
    }
}
