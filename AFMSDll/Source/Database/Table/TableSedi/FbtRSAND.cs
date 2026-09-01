using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRSAND : _FBTableBase
    {
        public const string TABLE_NAME = "RSAND";

        public const string COL_A_DEVICE_TYPE = "ADEVICETYPE";
        public const string COL_A_AVG_SCB = "AAVG_SCB";
        public const string COL_A_A = "AA";
        public const string COL_A_B = "AB";
        public const string COL_A_SA = "ASA";
        public const string COL_A_SB = "ASB";
        public const string COL_A_SSC = "ASSC";
        public const string COL_A_DISCHARGE1 = "ADISCHARGE1";
        public const string COL_A_DISCHARGE2 = "ADISCHARGE2";
        public const string COL_A_TOTAL_SAND1 = "ATOTALSAND1";
        public const string COL_A_TOTAL_SAND2 = "ATOTALSAND2";

        public const string COL_B_DEVICE_TYPE = "BDEVICETYPE";
        public const string COL_B_AVG_SCB = "BAVG_SCB";
        public const string COL_B_A = "BA";
        public const string COL_B_B = "BB";
        public const string COL_B_SA = "BSA";
        public const string COL_B_SB = "BSB";
        public const string COL_B_SSC = "BSSC";
        public const string COL_B_DISCHARGE1 = "BDISCHARGE1";
        public const string COL_B_DISCHARGE2 = "BDISCHARGE2";
        public const string COL_B_TOTAL_SAND1 = "BTOTALSAND1";
        public const string COL_B_TOTAL_SAND2 = "BTOTALSAND2";

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

            sql.AppendLine($"{COL_A_DEVICE_TYPE} VARCHAR(20),");
            sql.AppendLine($"{COL_A_AVG_SCB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_A} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_B} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_SA} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_SB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_SSC} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_DISCHARGE1} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_DISCHARGE2} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_TOTAL_SAND1} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_TOTAL_SAND2} DOUBLE PRECISION,");

            sql.AppendLine($"{COL_B_DEVICE_TYPE} VARCHAR(20),");
            sql.AppendLine($"{COL_B_AVG_SCB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_A} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_B} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_SA} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_SB} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_SSC} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_DISCHARGE1} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_DISCHARGE2} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_TOTAL_SAND1} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_TOTAL_SAND2} DOUBLE PRECISION,");

            sql.AppendLine($"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_MEASURE_DATE}, {COL_MEASURE_TIME})");
            sql.Append(')');

            return sql.ToString();
        }
    }
}
