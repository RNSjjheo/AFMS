using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtAFMSSediSSCProfile : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_SEDI_SSC_PROFILE";
        public const string COL_DEVICE_TYPE = "DEVICETYPE";
        public const string COL_HYDRO_TABLE_NAME = "HYDRO_TABLE_NAME";
        public const string COL_CELL_FROM = "CELLFROM";
        public const string COL_CELL_TO = "CELLTO";
        public const string COL_K_VALUE = "KVALUE";
        public const string COL_BEAM_ANGLE = "BEAMANGLE";
        public const string COL_SSC_A = "SSCA";
        public const string COL_SSC_B = "SSCB";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine($"CREATE TABLE {TABLE_NAME} (");
            sql.AppendLine($"{COL_ID} INTEGER NOT NULL,");
            sql.AppendLine($"{COL_MEASURE_DATE} CHAR(8) NOT NULL,");
            sql.AppendLine($"{COL_MEASURE_TIME} CHAR(6) NOT NULL,");
            sql.AppendLine($"{COL_DEVICE_TYPE} VARCHAR(20),");
            sql.AppendLine($"{COL_HYDRO_TABLE_NAME} VARCHAR(20) NOT NULL,");
            sql.AppendLine($"{COL_CELL_FROM} INTEGER,");
            sql.AppendLine($"{COL_CELL_TO} INTEGER,");
            sql.AppendLine($"{COL_K_VALUE} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_BEAM_ANGLE} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_SSC_A} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_SSC_B} DOUBLE PRECISION,");

            sql.AppendLine($"{GetEnumCheckClause<HydroMetherTableType>(COL_HYDRO_TABLE_NAME)},");
            sql.AppendLine($"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_ID})");
            sql.Append(')');

            return sql.ToString();
        }

        public override string CheckNewColumn(FBDatabase db)
        {
            return "";
        }
    }
}
