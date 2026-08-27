using AFMSDll;

namespace AFMSDataViewer
{
    internal interface IRealtimeChartQuery
    {
        string Build();
    }

    internal abstract class RealtimeChartQuery : IRealtimeChartQuery
    {
        protected RealtimeChartQuery(DateTime rangeStart, DateTime rangeEnd)
        {
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
        }

        protected DateTime RangeStart { get; }
        protected DateTime RangeEnd { get; }

        public abstract string Build();

        protected string MeasurementTimeCondition(string? alias = null)
        {
            string prefix = string.IsNullOrEmpty(alias) ? string.Empty : alias + ".";
            string measureDate = $"{prefix}{_FBTableBase.COL_MEASURE_DATE}";
            string measureTime = $"{prefix}{_FBTableBase.COL_MEASURE_TIME}";

            return $"{measureDate} BETWEEN '{RangeStart:yyyyMMdd}' AND '{RangeEnd:yyyyMMdd}'" +
                $" AND ({measureDate} > '{RangeStart:yyyyMMdd}' OR {measureTime} >= '{RangeStart:HHmmss}')" +
                $" AND ({measureDate} < '{RangeEnd:yyyyMMdd}' OR {measureTime} <= '{RangeEnd:HHmmss}')";
        }

        protected string SlotTimeCondition(string alias = "S") =>
            $"{alias}.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} >= '{RangeStart:yyyy-MM-dd HH:mm:ss}' AND " +
            $"{alias}.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} <= '{RangeEnd:yyyy-MM-dd HH:mm:ss}'";

        protected static string SlotTimeValue(string alias = "S") =>
            $"CAST({alias}.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} AS TIMESTAMP)";
    }

    internal static class RealtimeChartQueryFactory
    {
        public static IRealtimeChartQuery Create(ChartMainType chartType, DateTime rangeStart, DateTime rangeEnd,
            string? velocitySourceType = null, int? velocityDeviceNo = null) => chartType switch
        {
            ChartMainType.Discharge => new RealtimeDischargeChartQuery(rangeStart, rangeEnd),
            ChartMainType.Velocity => new RealtimeVelocityChartQuery(rangeStart, rangeEnd, velocitySourceType, velocityDeviceNo),
            ChartMainType.Level => new RealtimeLevelChartQuery(rangeStart, rangeEnd),
            _ => new RealtimePowerChartQuery(rangeStart, rangeEnd)
        };
    }
}
