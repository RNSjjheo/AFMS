using System.Data;
using System.Globalization;
using AFMSDll;

namespace AFMSDataViewer
{
    /// <summary>유량 산정 결과를 Hub 전달용 DischargeMeasurement로 변환합니다.</summary>
    internal sealed class DischargeMeasurementDataSource : IMeasurementDataSource
    {
        public string Name => "유량";

        public Task<MeasurementBatch> LoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using FBDatabase database = FBProvider.Instance.CreateDatabase();
            DataTable table = MeasurementDataSourceReader.Execute(
                database,
                BuildQuery(from, to),
                "기간 유량 조회 실패");
            MeasurementBatch batch = new();

            foreach (DataRow row in table.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DateTime time = MeasurementDataSourceReader.ParseSourceTime(row["SOURCE_TIME"]);
                string deviceType = Convert.ToString(row["DEVICE_TYPE"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
                string method = Convert.ToString(row["DISCHARGE_METHOD"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(deviceType) || string.IsNullOrWhiteSpace(method) || row["DEVICE_ID"] == DBNull.Value)
                    continue;

                int deviceId = Convert.ToInt32(row["DEVICE_ID"], CultureInfo.InvariantCulture);
                string? meterType = row.Table.Columns.Contains("METER_TYPE") && row["METER_TYPE"] != DBNull.Value
                    ? Convert.ToString(row["METER_TYPE"], CultureInfo.InvariantCulture)?.Trim()
                    : null;
                bool isValid = row["CHART_VALUE"] != DBNull.Value;
                double value = isValid
                    ? Convert.ToDouble(row["CHART_VALUE"], CultureInfo.InvariantCulture)
                    : 0D;
                isValid &= double.IsFinite(value);

                batch.Discharges.Add(new DischargeMeasurement(
                    time, deviceType, deviceId, method, isValid ? value : 0D, meterType, isValid));
            }

            return Task.FromResult(batch);
        }

        private static string BuildQuery(DateTime from, DateTime to)
        {
            string type = FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE;
            string deviceId = FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID;
            string method = FbtAFMSDischargeResult.COL_DISCHARGE_METHOD;
            string sourceTime = FbtAFMSDischargeResult.COL_SOURCE_TIME;
            string configId = FbtAFMSDischargeMethodConfig.COL_ID;
            string configType = FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE;
            string configDeviceId = FbtAFMSDischargeMethodConfig.COL_DEVICE_ID;
            string configMethod = FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD;
            string sql = $"SELECT {SlotTimeValue()} AS SOURCE_TIME,";
            sql += $" TRIM(D.DISCHARGE_METHOD) || ' ' || TRIM(D.DEVICE_TYPE) || ' ' || CAST(D.DEVICE_ID AS VARCHAR(12)) AS SERIES,";
            sql += $" R.CHART_VALUE, D.DEVICE_TYPE, D.DEVICE_ID, D.DISCHARGE_METHOD, TRIM(H.{FbtAFMSHydroMeter.COL_DEVICE_NAME}) AS METER_TYPE";
            sql += $" FROM {FbtAFMSDischargeTimeslot.TABLE_NAME} S";
            sql += $" CROSS JOIN (SELECT TRIM(C.{configType}) AS DEVICE_TYPE,";
            sql += $" C.{configDeviceId} AS DEVICE_ID, TRIM(C.{configMethod}) AS DISCHARGE_METHOD";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C";
            sql += $" WHERE C.{configId} = (SELECT MAX(C2.{configId})";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C2";
            sql += $" WHERE C2.{configType} = C.{configType}";
            sql += $" AND C2.{configDeviceId} = C.{configDeviceId}";
            sql += $" AND C2.{configMethod} = C.{configMethod})";
            sql += $" AND C.{FbtAFMSDischargeMethodConfig.COL_ENABLED} = 1) D";
            sql += $" LEFT JOIN {FbtAFMSHydroMeter.TABLE_NAME} H ON D.DEVICE_TYPE = '{nameof(MeasurementDeviceType.VelocityMeter)}'";
            sql += $" AND H.{FbtAFMSHydroMeter.COL_ID} = D.DEVICE_ID";
            sql += $" LEFT JOIN (SELECT {sourceTime} AS SLOT_TIME, TRIM({type}) AS DEVICE_TYPE, {deviceId} AS DEVICE_ID,";
            sql += $" TRIM({method}) AS DISCHARGE_METHOD, AVG({FbtAFMSDischargeResult.COL_DISCHARGE}) AS CHART_VALUE";
            sql += $" FROM {FbtAFMSDischargeResult.TABLE_NAME} WHERE {sourceTime} >= '{from:yyyy-MM-dd HH:mm:ss}'";
            sql += $" AND {sourceTime} <= '{to:yyyy-MM-dd HH:mm:ss}'";
            sql += $" GROUP BY {sourceTime}, {type}, {deviceId}, {method}) R";
            sql += " ON R.SLOT_TIME = S.SLOT_TIME AND R.DEVICE_TYPE = D.DEVICE_TYPE AND R.DEVICE_ID = D.DEVICE_ID";
            sql += " AND R.DISCHARGE_METHOD = D.DISCHARGE_METHOD";
            sql += $" WHERE {SlotTimeCondition(from, to)} ORDER BY S.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} DESC";
            return sql;
        }

        private static string SlotTimeCondition(DateTime from, DateTime to, string alias = "S") =>
            $"{alias}.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} >= '{from:yyyy-MM-dd HH:mm:ss}' AND " +
            $"{alias}.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} <= '{to:yyyy-MM-dd HH:mm:ss}'";

        private static string SlotTimeValue(string alias = "S") =>
            $"CAST({alias}.{FbtAFMSDischargeTimeslot.COL_SLOT_TIME} AS TIMESTAMP)";
    }
}
