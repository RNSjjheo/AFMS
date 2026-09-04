using AFMSDll;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace AFMSSediService
{
    public class ChannelMasterSource
    {
        public string HeaderTable;
        public string CellTable;
        public string ReadyFlagColumn;

        public ChannelMasterSource(HydroMetherTableType type)
        {
            switch (type)
            {
                case HydroMetherTableType.RHYDROMETER1:
                    HeaderTable = FbtRHYDROMETER1.TABLE_NAME;
                    CellTable = FbtRHYDROMETER1CELL.TABLE_NAME;
                    ReadyFlagColumn = FbtRPOINT.COL_HYDROMETER1_FLAG;
                    break;
                case HydroMetherTableType.RHYDROMETER2:
                    HeaderTable = FbtRHYDROMETER2.TABLE_NAME;
                    CellTable = FbtRHYDROMETER2CELL.TABLE_NAME;
                    ReadyFlagColumn = FbtRPOINT.COL_HYDROMETER2_FLAG;
                    break;
                case HydroMetherTableType.RHYDROMETER3:
                    HeaderTable = FbtRHYDROMETER3.TABLE_NAME;
                    CellTable = FbtRHYDROMETER3CELL.TABLE_NAME;
                    ReadyFlagColumn = FbtRPOINT.COL_HYDROMETER3_FLAG;
                    break;
                default:
                    throw new InvalidOperationException($"지원하지 않는 유속계 테이블입니다. 현재 값={profile.HydroTableName}")
                    break;
                    return new ChannelMasterSource(FbtRHYDROMETER1.TABLE_NAME, FbtRHYDROMETER1CELL.TABLE_NAME, FbtRPOINT.COL_HYDROMETER1_FLAG);
                    HydroMetherTableType.RHYDROMETER1 =>,
                HydroMetherTableType.RHYDROMETER2 => new ChannelMasterSource(FbtRHYDROMETER2.TABLE_NAME, FbtRHYDROMETER2CELL.TABLE_NAME, FbtRPOINT.COL_HYDROMETER2_FLAG),
                HydroMetherTableType.RHYDROMETER3 => new ChannelMasterSource(FbtRHYDROMETER3.TABLE_NAME, FbtRHYDROMETER3CELL.TABLE_NAME, FbtRPOINT.COL_HYDROMETER3_FLAG),
                _ => throw new InvalidOperationException($"지원하지 않는 유속계 테이블입니다. 현재 값={profile.HydroTableName}")
            }
        }

        public static ChannelMasterSource FromProfile(HydroMetherTableType type)
        {

        }
    }
    internal sealed record ChannelMasterSource(string HeaderTable, string CellTable, string ReadyFlagColumn)
    {
        public static ChannelMasterSource FromProfile(SSCDeviceProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);


        }
    }
}
