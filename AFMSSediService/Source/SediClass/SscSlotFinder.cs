using AFMSDll;
using System.Data;

namespace AFMSSediService
{
    internal sealed class SscSlotFinder
    {
        public SscCalculationSlot? FindOldestUnprocessedSlot()
        {
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
            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"미처리 SSC 슬롯 조회에 실패했습니다.\n{error}");
            if (db.Results.Rows.Count == 0) return null;

            DataRow row = db.Results.Rows[0];
            return new SscCalculationSlot(
                Convert.ToInt32(row[FbtAFMSSediTimeslot.COL_ID]),
                Convert.ToString(row[FbtAFMSSediTimeslot.COL_MEASURE_DATE])?.Trim() ?? string.Empty,
                Convert.ToString(row[FbtAFMSSediTimeslot.COL_MEASURE_TIME])?.Trim() ?? string.Empty,
                Convert.ToDateTime(row[FbtAFMSSediTimeslot.COL_SLOT_TIME]));
        }
    }
}
