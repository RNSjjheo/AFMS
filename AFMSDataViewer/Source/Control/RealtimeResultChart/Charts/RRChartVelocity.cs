using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RRChartVelocity : RealtimeResultChart
    {
        private sealed record DeviceOption(
            int Id,
            string SourceType,
            string DeviceKey,
            int Number,
            int TransectCount,
            string Text)
        {
            public override string ToString() => Text;
        }

        private sealed record TransectOption(int Number)
        {
            public override string ToString() => $"{Number}번 측선";
        }

        private bool populating;

        public RRChartVelocity(MeasurementDataHub measurementDataHub, DateTime start, DateTime end)
            : base(ChartMainType.Velocity, start, end, measurementDataHub)
        {
            TopLayout.uiComboMain.SelectedIndexChanged += (_, _) =>
            {
                if (populating) return;
                populating = true;
                try
                {
                    PopulateTransects(true);
                }
                finally
                {
                    populating = false;
                }
                LoadData();
            };
            TopLayout.uiComboSub.SelectedIndexChanged += (_, _) => { if (!populating) LoadData(); };
            TopLayout.uiButtonDetails.Click += ShowAnalysis;
        }

        public override void LoadData()
        {
            IReadOnlyList<VelocityMeasurementSlotSnapshot> slots =
                MeasurementDataHub!.GetVelocitySlots(RangeStart, RangeEnd);
            VelocityMeasurement[] measurements = slots
                .SelectMany(slot => slot.Measurements)
                .ToArray();
            PopulateDevices(measurements);

            DeviceOption? device = TopLayout.uiComboMain.SelectedItem as DeviceOption;
            TransectOption? transect = TopLayout.uiComboSub.SelectedItem as TransectOption;
            if (device == null || transect == null)
            {
                SetSeries([]);
                return;
            }

            List<RealtimeChartPoint> points = slots.Select(slot =>
            {
                VelocityMeasurement? measurement = slot.Measurements.FirstOrDefault(item =>
                    item.SourceType == device.SourceType && item.DeviceKey == device.DeviceKey);
                VelocityTransectMeasurement? value = measurement?.Transects.FirstOrDefault(item =>
                    item.TransectNo == transect.Number);
                bool isValid = value is { IsValid: true } && double.IsFinite(value.Velocity);
                return new RealtimeChartPoint(
                    measurement?.Time ?? slot.SlotTime,
                    isValid ? value!.Velocity : 0D,
                    !isValid);
            }).OrderBy(point => point.Time).ToList();

            SetSeries([new RealtimeChartSeries($"{transect.Number}번 측선", GetSeriesColor(0), points)]);
        }

        private void PopulateDevices(IReadOnlyList<VelocityMeasurement> measurements)
        {
            DeviceOption? previous = TopLayout.uiComboMain.SelectedItem as DeviceOption;
            populating = true;
            try
            {
                TopLayout.uiComboMain.Items.Clear();
                foreach (VelocityMeasurement measurement in measurements
                    .GroupBy(item => (item.SourceType, item.DeviceKey))
                    .Select(group => group.First())
                    .OrderBy(item => item.DeviceNo)
                    .ThenBy(item => item.DeviceId))
                {
                    HydroMeterType meterType = Enum.TryParse(measurement.MeterType, true, out HydroMeterType parsed)
                        ? parsed
                        : HydroMeterType.None;
                    int transectCount = Math.Max(1,
                        measurement.Transects.Select(item => item.TransectNo).DefaultIfEmpty(1).Max());
                    TopLayout.uiComboMain.Items.Add(new DeviceOption(
                        measurement.DeviceId,
                        measurement.SourceType,
                        measurement.DeviceKey,
                        measurement.DeviceNo,
                        transectCount,
                        EnumPaser.GetKorString(meterType)));
                }

                TopLayout.uiComboMain.SelectedItem = TopLayout.uiComboMain.Items.Cast<DeviceOption>()
                    .FirstOrDefault(item => item.Id == previous?.Id && item.SourceType == previous?.SourceType)
                    ?? TopLayout.uiComboMain.Items.Cast<object>().FirstOrDefault();
                PopulateTransects();
            }
            finally
            {
                populating = false;
            }
        }

        private void PopulateTransects(bool reset = false)
        {
            int previous = reset ? 1 : (TopLayout.uiComboSub.SelectedItem as TransectOption)?.Number ?? 1;
            DeviceOption? device = TopLayout.uiComboMain.SelectedItem as DeviceOption;
            TopLayout.uiComboSub.Items.Clear();
            for (int number = 1; number <= (device?.TransectCount ?? 0); number++)
                TopLayout.uiComboSub.Items.Add(new TransectOption(number));
            TopLayout.uiComboSub.SelectedItem = TopLayout.uiComboSub.Items.Cast<TransectOption>()
                .FirstOrDefault(item => item.Number == previous) ?? TopLayout.uiComboSub.Items.Cast<object>().FirstOrDefault();
        }

        private void ShowAnalysis(object? sender, EventArgs e)
        {
            RealtimeChartSeries? series = AvailableSeries
                .Where(series => series.Points.Count > 0)
                .MaxBy(series => series.Points.Max(point => point.Time));
            if (series == null) return;
            RealtimeChartPoint point = series.Points.MaxBy(point => point.Time)!;

            int? transect = (TopLayout.uiComboSub.SelectedItem as TransectOption)?.Number;
            using DlgDataAnalysis dialog = new(ChartType, series, point, transect);
            dialog.ShowDialog(FindForm());
        }
    }
}
