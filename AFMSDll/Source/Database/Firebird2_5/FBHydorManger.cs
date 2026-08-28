using System;
using System.Data;

namespace AFMSDll
{
    public static class FBHydorManger
    {
        public static void SyncAdd()
        {
            string sql = $"SELECT {FbtSETUP.COL_PK2},";
            sql += "\n" + $"{FbtSETUP.COL_VALUE01},";
            sql += "\n" + $"{FbtSETUP.COL_VALUE02},";
            sql += "\n" + $"{FbtSETUP.COL_VALUE05},";
            sql += "\n" + $"{FbtSETUP.COL_VALUE11}";
            sql += "\n" + $"FROM {FbtSETUP.TABLE_NAME}";
            sql += "\n" + $"WHERE {FbtSETUP.COL_PK1} = 10";
            sql += "\n" + $"AND {FbtSETUP.COL_PK2} IN (2, 3, 5)";
            sql += "\n" + $"ORDER BY {FbtSETUP.COL_PK2}";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                int pk2 = Convert.ToInt32(row[FbtSETUP.COL_PK2]);
                string value01 = Convert.ToString(row[FbtSETUP.COL_VALUE01])?.Trim() ?? "";
                string value02 = Convert.ToString(row[FbtSETUP.COL_VALUE02])?.Trim() ?? "";
                string value05 = Convert.ToString(row[FbtSETUP.COL_VALUE05])?.Trim() ?? "";
                string value11 = Convert.ToString(row[FbtSETUP.COL_VALUE11])?.Trim() ?? "";

                if (!IsSyncHydroMeter(value01)) continue;

                HydroMeterType hydroMeterType = GetHydroMeterType(value01);

                switch (hydroMeterType)
                {
                    case HydroMeterType.ChannelMaster:
                    case HydroMeterType.RQ30D:
                    case HydroMeterType.SonTek:
                        SyncHydroMeter(pk2, hydroMeterType, value02, value05, value11);
                        break;
                    default:
                        break;
                }
            }
        }

        public static void SyncRemove()
        {
            string sql = $"SELECT {FbtSETUP.COL_PK2}, {FbtSETUP.COL_VALUE01}";
            sql += "\n" + $"FROM {FbtSETUP.TABLE_NAME}";
            sql += "\n" + $"WHERE {FbtSETUP.COL_PK1} = 10";
            sql += "\n" + $"AND {FbtSETUP.COL_PK2} IN (2, 3, 5)";
            sql += "\n" + $"ORDER BY {FbtSETUP.COL_PK2}";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                int pk2 = Convert.ToInt32(row[FbtSETUP.COL_PK2]);
                string value01 = Convert.ToString(row[FbtSETUP.COL_VALUE01])?.Trim() ?? "";

                if (IsSyncHydroMeter(value01)) continue;

                RemoveHydroMeter(pk2);
            }
        }

        private static bool IsSyncHydroMeter(string value)
        {
            if (!Enum.TryParse(value, true, out HydroMeterType hydroMeterType)) return false;

            switch (hydroMeterType)
            {
                case HydroMeterType.ChannelMaster:
                case HydroMeterType.RQ30D:
                case HydroMeterType.SonTek:
                    return true;

                default:
                    return false;
            }
        }

        private static HydroMeterType GetHydroMeterType(string value)
        {
            if (Enum.TryParse(value, true, out HydroMeterType hydroMeterType)) return hydroMeterType;

            return HydroMeterType.None;
        }

        private static void SyncHydroMeter(int pk2, HydroMeterType hydroMeterType, string commConfig, string value05, string value11)
        {
            string dataTable;

            switch (pk2)
            {
                case 2:
                    dataTable = "RHYDROMETER1";
                    break;

                case 3:
                    dataTable = "RHYDROMETER2";
                    break;

                case 5:
                    dataTable = "RHYDROMETER3";
                    break;

                default:
                    return;
            }

            string sql = $"SELECT {FbtAFMSHydroMeter.COL_ID}";
            sql += "\n" + $"FROM {FbtAFMSHydroMeter.TABLE_NAME}";
            sql += "\n" + $"WHERE {FbtAFMSHydroMeter.COL_DATA_TABLE} = '{dataTable}'";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql);

            if (db.Results.Rows.Count > 0) return;

            int id = FBProvider.Instance.GetNextID(FbtAFMSHydroMeter.TABLE_NAME);
            int deviceNo = pk2 - 1;

            sql = $"INSERT INTO {FbtAFMSHydroMeter.TABLE_NAME}";
            sql += "\n" + $"({FbtAFMSHydroMeter.COL_ID},";
            sql += "\n" + $"{FbtAFMSHydroMeter.COL_MEASURE_DATE},";
            sql += "\n" + $"{FbtAFMSHydroMeter.COL_MEASURE_TIME},";
            sql += "\n" + $"{FbtAFMSHydroMeter.COL_DEVICE_NAME},";
            sql += "\n" + $"{FbtAFMSHydroMeter.COL_DEVICE_NO},";
            sql += "\n" + $"{FbtAFMSHydroMeter.COL_DATA_TABLE},";
            sql += "\n" + $"{FbtAFMSHydroMeter.COL_AFMS_ONLY},";
            sql += "\n" + $"{FbtAFMSHydroMeter.COL_DEVICE_ATTR},";
            sql += "\n" + $"{FbtAFMSHydroMeter.COL_COMM_CONFIG}";
            sql += "\n" + $") VALUES (";
            sql += "\n" + $"{id},";
            sql += "\n" + $"'{DateTime.Now:yyyyMMdd}',";
            sql += "\n" + $"'{DateTime.Now:HHmmss}',";
            sql += "\n" + $"'{hydroMeterType}',";
            sql += "\n" + $"{deviceNo},";
            sql += "\n" + $"'{dataTable}',";
            sql += "\n" + $"0,";
            if (hydroMeterType == HydroMeterType.ChannelMaster)
            {
                sql += "\n" + $"'{value11}',";
            }
            else if (hydroMeterType == HydroMeterType.RQ30D)
            {
                sql += "\n" + $"'{value05}',";
            }
            else
            {
                sql += "\n" + $" ,";
            }
            sql += "\n" + $"'{commConfig.Replace("'", "''")}'";
            sql += "\n" + $")";

            db.RunNonQuery(sql);
        }

        private static void RemoveHydroMeter(int pk2)
        {
            string dataTable;

            switch (pk2)
            {
                case 2:
                    dataTable = "RHYDROMETER1";
                    break;

                case 3:
                    dataTable = "RHYDROMETER2";
                    break;

                case 5:
                    dataTable = "RHYDROMETER3";
                    break;

                default:
                    return;
            }

            string sql = $"DELETE FROM {FbtAFMSHydroMeter.TABLE_NAME}";
            sql += "\n" + $"WHERE {FbtAFMSHydroMeter.COL_DATA_TABLE} = '{dataTable}'";
            sql += "\n" + $"AND {FbtAFMSHydroMeter.COL_AFMS_ONLY} = 0";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunNonQuery(sql);
        }
    }
}
