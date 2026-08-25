using RnsLibrary;

namespace AFMSDischargeService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            RnsLog.Init(Environment.UserInteractive, "AFMSDischargeService", 100, 0);
            RnsLog.Start();
            RnsLog.AppenderInfo();
            RnsLog.ShowVersion();

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
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}
