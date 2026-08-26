using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class QTransectMeasurement
    {
        public int No { get; internal set; }
        public double Velocity { get; internal set; }
        public double? PositionX { get; internal set; }
        public double? PositionY { get; internal set; }
        public double? StandardUncertainty { get; internal set; }
        public double? ExpandedUncertainty { get; internal set; }
    }

    public sealed class QMeasurementContext
    {
        public string DeviceName { get; internal set; } = string.Empty;
        public string TableName { get; internal set; } = string.Empty;
        public _FBTableBase? Table { get; internal set; }
        public bool HasSource { get; internal set; }
        public int SourceId { get; internal set; } = -1;
        public DateOnly SourceDate { get; internal set; }
        public TimeOnly SourceTime { get; internal set; }
        public DateTime? LastCalculatedSourceTime { get; internal set; }
        public bool HasWaterLevel { get; internal set; }
        public double WaterLevel { get; internal set; }
        public DateOnly WaterLevelDate { get; internal set; }
        public TimeOnly WaterLevelTime { get; internal set; }
        public List<QTransectMeasurement> Transects { get; } = new();
    }

}
