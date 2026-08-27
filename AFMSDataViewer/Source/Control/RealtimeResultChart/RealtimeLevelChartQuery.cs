using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RealtimeLevelChartQuery(DateTime rangeStart, DateTime rangeEnd)
        : RealtimeChartQuery(rangeStart, rangeEnd)
    {
        public override string Build()
        {
            string sql = $"SELECT S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} AS SOURCE_TIME, '수위계' AS SERIES, W.CHART_VALUE";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += $" LEFT JOIN (SELECT {_FBTableBase.COL_MEASURE_DATE} AS M_DATE, {_FBTableBase.COL_MEASURE_TIME} AS M_TIME,";
            sql += $" AVG({FbtWATERLEVEL.COL_AVG_WATER_LEVEL}) AS CHART_VALUE FROM {FbtWATERLEVEL.TABLE_NAME}";
            sql += $" WHERE {MeasurementTimeCondition()}";
            sql += $" GROUP BY {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}) W";
            sql += $" ON W.M_DATE = S.{_FBTableBase.COL_MEASURE_DATE} AND W.M_TIME = S.{_FBTableBase.COL_MEASURE_TIME}";
            sql += $" WHERE {SlotTimeCondition()} ORDER BY S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} DESC";
            return sql;
        }
    }
}
