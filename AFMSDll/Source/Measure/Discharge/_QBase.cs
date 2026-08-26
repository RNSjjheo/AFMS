using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public abstract class _QBase
    {
        public int Id { get; set; } = -1;
        public MeasurementDeviceType DeviceType { get; set; } = MeasurementDeviceType.None;
        public int DeviceId { get; set; } = -1;
        public int DischargeConfigId { get; set; } = -1;
        public DateOnly MeasureDate { get; set; }
        public TimeOnly MeasureTime { get; set; }
        public double Value { get; set; }
        public double Uncertainty { get; set; }
        public DischargeMethod Method { get; }
        public int MethodConfigId { get; set; } = -1;
        public CrossSection CrossSection { get; } = new();

        protected _QBase(DischargeMethod method)
        {
            Method = method;
        }
    }
}
