using AFMSDll;
using System.Data;
using System.Drawing.Drawing2D;

namespace AFMSDataViewer.Source.Control
{
    internal sealed class DischargeResultChart : UserControl
    {
        private sealed record ChartPoint(DateTime Time, double Value);

        private sealed class ChartSeries
        {
            public required string Name { get; init; }
            public required Color Color { get; init; }
            public List<ChartPoint> Points { get; } = new();
        }

        private sealed class PlotSurface : System.Windows.Forms.Control
        {
            private readonly List<ChartSeries> series = new();
            private string message = string.Empty;

            public PlotSurface()
            {
                Dock = DockStyle.Fill;
                BackColor = Color.White;
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.UserPaint, true);
            }

            public void SetData(IEnumerable<ChartSeries> values, string emptyMessage)
            {
                series.Clear();
                series.AddRange(values);
                message = emptyMessage;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                List<ChartPoint> points = series.SelectMany(item => item.Points).ToList();
                if (points.Count == 0)
                {
                    DrawCenteredMessage(e.Graphics, string.IsNullOrEmpty(message)
                        ? "표시할 유량 산정 결과가 없습니다."
                        : message);
                    return;
                }

                Rectangle plot = new(66, 42, Math.Max(10, Width - 90), Math.Max(10, Height - 96));
                if (plot.Width < 40 || plot.Height < 40) return;

                DateTime minTime = points.Min(item => item.Time);
                DateTime maxTime = points.Max(item => item.Time);
                if (maxTime <= minTime) maxTime = minTime.AddMinutes(1);

                double minValue = Math.Min(0.0, points.Min(item => item.Value));
                double maxValue = points.Max(item => item.Value);
                if (maxValue <= minValue) maxValue = minValue + 1.0;
                double padding = (maxValue - minValue) * 0.08;
                maxValue += padding;
                if (minValue < 0.0) minValue -= padding;

                DrawGrid(e.Graphics, plot, minTime, maxTime, minValue, maxValue);
                DrawSeries(e.Graphics, plot, minTime, maxTime, minValue, maxValue);
                DrawLegend(e.Graphics, plot);
            }

            private void DrawGrid(Graphics graphics, Rectangle plot, DateTime minTime, DateTime maxTime,
                double minValue, double maxValue)
            {
                using Pen gridPen = new(Color.FromArgb(228, 233, 238), 1F);
                using Pen axisPen = new(Color.FromArgb(135, 148, 160), 1F);
                using Font labelFont = new("맑은 고딕", 8F);
                using Brush labelBrush = new SolidBrush(Color.FromArgb(85, 95, 105));

                const int divisions = 5;
                for (int index = 0; index <= divisions; index++)
                {
                    float y = plot.Bottom - plot.Height * index / (float)divisions;
                    graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                    double value = minValue + (maxValue - minValue) * index / divisions;
                    string text = value.ToString("0.###");
                    SizeF size = graphics.MeasureString(text, labelFont);
                    graphics.DrawString(text, labelFont, labelBrush, plot.Left - size.Width - 7, y - size.Height / 2);
                }

                TimeSpan range = maxTime - minTime;
                string timeFormat = range.TotalDays >= 2 ? "MM-dd\nHH:mm" : "HH:mm";
                for (int index = 0; index <= divisions; index++)
                {
                    float x = plot.Left + plot.Width * index / (float)divisions;
                    graphics.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                    DateTime time = minTime.AddTicks(range.Ticks * index / divisions);
                    string text = time.ToString(timeFormat).Replace("\n", Environment.NewLine);
                    SizeF size = graphics.MeasureString(text, labelFont);
                    graphics.DrawString(text, labelFont, labelBrush, x - size.Width / 2, plot.Bottom + 6);
                }

                graphics.DrawRectangle(axisPen, plot);
                using Font unitFont = new("맑은 고딕", 9F, FontStyle.Bold);
                graphics.DrawString("유량 (m³/s)", unitFont, labelBrush, plot.Left, 13F);
            }

            private void DrawSeries(Graphics graphics, Rectangle plot, DateTime minTime, DateTime maxTime,
                double minValue, double maxValue)
            {
                double timeRange = (maxTime - minTime).TotalMilliseconds;
                double valueRange = maxValue - minValue;
                foreach (ChartSeries item in series)
                {
                    PointF[] coordinates = item.Points.OrderBy(point => point.Time).Select(point => new PointF(
                        plot.Left + (float)((point.Time - minTime).TotalMilliseconds / timeRange * plot.Width),
                        plot.Bottom - (float)((point.Value - minValue) / valueRange * plot.Height))).ToArray();
                    if (coordinates.Length == 0) continue;

                    using Pen linePen = new(item.Color, 2F) { LineJoin = LineJoin.Round };
                    if (coordinates.Length > 1) graphics.DrawLines(linePen, coordinates);
                    using Brush pointBrush = new SolidBrush(item.Color);
                    foreach (PointF point in coordinates)
                        graphics.FillEllipse(pointBrush, point.X - 2.5F, point.Y - 2.5F, 5F, 5F);
                }
            }

            private void DrawLegend(Graphics graphics, Rectangle plot)
            {
                using Font font = new("맑은 고딕", 8F);
                float x = plot.Right;
                foreach (ChartSeries item in series.AsEnumerable().Reverse())
                {
                    SizeF size = graphics.MeasureString(item.Name, font);
                    x -= size.Width + 25F;
                    if (x < plot.Left) break;
                    using Pen pen = new(item.Color, 3F);
                    graphics.DrawLine(pen, x, 24F, x + 13F, 24F);
                    graphics.DrawString(item.Name, font, Brushes.DimGray, x + 17F, 17F);
                    x -= 12F;
                }
            }

            private void DrawCenteredMessage(Graphics graphics, string text)
            {
                using Font font = new("맑은 고딕", 10F);
                using Brush brush = new SolidBrush(Color.FromArgb(95, 105, 115));
                SizeF size = graphics.MeasureString(text, font);
                graphics.DrawString(text, font, brush,
                    Math.Max(0F, (Width - size.Width) / 2F),
                    Math.Max(0F, (Height - size.Height) / 2F));
            }
        }

        private static readonly Color[] SeriesColors =
        {
            Color.FromArgb(2, 146, 93),
            Color.FromArgb(35, 116, 210),
            Color.FromArgb(239, 126, 35),
            Color.FromArgb(137, 85, 190),
            Color.FromArgb(213, 67, 84),
            Color.FromArgb(25, 160, 173)
        };

        private readonly PlotSurface plot = new();
        private readonly Label title = new();
        public event EventHandler? BackRequested;

        public DischargeResultChart()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Margin = Padding.Empty;

            TableLayoutPanel layout = new();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.RowCount = 2;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel header = new();
            header.Dock = DockStyle.Fill;
            header.Margin = Padding.Empty;
            header.ColumnCount = 3;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));

            title.Dock = DockStyle.Fill;
            title.Text = "유량 산정 결과";
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(36, 75, 55);
            title.Padding = new Padding(12, 0, 0, 0);

            Button refresh = CreateHeaderButton("새로고침");
            Button back = CreateHeaderButton("차트 선택");
            refresh.Click += (_, _) => LoadData();
            back.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(refresh, 1, 0);
            header.Controls.Add(back, 2, 0);
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(plot, 0, 1);
            Controls.Add(layout);
        }

        public void LoadData()
        {
            const int MaxRows = 500;
            string sql = $"SELECT FIRST {MaxRows}" +
                $" {_FBTableBase.COL_ID}," +
                $" {FbtAFMSDischargeResult.COL_SOURCE_TIME}," +
                $" TRIM({FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE}) AS {FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE}," +
                $" {FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID} + 0 AS {FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID}," +
                $" TRIM({FbtAFMSDischargeResult.COL_DISCHARGE_METHOD}) AS {FbtAFMSDischargeResult.COL_DISCHARGE_METHOD}," +
                $" {FbtAFMSDischargeResult.COL_DISCHARGE}" +
                $" FROM {FbtAFMSDischargeResult.TABLE_NAME}" +
                $" WHERE {FbtAFMSDischargeResult.COL_CALCULATION_STATUS} = 'Calculated'" +
                $" AND {FbtAFMSDischargeResult.COL_SOURCE_TIME} IS NOT NULL" +
                $" AND {FbtAFMSDischargeResult.COL_DISCHARGE} IS NOT NULL" +
                $" ORDER BY {FbtAFMSDischargeResult.COL_SOURCE_TIME} DESC," +
                $" {FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE}," +
                $" {FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID}," +
                $" {FbtAFMSDischargeResult.COL_DISCHARGE_METHOD}";

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                plot.SetData(Array.Empty<ChartSeries>(), $"유량 결과 조회 오류\n{error}");
                title.Text = "유량 산정 결과 - 조회 실패";
                return;
            }

            Dictionary<string, ChartSeries> groups = new();
            foreach (DataRow row in table.Rows.Cast<DataRow>().Reverse())
            {
                DateTime time = Convert.ToDateTime(row[FbtAFMSDischargeResult.COL_SOURCE_TIME]);
                double discharge = Convert.ToDouble(row[FbtAFMSDischargeResult.COL_DISCHARGE]);
                if (!double.IsFinite(discharge)) continue;

                string deviceType = row[FbtAFMSDischargeResult.COL_SOURCE_DEVICE_TYPE].ToText();
                int deviceId = Convert.ToInt32(row[FbtAFMSDischargeResult.COL_SOURCE_DEVICE_ID]);
                string methodText = row[FbtAFMSDischargeResult.COL_DISCHARGE_METHOD].ToText();
                string key = $"{deviceType}:{deviceId}:{methodText}";
                if (!groups.TryGetValue(key, out ChartSeries? series))
                {
                    string methodName = Enum.TryParse(methodText, true, out DischargeMethod method)
                        ? EnumPaser.GetKorString(method)
                        : methodText;
                    series = new ChartSeries
                    {
                        Name = $"{methodName} · {deviceType} {deviceId}",
                        Color = SeriesColors[groups.Count % SeriesColors.Length]
                    };
                    groups.Add(key, series);
                }
                series.Points.Add(new ChartPoint(time, discharge));
            }

            plot.SetData(groups.Values, "표시할 유량 산정 결과가 없습니다.");
            title.Text = groups.Count == 0
                ? "유량 산정 결과"
                : $"유량 산정 결과 · 최근 {groups.Sum(item => item.Value.Points.Count)}건";
        }

        private static Button CreateHeaderButton(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(55, 75, 65),
            Font = new Font("맑은 고딕", 8F),
            Margin = new Padding(2, 5, 2, 5),
            Cursor = Cursors.Hand
        };
    }
}
