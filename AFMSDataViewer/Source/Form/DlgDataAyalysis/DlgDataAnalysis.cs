using AFMSDll;

namespace AFMSDataViewer
{
    internal class DlgDataAnalysis : AFMSForm
    {
        private readonly AFMSTabControl analysisTabs = new() { Dock = DockStyle.Fill };

        public ChartMainType SourceChartType { get; }
        public RealtimeChartSeries SelectedSeries { get; }
        public RealtimeChartPoint SelectedPoint { get; }
        public int? TransectNo { get; }

        public DlgDataAnalysis(ChartMainType sourceChartType, RealtimeChartSeries selectedSeries,
            RealtimeChartPoint selectedPoint, int? transectNo = null)
        {
            SourceChartType = sourceChartType;
            SelectedSeries = selectedSeries;
            SelectedPoint = selectedPoint;
            TransectNo = transectNo;

            Text = $"데이터 분석 - {selectedSeries.Name} ({selectedPoint.Time:yyyy-MM-dd HH:mm})";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1000, 700);
            ShowMinimizeButton = false;
            ShowInfoButton = false;
            ShowInTaskbar = false;
            Controls.Add(analysisTabs);
            ConfigureAnalysisTabs();
        }

        private void ConfigureAnalysisTabs()
        {
            // WinForms TabPage.Visible does not hide tab headers. Add only applicable pages.
            switch (SourceChartType)
            {
                case ChartMainType.Velocity:
                    analysisTabs.TabPages.Add(new TabPage("유속 분석"));
                    break;
                case ChartMainType.Discharge:
                    analysisTabs.TabPages.Add(new TabPage("유량 분석"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SourceChartType), SourceChartType,
                        "유속 또는 유량 차트에서만 데이터 분석을 실행할 수 있습니다.");
            }
        }
    }
}
