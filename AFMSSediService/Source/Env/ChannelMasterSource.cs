using AFMSDll;
using System.Data;

namespace AFMSSediService
{
    internal sealed record ChannelMasterSource(
        int DeviceNumber,
        string HeaderTable,
        string CellTable,
        string ReadyFlagColumn)
    {
        public static ChannelMasterSource LoadFromRSetup()
        {
            string sql = $"SELECT {FbtSETUP.COL_PK2}, {FbtSETUP.COL_VALUE01}";
            sql += $" FROM {FbtSETUP.TABLE_NAME}";
            sql += $" WHERE {FbtSETUP.COL_PK1} = 10";
            sql += $" AND {FbtSETUP.COL_PK2} IN (2, 3, 5)";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"RSETUP ChannelMaster 설정 조회에 실패했습니다.\n{error}");

            List<int> configured = db.Results.Rows.Cast<DataRow>()
                .Where(row => string.Equals(
                    Convert.ToString(row[FbtSETUP.COL_VALUE01])?.Trim(),
                    "ChannelMaster",
                    StringComparison.OrdinalIgnoreCase))
                .Select(row => Convert.ToInt32(row[FbtSETUP.COL_PK2]))
                .ToList();

            if (configured.Count != 1)
                throw new InvalidOperationException(
                    $"RSETUP에는 SSC 대상 ChannelMaster가 정확히 1대 설정되어야 합니다. 현재 설정 수={configured.Count}");

            return configured[0] switch
            {
                2 => new ChannelMasterSource(1, "RHYDROMETER1", "RHYDROMETER1CELL", FbtRPOINT.COL_HYDROMETER1_FLAG),
                3 => new ChannelMasterSource(2, "RHYDROMETER2", "RHYDROMETER2CELL", FbtRPOINT.COL_HYDROMETER2_FLAG),
                5 => new ChannelMasterSource(3, "RHYDROMETER3", "RHYDROMETER3CELL", FbtRPOINT.COL_HYDROMETER3_FLAG),
                _ => throw new InvalidOperationException("지원하지 않는 RSETUP ChannelMaster 위치입니다.")
            };
        }
    }
}
