using AFMSDll;
using log4net;

namespace AFMSSediService
{
    public class Program
    {
        private const string PROCESS_NAME = "AFMSSSCService";
        private static readonly ILog Log = LogManager.GetLogger(PROCESS_NAME);

        public static void Main(string[] args)
        {
            AFMSLog.Initialize(Environment.UserInteractive, PROCESS_NAME);
            AFMSLogBanner.WriteStartup(PROCESS_NAME, "AFMS SSC SERVICE");

            string programPath = Environment.ProcessPath ?? throw ExInvalid.ProgramPathUnknown();

            ServiceInstallResult installResult = WindowsServiceManager.EnsureInstalled(programPath, PROCESS_NAME);
            Log.Info(installResult.Status);
            Log.Info(installResult.Message);

            FBProvider.Instance.Initialize(FBProvider.SetFBConnStrBuilder());

            InitializeDatabase();

            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new AFMSLogLoggerProvider());
            builder.Services.Configure<SSCServiceOptions>(builder.Configuration.GetSection(SSCServiceOptions.SectionName));
            builder.Services.AddHostedService<WorkerSlotProcess>();
            builder.Services.AddHostedService<WorkerSSCProcess>();

            var host = builder.Build();
            host.Run();
        }

        private static void InitializeDatabase()
        {
            foreach (string message in FBProvider.Instance.CheckTables(false))
            {
                Log.Info(message);
            }
        }

    }
}
