using System;
using System.Collections.Generic;
using System.Text;
using AFMSDll;

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
}
