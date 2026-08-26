using log4net;
using System.Reflection;

namespace AFMSDll
{
    public static class RnsLogBanner
    {
        private static readonly ILog Log = LogManager.GetLogger("SYS");

        public static void WriteStartup(string programName, string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(programName))
                throw new ArgumentException("프로그램명이 필요합니다.", nameof(programName));

            string title = string.IsNullOrWhiteSpace(displayName)
                ? programName.ToUpperInvariant()
                : displayName!;
            Assembly processAssembly = Assembly.GetEntryAssembly()
                ?? throw new InvalidOperationException("실행 프로세스의 어셈블리 정보를 확인할 수 없습니다.");
            Assembly afmsDllAssembly = typeof(RnsLogBanner).Assembly;
            string runMode = Environment.UserInteractive ? "Console" : "Windows Service";

            string banner = $"""

                ==================================================================
                  {title}
                ==================================================================
                  PROCESS  {programName}
                           v{GetVersion(processAssembly)}  |  Build {GetBuildDate(processAssembly)}
                  AFMSDLL  v{GetVersion(afmsDllAssembly)}  |  Build {GetBuildDate(afmsDllAssembly)}
                ------------------------------------------------------------------
                  STARTED  {DateTime.Now:yyyy-MM-dd HH:mm:ss}  |  {runMode}
                ==================================================================
                """;

            Log.Info(banner);
        }

        private static string GetVersion(Assembly assembly)
        {
            string version = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "Unknown";

            return version.Split('+')[0];
        }

        private static string GetBuildDate(Assembly assembly)
        {
            return assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "BuildDate")?
                .Value
                ?? "Unknown";
        }
    }
}
