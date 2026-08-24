using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AFMSDll
{
    public class AFMSBuild
    {
        public const string NAME = "AMFS";
        public static string GetVersion()
        {
            Assembly assembly = Assembly.GetEntryAssembly()
                ?? Assembly.GetExecutingAssembly();

            string version = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                ?? "Unknown";

            return version.Split('+')[0];
        }

        public static string GetBuildDate()
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            AssemblyMetadataAttribute? attribute = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(x => x.Key == "BuildDate");

            return attribute?.Value ?? "Unknown";
        }
    }
}
