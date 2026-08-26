using log4net;

namespace AFMSDll
{
    public static class AFMSLogBanner
    {
        public static void WriteStartup(string programName, string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(programName))
                throw new ArgumentException("프로그램명이 필요합니다.", nameof(programName));

            string title = string.IsNullOrWhiteSpace(displayName)
                ? programName.ToUpperInvariant()
                : displayName!;
            var processAssembly = BuildInfoHelper.ProcessAssembly;
            var afmsDllAssembly = BuildInfoHelper.AFMSDllAssembly;
            string runMode = Environment.UserInteractive ? "Console" : "Windows Service";

            string banner = $"""

                ==================================================================
                  {title}
                ==================================================================
                  PROCESS  {programName}
                           v{BuildInfoHelper.GetVersion(processAssembly)}  |  Build {BuildInfoHelper.GetBuildDate(processAssembly)}
                  AFMSDLL  v{BuildInfoHelper.GetVersion(afmsDllAssembly)}  |  Build {BuildInfoHelper.GetBuildDate(afmsDllAssembly)}
                ------------------------------------------------------------------
                  STARTED  {DateTime.Now:yyyy-MM-dd HH:mm:ss}  |  {runMode}
                ==================================================================
                """;

            LogManager.GetLogger(programName).Info(banner);
        }

    }
}
