namespace AFMSDataViewer
{
    partial class UCSystemInfo
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
            uiPnMain = new AFMSDll.AFMSPanel();
            uiTpMain = new TableLayoutPanel();
            uiSysInfo = new InfoSysStatus();
            uiInfoDev = new InfoDevDetail();
            uiInfoSite = new InfoSites();
            infoVersion1 = new InfoVersion();
            uiPnMain.SuspendLayout();
            uiTpMain.SuspendLayout();
            SuspendLayout();
            // 
            // uiPnMain
            // 
            uiPnMain.BackColor = Color.White;
            uiPnMain.Controls.Add(uiTpMain);
            uiPnMain.Dock = DockStyle.Fill;
            uiPnMain.Location = new Point(0, 0);
            uiPnMain.Name = "uiPnMain";
            uiPnMain.Padding = new Padding(8);
            uiPnMain.Size = new Size(274, 507);
            uiPnMain.TabIndex = 0;
            // 
            // uiTpMain
            // 
            uiTpMain.ColumnCount = 1;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.Controls.Add(uiSysInfo, 0, 0);
            uiTpMain.Controls.Add(uiInfoDev, 0, 2);
            uiTpMain.Controls.Add(uiInfoSite, 0, 1);
            uiTpMain.Controls.Add(infoVersion1, 0, 3);
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.Location = new Point(8, 8);
            uiTpMain.Name = "uiTpMain";
            uiTpMain.RowCount = 4;
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.Size = new Size(258, 491);
            uiTpMain.TabIndex = 0;
            // 
            // uiSysInfo
            // 
            uiSysInfo.Dock = DockStyle.Fill;
            uiSysInfo.Location = new Point(3, 7);
            uiSysInfo.Margin = new Padding(3, 7, 3, 0);
            uiSysInfo.Name = "uiSysInfo";
            uiSysInfo.Size = new Size(252, 113);
            uiSysInfo.TabIndex = 0;
            // 
            // uiInfoDev
            // 
            uiInfoDev.Dock = DockStyle.Fill;
            uiInfoDev.Location = new Point(3, 243);
            uiInfoDev.Name = "uiInfoDev";
            uiInfoDev.Size = new Size(252, 205);
            uiInfoDev.TabIndex = 1;
            // 
            // uiInfoSite
            // 
            uiInfoSite.Dock = DockStyle.Fill;
            uiInfoSite.Location = new Point(3, 123);
            uiInfoSite.Name = "uiInfoSite";
            uiInfoSite.Size = new Size(252, 114);
            uiInfoSite.TabIndex = 2;
            // 
            // infoVersion1
            // 
            infoVersion1.Dock = DockStyle.Fill;
            infoVersion1.Location = new Point(3, 454);
            infoVersion1.Name = "infoVersion1";
            infoVersion1.Size = new Size(252, 34);
            infoVersion1.TabIndex = 3;
            // 
            // UCSystemInfo
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiPnMain);
            Name = "UCSystemInfo";
            Size = new Size(274, 507);
            uiPnMain.ResumeLayout(false);
            uiTpMain.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel uiTpMain;
        public AFMSDll.AFMSPanel uiPnMain;
        private InfoSysStatus uiSysInfo;
        private InfoDevDetail uiInfoDev;
        private InfoSites uiInfoSite;
        private InfoVersion infoVersion1;
    }
}
