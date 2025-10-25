using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus
{
    public partial class FrmMain
    {
        private System.Windows.Forms.Timer scheduleNoticeTimer;
        private Panel scheduleNoticePanel;
        private Panel scheduleNoticeAccent;
        private Label scheduleNoticeTitle;
        private Label scheduleNoticeMessage;
        private readonly Queue<ScheduledScanNotice> scheduleNoticeQueue = new Queue<ScheduledScanNotice>();
        private DateTime scheduleNoticeExpiryUtc;
        private DateTime scheduleNoticeShownUtc;
        private string scheduleNoticeShowingKind;
        private DateTime scheduleNoticeListeningSinceUtc;

        private void InitializeScheduleNotifications()
        {
            if (scheduleNoticeTimer != null) return;
            // Nhận thông báo phát ngay trước khi UI khởi tạo nhưng không mở lại
            // các thông báo của những phiên quét cũ.
            scheduleNoticeListeningSinceUtc = DateTime.UtcNow.AddSeconds(-15);
            scheduleNoticePanel = new Panel
            {
                Name = "ScheduledScanToast", Size = new Size(405, 108),
                BackColor = Color.White, Visible = false
            };
            scheduleNoticePanel.Paint += (sender, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(211, 225, 242)))
                    e.Graphics.DrawRectangle(pen, 0, 0,
                        scheduleNoticePanel.Width - 1, scheduleNoticePanel.Height - 1);
            };
            scheduleNoticeAccent = new Panel
            {
                Bounds = new Rectangle(0, 1, 5, 106),
                BackColor = Color.FromArgb(27, 115, 236)
            };
            scheduleNoticeTitle = new Label
            {
                Bounds = new Rectangle(20, 13, 335, 25),
                Font = new Font(global::ScanAndRemoveVirus.Control.Theme.UiFontFamily, 11.55F, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 42, 75), AutoEllipsis = true,
                Text = "Lịch quét tự động"
            };
            scheduleNoticeMessage = new Label
            {
                Bounds = new Rectangle(20, 43, 366, 53),
                Font = new Font(global::ScanAndRemoveVirus.Control.Theme.UiFontFamily, 9.65F, FontStyle.Regular),
                ForeColor = Color.FromArgb(73, 92, 119), AutoEllipsis = true,
                Text = ""
            };
            var close = new Button
            {
                Text = "×", FlatStyle = FlatStyle.Flat,
                Bounds = new Rectangle(366, 9, 27, 27),
                ForeColor = Color.FromArgb(92, 107, 132),
                Font = new Font(global::ScanAndRemoveVirus.Control.Theme.UiFontFamily, 13.4F),
                BackColor = Color.White, Cursor = Cursors.Hand,
                TabStop = false
            };
            close.FlatAppearance.BorderSize = 0;
            close.Click += (sender, e) => DismissScheduledToast();
            scheduleNoticePanel.Controls.AddRange(new System.Windows.Forms.Control[] {
                scheduleNoticeAccent, scheduleNoticeTitle, scheduleNoticeMessage, close
            });
            Controls.Add(scheduleNoticePanel);
            PlaceScheduledToast();
            Resize += (sender, e) => PlaceScheduledToast();
            scheduleNoticeTimer = new System.Windows.Forms.Timer { Interval = 900 };
            scheduleNoticeTimer.Tick += ScheduledNotificationTick;
            scheduleNoticeTimer.Start();
            FormClosed += (sender, e) =>
            {
                scheduleNoticeTimer.Stop();
                scheduleNoticeTimer.Dispose();
                scheduleNoticeTimer = null;
            };
        }

        private void PlaceScheduledToast()
        {
            if (scheduleNoticePanel == null || scheduleNoticePanel.IsDisposed) return;
            int width = Math.Max(180, Math.Min(405, ClientSize.Width - 24));
            scheduleNoticePanel.Width = width;
            scheduleNoticeTitle.Width = Math.Max(60, width - 72);
            scheduleNoticeMessage.Width = Math.Max(60, width - 39);
            var close = scheduleNoticePanel.Controls.OfType<Button>().FirstOrDefault();
            if (close != null) close.Left = width - close.Width - 12;
            int y = protectionNoticePanel != null && !protectionNoticePanel.IsDisposed &&
                protectionNoticePanel.Visible ? protectionNoticePanel.Bottom + 10 : 12;
            scheduleNoticePanel.Location = new Point(
                Math.Max(8, ClientSize.Width - width - 16), y);
            if (scheduleNoticePanel.Visible) scheduleNoticePanel.BringToFront();
        }

        private void ScheduledNotificationTick(object sender, EventArgs e)
        {
            if (IsDisposed || Disposing) return;
            foreach (var notice in ScheduledScanNoticeStore.Drain(scheduleNoticeListeningSinceUtc))
            {
                if (scheduleNoticeQueue.Count < 20) scheduleNoticeQueue.Enqueue(notice);
            }
            if (scheduleNoticePanel.Visible && DateTime.UtcNow < scheduleNoticeExpiryUtc)
            {
                // Nếu đã quét xong, chuyển ngay sang kết quả sau ít nhất 2 giây
                // hiển thị thông báo bắt đầu (không bắt người dùng chờ hết 9 giây).
                if (scheduleNoticeShowingKind == "started" && scheduleNoticeQueue.Count > 0 &&
                    scheduleNoticeQueue.Peek().Kind != "started" &&
                    (DateTime.UtcNow - scheduleNoticeShownUtc).TotalSeconds >= 2)
                    ShowNextScheduledToast();
                return;
            }
            ShowNextScheduledToast();
        }

        private void DismissScheduledToast()
        {
            if (scheduleNoticePanel == null) return;
            scheduleNoticePanel.Visible = false;
            ShowNextScheduledToast();
        }

        private void ShowNextScheduledToast()
        {
            if (scheduleNoticePanel == null || scheduleNoticePanel.IsDisposed) return;
            if (scheduleNoticeQueue.Count == 0)
            {
                scheduleNoticePanel.Visible = false;
                return;
            }
            ScheduledScanNotice notice = scheduleNoticeQueue.Dequeue();
            bool started = notice.Kind == "started";
            bool failed = notice.Kind == "failed";
            bool threats = notice.Kind == "threats";
            scheduleNoticeAccent.BackColor = failed ? Color.FromArgb(220, 63, 63) :
                started ? Color.FromArgb(27, 115, 236) :
                threats ? Color.FromArgb(229, 145, 32) : Color.FromArgb(19, 159, 97);
            scheduleNoticeTitle.Text = failed ? "Quét tự động gặp lỗi" :
                started ? "Đã bắt đầu quét tự động" :
                threats ? "Hoàn tất · Có mối đe dọa" : "Đã hoàn tất quét tự động";
            scheduleNoticeMessage.Text = "Lịch: " + notice.ScheduleName + "\n" + notice.Message;
            scheduleNoticeShowingKind = notice.Kind;
            scheduleNoticeShownUtc = DateTime.UtcNow;
            scheduleNoticeExpiryUtc = scheduleNoticeShownUtc.AddSeconds(9);
            scheduleNoticePanel.Visible = true;
            PlaceScheduledToast();
            // Cập nhật lịch và Lịch sử sau phiên quét từ Task Scheduler mà không
            // phụ thuộc form quét thủ công (chạy ở tiến trình khác).
            if (!started)
            {
                QueueScheduleSummaryRefresh(true);
                QueueSketchHistoryRefresh();
            }
        }
    }
}
