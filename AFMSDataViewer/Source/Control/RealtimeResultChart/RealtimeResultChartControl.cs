using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    public class RealtimeResultChartControl : TableLayoutPanel
    {
        public Panel uiPnIcon;
        public AFMSComboBox uiComboMain;
        public AFMSComboBox uiComboSub;
        public RoundedTwoLabel uiValueMin;
        public RoundedTwoLabel uiValueAvg;
        public RoundedTwoLabel uiValueMax;

        public RealtimeResultChartControl(ChartMainType chartType)
        {
            RowStyles.Clear();
            ColumnStyles.Clear();
            RowCount = 1;
            ColumnCount = 7;
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));

            uiPnIcon = new Panel();
            uiPnIcon.Dock = DockStyle.Fill;
            uiPnIcon.BackgroundImageLayout = ImageLayout.Stretch; // Panel 크기에 강제로 맞춤
            if (chartType == ChartMainType.Velocity)
            {
                uiPnIcon.BackgroundImage =  AFMSIcon.FlowVelocity(24, 24);
                uiPnIcon.BackgroundImageLayout = ImageLayout.Zoom;
            }
            else if (chartType == ChartMainType.Discharge)
            {
                uiPnIcon.BackgroundImage = AFMSIcon.FlowRate(24, 24);
                uiPnIcon.BackgroundImageLayout = ImageLayout.Zoom;
            }

            uiComboMain = new AFMSComboBox();
            uiComboMain.Dock = DockStyle.Fill;

            uiComboSub = new AFMSComboBox();
            uiComboSub.Dock = DockStyle.Fill;

            uiValueMin = new RoundedTwoLabel(false);
            uiValueMin.Dock = DockStyle.Fill;

            uiValueAvg = new RoundedTwoLabel(false);
            uiValueAvg.Dock = DockStyle.Fill;

            uiValueMax = new RoundedTwoLabel(false);
            uiValueMax.Dock = DockStyle.Fill;

            Controls.Add(uiPnIcon, 0, 0);
            Controls.Add(uiComboMain, 1, 0);
            Controls.Add(uiComboSub, 2, 0);

            Controls.Add(uiValueMin, 4, 0);
            Controls.Add(uiValueAvg, 5, 0);
            Controls.Add(uiValueMax, 6, 0);
        }
    }
}
