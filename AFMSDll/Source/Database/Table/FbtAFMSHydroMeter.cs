using System;

namespace AFMSDll
{
    public class FbtAFMSHydroMeter : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_HYDRO_METER";

        public const string COL_DEVICE_NAME = "DEVICE_NAME";
        public const string COL_DEVICE_NO = "DEVICE_NO";
        public const string COL_DATA_TABLE = "DATA_TABLE";
        public const string COL_AFMS_ONLY = "AFMS_ONLY";
        public const string COL_COMM_CONFIG = "COMM_CONFIG";
        public const string COL_DEVICE_ATTR = "DEVICE_ATTR";
        public const string COL_TRANSECT_CNT = "TRANSECT_CNT";

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
            sql += "\n" + $"{COL_DEVICE_NAME} VARCHAR(50),";
            sql += "\n" + $"{COL_DEVICE_NO} INTEGER,";
            sql += "\n" + $"{COL_DATA_TABLE} VARCHAR(50),";
            sql += "\n" + $"{COL_DEVICE_ATTR} VARCHAR(50),";
            sql += "\n" + $"{COL_TRANSECT_CNT} INTEGER,";
            sql += "\n" + $"{COL_AFMS_ONLY} INTEGER,";
            sql += "\n" + $"{COL_COMM_CONFIG} VARCHAR(200),";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + ")";

            return sql;
        }

        public override string CheckNewColumn(FBDatabase db)
        {
            string result;

            if (!HasColumn(db, COL_DEVICE_NAME))
            {
                result = AddColumn(db, COL_DEVICE_NAME, "VARCHAR(50)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_DEVICE_NO))
            {
                result = AddColumn(db, COL_DEVICE_NO, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_COMM_CONFIG))
            {
                result = AddColumn(db, COL_COMM_CONFIG, "VARCHAR(200)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_DATA_TABLE))
            {
                result = AddColumn(db, COL_DATA_TABLE, "VARCHAR(50)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_AFMS_ONLY))
            {
                result = AddColumn(db, COL_AFMS_ONLY, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_DEVICE_ATTR))
            {
                result = AddColumn(db, COL_DEVICE_ATTR, "VARCHAR(50)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_TRANSECT_CNT))
            {
                result = AddColumn(db, COL_TRANSECT_CNT, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            return "";
        }
    }
}