using AFMSDll;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WinForms;
using System.Data;
using System.Globalization;
using System.Reflection.Metadata;
using WinFormsLabel = System.Windows.Forms.Label;

namespace AFMSDataViewer
{
    internal sealed class RealtimeResultChart : UserControl
    {
        private const string ChartFontName = "맑은 고딕";

        private sealed record ChartPoint(DateTime Time, double Value, bool IsMissing = false);
        private sealed record ChartSeries(string Name, System.Drawing.Color Color, List<ChartPoint> Points,
            bool SecondaryAxis = false, string? DeviceType = null, int? DeviceId = null, string? DischargeMethod = null);
        private sealed record DischargeDeviceOption(string DeviceType, int DeviceId, string DisplayText)
        {
            public override string ToString() => DisplayText;
        }
        private sealed record DischargeMethodOption(string Method, string DisplayText)
        {
            public override string ToString() => DisplayText;
        }

        private static readonly System.Drawing.Color[] SeriesColors =
        {
            System.Drawing.Color.FromArgb(19, 187, 130), System.Drawing.Color.FromArgb(30, 190, 210),
            System.Drawing.Color.FromArgb(132, 82, 246), System.Drawing.Color.FromArgb(244, 165, 36)
        };

        private readonly ChartMainType chartType;
        private readonly FormsPlot formsPlot = new();
        private readonly AFMSSectionPanel chartSection = new();
        private readonly Button maximizeToggle = new();
        private readonly WinFormsLabel title = new();
        private readonly WinFormsLabel minimum = CreateStatLabel("최소");
        private readonly WinFormsLabel average = CreateStatLabel("평균", true);
        private readonly WinFormsLabel maximum = CreateStatLabel("최대");
        private readonly ComboBox seriesSelector = new();
        private readonly CheckBox compareDischarge = new();
        private readonly ToolTip hoverTip = new() { InitialDelay = 0, ReshowDelay = 0, AutoPopDelay = 5000 };
        private readonly List<ChartSeries> availableSeries = new();
        private DateTime rangeStart;
        private DateTime rangeEnd;
        private bool isMaximized;
        private bool isPopulatingSelectors;

        public event EventHandler? MaximizeRequested;
        public RealtimeResultChartControl TopLayout;
        private TableLayoutPanel uiTpMain;

        public RealtimeResultChart(ChartMainType chartType, DateTime rangeStart, DateTime rangeEnd)
        {
            this.chartType = chartType;
            this.rangeStart = rangeStart;
            this.rangeEnd = rangeEnd;
            Dock = DockStyle.Fill;
            BackColor = System.Drawing.Color.White;
            Margin = Padding.Empty;

            chartSection.Dock = DockStyle.Fill;
            chartSection.Margin = Padding.Empty;
            chartSection.BackColor = System.Drawing.Color.White;
            chartSection.BorderRadius = 8;
            chartSection.BorderColor = System.Drawing.Color.FromArgb(225, 229, 235);
            chartSection.BorderThickness = 1F;
            chartSection.HeaderText = GetTitle();
            chartSection.HeaderBackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            chartSection.HeaderColor = System.Drawing.Color.FromArgb(55, 62, 72);
            chartSection.HeaderLineColor = System.Drawing.Color.FromArgb(225, 229, 235);
            chartSection.SectionStyle = AFMSSectionStyle.FilledHeader;
            chartSection.Font = new System.Drawing.Font(ChartFontName, 9F, System.Drawing.FontStyle.Bold);

            ConfigureMaximizeToggle();

            uiTpMain = new TableLayoutPanel();
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.Margin = Padding.Empty;
            uiTpMain.BackColor = System.Drawing.Color.Transparent;
            uiTpMain.RowStyles.Clear();
            uiTpMain.ColumnStyles.Clear();
            uiTpMain.RowCount = 2;
            uiTpMain.ColumnCount = 1;
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            TopLayout = new RealtimeResultChartControl(chartType);
            TopLayout.Dock = DockStyle.Fill;
            if (chartType == ChartMainType.Discharge)
            {
                TopLayout.uiComboMain.SelectedIndexChanged += (_, _) =>
                {
                    if (isPopulatingSelectors) return;
                    PopulateDischargeMethodSelector();
                    DrawSelectedSeries();
                };
                TopLayout.uiComboSub.SelectedIndexChanged += (_, _) =>
                {
                    if (!isPopulatingSelectors) DrawSelectedSeries();
                };
            }

            ConfigurePlot();
            uiTpMain.Controls.Add(TopLayout, 0, 0);
            uiTpMain.Controls.Add(formsPlot, 0, 1);
            chartSection.ContentLayout.Controls.Add(uiTpMain);
            chartSection.Controls.Add(maximizeToggle);
            maximizeToggle.BringToFront();
            Controls.Add(chartSection);
        }

        private void ConfigureMaximizeToggle()
        {
            maximizeToggle.Size = new System.Drawing.Size(28, 28);
            maximizeToggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            maximizeToggle.FlatStyle = FlatStyle.Flat;
            maximizeToggle.FlatAppearance.BorderSize = 0;
            maximizeToggle.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(232, 237, 243);
            maximizeToggle.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(220, 227, 235);
            maximizeToggle.BackColor = System.Drawing.Color.Transparent;
            maximizeToggle.ForeColor = System.Drawing.Color.FromArgb(55, 62, 72);
            maximizeToggle.Cursor = Cursors.Hand;
            maximizeToggle.TabStop = false;
            maximizeToggle.AccessibleName = "차트 최대화";
            hoverTip.SetToolTip(maximizeToggle, "최대화");
            maximizeToggle.Paint += MaximizeToggle_Paint;
            maximizeToggle.Click += (_, _) =>
            {
                isMaximized = !isMaximized;
                maximizeToggle.AccessibleName = isMaximized ? "차트 기본 크기로 복원" : "차트 최대화";
                hoverTip.SetToolTip(maximizeToggle, isMaximized ? "기본 크기로 복원" : "최대화");
                maximizeToggle.Invalidate();
                MaximizeRequested?.Invoke(this, EventArgs.Empty);
            };

            chartSection.Resize += (_, _) => PositionMaximizeToggle();
            PositionMaximizeToggle();
        }

        private void PositionMaximizeToggle()
        {
            maximizeToggle.Location = new System.Drawing.Point(
                Math.Max(0, chartSection.ClientSize.Width - maximizeToggle.Width - 6),
                Math.Max(0, (chartSection.HeaderHeight - maximizeToggle.Height) / 2));
        }

        private void MaximizeToggle_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using System.Drawing.Pen pen = new(maximizeToggle.ForeColor, 1.4F);

            if (isMaximized)
            {
                e.Graphics.DrawRectangle(pen, 11, 8, 8, 8);
                e.Graphics.DrawRectangle(pen, 8, 11, 8, 8);
            }
            else
            {
                e.Graphics.DrawRectangle(pen, 9, 9, 10, 10);
            }
        }

        public void SetTimeRange(DateTime start, DateTime end)
        {
            if (start >= end) throw new ArgumentException("차트 시작 시각은 종료 시각보다 이전이어야 합니다.");
            rangeStart = start;
            rangeEnd = end;
            LoadData();
        }

        private void ConfigurePlot()
        {
            formsPlot.Dock = DockStyle.Fill;
            formsPlot.Plot.Font.Set(ChartFontName);
            formsPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
            formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
            formsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E1EAF2");
            formsPlot.Plot.Axes.Left.Label.Text = GetUnit();
            formsPlot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
            formsPlot.MouseMove += FormsPlot_MouseMove;
            formsPlot.MouseLeave += (_, _) => hoverTip.Hide(formsPlot);
        }

        public void LoadData()
        {
            try
            {
                using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
                DataTable table = db.Execute(GetSql(), out string error);
                if (!string.IsNullOrEmpty(error)) { ShowMessage(error); return; }

                availableSeries.Clear();
                foreach (IGrouping<string, DataRow> group in table.Rows.Cast<DataRow>()
                    .Where(row => row["SERIES"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["SERIES"].ToText()))
                    .GroupBy(row => row["SERIES"].ToText()))
                {
                    List<ChartPoint> points = group.Reverse().Select(row =>
                    {
                        bool isMissing = row["CHART_VALUE"] == DBNull.Value;
                        double value = isMissing ? 0D : Convert.ToDouble(row["CHART_VALUE"]);
                        return new ChartPoint(ParseSourceTime(row["SOURCE_TIME"]), value, isMissing);
                    }).Where(point => double.IsFinite(point.Value)).ToList();
                    if (points.Count > 0)
                    {
                        DataRow first = group.First();
                        string? deviceType = table.Columns.Contains("DEVICE_TYPE") ? first["DEVICE_TYPE"].ToText().Trim() : null;
                        int? deviceId = table.Columns.Contains("DEVICE_ID") ? Convert.ToInt32(first["DEVICE_ID"]) : null;
                        string? method = table.Columns.Contains("DISCHARGE_METHOD") ? first["DISCHARGE_METHOD"].ToText().Trim() : null;
                        availableSeries.Add(new ChartSeries(group.Key, GetSeriesColor(availableSeries.Count), points,
                            DeviceType: deviceType, DeviceId: deviceId, DischargeMethod: method));
                    }
                }

                if (chartType == ChartMainType.Level && compareDischarge.Checked) AddDischargeComparison(db);
                if (chartType == ChartMainType.Discharge) PopulateDischargeSelectors();
                else PopulateSelector();
                DrawSelectedSeries();
                chartSection.HeaderText = GetTitle();
            }
            catch (Exception ex) { ShowMessage(ex.Message); }
        }

        private void AddDischargeComparison(FBDatabase db)
        {
            DataTable table = db.Execute(GetDischargeSql(), out string error);
            if (!string.IsNullOrEmpty(error)) return;
            List<ChartPoint> points = table.Rows.Cast<DataRow>()
                .GroupBy(row => ParseSourceTime(row["SOURCE_TIME"]))
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    double[] values = group.Where(row => row["CHART_VALUE"] != DBNull.Value)
                        .Select(row => Convert.ToDouble(row["CHART_VALUE"])).Where(double.IsFinite).ToArray();
                    return new ChartPoint(group.Key, values.Length == 0 ? 0D : values.Average(), values.Length == 0);
                }).ToList();
            if (points.Count > 0) availableSeries.Add(new ChartSeries("유량 비교", SeriesColors[0], points, true));
        }

        private void PopulateSelector()
        {
            string? selected = seriesSelector.SelectedItem?.ToString();
            seriesSelector.BeginUpdate(); seriesSelector.Items.Clear(); seriesSelector.Items.Add("전체");
            foreach (ChartSeries series in availableSeries.Where(series => !series.SecondaryAxis)) seriesSelector.Items.Add(series.Name);
            seriesSelector.SelectedItem = selected != null && seriesSelector.Items.Contains(selected) ? selected : "전체";
            seriesSelector.EndUpdate();
        }

        private void PopulateDischargeSelectors()
        {
            string? selectedType = (TopLayout.uiComboMain.SelectedItem as DischargeDeviceOption)?.DeviceType;
            int? selectedId = (TopLayout.uiComboMain.SelectedItem as DischargeDeviceOption)?.DeviceId;

            isPopulatingSelectors = true;
            try
            {
                TopLayout.uiComboMain.Items.Clear();
                foreach (DischargeDeviceOption device in availableSeries
                    .Where(series => series.DeviceType != null && series.DeviceId.HasValue)
                    .GroupBy(series => (series.DeviceType!, series.DeviceId!.Value))
                    .Select(group => CreateDeviceOption(group.Key.Item1, group.Key.Item2))
                    .OrderBy(option => option.DeviceType == nameof(MeasurementDeviceType.VelocityMeter) ? 0 : 1)
                    .ThenBy(option => option.DeviceId))
                {
                    TopLayout.uiComboMain.Items.Add(device);
                }

                DischargeDeviceOption? selected = TopLayout.uiComboMain.Items.Cast<DischargeDeviceOption>()
                    .FirstOrDefault(option => option.DeviceType == selectedType && option.DeviceId == selectedId);
                TopLayout.uiComboMain.SelectedItem = selected ?? TopLayout.uiComboMain.Items.Cast<object>().FirstOrDefault();
                PopulateDischargeMethodSelector();
            }
            finally
            {
                isPopulatingSelectors = false;
            }
        }

        private void PopulateDischargeMethodSelector()
        {
            string? selectedMethod = (TopLayout.uiComboSub.SelectedItem as DischargeMethodOption)?.Method;
            DischargeDeviceOption? device = TopLayout.uiComboMain.SelectedItem as DischargeDeviceOption;

            bool wasPopulating = isPopulatingSelectors;
            isPopulatingSelectors = true;
            try
            {
                TopLayout.uiComboSub.Items.Clear();
                TopLayout.uiComboSub.Items.Add("전체");
                if (device != null)
                {
                    foreach (string method in availableSeries
                        .Where(series => series.DeviceType == device.DeviceType && series.DeviceId == device.DeviceId)
                        .Where(series => !string.IsNullOrWhiteSpace(series.DischargeMethod))
                        .Select(series => series.DischargeMethod!)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(GetDischargeMethodOrder))
                    {
                        TopLayout.uiComboSub.Items.Add(new DischargeMethodOption(method, GetDischargeMethodDisplay(method)));
                    }
                }

                DischargeMethodOption? selected = TopLayout.uiComboSub.Items.Cast<object>()
                    .OfType<DischargeMethodOption>().FirstOrDefault(option => option.Method == selectedMethod);
                TopLayout.uiComboSub.SelectedItem = selected ?? TopLayout.uiComboSub.Items[0];
            }
            finally
            {
                isPopulatingSelectors = wasPopulating;
            }
        }

        private static DischargeDeviceOption CreateDeviceOption(string deviceType, int deviceId)
        {
            string display = deviceType switch
            {
                nameof(MeasurementDeviceType.VelocityMeter) => $"{deviceId}번 유속계",
                nameof(MeasurementDeviceType.WaterLevelGauge) => deviceId > 0 ? $"{deviceId}번 수위계" : "수위계",
                _ => $"{deviceType} {deviceId}"
            };
            return new DischargeDeviceOption(deviceType, deviceId, display);
        }

        private static string GetDischargeMethodDisplay(string method) =>
            Enum.TryParse(method, true, out DischargeMethod parsed) ? EnumPaser.GetKorString(parsed) : method;

        private static int GetDischargeMethodOrder(string method) =>
            Enum.TryParse(method, true, out DischargeMethod parsed) ? (int)parsed : int.MaxValue;

        private void DrawSelectedSeries()
        {
            string selected = seriesSelector.SelectedItem?.ToString() ?? "전체";
            formsPlot.Plot.Clear();
            formsPlot.Plot.Axes.Right.IsVisible = false;
            List<ChartSeries> visible = GetVisibleSeries(selected);

            foreach (ChartSeries source in visible)
            {
                double[] xs = source.Points.Select(point => point.Time.ToOADate()).ToArray();
                double[] ys = source.Points.Select(point => point.Value).ToArray();
                Scatter scatter = formsPlot.Plot.Add.Scatter(xs, ys);
                scatter.LegendText = source.Name;
                scatter.Color = ToScottColor(source.Color);
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0;
                scatter.FillY = true;
                scatter.FillYValue = 0;
                scatter.FillYColor = ToScottColor(System.Drawing.Color.FromArgb(45, source.Color));
                AddPointMarkers(source, false, source.Color);
                AddPointMarkers(source, true, System.Drawing.Color.FromArgb(75, 85, 99));
                if (source.SecondaryAxis)
                {
                    scatter.Axes.YAxis = formsPlot.Plot.Axes.Right;
                    formsPlot.Plot.Axes.Right.IsVisible = true;
                    formsPlot.Plot.Axes.Right.Label.Text = "m³/s";
                }
            }

            formsPlot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
            formsPlot.Plot.Axes.AutoScale();
            formsPlot.Plot.Axes.SetLimitsX(rangeStart.ToOADate(), rangeEnd.ToOADate());
            formsPlot.Plot.HideLegend();
            formsPlot.Refresh();
            UpdateStatistics(visible.Where(series => !series.SecondaryAxis).SelectMany(series => series.Points).Select(point => point.Value));
        }

        private void AddPointMarkers(ChartSeries source, bool missing, System.Drawing.Color color)
        {
            ChartPoint[] points = source.Points.Where(point => point.IsMissing == missing).ToArray();
            if (points.Length == 0) return;

            Scatter markers = formsPlot.Plot.Add.Scatter(
                points.Select(point => point.Time.ToOADate()).ToArray(),
                points.Select(point => point.Value).ToArray());
            markers.Color = ToScottColor(color);
            markers.LineWidth = 0;
            markers.MarkerSize = 5;
            if (source.SecondaryAxis) markers.Axes.YAxis = formsPlot.Plot.Axes.Right;
        }

        private List<ChartSeries> GetVisibleSeries(string selected = "전체")
        {
            if (chartType == ChartMainType.Discharge)
            {
                DischargeDeviceOption? device = TopLayout.uiComboMain.SelectedItem as DischargeDeviceOption;
                DischargeMethodOption? method = TopLayout.uiComboSub.SelectedItem as DischargeMethodOption;
                return availableSeries.Where(series => device != null &&
                    series.DeviceType == device.DeviceType && series.DeviceId == device.DeviceId &&
                    (method == null || series.DischargeMethod == method.Method)).ToList();
            }
            return availableSeries.Where(series => selected == "전체" || series.Name == selected || series.SecondaryAxis).ToList();
        }

        private void FormsPlot_MouseMove(object? sender, MouseEventArgs e)
        {
            if (availableSeries.Count == 0 || formsPlot.Width <= 0) return;
            Coordinates coordinates = formsPlot.Plot.GetCoordinates(new Pixel(e.X, e.Y));
            DateTime cursorTime = DateTime.FromOADate(coordinates.X);
            ChartSeries? nearestSeries = null;
            ChartPoint? nearestPoint = null;
            double nearestSeconds = double.MaxValue;
            string selected = seriesSelector.SelectedItem?.ToString() ?? "전체";
            foreach (ChartSeries series in GetVisibleSeries(selected))
            {
                ChartPoint? point = series.Points.MinBy(point => Math.Abs((point.Time - cursorTime).TotalSeconds));
                if (point == null) continue;
                double seconds = Math.Abs((point.Time - cursorTime).TotalSeconds);
                if (seconds >= nearestSeconds) continue;
                nearestSeconds = seconds; nearestPoint = point; nearestSeries = series;
            }
            if (nearestPoint == null || nearestSeries == null) return;
            string missingText = nearestPoint.IsMissing ? " (데이터 없음)" : string.Empty;
            hoverTip.Show($"{nearestSeries.Name}\n{nearestPoint.Value:0.00} {(nearestSeries.SecondaryAxis ? "m³/s" : GetUnit())}{missingText}\n{nearestPoint.Time:yyyy-MM-dd HH:mm}", formsPlot, e.X + 14, e.Y + 14, 1000);
        }

        private void UpdateStatistics(IEnumerable<double> values)
        {
            double[] data = values.ToArray();
            minimum.Text = data.Length == 0 ? "최소\n-" : $"최소\n{data.Min():0.0}";
            average.Text = data.Length == 0 ? "평균\n-" : $"평균\n{data.Average():0.0}";
            maximum.Text = data.Length == 0 ? "최대\n-" : $"최대\n{data.Max():0.0}";
            TopLayout.uiValueMin.Value = data.Length == 0 ? "-" : $"{data.Min():0.0}";
            TopLayout.uiValueAvg.Value = data.Length == 0 ? "-" : $"{data.Average():0.0}";
            TopLayout.uiValueMax.Value = data.Length == 0 ? "-" : $"{data.Max():0.0}";
        }

        private void ShowMessage(string message)
        {
            availableSeries.Clear(); formsPlot.Plot.Clear(); formsPlot.Refresh();
            chartSection.HeaderText = $"{GetTitle()} - 조회 오류: {message}"; UpdateStatistics(Array.Empty<double>());
        }

        private static DateTime ParseSourceTime(object value)
        {
            if (value is DateTime time) return time;
            string text = value.ToText().Trim();
            if (DateTime.TryParseExact(text, new[] { "yyyyMMdd HHmmss", "yyyyMMdd HHmmss.fff" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out time)) return time;
            return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        private string GetSql() => RealtimeChartQueryFactory.Create(chartType, rangeStart, rangeEnd).Build();

        private string GetDischargeSql() => new RealtimeDischargeChartQuery(rangeStart, rangeEnd).Build();

        private string GetTitle() => chartType switch { ChartMainType.Velocity => "유속계", ChartMainType.Level => "수위계", ChartMainType.Discharge => "유량계", _ => "전원" };
        private string GetUnit() => chartType switch { ChartMainType.Velocity => "m/s", ChartMainType.Level => "m", ChartMainType.Discharge => "m³/s", _ => "V" };
        private System.Drawing.Color GetColor() => chartType switch
        {
            ChartMainType.Discharge => System.Drawing.Color.FromArgb(16, 185, 129),
            ChartMainType.Level => System.Drawing.Color.FromArgb(29, 193, 211),
            ChartMainType.Velocity => System.Drawing.Color.FromArgb(139, 92, 246),
            _ => System.Drawing.Color.FromArgb(37, 99, 235)
        };

        private System.Drawing.Color GetSeriesColor(int seriesIndex) => seriesIndex == 0
            ? GetColor()
            : SeriesColors[(seriesIndex - 1) % SeriesColors.Length];
        private static ScottPlot.Color ToScottColor(System.Drawing.Color color) => new(color.R, color.G, color.B, color.A);
        private static WinFormsLabel CreateStatLabel(string caption, bool highlighted = false) => new()
        {
            Text = $"{caption}\n-", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
            Font = new System.Drawing.Font("맑은 고딕", 8F), ForeColor = System.Drawing.Color.FromArgb(58, 91, 120),
            BackColor = highlighted ? System.Drawing.Color.FromArgb(224, 255, 243) : System.Drawing.Color.White,
            Margin = new Padding(2, 8, 2, 8), BorderStyle = BorderStyle.FixedSingle
        };
    }
}
