using AFMSDll;
using log4net;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace AFMSExtraLogger
{
    public class TcpPacketServer
    {
        private static readonly ILog Log = LogManager.GetLogger("TCP");
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<Guid, ClientSession> _clients = new();
        private readonly ConcurrentDictionary<Guid, Task> _clientTasks = new();
        private TcpMessageDispatcher? _dispatcher;
        public TcpPacketServer(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
        }

        /// <summary>
        /// 모든 정상 패킷을 수신했을 때 호출된다.
        /// </summary>
        public Func<ClientInfo, TcpPacket, CancellationToken, Task>? PacketReceivedAsync { get; set; }

        /// <summary>
        /// CMD가 0xFA이고 JSON 변환에 성공했을 때 호출된다.
        /// </summary>
        public Func<ClientInfo, JsonElement, CancellationToken, Task>? JsonReceivedAsync { get; set; }

        public IReadOnlyCollection<ClientInfo> Clients =>_clients.Values.Select(client => client.Info).ToArray();

        public void SetDispatcher(TcpMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// 서버 실행
        /// </summary>
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            _listener.Start();

            TcpBrocastBuffer.WriteLog("SYS", $"TCP 서버 시작: {_listener.LocalEndpoint}");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);

                    tcpClient.NoDelay = true;

                    var session = new ClientSession(tcpClient);

                    if (!_clients.TryAdd(session.Info.Id, session))
                    {
                        session.Dispose();
                        continue;
                    }

                    TcpBrocastBuffer.WriteLog("SYS", $"클라이언트 접속: {session.Info.RemoteEndPoint}, ID={session.Info.Id}");

                    Task clientTask = HandleClientAsync(session, cancellationToken);

                    _clientTasks[session.Info.Id] = clientTask;

                    _ = clientTask.ContinueWith(
                        completedTask =>
                        {
                            _clientTasks.TryRemove(session.Info.Id, out _);
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 정상 종료
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                // 서버 종료 과정
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                // 서버 종료 과정
            }
            finally
            {
                _listener.Stop();

                ClientSession[] sessions = _clients.Values.ToArray();

                foreach (ClientSession session in sessions)
                {
                    session.Dispose();
                }

                Task[] runningTasks = _clientTasks.Values.ToArray();

                if (runningTasks.Length > 0)
                {
                    try
                    {
                        await Task.WhenAll(runningTasks);
                    }
                    catch
                    {
                        // 각 클라이언트 작업에서 이미 예외를 처리한다.
                    }
                }

                _clients.Clear();

                Log.Info("TCP 서버 종료");
            }
        }

        private async Task HandleClientAsync(ClientSession session, CancellationToken cancellationToken)
        {
            byte[] readBuffer = new byte[8192];

            var parser = new PacketStreamParser();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int receivedCount =
                        await session.Stream.ReadAsync(
                            readBuffer.AsMemory(),
                            cancellationToken);

                    if (receivedCount == 0)
                    {
                        session.DisconnectReason ??= "RemoteClosed";

                        break;
                    }

                    parser.Append(
                        readBuffer.AsSpan(
                            0,
                            receivedCount));

                    while (true)
                    {
                        PacketParseResult result = parser.TryReadPacket(out TcpPacket? packet, out string? error);

                        if (result == PacketParseResult.NeedMoreData) break;

                        if (result == PacketParseResult.InvalidData)
                        {
                            Log.Warn(
                                $"패킷 오류 | " +
                                $"Remote={session.Info.RemoteEndPoint} | " +
                                $"Error={error}");

                            continue;
                        }

                        if (packet is not null)
                        {
                            await ProcessPacketAsync(session, packet, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                session.DisconnectReason ??= "ServerStopping";
            }
            catch (ObjectDisposedException)
            {
                session.DisconnectReason ??= "ConnectionDisposed";
            }
            catch (IOException ex)
            {
                session.DisconnectReason ??= "NetworkError";

                TcpBrocastBuffer.WriteLog("SYS", $"클라이언트 통신 오류 | Remote={session.Info.RemoteEndPoint} | Error={ex.Message}");
            }
            catch (SocketException ex)
            {
                session.DisconnectReason ??= "SocketError";

                TcpBrocastBuffer.WriteLog("SYS", $"클라이언트 소켓 오류 | Remote={session.Info.RemoteEndPoint} | Error={ex.Message}");
            }
            catch (Exception ex)
            {
                session.DisconnectReason ??= "UnhandledError";

                TcpBrocastBuffer.WriteLog("SYS", $"클라이언트 처리 오류 | Remote={session.Info.RemoteEndPoint} {ex.ToString()}");
            }
            finally
            {
                _clients.TryRemove(session.Info.Id, out _);

                session.Dispose();

                _dispatcher?.OnClientDisconnected(session.Info, session.DisconnectReason?? "Unknown");
            }
        }

        private async Task ProcessPacketAsync(ClientSession session, TcpPacket packet,  CancellationToken cancellationToken)
        {
            // 모든 명령에 대한 공통 이벤트
            if (PacketReceivedAsync is not null)
            {
                await PacketReceivedAsync(session.Info, packet, cancellationToken);
            }

            // CMD가 FA가 아니면 JSON 처리를 하지 않는다.
            if (packet.Command != PacketJsonProtocol.JSON_CMD) return;

            try
            {
                // UTF-8 JSON 데이터를 직접 해석
                using JsonDocument document = JsonDocument.Parse(packet.Data);

                // JsonDocument가 Dispose된 뒤에도 사용할 수 있도록 복사
                JsonElement json = document.RootElement.Clone();

                if (JsonReceivedAsync is not null)
                {
                    await JsonReceivedAsync(session.Info, json, cancellationToken);
                }
            }
            catch (JsonException ex)
            {
                string receivedText = Encoding.UTF8.GetString(packet.Data);

                Console.WriteLine($"[{session.Info.RemoteEndPoint}] JSON 변환 실패: {ex.Message}");
                Console.WriteLine($"수신 문자열: {receivedText}");
            }
        }

        /// <summary>
        /// 특정 클라이언트에 바이너리 패킷 전송
        /// </summary>
        public async Task<bool> SendAsync(Guid clientId, byte command, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (!_clients.TryGetValue(clientId, out ClientSession? session)) return false;

            byte[] packet = PacketJsonProtocol.Encode(command, data.Span);

            try
            {
                await session.SendLock.WaitAsync(cancellationToken);

                try
                {
                    await session.Stream.WriteAsync(packet.AsMemory(), cancellationToken);
                }
                finally
                {
                    session.SendLock.Release();
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// 특정 클라이언트에 문자열 전송
        /// </summary>
        public Task<bool> SendTextAsync(Guid clientId, byte command, string text, CancellationToken cancellationToken = default)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);

            return SendAsync(clientId, command, data, cancellationToken);
        }

        /// <summary>
        /// 특정 클라이언트에 JSON 전송
        /// CMD는 0xFA로 설정된다.
        /// </summary>
        public Task<bool> SendJsonAsync(Guid clientId, object value, CancellationToken cancellationToken = default)
        {
            byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(value);

            return SendAsync(clientId, PacketJsonProtocol.JSON_CMD, jsonData, cancellationToken);
        }

        /// <summary>
        /// 접속한 모든 클라이언트에 동일한 패킷 전송
        /// 반환값은 전송에 성공한 클라이언트 수
        /// </summary>
        public async Task<int> BroadcastAsync(byte command, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            Guid[] clientIds = _clients.Keys.ToArray();

            Task<bool>[] sendTasks = clientIds.Select(clientId => SendAsync(clientId, command, data, cancellationToken)).ToArray();

            bool[] results = await Task.WhenAll(sendTasks);

            return results.Count(result => result);
        }

        /// <summary>
        /// 접속한 모든 클라이언트에 JSON 전송
        /// </summary>
        public Task<int> BroadcastJsonAsync(_PacketBase value, CancellationToken cancellationToken = default)
        {
            value.SendingTime = DateTime.Now;

            byte[] jsonData = Encoding.UTF8.GetBytes(value.GetJsonString());

            Guid[] clientIds = _clients.Keys.ToArray();

            Log.Info($"SEND Brocadcast for client {clientIds.Length} => {value.JsonType.ToString()}");
            Console.WriteLine(value.GetJsonString());

            return BroadcastAsync(PacketJsonProtocol.JSON_CMD, jsonData, cancellationToken);
        }

        public Task<bool> DisconnectClientAsync(Guid clientId, string reason)
        {
            if (!_clients.TryGetValue(clientId, out ClientSession? session))
            {
                return Task.FromResult(false);
            }

            session.DisconnectReason = reason;
            session.Dispose();

            return Task.FromResult(true);
        }
    }
}
