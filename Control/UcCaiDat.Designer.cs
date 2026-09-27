namespace ScanAndRemoveVirus.Control
{
    partial class UcCaiDat
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnSamples = new ScanAndRemoveVirus.Control.UiButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tlpBody = new System.Windows.Forms.TableLayoutPanel();
            this.grpGeneral = new ScanAndRemoveVirus.Control.UiGroup();
            this.tlpGeneral = new System.Windows.Forms.TableLayoutPanel();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.chkAutoUpdate = new System.Windows.Forms.CheckBox();
            this.chkShowNotification = new System.Windows.Forms.CheckBox();
            this.chkSendSamples = new System.Windows.Forms.CheckBox();
            this.lblGeneralHint = new System.Windows.Forms.Label();
            this.grpGuards = new ScanAndRemoveVirus.Control.UiGroup();
            this.tlpGuards = new System.Windows.Forms.TableLayoutPanel();
            this.chkUsbGuard = new System.Windows.Forms.CheckBox();
            this.chkDownloadGuard = new System.Windows.Forms.CheckBox();
            this.chkBehaviorGuard = new System.Windows.Forms.CheckBox();
            this.chkStartupGuard = new System.Windows.Forms.CheckBox();
            this.chkRestoreGuard = new System.Windows.Forms.CheckBox();
            this.lblGuardsHint = new System.Windows.Forms.Label();
            this.grpVt = new ScanAndRemoveVirus.Control.UiGroup();
            this.tlpVt = new System.Windows.Forms.TableLayoutPanel();
            this.chkVtAutoQuery = new System.Windows.Forms.CheckBox();
            this.lblVtStatus = new System.Windows.Forms.Label();
            this.txtVtKey = new System.Windows.Forms.TextBox();
            this.tlpVtButtons = new System.Windows.Forms.TableLayoutPanel();
            this.btnSaveVtKey = new ScanAndRemoveVirus.Control.UiButton();
            this.btnClearVtKey = new ScanAndRemoveVirus.Control.UiButton();
            this.lblVtHint = new System.Windows.Forms.Label();
            this.grpData = new ScanAndRemoveVirus.Control.UiGroup();
            this.tlpData = new System.Windows.Forms.TableLayoutPanel();
            this.lblAppVersion = new System.Windows.Forms.Label();
            this.lblDbUpdate = new System.Windows.Forms.Label();
            this.lblQuarantined = new System.Windows.Forms.Label();
            this.lblDataPath = new System.Windows.Forms.Label();
            this.tlpDataButtons = new System.Windows.Forms.TableLayoutPanel();
            this.btnOpenData = new ScanAndRemoveVirus.Control.UiButton();
            this.btnClearCache = new ScanAndRemoveVirus.Control.UiButton();
            this.tlpFooter = new System.Windows.Forms.TableLayoutPanel();
            this.lblSavedAt = new System.Windows.Forms.Label();
            this.btnResetDefaults = new ScanAndRemoveVirus.Control.UiButton();
            this.btnSaveSettings = new ScanAndRemoveVirus.Control.UiButton();
            this.tableLayoutPanel1.SuspendLayout();
            this.tlpBody.SuspendLayout();
            this.grpGeneral.SuspendLayout();
            this.tlpGeneral.SuspendLayout();
            this.grpGuards.SuspendLayout();
            this.tlpGuards.SuspendLayout();
            this.grpVt.SuspendLayout();
            this.tlpVt.SuspendLayout();
            this.tlpVtButtons.SuspendLayout();
            this.grpData.SuspendLayout();
            this.tlpData.SuspendLayout();
            this.tlpDataButtons.SuspendLayout();
            this.tlpFooter.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSamples
            // 
            this.btnSamples.BackColor = System.Drawing.Color.Transparent;
            this.btnSamples.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSamples.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSamples.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSamples.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnSamples.Icon = null;
            this.btnSamples.IconGap = 8;
            this.btnSamples.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnSamples.Location = new System.Drawing.Point(310, 2);
            this.btnSamples.Margin = new System.Windows.Forms.Padding(2);
            this.btnSamples.Name = "btnSamples";
            this.btnSamples.Radius = 8;
            this.btnSamples.Size = new System.Drawing.Size(110, 40);
            this.btnSamples.TabIndex = 2;
            this.btnSamples.Text = "Tạo tệp mẫu";
            this.toolTip1.SetToolTip(this.btnSamples, "Tạo 3 tệp mẫu vô hại, mỗi tệp ứng với một kỹ thuật phát hiện, để thử toàn trình q" +
        "uét - cách ly.");
            this.btnSamples.UseVisualStyleBackColor = false;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.White;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tlpBody, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1174, 829);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tlpBody
            // 
            this.tlpBody.ColumnCount = 2;
            this.tlpBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpBody.Controls.Add(this.grpGeneral, 0, 0);
            this.tlpBody.Controls.Add(this.grpGuards, 1, 0);
            this.tlpBody.Controls.Add(this.grpVt, 0, 1);
            this.tlpBody.Controls.Add(this.grpData, 1, 1);
            this.tlpBody.Controls.Add(this.tlpFooter, 0, 2);
            this.tlpBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpBody.Location = new System.Drawing.Point(3, 3);
            this.tlpBody.Name = "tlpBody";
            this.tlpBody.RowCount = 3;
            this.tlpBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 52F));
            this.tlpBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.tlpBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tlpBody.Size = new System.Drawing.Size(1168, 823);
            this.tlpBody.TabIndex = 1;
            // 
            // grpGeneral
            // 
            this.grpGeneral.BackColor = System.Drawing.Color.Transparent;
            this.grpGeneral.Controls.Add(this.tlpGeneral);
            this.grpGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpGeneral.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.grpGeneral.Location = new System.Drawing.Point(8, 8);
            this.grpGeneral.Margin = new System.Windows.Forms.Padding(8);
            this.grpGeneral.Name = "grpGeneral";
            this.grpGeneral.Padding = new System.Windows.Forms.Padding(20, 62, 20, 16);
            this.grpGeneral.Size = new System.Drawing.Size(568, 375);
            this.grpGeneral.TabIndex = 0;
            this.grpGeneral.TabStop = false;
            this.grpGeneral.Text = "Khởi động & đồng bộ";
            // 
            // tlpGeneral
            // 
            this.tlpGeneral.ColumnCount = 1;
            this.tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpGeneral.Controls.Add(this.chkAutoStart, 0, 0);
            this.tlpGeneral.Controls.Add(this.chkAutoUpdate, 0, 1);
            this.tlpGeneral.Controls.Add(this.chkShowNotification, 0, 2);
            this.tlpGeneral.Controls.Add(this.chkSendSamples, 0, 3);
            this.tlpGeneral.Controls.Add(this.lblGeneralHint, 0, 4);
            this.tlpGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpGeneral.Location = new System.Drawing.Point(20, 62);
            this.tlpGeneral.Margin = new System.Windows.Forms.Padding(0);
            this.tlpGeneral.Name = "tlpGeneral";
            this.tlpGeneral.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tlpGeneral.RowCount = 5;
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tlpGeneral.Size = new System.Drawing.Size(528, 297);
            this.tlpGeneral.TabIndex = 0;
            // 
            // chkAutoStart
            // 
            this.chkAutoStart.AutoSize = true;
            this.chkAutoStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkAutoStart.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkAutoStart.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkAutoStart.Location = new System.Drawing.Point(12, 4);
            this.chkAutoStart.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(504, 36);
            this.chkAutoStart.TabIndex = 0;
            this.chkAutoStart.Text = "* Tự động khởi động cùng Windows khi đăng nhập";
            this.chkAutoStart.UseVisualStyleBackColor = true;
            // 
            // chkAutoUpdate
            // 
            this.chkAutoUpdate.AutoSize = true;
            this.chkAutoUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkAutoUpdate.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkAutoUpdate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkAutoUpdate.Location = new System.Drawing.Point(12, 40);
            this.chkAutoUpdate.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkAutoUpdate.Name = "chkAutoUpdate";
            this.chkAutoUpdate.Size = new System.Drawing.Size(504, 36);
            this.chkAutoUpdate.TabIndex = 1;
            this.chkAutoUpdate.Text = "Tự động cập nhật CSDL chữ ký khi mở app (quá 24 giờ)";
            this.chkAutoUpdate.UseVisualStyleBackColor = true;
            this.chkAutoUpdate.CheckedChanged += new System.EventHandler(this.FlagToggle_CheckedChanged);
            // 
            // chkShowNotification
            // 
            this.chkShowNotification.AutoSize = true;
            this.chkShowNotification.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkShowNotification.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkShowNotification.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkShowNotification.Location = new System.Drawing.Point(12, 76);
            this.chkShowNotification.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkShowNotification.Name = "chkShowNotification";
            this.chkShowNotification.Size = new System.Drawing.Size(504, 36);
            this.chkShowNotification.TabIndex = 2;
            this.chkShowNotification.Text = "Hiển thị pop-up cảnh báo khi phát hiện mối đe dọa";
            this.chkShowNotification.UseVisualStyleBackColor = true;
            this.chkShowNotification.CheckedChanged += new System.EventHandler(this.FlagToggle_CheckedChanged);
            // 
            // chkSendSamples
            // 
            this.chkSendSamples.AutoSize = true;
            this.chkSendSamples.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkSendSamples.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkSendSamples.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkSendSamples.Location = new System.Drawing.Point(12, 112);
            this.chkSendSamples.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkSendSamples.Name = "chkSendSamples";
            this.chkSendSamples.Size = new System.Drawing.Size(504, 36);
            this.chkSendSamples.TabIndex = 3;
            this.chkSendSamples.Text = "* Gửi mẫu tệp nghi ngờ để phân tích";
            this.chkSendSamples.UseVisualStyleBackColor = true;
            // 
            // lblGeneralHint
            // 
            this.lblGeneralHint.AutoSize = true;
            this.lblGeneralHint.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblGeneralHint.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGeneralHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblGeneralHint.Location = new System.Drawing.Point(12, 263);
            this.lblGeneralHint.Margin = new System.Windows.Forms.Padding(6, 0, 6, 4);
            this.lblGeneralHint.Name = "lblGeneralHint";
            this.lblGeneralHint.Size = new System.Drawing.Size(504, 26);
            this.lblGeneralHint.TabIndex = 4;
            this.lblGeneralHint.Text = "* Các mục đánh dấu sao chỉ có hiệu lực sau khi bấm \"Lưu thiết lập\".\r\nCờ bật/tắt c" +
    "òn lại được ghi và áp dụng ngay lập tức.";
            // 
            // grpGuards
            // 
            this.grpGuards.BackColor = System.Drawing.Color.Transparent;
            this.grpGuards.Controls.Add(this.tlpGuards);
            this.grpGuards.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpGuards.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.grpGuards.Location = new System.Drawing.Point(592, 8);
            this.grpGuards.Margin = new System.Windows.Forms.Padding(8);
            this.grpGuards.Name = "grpGuards";
            this.grpGuards.Padding = new System.Windows.Forms.Padding(20, 62, 20, 16);
            this.grpGuards.Size = new System.Drawing.Size(568, 375);
            this.grpGuards.TabIndex = 1;
            this.grpGuards.TabStop = false;
            this.grpGuards.Text = "Bảo vệ chủ động (guard nền)";
            // 
            // tlpGuards
            // 
            this.tlpGuards.ColumnCount = 1;
            this.tlpGuards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpGuards.Controls.Add(this.chkUsbGuard, 0, 0);
            this.tlpGuards.Controls.Add(this.chkDownloadGuard, 0, 1);
            this.tlpGuards.Controls.Add(this.chkBehaviorGuard, 0, 2);
            this.tlpGuards.Controls.Add(this.chkStartupGuard, 0, 3);
            this.tlpGuards.Controls.Add(this.chkRestoreGuard, 0, 4);
            this.tlpGuards.Controls.Add(this.lblGuardsHint, 0, 5);
            this.tlpGuards.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpGuards.Location = new System.Drawing.Point(20, 62);
            this.tlpGuards.Margin = new System.Windows.Forms.Padding(0);
            this.tlpGuards.Name = "tlpGuards";
            this.tlpGuards.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tlpGuards.RowCount = 6;
            this.tlpGuards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpGuards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpGuards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpGuards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpGuards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpGuards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tlpGuards.Size = new System.Drawing.Size(528, 297);
            this.tlpGuards.TabIndex = 0;
            // 
            // chkUsbGuard
            // 
            this.chkUsbGuard.AutoSize = true;
            this.chkUsbGuard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkUsbGuard.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkUsbGuard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkUsbGuard.Location = new System.Drawing.Point(12, 4);
            this.chkUsbGuard.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkUsbGuard.Name = "chkUsbGuard";
            this.chkUsbGuard.Size = new System.Drawing.Size(504, 36);
            this.chkUsbGuard.TabIndex = 0;
            this.chkUsbGuard.Text = "Quét ổ USB/removable ngay khi vừa cắm vào";
            this.chkUsbGuard.UseVisualStyleBackColor = true;
            this.chkUsbGuard.CheckedChanged += new System.EventHandler(this.FlagToggle_CheckedChanged);
            // 
            // chkDownloadGuard
            // 
            this.chkDownloadGuard.AutoSize = true;
            this.chkDownloadGuard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkDownloadGuard.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDownloadGuard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkDownloadGuard.Location = new System.Drawing.Point(12, 40);
            this.chkDownloadGuard.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkDownloadGuard.Name = "chkDownloadGuard";
            this.chkDownloadGuard.Size = new System.Drawing.Size(504, 36);
            this.chkDownloadGuard.TabIndex = 1;
            this.chkDownloadGuard.Text = "Quét tệp tải về (đánh dấu Zone.Identifier trong Downloads)";
            this.chkDownloadGuard.UseVisualStyleBackColor = true;
            this.chkDownloadGuard.CheckedChanged += new System.EventHandler(this.FlagToggle_CheckedChanged);
            // 
            // chkBehaviorGuard
            // 
            this.chkBehaviorGuard.AutoSize = true;
            this.chkBehaviorGuard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkBehaviorGuard.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkBehaviorGuard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkBehaviorGuard.Location = new System.Drawing.Point(12, 76);
            this.chkBehaviorGuard.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkBehaviorGuard.Name = "chkBehaviorGuard";
            this.chkBehaviorGuard.Size = new System.Drawing.Size(504, 36);
            this.chkBehaviorGuard.TabIndex = 2;
            this.chkBehaviorGuard.Text = "Giám sát hành vi tiến trình đáng ngờ (WMI)";
            this.chkBehaviorGuard.UseVisualStyleBackColor = true;
            this.chkBehaviorGuard.CheckedChanged += new System.EventHandler(this.FlagToggle_CheckedChanged);
            // 
            // chkStartupGuard
            // 
            this.chkStartupGuard.AutoSize = true;
            this.chkStartupGuard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkStartupGuard.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkStartupGuard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkStartupGuard.Location = new System.Drawing.Point(12, 112);
            this.chkStartupGuard.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkStartupGuard.Name = "chkStartupGuard";
            this.chkStartupGuard.Size = new System.Drawing.Size(504, 36);
            this.chkStartupGuard.TabIndex = 3;
            this.chkStartupGuard.Text = "Canh thư mục Startup — báo ngay khi tệp lạ rơi vào";
            this.chkStartupGuard.UseVisualStyleBackColor = true;
            this.chkStartupGuard.CheckedChanged += new System.EventHandler(this.FlagToggle_CheckedChanged);
            // 
            // chkRestoreGuard
            // 
            this.chkRestoreGuard.AutoSize = true;
            this.chkRestoreGuard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkRestoreGuard.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkRestoreGuard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkRestoreGuard.Location = new System.Drawing.Point(12, 148);
            this.chkRestoreGuard.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkRestoreGuard.Name = "chkRestoreGuard";
            this.chkRestoreGuard.Size = new System.Drawing.Size(504, 36);
            this.chkRestoreGuard.TabIndex = 4;
            this.chkRestoreGuard.Text = "Chặn khôi phục tệp còn độc từ khu cách ly";
            this.chkRestoreGuard.UseVisualStyleBackColor = true;
            this.chkRestoreGuard.CheckedChanged += new System.EventHandler(this.FlagToggle_CheckedChanged);
            // 
            // lblGuardsHint
            // 
            this.lblGuardsHint.AutoSize = true;
            this.lblGuardsHint.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblGuardsHint.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGuardsHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblGuardsHint.Location = new System.Drawing.Point(12, 263);
            this.lblGuardsHint.Margin = new System.Windows.Forms.Padding(6, 0, 6, 4);
            this.lblGuardsHint.Name = "lblGuardsHint";
            this.lblGuardsHint.Size = new System.Drawing.Size(504, 26);
            this.lblGuardsHint.TabIndex = 5;
            this.lblGuardsHint.Text = "Mỗi công tắc bật/tắt cơ chế thật ngay khi chạm và tự ghi vào settings.ini.\r\nTrùng" +
    " khớp với các hàng guard ở tab Bảo vệ (một nguồn dữ liệu).";
            // 
            // grpVt
            // 
            this.grpVt.BackColor = System.Drawing.Color.Transparent;
            this.grpVt.Controls.Add(this.tlpVt);
            this.grpVt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVt.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.grpVt.Location = new System.Drawing.Point(8, 399);
            this.grpVt.Margin = new System.Windows.Forms.Padding(8);
            this.grpVt.Name = "grpVt";
            this.grpVt.Padding = new System.Windows.Forms.Padding(20, 62, 20, 16);
            this.grpVt.Size = new System.Drawing.Size(568, 345);
            this.grpVt.TabIndex = 2;
            this.grpVt.TabStop = false;
            this.grpVt.Text = "VirusTotal (phân tích cloud)";
            // 
            // tlpVt
            // 
            this.tlpVt.ColumnCount = 1;
            this.tlpVt.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpVt.Controls.Add(this.chkVtAutoQuery, 0, 0);
            this.tlpVt.Controls.Add(this.lblVtStatus, 0, 1);
            this.tlpVt.Controls.Add(this.txtVtKey, 0, 2);
            this.tlpVt.Controls.Add(this.tlpVtButtons, 0, 3);
            this.tlpVt.Controls.Add(this.lblVtHint, 0, 4);
            this.tlpVt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpVt.Location = new System.Drawing.Point(20, 62);
            this.tlpVt.Margin = new System.Windows.Forms.Padding(0);
            this.tlpVt.Name = "tlpVt";
            this.tlpVt.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tlpVt.RowCount = 5;
            this.tlpVt.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpVt.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tlpVt.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpVt.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpVt.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpVt.Size = new System.Drawing.Size(528, 267);
            this.tlpVt.TabIndex = 0;
            // 
            // chkVtAutoQuery
            // 
            this.chkVtAutoQuery.AutoSize = true;
            this.chkVtAutoQuery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkVtAutoQuery.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkVtAutoQuery.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.chkVtAutoQuery.Location = new System.Drawing.Point(12, 4);
            this.chkVtAutoQuery.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkVtAutoQuery.Name = "chkVtAutoQuery";
            this.chkVtAutoQuery.Size = new System.Drawing.Size(504, 36);
            this.chkVtAutoQuery.TabIndex = 0;
            this.chkVtAutoQuery.Text = "Tự động tra VirusTotal khi heuristic nghi ngờ";
            this.chkVtAutoQuery.UseVisualStyleBackColor = true;
            this.chkVtAutoQuery.CheckedChanged += new System.EventHandler(this.FlagToggle_CheckedChanged);
            // 
            // lblVtStatus
            // 
            this.lblVtStatus.AutoSize = true;
            this.lblVtStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVtStatus.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVtStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblVtStatus.Location = new System.Drawing.Point(12, 40);
            this.lblVtStatus.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblVtStatus.Name = "lblVtStatus";
            this.lblVtStatus.Size = new System.Drawing.Size(504, 26);
            this.lblVtStatus.TabIndex = 1;
            this.lblVtStatus.Text = "API key: chưa cấu hình";
            // 
            // txtVtKey
            // 
            this.txtVtKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVtKey.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtVtKey.Location = new System.Drawing.Point(12, 66);
            this.txtVtKey.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.txtVtKey.Name = "txtVtKey";
            this.txtVtKey.PasswordChar = '*';
            this.txtVtKey.Size = new System.Drawing.Size(504, 25);
            this.txtVtKey.TabIndex = 2;
            // 
            // tlpVtButtons
            // 
            this.tlpVtButtons.ColumnCount = 3;
            this.tlpVtButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpVtButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpVtButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpVtButtons.Controls.Add(this.btnSaveVtKey, 0, 0);
            this.tlpVtButtons.Controls.Add(this.btnClearVtKey, 1, 0);
            this.tlpVtButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpVtButtons.Location = new System.Drawing.Point(12, 100);
            this.tlpVtButtons.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tlpVtButtons.Name = "tlpVtButtons";
            this.tlpVtButtons.RowCount = 1;
            this.tlpVtButtons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpVtButtons.Size = new System.Drawing.Size(504, 44);
            this.tlpVtButtons.TabIndex = 3;
            // 
            // btnSaveVtKey
            // 
            this.btnSaveVtKey.BackColor = System.Drawing.Color.Transparent;
            this.btnSaveVtKey.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSaveVtKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveVtKey.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveVtKey.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnSaveVtKey.Icon = null;
            this.btnSaveVtKey.IconGap = 8;
            this.btnSaveVtKey.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnSaveVtKey.Location = new System.Drawing.Point(2, 2);
            this.btnSaveVtKey.Margin = new System.Windows.Forms.Padding(2);
            this.btnSaveVtKey.Name = "btnSaveVtKey";
            this.btnSaveVtKey.Radius = 8;
            this.btnSaveVtKey.Size = new System.Drawing.Size(146, 40);
            this.btnSaveVtKey.TabIndex = 0;
            this.btnSaveVtKey.Text = "Lưu API key";
            this.btnSaveVtKey.UseVisualStyleBackColor = false;
            // 
            // btnClearVtKey
            // 
            this.btnClearVtKey.BackColor = System.Drawing.Color.Transparent;
            this.btnClearVtKey.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClearVtKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClearVtKey.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearVtKey.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnClearVtKey.Icon = null;
            this.btnClearVtKey.IconGap = 8;
            this.btnClearVtKey.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnClearVtKey.Location = new System.Drawing.Point(152, 2);
            this.btnClearVtKey.Margin = new System.Windows.Forms.Padding(2);
            this.btnClearVtKey.Name = "btnClearVtKey";
            this.btnClearVtKey.Radius = 8;
            this.btnClearVtKey.Size = new System.Drawing.Size(146, 40);
            this.btnClearVtKey.TabIndex = 1;
            this.btnClearVtKey.Text = "Gỡ API key";
            this.btnClearVtKey.UseVisualStyleBackColor = false;
            // 
            // lblVtHint
            // 
            this.lblVtHint.AutoSize = true;
            this.lblVtHint.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblVtHint.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVtHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblVtHint.Location = new System.Drawing.Point(12, 144);
            this.lblVtHint.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblVtHint.Name = "lblVtHint";
            this.lblVtHint.Size = new System.Drawing.Size(504, 13);
            this.lblVtHint.TabIndex = 4;
            this.lblVtHint.Text = "Lấy key miễn phí tại virustotal.com/gui/settings/api-key — lưu tại vtapikey.txt.";
            // 
            // grpData
            // 
            this.grpData.BackColor = System.Drawing.Color.Transparent;
            this.grpData.Controls.Add(this.tlpData);
            this.grpData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpData.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.grpData.Location = new System.Drawing.Point(592, 399);
            this.grpData.Margin = new System.Windows.Forms.Padding(8);
            this.grpData.Name = "grpData";
            this.grpData.Padding = new System.Windows.Forms.Padding(20, 62, 20, 16);
            this.grpData.Size = new System.Drawing.Size(568, 345);
            this.grpData.TabIndex = 3;
            this.grpData.TabStop = false;
            this.grpData.Text = "Dữ liệu & công cụ";
            // 
            // tlpData
            // 
            this.tlpData.ColumnCount = 1;
            this.tlpData.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpData.Controls.Add(this.lblAppVersion, 0, 0);
            this.tlpData.Controls.Add(this.lblDbUpdate, 0, 1);
            this.tlpData.Controls.Add(this.lblQuarantined, 0, 2);
            this.tlpData.Controls.Add(this.lblDataPath, 0, 3);
            this.tlpData.Controls.Add(this.tlpDataButtons, 0, 4);
            this.tlpData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpData.Location = new System.Drawing.Point(20, 62);
            this.tlpData.Margin = new System.Windows.Forms.Padding(0);
            this.tlpData.Name = "tlpData";
            this.tlpData.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tlpData.RowCount = 5;
            this.tlpData.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tlpData.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tlpData.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tlpData.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tlpData.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpData.Size = new System.Drawing.Size(528, 267);
            this.tlpData.TabIndex = 0;
            // 
            // lblAppVersion
            // 
            this.lblAppVersion.AutoSize = true;
            this.lblAppVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAppVersion.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAppVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.lblAppVersion.Location = new System.Drawing.Point(12, 4);
            this.lblAppVersion.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAppVersion.Name = "lblAppVersion";
            this.lblAppVersion.Size = new System.Drawing.Size(504, 28);
            this.lblAppVersion.TabIndex = 0;
            this.lblAppVersion.Text = "Phiên bản ứng dụng: —";
            // 
            // lblDbUpdate
            // 
            this.lblDbUpdate.AutoSize = true;
            this.lblDbUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDbUpdate.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDbUpdate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.lblDbUpdate.Location = new System.Drawing.Point(12, 32);
            this.lblDbUpdate.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblDbUpdate.Name = "lblDbUpdate";
            this.lblDbUpdate.Size = new System.Drawing.Size(504, 28);
            this.lblDbUpdate.TabIndex = 1;
            this.lblDbUpdate.Text = "CSDL chữ ký: —";
            // 
            // lblQuarantined
            // 
            this.lblQuarantined.AutoSize = true;
            this.lblQuarantined.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQuarantined.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblQuarantined.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.lblQuarantined.Location = new System.Drawing.Point(12, 60);
            this.lblQuarantined.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblQuarantined.Name = "lblQuarantined";
            this.lblQuarantined.Size = new System.Drawing.Size(504, 28);
            this.lblQuarantined.TabIndex = 2;
            this.lblQuarantined.Text = "Đang cách ly: —";
            // 
            // lblDataPath
            // 
            this.lblDataPath.AutoSize = true;
            this.lblDataPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDataPath.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDataPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblDataPath.Location = new System.Drawing.Point(12, 88);
            this.lblDataPath.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblDataPath.Name = "lblDataPath";
            this.lblDataPath.Size = new System.Drawing.Size(504, 40);
            this.lblDataPath.TabIndex = 3;
            this.lblDataPath.Text = "Thư mục dữ liệu: —";
            // 
            // tlpDataButtons
            // 
            this.tlpDataButtons.ColumnCount = 4;
            this.tlpDataButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.tlpDataButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26F));
            this.tlpDataButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23.01587F));
            this.tlpDataButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15.87302F));
            this.tlpDataButtons.Controls.Add(this.btnOpenData, 0, 0);
            this.tlpDataButtons.Controls.Add(this.btnClearCache, 1, 0);
            this.tlpDataButtons.Controls.Add(this.btnSamples, 2, 0);
            this.tlpDataButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpDataButtons.Location = new System.Drawing.Point(12, 128);
            this.tlpDataButtons.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tlpDataButtons.Name = "tlpDataButtons";
            this.tlpDataButtons.RowCount = 1;
            this.tlpDataButtons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpDataButtons.Size = new System.Drawing.Size(504, 44);
            this.tlpDataButtons.TabIndex = 4;
            // 
            // btnOpenData
            // 
            this.btnOpenData.BackColor = System.Drawing.Color.Transparent;
            this.btnOpenData.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOpenData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOpenData.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenData.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnOpenData.Icon = null;
            this.btnOpenData.IconGap = 8;
            this.btnOpenData.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnOpenData.Location = new System.Drawing.Point(2, 2);
            this.btnOpenData.Margin = new System.Windows.Forms.Padding(2);
            this.btnOpenData.Name = "btnOpenData";
            this.btnOpenData.Radius = 8;
            this.btnOpenData.Size = new System.Drawing.Size(175, 40);
            this.btnOpenData.TabIndex = 0;
            this.btnOpenData.Text = "Mở thư mục dữ liệu";
            this.btnOpenData.UseVisualStyleBackColor = false;
            // 
            // btnClearCache
            // 
            this.btnClearCache.BackColor = System.Drawing.Color.Transparent;
            this.btnClearCache.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClearCache.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClearCache.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearCache.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnClearCache.Icon = null;
            this.btnClearCache.IconGap = 8;
            this.btnClearCache.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnClearCache.Location = new System.Drawing.Point(181, 2);
            this.btnClearCache.Margin = new System.Windows.Forms.Padding(2);
            this.btnClearCache.Name = "btnClearCache";
            this.btnClearCache.Radius = 8;
            this.btnClearCache.Size = new System.Drawing.Size(125, 40);
            this.btnClearCache.TabIndex = 1;
            this.btnClearCache.Text = "Xóa cache quét";
            this.btnClearCache.UseVisualStyleBackColor = false;
            // 
            // tlpFooter
            // 
            this.tlpFooter.ColumnCount = 4;
            this.tlpBody.SetColumnSpan(this.tlpFooter, 2);
            this.tlpFooter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpFooter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpFooter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.tlpFooter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.tlpFooter.Controls.Add(this.lblSavedAt, 0, 0);
            this.tlpFooter.Controls.Add(this.btnResetDefaults, 1, 0);
            this.tlpFooter.Controls.Add(this.btnSaveSettings, 2, 0);
            this.tlpFooter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpFooter.Location = new System.Drawing.Point(0, 752);
            this.tlpFooter.Margin = new System.Windows.Forms.Padding(0);
            this.tlpFooter.Name = "tlpFooter";
            this.tlpFooter.RowCount = 1;
            this.tlpFooter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpFooter.Size = new System.Drawing.Size(1168, 71);
            this.tlpFooter.TabIndex = 4;
            // 
            // lblSavedAt
            // 
            this.lblSavedAt.AutoSize = true;
            this.lblSavedAt.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblSavedAt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSavedAt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(163)))), ((int)(((byte)(74)))));
            this.lblSavedAt.Location = new System.Drawing.Point(6, 0);
            this.lblSavedAt.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblSavedAt.Name = "lblSavedAt";
            this.lblSavedAt.Size = new System.Drawing.Size(0, 71);
            this.lblSavedAt.TabIndex = 0;
            this.lblSavedAt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnResetDefaults
            // 
            this.btnResetDefaults.BackColor = System.Drawing.Color.Transparent;
            this.btnResetDefaults.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnResetDefaults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnResetDefaults.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetDefaults.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnResetDefaults.Icon = null;
            this.btnResetDefaults.IconGap = 8;
            this.btnResetDefaults.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnResetDefaults.Location = new System.Drawing.Point(842, 2);
            this.btnResetDefaults.Margin = new System.Windows.Forms.Padding(2);
            this.btnResetDefaults.Name = "btnResetDefaults";
            this.btnResetDefaults.Radius = 8;
            this.btnResetDefaults.Size = new System.Drawing.Size(146, 67);
            this.btnResetDefaults.TabIndex = 1;
            this.btnResetDefaults.Text = "Khôi phục mặc định";
            this.btnResetDefaults.UseVisualStyleBackColor = false;
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.BackColor = System.Drawing.Color.Transparent;
            this.btnSaveSettings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSaveSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveSettings.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnSaveSettings.Icon = null;
            this.btnSaveSettings.IconGap = 8;
            this.btnSaveSettings.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnSaveSettings.Location = new System.Drawing.Point(992, 2);
            this.btnSaveSettings.Margin = new System.Windows.Forms.Padding(2);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Radius = 8;
            this.btnSaveSettings.Size = new System.Drawing.Size(166, 67);
            this.btnSaveSettings.TabIndex = 2;
            this.btnSaveSettings.Text = "Lưu thiết lập";
            this.btnSaveSettings.UseVisualStyleBackColor = false;
            // 
            // UcCaiDat
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "UcCaiDat";
            this.Size = new System.Drawing.Size(1174, 829);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tlpBody.ResumeLayout(false);
            this.grpGeneral.ResumeLayout(false);
            this.tlpGeneral.ResumeLayout(false);
            this.tlpGeneral.PerformLayout();
            this.grpGuards.ResumeLayout(false);
            this.tlpGuards.ResumeLayout(false);
            this.tlpGuards.PerformLayout();
            this.grpVt.ResumeLayout(false);
            this.tlpVt.ResumeLayout(false);
            this.tlpVt.PerformLayout();
            this.tlpVtButtons.ResumeLayout(false);
            this.grpData.ResumeLayout(false);
            this.tlpData.ResumeLayout(false);
            this.tlpData.PerformLayout();
            this.tlpDataButtons.ResumeLayout(false);
            this.tlpFooter.ResumeLayout(false);
            this.tlpFooter.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tlpBody;
        private ScanAndRemoveVirus.Control.UiGroup grpGeneral;
        private System.Windows.Forms.TableLayoutPanel tlpGeneral;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.CheckBox chkAutoUpdate;
        private System.Windows.Forms.CheckBox chkSendSamples;
        private System.Windows.Forms.CheckBox chkShowNotification;
        private System.Windows.Forms.Label lblGeneralHint;
        private ScanAndRemoveVirus.Control.UiGroup grpGuards;
        private System.Windows.Forms.TableLayoutPanel tlpGuards;
        private System.Windows.Forms.CheckBox chkUsbGuard;
        private System.Windows.Forms.CheckBox chkDownloadGuard;
        private System.Windows.Forms.CheckBox chkBehaviorGuard;
        private System.Windows.Forms.CheckBox chkStartupGuard;
        private System.Windows.Forms.CheckBox chkRestoreGuard;
        private System.Windows.Forms.Label lblGuardsHint;
        private ScanAndRemoveVirus.Control.UiGroup grpVt;
        private System.Windows.Forms.TableLayoutPanel tlpVt;
        private System.Windows.Forms.CheckBox chkVtAutoQuery;
        private System.Windows.Forms.Label lblVtStatus;
        private System.Windows.Forms.TextBox txtVtKey;
        private System.Windows.Forms.TableLayoutPanel tlpVtButtons;
        private ScanAndRemoveVirus.Control.UiButton btnSaveVtKey;
        private ScanAndRemoveVirus.Control.UiButton btnClearVtKey;
        private System.Windows.Forms.Label lblVtHint;
        private ScanAndRemoveVirus.Control.UiGroup grpData;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TableLayoutPanel tlpData;
        private System.Windows.Forms.Label lblAppVersion;
        private System.Windows.Forms.Label lblDbUpdate;
        private System.Windows.Forms.Label lblQuarantined;
        private System.Windows.Forms.Label lblDataPath;
        private System.Windows.Forms.TableLayoutPanel tlpDataButtons;
        private ScanAndRemoveVirus.Control.UiButton btnOpenData;
        private ScanAndRemoveVirus.Control.UiButton btnClearCache;
        private ScanAndRemoveVirus.Control.UiButton btnSamples;
        private System.Windows.Forms.TableLayoutPanel tlpFooter;
        private System.Windows.Forms.Label lblSavedAt;
        private ScanAndRemoveVirus.Control.UiButton btnResetDefaults;
        private ScanAndRemoveVirus.Control.UiButton btnSaveSettings;
    }
}
