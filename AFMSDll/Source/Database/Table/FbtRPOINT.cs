using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRPOINT : _FBTableBase
    {
        public const string TABLE_NAME = "RPOINT";

        public const string COL_HYDROMETER1_FLAG = "HYDROMETER1FLAG";
        public const string COL_HYDROMETER2_FLAG = "HYDROMETER2FLAG";
        public const string COL_HYDROMETER3_FLAG = "HYDROMETER3FLAG";
        public const string COL_WATER_LEVEL_FLAG = "WATERLEVELFLAG";
        public const string COL_STREAM_FLAG = "STREAMFLAG";
        public const string COL_FILE_FLAG = "FILEFLAG";
        public const string COL_SERVER_FLAG = "SERVERFLAG";
        public const string COL_RNSEA_FLAG = "RNSEAFLAG";

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
            sql.AppendLine($"{COL_HYDROMETER1_FLAG} CHAR(1),");
            sql.AppendLine($"{COL_HYDROMETER2_FLAG} CHAR(1),");
            sql.AppendLine($"{COL_HYDROMETER3_FLAG} CHAR(1),");
            sql.AppendLine($"{COL_WATER_LEVEL_FLAG} CHAR(1),");
            sql.AppendLine($"{COL_STREAM_FLAG} CHAR(1),");
            sql.AppendLine($"{COL_FILE_FLAG} CHAR(1),");
            sql.AppendLine($"{COL_SERVER_FLAG} CHAR(1),");
            sql.AppendLine($"{COL_RNSEA_FLAG} CHAR(1),");
            sql.AppendLine(
                $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY " +
                $"({COL_MEASURE_DATE}, {COL_MEASURE_TIME})");
            sql.Append(')');
            return sql.ToString();
        }
    }
}
