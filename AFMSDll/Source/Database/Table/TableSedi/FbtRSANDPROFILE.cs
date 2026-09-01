using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRSANDPROFILE : _FBTableBase
    {
        public const string TABLE_NAME = "RSANDPROFILE";

        public const string COL_PROFILE_ID = "PROFILEID";
        public const string COL_PROFILE_DATE = "PROFILEDATE";
        public const string COL_PROFILE_TIME = "PROFILETIME";
        public const string COL_PROFILE_NAME = "PROFILENAME";

        public const string COL_A_SETUP_FLAG = "ASETUPFLAG";
        public const string COL_A_DEVICE_TYPE = "ADEVICETYPE";
        public const string COL_A_VALID_CELL_TYPE = "AVALIDCELLTYPE";
        public const string COL_A_CELL_FROM = "ACELLFROM";
        public const string COL_A_CELL_TO = "ACELLTO";
        public const string COL_A_DB_FROM = "ADBFROM";
        public const string COL_A_DB_TO = "ADBTO";
        public const string COL_A_REGRESSION = "AREGRESSION";
        public const string COL_A_K_VALUE = "AKVALUE";
        public const string COL_A_BEAM_ANGLE = "ABEAMANGLE";
        public const string COL_A_SSC_A = "ASSCA";
        public const string COL_A_SSC_B = "ASSCB";

        public const string COL_B_SETUP_FLAG = "BSETUPFLAG";
        public const string COL_B_DEVICE_TYPE = "BDEVICETYPE";
        public const string COL_B_VALID_CELL_TYPE = "BVALIDCELLTYPE";
        public const string COL_B_CELL_FROM = "BCELLFROM";
        public const string COL_B_CELL_TO = "BCELLTO";
        public const string COL_B_DB_FROM = "BDBFROM";
        public const string COL_B_DB_TO = "BDBTO";
        public const string COL_B_REGRESSION = "BREGRESSION";
        public const string COL_B_K_VALUE = "BKVALUE";
        public const string COL_B_BEAM_ANGLE = "BBEAMANGLE";
        public const string COL_B_SSC_A = "BSSCA";
        public const string COL_B_SSC_B = "BSSCB";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine($"CREATE TABLE {TABLE_NAME} (");
            sql.AppendLine($"{COL_PROFILE_ID} INTEGER NOT NULL,");
            sql.AppendLine($"{COL_PROFILE_DATE} CHAR(8) NOT NULL,");
            sql.AppendLine($"{COL_PROFILE_TIME} CHAR(6) NOT NULL,");
            sql.AppendLine($"{COL_PROFILE_NAME} VARCHAR(30),");

            sql.AppendLine($"{COL_A_SETUP_FLAG} CHAR(1),");
            sql.AppendLine($"{COL_A_DEVICE_TYPE} VARCHAR(20),");
            sql.AppendLine($"{COL_A_VALID_CELL_TYPE} VARCHAR(20),");
            sql.AppendLine($"{COL_A_CELL_FROM} INTEGER,");
            sql.AppendLine($"{COL_A_CELL_TO} INTEGER,");
            sql.AppendLine($"{COL_A_DB_FROM} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_DB_TO} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_REGRESSION} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_K_VALUE} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_BEAM_ANGLE} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_SSC_A} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_A_SSC_B} DOUBLE PRECISION,");

            sql.AppendLine($"{COL_B_SETUP_FLAG} CHAR(1),");
            sql.AppendLine($"{COL_B_DEVICE_TYPE} VARCHAR(20),");
            sql.AppendLine($"{COL_B_VALID_CELL_TYPE} VARCHAR(20),");
            sql.AppendLine($"{COL_B_CELL_FROM} INTEGER,");
            sql.AppendLine($"{COL_B_CELL_TO} INTEGER,");
            sql.AppendLine($"{COL_B_DB_FROM} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_DB_TO} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_REGRESSION} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_K_VALUE} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_BEAM_ANGLE} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_SSC_A} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_B_SSC_B} DOUBLE PRECISION,");

            sql.AppendLine($"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_PROFILE_ID})");
            sql.Append(')');

            return sql.ToString();
        }
    }
}
