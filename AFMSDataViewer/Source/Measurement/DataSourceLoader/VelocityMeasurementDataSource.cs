using System.Data;
using System.Globalization;
using AFMSDll;

namespace AFMSDataViewer
{
    /// <summary>MPDS·영상식 유속 자료를 장비 및 측선별 Hub 측정값으로 변환합니다.</summary>
    internal sealed class VelocityMeasurementDataSource : IMeasurementDataSource
    {
        private sealed record DeviceConfiguration(
            int Id,
            int Number,
            int TransectCount,
            string SourceTable,
            string MeterType);

        private sealed record TransectValue(double Velocity, double Uncertainty, bool IsValid);

        public string Name => "유속";

        public Task<MeasurementBatch> LoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using FBDatabase database = FBProvider.Instance.CreateDatabase();
            IReadOnlyList<DeviceConfiguration> configurations = LoadConfigurations(database, cancellationToken);
            IReadOnlyDictionary<DateTime, IReadOnlyDictionary<int, TransectValue>> mpdsBySlot =
                configurations.Any(IsMpds)
                    ? LoadMpds(database, from, to, cancellationToken)
                    : new Dictionary<DateTime, IReadOnlyDictionary<int, TransectValue>>();
            IReadOnlyDictionary<DateTime, IReadOnlyDictionary<int, TransectValue>> videoBySlot =
                configurations.Any(IsVideo)
                    ? LoadVideo(database, from, to, cancellationToken)
                    : new Dictionary<DateTime, IReadOnlyDictionary<int, TransectValue>>();

            MeasurementBatch batch = new();
            foreach (DeviceConfiguration configuration in configurations)
            {
                IReadOnlyDictionary<DateTime, IReadOnlyDictionary<int, TransectValue>> source =
                    IsVideo(configuration) ? videoBySlot : mpdsBySlot;
                foreach (DateTime slotTime in MeasurementDataSourceReader.EnumerateSlots(from, to))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    source.TryGetValue(slotTime, out IReadOnlyDictionary<int, TransectValue>? values);
                    List<VelocityTransectMeasurement> transects = new(configuration.TransectCount);
                    for (int number = 1; number <= configuration.TransectCount; number++)
                    {
                        TransectValue? value = null;
                        bool found = values != null && values.TryGetValue(number, out value);
                        transects.Add(new VelocityTransectMeasurement(
                            number,
                            found && value!.IsValid ? value.Velocity : 0D,
                            found && value!.IsValid ? value.Uncertainty : 0D,
                            found && value!.IsValid));
                    }

                    string deviceKey = configuration.Id.ToString(CultureInfo.InvariantCulture);
                    VelocityMeasurement measurement = IsVideo(configuration)
                        ? new VideoVelocityMeasurement(slotTime, deviceKey, transects,
                            configuration.Id, configuration.Number, configuration.MeterType)
                        : new MpdsVelocityMeasurement(slotTime, deviceKey, transects,
                            configuration.Id, configuration.Number, configuration.MeterType);
                    batch.Velocities.Add(measurement);
                }
            }

            return Task.FromResult(batch);
        }

        private static IReadOnlyList<DeviceConfiguration> LoadConfigurations(
            FBDatabase database,
            CancellationToken cancellationToken)
        {
            string sql = $"SELECT {_FBTableBase.COL_ID}, {FbtAFMSHydroMeter.COL_DEVICE_NO},";
            sql += $" {FbtAFMSHydroMeter.COL_TRANSECT_CNT}, TRIM({FbtAFMSHydroMeter.COL_DATA_TABLE}),";
            sql += $" TRIM({FbtAFMSHydroMeter.COL_DEVICE_NAME}) FROM {FbtAFMSHydroMeter.TABLE_NAME}";
            sql += $" WHERE TRIM({FbtAFMSHydroMeter.COL_DATA_TABLE}) IN";
            sql += $" ('{FbtHYDROMETERMPDS.TABLE_NAME}', '{FbtHYDROMETERVIDEO.TABLE_NAME}')";
            sql += $" ORDER BY {FbtAFMSHydroMeter.COL_DEVICE_NO}, {_FBTableBase.COL_ID}";

            DataTable table = MeasurementDataSourceReader.Execute(database, sql, "유속계 설정 조회 실패");
            List<DeviceConfiguration> result = new(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int id = Convert.ToInt32(row[0], CultureInfo.InvariantCulture);
                int number = row[1] == DBNull.Value ? id : Convert.ToInt32(row[1], CultureInfo.InvariantCulture);
                int transectCount = row[2] == DBNull.Value ? 1 : Math.Max(1, Convert.ToInt32(row[2], CultureInfo.InvariantCulture));
                string sourceTable = Convert.ToString(row[3], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
                string meterType = Convert.ToString(row[4], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
                result.Add(new DeviceConfiguration(id, number, transectCount, sourceTable, meterType));
            }
            return result;
        }

        private static IReadOnlyDictionary<DateTime, IReadOnlyDictionary<int, TransectValue>> LoadMpds(
            FBDatabase database,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken)
        {
            string sql = $"SELECT M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME},";
            sql += $" C.{FbtHYDROMETERMPDSCELL.COL_DEV_NO}, AVG(C.{FbtHYDROMETERMPDSCELL.COL_VELOCITY}),";
            sql += $" AVG(C.{FbtHYDROMETERMPDSCELL.COL_VSTDUNCERT})";
            sql += $" FROM {FbtHYDROMETERMPDS.TABLE_NAME} M JOIN {FbtHYDROMETERMPDSCELL.TABLE_NAME} C";
            sql += $" ON C.{FbtHYDROMETERMPDSCELL.COL_MPDS_ID} = M.{_FBTableBase.COL_ID}";
            sql += $" WHERE {MeasurementDataSourceReader.BuildTimeCondition(from, to, "M")}";
            sql += $" GROUP BY M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}, C.{FbtHYDROMETERMPDSCELL.COL_DEV_NO}";
            return LoadTransects(database, sql, "기간 MPDS 유속 조회 실패", cancellationToken);
        }

        private static IReadOnlyDictionary<DateTime, IReadOnlyDictionary<int, TransectValue>> LoadVideo(
            FBDatabase database,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken)
        {
            string sql = $"SELECT M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME},";
            sql += $" C.{FbtHYDROMETERVIDEOCELL.COL_CELL_NO}, AVG(C.{FbtHYDROMETERVIDEOCELL.COL_VELOCITY}),";
            sql += $" AVG(C.{FbtHYDROMETERVIDEOCELL.COL_UNCERTAINTY})";
            sql += $" FROM {FbtHYDROMETERVIDEO.TABLE_NAME} M JOIN {FbtHYDROMETERVIDEOCELL.TABLE_NAME} C";
            sql += $" ON C.{FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID} = M.{_FBTableBase.COL_ID}";
            sql += $" WHERE {MeasurementDataSourceReader.BuildTimeCondition(from, to, "M")}";
            sql += $" GROUP BY M.{_FBTableBase.COL_MEASURE_DATE}, M.{_FBTableBase.COL_MEASURE_TIME}, C.{FbtHYDROMETERVIDEOCELL.COL_CELL_NO}";
            return LoadTransects(database, sql, "기간 영상식 유속 조회 실패", cancellationToken);
        }

        private static IReadOnlyDictionary<DateTime, IReadOnlyDictionary<int, TransectValue>> LoadTransects(
            FBDatabase database,
            string sql,
            string errorContext,
            CancellationToken cancellationToken)
        {
            DataTable table = MeasurementDataSourceReader.Execute(database, sql, errorContext);
            Dictionary<DateTime, Dictionary<int, TransectValue>> byTime = new();
            foreach (DataRow row in table.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!MeasurementDataSourceReader.TryReadTime(row, out DateTime time) || row[2] == DBNull.Value)
                    continue;

                int transectNo = Convert.ToInt32(row[2], CultureInfo.InvariantCulture);
                double velocity = row[3] == DBNull.Value ? 0D : Convert.ToDouble(row[3], CultureInfo.InvariantCulture);
                double uncertainty = row[4] == DBNull.Value ? 0D : Convert.ToDouble(row[4], CultureInfo.InvariantCulture);
                bool isValid = row[3] != DBNull.Value && double.IsFinite(velocity) && double.IsFinite(uncertainty);
                if (!byTime.TryGetValue(time, out Dictionary<int, TransectValue>? transects))
                {
                    transects = new Dictionary<int, TransectValue>();
                    byTime.Add(time, transects);
                }
                transects[transectNo] = new TransectValue(
                    isValid ? velocity : 0D,
                    isValid ? uncertainty : 0D,
                    isValid);
            }

            return byTime
                .GroupBy(item => MeasurementDataHub.AlignToSlot(item.Key))
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyDictionary<int, TransectValue>)group.MaxBy(item => item.Key)!.Value);
        }

        private static bool IsMpds(DeviceConfiguration configuration) =>
            string.Equals(configuration.SourceTable, FbtHYDROMETERMPDS.TABLE_NAME, StringComparison.OrdinalIgnoreCase);

        private static bool IsVideo(DeviceConfiguration configuration) =>
            string.Equals(configuration.SourceTable, FbtHYDROMETERVIDEO.TABLE_NAME, StringComparison.OrdinalIgnoreCase);
    }
}
