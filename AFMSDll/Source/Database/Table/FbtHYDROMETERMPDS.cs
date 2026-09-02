using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtHYDROMETERMPDS : _FBTableBase
    {
        public const string TABLE_NAME = "RHYDROMETERMPDS";

        public const string COL_MEASURE_OK = "MEASURE_OK";
        public const string COL_POINT_CODE = "POINT_CODE";
        public const string COL_DEVICE_COUNT = "DEVICE_COUNT";
        public const string COL_DEVICE_VOLT = "DEVICE_VOLT";
        public const string COL_WATER_LEVEL = "WATER_LEVEL";

        public const string COL_WIND_SPEED = "WIND_SPEED";
        public const string COL_WIND_GUST = "WIND_GUST";
        public const string COL_WIND_DIRECTION = "WIND_DIRECTION";
        public const string COL_TEMPERATURE = "TEMPERATURE";
        public const string COL_HUMIDITY = "HUMIDITY";
        public const string COL_ATMOSPHERE = "ATMOSPHERE";
        public const string COL_COLLACTOR_RSSI = "COLLACTOR_RSSI";

        public const string COL_RESERVED1 = "RESERVED1";

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_DATE} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_OK} INT,";         // 20260818 추가되는 컬럼
            sql += "\n" + $"{COL_POINT_CODE} VARCHAR(7) NOT NULL,";
            sql += "\n" + $"{COL_DEVICE_COUNT} INT NOT NULL,";
            sql += "\n" + $"{COL_DEVICE_VOLT} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_WATER_LEVEL} DOUBLE PRECISION,";

            sql += "\n" + $"{COL_WIND_SPEED} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_WIND_GUST} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_WIND_DIRECTION} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_TEMPERATURE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_HUMIDITY} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_ATMOSPHERE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_COLLACTOR_RSSI} SMALLINT,";
            sql += "\n" + $"{COL_RESERVED1} DOUBLE PRECISION,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + $")";

            return sql;
        }

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string CheckNewColumn(FBDatabase db)
        {
            if (!HasColumn(db, COL_MEASURE_OK))
            {
                return AddColumn(db, COL_MEASURE_OK, "INT");
            }

            return "";
        }

        public override string CheckNewIndexes(FBDatabase db)
        {
            return EnsureIndex(db, "IDX_MPDS_MEASURE_TIME", COL_MEASURE_DATE, COL_MEASURE_TIME, COL_ID);
        }
    }
}
