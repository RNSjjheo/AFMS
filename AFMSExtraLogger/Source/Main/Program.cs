using AFMSDll;
using AFMSExtraLogger.Source.Singleton;
using AFMSExtraLogger.Source.TcpServer;
using FirebirdSql.Data.FirebirdClient;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RnsLibrary;
using System.Net;
using System.Reflection;

namespace AFMSExtraLogger
{
    public class Program
    {
        private static readonly ILog Log = LogManager.GetLogger("SYS");
        private static RestApiManager _RestApi;
        public static async Task<int> Main(string[] args)
        {
            RnsLog.Init(Environment.UserInteractive, "AFMSExtraLogger", 100, 0);
            RnsLog.Start();
            RnsLog.AppenderInfo();
            RnsLog.ShowVersion();

            RegisterGlobalExceptionHandlers();

            FBProvider.Instance.Initialize(FBProvider.SetFBConnStrBuilder());
            List<string> dbtablelog = FBProvider.Instance.CheckTables();

            Configuration.Instance.Setup();

            try
            {
                var builder = WebApplication.CreateBuilder(
                    new WebApplicationOptions
                    {
                        Args = args,
                        ContentRootPath = AppContext.BaseDirectory
                    });

                builder.Configuration
                    .AddJsonFile("AFMSExtraLogger.settings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile(
                        $"AFMSExtraLogger.settings.{builder.Environment.EnvironmentName}.json",
                        optional: true,
                        reloadOnChange: true);

                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "AFMS Extra Logger";
                });

                builder.Services.AddSingleton(serviceProvider => new TcpPacketServer(IPAddress.Any, 8003));
                builder.Services.AddSingleton<IRequestTaskQueue, RequestTaskQueue>();
                builder.Services.AddSingleton<TcpMessageDispatcher>();
                builder.Services.AddHostedService<RequestTaskWorker>();
                builder.Services.AddHostedService<TcpServerWorker>();
                builder.Services.AddHostedService<DiagnosticsWorker>();

                if (DiagnosticsOwner.Instance.MPDSPort != "")
                {
                    builder.Services.AddHostedService<RFSerialServer>();
                }

                var app = builder.Build();

                _RestApi = new RestApiManager(builder, app);
                _RestApi.SetTcpServer(app.Services.GetRequiredService<TcpPacketServer>());
                _RestApi.Regist();

                TcpBrocastBuffer.WriteLog("SYS", $"===========================================================");
                TcpBrocastBuffer.WriteLog("SYS", $"= AFMS Extra Logger");
                TcpBrocastBuffer.WriteLog("SYS", $"= 버전: {AFMSBuild.GetVersion()} ");
                TcpBrocastBuffer.WriteLog("SYS", $"= 빌드: {AFMSBuild.GetBuildDate()} ");
                TcpBrocastBuffer.WriteLog("SYS", $"===========================================================");

                foreach (string log in dbtablelog)
                {
                    TcpBrocastBuffer.WriteLog("SYS", log);
                }

                app.Run();

                Log.Info("AFMSExtraLogger 정상 종료");

                return 0;
            }
            catch (Exception ex)
            {
                // 프로그램 시작, Host 실행 등에서 빠져나온 최상위 예외
                WriteFatalLog("Program.Main에서 처리되지 않은 예외가 발생했습니다.", ex);

                // 비정상 종료 코드
                return 1;
            }
            finally
            {
                try
                {
                    // 남아 있는 로그를 기록하고 Appender를 종료합니다.
                    LogManager.Shutdown();
                }
                catch
                {
                    // 종료 처리 중 추가 예외는 무시
                }
            }
        }

        private static void RegisterGlobalExceptionHandlers()
        {
            /*
             * 일반 스레드에서 처리되지 않은 예외
             *
             * 이 이벤트가 실행된 뒤 프로세스가 종료될 수 있으므로
             * 처리 코드를 최대한 단순하게 유지해야 합니다.
             */
            AppDomain.CurrentDomain.UnhandledException +=
                (_, eventArgs) =>
                {
                    try
                    {
                        if (eventArgs.ExceptionObject is Exception ex)
                        {
                            WriteFatalLog(
                                $"AppDomain 처리되지 않은 예외 발생. " +
                                $"IsTerminating={eventArgs.IsTerminating}",
                                ex);
                        }
                        else
                        {
                            Log.Fatal(
                                $"AppDomain 처리되지 않은 객체가 발생했습니다. " +
                                $"IsTerminating={eventArgs.IsTerminating}, " +
                                $"ExceptionObject={eventArgs.ExceptionObject}");
                        }
                    }
                    catch
                    {
                        // 전역 예외 처리기에서 다시 예외를 발생시키지 않음
                    }
                    finally
                    {
                        if (eventArgs.IsTerminating)
                        {
                            try
                            {
                                LogManager.Shutdown();
                            }
                            catch
                            {
                            }
                        }
                    }
                };

            /*
             * await되지 않았거나 결과를 확인하지 않은 Task의 예외
             *
             * 이 이벤트는 예외 발생 즉시 실행된다는 보장이 없으므로
             * 보조적인 진단 용도로만 사용합니다.
             */
            TaskScheduler.UnobservedTaskException +=
                (_, eventArgs) =>
                {
                    try
                    {
                        Log.Error(
                            "관찰되지 않은 Task 예외가 발생했습니다.",
                            eventArgs.Exception.Flatten());

                        eventArgs.SetObserved();
                    }
                    catch
                    {
                    }
                };
        }

        private static void WriteFatalLog(
            string message,
            Exception exception)
        {
            try
            {
                Log.Fatal(message, exception);
            }
            catch
            {
                // 로그 시스템 자체에 문제가 발생한 경우를 대비
                WriteEmergencyLog(message, exception);
            }
        }

        private static void WriteEmergencyLog(
            string message,
            Exception exception)
        {
            try
            {
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "AFMSExtraLogger",
                    "Log");

                Directory.CreateDirectory(logDirectory);

                string filePath = Path.Combine(
                    logDirectory,
                    "FatalError.log");

                string text =
                    $"""
                    ==================================================
                    Time       : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}
                    Process ID : {Environment.ProcessId}
                    Message    : {message}
                    Exception  : {exception}
                    ==================================================

                    """;

                File.AppendAllText(filePath, text);
            }
            catch
            {
                // 여기서도 실패하면 더 이상 처리할 방법이 없음
            }
        }
    


    }
}
