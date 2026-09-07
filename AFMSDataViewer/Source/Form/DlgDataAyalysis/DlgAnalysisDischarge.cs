using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    internal class DlgAnalysisDischarge : DlgDataAnalysis
    {
        public DlgAnalysisDischarge(RealtimeChartSeries series, RealtimeChartPoint point, int? tranNo = null, MeasurementDataHub? hub = null, VelocityMeasurement? velocityMeasurement = null, double? min = null, double? max = null, Tracking? linkedTracking = null) : base(series, point, tranNo, hub, velocityMeasurement, min, max, linkedTracking)
        {
            SourceChartType = ChartMainType.Discharge;
        }

        protected override void ConfigureAnalysisTabs()
        {
            //uiTabs.TabPages.AddRange(CreateAnalysisPages());
        }
    }
}
