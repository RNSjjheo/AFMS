using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AFMSDataViewer.Source.Form.FormMain
{
    public partial class MainDeviceInfo : UserControl
    {
        private InfoSection SectionSiteInfo;
        private InfoSection SectionDevLevel;
        private InfoSection SectionDevPower;
        private InfoSection SectionDevHydro;

        public MainDeviceInfo()
        {
            InitializeComponent();

            statusInfoCard1.Dock = DockStyle.Fill;
            statusInfoCard1.Margin = new Padding(5);
            statusInfoCard1.BackColor = DllColorHelper.HexToColor("#F8FAFC");

            SectionSiteInfo = new InfoSection();
            SectionSiteInfo.Title = "지점 정보";

            SectionDevPower = new InfoSection();
            SectionDevPower.Title = "전원 감시";

            SectionDevLevel = new InfoSection();
            SectionDevLevel.Title = "수위계";

            SectionDevHydro = new InfoSection();
            SectionDevHydro.Title = "유속계";

            InfoItem SiteCode = new InfoItem();
            SiteCode.Name = "지점코드";
            SiteCode.Value1 = "3004585";

            InfoItem SiteName = new InfoItem();
            SiteName.Name = "지점명칭";
            SiteName.Value1 = "여주시(원부교)";

            InfoItem SitePeriod = new InfoItem();
            SitePeriod.Name = "측정주기";
            SitePeriod.Value1 = "10:00";

            InfoItem DevLevel = new InfoItem();
            DevLevel.Name = "RNSWATER";
            DevLevel.Value1 = "COM4";

            InfoItem DevVTHL = new InfoItem();
            DevVTHL.Name = "VTHLogger";
            DevVTHL.Value1 = "COM2";

            InfoItem DevHydro1 = new InfoItem();
            DevHydro1.Name = "RQ30D";
            DevHydro1.Value1 = "1+4";
            DevHydro1.Value2 = "COM6";

            InfoItem DevHydro2 = new InfoItem();
            DevHydro2.Name = "CM1200";
            DevHydro2.Value1 = "좌안";
            DevHydro2.Value2 = "COM6";

            InfoItem DevHydro3 = new InfoItem();
            DevHydro3.Name = "CM600";
            DevHydro3.Value1 = "우안";
            DevHydro3.Value2 = "COM3";

            InfoItem DevHydro4 = new InfoItem();
            DevHydro4.Name = "영상유속계";
            DevHydro4.Value1 = "Web";

            InfoItem DevHydro5 = new InfoItem();
            DevHydro5.Name = "전자파표면유속계";
            DevHydro5.Value1 = "COM5";


            SectionSiteInfo.Items.Add(SiteCode);
            SectionSiteInfo.Items.Add(SiteName);
            SectionSiteInfo.Items.Add(SitePeriod);

            SectionDevLevel.Items.Add(DevLevel);
            SectionDevPower.Items.Add(DevVTHL);

            SectionDevHydro.Items.Add(DevHydro1);
            SectionDevHydro.Items.Add(DevHydro2);
            SectionDevHydro.Items.Add(DevHydro3);
            SectionDevHydro.Items.Add(DevHydro4);
            SectionDevHydro.Items.Add(DevHydro5);

            statusInfoCard1.AddSection(SectionSiteInfo);
            statusInfoCard1.AddSection(SectionDevPower);
            statusInfoCard1.AddSection(SectionDevLevel);
            statusInfoCard1.AddSection(SectionDevHydro);

        }
    }
}

