using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtHYDROMETERVIDEO : _FBTableBase
    {
        public const string TABLE_NAME = "RHYDROMETERVIDEO";

        public const string COL_SITE_CODE = "SITE_CODE";
        public const string COL_DEVICE_TYPE = "DEVICE_TYPE";
        public const string COL_STATUS = "STATUS";
        public const string COL_MEASURE_OK = "MEASURE_OK";
        public const string COL_INTERVAL = "MEAS_INTERVAL";
        public const string COL_WATERLEVEL = "WATERL_LEVEL";
        public const string COL_AREA = "AREA";
        public const string COL_AREA_UNCERTAINTY = "AREA_UNCERTAINTY";
        public const string COL_VELO = "VELOCITY";
        public const string COL_VELO_UNCERTAINTY = "VELOCITY_UNCERTAINTY";
        public const string COL_DISC = "DISCHARGE";
        public const string COL_DISC_UNCERTAINTY = "DISCHARGE_UNCERTAINTY";
        public const string COL_CELL_COUNT = "CELL_COUNT";
        public const string COL_CELL_LENGTH = "CELL_LENGTH";
        public override string GetTableName()
        {
            return TABLE_NAME;
        }
        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_DATE} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_SITE_CODE} VARCHAR(7) NOT NULL,";
            sql += "\n" + $"{COL_DEVICE_TYPE} INT NOT NULL,";
            sql += "\n" + $"{COL_STATUS} INT NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_OK} INT,";         // 20260818 추가되는 컬럼
            sql += "\n" + $"{COL_INTERVAL} INT NOT NULL,";
            sql += "\n" + $"{COL_WATERLEVEL} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_AREA} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_AREA_UNCERTAINTY} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_VELO} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_VELO_UNCERTAINTY} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_DISC} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_DISC_UNCERTAINTY} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_CELL_COUNT} INT NOT NULL,";
            sql += "\n" + $"{COL_CELL_LENGTH} DOUBLE PRECISION,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + $")";

            return sql;
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
            return EnsureIndex(db, "IDX_VIDEO_MEASURE_TIME", COL_MEASURE_DATE, COL_MEASURE_TIME, COL_ID);
        }
    }
}
