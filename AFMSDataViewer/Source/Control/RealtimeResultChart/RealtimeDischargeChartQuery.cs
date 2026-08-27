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
            string configId = FbtAFMSDischargeMethodConfig.COL_ID;
            string configType = FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE;
            string configDeviceId = FbtAFMSDischargeMethodConfig.COL_DEVICE_ID;
            string configMethod = FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD;
            string sql = $"SELECT {SlotTimeValue()} AS SOURCE_TIME,";
            sql += $" TRIM(D.DISCHARGE_METHOD) || ' ' || TRIM(D.DEVICE_TYPE) || ' ' || CAST(D.DEVICE_ID AS VARCHAR(12)) AS SERIES,";
            sql += " R.CHART_VALUE, D.DEVICE_TYPE, D.DEVICE_ID, D.DISCHARGE_METHOD";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += $" CROSS JOIN (SELECT TRIM(C.{configType}) AS DEVICE_TYPE,";
            sql += $" C.{configDeviceId} AS DEVICE_ID, TRIM(C.{configMethod}) AS DISCHARGE_METHOD";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C";
            sql += $" WHERE C.{configId} = (SELECT MAX(C2.{configId})";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C2";
            sql += $" WHERE C2.{configType} = C.{configType}";
            sql += $" AND C2.{configDeviceId} = C.{configDeviceId}";
            sql += $" AND C2.{configMethod} = C.{configMethod})";
            sql += $" AND C.{FbtAFMSDischargeMethodConfig.COL_ENABLED} = 1) D";
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
