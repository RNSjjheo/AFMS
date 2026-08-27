using AFMSDll;
using System.Data;
using System.Text.Json;

namespace AFMSSettings
{
    internal static class DischargeMethodConfigStore
    {
        private const int CurrentVersion = 1;

        public static string Save(
            MeasurementDeviceType deviceType,
            int deviceId,
            DischargeMethod method,
            object calculation,
            string description)
        {
            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            int? transectId = deviceType == MeasurementDeviceType.VelocityMeter
                ? GetLatestId(db, FbtAFMSHydroTransect.TABLE_NAME, FbtAFMSHydroTransect.COL_HYDRO_ID, deviceId)
                : null;
            int? crossSectionId = GetLatestId(db, FbtAFMSCrossSection.TABLE_NAME, null, null);

            string json = JsonSerializer.Serialize(new
            {
                version = CurrentVersion,
                device = new { type = deviceType.ToString(), id = deviceId },
                transectConfigId = transectId,
                crossSectionId,
                calculation
            });

            QueryBuilderInsert query = new();
            query.Table = FbtAFMSDischargeMethodConfig.TABLE_NAME;
            query.AutoIncrement = FbtAFMSDischargeMethodConfig.COL_ID;
            query.Value(FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE, deviceType.ToString());
            query.Value(FbtAFMSDischargeMethodConfig.COL_DEVICE_ID, deviceId);
            query.Value(FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD, method.ToString());
            query.Value(FbtAFMSDischargeMethodConfig.COL_TRANSECT_CONFIG_ID, transectId);
            query.Value(FbtAFMSDischargeMethodConfig.COL_CROSS_SECTION_ID, crossSectionId);
            query.Value(FbtAFMSDischargeMethodConfig.COL_CONFIG_VERSION, CurrentVersion);
            query.Value(FbtAFMSDischargeMethodConfig.COL_CONFIG_JSON, json);
            query.Value(FbtAFMSDischargeMethodConfig.COL_ENABLED, 1);
            query.Value(FbtAFMSDischargeMethodConfig.COL_CREATED_AT, DateTime.Now, typeof(DateTime));
            query.Value(FbtAFMSDischargeMethodConfig.COL_DESCRIPTION, description);
            db.Execute(query, out string error);
            return error;
        }

        private static int? GetLatestId(FBDatabase db, string table, string? filterColumn, int? filterValue)
        {
            string sql = $"SELECT MAX({_FBTableBase.COL_ID}) FROM {table}";
            if (filterColumn != null && filterValue.HasValue) sql += $" WHERE {filterColumn} = {filterValue.Value}";
            DataTable result = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error) || result.Rows.Count == 0 || result.Rows[0][0] == DBNull.Value) return null;
            return Convert.ToInt32(result.Rows[0][0]);
        }
    }
}
