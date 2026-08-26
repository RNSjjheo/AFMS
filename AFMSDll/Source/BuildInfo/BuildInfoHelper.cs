using System;
using System.Diagnostics;
using System.Reflection;

namespace AFMSDll
{
    public static class BuildInfoHelper
    {
        public static Assembly ProcessAssembly => Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("실행 프로세스의 어셈블리 정보를 확인할 수 없습니다.");

        public static Assembly AFMSDllAssembly => typeof(BuildInfoHelper).Assembly;

        public static DateTime ProcessStartedAt
        {
            get
            {
                using Process process = Process.GetCurrentProcess();
                return process.StartTime;
            }
        }

        public static string GetVersion(Assembly assembly)
        {
            string version = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "Unknown";

            return version.Split('+')[0];
        }

        public static string GetBuildDate(Assembly assembly)
        {
            return assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "BuildDate")?
                .Value
                ?? "Unknown";
        }
    }
}
