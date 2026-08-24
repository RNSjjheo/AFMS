using log4net;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace AFMSExtraLogger
{
    public class RFSerialServer : BackgroundService
    {
        private static readonly ILog Log = LogManager.GetLogger("_RF");

        private readonly SerialOptions _options;
        private readonly RFSerialProtocolHandler _protocolHandler;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        private SerialPort? _activePort;

        public RFSerialServer()
        {
            _options = new SerialOptions();
            _options.PortName = DiagnosticsOwner.Instance.MPDSPort;

            _protocolHandler = new RFSerialProtocolHandler();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool firstAttempt = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                // 최초 실행은 즉시 연결을 시도하고,
                // 두 번째 연결 시도부터는 30초 대기
                if (!firstAttempt)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                firstAttempt = false;

                try
                {
                    using SerialPort port = CreateSerialPort();

                    _activePort = port;

                    TcpBrocastBuffer.WriteLog("_RF", $"시리얼 포트 연결 시도: {port.PortName}");

                    port.Open();

                    await Task.Delay(500, stoppingToken);

                    if (!port.IsOpen)
                    {
                        throw new IOException($"시리얼 포트가 열리지 않았습니다: {port.PortName}");
                    }

                    string msg = $"시리얼 포트 연결 성공: {port.PortName}, ";
                    msg += $"{port.BaudRate}, {port.DataBits}, ";
                    msg +=$"{port.Parity}, {port.StopBits}";
                    TcpBrocastBuffer.WriteLog("_RF", msg);
                    DiagnosticsOwner.Instance.MPDSPort = port.PortName;

                    await SendAsync(port, "AT\r", stoppingToken);

                    await Task.Delay(100, stoppingToken);

                    await SendAsync(port, "AT+INFO?\r", stoppingToken);

                    // 연결이 유지되는 동안 여기서 계속 수신
                    await ReadLoopAsync(port, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (UnauthorizedAccessException ex)
                {
                    TcpBrocastBuffer.WriteLog("_RF",$"시리얼 접근 거부: {ex.Message}");
                }
                catch (IOException ex)
                {
                    TcpBrocastBuffer.WriteLog("_RF", $"시리얼 입출력 오류: {ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    TcpBrocastBuffer.WriteLog("_RF", $"시리얼 상태 오류: {ex.Message}");
                }
                catch (Exception ex)
                {
                    TcpBrocastBuffer.WriteLog("_RF", $"시리얼 예외: {ex}");
                }
                finally
                {
                    if (_activePort != null)
                    {
                        try
                        {
                            if (_activePort.IsOpen) _activePort.Close();
                        }
                        catch (Exception ex)
                        {
                            TcpBrocastBuffer.WriteLog("_RF",$"시리얼 포트 종료 오류: {ex.Message}");
                        }
                        finally
                        {
                            _activePort = null;
                        }
                    }
                }
            }

            TcpBrocastBuffer.WriteLog("_RF", "시리얼 통신 Worker가 종료되었습니다.");
        }

        private SerialPort CreateSerialPort()
        {
            return new SerialPort(
                _options.PortName,
                _options.BaudRate,
                _options.Parity,
                _options.DataBits,
                _options.StopBits)
            {
                Handshake = _options.Handshake,
                Encoding = Encoding.ASCII,
                DtrEnable = false,
                RtsEnable = false,
                ReadBufferSize = Math.Max(4096, _options.ReadBufferSize),
                WriteBufferSize = Math.Max(2048, _options.WriteBufferSize),
                ReadTimeout = 3000,
                WriteTimeout = 3000,
                NewLine = "\r\n"
            };
        }

        private async Task ReadLoopAsync(SerialPort port, CancellationToken cancellationToken)
        {
            byte[] readBuffer = new byte[1024];
            StringBuilder lineBuffer = new();

            while (!cancellationToken.IsCancellationRequested && port.IsOpen)
            {
                int readCount = await port.BaseStream.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), cancellationToken);

                if (readCount == 0)
                {
                    TcpBrocastBuffer.WriteLog("_RF", "시리얼 포트가 닫혔습니다.");
                    return;
                } 

                lineBuffer.Append(Encoding.ASCII.GetString(readBuffer, 0, readCount));

                while (TryTakeLine(lineBuffer, out string? line))
                {
                    await _protocolHandler.HandleLineAsync(line, (text, token) => SendAsync(port, text, token), cancellationToken);
                }

                if (lineBuffer.Length > 65536)
                {
                    TcpBrocastBuffer.WriteLog("_RF", "\"개행 없이 64KB 이상 수신되어 버퍼를 초기화합니다.");
                    lineBuffer.Clear();
                }
            }
        }


        private async Task SendAsync(SerialPort port, string text,  CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);

            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                if (!port.IsOpen)
                {
                    TcpBrocastBuffer.WriteLog("_RF", "시리얼 포트가 닫혀 있어 송신할 수 없습니다.");
                    return;
                }

                await port.BaseStream.WriteAsync(bytes.AsMemory(), cancellationToken);
                await port.BaseStream.FlushAsync(cancellationToken);

                Log.Info($"SERIAL TX: "+  text.Replace("\r", "<CR>"));
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private static bool TryTakeLine(StringBuilder buffer, out string? line)
        {
            line = null;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != '\n')  continue;

                int length = i;
                if (length > 0 && buffer[length - 1] == '\r') length--;

                line = buffer.ToString(0, length);
                buffer.Remove(0, i + 1);

                return true;
            }

            return false;
        }
    }
}
