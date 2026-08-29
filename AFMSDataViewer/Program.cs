using Krypton.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AFMSDataViewer
{
    internal static class Program
    {
        // 프로그램이 실행되는 동안 유지되도록 정적 필드로 선언
        private static readonly KryptonManager _kryptonManager = new();
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            //_kryptonManager.GlobalPaletteMode = PaletteMode.Microsoft365BlackDarkMode;

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<MeasurementDataHub>();
                    services.AddSingleton<MeasurementRefreshService>();
                    services.AddHostedService(provider =>
                        provider.GetRequiredService<MeasurementRefreshService>());
                    services.AddSingleton<FormMain>();
                })
                .Build();

            // FormMain 생성자에서 Firebird 연결을 초기화한 후 백그라운드 갱신을 시작합니다.
            FormMain mainForm = host.Services.GetRequiredService<FormMain>();
            host.Start();
            try
            {
                Application.Run(mainForm);
            }
            finally
            {
                host.StopAsync().GetAwaiter().GetResult();
            }
        }
    }
}
