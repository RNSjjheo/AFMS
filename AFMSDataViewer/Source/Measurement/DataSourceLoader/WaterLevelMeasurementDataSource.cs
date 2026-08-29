using System.Data;
using System.Globalization;
using AFMSDll;

namespace AFMSDataViewer
{
    /// <summary>수위 DB 자료를 Hub 전달용 LevelMeasurement로 변환합니다.</summary>
    internal sealed class WaterLevelMeasurementDataSource : IMeasurementDataSource
    {
        public string Name => "수위";

        public Task<MeasurementBatch> LoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = $"SELECT {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}, AVG({FbtWATERLEVEL.COL_AVG_WATER_LEVEL})";
            sql += $" FROM {FbtWATERLEVEL.TABLE_NAME} WHERE {MeasurementDataSourceReader.BuildTimeCondition(from, to)}";
            sql += $" GROUP BY {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}";

            using FBDatabase database = FBProvider.Instance.CreateDatabase();
            DataTable table = MeasurementDataSourceReader.Execute(database, sql, "기간 수위 조회 실패");
            MeasurementBatch batch = new();
            List<LevelMeasurement> measurements = new();

            foreach (DataRow row in table.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (row[2] == DBNull.Value || !MeasurementDataSourceReader.TryReadTime(row, out DateTime time))
                    continue;

                double value = Convert.ToDouble(row[2], CultureInfo.InvariantCulture);
                bool isValid = double.IsFinite(value);
                measurements.Add(new LevelMeasurement(time, "WaterLevelGauge", isValid ? value : 0D, isValid));
            }

            IReadOnlyDictionary<DateTime, LevelMeasurement> measurementsBySlot = measurements
                .GroupBy(measurement => MeasurementDataHub.AlignToSlot(measurement.Time))
                .ToDictionary(group => group.Key, group => group.MaxBy(measurement => measurement.Time)!);

            foreach (DateTime slotTime in MeasurementDataSourceReader.EnumerateSlots(from, to))
            {
                batch.Levels.Add(measurementsBySlot.TryGetValue(slotTime, out LevelMeasurement? measurement)
                    ? measurement
                    : new LevelMeasurement(slotTime, "WaterLevelGauge", 0D, false));
            }

            return Task.FromResult(batch);
        }
    }
}
