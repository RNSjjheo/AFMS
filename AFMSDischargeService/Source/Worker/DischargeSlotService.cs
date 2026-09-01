using AFMSDll;
using Microsoft.Extensions.Options;
using System.Data;

namespace AFMSDischargeService
{
    internal sealed class DischargeSlotService(
        ILogger<DischargeSlotService> logger,
        IHostApplicationLifetime applicationLifetime,
        IOptions<DischargeServiceOptions> options) : BackgroundService
    {
        private static readonly TimeSpan SlotInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
        private int startupCrossSectionId = -1;

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using CancellationTokenSource startupCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                applicationLifetime.ApplicationStopping);
            CancellationToken stoppingToken = startupCancellation.Token;

            stoppingToken.ThrowIfCancellationRequested();

            List<string> tableLogs = FBProvider.Instance.CheckTables();
            stoppingToken.ThrowIfCancellationRequested();

            foreach (string tableLog in tableLogs)
            {
                logger.LogInformation("DB 테이블 확인: {TableLog}", tableLog);
            }

            bool timeslotTableExists = FBProvider.Instance.ExistTable(FbtAFMSDischargeTimeslot.TABLE_NAME, tableLogs);
            bool resultTableExists = FBProvider.Instance.ExistTable(FbtAFMSDischargeResult.TABLE_NAME, tableLogs);

            if (!timeslotTableExists || !resultTableExists)
            {
                throw new InvalidOperationException("유량 테이블을 준비하지 못해 서비스를 시작할 수 없습니다.");
            }

            startupCrossSectionId = LoadStartupCrossSectionId();
            logger.LogInformation(
                "유량 슬롯 생성 단면을 고정했습니다: CrossSectionId={CrossSectionId}",
                startupCrossSectionId);

            CreateMissingSlots(stoppingToken);
            logger.LogInformation("초기 유량 슬롯 준비를 완료했습니다.");

            stoppingToken.ThrowIfCancellationRequested();
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(PollInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        CreateMissingSlots(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "유량 슬롯 생성 중 오류가 발생했습니다.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private void CreateMissingSlots(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            DateTime lastPassedSlot = GetLastPassedSlot(DateTime.Now);
            DateTime calculationStartTime = options.Value.CalculationStartTime;
            if (lastPassedSlot < calculationStartTime) return;

            DateTime? latestSlot = GetLatestSlotTime();
            DateTime nextSlot = latestSlot.HasValue
                ? latestSlot.Value.Add(SlotInterval)
                : calculationStartTime;
            if (nextSlot < calculationStartTime)
                nextSlot = calculationStartTime;

            int createdCount = 0;
            while (nextSlot <= lastPassedSlot)
            {
                stoppingToken.ThrowIfCancellationRequested();

                string error = InsertSlot(nextSlot, startupCrossSectionId);
                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException($"{nextSlot:yyyy-MM-dd HH:mm:ss} 슬롯 생성 실패: {error}");

                createdCount++;
                nextSlot = nextSlot.Add(SlotInterval);
            }

            if (createdCount > 0)
            {
                logger.LogInformation(
                    "유량 슬롯 {Count}개를 생성했습니다. 마지막 슬롯: {LastSlot}",
                    createdCount,
                    lastPassedSlot);
            }
        }

        private static DateTime GetLastPassedSlot(DateTime now)
        {
            DateTime boundary = new(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute / 10 * 10,
                0,
                now.Kind);

            return now > boundary ? boundary : boundary.Subtract(SlotInterval);
        }

        private static DateTime? GetLatestSlotTime()
        {
            QueryBuilderSelect query = new();
            query.Table = FbtAFMSDischargeTimeslot.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSDischargeTimeslot.COL_SLOT_TIME);
            query.OrderByDesc(FbtAFMSDischargeTimeslot.COL_SLOT_TIME);

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (table.Rows.Count == 0 || table.Rows[0][FbtAFMSDischargeTimeslot.COL_SLOT_TIME] == DBNull.Value) return null;

            return Convert.ToDateTime(table.Rows[0][FbtAFMSDischargeTimeslot.COL_SLOT_TIME]);
        }

        private static int LoadStartupCrossSectionId()
        {
            string sql = $"SELECT MAX({FbtAFMSCrossSection.COL_ID}) FROM {FbtAFMSCrossSection.TABLE_NAME}";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"서비스 시작 단면 조회 실패: {error}");
            if (table.Rows.Count == 0 || table.Rows[0][0] == DBNull.Value)
                throw new InvalidOperationException("서비스 시작 시 사용할 단면 정보가 없습니다.");

            return Convert.ToInt32(table.Rows[0][0]);
        }

        private static string InsertSlot(DateTime slotTime, int crossSectionId)
        {
            QueryBuilderInsert query = new();
            query.Table = FbtAFMSDischargeTimeslot.TABLE_NAME;
            query.AutoIncrement = FbtAFMSDischargeTimeslot.COL_ID;
            query.Value(FbtAFMSDischargeTimeslot.COL_MEASURE_DATE, slotTime.ToString("yyyyMMdd"));
            query.Value(FbtAFMSDischargeTimeslot.COL_MEASURE_TIME, slotTime.ToString("HHmmss"));
            query.Value(FbtAFMSDischargeTimeslot.COL_SLOT_TIME, slotTime, typeof(DateTime));
            query.Value(FbtAFMSDischargeTimeslot.COL_CROSS_SECTION_ID, crossSectionId);

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.Execute(query, out string error);
            return error;
        }
    }
}
