using AFMSDll;
using System.Data;

namespace AFMSDischargeService
{
    internal sealed class DischargeCalculationWorker(
        ILogger<DischargeCalculationWorker> logger) : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
        private readonly List<QCalculatorBase> calculators = new();
        private readonly HashSet<string> loggedReadyMeasurements = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LoadCalculators(stoppingToken);
            InitializeCalculators(stoppingToken);

            LogCalculatorList();
            LogCurrentTargets("서비스 시작");
            DateTime nextTargetLogAt = GetNextTenMinuteBoundary(DateTime.Now);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    DateTime now = DateTime.Now;
                    if (now >= nextTargetLogAt)
                    {
                        LogCurrentTargets("10분 정시");
                        do
                        {
                            nextTargetLogAt = nextTargetLogAt.AddMinutes(10);
                        }
                        while (nextTargetLogAt <= now);
                    }

                    bool calculationCompleted = false;
                    using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);

                    foreach (QCalculatorBase calculator in calculators)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        if (!calculator.IsImplemented) continue;
                        if (!calculator.PrepareNextCalculation(db)) continue;
                        LogReadyMeasurement(calculator);
                        if (!calculator.CalculateAndMoveNext(db)) continue;
                        calculationCompleted = true;
                        LogCalculationCompleted(calculator);
                    }

                    if (!calculationCompleted)
                    {
                        TimeSpan delay = nextTargetLogAt - DateTime.Now;
                        if (delay > PollInterval) delay = PollInterval;
                        if (delay > TimeSpan.Zero)
                            await Task.Delay(delay, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private void LogCalculatorList()
        {
            logger.LogInformation(
                "서비스 시작 설정을 기준으로 유량 객체 {Count}개를 준비했습니다.",
                calculators.Count);

            for (int index = 0; index < calculators.Count; index++)
            {
                QCalculatorBase calculator = calculators[index];
                QConfiguration config = calculator.Configuration;
                logger.LogInformation(
                    "유량 객체 [{Index}/{Count}]: {DeviceType} {DeviceId} ({DeviceName}), {MethodName}",
                    index + 1,
                    calculators.Count,
                    config.DeviceType,
                    config.DeviceId,
                    calculator.Measurement.DeviceName,
                    GetMethodLogName(config.Method));
            }
        }

        private void LogCurrentTargets(string reason)
        {
            foreach (QCalculatorBase calculator in calculators)
            {
                QConfiguration config = calculator.Configuration;
                QMeasurementContext measurement = calculator.Measurement;
                QCalculationContext calculation = calculator.Calculation;
                string sourceKey = measurement.HasSource
                    ? $"{measurement.SourceDate:yyyy-MM-dd} {measurement.SourceTime:HH:mm:ss}"
                    : "없음";
                string slotKey = calculation.SlotId >= 0
                    ? $"{calculation.SlotDate:yyyy-MM-dd} {calculation.SlotTime:HH:mm:ss}"
                    : "없음";

                logger.LogInformation(
                    "유량 조회 위치({Reason}): {DeviceType} {DeviceId} ({DeviceName}), {MethodName}, 원시자료 키 {SourceKey}, 슬롯 키 {SlotKey}",
                    reason,
                    config.DeviceType,
                    config.DeviceId,
                    measurement.DeviceName,
                    GetMethodLogName(config.Method),
                    sourceKey,
                    slotKey);
            }
        }

        private static DateTime GetNextTenMinuteBoundary(DateTime now)
        {
            int minute = (now.Minute / 10) * 10;
            DateTime boundary = new(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                minute,
                0,
                now.Kind);
            return boundary.AddMinutes(10);
        }

        private void LogCalculationCompleted(QCalculatorBase calculator)
        {
            logger.LogInformation(
                "유량 완료: {DeviceType} {DeviceId} ({DeviceName}), {MethodName}, 유량 {Discharge}, 평균유속 {Velocity}, 단면적 {Area}",
                calculator.Configuration.DeviceType,
                calculator.Configuration.DeviceId,
                calculator.Measurement.DeviceName,
                GetMethodLogName(calculator.Configuration.Method),
                calculator.Calculation.Value,
                calculator.Calculation.Velocity,
                calculator.Calculation.CrossSectionArea);
        }

        private void LogReadyMeasurement(QCalculatorBase calculator)
        {
            QConfiguration config = calculator.Configuration;
            QMeasurementContext measurement = calculator.Measurement;
            QCalculationContext calculation = calculator.Calculation;

            string key = $"{config.DeviceType}:{config.DeviceId}:{config.Method}:" +
                         $"{measurement.SourceId}:{measurement.SourceDate:yyyyMMdd}:" +
                         $"{measurement.SourceTime:HHmmss}";

            if (!loggedReadyMeasurements.Add(key)) return;

            logger.LogInformation(
                "산정 가능 데이터 발견: {DeviceType} {DeviceId} ({DeviceName}), {MethodName}, 측정 테이블 {MeasurementTable}, 측정값 {MeasurementId} ({MeasurementDate} {MeasurementTime}), 슬롯 {SlotId}",
                config.DeviceType,
                config.DeviceId,
                measurement.DeviceName,
                GetMethodLogName(config.Method),
                measurement.Table!.GetTableName(),
                measurement.SourceId,
                measurement.SourceDate,
                measurement.SourceTime,
                calculation.SlotId);
        }

        private void LoadCalculators(CancellationToken stoppingToken)
        {
            calculators.Clear();

            string sql = $"SELECT C.{FbtAFMSDischargeConfig.COL_ID},";
            sql += $" C.{FbtAFMSDischargeConfig.COL_DEVICE_TYPE},";
            sql += $" C.{FbtAFMSDischargeConfig.COL_DEVICE_ID},";
            sql += $" C.{FbtAFMSDischargeConfig.COL_DISCHARGE_METHOD}";
            sql += $" FROM {FbtAFMSDischargeConfig.TABLE_NAME} C";
            sql += $" WHERE C.{FbtAFMSDischargeConfig.COL_ID} = (";
            sql += $"SELECT MAX(C2.{FbtAFMSDischargeConfig.COL_ID})";
            sql += $" FROM {FbtAFMSDischargeConfig.TABLE_NAME} C2";
            sql += $" WHERE C2.{FbtAFMSDischargeConfig.COL_DEVICE_TYPE} = C.{FbtAFMSDischargeConfig.COL_DEVICE_TYPE}";
            sql += $" AND C2.{FbtAFMSDischargeConfig.COL_DEVICE_ID} = C.{FbtAFMSDischargeConfig.COL_DEVICE_ID}";
            sql += $" AND C2.{FbtAFMSDischargeConfig.COL_DISCHARGE_METHOD} = C.{FbtAFMSDischargeConfig.COL_DISCHARGE_METHOD})";
            sql += $" AND C.{FbtAFMSDischargeConfig.COL_ENABLED} = 1";
            sql += $" ORDER BY C.{FbtAFMSDischargeConfig.COL_DEVICE_TYPE}, C.{FbtAFMSDischargeConfig.COL_DEVICE_ID}";

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"유량 설정 조회 실패: {error}");

            foreach (DataRow row in table.Rows)
            {
                stoppingToken.ThrowIfCancellationRequested();

                if (!Enum.TryParse(Convert.ToString(row[FbtAFMSDischargeConfig.COL_DEVICE_TYPE]), true,
                        out MeasurementDeviceType deviceType) ||
                    !Enum.TryParse(Convert.ToString(row[FbtAFMSDischargeConfig.COL_DISCHARGE_METHOD]), true,
                        out DischargeMethod method)) continue;

                QCalculatorBase? calculator = CreateCalculator(method);
                if (calculator == null || !IsSupportedDevice(method, deviceType)) continue;

                calculator.Configuration.DischargeConfigId = Convert.ToInt32(row[FbtAFMSDischargeConfig.COL_ID]);
                calculator.Configuration.DeviceType = deviceType;
                calculator.Configuration.DeviceId = Convert.ToInt32(row[FbtAFMSDischargeConfig.COL_DEVICE_ID]);
                calculators.Add(calculator);
            }
        }

        private void InitializeCalculators(CancellationToken stoppingToken)
        {
            List<QCalculatorBase> initializedCalculators = new();
            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);

            foreach (QCalculatorBase calculator in calculators)
            {
                stoppingToken.ThrowIfCancellationRequested();

                if (!HasCurrentDeviceConfigurations(db, calculator, out string freshnessError))
                {
                    logger.LogWarning(
                        "유량 설정 필요: {DeviceType} {DeviceId}, {MethodName}, {Reason}",
                        calculator.Configuration.DeviceType,
                        calculator.Configuration.DeviceId,
                        GetMethodLogName(calculator.Configuration.Method),
                        freshnessError);
                    continue;
                }

                calculator.Configuration.MethodConfigId = GetLatestMethodConfigId(
                    db,
                    calculator.Configuration.Method,
                    calculator.Configuration.DeviceType,
                    calculator.Configuration.DeviceId);
                if (calculator.Configuration.MethodConfigId < 0) continue;

                if (!calculator.TryLoadConfiguration(db, out string configurationError))
                {
                    throw new InvalidOperationException(
                        $"유량 설정 조회 실패 " +
                        $"({calculator.Configuration.DeviceType} {calculator.Configuration.DeviceId}, {calculator.Configuration.Method}): {configurationError}");
                }

                if (!calculator.TryConnectMeasurementTable(db, out string measurementTableError))
                {
                    throw new InvalidOperationException(
                        $"측정 테이블 연결 실패 " +
                        $"({calculator.Configuration.DeviceType} {calculator.Configuration.DeviceId}, {calculator.Configuration.Method}): {measurementTableError}");
                }

                bool hasMeasurementStart = calculator.TryLoadMeasurementStart(db, out string measurementStartError);
                if (!string.IsNullOrEmpty(measurementStartError))
                {
                    throw new InvalidOperationException(
                        $"유속 자료 시작값 조회 실패 " +
                        $"({calculator.Configuration.DeviceType} {calculator.Configuration.DeviceId}, {calculator.Configuration.Method}): {measurementStartError}");
                }

                bool hasStartSlot = calculator.TryLoadStartSlot(db, out string startSlotError);
                if (!string.IsNullOrEmpty(startSlotError))
                {
                    throw new InvalidOperationException(
                        $"유량 시작 슬롯 조회 실패 " +
                        $"({calculator.Configuration.DeviceType} {calculator.Configuration.DeviceId}, {calculator.Configuration.Method}): {startSlotError}");
                }

                initializedCalculators.Add(calculator);

                string lastCalculatedSource = calculator.Measurement.LastCalculatedSourceTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "없음";
                string measurementStart = hasMeasurementStart
                    ? $"{calculator.Measurement.SourceId} ({calculator.Measurement.SourceDate:yyyy-MM-dd} {calculator.Measurement.SourceTime:HH:mm:ss})"
                    : "없음";
                string slotStart = hasStartSlot
                    ? $"{calculator.Calculation.SlotId} ({calculator.Calculation.SlotDate:yyyy-MM-dd} {calculator.Calculation.SlotTime:HH:mm:ss})"
                    : "없음";

                logger.LogInformation(
                    "유량 객체 시작값: {DeviceType} {DeviceId} ({DeviceName}), {MethodName}, 측정 테이블 {MeasurementTable}, 마지막 산정값 {LastCalculatedSource}, 유속 시작값 {MeasurementStart}, 슬롯 시작값 {SlotStart}",
                    calculator.Configuration.DeviceType,
                    calculator.Configuration.DeviceId,
                    calculator.Measurement.DeviceName,
                    GetMethodLogName(calculator.Configuration.Method),
                    calculator.Measurement.Table!.GetTableName(),
                    lastCalculatedSource,
                    measurementStart,
                    slotStart);
            }

            calculators.Clear();
            calculators.AddRange(initializedCalculators);
        }

        private static bool HasCurrentTransectConfiguration(
            FBDatabase db,
            QCalculatorBase calculator,
            out string error)
        {
            if (calculator.Configuration.DeviceType != MeasurementDeviceType.VelocityMeter)
            {
                error = string.Empty;
                return true;
            }

            string sql = $"SELECT FIRST 1 C.{FbtAFMSDischargeMethodConfig.COL_TRANSECT_CONFIG_ID}";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C";
            sql += $" WHERE C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE} = '{calculator.Configuration.DeviceType}'";
            sql += $" AND C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID} = {calculator.Configuration.DeviceId}";
            sql += $" AND C.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD} = '{calculator.Configuration.Method}'";
            sql += $" AND C.{FbtAFMSDischargeMethodConfig.COL_ENABLED} = 1";
            sql += $" ORDER BY C.{FbtAFMSDischargeMethodConfig.COL_ID} DESC";
            DataTable config = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (config.Rows.Count == 0 || config.Rows[0][0] == DBNull.Value)
            {
                error = "통합 유량 설정이 없습니다.";
                return false;
            }

            int configuredTransectId = Convert.ToInt32(config.Rows[0][0]);
            string latestSql = $"SELECT MAX({FbtAFMSHydroTransect.COL_ID}) FROM {FbtAFMSHydroTransect.TABLE_NAME}";
            latestSql += $" WHERE {FbtAFMSHydroTransect.COL_HYDRO_ID} = {calculator.Configuration.DeviceId}";
            DataTable latest = db.Execute(latestSql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (latest.Rows.Count == 0 || latest.Rows[0][0] == DBNull.Value)
            {
                error = "최신 측선 설정이 없습니다.";
                return false;
            }

            int latestTransectId = Convert.ToInt32(latest.Rows[0][0]);
            if (configuredTransectId != latestTransectId)
            {
                error = $"측선 설정이 변경되었습니다({configuredTransectId} → {latestTransectId}). 모든 유량 산정법을 다시 설정해야 합니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private bool HasCurrentDeviceConfigurations(
            FBDatabase db,
            QCalculatorBase calculator,
            out string error)
        {
            foreach (QCalculatorBase configured in calculators.Where(item =>
                         item.Configuration.DeviceType == calculator.Configuration.DeviceType &&
                         item.Configuration.DeviceId == calculator.Configuration.DeviceId))
            {
                if (!HasCurrentTransectConfiguration(db, configured, out error)) return false;
            }

            error = string.Empty;
            return true;
        }

        private static QCalculatorBase? CreateCalculator(DischargeMethod method)
        {
            return method switch
            {
                DischargeMethod.MidSection => new QMidSectionCalculator(),
                DischargeMethod.RatingCurve => new QRatingCurveCalculator(),
                DischargeMethod.SurfaceVelo => new QSurfaceVelocityCalculator(),
                DischargeMethod.VeloDist => new QVelocityDistributionCalculator(),
                _ => null
            };
        }

        private static string GetMethodLogName(DischargeMethod method)
        {
            return method switch
            {
                DischargeMethod.VeloDist => "유속분포법",
                DischargeMethod.MidSection => "중간단면적법",
                DischargeMethod.SurfaceVelo => "지표유속법",
                DischargeMethod.RatingCurve => "수위-유량곡선법",
                _ => $"{method}법"
            };
        }

        private static bool IsSupportedDevice(DischargeMethod method, MeasurementDeviceType deviceType)
        {
            return method == DischargeMethod.RatingCurve
                ? deviceType == MeasurementDeviceType.WaterLevelGauge
                : deviceType == MeasurementDeviceType.VelocityMeter;
        }

        private static int GetLatestMethodConfigId(
            FBDatabase db,
            DischargeMethod method,
            MeasurementDeviceType deviceType,
            int deviceId)
        {
            string tableName;
            string? deviceColumn;

            switch (method)
            {
                case DischargeMethod.MidSection:
                    tableName = FbtAFMSDiscAttrMidSection.TABLE_NAME;
                    deviceColumn = FbtAFMSDiscAttrMidSection.COL_HYDRO_ID;
                    break;
                case DischargeMethod.SurfaceVelo:
                    tableName = FbtAFMSDiscAttrSurfaceVelo.TABLE_NAME;
                    deviceColumn = FbtAFMSDiscAttrSurfaceVelo.COL_HYDRO_ID;
                    break;
                case DischargeMethod.VeloDist:
                    tableName = FbtAFMSDiscAttrVelocityDistribution.TABLE_NAME;
                    deviceColumn = FbtAFMSDiscAttrVelocityDistribution.COL_HYDRO_ID;
                    break;
                case DischargeMethod.RatingCurve:
                    tableName = FbtAFMSDiscAttrRatingCurve.TABLE_NAME;
                    deviceColumn = null;
                    break;
                default:
                    return -1;
            }

            string sql = $"SELECT MAX({FbtAFMSDischargeConfig.COL_ID}) FROM {tableName}";
            if (deviceType == MeasurementDeviceType.VelocityMeter && deviceColumn != null)
                sql += $" WHERE {deviceColumn} = {deviceId}";

            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error) || table.Rows.Count == 0 || table.Rows[0][0] == DBNull.Value)
                return -1;

            return Convert.ToInt32(table.Rows[0][0]);
        }
    }
}
