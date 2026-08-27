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
            BackColor = Color.Transparent;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            RowStyles.Clear();
            ColumnStyles.Clear();
            RowCount = 1;
            ColumnCount = 7;
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));

            uiPnIcon = new Panel();
            uiPnIcon.Dock = DockStyle.Fill;
            uiPnIcon.Margin = new Padding(0, 7, 6, 7);
            uiPnIcon.BackgroundImage = GetChartIcon(chartType);
            uiPnIcon.BackgroundImageLayout = ImageLayout.Zoom;

            uiComboMain = new AFMSComboBox();
            uiComboMain.Dock = DockStyle.Fill;
            uiComboMain.Margin = new Padding(0, 10, 6, 10);
            uiComboMain.PlaceholderText = GetMainPlaceholder(chartType);
            uiComboMain.BorderRadius = 15;

            uiComboSub = new AFMSComboBox();
            uiComboSub.Dock = DockStyle.Fill;
            uiComboSub.Margin = new Padding(0, 10, 6, 10);
            uiComboSub.PlaceholderText = GetSubPlaceholder(chartType);
            uiComboSub.BorderRadius = 15;

            uiValueMin = new RoundedTwoLabel(false);
            uiValueAvg = new RoundedTwoLabel(false);
            uiValueMax = new RoundedTwoLabel(false);
            SetupValueCard(uiValueMin, "최소");
            SetupValueCard(uiValueAvg, "평균", true);
            SetupValueCard(uiValueMax, "최대");

            Controls.Add(uiPnIcon, 0, 0);
            Controls.Add(uiComboMain, 1, 0);
            Controls.Add(uiComboSub, 2, 0);

            Controls.Add(uiValueMin, 4, 0);
            Controls.Add(uiValueAvg, 5, 0);
            Controls.Add(uiValueMax, 6, 0);
        }

        private static void SetupValueCard(RoundedTwoLabel card, string key, bool highlighted = false)
        {
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(3);
            card.Padding = new Padding(2);
            card.BorderRadius = 5;
            card.BorderThickness = 1F;
            card.BorderColor = highlighted ? ColorTranslator.FromHtml("#A7F3D0") : ColorTranslator.FromHtml("#DCE5EF");
            card.BackColor = highlighted ? ColorTranslator.FromHtml("#E8FFF5") : Color.White;
            card.MainTablePanel.BackColor = Color.Transparent;
            card.Key = key;
            card.Value = "-";
            card.KeyFont = new Font("맑은 고딕", 8F, FontStyle.Regular);
            card.ValueFont = new Font("맑은 고딕", 10F, FontStyle.Bold);
            card.KeyForeColor = ColorTranslator.FromHtml("#64748B");
            card.ValueForeColor = highlighted ? ColorTranslator.FromHtml("#059669") : ColorTranslator.FromHtml("#64748B");
        }

        private static Image GetChartIcon(ChartMainType chartType) => chartType switch
        {
            ChartMainType.Velocity => AFMSIcon.FlowVelocity(36, 36),
            ChartMainType.Level => AFMSIcon.WaterLevel(36, 36),
            ChartMainType.Discharge => AFMSIcon.FlowRate(36, 36),
            _ => AFMSIcon.Vth(36, 36)
        };

        private static string GetMainPlaceholder(ChartMainType chartType) => chartType switch
        {
            ChartMainType.Velocity => "1번 유속계",
            ChartMainType.Level => "1번 수위계",
            ChartMainType.Discharge => "1번 유량계",
            _ => "1번 전원계"
        };

        private static string GetSubPlaceholder(ChartMainType chartType) => chartType == ChartMainType.Discharge
            ? "유량법 선택"
            : "측정값 선택";
    }
}
