using AFMSDll;
using System.Net.Sockets;
using System.Text.Json;

namespace AFMSLoggerMonitors
{
    internal sealed class TcpConnectionChangedEventArgs : EventArgs
    {
        public TcpConnectionChangedEventArgs(bool connected)
        {
            Connected = connected;
        }

        public bool Connected { get; }
    }

    internal sealed class LoggerJsonReceivedEventArgs : EventArgs
    {
        public LoggerJsonReceivedEventArgs(JsonElement json)
        {
            Json = json;
        }

        public JsonElement Json { get; }
    }

    internal sealed class LoggerTcpClient : IDisposable
    {
        private readonly string host;
        private readonly int port;
        private readonly TimeSpan reconnectDelay;
        private readonly object stateLock = new object();
        private CancellationTokenSource? cancellationTokenSource;
        private Task? workerTask;
        private bool connected;
        private bool disposed;

        public LoggerTcpClient(string host, int port, TimeSpan reconnectDelay)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("호스트가 필요합니다.", nameof(host));
            if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            if (reconnectDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(reconnectDelay));

            this.host = host;
            this.port = port;
            this.reconnectDelay = reconnectDelay;
        }

        public event EventHandler<TcpConnectionChangedEventArgs>? ConnectionChanged;
        public event EventHandler<LoggerJsonReceivedEventArgs>? JsonReceived;
        public event EventHandler<Exception>? CommunicationFailed;

        public bool Connected
        {
            get
            {
                lock (stateLock) return connected;
            }
        }

        public void Start()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (workerTask != null) return;

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            workerTask = Task.Run(() => RunAsync(cancellationToken), cancellationToken);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using TcpClient client = new TcpClient();
                try
                {
                    await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
                    SetConnected(true);
                    await ReceiveAsync(client, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception) when (exception is SocketException or IOException or InvalidOperationException)
                {
                    CommunicationFailed?.Invoke(this, exception);
                }
                finally
                {
                    SetConnected(false);
                }

                try
                {
                    await Task.Delay(reconnectDelay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task ReceiveAsync(TcpClient client, CancellationToken cancellationToken)
        {
            NetworkStream stream = client.GetStream();
            PacketStreamParser parser = new PacketStreamParser();
            byte[] buffer = new byte[8192];

            while (!cancellationToken.IsCancellationRequested)
            {
                int receivedCount = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (receivedCount == 0) return;

                parser.Append(buffer.AsSpan(0, receivedCount));
                ReadPackets(parser);
            }
        }

        private void ReadPackets(PacketStreamParser parser)
        {
            while (true)
            {
                PacketParseResult result = parser.TryReadPacket(out TcpPacket? packet, out _);
                if (result == PacketParseResult.NeedMoreData) return;
                if (result == PacketParseResult.InvalidData || packet == null || packet.Command != PacketJsonProtocol.JSON_CMD) continue;

                try
                {
                    using JsonDocument document = JsonDocument.Parse(packet.Data);
                    JsonReceived?.Invoke(this, new LoggerJsonReceivedEventArgs(document.RootElement.Clone()));
                }
                catch (JsonException exception)
                {
                    CommunicationFailed?.Invoke(this, exception);
                }
            }
        }

        private void SetConnected(bool value)
        {
            lock (stateLock)
            {
                if (connected == value) return;
                connected = value;
            }

            ConnectionChanged?.Invoke(this, new TcpConnectionChangedEventArgs(value));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            cancellationTokenSource?.Cancel();
            try
            {
                workerTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            workerTask = null;
        }
    }
}
