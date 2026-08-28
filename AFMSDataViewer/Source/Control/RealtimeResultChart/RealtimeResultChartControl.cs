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
        public Button uiButtonDetails;
        public const int ControlAreaHeight = 75;
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
            ColumnCount = 5;
            RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 7F));
            RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 69F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66F));

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

            uiValueMin = new RoundedTwoLabel(false);
            uiValueAvg = new RoundedTwoLabel(false);
            uiValueMax = new RoundedTwoLabel(false);

            SetupValueCard(uiValueMin, "최소", chartType);
            SetupValueCard(uiValueAvg, "평균", chartType, true);
            SetupValueCard(uiValueMax, "최대", chartType);

            Controls.Add(uiPnIcon, 0, 0);
            SetRowSpan(uiPnIcon, 3);
            Controls.Add(uiComboMain, 1, 0);
            Controls.Add(uiComboSub, 1, 2);

            Controls.Add(uiValueMin, 2, 0);
            Controls.Add(uiValueAvg, 3, 0);
            Controls.Add(uiValueMax, 4, 0);

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
            Controls.Add(uiButtonDetails, 2, 2);
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

        private void SetupValueCard(RoundedTwoLabel card, string key, ChartMainType chartType, bool highlighted = false)
        {
            (Color accent, Color background, Color border) = GetChartTheme(chartType);
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(3, 0, 3, 0);
            card.Padding = new Padding(2);
            card.BorderRadius = 5;
            card.BorderThickness = 1F;
            card.BorderColor = highlighted ? border : ColorTranslator.FromHtml("#DCE5EF");
            card.BackColor = highlighted ? background : Color.White;
            card.MainTablePanel.BackColor = Color.Transparent;
            card.Key = key switch { "최소" => "↓", "평균" => "x\u0304", _ => "↑" };
            card.AccessibleName = key;
            card.MainTablePanel.Controls.Clear();
            card.MainTablePanel.RowStyles.Clear();
            card.MainTablePanel.ColumnStyles.Clear();
            card.MainTablePanel.RowCount = 1;
            card.MainTablePanel.ColumnCount = 2;
            card.MainTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            card.MainTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
            card.MainTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            card.MainTablePanel.Controls.Add(card.LbKey, 0, 0);
            card.MainTablePanel.Controls.Add(card.LbValue, 1, 0);
            statisticsTip.SetToolTip(card, key);
            statisticsTip.SetToolTip(card.LbKey, key);
            statisticsTip.SetToolTip(card.LbValue, key);
            card.Value = "-";
            card.KeyFont = new Font("Segoe UI", 11F, FontStyle.Regular);
            card.ValueFont = new Font("맑은 고딕", 9F, FontStyle.Regular);
            card.LbKey.TextAlign = ContentAlignment.MiddleCenter;
            card.LbKey.Padding = Padding.Empty;
            card.LbValue.TextAlign = ContentAlignment.MiddleCenter;
            card.LbValue.Padding = Padding.Empty;
            card.KeyForeColor = ColorTranslator.FromHtml("#64748B");
            card.ValueForeColor = highlighted ? accent : ColorTranslator.FromHtml("#64748B");
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
            ChartMainType.Velocity => AFMSIcon.FlowVelocity(126, 126),
            ChartMainType.Level => AFMSIcon.WaterLevel(126, 126),
            ChartMainType.Discharge => AFMSIcon.FlowRate(126, 126),
            _ => AFMSIcon.Vth(126, 126)
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
