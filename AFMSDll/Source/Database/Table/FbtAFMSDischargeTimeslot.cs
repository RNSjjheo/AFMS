namespace AFMSDll
{
    public class FbtAFMSDischargeTimeslot : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DISCHARGE_TIMESLOT";

        public const string COL_SLOT_TIME = "SLOT_TIME";
        public const string COL_CROSS_SECTION_ID = "CROSS_SECTION_ID";
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
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_SLOT_TIME} TIMESTAMP NOT NULL,";
            sql += "\n" + $"{COL_CROSS_SECTION_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_CREATED_AT} TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_DIS_SLOT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_DIS_SLOT_TIME UNIQUE({COL_SLOT_TIME})";
            sql += "\n" + ")";

            return sql;
        }

        public override string CheckNewColumn(FBDatabase db)
        {
            string result;

            if (!HasColumn(db, COL_MEASURE_DATE))
            {
                result = AddColumn(db, COL_MEASURE_DATE, "VARCHAR(8)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_MEASURE_TIME))
            {
                result = AddColumn(db, COL_MEASURE_TIME, "VARCHAR(8)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_CROSS_SECTION_ID))
            {
                result = AddColumn(db, COL_CROSS_SECTION_ID, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            return string.Empty;
        }
    }
}
