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
    public partial class UCSystemInfo : UserControl
    {
        public UCSystemInfo()
        {
            InitializeComponent();

            uiPnMain.BackColor = DllColorHelper.HexToColor("#F0F4F9");
            uiPnMain.Margin = new Padding(5);

            uiTpMain.RowStyles[0].Height = 90;
            uiTpMain.RowStyles[1].Height = 120;
            uiTpMain.Margin = Padding.Empty;

            uiSysInfo.Margin = Padding.Empty;
            uiSysInfo.Padding = Padding.Empty;

            uiInfoSite.Margin = new Padding(0, 8, 0, 0);
            uiInfoSite.Padding = Padding.Empty;

            uiInfoDev.Margin = new Padding(0, 8, 0, 0);
            uiInfoDev.Padding = Padding.Empty;

            SetupMainPanel(uiSysInfo.uiPnMain);
            SetupMainPanel(uiInfoSite.uiPnMain);
            SetupMainPanel(uiInfoDev.uiPnMain);
        }

        private void SetupMainPanel(AFMSPanel panel)
        {
            panel.BorderStyle = BorderStyle.None;
            panel.BorderRadius = 5;
            panel.BorderColor = DllColorHelper.HexToColor("#E3E9F1");
            panel.BackColor = Color.White;
            panel.Padding = new Padding(6);
            panel.Margin = new Padding(5);
        }

        private void SetupMainPanel(AFMSSectionPanel panel)
        {
            panel.BorderRadius = 5;
            panel.BorderColor = DllColorHelper.HexToColor("#E3E9F1");
            panel.BorderThickness = 1F;
            panel.BackColor = Color.White;
            panel.Padding = new Padding(6);
            panel.Margin = new Padding(5);
        }

        public void ReadDatabase()
        {
            uiSysInfo.ReadDatabase();
            uiInfoSite.ReadDatabase();
            uiInfoDev.ReadDatabase();
        }
    }
}
