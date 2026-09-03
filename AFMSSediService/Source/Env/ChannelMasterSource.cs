using AFMSDll;
namespace AFMSSediService
{
    internal sealed record ChannelMasterSource(string HeaderTable, string CellTable, string ReadyFlagColumn)
    {
        public static ChannelMasterSource FromProfile(SSCDeviceProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            return profile.HydroTableName switch
            {
                HydroMetherTableType.RHYDROMETER1 => new ChannelMasterSource(FbtRHYDROMETER1.TABLE_NAME, FbtRHYDROMETER1CELL.TABLE_NAME, FbtRPOINT.COL_HYDROMETER1_FLAG),
                HydroMetherTableType.RHYDROMETER2 => new ChannelMasterSource(FbtRHYDROMETER2.TABLE_NAME, FbtRHYDROMETER2CELL.TABLE_NAME, FbtRPOINT.COL_HYDROMETER2_FLAG),
                HydroMetherTableType.RHYDROMETER3 => new ChannelMasterSource(FbtRHYDROMETER3.TABLE_NAME, FbtRHYDROMETER3CELL.TABLE_NAME, FbtRPOINT.COL_HYDROMETER3_FLAG),
                _ => throw new InvalidOperationException($"지원하지 않는 유속계 테이블입니다. 현재 값={profile.HydroTableName}")
            };
        }
    }
}
