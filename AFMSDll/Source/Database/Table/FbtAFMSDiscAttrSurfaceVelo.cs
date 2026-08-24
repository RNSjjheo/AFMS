using System;
using System.Collections.Generic;
using System.Text;

using System;

namespace AFMSDll
{
    public class FbtAFMSDiscAttrSurfaceVelo : _FBTableBase
    {
        //지표유속
        public const string TABLE_NAME = "AFMS_DIS_ATTR_SURFACE_VELO";

        public const string COL_DIS_VER = "DIS_VER";
        public const string COL_HYDRO_ID = "HYDRO_ID";
        public const string COL_CELL_RANGE_MIN = "CELL_RANGE_MIN";
        public const string COL_CELL_RANGE_MAX = "CELL_RANGE_MAX";
        public const string COL_UCERT_V_ST = "UCERT_V_ST";
        public const string COL_UCERT_V_INDEX = "UCERT_V_INDEX";
        public const string COL_COEFF_COUNT = "COEFF_COUNT";
        public const string COL_DIS_ATTR = "DIS_ATTR";

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
            sql += "\n" + $"{COL_DIS_VER} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_HYDRO_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_UCERT_V_ST} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_UCERT_V_INDEX} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_CELL_RANGE_MIN} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_CELL_RANGE_MAX} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_COEFF_COUNT} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_DIS_ATTR} BLOB SUB_TYPE TEXT CHARACTER SET UTF8,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_ID})";
            sql += "\n" + ")";

            return sql;
        }
    }
}