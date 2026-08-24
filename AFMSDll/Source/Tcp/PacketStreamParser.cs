using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class PacketStreamParser
    {
        private readonly List<byte> _buffer = new();
        public int BufferedCount => _buffer.Count;

        public void Append(ReadOnlySpan<byte> data)
        {
            foreach (byte value in data)
            {
                _buffer.Add(value);
            }
        }

        public PacketParseResult TryReadPacket(out TcpPacket? packet, out string? error)
        {
            packet = null;
            error = null;

            if (_buffer.Count == 0) return PacketParseResult.NeedMoreData;

            // STX 위치를 찾는다.
            int stxIndex = _buffer.IndexOf(PacketJsonProtocol.STX);

            if (stxIndex < 0)
            {
                int discardedCount = _buffer.Count;

                _buffer.Clear();

                error = $"STX를 찾지 못해 {discardedCount}바이트를 폐기했습니다.";

                return PacketParseResult.InvalidData;
            }

            // STX 앞에 불필요한 데이터가 있으면 제거
            if (stxIndex > 0)
            {
                _buffer.RemoveRange(0, stxIndex);

                error = $"STX 앞의 {stxIndex}바이트를 폐기했습니다.";

                return PacketParseResult.InvalidData;
            }

            // STX + CMD + LEN 2바이트가 들어올 때까지 대기
            if (_buffer.Count < 4) return PacketParseResult.NeedMoreData;

            byte command = _buffer[1];

            // Big Endian 길이 해석
            int dataLength =
                (_buffer[2] << 8) |
                _buffer[3];

            if (dataLength > PacketJsonProtocol.MaxDataLength)
            {
                _buffer.RemoveAt(0);

                error = $"허용되지 않는 데이터 길이입니다. Length={dataLength}";

                return PacketParseResult.InvalidData;
            }

            // STX 1 + CMD 1 + LEN 2 + DATA + CHECKSUM 1 + ETX 1
            int totalPacketLength = 6 + dataLength;

            if (_buffer.Count < totalPacketLength)
            {
                return PacketParseResult.NeedMoreData;
            }

            int dataIndex = 4;
            int checksumIndex = dataIndex + dataLength;
            int etxIndex = checksumIndex + 1;

            if (_buffer[etxIndex] != PacketJsonProtocol.ETX)
            {
                byte invalidEtx = _buffer[etxIndex];

                // 다음 STX를 다시 찾을 수 있도록 첫 바이트만 제거
                _buffer.RemoveAt(0);

                error = $"ETX 오류: 수신=0x{invalidEtx:X2}, 예상=0x{PacketJsonProtocol.ETX:X2}";

                return PacketParseResult.InvalidData;
            }

            byte receivedChecksum = _buffer[checksumIndex];

            // CMD + LEN-H + LEN-L + DATA
            byte calculatedChecksum = PacketJsonProtocol.CalculateChecksum(_buffer, 1, 3 + dataLength);

            if (receivedChecksum != calculatedChecksum)
            {
                _buffer.RemoveAt(0);

                error = $"Checksum 오류: 수신=0x{receivedChecksum:X2}, 계산=0x{calculatedChecksum:X2}";

                return PacketParseResult.InvalidData;
            }

            byte[] data = new byte[dataLength];

            if (dataLength > 0)
            {
                _buffer.CopyTo(dataIndex, data, 0, dataLength);
            }

            _buffer.RemoveRange(0, totalPacketLength);

            packet = new TcpPacket(command, data);

            return PacketParseResult.PacketReceived;
        }
    }
}
