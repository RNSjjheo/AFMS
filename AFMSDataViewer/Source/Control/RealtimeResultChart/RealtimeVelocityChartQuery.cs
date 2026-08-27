using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RealtimeVelocityChartQuery(DateTime rangeStart, DateTime rangeEnd)
        : RealtimeChartQuery(rangeStart, rangeEnd)
    {
        public override string Build()
        {
            string sql = $"SELECT S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} AS SOURCE_TIME,";
            sql += " 'MPDS ' || CAST(D.DEV_NO AS VARCHAR(12)) AS SERIES, V.CHART_VALUE";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += $" CROSS JOIN (SELECT DISTINCT C.{FbtHYDROMETERMPDSCELL.COL_DEV_NO} AS DEV_NO FROM {FbtHYDROMETERMPDS.TABLE_NAME} M";
            sql += $" JOIN {FbtHYDROMETERMPDSCELL.TABLE_NAME} C ON C.{FbtHYDROMETERMPDSCELL.COL_MPDS_ID} = M.{_FBTableBase.COL_ID}";
            sql += $" WHERE {MeasurementTimeCondition("M")}) D";
            sql += $" LEFT JOIN (SELECT M.{_FBTableBase.COL_MEASURE_DATE} AS M_DATE, M.{_FBTableBase.COL_MEASURE_TIME} AS M_TIME,";
            sql += $" C.{FbtHYDROMETERMPDSCELL.COL_DEV_NO} AS DEV_NO, AVG(C.{FbtHYDROMETERMPDSCELL.COL_VELOCITY}) AS CHART_VALUE";
            sql += $" FROM {FbtHYDROMETERMPDS.TABLE_NAME} M JOIN {FbtHYDROMETERMPDSCELL.TABLE_NAME} C";
            sql += $" ON C.{FbtHYDROMETERMPDSCELL.COL_MPDS_ID} = M.{_FBTableBase.COL_ID}";
            sql += $" WHERE {MeasurementTimeCondition("M")}";
            sql += $" GROUP BY M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}, C.{FbtHYDROMETERMPDSCELL.COL_DEV_NO}) V";
            sql += $" ON V.M_DATE = S.{_FBTableBase.COL_MEASURE_DATE} AND V.M_TIME = S.{_FBTableBase.COL_MEASURE_TIME} AND V.DEV_NO = D.DEV_NO";
            sql += $" WHERE {SlotTimeCondition()}";
            sql += " UNION ALL ";
            sql += $"SELECT S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} AS SOURCE_TIME,";
            sql += " '영상 ' || CAST(D.CELL_NO AS VARCHAR(12)) AS SERIES, V.CHART_VALUE";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += $" CROSS JOIN (SELECT DISTINCT C.{FbtHYDROMETERVIDEOCELL.COL_CELL_NO} AS CELL_NO FROM {FbtHYDROMETERVIDEO.TABLE_NAME} M";
            sql += $" JOIN {FbtHYDROMETERVIDEOCELL.TABLE_NAME} C ON C.{FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID} = M.{_FBTableBase.COL_ID}";
            sql += $" WHERE {MeasurementTimeCondition("M")}) D";
            sql += $" LEFT JOIN (SELECT M.{_FBTableBase.COL_MEASURE_DATE} AS M_DATE, M.{_FBTableBase.COL_MEASURE_TIME} AS M_TIME,";
            sql += $" C.{FbtHYDROMETERVIDEOCELL.COL_CELL_NO} AS CELL_NO, AVG(C.{FbtHYDROMETERVIDEOCELL.COL_VELOCITY}) AS CHART_VALUE";
            sql += $" FROM {FbtHYDROMETERVIDEO.TABLE_NAME} M JOIN {FbtHYDROMETERVIDEOCELL.TABLE_NAME} C";
            sql += $" ON C.{FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID} = M.{_FBTableBase.COL_ID}";
            sql += $" WHERE {MeasurementTimeCondition("M")}";
            sql += $" GROUP BY M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}, C.{FbtHYDROMETERVIDEOCELL.COL_CELL_NO}) V";
            sql += $" ON V.M_DATE = S.{_FBTableBase.COL_MEASURE_DATE} AND V.M_TIME = S.{_FBTableBase.COL_MEASURE_TIME} AND V.CELL_NO = D.CELL_NO";
            sql += $" WHERE {SlotTimeCondition()}";
            sql += " ORDER BY 1 DESC";
            return sql;
        }
    }
}
