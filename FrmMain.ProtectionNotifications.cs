using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus
{
    public partial class FrmMain
    {
        private readonly Queue<ProtectionNotice> protectionNoticeQueue = new Queue<ProtectionNotice>();
        private Panel protectionNoticePanel;
        private Panel protectionNoticeAccent;
        private Label protectionNoticeHeading;
        private Label protectionNoticeBody;
        private System.Windows.Forms.Timer protectionNoticeTimer;
        private NotifyIcon protectionTrayIcon;
        private DateTime protectionNoticeExpiresUtc = DateTime.MinValue;
        private DateTime protectionNoticeDisplayedUtc = DateTime.MinValue;

        private void InitializeProtectionNotifications()
        {
            if (protectionNoticeTimer != null) return;
            protectionNoticePanel = new Panel {
                Name = "ProtectionNotificationToast", Size = new Size(414, 105),
                BackColor = Color.White, Visible = false
            };
            protectionNoticePanel.Paint += (sender, e) => {
                using (var pen = new Pen(Color.FromArgb(205, 220, 239)))
                    e.Graphics.DrawRectangle(pen, 0, 0, protectionNoticePanel.Width - 1, protectionNoticePanel.Height - 1);
            };
            protectionNoticeAccent = new Panel { Location = new Point(1, 1), Size = new Size(5, 103),
                BackColor = Color.FromArgb(31, 112, 234) };
            protectionNoticeHeading = new Label { Bounds = new Rectangle(22, 11, 341, 25),
                Font = new Font(Theme.UiFontFamily, 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(18, 42, 76), AutoEllipsis = true };
            protectionNoticeBody = new Label { Bounds = new Rectangle(22, 40, 370, 53),
                Font = new Font(Theme.UiFontFamily, 9.7f),
                ForeColor = Color.FromArgb(66, 88, 123), AutoEllipsis = true };
            var close = new Button { Text = "×", Location = new Point(374, 8), Size = new Size(29, 28),
                FlatStyle = FlatStyle.Flat, BackColor = Color.White,
                ForeColor = Color.FromArgb(99, 120, 151), Cursor = Cursors.Hand,
                Font = new Font(Theme.UiFontFamily, 13f), TabStop = false };
            close.FlatAppearance.BorderSize = 0;
            close.Click += (sender, e) =>
            {
                protectionNoticePanel.Visible = false;
                protectionNoticeExpiresUtc = DateTime.MinValue;
                PlaceScheduledToast();
            };
            protectionNoticePanel.Controls.AddRange(new System.Windows.Forms.Control[] {
                protectionNoticeAccent, protectionNoticeHeading, protectionNoticeBody, close
            });
            Controls.Add(protectionNoticePanel);
            protectionNoticeTimer = new System.Windows.Forms.Timer { Interval = 500 };
            protectionNoticeTimer.Tick += (sender, e) => DisplayNextProtectionNotification();
            protectionNoticeTimer.Start();
            protectionTrayIcon = new NotifyIcon { Icon = Icon ?? SystemIcons.Information,
                Text = "Công cụ quét và diệt virus", Visible = true };
            ProtectionNotificationService.Received += ReceiveProtectionNotification;
            Shown += (sender, e) =>
            {
                int active = (RealTimeProtection.IsRunning ? 1 : 0);
                foreach (string key in new[] { "file", "usb", "download", "behavior", "startup" })
                    if (GuardService.IsGuardRunning(key)) active++;
                ProtectionNotificationService.Publish("Trạng thái bảo vệ",
                    "Đang hoạt động " + active + "/6 mô-đun bảo vệ. " +
                    (active < 6 ? "Kiểm tra trang Bảo vệ để biết các mô-đun chưa hoạt động." :
                        "Các bộ giám sát đã khởi động; không thay thế phần mềm chống mã độc cấp hệ thống."),
                    active < 6 ? ProtectionNoticeLevel.Warning : ProtectionNoticeLevel.Info,
                    "protection-startup", 10);
            };
            RealTimeProtection.ThreatDetected += NotifyRealtimeThreat;
            GuardService.ThreatDetected += NotifyGuardThreat;
            Resize += (sender, e) => PlaceProtectionNotification();
            FormClosed += (sender, e) => StopProtectionNotifications();
        }

        // Tất cả thông báo đặt trong góc trên bên phải vùng làm việc (bên dưới thanh tiêu đề).
        // Nếu đang hiện thông báo quét theo lịch, đặt khung đó ngay dưới thông báo bảo vệ.
        private void PlaceProtectionNotification()
        {
            if (protectionNoticePanel == null || protectionNoticePanel.IsDisposed) return;
            int width = Math.Max(180, Math.Min(414, ClientSize.Width - 24));
            protectionNoticePanel.Width = width;
            protectionNoticeAccent.Height = protectionNoticePanel.Height - 2;
            protectionNoticeHeading.Width = Math.Max(60, width - 76);
            protectionNoticeBody.Width = Math.Max(60, width - 44);
            var close = protectionNoticePanel.Controls.OfType<Button>().FirstOrDefault();
            if (close != null) close.Left = width - close.Width - 11;
            int x = Math.Max(8, ClientSize.Width - width - 16);
            protectionNoticePanel.Location = new Point(x, 12);
            if (protectionNoticePanel.Visible) protectionNoticePanel.BringToFront();
            PlaceScheduledToast();
        }

        private void ReceiveProtectionNotification(ProtectionNotice notice)
        {
            if (IsDisposed || Disposing) return;
            try
            {
                if (!IsHandleCreated) return;
                if (InvokeRequired)
                    BeginInvoke((MethodInvoker)(() => ReceiveProtectionNotification(notice)));
                else
                {
                    // Giới hạn hàng đợi để không dồn thông báo từ hàng trăm tệp.
                    if (protectionNoticeQueue.Count >= 12) protectionNoticeQueue.Dequeue();
                    protectionNoticeQueue.Enqueue(notice);
                    DisplayNextProtectionNotification();
                }
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void DisplayNextProtectionNotification()
        {
            if (protectionNoticePanel == null || protectionNoticePanel.IsDisposed) return;
            if (DateTime.UtcNow < protectionNoticeExpiresUtc) return;
            if (protectionNoticeQueue.Count == 0)
            {
                if (protectionNoticePanel.Visible)
                {
                    protectionNoticePanel.Visible = false;
                    PlaceScheduledToast();
                }
                return;
            }
            var item = protectionNoticeQueue.Dequeue();
            protectionNoticeHeading.Text = item.Title;
            protectionNoticeBody.Text = item.Message;
            protectionNoticeAccent.BackColor = item.Level == ProtectionNoticeLevel.Error ? Color.FromArgb(220, 59, 67) :
                item.Level == ProtectionNoticeLevel.Warning ? Color.FromArgb(225, 142, 39) :
                item.Level == ProtectionNoticeLevel.Success ? Color.FromArgb(24, 163, 102) : Color.FromArgb(26, 114, 236);
            protectionNoticeDisplayedUtc = DateTime.UtcNow;
            protectionNoticeExpiresUtc = protectionNoticeDisplayedUtc.AddSeconds(
                item.Level == ProtectionNoticeLevel.Warning || item.Level == ProtectionNoticeLevel.Error ? 9 : 6);
            if (WindowState == FormWindowState.Minimized || !Visible)
            {
                protectionNoticePanel.Visible = false;
                PlaceScheduledToast();
                try
                {
                    if (protectionTrayIcon != null)
                        protectionTrayIcon.ShowBalloonTip(6000, item.Title, item.Message.Length > 230 ?
                            item.Message.Substring(0, 227) + "..." : item.Message,
                            item.Level == ProtectionNoticeLevel.Error ? ToolTipIcon.Error :
                            item.Level == ProtectionNoticeLevel.Warning ? ToolTipIcon.Warning : ToolTipIcon.Info);
                }
                catch (Exception) { }
            }
            else
            {
                protectionNoticePanel.Visible = true;
                PlaceProtectionNotification();
            }
        }

        private void NotifyGuardThreat(ThreatFound threat) { ShowDetectedThreat(threat, "Bộ bảo vệ"); }
        private void NotifyRealtimeThreat(ThreatFound threat) { ShowDetectedThreat(threat, "Bảo vệ thời gian thực"); }
        private void ShowDetectedThreat(ThreatFound threat, string source)
        {
            if (threat == null) return;
            string name = "Tệp không rõ";
            try { name = Path.GetFileName(threat.FilePath ?? "") ?? "Tệp không rõ"; }
            catch (ArgumentException) { }
            bool heuristic = ScanEngine.IsHeuristicOnly(threat);
            ProtectionNotificationService.Publish(heuristic ? "Phát hiện hành vi nghi vấn" : "Phát hiện mối đe dọa",
                source + ": " + name + ". " + (heuristic ? "Chưa tự cách ly; cần kiểm tra." :
                    "Kiểm tra trang Mối đe dọa / Cách ly để xem trạng thái xử lý."),
                ProtectionNoticeLevel.Warning, "threat:" + (threat.FilePath ?? name), 25);
        }

        private void StopProtectionNotifications()
        {
            ProtectionNotificationService.Received -= ReceiveProtectionNotification;
            RealTimeProtection.ThreatDetected -= NotifyRealtimeThreat;
            GuardService.ThreatDetected -= NotifyGuardThreat;
            if (protectionNoticeTimer != null) { protectionNoticeTimer.Stop(); protectionNoticeTimer.Dispose(); protectionNoticeTimer = null; }
            if (protectionTrayIcon != null) { protectionTrayIcon.Visible = false; protectionTrayIcon.Dispose(); protectionTrayIcon = null; }
        }
    }
}
