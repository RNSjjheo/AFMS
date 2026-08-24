using AFMSDll;
using log4net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace AFMSExtraMonitor
{
    public sealed class TcpPacketClient : IAsyncDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger("TCP");

        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(20);

        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions {IncludeFields = true, PropertyNameCaseInsensitive = true };

        private TcpClient? _client;
        private NetworkStream? _stream;
        private string _clientId = string.Empty;
        private int _disposed;

        public event Action<Diagnotics>? DiagnosticsReceived;
        public event Action<string>? UnknownJsonReceived;
        public event Action<bool>? ConnectionChanged;

        public bool IsConnected => _client?.Connected == true && _stream != null;

        public async Task RunAsync(string serverAddress, int serverPort, string clientId, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            _clientId = clientId;

            _client = new TcpClient{NoDelay = true};

            IPAddress address = IPAddress.Parse(serverAddress);

            Log.Info($"TCP 서버 연결 시도 | Server={serverAddress}:{serverPort}");

            await _client.ConnectAsync(address, serverPort, cancellationToken);

            _stream = _client.GetStream();

            Log.Info($"TCP 서버 연결 완료 | Server={serverAddress}:{serverPort} | ClientId={_clientId}");

            ConnectionChanged?.Invoke(true);

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task receiveTask = ReceiveLoopAsync(linkedCts.Token);

            Task heartbeatTask = HeartbeatLoopAsync(linkedCts.Token);

            try
            {
                // 수신 또는 Heartbeat 작업 중 하나가 종료되면
                // 나머지 작업도 종료시킨다.
                await Task.WhenAny(receiveTask, heartbeatTask);

                linkedCts.Cancel();

                await Task.WhenAll(receiveTask, heartbeatTask);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
            {
                // 정상적인 연결 종료
            }
            finally
            {
                ConnectionChanged?.Invoke(false);

                Log.Info($"TCP 서버 연결 종료 | ClientId={_clientId}");
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            NetworkStream stream = _stream?? throw new InvalidOperationException("TCP 서버에 연결되지 않았습니다.");

            byte[] readBuffer = new byte[8192];
            PacketStreamParser parser = new PacketStreamParser();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int receivedCount = await stream.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), cancellationToken);

                    // 서버가 정상적으로 연결을 종료함
                    if (receivedCount == 0)
                    {
                        Log.Info("TCP 서버에서 연결을 종료했습니다.");

                        break;
                    }

                    parser.Append(readBuffer.AsSpan(0, receivedCount));

                    while (true)
                    {
                        PacketParseResult result = parser.TryReadPacket(out TcpPacket? packet, out string? error);

                        if (result == PacketParseResult.NeedMoreData) break;

                        if (result == PacketParseResult.InvalidData)
                        {
                            Log.Warn($"수신 패킷 오류 | Error={error}");
                            continue;
                        }

                        if (packet != null)
                        {
                            await ProcessPacketAsync(packet, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // 정상적인 종료 요청
            }
            catch (IOException ex)
            {
                Log.Warn($"TCP 수신 오류 | Error={ex.Message}");
            }
            catch (SocketException ex)
            {
                Log.Warn($"TCP 소켓 오류 | Error={ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                // 연결 종료 과정
            }
            catch (Exception ex)
            {
                Log.Error("TCP 수신 처리 오류", ex);

                throw;
            }
        }

        private Task ProcessPacketAsync(TcpPacket packet, CancellationToken cancellationToken)
        {
            Log.Debug($"패킷 수신 | CMD=0x{packet.Command:X2} | Length={packet.Data.Length}");

            if (packet.Command != PacketJsonProtocol.JSON_CMD)
            {
                Log.Warn($"지원하지 않는 패킷 명령 | CMD=0x{packet.Command:X2}");

                return Task.CompletedTask;
            }

            ProcessJsonPacket(packet.Data);

            return Task.CompletedTask;
        }

        private void ProcessJsonPacket(
            byte[] jsonData)
        {
            try
            {
                // UTF-8 JSON 바이트를 직접 파싱
                using JsonDocument document = JsonDocument.Parse(jsonData);

                JsonElement root =
                    document.RootElement;

                if (!TryGetPacketJsonType(root, out JsonPacketType jsonType))
                {
                    string unknownJson = Encoding.UTF8.GetString(jsonData);

                    Log.Warn($"JsonType을 찾을 수 없습니다. | Data={unknownJson}");

                    UnknownJsonReceived?.Invoke(unknownJson);
                    return;
                }

                Log.Info($"Broadcast JSON 수신 | JsonType={jsonType}");

                switch (jsonType)
                {
                    case JsonPacketType.Diagnotics:
                        ProcessDiagnostics(jsonData);
                        break;

                    default:
                        ProcessUnknownJson(jsonType, jsonData);
                        break;
                }
            }
            catch (JsonException ex)
            {
                string text = Encoding.UTF8.GetString(jsonData);

                Log.Error($"JSON 파싱 오류 | Error={ex.Message} | Data={text}");
            }
        }

        private void ProcessDiagnostics(
            byte[] jsonData)
        {
            Diagnotics? diagnostics = JsonSerializer.Deserialize<Diagnotics>(jsonData, _jsonOptions);

            if (diagnostics == null)
            {
                Log.Warn("Diagnostics JSON 변환 결과가 null입니다.");
                return;
            }

            Log.Info(
                $"Diagnostics 수신 | " +
                $"Memory={diagnostics.MemoryUsage:F2} MB | " +
                $"Min={diagnostics.MemoryUsageMin:F2} MB | " +
                $"Max={diagnostics.MemoryUsageMax:F2} MB");

            DiagnosticsReceived?.Invoke(
                diagnostics);
        }

        private void ProcessUnknownJson(JsonPacketType jsonType, byte[] jsonData)
        {
            string json = Encoding.UTF8.GetString(jsonData);

            Log.Warn(
                $"처리되지 않은 Broadcast | " +
                $"JsonType={jsonType} | " +
                $"Data={json}");

            UnknownJsonReceived?.Invoke(json);
        }

        private static bool TryGetPacketJsonType(JsonElement root, out JsonPacketType jsonType)
        {
            jsonType = default;

            if (root.ValueKind != JsonValueKind.Object) return false;


            foreach (JsonProperty property in root.EnumerateObject())
            {
                bool nameMatched = string.Equals(property.Name, "_JsonType", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(  property.Name, "JsonType", StringComparison.OrdinalIgnoreCase);

                if (!nameMatched) continue;


                // 다음처럼 숫자로 전달되는 경우
                // "_JsonType": 80
                if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    if (property.Value.TryGetInt32(out int number))
                    {
                        jsonType = (JsonPacketType)number;
                        return true;
                    }
                }

                // 다음처럼 문자열로 전달되는 경우
                // "JsonType": "Diagnotics"
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    string? text = property.Value.GetString();

                    return Enum.TryParse(text, true, out jsonType);
                }
            }

            return false;
        }

        private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await SendHeartbeatAsync(cancellationToken);

                    await Task.Delay(HeartbeatInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 정상 종료
            }
        }

        private async Task SendHeartbeatAsync(CancellationToken cancellationToken)
        {
            var heartbeat = new
            {
                _JsonType = JsonPacketType.Heartbeat,
                _ClientId = _clientId,
                _DateTime = DateTime.Now
            };

            await SendJsonAsync(heartbeat, cancellationToken);

            Log.Debug($"Heartbeat 전송 | ClientId={_clientId}");
        }

        public async Task SendJsonAsync(object value, CancellationToken cancellationToken = default)
        {
            NetworkStream stream = _stream?? throw new InvalidOperationException("TCP 서버에 연결되지 않았습니다.");

            byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);

            byte[] packet = PacketJsonProtocol.Encode(PacketJsonProtocol.JSON_CMD, jsonData);

            await _sendLock.WaitAsync(
                cancellationToken);

            try
            {
                await stream.WriteAsync(
                    packet.AsMemory(),
                    cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;


            try
            {
                _client?.Client.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // 이미 연결 종료됨
            }

            if (_stream != null)
            {
                await _stream.DisposeAsync();
            }

            _client?.Dispose();
            _sendLock.Dispose();

            _stream = null;
            _client = null;
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        }
    }
}
