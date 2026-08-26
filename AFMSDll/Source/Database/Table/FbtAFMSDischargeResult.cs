namespace AFMSDll
{
    public class FbtAFMSDischargeResult : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DISCHARGE_RESULT";

        public const string COL_SLOT_ID = "SLOT_ID";
        public const string COL_SOURCE_DEVICE_TYPE = "SOURCE_DEVICE_TYPE";
        public const string COL_SOURCE_DEVICE_ID = "SOURCE_DEVICE_ID";
        public const string COL_DISCHARGE_METHOD = "DISCHARGE_METHOD";
        public const string COL_HYDRO_CONFIG_ID = "HYDRO_CONFIG_ID";
        public const string COL_DISCHARGE_CONFIG_ID = "DISCHARGE_CONFIG_ID";
        public const string COL_CROSS_SECTION_ID = "CROSS_SECTION_ID";
        public const string COL_TRANSECT_CONFIG_ID = "TRANSECT_CONFIG_ID";
        public const string COL_METHOD_CONFIG_ID = "METHOD_CONFIG_ID";
        public const string COL_WATER_LEVEL = "WATER_LEVEL";
        public const string COL_VELOCITY = "VELOCITY";
        public const string COL_CROSS_SECTION_AREA = "CROSS_SECTION_AREA";
        public const string COL_DISCHARGE = "DISCHARGE";
        public const string COL_SOURCE_TIME = "SOURCE_TIME";
        public const string COL_CALCULATED_AT = "CALCULATED_AT";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_SLOT_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_SOURCE_DEVICE_TYPE} VARCHAR(30) NOT NULL,";
            sql += "\n" + $"{COL_SOURCE_DEVICE_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_DISCHARGE_METHOD} VARCHAR(32) NOT NULL,";
            sql += "\n" + $"{COL_HYDRO_CONFIG_ID} INTEGER,";
            sql += "\n" + $"{COL_DISCHARGE_CONFIG_ID} INTEGER,";
            sql += "\n" + $"{COL_CROSS_SECTION_ID} INTEGER,";
            sql += "\n" + $"{COL_TRANSECT_CONFIG_ID} INTEGER,";
            sql += "\n" + $"{COL_METHOD_CONFIG_ID} INTEGER,";
            sql += "\n" + $"{COL_WATER_LEVEL} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_VELOCITY} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_CROSS_SECTION_AREA} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_DISCHARGE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_SOURCE_TIME} TIMESTAMP,";
            sql += "\n" + $"{COL_CALCULATED_AT} TIMESTAMP,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_DIS_RESULT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_DIS_RESULT UNIQUE(";
            sql += $"{COL_SLOT_ID}, {COL_SOURCE_DEVICE_TYPE}, {COL_SOURCE_DEVICE_ID}, {COL_DISCHARGE_METHOD})";
            sql += "\n" + ")";

            return sql;
        }

        public override string CheckNewColumn(FBDatabase db)
        {
            string result;

            if (!HasColumn(db, COL_SOURCE_DEVICE_TYPE))
            {
                result = AddColumn(db, COL_SOURCE_DEVICE_TYPE, "VARCHAR(30)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_SOURCE_DEVICE_ID))
            {
                result = AddColumn(db, COL_SOURCE_DEVICE_ID, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            return string.Empty;
        }
    }
}
