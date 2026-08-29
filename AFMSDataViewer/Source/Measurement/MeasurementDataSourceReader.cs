using System.Data;
using System.Globalization;
using AFMSDll;

namespace AFMSDataViewer
{
    internal static class MeasurementDataSourceReader
    {
        public static DataTable Execute(FBDatabase database, string sql, string errorContext)
        {
            DataTable table = database.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"{errorContext}: {error}");

            return table;
        }

        public static string BuildTimeCondition(DateTime from, DateTime to, string? alias = null)
        {
            string fromValue = from.ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
            string toValue = to.ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
            string prefix = string.IsNullOrWhiteSpace(alias) ? string.Empty : alias.Trim() + ".";
            string measuredAt = $"({prefix}{_FBTableBase.COL_MEASURE_DATE} || ' ' || {prefix}{_FBTableBase.COL_MEASURE_TIME})";
            return $"{measuredAt} >= '{fromValue}' AND {measuredAt} <= '{toValue}'";
        }

        public static bool TryReadTime(DataRow row, out DateTime time)
        {
            string value = $"{Convert.ToString(row[0], CultureInfo.InvariantCulture)} {Convert.ToString(row[1], CultureInfo.InvariantCulture)}";
            return DateTime.TryParseExact(value, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out time);
        }

        public static IEnumerable<DateTime> EnumerateSlots(DateTime from, DateTime to)
        {
            DateTime first = MeasurementDataHub.AlignToSlot(from);
            DateTime last = MeasurementDataHub.AlignToSlot(to);
            for (DateTime slotTime = first; slotTime <= last; slotTime += MeasurementDataHub.SlotInterval)
                yield return slotTime;
        }
    }
}
