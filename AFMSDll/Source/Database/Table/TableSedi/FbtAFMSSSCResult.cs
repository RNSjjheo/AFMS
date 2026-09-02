namespace AFMSDll
{
    public sealed class FbtAFMSSSCResult : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_SSC_RESULT";

        public const string COL_SLOT_ID = "SLOT_ID";

        public const string COL_A_DEVICE_TYPE = "A_DEVICE_TYPE";
        public const string COL_A_AVG_SCB = "A_AVG_SCB";
        public const string COL_A_REGRESSION_SLOPE = "A_REGRESSION_SLOPE";
        public const string COL_A_REGRESSION_INTERCEPT = "A_REGRESSION_INTERCEPT";
        public const string COL_A_SSC_SLOPE = "A_SSC_SLOPE";
        public const string COL_A_SSC_INTERCEPT = "A_SSC_INTERCEPT";
        public const string COL_A_SSC = "A_SSC";

        public const string COL_B_DEVICE_TYPE = "B_DEVICE_TYPE";
        public const string COL_B_AVG_SCB = "B_AVG_SCB";
        public const string COL_B_REGRESSION_SLOPE = "B_REGRESSION_SLOPE";
        public const string COL_B_REGRESSION_INTERCEPT = "B_REGRESSION_INTERCEPT";
        public const string COL_B_SSC_SLOPE = "B_SSC_SLOPE";
        public const string COL_B_SSC_INTERCEPT = "B_SSC_INTERCEPT";
        public const string COL_B_SSC = "B_SSC";

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

            sql += "\n" + $"{COL_A_DEVICE_TYPE} VARCHAR(20),";
            sql += "\n" + $"{COL_A_AVG_SCB} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_A_REGRESSION_SLOPE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_A_REGRESSION_INTERCEPT} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_A_SSC_SLOPE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_A_SSC_INTERCEPT} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_A_SSC} DOUBLE PRECISION,";

            sql += "\n" + $"{COL_B_DEVICE_TYPE} VARCHAR(20),";
            sql += "\n" + $"{COL_B_AVG_SCB} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_B_REGRESSION_SLOPE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_B_REGRESSION_INTERCEPT} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_B_SSC_SLOPE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_B_SSC_INTERCEPT} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_B_SSC} DOUBLE PRECISION,";

            sql += "\n" + $"{COL_CALCULATED_AT} TIMESTAMP,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_SSC_RESULT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_SSC_RESULT_SLOT UNIQUE({COL_SLOT_ID})";
            sql += "\n" + ")";

            return sql;
        }
    }
}
