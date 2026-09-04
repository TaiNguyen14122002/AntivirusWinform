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
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.lblQuarantineTitle = new System.Windows.Forms.Label();
            this.lblQuarantineSubtitle = new System.Windows.Forms.Label();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.grpQuarentineList = new System.Windows.Forms.GroupBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.dgvQuarantine = new System.Windows.Forms.DataGridView();
            this.colPick = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.colFileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOriginaPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colThreatName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDetectedTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFileSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnRestore = new System.Windows.Forms.Button();
            this.btnRestoreAll = new System.Windows.Forms.Button();
            this.btnDeletePermanent = new System.Windows.Forms.Button();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.btnRefreshQuarantine = new System.Windows.Forms.Button();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.lblTotalFilesTitle = new System.Windows.Forms.Label();
            this.lblTotalFilesValue = new System.Windows.Forms.Label();
            this.grpQuarantineInfo = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel10 = new System.Windows.Forms.TableLayoutPanel();
            this.lblInfo1 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.grpQuarentineList.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvQuarantine)).BeginInit();
            this.tableLayoutPanel5.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.tableLayoutPanel8.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.grpQuarantineInfo.SuspendLayout();
            this.tableLayoutPanel10.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 88.88889F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1174, 829);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.lblQuarantineTitle, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblQuarantineSubtitle, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(1168, 83);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // lblQuarantineTitle
            // 
            this.lblQuarantineTitle.AutoSize = true;
            this.lblQuarantineTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQuarantineTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblQuarantineTitle.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblQuarantineTitle.Location = new System.Drawing.Point(3, 0);
            this.lblQuarantineTitle.Name = "lblQuarantineTitle";
            this.lblQuarantineTitle.Size = new System.Drawing.Size(1162, 41);
            this.lblQuarantineTitle.TabIndex = 0;
            this.lblQuarantineTitle.Text = "Cách ly";
            // 
            // lblQuarantineSubtitle
            // 
            this.lblQuarantineSubtitle.AutoSize = true;
            this.lblQuarantineSubtitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQuarantineSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblQuarantineSubtitle.Location = new System.Drawing.Point(3, 41);
            this.lblQuarantineSubtitle.Name = "lblQuarantineSubtitle";
            this.lblQuarantineSubtitle.Size = new System.Drawing.Size(1162, 42);
            this.lblQuarantineSubtitle.TabIndex = 1;
            this.lblQuarantineSubtitle.Text = "Các tệp tin bị phát hiện là mối đe dọa sẽ được cách ly để ngăn chặn nguy cơ gây h" +
    "ại cho hệ thống.";
            this.lblQuarantineSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this.tableLayoutPanel4, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.grpQuarantineInfo, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 92);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(1168, 713);
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
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(1162, 564);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // grpQuarentineList
            // 
            this.grpQuarentineList.Controls.Add(this.tableLayoutPanel5);
            this.grpQuarentineList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpQuarentineList.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpQuarentineList.Location = new System.Drawing.Point(3, 3);
            this.grpQuarentineList.Name = "grpQuarentineList";
            this.grpQuarentineList.Size = new System.Drawing.Size(1156, 558);
            this.grpQuarentineList.TabIndex = 0;
            this.grpQuarentineList.TabStop = false;
            this.grpQuarentineList.Text = "Danh sách tệp trong cách ly";
            this.grpQuarentineList.Enter += new System.EventHandler(this.grpQuarentineList_Enter);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dgvQuarantine);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(10, 10);
            this.panel1.Margin = new System.Windows.Forms.Padding(10);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10);
            this.panel1.Size = new System.Drawing.Size(1130, 433);
            this.panel1.TabIndex = 0;
            // 
            // dgvQuarantine
            // 
            // Hình thức + hành vi chung của table do Theme.StyleGrid đảm nhiệm (code-behind)
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
            this.dgvQuarantine.Size = new System.Drawing.Size(1110, 413);
            this.dgvQuarantine.TabIndex = 4;
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
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 1;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Controls.Add(this.panel2, 0, 1);
            this.tableLayoutPanel5.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 21);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 2;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 85F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(1150, 534);
            this.tableLayoutPanel5.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.tableLayoutPanel6);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(10, 463);
            this.panel2.Margin = new System.Windows.Forms.Padding(10);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1130, 61);
            this.panel2.TabIndex = 1;
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 2;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel6.Controls.Add(this.tableLayoutPanel7, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.tableLayoutPanel8, 1, 0);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 1;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(1130, 61);
            this.tableLayoutPanel6.TabIndex = 0;
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 3;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 31.42857F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34.28571F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34.28571F));
            this.tableLayoutPanel7.Controls.Add(this.btnRestore, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.btnRestoreAll, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.btnDeletePermanent, 2, 0);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 1;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(672, 55);
            this.tableLayoutPanel7.TabIndex = 0;
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
            // btnRestore
            // 
            this.btnRestore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRestore.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRestore.Location = new System.Drawing.Point(3, 3);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(152, 49);
            this.btnRestore.TabIndex = 0;
            this.btnRestore.Text = "Khôi phục";
            this.btnRestore.UseVisualStyleBackColor = true;
            // 
            // btnRestoreAll
            // 
            this.btnRestoreAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRestoreAll.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRestoreAll.Location = new System.Drawing.Point(161, 3);
            this.btnRestoreAll.Name = "btnRestoreAll";
            this.btnRestoreAll.Size = new System.Drawing.Size(191, 49);
            this.btnRestoreAll.TabIndex = 1;
            this.btnRestoreAll.Text = "Khôi phục tất cả";
            this.btnRestoreAll.UseVisualStyleBackColor = true;
            // 
            // btnDeletePermanent
            // 
            this.btnDeletePermanent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDeletePermanent.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDeletePermanent.Location = new System.Drawing.Point(358, 3);
            this.btnDeletePermanent.Name = "btnDeletePermanent";
            this.btnDeletePermanent.Size = new System.Drawing.Size(152, 49);
            this.btnDeletePermanent.TabIndex = 2;
            this.btnDeletePermanent.Text = "Xóa vĩnh viễn";
            this.btnDeletePermanent.UseVisualStyleBackColor = true;
            // 
            // btnDeleteAll đã bỏ: xóa theo lựa chọn là đủ, tránh mất toàn bộ khu cách ly vì 1 cú click
            //
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.ColumnCount = 3;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel8.Controls.Add(this.btnRefreshQuarantine, 2, 0);
            this.tableLayoutPanel8.Controls.Add(this.tableLayoutPanel9, 1, 0);
            this.tableLayoutPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel8.Location = new System.Drawing.Point(681, 3);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 1;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel8.Size = new System.Drawing.Size(446, 55);
            this.tableLayoutPanel8.TabIndex = 1;
            // 
            // btnRefreshQuarantine
            // 
            this.btnRefreshQuarantine.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefreshQuarantine.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRefreshQuarantine.Location = new System.Drawing.Point(337, 3);
            this.btnRefreshQuarantine.Name = "btnRefreshQuarantine";
            this.btnRefreshQuarantine.Size = new System.Drawing.Size(106, 49);
            this.btnRefreshQuarantine.TabIndex = 0;
            this.btnRefreshQuarantine.Text = "Làm mới";
            this.btnRefreshQuarantine.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.ColumnCount = 2;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel9.Controls.Add(this.lblTotalFilesTitle, 0, 0);
            this.tableLayoutPanel9.Controls.Add(this.lblTotalFilesValue, 1, 0);
            this.tableLayoutPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel9.Location = new System.Drawing.Point(114, 3);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 1;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel9.Size = new System.Drawing.Size(217, 49);
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
            this.lblTotalFilesTitle.Size = new System.Drawing.Size(102, 49);
            this.lblTotalFilesTitle.TabIndex = 0;
            this.lblTotalFilesTitle.Text = "Tổng số tệp:";
            this.lblTotalFilesTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTotalFilesValue
            // 
            this.lblTotalFilesValue.AutoSize = true;
            this.lblTotalFilesValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalFilesValue.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalFilesValue.Location = new System.Drawing.Point(111, 0);
            this.lblTotalFilesValue.Name = "lblTotalFilesValue";
            this.lblTotalFilesValue.Size = new System.Drawing.Size(103, 49);
            this.lblTotalFilesValue.TabIndex = 1;
            this.lblTotalFilesValue.Text = "4";
            this.lblTotalFilesValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpQuarantineInfo
            // 
            this.grpQuarantineInfo.Controls.Add(this.tableLayoutPanel10);
            this.grpQuarantineInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpQuarantineInfo.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpQuarantineInfo.Location = new System.Drawing.Point(3, 573);
            this.grpQuarantineInfo.Name = "grpQuarantineInfo";
            this.grpQuarantineInfo.Size = new System.Drawing.Size(1162, 137);
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
            this.tableLayoutPanel10.Location = new System.Drawing.Point(3, 21);
            this.tableLayoutPanel10.Name = "tableLayoutPanel10";
            this.tableLayoutPanel10.RowCount = 4;
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel10.Size = new System.Drawing.Size(1156, 113);
            this.tableLayoutPanel10.TabIndex = 0;
            // 
            // lblInfo1
            // 
            this.lblInfo1.AutoSize = true;
            this.lblInfo1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfo1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInfo1.Location = new System.Drawing.Point(3, 0);
            this.lblInfo1.Name = "lblInfo1";
            this.lblInfo1.Size = new System.Drawing.Size(1150, 36);
            this.lblInfo1.TabIndex = 0;
            this.lblInfo1.Text = "- \"Khôi phục\" thả tệp về vị trí cũ (còn khôi phục được); \"Xóa vĩnh viễn\" xóa hẳn tệp đã chọn khỏi đĩa.";
            this.lblInfo1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1150, 36);
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
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.grpQuarentineList.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvQuarantine)).EndInit();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel8.ResumeLayout(false);
            this.tableLayoutPanel9.ResumeLayout(false);
            this.tableLayoutPanel9.PerformLayout();
            this.grpQuarantineInfo.ResumeLayout(false);
            this.tableLayoutPanel10.ResumeLayout(false);
            this.tableLayoutPanel10.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label lblQuarantineTitle;
        private System.Windows.Forms.Label lblQuarantineSubtitle;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.GroupBox grpQuarentineList;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView dgvQuarantine;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colPick;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Button btnRestoreAll;
        private System.Windows.Forms.Button btnDeletePermanent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOriginaPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn colThreatName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDetectedTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFileSize;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
        private System.Windows.Forms.Button btnRefreshQuarantine;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel9;
        private System.Windows.Forms.Label lblTotalFilesTitle;
        private System.Windows.Forms.Label lblTotalFilesValue;
        private System.Windows.Forms.GroupBox grpQuarantineInfo;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel10;
        private System.Windows.Forms.Label lblInfo1;
        private System.Windows.Forms.Label label1;
    }
}
