using AFMSDll;

namespace AFMSDataViewer
{
    internal sealed class RealtimeChartLegendController : IDisposable
    {
        private readonly ChartMainType chartType;
        private readonly TableLayoutPanel layout;
        private readonly RowStyle legendRow;
        private readonly HashSet<string> hiddenSeries = new(StringComparer.Ordinal);
        private string? singleSeriesKey;
        private bool disposed;

        public RealtimeChartLegend View { get; } = new();
        public event EventHandler? VisibilityChanged;

        public RealtimeChartLegendController(ChartMainType chartType, TableLayoutPanel layout, int rowIndex)
        {
            this.chartType = chartType;
            this.layout = layout;
            legendRow = layout.RowStyles[rowIndex];
            View.Dock = DockStyle.Fill;
            View.Visible = false;
            View.PreferredHeightChanged += OnPreferredHeightChanged;
            View.SeriesToggleRequested += OnSeriesToggleRequested;
            layout.Controls.Add(View, 0, rowIndex);
            layout.SizeChanged += OnLayoutSizeChanged;
            OnPreferredHeightChanged(this, EventArgs.Empty);
        }

        public void Update(IEnumerable<RealtimeChartSeries> series, int? velocityDeviceId)
        {
            RealtimeChartSeries[] selected = series.ToArray();
            singleSeriesKey = selected.Length == 1 ? GetKey(selected[0], velocityDeviceId) : null;
            if (selected.Length <= 1)
            {
                View.SetItems(Array.Empty<RealtimeChartLegendItem>());
                View.Visible = false;
                return;
            }

            View.Visible = true;
            View.SetItems(selected.Select(source =>
            {
                string key = GetKey(source, velocityDeviceId);
                return new RealtimeChartLegendItem(key, GetText(source), source.Color, !hiddenSeries.Contains(key));
            }));
        }

        public bool IsVisible(RealtimeChartSeries series, int? velocityDeviceId)
        {
            string key = GetKey(series, velocityDeviceId);
            // A solo selection has no legend to restore it; always show it without
            // discarding the user's hidden state for the multi-series selection.
            return key == singleSeriesKey || !hiddenSeries.Contains(key);
        }

        // A failed/empty query clears the view, not the user's visibility choices.
        public void Clear()
        {
            singleSeriesKey = null;
            View.SetItems(Array.Empty<RealtimeChartLegendItem>());
            View.Visible = false;
        }

        private void OnPreferredHeightChanged(object? sender, EventArgs e)
        {
            // The plot occupies the remaining percentage row.
            legendRow.Height = View.PreferredHeight;
        }

        private void OnLayoutSizeChanged(object? sender, EventArgs e)
        {
            // Width-driven wrapping can change the row height during a layout
            // pass. Apply that new height once the resize pass has completed.
            layout.PerformLayout();
        }

        private void OnSeriesToggleRequested(object? sender, LegendSeriesToggleEventArgs e)
        {
            if (!hiddenSeries.Add(e.Key)) hiddenSeries.Remove(e.Key);
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        private string GetKey(RealtimeChartSeries series, int? velocityDeviceId)
        {
            if (chartType == ChartMainType.Discharge)
                return $"discharge|{series.DeviceType}|{series.DeviceId}|{series.DischargeMethod}";
            if (chartType == ChartMainType.Velocity)
                return $"velocity|{velocityDeviceId}|{series.Name}";
            return $"{chartType}|{series.SecondaryAxis}|{series.Name}";
        }

        private string GetText(RealtimeChartSeries series)
        {
            if (series.SecondaryAxis) return series.Name;
            if (chartType == ChartMainType.Velocity && series.Name.EndsWith("번 측선", StringComparison.Ordinal))
                return series.Name[..^3];
            if (chartType == ChartMainType.Discharge && !string.IsNullOrEmpty(series.DischargeMethod))
                return Enum.TryParse(series.DischargeMethod, true, out DischargeMethod method)
                    ? EnumPaser.GetKorString(method) : series.DischargeMethod;
            if (chartType == ChartMainType.Level && series.Name == "수위계") return "1번";
            return series.Name;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            View.PreferredHeightChanged -= OnPreferredHeightChanged;
            View.SeriesToggleRequested -= OnSeriesToggleRequested;
            layout.SizeChanged -= OnLayoutSizeChanged;
            VisibilityChanged = null;
            View.Dispose();
        }
    }
}
