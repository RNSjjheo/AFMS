using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRSAND : _FBTableBase
    {
        public const string TABLE_NAME = "RSAND";

        public const string COL_DEVICE_TYPE = "DEVICETYPE";
        public const string COL_AVG_SCB = "AVG_SCB";
        public const string COL_A = "A";
        public const string COL_B = "B";
        public const string COL_SA = "SA";
        public const string COL_SB = "SB";
        public const string COL_SSC = "SSC";
        public const string COL_DISCHARGE1 = "DISCHARGE1";
        public const string COL_DISCHARGE2 = "DISCHARGE2";
        public const string COL_TOTAL_SAND1 = "TOTALSAND1";
        public const string COL_TOTAL_SAND2 = "TOTALSAND2";

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

            sql.AppendLine($"{COL_DEVICE_TYPE} VARCHAR(20),");
            sql.AppendLine($"{COL_AVG_SCB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_SA} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_SB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_SSC} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_DISCHARGE1} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_DISCHARGE2} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_TOTAL_SAND1} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_TOTAL_SAND2} DOUBLE PRECISION,");

            sql.AppendLine($"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_MEASURE_DATE}, {COL_MEASURE_TIME})");
            sql.Append(')');

            return sql.ToString();
        }
    }
}
