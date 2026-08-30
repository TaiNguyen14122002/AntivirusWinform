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
            this.panel1 = new System.Windows.Forms.Panel();
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.tlpFilter = new System.Windows.Forms.TableLayoutPanel();
            this.btnFilterAll = new System.Windows.Forms.Button();
            this.btnFilterThreats = new System.Windows.Forms.Button();
            this.btnFilterUpdates = new System.Windows.Forms.Button();
            this.lblStats = new System.Windows.Forms.Label();
            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.colPick = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScanType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScanLocation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colThreatCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tlpToolbar = new System.Windows.Forms.TableLayoutPanel();
            this.lblSelection = new System.Windows.Forms.Label();
            this.btnViewDetail = new System.Windows.Forms.Button();
            this.btnDeleteHistory = new System.Windows.Forms.Button();
            this.btnExportReport = new System.Windows.Forms.Button();
            this.btnRefreshHistory = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tlpMain.SuspendLayout();
            this.tlpFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.tlpToolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 86F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1174, 829);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.lblHistoryTitle, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblHistorySubtitle, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(1174, 86);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // lblHistoryTitle
            // 
            this.lblHistoryTitle.AutoSize = true;
            this.lblHistoryTitle.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblHistoryTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHistoryTitle.ForeColor = ScanAndRemoveVirus.Control.Theme.TextDark;
            this.lblHistoryTitle.Location = new System.Drawing.Point(0, 0);
            this.lblHistoryTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblHistoryTitle.Name = "lblHistoryTitle";
            this.lblHistoryTitle.Size = new System.Drawing.Size(100, 44);
            this.lblHistoryTitle.TabIndex = 0;
            this.lblHistoryTitle.Text = "Lịch sử";
            this.lblHistoryTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHistorySubtitle
            // 
            this.lblHistorySubtitle.AutoSize = true;
            this.lblHistorySubtitle.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblHistorySubtitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHistorySubtitle.ForeColor = ScanAndRemoveVirus.Control.Theme.TextGray;
            this.lblHistorySubtitle.Location = new System.Drawing.Point(0, 44);
            this.lblHistorySubtitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblHistorySubtitle.Name = "lblHistorySubtitle";
            this.lblHistorySubtitle.Size = new System.Drawing.Size(500, 28);
            this.lblHistorySubtitle.TabIndex = 1;
            this.lblHistorySubtitle.Text = "Xem lại hoạt động quét, phát hiện mối đe dọa và cập nhật hệ thống — Tích ô \"Chọn\" để xóa nhiều dòng.";
            this.lblHistorySubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.tlpMain);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 89);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10);
            this.panel1.Size = new System.Drawing.Size(1168, 737);
            this.panel1.TabIndex = 1;
            // 
            // tlpMain
            // 
            this.tlpMain.ColumnCount = 1;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.Controls.Add(this.tlpFilter, 0, 0);
            this.tlpMain.Controls.Add(this.dgvHistory, 0, 1);
            this.tlpMain.Controls.Add(this.tlpToolbar, 0, 2);
            this.tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMain.Location = new System.Drawing.Point(10, 10);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 3;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.tlpMain.Size = new System.Drawing.Size(1148, 717);
            this.tlpMain.TabIndex = 0;
            // 
            // tlpFilter
            // 
            this.tlpFilter.ColumnCount = 7;
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.tlpFilter.Controls.Add(this.btnFilterAll, 0, 0);
            this.tlpFilter.Controls.Add(this.btnFilterThreats, 2, 0);
            this.tlpFilter.Controls.Add(this.btnFilterUpdates, 4, 0);
            this.tlpFilter.Controls.Add(this.lblStats, 5, 0);
            this.tlpFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpFilter.Location = new System.Drawing.Point(0, 0);
            this.tlpFilter.Margin = new System.Windows.Forms.Padding(0);
            this.tlpFilter.Name = "tlpFilter";
            this.tlpFilter.RowCount = 1;
            this.tlpFilter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpFilter.Size = new System.Drawing.Size(1148, 44);
            this.tlpFilter.TabIndex = 0;
            // 
            // btnFilterAll
            // 
            this.btnFilterAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFilterAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterAll.Location = new System.Drawing.Point(2, 7);
            this.btnFilterAll.Margin = new System.Windows.Forms.Padding(2, 7, 2, 7);
            this.btnFilterAll.Name = "btnFilterAll";
            this.btnFilterAll.Size = new System.Drawing.Size(126, 30);
            this.btnFilterAll.TabIndex = 0;
            this.btnFilterAll.Text = "Mọi phiên quét";
            this.btnFilterAll.UseVisualStyleBackColor = true;
            // 
            // btnFilterThreats
            // 
            this.btnFilterThreats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFilterThreats.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterThreats.Location = new System.Drawing.Point(142, 7);
            this.btnFilterThreats.Margin = new System.Windows.Forms.Padding(2, 7, 2, 7);
            this.btnFilterThreats.Name = "btnFilterThreats";
            this.btnFilterThreats.Size = new System.Drawing.Size(146, 30);
            this.btnFilterThreats.TabIndex = 1;
            this.btnFilterThreats.Text = "Có đe dọa";
            this.btnFilterThreats.UseVisualStyleBackColor = true;
            // 
            // btnFilterUpdates
            // 
            this.btnFilterUpdates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFilterUpdates.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterUpdates.Location = new System.Drawing.Point(302, 7);
            this.btnFilterUpdates.Margin = new System.Windows.Forms.Padding(2, 7, 2, 7);
            this.btnFilterUpdates.Name = "btnFilterUpdates";
            this.btnFilterUpdates.Size = new System.Drawing.Size(136, 30);
            this.btnFilterUpdates.TabIndex = 2;
            this.btnFilterUpdates.Text = "Cập nhật CSDL";
            this.btnFilterUpdates.UseVisualStyleBackColor = true;
            // 
            // lblStats
            // 
            this.lblStats.AutoSize = true;
            this.lblStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStats.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStats.ForeColor = ScanAndRemoveVirus.Control.Theme.TextGray;
            this.lblStats.Location = new System.Drawing.Point(449, 0);
            this.lblStats.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblStats.Name = "lblStats";
            this.lblStats.Size = new System.Drawing.Size(683, 44);
            this.lblStats.TabIndex = 3;
            this.lblStats.Text = "—";
            this.lblStats.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dgvHistory
            // 
            this.dgvHistory.AllowUserToAddRows = false;
            this.dgvHistory.AllowUserToDeleteRows = false;
            this.dgvHistory.AllowUserToResizeRows = false;
            this.dgvHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvHistory.BackgroundColor = System.Drawing.Color.White;
            this.dgvHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvHistory.ColumnHeadersHeight = 40;
            this.dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPick,
            this.colTime,
            this.colScanType,
            this.colScanLocation,
            this.colResult,
            this.colThreatCount,
            this.colDuration});
            this.dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistory.Location = new System.Drawing.Point(3, 47);
            this.dgvHistory.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.ReadOnly = false;
            this.dgvHistory.RowHeadersVisible = false;
            this.dgvHistory.RowHeadersWidth = 82;
            this.dgvHistory.RowTemplate.Height = 34;
            this.dgvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHistory.Size = new System.Drawing.Size(1142, 610);
            this.dgvHistory.TabIndex = 1;
            // 
            // colPick
            // 
            this.colPick.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colPick.HeaderText = "";
            this.colPick.Name = "colPick";
            this.colPick.ReadOnly = false;
            this.colPick.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.colPick.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colPick.ToolTipText = "Nhấp để chọn / bỏ chọn tất cả";
            this.colPick.Width = 48;
            // 
            // colTime
            // 
            this.colTime.FillWeight = 14F;
            this.colTime.HeaderText = "Thời gian";
            this.colTime.MinimumWidth = 10;
            this.colTime.Name = "colTime";
            this.colTime.ReadOnly = true;
            this.colTime.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.colTime.ToolTipText = "Nhấp để đổi chiều sắp xếp theo thời gian";
            // 
            // colScanType
            // 
            this.colScanType.FillWeight = 12F;
            this.colScanType.HeaderText = "Loại quét";
            this.colScanType.MinimumWidth = 10;
            this.colScanType.Name = "colScanType";
            this.colScanType.ReadOnly = true;
            this.colScanType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colScanLocation
            // 
            this.colScanLocation.FillWeight = 30F;
            this.colScanLocation.HeaderText = "Vị trí quét";
            this.colScanLocation.MinimumWidth = 10;
            this.colScanLocation.Name = "colScanLocation";
            this.colScanLocation.ReadOnly = true;
            this.colScanLocation.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colResult
            // 
            this.colResult.FillWeight = 13F;
            this.colResult.HeaderText = "Kết quả";
            this.colResult.MinimumWidth = 10;
            this.colResult.Name = "colResult";
            this.colResult.ReadOnly = true;
            this.colResult.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colThreatCount
            // 
            this.colThreatCount.FillWeight = 9F;
            this.colThreatCount.HeaderText = "Đe dọa";
            this.colThreatCount.MinimumWidth = 10;
            this.colThreatCount.Name = "colThreatCount";
            this.colThreatCount.ReadOnly = true;
            this.colThreatCount.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colDuration
            // 
            this.colDuration.FillWeight = 11F;
            this.colDuration.HeaderText = "Thời lượng";
            this.colDuration.MinimumWidth = 10;
            this.colDuration.Name = "colDuration";
            this.colDuration.ReadOnly = true;
            this.colDuration.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // tlpToolbar
            // 
            this.tlpToolbar.ColumnCount = 9;
            this.tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 115F));
            this.tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.tlpToolbar.Controls.Add(this.lblSelection, 0, 0);
            this.tlpToolbar.Controls.Add(this.btnViewDetail, 1, 0);
            this.tlpToolbar.Controls.Add(this.btnDeleteHistory, 3, 0);
            this.tlpToolbar.Controls.Add(this.btnExportReport, 5, 0);
            this.tlpToolbar.Controls.Add(this.btnRefreshHistory, 7, 0);
            this.tlpToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpToolbar.Location = new System.Drawing.Point(0, 661);            this.tlpToolbar.Margin = new System.Windows.Forms.Padding(0);
            this.tlpToolbar.Name = "tlpToolbar";
            this.tlpToolbar.RowCount = 1;
            this.tlpToolbar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpToolbar.Size = new System.Drawing.Size(1148, 56);
            this.tlpToolbar.TabIndex = 2;
            // 
            // lblSelection
            // 
            this.lblSelection.AutoSize = true;
            this.lblSelection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelection.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSelection.ForeColor = ScanAndRemoveVirus.Control.Theme.TextGray;
            this.lblSelection.Location = new System.Drawing.Point(6, 0);
            this.lblSelection.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblSelection.Name = "lblSelection";
            this.lblSelection.Size = new System.Drawing.Size(441, 56);
            this.lblSelection.TabIndex = 0;
            this.lblSelection.Text = "—";
            this.lblSelection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnViewDetail
            // 
            this.btnViewDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnViewDetail.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewDetail.Location = new System.Drawing.Point(460, 8);
            this.btnViewDetail.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.btnViewDetail.Name = "btnViewDetail";
            this.btnViewDetail.Size = new System.Drawing.Size(126, 40);
            this.btnViewDetail.TabIndex = 1;
            this.btnViewDetail.Text = "Xem chi tiết";
            this.btnViewDetail.UseVisualStyleBackColor = true;
            // 
            // btnDeleteHistory
            // 
            this.btnDeleteHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDeleteHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteHistory.Location = new System.Drawing.Point(598, 8);
            this.btnDeleteHistory.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.btnDeleteHistory.Name = "btnDeleteHistory";
            this.btnDeleteHistory.Size = new System.Drawing.Size(146, 40);
            this.btnDeleteHistory.TabIndex = 2;
            this.btnDeleteHistory.Text = "Xóa mục đã chọn";
            this.btnDeleteHistory.UseVisualStyleBackColor = true;
            // 
            // btnExportReport
            // 
            this.btnExportReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExportReport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportReport.Location = new System.Drawing.Point(914, 8);
            this.btnExportReport.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.btnExportReport.Name = "btnExportReport";
            this.btnExportReport.Size = new System.Drawing.Size(136, 40);
            this.btnExportReport.TabIndex = 4;
            this.btnExportReport.Text = "Xuất báo cáo";
            this.btnExportReport.UseVisualStyleBackColor = true;
            // 
            // btnRefreshHistory
            // 
            this.btnRefreshHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefreshHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefreshHistory.Location = new System.Drawing.Point(1062, 8);
            this.btnRefreshHistory.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.btnRefreshHistory.Name = "btnRefreshHistory";
            this.btnRefreshHistory.Size = new System.Drawing.Size(111, 40);
            this.btnRefreshHistory.TabIndex = 5;
            this.btnRefreshHistory.Text = "Làm mới";
            this.btnRefreshHistory.UseVisualStyleBackColor = true;
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
            this.panel1.ResumeLayout(false);
            this.tlpMain.ResumeLayout(false);
            this.tlpFilter.ResumeLayout(false);
            this.tlpFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.tlpToolbar.ResumeLayout(false);
            this.tlpToolbar.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label lblHistoryTitle;
        private System.Windows.Forms.Label lblHistorySubtitle;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TableLayoutPanel tlpMain;
        private System.Windows.Forms.TableLayoutPanel tlpFilter;
        private System.Windows.Forms.Button btnFilterAll;
        private System.Windows.Forms.Button btnFilterThreats;
        private System.Windows.Forms.Button btnFilterUpdates;
        private System.Windows.Forms.Label lblStats;
        private System.Windows.Forms.DataGridView dgvHistory;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colPick;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScanType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScanLocation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn colThreatCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDuration;
        private System.Windows.Forms.TableLayoutPanel tlpToolbar;
        private System.Windows.Forms.Label lblSelection;
        private System.Windows.Forms.Button btnViewDetail;
        private System.Windows.Forms.Button btnDeleteHistory;
        private System.Windows.Forms.Button btnExportReport;
        private System.Windows.Forms.Button btnRefreshHistory;
    }
}
