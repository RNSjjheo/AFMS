using AFMSDll;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WinForms;
using Color = System.Drawing.Color;

namespace AFMSDataViewer
{
    /// <summary>
    /// Reusable realtime chart shell. Data access belongs to the application-specific child class.
    /// </summary>
    public abstract class RealtimeResultChart : UserControl
    {
        private const string ChartFontName = "맑은 고딕";
        private static readonly Color[] SeriesColors =
        {
            Color.FromArgb(19, 187, 130), Color.FromArgb(30, 190, 210),
            Color.FromArgb(132, 82, 246), Color.FromArgb(244, 165, 36)
        };

        private readonly FormsPlot formsPlot = new();
        private readonly AFMSSectionPanel chartSection = new();
        private readonly Button maximizeToggle = new();
        private readonly Button closeButton = new();
        private readonly ToolTip hoverTip = new() { InitialDelay = 0, ReshowDelay = 0, AutoPopDelay = 5000 };
        private readonly List<RealtimeChartSeries> availableSeries = new();
        private readonly RealtimeChartLegendController legendController;
        private readonly TableLayoutPanel mainLayout = new();
        private bool isMaximized;
        private double? minimumY;
        private double? maximumY;

        protected RealtimeResultChart(ChartMainType chartType, DateTime rangeStart, DateTime rangeEnd)
        {
            ChartType = chartType;
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Margin = Padding.Empty;

            chartSection.Dock = DockStyle.Fill;
            chartSection.Margin = Padding.Empty;
            chartSection.BackColor = Color.White;
            chartSection.BorderRadius = 8;
            chartSection.BorderColor = Color.FromArgb(225, 229, 235);
            chartSection.BorderThickness = 1F;
            chartSection.HeaderText = TitleText;
            chartSection.HeaderBackColor = Color.FromArgb(245, 247, 250);
            chartSection.HeaderColor = Color.FromArgb(55, 62, 72);
            chartSection.HeaderLineColor = Color.FromArgb(225, 229, 235);
            chartSection.SectionStyle = AFMSSectionStyle.FilledHeader;
            chartSection.Font = new Font(ChartFontName, 9F, System.Drawing.FontStyle.Bold);

            ConfigureHeaderButtons();
            ConfigurePlot();

            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Margin = Padding.Empty;
            mainLayout.BackColor = Color.Transparent;
            mainLayout.RowCount = 3;
            mainLayout.ColumnCount = 1;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RealtimeResultChartControl.FIEXED_CONTROL_HEIGTH));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            TopLayout = new RealtimeResultChartControl(chartType) { Dock = DockStyle.Fill };
            legendController = new RealtimeChartLegendController(chartType, mainLayout, 1);
            legendController.VisibilityChanged += (_, _) => DrawSeries();
            mainLayout.Controls.Add(TopLayout, 0, 0);
            mainLayout.Controls.Add(formsPlot, 0, 2);
            chartSection.ContentLayout.Controls.Add(mainLayout);
            chartSection.Controls.Add(maximizeToggle);
            chartSection.Controls.Add(closeButton);
            maximizeToggle.BringToFront();
            closeButton.BringToFront();
            Controls.Add(chartSection);
        }

        public event EventHandler? MaximizeRequested;
        public event EventHandler? CloseRequested;
        public event EventHandler<RealtimeChartPointEventArgs>? PointDoubleClicked;

        public ChartMainType ChartType { get; }
        public DateTime RangeStart { get; private set; }
        public DateTime RangeEnd { get; private set; }
        public RealtimeResultChartControl TopLayout { get; }
        protected IReadOnlyList<RealtimeChartSeries> AvailableSeries => availableSeries;
        protected virtual string TitleText => ChartType switch
        {
            ChartMainType.Velocity => "유속차트",
            ChartMainType.Level => "수위차트",
            ChartMainType.Discharge => "유량차트",
            _ => "전압차트"
        };
        protected virtual string UnitText => ChartType switch
        {
            ChartMainType.Velocity => "m/s",
            ChartMainType.Level => "m",
            ChartMainType.Discharge => "m³/s",
            _ => "V"
        };

        public abstract void LoadData();

        public void SetTimeRange(DateTime start, DateTime end)
        {
            if (start >= end) throw new ArgumentException("차트 시작 시각은 종료 시각보다 이전이어야 합니다.");
            RangeStart = start;
            RangeEnd = end;
            LoadData();
        }

        public void SetSeries(IEnumerable<RealtimeChartSeries> series)
        {
            availableSeries.Clear();
            availableSeries.AddRange(series);
            chartSection.HeaderText = TitleText;
            legendController.Update(availableSeries, null);
            DrawSeries();
        }

        public void AddSeries(RealtimeChartSeries series)
        {
            availableSeries.Add(series);
            legendController.Update(availableSeries, null);
            DrawSeries();
        }

        public void ClearSeries()
        {
            availableSeries.Clear();
            legendController.Clear();
            DrawSeries();
        }

        public void SetYAxisRange(double? minimum, double? maximum)
        {
            minimumY = minimum;
            maximumY = maximum;
            DrawSeries();
        }

        protected Color GetSeriesColor(int seriesIndex) => seriesIndex == 0
            ? GetThemeColor()
            : SeriesColors[(seriesIndex - 1) % SeriesColors.Length];

        protected void ShowDataError(string message)
        {
            availableSeries.Clear();
            legendController.Clear();
            formsPlot.Plot.Clear();
            formsPlot.Refresh();
            chartSection.HeaderText = $"{TitleText} - 조회 오류: {message}";
            UpdateStatistics(Array.Empty<double>());
        }

        private void DrawSeries()
        {
            formsPlot.Plot.Clear();
            formsPlot.Plot.Axes.Right.IsVisible = false;
            List<RealtimeChartSeries> visible = availableSeries
                .Where(series => legendController.IsVisible(series, null)).ToList();

            foreach (RealtimeChartSeries source in visible)
            {
                double[] xs = source.Points.Select(point => point.Time.ToOADate()).ToArray();
                double[] ys = source.Points.Select(point => point.Value).ToArray();
                if (xs.Length == 0) continue;
                Scatter scatter = formsPlot.Plot.Add.Scatter(xs, ys);
                scatter.LegendText = source.Name;
                scatter.Color = ToScottColor(source.Color);
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0;
                scatter.FillY = true;
                scatter.FillYValue = 0;
                scatter.FillYColor = ToScottColor(Color.FromArgb(45, source.Color));
                AddPointMarkers(source, false, source.Color);
                AddPointMarkers(source, true, Color.FromArgb(75, 85, 99));
                if (source.SecondaryAxis)
                {
                    scatter.Axes.YAxis = formsPlot.Plot.Axes.Right;
                    formsPlot.Plot.Axes.Right.IsVisible = true;
                    formsPlot.Plot.Axes.Right.Label.Text = "m³/s";
                }
            }

            AddUnitAnnotation();
            ConfigureTimeTicks();
            formsPlot.Plot.Axes.AutoScale();
            formsPlot.Plot.Axes.SetLimitsX(RangeStart.ToOADate(), RangeEnd.ToOADate());
            if (minimumY.HasValue && maximumY.HasValue)
                formsPlot.Plot.Axes.SetLimitsY(minimumY.Value, maximumY.Value);
            formsPlot.Plot.HideLegend();
            formsPlot.Refresh();
            UpdateStatistics(visible.Where(series => !series.SecondaryAxis)
                .SelectMany(series => series.Points).Select(point => point.Value));
        }

        private void ConfigurePlot()
        {
            formsPlot.Dock = DockStyle.Fill;
            formsPlot.Margin = Padding.Empty;
            formsPlot.Plot.Font.Set(ChartFontName);
            formsPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
            formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
            formsPlot.Plot.Layout.Fixed(new PixelPadding(15, 15, 26, 16));
            formsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E1EAF2");
            formsPlot.Plot.DataBorder.Color = ScottPlot.Color.FromHex("#B8C9D8");
            formsPlot.Plot.Axes.Left.TickLabelStyle.FontSize = 8F;
            formsPlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 7F;
            formsPlot.MouseMove += FormsPlot_MouseMove;
            formsPlot.DoubleClick += FormsPlot_DoubleClick;
            formsPlot.MouseLeave += (_, _) => hoverTip.Hide(formsPlot);
        }

        private void ConfigureHeaderButtons()
        {
            ConfigureHeaderButton(maximizeToggle, "차트 최대화");
            ConfigureHeaderButton(closeButton, "차트 닫기");
            maximizeToggle.Paint += MaximizeToggle_Paint;
            closeButton.Paint += CloseButton_Paint;
            maximizeToggle.Click += (_, _) =>
            {
                isMaximized = !isMaximized;
                maximizeToggle.Invalidate();
                MaximizeRequested?.Invoke(this, EventArgs.Empty);
            };
            closeButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private static void ConfigureHeaderButton(Button button, string accessibleName)
        {
            button.Size = new Size(28, 28);
            button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button.Location = new Point(0, 1);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.Transparent;
            button.ForeColor = Color.FromArgb(55, 62, 72);
            button.Cursor = Cursors.Hand;
            button.TabStop = false;
            button.AccessibleName = accessibleName;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            closeButton.Left = Math.Max(0, Width - closeButton.Width - 8);
            maximizeToggle.Left = Math.Max(0, closeButton.Left - maximizeToggle.Width - 2);
        }

        private void MaximizeToggle_Paint(object? sender, PaintEventArgs e)
        {
            using Pen pen = new(maximizeToggle.ForeColor, 1.4F);
            if (isMaximized)
            {
                e.Graphics.DrawRectangle(pen, 11, 8, 8, 8);
                e.Graphics.DrawRectangle(pen, 8, 11, 8, 8);
            }
            else e.Graphics.DrawRectangle(pen, 9, 9, 10, 10);
        }

        private void CloseButton_Paint(object? sender, PaintEventArgs e)
        {
            using Pen pen = new(closeButton.ForeColor, 1.4F);
            e.Graphics.DrawLine(pen, 10, 10, 18, 18);
            e.Graphics.DrawLine(pen, 18, 10, 10, 18);
        }

        private void AddPointMarkers(RealtimeChartSeries source, bool missing, Color color)
        {
            RealtimeChartPoint[] points = source.Points.Where(point => point.IsMissing == missing).ToArray();
            if (points.Length == 0) return;
            Scatter markers = formsPlot.Plot.Add.Scatter(
                points.Select(point => point.Time.ToOADate()).ToArray(),
                points.Select(point => point.Value).ToArray());
            markers.Color = ToScottColor(color);
            markers.LineWidth = 0;
            markers.MarkerSize = 5;
            if (source.SecondaryAxis) markers.Axes.YAxis = formsPlot.Plot.Axes.Right;
        }

        private void AddUnitAnnotation()
        {
            Annotation unit = formsPlot.Plot.Add.Annotation(UnitText, Alignment.UpperLeft);
            unit.LabelStyle.FontName = ChartFontName;
            unit.LabelStyle.FontSize = 8F;
            unit.LabelStyle.ForeColor = ScottPlot.Color.FromHex("#64748B");
            unit.LabelStyle.BackgroundColor = ScottPlot.Colors.Transparent;
            unit.OffsetX = -18;
            unit.OffsetY = -14;
        }

        private void ConfigureTimeTicks()
        {
            TimeSpan duration = RangeEnd - RangeStart;
            ScottPlot.TickGenerators.NumericManual ticks = new();
            for (int index = 0; index < 4; index++)
            {
                DateTime time = RangeStart.AddTicks((long)(duration.Ticks * (index / 3D)));
                ticks.AddMajor(time.ToOADate(), duration <= TimeSpan.FromHours(24) ? $"{time:HH:mm}" : $"{time:MM-dd}");
            }
            formsPlot.Plot.Axes.Bottom.TickGenerator = ticks;
        }

        private void FormsPlot_DoubleClick(object? sender, EventArgs e)
        {
            if (e is MouseEventArgs mouse && mouse.Button != MouseButtons.Left) return;
            RealtimeChartPointEventArgs? nearest = FindNearestPoint(formsPlot.PointToClient(Control.MousePosition));
            if (nearest != null) PointDoubleClicked?.Invoke(this, nearest);
        }

        private void FormsPlot_MouseMove(object? sender, MouseEventArgs e)
        {
            RealtimeChartPointEventArgs? nearest = FindNearestPoint(e.Location);
            if (nearest == null) return;
            string missing = nearest.Point.IsMissing ? " (데이터 없음)" : string.Empty;
            hoverTip.Show($"{nearest.Series.Name}\n{nearest.Point.Value:0.00} {(nearest.Series.SecondaryAxis ? "m³/s" : UnitText)}{missing}\n{nearest.Point.Time:yyyy-MM-dd HH:mm}",
                formsPlot, e.X + 14, e.Y + 14, 1000);
        }

        private RealtimeChartPointEventArgs? FindNearestPoint(Point location)
        {
            if (availableSeries.Count == 0 || formsPlot.Plot.LastRender.DataRect.Width <= 0) return null;
            Pixel mouse = new(location.X, location.Y);
            double nearestDistance = Math.Pow(10 * formsPlot.DisplayScale, 2);
            RealtimeChartSeries? nearestSeries = null;
            RealtimeChartPoint? nearestPoint = null;
            foreach (RealtimeChartSeries series in availableSeries.Where(series => legendController.IsVisible(series, null)))
            foreach (RealtimeChartPoint point in series.Points)
            {
                Pixel pixel = formsPlot.Plot.GetPixel(new Coordinates(point.Time.ToOADate(), point.Value),
                    formsPlot.Plot.Axes.Bottom, series.SecondaryAxis ? formsPlot.Plot.Axes.Right : formsPlot.Plot.Axes.Left);
                double distance = Math.Pow(pixel.X - mouse.X, 2) + Math.Pow(pixel.Y - mouse.Y, 2);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestSeries = series;
                nearestPoint = point;
            }
            return nearestSeries == null || nearestPoint == null ? null : new(nearestSeries, nearestPoint);
        }

        private void UpdateStatistics(IEnumerable<double> values)
        {
            double[] data = values.Where(double.IsFinite).ToArray();
            TopLayout.uiValueMin.Text = data.Length == 0 ? "-" : $"{data.Min():0.0} {UnitText} ≤";
            TopLayout.uiValueAvg.Text = data.Length == 0 ? "-" : $"{data.Average():0.0} {UnitText}";
            TopLayout.uiValueMax.Text = data.Length == 0 ? "-" : $"≤ {data.Max():0.0} {UnitText}";
            TopLayout.FitStatisticsWidths();
        }

        private Color GetThemeColor() => ChartType switch
        {
            ChartMainType.Discharge => Color.FromArgb(16, 185, 129),
            ChartMainType.Level => Color.FromArgb(29, 193, 211),
            ChartMainType.Velocity => Color.FromArgb(139, 92, 246),
            _ => Color.FromArgb(37, 99, 235)
        };

        private static ScottPlot.Color ToScottColor(Color color) => new(color.R, color.G, color.B, color.A);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                legendController.Dispose();
                hoverTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
