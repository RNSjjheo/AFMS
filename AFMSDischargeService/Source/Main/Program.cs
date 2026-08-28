using AFMSDll;
using log4net;

namespace AFMSDischargeService
{
    public class Program
    {
        private const string PROCESS_NAME = "AFMSDischargeService";
        private static readonly ILog Log = LogManager.GetLogger(PROCESS_NAME);

        public static async Task<int> Main(string[] args)
        {
            AFMSLog.Initialize(Environment.UserInteractive, PROCESS_NAME);
            AFMSLogBanner.WriteStartup(PROCESS_NAME, "AFMS DISCHARGE SERVICE");

            string programPath = Environment.ProcessPath?? throw new InvalidOperationException("현재 프로그램 경로를 확인할 수 없습니다.");
            ServiceInstallResult installResult = WindowsServiceManager.EnsureInstalled(programPath, PROCESS_NAME);
            Log.Info(installResult.Status);
            Log.Info(installResult.Message);

            FBProvider.Instance.Initialize(FBProvider.SetFBConnStrBuilder());

            var builder = Host.CreateApplicationBuilder(
                new HostApplicationBuilderSettings
                {
                    Args = args,
                    ContentRootPath = AppContext.BaseDirectory
                });

            builder.Configuration
                .AddJsonFile("AFMSDischargeService.settings.json", optional: false, reloadOnChange: false)
                .AddJsonFile(
                    $"AFMSDischargeService.settings.{builder.Environment.EnvironmentName}.json",
                    optional: true,
                    reloadOnChange: false);

            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = PROCESS_NAME;
            });

            builder.Services.Configure<DischargeServiceOptions>(
                builder.Configuration.GetSection(DischargeServiceOptions.SectionName));

            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new AFMSLogLoggerProvider());

            // 초기 슬롯 준비가 끝난 뒤 다음 HostedService가 시작되도록 가장 먼저 등록합니다.
            builder.Services.AddHostedService<DischargeSlotService>();
            builder.Services.AddHostedService<DischargeCalculationWorker>();
            builder.Services.AddHostedService<BuildInfoWorker>();

            var host = builder.Build();
            try
            {
                await host.RunAsync();
                return 0;
            }
            finally
            {
                AFMSLog.Shutdown();
            }
        }
    }
}
