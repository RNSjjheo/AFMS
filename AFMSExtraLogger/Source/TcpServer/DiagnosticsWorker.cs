using AFMSDll;
using log4net;
using Newtonsoft.Json.Linq;
using RnsLibrary;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AFMSExtraLogger.Source.TcpServer
{
    public class DiagnosticsWorker : BackgroundService
    {
        private DateTime PreTime;
        private static readonly ILog Log = LogManager.GetLogger("DIAG");
        private TcpPacketServer _TcpServer;

        public DiagnosticsWorker(TcpPacketServer tcpserver)
        {
            _TcpServer = tcpserver ?? throw new ArgumentNullException(nameof(tcpserver));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PreTime = DateTime.Now;
            TimeSpan diff = DateTime.Now - PreTime;

            await Task.Delay(5000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _PacketBase packet = TcpBrocastBuffer.GetDequeue();

                    if (packet != null)
                    {
                        await _TcpServer.BroadcastJsonAsync(packet);
                    }
                    else
                    {
                        await Task.Delay(100, stoppingToken);
                    }

                    diff = DateTime.Now - PreTime;

                    if (diff.TotalMilliseconds < 30000) continue;
                    PreTime = DateTime.Now;

                    DiagnosticsOwner.Instance.UpdateCurrentProcessMemoryMB();                    
                }
                catch { }
            }
        }
    }
}
