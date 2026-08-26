namespace AFMSDll
{
    public class FbtAFMSDischargeResult : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DISCHARGE_RESULT";

        public const string COL_SLOT_ID = "SLOT_ID";
        public const string COL_HYDRO_ID = "HYDRO_ID";
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
        public const string COL_CALCULATION_STATUS = "CALCULATION_STATUS";
        public const string COL_ERROR_CODE = "ERROR_CODE";

        public const string STATUS_WAITING = "WAITING";
        public const string STATUS_SUCCESS = "SUCCESS";
        public const string STATUS_FAILED = "FAILED";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_SLOT_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_HYDRO_ID} INTEGER NOT NULL,";
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
            sql += "\n" + $"{COL_CALCULATION_STATUS} VARCHAR(20) DEFAULT '{STATUS_WAITING}' NOT NULL,";
            sql += "\n" + $"{COL_ERROR_CODE} VARCHAR(50),";
            sql += "\n" + $"CONSTRAINT PK_AFMS_DIS_RESULT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_DIS_RESULT UNIQUE(";
            sql += $"{COL_SLOT_ID}, {COL_HYDRO_ID}, {COL_DISCHARGE_METHOD})";
            sql += "\n" + ")";

            return sql;
        }
    }
}
