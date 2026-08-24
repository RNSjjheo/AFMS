using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AFMSDataViewer
{
    public partial class InfoVersion : UserControl
    {
        public InfoVersion()
        {
            InitializeComponent();

            label1.Text = "버전정보";
            label1.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            label1.ForeColor = DllColorHelper.GetDescStrColor();
            
            label2.Text = AFMSBuild.GetVersion();
            label2.Font = label1.Font;
            label2.ForeColor = DllColorHelper.GetDescStrColor();
            label2.TextAlign = ContentAlignment.MiddleRight;
        }
    }
}
