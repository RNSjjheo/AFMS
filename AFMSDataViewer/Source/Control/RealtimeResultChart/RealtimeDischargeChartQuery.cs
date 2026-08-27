using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RealtimeDischargeChartQuery(DateTime rangeStart, DateTime rangeEnd)
        : RealtimeChartQuery(rangeStart, rangeEnd)
    {
        public override string Build()
        {
            string type = FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE;
            string deviceId = FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID;
            string method = FbtAFMSDischargeResult.COL_DISCHARGE_METHOD;
            string sourceTime = FbtAFMSDischargeResult.COL_SOURCE_TIME;
            string sql = $"SELECT {SlotTimeValue()} AS SOURCE_TIME,";
            sql += $" TRIM(D.DISCHARGE_METHOD) || ' ' || TRIM(D.DEVICE_TYPE) || ' ' || CAST(D.DEVICE_ID AS VARCHAR(12)) AS SERIES,";
            sql += " R.CHART_VALUE, D.DEVICE_TYPE, D.DEVICE_ID, D.DISCHARGE_METHOD";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += $" CROSS JOIN (SELECT DISTINCT TRIM({type}) AS DEVICE_TYPE, {deviceId} AS DEVICE_ID, TRIM({method}) AS DISCHARGE_METHOD";
            sql += $" FROM {FbtAFMSDischargeResult.TABLE_NAME}) D";
            sql += $" LEFT JOIN (SELECT {sourceTime} AS SLOT_TIME, TRIM({type}) AS DEVICE_TYPE, {deviceId} AS DEVICE_ID,";
            sql += $" TRIM({method}) AS DISCHARGE_METHOD, AVG({FbtAFMSDischargeResult.COL_DISCHARGE}) AS CHART_VALUE";
            sql += $" FROM {FbtAFMSDischargeResult.TABLE_NAME} WHERE {sourceTime} >= '{RangeStart:yyyy-MM-dd HH:mm:ss}'";
            sql += $" AND {sourceTime} <= '{RangeEnd:yyyy-MM-dd HH:mm:ss}'";
            sql += $" GROUP BY {sourceTime}, {type}, {deviceId}, {method}) R";
            sql += " ON R.SLOT_TIME = S.SLOT_TIME AND R.DEVICE_TYPE = D.DEVICE_TYPE AND R.DEVICE_ID = D.DEVICE_ID";
            sql += " AND R.DISCHARGE_METHOD = D.DISCHARGE_METHOD";
            sql += $" WHERE {SlotTimeCondition()} ORDER BY S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} DESC";
            return sql;
        }
    }
}
