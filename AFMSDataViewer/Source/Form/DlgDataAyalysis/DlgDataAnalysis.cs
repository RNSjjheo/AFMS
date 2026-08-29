using System.Data;
using System.Globalization;
using AFMSDll;
using ScottPlot.WinForms;

namespace AFMSDataViewer
{
    internal class DlgDataAnalysis : AFMSForm
    {
        private sealed record SectionContext(
            CrossSectionPointCollection Points,
            TransectCollection Transects,
            double? WaterLevel,
            string Message);

        private readonly AFMSTabControl analysisTabs = new()
        {
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 9F),
            HeaderHeight = 40,
            TabSizingMode = AFMSTabSizingMode.Fill,
            AccentColor = Color.FromArgb(139, 92, 246),
            SizeMode = TabSizeMode.Fixed
        };
        private readonly MeasurementDataHub? measurementDataHub;
        private readonly VelocityMeasurement? velocityMeasurement;

        public ChartMainType SourceChartType { get; }
        public RealtimeChartSeries SelectedSeries { get; }
        public RealtimeChartPoint SelectedPoint { get; }
        public int? TransectNo { get; }

        public DlgDataAnalysis(
            ChartMainType sourceChartType,
            RealtimeChartSeries selectedSeries,
            RealtimeChartPoint selectedPoint,
            int? transectNo = null,
            MeasurementDataHub? measurementDataHub = null,
            VelocityMeasurement? velocityMeasurement = null)
        {
            SourceChartType = sourceChartType;
            SelectedSeries = selectedSeries;
            SelectedPoint = selectedPoint;
            TransectNo = transectNo;
            this.measurementDataHub = measurementDataHub;
            this.velocityMeasurement = velocityMeasurement;

            Text = $"데이터 분석 - {selectedSeries.Name} ({selectedPoint.Time:yyyy-MM-dd HH:mm})";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1000, 700);
            MinimumSize = new Size(760, 520);
            ShowMinimizeButton = false;
            ShowInfoButton = false;
            ShowInTaskbar = false;
            Controls.Add(analysisTabs);
            ConfigureAnalysisTabs();
        }

        private void ConfigureAnalysisTabs()
        {
            if (SourceChartType == ChartMainType.Velocity && velocityMeasurement != null)
            {
                SectionContext section = LoadSectionContext();
                analysisTabs.TabPages.Add(CreateVelocityPage());
                analysisTabs.TabPages.Add(CreateCrossSectionPage(section));
                analysisTabs.TabPages.Add(CreateMainFlowPage(section));
                return;
            }

            if (SourceChartType == ChartMainType.Discharge)
            {
                analysisTabs.TabPages.Add(new TabPage("유량 분석"));
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(SourceChartType), SourceChartType,
                "유속 또는 유량 차트에서만 데이터 분석을 실행할 수 있습니다.");
        }

        private TabPage CreateVelocityPage()
        {
            TabPage page = CreatePage("유속");
            AFMSDataGridView grid = new()
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true
            };

            AddGridColumn(grid, "측선", "측선", "0");
            AddGridColumn(grid, "유속", "유속 (m/s)", "N3");
            AddGridColumn(grid, "불확도", "불확도", "N3");

            IReadOnlyList<string> detailNames = velocityMeasurement!.Transects
                .SelectMany(item => item.AdditionalValues?.Keys ?? [])
                .Distinct()
                .ToArray();
            foreach (string detailName in detailNames)
                AddGridColumn(grid, detailName, detailName, "N3");

            foreach (VelocityTransectMeasurement transect in velocityMeasurement.Transects.OrderBy(item => item.TransectNo))
            {
                List<object> values = [
                    transect.TransectNo,
                    transect.IsValid ? transect.Velocity : DBNull.Value,
                    transect.IsValid ? transect.Uncertainty : DBNull.Value
                ];
                foreach (string detailName in detailNames)
                {
                    double? detailValue = null;
                    if (transect.AdditionalValues != null)
                        transect.AdditionalValues.TryGetValue(detailName, out detailValue);
                    values.Add(detailValue.HasValue ? detailValue.Value : DBNull.Value);
                }

                int rowIndex = grid.Rows.Add(values.ToArray());
                if (!transect.IsValid)
                {
                    grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184);
                    grid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
                }
                if (transect.TransectNo == TransectNo)
                    grid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(241, 236, 255);
            }

            page.Controls.Add(grid);
            return page;
        }

        private TabPage CreateCrossSectionPage(SectionContext context)
        {
            TabPage page = CreatePage("단면");
            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = new Padding(8)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label summary = new()
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(71, 85, 105),
                Text = BuildSectionSummary(context)
            };
            AFMSAreaChart chart = new() { Dock = DockStyle.Fill };
            chart.SetData(context.Points);
            chart.SetTransectMarkers(context.Transects.Select(item =>
                new AFMSChartTransectMarker(item.No, item.CenterLeftBankDistance)));

            layout.Controls.Add(summary, 0, 0);
            layout.Controls.Add(chart, 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateMainFlowPage(SectionContext context)
        {
            TabPage page = CreatePage("주흐름");
            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            VelocityTransectMeasurement[] valid = velocityMeasurement!.Transects
                .Where(item => item.IsValid)
                .OrderBy(item => item.TransectNo)
                .ToArray();
            VelocityTransectMeasurement? main = valid.MaxBy(item => Math.Abs(item.Velocity));
            Label summary = new()
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                Text = main == null
                    ? "주흐름을 판단할 수 있는 유효한 유속 자료가 없습니다."
                    : $"주흐름  {main.TransectNo}번 측선   {main.Velocity:N3} m/s"
            };

            FormsPlot plot = new() { Dock = DockStyle.Fill };
            plot.Plot.Font.Set("맑은 고딕");
            plot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
            plot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
            plot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E1EAF2");
            if (valid.Length > 0)
            {
                double[] xs = valid.Select(item => GetTransectPosition(context.Transects, item.TransectNo)).ToArray();
                double[] ys = valid.Select(item => item.Velocity).ToArray();
                var scatter = plot.Plot.Add.Scatter(xs, ys);
                scatter.Color = ScottPlot.Color.FromHex("#8B5CF6");
                scatter.LineWidth = 2F;
                scatter.MarkerSize = 8F;
                plot.Plot.Axes.Bottom.Label.Text = context.Transects.Count > 0 ? "좌안 기준 거리 (m)" : "측선";
                plot.Plot.Axes.Left.Label.Text = "유속 (m/s)";
                plot.Plot.Axes.AutoScale();
            }
            plot.Refresh();

            layout.Controls.Add(summary, 0, 0);
            layout.Controls.Add(plot, 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private SectionContext LoadSectionContext()
        {
            MeasurementSlot? slot = measurementDataHub?.GetSlots(SelectedPoint.Time, SelectedPoint.Time).FirstOrDefault();
            CrossSectionPointCollection points = slot?.CrossSectionDefinition.CreatePointCollection()
                ?? new CrossSectionPointCollection();
            double? waterLevel = velocityMeasurement!.Transects
                .Select(item => item.AdditionalValues != null &&
                                item.AdditionalValues.TryGetValue("수위(m)", out double? value)
                    ? value
                    : null)
                .FirstOrDefault(value => value.HasValue);
            if (!waterLevel.HasValue && slot?.MeasurementDevices.WaterLevelGauge.IsValid == true)
                waterLevel = slot.MeasurementDevices.WaterLevelGauge.Level;
            string message = string.Empty;

            if (points.Count == 0)
            {
                try { points = LoadCrossSection(SelectedPoint.Time); }
                catch (InvalidOperationException ex) { message = ex.Message; }
            }
            points.WaterLevel = waterLevel;

            TransectCollection transects = new();
            try { transects = LoadTransects(velocityMeasurement!.DeviceId, SelectedPoint.Time); }
            catch (InvalidOperationException ex)
            {
                message = string.IsNullOrWhiteSpace(message) ? ex.Message : $"{message} / {ex.Message}";
            }

            return new SectionContext(points, transects, waterLevel, message);
        }

        private static CrossSectionPointCollection LoadCrossSection(DateTime time)
        {
            string date = time.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string clock = time.ToString("HHmmss", CultureInfo.InvariantCulture);
            string sql = $"SELECT FIRST 1 {FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION}," +
                         $" {FbtAFMSCrossSection.COL_POINT_DATA} FROM {FbtAFMSCrossSection.TABLE_NAME}" +
                         $" WHERE ({_FBTableBase.COL_MEASURE_DATE} < '{date}' OR" +
                         $" ({_FBTableBase.COL_MEASURE_DATE} = '{date}' AND {_FBTableBase.COL_MEASURE_TIME} <= '{clock}'))" +
                         $" ORDER BY {_FBTableBase.COL_MEASURE_DATE} DESC, {_FBTableBase.COL_MEASURE_TIME} DESC, {_FBTableBase.COL_ID} DESC";
            using FBDatabase database = FBProvider.Instance.CreateDatabase();
            DataTable table = database.Execute(sql, out string error);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException("단면 설정을 조회하지 못했습니다.");
            if (table.Rows.Count == 0) return new CrossSectionPointCollection();

            DataRow row = table.Rows[0];
            double zero = row[0] == DBNull.Value ? 0D : Convert.ToDouble(row[0], CultureInfo.InvariantCulture);
            string json = Convert.ToString(row[1], CultureInfo.InvariantCulture) ?? string.Empty;
            try { return CrossSectionPointBuilder.Build(json, zero); }
            catch (System.Text.Json.JsonException) { throw new InvalidOperationException("단면 설정 형식이 올바르지 않습니다."); }
        }

        private static TransectCollection LoadTransects(int deviceId, DateTime time)
        {
            if (deviceId <= 0) return new TransectCollection();
            string date = time.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string clock = time.ToString("HHmmss", CultureInfo.InvariantCulture);
            string sql = $"SELECT FIRST 1 {FbtAFMSHydroTransect.COL_DISTANCE_DATAS}" +
                         $" FROM {FbtAFMSHydroTransect.TABLE_NAME}" +
                         $" WHERE {FbtAFMSHydroTransect.COL_HYDRO_ID} = {deviceId}" +
                         $" AND ({_FBTableBase.COL_MEASURE_DATE} < '{date}' OR" +
                         $" ({_FBTableBase.COL_MEASURE_DATE} = '{date}' AND {_FBTableBase.COL_MEASURE_TIME} <= '{clock}'))" +
                         $" ORDER BY {_FBTableBase.COL_MEASURE_DATE} DESC, {_FBTableBase.COL_MEASURE_TIME} DESC, {_FBTableBase.COL_ID} DESC";
            using FBDatabase database = FBProvider.Instance.CreateDatabase();
            DataTable table = database.Execute(sql, out string error);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException("측선 설정을 조회하지 못했습니다.");
            if (table.Rows.Count == 0 || table.Rows[0][0] == DBNull.Value) return new TransectCollection();

            string json = Convert.ToString(table.Rows[0][0], CultureInfo.InvariantCulture) ?? string.Empty;
            if (TransectBuilder.TryBuild(json, out TransectCollection transects)) return transects;
            throw new InvalidOperationException("측선 설정 형식이 올바르지 않습니다.");
        }

        private static string BuildSectionSummary(SectionContext context)
        {
            string level = context.WaterLevel.HasValue ? $"수위 {context.WaterLevel:N3} m" : "수위 자료 없음";
            string section = context.Points.Count > 0 ? $"단면점 {context.Points.Count}개" : "단면 자료 없음";
            string transects = context.Transects.Count > 0 ? $"측선 {context.Transects.Count}개" : "측선 정보 없음";
            string result = $"{level}   |   {section}   |   {transects}";
            return string.IsNullOrWhiteSpace(context.Message) ? result : $"{result}   |   {context.Message}";
        }

        private static double GetTransectPosition(TransectCollection transects, int transectNo) =>
            transects.FirstOrDefault(item => item.No == transectNo)?.CenterLeftBankDistance ?? transectNo;

        private static TabPage CreatePage(string text) => new(text)
        {
            BackColor = Color.White,
            Padding = new Padding(8)
        };

        private static void AddGridColumn(DataGridView grid, string name, string header, string format)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Format = format,
                    NullValue = "-"
                }
            });
        }
    }
}
