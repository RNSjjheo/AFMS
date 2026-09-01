using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    internal sealed record QueryPeriodOption(RealtimeQueryPeriod Period)
    {
        public TimeSpan Duration => TimeSpan.FromHours((int)Period);
        public override string ToString() => $"최근 {((int)Period).ToString()}시간";
    }

    internal class MeasurementQueryRange : List<QueryPeriodOption>
    {
        public MeasurementQueryRange()
        {
            Add(new QueryPeriodOption(RealtimeQueryPeriod.Hours6));
            Add(new QueryPeriodOption(RealtimeQueryPeriod.Hours12));
            Add(new QueryPeriodOption(RealtimeQueryPeriod.Hours24));
        }
    }

}
