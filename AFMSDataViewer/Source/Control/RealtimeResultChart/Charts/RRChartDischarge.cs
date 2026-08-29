using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RRChartDischarge : RealtimeResultChart
    {
        private sealed record DeviceOption(string Type, int Id, string Text)
        {
            public override string ToString() => Text;
        }
        private sealed record MethodOption(string Method, string Text)
        {
            public override string ToString() => Text;
        }

        private readonly List<RealtimeChartSeries> loadedSeries = new();
        private bool populating;

        public RRChartDischarge(MeasurementDataHub measurementDataHub, DateTime start, DateTime end)
            : base(ChartMainType.Discharge, start, end, measurementDataHub)
        {
            TopLayout.uiComboMain.SelectedIndexChanged += (_, _) =>
            {
                if (populating) return;
                PopulateMethods();
                ApplySelection();
            };
            TopLayout.uiComboSub.SelectedIndexChanged += (_, _) => { if (!populating) ApplySelection(); };
            TopLayout.uiButtonDetails.Click += ShowAnalysis;
        }

        public override void LoadData()
        {
            loadedSeries.Clear();
            foreach (IGrouping<(string DeviceType, int DeviceId, string Method), DischargeMeasurement> group in
                MeasurementDataHub!.GetSlots(RangeStart, RangeEnd)
                    .SelectMany(slot => slot.Discharges)
                    .GroupBy(item => (item.DeviceType, item.DeviceId, item.Method))
                    .OrderBy(group => group.Key.DeviceType == nameof(MeasurementDeviceType.VelocityMeter) ? 0 : 1)
                    .ThenBy(group => group.Key.DeviceId)
                    .ThenBy(group => GetMethodOrder(group.Key.Method)))
            {
                DischargeMeasurement first = group.First();
                string deviceText = GetDeviceText(first.DeviceType, first.DeviceId, first.MeterType);
                string name = first.DeviceType == nameof(MeasurementDeviceType.VelocityMeter)
                    ? $"{GetMethodText(first.Method)} {deviceText}"
                    : deviceText;
                List<RealtimeChartPoint> points = group
                    .OrderBy(item => item.Time)
                    .Select(item => new RealtimeChartPoint(item.Time, item.IsValid ? item.Value : 0D, !item.IsValid))
                    .ToList();
                loadedSeries.Add(new RealtimeChartSeries(
                    name,
                    GetSeriesColor(loadedSeries.Count),
                    points,
                    Key: $"{first.DeviceType}|{first.DeviceId}|{first.Method}",
                    LegendText: GetMethodText(first.Method),
                    DeviceType: first.DeviceType,
                    DeviceId: first.DeviceId,
                    DischargeMethod: first.Method,
                    MeterType: first.MeterType));
            }

            PopulateDevices();
            ApplySelection();
        }

        private void PopulateDevices()
        {
            DeviceOption? previous = TopLayout.uiComboMain.SelectedItem as DeviceOption;
            populating = true;
            try
            {
                TopLayout.uiComboMain.Items.Clear();
                foreach (DeviceOption option in loadedSeries
                    .Where(series => series.DeviceType != null && series.DeviceId.HasValue)
                    .GroupBy(series => (series.DeviceType!, series.DeviceId!.Value))
                    .Select(group => new DeviceOption(group.Key.Item1, group.Key.Item2,
                        GetDeviceText(group.Key.Item1, group.Key.Item2, group.First().MeterType)))
                    .OrderBy(option => option.Type == nameof(MeasurementDeviceType.VelocityMeter) ? 0 : 1)
                    .ThenBy(option => option.Id))
                    TopLayout.uiComboMain.Items.Add(option);
                TopLayout.uiComboMain.SelectedItem = TopLayout.uiComboMain.Items.Cast<DeviceOption>()
                    .FirstOrDefault(item => item.Type == previous?.Type && item.Id == previous?.Id)
                    ?? TopLayout.uiComboMain.Items.Cast<object>().FirstOrDefault();
                PopulateMethods();
            }
            finally { populating = false; }
        }

        private void PopulateMethods()
        {
            DeviceOption? device = TopLayout.uiComboMain.SelectedItem as DeviceOption;
            string? previous = (TopLayout.uiComboSub.SelectedItem as MethodOption)?.Method;
            bool wasPopulating = populating;
            populating = true;
            try
            {
                TopLayout.uiComboSub.Items.Clear();
                TopLayout.uiComboSub.Items.Add("전체");
                if (device != null)
                foreach (string method in loadedSeries
                    .Where(series => series.DeviceType == device.Type && series.DeviceId == device.Id)
                    .Select(series => series.DischargeMethod).Where(method => !string.IsNullOrWhiteSpace(method))
                    .Cast<string>().Distinct().OrderBy(GetMethodOrder))
                    TopLayout.uiComboSub.Items.Add(new MethodOption(method, GetMethodText(method)));
                TopLayout.uiComboSub.SelectedItem = TopLayout.uiComboSub.Items.Cast<object>().OfType<MethodOption>()
                    .FirstOrDefault(item => item.Method == previous) ?? TopLayout.uiComboSub.Items[0];
                TopLayout.SetSubComboVisible(device?.Type == nameof(MeasurementDeviceType.VelocityMeter));
            }
            finally { populating = wasPopulating; }
        }

        private void ApplySelection()
        {
            DeviceOption? device = TopLayout.uiComboMain.SelectedItem as DeviceOption;
            MethodOption? method = TopLayout.uiComboSub.SelectedItem as MethodOption;
            SetSeries(loadedSeries.Where(series => device != null &&
                series.DeviceType == device.Type && series.DeviceId == device.Id &&
                (method == null || series.DischargeMethod == method.Method)));
        }

        private void ShowAnalysis(object? sender, EventArgs e)
        {
            RealtimeChartSeries? series = AvailableSeries
                .Where(series => series.Points.Count > 0)
                .MaxBy(series => series.Points.Max(point => point.Time));
            if (series == null) return;
            RealtimeChartPoint point = series.Points.MaxBy(point => point.Time)!;

            using DlgDataAnalysis dialog = new(ChartType, series, point, null);
            dialog.ShowDialog(FindForm());
        }

        private static string GetDeviceText(string type, int id, string? meterType)
        {
            if (type == nameof(MeasurementDeviceType.WaterLevelGauge)) return id > 0 ? $"{id}번 수위계" : "수위계";
            if (type != nameof(MeasurementDeviceType.VelocityMeter)) return $"{type} {id}";
            HydroMeterType parsed = Enum.TryParse(meterType, true, out HydroMeterType value) ? value : HydroMeterType.None;
            return EnumPaser.GetKorString(parsed);
        }

        private static string GetMethodText(string method) =>
            Enum.TryParse(method, true, out DischargeMethod value) ? EnumPaser.GetKorString(value) : method;
        private static int GetMethodOrder(string method) =>
            Enum.TryParse(method, true, out DischargeMethod value) ? (int)value : int.MaxValue;
    }
}
