namespace AFMSSettings
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
            afmsTabControl1 = new AFMSDll.AFMSTabControl();
            SuspendLayout();
            // 
            // afmsTabControl1
            // 
            afmsTabControl1.Dock = DockStyle.Fill;
            afmsTabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            afmsTabControl1.Font = new Font("Segoe UI", 9F);
            afmsTabControl1.ItemSize = new Size(120, 40);
            afmsTabControl1.Location = new Point(0, 31);
            afmsTabControl1.Name = "afmsTabControl1";
            afmsTabControl1.Padding = new Point(12, 5);
            afmsTabControl1.SelectedIndex = 0;
            afmsTabControl1.Size = new Size(1007, 481);
            afmsTabControl1.SizeMode = TabSizeMode.Fixed;
            afmsTabControl1.TabIndex = 0;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1007, 512);
            Controls.Add(afmsTabControl1);
            Name = "FormMain";
            Text = "Form1";
            Load += Form1_Load;
            Controls.SetChildIndex(afmsTabControl1, 0);
            ResumeLayout(false);
        }

        #endregion

        private AFMSDll.AFMSTabControl afmsTabControl1;
    }
}
