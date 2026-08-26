using System;
using System.Data;
using System.Globalization;
using log4net;

namespace AFMSDll
{
    public abstract class _QBase
    {
        private readonly ILog log;

        public int Id { get; set; } = -1;
        public int SlotId { get; private set; } = -1;
        public MeasurementDeviceType DeviceType { get; set; } = MeasurementDeviceType.None;
        public int DeviceId { get; set; } = -1;
        public string DeviceName { get; private set; } = string.Empty;
        public string MeasurementTableName { get; private set; } = string.Empty;
        public _FBTableBase? MeasurementTable { get; private set; }
        public bool HasMeasurementStart { get; private set; }
        public int MeasurementStartId { get; private set; } = -1;
        public DateOnly MeasurementStartDate { get; private set; }
        public TimeOnly MeasurementStartTime { get; private set; }
        public DateTime? LastCalculatedSourceTime { get; private set; }
        public int DischargeConfigId { get; set; } = -1;
        public DateOnly MeasureDate { get; set; }
        public TimeOnly MeasureTime { get; set; }
        public double Value { get; set; }
        public double Uncertainty { get; set; }
        public DischargeMethod Method { get; }
        public int MethodConfigId { get; set; } = -1;
        public CrossSection CrossSection { get; } = new();

        protected _QBase(DischargeMethod method)
        {
            Method = method;
            log = LogManager.GetLogger(GetType());
        }

        /// <summary>
        /// 현재 또는 이후 측정자료 중 산정 가능한 시작값과 슬롯을 준비합니다.
        /// 실패 로그는 산정 객체가 직접 기록합니다.
        /// </summary>
        public bool PrepareNextCalculation(FBDatabase db)
        {
            if (!TryCheckMeasurementStartReceived(db, out bool received, out string error))
            {
                LogFailure("입력자료 확인 실패", error);
                return false;
            }

            if (received) return SlotId >= 0;

            int previousId = MeasurementStartId;
            DateOnly previousDate = MeasurementStartDate;
            TimeOnly previousTime = MeasurementStartTime;

            if (!TryMoveToNextReceivedMeasurement(db, out bool moved, out error))
            {
                LogFailure("다음 수신 완료 자료 조회 실패", error);
                return false;
            }

            if (!moved) return false;

            bool hasSlot = TryLoadStartSlot(db, out error);
            if (!string.IsNullOrEmpty(error))
            {
                LogFailure("이동한 측정자료의 슬롯 조회 실패", error);
                return false;
            }

            log.Info(
                $"유량 산정 시작값 이동: {GetLogContext()}, " +
                $"이전 {previousId} ({previousDate:yyyy-MM-dd} {previousTime:HH:mm:ss}), " +
                $"변경 {MeasurementStartId} ({MeasurementStartDate:yyyy-MM-dd} {MeasurementStartTime:HH:mm:ss}), " +
                $"슬롯 {(hasSlot ? SlotId : -1)}");

            return hasSlot;
        }

        private void LogFailure(string operation, string error)
        {
            log.Error($"유량 산정 {operation}: {GetLogContext()}, 오류 {error}");
        }

        private string GetLogContext()
        {
            return $"{DeviceType} {DeviceId} ({DeviceName}), 산정법 {Method}";
        }

        /// <summary>
        /// DeviceId에 해당하는 측정장비 종류를 확인하고 원시 측정 테이블을 연결합니다.
        /// </summary>
        public bool TryConnectMeasurementTable(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            DeviceName = string.Empty;
            MeasurementTableName = string.Empty;
            MeasurementTable = null;

            if (DeviceType == MeasurementDeviceType.WaterLevelGauge)
            {
                DeviceName = "시스템 수위계";
                MeasurementTable = new FbtWATERLEVEL();
                MeasurementTableName = MeasurementTable.GetTableName();
                error = string.Empty;
                return true;
            }

            if (DeviceType != MeasurementDeviceType.VelocityMeter || DeviceId < 0)
            {
                error = "조회할 유속계가 설정되지 않았습니다.";
                return false;
            }

            QueryBuilderSelect query = new();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSHydroMeter.COL_DEVICE_NAME);
            query.Add(FbtAFMSHydroMeter.COL_DATA_TABLE);
            query.Where(FbtAFMSHydroMeter.COL_ID, "=", DeviceId);

            DataTable table = db.Execute(query, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"유속계 정보를 찾을 수 없습니다: DeviceId={DeviceId}";
                return false;
            }

            DeviceName = Convert.ToString(table.Rows[0][FbtAFMSHydroMeter.COL_DEVICE_NAME])?.Trim() ?? string.Empty;
            MeasurementTableName = Convert.ToString(table.Rows[0][FbtAFMSHydroMeter.COL_DATA_TABLE])?.Trim() ?? string.Empty;

            if (string.Equals(MeasurementTableName, FbtHYDROMETERMPDS.TABLE_NAME, StringComparison.OrdinalIgnoreCase))
                MeasurementTable = new FbtHYDROMETERMPDS();
            else if (string.Equals(MeasurementTableName, FbtHYDROMETERVIDEO.TABLE_NAME, StringComparison.OrdinalIgnoreCase))
                MeasurementTable = new FbtHYDROMETERVIDEO();

            if (MeasurementTable == null)
            {
                error = string.IsNullOrEmpty(MeasurementTableName)
                    ? $"유속계의 데이터 테이블이 설정되지 않았습니다: DeviceId={DeviceId}"
                    : $"지원하지 않는 유속계 데이터 테이블입니다: {MeasurementTableName}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// 현재 산정법과 장비로 마지막 산정한 원시시각 다음의 첫 측정자료를 불러옵니다.
        /// 산정 이력이 없으면 연결된 측정 테이블의 가장 이른 자료부터 시작합니다.
        /// </summary>
        public bool TryLoadMeasurementStart(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            MeasurementStartId = -1;
            MeasurementStartDate = default;
            MeasurementStartTime = default;
            HasMeasurementStart = false;
            LastCalculatedSourceTime = null;

            if (MeasurementTable == null || string.IsNullOrEmpty(MeasurementTableName))
            {
                error = "측정 테이블이 연결되지 않았습니다.";
                return false;
            }

            string resultSql = $"SELECT MAX({FbtAFMSDischargeResult.COL_SOURCE_TIME})";
            resultSql += $" FROM {FbtAFMSDischargeResult.TABLE_NAME}";
            resultSql += $" WHERE {FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE} = '{DeviceType}'";
            resultSql += $" AND {FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID} = {DeviceId}";
            resultSql += $" AND {FbtAFMSDischargeResult.COL_DISCHARGE_METHOD} = '{Method}'";

            DataTable resultTable = db.Execute(resultSql, out error);
            if (!string.IsNullOrEmpty(error)) return false;

            if (resultTable.Rows.Count > 0 && resultTable.Rows[0][0] != DBNull.Value)
                LastCalculatedSourceTime = Convert.ToDateTime(resultTable.Rows[0][0]);

            bool hasMeasurementId = DeviceType == MeasurementDeviceType.VelocityMeter;
            string sourceSql = "SELECT FIRST 1";
            if (hasMeasurementId) sourceSql += $" {FbtAFMSHydroMeter.COL_ID},";
            sourceSql += $" {FbtAFMSHydroMeter.COL_MEASURE_DATE},";
            sourceSql += $" {FbtAFMSHydroMeter.COL_MEASURE_TIME}";
            sourceSql += $" FROM {MeasurementTableName}";
            if (LastCalculatedSourceTime.HasValue)
            {
                string lastSourceDateTime = LastCalculatedSourceTime.Value.ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
                sourceSql += $" WHERE {_FBTableBase.SQL_MEASURE_DATETIME} > '{lastSourceDateTime}'";
            }
            sourceSql += $" ORDER BY {_FBTableBase.SQL_MEASURE_DATETIME}";
            if (hasMeasurementId) sourceSql += $", {FbtAFMSHydroMeter.COL_ID}";

            DataTable sourceTable = db.Execute(sourceSql, out error);
            if (!string.IsNullOrEmpty(error) || sourceTable.Rows.Count == 0) return false;

            DataRow sourceRow = sourceTable.Rows[0];
            string measureDate = Convert.ToString(sourceRow[FbtAFMSHydroMeter.COL_MEASURE_DATE]) ?? string.Empty;
            string measureTime = Convert.ToString(sourceRow[FbtAFMSHydroMeter.COL_MEASURE_TIME]) ?? string.Empty;
            if (!TryParseMeasureDateTime(measureDate, measureTime, out DateOnly parsedDate, out TimeOnly parsedTime))
            {
                error = $"유속 자료의 측정시각 형식이 올바르지 않습니다: {measureDate} {measureTime}";
                return false;
            }

            MeasurementStartId = hasMeasurementId
                ? Convert.ToInt32(sourceRow[FbtAFMSHydroMeter.COL_ID])
                : -1;
            MeasurementStartDate = parsedDate;
            MeasurementStartTime = parsedTime;
            HasMeasurementStart = true;
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// 현재 측정 시작값의 마스터 및 셀 자료가 모두 수신되었는지 확인합니다.
        /// </summary>
        public bool TryCheckMeasurementStartReceived(FBDatabase db, out bool received, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            received = false;
            error = string.Empty;

            if (MeasurementTable == null || string.IsNullOrEmpty(MeasurementTableName))
            {
                error = "측정 테이블이 연결되지 않았습니다.";
                return false;
            }

            if (!HasMeasurementStart)
                return true;

            if (MeasurementTable is FbtWATERLEVEL)
                return TryCheckWaterLevelReceived(db, out received, out error);

            if (MeasurementStartId < 0) return true;

            if (MeasurementTable is FbtHYDROMETERMPDS)
            {
                return TryCheckCellDataReceived(
                    db,
                    FbtHYDROMETERMPDS.COL_DEVICE_COUNT,
                    FbtHYDROMETERMPDS.COL_MEASURE_OK,
                    FbtHYDROMETERMPDSCELL.TABLE_NAME,
                    FbtHYDROMETERMPDSCELL.COL_MPDS_ID,
                    out received,
                    out error);
            }

            if (MeasurementTable is FbtHYDROMETERVIDEO)
            {
                return TryCheckCellDataReceived(
                    db,
                    FbtHYDROMETERVIDEO.COL_CELL_COUNT,
                    FbtHYDROMETERVIDEO.COL_MEASURE_OK,
                    FbtHYDROMETERVIDEOCELL.TABLE_NAME,
                    FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID,
                    out received,
                    out error);
            }

            error = $"수신 완료 여부를 확인할 수 없는 측정 테이블입니다: {MeasurementTableName}";
            return false;
        }

        /// <summary>
        /// 현재 시작값보다 늦은 자료 중 모든 마스터/셀 자료가 수신된 첫 자료로 시작값을 이동합니다.
        /// </summary>
        public bool TryMoveToNextReceivedMeasurement(FBDatabase db, out bool moved, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            moved = false;
            error = string.Empty;

            if (!HasMeasurementStart || MeasurementTable is FbtWATERLEVEL)
                return true;

            string expectedCountColumn;
            string measureOkColumn;
            string cellTableName;
            string parentIdColumn;

            if (MeasurementTable is FbtHYDROMETERMPDS)
            {
                expectedCountColumn = FbtHYDROMETERMPDS.COL_DEVICE_COUNT;
                measureOkColumn = FbtHYDROMETERMPDS.COL_MEASURE_OK;
                cellTableName = FbtHYDROMETERMPDSCELL.TABLE_NAME;
                parentIdColumn = FbtHYDROMETERMPDSCELL.COL_MPDS_ID;
            }
            else if (MeasurementTable is FbtHYDROMETERVIDEO)
            {
                expectedCountColumn = FbtHYDROMETERVIDEO.COL_CELL_COUNT;
                measureOkColumn = FbtHYDROMETERVIDEO.COL_MEASURE_OK;
                cellTableName = FbtHYDROMETERVIDEOCELL.TABLE_NAME;
                parentIdColumn = FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID;
            }
            else
            {
                error = $"다음 수신 완료 자료를 확인할 수 없는 측정 테이블입니다: {MeasurementTableName}";
                return false;
            }

            string currentDateTime = $"{MeasurementStartDate:yyyyMMdd} {MeasurementStartTime:HHmmss}";
            string sql = $"SELECT FIRST 1 M.{_FBTableBase.COL_ID},";
            sql += $" M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}";
            sql += $" FROM {MeasurementTableName} M";
            sql += $" WHERE (M.{_FBTableBase.COL_MEASURE_DATE} || ' ' || M.{_FBTableBase.COL_MEASURE_TIME}) > '{currentDateTime}'";
            sql += $" AND (M.{measureOkColumn} IS NULL OR M.{measureOkColumn} = 1)";
            sql += $" AND (SELECT COUNT(*) FROM {cellTableName} C";
            sql += $" WHERE C.{parentIdColumn} = M.{_FBTableBase.COL_ID}) >= COALESCE(M.{expectedCountColumn}, 0)";
            sql += $" ORDER BY M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}, M.{_FBTableBase.COL_ID}";

            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error) || table.Rows.Count == 0) return string.IsNullOrEmpty(error);

            DataRow row = table.Rows[0];
            string measureDate = Convert.ToString(row[_FBTableBase.COL_MEASURE_DATE]) ?? string.Empty;
            string measureTime = Convert.ToString(row[_FBTableBase.COL_MEASURE_TIME]) ?? string.Empty;
            if (!TryParseMeasureDateTime(measureDate, measureTime, out DateOnly parsedDate, out TimeOnly parsedTime))
            {
                error = $"다음 유속 자료의 측정시각 형식이 올바르지 않습니다: {measureDate} {measureTime}";
                return false;
            }

            MeasurementStartId = Convert.ToInt32(row[_FBTableBase.COL_ID]);
            MeasurementStartDate = parsedDate;
            MeasurementStartTime = parsedTime;
            moved = true;
            return true;
        }

        private bool TryCheckWaterLevelReceived(FBDatabase db, out bool received, out string error)
        {
            string date = MeasurementStartDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string time = MeasurementStartTime.ToString("HHmmss", CultureInfo.InvariantCulture);
            string sql = "SELECT COUNT(*)";
            sql += $" FROM {MeasurementTableName}";
            sql += $" WHERE {_FBTableBase.COL_MEASURE_DATE} = '{date}'";
            sql += $" AND {_FBTableBase.COL_MEASURE_TIME} = '{time}'";

            DataTable table = db.Execute(sql, out error);
            received = string.IsNullOrEmpty(error) &&
                       table.Rows.Count > 0 &&
                       Convert.ToInt32(table.Rows[0][0]) > 0;
            return string.IsNullOrEmpty(error);
        }

        private bool TryCheckCellDataReceived(
            FBDatabase db,
            string expectedCountColumn,
            string measureOkColumn,
            string cellTableName,
            string parentIdColumn,
            out bool received,
            out string error)
        {
            received = false;

            string masterSql = $"SELECT FIRST 1 {expectedCountColumn}, {measureOkColumn}";
            masterSql += $" FROM {MeasurementTableName}";
            masterSql += $" WHERE {_FBTableBase.COL_ID} = {MeasurementStartId}";

            DataTable masterTable = db.Execute(masterSql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (masterTable.Rows.Count == 0) return true;

            DataRow masterRow = masterTable.Rows[0];
            int expectedCount = masterRow[expectedCountColumn] == DBNull.Value
                ? 0
                : Convert.ToInt32(masterRow[expectedCountColumn]);
            if (masterRow[measureOkColumn] != DBNull.Value && Convert.ToInt32(masterRow[measureOkColumn]) != 1)
                return true;

            string cellSql = $"SELECT COUNT(*) FROM {cellTableName}";
            cellSql += $" WHERE {parentIdColumn} = {MeasurementStartId}";

            DataTable cellTable = db.Execute(cellSql, out error);
            if (!string.IsNullOrEmpty(error)) return false;

            int receivedCount = cellTable.Rows.Count == 0 ? 0 : Convert.ToInt32(cellTable.Rows[0][0]);
            received = receivedCount >= expectedCount;
            return true;
        }

        /// <summary>
        /// 현재 산정법과 측정장비 조합에서 아직 유량 결과가 없는 가장 이른 슬롯을 불러옵니다.
        /// </summary>
        /// <returns>산정할 슬롯이 있으면 true, 없으면 false입니다.</returns>
        public bool TryLoadStartSlot(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            error = ValidateCalculationKey();
            if (!string.IsNullOrEmpty(error))
            {
                ClearStartSlot();
                return false;
            }

            string sql = $"SELECT FIRST 1 S.{FbtAFMSDischargeTimeslot.COL_ID},";
            sql += $" S.{FbtAFMSDischargeTimeslot.COL_MEASURE_DATE},";
            sql += $" S.{FbtAFMSDischargeTimeslot.COL_MEASURE_TIME}";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += " WHERE NOT EXISTS (";
            sql += $"SELECT 1 FROM {FbtAFMSDischargeResult.TABLE_NAME} R";
            sql += $" WHERE R.{FbtAFMSDischargeResult.COL_SLOT_ID} = S.{FbtAFMSDischargeTimeslot.COL_ID}";
            sql += $" AND R.{FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE} = '{DeviceType}'";
            sql += $" AND R.{FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID} = {DeviceId}";
            sql += $" AND R.{FbtAFMSDischargeResult.COL_DISCHARGE_METHOD} = '{Method}')";
            if (HasMeasurementStart)
            {
                string measurementStart = $"{MeasurementStartDate:yyyyMMdd} {MeasurementStartTime:HHmmss}";
                sql += $" AND (S.{FbtAFMSDischargeTimeslot.COL_MEASURE_DATE} || ' ' || S.{FbtAFMSDischargeTimeslot.COL_MEASURE_TIME}) >= '{measurementStart}'";
            }
            sql += $" ORDER BY S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME}";

            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error) || table.Rows.Count == 0)
            {
                ClearStartSlot();
                return false;
            }

            DataRow row = table.Rows[0];
            string measureDate = Convert.ToString(row[FbtAFMSDischargeTimeslot.COL_MEASURE_DATE]) ?? string.Empty;
            string measureTime = Convert.ToString(row[FbtAFMSDischargeTimeslot.COL_MEASURE_TIME]) ?? string.Empty;

            if (!TryParseMeasureDateTime(measureDate, measureTime, out DateOnly parsedDate, out TimeOnly parsedTime))
            {
                ClearStartSlot();
                error = $"유량 슬롯의 측정시각 형식이 올바르지 않습니다: {measureDate} {measureTime}";
                return false;
            }

            SlotId = Convert.ToInt32(row[FbtAFMSDischargeTimeslot.COL_ID]);
            MeasureDate = parsedDate;
            MeasureTime = parsedTime;
            return true;
        }

        private static bool TryParseMeasureDateTime(
            string measureDate,
            string measureTime,
            out DateOnly parsedDate,
            out TimeOnly parsedTime)
        {
            bool validDate = DateOnly.TryParseExact(measureDate, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out parsedDate);
            bool validTime = TimeOnly.TryParseExact(measureTime, "HHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out parsedTime);
            return validDate && validTime;
        }

        private string ValidateCalculationKey()
        {
            if (DeviceType == MeasurementDeviceType.None)
                return "측정장비 유형이 설정되지 않았습니다.";
            if (DeviceId < 0)
                return "측정장비 ID가 설정되지 않았습니다.";
            if (Method == DischargeMethod.None)
                return "유량 산정법이 설정되지 않았습니다.";

            return string.Empty;
        }

        private void ClearStartSlot()
        {
            SlotId = -1;
            MeasureDate = default;
            MeasureTime = default;
        }
    }
}
