namespace AFMSDataViewer
{
    internal sealed class RealtimeChartLegendController : IDisposable
    {
        private readonly ChartMainType chartType;
        private readonly HashSet<string> hiddenSeries = new(StringComparer.Ordinal);
        private string? singleSeriesKey;
        private bool disposed;

        public RealtimeChartLegend View { get; } = new();
        public event EventHandler? VisibilityChanged;

        public RealtimeChartLegendController(ChartMainType chartType, Control host)
        {
            this.chartType = chartType;
            View.Dock = DockStyle.Fill;
            View.Visible = false;
            View.SeriesToggleRequested += OnSeriesToggleRequested;
            host.Controls.Add(View);
        }

        public void Update(IEnumerable<RealtimeChartSeries> series, int? velocityDeviceId)
        {
            RealtimeChartSeries[] selected = series.ToArray();
            singleSeriesKey = selected.Length == 1 ? GetKey(selected[0], velocityDeviceId) : null;
            if (chartType != ChartMainType.Discharge || selected.Length == 0)
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

        private void OnSeriesToggleRequested(object? sender, LegendSeriesToggleEventArgs e)
        {
            bool isVisible;
            if (hiddenSeries.Add(e.Key))
            {
                isVisible = false;
            }
            else
            {
                hiddenSeries.Remove(e.Key);
                isVisible = true;
            }
            View.SetItemVisibility(e.Key, isVisible);
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        private string GetKey(RealtimeChartSeries series, int? velocityDeviceId)
        {
            return series.Key ?? $"{chartType}|{velocityDeviceId}|{series.SecondaryAxis}|{series.Name}";
        }

        private string GetText(RealtimeChartSeries series)
        {
            return series.LegendText ?? series.Name;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            View.SeriesToggleRequested -= OnSeriesToggleRequested;
            VisibilityChanged = null;
            View.Dispose();
        }
    }
}
