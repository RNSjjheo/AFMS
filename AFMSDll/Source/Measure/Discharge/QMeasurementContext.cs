using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class QTransectMeasurement
    {
        public int No { get; set; }
        public double Velocity { get; set; }
        public double? PositionX { get; set; }
        public double? PositionY { get; set; }
        public double? StandardUncertainty { get; set; }
        public double? ExpandedUncertainty { get; set; }
    }

    public sealed class QMeasurementContext
    {
        public string DeviceName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public _FBTableBase? Table { get; set; }
        public bool HasSource { get; set; }
        public int SourceId { get; set; } = -1;
        public DateOnly SourceDate { get; set; }
        public TimeOnly SourceTime { get; set; }
        public DateTime? LastCalculatedSourceTime { get; set; }
        public bool HasWaterLevel { get; set; }
        public double WaterLevel { get; set; }
        public DateOnly WaterLevelDate { get; set; }
        public TimeOnly WaterLevelTime { get; set; }
        public List<QTransectMeasurement> Transects { get; } = new();
    }

}
