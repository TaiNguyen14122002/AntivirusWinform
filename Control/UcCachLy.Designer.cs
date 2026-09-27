namespace ScanAndRemoveVirus.Control
{
    partial class UcCachLy
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.grpQuarentineList = new ScanAndRemoveVirus.Control.UiGroup();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.btnRestore = new ScanAndRemoveVirus.Control.UiButton();
            this.btnRestoreAll = new ScanAndRemoveVirus.Control.UiButton();
            this.btnDeletePermanent = new ScanAndRemoveVirus.Control.UiButton();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.btnRefreshQuarantine = new ScanAndRemoveVirus.Control.UiButton();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.lblTotalFilesTitle = new System.Windows.Forms.Label();
            this.lblTotalFilesValue = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.dgvQuarantine = new System.Windows.Forms.DataGridView();
            this.colPick = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colFileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOriginaPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colThreatName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDetectedTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFileSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpQuarantineInfo = new ScanAndRemoveVirus.Control.UiGroup();
            this.tableLayoutPanel10 = new System.Windows.Forms.TableLayoutPanel();
            this.lblInfo1 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.grpQuarentineList.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.tableLayoutPanel8.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvQuarantine)).BeginInit();
            this.grpQuarantineInfo.SuspendLayout();
            this.tableLayoutPanel10.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1174, 829);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this.tableLayoutPanel4, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.grpQuarantineInfo, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(1168, 803);
            this.tableLayoutPanel3.TabIndex = 1;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.grpQuarentineList, 0, 0);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 1;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 564F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(1162, 669);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // grpQuarentineList
            // 
            this.grpQuarentineList.BackColor = System.Drawing.Color.Transparent;
            this.grpQuarentineList.Controls.Add(this.tableLayoutPanel5);
            this.grpQuarentineList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpQuarentineList.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.grpQuarentineList.Location = new System.Drawing.Point(3, 3);
            this.grpQuarentineList.Name = "grpQuarentineList";
            this.grpQuarentineList.Padding = new System.Windows.Forms.Padding(20, 62, 20, 16);
            this.grpQuarentineList.Size = new System.Drawing.Size(1156, 663);
            this.grpQuarentineList.TabIndex = 0;
            this.grpQuarentineList.TabStop = false;
            this.grpQuarentineList.Text = "Danh sách tệp trong cách ly";
            this.grpQuarentineList.Enter += new System.EventHandler(this.grpQuarentineList_Enter);
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 1;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Controls.Add(this.panel2, 0, 1);
            this.tableLayoutPanel5.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(20, 62);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 2;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(1116, 585);
            this.tableLayoutPanel5.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.tableLayoutPanel6);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(10, 529);
            this.panel2.Margin = new System.Windows.Forms.Padding(10);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1096, 46);
            this.panel2.TabIndex = 1;
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 2;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.tableLayoutPanel6.Controls.Add(this.tableLayoutPanel7, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.tableLayoutPanel8, 1, 0);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 1;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(1096, 46);
            this.tableLayoutPanel6.TabIndex = 0;
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 4;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 132F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.Controls.Add(this.btnRestore, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.btnRestoreAll, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.btnDeletePermanent, 2, 0);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 1;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(830, 40);
            this.tableLayoutPanel7.TabIndex = 0;
            // 
            // btnRestore
            // 
            this.btnRestore.BackColor = System.Drawing.Color.Transparent;
            this.btnRestore.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRestore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRestore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestore.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnRestore.Icon = null;
            this.btnRestore.IconGap = 8;
            this.btnRestore.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnRestore.Location = new System.Drawing.Point(0, 0);
            this.btnRestore.Margin = new System.Windows.Forms.Padding(0);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Radius = 8;
            this.btnRestore.Size = new System.Drawing.Size(132, 40);
            this.btnRestore.TabIndex = 0;
            this.btnRestore.Text = "Khôi phục";
            this.btnRestore.UseVisualStyleBackColor = false;
            // 
            // btnRestoreAll
            // 
            this.btnRestoreAll.BackColor = System.Drawing.Color.Transparent;
            this.btnRestoreAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRestoreAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRestoreAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestoreAll.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnRestoreAll.Icon = null;
            this.btnRestoreAll.IconGap = 8;
            this.btnRestoreAll.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnRestoreAll.Location = new System.Drawing.Point(132, 0);
            this.btnRestoreAll.Margin = new System.Windows.Forms.Padding(0);
            this.btnRestoreAll.Name = "btnRestoreAll";
            this.btnRestoreAll.Radius = 8;
            this.btnRestoreAll.Size = new System.Drawing.Size(170, 40);
            this.btnRestoreAll.TabIndex = 1;
            this.btnRestoreAll.Text = "Khôi phục tất cả";
            this.btnRestoreAll.UseVisualStyleBackColor = false;
            // 
            // btnDeletePermanent
            // 
            this.btnDeletePermanent.BackColor = System.Drawing.Color.Transparent;
            this.btnDeletePermanent.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDeletePermanent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDeletePermanent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeletePermanent.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnDeletePermanent.Icon = null;
            this.btnDeletePermanent.IconGap = 8;
            this.btnDeletePermanent.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnDeletePermanent.Location = new System.Drawing.Point(302, 0);
            this.btnDeletePermanent.Margin = new System.Windows.Forms.Padding(0);
            this.btnDeletePermanent.Name = "btnDeletePermanent";
            this.btnDeletePermanent.Radius = 8;
            this.btnDeletePermanent.Size = new System.Drawing.Size(150, 40);
            this.btnDeletePermanent.TabIndex = 2;
            this.btnDeletePermanent.Text = "Xóa vĩnh viễn";
            this.btnDeletePermanent.UseVisualStyleBackColor = false;
            // 
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.ColumnCount = 2;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel8.Controls.Add(this.btnRefreshQuarantine, 1, 0);
            this.tableLayoutPanel8.Controls.Add(this.tableLayoutPanel9, 0, 0);
            this.tableLayoutPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel8.Location = new System.Drawing.Point(839, 3);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 1;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel8.Size = new System.Drawing.Size(254, 40);
            this.tableLayoutPanel8.TabIndex = 1;
            // 
            // btnRefreshQuarantine
            // 
            this.btnRefreshQuarantine.BackColor = System.Drawing.Color.Transparent;
            this.btnRefreshQuarantine.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefreshQuarantine.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefreshQuarantine.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefreshQuarantine.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnRefreshQuarantine.Icon = null;
            this.btnRefreshQuarantine.IconGap = 8;
            this.btnRefreshQuarantine.Kind = ScanAndRemoveVirus.Control.UiButton.Variant.Primary;
            this.btnRefreshQuarantine.Location = new System.Drawing.Point(134, 0);
            this.btnRefreshQuarantine.Margin = new System.Windows.Forms.Padding(0);
            this.btnRefreshQuarantine.Name = "btnRefreshQuarantine";
            this.btnRefreshQuarantine.Radius = 8;
            this.btnRefreshQuarantine.Size = new System.Drawing.Size(120, 40);
            this.btnRefreshQuarantine.TabIndex = 0;
            this.btnRefreshQuarantine.Text = "Làm mới";
            this.btnRefreshQuarantine.UseVisualStyleBackColor = false;
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.ColumnCount = 2;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel9.Controls.Add(this.lblTotalFilesTitle, 0, 0);
            this.tableLayoutPanel9.Controls.Add(this.lblTotalFilesValue, 1, 0);
            this.tableLayoutPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel9.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 1;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel9.Size = new System.Drawing.Size(128, 34);
            this.tableLayoutPanel9.TabIndex = 1;
            this.tableLayoutPanel9.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel9_Paint);
            // 
            // lblTotalFilesTitle
            // 
            this.lblTotalFilesTitle.AutoSize = true;
            this.lblTotalFilesTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalFilesTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalFilesTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTotalFilesTitle.Name = "lblTotalFilesTitle";
            this.lblTotalFilesTitle.Size = new System.Drawing.Size(84, 34);
            this.lblTotalFilesTitle.TabIndex = 0;
            this.lblTotalFilesTitle.Text = "Tổng số tệp:";
            this.lblTotalFilesTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTotalFilesValue
            // 
            this.lblTotalFilesValue.AutoSize = true;
            this.lblTotalFilesValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalFilesValue.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalFilesValue.Location = new System.Drawing.Point(93, 0);
            this.lblTotalFilesValue.Name = "lblTotalFilesValue";
            this.lblTotalFilesValue.Size = new System.Drawing.Size(32, 34);
            this.lblTotalFilesValue.TabIndex = 1;
            this.lblTotalFilesValue.Text = "4";
            this.lblTotalFilesValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dgvQuarantine);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(10, 10);
            this.panel1.Margin = new System.Windows.Forms.Padding(10);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10);
            this.panel1.Size = new System.Drawing.Size(1096, 499);
            this.panel1.TabIndex = 0;
            // 
            // dgvQuarantine
            // 
            this.dgvQuarantine.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPick,
            this.colFileName,
            this.colOriginaPath,
            this.colThreatName,
            this.colDetectedTime,
            this.colFileSize});
            this.dgvQuarantine.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvQuarantine.Location = new System.Drawing.Point(10, 10);
            this.dgvQuarantine.Margin = new System.Windows.Forms.Padding(10);
            this.dgvQuarantine.Name = "dgvQuarantine";
            this.dgvQuarantine.Size = new System.Drawing.Size(1076, 479);
            this.dgvQuarantine.TabIndex = 4;
            // 
            // colPick
            // 
            this.colPick.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colPick.HeaderText = "";
            this.colPick.Name = "colPick";
            this.colPick.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.colPick.ToolTipText = "Nhấp để chọn / bỏ chọn tất cả";
            this.colPick.Width = 48;
            // 
            // colFileName
            // 
            this.colFileName.FillWeight = 16F;
            this.colFileName.HeaderText = "Tên tệp";
            this.colFileName.MinimumWidth = 10;
            this.colFileName.Name = "colFileName";
            this.colFileName.ReadOnly = true;
            // 
            // colOriginaPath
            // 
            this.colOriginaPath.FillWeight = 32F;
            this.colOriginaPath.HeaderText = "Đường dẫn gốc";
            this.colOriginaPath.MinimumWidth = 10;
            this.colOriginaPath.Name = "colOriginaPath";
            this.colOriginaPath.ReadOnly = true;
            // 
            // colThreatName
            // 
            this.colThreatName.FillWeight = 20F;
            this.colThreatName.HeaderText = "Mối đe dọa";
            this.colThreatName.MinimumWidth = 10;
            this.colThreatName.Name = "colThreatName";
            this.colThreatName.ReadOnly = true;
            // 
            // colDetectedTime
            // 
            this.colDetectedTime.FillWeight = 18F;
            this.colDetectedTime.HeaderText = "Thời gian phát hiện";
            this.colDetectedTime.MinimumWidth = 10;
            this.colDetectedTime.Name = "colDetectedTime";
            this.colDetectedTime.ReadOnly = true;
            // 
            // colFileSize
            // 
            this.colFileSize.FillWeight = 14F;
            this.colFileSize.HeaderText = "Kích thước";
            this.colFileSize.MinimumWidth = 10;
            this.colFileSize.Name = "colFileSize";
            this.colFileSize.ReadOnly = true;
            // 
            // grpQuarantineInfo
            // 
            this.grpQuarantineInfo.BackColor = System.Drawing.Color.Transparent;
            this.grpQuarantineInfo.Controls.Add(this.tableLayoutPanel10);
            this.grpQuarantineInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpQuarantineInfo.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.grpQuarantineInfo.Location = new System.Drawing.Point(3, 678);
            this.grpQuarantineInfo.Name = "grpQuarantineInfo";
            this.grpQuarantineInfo.Padding = new System.Windows.Forms.Padding(20, 62, 20, 16);
            this.grpQuarantineInfo.Size = new System.Drawing.Size(1162, 122);
            this.grpQuarantineInfo.TabIndex = 1;
            this.grpQuarantineInfo.TabStop = false;
            this.grpQuarantineInfo.Text = "Thông tin";
            // 
            // tableLayoutPanel10
            // 
            this.tableLayoutPanel10.ColumnCount = 1;
            this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel10.Controls.Add(this.lblInfo1, 0, 0);
            this.tableLayoutPanel10.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel10.Location = new System.Drawing.Point(20, 62);
            this.tableLayoutPanel10.Name = "tableLayoutPanel10";
            this.tableLayoutPanel10.RowCount = 2;
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.tableLayoutPanel10.Size = new System.Drawing.Size(1122, 44);
            this.tableLayoutPanel10.TabIndex = 0;
            // 
            // lblInfo1
            // 
            this.lblInfo1.AutoSize = true;
            this.lblInfo1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfo1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInfo1.Location = new System.Drawing.Point(3, 0);
            this.lblInfo1.Name = "lblInfo1";
            this.lblInfo1.Size = new System.Drawing.Size(1116, 22);
            this.lblInfo1.TabIndex = 0;
            this.lblInfo1.Text = "- \"Khôi phục\" thả tệp về vị trí cũ (còn khôi phục được); \"Xóa vĩnh viễn\" xóa hẳn " +
    "tệp đã chọn khỏi đĩa.";
            this.lblInfo1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1116, 22);
            this.label1.TabIndex = 1;
            this.label1.Text = "- Bạn có thể khôi phục nếu xác định đây là tệp an toàn.";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // UcCachLy
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "UcCachLy";
            this.Size = new System.Drawing.Size(1174, 829);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.grpQuarentineList.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel8.ResumeLayout(false);
            this.tableLayoutPanel9.ResumeLayout(false);
            this.tableLayoutPanel9.PerformLayout();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvQuarantine)).EndInit();
            this.grpQuarantineInfo.ResumeLayout(false);
            this.tableLayoutPanel10.ResumeLayout(false);
            this.tableLayoutPanel10.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private ScanAndRemoveVirus.Control.UiGroup grpQuarentineList;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView dgvQuarantine;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colPick;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private ScanAndRemoveVirus.Control.UiButton btnRestore;
        private ScanAndRemoveVirus.Control.UiButton btnRestoreAll;
        private ScanAndRemoveVirus.Control.UiButton btnDeletePermanent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOriginaPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn colThreatName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDetectedTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFileSize;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
        private ScanAndRemoveVirus.Control.UiButton btnRefreshQuarantine;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel9;
        private System.Windows.Forms.Label lblTotalFilesTitle;
        private System.Windows.Forms.Label lblTotalFilesValue;
        private ScanAndRemoveVirus.Control.UiGroup grpQuarantineInfo;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel10;
        private System.Windows.Forms.Label lblInfo1;
        private System.Windows.Forms.Label label1;
    }
}
