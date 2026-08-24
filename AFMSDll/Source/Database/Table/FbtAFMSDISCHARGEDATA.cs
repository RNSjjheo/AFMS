using System;

namespace AFMSDll
{
    public class FbtAFMSDischargeData : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DISCHARGE_DATA";

        public const string COL_DISCHARGE_TYPE = "Discharge_Type";
        public const string COL_HYDRO_METER = "Hydro_Meter";
        public const string COL_HYDRO_ID = "Hydro_Id";
        public const string COL_AREA_ID = "Area_Id";
        public const string COL_DISCHARGE_ATTR_ID = "Discharge_Attr_Id";
        public const string COL_MEASURE_OK = "MEASURE_OK";
        public const string COL_AREA_FLOW = "Area_Flow";
        public const string COL_DISCHARGE = "Discharge";

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
            sql += "\n" + $"{COL_DISCHARGE_TYPE} VARCHAR(50),";
            sql += "\n" + $"{COL_HYDRO_METER} VARCHAR(50),";
            sql += "\n" + $"{COL_HYDRO_ID} INTEGER,";
            sql += "\n" + $"{COL_AREA_ID} INTEGER,";
            sql += "\n" + $"{COL_DISCHARGE_ATTR_ID} INTEGER,";
            sql += "\n" + $"{COL_MEASURE_OK} INTEGER,";
            sql += "\n" + $"{COL_AREA_FLOW} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_DISCHARGE} DOUBLE PRECISION,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + ")";

            return sql;
        }

        public override string CheckNewColumn(FBDatabase db)
        {
            string result;

            if (!HasColumn(db, COL_DISCHARGE_TYPE))
            {
                result = AddColumn(db, COL_DISCHARGE_TYPE, "VARCHAR(50)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_HYDRO_METER))
            {
                result = AddColumn(db, COL_HYDRO_METER, "VARCHAR(50)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_HYDRO_ID))
            {
                result = AddColumn(db, COL_HYDRO_ID, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_AREA_ID))
            {
                result = AddColumn(db, COL_AREA_ID, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_DISCHARGE_ATTR_ID))
            {
                result = AddColumn(db, COL_DISCHARGE_ATTR_ID, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_MEASURE_OK))
            {
                result = AddColumn(db, COL_MEASURE_OK, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_AREA_FLOW))
            {
                result = AddColumn(db, COL_AREA_FLOW, "DOUBLE PRECISION");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_DISCHARGE))
            {
                result = AddColumn(db, COL_DISCHARGE, "DOUBLE PRECISION");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            return "";
        }
    }
}