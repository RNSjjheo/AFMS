using AFMSDll;

namespace AFMSDataViewer
{
    internal enum PowerChartValueType
    {
        Input,
        Output
    }

    internal sealed class RealtimePowerChartQuery(DateTime rangeStart, DateTime rangeEnd,
        PowerChartValueType? valueType = null)
        : RealtimeChartQuery(rangeStart, rangeEnd)
    {
        public override string Build()
        {
            PowerChartValueType selectedValueType = valueType ?? PowerChartValueType.Input;
            string valueColumn = selectedValueType == PowerChartValueType.Output
                ? FbtVTHLOGGER.COL_DCBATTERY
                : FbtVTHLOGGER.COL_DCCHARGE;
            string seriesName = selectedValueType == PowerChartValueType.Output ? "출력전압" : "입력전압";

            string values = $"SELECT {_FBTableBase.COL_MEASURE_DATE} AS M_DATE, {_FBTableBase.COL_MEASURE_TIME} AS M_TIME,";
            values += $" AVG({valueColumn}) AS CHART_VALUE FROM {FbtVTHLOGGER.TABLE_NAME}";
            values += $" WHERE {MeasurementTimeCondition()}";
            values += $" GROUP BY {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}";
            string join = $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S LEFT JOIN ({values}) V";
            join += $" ON V.M_DATE = S.{_FBTableBase.COL_MEASURE_DATE} AND V.M_TIME = S.{_FBTableBase.COL_MEASURE_TIME}";
            string condition = $" WHERE {SlotTimeCondition()}";
            string slotTime = SlotTimeValue();
            string sql = $"SELECT {slotTime} AS SOURCE_TIME, '{seriesName}' AS SERIES, V.CHART_VALUE AS CHART_VALUE{join}{condition}";
            sql += " ORDER BY 1 DESC";
            return sql;
        }
    }
}
