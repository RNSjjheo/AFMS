using System.IO.Ports;

namespace AFMSExtraLogger
{
    public class SerialOptions
    {
        public const string SectionName = "Serial";

        public string PortName { get; set; } = "COM3";
        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;
        public int ReadBufferSize { get; set; } = 8192;
        public int WriteBufferSize { get; set; } = 2048;
        public int ReconnectDelaySeconds { get; set; } = 5;
    }
}
