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

            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}
