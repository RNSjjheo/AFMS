using AFMSDll;

namespace AFMSExtraLogger
{
    public sealed record RfModemMessage
    {
        public string RfAddress;
        public int? Rssi;
        public string HexFrame;

        public RfModemMessage(string address, int? rssi, string hexFrame)
        {
            RfAddress = address;
            Rssi = rssi;
            HexFrame = hexFrame;
        }
    }

    public readonly record struct ProtocolFrame(
    byte Command,
    byte[] Payload);

    public sealed record DeviceRequestData(
        string Date,
        string Time);

    public sealed record StationInfoData(
        string PointCode,
        byte DeviceCount,
        string MeasureKey,
        DateTime MeasureTime,
        float CollectorVolt,
        float Waterlevel,
        float Reserved1,
        float Reserved2);

    public sealed record MPDSCell
    {
        public int Id;
        public int MpdsId;
        public byte DeviceNumber;
        public byte DeviceStatus;
        public MpdsDevType DeviceType;
        public float BoardVolt;
        public float WaterLevel;
        public float Velocity;
        public float Snr;
        public float Discharge;
        public float FilterVelocity;
        public float FilterDischarge;
        public ushort Opposite;
        public float Inclination;
        public short RfRssi;
        public float VelocityStandardUncertainty;
        public float VelocityExpandedUncertainty;
    }


    public sealed record WindData(
        float WindSpeed,
        float WindGust,
        float WindDirection,
        float Temperature,
        float Humidity,
        float Atmosphere);

    public sealed class MeasurementBatch
    {
        public int Id;
        public StationInfoData Info { get; }
        public List<MPDSCell> Cells { get; } = [];
        public WindData? Wind { get; set; }
        public short CollectorRSSI { get; set; }

        public MeasurementBatch(StationInfoData info)
        {
            Info = info;
        }
    }
}
