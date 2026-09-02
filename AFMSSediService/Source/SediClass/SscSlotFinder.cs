using AFMSDll;
using System.Globalization;
using System.Data;

namespace AFMSSediService
{
    internal sealed class SscSlotFinder
    {
        private static readonly TimeSpan SlotLogInterval = TimeSpan.FromMinutes(10);

        private readonly ILogger<WorkerSSCProcess> logger;
        private int? lastLoggedSlotId;
        private string? lastLoggedSlotState;
        private DateTime? lastLoggedSlotAt;

        public SscCalculationSlot? SelectedSlot { get; private set; }

        public SscSlotFinder(ILogger<WorkerSSCProcess> logger)
        {
            this.logger = logger;
        }

        public void FindOldestUnprocessedSlot()
        {
            if (SelectedSlot != null)
            {
                TimeSpan diff = DateTime.Now - SelectedSlot.SlotTime;

                if (diff >= TimeSpan.Zero && diff < TimeSpan.FromMinutes(9))
                {
                    return;
                }
            }

            string sql = "SELECT FIRST 1";
            sql += $" T.{FbtAFMSSediTimeslot.COL_ID},";
            sql += $" T.{FbtAFMSSediTimeslot.COL_MEASURE_DATE},";
            sql += $" T.{FbtAFMSSediTimeslot.COL_MEASURE_TIME},";
            sql += $" T.{FbtAFMSSediTimeslot.COL_SLOT_TIME}";
            sql += $" FROM {FbtAFMSSediTimeslot.TABLE_NAME} T";
            sql += $" LEFT JOIN {FbtAFMSSSCResult.TABLE_NAME} R";
            sql += $" ON R.{FbtAFMSSSCResult.COL_SLOT_ID} = T.{FbtAFMSSediTimeslot.COL_ID}";
            sql += $" WHERE R.{FbtAFMSSSCResult.COL_ID} IS NULL";
            sql += $" ORDER BY T.{FbtAFMSSediTimeslot.COL_SLOT_TIME}";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql, out string error);

            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException($"미처리 SSC 슬롯 조회에 실패했습니다.\n{error}");
            if (db.Results.Rows.Count == 0)
            {
                SelectedSlot = null;
                ResetSlotLogState();
                return;
            }

            DataRow row = db.Results.Rows[0];
            SelectedSlot = new SscCalculationSlot(
                Convert.ToInt32(row[FbtAFMSSediTimeslot.COL_ID]),
                Convert.ToString(row[FbtAFMSSediTimeslot.COL_MEASURE_DATE])?.Trim() ?? string.Empty,
                Convert.ToString(row[FbtAFMSSediTimeslot.COL_MEASURE_TIME])?.Trim() ?? string.Empty,
                Convert.ToDateTime(row[FbtAFMSSediTimeslot.COL_SLOT_TIME]));
        }

        public void FindOldestVelocityMeasuredSlot(RSandProfileSnapshot profile)
        {
            SscCalculationSlot? startSlot = SelectedSlot;
            SelectedSlot = null;
            if (startSlot == null)
            {
                ResetSlotLogState();
                return;
            }

            List<string> velocityConditions = new List<string>();
            if (profile.A.IsEnabled)
                velocityConditions.Add($"COALESCE(P.{FbtRPOINT.COL_HYDROMETER1_FLAG}, 'N') = 'Y'");
            if (profile.B.IsEnabled)
                velocityConditions.Add($"COALESCE(P.{FbtRPOINT.COL_HYDROMETER2_FLAG}, 'N') = 'Y'");
            if (velocityConditions.Count == 0)
            {
                LogWatchedSlot(startSlot, "활성 SSC 장비 없음");
                return;
            }

            string startTime = startSlot.SlotTime.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture);

            string sql = "SELECT FIRST 1";
            sql += $" T.{FbtAFMSSediTimeslot.COL_ID},";
            sql += $" T.{FbtAFMSSediTimeslot.COL_MEASURE_DATE},";
            sql += $" T.{FbtAFMSSediTimeslot.COL_MEASURE_TIME},";
            sql += $" T.{FbtAFMSSediTimeslot.COL_SLOT_TIME}";
            sql += $" FROM {FbtAFMSSediTimeslot.TABLE_NAME} T";
            sql += $" INNER JOIN {FbtRPOINT.TABLE_NAME} P";
            sql += $" ON P.{FbtRPOINT.COL_MEASURE_DATE} = T.{FbtAFMSSediTimeslot.COL_MEASURE_DATE}";
            sql += $" AND P.{FbtRPOINT.COL_MEASURE_TIME} = T.{FbtAFMSSediTimeslot.COL_MEASURE_TIME}";
            sql += $" LEFT JOIN {FbtAFMSSSCResult.TABLE_NAME} R";
            sql += $" ON R.{FbtAFMSSSCResult.COL_SLOT_ID} = T.{FbtAFMSSediTimeslot.COL_ID}";
            sql += $" WHERE R.{FbtAFMSSSCResult.COL_ID} IS NULL";
            sql += $" AND T.{FbtAFMSSediTimeslot.COL_SLOT_TIME} >= '{startTime}'";
            sql += " AND " + string.Join(" AND ", velocityConditions);
            sql += $" ORDER BY T.{FbtAFMSSediTimeslot.COL_SLOT_TIME}";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"유속 측정 완료 슬롯 조회에 실패했습니다.\n{error}");
            if (db.Results.Rows.Count == 0)
            {
                LogWatchedSlot(startSlot, "유속 데이터 대기");
                return;
            }

            DataRow row = db.Results.Rows[0];
            SelectedSlot = new SscCalculationSlot(
                Convert.ToInt32(row[FbtAFMSSediTimeslot.COL_ID]),
                Convert.ToString(row[FbtAFMSSediTimeslot.COL_MEASURE_DATE])?.Trim() ?? string.Empty,
                Convert.ToString(row[FbtAFMSSediTimeslot.COL_MEASURE_TIME])?.Trim() ?? string.Empty,
                Convert.ToDateTime(row[FbtAFMSSediTimeslot.COL_SLOT_TIME]));

            LogWatchedSlot(SelectedSlot, "SSC 처리 대상");
        }

        private void LogWatchedSlot(SscCalculationSlot slot, string slotState)
        {
            DateTime now = DateTime.Now;
            bool slotChanged = lastLoggedSlotId != slot.Id || lastLoggedSlotState != slotState;
            bool logIntervalElapsed = !lastLoggedSlotAt.HasValue ||
                now - lastLoggedSlotAt.Value >= SlotLogInterval;

            if (!slotChanged && !logIntervalElapsed) return;

            logger.LogInformation(
                "현재 바라보는 SSC 슬롯입니다. 상태={SlotState}, SlotId={SlotId}, SlotTime={SlotTime:yyyy-MM-dd HH:mm:ss}, 측정={MeasureDate} {MeasureTime}",
                slotState,
                slot.Id,
                slot.SlotTime,
                slot.MeasureDate,
                slot.MeasureTime);

            lastLoggedSlotId = slot.Id;
            lastLoggedSlotState = slotState;
            lastLoggedSlotAt = now;
        }

        private void ResetSlotLogState()
        {
            lastLoggedSlotId = null;
            lastLoggedSlotState = null;
            lastLoggedSlotAt = null;
        }
    }
}
