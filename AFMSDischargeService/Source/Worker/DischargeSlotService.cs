using AFMSDll;
using System.Data;

namespace AFMSDischargeService
{
    internal sealed class DischargeSlotService(ILogger<DischargeSlotService> logger) : BackgroundService
    {
        private static readonly DateTime FirstSlotTime = new(2026, 7, 14, 0, 0, 0, DateTimeKind.Local);
        private static readonly TimeSpan SlotInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (string error in FBProvider.Instance.CheckDischargeTables())
                logger.LogError("유량 테이블 확인 오류: {Error}", error);

            CreateMissingSlots(stoppingToken);

            using PeriodicTimer timer = new(PollInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    CreateMissingSlots(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private void CreateMissingSlots(CancellationToken stoppingToken)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                DateTime lastPassedSlot = GetLastPassedSlot(DateTime.Now);
                if (lastPassedSlot < FirstSlotTime) return;

                DateTime? latestSlot = GetLatestSlotTime();
                DateTime nextSlot = latestSlot.HasValue
                    ? latestSlot.Value.Add(SlotInterval)
                    : FirstSlotTime;
                if (nextSlot < FirstSlotTime) nextSlot = FirstSlotTime;

                int createdCount = 0;
                while (nextSlot <= lastPassedSlot)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    string error = InsertSlot(nextSlot);
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "유량 슬롯 생성 중 오류가 발생했습니다.");
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

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (table.Rows.Count == 0 || table.Rows[0][FbtAFMSDischargeTimeslot.COL_SLOT_TIME] == DBNull.Value) return null;

            return Convert.ToDateTime(table.Rows[0][FbtAFMSDischargeTimeslot.COL_SLOT_TIME]);
        }

        private static string InsertSlot(DateTime slotTime)
        {
            QueryBuilderInsert query = new();
            query.Table = FbtAFMSDischargeTimeslot.TABLE_NAME;
            query.AutoIncrement = FbtAFMSDischargeTimeslot.COL_ID;
            query.Value(FbtAFMSDischargeTimeslot.COL_MEASURE_DATE, slotTime.ToString("yyyyMMdd"));
            query.Value(FbtAFMSDischargeTimeslot.COL_MEASURE_TIME, slotTime.ToString("HHmmss"));
            query.Value(FbtAFMSDischargeTimeslot.COL_SLOT_TIME, slotTime, typeof(DateTime));

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            db.Execute(query, out string error);
            return error;
        }
    }
}
