using AFMSDll;
using log4net;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace AFMSExtraMonitor
{
    public sealed class TcpReconnectClient
    {
        private static readonly ILog Log = LogManager.GetLogger("TCP");

        private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

        private readonly string _serverAddress;
        private readonly int _serverPort;
        private readonly string _clientId;

        private readonly object _connectionLock = new();
        private readonly object _sendLock = new();

        private CancellationTokenSource? _cancellation;
        private Thread? _connectionThread;
        private Thread? _receiveThread;

        private TcpClient? _client;
        private NetworkStream? _stream;

        private int _connected;
        private int _disposed;

        public TcpReconnectClient(string serverAddress, int serverPort, string clientId)
        {
            _serverAddress = serverAddress;
            _serverPort = serverPort;
            _clientId = clientId;
        }

        public bool IsConnected => Volatile.Read(ref _connected) == 1;

        /// <summary>
        /// 연결 상태가 변경되었을 때 발생한다.
        /// true: 연결됨
        /// false: 연결 해제
        /// </summary>
        public event Action<bool>? ConnectionChanged;

        /// <summary>
        /// JSON Broadcast를 수신했을 때 발생한다.
        /// </summary>
        public event Action<string>? JsonReceived;

        /// <summary>
        /// 일반 패킷을 수신했을 때 발생한다.
        /// </summary>
        public event Action<TcpPacket>? PacketReceived;

        public void Start()
        {
            ThrowIfDisposed();

            if (_connectionThread?.IsAlive == true) return;

            _cancellation = new CancellationTokenSource();

            _connectionThread = new Thread(() => ConnectionThreadProc(_cancellation.Token))
                {
                    Name = "TCP Connection Thread",
                    IsBackground = true
                };

            _connectionThread.Start();

            Log.Info($"TCP Client 시작 | Server={_serverAddress}:{_serverPort} | ClientId={_clientId}");
        }

        public void Stop()
        {
            CancellationTokenSource? cancellation = _cancellation;

            if (cancellation == null) return;

            Log.Info("TCP Client 종료 요청");

            cancellation.Cancel();

            // Read() 또는 Write()가 대기 중이면
            // 소켓을 닫아서 즉시 빠져나오게 한다.
            Disconnect(reason: "ClientStopping");
            JoinThread(_connectionThread, 3000);
            JoinThread(_receiveThread, 1000);

            cancellation.Dispose();

            _cancellation = null;
            _connectionThread = null;
            _receiveThread = null;

            Log.Info("TCP Client 종료 완료");
        }

        /// <summary>
        /// 연결 관리 전용 스레드
        /// </summary>
        private void ConnectionThreadProc(CancellationToken cancellationToken)
        {
            DateTime nextHeartbeatUtc = DateTime.MinValue;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!IsConnected)
                    {
                        bool connected = TryConnect(cancellationToken);

                        if (connected)
                        {
                            nextHeartbeatUtc = DateTime.Now;
                            continue;
                        }

                        // 재접속까지 5초 대기한다.
                        // 취소되면 즉시 true가 반환된다.
                        if (cancellationToken.WaitHandle.WaitOne(ReconnectInterval)) break;

                        continue;
                    }

                    if (DateTime.Now >= nextHeartbeatUtc)
                    {
                        bool sent = SendHeartbeat(cancellationToken);

                        if (sent)
                        {
                            nextHeartbeatUtc = DateTime.Now.Add(HeartbeatInterval);
                        }
                        else
                        {
                            // 전송 실패 시 연결이 종료되므로
                            // 다음 반복에서 재접속한다.
                            nextHeartbeatUtc = DateTime.MinValue;
                        }
                    }

                    // 연결 상태를 빠르게 확인하기 위한 짧은 대기
                    if (cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(200))) break;
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Log.Error("TCP 연결 관리 스레드 오류", ex);
                }
            }
            finally
            {
                Disconnect(reason: "ConnectionThreadStopped");

                Log.Info("TCP 연결 관리 스레드 종료");
            }
        }

        private bool TryConnect(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return false;


            TcpClient newClient = new TcpClient{ NoDelay = true };

            try
            {
                Log.Info($"TCP 서버 연결 시도 | Server={_serverAddress}:{_serverPort}");

                using CancellationTokenSource connectCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                connectCancellation.CancelAfter(ConnectTimeout);

                newClient.ConnectAsync(_serverAddress,_serverPort,connectCancellation.Token)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();

                NetworkStream newStream = newClient.GetStream();

                lock (_connectionLock)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        newStream.Dispose();
                        newClient.Dispose();

                        return false;
                    }

                    _client = newClient;
                    _stream = newStream;

                    Volatile.Write(ref _connected, 1);
                }

                Log.Info(
                    $"TCP 서버 연결 완료 | " +
                    $"Server={_serverAddress}:{_serverPort} | " +
                    $"ClientId={_clientId}");

                RaiseConnectionChanged(true);

                StartReceiveThread(newClient, newStream, cancellationToken);

                return true;
            }
            catch (OperationCanceledException)
            {
                newClient.Dispose();

                if (!cancellationToken.IsCancellationRequested)
                {
                    Log.Warn($"TCP 연결 시간 초과 | Server={_serverAddress}:{_serverPort}");
                }

                return false;
            }
            catch (SocketException ex)
            {
                newClient.Dispose();

                Log.Warn(
                    $"TCP 연결 실패 | " +
                    $"Server={_serverAddress}:{_serverPort} | " +
                    $"Error={ex.Message}");

                return false;
            }
            catch (Exception ex)
            {
                newClient.Dispose();

                if (!cancellationToken.IsCancellationRequested)
                {
                    Log.Error(
                        $"TCP 연결 오류 | " +
                        $"Server={_serverAddress}:{_serverPort}",
                        ex);
                }

                return false;
            }
        }

        private void StartReceiveThread(TcpClient ownerClient, NetworkStream ownerStream, CancellationToken cancellationToken)
        {
            Thread receiveThread =
                new Thread(
                    () => ReceiveThreadProc(
                        ownerClient,
                        ownerStream,
                        cancellationToken))
                {
                    Name = "TCP Receive Thread",
                    IsBackground = true
                };

            _receiveThread = receiveThread;

            receiveThread.Start();
        }

        /// <summary>
        /// 서버 Broadcast 수신 전용 스레드
        /// </summary>
        private void ReceiveThreadProc(TcpClient ownerClient, NetworkStream ownerStream, CancellationToken cancellationToken)
        {
            byte[] readBuffer = new byte[8192];

            PacketStreamParser parser = new PacketStreamParser();

            string disconnectReason = "Unknown";

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int receivedCount = ownerStream.Read(readBuffer, 0, readBuffer.Length);

                    // 서버가 정상적으로 연결을 종료한 경우
                    if (receivedCount == 0)
                    {
                        disconnectReason = "RemoteClosed";
                        break;
                    }

                    parser.Append(readBuffer.AsSpan(0, receivedCount));

                    while (true)
                    {
                        PacketParseResult result =
                            parser.TryReadPacket(
                                out TcpPacket? packet,
                                out string? error);

                        if (result == PacketParseResult.NeedMoreData) break;

                        if (result == PacketParseResult.InvalidData)
                        {
                            Log.Warn($"TCP 패킷 오류 | Error={error}");
                            continue;
                        }

                        if (packet != null) ProcessReceivedPacket(packet);

                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    disconnectReason = "ClientStopping";
                }
            }
            catch (IOException ex)
            {
                disconnectReason = "NetworkError";

                if (!cancellationToken.IsCancellationRequested)
                {                    
                    Log.Warn($"TCP 수신 오류 | Error={ex.Message}");
                }
            }
            catch (SocketException ex)
            {
                disconnectReason = "SocketError";

                if (!cancellationToken.IsCancellationRequested)
                {
                    Log.Warn($"TCP 소켓 오류 | Error={ex.Message}");
                }
            }
            catch (ObjectDisposedException)
            {
                disconnectReason = cancellationToken.IsCancellationRequested
                        ? "ClientStopping"
                        : "ConnectionDisposed";
            }
            catch (Exception ex)
            {
                disconnectReason = "ReceiveError";

                if (!cancellationToken.IsCancellationRequested)
                {
                    Log.Error("TCP 수신 스레드 오류", ex);
                }
            }
            finally
            {
                // 현재 연결이 ownerClient와 같은 경우에만
                // 연결 해제 처리한다.
                Disconnect(disconnectReason, ownerClient);

                Log.Info($"TCP 수신 스레드 종료 | Reason={disconnectReason}");
            }
        }

        private void ProcessReceivedPacket(TcpPacket packet)
        {
            try
            {
                PacketReceived?.Invoke(packet);

                if (packet.Command != PacketJsonProtocol.JSON_CMD)
                {
                    Log.Debug($"일반 패킷 수신 | CMD=0x{packet.Command:X2} | Length={packet.Data.Length}");
                    return;
                }

                string json = Encoding.UTF8.GetString(packet.Data);

                Log.Debug($"JSON Broadcast 수신 | Length={packet.Data.Length}");
                Log.Debug(json);
                JsonReceived?.Invoke(json);
            }
            catch (Exception ex)
            {
                Log.Error("수신 패킷 이벤트 처리 오류", ex);
            }
        }

        private bool SendHeartbeat(CancellationToken cancellationToken)
        {
            TcpClient? ownerClient;
            NetworkStream? ownerStream;

            lock (_connectionLock)
            {
                ownerClient = _client;
                ownerStream = _stream;
            }

            if (ownerClient == null || ownerStream == null || !IsConnected) return false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var heartbeat = new
                    {
                        _JsonType = JsonPacketType.Heartbeat,
                        _ClientId =_clientId,
                        _DateTime = DateTime.Now
                    };

                // JSON 문자열을 다시 직렬화하지 않고
                // 객체를 바로 UTF-8 JSON으로 변환한다.
                byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(heartbeat);
                byte[] packet = PacketJsonProtocol.Encode(PacketJsonProtocol.JSON_CMD, jsonData);

                lock (_sendLock)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ownerStream.Write(packet, 0, packet.Length);

                    ownerStream.Flush();
                }

                Log.Debug($"Heartbeat 전송 | ClientId={_clientId} | Time={DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Log.Warn(
                    $"Heartbeat 전송 실패 | " +
                    $"ClientId={_clientId} | " +
                    $"Error={ex.Message}");

                Disconnect(
                    reason: "HeartbeatSendFailed",
                    expectedClient: ownerClient);

                return false;
            }
        }

        /// <summary>
        /// 일반 JSON 메시지 전송
        /// </summary>
        public bool SendJson(object value)
        {
            TcpClient? ownerClient;
            NetworkStream? ownerStream;

            lock (_connectionLock)
            {
                ownerClient = _client;
                ownerStream = _stream;
            }

            if (ownerClient == null || ownerStream == null || !IsConnected) return false;

            try
            {
                byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(value);

                byte[] packet = PacketJsonProtocol.Encode(PacketJsonProtocol.JSON_CMD, jsonData);

                lock (_sendLock)
                {
                    ownerStream.Write(packet, 0, packet.Length);
                    ownerStream.Flush();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"TCP JSON 전송 실패 | Error={ex.Message}");
                Disconnect(reason: "JsonSendFailed", expectedClient: ownerClient);
                return false;
            }
        }

        private void Disconnect(string reason, TcpClient? expectedClient = null)
        {
            TcpClient? closeClient;
            NetworkStream? closeStream;

            lock (_connectionLock)
            {
                // 이전 수신 스레드가 새 연결까지 종료하지 못하도록 한다.
                if (expectedClient != null && !ReferenceEquals(_client, expectedClient)) return;

                closeClient = _client;
                closeStream = _stream;

                if (closeClient == null && closeStream == null)
                {
                    return;
                }

                _client = null;
                _stream = null;

                Volatile.Write(ref _connected, 0);
            }

            try
            {
                closeClient?.Client.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // 이미 연결이 종료된 경우
            }

            try
            {
                closeStream?.Dispose();
            }
            catch
            {
                // 종료 중 오류 무시
            }

            try
            {
                closeClient?.Dispose();
            }
            catch
            {
                // 종료 중 오류 무시
            }

            Log.Info($"TCP 연결 해제 | Server={_serverAddress}:{_serverPort} | Reason={reason}");

            RaiseConnectionChanged(false);
        }

        private void RaiseConnectionChanged(bool connected)
        {
            try
            {
                ConnectionChanged?.Invoke(connected);
            }
            catch (Exception ex)
            {
                Log.Error("ConnectionChanged 이벤트 오류", ex);
            }
        }

        private static void JoinThread(Thread? thread, int timeoutMilliseconds)
        {
            if (thread == null || thread == Thread.CurrentThread) return;

            try
            {
                thread.Join(timeoutMilliseconds);
            }
            catch
            {
                // 종료 과정
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            Stop();
        }
    }
}
