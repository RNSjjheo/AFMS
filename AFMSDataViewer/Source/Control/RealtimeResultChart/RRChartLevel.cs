using AFMSDll;
using System.Data;

namespace AFMSDataViewer
{
    internal sealed class RRChartLevel : RealtimeResultChart
    {
        public RRChartLevel(DateTime start, DateTime end) : base(ChartMainType.Level, start, end)
        {
            TopLayout.SetSubComboVisible(false);
        }

        public override void LoadData()
        {
            try
            {
                using FBDatabase db = FBProvider.Instance.CreateDatabase();
                DataTable table = db.Execute(new RealtimeLevelChartQuery(RangeStart, RangeEnd).Build(), out string error);
                if (!string.IsNullOrEmpty(error)) { ShowDataError(error); return; }
                SetSeries(RRChartDataMapper.Map(table, GetSeriesColor));
            }
            catch (Exception ex) { ShowDataError(ex.Message); }
        }
    }
}
