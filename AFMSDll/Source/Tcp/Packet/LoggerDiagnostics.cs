namespace AFMSDll
{
    public sealed class LoggerDiagnostics : _PacketBase
    {
        public LoggerDiagnostics()
        {
            JsonType = JsonPacketType.Diagnotics;
        }

        public DateTime ServiceStartTime { get; set; }
        public long MemoryUsageBytes { get; set; }
        public DateTime? LastMeasurementTime { get; set; }
    }
}
