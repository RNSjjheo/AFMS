namespace AFMSDll
{
    public class FbtAFMSDischargeTimeslot : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DISCHARGE_TIMESLOT";

        public const string COL_SLOT_TIME = "SLOT_TIME";
        public const string COL_STATUS = "STATUS";
        public const string COL_CREATED_AT = "CREATED_AT";
        public const string COL_LAST_CALCULATED_AT = "LAST_CALCULATED_AT";

        public const string STATUS_WAITING = "WAITING";
        public const string STATUS_PROCESSING = "PROCESSING";
        public const string STATUS_COMPLETED = "COMPLETED";
        public const string STATUS_PARTIAL = "PARTIAL";
        public const string STATUS_FAILED = "FAILED";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_SLOT_TIME} TIMESTAMP NOT NULL,";
            sql += "\n" + $"{COL_STATUS} VARCHAR(20) DEFAULT '{STATUS_WAITING}' NOT NULL,";
            sql += "\n" + $"{COL_CREATED_AT} TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,";
            sql += "\n" + $"{COL_LAST_CALCULATED_AT} TIMESTAMP,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_DIS_SLOT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_DIS_SLOT_TIME UNIQUE({COL_SLOT_TIME})";
            sql += "\n" + ")";

            return sql;
        }
    }
}
