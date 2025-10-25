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
            pnlThongKe.MinimumSize = new Size(0, 168);
            if (tableLayoutPanel12.RowStyles.Count > 1)
                tableLayoutPanel12.RowStyles[1].Height = 172F;

            BuildDashboardStatCard(grpThreats, tlpThreats, lblThreatTitle, lblThreatCount, lblThreatText,
                UiIcons.AlertCircle(34), Theme.RedTint, new Padding(0, 0, 10, 0));
            BuildDashboardStatCard(grpScannedFiles, tlpScanned, lblScannedTitle, lblScannedCount, lblScannedUnit,
                UiIcons.FileIcon(34, "exe"), Theme.BlueFaint, new Padding(10, 0, 10, 0));
            BuildDashboardStatCard(grpLastScan, tlpLastScan, lblLastScanTitle, lblLastScanDate, lblLastScanType,
                UiIcons.Clock(34, Theme.Blue), Theme.BlueFaint, new Padding(10, 0, 0, 0));
            lblScannedUnit.Text = "Tổng số tệp đã kiểm tra";
            lblLastScanType.Visible = true;
            lblThreatCount.ForeColor = Theme.Green;
            lblScannedCount.ForeColor = Theme.Blue;
            ApplyDashboardStatsResponsive();
            pnlThongKe.Resize += delegate { ApplyDashboardStatsResponsive(); };

            grpActivity.ShowBorder = true;
            grpActivity.Radius = 14;
            grpActivity.FillColor = Color.White;
            grpActivity.Padding = new Padding(22, 16, 22, 16);
            grpActivity.Margin = Padding.Empty;
            grpActivity.SetHeader("Hoạt động gần đây", UiIcons.Clock(18, Theme.Blue));
            lnkXemLichSu.Text = "Xem lịch sử";
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

        private static void BuildDashboardStatCard(Panel card, TableLayoutPanel content,
            Label title, Label value, Label hint, Image icon, Color iconBackColor, Padding margin)
        {
            card.SuspendLayout();
            content.SuspendLayout();

            card.BackColor = Theme.DashboardBg;
            card.Margin = margin;
            card.Padding = new Padding(24, 18, 24, 18);
            card.MinimumSize = new Size(0, 156);
            card.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                UiKit.Fill(e.Graphics, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 16,
                    Color.White, Theme.CardBorder, 1);
            };

            if (content.Parent != null)
                content.Parent.Controls.Remove(content);

            card.Controls.Clear();
            card.Controls.Add(content);

            content.Dock = DockStyle.Fill;
            content.BackColor = Color.Transparent;
            content.Margin = Padding.Empty;
            content.Padding = Padding.Empty;
            content.ColumnStyles.Clear();
            content.RowStyles.Clear();
            content.Controls.Clear();
            content.ColumnCount = 2;
            content.RowCount = 1;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel iconWrap = CreateDashboardStatIcon(iconBackColor, icon);
            iconWrap.Margin = Padding.Empty;
            iconWrap.Anchor = AnchorStyles.None;

            TableLayoutPanel textLayout = new TableLayoutPanel();
            textLayout.BackColor = Color.Transparent;
            textLayout.Dock = DockStyle.Fill;
            textLayout.Margin = Padding.Empty;
            textLayout.Padding = new Padding(0, 6, 0, 6);
            textLayout.ColumnCount = 1;
            textLayout.RowCount = 3;
            textLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            textLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            textLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            foreach (Label label in new[] { title, value, hint })
            {
                label.AutoSize = false;
                label.AutoEllipsis = true;
                label.Dock = DockStyle.Fill;
                label.Margin = Padding.Empty;
                label.BackColor = Color.Transparent;
                label.TextAlign = ContentAlignment.MiddleLeft;
            }

            title.ForeColor = Theme.TextMid;
            title.Font = Theme.TitleFont;
            value.ForeColor = Theme.TextDark;
            value.Font = Theme.StatBigFont;
            hint.ForeColor = Theme.TextGray;
            hint.Font = Theme.BodyBigFont;

            if (object.ReferenceEquals(value, title))
                value.Font = Theme.StatBigFont;

            if (object.ReferenceEquals(title, value) || object.ReferenceEquals(title, hint))
            {
                // Không xảy ra trong thiết kế hiện tại; để tránh cảnh báo logic sai về sau.
            }

            if (title == null || value == null || hint == null)
            {
                content.ResumeLayout(true);
                card.ResumeLayout(true);
                return;
            }

            textLayout.Controls.Add(title, 0, 0);
            textLayout.Controls.Add(value, 0, 1);
            textLayout.Controls.Add(hint, 0, 2);
            content.Controls.Add(iconWrap, 0, 0);
            content.Controls.Add(textLayout, 1, 0);

            card.ResumeLayout(true);
            content.ResumeLayout(true);
        }

        private static Panel CreateDashboardStatIcon(Color fillColor, Image icon)
        {
            Panel host = new Panel();
            host.BackColor = Color.Transparent;
            host.Size = new Size(102, 102);
            host.MinimumSize = host.Size;
            host.MaximumSize = host.Size;
            host.Margin = Padding.Empty;
            host.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                UiKit.Fill(e.Graphics, new Rectangle(0, 0, host.Width - 1, host.Height - 1), 18,
                    fillColor, fillColor, 1);
            };

            PictureBox pic = new PictureBox();
            pic.BackColor = Color.Transparent;
            pic.Dock = DockStyle.Fill;
            pic.Margin = new Padding(22);
            pic.Padding = new Padding(22);
            pic.SizeMode = PictureBoxSizeMode.CenterImage;
            pic.Image = icon;
            host.Controls.Add(pic);
            return host;
        }

        private void ApplyDashboardStatsResponsive()
        {
            if (pnlThongKe == null) return;

            int width = pnlThongKe.ClientSize.Width;
            int desiredHeight;

            pnlThongKe.SuspendLayout();
            pnlThongKe.Controls.Clear();
            pnlThongKe.ColumnStyles.Clear();
            pnlThongKe.RowStyles.Clear();

            if (width >= 1320)
            {
                pnlThongKe.ColumnCount = 3;
                pnlThongKe.RowCount = 1;
                pnlThongKe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                pnlThongKe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                pnlThongKe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
                pnlThongKe.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                grpThreats.Margin = new Padding(0, 0, 10, 0);
                grpScannedFiles.Margin = new Padding(10, 0, 10, 0);
                grpLastScan.Margin = new Padding(10, 0, 0, 0);

                pnlThongKe.Controls.Add(grpThreats, 0, 0);
                pnlThongKe.Controls.Add(grpScannedFiles, 1, 0);
                pnlThongKe.Controls.Add(grpLastScan, 2, 0);
                desiredHeight = 168;
            }
            else if (width >= 900)
            {
                pnlThongKe.ColumnCount = 2;
                pnlThongKe.RowCount = 2;
                pnlThongKe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                pnlThongKe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                pnlThongKe.RowStyles.Add(new RowStyle(SizeType.Absolute, 168F));
                pnlThongKe.RowStyles.Add(new RowStyle(SizeType.Absolute, 168F));

                grpThreats.Margin = new Padding(0, 0, 10, 10);
                grpScannedFiles.Margin = new Padding(10, 0, 0, 10);
                grpLastScan.Margin = new Padding(0, 10, 0, 0);

                pnlThongKe.Controls.Add(grpThreats, 0, 0);
                pnlThongKe.Controls.Add(grpScannedFiles, 1, 0);
                pnlThongKe.Controls.Add(grpLastScan, 0, 1);
                pnlThongKe.SetColumnSpan(grpLastScan, 2);
                desiredHeight = 346;
            }
            else
            {
                pnlThongKe.ColumnCount = 1;
                pnlThongKe.RowCount = 3;
                pnlThongKe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                pnlThongKe.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
                pnlThongKe.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
                pnlThongKe.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));

                grpThreats.Margin = new Padding(0, 0, 0, 10);
                grpScannedFiles.Margin = new Padding(0, 0, 0, 10);
                grpLastScan.Margin = Padding.Empty;

                pnlThongKe.Controls.Add(grpThreats, 0, 0);
                pnlThongKe.Controls.Add(grpScannedFiles, 0, 1);
                pnlThongKe.Controls.Add(grpLastScan, 0, 2);
                desiredHeight = 500;
            }

            pnlThongKe.MinimumSize = new Size(0, desiredHeight);
            pnlThongKe.Height = desiredHeight;
            if (tableLayoutPanel12.RowStyles.Count > 1)
                tableLayoutPanel12.RowStyles[1].Height = desiredHeight + 4;

            pnlThongKe.ResumeLayout(true);
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
