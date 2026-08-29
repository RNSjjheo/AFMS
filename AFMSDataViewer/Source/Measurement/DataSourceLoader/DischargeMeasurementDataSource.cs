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
                new RealtimeDischargeChartQuery(from, to).Build(),
                "기간 유량 조회 실패");
            MeasurementBatch batch = new();

            foreach (DataRow row in table.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DateTime time = RRChartDataMapper.ParseSourceTime(row["SOURCE_TIME"]);
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
    }
}
