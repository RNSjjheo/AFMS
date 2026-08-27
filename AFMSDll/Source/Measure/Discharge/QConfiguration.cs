using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class QConfiguration
    {
        internal QConfiguration(DischargeMethod method)
        {
            Method = method;
        }

        public int MethodConfigId { get; set; } = -1;
        public int TransectConfigId { get; set; } = -1;
        public MeasurementDeviceType DeviceType { get; set; } = MeasurementDeviceType.None;
        public int DeviceId { get; set; } = -1;
        public DischargeMethod Method { get; }
        public CrossSection CrossSection { get; } = new();
    }
}
