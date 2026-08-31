using System.Drawing.Drawing2D;

namespace AFMSDataViewer
{
    internal sealed record RealtimeChartLegendItem(string Key, string Text, Color Color, bool IsVisible);

    internal sealed class LegendSeriesToggleEventArgs(string key) : EventArgs
    {
        public string Key { get; } = key;
    }

    // Chart-independent legend: a single row clipped by the available width.
    internal sealed class RealtimeChartLegend : Control
    {
        private readonly ToolTip toolTip = new();
        private RealtimeChartLegendItem[] items = [];
        private int itemWidth;
        private int rowCount;
        private int preferredHeight;
        private int hoveredIndex = -1;

        public event EventHandler? PreferredHeightChanged;
        public event EventHandler<LegendSeriesToggleEventArgs>? SeriesToggleRequested;

        public int PreferredHeight => preferredHeight;
        private int RowHeight => ScalePixels(rowCount == 1 ? 22 : 28);
        private int Inset => ScalePixels(8);
        private int MarkerWidth => ScalePixels(36);
        private int ScalePixels(int value) => (int)Math.Round(value * DeviceDpi / 96D);

        public RealtimeChartLegend()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
            BackColor = Color.White;
            ForeColor = Color.FromArgb(30, 41, 59);
            Font = new Font("맑은 고딕", 9F);
            Margin = Padding.Empty;
            TabStop = false;
        }

        public void SetItems(IEnumerable<RealtimeChartLegendItem> newItems)
        {
            RealtimeChartLegendItem[] next = newItems.ToArray();
            if (items.SequenceEqual(next)) return;
            items = next;
            hoveredIndex = -1;
            toolTip.SetToolTip(this, null);
            UpdateLayout();
        }

        public void SetItemVisibility(string key, bool isVisible)
        {
            int index = Array.FindIndex(items, item => item.Key == key);
            if (index < 0 || items[index].IsVisible == isVisible) return;
            items[index] = items[index] with { IsVisible = isVisible };
            Invalidate(GetItemBounds(index));
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateLayout();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            UpdateLayout();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            rowCount = items.Length == 0 ? 0 : 1;
            int availableWidth = Math.Max(0, ClientSize.Width - Inset * 2);
            itemWidth = items.Length == 0 ? 0 : Math.Max(1, availableWidth / items.Length);
            int height = rowCount == 0 ? 0 : RowHeight + ScalePixels(2);
            if (preferredHeight != height)
            {
                preferredHeight = height;
                PreferredHeightChanged?.Invoke(this, EventArgs.Empty);
            }
            Invalidate();
        }

        private Rectangle GetItemBounds(int index) => new(
            Inset + index * itemWidth,
            ScalePixels(2), itemWidth, RowHeight);

        private Rectangle GetMarkerBounds(int index)
        {
            Rectangle bounds = GetItemBounds(index);
            return new Rectangle(bounds.X, bounds.Y, MarkerWidth, bounds.Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            for (int index = 0; index < items.Length; index++)
            {
                Rectangle bounds = GetItemBounds(index);
                if (!bounds.IntersectsWith(ClientRectangle)) continue;
                RealtimeChartLegendItem item = items[index];
                Color markerColor = item.IsVisible ? item.Color : Color.FromArgb(156, 163, 175);
                Color textColor = item.IsVisible ? ForeColor : Color.FromArgb(156, 163, 175);
                int centerY = bounds.Y + bounds.Height / 2;
                using Pen pen = new(markerColor, ScalePixels(2));
                using SolidBrush brush = new(markerColor);
                e.Graphics.DrawLine(pen, bounds.X + ScalePixels(2), centerY, bounds.X + ScalePixels(28), centerY);
                e.Graphics.FillEllipse(brush, bounds.X + ScalePixels(11), centerY - ScalePixels(4), ScalePixels(8), ScalePixels(8));
                Rectangle textBounds = new(bounds.X + MarkerWidth, bounds.Y,
                    bounds.Width - MarkerWidth - ScalePixels(4), bounds.Height);
                TextRenderer.DrawText(e.Graphics, item.Text, Font, textBounds, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;
            for (int index = 0; index < items.Length; index++)
            {
                if (!GetMarkerBounds(index).Contains(e.Location)) continue;
                SeriesToggleRequested?.Invoke(this, new LegendSeriesToggleEventArgs(items[index].Key));
                return;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = -1;
            for (int i = 0; i < items.Length; i++)
            {
                if (!GetItemBounds(i).Contains(e.Location)) continue;
                index = i;
                break;
            }
            Cursor = index >= 0 && GetMarkerBounds(index).Contains(e.Location) ? Cursors.Hand : Cursors.Default;
            if (hoveredIndex == index) return;
            hoveredIndex = index;
            toolTip.SetToolTip(this, index < 0 ? null : $"{items[index].Text}\n마커 더블클릭: 그래프 숨기기/표시");
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoveredIndex = -1;
            Cursor = Cursors.Default;
            toolTip.SetToolTip(this, null);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
