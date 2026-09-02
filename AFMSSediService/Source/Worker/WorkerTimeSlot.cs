using AFMSDll;
using Microsoft.Extensions.Options;
using System.Data;

namespace AFMSSediService
{
    internal sealed class WorkerTimeSlot(ILogger<WorkerTimeSlot> logger, IHostApplicationLifetime applicationLifetime, IOptions<SSCServiceOptions> options) : BackgroundService
    {
        private static readonly TimeSpan SlotInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using CancellationTokenSource startupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, applicationLifetime.ApplicationStopping);
            CancellationToken stoppingToken = startupCancellation.Token;

            stoppingToken.ThrowIfCancellationRequested();

            List<string> tableLogs = [];
            if (!FBProvider.Instance.ExistTable(FbtAFMSSediTimeslot.TABLE_NAME, tableLogs))
            {
                throw new InvalidOperationException(
                    $"{FbtAFMSSediTimeslot.TABLE_NAME} 테이블을 준비하지 못해 서비스를 시작할 수 없습니다.");
            }

            CreateMissingSlots(stoppingToken);
            logger.LogInformation("초기 SEDI 슬롯 준비를 완료했습니다.");

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
                        logger.LogError(exception, "SEDI 슬롯 생성 중 오류가 발생했습니다.");
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

                string error = InsertSlot(nextSlot);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException(
                        $"{nextSlot:yyyy-MM-dd HH:mm:ss} SEDI 슬롯 생성 실패: {error}");
                }

                createdCount++;
                nextSlot = nextSlot.Add(SlotInterval);
            }

            if (createdCount > 0)
            {
                logger.LogInformation(
                    "SEDI 슬롯 {Count}개를 생성했습니다. 마지막 슬롯: {LastSlot}",
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

            return now > boundary
                ? boundary
                : boundary.Subtract(SlotInterval);
        }

        private static DateTime? GetLatestSlotTime()
        {
            QueryBuilderSelect query = new();
            query.Table = FbtAFMSSediTimeslot.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSSediTimeslot.COL_SLOT_TIME);
            query.OrderByDesc(FbtAFMSSediTimeslot.COL_SLOT_TIME);

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException(error);
            if (table.Rows.Count == 0 ||
                table.Rows[0][FbtAFMSSediTimeslot.COL_SLOT_TIME] == DBNull.Value)
                return null;

            return Convert.ToDateTime(
                table.Rows[0][FbtAFMSSediTimeslot.COL_SLOT_TIME]);
        }

        private static string InsertSlot(DateTime slotTime)
        {
            QueryBuilderInsert query = new();
            query.Table = FbtAFMSSediTimeslot.TABLE_NAME;
            query.AutoIncrement = FbtAFMSSediTimeslot.COL_ID;
            query.Value(
                FbtAFMSSediTimeslot.COL_MEASURE_DATE,
                slotTime.ToString("yyyyMMdd"));
            query.Value(
                FbtAFMSSediTimeslot.COL_MEASURE_TIME,
                slotTime.ToString("HHmmss"));
            query.Value(
                FbtAFMSSediTimeslot.COL_SLOT_TIME,
                slotTime,
                typeof(DateTime));

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.Execute(query, out string error);
            return error;
        }
    }
}
