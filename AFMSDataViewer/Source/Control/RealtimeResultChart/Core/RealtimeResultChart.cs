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
        private readonly System.Windows.Forms.Timer trackingLabelTimer = new() { Interval = 10000 };
        private readonly List<RealtimeChartSeries> availableSeries = new();
        private readonly List<Text> trackingLabels = new();
        private readonly RealtimeChartLegendController legendController;
        private readonly TableLayoutPanel mainLayout = new();
        private readonly MeasurementDataHub? measurementDataHub;
        private VerticalLine? trackingLine;
        private DateTime? trackingTime;
        private DateTime trackingLabelsExpireAt;
        private int refreshPending;
        private bool isMaximized;
        private double? minimumY;
        private double? maximumY;

        protected RealtimeResultChart(ChartMainType chartType, DateTime rangeStart, DateTime rangeEnd,
            MeasurementDataHub? measurementDataHub = null)
        {
            ChartType = chartType;
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
            this.measurementDataHub = measurementDataHub;
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
            trackingLabelTimer.Tick += TrackingLabelTimer_Tick;

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

            if (measurementDataHub != null)
                measurementDataHub.Changed += MeasurementDataHub_Changed;
        }

        public event EventHandler? MaximizeRequested;
        public event EventHandler? CloseRequested;

        public ChartMainType ChartType { get; }
        public DateTime RangeStart { get; private set; }
        public DateTime RangeEnd { get; private set; }
        public RealtimeResultChartControl TopLayout { get; }
        protected IReadOnlyList<RealtimeChartSeries> AvailableSeries => availableSeries;
        protected MeasurementDataHub? MeasurementDataHub => measurementDataHub;
        protected virtual string TitleText
        {
            get
            {
                switch (ChartType)
                {
                    case ChartMainType.Velocity:
                        return "유속차트";

                    case ChartMainType.Level:
                        return "수위차트";

                    case ChartMainType.Discharge:
                        return "유량차트";

                    default:
                        return "전압차트";
                }
            }
        }


        protected virtual string UnitText
        {
            get
            {
                switch (ChartType)
                {
                    case ChartMainType.Velocity:
                        return "m/s";

                    case ChartMainType.Level:
                        return "m";

                    case ChartMainType.Discharge:
                        return "m³/s";

                    default:
                        return "V";
                }
            }
        }

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

        public void SetTrackingTime(DateTime time)
        {
            trackingTime = time;
            if (trackingLine == null)
            {
                DrawSeries();
            }
            else
            {
                trackingLine.X = time.ToOADate();
                formsPlot.Refresh();
            }

            ShowTrackingTooltip(time);
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
            trackingLine = null;
            trackingLabels.Clear();
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

            if (trackingTime.HasValue)
            {
                trackingLine = formsPlot.Plot.Add.VerticalLine(trackingTime.Value.ToOADate());
                trackingLine.Color = ToScottColor(Color.FromArgb(220, 38, 38));
                trackingLine.LineWidth = 1F;
                trackingLine.EnableAutoscale = false;
            }

            AddUnitAnnotation();
            ConfigureTimeTicks();
            formsPlot.Plot.Axes.AutoScale();
            formsPlot.Plot.Axes.SetLimitsX(RangeStart.ToOADate(), RangeEnd.ToOADate());
            if (minimumY.HasValue && maximumY.HasValue)
                formsPlot.Plot.Axes.SetLimitsY(minimumY.Value, maximumY.Value);
            formsPlot.Plot.HideLegend();
            formsPlot.Refresh();

            // 최초 렌더링으로 축과 데이터 영역이 확정된 뒤 마커 라벨의 방향을 계산합니다.
            if (visible.Count == 1 && trackingTime.HasValue && trackingLabelsExpireAt > DateTime.Now)
            {
                AddTrackingLabels(trackingTime.Value);
                formsPlot.Refresh();
            }

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

        private void ShowTrackingTooltip(DateTime time)
        {
            if (trackingLine == null) return;

            int visibleSeriesCount = availableSeries.Count(series =>
                legendController.IsVisible(series, null));
            if (visibleSeriesCount != 1)
            {
                trackingLabelTimer.Stop();
                trackingLabelsExpireAt = DateTime.MinValue;
                RemoveTrackingLabels();
                formsPlot.Refresh();
                return;
            }

            trackingLabelsExpireAt = DateTime.Now.AddSeconds(10);
            AddTrackingLabels(time);
            formsPlot.Refresh();

            trackingLabelTimer.Stop();
            trackingLabelTimer.Start();
        }

        private void AddTrackingLabels(DateTime time)
        {
            RemoveTrackingLabels();

            DateTime slotTime = MeasurementDataHub.AlignToSlot(time);
            foreach (RealtimeChartSeries series in availableSeries
                .Where(series => legendController.IsVisible(series, null)))
            {
                RealtimeChartPoint? point = series.Points
                    .Where(point => MeasurementDataHub.AlignToSlot(point.Time) == slotTime)
                    .MinBy(point => Math.Abs((point.Time - time).Ticks));
                if (point == null || point.IsMissing)
                    continue;

                string unit = series.SecondaryAxis ? "m³/s" : UnitText;
                string text = $"{series.Name}: {point.Value:0.00} {unit}".TrimEnd();
                Text label = formsPlot.Plot.Add.Text(text, point.Time.ToOADate(), point.Value);
                IYAxis yAxis = series.SecondaryAxis
                    ? formsPlot.Plot.Axes.Right
                    : formsPlot.Plot.Axes.Left;
                label.Axes.YAxis = yAxis;

                PixelRect dataRect = formsPlot.Plot.LastRender.DataRect;
                bool placeLeft = false;
                bool placeBelow = false;
                if (dataRect.HasArea)
                {
                    Pixel markerPixel = formsPlot.Plot.GetPixel(
                        new Coordinates(point.Time.ToOADate(), point.Value),
                        formsPlot.Plot.Axes.Bottom,
                        yAxis);
                    float maximumWidth = Math.Max(80F, dataRect.Width / 2F);
                    float estimatedWidth = Math.Clamp(text.Length * 7F + 12F, 80F, maximumWidth);
                    placeLeft = markerPixel.X + estimatedWidth > dataRect.TopRight.X;
                    placeBelow = markerPixel.Y - 32F < dataRect.TopLeft.Y;
                }

                label.OffsetX = placeLeft ? -8F : 8F;
                label.OffsetY = placeBelow ? 8F : -8F;
                label.Alignment = (placeLeft, placeBelow) switch
                {
                    (true, true) => Alignment.UpperRight,
                    (true, false) => Alignment.LowerRight,
                    (false, true) => Alignment.UpperLeft,
                    _ => Alignment.LowerLeft
                };
                label.LabelFontName = ChartFontName;
                label.LabelFontSize = 8F;
                label.LabelFontColor = ScottPlot.Color.FromHex("#991B1B");
                label.LabelBackgroundColor = ScottPlot.Color.FromHex("#FFF7ED");
                label.LabelBorderColor = ScottPlot.Color.FromHex("#FCA5A5");
                label.LabelBorderWidth = 1F;
                label.LabelPadding = 3F;
                trackingLabels.Add(label);
            }
        }

        private void TrackingLabelTimer_Tick(object? sender, EventArgs e)
        {
            trackingLabelTimer.Stop();
            trackingLabelsExpireAt = DateTime.MinValue;
            if (formsPlot.IsDisposed) return;

            RemoveTrackingLabels();
            formsPlot.Refresh();
        }

        private void RemoveTrackingLabels()
        {
            foreach (Text label in trackingLabels)
                formsPlot.Plot.Remove(label);
            trackingLabels.Clear();
        }

        private void UpdateStatistics(IEnumerable<double> values)
        {
            double[] data = values.Where(double.IsFinite).ToArray();
            TopLayout.uiValueMin.Text = data.Length == 0 ? "-" : $"{data.Min():0.0} {UnitText}≤";
            TopLayout.uiValueAvg.Text = data.Length == 0 ? "-" : $"{data.Average():0.0}{UnitText}";
            TopLayout.uiValueMax.Text = data.Length == 0 ? "-" : $"≤ {data.Max():0.0}{UnitText}";
        }

        private Color GetThemeColor() => ChartType switch
        {
            ChartMainType.Discharge => Color.FromArgb(16, 185, 129),
            ChartMainType.Level => Color.FromArgb(29, 193, 211),
            ChartMainType.Velocity => Color.FromArgb(139, 92, 246),
            _ => Color.FromArgb(37, 99, 235)
        };

        private static ScottPlot.Color ToScottColor(Color color) => new(color.R, color.G, color.B, color.A);

        private void MeasurementDataHub_Changed(object? sender, MeasurementDataChangedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated || Interlocked.Exchange(ref refreshPending, 1) != 0) return;

            try
            {
                BeginInvoke(new Action(() =>
                {
                    Interlocked.Exchange(ref refreshPending, 0);
                    if (!IsDisposed) LoadData();
                }));
            }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref refreshPending, 0);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (measurementDataHub != null)
                    measurementDataHub.Changed -= MeasurementDataHub_Changed;
                legendController.Dispose();
                trackingLabelTimer.Stop();
                trackingLabelTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
