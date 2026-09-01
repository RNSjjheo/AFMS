using AFMSDll;
using Microsoft.Extensions.Options;
using System;
using System.Data;

namespace AFMSSediService
{
    internal abstract class WorkerSSC : BackgroundService
    {
        protected abstract Task<int> ProcessBatchAsync(RSandProfileSnapshot profile, SedFileWriter fileWriter, CancellationToken cancellationToken);
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
            SedFileWriter fileWriter = new SedFileWriter(Options.DataDirectory);

            Logger.LogInformation(
                "SSC 프로파일을 메모리에 로드했습니다. " +
                "ProfileId={ProfileId}, " +
                "A={ADeviceType}({ACellFrom}~{ACellTo}), " +
                "B={BDeviceType}({BCellFrom}~{BCellTo})",
                profile.ProfileId,
                profile.A.DeviceType,
                profile.A.CellFrom,
                profile.A.CellTo,
                profile.B.DeviceType,
                profile.B.CellFrom,
                profile.B.CellTo);
            Logger.LogInformation(
                "SSC 계산 시작시각={CalculationStartTime}, 배치크기={BatchSize}, Data폴더={DataDirectory}",
                Options.CalculationStartTime,
                Options.BatchSize,
                Options.DataDirectory);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    int processed = await ProcessBatchAsync(profile, fileWriter, stoppingToken);
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



        private static RSandProfileSnapshot LoadLatestProfile()
        {
            string sql = "SELECT FIRST 1";
            sql += $" {FbtRSANDPROFILE.COL_PROFILE_ID},";
            sql += $" {FbtRSANDPROFILE.COL_PROFILE_DATE},";
            sql += $" {FbtRSANDPROFILE.COL_PROFILE_TIME},";
            sql += $" {FbtRSANDPROFILE.COL_PROFILE_NAME},";
            sql += $" {FbtRSANDPROFILE.COL_A_SETUP_FLAG},";
            sql += $" {FbtRSANDPROFILE.COL_A_DEVICE_TYPE},";
            sql += $" {FbtRSANDPROFILE.COL_A_VALID_CELL_TYPE},";
            sql += $" {FbtRSANDPROFILE.COL_A_CELL_FROM},";
            sql += $" {FbtRSANDPROFILE.COL_A_CELL_TO},";
            sql += $" {FbtRSANDPROFILE.COL_A_DB_FROM},";
            sql += $" {FbtRSANDPROFILE.COL_A_DB_TO},";
            sql += $" {FbtRSANDPROFILE.COL_A_REGRESSION},";
            sql += $" {FbtRSANDPROFILE.COL_A_K_VALUE},";
            sql += $" {FbtRSANDPROFILE.COL_A_BEAM_ANGLE},";
            sql += $" {FbtRSANDPROFILE.COL_A_SSC_A},";
            sql += $" {FbtRSANDPROFILE.COL_A_SSC_B},";
            sql += $" {FbtRSANDPROFILE.COL_B_SETUP_FLAG},";
            sql += $" {FbtRSANDPROFILE.COL_B_DEVICE_TYPE},";
            sql += $" {FbtRSANDPROFILE.COL_B_VALID_CELL_TYPE},";
            sql += $" {FbtRSANDPROFILE.COL_B_CELL_FROM},";
            sql += $" {FbtRSANDPROFILE.COL_B_CELL_TO},";
            sql += $" {FbtRSANDPROFILE.COL_B_DB_FROM},";
            sql += $" {FbtRSANDPROFILE.COL_B_DB_TO},";
            sql += $" {FbtRSANDPROFILE.COL_B_REGRESSION},";
            sql += $" {FbtRSANDPROFILE.COL_B_K_VALUE},";
            sql += $" {FbtRSANDPROFILE.COL_B_BEAM_ANGLE},";
            sql += $" {FbtRSANDPROFILE.COL_B_SSC_A},";
            sql += $" {FbtRSANDPROFILE.COL_B_SSC_B}";
            sql += $" FROM {FbtRSANDPROFILE.TABLE_NAME}";
            sql += $" ORDER BY {FbtRSANDPROFILE.COL_PROFILE_ID} DESC";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException(
                    $"{FbtRSANDPROFILE.TABLE_NAME} 조회에 실패했습니다.\n{error}");
            if (db.Results.Rows.Count == 0)
                throw new InvalidOperationException(
                    $"{FbtRSANDPROFILE.TABLE_NAME}에 SSC 연산 프로파일이 없습니다.");

            return CreateSnapshot(db.Results.Rows[0]);
        }

        private static RSandProfileSnapshot CreateSnapshot(DataRow row)
        {
            RSandDeviceProfile profileA = CreateDeviceProfile(row, "A");
            RSandDeviceProfile profileB = CreateDeviceProfile(row, "B");

            return new RSandProfileSnapshot(
                GetInt32(row, FbtRSANDPROFILE.COL_PROFILE_ID),
                GetString(row, FbtRSANDPROFILE.COL_PROFILE_DATE),
                GetString(row, FbtRSANDPROFILE.COL_PROFILE_TIME),
                GetString(row, FbtRSANDPROFILE.COL_PROFILE_NAME),
                profileA,
                profileB);
        }

        private static RSandDeviceProfile CreateDeviceProfile(DataRow row, string prefix)
        {
            bool isA = prefix == "A";
            return new RSandDeviceProfile(
                GetString(row, isA ? FbtRSANDPROFILE.COL_A_SETUP_FLAG : FbtRSANDPROFILE.COL_B_SETUP_FLAG),
                GetString(row, isA ? FbtRSANDPROFILE.COL_A_DEVICE_TYPE : FbtRSANDPROFILE.COL_B_DEVICE_TYPE),
                GetString(row, isA ? FbtRSANDPROFILE.COL_A_VALID_CELL_TYPE : FbtRSANDPROFILE.COL_B_VALID_CELL_TYPE),
                GetInt32(row, isA ? FbtRSANDPROFILE.COL_A_CELL_FROM : FbtRSANDPROFILE.COL_B_CELL_FROM),
                GetInt32(row, isA ? FbtRSANDPROFILE.COL_A_CELL_TO : FbtRSANDPROFILE.COL_B_CELL_TO),
                GetDouble(row, isA ? FbtRSANDPROFILE.COL_A_DB_FROM : FbtRSANDPROFILE.COL_B_DB_FROM),
                GetDouble(row, isA ? FbtRSANDPROFILE.COL_A_DB_TO : FbtRSANDPROFILE.COL_B_DB_TO),
                GetDouble(row, isA ? FbtRSANDPROFILE.COL_A_REGRESSION : FbtRSANDPROFILE.COL_B_REGRESSION),
                GetDouble(row, isA ? FbtRSANDPROFILE.COL_A_K_VALUE : FbtRSANDPROFILE.COL_B_K_VALUE),
                GetDouble(row, isA ? FbtRSANDPROFILE.COL_A_BEAM_ANGLE : FbtRSANDPROFILE.COL_B_BEAM_ANGLE),
                GetDouble(row, isA ? FbtRSANDPROFILE.COL_A_SSC_A : FbtRSANDPROFILE.COL_B_SSC_A),
                GetDouble(row, isA ? FbtRSANDPROFILE.COL_A_SSC_B : FbtRSANDPROFILE.COL_B_SSC_B));
        }

        private static string GetString(DataRow row, string columnName) =>
            Convert.ToString(row[columnName])?.Trim() ?? string.Empty;

        private static int GetInt32(DataRow row, string columnName) =>
            row[columnName] == DBNull.Value ? 0 : Convert.ToInt32(row[columnName]);

        private static double GetDouble(DataRow row, string columnName) =>
            row[columnName] == DBNull.Value ? 0.0 : Convert.ToDouble(row[columnName]);
    }
}
