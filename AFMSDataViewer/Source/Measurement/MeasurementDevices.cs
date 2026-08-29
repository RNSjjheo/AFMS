using System;
using System.Collections.Generic;

namespace AFMSDataViewer
{
    public sealed class MeasurementDevices
    {
        private readonly MeasurementSlot _slot;
        public DateTime SlotTime => _slot.SlotTime;
        public List<VelocityMeasurement> HydroMeters { get; } = [];

        public WaterLevelGauge WaterLevelGauge { get; }

        public PowerDevice Power { get; }
        public MeasurementDevices(MeasurementSlot slot)
        {
            ArgumentNullException.ThrowIfNull(slot);
            _slot = slot;
            WaterLevelGauge = new WaterLevelGauge(slot);
            Power = new PowerDevice(slot);
        }
    }
}
