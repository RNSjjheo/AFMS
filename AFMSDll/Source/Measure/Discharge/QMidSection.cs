using System.Data;
using System.Text.Json;

namespace AFMSDll
{
    public sealed class QMidSection : _QBase
    {
        /// <summary>현재 유속계에 설정된 최신 측선 목록입니다.</summary>
        public TransectCollection Transects => Configuration.CrossSection.Transects;
        /// <summary>현재 원시자료에서 수집된 측선별 유속정보입니다.</summary>
        public IReadOnlyList<QTransectMeasurement> TransectMeasurements => Measurement.Transects;
        public DiscVerMidSection Version { get; private set; }
        public int CellRangeMin { get; private set; }
        public int CellRangeMax { get; private set; }
        public double ConversionFactor { get; private set; }

        public QMidSection(): base(DischargeMethod.MidSection)
        {
        }

        public override bool TryLoadConfiguration(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            Transects.Clear();
            Configuration.CrossSection.Points.Clear();
            Configuration.CrossSection.Id = -1;
            Configuration.CrossSection.Description = string.Empty;
            Configuration.CrossSection.ZeroPointElevation = 0.0;

            if (!TryLoadMethodConfiguration(db, out error)) return false;
            if (!TryLoadTransects(db, out error)) return false;
            return TryLoadCrossSection(db, out error);
        }

        private bool TryLoadMethodConfiguration(FBDatabase db, out string error)
        {
            Version = default;
            CellRangeMin = 0;
            CellRangeMax = 0;
            ConversionFactor = 0.0;

            string sql = $"SELECT FIRST 1 {FbtAFMSDiscAttrMidSection.COL_DIS_VER},";
            sql += $" {FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MIN},";
            sql += $" {FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MAX},";
            sql += $" {FbtAFMSDiscAttrMidSection.COL_CONVERSION_FACTOR}";
            sql += $" FROM {FbtAFMSDiscAttrMidSection.TABLE_NAME}";
            sql += $" WHERE {FbtAFMSDiscAttrMidSection.COL_ID} = {Configuration.MethodConfigId}";
            sql += $" AND {FbtAFMSDiscAttrMidSection.COL_HYDRO_ID} = {Configuration.DeviceId}";

            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"중간단면적법 설정을 찾을 수 없습니다: ConfigId={Configuration.MethodConfigId}, DeviceId={Configuration.DeviceId}";
                return false;
            }

            DataRow row = table.Rows[0];
            if (row[FbtAFMSDiscAttrMidSection.COL_CONVERSION_FACTOR] == DBNull.Value)
            {
                error = $"중간단면적법 환산계수가 설정되지 않았습니다: ConfigId={Configuration.MethodConfigId}";
                return false;
            }

            int cellRangeMin = Convert.ToInt32(row[FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MIN]);
            int cellRangeMax = Convert.ToInt32(row[FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MAX]);
            double conversionFactor = Convert.ToDouble(row[FbtAFMSDiscAttrMidSection.COL_CONVERSION_FACTOR]);
            if (cellRangeMin < 1 || cellRangeMax < cellRangeMin)
            {
                error = $"중간단면적법 셀 범위가 올바르지 않습니다: {cellRangeMin}~{cellRangeMax}";
                return false;
            }
            if (!double.IsFinite(conversionFactor) || conversionFactor <= 0.0)
            {
                error = $"중간단면적법 환산계수가 올바르지 않습니다: {conversionFactor}";
                return false;
            }

            Version = (DiscVerMidSection)Convert.ToInt32(row[FbtAFMSDiscAttrMidSection.COL_DIS_VER]);
            CellRangeMin = cellRangeMin;
            CellRangeMax = cellRangeMax;
            ConversionFactor = conversionFactor;
            error = string.Empty;
            return true;
        }

        private bool TryLoadTransects(FBDatabase db, out string error)
        {

            QueryBuilderSelect query = new();
            query.Table = FbtAFMSHydroTransect.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSHydroTransect.COL_DISTANCE_DATAS);
            query.Where(FbtAFMSHydroTransect.COL_HYDRO_ID, "=", Configuration.DeviceId);
            query.OrderByDesc(FbtAFMSHydroTransect.COL_ID);

            DataTable table = db.Execute(query, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"유속계에 설정된 측선 정보를 찾을 수 없습니다: DeviceId={Configuration.DeviceId}";
                return false;
            }

            string json = Convert.ToString(table.Rows[0][FbtAFMSHydroTransect.COL_DISTANCE_DATAS]) ?? string.Empty;
            if (!TransectBuilder.TryBuild(json, out TransectCollection transects))
            {
                error = $"유속계 측선 설정을 읽을 수 없습니다: DeviceId={Configuration.DeviceId}";
                return false;
            }

            Transects.AddRange(transects);
            error = string.Empty;
            return true;
        }

        private bool TryLoadCrossSection(FBDatabase db, out string error)
        {
            QueryBuilderSelect query = new();
            query.Table = FbtAFMSCrossSection.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSCrossSection.COL_ID);
            query.Add(FbtAFMSCrossSection.COL_DESCRIPTION);
            query.Add(FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION);
            query.Add(FbtAFMSCrossSection.COL_POINT_DATA);
            query.OrderByDesc(FbtAFMSCrossSection.COL_ID);

            DataTable table = db.Execute(query, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = "설정된 단면 정보를 찾을 수 없습니다.";
                return false;
            }

            DataRow row = table.Rows[0];
            int crossSectionId = Convert.ToInt32(row[FbtAFMSCrossSection.COL_ID]);
            string description = Convert.ToString(row[FbtAFMSCrossSection.COL_DESCRIPTION]) ?? string.Empty;
            double zeroPointElevation = Convert.ToDouble(row[FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION]);
            string json = Convert.ToString(row[FbtAFMSCrossSection.COL_POINT_DATA]) ?? string.Empty;

            CrossSectionPointCollection points;
            try
            {
                points = CrossSectionPointBuilder.Build(json, zeroPointElevation);
            }
            catch (JsonException ex)
            {
                error = $"단면 설정을 읽을 수 없습니다: ID={crossSectionId}, {ex.Message}";
                return false;
            }

            if (points.Count < 2)
            {
                error = $"단면적 산정에 필요한 단면 좌표가 부족합니다: ID={crossSectionId}";
                return false;
            }

            CrossSection crossSection = Configuration.CrossSection;
            crossSection.Id = crossSectionId;
            crossSection.Description = description;
            crossSection.ZeroPointElevation = zeroPointElevation;
            crossSection.Points.AddRange(points);
            error = string.Empty;
            return true;
        }

        protected override bool TryLoadCalculationMeasurements(
            FBDatabase db,
            out bool loaded,
            out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            Measurement.HasWaterLevel = false;
            Measurement.WaterLevel = 0.0;
            Measurement.WaterLevelDate = default;
            Measurement.WaterLevelTime = default;
            Measurement.Transects.Clear();
            loaded = false;

            string date = Calculation.SlotDate.ToString("yyyyMMdd");
            string time = Calculation.SlotTime.ToString("HHmmss");
            string sql = $"SELECT FIRST 1 {FbtWATERLEVEL.COL_AVG_WATER_LEVEL}";
            sql += $" FROM {FbtWATERLEVEL.TABLE_NAME}";
            sql += $" WHERE {FbtWATERLEVEL.COL_MEASURE_DATE} = '{date}'";
            sql += $" AND {FbtWATERLEVEL.COL_MEASURE_TIME} = '{time}'";
            sql += $" AND {FbtWATERLEVEL.COL_AVG_WATER_LEVEL} IS NOT NULL";

            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = string.Empty;
                return true;
            }

            double waterLevel = Convert.ToDouble(table.Rows[0][FbtWATERLEVEL.COL_AVG_WATER_LEVEL]);
            if (!double.IsFinite(waterLevel))
            {
                error = $"수위값이 올바르지 않습니다: {date} {time}";
                return false;
            }

            Measurement.HasWaterLevel = true;
            Measurement.WaterLevel = waterLevel;
            Measurement.WaterLevelDate = Calculation.SlotDate;
            Measurement.WaterLevelTime = Calculation.SlotTime;
            return TryLoadTransectMeasurements(db, out loaded, out error);
        }

        private bool TryLoadTransectMeasurements(
            FBDatabase db,
            out bool loaded,
            out string error)
        {
            loaded = false;

            string tableName;
            string parentColumn;
            string noColumn;
            string velocityColumn;
            string? positionXColumn = null;
            string? positionYColumn = null;
            string? standardUncertaintyColumn = null;
            string? expandedUncertaintyColumn = null;

            if (Measurement.Table is FbtHYDROMETERMPDS)
            {
                tableName = FbtHYDROMETERMPDSCELL.TABLE_NAME;
                parentColumn = FbtHYDROMETERMPDSCELL.COL_MPDS_ID;
                noColumn = FbtHYDROMETERMPDSCELL.COL_DEV_NO;
                velocityColumn = FbtHYDROMETERMPDSCELL.COL_VELOCITY;
                standardUncertaintyColumn = FbtHYDROMETERMPDSCELL.COL_VSTDUNCERT;
                expandedUncertaintyColumn = FbtHYDROMETERMPDSCELL.COL_VEXTUNCERT;
            }
            else if (Measurement.Table is FbtHYDROMETERVIDEO)
            {
                tableName = FbtHYDROMETERVIDEOCELL.TABLE_NAME;
                parentColumn = FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID;
                noColumn = FbtHYDROMETERVIDEOCELL.COL_CELL_NO;
                velocityColumn = FbtHYDROMETERVIDEOCELL.COL_VELOCITY;
                positionXColumn = FbtHYDROMETERVIDEOCELL.COL_POS_X;
                positionYColumn = FbtHYDROMETERVIDEOCELL.COL_POS_Y;
                standardUncertaintyColumn = FbtHYDROMETERVIDEOCELL.COL_UNCERTAINTY;
            }
            else
            {
                error = $"측선 유속을 읽을 수 없는 측정 테이블입니다: {Measurement.TableName}";
                return false;
            }

            string sql = $"SELECT {noColumn}, {velocityColumn}";
            if (positionXColumn != null) sql += $", {positionXColumn}";
            if (positionYColumn != null) sql += $", {positionYColumn}";
            if (standardUncertaintyColumn != null) sql += $", {standardUncertaintyColumn}";
            if (expandedUncertaintyColumn != null) sql += $", {expandedUncertaintyColumn}";
            sql += $" FROM {tableName}";
            sql += $" WHERE {parentColumn} = {Measurement.SourceId}";
            sql += $" ORDER BY {noColumn}";

            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = string.Empty;
                return true;
            }

            foreach (DataRow row in table.Rows)
            {
                if (row[velocityColumn] == DBNull.Value)
                {
                    Measurement.Transects.Clear();
                    error = string.Empty;
                    return true;
                }

                double velocity = Convert.ToDouble(row[velocityColumn]);
                if (!double.IsFinite(velocity))
                {
                    error = $"측선 유속값이 올바르지 않습니다: 측선 {row[noColumn]}";
                    Measurement.Transects.Clear();
                    return false;
                }

                Measurement.Transects.Add(new QTransectMeasurement
                {
                    No = Convert.ToInt32(row[noColumn]),
                    Velocity = velocity,
                    PositionX = ReadNullableDouble(row, positionXColumn),
                    PositionY = ReadNullableDouble(row, positionYColumn),
                    StandardUncertainty = ReadNullableDouble(row, standardUncertaintyColumn),
                    ExpandedUncertainty = ReadNullableDouble(row, expandedUncertaintyColumn)
                });
            }

            loaded = true;
            error = string.Empty;
            return true;
        }

        private static double? ReadNullableDouble(DataRow row, string? columnName)
        {
            if (columnName == null || row[columnName] == DBNull.Value) return null;
            double value = Convert.ToDouble(row[columnName]);
            return double.IsFinite(value) ? value : null;
        }
    }
}
