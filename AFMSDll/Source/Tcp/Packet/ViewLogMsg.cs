namespace AFMSDll
{
    public sealed class ViewLogMsg : _PacketBase
    {
        public ViewLogMsg()
        {
            JsonType = JsonPacketType.ViewerLogMsg;
        }

        public string LogHost { get; set; } = string.Empty;
        public string LogLevel { get; set; } = string.Empty;
        public string LogMsg { get; set; } = string.Empty;
        public DateTime LogTime { get; set; }
    }
}
