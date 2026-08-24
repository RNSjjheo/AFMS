using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace AFMSDll
{
    public class FbtAFMSDischargeConfig : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DISCHARGE_CONFIG";
        public const string COL_HYDRO_ID = "HYDRO_ID";
        public const string COL_MID_SECTION = "MID_SECTION";
        public const string COL_RATING_CURVE = "RATING_CURVE";
        public const string COL_SURFACE_VELOCITY = "SURFACE_VELOCITY";//SurfaceVelocity
        public const string COL_VELOCITY_DISTRIBUTION= "VELOCITY_DISTRIBUTION";  //VelocityDistribution
        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_DATE} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_HYDRO_ID} INTEGER NOT NULL,";

            foreach (DischargeMethod method in Enum.GetValues(typeof(DischargeMethod)))
            {
                if (method == DischargeMethod.None) continue;
                sql += "\n" + $"{GetMethodColumn(method)} INTEGER,";
            }

            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + ")";

            return sql;
        }

        public override string CheckNewColumn(FBDatabase db)
        {
            foreach (DischargeMethod method in Enum.GetValues(typeof(DischargeMethod)))
            {
                if (method == DischargeMethod.None) continue;

                string columnName = GetMethodColumn(method);
                if (HasColumn(db, columnName)) continue;

                string result = AddColumn(db, columnName, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            return "";
        }

        public static string GetMethodColumn(DischargeMethod method)
        {
            switch (method)
            {
                case DischargeMethod.MidSection:
                    return COL_MID_SECTION;
                case DischargeMethod.RatingCurve:
                    return COL_RATING_CURVE;
                case DischargeMethod.SurfaceVelo:
                    return COL_SURFACE_VELOCITY;
                case DischargeMethod.VeloDist:
                    return COL_VELOCITY_DISTRIBUTION;
            }

            return "";
        }
    }
}