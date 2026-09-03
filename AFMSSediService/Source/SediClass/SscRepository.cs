using AFMSDll;
using System.Data;
using System.Globalization;

namespace AFMSSediService
{
    internal sealed class SscRepository
    {
        public ChannelMasterMeasurement LoadChannelMaster(SscCalculationSlot slot, ChannelMasterSource source)
        {
            string where = KeyWhere(slot);
            string headerSql = "SELECT FIRST 1 VALUE02, VALUE03, VALUE04, VALUE05,";
            headerSql += " VALUE11, VALUE16, VALUE17, VALUE19, VALUE26";
            headerSql += $" FROM {source.HeaderTable} WHERE {where}";
            headerSql += " AND UPPER(TRIM(HYDROKIND)) = 'CHANNELMASTER'";

            using DataTable header = Query(headerSql);
            if (header.Rows.Count == 0)
                throw new InvalidOperationException(
                    $"{slot.MeasureDate}{slot.MeasureTime}의 {source.HeaderTable} ChannelMaster 데이터가 없습니다.");

            string cellSql = "SELECT CELLNO, VALUE01, VALUE02, VALUE03, VALUE04";
            cellSql += $" FROM {source.CellTable} WHERE {where} ORDER BY CELLNO";
            using DataTable cellRows = Query(cellSql);
            if (cellRows.Rows.Count == 0)
                throw new InvalidOperationException(
                    $"{slot.MeasureDate}{slot.MeasureTime}의 {source.CellTable} 셀 데이터가 없습니다.");

            DataRow row = header.Rows[0];
            List<ChannelMasterCell> cells = cellRows.Rows.Cast<DataRow>()
                .Select(cell => new ChannelMasterCell(
                    GetInt32(cell, "CELLNO"),
                    GetInt32(cell, "VALUE01"),
                    GetInt32(cell, "VALUE02"),
                    GetInt32(cell, "VALUE03"),
                    GetInt32(cell, "VALUE04")))
                .ToList();

            return new ChannelMasterMeasurement(
                new SscMeasurementKey(slot.MeasureDate, slot.MeasureTime),
                GetDouble(row, "VALUE19") * 0.01,
                GetDouble(row, "VALUE26") * 0.0001,
                GetDouble(row, "VALUE16") * 0.01,
                GetDouble(row, "VALUE17") * 0.01,
                GetInt32(row, "VALUE02"),
                GetInt32(row, "VALUE04"),
                GetInt32(row, "VALUE03"),
                GetInt32(row, "VALUE05"),
                GetInt32(row, "VALUE11"),
                cells);
        }

        public double LoadDischarge(SscCalculationSlot slot)
        {
            string end = slot.MeasureDate + slot.MeasureTime;
            string start = slot.SlotTime.AddHours(-1)
                .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string sql = "SELECT FIRST 1 AVGSTREAM FROM RSTREAM";
            sql += $" WHERE (MEASUREDATE || MEASURETIME) BETWEEN '{start}' AND '{end}'";
            sql += " ORDER BY MEASUREDATE DESC, MEASURETIME DESC";

            using DataTable table = Query(sql);
            return table.Rows.Count == 0 ? 0.0 : GetDouble(table.Rows[0], "AVGSTREAM");
        }

        public void Save(SscCalculationSlot slot, SscCalculationResult result)
        {
            int sediId = FBProvider.Instance.GetNextID(FbtAFMSSediResult.TABLE_NAME);
            string sediSql = $"UPDATE OR INSERT INTO {FbtAFMSSediResult.TABLE_NAME} (";
            sediSql += $"{FbtAFMSSediResult.COL_ID}, {FbtAFMSSediResult.COL_SLOT_ID}, ";
            sediSql += $"{FbtAFMSSediResult.COL_DISCHARGE1}, {FbtAFMSSediResult.COL_DISCHARGE2}, ";
            sediSql += $"{FbtAFMSSediResult.COL_TOTAL_SAND1}, {FbtAFMSSediResult.COL_TOTAL_SAND2}, ";
            sediSql += $"{FbtAFMSSediResult.COL_CALCULATED_AT}) VALUES (";
            sediSql += $"{sediId}, {slot.Id}, {Number(result.Discharge1)}, {Number(result.Discharge2)}, ";
            sediSql += $"{Number(result.TotalSand1)}, {Number(result.TotalSand2)}, CURRENT_TIMESTAMP) ";
            sediSql += $"MATCHING ({FbtAFMSSediResult.COL_SLOT_ID})";
            Execute(sediSql);

            int sscId = FBProvider.Instance.GetNextID(FbtAFMSSediSSCResult.TABLE_NAME);
            string sscSql = $"UPDATE OR INSERT INTO {FbtAFMSSediSSCResult.TABLE_NAME} (";
            sscSql += $"{FbtAFMSSediSSCResult.COL_ID}, {FbtAFMSSediSSCResult.COL_SLOT_ID}, ";
            sscSql += $"{FbtAFMSSediSSCResult.COL_DEVICE_TYPE}, {FbtAFMSSediSSCResult.COL_AVG_VELOCITY}, {FbtAFMSSediSSCResult.COL_AVG_SCB}, ";
            sscSql += $"{FbtAFMSSediSSCResult.COL_REGRESSION_SLOPE}, {FbtAFMSSediSSCResult.COL_REGRESSION_INTERCEPT}, ";
            sscSql += $"{FbtAFMSSediSSCResult.COL_SSC_SLOPE}, {FbtAFMSSediSSCResult.COL_SSC_INTERCEPT}, ";
            sscSql += $"{FbtAFMSSediSSCResult.COL_SSC}, {FbtAFMSSediSSCResult.COL_CALCULATED_AT}) VALUES (";
            sscSql += $"{sscId}, {slot.Id}, '{Escape(result.DeviceType)}', {Number(result.AverageVelocity)}, {Number(result.AverageScb)}, ";
            sscSql += $"{Number(result.RegressionSlope)}, {Number(result.RegressionIntercept)}, ";
            sscSql += $"{Number(result.SscSlope)}, {Number(result.SscIntercept)}, ";
            sscSql += $"{Number(result.Ssc)}, CURRENT_TIMESTAMP) ";
            sscSql += $"MATCHING ({FbtAFMSSediSSCResult.COL_SLOT_ID})";
            Execute(sscSql);
        }

        private static string KeyWhere(SscCalculationSlot slot) =>
            $"MEASUREDATE = '{Escape(slot.MeasureDate)}' AND MEASURETIME = '{Escape(slot.MeasureTime)}'";

        private static DataTable Query(string sql)
        {
            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            return db.Results.Copy();
        }

        private static void Execute(string sql)
        {
            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            string error = db.RunNonQuery(sql);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }

        private static int GetInt32(DataRow row, string column) =>
            row[column] == DBNull.Value ? 0 : Convert.ToInt32(row[column]);

        private static double GetDouble(DataRow row, string column) =>
            row[column] == DBNull.Value ? 0.0 : Convert.ToDouble(row[column]);

        private static string Number(double value)
        {
            if (!double.IsFinite(value))
                throw new InvalidOperationException("DB에 저장할 계산값이 유효하지 않습니다.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value) => value.Replace("'", "''");
    }
}
