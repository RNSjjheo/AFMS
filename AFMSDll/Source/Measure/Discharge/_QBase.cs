using System;
using System.Data;
using System.Globalization;

namespace AFMSDll
{
    public abstract class _QBase
    {
        public int Id { get; set; } = -1;
        public int SlotId { get; private set; } = -1;
        public MeasurementDeviceType DeviceType { get; set; } = MeasurementDeviceType.None;
        public int DeviceId { get; set; } = -1;
        public int DischargeConfigId { get; set; } = -1;
        public DateOnly MeasureDate { get; set; }
        public TimeOnly MeasureTime { get; set; }
        public double Value { get; set; }
        public double Uncertainty { get; set; }
        public DischargeMethod Method { get; }
        public int MethodConfigId { get; set; } = -1;
        public CrossSection CrossSection { get; } = new();

        protected _QBase(DischargeMethod method)
        {
            Method = method;
        }

        /// <summary>
        /// 현재 산정법과 측정장비 조합에서 아직 유량 결과가 없는 가장 이른 슬롯을 불러옵니다.
        /// </summary>
        /// <returns>산정할 슬롯이 있으면 true, 없으면 false입니다.</returns>
        public bool TryLoadStartSlot(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            error = ValidateCalculationKey();
            if (!string.IsNullOrEmpty(error))
            {
                ClearStartSlot();
                return false;
            }

            string sql = $"SELECT FIRST 1 S.{FbtAFMSDischargeTimeslot.COL_ID},";
            sql += $" S.{FbtAFMSDischargeTimeslot.COL_MEASURE_DATE},";
            sql += $" S.{FbtAFMSDischargeTimeslot.COL_MEASURE_TIME}";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += " WHERE NOT EXISTS (";
            sql += $"SELECT 1 FROM {FbtAFMSDischargeResult.TABLE_NAME} R";
            sql += $" WHERE R.{FbtAFMSDischargeResult.COL_SLOT_ID} = S.{FbtAFMSDischargeTimeslot.COL_ID}";
            sql += $" AND R.{FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE} = '{DeviceType}'";
            sql += $" AND R.{FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID} = {DeviceId}";
            sql += $" AND R.{FbtAFMSDischargeResult.COL_DISCHARGE_METHOD} = '{Method}')";
            sql += $" ORDER BY S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME}";

            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error) || table.Rows.Count == 0)
            {
                ClearStartSlot();
                return false;
            }

            DataRow row = table.Rows[0];
            string measureDate = Convert.ToString(row[FbtAFMSDischargeTimeslot.COL_MEASURE_DATE]) ?? string.Empty;
            string measureTime = Convert.ToString(row[FbtAFMSDischargeTimeslot.COL_MEASURE_TIME]) ?? string.Empty;

            if (!DateOnly.TryParseExact(measureDate, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateOnly parsedDate) ||
                !TimeOnly.TryParseExact(measureTime, "HHmmss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out TimeOnly parsedTime))
            {
                ClearStartSlot();
                error = $"유량 슬롯의 측정시각 형식이 올바르지 않습니다: {measureDate} {measureTime}";
                return false;
            }

            SlotId = Convert.ToInt32(row[FbtAFMSDischargeTimeslot.COL_ID]);
            MeasureDate = parsedDate;
            MeasureTime = parsedTime;
            return true;
        }

        private string ValidateCalculationKey()
        {
            if (DeviceType == MeasurementDeviceType.None)
                return "측정장비 유형이 설정되지 않았습니다.";
            if (DeviceId < 0)
                return "측정장비 ID가 설정되지 않았습니다.";
            if (Method == DischargeMethod.None)
                return "유량 산정법이 설정되지 않았습니다.";

            return string.Empty;
        }

        private void ClearStartSlot()
        {
            SlotId = -1;
            MeasureDate = default;
            MeasureTime = default;
        }
    }
}
