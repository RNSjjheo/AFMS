using AFMSDll;
using Microsoft.Extensions.Options;
using System.Data;

namespace AFMSDischargeService
{
    internal sealed class DischargeCalculationWorker(
        ILogger<DischargeCalculationWorker> logger,
        IOptions<DischargeServiceOptions> options) : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan BacklogDelay = TimeSpan.FromMilliseconds(20);
        private readonly List<QCalculatorBase> calculators = new();
        private readonly HashSet<string> loggedReadyMeasurements = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string completedSourceKey;
            string completedSlotKey;

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
                    using FBDatabase db = FBProvider.Instance.CreateDatabase();

                    foreach (QCalculatorBase calculator in calculators)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        if (!calculator.IsImplemented) continue;
                        if (!calculator.PrepareNextCalculation(db)) continue;
                        completedSourceKey = DischargeLogFormatter.GetSourceKey(calculator.Measurement);
                        completedSlotKey = DischargeLogFormatter.GetSlotKey(calculator.Calculation);
                        LogReadyMeasurement(calculator);
                        if (!calculator.CalculateAndMoveNext(db)) continue;
                        calculationCompleted = true;
                        LogCalculationCompleted(calculator, completedSourceKey, completedSlotKey);
                    }

                    if (calculationCompleted)
                    {
                        await Task.Delay(BacklogDelay, stoppingToken);
                    }
                    else
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

        private void LogCalculationCompleted(
            QCalculatorBase calculator,
            string sourceKey,
            string slotKey)
        {
            QConfiguration config = calculator.Configuration;
            QMeasurementContext measurement = calculator.Measurement;
            QCalculationContext calculation = calculator.Calculation;

            if (calculation.Status == DischargeCalculationStatus.CalculationFailed)
            {
                logger.LogWarning(
                    "[Q ERROR] {MethodName} | 장비={DeviceKey} | 원시={SourceKey} | 슬롯={SlotKey} | 상태={Status} | 오류={Error} | 결과 저장됨",
                    DischargeLogFormatter.GetMethodName(config.Method),
                    DischargeLogFormatter.GetDeviceKey(config, measurement),
                    sourceKey,
                    slotKey,
                    calculation.Status,
                    calculation.StatusMessage);
                return;
            }

            logger.LogInformation(
                "[Q DONE ] {MethodName} | 장비={DeviceKey} | 원시={SourceKey} | 슬롯={SlotKey} | 상태={Status} | Q={Discharge:0.###} | V={Velocity:0.###} | A={Area:0.###}",
                DischargeLogFormatter.GetMethodName(config.Method),
                DischargeLogFormatter.GetDeviceKey(config, measurement),
                sourceKey,
                slotKey,
                calculation.Status,
                calculation.Value,
                calculation.Velocity,
                calculation.CrossSectionArea);
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
                "[Q READY] {MethodName} | 장비={DeviceKey} | 원시={SourceKey} | 슬롯={SlotKey}",
                DischargeLogFormatter.GetMethodName(config.Method),
                DischargeLogFormatter.GetDeviceKey(config, measurement),
                DischargeLogFormatter.GetSourceKey(measurement),
                DischargeLogFormatter.GetSlotKey(calculation));
        }

        private void LoadCalculators(CancellationToken stoppingToken)
        {
            calculators.Clear();

            string sql = $"SELECT C.{FbtAFMSDischargeMethodConfig.COL_ID},";
            sql += $" C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE},";
            sql += $" C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID},";
            sql += $" C.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD}";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C";
            sql += $" WHERE C.{FbtAFMSDischargeMethodConfig.COL_ID} = (";
            sql += $"SELECT MAX(C2.{FbtAFMSDischargeMethodConfig.COL_ID})";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C2";
            sql += $" WHERE C2.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE} = C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE}";
            sql += $" AND C2.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID} = C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID}";
            sql += $" AND C2.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD} = C.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD})";
            sql += $" AND C.{FbtAFMSDischargeMethodConfig.COL_ENABLED} = 1";
            sql += $" ORDER BY C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE}, C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID}";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"유량 설정 조회 실패: {error}");

            foreach (DataRow row in table.Rows)
            {
                stoppingToken.ThrowIfCancellationRequested();

                if (!Enum.TryParse(Convert.ToString(row[FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE]), true,
                        out MeasurementDeviceType deviceType) ||
                    !Enum.TryParse(Convert.ToString(row[FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD]), true,
                        out DischargeMethod method)) continue;

                QCalculatorBase? calculator = CreateCalculator(
                    method,
                    options.Value.CalculationStartTime);
                if (calculator == null || !IsSupportedDevice(method, deviceType)) continue;

                calculator.Configuration.MethodConfigId = Convert.ToInt32(row[FbtAFMSDischargeMethodConfig.COL_ID]);
                calculator.Configuration.DeviceType = deviceType;
                calculator.Configuration.DeviceId = Convert.ToInt32(row[FbtAFMSDischargeMethodConfig.COL_DEVICE_ID]);
                calculators.Add(calculator);
            }
        }

        private void InitializeCalculators(CancellationToken stoppingToken)
        {
            List<QCalculatorBase> initializedCalculators = new();
            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            int startupCrossSectionId = LoadStartupCrossSectionId(db);
            Dictionary<int, int> startupTransectIds = LoadStartupTransectIds(db);

            logger.LogInformation(
                "서비스 시작 단면을 고정했습니다: CrossSectionId={CrossSectionId}",
                startupCrossSectionId);

            foreach (QCalculatorBase calculator in calculators)
            {
                stoppingToken.ThrowIfCancellationRequested();
                calculator.StartupCrossSectionId = startupCrossSectionId;
                if (calculator.Configuration.DeviceType == MeasurementDeviceType.VelocityMeter)
                {
                    if (!startupTransectIds.TryGetValue(
                            calculator.Configuration.DeviceId,
                            out int startupTransectId))
                    {
                        logger.LogWarning(
                            "유량 객체를 제외합니다: {DeviceType} {DeviceId}, {MethodName}, 서비스 시작 시 사용할 측선 정보가 없습니다.",
                            calculator.Configuration.DeviceType,
                            calculator.Configuration.DeviceId,
                            GetMethodLogName(calculator.Configuration.Method));
                        continue;
                    }

                    calculator.StartupTransectConfigId = startupTransectId;
                }

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

        private static int LoadStartupCrossSectionId(FBDatabase db)
        {
            string sql = $"SELECT MAX({FbtAFMSCrossSection.COL_ID}) FROM {FbtAFMSCrossSection.TABLE_NAME}";
            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"서비스 시작 단면 조회 실패: {error}");
            if (table.Rows.Count == 0 || table.Rows[0][0] == DBNull.Value)
                throw new InvalidOperationException("서비스 시작 시 사용할 단면 정보가 없습니다.");

            return Convert.ToInt32(table.Rows[0][0]);
        }

        private static Dictionary<int, int> LoadStartupTransectIds(FBDatabase db)
        {
            string sql = $"SELECT {FbtAFMSHydroTransect.COL_HYDRO_ID},";
            sql += $" MAX({FbtAFMSHydroTransect.COL_ID})";
            sql += $" FROM {FbtAFMSHydroTransect.TABLE_NAME}";
            sql += $" GROUP BY {FbtAFMSHydroTransect.COL_HYDRO_ID}";

            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"서비스 시작 측선 조회 실패: {error}");

            return table.Rows.Cast<DataRow>().ToDictionary(
                row => Convert.ToInt32(row[0]),
                row => Convert.ToInt32(row[1]));
        }

        private static QCalculatorBase? CreateCalculator(
            DischargeMethod method,
            DateTime calculationStartTime)
        {
            return method switch
            {
                DischargeMethod.MidSection => new QMidSectionCalculator(calculationStartTime),
                DischargeMethod.RatingCurve => new QRatingCurveCalculator(calculationStartTime),
                DischargeMethod.SurfaceVelo => new QSurfaceVelocityCalculator(calculationStartTime),
                DischargeMethod.VeloDist => new QVelocityDistributionCalculator(calculationStartTime),
                _ => null
            };
        }

        private static string GetMethodLogName(DischargeMethod method)
        {
            return DischargeLogFormatter.GetMethodName(method);
        }

        private static bool IsSupportedDevice(DischargeMethod method, MeasurementDeviceType deviceType)
        {
            return method == DischargeMethod.RatingCurve
                ? deviceType == MeasurementDeviceType.WaterLevelGauge
                : deviceType == MeasurementDeviceType.VelocityMeter;
        }

    }
}
