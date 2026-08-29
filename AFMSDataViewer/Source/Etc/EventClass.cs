using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    public sealed class ChartSelectedEventArgs : EventArgs
    {
        public ChartSelectedEventArgs(ChartMainType chartType, string text)
        {
            ChartType = chartType;
            Text = text;
        }

        public ChartMainType ChartType { get; }

        public string Text { get; }
    }

    public sealed class TrackingTimeChangedEventArgs(DateTime time) : EventArgs
    {
        public DateTime Time { get; } = time;
    }
}
