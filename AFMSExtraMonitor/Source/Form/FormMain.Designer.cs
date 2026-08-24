namespace AFMSExtraMonitor
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            propertyGrid1 = new PropertyGrid();
            splitter1 = new Splitter();
            uiTabGrid = new TabControl();
            tabPage1 = new TabPage();
            uiGridLive = new DataGridView();
            tabPage2 = new TabPage();
            uiGridFull = new DataGridView();
            uiTabGrid.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)uiGridLive).BeginInit();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)uiGridFull).BeginInit();
            SuspendLayout();
            // 
            // propertyGrid1
            // 
            propertyGrid1.BackColor = SystemColors.Control;
            propertyGrid1.Dock = DockStyle.Left;
            propertyGrid1.HelpVisible = false;
            propertyGrid1.Location = new Point(0, 0);
            propertyGrid1.Name = "propertyGrid1";
            propertyGrid1.Size = new Size(200, 450);
            propertyGrid1.TabIndex = 3;
            propertyGrid1.ToolbarVisible = false;
            // 
            // splitter1
            // 
            splitter1.Location = new Point(200, 0);
            splitter1.Name = "splitter1";
            splitter1.Size = new Size(3, 450);
            splitter1.TabIndex = 4;
            splitter1.TabStop = false;
            // 
            // uiTabGrid
            // 
            uiTabGrid.Controls.Add(tabPage1);
            uiTabGrid.Controls.Add(tabPage2);
            uiTabGrid.Dock = DockStyle.Fill;
            uiTabGrid.Location = new Point(203, 0);
            uiTabGrid.Name = "uiTabGrid";
            uiTabGrid.SelectedIndex = 0;
            uiTabGrid.Size = new Size(597, 450);
            uiTabGrid.TabIndex = 5;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(uiGridLive);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(589, 422);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "실시간";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // uiGridLive
            // 
            uiGridLive.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("맑은 고딕", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            uiGridLive.DefaultCellStyle = dataGridViewCellStyle1;
            uiGridLive.Dock = DockStyle.Fill;
            uiGridLive.Location = new Point(3, 3);
            uiGridLive.Name = "uiGridLive";
            uiGridLive.Size = new Size(583, 416);
            uiGridLive.TabIndex = 0;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(uiGridFull);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(589, 422);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "전체";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // uiGridFull
            // 
            uiGridFull.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            uiGridFull.Dock = DockStyle.Fill;
            uiGridFull.Location = new Point(3, 3);
            uiGridFull.Name = "uiGridFull";
            uiGridFull.Size = new Size(583, 416);
            uiGridFull.TabIndex = 0;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(uiTabGrid);
            Controls.Add(splitter1);
            Controls.Add(propertyGrid1);
            Name = "FormMain";
            Text = "Form1";
            Load += Form1_Load;
            uiTabGrid.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)uiGridLive).EndInit();
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)uiGridFull).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private PropertyGrid propertyGrid1;
        private Splitter splitter1;
        private TabControl uiTabGrid;
        private TabPage tabPage1;
        private DataGridView uiGridLive;
        private TabPage tabPage2;
        private DataGridView uiGridFull;
    }
}
