using System.Data;
using System.Globalization;
using AFMSDll;

namespace AFMSDataViewer
{
    /// <summary>전력 DB 자료를 Hub 전달용 VoltageMeasurement로 변환합니다.</summary>
    internal sealed class PowerMeasurementDataSource : IMeasurementDataSource
    {
        public string Name => "전력";

        public Task<MeasurementBatch> LoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = $"SELECT {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}, AVG({FbtVTHLOGGER.COL_DCCHARGE}),";
            sql += $" AVG({FbtVTHLOGGER.COL_DCBATTERY}) FROM {FbtVTHLOGGER.TABLE_NAME}";
            sql += $" WHERE {MeasurementDataSourceReader.BuildTimeCondition(from, to)}";
            sql += $" GROUP BY {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}";

            using FBDatabase database = FBProvider.Instance.CreateDatabase();
            DataTable table = MeasurementDataSourceReader.Execute(database, sql, "기간 전력 조회 실패");
            MeasurementBatch batch = new();
            List<VoltageMeasurement> measurements = new();

            foreach (DataRow row in table.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!MeasurementDataSourceReader.TryReadTime(row, out DateTime time)) continue;

                double? rawInputVoltage = row[2] == DBNull.Value
                    ? null
                    : Convert.ToDouble(row[2], CultureInfo.InvariantCulture);
                double? rawOutputVoltage = row[3] == DBNull.Value
                    ? null
                    : Convert.ToDouble(row[3], CultureInfo.InvariantCulture);
                bool isInputValid = rawInputVoltage.HasValue && double.IsFinite(rawInputVoltage.Value);
                bool isOutputValid = rawOutputVoltage.HasValue && double.IsFinite(rawOutputVoltage.Value);

                measurements.Add(new VoltageMeasurement(
                    time,
                    "PowerDevice",
                    isInputValid ? rawInputVoltage : 0D,
                    isOutputValid ? rawOutputVoltage : 0D,
                    isInputValid,
                    isOutputValid));
            }

            IReadOnlyDictionary<DateTime, VoltageMeasurement> measurementsBySlot = measurements
                .GroupBy(measurement => MeasurementDataHub.AlignToSlot(measurement.Time))
                .ToDictionary(group => group.Key, group => group.MaxBy(measurement => measurement.Time)!);

            foreach (DateTime slotTime in MeasurementDataSourceReader.EnumerateSlots(from, to))
            {
                batch.Voltages.Add(measurementsBySlot.TryGetValue(slotTime, out VoltageMeasurement? measurement)
                    ? measurement
                    : new VoltageMeasurement(slotTime, "PowerDevice", 0D, 0D, false, false));
            }

            return Task.FromResult(batch);
        }
    }
}
