using AFMSDll;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RnsLibrary;

namespace AFMSLoggerVideoHydorsem
{
    public class Program
    {
        public const string ProcessName = "AFMSLoggerVideoHydorsem";
        private const int DefaultMonitoringPort = 8004;
        private static readonly ILog Log = LogManager.GetLogger("SYS");

        public static async Task<int> Main(string[] args)
        {
            RnsLog.Init(Environment.UserInteractive, ProcessName, 100, 0);
            RnsLog.Start();
            RnsLog.AppenderInfo();
            RnsLog.ShowVersion();

            try
            {
                FBProvider.Instance.Initialize(FBProvider.SetFBConnStrBuilder());
                List<string> databaseLogs = FBProvider.Instance.CheckTables();
                Configuration.Instance.Setup();

                WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    Args = args,
                    ContentRootPath = AppContext.BaseDirectory
                });
                builder.WebHost.UseUrls($"http://0.0.0.0:{Configuration.Instance.WebPort}");
                builder.Services.AddWindowsService(options => options.ServiceName = "AFMS Logger Video Hydorsem");

                int monitoringPort = builder.Configuration.GetValue("MonitoringPort", DefaultMonitoringPort);
                builder.Services.AddSingleton<IRequestTaskQueue, RequestTaskQueue>();
                builder.Services.AddTcpLogging(options =>
                {
                    options.Port = monitoringPort;
                    options.ServiceName = ProcessName;
                });
                builder.Services.AddHostedService<RequestTaskWorker>();

                WebApplication app = builder.Build();
                new RestApiManager(app).Regist();

                await app.StartAsync();
                WriteStartupLogs(monitoringPort, databaseLogs);
                await app.WaitForShutdownAsync();
                Log.Info($"{ProcessName} 정상 종료");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal($"{ProcessName}에서 처리되지 않은 예외가 발생했습니다.", ex);
                return 1;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static void WriteStartupLogs(int monitoringPort, IEnumerable<string> databaseLogs)
        {
            Log.Info("===========================================================");
            Log.Info($"= {ProcessName}");
            Log.Info($"= 버전: {AFMSBuild.GetVersion()}");
            Log.Info($"= 빌드: {AFMSBuild.GetBuildDate()}");
            Log.Info($"= 영상 수신: {Configuration.Instance.WebPort}/{Configuration.Instance.WebPath}");
            Log.Info($"= 모니터링 포트: {monitoringPort}");
            Log.Info("===========================================================");
            foreach (string message in databaseLogs) Log.Info(message);
        }
    }
}
