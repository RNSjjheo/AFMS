namespace AFMSDll
{
    public enum SediTransmissionStatus
    {
        NOT_SEND = 0, // 외부 시스템으로 아직 전송하지 않음
        COMPLETED = 1 // 외부 시스템 전송 성공
    }

    public sealed class FbtAFMSSediSSCTimeslot : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_SEDI_SSC_TIMESLOT";

        public const string COL_SLOT_TIME = "SLOT_TIME";
        public const string COL_CREATED_AT = "CREATED_AT";
        public const string COL_SEND_STATUS = "SEND_STATUS";

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
            sql += "\n" + $"{COL_SEND_STATUS} VARCHAR(16) DEFAULT '{SediTransmissionStatus.NOT_SEND}' NOT NULL,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_SEDI_SSC_TIMESLOT PRIMARY KEY({COL_ID}),";
            sql += "\n" + $"CONSTRAINT UQ_AFMS_SEDI_SSC_TIMESLOT_TIME UNIQUE({COL_SLOT_TIME}),";
            sql += "\n" + $"CONSTRAINT CK_SEDI_SEND_STATUS {GetEnumCheckClause<SediTransmissionStatus>(COL_SEND_STATUS)}";
            sql += "\n" + ")";

            return sql;
        }

        public override string CheckNewColumn(FBDatabase db)
        {
            if (HasColumn(db, COL_SEND_STATUS)) return string.Empty;

            string columnType = "";
            columnType += $"VARCHAR(16) DEFAULT '{SediTransmissionStatus.NOT_SEND}' NOT NULL ";
            columnType += GetEnumCheckClause<SediTransmissionStatus>(COL_SEND_STATUS);

            return AddColumn(db, COL_SEND_STATUS, columnType);
        }
    }
}
