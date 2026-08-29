using AFMSDll;

namespace AFMSDataViewer
{
    /// <summary>
    /// 영구 UI 설정이 마련되기 전까지 DataViewer 차트의 Y축 범위를 보관합니다.
    /// Minimum 또는 Maximum이 null이면 해당 차트는 자동 범위를 사용합니다.
    /// </summary>
    public static class DataViewerChartSettings
    {
        public static ChartAxisRange Velocity { get; } = new();
        public static ChartAxisRange Level { get; } = new(0.9, 1.3);
        public static ChartAxisRange Discharge { get; } = new();
        public static ChartAxisRange Voltage { get; } = new();

        public static ChartAxisRange GetAxisRange(ChartMainType chartType) => chartType switch
        {
            ChartMainType.Velocity => Velocity,
            ChartMainType.Level => Level,
            ChartMainType.Discharge => Discharge,
            _ => Voltage
        };
    }

    public sealed class ChartAxisRange
    {
        public ChartAxisRange(double? minimum = null, double? maximum = null)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public double? Minimum { get; set; }
        public double? Maximum { get; set; }

        public bool TryGetFixedRange(out double minimum, out double maximum)
        {
            minimum = Minimum ?? 0D;
            maximum = Maximum ?? 0D;
            return Minimum.HasValue && Maximum.HasValue &&
                double.IsFinite(minimum) && double.IsFinite(maximum) && minimum < maximum;
        }
    }
}
