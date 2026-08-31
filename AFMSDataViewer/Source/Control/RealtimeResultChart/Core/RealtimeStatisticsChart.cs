using AFMSDll;
using ScottPlot;
using ScottPlot.Plottables;
using System.ComponentModel;

namespace AFMSDataViewer
{
    /// <summary>
    /// 평균, 최소, 최대 값을 가로 기준선으로 표시할 수 있는 실시간 결과 차트입니다.
    /// </summary>
    public abstract class RealtimeStatisticsChart : RealtimeResultChart
    {
        private double average;
        private double minimum;
        private double maximum;
        private bool averageEnabled = true;
        private bool minimumEnabled = true;
        private bool maximumEnabled = true;

        protected RealtimeStatisticsChart(
            ChartMainType chartType,
            DateTime rangeStart,
            DateTime rangeEnd,
            MeasurementDataHub? measurementDataHub = null)
            : base(chartType, rangeStart, rangeEnd, measurementDataHub)
        {
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double SetAvg
        {
            get => average;
            set
            {
                average = ValidateValue(value, nameof(value));
                RedrawChart();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double SetMin
        {
            get => minimum;
            set
            {
                minimum = ValidateValue(value, nameof(value));
                RedrawChart();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double SetMax
        {
            get => maximum;
            set
            {
                maximum = ValidateValue(value, nameof(value));
                RedrawChart();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableAvg
        {
            get => averageEnabled;
            set
            {
                averageEnabled = value;
                RedrawChart();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableMin
        {
            get => minimumEnabled;
            set
            {
                minimumEnabled = value;
                RedrawChart();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableMax
        {
            get => maximumEnabled;
            set
            {
                maximumEnabled = value;
                RedrawChart();
            }
        }

        // 요청 API의 오탈자와 기존 호출 코드도 호환합니다.
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EanbleAvg { get => EnableAvg; set => EnableAvg = value; }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EanbleMin { get => EnableMin; set => EnableMin = value; }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EanbleMax { get => EnableMax; set => EnableMax = value; }

        protected override void OnSeriesChanged(IReadOnlyList<RealtimeChartSeries> series)
        {
            base.OnSeriesChanged(series);
            double[] values = series
                .Where(item => !item.SecondaryAxis)
                .SelectMany(item => item.Points)
                .Where(point => !point.IsMissing && double.IsFinite(point.Value))
                .Select(point => point.Value)
                .ToArray();
            if (values.Length == 0) return;

            minimum = values.Min();
            average = values.Average();
            maximum = values.Max();
        }

        protected override void DrawChartOverlays(Plot plot)
        {
            base.DrawChartOverlays(plot);
            if (averageEnabled)
                AddReferenceLine(plot, average);
            if (minimumEnabled)
                AddReferenceLine(plot, minimum);
            if (maximumEnabled)
                AddReferenceLine(plot, maximum);
        }

        private static void AddReferenceLine(Plot plot, double value)
        {
            HorizontalLine line = plot.Add.HorizontalLine(value);
            line.Color = Colors.Blue;
            line.LineWidth = 1F;
            line.LinePattern = LinePattern.Solid;
            line.EnableAutoscale = false;
        }

        private static double ValidateValue(double value, string parameterName)
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(parameterName, "기준선 값은 유한한 숫자여야 합니다.");
            return value;
        }
    }
}
