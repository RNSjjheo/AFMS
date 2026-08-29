using AFMSDll;
using System.Data;

namespace AFMSDataViewer
{
    internal sealed class RRChartVTH : RealtimeResultChart
    {
        private sealed record ValueOption(PowerChartValueType Type, string Text)
        {
            public override string ToString() => Text;
        }
        public RRChartVTH(DateTime start, DateTime end) : base(ChartMainType.VTH, start, end)
        {
            TopLayout.SetSubComboVisible(false);
            TopLayout.uiComboMain.Items.Add(new ValueOption(PowerChartValueType.Input, "입력전압"));
            TopLayout.uiComboMain.Items.Add(new ValueOption(PowerChartValueType.Output, "출력전압"));
            TopLayout.uiComboMain.SelectedIndex = 0;
            TopLayout.uiComboMain.SelectedIndexChanged += (_, _) => LoadData();
        }

        public override void LoadData()
        {
            try
            {
                using FBDatabase db = FBProvider.Instance.CreateDatabase();
                PowerChartValueType type = (TopLayout.uiComboMain.SelectedItem as ValueOption)?.Type ?? PowerChartValueType.Input;
                DataTable table = db.Execute(new RealtimePowerChartQuery(RangeStart, RangeEnd, type).Build(), out string error);
                if (!string.IsNullOrEmpty(error)) { ShowDataError(error); return; }
                SetSeries(RRChartDataMapper.Map(table, GetSeriesColor));
            }
            catch (Exception ex) { ShowDataError(ex.Message); }
        }
    }
}
