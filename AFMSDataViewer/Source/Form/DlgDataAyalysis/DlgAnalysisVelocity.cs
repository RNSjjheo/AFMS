using AFMSDll;
using ScottPlot.WinForms;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    internal class DlgAnalysisVelocity : DlgDataAnalysis
    {
        public DlgAnalysisVelocity(RealtimeChartSeries series, RealtimeChartPoint point, int? tranNo = null, MeasurementDataHub? hub = null, VelocityMeasurement? velocityMeasurement = null, double? min = null, double? max = null, Tracking? linkedTracking = null) : base(series, point, tranNo, hub, velocityMeasurement, min, max, linkedTracking)
        {
            SourceChartType = ChartMainType.Velocity;
        }

        protected override void ConfigureAnalysisTabs()
        {
            if (velocityMeasurement != null) return;

            SectionContext section = LoadSectionContext();

            CreateVelocityPage();
            CreateTimeDistributionPage(section);
            CreateCrossSectionPage(section);
            CreateMainFlowPage(section);
        }


        private TabPage CreateTimeDistributionPage(SectionContext context)
        {
            TabPage page = CreatePage("시간분포");
            VelocityTimeDistributionChart chart = new()
            {
                Dock = DockStyle.Fill,
                MinimumVelocity = -0.5D,
                MaximumVelocity = 0.5D
            };
            if (measurementDataHub != null)
                chart.SetData(measurementDataHub, velocityMeasurement!, context.Transects);
            page.Controls.Add(chart);
            return page;
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
                    : $"주흐름 측선{main.TransectNo}   {main.Velocity:N3} m/s"
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
                if (minimumVelocity.HasValue && maximumVelocity.HasValue && minimumVelocity.Value < maximumVelocity.Value)
                    plot.Plot.Axes.SetLimitsY(minimumVelocity.Value, maximumVelocity.Value);
            }
            plot.Refresh();

            layout.Controls.Add(summary, 0, 0);
            layout.Controls.Add(plot, 0, 1);
            page.Controls.Add(layout);
            return page;
        }
    }
}
