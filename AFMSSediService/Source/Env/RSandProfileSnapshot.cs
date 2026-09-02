using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    internal sealed record SSCProfileSnapshot(int ProfileId, string ProfileDate, string ProfileTime, SSCDeviceProfile Device);

    internal sealed record SSCDeviceProfile(string DeviceType, AFMSDll.HydroMetherTableType HydroTableName, int CellFrom, int CellTo, double KValue, double BeamAngle,
        double SscA, double SscB);
}
