namespace AFMSDll
{
    public enum ChartMainType
    {
        Velocity,
        Level,
        Discharge,
        VTH
    }

    public sealed record RealtimeChartPoint(DateTime Time, double Value, bool IsMissing = false);

    public sealed record RealtimeChartSeries(
        string Name,
        Color Color,
        IReadOnlyList<RealtimeChartPoint> Points,
        bool SecondaryAxis = false,
        string? Key = null,
        string? LegendText = null,
        string? DeviceType = null,
        int? DeviceId = null,
        string? DischargeMethod = null,
        string? MeterType = null);

    public sealed class RealtimeChartPointEventArgs(
        RealtimeChartSeries series,
        RealtimeChartPoint point) : EventArgs
    {
        public RealtimeChartSeries Series { get; } = series;
        public RealtimeChartPoint Point { get; } = point;
    }
}
