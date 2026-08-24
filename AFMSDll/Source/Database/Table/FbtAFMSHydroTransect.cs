using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtAFMSHydroTransect : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_HYDRO_TRANSECT";

        public const string COL_HYDRO_ID = "HYDRO_ID";
        public const string COL_TRANSECT_COUNT = "TRANSECT_COUNT";
        public const string COL_DISTANCE_DATAS = "DISTANCE_DATAS";
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
            sql += "\n" + $"{COL_HYDRO_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_TRANSECT_COUNT} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_DISTANCE_DATAS} BLOB SUB_TYPE TEXT CHARACTER SET UTF8,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_ID})";
            sql += "\n" + ")";

            return sql;
        }

    }
}
