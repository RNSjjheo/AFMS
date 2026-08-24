using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AFMSDll
{
    public class PacketJsonProtocol
    {
        public const byte STX = 0xFA;
        public const byte ETX = 0xF5;
        public const byte JSON_CMD = 0xFA;
        public const int MaxDataLength = ushort.MaxValue;

        public static byte[] Encode(byte command, ReadOnlySpan<byte> data)
        {
            if (data.Length > MaxDataLength)
            {
                throw new ArgumentOutOfRangeException(nameof(data), $"데이터 길이는 {MaxDataLength}바이트를 초과할 수 없습니다.");
            }

            ushort dataLength = (ushort)data.Length;

            // STX 1 + CMD 1 + LEN 2 + DATA N + CHECKSUM 1 + ETX 1
            byte[] packet = new byte[6 + data.Length];

            packet[0] = STX;
            packet[1] = command;

            // Big Endian
            packet[2] = (byte)(dataLength >> 8);
            packet[3] = (byte)(dataLength & 0xFF);

            data.CopyTo(packet.AsSpan(4));

            int checksumIndex = 4 + data.Length;
            int etxIndex = checksumIndex + 1;

            // CMD + LEN-H + LEN-L + DATA
            packet[checksumIndex] = CalculateChecksum(packet.AsSpan(1, 3 + data.Length));

            packet[etxIndex] = ETX;

            return packet;
        }

        /// <summary>
        /// 문자열 데이터를 UTF-8 패킷으로 생성한다.
        /// </summary>
        public static byte[] EncodeText(byte command, string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);

            return Encode(command, data);
        }

        /// <summary>
        /// 객체를 JSON으로 직렬화하여 패킷으로 생성한다.
        /// </summary>
        public static byte[] EncodeJson(object value)
        {
            byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(value);

            return Encode(JSON_CMD, jsonData);
        }

        /// <summary>
        /// 체크섬 계산
        ///
        /// CMD + LEN-H + LEN-L + DATA의 합에서
        /// 하위 1바이트만 사용한다.
        /// </summary>
        public static byte CalculateChecksum(ReadOnlySpan<byte> data)
        {
            int sum = 0;

            foreach (byte value in data)
            {
                sum += value;
            }

            return unchecked((byte)sum);
        }

        internal static byte CalculateChecksum(List<byte> buffer, int offset, int count)
        {
            int sum = 0;

            for (int i = offset; i < offset + count; i++)
            {
                sum += buffer[i];
            }

            return unchecked((byte)sum);
        }
    }
}
