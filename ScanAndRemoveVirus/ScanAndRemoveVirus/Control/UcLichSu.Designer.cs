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
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.71429F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 89.28572F));
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
            this.tableLayoutPanel2.Size = new System.Drawing.Size(1168, 80);
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
            this.lblHistoryTitle.Size = new System.Drawing.Size(1162, 40);
            this.lblHistoryTitle.TabIndex = 0;
            this.lblHistoryTitle.Text = "Lịch sử";
            this.lblHistoryTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHistorySubtitle
            // 
            this.lblHistorySubtitle.AutoSize = true;
            this.lblHistorySubtitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHistorySubtitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHistorySubtitle.Location = new System.Drawing.Point(3, 40);
            this.lblHistorySubtitle.Name = "lblHistorySubtitle";
            this.lblHistorySubtitle.Size = new System.Drawing.Size(1162, 40);
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
            this.tabHistory.Location = new System.Drawing.Point(3, 89);
            this.tabHistory.Name = "tabHistory";
            this.tabHistory.SelectedIndex = 0;
            this.tabHistory.Size = new System.Drawing.Size(1168, 716);
            this.tabHistory.TabIndex = 1;
            // 
            // tabScanHistory
            // 
            this.tabScanHistory.Controls.Add(this.panel1);
            this.tabScanHistory.Location = new System.Drawing.Point(4, 26);
            this.tabScanHistory.Name = "tabScanHistory";
            this.tabScanHistory.Padding = new System.Windows.Forms.Padding(3);
            this.tabScanHistory.Size = new System.Drawing.Size(1160, 686);
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
            this.panel1.Size = new System.Drawing.Size(1154, 680);
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
            this.tableLayoutPanel3.Size = new System.Drawing.Size(1134, 660);
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
            this.dgvHistory.Size = new System.Drawing.Size(1114, 541);
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
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 564);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(1128, 93);
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
            this.tableLayoutPanel5.Size = new System.Drawing.Size(558, 67);
            this.tableLayoutPanel5.TabIndex = 0;
            // 
            // btnViewDetail
            // 
            this.btnViewDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnViewDetail.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnViewDetail.Location = new System.Drawing.Point(3, 3);
            this.btnViewDetail.Name = "btnViewDetail";
            this.btnViewDetail.Size = new System.Drawing.Size(133, 61);
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
            this.tableLayoutPanel6.Size = new System.Drawing.Size(558, 67);
            this.tableLayoutPanel6.TabIndex = 1;
            // 
            // btnRefreshHistory
            // 
            this.btnRefreshHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefreshHistory.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRefreshHistory.Location = new System.Drawing.Point(381, 3);
            this.btnRefreshHistory.Name = "btnRefreshHistory";
            this.btnRefreshHistory.Size = new System.Drawing.Size(174, 61);
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
            this.btnExportReport.Size = new System.Drawing.Size(173, 61);
            this.btnExportReport.TabIndex = 1;
            this.btnExportReport.Text = "Xuất báo cáo";
            this.btnExportReport.UseVisualStyleBackColor = true;
            // 
            // tabThreatHistory
            // 
            this.tabThreatHistory.Location = new System.Drawing.Point(4, 26);
            this.tabThreatHistory.Name = "tabThreatHistory";
            this.tabThreatHistory.Padding = new System.Windows.Forms.Padding(3);
            this.tabThreatHistory.Size = new System.Drawing.Size(1160, 686);
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
    }
}
