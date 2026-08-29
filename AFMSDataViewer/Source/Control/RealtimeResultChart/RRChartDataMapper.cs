using AFMSDll;
using System.Data;
using System.Globalization;

namespace AFMSDataViewer
{
    internal static class RRChartDataMapper
    {
        public static List<RealtimeChartSeries> Map(
            DataTable table,
            Func<int, Color> colorSelector,
            Func<DataRow, string, string>? nameSelector = null)
        {
            List<RealtimeChartSeries> result = new();
            foreach (IGrouping<string, DataRow> group in table.Rows.Cast<DataRow>()
                .Where(row => row["SERIES"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["SERIES"].ToText()))
                .GroupBy(row => row["SERIES"].ToText()))
            {
                List<RealtimeChartPoint> points = group.Reverse().Select(row =>
                {
                    bool missing = row["CHART_VALUE"] == DBNull.Value;
                    double value = missing ? 0D : Convert.ToDouble(row["CHART_VALUE"]);
                    return new RealtimeChartPoint(ParseSourceTime(row["SOURCE_TIME"]), value, missing);
                }).Where(point => double.IsFinite(point.Value)).ToList();
                if (points.Count == 0) continue;

                DataRow first = group.First();
                string? deviceType = GetText(table, first, "DEVICE_TYPE");
                int? deviceId = GetInt32(table, first, "DEVICE_ID");
                string? method = GetText(table, first, "DISCHARGE_METHOD");
                string? meterType = GetText(table, first, "METER_TYPE");
                string name = nameSelector?.Invoke(first, group.Key) ?? group.Key;
                result.Add(new RealtimeChartSeries(name, colorSelector(result.Count), points,
                    Key: $"{deviceType}|{deviceId}|{method}|{group.Key}",
                    DeviceType: deviceType, DeviceId: deviceId,
                    DischargeMethod: method, MeterType: meterType));
            }
            return result;
        }

        public static DateTime ParseSourceTime(object value)
        {
            if (value is DateTime time) return time;
            string text = value.ToText().Trim();
            if (DateTime.TryParseExact(text, new[] { "yyyyMMdd HHmmss", "yyyyMMdd HHmmss.fff" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out time)) return time;
            return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        private static string? GetText(DataTable table, DataRow row, string column) =>
            table.Columns.Contains(column) ? row[column].ToText().Trim() : null;

        private static int? GetInt32(DataTable table, DataRow row, string column) =>
            table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToInt32(row[column]) : null;
    }
}
