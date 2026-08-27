namespace AFMSSettings
{
    // 통합 JSON 설정을 기존 그리드 형태로 표시하기 위한 화면 전용 컬럼명입니다.
    internal static class FbtAFMSDiscAttrMidSection
    {
        public const string COL_ID = "ID";
        public const string COL_DIS_VER = "DIS_VER";
        public const string COL_HYDRO_ID = "HYDRO_ID";
        public const string COL_CELL_RANGE_MIN = "CELL_RANGE_MIN";
        public const string COL_CELL_RANGE_MAX = "CELL_RANGE_MAX";
        public const string COL_DIS_ATTR = "DIS_ATTR";
    }

    internal static class FbtAFMSDiscAttrRatingCurve
    {
        public const string COL_ID = "ID";
        public const string COL_DIS_VER = "DIS_VER";
        public const string COL_COEFF_COUNT = "COEFF_COUNT";
        public const string COL_DIS_ATTR = "DIS_ATTR";
    }

    internal static class FbtAFMSDiscAttrSurfaceVelo
    {
        public const string COL_ID = "ID";
        public const string COL_DIS_VER = "DIS_VER";
        public const string COL_HYDRO_ID = "HYDRO_ID";
        public const string COL_CELL_RANGE_MIN = "CELL_RANGE_MIN";
        public const string COL_CELL_RANGE_MAX = "CELL_RANGE_MAX";
        public const string COL_UCERT_V_ST = "UCERT_V_ST";
        public const string COL_UCERT_V_INDEX = "UCERT_V_INDEX";
        public const string COL_DIS_ATTR = "DIS_ATTR";
    }

    internal static class FbtAFMSDiscAttrVelocityDistribution
    {
        public const string COL_ID = "ID";
        public const string COL_DIS_VER = "DIS_VER";
        public const string COL_HYDRO_ID = "HYDRO_ID";
        public const string COL_PHI = "PHI";
        public const string COL_HORIZONTAL_GRID_M = "HORIZONTAL_GRID_M";
        public const string COL_VERTICAL_GRID_M = "VERTICAL_GRID_M";
        public const string COL_MAX_VELOCITY_DEPTH_RATIO = "MAX_VELOCITY_DEPTH_RATIO";
        public const string COL_MIN_POSITIVE_MEASUREMENTS = "MIN_POSITIVE_MEASUREMENTS";
        public const string COL_TRANSECT_NOS = "TRANSECT_NOS";
    }
}
