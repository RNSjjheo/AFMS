using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RealtimePowerChartQuery(DateTime rangeStart, DateTime rangeEnd)
        : RealtimeChartQuery(rangeStart, rangeEnd)
    {
        public override string Build()
        {
            string values = $"SELECT {_FBTableBase.COL_MEASURE_DATE} AS M_DATE, {_FBTableBase.COL_MEASURE_TIME} AS M_TIME,";
            values += $" AVG({FbtVTHLOGGER.COL_VOLT}) AS INPUT_VALUE, AVG({FbtVTHLOGGER.COL_DCCHARGE}) AS CHARGE_VALUE,";
            values += $" AVG({FbtVTHLOGGER.COL_DCBATTERY}) AS BATTERY_VALUE FROM {FbtVTHLOGGER.TABLE_NAME}";
            values += $" WHERE {MeasurementTimeCondition()}";
            values += $" GROUP BY {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}";
            string join = $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S LEFT JOIN ({values}) V";
            join += $" ON V.M_DATE = S.{_FBTableBase.COL_MEASURE_DATE} AND V.M_TIME = S.{_FBTableBase.COL_MEASURE_TIME}";
            string condition = $" WHERE {SlotTimeCondition()}";
            string slotTime = SlotTimeValue();
            string sql = $"SELECT {slotTime} AS SOURCE_TIME, '입력 전압' AS SERIES, V.INPUT_VALUE AS CHART_VALUE{join}{condition}";
            sql += $" UNION ALL SELECT {slotTime} AS SOURCE_TIME, '충전 전압' AS SERIES, V.CHARGE_VALUE AS CHART_VALUE{join}{condition}";
            sql += $" UNION ALL SELECT {slotTime} AS SOURCE_TIME, '배터리 전압' AS SERIES, V.BATTERY_VALUE AS CHART_VALUE{join}{condition}";
            sql += " ORDER BY 1 DESC";
            return sql;
        }
    }
}
