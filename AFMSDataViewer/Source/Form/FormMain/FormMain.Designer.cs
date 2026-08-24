namespace AFMSDataViewer
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            BottomToolStripPanel = new ToolStripPanel();
            TopToolStripPanel = new ToolStripPanel();
            RightToolStripPanel = new ToolStripPanel();
            LeftToolStripPanel = new ToolStripPanel();
            ContentPanel = new ToolStripContentPanel();
            uiTpMain = new TableLayoutPanel();
            afmsTabBar1 = new AFMSDll.AFMSTabBar();
            uiSysInfo = new UCSystemInfo();
            uiPnMain = new Panel();
            uiTpMain.SuspendLayout();
            SuspendLayout();
            // 
            // BottomToolStripPanel
            // 
            BottomToolStripPanel.Location = new Point(0, 0);
            BottomToolStripPanel.Name = "BottomToolStripPanel";
            BottomToolStripPanel.Orientation = Orientation.Horizontal;
            BottomToolStripPanel.RowMargin = new Padding(3, 0, 0, 0);
            BottomToolStripPanel.Size = new Size(0, 0);
            // 
            // TopToolStripPanel
            // 
            TopToolStripPanel.Location = new Point(0, 0);
            TopToolStripPanel.Name = "TopToolStripPanel";
            TopToolStripPanel.Orientation = Orientation.Horizontal;
            TopToolStripPanel.RowMargin = new Padding(3, 0, 0, 0);
            TopToolStripPanel.Size = new Size(0, 0);
            // 
            // RightToolStripPanel
            // 
            RightToolStripPanel.Location = new Point(0, 0);
            RightToolStripPanel.Name = "RightToolStripPanel";
            RightToolStripPanel.Orientation = Orientation.Horizontal;
            RightToolStripPanel.RowMargin = new Padding(3, 0, 0, 0);
            RightToolStripPanel.Size = new Size(0, 0);
            // 
            // LeftToolStripPanel
            // 
            LeftToolStripPanel.Location = new Point(0, 0);
            LeftToolStripPanel.Name = "LeftToolStripPanel";
            LeftToolStripPanel.Orientation = Orientation.Horizontal;
            LeftToolStripPanel.RowMargin = new Padding(3, 0, 0, 0);
            LeftToolStripPanel.Size = new Size(0, 0);
            // 
            // ContentPanel
            // 
            ContentPanel.Size = new Size(439, 113);
            // 
            // uiTpMain
            // 
            uiTpMain.ColumnCount = 2;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.Controls.Add(afmsTabBar1, 0, 0);
            uiTpMain.Controls.Add(uiSysInfo, 0, 1);
            uiTpMain.Controls.Add(uiPnMain, 1, 1);
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.Location = new Point(0, 31);
            uiTpMain.Margin = new Padding(0);
            uiTpMain.Name = "uiTpMain";
            uiTpMain.RowCount = 2;
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.Size = new Size(1092, 534);
            uiTpMain.TabIndex = 3;
            // 
            // afmsTabBar1
            // 
            afmsTabBar1.BackColor = Color.Transparent;
            uiTpMain.SetColumnSpan(afmsTabBar1, 2);
            afmsTabBar1.Dock = DockStyle.Fill;
            afmsTabBar1.Font = new Font("Segoe UI", 9F);
            afmsTabBar1.Location = new Point(3, 3);
            afmsTabBar1.MinimumSize = new Size(100, 40);
            afmsTabBar1.Name = "afmsTabBar1";
            afmsTabBar1.Size = new Size(1086, 40);
            afmsTabBar1.TabIndex = 5;
            afmsTabBar1.Text = "afmsTabBar1";
            // 
            // uiSysInfo
            // 
            uiSysInfo.Dock = DockStyle.Fill;
            uiSysInfo.Location = new Point(3, 48);
            uiSysInfo.Name = "uiSysInfo";
            uiSysInfo.Size = new Size(244, 483);
            uiSysInfo.TabIndex = 6;
            // 
            // uiPnMain
            // 
            uiPnMain.BackColor = Color.White;
            uiPnMain.Dock = DockStyle.Fill;
            uiPnMain.Location = new Point(253, 48);
            uiPnMain.Name = "uiPnMain";
            uiPnMain.Size = new Size(836, 483);
            uiPnMain.TabIndex = 7;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1092, 565);
            Controls.Add(uiTpMain);
            Name = "FormMain";
            Text = "통합자동유량측정시스템";
            Controls.SetChildIndex(uiTpMain, 0);
            uiTpMain.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private ToolStripPanel BottomToolStripPanel;
        private ToolStripPanel TopToolStripPanel;
        private ToolStripPanel RightToolStripPanel;
        private ToolStripPanel LeftToolStripPanel;
        private ToolStripContentPanel ContentPanel;
        private TableLayoutPanel uiTpMain;
        private AFMSDll.AFMSTabBar afmsTabBar1;
        private UCSystemInfo uiSysInfo;
        private Panel uiPnMain;
    }
}
