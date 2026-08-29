using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RRChartLevel : RealtimeResultChart
    {
        public RRChartLevel(MeasurementDataHub measurementDataHub, DateTime start, DateTime end)
            : base(ChartMainType.Level, start, end, measurementDataHub)
        {
            TopLayout.SetSubComboVisible(false);
        }

        public override void LoadData()
        {
            MeasurementDataHub dataHub = MeasurementDataHub!;
            List<RealtimeChartPoint> points = dataHub.GetSlots(RangeStart, RangeEnd)
                .Select(slot => slot.MeasurementDevices.WaterLevelGauge)
                .Where(measurement => measurement != null && double.IsFinite(measurement.Value))
                .Select(measurement => new RealtimeChartPoint(measurement!.Time, measurement.Value))
                .OrderBy(point => point.Time)
                .ToList();

            SetSeries(points.Count == 0
                ? []
                : [new RealtimeChartSeries("수위", GetSeriesColor(0), points)]);
        }
    }
}
