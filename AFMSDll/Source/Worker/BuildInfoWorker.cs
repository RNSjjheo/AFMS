using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AFMSDll
{
    public sealed class BuildInfoWorker(ILogger<BuildInfoWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            WriteBuildInfo();

            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;
                DateTime nextHour = new DateTime(
                    now.Year,
                    now.Month,
                    now.Day,
                    now.Hour,
                    0,
                    0,
                    now.Kind).AddHours(1);

                try
                {
                    await Task.Delay(nextHour - now, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                WriteBuildInfo();
            }
        }

        private void WriteBuildInfo()
        {
            DateTime startedAt = BuildInfoHelper.ProcessStartedAt;
            TimeSpan uptime = DateTime.Now - startedAt;

            logger.LogInformation(
                "프로세스 v{ProcessVersion} | 빌드 {ProcessBuildDate} | 실행 {StartedAt:yyyy-MM-dd HH:mm:ss} | 가동 {UptimeDays}일 {Uptime:hh\\:mm\\:ss} , AFMSDll v{DllVersion} | DLL 빌드 {DllBuildDate}",
                BuildInfoHelper.GetVersion(BuildInfoHelper.ProcessAssembly),
                BuildInfoHelper.GetBuildDate(BuildInfoHelper.ProcessAssembly),
                startedAt,
                (int)uptime.TotalDays,
                uptime,
                BuildInfoHelper.GetVersion(BuildInfoHelper.AFMSDllAssembly),
                BuildInfoHelper.GetBuildDate(BuildInfoHelper.AFMSDllAssembly));
        }
    }
}
