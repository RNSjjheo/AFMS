using AFMSDll;
using log4net;
using RnsLibrary;

namespace AFMSDischargeService
{
    public class Program
    {
        private const string PROCESS_NAME = "AFMSDischargeService";
        private static readonly ILog Log = LogManager.GetLogger("SYS");

        public static async Task<int> Main(string[] args)
        {
            RnsLog.Init(Environment.UserInteractive, PROCESS_NAME, 100, 0);
            RnsLog.Start();
            RnsLog.AppenderInfo();
            RnsLog.ShowVersion();

            string programPath = Environment.ProcessPath?? throw new InvalidOperationException("현재 프로그램 경로를 확인할 수 없습니다.");
            ServiceInstallResult installResult = WindowsServiceManager.EnsureInstalled(programPath, PROCESS_NAME);
            Log.Info(installResult.Status);
            Log.Info(installResult.Message);

            FBProvider.Instance.ConnStrBuilder = FBProvider.SetFBConnStrBuilder();

            var builder = Host.CreateApplicationBuilder(
                new HostApplicationBuilderSettings
                {
                    Args = args,
                    ContentRootPath = AppContext.BaseDirectory
                });

            builder.Configuration
                .AddJsonFile("AFMSDischargeService.settings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(
                    $"AFMSDischargeService.settings.{builder.Environment.EnvironmentName}.json",
                    optional: true,
                    reloadOnChange: true);

            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = PROCESS_NAME;
            });

            // 초기 슬롯 준비가 끝난 뒤 다음 HostedService가 시작되도록 가장 먼저 등록합니다.
            builder.Services.AddHostedService<DischargeSlotService>();

            var host = builder.Build();
            await host.RunAsync();
            return 0;
        }
    }
}
