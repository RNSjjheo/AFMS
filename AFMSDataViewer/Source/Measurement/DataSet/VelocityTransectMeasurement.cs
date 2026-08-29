namespace AFMSDataViewer
{
    /// <summary>한 측선의 유속 및 불확도 자료입니다.</summary>
    public sealed record VelocityTransectMeasurement(
        int TransectNo,
        double Velocity,
        double Uncertainty,
        bool IsValid = true);
}
