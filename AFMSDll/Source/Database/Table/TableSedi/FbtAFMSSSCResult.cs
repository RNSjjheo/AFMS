namespace AFMSDll
{
    public sealed class FbtAFMSSediSSCResult : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_SEDI_SSC_RESULT";

        public const string COL_SLOT_ID = "SLOT_ID";

        public const string COL_DEVICE_TYPE = "DEVICE_TYPE";
        public const string COL_AVG_SCB = "AVG_SCB";
        public const string COL_REGRESSION_SLOPE = "REGRESSION_SLOPE";
        public const string COL_REGRESSION_INTERCEPT = "REGRESSION_INTERCEPT";
        public const string COL_SSC_SLOPE = "SSC_SLOPE";
        public const string COL_SSC_INTERCEPT = "SSC_INTERCEPT";
        public const string COL_SSC = "SSC";

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

            sql += "\n" + $"{COL_DEVICE_TYPE} VARCHAR(20),";
            sql += "\n" + $"{COL_AVG_SCB} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_REGRESSION_SLOPE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_REGRESSION_INTERCEPT} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_SSC_SLOPE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_SSC_INTERCEPT} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_SSC} DOUBLE PRECISION,";

            sql += "\n" + $"{COL_CALCULATED_AT} TIMESTAMP,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_SEDI_SSC_RESULT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_SEDI_SSC_RESULT_SLOT UNIQUE({COL_SLOT_ID})";
            sql += "\n" + ")";

            return sql;
        }
    }
}
