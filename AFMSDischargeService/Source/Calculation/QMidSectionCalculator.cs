using System.Data;
using System.Text.Json;
using AFMSDll;

namespace AFMSDischargeService
{
    internal sealed class QMidSectionCalculator : QCalculatorBase
    {
        public override bool IsImplemented => true;

        /// <summary>현재 원시자료에서 수집된 측선별 유속정보입니다.</summary>
        public IReadOnlyList<QTransectMeasurement> TransectMeasurements => Measurement.Transects;
        public DiscVerMidSection Version { get; private set; }
        public int CellRangeMin { get; private set; }
        public int CellRangeMax { get; private set; }
        public double ConversionFactor { get; private set; }

        public QMidSectionCalculator(): base(DischargeMethod.MidSection)
        {
        }

        public override bool Calculate(out string error)
        {
            double discharge = 0.0;
            double area = 0.0;
            CrossSection crossSection = Configuration.CrossSection;
            QTransectMeasurement measurement;

            error = ValidateCalculationInputs();
            if (!string.IsNullOrEmpty(error)) return false;

            crossSection.CalculateTransectAreas(Measurement.WaterLevel);

            foreach (Transect transect in CalculationTransects)
            {
                measurement = TransectMeasurements.First(item => item.No == transect.No);

                area += transect.SectionArea;
                discharge += transect.SectionArea * measurement.Velocity * ConversionFactor;
            }

            if (!double.IsFinite(area) || !double.IsFinite(discharge))
            {
                error = "중간단면적법 산정 결과가 올바르지 않습니다.";
                return false;
            }

            Calculation.CrossSectionArea = area;
            Calculation.Velocity = area > 0.0 ? discharge / area : 0.0;
            Calculation.Value = discharge;
            error = string.Empty;

            return true;
        }

        private string ValidateCalculationInputs()
        {
            List<CrossSectionPoint> section;
            double sectionStart;
            double sectionEnd;
            List<Transect> orderedTransects;
            Transect transect;
            double center;
            QTransectMeasurement? measurement;

            if (Calculation.SlotId < 0) return "산정 슬롯이 준비되지 않았습니다.";
            if (!Measurement.HasWaterLevel) return "산정할 수위자료가 준비되지 않았습니다.";
            if (!double.IsFinite(Measurement.WaterLevel)) return "산정할 수위값이 올바르지 않습니다.";
            if (CellRangeMin < 1 || CellRangeMax < CellRangeMin)
                return $"중간단면적법 셀 범위가 올바르지 않습니다: {CellRangeMin}~{CellRangeMax}";
            if (!double.IsFinite(ConversionFactor) || ConversionFactor <= 0.0)
                return $"중간단면적법 환산계수가 올바르지 않습니다: {ConversionFactor}";
            if (Configuration.CrossSection.Points.Count < 2) return "단면정보가 준비되지 않았습니다.";
            if (Transects.Count == 0) return "설정된 측선정보가 없습니다.";
            if (CalculationTransects.Count == 0) return "산정에 사용할 측선정보가 없습니다.";
            if (TransectMeasurements.Count == 0) return "수집된 측선 유속자료가 없습니다.";

            section = Configuration.CrossSection.Points
                .OrderBy(point => point.LeftBankDistance)
                .ToList();
            if (section.Any(point =>
                    !double.IsFinite(point.LeftBankDistance) || !double.IsFinite(point.Elevation)))
                return "단면 좌표에 올바르지 않은 값이 있습니다.";

            sectionStart = section[0].LeftBankDistance;
            sectionEnd = section[^1].LeftBankDistance;
            if (sectionStart > 0.0 || sectionEnd <= 0.0)
                return "단면 좌표에는 좌안 거리 0과 그보다 큰 우안 거리가 포함되어야 합니다.";

            orderedTransects = Transects
                .OrderBy(transect => transect.CenterLeftBankDistance)
                .ToList();
            for (int index = 0; index < orderedTransects.Count; index++)
            {
                transect = orderedTransects[index];
                center = transect.CenterLeftBankDistance;
                if (!double.IsFinite(center) || center < 0.0 || center > sectionEnd)
                    return $"{transect.No}번 측선의 중심 위치가 단면 범위를 벗어났습니다.";
                if (index > 0 && orderedTransects[index - 1].CenterLeftBankDistance == center)
                    return $"측선 중심 위치가 중복되어 있습니다: {center}";
            }

            foreach (Transect calculationTransect in CalculationTransects)
            {
                measurement = TransectMeasurements
                    .FirstOrDefault(item => item.No == calculationTransect.No);
                if (measurement == null) return $"{calculationTransect.No}번 측선의 유속자료가 없습니다.";
                if (!double.IsFinite(measurement.Velocity))
                    return $"{calculationTransect.No}번 측선의 유속값이 올바르지 않습니다.";
            }

            return string.Empty;
        }

        public override bool TryLoadConfiguration(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            ClearTransects();
            Configuration.CrossSection.Points.Clear();
            Configuration.CrossSection.Id = -1;
            Configuration.CrossSection.Description = string.Empty;
            Configuration.CrossSection.ZeroPointElevation = 0.0;

            if (!TryLoadMethodConfiguration(db, out error)) return false;
            if (!TryLoadTransects(db, out error)) return false;
            if (!TrySetCalculationTransects(CellRangeMin, CellRangeMax, out error)) return false;
            return TryLoadCrossSection(db, out error);
        }

        private bool TryLoadMethodConfiguration(FBDatabase db, out string error)
        {
            Version = default;
            CellRangeMin = 0;
            CellRangeMax = 0;
            ConversionFactor = 0.0;

            string sql = $"SELECT FIRST 1 {FbtAFMSDischargeMethodConfig.COL_CONFIG_JSON}";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME}";
            sql += $" WHERE {FbtAFMSDischargeMethodConfig.COL_ID} = {Configuration.MethodConfigId}";
            sql += $" AND {FbtAFMSDischargeMethodConfig.COL_DEVICE_ID} = {Configuration.DeviceId}";
            sql += $" AND {FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD} = '{DischargeMethod.MidSection}'";

            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"중간단면적법 설정을 찾을 수 없습니다: ConfigId={Configuration.MethodConfigId}, DeviceId={Configuration.DeviceId}";
                return false;
            }

            int disVer;
            int cellRangeMin;
            int cellRangeMax;
            double conversionFactor;
            try
            {
                using JsonDocument document = JsonDocument.Parse(
                    table.Rows[0][FbtAFMSDischargeMethodConfig.COL_CONFIG_JSON].ToText());
                JsonElement calculation = document.RootElement.GetProperty("calculation");
                disVer = calculation.GetProperty("DisVer").GetInt32();
                cellRangeMin = calculation.GetProperty("CellMin").GetInt32();
                cellRangeMax = calculation.GetProperty("CellMax").GetInt32();
                conversionFactor = calculation.GetProperty("ConversionFactor").GetDouble();
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
            {
                error = $"중간단면적법 설정 JSON을 읽을 수 없습니다: {ex.Message}";
                return false;
            }
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

            Version = (DiscVerMidSection)disVer;
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
            query.Add(FbtAFMSHydroTransect.COL_ID);
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

            DataRow row = table.Rows[0];
            string json = Convert.ToString(row[FbtAFMSHydroTransect.COL_DISTANCE_DATAS]) ?? string.Empty;
            if (!TransectBuilder.TryBuild(json, out TransectCollection transects))
            {
                error = $"유속계 측선 설정을 읽을 수 없습니다: DeviceId={Configuration.DeviceId}";
                return false;
            }

            Configuration.TransectConfigId = Convert.ToInt32(row[FbtAFMSHydroTransect.COL_ID]);
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
