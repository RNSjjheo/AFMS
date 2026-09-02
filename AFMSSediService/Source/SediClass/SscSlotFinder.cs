using AFMSDll;
using System.Data;

namespace AFMSSediService
{
    internal sealed class SscSlotFinder
    {
        private readonly ILogger<WorkerSSCProcess> logger;
        private int? lastLoggedSlotId;
        private int? startSlotId;

        public SscSlotFinder(ILogger<WorkerSSCProcess> logger)
        {
            this.logger = logger;
        }

        public void InitializeStart(ChannelMasterSource source)
        {
            SscCalculationSlot? slot = QueryNext(source, null);
            if (slot == null) return;

            startSlotId = slot.Id;
            logger.LogInformation(
                "SSC 계산 시작 슬롯을 확정했습니다. SlotId={SlotId}, SlotTime={SlotTime:yyyy-MM-dd HH:mm:ss}, Source={Source}",
                slot.Id,
                slot.SlotTime,
                source.HeaderTable);
        }

        public SscCalculationSlot? FindNext(ChannelMasterSource source)
        {
            if (!startSlotId.HasValue) InitializeStart(source);
            SscCalculationSlot? slot = startSlotId.HasValue ? QueryNext(source, startSlotId.Value) : null;
            if (slot != null && lastLoggedSlotId != slot.Id)
            {
                logger.LogInformation(
                    "SSC 처리 대상 슬롯을 확인했습니다. SlotId={SlotId}, SlotTime={SlotTime:yyyy-MM-dd HH:mm:ss}, Source={Source}",
                    slot.Id,
                    slot.SlotTime,
                    source.HeaderTable);
                lastLoggedSlotId = slot.Id;
            }

            return slot;
        }

        private SscCalculationSlot? QueryNext(ChannelMasterSource source, int? minimumSlotId)
        {
            string sql = "SELECT FIRST 1";
            sql += $" T.{FbtAFMSSediSSCTimeslot.COL_ID},";
            sql += $" T.{FbtAFMSSediSSCTimeslot.COL_MEASURE_DATE},";
            sql += $" T.{FbtAFMSSediSSCTimeslot.COL_MEASURE_TIME},";
            sql += $" T.{FbtAFMSSediSSCTimeslot.COL_SLOT_TIME}";
            sql += $" FROM {FbtAFMSSediSSCTimeslot.TABLE_NAME} T";
            sql += $" INNER JOIN {FbtRPOINT.TABLE_NAME} P";
            sql += $" ON P.{FbtRPOINT.COL_MEASURE_DATE} = T.{FbtAFMSSediSSCTimeslot.COL_MEASURE_DATE}";
            sql += $" AND P.{FbtRPOINT.COL_MEASURE_TIME} = T.{FbtAFMSSediSSCTimeslot.COL_MEASURE_TIME}";
            sql += $" LEFT JOIN {FbtAFMSSediSSCResult.TABLE_NAME} R";
            sql += $" ON R.{FbtAFMSSediSSCResult.COL_SLOT_ID} = T.{FbtAFMSSediSSCTimeslot.COL_ID}";
            sql += $" WHERE R.{FbtAFMSSediSSCResult.COL_ID} IS NULL";
            sql += $" AND COALESCE(P.{source.ReadyFlagColumn}, 'N') = 'Y'";
            if (minimumSlotId.HasValue)
                sql += $" AND T.{FbtAFMSSediSSCTimeslot.COL_ID} >= {minimumSlotId.Value}";
            sql += $" ORDER BY T.{FbtAFMSSediSSCTimeslot.COL_SLOT_TIME}";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"SSC 처리 대상 슬롯 조회에 실패했습니다.\n{error}");
            if (db.Results.Rows.Count == 0) return null;

            DataRow row = db.Results.Rows[0];
            SscCalculationSlot slot = new(
                Convert.ToInt32(row[FbtAFMSSediSSCTimeslot.COL_ID]),
                Convert.ToString(row[FbtAFMSSediSSCTimeslot.COL_MEASURE_DATE])?.Trim() ?? string.Empty,
                Convert.ToString(row[FbtAFMSSediSSCTimeslot.COL_MEASURE_TIME])?.Trim() ?? string.Empty,
                Convert.ToDateTime(row[FbtAFMSSediSSCTimeslot.COL_SLOT_TIME]));

            return slot;
        }
    }
}
