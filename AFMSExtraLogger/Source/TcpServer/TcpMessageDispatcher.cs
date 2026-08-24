using AFMSDll;
using log4net;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace AFMSExtraLogger
{
    public class TcpMessageDispatcher
    {
        private static readonly ILog Log = LogManager.GetLogger("TCP");
        private readonly TcpPacketServer _server;
        private readonly ConcurrentDictionary<Guid, ClientState> _clients = new();
        public TcpMessageDispatcher(TcpPacketServer server)
        {
            _server = server;
        }

        public void OnClientConnected(ClientInfo client)
        {
            var state = new ClientState(client);

            _clients[client.Id] = state;

            Log.Info($"클라이언트 접속 | SessionId={client.Id} | Remote={client.RemoteEndPoint} | 접속수={_clients.Count}");
        }

        public void OnClientDisconnected(ClientInfo client, string reason)
        {
            _clients.TryRemove(client.Id, out ClientState? state);

            string deviceId = state?.DeviceId ?? "Unknown";

            Log.Info(
                $"클라이언트 연결 종료 | " +
                $"SessionId={client.Id} | " +
                $"ClientId={deviceId} | " +
                $"Remote={client.RemoteEndPoint} | " +
                $"Reason={reason} | " +
                $"접속수={_clients.Count}");
        }

        /// <summary>
        /// TcpPacketServer가 수신한 완전한 패킷을 전달한다.
        /// </summary>
        public async Task DispatchAsync(ClientInfo client, TcpPacket packet, CancellationToken cancellationToken)
        {
            Log.Debug($"패킷 수신 | Remote={client.RemoteEndPoint} | CMD=0x{packet.Command:X2} | Length={packet.Data.Length}");

            if (packet.Command == PacketJsonProtocol.JSON_CMD)
            {
                await ProcessJsonPacketAsync(client, packet.Data, cancellationToken);
                return;
            }

            await ProcessBinaryPacketAsync(client, packet, cancellationToken);
        }

        private async Task ProcessJsonPacketAsync(ClientInfo client, byte[] jsonData, CancellationToken cancellationToken)
        {
            try
            {
                using JsonDocument document =JsonDocument.Parse(jsonData);

                JsonElement root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    Log.Warn($"JSON 형식 오류 | Remote={client.RemoteEndPoint} | JSON 최상위 형식이 Object가 아닙니다.");
                    return;
                }

                string? messageType = GetJsonString(root, "Type");

                if (string.IsNullOrWhiteSpace(messageType))
                {
                    Log.Warn($"JSON Type 누락 | Remote={client.RemoteEndPoint} | Data={root.GetRawText()}");
                    return;
                }

                switch (messageType.ToUpperInvariant())
                {
                    case "HEARTBEAT":
                        await ProcessHeartbeatAsync(
                            client,
                            root,
                            cancellationToken);
                        break;

                    default:
                        await ProcessNormalJsonAsync(
                            client,
                            messageType,
                            root,
                            cancellationToken);
                        break;
                }
            }
            catch (JsonException ex)
            {
                string text =
                    Encoding.UTF8.GetString(jsonData);

                Log.Error($"JSON 변환 실패 | Remote={client.RemoteEndPoint} | Error={ex.Message} | Data={text}");
            }
            catch (Exception ex)
            {
                Log.Error($"JSON 처리 중 오류 | Remote={client.RemoteEndPoint}", ex);
            }
        }

        private async Task ProcessHeartbeatAsync(ClientInfo client, JsonElement json, CancellationToken cancellationToken)
        {
            if (!_clients.TryGetValue(client.Id, out ClientState? state))
            {
                Log.Warn($"등록되지 않은 클라이언트의 Heartbeat |SessionId={client.Id} | Remote={client.RemoteEndPoint}");
                return;
            }

            string? clientId = GetJsonString(json, "ClientId");

            string? clientDateTime = GetJsonString(json, "DateTime");

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                state.DeviceId = clientId;
            }

            state.TimeLastHeartbeat = DateTime.Now;

            Log.Debug($"Heartbeat 수신 | SessionId={client.Id} | ClientId={state.DeviceId ?? "Unknown"} | Remote={client.RemoteEndPoint} | ClientTime={clientDateTime ?? "Unknown"}");

            var response = new
            {
                Type = "HeartbeatAck",
                Result = "OK",
                ClientId = state.DeviceId,
                ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            bool sent = await _server.SendJsonAsync(client.Id, response, cancellationToken);

            if (!sent)
            {
                Log.Warn(
                    $"Heartbeat 응답 실패 | " +
                    $"SessionId={client.Id} | " +
                    $"ClientId={state.DeviceId ?? "Unknown"} | " +
                    $"Remote={client.RemoteEndPoint}");
            }
        }

        /// <summary>
        /// Heartbeat가 아닌 일반 JSON 처리 위치
        /// </summary>
        private Task ProcessNormalJsonAsync(ClientInfo client, string messageType, JsonElement json, CancellationToken cancellationToken)
        {
            Log.Info(
                $"일반 JSON 수신 | " +
                $"SessionId={client.Id} | " +
                $"Remote={client.RemoteEndPoint} | " +
                $"Type={messageType} | " +
                $"Data={json.GetRawText()}");

            /*
             * 요청별 처리 코드를 이 위치에 작성한다.
             *
             * 예:
             *
             * switch (messageType.ToUpperInvariant())
             * {
             *     case "MEASUREDATA":
             *         return ProcessMeasureDataAsync(...);
             *
             *     case "BROADCAST":
             *         return ProcessBroadcastAsync(...);
             * }
             */

            return Task.CompletedTask;
        }

        private Task ProcessBinaryPacketAsync(ClientInfo client, TcpPacket packet, CancellationToken cancellationToken)
        {
            Log.Warn(
                $"지원하지 않는 바이너리 패킷 | " +
                $"SessionId={client.Id} | " +
                $"Remote={client.RemoteEndPoint} | " +
                $"CMD=0x{packet.Command:X2} | " +
                $"Length={packet.Data.Length}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// 5초마다 모든 클라이언트의 Heartbeat 상태를 확인한다.
        /// </summary>
        public async Task MonitorHeartbeatAsync(CancellationToken cancellationToken)
        {
            long intervalSec = 5;
            int timeout = 60;

            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSec));
            Log.Info($"Heartbeat 감시 시작 | " + $"Timeout={timeout}초 | CheckInterval={intervalSec}초");

            try
            {

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    ClientState[] states = _clients.Values.ToArray();

                    foreach (ClientState state in states)
                    {
                        TimeSpan diff = DateTime.Now - state.TimeLastHeartbeat;

                        if (diff.TotalSeconds <= timeout)
                        {
                            continue;
                        }

                        Log.Warn(
                            $"Heartbeat Timeout | " +
                            $"SessionId={state.Client.Id} | " +
                            $"ClientId={state.DeviceId ?? "Unknown"} | " +
                            $"Remote={state.Client.RemoteEndPoint} | " +
                            $"경과시간={diff.TotalSeconds:F1}초 | " +
                            $"마지막수신=" +
                            $"{state.TimeLastHeartbeat.ToLocalTime():yyyy-MM-dd HH:mm:ss}");

                        await _server.DisconnectClientAsync(
                            state.Client.Id,
                            "HeartbeatTimeout");
                    }
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // 서비스 정상 종료
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Heartbeat 감시 작업 오류",
                    ex);
            }
            finally
            {
                Log.Info("Heartbeat 감시 종료");
            }
        }

        private static string? GetJsonString(
            JsonElement root,
            string propertyName)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (JsonProperty property in
                     root.EnumerateObject())
            {
                if (!string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return property.Value.ValueKind switch
                {
                    JsonValueKind.String =>
                        property.Value.GetString(),

                    JsonValueKind.Number =>
                        property.Value.GetRawText(),

                    JsonValueKind.True =>
                        bool.TrueString,

                    JsonValueKind.False =>
                        bool.FalseString,

                    _ => null
                };
            }

            return null;
        }
    }
}
