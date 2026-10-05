using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    // Bố cục dashboard dùng lại toàn bộ control và luồng quét hiện có.
    public partial class UcTongQuan
    {
        private Label lblOverviewTitle;
        private Label lblOverviewSubtitle;
        private Label lblDashboardEyebrow;
        private Label lblDashboardWarningEyebrow;
        private Label lblDashboardDevice;
        private Label lblEmptyActivity;
        private bool dashboardReady;
        private bool dashboardProtectionRunning;

        private void BuildDashboard()
        {
            SuspendLayout();
            BackColor = Theme.DashboardBg;
            pnlContent.BackColor = Theme.DashboardBg;
            tableLayoutPanel11.BackColor = Theme.DashboardBg;
            tableLayoutPanel11.Margin = Padding.Empty;
            tableLayoutPanel12.BackColor = Theme.DashboardBg;
            tableLayoutPanel12.Margin = Padding.Empty;

            pnlPageHead.BackColor = Theme.DashboardBg;
            pnlPageHead.Dock = DockStyle.Top;
            pnlPageHead.Height = 84;
            lblOverviewTitle = new Label
            {
                Name = "lblOverviewTitle", Text = "Tổng quan", AutoSize = true,
                BackColor = Color.Transparent
            };
            lblOverviewSubtitle = new Label
            {
                Name = "lblOverviewSubtitle", AutoSize = true,
                BackColor = Color.Transparent, ForeColor = Theme.TextGray
            };
            pnlPageHead.Controls.Add(lblOverviewTitle);
            pnlPageHead.Controls.Add(lblOverviewSubtitle);
            lblOverviewTitle.Location = new Point(0, 3);
            lblOverviewTitle.Font = Theme.PageTitleFont;
            lblOverviewTitle.ForeColor = Theme.TextDark;
            lblOverviewSubtitle.Visible = true;
            lblOverviewSubtitle.Location = new Point(0, 42);
            lblOverviewSubtitle.Font = Theme.BodyFont;
            lblOverviewSubtitle.Text = "Theo dõi trạng thái bảo vệ và các hoạt động trên máy tính.";
            lblDashboardDevice = new Label
            {
                Name = "lblDashboardDevice", Text = "THIẾT BỊ NÀY",
                AutoSize = false, Size = new Size(124, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.DashboardCaptionFont, ForeColor = Theme.TextGray,
                BackColor = Theme.DashboardBg, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlPageHead.Controls.Add(lblDashboardDevice);
            pnlPageHead.Resize += delegate
            {
                lblDashboardDevice.Location = new Point(Math.Max(0, pnlPageHead.Width - 124), 7);
                lblDashboardDevice.Visible = pnlPageHead.Width >= 840;
                lblOverviewSubtitle.MaximumSize = new Size(Math.Max(1, pnlPageHead.Width), 35);
            };

            pnlHeroHost.BackColor = Theme.DashboardBg;
            pnlHeroHost.Margin = new Padding(0, 0, 0, 16);
            lblDashboardEyebrow = DashboardEyebrow("lblDashboardEyebrow", "TRẠNG THÁI BẢO VỆ", Theme.Green);
            lblDashboardWarningEyebrow = DashboardEyebrow("lblDashboardWarningEyebrow", "CẦN XỬ LÝ", Theme.RedText);
            LayoutDashboardHero(pnlAnToan, tlpAnToanText, picShield, lblProtectionStatus,
                lblAnToanSub, flowHeaderActions, lblDashboardEyebrow, false);
            LayoutDashboardHero(pnlPhatHienDeDoa, tlpPhatHienText, picCanhBao, lblPhatHienTitle,
                lblPhatHienSub, flowPhatHienActions, lblDashboardWarningEyebrow, true);
            btnScanNow.Size = new Size(168, 44);
            btnScanNow.Margin = new Padding(0, 0, 12, 0);
            btnScanNow.Text = "Quét ngay";
            btnQuetNangCao.Size = new Size(200, 44);
            btnQuetNangCao.Text = "Quét nâng cao  ▾";
            btnQuetNangCao.Margin = Padding.Empty;
            btnXemChiTiet.Size = new Size(168, 44);
            btnXemChiTiet.Margin = new Padding(0, 0, 12, 0);
            btnQuetLai.Size = new Size(150, 44);
            btnQuetLai.Margin = Padding.Empty;
            foreach (UiButton b in new[] { btnScanNow, btnQuetNangCao, btnXemChiTiet, btnQuetLai })
                b.Radius = 9;
            // Dòng tiến trình có hàng riêng để không tràn khỏi hai nút quét.
            flowHeaderActions.Controls.Remove(lblScanProgress);
            lblScanProgress.Dock = DockStyle.Bottom;
            lblScanProgress.Height = 30;
            lblScanProgress.Margin = Padding.Empty;
            lblScanProgress.BackColor = Theme.DashboardBg;
            lblScanProgress.Font = Theme.SmallFont;
            Controls.Add(lblScanProgress);

            pnlThongKe.BackColor = Theme.DashboardBg;
            pnlThongKe.Margin = new Padding(0, 0, 0, 16);
            LayoutDashboardStat(grpThreats, tlpThreats, lblThreatTitle, lblThreatCount, lblThreatText,
                new Padding(0, 0, 8, 0));
            LayoutDashboardStat(grpScannedFiles, tlpScanned, lblScannedTitle, lblScannedCount, lblScannedUnit,
                new Padding(8, 0, 8, 0));
            LayoutDashboardStat(grpLastScan, tlpLastScan, lblLastScanTitle, lblLastScanDate, lblLastScanType,
                new Padding(8, 0, 0, 0));
            lblScannedUnit.Text = "Tệp trong phiên quét gần nhất";
            lblLastScanDate.Font = Theme.DashboardDateFont;
            lblLastScanType.Visible = true;
            lblThreatText.Font = Theme.SmallFont;
            lblThreatCount.Font = Theme.StatBigFont;
            lblScannedCount.Font = Theme.StatBigFont;
            lblThreatCount.ForeColor = Theme.Green;
            lblScannedCount.ForeColor = Theme.Blue;

            grpActivity.ShowBorder = true;
            grpActivity.Radius = 14;
            grpActivity.FillColor = Color.White;
            grpActivity.Padding = new Padding(22, 16, 22, 16);
            grpActivity.Margin = Padding.Empty;
            grpActivity.SetHeader("Hoạt động gần đây", UiIcons.Clock(18, Theme.Blue));
            lnkXemLichSu.Text = "Xem lịch sử  ›";
            lnkXemLichSu.Margin = new Padding(8, 6, 0, 0);
            lblActivitySummary.Visible = false;
            dgvActivity.RowTemplate.Height = 56;
            dgvActivity.DefaultCellStyle.Font = Theme.BodyFont;
            dgvActivity.DefaultCellStyle.Padding = new Padding(0, 0, 6, 0);
            dgvActivity.DefaultCellStyle.SelectionBackColor = Theme.BlueFaint;
            dgvActivity.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            dgvActivity.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvActivity.GridColor = Theme.Line;
            colActMark.Width = 42;
            colActMark.DefaultCellStyle.Padding = new Padding(0);
            colActMark.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colActDesc.FillWeight = 70;
            colActTime.FillWeight = 30;
            colActTime.DefaultCellStyle.Font = Theme.SmallFont;
            colActTime.DefaultCellStyle.ForeColor = Theme.TextGray;
            colActTime.DefaultCellStyle.Padding = Padding.Empty;
            lblEmptyActivity = new Label
            {
                Name = "lblEmptyActivity", Dock = DockStyle.Fill,
                Text = "Chưa có hoạt động nào\nBắt đầu quét để kiểm tra máy tính của bạn.",
                TextAlign = ContentAlignment.MiddleCenter, Font = Theme.BodyFont,
                ForeColor = Theme.TextGray, BackColor = Color.White
            };
            grpActivity.Body().Controls.Add(lblEmptyActivity);

            grpAction.Radius = 14;
            grpAction.Margin = Padding.Empty;
            dashboardReady = true;
            AutoScroll = true;
            UpdateDashboardExtent();
            ResumeLayout(true);
        }

        private static Label DashboardEyebrow(string name, string text, Color color)
        {
            return new Label
            {
                Name = name, Text = text, Dock = DockStyle.Fill, AutoSize = false,
                Font = Theme.DashboardCaptionFont, ForeColor = color,
                BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft,
                Margin = Padding.Empty
            };
        }

        private void LayoutDashboardHero(Panel host, TableLayoutPanel layout, PictureBox icon,
            Label title, Label subtitle, FlowLayoutPanel actions, Label eyebrow, bool warning)
        {
            host.BackColor = Theme.DashboardBg;
            host.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                UiKit.Fill(e.Graphics, new Rectangle(0, 0, host.Width - 1, host.Height - 1), 16,
                    warning ? Theme.RedTint : dashboardProtectionRunning ? Theme.DashboardHeroBg : Theme.AmberTint,
                    warning ? Theme.RedSoft : dashboardProtectionRunning ? Theme.DashboardHeroBorder : Theme.AmberSoft, 1);
            };
            layout.SuspendLayout();
            layout.Controls.Clear();
            layout.ColumnStyles.Clear();
            layout.RowStyles.Clear();
            layout.ColumnCount = 2;
            layout.RowCount = 4;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
            foreach (float h in new[] { 26F, 68F, 44F, 50F })
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            layout.Padding = new Padding(26, 22, 26, 20);
            layout.BackColor = Color.Transparent;
            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(subtitle, 0, 2);
            layout.Controls.Add(actions, 0, 3);
            layout.Controls.Add(icon, 1, 0);
            layout.SetRowSpan(icon, 4);
            icon.Anchor = AnchorStyles.None;
            icon.Size = new Size(112, 112);
            icon.Margin = new Padding(16, 0, 0, 0);
            icon.BackColor = Color.Transparent;
            if (warning) icon.Image = UiIcons.HeroBadge(112, false);
            title.AutoSize = false;
            title.AutoEllipsis = true;
            title.Dock = DockStyle.Fill;
            title.Margin = Padding.Empty;
            title.BackColor = Color.Transparent;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Font = Theme.DashboardHeroFont;
            subtitle.AutoSize = false;
            subtitle.AutoEllipsis = true;
            subtitle.Dock = DockStyle.Fill;
            subtitle.Margin = Padding.Empty;
            subtitle.BackColor = Color.Transparent;
            subtitle.Font = Theme.BodyFont;
            subtitle.ForeColor = Theme.TextGray;
            subtitle.TextAlign = ContentAlignment.TopLeft;
            actions.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            actions.AutoSize = false;
            actions.Size = new Size(390, 48);
            actions.Margin = Padding.Empty;
            actions.Padding = Padding.Empty;
            actions.BackColor = Color.Transparent;
            actions.WrapContents = false;
            layout.ResumeLayout(true);
        }

        private static void LayoutDashboardStat(Panel card, TableLayoutPanel layout,
            Label title, Label value, Label hint, Padding margin)
        {
            card.BackColor = Theme.DashboardBg;
            card.Margin = margin;
            card.Padding = new Padding(20, 15, 18, 13);
            card.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                UiKit.Fill(e.Graphics, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14,
                    Color.White, Theme.CardBorder, 1);
            };
            layout.BackColor = Color.Transparent;
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            foreach (Label label in new[] { title, value, hint })
            {
                label.AutoSize = false;
                label.AutoEllipsis = true;
                label.Dock = DockStyle.Fill;
                label.Margin = Padding.Empty;
                label.BackColor = Color.Transparent;
                label.TextAlign = ContentAlignment.MiddleLeft;
            }
            title.ForeColor = Theme.TextGray;
            title.Font = Theme.BodyFont;
            value.ForeColor = Theme.TextDark;
            hint.ForeColor = Theme.TextGray;
        }

        private void UpdateDashboardExtent()
        {
            if (!dashboardReady) return;
            bool overview = currentView == TongQuanView.AnToan || currentView == TongQuanView.PhatHienDeDoa;
            int minWidth = overview ? RongToiThieu :
                currentView == TongQuanView.QuetNangCao ? RongToiThieuTrangNangCao : 980;
            int minHeight = overview ? CaoToiThieu : 640;
            tableLayoutPanel11.MinimumSize = new Size(minWidth, minHeight);
            AutoScrollMinSize = new Size(minWidth, minHeight + (overview ? pnlPageHead.Height : 0)
                + (lblScanProgress.Visible ? lblScanProgress.Height : 0));
        }

        private void SyncDashboardActivity(int count)
        {
            if (!dashboardReady) return;
            lblEmptyActivity.Visible = count == 0;
            if (count == 0) lblEmptyActivity.BringToFront();
            else tlpActivity.BringToFront();
            lnkXemLichSu.Visible = count > 0;
        }

        private void SyncDashboardProtection(bool running)
        {
            if (!dashboardReady) return;
            lblDashboardEyebrow.Text = running ? "BẢO VỆ ĐANG HOẠT ĐỘNG" : "CẦN BẬT BẢO VỆ";
            lblDashboardEyebrow.ForeColor = running ? Theme.Green : Theme.Amber;
            dashboardProtectionRunning = running;
            pnlAnToan.Invalidate(true);
            // UiIcons trả bitmap dùng chung từ cache; control không sở hữu ảnh để Dispose.
            picShield.Image = running ? UiIcons.HeroBadge(112, true) : UiIcons.PausedShieldBadge(112);
        }
    }
}
