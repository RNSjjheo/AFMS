using AFMSDll;
using Microsoft.Extensions.Options;
using System;
using System.Data;

namespace AFMSSediService
{
    internal abstract class WorkerSSC : BackgroundService
    {
        protected abstract Task<int> ProcessBatchAsync(RSandProfileSnapshot profile, ChannelMasterSource source, SedFileWriter fileWriter, CancellationToken cancellationToken);
        protected readonly ILogger Logger;
        protected readonly SSCServiceOptions Options;

        protected WorkerSSC(ILogger logger, IOptions<SSCServiceOptions> options)
        {
            Logger = logger;
            Options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            RSandProfileSnapshot profile = LoadLatestProfile();
            ChannelMasterSource source = ChannelMasterSource.LoadFromRSetup();
            SedFileWriter fileWriter = new SedFileWriter(Options.DataDirectory);

            Logger.LogInformation(
                "SSC 프로파일을 메모리에 로드했습니다. " +
                "ProfileId={ProfileId}, " +
                "Device={DeviceType}({CellFrom}~{CellTo}), " +
                "Source={HeaderTable}/{CellTable}",
                profile.ProfileId,
                profile.Device.DeviceType,
                profile.Device.CellFrom,
                profile.Device.CellTo,
                source.HeaderTable,
                source.CellTable);
            Logger.LogInformation(
                "SSC 계산 시작시각={CalculationStartTime}, 배치크기={BatchSize}, Data폴더={DataDirectory}",
                Options.CalculationStartTime,
                Options.BatchSize,
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
            sql += $" {FbtAFMSSediSSCProfile.COL_PROFILE_ID},";
            sql += $" {FbtAFMSSediSSCProfile.COL_PROFILE_DATE},";
            sql += $" {FbtAFMSSediSSCProfile.COL_PROFILE_TIME},";
            sql += $" {FbtAFMSSediSSCProfile.COL_PROFILE_NAME},";
            sql += $" {FbtAFMSSediSSCProfile.COL_DEVICE_TYPE},";
            sql += $" {FbtAFMSSediSSCProfile.COL_CELL_FROM},";
            sql += $" {FbtAFMSSediSSCProfile.COL_CELL_TO},";
            sql += $" {FbtAFMSSediSSCProfile.COL_K_VALUE},";
            sql += $" {FbtAFMSSediSSCProfile.COL_BEAM_ANGLE},";
            sql += $" {FbtAFMSSediSSCProfile.COL_SSC_A},";
            sql += $" {FbtAFMSSediSSCProfile.COL_SSC_B}";
            sql += $" FROM {FbtAFMSSediSSCProfile.TABLE_NAME}";
            sql += $" ORDER BY {FbtAFMSSediSSCProfile.COL_PROFILE_ID} DESC";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException(
                    $"{FbtAFMSSediSSCProfile.TABLE_NAME} 조회에 실패했습니다.\n{error}");
            if (db.Results.Rows.Count == 0)
                throw new InvalidOperationException(
                    $"{FbtAFMSSediSSCProfile.TABLE_NAME}에 SSC 연산 프로파일이 없습니다.");

            return CreateSnapshot(db.Results.Rows[0]);
        }

        private static SSCProfileSnapshot CreateSnapshot(DataRow row)
        {
            return new SSCProfileSnapshot(
                GetInt32(row, FbtAFMSSediSSCProfile.COL_PROFILE_ID),
                GetString(row, FbtAFMSSediSSCProfile.COL_PROFILE_DATE),
                GetString(row, FbtAFMSSediSSCProfile.COL_PROFILE_TIME),
                GetString(row, FbtAFMSSediSSCProfile.COL_PROFILE_NAME),
                CreateDeviceProfile(row));
        }

        private static RSandDeviceProfile CreateDeviceProfile(DataRow row)
        {
            return new RSandDeviceProfile(
                GetString(row, FbtAFMSSediSSCProfile.COL_DEVICE_TYPE),
                GetInt32(row, FbtAFMSSediSSCProfile.COL_CELL_FROM),
                GetInt32(row, FbtAFMSSediSSCProfile.COL_CELL_TO),
                GetDouble(row, FbtAFMSSediSSCProfile.COL_K_VALUE),
                GetDouble(row, FbtAFMSSediSSCProfile.COL_BEAM_ANGLE),
                GetDouble(row, FbtAFMSSediSSCProfile.COL_SSC_A),
                GetDouble(row, FbtAFMSSediSSCProfile.COL_SSC_B));
        }

        private static string GetString(DataRow row, string columnName) =>
            Convert.ToString(row[columnName])?.Trim() ?? string.Empty;

        private static int GetInt32(DataRow row, string columnName) =>
            row[columnName] == DBNull.Value ? 0 : Convert.ToInt32(row[columnName]);

        private static double GetDouble(DataRow row, string columnName) =>
            row[columnName] == DBNull.Value ? 0.0 : Convert.ToDouble(row[columnName]);
    }
}
