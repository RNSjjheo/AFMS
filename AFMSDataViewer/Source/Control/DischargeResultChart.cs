using AFMSDll;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WinForms;
using System.Data;
using System.Globalization;
using WinFormsLabel = System.Windows.Forms.Label;

namespace AFMSDataViewer.Source.Control
{
    internal sealed class RealtimeResultChart : UserControl
    {
        private sealed record ChartPoint(DateTime Time, double Value);
        private sealed record ChartSeries(string Name, System.Drawing.Color Color, List<ChartPoint> Points, bool SecondaryAxis = false);

        private const int MaxRows = 500;
        private static readonly System.Drawing.Color[] SeriesColors =
        {
            System.Drawing.Color.FromArgb(19, 187, 130), System.Drawing.Color.FromArgb(30, 190, 210),
            System.Drawing.Color.FromArgb(132, 82, 246), System.Drawing.Color.FromArgb(244, 165, 36)
        };

        private readonly ChartMainType chartType;
        private readonly FormsPlot formsPlot = new();
        private readonly WinFormsLabel title = new();
        private readonly WinFormsLabel minimum = CreateStatLabel("최소");
        private readonly WinFormsLabel average = CreateStatLabel("평균", true);
        private readonly WinFormsLabel maximum = CreateStatLabel("최대");
        private readonly ComboBox seriesSelector = new();
        private readonly CheckBox compareDischarge = new();
        private readonly ToolTip hoverTip = new() { InitialDelay = 0, ReshowDelay = 0, AutoPopDelay = 5000 };
        private readonly List<ChartSeries> availableSeries = new();

        public event EventHandler? BackRequested;

        public RealtimeResultChart(ChartMainType chartType)
        {
            this.chartType = chartType;
            Dock = DockStyle.Fill;
            BackColor = System.Drawing.Color.White;
            Margin = Padding.Empty;
            BuildLayout();
            ConfigurePlot();
        }

        private void BuildLayout()
        {
            TableLayoutPanel root = new() { Dock = DockStyle.Fill, Margin = Padding.Empty, RowCount = 2, ColumnCount = 1 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel header = new() { Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 8, BackColor = System.Drawing.Color.FromArgb(247, 250, 253) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 3; i++) header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36F));

            title.Dock = DockStyle.Fill;
            title.Text = GetTitle();
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            title.ForeColor = GetColor();

            seriesSelector.Dock = DockStyle.Fill;
            seriesSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            seriesSelector.Margin = new Padding(3, 18, 3, 18);
            seriesSelector.SelectedIndexChanged += (_, _) => DrawSelectedSeries();

            compareDischarge.Text = "유량 비교";
            compareDischarge.Dock = DockStyle.Fill;
            compareDischarge.TextAlign = ContentAlignment.MiddleCenter;
            compareDischarge.Visible = chartType == ChartMainType.Level;
            compareDischarge.CheckedChanged += (_, _) => LoadData();

            Button back = new() { Text = "×", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(2) };
            back.FlatAppearance.BorderSize = 0;
            back.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);

            header.Controls.Add(title, 0, 0); header.Controls.Add(seriesSelector, 1, 0); header.Controls.Add(compareDischarge, 2, 0);
            header.Controls.Add(minimum, 3, 0); header.Controls.Add(average, 4, 0); header.Controls.Add(maximum, 5, 0); header.Controls.Add(back, 7, 0);
            root.Controls.Add(header, 0, 0); root.Controls.Add(formsPlot, 0, 1); Controls.Add(root);
        }

        private void ConfigurePlot()
        {
            formsPlot.Dock = DockStyle.Fill;
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
                foreach (IGrouping<string, DataRow> group in table.Rows.Cast<DataRow>().Where(row => row["VALUE"] != DBNull.Value).GroupBy(row => row["SERIES"].ToText()))
                {
                    List<ChartPoint> points = group.Reverse().Select(row => new ChartPoint(ParseSourceTime(row["SOURCE_TIME"]), Convert.ToDouble(row["VALUE"])))
                        .Where(point => double.IsFinite(point.Value)).ToList();
                    if (points.Count > 0) availableSeries.Add(new ChartSeries(group.Key, SeriesColors[availableSeries.Count % SeriesColors.Length], points));
                }

                if (chartType == ChartMainType.Level && compareDischarge.Checked) AddDischargeComparison(db);
                PopulateSelector();
                DrawSelectedSeries();
                title.Text = GetTitle();
            }
            catch (Exception ex) { ShowMessage(ex.Message); }
        }

        private void AddDischargeComparison(FBDatabase db)
        {
            DataTable table = db.Execute(GetDischargeSql(), out string error);
            if (!string.IsNullOrEmpty(error)) return;
            List<ChartPoint> points = table.Rows.Cast<DataRow>().Reverse().Where(row => row["VALUE"] != DBNull.Value)
                .Select(row => new ChartPoint(ParseSourceTime(row["SOURCE_TIME"]), Convert.ToDouble(row["VALUE"]))).Where(point => double.IsFinite(point.Value)).ToList();
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

        private void DrawSelectedSeries()
        {
            string selected = seriesSelector.SelectedItem?.ToString() ?? "전체";
            formsPlot.Plot.Clear();
            formsPlot.Plot.Axes.Right.IsVisible = false;
            List<ChartSeries> visible = availableSeries.Where(series => selected == "전체" || series.Name == selected || series.SecondaryAxis).ToList();

            foreach (ChartSeries source in visible)
            {
                double[] xs = source.Points.Select(point => point.Time.ToOADate()).ToArray();
                double[] ys = source.Points.Select(point => point.Value).ToArray();
                Scatter scatter = formsPlot.Plot.Add.Scatter(xs, ys);
                scatter.LegendText = source.Name;
                scatter.Color = ToScottColor(source.Color);
                scatter.LineWidth = 2;
                scatter.MarkerSize = 5;
                scatter.FillY = true;
                scatter.FillYValue = 0;
                scatter.FillYColor = ToScottColor(System.Drawing.Color.FromArgb(45, source.Color));
                if (source.SecondaryAxis)
                {
                    scatter.Axes.YAxis = formsPlot.Plot.Axes.Right;
                    formsPlot.Plot.Axes.Right.IsVisible = true;
                    formsPlot.Plot.Axes.Right.Label.Text = "m³/s";
                }
            }

            formsPlot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
            formsPlot.Plot.Axes.AutoScale();
            formsPlot.Plot.ShowLegend(Alignment.UpperRight);
            formsPlot.Refresh();
            UpdateStatistics(visible.Where(series => !series.SecondaryAxis).SelectMany(series => series.Points).Select(point => point.Value));
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
            foreach (ChartSeries series in availableSeries.Where(series => selected == "전체" || series.Name == selected || series.SecondaryAxis))
            {
                ChartPoint? point = series.Points.MinBy(point => Math.Abs((point.Time - cursorTime).TotalSeconds));
                if (point == null) continue;
                double seconds = Math.Abs((point.Time - cursorTime).TotalSeconds);
                if (seconds >= nearestSeconds) continue;
                nearestSeconds = seconds; nearestPoint = point; nearestSeries = series;
            }
            if (nearestPoint == null || nearestSeries == null) return;
            hoverTip.Show($"{nearestSeries.Name}\n{nearestPoint.Value:0.00} {(nearestSeries.SecondaryAxis ? "m³/s" : GetUnit())}\n{nearestPoint.Time:yyyy-MM-dd HH:mm}", formsPlot, e.X + 14, e.Y + 14, 1000);
        }

        private void UpdateStatistics(IEnumerable<double> values)
        {
            double[] data = values.ToArray();
            minimum.Text = data.Length == 0 ? "최소\n-" : $"최소\n{data.Min():0.0}";
            average.Text = data.Length == 0 ? "평균\n-" : $"평균\n{data.Average():0.0}";
            maximum.Text = data.Length == 0 ? "최대\n-" : $"최대\n{data.Max():0.0}";
        }

        private void ShowMessage(string message)
        {
            availableSeries.Clear(); formsPlot.Plot.Clear(); formsPlot.Refresh();
            title.Text = $"{GetTitle()} - 조회 오류: {message}"; UpdateStatistics(Array.Empty<double>());
        }

        private static DateTime ParseSourceTime(object value)
        {
            if (value is DateTime time) return time;
            string text = value.ToText().Trim();
            if (DateTime.TryParseExact(text, new[] { "yyyyMMdd HHmmss", "yyyyMMdd HHmmss.fff" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out time)) return time;
            return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        private string GetSql() => chartType switch
        {
            ChartMainType.Level => $"SELECT FIRST {MaxRows} ({_FBTableBase.COL_MEASURE_DATE} || ' ' || {_FBTableBase.COL_MEASURE_TIME}) AS SOURCE_TIME, '수위계' AS SERIES, {FbtWATERLEVEL.COL_AVG_WATER_LEVEL} AS VALUE FROM {FbtWATERLEVEL.TABLE_NAME} WHERE {FbtWATERLEVEL.COL_AVG_WATER_LEVEL} IS NOT NULL ORDER BY {_FBTableBase.COL_MEASURE_DATE} DESC, {_FBTableBase.COL_MEASURE_TIME} DESC",
            ChartMainType.Discharge => GetDischargeSql(),
            ChartMainType.VTH => $"SELECT FIRST {MaxRows} ({_FBTableBase.COL_MEASURE_DATE} || ' ' || {_FBTableBase.COL_MEASURE_TIME}) AS SOURCE_TIME, '전압' AS SERIES, {FbtVTHLOGGER.COL_VOLT} AS VALUE FROM {FbtVTHLOGGER.TABLE_NAME} WHERE {FbtVTHLOGGER.COL_VOLT} IS NOT NULL ORDER BY {_FBTableBase.COL_MEASURE_DATE} DESC, {_FBTableBase.COL_MEASURE_TIME} DESC",
            _ => GetVelocitySql()
        };

        private static string GetDischargeSql() => $"SELECT FIRST {MaxRows} {FbtAFMSDischargeResult.COL_SOURCE_TIME} AS SOURCE_TIME, TRIM({FbtAFMSDischargeResult.COL_DISCHARGE_METHOD}) AS SERIES, {FbtAFMSDischargeResult.COL_DISCHARGE} AS VALUE FROM {FbtAFMSDischargeResult.TABLE_NAME} WHERE {FbtAFMSDischargeResult.COL_DISCHARGE} IS NOT NULL ORDER BY {FbtAFMSDischargeResult.COL_SOURCE_TIME} DESC";
        private static string GetVelocitySql() => $"SELECT FIRST {MaxRows} (M.{_FBTableBase.COL_MEASURE_DATE} || ' ' || M.{_FBTableBase.COL_MEASURE_TIME}) AS SOURCE_TIME, 'MPDS ' || C.{FbtHYDROMETERMPDSCELL.COL_DEV_NO} AS SERIES, C.{FbtHYDROMETERMPDSCELL.COL_VELOCITY} AS VALUE FROM {FbtHYDROMETERMPDS.TABLE_NAME} M JOIN {FbtHYDROMETERMPDSCELL.TABLE_NAME} C ON C.{FbtHYDROMETERMPDSCELL.COL_MPDS_ID}=M.{_FBTableBase.COL_ID} WHERE C.{FbtHYDROMETERMPDSCELL.COL_VELOCITY} IS NOT NULL ORDER BY M.{_FBTableBase.COL_MEASURE_DATE} DESC, M.{_FBTableBase.COL_MEASURE_TIME} DESC";

        private string GetTitle() => chartType switch { ChartMainType.Velocity => "유속계", ChartMainType.Level => "수위계", ChartMainType.Discharge => "유량계", _ => "전원" };
        private string GetUnit() => chartType switch { ChartMainType.Velocity => "m/s", ChartMainType.Level => "m", ChartMainType.Discharge => "m³/s", _ => "V" };
        private System.Drawing.Color GetColor() => chartType switch { ChartMainType.Velocity => SeriesColors[2], ChartMainType.Level => SeriesColors[1], ChartMainType.Discharge => SeriesColors[0], _ => SeriesColors[3] };
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
