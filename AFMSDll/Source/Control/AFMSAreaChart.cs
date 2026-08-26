using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AFMSDll
{
    public enum AFMSChartIntervalMode
    {
        DivisionCount,
        FixedInterval
    }

    public class AFMSAreaChart : Control
    {
        private CrossSectionPointCollection _points = new CrossSectionPointCollection();

        private double? _xMin;
        private double? _xMax;
        private double? _yMin;
        private double? _yMax;

        private AFMSChartIntervalMode _xIntervalMode = AFMSChartIntervalMode.DivisionCount;
        private AFMSChartIntervalMode _yIntervalMode = AFMSChartIntervalMode.DivisionCount;

        private int _xDivisionCount = 10;
        private int _yDivisionCount = 8;
        private double _xInterval = 10.0;
        private double _yInterval = 1.0;
        [Browsable(false)]
        public CrossSectionPointCollection Data => _points;


        public AFMSAreaChart()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            BackColor = Color.White;
        }

        public void SetData(CrossSectionPointCollection? points)
        {
            _points = points ?? new CrossSectionPointCollection();
            Invalidate();
        }

        public void ClearData()
        {
            _points.Clear();
            _points.ClearWaterLevel();
            Invalidate();
        }

        public void ClearWaterLevel()
        {
            _points.ClearWaterLevel();
            Invalidate();
        }

        public void SetXAxisRange(double min, double max)
        {
            ValidateAxisRange(min, max, "X");
            _xMin = min;
            _xMax = max;
            Invalidate();
        }

        public void ClearXAxisRange()
        {
            _xMin = null;
            _xMax = null;
            Invalidate();
        }

        public void SetYAxisRange(double min, double max)
        {
            ValidateAxisRange(min, max, "Y");
            _yMin = min;
            _yMax = max;
            Invalidate();
        }

        public void ClearYAxisRange()
        {
            _yMin = null;
            _yMax = null;
            Invalidate();
        }

        public void ClearAxisRanges()
        {
            _xMin = null;
            _xMax = null;
            _yMin = null;
            _yMax = null;
            Invalidate();
        }

        public void SetXAxisDivisionCount(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "X축 등분 개수는 1 이상이어야 합니다.");

            _xIntervalMode = AFMSChartIntervalMode.DivisionCount;
            _xDivisionCount = count;
            Invalidate();
        }

        public void SetYAxisDivisionCount(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Y축 등분 개수는 1 이상이어야 합니다.");

            _yIntervalMode = AFMSChartIntervalMode.DivisionCount;
            _yDivisionCount = count;
            Invalidate();
        }

        public void SetXAxisInterval(double interval)
        {
            if (interval <= 0 || double.IsNaN(interval) || double.IsInfinity(interval))
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "X축 간격은 0보다 큰 유효한 숫자여야 합니다.");
            }

            _xIntervalMode = AFMSChartIntervalMode.FixedInterval;
            _xInterval = interval;
            Invalidate();
        }

        public void SetYAxisInterval(double interval)
        {
            if (interval <= 0 || double.IsNaN(interval) || double.IsInfinity(interval))
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Y축 간격은 0보다 큰 유효한 숫자여야 합니다.");
            }

            _yIntervalMode = AFMSChartIntervalMode.FixedInterval;
            _yInterval = interval;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(BackColor);

            if (_points.Count < 2)
            {
                DrawEmptyMessage(g);
                return;
            }

            DrawChart(g);
        }

        private void DrawChart(Graphics g)
        {
            const int LEFT = 65;
            const int RIGHT = 25;
            const int TOP = 25;
            const int BOTTOM = 55;

            Rectangle plotRect = new Rectangle(LEFT, TOP, Math.Max(1, Width - LEFT - RIGHT), Math.Max(1, Height - TOP - BOTTOM));

            double dataMinX = _points.Min(point => point.X);
            double dataMaxX = _points.Max(point => point.X);
            double dataMinY = _points.Min(point => point.Y);
            double dataMaxY = _points.Max(point => point.Y);

            double minX = _xMin ?? dataMinX;
            double maxX = _xMax ?? dataMaxX;
            double minY = _yMin ?? dataMinY;
            double maxY = _yMax ?? dataMaxY;

            NormalizeRange(ref minX, ref maxX);
            NormalizeRange(ref minY, ref maxY);

            PointF[] profilePoints = CreateProfilePoints(plotRect, minX, maxX, minY, maxY);

            DrawGrid(g, plotRect, minX, maxX, minY, maxY);

            GraphicsState clipState = g.Save();
            g.SetClip(plotRect);

            DrawWater(g, plotRect, minY, maxY);
            DrawArea(g, plotRect, profilePoints);
            DrawWaterLevel(g, plotRect, minX, maxX, minY, maxY);

            g.Restore(clipState);

            DrawAxisTitles(g, plotRect);
        }

        private PointF[] CreateProfilePoints(Rectangle rect, double minX, double maxX, double minY, double maxY)
        {
            PointF[] points = new PointF[_points.Count];

            for (int i = 0; i < _points.Count; i++)
            {
                float x = ConvertX(_points[i].X, rect, minX, maxX);
                float y = ConvertY(_points[i].Y, rect, minY, maxY);
                points[i] = new PointF(x, y);
            }

            return points;
        }

        private void DrawGrid(Graphics g, Rectangle rect, double minX, double maxX, double minY, double maxY)
        {
            using Pen gridPen = new Pen(Color.FromArgb(225, 229, 235));
            using Pen axisPen = new Pen(Color.FromArgb(155, 160, 168));
            using Font axisFont = new System.Drawing.Font("Segoe UI", 8F);
            using SolidBrush textBrush = new SolidBrush(Color.FromArgb(90, 95, 105));

            IEnumerable<double> xTicks = CreateTicks(minX, maxX, _xIntervalMode, _xDivisionCount, _xInterval);
            IEnumerable<double> yTicks = CreateTicks(minY, maxY, _yIntervalMode, _yDivisionCount, _yInterval);

            foreach (double value in xTicks)
            {
                float x = ConvertX(value, rect, minX, maxX);
                g.DrawLine(gridPen, x, rect.Top, x, rect.Bottom);

                string text = FormatAxisValue(value);
                SizeF size = g.MeasureString(text, axisFont);
                g.DrawString(text, axisFont, textBrush, x - size.Width / 2f, rect.Bottom + 6);
            }

            foreach (double value in yTicks)
            {
                float y = ConvertY(value, rect, minY, maxY);
                g.DrawLine(gridPen, rect.Left, y, rect.Right, y);

                string text = FormatAxisValue(value);
                SizeF size = g.MeasureString(text, axisFont);
                g.DrawString(text, axisFont, textBrush, rect.Left - size.Width - 8, y - size.Height / 2f);
            }

            g.DrawLine(axisPen, rect.Left, rect.Top, rect.Left, rect.Bottom);
            g.DrawLine(axisPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
        }

        private static IEnumerable<double> CreateTicks(double min, double max, AFMSChartIntervalMode mode, int divisionCount, double interval)
        {
            if (mode == AFMSChartIntervalMode.DivisionCount)
            {
                for (int i = 0; i <= divisionCount; i++) yield return min + ((max - min) * i / divisionCount);

                yield break;
            }

            const double EPSILON = 0.000000001;
            double value = min;
            int guard = 0;

            while (value <= max + EPSILON && guard < 10000)
            {
                yield return value;
                value += interval;
                guard++;
            }

            if (guard == 0 || Math.Abs((value - interval) - max) > EPSILON) yield return max;
        }

        private void DrawWater(Graphics g, Rectangle rect, double minY, double maxY)
        {
            if (!_points.WaterLevel.HasValue) return;

            float waterY = ConvertY(_points.WaterLevel.Value, rect, minY, maxY);
            if (waterY >= rect.Bottom) return;

            float top = Math.Max(rect.Top, waterY);
            float height = rect.Bottom - top;
            if (height <= 0) return;

            using SolidBrush waterBrush = new SolidBrush(Color.FromArgb(125, 80, 165, 235));
            g.FillRectangle(waterBrush, rect.Left, top, rect.Width, height);
        }

        private void DrawArea(Graphics g, Rectangle rect, PointF[] points)
        {
            using GraphicsPath areaPath = new GraphicsPath();

            areaPath.AddLines(points);
            areaPath.AddLine(points[^1].X, points[^1].Y, points[^1].X, rect.Bottom);
            areaPath.AddLine(points[^1].X, rect.Bottom, points[0].X, rect.Bottom);
            areaPath.CloseFigure();

            using SolidBrush areaBrush = new SolidBrush(Color.FromArgb(225, 215, 175));
            using Pen linePen = new Pen(Color.FromArgb(105, 110, 115), 2F);

            g.FillPath(areaBrush, areaPath);
            g.DrawLines(linePen, points);
        }

        private void DrawWaterLevel(Graphics g, Rectangle rect, double minX, double maxX, double minY, double maxY)
        {
            if (!_points.WaterLevel.HasValue || _points.Count < 2) return;

            double waterLevel = _points.WaterLevel.Value;
            float y = ConvertY(waterLevel, rect, minY, maxY);

            if (y < rect.Top || y > rect.Bottom) return;

            using Pen waterLevelPen = new Pen(Color.FromArgb(220, 45, 45), 2F);
            List<(float StartX, float EndX)> segments = GetWaterSurfaceSegments(rect, waterLevel, minX, maxX);

            if (segments.Count == 0) return;

            foreach ((float startX, float endX) in segments) g.DrawLine(waterLevelPen, startX, y, endX, y);

            DrawWaterLevelLabel(g, rect, y, waterLevel, segments);
        }

        private List<(float StartX, float EndX)> GetWaterSurfaceSegments(Rectangle rect, double waterLevel, double minX, double maxX)
        {
            List<(float StartX, float EndX)> segments = new List<(float StartX, float EndX)>();
            float? segmentStart = null;
            float segmentEnd = 0F;

            for (int i = 0; i < _points.Count - 1; i++)
            {
                CrossSectionPoint p1 = _points[i];
                CrossSectionPoint p2 = _points[i + 1];

                bool p1Wet = p1.Y <= waterLevel;
                bool p2Wet = p2.Y <= waterLevel;

                float x1 = ConvertX(p1.X, rect, minX, maxX);
                float x2 = ConvertX(p2.X, rect, minX, maxX);

                if (p1Wet && p2Wet)
                {
                    segmentStart ??= x1;
                    segmentEnd = x2;
                    continue;
                }

                if (p1Wet && !p2Wet)
                {
                    float crossX = GetWaterIntersectionX(p1, p2, rect, minX, maxX, waterLevel);
                    segmentStart ??= x1;
                    segmentEnd = crossX;
                    AddWaterSegment(segments, ref segmentStart, segmentEnd);
                    continue;
                }

                if (!p1Wet && p2Wet)
                {
                    float crossX = GetWaterIntersectionX(p1, p2, rect, minX, maxX, waterLevel);
                    segmentStart = crossX;
                    segmentEnd = x2;
                }
            }

            if (segmentStart.HasValue) AddWaterSegment(segments, ref segmentStart, segmentEnd);

            return ClipWaterSegments(segments, rect);
        }

        private static List<(float StartX, float EndX)> ClipWaterSegments(List<(float StartX, float EndX)> segments, Rectangle rect)
        {
            List<(float StartX, float EndX)> result = new List<(float StartX, float EndX)>();

            foreach ((float startX, float endX) in segments)
            {
                float clippedStart = Math.Max(rect.Left, Math.Min(rect.Right, startX));
                float clippedEnd = Math.Max(rect.Left, Math.Min(rect.Right, endX));

                if (clippedEnd > clippedStart) result.Add((clippedStart, clippedEnd));
            }

            return result;
        }

        private static float GetWaterIntersectionX(CrossSectionPoint p1, CrossSectionPoint p2, Rectangle rect, double minX, double maxX, double waterLevel)
        {
            double elevDiff = p2.Y - p1.Y;

            if (Math.Abs(elevDiff) < double.Epsilon) return ConvertX(p1.X, rect, minX, maxX);

            double ratio = (waterLevel - p1.Y) / elevDiff;
            double dist = p1.X + ((p2.X - p1.X) * ratio);

            return ConvertX(dist, rect, minX, maxX);
        }

        private static void AddWaterSegment(List<(float StartX, float EndX)> segments, ref float? startX, float endX)
        {
            if (!startX.HasValue) return;

            if (endX > startX.Value) segments.Add((startX.Value, endX));

            startX = null;
        }

        private void DrawWaterLevelLabel(Graphics g, Rectangle rect, float y, double waterLevel, List<(float StartX, float EndX)> segments)
        {
            (float StartX, float EndX) labelSegment = segments.OrderByDescending(x => x.EndX - x.StartX).First();

            using Font labelFont = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold);
            using SolidBrush labelBrush = new SolidBrush(Color.FromArgb(45, 55, 65));
            using SolidBrush labelBackBrush = new SolidBrush(Color.FromArgb(230, Color.White));

            string label = $"수위 {waterLevel:0.##}m";
            SizeF labelSize = g.MeasureString(label, labelFont);

            float labelX = labelSegment.EndX - labelSize.Width - 8;
            if (labelX < labelSegment.StartX + 4) labelX = labelSegment.StartX + 4;

            float labelY = Math.Max(rect.Top + 2, y - labelSize.Height - 4);
            RectangleF labelRect = new RectangleF(labelX - 3, labelY - 1, labelSize.Width + 6, labelSize.Height + 2);

            //g.FillRectangle(labelBackBrush, labelRect);
            g.DrawString(label, labelFont, labelBrush, labelX, labelY);
        }

        private void DrawAxisTitles(Graphics g, Rectangle rect)
        {
            using Font font = new System.Drawing.Font("Segoe UI", 9F);
            using SolidBrush brush = new SolidBrush(Color.FromArgb(75, 80, 90));

            const string xTitle = "좌안거리(m)";
            const string yTitle = "수위(m)";

            SizeF xSize = g.MeasureString(xTitle, font);
            g.DrawString(xTitle, font, brush, rect.Left + (rect.Width - xSize.Width) / 2f, Height - xSize.Height - 3);

            GraphicsState state = g.Save();

            g.TranslateTransform(15, rect.Top + rect.Height / 2f);
            g.RotateTransform(-90);

            SizeF ySize = g.MeasureString(yTitle, font);
            g.DrawString(yTitle, font, brush, -ySize.Width / 2f, 0);

            g.Restore(state);
        }

        private static float ConvertX(double value, Rectangle rect, double min, double max)
        {
            return rect.Left + (float)((value - min) / (max - min) * rect.Width);
        }

        private static float ConvertY(double value, Rectangle rect, double min, double max)
        {
            return rect.Bottom - (float)((value - min) / (max - min) * rect.Height);
        }

        private static string FormatAxisValue(double value)
        {
            return value.ToString("0.##");
        }

        private static void ValidateAxisRange(double min, double max, string axisName)
        {
            if (double.IsNaN(min) || double.IsNaN(max) || double.IsInfinity(min) || double.IsInfinity(max))
            {
                throw new ArgumentException($"{axisName}축 최소/최대값은 유효한 숫자여야 합니다.");
            }

            if (min >= max) throw new ArgumentException($"{axisName}축 최소값은 최대값보다 작아야 합니다.");
        }

        private static void NormalizeRange(ref double min, ref double max)
        {
            if (Math.Abs(max - min) >= double.Epsilon) return;

            min -= 0.5;
            max += 0.5;
        }

        private void DrawEmptyMessage(Graphics g)
        {
            const string message = "단면 데이터가 없습니다.";

            TextRenderer.DrawText(g, message, Font, ClientRectangle, Color.FromArgb(145, 150, 160),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
