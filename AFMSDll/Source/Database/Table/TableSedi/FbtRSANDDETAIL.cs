using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRSANDDETAIL : _FBTableBase
    {
        public const string TABLE_NAME = "RSANDDETAIL";

        public const string COL_CELL_NO = "CELLNO";

        public const string COL_MB = "MB";
        public const string COL_R = "R";
        public const string COL_U = "U";
        public const string COL_AW = "AW";
        public const string COL_AS = "AS";
        public const string COL_WCB = "WCB";
        public const string COL_SCB = "SCB";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine($"CREATE TABLE {TABLE_NAME} (");
            sql.AppendLine($"{COL_MEASURE_DATE} CHAR(8) NOT NULL,");
            sql.AppendLine($"{COL_MEASURE_TIME} CHAR(6) NOT NULL,");
            sql.AppendLine($"{COL_CELL_NO} INTEGER NOT NULL,");

            sql.AppendLine($"{COL_MB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_R} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_U} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_AW} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_AS} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_WCB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_SCB} DOUBLE PRECISION,");

            sql.AppendLine($"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_MEASURE_DATE}, {COL_MEASURE_TIME}, {COL_CELL_NO})");
            sql.Append(')');

            return sql.ToString();
        }
    }
}
