using AFMSDll;
using Microsoft.Extensions.Options;
using System;
using System.Configuration;
using System.Data;
using System.Globalization;

namespace AFMSSediService
{
    internal abstract class WorkerSSC : BackgroundService
    {
        protected abstract Task<int> ProcessBatchAsync(SSCProfileSnapshot profile, ChannelMasterSource source, SedFileWriter fileWriter, CancellationToken cancellationToken);
        protected readonly ILogger Logger;
        protected readonly SSCServiceOptions Options;

        protected WorkerSSC(ILogger logger, IOptions<SSCServiceOptions> options)
        {
            Logger = logger;
            Options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            SSCProfileSnapshot profile = LoadLatestProfile();
            ChannelMasterSource source = ChannelMasterSource.FromProfile(profile.Device);
            SedFileWriter fileWriter = new SedFileWriter(Options.DataDirectory);

            Logger.LogInformation("SSC 프로파일을 메모리에 로드했습니다. ProfileId={ProfileId}, Device={DeviceType}, " +
                "HydroTable={HydroTableName}({CellFrom}~{CellTo}), Source={HeaderTable}/{CellTable}", profile.ProfileId, profile.Device.DeviceType,
                profile.Device.HydroTableName, profile.Device.CellFrom, profile.Device.CellTo, source.HeaderTable, source.CellTable);
            Logger.LogInformation("SSC 계산 시작시각={CalculationStartTime}, 배치크기={BatchSize}, Data폴더={DataDirectory}", Options.CalculationStartTime, Options.BatchSize,
                Options.DataDirectory);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    int processed = await ProcessBatchAsync(profile, source, fileWriter, stoppingToken);
                    if (processed > 0)
                        Logger.LogInformation("SSC 자료 {ProcessedCount}건을 처리했습니다.", processed);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "SSC 처리 대상 조회 중 오류가 발생했습니다.");
                }

                await Task.Delay(Options.PollInterval, stoppingToken);
            }
        }



        private static SSCProfileSnapshot LoadLatestProfile()
        {
            string sql = "SELECT FIRST 1";
            sql += $" {FbtAFMSSediSSCProfile.COL_ID},";
            sql += $" {FbtAFMSSediSSCProfile.COL_MEASURE_DATE},";
            sql += $" {FbtAFMSSediSSCProfile.COL_MEASURE_TIME},";
            sql += $" {FbtAFMSSediSSCProfile.COL_DEVICE_TYPE},";
            sql += $" {FbtAFMSSediSSCProfile.COL_HYDRO_TABLE_NAME},";
            sql += $" {FbtAFMSSediSSCProfile.COL_CELL_FROM},";
            sql += $" {FbtAFMSSediSSCProfile.COL_CELL_TO},";
            sql += $" {FbtAFMSSediSSCProfile.COL_K_VALUE},";
            sql += $" {FbtAFMSSediSSCProfile.COL_BEAM_ANGLE},";
            sql += $" {FbtAFMSSediSSCProfile.COL_SSC_A},";
            sql += $" {FbtAFMSSediSSCProfile.COL_SSC_B}";
            sql += $" FROM {FbtAFMSSediSSCProfile.TABLE_NAME}";
            sql += $" ORDER BY {FbtAFMSSediSSCProfile.COL_ID} DESC";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql, out string error);

            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"{FbtAFMSSediSSCProfile.TABLE_NAME} 조회에 실패했습니다.\n{error}");
            if (db.Results.Rows.Count == 0)
            {
                InsertDefaultProfile(db);
                db.RunQuery(sql, out error);

                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException($"기본 SSC 연산 프로파일 저장 후 조회에 실패했습니다.\n{error}");
                if (db.Results.Rows.Count == 0)
                    throw new InvalidOperationException($"{FbtAFMSSediSSCProfile.TABLE_NAME} 기본 프로파일을 조회할 수 없습니다.");
            }

            return CreateSnapshot(db.Results.Rows[0]);
        }

        private static void InsertDefaultProfile(FBDatabase db)
        {
            DateTime now = DateTime.Now;
            string profileDate = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string profileTime = now.ToString("HHmmss", CultureInfo.InvariantCulture);

            QueryBuilderInsert query = new QueryBuilderInsert();
            query.Table = FbtAFMSSediSSCProfile.TABLE_NAME;
            query.AutoIncrement = FbtAFMSSediSSCProfile.COL_ID;
            query.Value(FbtAFMSSediSSCProfile.COL_MEASURE_DATE, profileDate);
            query.Value(FbtAFMSSediSSCProfile.COL_MEASURE_TIME, profileTime);
            query.Value(FbtAFMSSediSSCProfile.COL_DEVICE_TYPE, "CM600");
            query.Value(FbtAFMSSediSSCProfile.COL_HYDRO_TABLE_NAME, HydroMetherTableType.RHYDROMETER1.ToString());
            query.Value(FbtAFMSSediSSCProfile.COL_CELL_FROM, 1);
            query.Value(FbtAFMSSediSSCProfile.COL_CELL_TO, 10);
            query.Value(FbtAFMSSediSSCProfile.COL_K_VALUE, 0.25);
            query.Value(FbtAFMSSediSSCProfile.COL_BEAM_ANGLE, 25.0);
            query.Value(FbtAFMSSediSSCProfile.COL_SSC_A, 0.1);
            query.Value(FbtAFMSSediSSCProfile.COL_SSC_B, 0.1);

            db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"{FbtAFMSSediSSCProfile.TABLE_NAME} 기본 프로파일 저장에 실패했습니다.\n{error}");
        }

        private static SSCProfileSnapshot CreateSnapshot(DataRow row)
        {
            return new SSCProfileSnapshot(
                GetInt32(row, FbtAFMSSediSSCProfile.COL_ID),
                GetString(row, FbtAFMSSediSSCProfile.COL_MEASURE_DATE),
                GetString(row, FbtAFMSSediSSCProfile.COL_MEASURE_TIME),
                CreateDeviceProfile(row));
        }

        private static SSCDeviceProfile CreateDeviceProfile(DataRow row)
        {
            return new SSCDeviceProfile(
                GetString(row, FbtAFMSSediSSCProfile.COL_DEVICE_TYPE),
                GetHydroTableName(row),
                GetInt32(row, FbtAFMSSediSSCProfile.COL_CELL_FROM),
                GetInt32(row, FbtAFMSSediSSCProfile.COL_CELL_TO),
                GetDouble(row, FbtAFMSSediSSCProfile.COL_K_VALUE),
                GetDouble(row, FbtAFMSSediSSCProfile.COL_BEAM_ANGLE),
                GetDouble(row, FbtAFMSSediSSCProfile.COL_SSC_A),
                GetDouble(row, FbtAFMSSediSSCProfile.COL_SSC_B));
        }

        private static HydroMetherTableType GetHydroTableName(DataRow row)
        {
            string value = GetString(row, FbtAFMSSediSSCProfile.COL_HYDRO_TABLE_NAME);
            string[] allowedValues = Enum.GetNames<HydroMetherTableType>();
            if (allowedValues.Contains(value, StringComparer.Ordinal))
            {
                return Enum.Parse<HydroMetherTableType>(value);
            }

            throw new InvalidOperationException($"{FbtAFMSSediSSCProfile.TABLE_NAME}.{FbtAFMSSediSSCProfile.COL_HYDRO_TABLE_NAME} 값이 올바르지 않습니다. " +
                $"현재 값='{value}', 허용 값={string.Join(", ", allowedValues)}");
        }

        private static string GetString(DataRow row, string columnName) =>
            Convert.ToString(row[columnName])?.Trim() ?? string.Empty;

        private static int GetInt32(DataRow row, string columnName) =>
            row[columnName] == DBNull.Value ? 0 : Convert.ToInt32(row[columnName]);

        private static double GetDouble(DataRow row, string columnName) =>
            row[columnName] == DBNull.Value ? 0.0 : Convert.ToDouble(row[columnName]);
    }
}
