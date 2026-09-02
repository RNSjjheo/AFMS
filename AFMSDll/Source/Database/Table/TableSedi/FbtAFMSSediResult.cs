namespace AFMSDll
{
    public sealed class FbtAFMSSediResult : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_SEDI_RESULT";

        public const string COL_SLOT_ID = "SLOT_ID";

        public const string COL_DISCHARGE1 = "DISCHARGE1";
        public const string COL_DISCHARGE2 = "DISCHARGE2";
        public const string COL_TOTAL_SAND1 = "TOTAL_SAND1";
        public const string COL_TOTAL_SAND2 = "TOTAL_SAND2";

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

            sql += "\n" + $"{COL_DISCHARGE1} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_DISCHARGE2} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_TOTAL_SAND1} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_TOTAL_SAND2} DOUBLE PRECISION,";

            sql += "\n" + $"{COL_CALCULATED_AT} TIMESTAMP,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_SEDI_RESULT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_SEDI_RESULT_SLOT UNIQUE({COL_SLOT_ID})";
            sql += "\n" + ")";

            return sql;
        }
    }
}
