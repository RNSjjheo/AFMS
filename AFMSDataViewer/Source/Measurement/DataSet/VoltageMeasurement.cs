namespace AFMSDataViewer
{
    /// <summary>MeasurementDataHub에 전달되는 슬롯별 전원 전압 자료입니다.</summary>
    public sealed record VoltageMeasurement(
        DateTime Time,
        string DeviceKey,
        double? InputVoltage,
        double? OutputVoltage,
        bool IsInputValid = true,
        bool IsOutputValid = true) : IRealtimeMeasurement;
}
