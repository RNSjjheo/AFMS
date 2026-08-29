namespace AFMSDataViewer
{
    /// <summary>MeasurementDataHub에 전달되는 슬롯별 수위 자료입니다.</summary>
    public sealed record LevelMeasurement(
        DateTime Time,
        string DeviceKey,
        double Value,
        bool IsValid = true) : IRealtimeMeasurement;
}
