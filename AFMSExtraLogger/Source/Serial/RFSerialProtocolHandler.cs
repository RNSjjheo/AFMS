using log4net;
using System.Net;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AFMSExtraLogger
{ 
    public class RFSerialProtocolHandler
    {
        private static readonly ILog Log = LogManager.GetLogger("_RF");
        private MeasurementBatch? _currentBatch;
        private string? _lastCompletedMeasureKey;
        private bool _discardCurrentSequence;

        public RFSerialProtocolHandler()
        {

        }

        public async Task HandleLineAsync(string line, Func<string, CancellationToken, Task> sendAsync,  CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            Log.Info($"SERIAL RX: {line}");

            if (!line.StartsWith(RFProtocol.KEY_DATA, StringComparison.OrdinalIgnoreCase))
            {
                HandleModemResponse(line);
                return;
            }

            if (!RFProtocol.TryParseModemLine(line, out RfModemMessage? modemMessage, out string modemError) || modemMessage is null)
            {
                TcpBrocastBuffer.WriteLog("_RF", $"RF 문자열 분석 실패: {modemError}; RX={line}");
                return;
            }

            if (!RFProtocol.TryParseFrame(modemMessage.HexFrame, out ProtocolFrame frame, out string frameError))
            {
                string msgfail = $"RF 프레임 분석 실패: Address={modemMessage.RfAddress}, Error={frameError}, HEX={modemMessage.HexFrame}";
                TcpBrocastBuffer.WriteLog("_RF", msgfail);
                return;
            }

            Log.Info($"RF 수신: Address={modemMessage.RfAddress}, RSSI={modemMessage.Rssi}, CMD=0x{frame.Command:X2}, LEN={frame.Payload.Length}");

            switch ((RFProtocolCommand)frame.Command)
            {
                case RFProtocolCommand.DeviceRequest:
                    await HandleDeviceRequestAsync(modemMessage, frame, sendAsync, cancellationToken);
                    break;

                case RFProtocolCommand.StationInfo:
                    HandleStationInfo((short)modemMessage.Rssi, frame.Payload);
                    break;

                case RFProtocolCommand.Velocity:
                    HandleVelocity(frame.Payload);
                    break;

                case RFProtocolCommand.Wind:
                    HandleWind(frame.Payload);
                    break;

                default:
                    TcpBrocastBuffer.WriteLog("_RF", $"지원하지 않는 CMD입니다: 0x{frame.Command}");
                    break;
            }
        }

        private async Task HandleDeviceRequestAsync(RfModemMessage modemMessage, ProtocolFrame frame, Func<string, CancellationToken, Task> sendAsync, CancellationToken cancellationToken)
        {
            if (!RFProtocol.TryParseDeviceRequest(frame.Payload, out DeviceRequestData? request, out string error) || request is null)
            {
                TcpBrocastBuffer.WriteLog("_RF", $"\"DEVICE_REQ 분석 실패: {error}");
                return;
            }

            TcpBrocastBuffer.WriteLog("_RF", $"DEV REQ: Date={request.Date}, CMD={frame.Command.ToString("X2")}");

            string responseFrame = RFProtocol.BuildHexFrame((byte)RFProtocolCommand.DeviceResponse, [RFProtocol.StationDeviceNumber]);
            string command = RFProtocol.BuildAtUDataCommand(modemMessage.RfAddress, responseFrame);

            await sendAsync(command, cancellationToken);

            DiagnosticsOwner.Instance.MPDSRFInfo = modemMessage.RfAddress;
            TcpBrocastBuffer.WriteLog("_RF", $"DEV RES: Address={modemMessage.RfAddress}, DeviceNumber={RFProtocol.StationDeviceNumber}, HEX={responseFrame}");
        }

        private void HandleStationInfo(short collectorRSSI, byte[] payload)
        {
            if (!RFProtocol.TryParseStationInfo(payload, out StationInfoData? info, out string error) || info is null)
            {
                TcpBrocastBuffer.WriteLog("_RF", $"STATION_SENDDATA_INFO 분석 실패: {error}");
                ResetSequence();
                return;
            }

            if (_currentBatch is not null)
            {
                TcpBrocastBuffer.WriteLog("_RF", $"\"이전 측정 시퀀스가 0x1C 없이 종료되었습니다. 이전 MeasureKey={_currentBatch.Info.MeasureKey}");
            }

            _discardCurrentSequence = string.Equals(info.MeasureKey, _lastCompletedMeasureKey, StringComparison.Ordinal);

            _currentBatch = _discardCurrentSequence? null : new MeasurementBatch(info);
            _currentBatch?.CollectorRSSI = collectorRSSI;

            if (_discardCurrentSequence)
            {
                string msgSkip = "[중복 수신] ";
                msgSkip += $"Key={info.MeasureKey} ";
                msgSkip += $"재전송이므로 이번 0x1A~0x1C 시퀀스 폐기";

                TcpBrocastBuffer.WriteLog("_RF", msgSkip);
                return;
            }

            TcpBrocastBuffer.WriteLog("_RF", $"[측정 시작] " + GetInfoMessage(info));
        }

        private string GetInfoMessage(StationInfoData info)
        {
            string msg = "";
            msg += $"Key={info.MeasureKey}, ";
            msg += $"PointCode={info.PointCode},";
            msg += $"DevCount={info.DeviceCount}, ";
            msg += $"Time={info.MeasureTime.ToString("yyyy-MM-dd HH:mm:ss")}, ";
            msg += $"Volt={info.CollectorVolt}";

            return msg;
        }

        private void HandleVelocity(byte[] payload)
        {
            if (_discardCurrentSequence) return;

            if (_currentBatch is null)
            {
                TcpBrocastBuffer.WriteLog("_RF", $"0x1A 수신 전에 0x1B가 수신되어 폐기합니다.");
                return;
            }

            if (!RFProtocol.TryParseVelocity(payload, out MPDSCell? velocity, out string error) || velocity is null)
            {
                TcpBrocastBuffer.WriteLog("_RF", $"STATION_SENDDATA_VELO 분석 실패: {error}");
                return;
            }

            _currentBatch.Cells.Add(velocity);
            
            string msg = $"[유속 측정] Dev={velocity.DeviceNumber}, ";
            msg += $"Status={velocity.DeviceStatus}, ";
            msg += $"Type={velocity.DeviceType}, ";
            msg += $"Velo={velocity.Velocity.ToString("0.000")}, ";
            msg += $"WaterLevel={velocity.WaterLevel.ToString("0.00")}, ";
            msg += $"Volt={velocity.BoardVolt.ToString("0.00")}, ";
            msg += $"Q={velocity.Discharge.ToString("0.00")}, ";
            msg += $"RSSI={velocity.RfRssi.ToString()}, ";
            msg += $"StdU={velocity.VelocityStandardUncertainty.ToString("0.00")}, ";
            msg += $"ExtU={velocity.VelocityExpandedUncertainty.ToString("0.00")}, ";

            TcpBrocastBuffer.WriteLog("_RF", msg);
        }

        private void HandleWind(byte[] payload)
        {
            if (_discardCurrentSequence)
            {
                ResetSequence();
                return;
            }

            if (_currentBatch is null)
            {
                TcpBrocastBuffer.WriteLog("_RF", $"0x1A 수신 전에 0x1C가 수신되어 폐기합니다.");
                return;
            }

            if (!RFProtocol.TryParseWind(payload, out WindData? wind, out string error) || wind is null)
            {
                TcpBrocastBuffer.WriteLog("_RF", $"STATION_SENDDATA_WIND 분석 실패: {error}");
                ResetSequence();
                return;
            }

            _currentBatch.Wind = wind;
            CompleteMeasurement(_currentBatch);
            ResetSequence();
        }

        private void CompleteMeasurement(MeasurementBatch batch)
        {
            if (batch.Cells.Count != batch.Info.DeviceCount)
            {
                TcpBrocastBuffer.WriteLog("_RF", $"DeviceCount 불일치: 예정={batch.Info.DeviceCount}, 수신={batch.Cells.Count}");
            }

            _lastCompletedMeasureKey = batch.Info.MeasureKey;

            TcpBrocastBuffer.WriteLog("_RF", $"[측정 완료] " + GetInfoMessage(batch.Info));

            DiagnosticsOwner.Instance.MPDSMeasDate = batch.Info.MeasureTime.ToString("yyyy-MM-dd") ?? "Unknown";
            DiagnosticsOwner.Instance.MPDSMeasTime = batch.Info.MeasureTime.ToString("HH:mm:ss") ?? "Unknown";
            DiagnosticsOwner.Instance.MPDSMeasCnt = batch.Info.DeviceCount;
            DiagnosticsOwner.Instance.MPDSDevVolt = batch.Info.CollectorVolt;
            DiagnosticsOwner.Instance.MPDSRFRssi = batch.CollectorRSSI;
            DiagnosticsOwner.Instance.MPDSWaterLvl = batch.Info.Waterlevel;

            double total = 0;
            foreach (MPDSCell cell in batch.Cells)
            {
                total += cell.Velocity;
            }

            if (batch.Info.DeviceCount != 0)
            {
                DiagnosticsOwner.Instance.MPDSSimpleVelo = total / batch.Info.DeviceCount;
            }

            bool result = DBWriter.InsertMPDS(batch);
        }

        private void HandleModemResponse(string line)
        {
            if (line.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                TcpBrocastBuffer.WriteLog("_RF", $"RF 모듈 응답: OK");
            }
            else if (line.StartsWith("+INFO", StringComparison.OrdinalIgnoreCase))
            {
                TcpBrocastBuffer.WriteLog("_RF", $"RF 모듈 정보: {line}");
                string[] values = line.Split(',');
                if (values.Length < 2) return;

                DiagnosticsOwner.Instance.MPDSRFInfo = values[1];
            }
            else if (line.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                TcpBrocastBuffer.WriteLog("_RF", $"RF 모듈 오류 응답: {line}");
            }
            else
            {
                TcpBrocastBuffer.WriteLog("_RF", $"RF 모듈 기타 응답: {line}");
            }
        }

        private void ResetSequence()
        {
            _currentBatch = null;
            _discardCurrentSequence = false;
        }
    }
}
