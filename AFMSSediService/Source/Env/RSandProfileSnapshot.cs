using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    internal sealed record RSandProfileSnapshot(int ProfileId, string ProfileDate, string ProfileTime, string ProfileName, RSandDeviceProfile A, RSandDeviceProfile B);

    internal sealed record RSandDeviceProfile(
        string SetupFlag,
        string DeviceType,
        int CellFrom,
        int CellTo,
        double KValue,
        double BeamAngle,
        double SscA,
        double SscB)
    {
        public bool IsEnabled =>
            string.Equals(SetupFlag, "Y", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(DeviceType, "NONE", StringComparison.OrdinalIgnoreCase);
    }
}
