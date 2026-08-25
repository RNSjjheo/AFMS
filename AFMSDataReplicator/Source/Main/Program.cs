using AFMSDll;
using FirebirdSql.Data.Logging;
using log4net;
using Microsoft.VisualBasic.Logging;
using RnsLibrary;

namespace AFMSDataReplicator
{
    public class Program
    {
        private static readonly ILog Log = LogManager.GetLogger("SYS");
        private const string PROCESS_NAME = "AFMSDataReplicator";
        public static async Task<int> Main(string[] args)
        {
            RnsLog.Init(Environment.UserInteractive, PROCESS_NAME, 100, 0);
            RnsLog.Start();
            RnsLog.AppenderInfo();
            RnsLog.ShowVersion();

            string programPath = Environment.ProcessPath ?? throw new InvalidOperationException("현재 프로그램 경로를 확인할 수 없습니다.");

            ServiceInstallResult result = WindowsServiceManager.EnsureInstalled(programPath, PROCESS_NAME);

            Log.Info(result.Status);
            Log.Info(result.Message);

            FBProvider.Instance.ConnStrBuilder = FBProvider.SetFBConnStrBuilder();
            List<string> dbtablelog = FBProvider.Instance.CheckTables();

            Log.Info($"===========================================================");
            Log.Info($"= AFMS Extra Logger");
            Log.Info($"= 버전: {AFMSBuild.GetVersion()} ");
            Log.Info($"= 빌드: {AFMSBuild.GetBuildDate()} ");
            Log.Info($"===========================================================");

            foreach (string log in dbtablelog)
            {
                Log.Info(log);
            }

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(
                new HostApplicationBuilderSettings
                {
                    Args = args,
                    ContentRootPath = AppContext.BaseDirectory
                });

            builder.Configuration
                .AddJsonFile("AFMSDataReplicator.settings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(
                    $"AFMSDataReplicator.settings.{builder.Environment.EnvironmentName}.json",
                    optional: true,
                    reloadOnChange: true);

            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = PROCESS_NAME;
            });

            builder.Services.AddHostedService<ReplicationWorker>();

            IHost host = builder.Build();

            await host.RunAsync();

            return 0;
        }
    }
}
