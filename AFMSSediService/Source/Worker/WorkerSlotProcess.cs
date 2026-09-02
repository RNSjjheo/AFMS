using AFMSDll;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static System.Windows.Forms.Design.AxImporter;

namespace AFMSSediService
{
    internal class WorkerSlotProcess : WorkerSlot
    {
        private static readonly TimeSpan SlotInterval = TimeSpan.FromMinutes(10);


        public WorkerSlotProcess(ILogger<WorkerSlotProcess> logger, IHostApplicationLifetime lifetime, IOptions<SSCServiceOptions> options) : base(logger, lifetime, options)
        {
        }

        protected override void CreateMissingSlots(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            DateTime lastPassedSlot = GetLastPassedSlot(DateTime.Now);
            DateTime calculationStartTime = Options.Value.CalculationStartTime;
            if (lastPassedSlot < calculationStartTime) return;

            DateTime? latestSlot = GetLatestSlotTime();
            DateTime nextSlot = latestSlot.HasValue ? latestSlot.Value.Add(SlotInterval): calculationStartTime;
            if (nextSlot < calculationStartTime) nextSlot = calculationStartTime;

            int createdCount = 0;
            while (nextSlot <= lastPassedSlot)
            {
                stoppingToken.ThrowIfCancellationRequested();

                string error = InsertSlot(nextSlot);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException($"{nextSlot:yyyy-MM-dd HH:mm:ss} SEDI 슬롯 생성 실패: {error}");
                }

                Logger.LogInformation("SEDI 슬롯을 생성했습니다. 슬롯={SlotTime:yyyy-MM-dd HH:mm:ss}", nextSlot);
                createdCount++;
                nextSlot = nextSlot.Add(SlotInterval);
            }

            if (createdCount > 0)
            {
                Logger.LogInformation("SEDI 슬롯 {Count}개를 생성했습니다. 마지막 슬롯: {LastSlot}", createdCount, lastPassedSlot);
            }
        }

        protected static DateTime GetLastPassedSlot(DateTime now)
        {
            DateTime boundary = new(now.Year, now.Month, now.Day, now.Hour, now.Minute / 10 * 10, 0, now.Kind);

            return now > boundary ? boundary : boundary.Subtract(SlotInterval);
        }

        protected static DateTime? GetLatestSlotTime()
        {
            QueryBuilderSelect query = new();
            query.Table = FbtAFMSSediTimeslot.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSSediTimeslot.COL_SLOT_TIME);
            query.OrderByDesc(FbtAFMSSediTimeslot.COL_SLOT_TIME);

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (table.Rows.Count == 0 || table.Rows[0][FbtAFMSSediTimeslot.COL_SLOT_TIME] == DBNull.Value) return null;

            return Convert.ToDateTime(table.Rows[0][FbtAFMSSediTimeslot.COL_SLOT_TIME]);
        }

        protected static string InsertSlot(DateTime slotTime)
        {
            QueryBuilderInsert query = new();
            query.Table = FbtAFMSSediTimeslot.TABLE_NAME;
            query.AutoIncrement = FbtAFMSSediTimeslot.COL_ID;
            query.Value(FbtAFMSSediTimeslot.COL_MEASURE_DATE, slotTime.ToString("yyyyMMdd"));
            query.Value(FbtAFMSSediTimeslot.COL_MEASURE_TIME, slotTime.ToString("HHmmss"));
            query.Value(FbtAFMSSediTimeslot.COL_SLOT_TIME, slotTime, typeof(DateTime));
            query.Value(
                FbtAFMSSediTimeslot.COL_SEND_STATUS,
                SediTransmissionStatus.NOT_SEND.ToString());

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.Execute(query, out string error);

            return error;
        }
    }
}
