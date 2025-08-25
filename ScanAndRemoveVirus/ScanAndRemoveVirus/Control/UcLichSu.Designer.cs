namespace ScanAndRemoveVirus.Control
{
    partial class UcLichSu
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
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.lblHistoryTitle = new System.Windows.Forms.Label();
            this.lblHistorySubtitle = new System.Windows.Forms.Label();
            this.tabHistory = new System.Windows.Forms.TabControl();
            this.tabScanHistory = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScanType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScanLocation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colThreatCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.btnViewDetail = new System.Windows.Forms.Button();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.btnRefreshHistory = new System.Windows.Forms.Button();
            this.btnExportReport = new System.Windows.Forms.Button();
            this.tabThreatHistory = new System.Windows.Forms.TabPage();
            this.tabUpdateHistory = new System.Windows.Forms.TabPage();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvThreatHistory = new System.Windows.Forms.DataGridView();
            this.lblThreatListTitle = new System.Windows.Forms.Label();
            this.colDetectedTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colThreatName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colThreatType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFilePath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAction = new System.Windows.Forms.DataGridViewButtonColumn();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.grpFilter = new System.Windows.Forms.GroupBox();
            this.grpHistoryInfo = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.lblTimeFilter = new System.Windows.Forms.Label();
            this.lblThreatTypeFilter = new System.Windows.Forms.Label();
            this.lblStatusFilter = new System.Windows.Forms.Label();
            this.cboTimeFilter = new System.Windows.Forms.ComboBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.cboThreatTypeFilter = new System.Windows.Forms.ComboBox();
            this.panel5 = new System.Windows.Forms.Panel();
            this.cboStatusFilter = new System.Windows.Forms.ComboBox();
            this.btnApplyFilter = new System.Windows.Forms.Button();
            this.tableLayoutPanel10 = new System.Windows.Forms.TableLayoutPanel();
            this.lblTotalThreatTitle = new System.Windows.Forms.Label();
            this.lblQuarantinedTitle = new System.Windows.Forms.Label();
            this.lblDeletedTitle = new System.Windows.Forms.Label();
            this.lblUnhandledTitle = new System.Windows.Forms.Label();
            this.lblTotalThreatValue = new System.Windows.Forms.Label();
            this.lblQuarantinedValue = new System.Windows.Forms.Label();
            this.lblDeletedValue = new System.Windows.Forms.Label();
            this.lblUnhandledValue = new System.Windows.Forms.Label();
            this.tableLayoutPanel11 = new System.Windows.Forms.TableLayoutPanel();
            this.btnExportThreatReport = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tabHistory.SuspendLayout();
            this.tabScanHistory.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.tableLayoutPanel4.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.tabThreatHistory.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvThreatHistory)).BeginInit();
            this.tableLayoutPanel8.SuspendLayout();
            this.grpFilter.SuspendLayout();
            this.grpHistoryInfo.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel5.SuspendLayout();
            this.tableLayoutPanel10.SuspendLayout();
            this.tableLayoutPanel11.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tabHistory, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.71429F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 89.28571F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1174, 829);
            this.tableLayoutPanel1.TabIndex = 0;
            this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.lblHistoryTitle, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblHistorySubtitle, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(1168, 82);
            this.tableLayoutPanel2.TabIndex = 0;
            this.tableLayoutPanel2.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel2_Paint);
            // 
            // lblHistoryTitle
            // 
            this.lblHistoryTitle.AutoSize = true;
            this.lblHistoryTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHistoryTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHistoryTitle.Location = new System.Drawing.Point(3, 0);
            this.lblHistoryTitle.Name = "lblHistoryTitle";
            this.lblHistoryTitle.Size = new System.Drawing.Size(1162, 41);
            this.lblHistoryTitle.TabIndex = 0;
            this.lblHistoryTitle.Text = "Lịch sử";
            this.lblHistoryTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHistorySubtitle
            // 
            this.lblHistorySubtitle.AutoSize = true;
            this.lblHistorySubtitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHistorySubtitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHistorySubtitle.Location = new System.Drawing.Point(3, 41);
            this.lblHistorySubtitle.Name = "lblHistorySubtitle";
            this.lblHistorySubtitle.Size = new System.Drawing.Size(1162, 41);
            this.lblHistorySubtitle.TabIndex = 1;
            this.lblHistorySubtitle.Text = "Xem lại hoạt động quét, phát hiện mối đe dọa và cập nhập hệ thống.";
            this.lblHistorySubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabHistory
            // 
            this.tabHistory.Controls.Add(this.tabScanHistory);
            this.tabHistory.Controls.Add(this.tabThreatHistory);
            this.tabHistory.Controls.Add(this.tabUpdateHistory);
            this.tabHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabHistory.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabHistory.Location = new System.Drawing.Point(3, 91);
            this.tabHistory.Name = "tabHistory";
            this.tabHistory.SelectedIndex = 0;
            this.tabHistory.Size = new System.Drawing.Size(1168, 735);
            this.tabHistory.TabIndex = 1;
            // 
            // tabScanHistory
            // 
            this.tabScanHistory.Controls.Add(this.panel1);
            this.tabScanHistory.Location = new System.Drawing.Point(4, 26);
            this.tabScanHistory.Name = "tabScanHistory";
            this.tabScanHistory.Padding = new System.Windows.Forms.Padding(3);
            this.tabScanHistory.Size = new System.Drawing.Size(1160, 705);
            this.tabScanHistory.TabIndex = 0;
            this.tabScanHistory.Text = "Lịch sử quét";
            this.tabScanHistory.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.tableLayoutPanel3);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Margin = new System.Windows.Forms.Padding(5);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10);
            this.panel1.Size = new System.Drawing.Size(1154, 699);
            this.panel1.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.dgvHistory, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.tableLayoutPanel4, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(10, 10);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 85F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(1134, 679);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // dgvHistory
            // 
            this.dgvHistory.AllowUserToAddRows = false;
            this.dgvHistory.AllowUserToDeleteRows = false;
            this.dgvHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvHistory.BackgroundColor = System.Drawing.Color.White;
            this.dgvHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTime,
            this.colScanType,
            this.colScanLocation,
            this.colResult,
            this.colThreatCount,
            this.colDuration});
            this.dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistory.Location = new System.Drawing.Point(10, 10);
            this.dgvHistory.Margin = new System.Windows.Forms.Padding(10);
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.ReadOnly = true;
            this.dgvHistory.RowHeadersVisible = false;
            this.dgvHistory.RowHeadersWidth = 82;
            this.dgvHistory.RowTemplate.Height = 33;
            this.dgvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHistory.Size = new System.Drawing.Size(1114, 557);
            this.dgvHistory.TabIndex = 3;
            // 
            // colTime
            // 
            this.colTime.HeaderText = "Thời gian";
            this.colTime.MinimumWidth = 10;
            this.colTime.Name = "colTime";
            this.colTime.ReadOnly = true;
            // 
            // colScanType
            // 
            this.colScanType.HeaderText = "Loại quét";
            this.colScanType.MinimumWidth = 10;
            this.colScanType.Name = "colScanType";
            this.colScanType.ReadOnly = true;
            // 
            // colScanLocation
            // 
            this.colScanLocation.HeaderText = "Vị trí quét";
            this.colScanLocation.MinimumWidth = 10;
            this.colScanLocation.Name = "colScanLocation";
            this.colScanLocation.ReadOnly = true;
            // 
            // colResult
            // 
            this.colResult.HeaderText = "Kết quả";
            this.colResult.Name = "colResult";
            this.colResult.ReadOnly = true;
            // 
            // colThreatCount
            // 
            this.colThreatCount.HeaderText = "Số mối đe dọa";
            this.colThreatCount.Name = "colThreatCount";
            this.colThreatCount.ReadOnly = true;
            // 
            // colDuration
            // 
            this.colDuration.HeaderText = "Thời gian quét";
            this.colDuration.Name = "colDuration";
            this.colDuration.ReadOnly = true;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Controls.Add(this.tableLayoutPanel5, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.tableLayoutPanel6, 1, 1);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 580);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(1128, 96);
            this.tableLayoutPanel4.TabIndex = 2;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 4;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel5.Controls.Add(this.btnViewDetail, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 23);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(558, 70);
            this.tableLayoutPanel5.TabIndex = 0;
            // 
            // btnViewDetail
            // 
            this.btnViewDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnViewDetail.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnViewDetail.Location = new System.Drawing.Point(3, 3);
            this.btnViewDetail.Name = "btnViewDetail";
            this.btnViewDetail.Size = new System.Drawing.Size(133, 64);
            this.btnViewDetail.TabIndex = 0;
            this.btnViewDetail.Text = "Xem chi tiết";
            this.btnViewDetail.UseVisualStyleBackColor = true;
            this.btnViewDetail.Click += new System.EventHandler(this.btnViewDetail_Click);
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 4;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel6.Controls.Add(this.btnRefreshHistory, 3, 0);
            this.tableLayoutPanel6.Controls.Add(this.btnExportReport, 1, 0);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(567, 23);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 1;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(558, 70);
            this.tableLayoutPanel6.TabIndex = 1;
            // 
            // btnRefreshHistory
            // 
            this.btnRefreshHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefreshHistory.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRefreshHistory.Location = new System.Drawing.Point(381, 3);
            this.btnRefreshHistory.Name = "btnRefreshHistory";
            this.btnRefreshHistory.Size = new System.Drawing.Size(174, 64);
            this.btnRefreshHistory.TabIndex = 0;
            this.btnRefreshHistory.Text = "Làm mới";
            this.btnRefreshHistory.UseVisualStyleBackColor = true;
            // 
            // btnExportReport
            // 
            this.btnExportReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExportReport.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportReport.Location = new System.Drawing.Point(182, 3);
            this.btnExportReport.Name = "btnExportReport";
            this.btnExportReport.Size = new System.Drawing.Size(173, 64);
            this.btnExportReport.TabIndex = 1;
            this.btnExportReport.Text = "Xuất báo cáo";
            this.btnExportReport.UseVisualStyleBackColor = true;
            // 
            // tabThreatHistory
            // 
            this.tabThreatHistory.Controls.Add(this.panel2);
            this.tabThreatHistory.Location = new System.Drawing.Point(4, 26);
            this.tabThreatHistory.Name = "tabThreatHistory";
            this.tabThreatHistory.Padding = new System.Windows.Forms.Padding(3);
            this.tabThreatHistory.Size = new System.Drawing.Size(1160, 705);
            this.tabThreatHistory.TabIndex = 1;
            this.tabThreatHistory.Text = "Lịch sử phát hiện";
            this.tabThreatHistory.UseVisualStyleBackColor = true;
            // 
            // tabUpdateHistory
            // 
            this.tabUpdateHistory.Location = new System.Drawing.Point(4, 26);
            this.tabUpdateHistory.Name = "tabUpdateHistory";
            this.tabUpdateHistory.Size = new System.Drawing.Size(1160, 686);
            this.tabUpdateHistory.TabIndex = 2;
            this.tabUpdateHistory.Text = "Lịch sử cập nhập";
            this.tabUpdateHistory.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.White;
            this.panel2.Controls.Add(this.tableLayoutPanel7);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panel2.Location = new System.Drawing.Point(3, 3);
            this.panel2.Margin = new System.Windows.Forms.Padding(5);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(10);
            this.panel2.Size = new System.Drawing.Size(1154, 699);
            this.panel2.TabIndex = 1;
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 1;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.Controls.Add(this.dgvThreatHistory, 0, 1);
            this.tableLayoutPanel7.Controls.Add(this.lblThreatListTitle, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.tableLayoutPanel8, 0, 2);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(10, 10);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 3;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(1134, 679);
            this.tableLayoutPanel7.TabIndex = 0;
            // 
            // dgvThreatHistory
            // 
            this.dgvThreatHistory.AllowUserToAddRows = false;
            this.dgvThreatHistory.AllowUserToDeleteRows = false;
            this.dgvThreatHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvThreatHistory.BackgroundColor = System.Drawing.Color.White;
            this.dgvThreatHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvThreatHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvThreatHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colDetectedTime,
            this.colThreatName,
            this.colThreatType,
            this.colFilePath,
            this.colStatus,
            this.colAction});
            this.dgvThreatHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvThreatHistory.Location = new System.Drawing.Point(10, 30);
            this.dgvThreatHistory.Margin = new System.Windows.Forms.Padding(10);
            this.dgvThreatHistory.Name = "dgvThreatHistory";
            this.dgvThreatHistory.ReadOnly = true;
            this.dgvThreatHistory.RowHeadersVisible = false;
            this.dgvThreatHistory.RowHeadersWidth = 82;
            this.dgvThreatHistory.RowTemplate.Height = 33;
            this.dgvThreatHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvThreatHistory.Size = new System.Drawing.Size(1114, 441);
            this.dgvThreatHistory.TabIndex = 3;
            // 
            // lblThreatListTitle
            // 
            this.lblThreatListTitle.AutoSize = true;
            this.lblThreatListTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThreatListTitle.Location = new System.Drawing.Point(3, 0);
            this.lblThreatListTitle.Name = "lblThreatListTitle";
            this.lblThreatListTitle.Size = new System.Drawing.Size(1128, 20);
            this.lblThreatListTitle.TabIndex = 4;
            this.lblThreatListTitle.Text = "Danh sách các mối đe dọa đã được phát hiện";
            this.lblThreatListTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // colDetectedTime
            // 
            this.colDetectedTime.HeaderText = "Thời gian phát hiện";
            this.colDetectedTime.Name = "colDetectedTime";
            this.colDetectedTime.ReadOnly = true;
            // 
            // colThreatName
            // 
            this.colThreatName.HeaderText = "Tên mối đe dọa";
            this.colThreatName.Name = "colThreatName";
            this.colThreatName.ReadOnly = true;
            // 
            // colThreatType
            // 
            this.colThreatType.HeaderText = "Loại mối đe dọa";
            this.colThreatType.Name = "colThreatType";
            this.colThreatType.ReadOnly = true;
            // 
            // colFilePath
            // 
            this.colFilePath.HeaderText = "Vị trí (Đường dẫn)";
            this.colFilePath.Name = "colFilePath";
            this.colFilePath.ReadOnly = true;
            // 
            // colStatus
            // 
            this.colStatus.HeaderText = "Trạng thái xử lý";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            // 
            // colAction
            // 
            this.colAction.HeaderText = "Hành động";
            this.colAction.Name = "colAction";
            this.colAction.ReadOnly = true;
            this.colAction.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colAction.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colAction.Text = "Xem chi tiết";
            this.colAction.UseColumnTextForButtonValue = true;
            // 
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.ColumnCount = 3;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel8.Controls.Add(this.grpFilter, 0, 0);
            this.tableLayoutPanel8.Controls.Add(this.grpHistoryInfo, 1, 0);
            this.tableLayoutPanel8.Controls.Add(this.tableLayoutPanel11, 2, 0);
            this.tableLayoutPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel8.Location = new System.Drawing.Point(3, 484);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 1;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel8.Size = new System.Drawing.Size(1128, 192);
            this.tableLayoutPanel8.TabIndex = 5;
            // 
            // grpFilter
            // 
            this.grpFilter.Controls.Add(this.tableLayoutPanel9);
            this.grpFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpFilter.Location = new System.Drawing.Point(3, 3);
            this.grpFilter.Name = "grpFilter";
            this.grpFilter.Size = new System.Drawing.Size(370, 186);
            this.grpFilter.TabIndex = 0;
            this.grpFilter.TabStop = false;
            this.grpFilter.Text = "Bộ lọc";
            // 
            // grpHistoryInfo
            // 
            this.grpHistoryInfo.Controls.Add(this.tableLayoutPanel10);
            this.grpHistoryInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpHistoryInfo.Location = new System.Drawing.Point(379, 3);
            this.grpHistoryInfo.Name = "grpHistoryInfo";
            this.grpHistoryInfo.Size = new System.Drawing.Size(370, 186);
            this.grpHistoryInfo.TabIndex = 1;
            this.grpHistoryInfo.TabStop = false;
            this.grpHistoryInfo.Text = "Thông tin";
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.ColumnCount = 4;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel9.Controls.Add(this.btnApplyFilter, 1, 3);
            this.tableLayoutPanel9.Controls.Add(this.panel5, 2, 2);
            this.tableLayoutPanel9.Controls.Add(this.panel4, 2, 1);
            this.tableLayoutPanel9.Controls.Add(this.lblTimeFilter, 1, 0);
            this.tableLayoutPanel9.Controls.Add(this.lblThreatTypeFilter, 1, 1);
            this.tableLayoutPanel9.Controls.Add(this.lblStatusFilter, 1, 2);
            this.tableLayoutPanel9.Controls.Add(this.panel3, 2, 0);
            this.tableLayoutPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel9.Location = new System.Drawing.Point(3, 21);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 4;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel9.Size = new System.Drawing.Size(364, 162);
            this.tableLayoutPanel9.TabIndex = 0;
            this.tableLayoutPanel9.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel9_Paint);
            // 
            // lblTimeFilter
            // 
            this.lblTimeFilter.AutoSize = true;
            this.lblTimeFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTimeFilter.Location = new System.Drawing.Point(23, 0);
            this.lblTimeFilter.Name = "lblTimeFilter";
            this.lblTimeFilter.Size = new System.Drawing.Size(156, 40);
            this.lblTimeFilter.TabIndex = 0;
            this.lblTimeFilter.Text = "Khoảng thời gian:";
            this.lblTimeFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTimeFilter.Click += new System.EventHandler(this.lblTimeFilter_Click);
            // 
            // lblThreatTypeFilter
            // 
            this.lblThreatTypeFilter.AutoSize = true;
            this.lblThreatTypeFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThreatTypeFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThreatTypeFilter.Location = new System.Drawing.Point(23, 40);
            this.lblThreatTypeFilter.Name = "lblThreatTypeFilter";
            this.lblThreatTypeFilter.Size = new System.Drawing.Size(156, 40);
            this.lblThreatTypeFilter.TabIndex = 1;
            this.lblThreatTypeFilter.Text = "Loại mối đe dọa:";
            this.lblThreatTypeFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStatusFilter
            // 
            this.lblStatusFilter.AutoSize = true;
            this.lblStatusFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatusFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatusFilter.Location = new System.Drawing.Point(23, 80);
            this.lblStatusFilter.Name = "lblStatusFilter";
            this.lblStatusFilter.Size = new System.Drawing.Size(156, 40);
            this.lblStatusFilter.TabIndex = 2;
            this.lblStatusFilter.Text = "Trạng thái xử lý:";
            this.lblStatusFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboTimeFilter
            // 
            this.cboTimeFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboTimeFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTimeFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboTimeFilter.FormattingEnabled = true;
            this.cboTimeFilter.Items.AddRange(new object[] {
            "7 ngày qua",
            "30 ngày qua",
            "90 ngày qua",
            "Tất cả"});
            this.cboTimeFilter.Location = new System.Drawing.Point(2, 2);
            this.cboTimeFilter.Name = "cboTimeFilter";
            this.cboTimeFilter.Size = new System.Drawing.Size(152, 25);
            this.cboTimeFilter.TabIndex = 3;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.cboTimeFilter);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(185, 3);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(2);
            this.panel3.Size = new System.Drawing.Size(156, 34);
            this.panel3.TabIndex = 3;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.cboThreatTypeFilter);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(185, 43);
            this.panel4.Name = "panel4";
            this.panel4.Padding = new System.Windows.Forms.Padding(2);
            this.panel4.Size = new System.Drawing.Size(156, 34);
            this.panel4.TabIndex = 4;
            // 
            // cboThreatTypeFilter
            // 
            this.cboThreatTypeFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboThreatTypeFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboThreatTypeFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboThreatTypeFilter.FormattingEnabled = true;
            this.cboThreatTypeFilter.Items.AddRange(new object[] {
            "Tất cả",
            "Trojan",
            "Worm",
            "Adware",
            "HackTool",
            "PUA",
            "Riskware"});
            this.cboThreatTypeFilter.Location = new System.Drawing.Point(2, 2);
            this.cboThreatTypeFilter.Name = "cboThreatTypeFilter";
            this.cboThreatTypeFilter.Size = new System.Drawing.Size(152, 25);
            this.cboThreatTypeFilter.TabIndex = 3;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.cboStatusFilter);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel5.Location = new System.Drawing.Point(185, 83);
            this.panel5.Name = "panel5";
            this.panel5.Padding = new System.Windows.Forms.Padding(2);
            this.panel5.Size = new System.Drawing.Size(156, 34);
            this.panel5.TabIndex = 4;
            // 
            // cboStatusFilter
            // 
            this.cboStatusFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboStatusFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStatusFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboStatusFilter.FormattingEnabled = true;
            this.cboStatusFilter.Items.AddRange(new object[] {
            "Tất cả",
            "Đã cách ly",
            "Đã xóa",
            "Chưa xử lý"});
            this.cboStatusFilter.Location = new System.Drawing.Point(2, 2);
            this.cboStatusFilter.Name = "cboStatusFilter";
            this.cboStatusFilter.Size = new System.Drawing.Size(152, 25);
            this.cboStatusFilter.TabIndex = 3;
            // 
            // btnApplyFilter
            // 
            this.tableLayoutPanel9.SetColumnSpan(this.btnApplyFilter, 2);
            this.btnApplyFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnApplyFilter.Location = new System.Drawing.Point(23, 123);
            this.btnApplyFilter.Name = "btnApplyFilter";
            this.btnApplyFilter.Size = new System.Drawing.Size(318, 36);
            this.btnApplyFilter.TabIndex = 5;
            this.btnApplyFilter.Text = "Áp dụng bộ lọc";
            this.btnApplyFilter.UseVisualStyleBackColor = true;
            this.btnApplyFilter.Click += new System.EventHandler(this.btnApplyFilter_Click);
            // 
            // tableLayoutPanel10
            // 
            this.tableLayoutPanel10.ColumnCount = 4;
            this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel10.Controls.Add(this.lblTotalThreatTitle, 1, 0);
            this.tableLayoutPanel10.Controls.Add(this.lblQuarantinedTitle, 1, 1);
            this.tableLayoutPanel10.Controls.Add(this.lblDeletedTitle, 1, 2);
            this.tableLayoutPanel10.Controls.Add(this.lblUnhandledTitle, 1, 3);
            this.tableLayoutPanel10.Controls.Add(this.lblTotalThreatValue, 2, 0);
            this.tableLayoutPanel10.Controls.Add(this.lblQuarantinedValue, 2, 1);
            this.tableLayoutPanel10.Controls.Add(this.lblDeletedValue, 2, 2);
            this.tableLayoutPanel10.Controls.Add(this.lblUnhandledValue, 2, 3);
            this.tableLayoutPanel10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel10.Location = new System.Drawing.Point(3, 21);
            this.tableLayoutPanel10.Name = "tableLayoutPanel10";
            this.tableLayoutPanel10.RowCount = 4;
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel10.Size = new System.Drawing.Size(364, 162);
            this.tableLayoutPanel10.TabIndex = 0;
            this.tableLayoutPanel10.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel10_Paint);
            // 
            // lblTotalThreatTitle
            // 
            this.lblTotalThreatTitle.AutoSize = true;
            this.lblTotalThreatTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalThreatTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalThreatTitle.Location = new System.Drawing.Point(23, 0);
            this.lblTotalThreatTitle.Name = "lblTotalThreatTitle";
            this.lblTotalThreatTitle.Size = new System.Drawing.Size(237, 40);
            this.lblTotalThreatTitle.TabIndex = 0;
            this.lblTotalThreatTitle.Text = "Tổng số mối đe dọa đã phát hiện:";
            this.lblTotalThreatTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblQuarantinedTitle
            // 
            this.lblQuarantinedTitle.AutoSize = true;
            this.lblQuarantinedTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQuarantinedTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblQuarantinedTitle.Location = new System.Drawing.Point(23, 40);
            this.lblQuarantinedTitle.Name = "lblQuarantinedTitle";
            this.lblQuarantinedTitle.Size = new System.Drawing.Size(237, 40);
            this.lblQuarantinedTitle.TabIndex = 1;
            this.lblQuarantinedTitle.Text = "Đã cách ly:";
            this.lblQuarantinedTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDeletedTitle
            // 
            this.lblDeletedTitle.AutoSize = true;
            this.lblDeletedTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeletedTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDeletedTitle.Location = new System.Drawing.Point(23, 80);
            this.lblDeletedTitle.Name = "lblDeletedTitle";
            this.lblDeletedTitle.Size = new System.Drawing.Size(237, 40);
            this.lblDeletedTitle.TabIndex = 2;
            this.lblDeletedTitle.Text = "Đã xóa:";
            this.lblDeletedTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblUnhandledTitle
            // 
            this.lblUnhandledTitle.AutoSize = true;
            this.lblUnhandledTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUnhandledTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUnhandledTitle.Location = new System.Drawing.Point(23, 120);
            this.lblUnhandledTitle.Name = "lblUnhandledTitle";
            this.lblUnhandledTitle.Size = new System.Drawing.Size(237, 42);
            this.lblUnhandledTitle.TabIndex = 3;
            this.lblUnhandledTitle.Text = "Chưa xử lý:";
            this.lblUnhandledTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTotalThreatValue
            // 
            this.lblTotalThreatValue.AutoSize = true;
            this.lblTotalThreatValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalThreatValue.Location = new System.Drawing.Point(266, 0);
            this.lblTotalThreatValue.Name = "lblTotalThreatValue";
            this.lblTotalThreatValue.Size = new System.Drawing.Size(75, 40);
            this.lblTotalThreatValue.TabIndex = 4;
            this.lblTotalThreatValue.Text = "8";
            this.lblTotalThreatValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblQuarantinedValue
            // 
            this.lblQuarantinedValue.AutoSize = true;
            this.lblQuarantinedValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQuarantinedValue.Location = new System.Drawing.Point(266, 40);
            this.lblQuarantinedValue.Name = "lblQuarantinedValue";
            this.lblQuarantinedValue.Size = new System.Drawing.Size(75, 40);
            this.lblQuarantinedValue.TabIndex = 5;
            this.lblQuarantinedValue.Text = "5";
            this.lblQuarantinedValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDeletedValue
            // 
            this.lblDeletedValue.AutoSize = true;
            this.lblDeletedValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeletedValue.Location = new System.Drawing.Point(266, 80);
            this.lblDeletedValue.Name = "lblDeletedValue";
            this.lblDeletedValue.Size = new System.Drawing.Size(75, 40);
            this.lblDeletedValue.TabIndex = 6;
            this.lblDeletedValue.Text = "3";
            this.lblDeletedValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblUnhandledValue
            // 
            this.lblUnhandledValue.AutoSize = true;
            this.lblUnhandledValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUnhandledValue.Location = new System.Drawing.Point(266, 120);
            this.lblUnhandledValue.Name = "lblUnhandledValue";
            this.lblUnhandledValue.Size = new System.Drawing.Size(75, 42);
            this.lblUnhandledValue.TabIndex = 7;
            this.lblUnhandledValue.Text = "0";
            this.lblUnhandledValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel11
            // 
            this.tableLayoutPanel11.ColumnCount = 2;
            this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel11.Controls.Add(this.btnExportThreatReport, 1, 2);
            this.tableLayoutPanel11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel11.Location = new System.Drawing.Point(755, 3);
            this.tableLayoutPanel11.Name = "tableLayoutPanel11";
            this.tableLayoutPanel11.RowCount = 3;
            this.tableLayoutPanel11.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel11.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel11.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel11.Size = new System.Drawing.Size(370, 186);
            this.tableLayoutPanel11.TabIndex = 2;
            // 
            // btnExportThreatReport
            // 
            this.btnExportThreatReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExportThreatReport.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportThreatReport.Location = new System.Drawing.Point(151, 127);
            this.btnExportThreatReport.Name = "btnExportThreatReport";
            this.btnExportThreatReport.Size = new System.Drawing.Size(216, 56);
            this.btnExportThreatReport.TabIndex = 0;
            this.btnExportThreatReport.Text = "Xuất báo cáo";
            this.btnExportThreatReport.UseVisualStyleBackColor = true;
            // 
            // UcLichSu
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "UcLichSu";
            this.Size = new System.Drawing.Size(1174, 829);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tabHistory.ResumeLayout(false);
            this.tabScanHistory.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tabThreatHistory.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvThreatHistory)).EndInit();
            this.tableLayoutPanel8.ResumeLayout(false);
            this.grpFilter.ResumeLayout(false);
            this.grpHistoryInfo.ResumeLayout(false);
            this.tableLayoutPanel9.ResumeLayout(false);
            this.tableLayoutPanel9.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.tableLayoutPanel10.ResumeLayout(false);
            this.tableLayoutPanel10.PerformLayout();
            this.tableLayoutPanel11.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label lblHistoryTitle;
        private System.Windows.Forms.Label lblHistorySubtitle;
        private System.Windows.Forms.TabControl tabHistory;
        private System.Windows.Forms.TabPage tabScanHistory;
        private System.Windows.Forms.TabPage tabThreatHistory;
        private System.Windows.Forms.TabPage tabUpdateHistory;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Button btnViewDetail;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.Button btnRefreshHistory;
        private System.Windows.Forms.Button btnExportReport;
        private System.Windows.Forms.DataGridView dgvHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScanType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScanLocation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn colThreatCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDuration;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.DataGridView dgvThreatHistory;
        private System.Windows.Forms.Label lblThreatListTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDetectedTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colThreatName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colThreatType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFilePath;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.DataGridViewButtonColumn colAction;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
        private System.Windows.Forms.GroupBox grpFilter;
        private System.Windows.Forms.GroupBox grpHistoryInfo;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel9;
        private System.Windows.Forms.Label lblTimeFilter;
        private System.Windows.Forms.Label lblThreatTypeFilter;
        private System.Windows.Forms.Label lblStatusFilter;
        private System.Windows.Forms.ComboBox cboTimeFilter;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.ComboBox cboThreatTypeFilter;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.ComboBox cboStatusFilter;
        private System.Windows.Forms.Button btnApplyFilter;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel10;
        private System.Windows.Forms.Label lblTotalThreatTitle;
        private System.Windows.Forms.Label lblQuarantinedTitle;
        private System.Windows.Forms.Label lblDeletedTitle;
        private System.Windows.Forms.Label lblUnhandledTitle;
        private System.Windows.Forms.Label lblTotalThreatValue;
        private System.Windows.Forms.Label lblQuarantinedValue;
        private System.Windows.Forms.Label lblDeletedValue;
        private System.Windows.Forms.Label lblUnhandledValue;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel11;
        private System.Windows.Forms.Button btnExportThreatReport;
    }
}
