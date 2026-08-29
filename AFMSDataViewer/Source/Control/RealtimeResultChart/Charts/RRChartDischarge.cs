using AFMSDll;
using System.Data;

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

        public RRChartDischarge(DateTime start, DateTime end) : base(ChartMainType.Discharge, start, end)
        {
            TopLayout.uiComboMain.SelectedIndexChanged += (_, _) =>
            {
                if (populating) return;
                PopulateMethods();
                ApplySelection();
            };
            TopLayout.uiComboSub.SelectedIndexChanged += (_, _) => { if (!populating) ApplySelection(); };
            PointDoubleClicked += (_, e) =>
            {
                using DlgDataAnalysis dialog = new(ChartType, e.Series, e.Point, null);
                dialog.ShowDialog(FindForm());
            };
        }

        public override void LoadData()
        {
            try
            {
                using FBDatabase db = FBProvider.Instance.CreateDatabase();
                DataTable table = db.Execute(new RealtimeDischargeChartQuery(RangeStart, RangeEnd).Build(), out string error);
                if (!string.IsNullOrEmpty(error)) { ShowDataError(error); return; }
                loadedSeries.Clear();
                loadedSeries.AddRange(RRChartDataMapper.Map(table, GetSeriesColor, GetSeriesName)
                    .Select(series => series with
                    {
                        LegendText = string.IsNullOrWhiteSpace(series.DischargeMethod)
                            ? series.Name
                            : GetMethodText(series.DischargeMethod)
                    }));
                PopulateDevices();
                ApplySelection();
            }
            catch (Exception ex) { ShowDataError(ex.Message); }
        }

        private string GetSeriesName(DataRow row, string fallback)
        {
            string type = row.Table.Columns.Contains("DEVICE_TYPE") ? row["DEVICE_TYPE"].ToText().Trim() : string.Empty;
            int id = row.Table.Columns.Contains("DEVICE_ID") && row["DEVICE_ID"] != DBNull.Value
                ? Convert.ToInt32(row["DEVICE_ID"]) : 0;
            string method = row.Table.Columns.Contains("DISCHARGE_METHOD") ? row["DISCHARGE_METHOD"].ToText().Trim() : string.Empty;
            if (type != nameof(MeasurementDeviceType.VelocityMeter)) return fallback;
            return $"{GetMethodText(method)} {GetDeviceText(type, id, row["METER_TYPE"].ToText())}";
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
