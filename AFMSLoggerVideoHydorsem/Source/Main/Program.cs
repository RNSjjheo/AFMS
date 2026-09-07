using System.Net;
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
        private const string ProcessName = "AFMSLoggerVideoHydorsem";
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
                builder.WebHost.UseUrls($"http://0.0.0.0:{DiagnosticsOwner.Instance.WebPort}");
                builder.Services.AddWindowsService(options => options.ServiceName = "AFMS Logger Video Hydorsem");

                int monitoringPort = builder.Configuration.GetValue("MonitoringPort", DefaultMonitoringPort);
                builder.Services.AddSingleton(_ => new TcpPacketServer(IPAddress.Any, monitoringPort));
                builder.Services.AddSingleton<IRequestTaskQueue, RequestTaskQueue>();
                builder.Services.AddSingleton<TcpMessageDispatcher>();
                builder.Services.AddHostedService<RequestTaskWorker>();
                builder.Services.AddHostedService<TcpServerWorker>();
                builder.Services.AddHostedService<DiagnosticsWorker>();

                WebApplication app = builder.Build();
                new RestApiManager(app).Regist();

                WriteStartupLogs(monitoringPort, databaseLogs);
                await app.RunAsync();
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
            TcpBrocastBuffer.WriteLog("SYS", "===========================================================");
            TcpBrocastBuffer.WriteLog("SYS", $"= {ProcessName}");
            TcpBrocastBuffer.WriteLog("SYS", $"= 버전: {AFMSBuild.GetVersion()}");
            TcpBrocastBuffer.WriteLog("SYS", $"= 빌드: {AFMSBuild.GetBuildDate()}");
            TcpBrocastBuffer.WriteLog("SYS", $"= 영상 수신: {DiagnosticsOwner.Instance.WebPort}/{DiagnosticsOwner.Instance.WebPath}");
            TcpBrocastBuffer.WriteLog("SYS", $"= 모니터링 포트: {monitoringPort}");
            TcpBrocastBuffer.WriteLog("SYS", "===========================================================");
            foreach (string message in databaseLogs) TcpBrocastBuffer.WriteLog("SYS", message);
        }
    }
}
