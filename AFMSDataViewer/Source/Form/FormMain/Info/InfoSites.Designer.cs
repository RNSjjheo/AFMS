namespace AFMSDataViewer
{
    partial class InfoSites
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            uiPnMain = new AFMSDll.AFMSSectionPanel();
            SuspendLayout();
            // 
            // uiPnMain
            // 
            uiPnMain.BackColor = Color.White;
            // 
            // 
            // 
            uiPnMain.ContentLayout.BackColor = Color.Transparent;
            uiPnMain.ContentLayout.ColumnCount = 2;
            uiPnMain.ContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            uiPnMain.ContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            uiPnMain.ContentLayout.Location = new Point(1, 32);
            uiPnMain.ContentLayout.Margin = new Padding(0);
            uiPnMain.ContentLayout.Name = "";
            uiPnMain.ContentLayout.Padding = new Padding(10, 8, 10, 8);
            uiPnMain.ContentLayout.Size = new Size(344, 117);
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
            uiPnMain.Size = new Size(346, 150);
            uiPnMain.TabIndex = 0;
            // 
            // InfoSites
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiPnMain);
            Name = "InfoSites";
            Size = new Size(346, 150);
            ResumeLayout(false);
        }

        #endregion

        public AFMSDll.AFMSSectionPanel uiPnMain;
    }
}
