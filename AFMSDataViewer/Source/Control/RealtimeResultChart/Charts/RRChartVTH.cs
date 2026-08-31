using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RRChartVTH : RealtimeResultChart
    {
        private sealed record ValueOption(PowerChartValueType Type, string Text)
        {
            public override string ToString() => Text;
        }
        public RRChartVTH(MeasurementDataHub measurementDataHub, DateTime start, DateTime end)
            : base(ChartMainType.VTH, start, end, measurementDataHub)
        {
            TopLayout.SetSubComboVisible(false);
            TopLayout.uiComboMain.Items.Add(new ValueOption(PowerChartValueType.Input, "입력전압"));
            TopLayout.uiComboMain.Items.Add(new ValueOption(PowerChartValueType.Output, "출력전압"));
            TopLayout.uiComboMain.SelectedIndex = 0;
            TopLayout.uiComboMain.SelectedIndexChanged += (_, _) => LoadData();
        }

        public override void LoadData()
        {
            PowerChartValueType type = (TopLayout.uiComboMain.SelectedItem as ValueOption)?.Type ?? PowerChartValueType.Input;
            List<RealtimeChartPoint> points = MeasurementDataHub!.GetSlots(RangeStart, RangeEnd)
                .Select(slot =>
                {
                    VoltageMeasurement? measurement = slot.MeasurementDevices.Power.Measurement;
                    double? value = type == PowerChartValueType.Input
                        ? measurement?.InputVoltage
                        : measurement?.OutputVoltage;
                    bool isValid = measurement != null &&
                        (type == PowerChartValueType.Input
                            ? measurement.IsInputValid
                            : measurement.IsOutputValid) &&
                        value.HasValue && double.IsFinite(value.Value);
                    return new RealtimeChartPoint(
                        measurement?.Time ?? slot.SlotTime,
                        isValid ? value!.Value : 0D,
                        !isValid);
                })
                .OrderBy(point => point.Time)
                .ToList();

            string seriesName = type == PowerChartValueType.Input ? "입력전압" : "출력전압";
            SetSeries(points.Count == 0
                ? []
                : [new RealtimeChartSeries(seriesName, GetSeriesColor(0), points)]);
        }
    }

    internal enum PowerChartValueType
    {
        Input,
        Output
    }
}
