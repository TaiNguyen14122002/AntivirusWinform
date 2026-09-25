namespace ScanAndRemoveVirus
{
    partial class FrmMain
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.btnTongQuan = new System.Windows.Forms.Button();
            this.btnCachLy = new System.Windows.Forms.Button();
            this.btnLichSu = new System.Windows.Forms.Button();
            this.btnBaoVe = new System.Windows.Forms.Button();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.btnCaiDat = new System.Windows.Forms.Button();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlSidebar.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlSidebar
            // 
            this.pnlSidebar.BackColor = ScanAndRemoveVirus.Control.Theme.PageBg;
            this.pnlSidebar.Controls.Add(this.tableLayoutPanel2);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSidebar.Location = new System.Drawing.Point(3, 3);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.Size = new System.Drawing.Size(211, 797);
            this.pnlSidebar.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel4, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(211, 797);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this.btnTongQuan, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.btnCachLy, 1, 4);
            this.tableLayoutPanel3.Controls.Add(this.btnLichSu, 1, 2);
            this.tableLayoutPanel3.Controls.Add(this.btnBaoVe, 1, 3);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 7;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(205, 312);
            this.tableLayoutPanel3.TabIndex = 0;
            this.tableLayoutPanel3.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel3_Paint);
            // 
            // btnTongQuan
            // 
            this.btnTongQuan.BackColor = ScanAndRemoveVirus.Control.Theme.BlueFaint;
            this.btnTongQuan.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTongQuan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTongQuan.Font = new System.Drawing.Font("Segoe UI", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTongQuan.ForeColor = ScanAndRemoveVirus.Control.Theme.Blue;
            this.btnTongQuan.Location = new System.Drawing.Point(23, 47);
            this.btnTongQuan.Name = "btnTongQuan";
            this.btnTongQuan.Size = new System.Drawing.Size(159, 38);
            this.btnTongQuan.TabIndex = 0;
            this.btnTongQuan.Text = "Tổng quan";
            this.btnTongQuan.UseVisualStyleBackColor = false;
            this.btnTongQuan.Click += new System.EventHandler(this.btnTongQuan_Click);
            // 
            // btnCachLy
            // 
            this.btnCachLy.BackColor = ScanAndRemoveVirus.Control.Theme.BlueFaint;
            this.btnCachLy.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCachLy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCachLy.Font = new System.Drawing.Font("Segoe UI", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCachLy.ForeColor = ScanAndRemoveVirus.Control.Theme.Blue;
            this.btnCachLy.Location = new System.Drawing.Point(23, 179);
            this.btnCachLy.Name = "btnCachLy";
            this.btnCachLy.Size = new System.Drawing.Size(159, 38);
            this.btnCachLy.TabIndex = 2;
            this.btnCachLy.Text = "Cách ly";
            this.btnCachLy.UseVisualStyleBackColor = false;
            this.btnCachLy.Click += new System.EventHandler(this.btnCachLy_Click);
            // 
            // btnLichSu
            // 
            this.btnLichSu.BackColor = ScanAndRemoveVirus.Control.Theme.BlueFaint;
            this.btnLichSu.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLichSu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLichSu.Font = new System.Drawing.Font("Segoe UI", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLichSu.ForeColor = ScanAndRemoveVirus.Control.Theme.Blue;
            this.btnLichSu.Location = new System.Drawing.Point(23, 91);
            this.btnLichSu.Name = "btnLichSu";
            this.btnLichSu.Size = new System.Drawing.Size(159, 38);
            this.btnLichSu.TabIndex = 3;
            this.btnLichSu.Text = "Lịch sử";
            this.btnLichSu.UseVisualStyleBackColor = false;
            this.btnLichSu.Click += new System.EventHandler(this.btnLichSu_Click);
            // 
            // btnBaoVe
            // 
            this.btnBaoVe.BackColor = ScanAndRemoveVirus.Control.Theme.BlueFaint;
            this.btnBaoVe.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBaoVe.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBaoVe.Font = new System.Drawing.Font("Segoe UI", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBaoVe.ForeColor = ScanAndRemoveVirus.Control.Theme.Blue;
            this.btnBaoVe.Location = new System.Drawing.Point(23, 135);
            this.btnBaoVe.Name = "btnBaoVe";
            this.btnBaoVe.Size = new System.Drawing.Size(159, 38);
            this.btnBaoVe.TabIndex = 1;
            this.btnBaoVe.Text = "Bảo vệ";
            this.btnBaoVe.UseVisualStyleBackColor = false;
            this.btnBaoVe.Click += new System.EventHandler(this.btnBaoVe_Click);
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 3;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.Controls.Add(this.btnCaiDat, 1, 2);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 321);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 4;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 86.69623F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 13.30377F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(205, 473);
            this.tableLayoutPanel4.TabIndex = 1;
            // 
            // btnCaiDat
            // 
            this.btnCaiDat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCaiDat.BackColor = ScanAndRemoveVirus.Control.Theme.BlueFaint;
            this.btnCaiDat.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCaiDat.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCaiDat.Font = new System.Drawing.Font("Segoe UI", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCaiDat.ForeColor = ScanAndRemoveVirus.Control.Theme.Blue;
            this.btnCaiDat.Location = new System.Drawing.Point(23, 398);
            this.btnCaiDat.Name = "btnCaiDat";
            this.btnCaiDat.Size = new System.Drawing.Size(159, 51);
            this.btnCaiDat.TabIndex = 4;
            this.btnCaiDat.Text = "Cài đặt";
            this.btnCaiDat.UseVisualStyleBackColor = false;
            // 
            // pnlContent
            // 
            this.pnlContent.BackColor = System.Drawing.Color.White;
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(3, 23);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(30, 20, 30, 20);
            this.pnlContent.Size = new System.Drawing.Size(1179, 771);
            this.pnlContent.TabIndex = 1;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.pnlContent, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(220, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1185, 797);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 1;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel5.Controls.Add(this.tableLayoutPanel6, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.tableLayoutPanel7, 0, 1);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 2;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(1414, 829);
            this.tableLayoutPanel5.TabIndex = 3;
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 2;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15.42725F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 84.57275F));
            this.tableLayoutPanel6.Controls.Add(this.tableLayoutPanel1, 1, 0);
            this.tableLayoutPanel6.Controls.Add(this.pnlSidebar, 0, 0);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 1;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(1408, 803);
            this.tableLayoutPanel6.TabIndex = 0;
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 4;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel7.Controls.Add(this.lblStatus, 1, 0);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(0, 809);
            this.tableLayoutPanel7.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 1;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(1414, 20);
            this.tableLayoutPanel7.TabIndex = 1;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.Location = new System.Drawing.Point(20, 0);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(80, 18);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Trạng thái:";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FrmMain
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1414, 829);
            this.Controls.Add(this.tableLayoutPanel5);
            this.MinimumSize = new System.Drawing.Size(980, 620);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Công cụ quét và diệt virus";
            this.Resize += new System.EventHandler(this.FrmMain_Resize);
            this.pnlSidebar.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Button btnBaoVe;
        private System.Windows.Forms.Button btnTongQuan;
        private System.Windows.Forms.Button btnCaiDat;
        private System.Windows.Forms.Button btnLichSu;
        private System.Windows.Forms.Button btnCachLy;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.Label lblStatus;
    }
}