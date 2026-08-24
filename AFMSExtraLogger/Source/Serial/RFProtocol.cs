using AFMSDll;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace AFMSExtraLogger
{
    public enum RFProtocolCommand : byte
    {
        DeviceRequest = 0x14,
        DeviceResponse = 0x15,
        StationInfo = 0x1A,
        Velocity = 0x1B,
        Wind = 0x1C
    }

    public static class RFProtocol
    {
        public const byte Stx = 0xFA;
        public const byte Etx = 0xF5;
        public const byte FixedCrc = 0x00;
        public const byte StationDeviceNumber = 99;
        public const string KEY_DATA = "+DATA";
        public const string KEY_DATA2 = "+DATA2";
        public static bool TryParseModemLine(string line, out RfModemMessage? message, out string error)
        {
            message = null;
            error = string.Empty;

            string value = line.Trim();
            string[] fields = value.Split(',', StringSplitOptions.TrimEntries);

            if (fields.Length == 3 && fields[0].Equals(KEY_DATA, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidRfAddress(fields[1]))
                {
                    error = $"잘못된 RF 주소: {fields[1]}";
                    return false;
                }

                if (!IsValidHex(fields[2]))
                {
                    error = "HEXFRAME이 올바른 HEX 문자열이 아닙니다.";
                    return false;
                }

                message = new RfModemMessage(fields[1], null, fields[2]);
                return true;
            }

            if (fields.Length == 4 && fields[0].Equals(KEY_DATA2, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidRfAddress(fields[1]))
                {
                    error = $"잘못된 RF 주소: {fields[1]}";
                    return false;
                }

                if (!int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rssi))
                {
                    error = $"RSSI가 정수가 아닙니다: {fields[2]}";
                    return false;
                }

                if (!IsValidHex(fields[3]))
                {
                    error = "HEXFRAME이 올바른 HEX 문자열이 아닙니다.";
                    return false;
                }

                message = new RfModemMessage(fields[1], rssi, fields[3]);
                return true;
            }

            error = "+DATA 또는 +DATA2 형식이 아닙니다.";
            return false;
        }

        public static bool TryParseFrame(string hexFrame, out ProtocolFrame frame, out string error)
        {
            frame = default;
            error = string.Empty;

            byte[] bytes;
            try
            {
                bytes = Convert.FromHexString(hexFrame);
            }
            catch (FormatException)
            {
                error = "HEXFRAME 변환에 실패했습니다.";
                return false;
            }

            if (bytes.Length < 6)
            {
                error = $"프레임 길이가 너무 짧습니다: {bytes.Length}";
                return false;
            }

            if (bytes[0] != Stx)
            {
                error = $"STX 오류: 0x{bytes[0]:X2}";
                return false;
            }

            ushort payloadLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(2, 2));
            int expectedLength = 6 + payloadLength;

            if (bytes.Length != expectedLength)
            {
                error = $"프레임 길이 오류: 수신={bytes.Length}, 예상={expectedLength}, LEN={payloadLength}";
                return false;
            }

            int crcIndex = 4 + payloadLength;
            int etxIndex = crcIndex + 1;

            if (bytes[crcIndex] != FixedCrc)
            {
                error = $"CRC 오류: 0x{bytes[crcIndex]:X2}, 규약상 0x00이어야 합니다.";
                return false;
            }

            if (bytes[etxIndex] != Etx)
            {
                error = $"ETX 오류: 0x{bytes[etxIndex]:X2}";
                return false;
            }

            frame = new ProtocolFrame(bytes[1], bytes.AsSpan(4, payloadLength).ToArray());

            return true;
        }

        public static string BuildHexFrame(byte command, ReadOnlySpan<byte> payload)
        {
            byte[] frame = new byte[6 + payload.Length];

            frame[0] = Stx;
            frame[1] = command;
            BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2, 2), checked((ushort)payload.Length));
            payload.CopyTo(frame.AsSpan(4));
            frame[4 + payload.Length] = FixedCrc;
            frame[5 + payload.Length] = Etx;

            return Convert.ToHexString(frame);
        }

        public static string BuildAtUDataCommand(string rfAddress, string hexFrame)
        {
            return $"AT+UDATA={rfAddress},{hexFrame}\r";
        }

        public static bool TryParseDeviceRequest(ReadOnlySpan<byte> payload, out DeviceRequestData? data, out string error)
        {
            data = null;
            error = string.Empty;

            if (payload.Length != 14)
            {
                error = $"DEVICE_REQ PAYLOAD 길이 오류: {payload.Length}, 예상=14";
                return false;
            }

            data = new DeviceRequestData(Encoding.ASCII.GetString(payload[..8]), Encoding.ASCII.GetString(payload.Slice(8, 6)));

            return true;
        }

        public static bool TryParseStationInfo(ReadOnlySpan<byte> payload, out StationInfoData? data, out string error)
        {
            data = null;
            error = string.Empty;

            if (payload.Length != 30)
            {
                error = $"STATION_SENDDATA_INFO PAYLOAD 길이 오류: {payload.Length}, 예상=30";
                return false;
            }

            string pointCode = Encoding.ASCII.GetString(payload[..7]);
            byte deviceCount = payload[7];
            ReadOnlySpan<byte> rawTime = payload.Slice(8, 6);

            DateTime meastime = TryConvertMeasureTime(rawTime)??DateTime.Now;
            data = new StationInfoData(
                pointCode,
                deviceCount,
                Convert.ToHexString(rawTime),
                meastime,
                ReadSingleLittleEndian(payload.Slice(14, 4)),
                ReadSingleLittleEndian(payload.Slice(18, 4)),
                ReadSingleLittleEndian(payload.Slice(22, 4)),
                ReadSingleLittleEndian(payload.Slice(26, 4)));

            return true;
        }

        public static bool TryParseVelocity(ReadOnlySpan<byte> payload, out MPDSCell? data, out string error)
        {
            data = null;
            error = string.Empty;

            // 문서에는 LEN=37로 기재되어 있으나 전체 필드 합계는 39바이트입니다.
            // 두 형식 모두 수신할 수 있도록 처리합니다.
            if (payload.Length is not (37 or 39))
            {
                error = $"STATION_SENDDATA_VELO PAYLOAD 길이 오류: {payload.Length}, 허용=37 또는 39";
                return false;
            }

            data = new MPDSCell();
            data.DeviceNumber = payload[0];
            data.DeviceStatus = payload[1];
            data.BoardVolt = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2)) / 100f;
            data.DeviceType = EnumPaser.ConvertingMpdsDevType(payload[4]);
            data.WaterLevel = ReadSingleLittleEndian(payload.Slice(5, 4));
            data.Velocity = ReadSingleLittleEndian(payload.Slice(9, 4));
            data.Snr = ReadSingleLittleEndian(payload.Slice(13, 4));
            data.Discharge = ReadSingleLittleEndian(payload.Slice(17, 4));
            data.FilterVelocity = ReadSingleLittleEndian(payload.Slice(21, 4));
            data.FilterDischarge = ReadSingleLittleEndian(payload.Slice(25, 4));
            data.Opposite = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(29, 2));
            data.Inclination = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(31, 2)) / 100f;
            data.RfRssi = BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(33, 2));
            data.VelocityStandardUncertainty = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(35, 2)) / 100f;
            data.VelocityExpandedUncertainty = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(37, 2)) / 100f;
              
            return true;
        }

        public static bool TryParseWind(ReadOnlySpan<byte> payload, out WindData? data, out string error)
        {
            data = null;
            error = string.Empty;

            if (payload.Length != 24)
            {
                error = $"STATION_SENDDATA_WIND PAYLOAD 길이 오류: {payload.Length}, 예상=24";
                return false;
            }

            data = new WindData(
                ReadSingleLittleEndian(payload.Slice(0, 4)),
                ReadSingleLittleEndian(payload.Slice(4, 4)),
                ReadSingleLittleEndian(payload.Slice(8, 4)),
                ReadSingleLittleEndian(payload.Slice(12, 4)),
                ReadSingleLittleEndian(payload.Slice(16, 4)),
                ReadSingleLittleEndian(payload.Slice(20, 4)));

            return true;
        }

        private static float ReadSingleLittleEndian(ReadOnlySpan<byte> value)
        {
            int raw = BinaryPrimitives.ReadInt32LittleEndian(value);
            return BitConverter.Int32BitsToSingle(raw);
        }

        private static DateTime? TryConvertMeasureTime(ReadOnlySpan<byte> value)
        {
            if (value.Length != 6) return null;

            // 규약에는 y,m,d,h,m,s라고만 되어 있어 연도 기준값이 명시되어 있지 않습니다.
            // 일반적인 2자리 연도로 보고 2000 + y로 표시합니다.
            // 중복 판정은 이 변환값이 아니라 원본 6바이트 MeasureKey를 사용합니다.
            int year = 2000 + value[0];
            int month = value[1];
            int day = value[2];
            int hour = value[3];
            int minute = value[4];
            int second = value[5];

            try
            {
                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static bool IsValidRfAddress(string value)
        {
            return value.Length == 16 && IsValidHex(value);
        }

        private static bool IsValidHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length % 2 != 0) return false;

            foreach (char ch in value)
            {
                bool isHex =
                    ch is >= '0' and <= '9' or
                    >= 'A' and <= 'F' or
                    >= 'a' and <= 'f';

                if (!isHex) return false;
            }

            return true;
        }
    }
}
