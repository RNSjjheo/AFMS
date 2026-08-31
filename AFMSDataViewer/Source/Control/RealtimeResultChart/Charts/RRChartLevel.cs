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
                .Select(slot =>
                {
                    LevelMeasurement? measurement = slot.MeasurementDevices.WaterLevelGauge.Measurement;
                    bool isValid = measurement is { IsValid: true } && double.IsFinite(measurement.Value);
                    return new RealtimeChartPoint(
                        measurement?.Time ?? slot.SlotTime,
                        isValid ? measurement!.Value : 0D,
                        !isValid);
                })
                .OrderBy(point => point.Time)
                .ToList();

            SetSeries(points.Count == 0
                ? []
                : [new RealtimeChartSeries("수위", GetSeriesColor(0), points)]);
        }
    }
}
