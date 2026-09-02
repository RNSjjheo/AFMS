namespace AFMSDll
{
    public sealed class FbtAFMSSediResult : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_SEDI_RESULT";

        public const string COL_SLOT_ID = "SLOT_ID";

        public const string COL_A_DISCHARGE1 = "A_DISCHARGE1";
        public const string COL_A_DISCHARGE2 = "A_DISCHARGE2";
        public const string COL_A_TOTAL_SAND1 = "A_TOTAL_SAND1";
        public const string COL_A_TOTAL_SAND2 = "A_TOTAL_SAND2";

        public const string COL_B_DISCHARGE1 = "B_DISCHARGE1";
        public const string COL_B_DISCHARGE2 = "B_DISCHARGE2";
        public const string COL_B_TOTAL_SAND1 = "B_TOTAL_SAND1";
        public const string COL_B_TOTAL_SAND2 = "B_TOTAL_SAND2";

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

            sql += "\n" + $"{COL_A_DISCHARGE1} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_A_DISCHARGE2} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_A_TOTAL_SAND1} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_A_TOTAL_SAND2} DOUBLE PRECISION,";

            sql += "\n" + $"{COL_B_DISCHARGE1} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_B_DISCHARGE2} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_B_TOTAL_SAND1} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_B_TOTAL_SAND2} DOUBLE PRECISION,";

            sql += "\n" + $"{COL_CALCULATED_AT} TIMESTAMP,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_SEDI_RESULT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_SEDI_RESULT_SLOT UNIQUE({COL_SLOT_ID})";
            sql += "\n" + ")";

            return sql;
        }
    }
}
