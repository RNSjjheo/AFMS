namespace AFMSDll
{
    public class FbtAFMSDiscAttrRatingCurve : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DIS_ATTR_RATING_CURVE";
        public const string COL_DIS_VER = "DIS_VER";
        public const string COL_COEFF_COUNT = "COEFF_COUNT";
        public const string COL_DIS_ATTR = "DIS_ATTR";

        public override string GetTableName() => TABLE_NAME;

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_DATE} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_DIS_VER} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_COEFF_COUNT} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_DIS_ATTR} BLOB SUB_TYPE TEXT CHARACTER SET UTF8,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_ID})";
            sql += "\n)";
            return sql;
        }
    }
}
