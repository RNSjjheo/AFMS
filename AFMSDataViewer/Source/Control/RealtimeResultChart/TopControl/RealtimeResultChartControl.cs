using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    public class RealtimeResultChartControl : TableLayoutPanel
    {
        public const int FIEXED_CONTROL_HEIGTH = 32;
        public Panel uiPnIcon;
        public AFMSComboBox uiComboMain;
        public AFMSComboBox uiComboSub;
        public AFMSButton uiButtonDetails;
        private readonly bool supportsDetails;
        private readonly ToolTip statisticsTip = new();

        public RealtimeResultChartControl(ChartMainType chartType)
        {
            BackColor = Color.Transparent;
            Margin = Padding.Empty;
            Padding = new Padding(0, 0, 0, 0);
            supportsDetails = chartType is ChartMainType.Velocity or ChartMainType.Discharge;
            RowStyles.Clear();
            ColumnStyles.Clear();
            RowCount = 1;
            ColumnCount = 5;
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, FIEXED_CONTROL_HEIGTH));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));

            uiPnIcon = new Panel();
            uiPnIcon.Dock = DockStyle.Fill;
            uiPnIcon.Margin = new Padding(0, 0, 6, 0);
            uiPnIcon.BackgroundImage = GetChartIcon(chartType);
            uiPnIcon.BackgroundImageLayout = ImageLayout.Zoom;

            uiComboMain = new AFMSComboBox();
            uiComboMain.Dock = DockStyle.Fill;
            uiComboMain.Margin = new Padding(0, 0, 6, 0);
            uiComboMain.Padding = new Padding(2);
            uiComboMain.PlaceholderText = GetMainPlaceholder(chartType);
            uiComboMain.BorderRadius = 5;

            uiComboSub = new AFMSComboBox();
            uiComboSub.Dock = DockStyle.Fill;
            uiComboSub.Margin = new Padding(0, 0, 6, 0);
            uiComboSub.Padding = new Padding(2);
            uiComboSub.PlaceholderText = GetSubPlaceholder(chartType);
            uiComboSub.BorderRadius = 5;

            Controls.Add(uiPnIcon, 0, 0);
            Controls.Add(uiComboMain, 1, 0);
            Controls.Add(uiComboSub, 2, 0);

            (Color accent, Color background, Color border) = GetChartTheme(chartType);
            uiButtonDetails = new AFMSButton
            {
                Text = "상세",
                AccessibleName = "상세",
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 0, 3, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = background,
                ForeColor = accent,
                Font = new Font("맑은 고딕", 9F),
                Cursor = Cursors.Hand,
                Visible = supportsDetails,
                TextAlign = ContentAlignment.MiddleCenter,
                BorderRadius = 5
            };
            uiButtonDetails.FlatAppearance.BorderColor = border;
            Controls.Add(uiButtonDetails, 4, 0);

            SetSubComboVisible(supportsDetails);
        }

        public void SetSubComboVisible(bool visible)
        {
            visible &= supportsDetails;
            uiComboSub.Visible = visible;
            SetRowSpan(uiComboMain, visible ? 1 : 3);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                statisticsTip.Dispose();
                uiPnIcon.BackgroundImage?.Dispose();
            }
            base.Dispose(disposing);
        }

        private static (Color Accent, Color Background, Color Border) GetChartTheme(ChartMainType chartType) => chartType switch
        {
            ChartMainType.Discharge => (ColorTranslator.FromHtml("#10B981"), ColorTranslator.FromHtml("#E8FFF5"), ColorTranslator.FromHtml("#A7F3D0")),
            ChartMainType.Level => (ColorTranslator.FromHtml("#1DC1D3"), ColorTranslator.FromHtml("#E6FAFD"), ColorTranslator.FromHtml("#8DE6EF")),
            ChartMainType.Velocity => (ColorTranslator.FromHtml("#8B5CF6"), ColorTranslator.FromHtml("#F1ECFF"), ColorTranslator.FromHtml("#CFC0FF")),
            _ => (ColorTranslator.FromHtml("#2563EB"), ColorTranslator.FromHtml("#EAF1FF"), ColorTranslator.FromHtml("#B7CDFD"))
        };

        private static Image GetChartIcon(ChartMainType chartType) 
        {
            const int SIZE = FIEXED_CONTROL_HEIGTH - 0;
            const float CORNER_RADIUS = 5F;

            switch(chartType)
            {
                case ChartMainType.Velocity:
                    return AFMSIcon.Get(AFMSIcons.FlowVelocity, SIZE, cornerRadius: CORNER_RADIUS);

                case ChartMainType.Level:
                    return AFMSIcon.Get(AFMSIcons.WaterLevel, SIZE, cornerRadius: CORNER_RADIUS);

                case ChartMainType.Discharge:
                    return AFMSIcon.Get(AFMSIcons.FlowRate, SIZE, cornerRadius: CORNER_RADIUS);
                default:
                    return AFMSIcon.Get(AFMSIcons.Vth, SIZE, cornerRadius: CORNER_RADIUS);
            }
        }

        private static string GetMainPlaceholder(ChartMainType chartType) => chartType switch
        {
            ChartMainType.Velocity => "1번 유속계",
            ChartMainType.Level => "1번 수위계",
            ChartMainType.Discharge => "장비 선택",
            _ => "전압 선택"
        };

        private static string GetSubPlaceholder(ChartMainType chartType) => chartType switch
        {
            ChartMainType.Discharge => "유량 산정법 선택",
            ChartMainType.Velocity => "측선 선택",
            _ => "측정값 선택"
        };
    }
}
