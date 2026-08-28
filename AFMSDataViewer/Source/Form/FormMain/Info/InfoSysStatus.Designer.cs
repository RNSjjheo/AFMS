namespace AFMSDataViewer
{
    partial class InfoSysStatus
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Dispose();
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            uiPnMain = new AFMSDll.AFMSSectionPanel();
            SuspendLayout();
            // 
            // uiPnMain
            // 
            uiPnMain.BackColor = Color.White;
            uiPnMain.ContentLayout.BackColor = Color.Transparent;
            uiPnMain.ContentLayout.ColumnCount = 2;
            uiPnMain.ContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiPnMain.ContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiPnMain.ContentLayout.Location = new Point(1, 32);
            uiPnMain.ContentLayout.Margin = new Padding(0);
            uiPnMain.ContentLayout.Name = "";
            uiPnMain.ContentLayout.Padding = new Padding(10, 8, 10, 8);
            uiPnMain.ContentLayout.Size = new Size(333, 87);
            uiPnMain.ContentLayout.TabIndex = 0;
            uiPnMain.BorderRadius = 8;
            uiPnMain.BorderColor = Color.FromArgb(225, 229, 235);
            uiPnMain.BorderThickness = 0.5F;
            uiPnMain.Dock = DockStyle.Fill;
            uiPnMain.HeaderBackColor = Color.FromArgb(245, 247, 250);
            uiPnMain.HeaderColor = Color.FromArgb(55, 62, 72);
            uiPnMain.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            uiPnMain.HeaderLineColor = Color.FromArgb(225, 229, 235);
            uiPnMain.SectionStyle = AFMSDll.AFMSSectionStyle.FilledHeader;
            uiPnMain.Location = new Point(0, 0);
            uiPnMain.Name = "uiPnMain";
            uiPnMain.Size = new Size(335, 120);
            uiPnMain.TabIndex = 0;
            // 
            // InfoSysStatus
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiPnMain);
            Name = "InfoSysStatus";
            Size = new Size(335, 120);
            ResumeLayout(false);
        }

        #endregion

        public AFMSDll.AFMSSectionPanel uiPnMain;
    }
}
