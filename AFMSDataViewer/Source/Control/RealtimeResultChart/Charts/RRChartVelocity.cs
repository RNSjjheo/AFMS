using AFMSDll;
using System.Data;

namespace AFMSDataViewer
{
    internal sealed class RRChartVelocity : RealtimeResultChart
    {
        private sealed record DeviceOption(int Id, string SourceType, int Number, int TransectCount, string Text)
        {
            public override string ToString() => Text;
        }
        private sealed record TransectOption(int Number)
        {
            public override string ToString() => $"{Number}번 측선";
        }

        private bool populating;

        public RRChartVelocity(DateTime start, DateTime end) : base(ChartMainType.Velocity, start, end)
        {
            TopLayout.uiComboMain.SelectedIndexChanged += (_, _) =>
            {
                if (populating) return;
                PopulateTransects(true);
                LoadData();
            };
            TopLayout.uiComboSub.SelectedIndexChanged += (_, _) => { if (!populating) LoadData(); };
            PointDoubleClicked += ShowAnalysis;
        }

        public override void LoadData()
        {
            try
            {
                using FBDatabase db = FBProvider.Instance.CreateDatabase();
                PopulateDevices(db);
                DeviceOption? device = TopLayout.uiComboMain.SelectedItem as DeviceOption;
                TransectOption? transect = TopLayout.uiComboSub.SelectedItem as TransectOption;
                string sql = new RealtimeVelocityChartQuery(RangeStart, RangeEnd,
                    device?.SourceType, device?.Number, transect?.Number).Build();
                DataTable table = db.Execute(sql, out string error);
                if (!string.IsNullOrEmpty(error)) { ShowDataError(error); return; }
                SetSeries(RRChartDataMapper.Map(table, GetSeriesColor));
            }
            catch (Exception ex) { ShowDataError(ex.Message); }
        }

        private void PopulateDevices(FBDatabase db)
        {
            DeviceOption? previous = TopLayout.uiComboMain.SelectedItem as DeviceOption;
            DataTable table = db.Execute(new RealtimeVelocityChartQuery(RangeStart, RangeEnd).BuildDeviceList(), out string error);
            if (!string.IsNullOrEmpty(error)) return;
            populating = true;
            try
            {
                TopLayout.uiComboMain.Items.Clear();
                foreach (DataRow row in table.Rows)
                {
                    int id = Convert.ToInt32(row["DEVICE_ID"]);
                    int number = row["DEVICE_NO"] == DBNull.Value ? id : Convert.ToInt32(row["DEVICE_NO"]);
                    int count = row["TRANSECT_COUNT"] == DBNull.Value ? 1 : Math.Max(1, Convert.ToInt32(row["TRANSECT_COUNT"]));
                    string meterType = row["METER_TYPE"].ToText().Trim();
                    HydroMeterType parsed = Enum.TryParse(meterType, true, out HydroMeterType value) ? value : HydroMeterType.None;
                    TopLayout.uiComboMain.Items.Add(new DeviceOption(id, row["SOURCE_TYPE"].ToText().Trim(),
                        number, count, EnumPaser.GetKorString(parsed)));
                }
                TopLayout.uiComboMain.SelectedItem = TopLayout.uiComboMain.Items.Cast<DeviceOption>()
                    .FirstOrDefault(item => item.Id == previous?.Id) ?? TopLayout.uiComboMain.Items.Cast<object>().FirstOrDefault();
                PopulateTransects();
            }
            finally { populating = false; }
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

        private void ShowAnalysis(object? sender, RealtimeChartPointEventArgs e)
        {
            int? transect = (TopLayout.uiComboSub.SelectedItem as TransectOption)?.Number;
            using DlgDataAnalysis dialog = new(ChartType, e.Series, e.Point, transect);
            dialog.ShowDialog(FindForm());
        }
    }
}
