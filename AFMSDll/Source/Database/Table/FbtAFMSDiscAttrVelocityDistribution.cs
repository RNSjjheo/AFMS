namespace AFMSDll
{
    public class FbtAFMSDiscAttrVelocityDistribution : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DIS_ATTR_VELO_DIST";

        public const string COL_DIS_VER = "DIS_VER";
        public const string COL_HYDRO_ID = "HYDRO_ID";
        public const string COL_PHI = "PHI";
        public const string COL_HORIZONTAL_GRID_M = "HORIZONTAL_GRID_M";
        public const string COL_VERTICAL_GRID_M = "VERTICAL_GRID_M";
        public const string COL_MAX_VELOCITY_DEPTH_RATIO = "MAX_VELO_DEPTH_RATIO";
        public const string COL_FIT_MODE = "FIT_MODE";
        public const string COL_MIN_POSITIVE_MEASUREMENTS = "MIN_POSITIVE_COUNT";
        public const string COL_FLOW_CENTER_X = "FLOW_CENTER_X";
        public const string COL_BETA_LEFT = "BETA_LEFT";
        public const string COL_BETA_RIGHT = "BETA_RIGHT";
        public const string COL_TRANSECT_NOS = "TRANSECT_NOS";

        public override string GetTableName() => TABLE_NAME;

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_DATE} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_DIS_VER} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_HYDRO_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_PHI} DOUBLE PRECISION NOT NULL,";
            sql += "\n" + $"{COL_HORIZONTAL_GRID_M} DOUBLE PRECISION NOT NULL,";
            sql += "\n" + $"{COL_VERTICAL_GRID_M} DOUBLE PRECISION NOT NULL,";
            sql += "\n" + $"{COL_MAX_VELOCITY_DEPTH_RATIO} DOUBLE PRECISION NOT NULL,";
            sql += "\n" + $"{COL_FIT_MODE} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MIN_POSITIVE_MEASUREMENTS} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_FLOW_CENTER_X} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_BETA_LEFT} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_BETA_RIGHT} DOUBLE PRECISION,";
            sql += "\n" + $"{COL_TRANSECT_NOS} BLOB SUB_TYPE TEXT CHARACTER SET UTF8 NOT NULL,";
            sql += "\n" + $"CONSTRAINT PK_AFMS_DIS_VELO_DIST PRIMARY KEY({COL_ID})";
            sql += "\n" + ")";
            return sql;
        }
    }
}
