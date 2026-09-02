using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public enum HydroMetherTableType
    {
        RHYDROMETER1 = 0,
        RHYDROMETER2,
        RHYDROMETER3
    }

    public abstract class FbtRHYDROMETER : _FBTableBase
    {
        public const string COL_HYDRO_KIND = "HYDROKIND";
        public const string COL_AVG_VELOCITY = "AVGVELOCITY";
        public const string COL_MIN_VELOCITY = "MINVELOCITY";
        public const string COL_MAX_VELOCITY = "MAXVELOCITY";

        public const string COL_VALUE01 = "VALUE01";
        public const string COL_VALUE02 = "VALUE02";
        public const string COL_VALUE03 = "VALUE03";
        public const string COL_VALUE04 = "VALUE04";
        public const string COL_VALUE05 = "VALUE05";
        public const string COL_VALUE06 = "VALUE06";
        public const string COL_VALUE07 = "VALUE07";
        public const string COL_VALUE08 = "VALUE08";
        public const string COL_VALUE09 = "VALUE09";
        public const string COL_VALUE10 = "VALUE10";
        public const string COL_VALUE11 = "VALUE11";
        public const string COL_VALUE12 = "VALUE12";
        public const string COL_VALUE13 = "VALUE13";
        public const string COL_VALUE14 = "VALUE14";
        public const string COL_VALUE15 = "VALUE15";
        public const string COL_VALUE16 = "VALUE16";
        public const string COL_VALUE17 = "VALUE17";
        public const string COL_VALUE18 = "VALUE18";
        public const string COL_VALUE19 = "VALUE19";
        public const string COL_VALUE20 = "VALUE20";
        public const string COL_VALUE21 = "VALUE21";
        public const string COL_VALUE22 = "VALUE22";
        public const string COL_VALUE23 = "VALUE23";
        public const string COL_VALUE24 = "VALUE24";
        public const string COL_VALUE25 = "VALUE25";
        public const string COL_VALUE26 = "VALUE26";
        public const string COL_VALUE27 = "VALUE27";
        public const string COL_VALUE28 = "VALUE28";
        public const string COL_VALUE29 = "VALUE29";
        public const string COL_VALUE30 = "VALUE30";
        public const string COL_VALUE31 = "VALUE31";
        public const string COL_VALUE32 = "VALUE32";
        public const string COL_VALUE33 = "VALUE33";
        public const string COL_VALUE34 = "VALUE34";
        public const string COL_VALUE35 = "VALUE35";
        public const string COL_VALUE36 = "VALUE36";
        public const string COL_VALUE37 = "VALUE37";
        public const string COL_VALUE38 = "VALUE38";
        public const string COL_VALUE39 = "VALUE39";
        public const string COL_VALUE40 = "VALUE40";

        public const string COL_RAW_DATA = "RAWDATA";

        private static readonly string[] ValueColumns =
        {
            COL_VALUE01, COL_VALUE02, COL_VALUE03, COL_VALUE04, COL_VALUE05,
            COL_VALUE06, COL_VALUE07, COL_VALUE08, COL_VALUE09, COL_VALUE10,
            COL_VALUE11, COL_VALUE12, COL_VALUE13, COL_VALUE14, COL_VALUE15,
            COL_VALUE16, COL_VALUE17, COL_VALUE18, COL_VALUE19, COL_VALUE20,
            COL_VALUE21, COL_VALUE22, COL_VALUE23, COL_VALUE24, COL_VALUE25,
            COL_VALUE26, COL_VALUE27, COL_VALUE28, COL_VALUE29, COL_VALUE30,
            COL_VALUE31, COL_VALUE32, COL_VALUE33, COL_VALUE34, COL_VALUE35,
            COL_VALUE36, COL_VALUE37, COL_VALUE38, COL_VALUE39, COL_VALUE40
        };

        public override string GetCreateTableSql()
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine($"CREATE TABLE {GetTableName()} (");
            sql.AppendLine($"{COL_MEASURE_DATE} CHAR(8) NOT NULL,");
            sql.AppendLine($"{COL_MEASURE_TIME} CHAR(6) NOT NULL,");
            sql.AppendLine($"{COL_HYDRO_KIND} VARCHAR(40),");
            sql.AppendLine($"{COL_AVG_VELOCITY} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_MIN_VELOCITY} DOUBLE PRECISION,");
            sql.AppendLine($"{COL_MAX_VELOCITY} DOUBLE PRECISION,");

            foreach (string column in ValueColumns)
            {
                sql.AppendLine($"{column} DOUBLE PRECISION,");
            }

            sql.AppendLine($"{COL_RAW_DATA} VARCHAR(8000)");
            sql.Append(')');

            return sql.ToString();
        }

        public override string CheckNewIndexes(FBDatabase db)
        {
            string tableSuffix = GetTableName()[^1..];
            return EnsureIndex(db, $"IDX_RHYDRO{tableSuffix}_TIME_KIND", COL_MEASURE_DATE, COL_MEASURE_TIME, COL_HYDRO_KIND);
        }
    }
}
