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
            this.pnlNavHost = new System.Windows.Forms.Panel();
            this.pnlSidebarBottom = new System.Windows.Forms.Panel();
            this.lblVersion = new System.Windows.Forms.Label();
            this.pnlBrand = new System.Windows.Forms.Panel();
            this.picBrand = new System.Windows.Forms.PictureBox();
            this.lblBrandName = new System.Windows.Forms.Label();
            this.lblBrandTag = new System.Windows.Forms.Label();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.pnlSidebar.SuspendLayout();
            this.pnlSidebarBottom.SuspendLayout();
            this.pnlBrand.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picBrand)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlSidebar
            // 
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.pnlSidebar.Controls.Add(this.pnlNavHost);
            this.pnlSidebar.Controls.Add(this.pnlSidebarBottom);
            this.pnlSidebar.Controls.Add(this.pnlBrand);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Location = new System.Drawing.Point(0, 0);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.Size = new System.Drawing.Size(250, 829);
            this.pnlSidebar.TabIndex = 0;
            // 
            // pnlNavHost
            // 
            this.pnlNavHost.BackColor = System.Drawing.Color.Transparent;
            this.pnlNavHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlNavHost.Location = new System.Drawing.Point(0, 92);
            this.pnlNavHost.Name = "pnlNavHost";
            this.pnlNavHost.Padding = new System.Windows.Forms.Padding(14, 6, 14, 6);
            this.pnlNavHost.Size = new System.Drawing.Size(250, 621);
            this.pnlNavHost.TabIndex = 1;
            // 
            // pnlSidebarBottom
            // 
            this.pnlSidebarBottom.BackColor = System.Drawing.Color.Transparent;
            this.pnlSidebarBottom.Controls.Add(this.lblVersion);
            this.pnlSidebarBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSidebarBottom.Location = new System.Drawing.Point(0, 713);
            this.pnlSidebarBottom.Name = "pnlSidebarBottom";
            this.pnlSidebarBottom.Size = new System.Drawing.Size(250, 116);
            this.pnlSidebarBottom.TabIndex = 2;
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblVersion.Location = new System.Drawing.Point(31, 68);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(86, 13);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "Phiên bản 1.0.0";
            // 
            // pnlBrand
            // 
            this.pnlBrand.BackColor = System.Drawing.Color.Transparent;
            this.pnlBrand.Controls.Add(this.picBrand);
            this.pnlBrand.Controls.Add(this.lblBrandName);
            this.pnlBrand.Controls.Add(this.lblBrandTag);
            this.pnlBrand.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlBrand.Location = new System.Drawing.Point(0, 0);
            this.pnlBrand.Name = "pnlBrand";
            this.pnlBrand.Size = new System.Drawing.Size(250, 92);
            this.pnlBrand.TabIndex = 0;
            // 
            // picBrand
            // 
            this.picBrand.BackColor = System.Drawing.Color.Transparent;
            this.picBrand.Location = new System.Drawing.Point(22, 26);
            this.picBrand.Name = "picBrand";
            this.picBrand.Size = new System.Drawing.Size(36, 36);
            this.picBrand.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picBrand.TabIndex = 0;
            this.picBrand.TabStop = false;
            // 
            // lblBrandName
            // 
            this.lblBrandName.AutoSize = true;
            this.lblBrandName.BackColor = System.Drawing.Color.Transparent;
            this.lblBrandName.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold);
            this.lblBrandName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(86)))), ((int)(((byte)(216)))));
            this.lblBrandName.Location = new System.Drawing.Point(66, 24);
            this.lblBrandName.Name = "lblBrandName";
            this.lblBrandName.Size = new System.Drawing.Size(92, 25);
            this.lblBrandName.TabIndex = 1;
            this.lblBrandName.Text = "Antivirus";
            // 
            // lblBrandTag
            // 
            this.lblBrandTag.AutoSize = true;
            this.lblBrandTag.BackColor = System.Drawing.Color.Transparent;
            this.lblBrandTag.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblBrandTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblBrandTag.Location = new System.Drawing.Point(67, 51);
            this.lblBrandTag.Name = "lblBrandTag";
            this.lblBrandTag.Size = new System.Drawing.Size(132, 13);
            this.lblBrandTag.TabIndex = 2;
            this.lblBrandTag.Text = "Bảo vệ máy tính của bạn";
            // 
            // pnlContent
            // 
            this.pnlContent.BackColor = System.Drawing.Color.White;
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(250, 0);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(30, 20, 30, 20);
            this.pnlContent.Size = new System.Drawing.Size(1164, 829);
            this.pnlContent.TabIndex = 1;
            this.pnlContent.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlContent_Paint);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // FrmMain
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1414, 829);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlSidebar);
            this.MinimumSize = new System.Drawing.Size(1040, 660);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Công cụ quét và diệt virus";
            this.Resize += new System.EventHandler(this.FrmMain_Resize);
            this.pnlSidebar.ResumeLayout(false);
            this.pnlSidebarBottom.ResumeLayout(false);
            this.pnlSidebarBottom.PerformLayout();
            this.pnlBrand.ResumeLayout(false);
            this.pnlBrand.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picBrand)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Panel pnlBrand;
        private System.Windows.Forms.PictureBox picBrand;
        private System.Windows.Forms.Label lblBrandName;
        private System.Windows.Forms.Label lblBrandTag;
        private System.Windows.Forms.Panel pnlNavHost;
        private System.Windows.Forms.Panel pnlSidebarBottom;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}
