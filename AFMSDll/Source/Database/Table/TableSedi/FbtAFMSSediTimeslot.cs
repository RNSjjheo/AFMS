namespace AFMSDll
{
    public sealed class FbtAFMSSediTimeslot : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_SEDI_TIMESLOT";

        public const string COL_SLOT_TIME = "SLOT_TIME";
        public const string COL_CREATED_AT = "CREATED_AT";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_DATE} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(6) NOT NULL,";
            sql += "\n" + $"{COL_SLOT_TIME} TIMESTAMP NOT NULL,";
            sql += "\n" + $"{COL_CREATED_AT} TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_SEDI_SLOT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_SEDI_SLOT_TIME UNIQUE({COL_SLOT_TIME})";
            sql += "\n" + ")";

            return sql;
        }
    }
}
