using System.Data;
using System.Text.Json;
using AFMSDll;

namespace AFMSDischargeService
{
    internal sealed class QRatingCurveCalculator : QCalculatorBase
    {
        private sealed class RatingCurveCoefficient
        {
            public double MaxWaterLevel { get; init; }
            public double A { get; init; }
            public double B { get; init; }
            public double C { get; init; }
        }

        public const string VER1_ATTR_MAX_H = "max h";
        public const string VER1_ATTR_NODE1 = "a";
        public const string VER1_ATTR_NODE2 = "b";
        public const string VER1_ATTR_NODE3 = "c";

        private readonly List<RatingCurveCoefficient> coefficients = new();

        public override bool IsImplemented => true;
        public DiscVerRatingCurve Version { get; private set; }

        public QRatingCurveCalculator(DateTime calculationStartTime)
            : base(DischargeMethod.RatingCurve, calculationStartTime)
        {
        }

        public override bool Calculate(out string error)
        {
            double waterLevel = Measurement.WaterLevel;
            double discharge;
            double area;
            double velocity;
            string formula;
            CrossSectionPointCollection points = Configuration.CrossSection.Points;
            RatingCurveCoefficient coefficient;
            error = string.Empty;

            if (!TryValidateCalculationInputs(out error)) return false;

            if (waterLevel < coefficients[0].B)
            {
                points.WaterLevel = waterLevel;
                area = points.Area;
                discharge = 0.0;
                velocity = 0.0;
                formula = $"Q=0({FormatFormulaNumber(waterLevel)}" +
                    $"<{FormatFormulaNumber(coefficients[0].B)})";

                if (!TryValidateBelowMinimumResults(waterLevel, area, out error)) return false;

                SetCalculationResults(
                    area,
                    velocity,
                    discharge,
                    formula,
                    DischargeCalculationStatus.BelowRatingCurveMinimum,
                    $"수위 {waterLevel:G17}가 첫 구간 최저 적용 수위 {coefficients[0].B:G17}보다 낮아 유량을 0으로 처리했습니다.");
                return true;
            }

            if (!TryGetCoefficient(waterLevel, out coefficient, out error)) return false;
            if (!TryValidateEquationInputs(waterLevel, coefficient, out error)) return false;

            points.WaterLevel = waterLevel;
            area = points.Area;
            discharge = coefficient.A * Math.Pow(waterLevel - coefficient.B, coefficient.C);
            velocity = area > 0.0 ? discharge / area : 0.0;
            formula = $"Q={FormatFormulaNumber(coefficient.A)}*" +
                $"({FormatFormulaNumber(waterLevel)}-({FormatFormulaNumber(coefficient.B)}))" +
                $"^{FormatFormulaNumber(coefficient.C)}";

            if (!TryValidateCalculationResults(
                    waterLevel, coefficient, area, velocity, discharge, out error)) return false;

            SetCalculationResults(area, velocity, discharge, formula);

            return true;
        }

        private void SetCalculationResults(
            double area,
            double velocity,
            double discharge,
            string formula,
            DischargeCalculationStatus status = DischargeCalculationStatus.Calculated,
            string statusMessage = "")
        {
            Calculation.CrossSectionArea = area;
            Calculation.Velocity = velocity;
            Calculation.Value = discharge;
            Calculation.Uncertainty = 0.0;
            Calculation.Status = status;
            Calculation.StatusMessage = statusMessage;
            Calculation.Formula = formula;
        }

        private bool TryValidateCalculationInputs(out string error)
        {
            List<CrossSectionPoint> section;
            double sectionStart;
            double sectionEnd;

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
            if (coefficients.Count == 0)
            {
                error = "수위-유량곡선법 환산계수가 없습니다.";
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

            error = string.Empty;
            return true;
        }

        private bool TryGetCoefficient(
            double waterLevel,
            out RatingCurveCoefficient coefficient,
            out string error)
        {
            RatingCurveCoefficient? selectedCoefficient;

            selectedCoefficient = coefficients.FirstOrDefault(item => waterLevel <= item.MaxWaterLevel);
            if (selectedCoefficient == null)
            {
                coefficient = null!;
                error = $"수위 {waterLevel}에 적용할 수위-유량곡선 구간이 없습니다.";
                return false;
            }

            coefficient = selectedCoefficient;
            error = string.Empty;
            return true;
        }

        private static bool TryValidateEquationInputs(
            double waterLevel,
            RatingCurveCoefficient coefficient,
            out string error)
        {
            double baseValue = waterLevel - coefficient.B;
            bool isIntegerExponent = coefficient.C == Math.Truncate(coefficient.C);

            if (baseValue < 0.0 && !isIntegerExponent)
            {
                error = "수위-유량곡선식의 정의역을 벗어났습니다: " +
                    $"h={waterLevel:G17}, b={coefficient.B:G17}, c={coefficient.C:G17}, h-b={baseValue:G17}";
                return false;
            }
            if (baseValue == 0.0 && coefficient.C < 0.0)
            {
                error = "수위-유량곡선식의 정의역을 벗어났습니다: " +
                    $"h={waterLevel:G17}, b={coefficient.B:G17}, c={coefficient.C:G17}, h-b=0";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool TryValidateBelowMinimumResults(
            double waterLevel,
            double area,
            out string error)
        {
            if (!double.IsFinite(area) || area < 0.0)
            {
                error = $"최저 적용 수위 미만 자료의 단면적 결과가 올바르지 않습니다: h={waterLevel:G17}, A(h)={area:G17}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool TryValidateCalculationResults(
            double waterLevel,
            RatingCurveCoefficient coefficient,
            double area,
            double velocity,
            double discharge,
            out string error)
        {
            double baseValue = waterLevel - coefficient.B;

            if (!double.IsFinite(discharge))
            {
                error = "수위-유량곡선법 유량 결과가 올바르지 않습니다: " +
                    $"h={waterLevel:G17}, a={coefficient.A:G17}, b={coefficient.B:G17}, " +
                    $"c={coefficient.C:G17}, h-b={baseValue:G17}, Q={discharge:G17}";
                return false;
            }
            if (!double.IsFinite(area) || area < 0.0)
            {
                error = $"수위-유량곡선법 단면적 결과가 올바르지 않습니다: h={waterLevel:G17}, A(h)={area:G17}";
                return false;
            }
            if (!double.IsFinite(velocity))
            {
                error = $"수위-유량곡선법 평균유속 결과가 올바르지 않습니다: Q={discharge:G17}, A(h)={area:G17}, V={velocity:G17}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public override bool TryLoadConfiguration(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);

            coefficients.Clear();
            Configuration.CrossSection.Points.Clear();
            Configuration.CrossSection.Id = -1;
            Configuration.CrossSection.Description = string.Empty;
            Configuration.CrossSection.ZeroPointElevation = 0.0;

            if (!TryLoadMethodConfiguration(db, out error)) return false;
            return TryLoadCrossSection(db, out error);
        }

        private bool TryLoadMethodConfiguration(FBDatabase db, out string error)
        {
            DataTable table;
            int disVer;
            int coefficientIndex;
            List<RatingCurveCoefficient> loadedCoefficients = new();
            string sql;
            JsonDocument document;
            JsonElement calculation;
            JsonElement coefficientElements;
            JsonElement coefficientElement;

            Version = default;
            coefficients.Clear();

            sql = $"SELECT FIRST 1 {FbtAFMSDischargeMethodConfig.COL_CONFIG_JSON}";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME}";
            sql += $" WHERE {FbtAFMSDischargeMethodConfig.COL_ID} = {Configuration.MethodConfigId}";
            sql += $" AND {FbtAFMSDischargeMethodConfig.COL_DEVICE_ID} = {Configuration.DeviceId}";
            sql += $" AND {FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD} = '{DischargeMethod.RatingCurve}'";

            table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"수위-유량곡선법 설정을 찾을 수 없습니다: ConfigId={Configuration.MethodConfigId}";
                return false;
            }

            try
            {
                document = JsonDocument.Parse(
                    table.Rows[0][FbtAFMSDischargeMethodConfig.COL_CONFIG_JSON].ToText());
                using (document)
                {
                    calculation = document.RootElement.GetProperty("calculation");
                    coefficientElements = calculation.GetProperty("Coefficients");
                    disVer = calculation.GetProperty("DisVer").GetInt32();

                    for (coefficientIndex = 0; coefficientIndex < coefficientElements.GetArrayLength(); coefficientIndex++)
                    {
                        coefficientElement = coefficientElements[coefficientIndex];
                        loadedCoefficients.Add(new RatingCurveCoefficient
                        {
                            MaxWaterLevel = coefficientElement.GetProperty("MaxWaterLevel").GetDouble(),
                            A = coefficientElement.GetProperty("A").GetDouble(),
                            B = coefficientElement.GetProperty("B").GetDouble(),
                            C = coefficientElement.GetProperty("C").GetDouble()
                        });
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
            {
                error = $"수위-유량곡선법 설정 JSON을 읽을 수 없습니다: {ex.Message}";
                return false;
            }

            if (!Enum.IsDefined((DiscVerRatingCurve)disVer))
            {
                error = $"지원하지 않는 수위-유량곡선법 버전입니다: {disVer}";
                return false;
            }
            if (loadedCoefficients.Count == 0 || loadedCoefficients.Any(item =>
                    !double.IsFinite(item.MaxWaterLevel) || !double.IsFinite(item.A) ||
                    !double.IsFinite(item.B) || !double.IsFinite(item.C)))
            {
                error = "수위-유량곡선법 환산계수 설정이 올바르지 않습니다.";
                return false;
            }

            loadedCoefficients = loadedCoefficients.OrderBy(item => item.MaxWaterLevel).ToList();
            if (loadedCoefficients.Select(item => item.MaxWaterLevel).Distinct().Count() != loadedCoefficients.Count)
            {
                error = "수위-유량곡선법 최대 수위 값이 중복되어 있습니다.";
                return false;
            }
            if (loadedCoefficients[0].B > loadedCoefficients[0].MaxWaterLevel)
            {
                error = "수위-유량곡선법 첫 구간의 최저 적용 수위가 최대 수위보다 큽니다.";
                return false;
            }
            if (loadedCoefficients[0].C <= 0.0)
            {
                error = "수위-유량곡선법 첫 구간의 지수 c는 0보다 커야 합니다.";
                return false;
            }

            Version = (DiscVerRatingCurve)disVer;
            coefficients.AddRange(loadedCoefficients);
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
            query.OrderByDesc(FbtAFMSCrossSection.COL_ID);

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
            DataTable table;
            double waterLevel;
            string date;
            string time;
            string sql;

            ArgumentNullException.ThrowIfNull(db);

            Measurement.HasWaterLevel = false;
            Measurement.WaterLevel = 0.0;
            Measurement.WaterLevelDate = default;
            Measurement.WaterLevelTime = default;
            Measurement.Transects.Clear();
            loaded = false;

            date = Measurement.SourceDate.ToString("yyyyMMdd");
            time = Measurement.SourceTime.ToString("HHmmss");
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
            Measurement.WaterLevelDate = Measurement.SourceDate;
            Measurement.WaterLevelTime = Measurement.SourceTime;
            loaded = true;
            error = string.Empty;
            return true;
        }
    }
}
