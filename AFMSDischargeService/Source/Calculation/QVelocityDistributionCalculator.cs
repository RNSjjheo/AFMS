using System.Data;
using System.Text.Json;
using AFMSDll;

namespace AFMSDischargeService
{
    internal sealed class QVelocityDistributionCalculator : QCalculatorBase
    {
        private const double Eps = 1e-9;
        private const double BetaMin = 0.1;
        private const double BetaMax = 80.0;
        private readonly List<int> transectNos = new();

        private sealed record ProfilePoint(double X, double Bed);
        private sealed record Observation(double X, double Depth, double Velocity);
        private sealed record Fit(double Center, double BetaLeft, double BetaRight, double Umax, double Rmse);

        public override bool IsImplemented => true;
        public DiscVerVelocityDistribution Version { get; private set; }
        public double Phi { get; private set; }
        public double HorizontalGridM { get; private set; }
        public double VerticalGridM { get; private set; }
        public double MaxVelocityDepthRatio { get; private set; }
        public VelocityDistributionFitMode FitMode { get; private set; }
        public int MinimumPositiveMeasurements { get; private set; }
        public double? FlowCenterX { get; private set; }
        public double? BetaLeft { get; private set; }
        public double? BetaRight { get; private set; }

        public QVelocityDistributionCalculator(DateTime calculationStartTime)
            : base(DischargeMethod.VeloDist, calculationStartTime) { }

        public override bool Calculate(out string error)
        {
            if (!ValidateInputs(out error)) return false;

            List<CrossSectionPoint> section = Configuration.CrossSection.Points
                .OrderBy(p => p.LeftBankDistance).ToList();
            List<(double Left, double Right)> segments = FindWettedSegments(section, Measurement.WaterLevel);
            if (segments.Count == 0)
            {
                error = "현재 수위에서 침수된 횡단면 구간이 없습니다.";
                return false;
            }

            (double leftBank, double rightBank) = SelectMainChannel(section, segments);
            List<ProfilePoint> profile = BuildProfile(section, Measurement.WaterLevel, leftBank, rightBank);
            List<Observation> observations = BuildObservations(profile, leftBank, rightBank);
            int positives = observations.Count(o => o.Velocity > 0.0);
            if (positives < MinimumPositiveMeasurements)
            {
                error = $"주수로 안의 양의 유속 측선이 부족합니다: 필요={MinimumPositiveMeasurements}, 현재={positives}";
                return false;
            }

            double entropyM = SolveM(Phi);
            if (!TryFit(observations, profile, leftBank, rightBank, entropyM, out Fit fit, out error))
                return false;
            if (!TryIntegrate(profile, leftBank, rightBank, entropyM, fit,
                    out double area, out double discharge, out int nx, out int ny, out error))
                return false;

            double velocity = discharge / area;
            if (!double.IsFinite(area) || !double.IsFinite(discharge) || !double.IsFinite(velocity) ||
                area <= 0.0 || discharge < 0.0)
            {
                error = "유속분포법 산정 결과가 올바르지 않습니다.";
                return false;
            }

            Calculation.CrossSectionArea = area;
            Calculation.Velocity = velocity;
            Calculation.Value = discharge;
            Calculation.Formula = "Q=Σ(u[i,j]*ΔA[i,j])=" + FormatFormulaNumber(discharge) +
                ";A=ΣΔA[i,j]=" + FormatFormulaNumber(area) +
                ";V=Q/A=" + FormatFormulaNumber(velocity) +
                $";grid={nx}x{ny}" +
                ";M=" + FormatFormulaNumber(entropyM) +
                ";Umax=" + FormatFormulaNumber(fit.Umax) +
                ";x0=" + FormatFormulaNumber(fit.Center) +
                ";βL=" + FormatFormulaNumber(fit.BetaLeft) +
                ";βR=" + FormatFormulaNumber(fit.BetaRight);
            error = string.Empty;
            return true;
        }

        public override bool TryLoadConfiguration(FBDatabase db, out string error)
        {
            ArgumentNullException.ThrowIfNull(db);
            ClearTransects();
            transectNos.Clear();
            Configuration.CrossSection.Points.Clear();
            Configuration.CrossSection.Id = -1;

            if (!LoadMethodConfig(db, out error) || !LoadTransects(db, out error)) return false;
            if (!TrySetCalculationTransects(transectNos, out error)) return false;
            return LoadCrossSection(db, out error);
        }

        private bool LoadMethodConfig(FBDatabase db, out string error)
        {
            string sql = $"SELECT FIRST 1 {FbtAFMSDischargeMethodConfig.COL_TRANSECT_CONFIG_ID}," +
                $" {FbtAFMSDischargeMethodConfig.COL_CROSS_SECTION_ID}, {FbtAFMSDischargeMethodConfig.COL_CONFIG_JSON}" +
                $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME}" +
                $" WHERE {FbtAFMSDischargeMethodConfig.COL_ID}={Configuration.MethodConfigId}" +
                $" AND {FbtAFMSDischargeMethodConfig.COL_DEVICE_ID}={Configuration.DeviceId}" +
                $" AND {FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD}='{DischargeMethod.VeloDist}'";
            DataTable table = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"유속분포법 설정을 찾을 수 없습니다: ConfigId={Configuration.MethodConfigId}";
                return false;
            }

            DataRow row = table.Rows[0];
            if (row[0] == DBNull.Value || row[1] == DBNull.Value)
            {
                error = "유속분포법 설정에 측선 또는 단면 설정 ID가 없습니다.";
                return false;
            }

            try
            {
                Configuration.TransectConfigId = Convert.ToInt32(row[0]);
                Configuration.CrossSection.Id = Convert.ToInt32(row[1]);
                using JsonDocument doc = JsonDocument.Parse(row[2].ToText());
                JsonElement c = doc.RootElement.GetProperty("calculation");
                Version = (DiscVerVelocityDistribution)c.GetProperty("DisVer").GetInt32();
                Phi = c.GetProperty("Phi").GetDouble();
                HorizontalGridM = c.GetProperty("HorizontalGridM").GetDouble();
                VerticalGridM = c.GetProperty("VerticalGridM").GetDouble();
                MaxVelocityDepthRatio = c.GetProperty("MaxVelocityDepthRatio").GetDouble();
                FitMode = ReadFitMode(c.GetProperty("FitMode"));
                MinimumPositiveMeasurements = c.GetProperty("MinimumPositiveMeasurements").GetInt32();
                FlowCenterX = ReadNullable(c, "FlowCenterX");
                BetaLeft = ReadNullable(c, "BetaLeft");
                BetaRight = ReadNullable(c, "BetaRight");
                transectNos.AddRange(c.GetProperty("TransectNos").EnumerateArray().Select(v => v.GetInt32()));
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or
                                             KeyNotFoundException or FormatException or OverflowException)
            {
                error = $"유속분포법 설정 JSON을 읽을 수 없습니다: {ex.Message}";
                return false;
            }

            if (!Enum.IsDefined(Version) || !Enum.IsDefined(FitMode) ||
                !double.IsFinite(Phi) || Phi <= 0.5 || Phi >= 1.0 ||
                !double.IsFinite(HorizontalGridM) || HorizontalGridM <= 0.0 ||
                !double.IsFinite(VerticalGridM) || VerticalGridM <= 0.0 ||
                !double.IsFinite(MaxVelocityDepthRatio) || MaxVelocityDepthRatio < 0.0 || MaxVelocityDepthRatio >= 1.0 ||
                MinimumPositiveMeasurements < 1 || transectNos.Count == 0 ||
                transectNos.Any(n => n < 1) || transectNos.Distinct().Count() != transectNos.Count)
            {
                error = "유속분포법 설정값이 올바르지 않습니다.";
                return false;
            }
            if (FitMode == VelocityDistributionFitMode.Manual &&
                (!FlowCenterX.HasValue || !Positive(BetaLeft) || !Positive(BetaRight)))
            {
                error = "수동 적합에는 흐름 중심과 좌·우 β가 필요합니다.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        private bool LoadTransects(FBDatabase db, out string error)
        {
            QueryBuilderSelect query = new() { Table = FbtAFMSHydroTransect.TABLE_NAME, First = 1 };
            query.Add(FbtAFMSHydroTransect.COL_DISTANCE_DATAS);
            query.Where(FbtAFMSHydroTransect.COL_ID, "=", Configuration.TransectConfigId);
            query.Where(FbtAFMSHydroTransect.COL_HYDRO_ID, "=", Configuration.DeviceId);
            DataTable table = db.Execute(query, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0 ||
                !TransectBuilder.TryBuild(table.Rows[0][0].ToText(), out TransectCollection loaded))
            {
                error = $"측선 설정을 읽을 수 없습니다: ID={Configuration.TransectConfigId}";
                return false;
            }
            Transects.AddRange(loaded);
            return true;
        }

        private bool LoadCrossSection(FBDatabase db, out string error)
        {
            QueryBuilderSelect query = new() { Table = FbtAFMSCrossSection.TABLE_NAME, First = 1 };
            query.Add(FbtAFMSCrossSection.COL_DESCRIPTION);
            query.Add(FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION);
            query.Add(FbtAFMSCrossSection.COL_POINT_DATA);
            query.Where(FbtAFMSCrossSection.COL_ID, "=", Configuration.CrossSection.Id);
            DataTable table = db.Execute(query, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (table.Rows.Count == 0)
            {
                error = $"단면 설정을 찾을 수 없습니다: ID={Configuration.CrossSection.Id}";
                return false;
            }
            try
            {
                DataRow row = table.Rows[0];
                double zero = Convert.ToDouble(row[1]);
                CrossSectionPointCollection points = CrossSectionPointBuilder.Build(row[2].ToText(), zero);
                if (points.Count < 2) throw new JsonException("단면 좌표가 부족합니다.");
                Configuration.CrossSection.Description = Convert.ToString(row[0]) ?? string.Empty;
                Configuration.CrossSection.ZeroPointElevation = zero;
                Configuration.CrossSection.Points.AddRange(points);
            }
            catch (JsonException ex)
            {
                error = $"단면 설정을 읽을 수 없습니다: {ex.Message}";
                return false;
            }
            error = string.Empty;
            return true;
        }

        protected override bool TryLoadCalculationMeasurements(FBDatabase db, out bool loaded, out string error)
        {
            Measurement.HasWaterLevel = false;
            Measurement.Transects.Clear();
            loaded = false;
            string date = Calculation.SlotDate.ToString("yyyyMMdd");
            string time = Calculation.SlotTime.ToString("HHmmss");
            string sql = $"SELECT FIRST 1 {FbtWATERLEVEL.COL_AVG_WATER_LEVEL} FROM {FbtWATERLEVEL.TABLE_NAME}" +
                $" WHERE {FbtWATERLEVEL.COL_MEASURE_DATE}='{date}'" +
                $" AND {FbtWATERLEVEL.COL_MEASURE_TIME}='{time}'" +
                $" AND {FbtWATERLEVEL.COL_AVG_WATER_LEVEL} IS NOT NULL";
            DataTable levelTable = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            if (levelTable.Rows.Count == 0) return true;
            double level = Convert.ToDouble(levelTable.Rows[0][0]);
            if (!double.IsFinite(level))
            {
                error = "수위값이 올바르지 않습니다.";
                return false;
            }
            Measurement.HasWaterLevel = true;
            Measurement.WaterLevel = level;
            Measurement.WaterLevelDate = Calculation.SlotDate;
            Measurement.WaterLevelTime = Calculation.SlotTime;
            return LoadVelocities(db, out loaded, out error);
        }

        private bool LoadVelocities(FBDatabase db, out bool loaded, out string error)
        {
            loaded = false;
            string table, parent, no, velocity;
            if (Measurement.Table is FbtHYDROMETERMPDS)
            {
                table = FbtHYDROMETERMPDSCELL.TABLE_NAME;
                parent = FbtHYDROMETERMPDSCELL.COL_MPDS_ID;
                no = FbtHYDROMETERMPDSCELL.COL_DEV_NO;
                velocity = FbtHYDROMETERMPDSCELL.COL_VELOCITY;
            }
            else if (Measurement.Table is FbtHYDROMETERVIDEO)
            {
                table = FbtHYDROMETERVIDEOCELL.TABLE_NAME;
                parent = FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID;
                no = FbtHYDROMETERVIDEOCELL.COL_CELL_NO;
                velocity = FbtHYDROMETERVIDEOCELL.COL_VELOCITY;
            }
            else
            {
                error = $"측선 유속을 읽을 수 없는 테이블입니다: {Measurement.TableName}";
                return false;
            }

            string sql = $"SELECT {no},{velocity} FROM {table} WHERE {parent}={Measurement.SourceId}" +
                $" AND {no} IN ({string.Join(',', CalculationTransects.Select(t => t.No))}) ORDER BY {no}";
            DataTable data = db.Execute(sql, out error);
            if (!string.IsNullOrEmpty(error)) return false;
            foreach (DataRow row in data.Rows)
            {
                if (row[1] == DBNull.Value) return true;
                double value = Convert.ToDouble(row[1]);
                if (!double.IsFinite(value))
                {
                    error = $"측선 {row[0]}의 유속값이 올바르지 않습니다.";
                    return false;
                }
                Measurement.Transects.Add(new QTransectMeasurement { No = Convert.ToInt32(row[0]), Velocity = value });
            }
            loaded = CalculationTransects.All(t => Measurement.Transects.Any(m => m.No == t.No));
            error = string.Empty;
            return true;
        }

        private bool ValidateInputs(out string error)
        {
            if (Calculation.SlotId < 0 || !Measurement.HasWaterLevel ||
                !double.IsFinite(Measurement.WaterLevel) || Configuration.CrossSection.Points.Count < 2 ||
                CalculationTransects.Count == 0 || Measurement.Transects.Count == 0)
            {
                error = "유속분포법 산정 입력자료가 준비되지 않았습니다.";
                return false;
            }
            foreach (Transect t in CalculationTransects)
            {
                QTransectMeasurement? m = Measurement.Transects.FirstOrDefault(v => v.No == t.No);
                if (!double.IsFinite(t.CenterLeftBankDistance) || m == null ||
                    !double.IsFinite(m.Velocity) || m.Velocity < 0.0)
                {
                    error = $"측선 {t.No}의 위치 또는 유속값이 올바르지 않습니다.";
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        // 수리 계산부
        private bool TryFit(IReadOnlyList<Observation> obs, IReadOnlyList<ProfilePoint> profile,
            double left, double right, double entropyM, out Fit fit, out string error)
        {
            double center = FlowCenterX ?? profile.OrderBy(p => p.Bed).First().X;
            if (center <= left || center >= right)
            {
                fit = null!;
                error = $"흐름 중심이 주수로 내부에 있지 않습니다: {center}";
                return false;
            }
            if (FitMode == VelocityDistributionFitMode.AutoAsymmetric && !FlowCenterX.HasValue &&
                obs.Count >= 4 && obs.Select(o => o.X).Distinct().Count() >= 3 &&
                FitJoint(obs, left, right, entropyM, out fit))
            {
                error = string.Empty;
                return true;
            }
            if (FitMode != VelocityDistributionFitMode.Manual)
            {
                if (FitCommonBeta(obs, center, left, right, entropyM, out fit))
                {
                    error = string.Empty;
                    return true;
                }
                error = "공통 β 자동 적합에서 유효한 후보를 찾지 못했습니다.";
                return false;
            }
            if (!Evaluate(obs, center, left, right, BetaLeft!.Value, BetaRight!.Value,
                    entropyM, new double[obs.Count], out double umax, out double rmse))
            {
                fit = null!;
                error = "수동 설정으로 최대유속을 추정할 수 없습니다.";
                return false;
            }
            fit = new Fit(center, BetaLeft.Value, BetaRight.Value, umax, rmse);
            error = string.Empty;
            return true;
        }

        private bool FitCommonBeta(IReadOnlyList<Observation> obs, double center, double left,
            double right, double m, out Fit fit)
        {
            Fit? best = null;
            double[] values = new double[obs.Count];
            for (int i = 0; i < 1200; i++)
            {
                double beta = Math.Exp(Lerp(Math.Log(BetaMin), Math.Log(BetaMax), i, 1200));
                if (!Evaluate(obs, center, left, right, beta, beta, m, values,
                        out double umax, out double rmse)) continue;
                if (best == null || rmse < best.Rmse) best = new Fit(center, beta, beta, umax, rmse);
            }
            fit = best!;
            return best != null;
        }

        private bool FitJoint(IReadOnlyList<Observation> obs, double left, double right,
            double m, out Fit fit)
        {
            List<Observation> ordered = obs.OrderBy(o => o.X).ToList();
            double peak = ordered.Max(o => o.Velocity);
            int first = ordered.FindIndex(o => Close(o.Velocity, peak));
            int last = ordered.FindLastIndex(o => Close(o.Velocity, peak));
            double width = right - left;
            double margin = Math.Max(width * 1e-6, 1e-6);
            double low = Math.Max(Math.Max(left + margin, ordered[0].X + margin),
                first > 0 ? (ordered[first - 1].X + ordered[first].X) / 2 : ordered[first].X + margin);
            double high = Math.Min(Math.Min(right - margin, ordered[^1].X - margin),
                last < ordered.Count - 1 ? (ordered[last].X + ordered[last + 1].X) / 2 : ordered[last].X - margin);
            if (high <= low)
            {
                fit = null!;
                return false;
            }
            double absoluteLow = low, absoluteHigh = high;
            double bl0 = Math.Log(BetaMin), bl1 = Math.Log(BetaMax);
            double br0 = bl0, br1 = bl1;
            Fit? best = null;
            double[] values = new double[obs.Count];
            for (int round = 0; round <= 2; round++)
            {
                Fit? roundBest = null;
                for (int ci = 0; ci < 41; ci++)
                {
                    double center = Lerp(low, high, ci, 41);
                    if (!obs.Any(o => o.X < center - Eps) || !obs.Any(o => o.X > center + Eps)) continue;
                    for (int li = 0; li < 31; li++)
                    for (int ri = 0; ri < 31; ri++)
                    {
                        double betaL = Math.Exp(Lerp(bl0, bl1, li, 31));
                        double betaR = Math.Exp(Lerp(br0, br1, ri, 31));
                        if (!Evaluate(obs, center, left, right, betaL, betaR, m, values,
                                out double umax, out double rmse)) continue;
                        if (roundBest == null || rmse < roundBest.Rmse)
                            roundBest = new Fit(center, betaL, betaR, umax, rmse);
                    }
                }
                if (roundBest == null)
                {
                    fit = null!;
                    return false;
                }
                best = roundBest;
                if (round == 2) break;
                double dc = (high - low) / 40, dl = (bl1 - bl0) / 30, dr = (br1 - br0) / 30;
                low = Math.Max(absoluteLow, best.Center - Math.Max(dc * 2.5, width * 1e-5));
                high = Math.Min(absoluteHigh, best.Center + Math.Max(dc * 2.5, width * 1e-5));
                bl0 = Math.Max(Math.Log(BetaMin), Math.Log(best.BetaLeft) - Math.Max(dl * 2.5, 1e-5));
                bl1 = Math.Min(Math.Log(BetaMax), Math.Log(best.BetaLeft) + Math.Max(dl * 2.5, 1e-5));
                br0 = Math.Max(Math.Log(BetaMin), Math.Log(best.BetaRight) - Math.Max(dr * 2.5, 1e-5));
                br1 = Math.Min(Math.Log(BetaMax), Math.Log(best.BetaRight) + Math.Max(dr * 2.5, 1e-5));
            }
            fit = best!;
            return true;
        }

        private bool Evaluate(IReadOnlyList<Observation> obs, double center, double left, double right,
            double betaL, double betaR, double m, double[] values, out double umax, out double rmse)
        {
            double numerator = 0, denominator = 0, observedMax = 0;
            for (int i = 0; i < obs.Count; i++)
            {
                values[i] = UPlus(obs[i].X, obs[i].Depth, obs[i].Depth, center, left, right, betaL, betaR, m);
                if (!double.IsFinite(values[i]) || values[i] <= 1e-8)
                {
                    umax = rmse = double.NaN;
                    return false;
                }
                numerator += values[i] * obs[i].Velocity;
                denominator += values[i] * values[i];
                observedMax = Math.Max(observedMax, obs[i].Velocity);
            }
            if (denominator <= 0)
            {
                umax = rmse = double.NaN;
                return false;
            }
            umax = Math.Max(numerator / denominator, observedMax);
            double sum = 0;
            for (int i = 0; i < obs.Count; i++)
            {
                double residual = umax * values[i] - obs[i].Velocity;
                sum += residual * residual;
            }
            rmse = Math.Sqrt(sum / obs.Count);
            return double.IsFinite(umax) && double.IsFinite(rmse);
        }

        private bool TryIntegrate(IReadOnlyList<ProfilePoint> profile, double left, double right,
            double m, Fit fit, out double area, out double discharge, out int nx, out int ny, out string error)
        {
            double minBed = profile.Min(p => p.Bed);
            nx = Math.Max(2, (int)Math.Ceiling((right - left) / HorizontalGridM));
            ny = Math.Max(2, (int)Math.Ceiling((Measurement.WaterLevel - minBed) / VerticalGridM));
            double dx = (right - left) / nx, dy = (Measurement.WaterLevel - minBed) / ny;
            area = discharge = 0;
            for (int ix = 0; ix < nx; ix++)
            {
                double x = left + (ix + 0.5) * dx;
                double bed = BedAt(profile, x), depth = Measurement.WaterLevel - bed;
                if (depth <= Eps) continue;
                for (int iy = 0; iy < ny; iy++)
                {
                    double elevation = minBed + (iy + 0.5) * dy;
                    double localHeight = elevation - bed;
                    if (localHeight < 0 || elevation > Measurement.WaterLevel) continue;
                    double u = UPlus(x, depth, localHeight, fit.Center, left, right,
                        fit.BetaLeft, fit.BetaRight, m);
                    if (!double.IsFinite(u)) continue;
                    area += dx * dy;
                    discharge += fit.Umax * u * dx * dy;
                }
            }
            error = area > 0 ? string.Empty : "생성된 격자에 유효한 침수 셀이 없습니다.";
            return area > 0;
        }

        private double UPlus(double x, double depth, double localHeight, double center,
            double left, double right, double betaL, double betaR, double m)
        {
            bool isLeft = x <= center;
            double width = isLeft ? center - left : right - center;
            double beta = isLeft ? betaL : betaR;
            double denominatorY = depth * (1.0 - MaxVelocityDepthRatio);
            if (width <= Eps || denominatorY <= Eps) return double.NaN;
            double y = localHeight / denominatorY;
            double z = Math.Abs(x - center) / width;
            if (z > 1.0 + Eps) return double.NaN;
            double xi = y * Math.Pow(Math.Max(1.0 - z, 0.0), beta) * Math.Exp(beta * z - y + 1.0);
            xi = Math.Clamp(xi, 0.0, 1.0);
            return Math.Log(1.0 + (Math.Exp(m) - 1.0) * xi) / m;
        }

        private List<Observation> BuildObservations(IReadOnlyList<ProfilePoint> profile, double left, double right)
        {
            List<Observation> result = new();
            foreach (Transect t in CalculationTransects)
            {
                if (t.CenterLeftBankDistance < left || t.CenterLeftBankDistance > right) continue;
                double depth = Measurement.WaterLevel - BedAt(profile, t.CenterLeftBankDistance);
                if (depth <= Eps) continue;
                double velocity = Measurement.Transects.First(m => m.No == t.No).Velocity;
                result.Add(new Observation(t.CenterLeftBankDistance, depth, velocity));
            }
            return result;
        }

        private static List<(double Left, double Right)> FindWettedSegments(
            IReadOnlyList<CrossSectionPoint> section, double level)
        {
            List<(double Left, double Right)> result = new();
            double? start = section[0].Elevation <= level ? section[0].LeftBankDistance : null;
            for (int i = 0; i < section.Count - 1; i++)
            {
                CrossSectionPoint a = section[i], b = section[i + 1];
                bool aw = a.Elevation <= level, bw = b.Elevation <= level;
                if (b.LeftBankDistance < a.LeftBankDistance) continue;
                if (b.LeftBankDistance == a.LeftBankDistance)
                {
                    if (!aw && bw) start = b.LeftBankDistance;
                    else if (aw && !bw && start.HasValue && a.LeftBankDistance - start.Value > Eps)
                    {
                        result.Add((start.Value, a.LeftBankDistance));
                        start = null;
                    }
                    continue;
                }
                if (!aw && bw) start = CrossX(a, b, level);
                else if (aw && !bw)
                {
                    double end = CrossX(a, b, level);
                    if (end - (start ?? a.LeftBankDistance) > Eps) result.Add((start ?? a.LeftBankDistance, end));
                    start = null;
                }
            }
            if (start.HasValue && section[^1].LeftBankDistance - start.Value > Eps)
                result.Add((start.Value, section[^1].LeftBankDistance));
            return result;
        }

        private static (double Left, double Right) SelectMainChannel(
            IReadOnlyList<CrossSectionPoint> section, IReadOnlyList<(double Left, double Right)> segments)
        {
            double deepest = section.OrderBy(p => p.Elevation).First().LeftBankDistance;
            return segments.FirstOrDefault(s => deepest >= s.Left && deepest <= s.Right,
                segments.OrderByDescending(s => s.Right - s.Left).First());
        }

        private static List<ProfilePoint> BuildProfile(IEnumerable<CrossSectionPoint> section,
            double level, double left, double right) => section
            .Where(p => p.LeftBankDistance >= left && p.LeftBankDistance <= right)
            .Select(p => new ProfilePoint(p.LeftBankDistance, p.Elevation))
            .Append(new ProfilePoint(left, level)).Append(new ProfilePoint(right, level))
            .GroupBy(p => p.X).Select(g => new ProfilePoint(g.Key, g.Min(p => p.Bed)))
            .OrderBy(p => p.X).ToList();

        private static double BedAt(IReadOnlyList<ProfilePoint> profile, double x)
        {
            if (x <= profile[0].X) return profile[0].Bed;
            if (x >= profile[^1].X) return profile[^1].Bed;
            for (int i = 0; i < profile.Count - 1; i++)
            {
                ProfilePoint a = profile[i], b = profile[i + 1];
                if (x < a.X || x > b.X || b.X <= a.X) continue;
                return a.Bed + (b.Bed - a.Bed) * (x - a.X) / (b.X - a.X);
            }
            return double.NaN;
        }

        private static double CrossX(CrossSectionPoint a, CrossSectionPoint b, double level) =>
            Close(a.Elevation, b.Elevation) ? a.LeftBankDistance :
            a.LeftBankDistance + (level - a.Elevation) *
            (b.LeftBankDistance - a.LeftBankDistance) / (b.Elevation - a.Elevation);

        private static double SolveM(double phi)
        {
            double low = 1e-8, high = 100;
            for (int i = 0; i < 200; i++)
            {
                double mid = (low + high) / 2;
                if (PhiFromM(mid) < phi) low = mid; else high = mid;
            }
            return (low + high) / 2;
        }

        private static double PhiFromM(double m) => m < 1e-4
            ? 0.5 + m / 12.0 - m * m * m / 720.0
            : 1.0 + 1.0 / (Math.Exp(m) - 1.0) - 1.0 / m;

        private static VelocityDistributionFitMode ReadFitMode(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number) return (VelocityDistributionFitMode)value.GetInt32();
            if (Enum.TryParse(value.GetString(), true, out VelocityDistributionFitMode mode)) return mode;
            throw new JsonException("FitMode 값이 올바르지 않습니다.");
        }

        private static double? ReadNullable(JsonElement parent, string name) =>
            !parent.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null
                ? null : value.GetDouble();
        private static bool Positive(double? value) => value.HasValue && double.IsFinite(value.Value) && value > 0;
        private static double Lerp(double a, double b, int index, int count) => a + (b - a) * index / (count - 1.0);
        private static bool Close(double a, double b) => Math.Abs(a - b) <= Math.Max(1e-12, Math.Abs(b) * 1e-10);
    }
}
