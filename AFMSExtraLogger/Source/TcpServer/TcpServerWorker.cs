using log4net;

namespace AFMSExtraLogger
{
    public class TcpServerWorker : BackgroundService
    {
        private static readonly ILog Log = LogManager.GetLogger("TCP");
        private readonly TcpPacketServer _server;
        private readonly TcpMessageDispatcher _dispatcher;

        public TcpServerWorker(TcpPacketServer server, TcpMessageDispatcher dispatcher)
        {
            _server = server;
            _dispatcher = dispatcher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Info("TCP Server Worker 시작");

            _server.SetDispatcher(_dispatcher);

            Task serverTask = _server.RunAsync(stoppingToken);

            Task heartbeatTask = _dispatcher.MonitorHeartbeatAsync(stoppingToken);

            try
            {
                await Task.WhenAll(serverTask, heartbeatTask);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 정상적인 서비스 종료
            }
            catch (Exception ex)
            {
                Log.Error("TCP Server Worker 오류", ex);
                throw;
            }
            finally
            {
                Log.Info("TCP Server Worker 종료");
            }
        }
    }
}
