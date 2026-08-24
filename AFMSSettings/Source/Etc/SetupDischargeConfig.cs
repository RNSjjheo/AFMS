using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AFMSSettings
{
    public static class SetupDischargeConfig
    {
        public static void Sync()
        {
            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);

            List<HydroMeterType> hydroMeters = GetSetupHydroMeters(db);

            foreach (HydroMeterType hydroMeter in hydroMeters)
            {
                if (Exists(db, hydroMeter)) continue;

                string result = Insert(db, hydroMeter);
                if (!string.IsNullOrEmpty(result)) return;
            }
        }

        private static List<HydroMeterType> GetSetupHydroMeters(FBDatabase db)
        {
            List<HydroMeterType> result = new List<HydroMeterType>();
            HashSet<HydroMeterType> added = new HashSet<HydroMeterType>();

            string sql = "SELECT PK1, PK2, VALUE01";
            sql += "\n" + "FROM RSETUP";
            sql += "\n" + "WHERE (";
            sql += "\n" + "    (PK1 = 10 AND PK2 IN (2, 3, 4))";
            sql += "\n" + "    OR";
            sql += "\n" + "    (PK1 = 50 AND PK2 IN (1, 2))";
            sql += "\n" + ")";
            sql += "\n" + "AND VALUE01 IS NOT NULL";
            sql += "\n" + "AND UPPER(TRIM(VALUE01)) <> 'NONE'";
            sql += "\n" + "ORDER BY PK1, PK2";

            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error)) return result;

            foreach (DataRow row in db.Results.Rows)
            {
                string value = row["VALUE01"]?.ToString()?.Trim() ?? "";
                if (!TryGetHydroMeterType(value, out HydroMeterType hydroMeter)) continue;
                if (hydroMeter == HydroMeterType.None) continue;
                if (!added.Add(hydroMeter)) continue;

                result.Add(hydroMeter);
            }

            return result;
        }

        private static bool TryGetHydroMeterType(string value, out HydroMeterType hydroMeter)
        {
            hydroMeter = HydroMeterType.None;

            if (string.IsNullOrWhiteSpace(value)) return false;
            if (!Enum.TryParse(value, true, out hydroMeter)) return false;
            if (!Enum.IsDefined(typeof(HydroMeterType), hydroMeter)) return false;

            return hydroMeter != HydroMeterType.None;
        }

        private static bool Exists(FBDatabase db, HydroMeterType hydroMeter)
        {
            int id = (int)hydroMeter;

            string sql = $"SELECT COUNT(*)";
            sql += "\n" + $"FROM {FbtAFMSDischargeConfig.TABLE_NAME}";
            sql += "\n" + $"WHERE {_FBTableBase.COL_ID} = {id}";

            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error)) return false;
            if (db.Results.Rows.Count == 0) return false;

            return Convert.ToInt32(db.Results.Rows[0][0]) > 0;
        }

        private static string Insert(FBDatabase db, HydroMeterType hydroMeter)
        {
            List<string> columns = new List<string>();
            List<string> values = new List<string>();

            columns.Add(_FBTableBase.COL_ID);
            values.Add(((int)hydroMeter).ToString());

            columns.Add(_FBTableBase.COL_MEASURE_DATE);
            values.Add($"'{DateTime.Now:yyyyMMdd}'");

            columns.Add(_FBTableBase.COL_MEASURE_TIME);
            values.Add($"'{DateTime.Now:HHmmss}'");

            foreach (DischargeMethod method in Enum.GetValues(typeof(DischargeMethod)))
            {
                if (method == DischargeMethod.None) continue;

                columns.Add(FbtAFMSDischargeConfig.GetMethodColumn(method));
                values.Add("0");
            }

            StringBuilder sql = new StringBuilder();

            sql.Append($"INSERT INTO {FbtAFMSDischargeConfig.TABLE_NAME}");
            sql.Append("\n" + $"({string.Join(", ", columns)})");
            sql.Append("\n" + "VALUES");
            sql.Append("\n" + $"({string.Join(", ", values)})");

            return db.RunNonQuery(sql.ToString());
        }
    }
}
