namespace AFMSDll
{
    public class FbtAFMSCrossSection : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_CROSS_SECTION";

        public const string COL_DESCRIPTION = "DESCRIPTION";
        public const string COL_POINT_COUNT = "POINT_COUNT";
        public const string COL_ZERO_POINT_ELEVATION = "ZERO_POINT_ELEVATION";
        public const string COL_POINT_DATA = "POINT_DATA";

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_DATE} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_DESCRIPTION} VARCHAR(64) NOT NULL,";
            sql += "\n" + $"{COL_POINT_COUNT} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_ZERO_POINT_ELEVATION} DOUBLE PRECISION NOT NULL,";
            sql += "\n" + $"{COL_POINT_DATA} BLOB SUB_TYPE TEXT CHARACTER SET UTF8 NOT NULL,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + ")";

            return sql;
        }

        public override string GetTableName()
        {
            return TABLE_NAME;
        }
    }
}
