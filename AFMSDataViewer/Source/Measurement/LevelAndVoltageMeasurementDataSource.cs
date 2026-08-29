using System.Data;
using System.Globalization;
using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class LevelAndVoltageMeasurementDataSource : IMeasurementDataSource
    {
        public string Name => "수위 및 전압";

        public Task<MeasurementBatch> LoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MeasurementBatch batch = new();
            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            LoadLevels(db, from, to, batch, cancellationToken);
            LoadVoltages(db, from, to, batch, cancellationToken);
            return Task.FromResult(batch);
        }

        private static void LoadLevels(FBDatabase db, DateTime from, DateTime to, MeasurementBatch batch, CancellationToken cancellationToken)
        {
            string sql = $"SELECT {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}, AVG({FbtWATERLEVEL.COL_AVG_WATER_LEVEL})";
            sql += $" FROM {FbtWATERLEVEL.TABLE_NAME} WHERE {BuildTimeCondition(from, to)}";
            sql += $" GROUP BY {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}";

            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException($"기간 수위 조회 실패: {error}");

            foreach (DataRow row in table.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (row[2] == DBNull.Value || !TryReadTime(row, out DateTime time)) continue;
                batch.Levels.Add(new LevelMeasurement(time, "WaterLevelGauge", Convert.ToDouble(row[2], CultureInfo.InvariantCulture)));
            }
        }

        private static void LoadVoltages(FBDatabase db, DateTime from, DateTime to, MeasurementBatch batch, CancellationToken cancellationToken)
        {
            string sql = $"SELECT {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}, AVG({FbtVTHLOGGER.COL_DCCHARGE}),";
            sql += $" AVG({FbtVTHLOGGER.COL_DCBATTERY}) FROM {FbtVTHLOGGER.TABLE_NAME} WHERE {BuildTimeCondition(from, to)}";
            sql += $" GROUP BY {_FBTableBase.COL_MEASURE_DATE}, {_FBTableBase.COL_MEASURE_TIME}";

            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException($"기간 전압 조회 실패: {error}");

            foreach (DataRow row in table.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryReadTime(row, out DateTime time)) continue;

                double? inputVoltage = row[2] == DBNull.Value ? null : Convert.ToDouble(row[2], CultureInfo.InvariantCulture);
                double? outputVoltage = row[3] == DBNull.Value ? null : Convert.ToDouble(row[3], CultureInfo.InvariantCulture);
                if (!inputVoltage.HasValue && !outputVoltage.HasValue) continue;
                batch.Voltages.Add(new VoltageMeasurement(time, "VoltageMeter", inputVoltage, outputVoltage));
            }
        }

        private static string BuildTimeCondition(DateTime from, DateTime to)
        {
            string fromValue = from.ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
            string toValue = to.ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
            string measuredAt = $"({_FBTableBase.COL_MEASURE_DATE} || ' ' || {_FBTableBase.COL_MEASURE_TIME})";
            return $"{measuredAt} >= '{fromValue}' AND {measuredAt} <= '{toValue}'";
        }

        private static bool TryReadTime(DataRow row, out DateTime time)
        {
            string value = $"{Convert.ToString(row[0], CultureInfo.InvariantCulture)} {Convert.ToString(row[1], CultureInfo.InvariantCulture)}";
            return DateTime.TryParseExact(value, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
        }
    }
}
