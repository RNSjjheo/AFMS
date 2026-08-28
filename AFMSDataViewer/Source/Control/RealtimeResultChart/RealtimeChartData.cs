namespace AFMSDataViewer
{
    internal sealed record RealtimeChartPoint(DateTime Time, double Value, bool IsMissing = false);

    internal sealed record RealtimeChartSeries(string Name, Color Color, List<RealtimeChartPoint> Points,
        bool SecondaryAxis = false, string? DeviceType = null, int? DeviceId = null,
        string? DischargeMethod = null, string? MeterType = null);
}
