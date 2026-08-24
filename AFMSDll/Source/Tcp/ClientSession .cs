using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace AFMSDll
{
    public class ClientSession
    {


        public ClientInfo Info { get; }

        public TcpClient Client { get; }

        public NetworkStream Stream { get; }

        public SemaphoreSlim SendLock { get; }
        public string? DisconnectReason { get; set; }

        private int _disposed;
        public DateTime TimeConnected;
        public DateTime TimeLastHeartbeat;

        public ClientSession(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            SendLock = new SemaphoreSlim(1, 1);

            Info = new ClientInfo(Guid.NewGuid(), client.Client.RemoteEndPoint?.ToString() ?? "Unknown");

            // 접속 직후부터 1분간 Heartbeat를 기다린다.
            TimeConnected = DateTime.Now;
            TimeLastHeartbeat = DateTime.Now.AddHours(-1);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                Stream.Dispose();
            }
            catch
            {
                // 종료 중 예외 무시
            }

            try
            {
                Client.Dispose();
            }
            catch
            {
                // 종료 중 예외 무시
            }

            SendLock.Dispose();
        }
    }
}
