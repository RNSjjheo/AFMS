using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RealtimeVelocityChartQuery : RealtimeChartQuery
    {
        public const string MpdsSource = FbtHYDROMETERMPDS.TABLE_NAME;
        public const string VideoSource = FbtHYDROMETERVIDEO.TABLE_NAME;

        private readonly string? sourceType;
        private readonly int? deviceNo;
        private readonly int? transectNo;

        public RealtimeVelocityChartQuery(DateTime rangeStart, DateTime rangeEnd,
            string? sourceType = null, int? deviceNo = null, int? transectNo = null) : base(rangeStart, rangeEnd)
        {
            this.sourceType = sourceType;
            this.deviceNo = deviceNo;
            this.transectNo = transectNo;
        }

        public string BuildDeviceList()
        {
            string sql = $"SELECT {_FBTableBase.COL_ID} AS DEVICE_ID, TRIM({FbtAFMSHydroMeter.COL_DATA_TABLE}) AS SOURCE_TYPE,";
            sql += $" {FbtAFMSHydroMeter.COL_DEVICE_NO} AS DEVICE_NO, {FbtAFMSHydroMeter.COL_TRANSECT_CNT} AS TRANSECT_COUNT";
            sql += $" FROM {FbtAFMSHydroMeter.TABLE_NAME}";
            sql += $" WHERE TRIM({FbtAFMSHydroMeter.COL_DATA_TABLE}) IN ('{MpdsSource}', '{VideoSource}')";
            sql += $" ORDER BY {FbtAFMSHydroMeter.COL_DEVICE_NO}, {_FBTableBase.COL_ID}";
            return sql;
        }

        public override string Build()
        {
            if (!deviceNo.HasValue || !transectNo.HasValue || string.IsNullOrEmpty(sourceType)) return BuildEmptySeries();
            return sourceType == VideoSource ? BuildVideoQuery(transectNo.Value) : BuildMpdsQuery(transectNo.Value);
        }

        private string BuildMpdsQuery(int selectedTransectNo)
        {
            string sql = $"SELECT {SlotTimeValue()} AS SOURCE_TIME,";
            sql += $" '{selectedTransectNo}번 측선' AS SERIES, V.CHART_VALUE";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += $" LEFT JOIN (SELECT M.{_FBTableBase.COL_MEASURE_DATE} AS M_DATE, M.{_FBTableBase.COL_MEASURE_TIME} AS M_TIME,";
            sql += $" AVG(C.{FbtHYDROMETERMPDSCELL.COL_VELOCITY}) AS CHART_VALUE";
            sql += $" FROM {FbtHYDROMETERMPDS.TABLE_NAME} M JOIN {FbtHYDROMETERMPDSCELL.TABLE_NAME} C";
            sql += $" ON C.{FbtHYDROMETERMPDSCELL.COL_MPDS_ID} = M.{_FBTableBase.COL_ID}";
            sql += $" WHERE C.{FbtHYDROMETERMPDSCELL.COL_DEV_NO} = {selectedTransectNo} AND {MeasurementTimeCondition("M")}";
            sql += $" GROUP BY M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}) V";
            sql += $" ON V.M_DATE = S.{_FBTableBase.COL_MEASURE_DATE} AND V.M_TIME = S.{_FBTableBase.COL_MEASURE_TIME}";
            sql += $" WHERE {SlotTimeCondition()} ORDER BY S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} DESC";
            return sql;
        }

        private string BuildVideoQuery(int selectedTransectNo)
        {
            string sql = $"SELECT {SlotTimeValue()} AS SOURCE_TIME,";
            sql += $" '{selectedTransectNo}번 측선' AS SERIES, V.CHART_VALUE";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += $" LEFT JOIN (SELECT M.{_FBTableBase.COL_MEASURE_DATE} AS M_DATE, M.{_FBTableBase.COL_MEASURE_TIME} AS M_TIME,";
            sql += $" AVG(C.{FbtHYDROMETERVIDEOCELL.COL_VELOCITY}) AS CHART_VALUE";
            sql += $" FROM {FbtHYDROMETERVIDEO.TABLE_NAME} M JOIN {FbtHYDROMETERVIDEOCELL.TABLE_NAME} C";
            sql += $" ON C.{FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID} = M.{_FBTableBase.COL_ID}";
            sql += $" WHERE C.{FbtHYDROMETERVIDEOCELL.COL_CELL_NO} = {selectedTransectNo} AND {MeasurementTimeCondition("M")}";
            sql += $" GROUP BY M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}) V";
            sql += $" ON V.M_DATE = S.{_FBTableBase.COL_MEASURE_DATE} AND V.M_TIME = S.{_FBTableBase.COL_MEASURE_TIME}";
            sql += $" WHERE {SlotTimeCondition()} ORDER BY S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} DESC";
            return sql;
        }

        private string BuildEmptySeries() =>
            $"SELECT {SlotTimeValue()} AS SOURCE_TIME, '유속계' AS SERIES, CAST(NULL AS DOUBLE PRECISION) AS CHART_VALUE " +
            $"FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S WHERE {SlotTimeCondition()} " +
            $"ORDER BY S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} DESC";
    }
}
