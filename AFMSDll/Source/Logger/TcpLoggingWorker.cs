using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AFMSDll
{
    public sealed class TcpLoggingOptions
    {
        public int Port { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public TimeSpan DiagnosticsInterval { get; set; } = TimeSpan.FromSeconds(10);
    }

    public sealed class TcpLoggingDiagnostics
    {
        private long lastMeasurementTicks;

        public DateTime ServiceStartTime { get; } = Process.GetCurrentProcess().StartTime;
        public DateTime? LastMeasurementTime
        {
            get
            {
                long ticks = Interlocked.Read(ref lastMeasurementTicks);
                return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Local);
            }
        }

        public void ReportMeasurement(DateTime measurementTime)
        {
            Interlocked.Exchange(ref lastMeasurementTicks, measurementTime.ToLocalTime().Ticks);
        }
    }

    public sealed class TcpLoggingWorker : BackgroundService
    {
        private readonly TcpLoggingOptions options;
        private readonly TcpLoggingDiagnostics diagnostics;
        private readonly ConcurrentDictionary<Guid, ClientConnection> clients = new();
        private readonly Channel<_PacketBase> outgoing = Channel.CreateBounded<_PacketBase>(new BoundedChannelOptions(4096)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        private TcpListener? listener;
        private TcpLogAppender? appender;

        public TcpLoggingWorker(IOptions<TcpLoggingOptions> options, TcpLoggingDiagnostics diagnostics)
        {
            this.options = options.Value;
            this.diagnostics = diagnostics;
            if (this.options.Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(options), "TCP 로그 포트가 올바르지 않습니다.");
            if (string.IsNullOrWhiteSpace(this.options.ServiceName)) throw new ArgumentException("서비스명이 필요합니다.", nameof(options));
            if (this.options.DiagnosticsInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options), "진단 전송 주기가 올바르지 않습니다.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AttachAppender();
            listener = new TcpListener(IPAddress.Any, options.Port);
            listener.Start();

            try
            {
                await Task.WhenAll(AcceptClientsAsync(stoppingToken), SendPacketsAsync(stoppingToken), PublishDiagnosticsAsync(stoppingToken)).ConfigureAwait(false);
            }
            finally
            {
                DetachAppender();
                listener.Stop();
                foreach (ClientConnection client in clients.Values) client.Dispose();
                clients.Clear();
            }
        }

        private void AttachAppender()
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            appender = new TcpLogAppender(QueueLogEvent);
            appender.ActivateOptions();
            hierarchy.Root.AddAppender(appender);
        }

        private void DetachAppender()
        {
            if (appender == null) return;
            if (LogManager.GetRepository() is Hierarchy hierarchy) hierarchy.Root.RemoveAppender(appender);
            appender.Close();
            appender = null;
        }

        private void QueueLogEvent(LoggingEvent loggingEvent)
        {
            var message = new ViewLogMsg
            {
                ClientId = options.ServiceName,
                LogHost = loggingEvent.LoggerName ?? string.Empty,
                LogLevel = loggingEvent.Level?.DisplayName ?? string.Empty,
                LogMsg = loggingEvent.RenderedMessage ?? string.Empty,
                LogTime = loggingEvent.TimeStamp.ToLocalTime()
            };
            if (loggingEvent.ExceptionObject != null) message.LogMsg += Environment.NewLine + loggingEvent.GetExceptionString();
            outgoing.Writer.TryWrite(message);
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient tcpClient = await listener!.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                tcpClient.NoDelay = true;
                var connection = new ClientConnection(tcpClient);
                clients[connection.Id] = connection;
                _ = ObserveDisconnectAsync(connection, cancellationToken);
                await SendAsync(connection, CreateDiagnostics(), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ObserveDisconnectAsync(ClientConnection connection, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1];
            try
            {
                while (await connection.Stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false) > 0)
                {
                }
            }
            catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
            {
            }
            finally
            {
                clients.TryRemove(connection.Id, out _);
                connection.Dispose();
            }
        }

        private async Task SendPacketsAsync(CancellationToken cancellationToken)
        {
            await foreach (_PacketBase packet in outgoing.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                ClientConnection[] snapshot = clients.Values.ToArray();
                await Task.WhenAll(snapshot.Select(client => SendAsync(client, packet, cancellationToken))).ConfigureAwait(false);
            }
        }

        private async Task PublishDiagnosticsAsync(CancellationToken cancellationToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(options.DiagnosticsInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false)) outgoing.Writer.TryWrite(CreateDiagnostics());
        }

        private LoggerDiagnostics CreateDiagnostics()
        {
            using Process process = Process.GetCurrentProcess();
            process.Refresh();
            return new LoggerDiagnostics
            {
                ClientId = options.ServiceName,
                ServiceStartTime = diagnostics.ServiceStartTime,
                MemoryUsageBytes = process.WorkingSet64,
                LastMeasurementTime = diagnostics.LastMeasurementTime
            };
        }

        private async Task SendAsync(ClientConnection client, _PacketBase value, CancellationToken cancellationToken)
        {
            value.SendingTime = DateTime.Now;
            byte[] json = Encoding.UTF8.GetBytes(value.GetJsonString());
            byte[] packet = PacketJsonProtocol.Encode(PacketJsonProtocol.JSON_CMD, json);
            try
            {
                await client.SendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await client.Stream.WriteAsync(packet, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    client.SendLock.Release();
                }
            }
            catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
            {
                clients.TryRemove(client.Id, out _);
                client.Dispose();
            }
        }

        private sealed class ClientConnection : IDisposable
        {
            private readonly TcpClient client;
            private int disposed;

            public ClientConnection(TcpClient client)
            {
                this.client = client;
                Stream = client.GetStream();
            }

            public Guid Id { get; } = Guid.NewGuid();
            public NetworkStream Stream { get; }
            public SemaphoreSlim SendLock { get; } = new(1, 1);

            public void Dispose()
            {
                if (Interlocked.Exchange(ref disposed, 1) != 0) return;
                client.Dispose();
                SendLock.Dispose();
            }
        }

        private sealed class TcpLogAppender : AppenderSkeleton
        {
            private readonly Action<LoggingEvent> append;

            public TcpLogAppender(Action<LoggingEvent> append)
            {
                this.append = append;
                Name = nameof(TcpLoggingWorker);
            }

            protected override bool RequiresLayout => false;
            protected override void Append(LoggingEvent loggingEvent) => append(loggingEvent);
        }
    }

    public static class TcpLoggingServiceCollectionExtensions
    {
        public static IServiceCollection AddTcpLogging(this IServiceCollection services, Action<TcpLoggingOptions> configure)
        {
            services.Configure(configure);
            services.AddSingleton<TcpLoggingDiagnostics>();
            services.AddHostedService<TcpLoggingWorker>();
            return services;
        }
    }
}
