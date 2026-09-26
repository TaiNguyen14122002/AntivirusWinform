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
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tlpPage = new System.Windows.Forms.TableLayoutPanel();
            this.tlpHeader = new System.Windows.Forms.TableLayoutPanel();
            this.lblHistoryTitle = new System.Windows.Forms.Label();
            this.lblHistorySubtitle = new System.Windows.Forms.Label();
            this.tlpFilter = new System.Windows.Forms.TableLayoutPanel();
            this.tlpTuNgay = new System.Windows.Forms.TableLayoutPanel();
            this.lblTuNgay = new System.Windows.Forms.Label();
            this.dtpTuNgay = new System.Windows.Forms.DateTimePicker();
            this.tlpDenNgay = new System.Windows.Forms.TableLayoutPanel();
            this.lblDenNgay = new System.Windows.Forms.Label();
            this.dtpDenNgay = new System.Windows.Forms.DateTimePicker();
            this.tlpLoaiQuet = new System.Windows.Forms.TableLayoutPanel();
            this.lblLoaiQuet = new System.Windows.Forms.Label();
            this.cboLoaiQuet = new System.Windows.Forms.ComboBox();
            this.btnLoc = new System.Windows.Forms.Button();
            this.btnMore = new System.Windows.Forms.Button();
            this.pnlGrid = new System.Windows.Forms.Panel();
            this.lblEmpty = new System.Windows.Forms.Label();
            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.colScanTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScanType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFileCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colThreatCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colView = new System.Windows.Forms.DataGridViewButtonColumn();
            this.menuLichSu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miXemChiTiet = new System.Windows.Forms.ToolStripMenuItem();
            this.miLamMoi = new System.Windows.Forms.ToolStripMenuItem();
            this.miXuatCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.miSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.miXoaDong = new System.Windows.Forms.ToolStripMenuItem();
            this.miSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.miXemPhienQuet = new System.Windows.Forms.ToolStripMenuItem();
            this.miXemCanhBao = new System.Windows.Forms.ToolStripMenuItem();
            this.miXemCapNhat = new System.Windows.Forms.ToolStripMenuItem();
            this.tlpFooter = new System.Windows.Forms.TableLayoutPanel();
            this.lblTotal = new System.Windows.Forms.Label();
            this.tlpPager = new System.Windows.Forms.TableLayoutPanel();
            this.btnPrev = new System.Windows.Forms.Button();
            this.lblPage = new System.Windows.Forms.Label();
            this.btnNext = new System.Windows.Forms.Button();
            this.tlpPage.SuspendLayout();
            this.tlpHeader.SuspendLayout();
            this.tlpFilter.SuspendLayout();
            this.tlpTuNgay.SuspendLayout();
            this.tlpDenNgay.SuspendLayout();
            this.tlpLoaiQuet.SuspendLayout();
            this.pnlGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.menuLichSu.SuspendLayout();
            this.tlpFooter.SuspendLayout();
            this.tlpPager.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlpPage
            // 
            this.tlpPage.ColumnCount = 1;
            this.tlpPage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpPage.Controls.Add(this.tlpHeader, 0, 0);
            this.tlpPage.Controls.Add(this.tlpFilter, 0, 1);
            this.tlpPage.Controls.Add(this.pnlGrid, 0, 2);
            this.tlpPage.Controls.Add(this.tlpFooter, 0, 3);
            this.tlpPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpPage.Location = new System.Drawing.Point(0, 0);
            this.tlpPage.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.tlpPage.Name = "tlpPage";
            this.tlpPage.Padding = new System.Windows.Forms.Padding(68, 58, 68, 50);
            this.tlpPage.RowCount = 4;
            this.tlpPage.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpPage.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpPage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpPage.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpPage.Size = new System.Drawing.Size(2000, 1269);
            this.tlpPage.TabIndex = 0;
            // 
            // tlpHeader
            // 
            this.tlpHeader.AutoSize = true;
            this.tlpHeader.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpHeader.ColumnCount = 1;
            this.tlpHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpHeader.Controls.Add(this.lblHistoryTitle, 0, 0);
            this.tlpHeader.Controls.Add(this.lblHistorySubtitle, 0, 1);
            this.tlpHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpHeader.Location = new System.Drawing.Point(68, 58);
            this.tlpHeader.Margin = new System.Windows.Forms.Padding(0);
            this.tlpHeader.Name = "tlpHeader";
            this.tlpHeader.RowCount = 2;
            this.tlpHeader.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpHeader.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpHeader.Size = new System.Drawing.Size(1864, 131);
            this.tlpHeader.TabIndex = 0;
            // 
            // lblHistoryTitle
            // 
            this.lblHistoryTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHistoryTitle.Font = new System.Drawing.Font("Segoe UI", 25F, System.Drawing.FontStyle.Bold);
            this.lblHistoryTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(41)))), ((int)(((byte)(55)))));
            this.lblHistoryTitle.Location = new System.Drawing.Point(0, 0);
            this.lblHistoryTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHistoryTitle.Name = "lblHistoryTitle";
            this.lblHistoryTitle.Size = new System.Drawing.Size(1864, 85);
            this.lblHistoryTitle.TabIndex = 0;
            this.lblHistoryTitle.Text = "Lịch sử";
            // 
            // lblHistorySubtitle
            // 
            this.lblHistorySubtitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHistorySubtitle.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.lblHistorySubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblHistorySubtitle.Location = new System.Drawing.Point(0, 89);
            this.lblHistorySubtitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblHistorySubtitle.Name = "lblHistorySubtitle";
            this.lblHistorySubtitle.Size = new System.Drawing.Size(1864, 42);
            this.lblHistorySubtitle.TabIndex = 1;
            this.lblHistorySubtitle.Text = "Xem lại các lần quét và những mối đe dọa đã được xử lý.";
            // 
            // tlpFilter
            // 
            this.tlpFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpFilter.AutoSize = true;
            this.tlpFilter.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpFilter.ColumnCount = 5;
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 236F));
            this.tlpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this.tlpFilter.Controls.Add(this.tlpTuNgay, 0, 0);
            this.tlpFilter.Controls.Add(this.tlpDenNgay, 1, 0);
            this.tlpFilter.Controls.Add(this.tlpLoaiQuet, 2, 0);
            this.tlpFilter.Controls.Add(this.btnLoc, 3, 0);
            this.tlpFilter.Controls.Add(this.btnMore, 4, 0);
            this.tlpFilter.Location = new System.Drawing.Point(652, 220);
            this.tlpFilter.Margin = new System.Windows.Forms.Padding(0, 31, 0, 27);
            this.tlpFilter.Name = "tlpFilter";
            this.tlpFilter.RowCount = 1;
            this.tlpFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpFilter.Size = new System.Drawing.Size(1280, 88);
            this.tlpFilter.TabIndex = 1;
            // 
            // tlpTuNgay
            // 
            this.tlpTuNgay.ColumnCount = 1;
            this.tlpTuNgay.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpTuNgay.Controls.Add(this.lblTuNgay, 0, 0);
            this.tlpTuNgay.Controls.Add(this.dtpTuNgay, 0, 1);
            this.tlpTuNgay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpTuNgay.Location = new System.Drawing.Point(0, 0);
            this.tlpTuNgay.Margin = new System.Windows.Forms.Padding(0, 0, 28, 0);
            this.tlpTuNgay.Name = "tlpTuNgay";
            this.tlpTuNgay.RowCount = 2;
            this.tlpTuNgay.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpTuNgay.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpTuNgay.Size = new System.Drawing.Size(252, 88);
            this.tlpTuNgay.TabIndex = 0;
            // 
            // lblTuNgay
            // 
            this.lblTuNgay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTuNgay.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblTuNgay.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblTuNgay.Location = new System.Drawing.Point(0, 0);
            this.lblTuNgay.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblTuNgay.Name = "lblTuNgay";
            this.lblTuNgay.Size = new System.Drawing.Size(252, 31);
            this.lblTuNgay.TabIndex = 0;
            this.lblTuNgay.Text = "Từ ngày";
            // 
            // dtpTuNgay
            // 
            this.dtpTuNgay.CustomFormat = "dd/MM/yyyy";
            this.dtpTuNgay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpTuNgay.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpTuNgay.Location = new System.Drawing.Point(0, 39);
            this.dtpTuNgay.Margin = new System.Windows.Forms.Padding(0);
            this.dtpTuNgay.Name = "dtpTuNgay";
            this.dtpTuNgay.Size = new System.Drawing.Size(252, 31);
            this.dtpTuNgay.TabIndex = 1;
            // 
            // tlpDenNgay
            // 
            this.tlpDenNgay.ColumnCount = 1;
            this.tlpDenNgay.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpDenNgay.Controls.Add(this.lblDenNgay, 0, 0);
            this.tlpDenNgay.Controls.Add(this.dtpDenNgay, 0, 1);
            this.tlpDenNgay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpDenNgay.Location = new System.Drawing.Point(280, 0);
            this.tlpDenNgay.Margin = new System.Windows.Forms.Padding(0, 0, 28, 0);
            this.tlpDenNgay.Name = "tlpDenNgay";
            this.tlpDenNgay.RowCount = 2;
            this.tlpDenNgay.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpDenNgay.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpDenNgay.Size = new System.Drawing.Size(252, 88);
            this.tlpDenNgay.TabIndex = 1;
            // 
            // lblDenNgay
            // 
            this.lblDenNgay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDenNgay.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblDenNgay.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblDenNgay.Location = new System.Drawing.Point(0, 0);
            this.lblDenNgay.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblDenNgay.Name = "lblDenNgay";
            this.lblDenNgay.Size = new System.Drawing.Size(252, 31);
            this.lblDenNgay.TabIndex = 0;
            this.lblDenNgay.Text = "Đến ngày";
            // 
            // dtpDenNgay
            // 
            this.dtpDenNgay.CustomFormat = "dd/MM/yyyy";
            this.dtpDenNgay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpDenNgay.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDenNgay.Location = new System.Drawing.Point(0, 39);
            this.dtpDenNgay.Margin = new System.Windows.Forms.Padding(0);
            this.dtpDenNgay.Name = "dtpDenNgay";
            this.dtpDenNgay.Size = new System.Drawing.Size(252, 31);
            this.dtpDenNgay.TabIndex = 1;
            // 
            // tlpLoaiQuet
            // 
            this.tlpLoaiQuet.ColumnCount = 1;
            this.tlpLoaiQuet.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpLoaiQuet.Controls.Add(this.lblLoaiQuet, 0, 0);
            this.tlpLoaiQuet.Controls.Add(this.cboLoaiQuet, 0, 1);
            this.tlpLoaiQuet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpLoaiQuet.Location = new System.Drawing.Point(560, 0);
            this.tlpLoaiQuet.Margin = new System.Windows.Forms.Padding(0, 0, 28, 0);
            this.tlpLoaiQuet.Name = "tlpLoaiQuet";
            this.tlpLoaiQuet.RowCount = 2;
            this.tlpLoaiQuet.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpLoaiQuet.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpLoaiQuet.Size = new System.Drawing.Size(352, 88);
            this.tlpLoaiQuet.TabIndex = 2;
            // 
            // lblLoaiQuet
            // 
            this.lblLoaiQuet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLoaiQuet.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblLoaiQuet.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblLoaiQuet.Location = new System.Drawing.Point(0, 0);
            this.lblLoaiQuet.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblLoaiQuet.Name = "lblLoaiQuet";
            this.lblLoaiQuet.Size = new System.Drawing.Size(352, 31);
            this.lblLoaiQuet.TabIndex = 0;
            this.lblLoaiQuet.Text = "Loại quét";
            // 
            // cboLoaiQuet
            // 
            this.cboLoaiQuet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboLoaiQuet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLoaiQuet.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboLoaiQuet.FormattingEnabled = true;
            this.cboLoaiQuet.Location = new System.Drawing.Point(0, 39);
            this.cboLoaiQuet.Margin = new System.Windows.Forms.Padding(0);
            this.cboLoaiQuet.Name = "cboLoaiQuet";
            this.cboLoaiQuet.Size = new System.Drawing.Size(352, 33);
            this.cboLoaiQuet.TabIndex = 1;
            // 
            // btnLoc
            // 
            this.btnLoc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnLoc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoc.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnLoc.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnLoc.Location = new System.Drawing.Point(952, 23);
            this.btnLoc.Margin = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.btnLoc.Name = "btnLoc";
            this.btnLoc.Size = new System.Drawing.Size(212, 65);
            this.btnLoc.TabIndex = 3;
            this.btnLoc.Text = "Lọc";
            this.btnLoc.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnLoc.UseVisualStyleBackColor = false;
            // 
            // btnMore
            // 
            this.btnMore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnMore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMore.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnMore.Location = new System.Drawing.Point(1188, 23);
            this.btnMore.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.btnMore.Name = "btnMore";
            this.btnMore.Size = new System.Drawing.Size(80, 65);
            this.btnMore.TabIndex = 4;
            this.btnMore.UseVisualStyleBackColor = false;
            // 
            // pnlGrid
            // 
            this.pnlGrid.BackColor = System.Drawing.Color.White;
            this.pnlGrid.Controls.Add(this.lblEmpty);
            this.pnlGrid.Controls.Add(this.dgvHistory);
            this.pnlGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGrid.Location = new System.Drawing.Point(68, 335);
            this.pnlGrid.Margin = new System.Windows.Forms.Padding(0);
            this.pnlGrid.Name = "pnlGrid";
            this.pnlGrid.Size = new System.Drawing.Size(1864, 792);
            this.pnlGrid.TabIndex = 2;
            // 
            // lblEmpty
            // 
            this.lblEmpty.BackColor = System.Drawing.Color.White;
            this.lblEmpty.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblEmpty.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblEmpty.Location = new System.Drawing.Point(0, 92);
            this.lblEmpty.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblEmpty.Name = "lblEmpty";
            this.lblEmpty.Size = new System.Drawing.Size(1864, 92);
            this.lblEmpty.TabIndex = 1;
            this.lblEmpty.Text = "Không có lịch sử quét phù hợp";
            this.lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmpty.Visible = false;
            // 
            // dgvHistory
            // 
            this.dgvHistory.AllowUserToAddRows = false;
            this.dgvHistory.AllowUserToDeleteRows = false;
            this.dgvHistory.AllowUserToResizeRows = false;
            this.dgvHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvHistory.ColumnHeadersHeight = 46;
            this.dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colScanTime,
            this.colScanType,
            this.colResult,
            this.colFileCount,
            this.colThreatCount,
            this.colDuration,
            this.colView});
            this.dgvHistory.ContextMenuStrip = this.menuLichSu;
            this.dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistory.Location = new System.Drawing.Point(0, 0);
            this.dgvHistory.Margin = new System.Windows.Forms.Padding(0);
            this.dgvHistory.MultiSelect = false;
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.ReadOnly = true;
            this.dgvHistory.RowHeadersVisible = false;
            this.dgvHistory.RowHeadersWidth = 82;
            this.dgvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHistory.Size = new System.Drawing.Size(1864, 792);
            this.dgvHistory.TabIndex = 0;
            // 
            // colScanTime
            // 
            this.colScanTime.FillWeight = 118F;
            this.colScanTime.HeaderText = "Thời gian";
            this.colScanTime.MinimumWidth = 118;
            this.colScanTime.Name = "colScanTime";
            this.colScanTime.ReadOnly = true;
            // 
            // colScanType
            // 
            this.colScanType.FillWeight = 140F;
            this.colScanType.HeaderText = "Loại quét";
            this.colScanType.MinimumWidth = 130;
            this.colScanType.Name = "colScanType";
            this.colScanType.ReadOnly = true;
            // 
            // colResult
            // 
            this.colResult.FillWeight = 160F;
            this.colResult.HeaderText = "Kết quả";
            this.colResult.MinimumWidth = 140;
            this.colResult.Name = "colResult";
            this.colResult.ReadOnly = true;
            // 
            // colFileCount
            // 
            this.colFileCount.FillWeight = 108F;
            this.colFileCount.HeaderText = "Số tệp quét";
            this.colFileCount.MinimumWidth = 100;
            this.colFileCount.Name = "colFileCount";
            this.colFileCount.ReadOnly = true;
            // 
            // colThreatCount
            // 
            this.colThreatCount.FillWeight = 118F;
            this.colThreatCount.HeaderText = "Số mối đe dọa";
            this.colThreatCount.MinimumWidth = 110;
            this.colThreatCount.Name = "colThreatCount";
            this.colThreatCount.ReadOnly = true;
            // 
            // colDuration
            // 
            this.colDuration.FillWeight = 108F;
            this.colDuration.HeaderText = "Thời gian";
            this.colDuration.MinimumWidth = 100;
            this.colDuration.Name = "colDuration";
            this.colDuration.ReadOnly = true;
            // 
            // colView
            // 
            this.colView.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colView.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.colView.HeaderText = "Chi tiết";
            this.colView.MinimumWidth = 92;
            this.colView.Name = "colView";
            this.colView.ReadOnly = true;
            this.colView.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.colView.Text = "Xem";
            this.colView.UseColumnTextForButtonValue = true;
            // 
            // menuLichSu
            // 
            this.menuLichSu.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuLichSu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miXemChiTiet,
            this.miLamMoi,
            this.miXuatCsv,
            this.miSep1,
            this.miXoaDong,
            this.miSep2,
            this.miXemPhienQuet,
            this.miXemCanhBao,
            this.miXemCapNhat});
            this.menuLichSu.Name = "menuLichSu";
            this.menuLichSu.Size = new System.Drawing.Size(439, 296);
            // 
            // miXemChiTiet
            // 
            this.miXemChiTiet.Name = "miXemChiTiet";
            this.miXemChiTiet.Size = new System.Drawing.Size(438, 40);
            this.miXemChiTiet.Text = "Xem chi tiết dòng đang chọn";
            // 
            // miLamMoi
            // 
            this.miLamMoi.Name = "miLamMoi";
            this.miLamMoi.Size = new System.Drawing.Size(438, 40);
            this.miLamMoi.Text = "Làm mới dữ liệu";
            // 
            // miXuatCsv
            // 
            this.miXuatCsv.Name = "miXuatCsv";
            this.miXuatCsv.Size = new System.Drawing.Size(438, 40);
            this.miXuatCsv.Text = "Xuất CSV toàn bộ lịch sử...";
            // 
            // miSep1
            // 
            this.miSep1.Name = "miSep1";
            this.miSep1.Size = new System.Drawing.Size(435, 6);
            // 
            // miXoaDong
            // 
            this.miXoaDong.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.miXoaDong.Name = "miXoaDong";
            this.miXoaDong.Size = new System.Drawing.Size(438, 40);
            this.miXoaDong.Text = "Xóa dòng đang chọn...";
            // 
            // miSep2
            // 
            this.miSep2.Name = "miSep2";
            this.miSep2.Size = new System.Drawing.Size(435, 6);
            // 
            // miXemPhienQuet
            // 
            this.miXemPhienQuet.Checked = true;
            this.miXemPhienQuet.CheckOnClick = true;
            this.miXemPhienQuet.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miXemPhienQuet.Name = "miXemPhienQuet";
            this.miXemPhienQuet.Size = new System.Drawing.Size(438, 40);
            this.miXemPhienQuet.Text = "Chỉ hiện phiên quét";
            // 
            // miXemCanhBao
            // 
            this.miXemCanhBao.CheckOnClick = true;
            this.miXemCanhBao.Name = "miXemCanhBao";
            this.miXemCanhBao.Size = new System.Drawing.Size(438, 40);
            this.miXemCanhBao.Text = "Chỉ hiện cảnh báo thời gian thực";
            // 
            // miXemCapNhat
            // 
            this.miXemCapNhat.CheckOnClick = true;
            this.miXemCapNhat.Name = "miXemCapNhat";
            this.miXemCapNhat.Size = new System.Drawing.Size(438, 40);
            this.miXemCapNhat.Text = "Nhật ký cập nhật CSDL";
            // 
            // tlpFooter
            // 
            this.tlpFooter.AutoSize = true;
            this.tlpFooter.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpFooter.ColumnCount = 2;
            this.tlpFooter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpFooter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpFooter.Controls.Add(this.lblTotal, 0, 0);
            this.tlpFooter.Controls.Add(this.tlpPager, 1, 0);
            this.tlpFooter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpFooter.Location = new System.Drawing.Point(68, 1150);
            this.tlpFooter.Margin = new System.Windows.Forms.Padding(0, 23, 0, 0);
            this.tlpFooter.Name = "tlpFooter";
            this.tlpFooter.RowCount = 1;
            this.tlpFooter.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpFooter.Size = new System.Drawing.Size(1864, 69);
            this.tlpFooter.TabIndex = 3;
            // 
            // lblTotal
            // 
            this.lblTotal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotal.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblTotal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblTotal.Location = new System.Drawing.Point(0, 0);
            this.lblTotal.Margin = new System.Windows.Forms.Padding(0);
            this.lblTotal.Name = "lblTotal";
            this.lblTotal.Size = new System.Drawing.Size(1568, 69);
            this.lblTotal.TabIndex = 0;
            this.lblTotal.Text = "Tổng cộng: 0 lần quét";
            this.lblTotal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tlpPager
            // 
            this.tlpPager.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.tlpPager.AutoSize = true;
            this.tlpPager.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpPager.ColumnCount = 3;
            this.tlpPager.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.tlpPager.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 152F));
            this.tlpPager.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.tlpPager.Controls.Add(this.btnPrev, 0, 0);
            this.tlpPager.Controls.Add(this.lblPage, 1, 0);
            this.tlpPager.Controls.Add(this.btnNext, 2, 0);
            this.tlpPager.Location = new System.Drawing.Point(1568, 0);
            this.tlpPager.Margin = new System.Windows.Forms.Padding(0);
            this.tlpPager.Name = "tlpPager";
            this.tlpPager.RowCount = 1;
            this.tlpPager.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpPager.Size = new System.Drawing.Size(296, 69);
            this.tlpPager.TabIndex = 1;
            // 
            // btnPrev
            // 
            this.btnPrev.Enabled = false;
            this.btnPrev.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrev.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnPrev.Location = new System.Drawing.Point(0, 0);
            this.btnPrev.Margin = new System.Windows.Forms.Padding(0);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(72, 69);
            this.btnPrev.TabIndex = 0;
            this.btnPrev.UseVisualStyleBackColor = false;
            // 
            // lblPage
            // 
            this.lblPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPage.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblPage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblPage.Location = new System.Drawing.Point(72, 0);
            this.lblPage.Margin = new System.Windows.Forms.Padding(0);
            this.lblPage.Name = "lblPage";
            this.lblPage.Size = new System.Drawing.Size(152, 69);
            this.lblPage.TabIndex = 1;
            this.lblPage.Text = "0 / 0";
            this.lblPage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnNext
            // 
            this.btnNext.Enabled = false;
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnNext.Location = new System.Drawing.Point(224, 0);
            this.btnNext.Margin = new System.Windows.Forms.Padding(0);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(72, 69);
            this.btnNext.TabIndex = 2;
            this.btnNext.UseVisualStyleBackColor = false;
            // 
            // UcLichSu
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.tlpPage);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "UcLichSu";
            this.Size = new System.Drawing.Size(2000, 1269);
            this.tlpPage.ResumeLayout(false);
            this.tlpPage.PerformLayout();
            this.tlpHeader.ResumeLayout(false);
            this.tlpFilter.ResumeLayout(false);
            this.tlpTuNgay.ResumeLayout(false);
            this.tlpDenNgay.ResumeLayout(false);
            this.tlpLoaiQuet.ResumeLayout(false);
            this.pnlGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.menuLichSu.ResumeLayout(false);
            this.tlpFooter.ResumeLayout(false);
            this.tlpFooter.PerformLayout();
            this.tlpPager.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlpPage;
        private System.Windows.Forms.TableLayoutPanel tlpHeader;
        private System.Windows.Forms.Label lblHistoryTitle;
        private System.Windows.Forms.Label lblHistorySubtitle;
        private System.Windows.Forms.TableLayoutPanel tlpFilter;
        private System.Windows.Forms.TableLayoutPanel tlpTuNgay;
        private System.Windows.Forms.Label lblTuNgay;
        private System.Windows.Forms.DateTimePicker dtpTuNgay;
        private System.Windows.Forms.TableLayoutPanel tlpDenNgay;
        private System.Windows.Forms.Label lblDenNgay;
        private System.Windows.Forms.DateTimePicker dtpDenNgay;
        private System.Windows.Forms.TableLayoutPanel tlpLoaiQuet;
        private System.Windows.Forms.Label lblLoaiQuet;
        private System.Windows.Forms.ComboBox cboLoaiQuet;
        private System.Windows.Forms.Button btnLoc;
        private System.Windows.Forms.Button btnMore;
        private System.Windows.Forms.ContextMenuStrip menuLichSu;
        private System.Windows.Forms.ToolStripMenuItem miXemChiTiet;
        private System.Windows.Forms.ToolStripMenuItem miLamMoi;
        private System.Windows.Forms.ToolStripMenuItem miXuatCsv;
        private System.Windows.Forms.ToolStripSeparator miSep1;
        private System.Windows.Forms.ToolStripMenuItem miXoaDong;
        private System.Windows.Forms.ToolStripSeparator miSep2;
        private System.Windows.Forms.ToolStripMenuItem miXemPhienQuet;
        private System.Windows.Forms.ToolStripMenuItem miXemCanhBao;
        private System.Windows.Forms.ToolStripMenuItem miXemCapNhat;
        private System.Windows.Forms.Panel pnlGrid;
        private System.Windows.Forms.DataGridView dgvHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScanTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScanType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFileCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colThreatCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDuration;
        private System.Windows.Forms.DataGridViewButtonColumn colView;
        private System.Windows.Forms.Label lblEmpty;
        private System.Windows.Forms.TableLayoutPanel tlpFooter;
        private System.Windows.Forms.Label lblTotal;
        private System.Windows.Forms.TableLayoutPanel tlpPager;
        private System.Windows.Forms.Button btnPrev;
        private System.Windows.Forms.Label lblPage;
        private System.Windows.Forms.Button btnNext;
    }
}
