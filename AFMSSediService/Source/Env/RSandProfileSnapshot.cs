using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    internal sealed record SSCProfileSnapshot(
        int ProfileId,
        string ProfileDate,
        string ProfileTime,
        string ProfileName,
        RSandDeviceProfile Device);

    internal sealed record RSandDeviceProfile(
        string DeviceType,
        int CellFrom,
        int CellTo,
        double KValue,
        double BeamAngle,
        double SscA,
        double SscB)
    {
        public bool IsEnabled =>
            !string.Equals(DeviceType, "NONE", StringComparison.OrdinalIgnoreCase);
    }
}
