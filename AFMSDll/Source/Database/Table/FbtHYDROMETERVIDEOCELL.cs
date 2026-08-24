using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtHYDROMETERVIDEOCELL : _FBTableBase
    {
        public const string TABLE_NAME = "RHYDROMETERVIDEOCELL";
        public const string COL_VIDEO_ID = "VIDEO_ID";
        public const string COL_CELL_NO = "CELL_NO";
        public const string COL_VELOCITY = "VELOCITY";
        public const string COL_POS_X = "POS_X";
        public const string COL_POS_Y = "POS_Y";
        public const string COL_UNCERTAINTY = "UNCERTAINTY";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_VIDEO_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_CELL_NO} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_VELOCITY} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_POS_X} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_POS_Y} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_UNCERTAINTY} DOUBLE PRECISION,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + $")";

            return sql;
        }


    }
}
