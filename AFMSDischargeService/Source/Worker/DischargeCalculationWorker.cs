using AFMSDll;
using System.Data;

namespace AFMSDischargeService
{
    internal sealed class DischargeCalculationWorker(
        ILogger<DischargeCalculationWorker> logger) : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
        private readonly List<_QBase> calculators = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LoadCalculators(stoppingToken);

            logger.LogInformation(
                "서비스 시작 설정을 기준으로 유량 산정 객체 {Count}개를 준비했습니다.",
                calculators.Count);

            using PeriodicTimer timer = new(PollInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    foreach (_QBase calculator in calculators)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        // 슬롯별 입력 자료 확인과 유량 산정은 다음 구현에서 수행합니다.
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private void LoadCalculators(CancellationToken stoppingToken)
        {
            calculators.Clear();

            string sql = $"SELECT C.{FbtAFMSDischargeConfig.COL_ID},";
            sql += $" C.{FbtAFMSDischargeConfig.COL_HYDRO_ID},";
            sql += $" C.{FbtAFMSDischargeConfig.COL_MID_SECTION},";
            sql += $" C.{FbtAFMSDischargeConfig.COL_RATING_CURVE},";
            sql += $" C.{FbtAFMSDischargeConfig.COL_SURFACE_VELOCITY},";
            sql += $" C.{FbtAFMSDischargeConfig.COL_VELOCITY_DISTRIBUTION}";
            sql += $" FROM {FbtAFMSDischargeConfig.TABLE_NAME} C";
            sql += $" WHERE C.{FbtAFMSDischargeConfig.COL_ID} = (";
            sql += $"SELECT MAX(C2.{FbtAFMSDischargeConfig.COL_ID})";
            sql += $" FROM {FbtAFMSDischargeConfig.TABLE_NAME} C2";
            sql += $" WHERE C2.{FbtAFMSDischargeConfig.COL_HYDRO_ID}";
            sql += $" = C.{FbtAFMSDischargeConfig.COL_HYDRO_ID})";
            sql += $" ORDER BY C.{FbtAFMSDischargeConfig.COL_HYDRO_ID}";

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"유량 산정 설정 조회 실패: {error}");

            foreach (DataRow row in table.Rows)
            {
                stoppingToken.ThrowIfCancellationRequested();

                int dischargeConfigId = Convert.ToInt32(row[FbtAFMSDischargeConfig.COL_ID]);
                int hydroId = Convert.ToInt32(row[FbtAFMSDischargeConfig.COL_HYDRO_ID]);

                AddCalculatorIfEnabled(row, FbtAFMSDischargeConfig.COL_MID_SECTION,
                    new QMidSection(), dischargeConfigId, hydroId);
                AddCalculatorIfEnabled(row, FbtAFMSDischargeConfig.COL_RATING_CURVE,
                    new QRatingCurve(), dischargeConfigId, hydroId);
                AddCalculatorIfEnabled(row, FbtAFMSDischargeConfig.COL_SURFACE_VELOCITY,
                    new QSurfaceVelocity(), dischargeConfigId, hydroId);
                AddCalculatorIfEnabled(row, FbtAFMSDischargeConfig.COL_VELOCITY_DISTRIBUTION,
                    new QVelocityDistribution(), dischargeConfigId, hydroId);
            }
        }

        private void AddCalculatorIfEnabled(
            DataRow row,
            string columnName,
            _QBase calculator,
            int dischargeConfigId,
            int hydroId)
        {
            if (row[columnName] == DBNull.Value || Convert.ToInt32(row[columnName]) != 1) return;

            calculator.DischargeConfigId = dischargeConfigId;
            calculator.HydroMeterId = hydroId;
            calculators.Add(calculator);

            logger.LogInformation(
                "유량 산정 객체 추가: 유속계 {HydroId}, 산정법 {Method}",
                hydroId,
                calculator.Method);
        }
    }
}
