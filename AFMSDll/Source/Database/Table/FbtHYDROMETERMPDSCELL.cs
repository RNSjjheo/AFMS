using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtHYDROMETERMPDSCELL : _FBTableBase
    {
        public const string TABLE_NAME = "RHYDROMETERMPDSCELL";

        public const string COL_MPDS_ID = "MPDS_ID";
        public const string COL_DEV_NO = "DEV_NO";
        public const string COL_DEV_STATUS = "DEV_SATAUS";
        public const string COL_DEV_TYPE = "DEV_TYPE";
        public const string COL_BOARD_VOLT = "BOARD_VOLT";

        public const string COL_WATER_LEVEL = "WATER_LEVEL";
        public const string COL_VELOCITY = "VELCITY";
        public const string COL_SNR = "SNR";
        public const string COL_DISCHARGE = "DISCHARGE";
        public const string COL_FVELOCITY = "FVELOCITY";
        public const string COL_FDISCHARGE = "FDISCHARGE";
        public const string COL_OPPOSITE = "OPPOSITE";
        public const string COL_INCLINATION = "INCLINATION";
        public const string COL_RFRSSI = "RFRSSI";
        public const string COL_VSTDUNCERT = "VSTDUNCERT";
        public const string COL_VEXTUNCERT = "VEXTUNCERT";

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MPDS_ID} INT NOT NULL,";
            sql += "\n" + $"{COL_DEV_NO} INT NOT NULL,";
            sql += "\n" + $"{COL_DEV_STATUS} SMALLINT NOT NULL CHECK ({COL_DEV_STATUS} BETWEEN 0 AND 255),";
            sql += "\n" + $"{COL_DEV_TYPE} VARCHAR(16) NOT NULL,";
            sql += "\n" + $"{COL_BOARD_VOLT} DOUBLE PRECISION,";

            sql += "\n" + $"{COL_WATER_LEVEL} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_VELOCITY} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_SNR} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_DISCHARGE} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_FVELOCITY} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_FDISCHARGE} INT NOT NULL,";

            sql += "\n" + $"{COL_OPPOSITE} INTEGER NOT NULL CHECK ({COL_OPPOSITE} BETWEEN 0 AND 65535),";
            sql += "\n" + $"{COL_INCLINATION} INTEGER NOT NULL CHECK ({COL_INCLINATION} BETWEEN 0 AND 65535),";
            sql += "\n" + $"{COL_RFRSSI} SMALLINT,";
            sql += "\n" + $"{COL_VSTDUNCERT} INTEGER NOT NULL CHECK ({COL_VSTDUNCERT} BETWEEN 0 AND 65535),";
            sql += "\n" + $"{COL_VEXTUNCERT} INTEGER NOT NULL CHECK ({COL_VEXTUNCERT} BETWEEN 0 AND 65535),";

            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + $")";

            return sql;
        }

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string CheckNewIndexes(FBDatabase db)
        {
            return EnsureIndex(db, "IDX_MPDSCELL_PARENT_NO", COL_MPDS_ID, COL_DEV_NO);
        }
    }
}
