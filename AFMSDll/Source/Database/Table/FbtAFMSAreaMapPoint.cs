using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtAFMSAreaMapPoint : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_AREA_MAP_POINT";

        public const string COL_MAP_NAME = "MAP_NAME";
        public const string COL_POINT_COUNT = "POINT_COUNT";
        public const string COL_ZERO_POINT_ELEVATION = "ZERO_POINT_ELEVATION";
        public const string COL_MAP_DATA = "MAP_DATA";

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_DATE} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MAP_NAME} VARCHAR(64) NOT NULL,";
            sql += "\n" + $"{COL_POINT_COUNT} INT NOT NULL,";
            sql += "\n" + $"{COL_ZERO_POINT_ELEVATION} DOUBLE PRECISION NOT NULL,";
            sql += "\n" + $"{COL_MAP_DATA} BLOB SUB_TYPE TEXT CHARACTER SET UTF8,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + $")";

            return sql;
        }

        public override string GetTableName()
        {
            return TABLE_NAME;
        }
    }
}
