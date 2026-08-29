using AFMSDll;
using Krypton.Toolkit;
using System;
using System.Collections.Generic;
using System.Text;


namespace AFMSDataViewer
{
    public class ChartSelectPanel:AFMSPanel
    {
        public event EventHandler<ChartSelectedEventArgs>? ChartSelected;
        public event EventHandler? MaximizeRequested;

        private TableLayoutPanel uiTpMain;
        public TableLayoutPanel uiTpBtnArr;
        private ToolStrip uiMmBar;
        private AFMSButton uiBtnChartVelo;
        private AFMSButton uiBtnChartLevel;
        private AFMSButton uiBtnChartDisc;
        private AFMSButton uiBtnChartVTHL;
        private RealtimeResultChart? uiResultChart;
        private DateTime rangeStart;
        private DateTime rangeEnd;
        public ChartSelectPanel()
        {
            const float NODE_WIDTH = 70F;
            this.BackColor = DllColorHelper.HexToColor("#FFFFFF");

            uiTpMain = new TableLayoutPanel();
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.RowStyles.Clear();
            uiTpMain.ColumnStyles.Clear();
            uiTpMain.RowCount = 4;
            uiTpMain.ColumnCount = 3;
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, NODE_WIDTH + 5));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (NODE_WIDTH * 4) + 20));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiTpMain.BackColor = Color.Transparent;

            uiTpBtnArr = new TableLayoutPanel();
            uiTpBtnArr.Dock = DockStyle.Fill;
            uiTpBtnArr.RowStyles.Clear();
            uiTpBtnArr.ColumnStyles.Clear();
            uiTpBtnArr.RowCount = 1;
            uiTpBtnArr.ColumnCount = 6;
            uiTpBtnArr.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpBtnArr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiTpBtnArr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NODE_WIDTH));
            uiTpBtnArr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NODE_WIDTH));
            uiTpBtnArr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NODE_WIDTH));
            uiTpBtnArr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NODE_WIDTH));
            uiTpBtnArr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiTpBtnArr.BackColor = Color.White;

            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Text = "차트를 선택해주세요";
            label.BackColor = Color.Transparent;

            uiMmBar = new ToolStrip();
            uiMmBar.AutoSize = true;
            uiMmBar.ImageScalingSize = new System.Drawing.Size(24, 24);
            uiMmBar.BackColor = DllColorHelper.HexToColor("#FFFFFF");
            uiMmBar.Dock = DockStyle.Fill;
            uiMmBar.GripStyle = ToolStripGripStyle.Hidden;
            uiMmBar.Padding = Padding.Empty;
            uiMmBar.Margin = Padding.Empty;
            uiMmBar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            uiMmBar.Renderer = new BorderlessToolStripRenderer();

            uiBtnChartVelo = AddCenteredButton("유속", AFMSIcon.Get(AFMSIcons.FlowVelocitySelect, 24), ChartMainType.Velocity);
            uiBtnChartLevel = AddCenteredButton("수위", AFMSIcon.Get(AFMSIcons.WaterLevelSelect, 24), ChartMainType.Level);
            uiBtnChartDisc = AddCenteredButton("유량", AFMSIcon.Get(AFMSIcons.FlowRateSelect, 24), ChartMainType.Discharge);
            uiBtnChartVTHL = AddCenteredButton("전원", AFMSIcon.Get(AFMSIcons.VthSelect, 24), ChartMainType.VTH);

            uiTpBtnArr.Controls.Add(uiBtnChartVelo, 1, 0);
            uiTpBtnArr.Controls.Add(uiBtnChartLevel, 2, 0);
            uiTpBtnArr.Controls.Add(uiBtnChartDisc, 3, 0);
            uiTpBtnArr.Controls.Add(uiBtnChartVTHL, 4, 0);

            uiTpMain.Controls.Add(label, 1, 1);
            uiTpMain.Controls.Add(uiTpBtnArr, 1, 2);

            Controls.Add(uiTpMain);
        }



        private AFMSButton AddCenteredButton(string text, Image image, ChartMainType chartType)
        {
            AFMSButton button = new AFMSButton();
            button.Text = text;
            button.Image = image;
            button.TextImageRelation = TextImageRelation.ImageAboveText;
            button.ImageAlign = ContentAlignment.TopCenter;
            button.TextAlign = ContentAlignment.BottomCenter;
            button.Dock = DockStyle.Fill;
            button.Tag = chartType;

            button.Click += ChartButton_Click;

            return button;
        }

        private void ChartButton_Click(object? sender, EventArgs e)
        {
            if (sender is not AFMSButton button) return;

            if (button.Tag is not ChartMainType chartType) return;

            ChartSelected?.Invoke(this, new ChartSelectedEventArgs(chartType, button.Text));

            ShowChart(chartType);
        }

        private void ShowChart(ChartMainType chartType)
        {
            uiResultChart?.Dispose();
            uiResultChart = chartType switch
            {
                ChartMainType.Velocity => new RRChartVelocity(rangeStart, rangeEnd),
                ChartMainType.Level => new RRChartLevel(rangeStart, rangeEnd),
                ChartMainType.Discharge => new RRChartDischarge(rangeStart, rangeEnd),
                _ => new RRChartVTH(rangeStart, rangeEnd)
            };
            ChartAxisRange axisRange = DataViewerChartSettings.GetAxisRange(chartType);
            if (axisRange.TryGetFixedRange(out double minimumY, out double maximumY))
                uiResultChart.SetYAxisRange(minimumY, maximumY);
            uiResultChart.MaximizeRequested += UiResultChart_MaximizeRequested;
            uiResultChart.CloseRequested += UiResultChart_CloseRequested;
            Controls.Clear();
            Controls.Add(uiResultChart);
            uiResultChart.LoadData();
        }

        public void SetTimeRange(DateTime start, DateTime end)
        {
            rangeStart = start;
            rangeEnd = end;
            uiResultChart?.SetTimeRange(start, end);
        }

        /// <summary>표시 중인 실시간 차트를 제거하고 차트 선택 화면으로 돌아갑니다.</summary>
        public void ResetToChartSelection()
        {
            if (uiResultChart != null)
            {
                uiResultChart.MaximizeRequested -= UiResultChart_MaximizeRequested;
                uiResultChart.CloseRequested -= UiResultChart_CloseRequested;
                uiResultChart.Dispose();
                uiResultChart = null;
            }

            Controls.Clear();
            Controls.Add(uiTpMain);
        }

        private void UiResultChart_MaximizeRequested(object? sender, EventArgs e)
        {
            MaximizeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void UiResultChart_CloseRequested(object? sender, EventArgs e)
        {
            ResetToChartSelection();
        }
    }
}
