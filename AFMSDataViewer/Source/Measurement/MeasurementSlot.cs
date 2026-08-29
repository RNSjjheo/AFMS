using System;
using System.Collections.Generic;
using AFMSDll;

namespace AFMSDataViewer
{
    public sealed class MeasurementSlot
    {
        public MeasurementSlot(DateTime slotTime, CrossSectionDefinition crossSectionDefinition)
        {
            ArgumentNullException.ThrowIfNull(crossSectionDefinition);

            SlotTime = slotTime;
            CrossSectionDefinition = crossSectionDefinition;
            MeasurementDevices = new MeasurementDevices(this);
        }

        public DateTime SlotTime { get; }

        public CrossSectionDefinition CrossSectionDefinition { get; }

        public MeasurementDevices MeasurementDevices { get; }

        public List<DischargeMeasurement> Discharges { get; } = [];
    }
}
