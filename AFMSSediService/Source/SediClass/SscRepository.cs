using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace AFMSSediService
{ 
    internal sealed class SscRepository
    {
        private const string ChannelMaster = "CHANNELMASTER";

        public IReadOnlyList<SscMeasurementKey> LoadPendingKeys(
            DateTime startTime,
            int batchSize,
            RSandProfileSnapshot profile)
        {
            int count = Math.Clamp(batchSize, 1, 1000);
            string start = startTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            List<string> readyConditions = [];
            if (profile.A.IsEnabled) readyConditions.Add("COALESCE(HYDROMETER1FLAG, 'N') = 'Y'");
            if (profile.B.IsEnabled) readyConditions.Add("COALESCE(HYDROMETER2FLAG, 'N') = 'Y'");
            if (readyConditions.Count == 0) return [];

            List<string> missingCalculations = [];
            if (profile.A.IsEnabled)
                missingCalculations.Add("COALESCE(TRIM(S.ADEVICETYPE), '') = ''");
            if (profile.B.IsEnabled)
                missingCalculations.Add("COALESCE(TRIM(S.BDEVICETYPE), '') = ''");

            string sql = $"SELECT FIRST {count} P.MEASUREDATE, P.MEASURETIME";
            sql += " FROM RPOINT P";
            sql += " LEFT JOIN RSAND S ON S.MEASUREDATE = P.MEASUREDATE";
            sql += " AND S.MEASURETIME = P.MEASURETIME";
            sql += $" WHERE (P.MEASUREDATE || P.MEASURETIME) >= '{start}'";
            sql += " AND (COALESCE(P.FILEFLAG, 'N') <> 'Y'";
            sql += " OR " + string.Join(" OR ", missingCalculations) + ")";
            sql += " AND " + string.Join(" AND ", readyConditions);
            sql += " ORDER BY P.MEASUREDATE, P.MEASURETIME";

            using DataTable table = Query(sql);
            return table.Rows.Cast<DataRow>()
                .Select(row => new SscMeasurementKey(
                    GetString(row, "MEASUREDATE"),
                    GetString(row, "MEASURETIME")))
                .ToList();
        }

        public void MarkInProgress(SscMeasurementKey key) => UpdateFileFlag(key, "S");
        public void MarkPending(SscMeasurementKey key) => UpdateFileFlag(key, "N");
        public void MarkCompleted(SscMeasurementKey key) => UpdateFileFlag(key, "Y");

        public bool HasCalculation(SscMeasurementKey key, int deviceNumber)
        {
            string prefix = GetDevicePrefix(deviceNumber);
            string sql = $"SELECT {prefix}DEVICETYPE FROM RSAND WHERE {KeyWhere(key)}";
            using DataTable table = Query(sql);
            return table.Rows.Count > 0 &&
                   !string.IsNullOrWhiteSpace(GetString(table.Rows[0], $"{prefix}DEVICETYPE"));
        }

        public ChannelMasterMeasurement LoadChannelMaster(
            SscMeasurementKey key,
            int deviceNumber)
        {
            string tableName = deviceNumber == 1 ? "RHYDROMETER1" : "RHYDROMETER2";
            string cellTableName = deviceNumber == 1 ? "RHYDROMETER1CELL" : "RHYDROMETER2CELL";

            string headerSql = "SELECT FIRST 1 VALUE02, VALUE03, VALUE04, VALUE05, VALUE11,";
            headerSql += " VALUE16, VALUE17, VALUE19, VALUE26";
            headerSql += $" FROM {tableName} WHERE {KeyWhere(key)}";
            headerSql += $" AND UPPER(TRIM(HYDROKIND)) = '{ChannelMaster}'";

            using DataTable header = Query(headerSql);
            if (header.Rows.Count == 0)
                throw new InvalidOperationException(
                    $"{key.Timestamp}의 ChannelMaster {deviceNumber}번 헤더가 없습니다.");

            string cellSql = "SELECT CELLNO, VALUE01, VALUE02, VALUE03, VALUE04";
            cellSql += $" FROM {cellTableName} WHERE {KeyWhere(key)} ORDER BY CELLNO";
            using DataTable cellsTable = Query(cellSql);
            if (cellsTable.Rows.Count == 0)
                throw new InvalidOperationException(
                    $"{key.Timestamp}의 ChannelMaster {deviceNumber}번 셀 자료가 없습니다.");

            DataRow row = header.Rows[0];
            List<ChannelMasterCell> cells = cellsTable.Rows.Cast<DataRow>()
                .Select(cell => new ChannelMasterCell(
                    GetInt32(cell, "CELLNO"),
                    GetInt32(cell, "VALUE01"),
                    GetInt32(cell, "VALUE02"),
                    GetInt32(cell, "VALUE03"),
                    GetInt32(cell, "VALUE04")))
                .ToList();

            return new ChannelMasterMeasurement(
                deviceNumber,
                key,
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

        public double LoadDischarge(SscMeasurementKey key)
        {
            string end = key.Timestamp;
            DateTime measuredAt = ParseTimestamp(key);
            string start = measuredAt.AddHours(-1)
                .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string sql = "SELECT FIRST 1 AVGSTREAM FROM RSTREAM";
            sql += $" WHERE (MEASUREDATE || MEASURETIME) BETWEEN '{start}' AND '{end}'";
            sql += " ORDER BY MEASUREDATE DESC, MEASURETIME DESC";

            using DataTable table = Query(sql);
            return table.Rows.Count == 0 ? 0.0 : GetDouble(table.Rows[0], "AVGSTREAM");
        }

        public void SaveCalculation(
            SscMeasurementKey key,
            int deviceNumber,
            SscCalculationResult result)
        {
            string prefix = GetDevicePrefix(deviceNumber);
            string columns = "MEASUREDATE, MEASURETIME,";
            columns += $" {prefix}DEVICETYPE, {prefix}AVG_SCB, {prefix}A, {prefix}B,";
            columns += $" {prefix}SA, {prefix}SB, {prefix}SSC,";
            columns += $" {prefix}DISCHARGE1, {prefix}DISCHARGE2,";
            columns += $" {prefix}TOTALSAND1, {prefix}TOTALSAND2";

            string values = $"'{key.MeasureDate}', '{key.MeasureTime}',";
            values += $" '{Escape(result.DeviceType)}', {Number(result.AverageScb)},";
            values += $" {Number(result.RegressionSlope)}, {Number(result.RegressionIntercept)},";
            values += $" {Number(result.SscSlope)}, {Number(result.SscIntercept)}, {Number(result.Ssc)},";
            values += $" {Number(result.Discharge1)}, {Number(result.Discharge2)},";
            values += $" {Number(result.TotalSand1)}, {Number(result.TotalSand2)}";

            Execute($"UPDATE OR INSERT INTO RSAND ({columns}) VALUES ({values}) " +
                    "MATCHING (MEASUREDATE, MEASURETIME)");

            foreach (SscCellCalculation cell in result.Cells)
            {
                string detailColumns = "MEASUREDATE, MEASURETIME, CELLNO,";
                detailColumns += $" {prefix}MB, {prefix}R, {prefix}U, {prefix}AW,";
                detailColumns += $" {prefix}AS, {prefix}WCB, {prefix}SCB";
                string detailValues = $"'{key.MeasureDate}', '{key.MeasureTime}', {cell.CellNumber},";
                detailValues += $" {Number(cell.Mb)}, {Number(cell.Range)},";
                detailValues += $" {Number(cell.SpreadingCoefficient)}, {Number(cell.WaterAbsorption)},";
                detailValues += $" {Number(cell.SedimentAttenuation)},";
                detailValues += $" {Number(cell.WaterCorrectedBackscatter)},";
                detailValues += $" {Number(cell.SedimentCorrectedBackscatter)}";

                Execute($"UPDATE OR INSERT INTO RSANDDETAIL ({detailColumns}) " +
                        $"VALUES ({detailValues}) MATCHING (MEASUREDATE, MEASURETIME, CELLNO)");
            }
        }

        public SediSedimentRecord LoadSedimentRecord(
            SscMeasurementKey key,
            RSandProfileSnapshot profile)
        {
            SediSedimentRecord record = new SediSedimentRecord
            {
                StationCode = LoadStationCode(),
                MeasurementTime = key.MeasureDate + key.MeasureTime[..4]
            };

            LoadVth(key, record, out bool hasVth);
            LoadWaterLevel(key, record, out bool hasWaterLevel);

            bool hasA = AddAdvm(record, key, 1, profile.A);
            bool hasB = AddAdvm(record, key, 2, profile.B);
            record.OverallDecision =
                (hasVth ? "0" : "2") +
                (hasWaterLevel ? "0" : "2") +
                (hasA ? "0" : "2") +
                (hasB ? "0" : "2");

            return record;
        }

        private bool AddAdvm(
            SediSedimentRecord record,
            SscMeasurementKey key,
            int deviceNumber,
            RSandDeviceProfile profile)
        {
            if (!profile.IsEnabled || !HasCalculation(key, deviceNumber)) return false;

            ChannelMasterMeasurement source = LoadChannelMaster(key, deviceNumber);
            string prefix = GetDevicePrefix(deviceNumber);
            string sql = $"SELECT {prefix}SSC, {prefix}TOTALSAND1, {prefix}TOTALSAND2";
            sql += $" FROM RSAND WHERE {KeyWhere(key)}";
            using DataTable result = Query(sql);
            if (result.Rows.Count == 0) return false;

            DataRow resultRow = result.Rows[0];
            SediAdvmRecord advm = new SediAdvmRecord
            {
                Number = deviceNumber,
                Type = DeviceTypeNumber(profile.DeviceType),
                Ssc = GetDouble(resultRow, $"{prefix}SSC"),
                Sediment = GetDouble(resultRow, $"{prefix}TOTALSAND1"),
                TotalSediment = GetDouble(resultRow, $"{prefix}TOTALSAND2"),
                StartCell = profile.CellFrom,
                EndCell = profile.CellTo,
                Decision = "00000",
                WaterTemperature = source.Temperature,
                Depth = source.Depth,
                Pitch = source.Pitch,
                Roll = source.Roll,
                CellCount = source.CellCount,
                CellSize = source.CellSizeCm,
                PingCount = source.PingCount,
                Frequency = source.Frequency,
                FirstCellDistance = source.FirstCellDistanceCm,
                LastCellDistance = source.CellCount * source.CellSizeCm + source.FirstCellDistanceCm
            };

            foreach (ChannelMasterCell cell in source.Cells)
            {
                advm.Cells.Add(new SediCellRecord
                {
                    Number = cell.Number,
                    VelocityEastWest = cell.VelocityEastWest,
                    VelocityNorthSouth = cell.VelocityNorthSouth,
                    Echo1 = cell.Echo1,
                    Echo2 = cell.Echo2
                });
            }

            record.Advms.Add(advm);
            return true;
        }

        private static int DeviceTypeNumber(string deviceType) =>
            deviceType.ToUpperInvariant() switch
            {
                "CM300" => 1,
                "CM600" => 2,
                "CM1200" => 3,
                _ => 0
            };

        private string LoadStationCode()
        {
            using DataTable table = Query(
                "SELECT FIRST 1 VALUE01 FROM RSETUP WHERE PK1 = 1 AND PK2 = 1");
            string stationCode = table.Rows.Count == 0
                ? string.Empty
                : GetString(table.Rows[0], "VALUE01");
            return string.IsNullOrWhiteSpace(stationCode) ? "UNKNOWN" : stationCode;
        }

        private void LoadVth(
            SscMeasurementKey key,
            SediSedimentRecord record,
            out bool found)
        {
            using DataTable table = QueryLatestWithinHour(
                "RVTHLOGGER",
                "VOLT, DCCHARGE, DCBATTERY, TEMPERATURE, HUMIDITY",
                key);
            found = table.Rows.Count > 0;
            record.VthDecision = found ? "00000" : "22222";
            if (!found) return;

            DataRow row = table.Rows[0];
            record.Ac = GetInt32(row, "VOLT");
            record.DcCharge = GetDouble(row, "DCCHARGE");
            record.DcBattery = GetDouble(row, "DCBATTERY");
            record.SystemTemperature = GetDouble(row, "TEMPERATURE");
            record.SystemHumidity = GetDouble(row, "HUMIDITY");
        }

        private void LoadWaterLevel(
            SscMeasurementKey key,
            SediSedimentRecord record,
            out bool found)
        {
            using DataTable table = QueryLatestWithinHour(
                "RWATERLEVEL",
                "VALUE03 AS DEPTH, VALUE07 AS SALINITY, VALUE10 AS WLOFFSET",
                key);
            found = table.Rows.Count > 0;
            record.WaterLevelDecision = found ? "00" : "22";
            if (!found) return;

            DataRow row = table.Rows[0];
            record.WaterDepth = GetDouble(row, "DEPTH");
            record.WaterLevelOffset = GetDouble(row, "WLOFFSET");
            record.WaterLevel = record.WaterDepth + record.WaterLevelOffset;
            record.Salinity = GetDouble(row, "SALINITY");
        }

        private DataTable QueryLatestWithinHour(
            string tableName,
            string columns,
            SscMeasurementKey key)
        {
            string end = key.Timestamp;
            string start = ParseTimestamp(key).AddHours(-1)
                .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string sql = $"SELECT FIRST 1 {columns} FROM {tableName}";
            sql += $" WHERE (MEASUREDATE || MEASURETIME) BETWEEN '{start}' AND '{end}'";
            sql += " ORDER BY MEASUREDATE DESC, MEASURETIME DESC";
            return Query(sql);
        }

        private void UpdateFileFlag(SscMeasurementKey key, string flag)
        {
            Execute($"UPDATE RPOINT SET FILEFLAG = '{Escape(flag)}' WHERE {KeyWhere(key)}");
        }

        private static string GetDevicePrefix(int deviceNumber) =>
            deviceNumber switch
            {
                1 => "A",
                2 => "B",
                _ => throw new ArgumentOutOfRangeException(nameof(deviceNumber))
            };

        private static string KeyWhere(SscMeasurementKey key) =>
            $"MEASUREDATE = '{Escape(key.MeasureDate)}' AND MEASURETIME = '{Escape(key.MeasureTime)}'";

        private static DateTime ParseTimestamp(SscMeasurementKey key) =>
            DateTime.ParseExact(
                key.Timestamp,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

        private static string Number(double value)
        {
            if (!double.IsFinite(value))
                throw new InvalidOperationException("DB에 저장할 계산값이 유효하지 않습니다.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value) => value.Replace("'", "''");

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

        private static string GetString(DataRow row, string columnName) =>
            Convert.ToString(row[columnName])?.Trim() ?? string.Empty;

        private static int GetInt32(DataRow row, string columnName) =>
            row[columnName] == DBNull.Value ? 0 : Convert.ToInt32(row[columnName]);

        private static double GetDouble(DataRow row, string columnName) =>
            row[columnName] == DBNull.Value ? 0.0 : Convert.ToDouble(row[columnName]);
    }
}
