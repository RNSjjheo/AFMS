using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    public class RealtimeResultChartControl : TableLayoutPanel
    {
        public const int FIEXED_CONTROL_HEIGTH = 70;
        public Panel uiPnIcon;
        public AFMSComboBox uiComboMain;
        public AFMSComboBox uiComboSub;
        public AFMSLabel uiValueMin;
        public AFMSLabel uiValueAvg;
        public AFMSLabel uiValueMax;
        public Button uiButtonDetails;
        private readonly bool supportsDetails;
        private readonly ToolTip statisticsTip = new();

        public RealtimeResultChartControl(ChartMainType chartType)
        {
            BackColor = Color.Transparent;
            Margin = Padding.Empty;
            Padding = new Padding(6);
            supportsDetails = chartType is ChartMainType.Velocity or ChartMainType.Discharge;
            RowStyles.Clear();
            ColumnStyles.Clear();
            RowCount = 3;
            ColumnCount = 6;
            RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 3F));
            RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, FIEXED_CONTROL_HEIGTH));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));

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

            uiValueMin = new AFMSLabel();
            uiValueAvg = new AFMSLabel();
            uiValueMax = new AFMSLabel();

            SetupValueCard(uiValueMin, "최소", chartType);
            SetupValueCard(uiValueAvg, "평균", chartType, true);
            SetupValueCard(uiValueMax, "최대", chartType);

            Controls.Add(uiPnIcon, 0, 0);
            SetRowSpan(uiPnIcon, 3);
            Controls.Add(uiComboMain, 1, 0);
            Controls.Add(uiComboSub, 1, 2);

            Controls.Add(uiValueMin, 3, 0);
            Controls.Add(uiValueAvg, 4, 0);
            Controls.Add(uiValueMax, 5, 0);

            (Color accent, Color background, Color border) = GetChartTheme(chartType);
            uiButtonDetails = new Button
            {
                Text = "상세보기", AccessibleName = "상세보기",
                Dock = DockStyle.Fill, Margin = new Padding(3, 0, 3, 0),
                FlatStyle = FlatStyle.Flat, BackColor = background, ForeColor = accent,
                Font = new Font("맑은 고딕", 9F), Cursor = Cursors.Hand,
                Visible = supportsDetails
            };
            uiButtonDetails.FlatAppearance.BorderColor = border;
            Controls.Add(uiButtonDetails, 3, 2);
            SetColumnSpan(uiButtonDetails, 3);

            if (!supportsDetails)
            {
                SetRowSpan(uiValueMin, 3);
                SetRowSpan(uiValueAvg, 3);
                SetRowSpan(uiValueMax, 3);
            }
            SetSubComboVisible(supportsDetails);
        }

        public void SetSubComboVisible(bool visible)
        {
            visible &= supportsDetails;
            uiComboSub.Visible = visible;
            SetRowSpan(uiComboMain, visible ? 1 : 3);
        }


        private void SetupValueCard(AFMSLabel card, string key, ChartMainType chartType, bool highlighted = false)
        {
            (Color accent, Color background, Color border) = GetChartTheme(chartType);
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(3, 0, 3, 0);
            card.Padding = new Padding(2);
            card.BorderRadius = 5;
            card.BorderThickness = 1F;
            card.BorderColor = highlighted ? border : ColorTranslator.FromHtml("#DCE5EF");
            card.BackColor = highlighted ? background : Color.White;
            card.AccessibleName = key;
            statisticsTip.SetToolTip(card, key);
            statisticsTip.SetToolTip(card.InnerLabel, key);
            card.Text = "-";
            card.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            card.TextAlign = ContentAlignment.MiddleCenter;
            card.ForeColor = highlighted ? accent : ColorTranslator.FromHtml("#64748B");
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

        private static Image GetChartIcon(ChartMainType chartType) => chartType switch
        {
            ChartMainType.Velocity => AFMSIcon.Get(AFMSIcons.FlowVelocity, 126),
            ChartMainType.Level => AFMSIcon.Get(AFMSIcons.WaterLevel, 126),
            ChartMainType.Discharge => AFMSIcon.Get(AFMSIcons.FlowRate, 126),
            _ => AFMSIcon.Get(AFMSIcons.Vth, 126)
        };

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
