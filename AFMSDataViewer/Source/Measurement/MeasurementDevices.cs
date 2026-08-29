using System;
using System.Collections.Generic;

namespace AFMSDataViewer
{
    public sealed class MeasurementDevices
    {
        private readonly MeasurementSlot _slot;
        public DateTime SlotTime => _slot.SlotTime;
        public List<VelocityMeasurement> HydroMeters { get; } = [];

        public LevelMeasurement? WaterLevelGauge { get; set; }

        public VoltageMeasurement? VoltageMeter { get; set; }
        public MeasurementDevices(MeasurementSlot slot)
        {
            ArgumentNullException.ThrowIfNull(slot);
            _slot = slot;
        }
    }
}
