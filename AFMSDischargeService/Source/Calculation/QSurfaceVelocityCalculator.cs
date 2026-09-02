using System.Data;
using System.Text.Json;
using AFMSDll;

namespace AFMSDischargeService
{
    internal sealed class QSurfaceVelocityCalculator : QCalculatorBase
    {
        private const double ChannelMasterVelocityScale = 0.001;

        private sealed class SurfaceVelocityCoefficient
        {
            public double MaxVi { get; init; }
            public double A { get; init; }
            public double B { get; init; }
        }

        public const string VER1_ATTR_NODE1 = "Max Vi";
        public const string VER1_ATTR_NODE2 = "a";
        public const string VER1_ATTR_NODE3 = "b";

        private readonly List<SurfaceVelocityCoefficient> coefficients = new();

        public override bool IsImplemented => true;
        public IReadOnlyList<QTransectMeasurement> TransectMeasurements => Measurement.Transects;
        public DiscVerSurfaceVelo Version { get; private set; }
        public int CellRangeMin { get; private set; }
        public int CellRangeMax { get; private set; }
        public double StandardVelocityUncertainty { get; private set; }
        public double IndexVelocityUncertainty { get; private set; }

        public QSurfaceVelocityCalculator(DateTime calculationStartTime)
            : base(DischargeMethod.SurfaceVelo, calculationStartTime)
        {
        }

        public override bool Calculate(out string error)
        {
            double indexVelocity = 0.0;
            double meanVelocity;
            double area;
            double discharge;
            string formula;
            CrossSection crossSection = Configuration.CrossSection;
            SurfaceVelocityCoefficient coefficient;
            QTransectMeasurement measurement;
            error = string.Empty;

            if (!TryValidateCalculationInputs(out error)) return false;

            foreach (Transect transect in CalculationTransects)
            {
                measurement = TransectMeasurements.First(item => item.No == transect.No);
                indexVelocity += measurement.Velocity;
            }

            indexVelocity /= CalculationTransects.Count;
            if (!TryGetCoefficient(indexVelocity, out coefficient, out error)) return false;

            crossSection.CalculateTransectAreas(Measurement.WaterLevel);
            area = Transects.Sum(item => item.SectionArea);
            meanVelocity = coefficient.A * indexVelocity + coefficient.B;
            discharge = meanVelocity * area;
            formula = $"Q={FormatFormulaNumber(area)}*" +
                $"({FormatFormulaNumber(coefficient.A)}*{FormatFormulaNumber(indexVelocity)}" +
                $"+({FormatFormulaNumber(coefficient.B)}))";

            if (!TryValidateCalculationResults(indexVelocity, meanVelocity, area, discharge, out error)) return false;

            Calculation.CrossSectionArea = area;
            Calculation.Velocity = meanVelocity;
            Calculation.Value = discharge;
            Calculation.Uncertainty = 0.0;
            Calculation.Formula = formula;

            return true;
        }

        private bool TryValidateCalculationInputs(out string error)
        {
            List<CrossSectionPoint> section;
            List<Transect> orderedTransects;
            double sectionStart;
            double sectionEnd;
            double center;
            Transect transect;
            QTransectMeasurement? measurement;

            if (Calculation.SlotId < 0)
            {
                error = "산정 슬롯이 준비되지 않았습니다.";
                return false;
            }
            if (!Measurement.HasWaterLevel || !double.IsFinite(Measurement.WaterLevel))
            {
                error = "산정할 수위자료가 준비되지 않았습니다.";
                return false;
            }
            if (Configuration.CrossSection.Points.Count < 2)
            {
                error = "단면정보가 준비되지 않았습니다.";
                return false;
            }
            if (Transects.Count == 0)
            {
                error = "설정된 측선정보가 없습니다.";
                return false;
            }
            if (CalculationTransects.Count == 0)
            {
                error = "지표유속법 산정에 사용할 측선정보가 없습니다.";
                return false;
            }
            if (TransectMeasurements.Count == 0)
            {
                error = "수집된 측선 유속자료가 없습니다.";
                return false;
            }
            if (coefficients.Count == 0)
            {
                error = "지표유속법 환산계수가 없습니다.";
                return false;
            }

            section = Configuration.CrossSection.Points.OrderBy(item => item.LeftBankDistance).ToList();
            if (section.Any(item =>
                    !double.IsFinite(item.LeftBankDistance) || !double.IsFinite(item.Elevation)))
            {
                error = "단면 좌표에 올바르지 않은 값이 있습니다.";
                return false;
            }

            sectionStart = section[0].LeftBankDistance;
            sectionEnd = section[^1].LeftBankDistance;
            if (sectionStart > 0.0 || sectionEnd <= 0.0)
            {
                error = "단면 좌표에는 좌안 거리 0과 그보다 큰 우안 거리가 포함되어야 합니다.";
                return false;
            }

            orderedTransects = Transects.OrderBy(item => item.CenterLeftBankDistance).ToList();
            for (int index = 0; index < orderedTransects.Count; index++)
            {
                transect = orderedTransects[index];
                center = transect.CenterLeftBankDistance;
                if (!double.IsFinite(center) || center < 0.0 || center > sectionEnd)
                {
                    error = $"{transect.No}번 측선의 중심 위치가 단면 범위를 벗어났습니다.";
                    return false;
                }
                if (index > 0 && orderedTransects[index - 1].CenterLeftBankDistance == center)
                {
                    error = $"측선 중심 위치가 중복되어 있습니다: {center}";
                    return false;
                }
            }

            foreach (Transect calculationTransect in CalculationTransects)
            {
                measurement = TransectMeasurements.FirstOrDefault(item => item.No == calculationTransect.No);
                if (measurement == null)
                {
                    error = $"{calculationTransect.No}번 측선의 유속자료가 없습니다.";
                    return false;
                }
                if (!double.IsFinite(measurement.Velocity))
                {
                    error = $"{calculationTransect.No}번 측선의 유속값이 올바르지 않습니다.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private bool TryGetCoefficient(
            double indexVelocity,
            out SurfaceVelocityCoefficient coefficient,
            out string error)
        {
            SurfaceVelocityCoefficient? selectedCoefficient;

            if (!double.IsFinite(indexVelocity))
            {
                coefficient = null!;
                error = "지표유속 대표값이 올바르지 않습니다.";
                return false;
            }

            selectedCoefficient = coefficients.FirstOrDefault(item => indexVelocity <= item.MaxVi);
            if (selectedCoefficient == null)
            {
                coefficient = null!;
                error = $"지표유속 {indexVelocity}에 적용할 환산계수 구간이 없습니다.";
                return false;
            }

            coefficient = selectedCoefficient;
            error = string.Empty;
            return true;
        }

        private static bool TryValidateCalculationResults(
            double indexVelocity,
            double meanVelocity,
            double area,
            double discharge,
            out string error)
        {
            if (!double.IsFinite(indexVelocity) || !double.IsFinite(meanVelocity) ||
                !double.IsFinite(area) || !double.IsFinite(discharge) || area < 0.0)
            {
                error = "지표유속법 산정 결과가 올바르지 않습니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public override bool TryLoadConfiguration(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            ClearTransects();
            coefficients.Clear();
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
            DataTable table;
            int disVer;
            int cellRangeMin;
            int cellRangeMax;
            double standardVelocityUncertainty;
            double indexVelocityUncertainty;
            List<SurfaceVelocityCoefficient> loadedCoefficients = new();
            string sql;

            Version = default;
            CellRangeMin = 0;
            CellRangeMax = 0;
            StandardVelocityUncertainty = 0.0;
            IndexVelocityUncertainty = 0.0;
            coefficients.Clear();

            sql = $"SELECT FIRST 1 {FbtAFMSDischargeMethodConfig.COL_CONFIG_JSON}";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME}";
            sql += $" WHERE {FbtAFMSDischargeMethodConfig.COL_ID} = {Configuration.MethodConfigId}";
            sql += $" AND {FbtAFMSDischargeMethodConfig.COL_DEVICE_ID} = {Configuration.DeviceId}";
            sql += $" AND {FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD} = '{DischargeMethod.SurfaceVelo}'";

            table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"지표유속법 설정을 찾을 수 없습니다: ConfigId={Configuration.MethodConfigId}, DeviceId={Configuration.DeviceId}";
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(
                    table.Rows[0][FbtAFMSDischargeMethodConfig.COL_CONFIG_JSON].ToText());
                JsonElement calculation = document.RootElement.GetProperty("calculation");
                JsonElement coefficientElements = calculation.GetProperty("Coefficients");

                disVer = calculation.GetProperty("DisVer").GetInt32();
                cellRangeMin = calculation.GetProperty("CellMin").GetInt32();
                cellRangeMax = calculation.GetProperty("CellMax").GetInt32();
                standardVelocityUncertainty = calculation.GetProperty("UcertVst").GetDouble();
                indexVelocityUncertainty = calculation.GetProperty("UcertVindex").GetDouble();

                foreach (JsonElement item in coefficientElements.EnumerateArray())
                {
                    loadedCoefficients.Add(new SurfaceVelocityCoefficient
                    {
                        MaxVi = item.GetProperty("MaxVi").GetDouble(),
                        A = item.GetProperty("A").GetDouble(),
                        B = item.GetProperty("C").GetDouble()
                    });
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
            {
                error = $"지표유속법 설정 JSON을 읽을 수 없습니다: {ex.Message}";
                return false;
            }

            if (cellRangeMin < 1 || cellRangeMax < cellRangeMin)
            {
                error = $"지표유속법 셀 범위가 올바르지 않습니다: {cellRangeMin}~{cellRangeMax}";
                return false;
            }
            if (!Enum.IsDefined((DiscVerSurfaceVelo)disVer))
            {
                error = $"지원하지 않는 지표유속법 버전입니다: {disVer}";
                return false;
            }
            if (!double.IsFinite(standardVelocityUncertainty) || standardVelocityUncertainty < 0.0 ||
                !double.IsFinite(indexVelocityUncertainty) || indexVelocityUncertainty < 0.0)
            {
                error = "지표유속법 불확도 설정이 올바르지 않습니다.";
                return false;
            }
            if (loadedCoefficients.Count == 0 || loadedCoefficients.Any(item =>
                    !double.IsFinite(item.MaxVi) || !double.IsFinite(item.A) || !double.IsFinite(item.B)))
            {
                error = "지표유속법 환산계수 설정이 올바르지 않습니다.";
                return false;
            }

            loadedCoefficients = loadedCoefficients.OrderBy(item => item.MaxVi).ToList();
            if (loadedCoefficients.Select(item => item.MaxVi).Distinct().Count() != loadedCoefficients.Count)
            {
                error = "지표유속법 Max Vi 값이 중복되어 있습니다.";
                return false;
            }

            Version = (DiscVerSurfaceVelo)disVer;
            CellRangeMin = cellRangeMin;
            CellRangeMax = cellRangeMax;
            StandardVelocityUncertainty = standardVelocityUncertainty;
            IndexVelocityUncertainty = indexVelocityUncertainty;
            coefficients.AddRange(loadedCoefficients);
            error = string.Empty;
            return true;
        }

        private bool TryLoadTransects(FBDatabase db, out string error)
        {
            QueryBuilderSelect query = new();
            DataTable table;
            DataRow row;
            string json;
            TransectCollection transects;

            query.Table = FbtAFMSHydroTransect.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSHydroTransect.COL_ID);
            query.Add(FbtAFMSHydroTransect.COL_DISTANCE_DATAS);
            query.Where(FbtAFMSHydroTransect.COL_ID, "=", StartupTransectConfigId);
            query.Where(FbtAFMSHydroTransect.COL_HYDRO_ID, "=", Configuration.DeviceId);

            table = db.Execute(query, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"유속계에 설정된 측선 정보를 찾을 수 없습니다: DeviceId={Configuration.DeviceId}";
                return false;
            }

            row = table.Rows[0];
            json = Convert.ToString(row[FbtAFMSHydroTransect.COL_DISTANCE_DATAS]) ?? string.Empty;
            if (!TransectBuilder.TryBuild(json, out transects))
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
            DataTable table;
            DataRow row;
            int crossSectionId;
            string description;
            double zeroPointElevation;
            string json;
            CrossSectionPointCollection points;
            CrossSection crossSection;

            query.Table = FbtAFMSCrossSection.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSCrossSection.COL_ID);
            query.Add(FbtAFMSCrossSection.COL_DESCRIPTION);
            query.Add(FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION);
            query.Add(FbtAFMSCrossSection.COL_POINT_DATA);
            query.Where(FbtAFMSCrossSection.COL_ID, "=", StartupCrossSectionId);

            table = db.Execute(query, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = "설정된 단면 정보를 찾을 수 없습니다.";
                return false;
            }

            row = table.Rows[0];
            crossSectionId = Convert.ToInt32(row[FbtAFMSCrossSection.COL_ID]);
            description = Convert.ToString(row[FbtAFMSCrossSection.COL_DESCRIPTION]) ?? string.Empty;
            zeroPointElevation = Convert.ToDouble(row[FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION]);
            json = Convert.ToString(row[FbtAFMSCrossSection.COL_POINT_DATA]) ?? string.Empty;

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

            crossSection = Configuration.CrossSection;
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
            string date;
            string time;
            string sql;
            DataTable table;
            double waterLevel;

            ArgumentNullException.ThrowIfNull(db);

            Measurement.HasWaterLevel = false;
            Measurement.WaterLevel = 0.0;
            Measurement.WaterLevelDate = default;
            Measurement.WaterLevelTime = default;
            Measurement.Transects.Clear();
            loaded = false;

            date = Calculation.SlotDate.ToString("yyyyMMdd");
            time = Calculation.SlotTime.ToString("HHmmss");
            sql = $"SELECT FIRST 1 {FbtWATERLEVEL.COL_AVG_WATER_LEVEL}";
            sql += $" FROM {FbtWATERLEVEL.TABLE_NAME}";
            sql += $" WHERE {FbtWATERLEVEL.COL_MEASURE_DATE} = '{date}'";
            sql += $" AND {FbtWATERLEVEL.COL_MEASURE_TIME} = '{time}'";
            sql += $" AND {FbtWATERLEVEL.COL_AVG_WATER_LEVEL} IS NOT NULL";

            table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = string.Empty;
                return true;
            }

            waterLevel = Convert.ToDouble(table.Rows[0][FbtWATERLEVEL.COL_AVG_WATER_LEVEL]);
            if (!double.IsFinite(waterLevel))
            {
                error = $"수위값이 올바르지 않습니다: {date} {time}";
                return false;
            }

            Measurement.HasWaterLevel = true;
            Measurement.WaterLevel = waterLevel;
            Measurement.WaterLevelDate = Calculation.SlotDate;
            Measurement.WaterLevelTime = Calculation.SlotTime;
            if (Measurement.Table is FbtRHYDROMETER)
                return TryLoadChannelMasterVelocity(db, out loaded, out error);
            return TryLoadTransectMeasurements(db, out loaded, out error);
        }

        private bool TryLoadChannelMasterVelocity(FBDatabase db, out bool loaded, out string error)
        {
            loaded = false;
            string date = Measurement.SourceDate.ToString("yyyyMMdd");
            string time = Measurement.SourceTime.ToString("HHmmss");
            string sql = $"SELECT FIRST 1 {FbtRHYDROMETER.COL_AVG_VELOCITY}";
            sql += $" FROM {Measurement.TableName}";
            sql += $" WHERE {FbtRHYDROMETER.COL_MEASURE_DATE} = '{date}'";
            sql += $" AND {FbtRHYDROMETER.COL_MEASURE_TIME} = '{time}'";
            sql += $" AND UPPER(TRIM({FbtRHYDROMETER.COL_HYDRO_KIND})) = 'CHANNELMASTER'";
            sql += $" AND {FbtRHYDROMETER.COL_AVG_VELOCITY} IS NOT NULL";

            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0) return true;

            double velocity = Convert.ToDouble(table.Rows[0][FbtRHYDROMETER.COL_AVG_VELOCITY]) * ChannelMasterVelocityScale;
            if (!double.IsFinite(velocity))
            {
                error = $"ChannelMaster 평균 유속값이 올바르지 않습니다: {date} {time}";
                return false;
            }

            foreach (Transect transect in CalculationTransects)
                Measurement.Transects.Add(new QTransectMeasurement { No = transect.No, Velocity = velocity });

            loaded = Measurement.Transects.Count > 0;
            error = string.Empty;
            return true;
        }

        private bool TryLoadTransectMeasurements(
            FBDatabase db,
            out bool loaded,
            out string error)
        {
            string tableName;
            string parentColumn;
            string noColumn;
            string velocityColumn;
            string? positionXColumn = null;
            string? positionYColumn = null;
            string? standardUncertaintyColumn = null;
            string? expandedUncertaintyColumn = null;
            string sql;
            DataTable table;
            double velocity;

            loaded = false;

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

            sql = $"SELECT {noColumn}, {velocityColumn}";
            if (positionXColumn != null) sql += $", {positionXColumn}";
            if (positionYColumn != null) sql += $", {positionYColumn}";
            if (standardUncertaintyColumn != null) sql += $", {standardUncertaintyColumn}";
            if (expandedUncertaintyColumn != null) sql += $", {expandedUncertaintyColumn}";
            sql += $" FROM {tableName}";
            sql += $" WHERE {parentColumn} = {Measurement.SourceId}";
            sql += $" AND {noColumn} BETWEEN {CellRangeMin} AND {CellRangeMax}";
            sql += $" ORDER BY {noColumn}";

            table = db.Execute(sql, out error);
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

                velocity = Convert.ToDouble(row[velocityColumn]);
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
            double value;

            if (columnName == null || row[columnName] == DBNull.Value) return null;
            value = Convert.ToDouble(row[columnName]);
            return double.IsFinite(value) ? value : null;
        }
    }
}
