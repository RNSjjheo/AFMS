using System;
using System.Data;
using System.Globalization;
using AFMSDll;
using log4net;

namespace AFMSDischargeService
{
    internal abstract class QCalculatorBase : _QBase
    {
        private const int MaxStatusMessageLength = 255;
        private readonly ILog log;
        private readonly TransectCollection calculationTransects = new();
        private readonly DateTime calculationStartTime;

        /// <summary>단면 구간 계산에 사용하는 전체 측선 목록입니다.</summary>
        public TransectCollection Transects => Configuration.CrossSection.Transects;
        /// <summary>현재 유량 산정법 설정에서 실제 연산에 사용하는 측선 목록입니다.</summary>
        public IReadOnlyList<Transect> CalculationTransects => calculationTransects;
        /// <summary>서비스 시작 시 확정된 공통 단면 ID입니다.</summary>
        public int StartupCrossSectionId { get; set; } = -1;
        /// <summary>서비스 시작 시 장비별로 확정된 공통 측선 설정 ID입니다.</summary>
        public int StartupTransectConfigId { get; set; } = -1;

        protected QCalculatorBase(
            DischargeMethod method,
            DateTime calculationStartTime) : base(method)
        {
            log = LogManager.GetLogger(GetType());
            this.calculationStartTime = calculationStartTime;
        }

        /// <summary>
        /// 준비된 설정, 수집자료 및 슬롯을 사용하여 유량을 산정합니다.
        /// </summary>
        public abstract bool Calculate(out string error);

        /// <summary>연속된 측선 번호 범위를 현재 산정법의 연산 대상으로 설정합니다.</summary>
        protected bool TrySetCalculationTransects(int minNo, int maxNo, out string error)
        {
            calculationTransects.Clear();

            if (minNo < 1 || maxNo < minNo)
            {
                error = $"산정 측선 범위가 올바르지 않습니다: {minNo}~{maxNo}";
                return false;
            }

            calculationTransects.AddRange(Transects.Where(item =>
                item.No >= minNo && item.No <= maxNo));
            if (calculationTransects.Count == 0)
            {
                error = $"산정 범위에 포함되는 측선이 없습니다: {minNo}~{maxNo}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>측선 번호 목록을 현재 산정법의 연산 대상으로 설정합니다.</summary>
        protected bool TrySetCalculationTransects(IEnumerable<int> transectNos, out string error)
        {
            HashSet<int> selectedNos;
            List<int> missingNos;

            ArgumentNullException.ThrowIfNull(transectNos);
            calculationTransects.Clear();

            selectedNos = transectNos.ToHashSet();
            if (selectedNos.Count == 0)
            {
                error = "산정에 사용할 측선이 설정되지 않았습니다.";
                return false;
            }

            calculationTransects.AddRange(Transects.Where(item => selectedNos.Contains(item.No)));
            missingNos = selectedNos
                .Except(calculationTransects.Select(item => item.No))
                .OrderBy(item => item)
                .ToList();
            if (missingNos.Count > 0)
            {
                calculationTransects.Clear();
                error = $"설정된 측선 정보를 찾을 수 없습니다: {string.Join(", ", missingNos)}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>전체 측선과 산정 대상 측선을 초기화합니다.</summary>
        protected void ClearTransects()
        {
            Transects.Clear();
            calculationTransects.Clear();
        }

        /// <summary>
        /// 현재 자료를 산정하고 결과 저장이 완료되면 다음 원시자료와 슬롯을 준비합니다.
        /// </summary>
        public bool CalculateAndMoveNext(FBDatabase db)
        {
            ArgumentNullException.ThrowIfNull(db);

            Calculation.Status = DischargeCalculationStatus.Calculated;
            Calculation.StatusMessage = string.Empty;
            Calculation.Formula = string.Empty;
            Calculation.CrossSectionArea = 0.0;
            Calculation.Velocity = 0.0;
            Calculation.Value = 0.0;
            Calculation.Uncertainty = 0.0;

            if (!Calculate(out string error))
            {
                Calculation.Status = DischargeCalculationStatus.CalculationFailed;
                Calculation.StatusMessage = NormalizeStatusMessage(error);
                Calculation.Formula = string.Empty;
                LogFailure("계산 실패", error);
            }

            if (!TrySaveCalculationResult(db, out error))
            {
                LogFailure("결과 저장 실패", error);
                return false;
            }

            if (!TryLoadMeasurementStart(db, out error))
            {
                if (!string.IsNullOrEmpty(error))
                {
                    LogFailure("다음 원시자료 조회 실패", error);
                    return false;
                }

                ClearStartSlot();
                return true;
            }

            TryLoadStartSlot(db, out error);
            if (!string.IsNullOrEmpty(error))
            {
                LogFailure("다음 슬롯 조회 실패", error);
                return false;
            }

            return true;
        }

        private bool TrySaveCalculationResult(FBDatabase db, out string error)
        {
            DateTime sourceTime = Measurement.SourceDate.ToDateTime(Measurement.SourceTime);
            QueryBuilderInsert query = new();
            query.Table = FbtAFMSDischargeResult.TABLE_NAME;
            query.AutoIncrement = FbtAFMSDischargeResult.COL_ID;
            query.Value(FbtAFMSDischargeResult.COL_SLOT_ID, Calculation.SlotId);
            query.Value(FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE, Configuration.DeviceType.ToString());
            query.Value(FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID, Configuration.DeviceId);
            query.Value(FbtAFMSDischargeResult.COL_DISCHARGE_METHOD, Configuration.Method.ToString());
            query.Value(FbtAFMSDischargeResult.COL_HYDRO_CONFIG_ID, Configuration.DeviceId);
            query.Value(FbtAFMSDischargeResult.COL_DISCHARGE_CONFIG_ID, Configuration.MethodConfigId);
            query.Value(FbtAFMSDischargeResult.COL_TRANSECT_CONFIG_ID, Configuration.TransectConfigId);
            query.Value(FbtAFMSDischargeResult.COL_METHOD_CONFIG_ID, Configuration.MethodConfigId);
            query.Value(FbtAFMSDischargeResult.COL_WATER_LEVEL,
                Measurement.HasWaterLevel ? Measurement.WaterLevel : null);
            bool calculationFailed = Calculation.Status == DischargeCalculationStatus.CalculationFailed;
            query.Value(FbtAFMSDischargeResult.COL_VELOCITY,
                calculationFailed ? null : Calculation.Velocity);
            query.Value(FbtAFMSDischargeResult.COL_CROSS_SECTION_AREA,
                calculationFailed ? null : Calculation.CrossSectionArea);
            query.Value(FbtAFMSDischargeResult.COL_DISCHARGE,
                calculationFailed ? null : Calculation.Value);
            query.Value(FbtAFMSDischargeResult.COL_CALCULATION_STATUS, Calculation.Status.ToString());
            query.Value(FbtAFMSDischargeResult.COL_STATUS_MESSAGE, Calculation.StatusMessage);
            query.Value(FbtAFMSDischargeResult.COL_CALCULATION_FORMULA, Calculation.Formula);
            query.Value(FbtAFMSDischargeResult.COL_SOURCE_TIME, sourceTime, typeof(DateTime));
            query.Value(FbtAFMSDischargeResult.COL_CALCULATED_AT, DateTime.Now, typeof(DateTime));

            db.Execute(query, out error);
            return string.IsNullOrEmpty(error);
        }

        private static string NormalizeStatusMessage(string error)
        {
            string message = string.IsNullOrWhiteSpace(error)
                ? "유량 산정 조건을 만족하지 못했습니다."
                : error.Trim();
            return message.Length <= MaxStatusMessageLength
                ? message
                : message[..MaxStatusMessageLength];
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

            if (received && Calculation.SlotId >= 0)
            {
                if (!TryPrepareCalculationMeasurements(db, out bool loaded)) return false;
                if (loaded) return true;
            }

            int previousId = Measurement.SourceId;
            DateOnly previousDate = Measurement.SourceDate;
            TimeOnly previousTime = Measurement.SourceTime;

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

            log.Info($"유량 산정 시작값 이동: {GetLogContext()}, " +
                $"이전 {previousId} ({previousDate:yyyy-MM-dd} {previousTime:HH:mm:ss}), " +
                $"변경 {Measurement.SourceId} ({Measurement.SourceDate:yyyy-MM-dd} {Measurement.SourceTime:HH:mm:ss}), " +
                $"슬롯 {(hasSlot ? DischargeLogFormatter.GetSlotKey(Calculation) : "없음")}");

            if (!hasSlot) return false;
            return TryPrepareCalculationMeasurements(db, out bool movedMeasurementLoaded) &&
                   movedMeasurementLoaded;
        }

        private bool TryPrepareCalculationMeasurements(FBDatabase db, out bool loaded)
        {
            if (!TryLoadCalculationMeasurements(db, out loaded, out string error))
            {
                LogFailure("산정 입력자료 조회 실패", error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 현재 슬롯 산정에 추가로 필요한 수위 등의 입력자료를 불러옵니다.
        /// </summary>
        protected virtual bool TryLoadCalculationMeasurements(
            FBDatabase db,
            out bool loaded,
            out string error)
        {
            ArgumentNullException.ThrowIfNull(db);
            loaded = true;
            error = string.Empty;
            return true;
        }

        private void LogFailure(string operation, string error)
        {
            log.Error(
                $"[Q FAIL ] {DischargeLogFormatter.GetMethodName(Configuration.Method)}" +
                $" | 장비={DischargeLogFormatter.GetDeviceKey(Configuration, Measurement)}" +
                $" | 원시={DischargeLogFormatter.GetSourceKey(Measurement)}" +
                $" | 슬롯={DischargeLogFormatter.GetSlotKey(Calculation)}" +
                $" | 단계={operation} | 오류={error}");
        }

        private string GetLogContext()
        {
            return $"{Configuration.DeviceType} {Configuration.DeviceId} ({Measurement.DeviceName}), 산정법 {Configuration.Method}";
        }

        /// <summary>
        /// 산정법별 설정정보를 불러옵니다. 파생 산정 객체에서 필요한 설정 조회를 구현합니다.
        /// </summary>
        public virtual bool TryLoadConfiguration(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// DeviceId에 해당하는 측정장비 종류를 확인하고 원시 측정 테이블을 연결합니다.
        /// </summary>
        public bool TryConnectMeasurementTable(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            Measurement.DeviceName = string.Empty;
            Measurement.TableName = string.Empty;
            Measurement.Table = null;

            if (Configuration.DeviceType == MeasurementDeviceType.WaterLevelGauge)
            {
                Measurement.DeviceName = "시스템 수위계";
                Measurement.Table = new FbtWATERLEVEL();
                Measurement.TableName = Measurement.Table.GetTableName();
                error = string.Empty;
                return true;
            }

            if (Configuration.DeviceType != MeasurementDeviceType.VelocityMeter || Configuration.DeviceId < 0)
            {
                error = "조회할 유속계가 설정되지 않았습니다.";
                return false;
            }

            QueryBuilderSelect query = new();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSHydroMeter.COL_DEVICE_NAME);
            query.Add(FbtAFMSHydroMeter.COL_DATA_TABLE);
            query.Where(FbtAFMSHydroMeter.COL_ID, "=", Configuration.DeviceId);

            DataTable table = db.Execute(query, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"유속계 정보를 찾을 수 없습니다: DeviceId={Configuration.DeviceId}";
                return false;
            }

            Measurement.DeviceName = Convert.ToString(table.Rows[0][FbtAFMSHydroMeter.COL_DEVICE_NAME])?.Trim() ?? string.Empty;
            Measurement.TableName = Convert.ToString(table.Rows[0][FbtAFMSHydroMeter.COL_DATA_TABLE])?.Trim() ?? string.Empty;

            if (string.Equals(Measurement.TableName, FbtHYDROMETERMPDS.TABLE_NAME, StringComparison.OrdinalIgnoreCase))
                Measurement.Table = new FbtHYDROMETERMPDS();
            else if (string.Equals(Measurement.TableName, FbtHYDROMETERVIDEO.TABLE_NAME, StringComparison.OrdinalIgnoreCase))
                Measurement.Table = new FbtHYDROMETERVIDEO();
            else if (Configuration.Method == DischargeMethod.SurfaceVelo)
                Measurement.Table = CreateChannelMasterTable(Measurement.TableName);

            if (Measurement.Table == null)
            {
                error = string.IsNullOrEmpty(Measurement.TableName)
                    ? $"유속계의 데이터 테이블이 설정되지 않았습니다: DeviceId={Configuration.DeviceId}"
                    : $"지원하지 않는 유속계 데이터 테이블입니다: {Measurement.TableName}";
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

            Measurement.SourceId = -1;
            Measurement.SourceDate = default;
            Measurement.SourceTime = default;
            Measurement.HasSource = false;
            Measurement.LastCalculatedSourceTime = null;

            if (Measurement.Table == null || string.IsNullOrEmpty(Measurement.TableName))
            {
                error = "측정 테이블이 연결되지 않았습니다.";
                return false;
            }

            string resultSql = $"SELECT MAX({FbtAFMSDischargeResult.COL_SOURCE_TIME})";
            resultSql += $" FROM {FbtAFMSDischargeResult.TABLE_NAME}";
            resultSql += $" WHERE {FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE} = '{Configuration.DeviceType}'";
            resultSql += $" AND {FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID} = {Configuration.DeviceId}";
            resultSql += $" AND {FbtAFMSDischargeResult.COL_DISCHARGE_METHOD} = '{Configuration.Method}'";

            DataTable resultTable = db.Execute(resultSql, out error);
            if (!string.IsNullOrEmpty(error)) return false;

            if (resultTable.Rows.Count > 0 && resultTable.Rows[0][0] != DBNull.Value)
                Measurement.LastCalculatedSourceTime = Convert.ToDateTime(resultTable.Rows[0][0]);

            bool hasMeasurementId = Configuration.DeviceType == MeasurementDeviceType.VelocityMeter && Measurement.Table is not FbtRHYDROMETER;
            string sourceSql = "SELECT FIRST 1";
            if (hasMeasurementId) sourceSql += $" {FbtAFMSHydroMeter.COL_ID},";
            sourceSql += $" {FbtAFMSHydroMeter.COL_MEASURE_DATE},";
            sourceSql += $" {FbtAFMSHydroMeter.COL_MEASURE_TIME}";
            sourceSql += $" FROM {Measurement.TableName}";
            string calculationStart = calculationStartTime
                .ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
            sourceSql += $" WHERE {_FBTableBase.SQL_MEASURE_DATETIME} >= '{calculationStart}'";
            if (Measurement.Table is FbtRHYDROMETER)
                sourceSql += $" AND UPPER(TRIM({FbtRHYDROMETER.COL_HYDRO_KIND})) = 'CHANNELMASTER'";
            if (Measurement.LastCalculatedSourceTime.HasValue)
            {
                string lastSourceDateTime = Measurement.LastCalculatedSourceTime.Value.ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
                sourceSql += $" AND {_FBTableBase.SQL_MEASURE_DATETIME} > '{lastSourceDateTime}'";
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

            Measurement.SourceId = hasMeasurementId
                ? Convert.ToInt32(sourceRow[FbtAFMSHydroMeter.COL_ID])
                : -1;
            Measurement.SourceDate = parsedDate;
            Measurement.SourceTime = parsedTime;
            Measurement.HasSource = true;
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

            if (Measurement.Table == null || string.IsNullOrEmpty(Measurement.TableName))
            {
                error = "측정 테이블이 연결되지 않았습니다.";
                return false;
            }

            if (!Measurement.HasSource)
                return true;

            if (Measurement.Table is FbtWATERLEVEL)
                return TryCheckWaterLevelReceived(db, out received, out error);

            if (Measurement.Table is FbtRHYDROMETER)
                return TryCheckChannelMasterReceived(db, out received, out error);

            if (Measurement.SourceId < 0) return true;

            if (Measurement.Table is FbtHYDROMETERMPDS)
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

            if (Measurement.Table is FbtHYDROMETERVIDEO)
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

            error = $"수신 완료 여부를 확인할 수 없는 측정 테이블입니다: {Measurement.TableName}";
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

            if (!Measurement.HasSource || Measurement.Table is FbtWATERLEVEL)
                return true;

            if (Measurement.Table is FbtRHYDROMETER)
                return TryMoveToNextReceivedChannelMaster(db, out moved, out error);

            string expectedCountColumn;
            string measureOkColumn;
            string cellTableName;
            string parentIdColumn;

            if (Measurement.Table is FbtHYDROMETERMPDS)
            {
                expectedCountColumn = FbtHYDROMETERMPDS.COL_DEVICE_COUNT;
                measureOkColumn = FbtHYDROMETERMPDS.COL_MEASURE_OK;
                cellTableName = FbtHYDROMETERMPDSCELL.TABLE_NAME;
                parentIdColumn = FbtHYDROMETERMPDSCELL.COL_MPDS_ID;
            }
            else if (Measurement.Table is FbtHYDROMETERVIDEO)
            {
                expectedCountColumn = FbtHYDROMETERVIDEO.COL_CELL_COUNT;
                measureOkColumn = FbtHYDROMETERVIDEO.COL_MEASURE_OK;
                cellTableName = FbtHYDROMETERVIDEOCELL.TABLE_NAME;
                parentIdColumn = FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID;
            }
            else
            {
                error = $"다음 수신 완료 자료를 확인할 수 없는 측정 테이블입니다: {Measurement.TableName}";
                return false;
            }

            string currentDateTime = $"{Measurement.SourceDate:yyyyMMdd} {Measurement.SourceTime:HHmmss}";
            string sql = $"SELECT FIRST 1 M.{_FBTableBase.COL_ID},";
            sql += $" M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}";
            sql += $" FROM {Measurement.TableName} M";
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

            Measurement.SourceId = Convert.ToInt32(row[_FBTableBase.COL_ID]);
            Measurement.SourceDate = parsedDate;
            Measurement.SourceTime = parsedTime;
            moved = true;
            return true;
        }

        private bool TryCheckChannelMasterReceived(FBDatabase db, out bool received, out string error)
        {
            received = false;
            if (!TryGetChannelMasterReadyFlag(out string readyFlagColumn))
            {
                error = $"ChannelMaster 수신 완료 플래그를 확인할 수 없는 측정 테이블입니다: {Measurement.TableName}";
                return false;
            }

            string date = Measurement.SourceDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string time = Measurement.SourceTime.ToString("HHmmss", CultureInfo.InvariantCulture);
            string sql = $"SELECT COUNT(*) FROM {FbtRPOINT.TABLE_NAME}";
            sql += $" WHERE {FbtRPOINT.COL_MEASURE_DATE} = '{date}'";
            sql += $" AND {FbtRPOINT.COL_MEASURE_TIME} = '{time}'";
            sql += $" AND COALESCE({readyFlagColumn}, 'N') = 'Y'";

            DataTable table = db.Execute(sql, out error);
            received = string.IsNullOrEmpty(error) && table.Rows.Count > 0 && Convert.ToInt32(table.Rows[0][0]) > 0;
            return string.IsNullOrEmpty(error);
        }

        private bool TryMoveToNextReceivedChannelMaster(FBDatabase db, out bool moved, out string error)
        {
            moved = false;
            if (!TryGetChannelMasterReadyFlag(out string readyFlagColumn))
            {
                error = $"ChannelMaster 수신 완료 플래그를 확인할 수 없는 측정 테이블입니다: {Measurement.TableName}";
                return false;
            }

            string currentDateTime = $"{Measurement.SourceDate:yyyyMMdd} {Measurement.SourceTime:HHmmss}";
            string sql = $"SELECT FIRST 1 M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}";
            sql += $" FROM {Measurement.TableName} M";
            sql += $" INNER JOIN {FbtRPOINT.TABLE_NAME} P";
            sql += $" ON P.{FbtRPOINT.COL_MEASURE_DATE} = M.{_FBTableBase.COL_MEASURE_DATE}";
            sql += $" AND P.{FbtRPOINT.COL_MEASURE_TIME} = M.{_FBTableBase.COL_MEASURE_TIME}";
            sql += $" WHERE (M.{_FBTableBase.COL_MEASURE_DATE} || ' ' || M.{_FBTableBase.COL_MEASURE_TIME}) > '{currentDateTime}'";
            sql += $" AND UPPER(TRIM(M.{FbtRHYDROMETER.COL_HYDRO_KIND})) = 'CHANNELMASTER'";
            sql += $" AND COALESCE(P.{readyFlagColumn}, 'N') = 'Y'";
            sql += $" ORDER BY M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}";

            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error) || table.Rows.Count == 0) return string.IsNullOrEmpty(error);

            DataRow row = table.Rows[0];
            string measureDate = Convert.ToString(row[_FBTableBase.COL_MEASURE_DATE]) ?? string.Empty;
            string measureTime = Convert.ToString(row[_FBTableBase.COL_MEASURE_TIME]) ?? string.Empty;
            if (!TryParseMeasureDateTime(measureDate, measureTime, out DateOnly parsedDate, out TimeOnly parsedTime))
            {
                error = $"다음 ChannelMaster 자료의 측정시각 형식이 올바르지 않습니다: {measureDate} {measureTime}";
                return false;
            }

            Measurement.SourceId = -1;
            Measurement.SourceDate = parsedDate;
            Measurement.SourceTime = parsedTime;
            moved = true;
            return true;
        }

        private bool TryGetChannelMasterReadyFlag(out string readyFlagColumn)
        {
            readyFlagColumn = Measurement.Table switch
            {
                FbtRHYDROMETER1 => FbtRPOINT.COL_HYDROMETER1_FLAG,
                FbtRHYDROMETER2 => FbtRPOINT.COL_HYDROMETER2_FLAG,
                FbtRHYDROMETER3 => FbtRPOINT.COL_HYDROMETER3_FLAG,
                _ => string.Empty
            };
            return !string.IsNullOrEmpty(readyFlagColumn);
        }

        private static FbtRHYDROMETER? CreateChannelMasterTable(string tableName)
        {
            if (string.Equals(tableName, FbtRHYDROMETER1.TABLE_NAME, StringComparison.OrdinalIgnoreCase)) return new FbtRHYDROMETER1();
            if (string.Equals(tableName, FbtRHYDROMETER2.TABLE_NAME, StringComparison.OrdinalIgnoreCase)) return new FbtRHYDROMETER2();
            if (string.Equals(tableName, FbtRHYDROMETER3.TABLE_NAME, StringComparison.OrdinalIgnoreCase)) return new FbtRHYDROMETER3();
            return null;
        }

        private bool TryCheckWaterLevelReceived(FBDatabase db, out bool received, out string error)
        {
            string date = Measurement.SourceDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string time = Measurement.SourceTime.ToString("HHmmss", CultureInfo.InvariantCulture);
            string sql = "SELECT COUNT(*)";
            sql += $" FROM {Measurement.TableName}";
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
            masterSql += $" FROM {Measurement.TableName}";
            masterSql += $" WHERE {_FBTableBase.COL_ID} = {Measurement.SourceId}";

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
            cellSql += $" WHERE {parentIdColumn} = {Measurement.SourceId}";

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
            sql += $" WHERE S.{FbtAFMSDischargeTimeslot.COL_CROSS_SECTION_ID} = {StartupCrossSectionId}";
            sql += " AND NOT EXISTS (";
            sql += $"SELECT 1 FROM {FbtAFMSDischargeResult.TABLE_NAME} R";
            sql += $" WHERE R.{FbtAFMSDischargeResult.COL_SLOT_ID} = S.{FbtAFMSDischargeTimeslot.COL_ID}";
            sql += $" AND R.{FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE} = '{Configuration.DeviceType}'";
            sql += $" AND R.{FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID} = {Configuration.DeviceId}";
            sql += $" AND R.{FbtAFMSDischargeResult.COL_DISCHARGE_METHOD} = '{Configuration.Method}')";
            string calculationStart = calculationStartTime
                .ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
            sql += $" AND (S.{FbtAFMSDischargeTimeslot.COL_MEASURE_DATE} || ' ' || S.{FbtAFMSDischargeTimeslot.COL_MEASURE_TIME}) >= '{calculationStart}'";
            if (Measurement.HasSource)
            {
                string measurementStart = $"{Measurement.SourceDate:yyyyMMdd} {Measurement.SourceTime:HHmmss}";
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

            Calculation.SlotId = Convert.ToInt32(row[FbtAFMSDischargeTimeslot.COL_ID]);
            Calculation.SlotDate = parsedDate;
            Calculation.SlotTime = parsedTime;
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
            if (Configuration.DeviceType == MeasurementDeviceType.None)
                return "측정장비 유형이 설정되지 않았습니다.";
            if (Configuration.DeviceId < 0)
                return "측정장비 ID가 설정되지 않았습니다.";
            if (Configuration.Method == DischargeMethod.None)
                return "유량 산정법이 설정되지 않았습니다.";

            return string.Empty;
        }

        protected static string FormatFormulaNumber(double value)
        {
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        private void ClearStartSlot()
        {
            Calculation.ClearSlot();
        }
    }
}
