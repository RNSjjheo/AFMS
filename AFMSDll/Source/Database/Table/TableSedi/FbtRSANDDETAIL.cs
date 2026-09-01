using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRSANDDETAIL : _FBTableBase
    {
        public const string TABLE_NAME = "RSANDDETAIL";

        public const string COL_CELL_NO = "CELLNO";

        public const string COL_A_MB = "AMB";
        public const string COL_A_R = "AR";
        public const string COL_A_U = "AU";
        public const string COL_A_AW = "AAW";
        public const string COL_A_AS = "AAS";
        public const string COL_A_WCB = "AWCB";
        public const string COL_A_SCB = "ASCB";

        public const string COL_B_MB = "BMB";
        public const string COL_B_R = "BR";
        public const string COL_B_U = "BU";
        public const string COL_B_AW = "BAW";
        public const string COL_B_AS = "BAS";
        public const string COL_B_WCB = "BWCB";
        public const string COL_B_SCB = "BSCB";

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

            sql.AppendLine($"{COL_A_MB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_R} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_U} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_AW} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_AS} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_WCB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_SCB} DOUBLE PRECISION,");

            sql.AppendLine($"{COL_B_MB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_R} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_U} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_AW} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_AS} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_WCB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_SCB} DOUBLE PRECISION,");

            sql.AppendLine($"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_MEASURE_DATE}, {COL_MEASURE_TIME}, {COL_CELL_NO})");
            sql.Append(')');

            return sql.ToString();
        }
    }
}
