using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using AFMSDll;

namespace AFMSDataViewer
{
    /// <summary>
    /// MeasurementDataHub의 전체 보관 범위에 있는 측선별 유속을 시간-위치 색상 분포로 표시합니다.
    /// 차트는 항상 600×900 비트맵으로 먼저 만든 뒤 현재 컨트롤 크기에 맞춰 렌더링합니다.
    /// </summary>
    [DesignerCategory("Code")]
    public sealed class VelocityTimeDistributionChart : Control
    {
        private const int RenderWidth = 600;
        private const int RenderHeight = 900;

        private readonly object imageSync = new();
        private readonly List<TransectPosition> transectPositions = [];
        private MeasurementDataHub? measurementDataHub;
        private string sourceType = string.Empty;
        private string deviceKey = string.Empty;
        private Bitmap? renderedImage;
        private double minimumVelocity = -0.5D;
        private double maximumVelocity = 0.5D;

        private sealed record TransectPosition(int Number, double Position, double Elevation);

        private sealed record TimeDistributionSlot(
            DateTime SlotTime,
            VelocityMeasurement? Measurement,
            double? GaugeWaterLevel);

        public VelocityTimeDistributionChart()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            BackColor = Color.White;
            MinimumSize = new Size(320, 220);
        }

        [Category("AFMS Data")]
        [DefaultValue(-0.5D)]
        [Description("파란색으로 표시할 최저 유속입니다. 단위는 m/s입니다.")]
        public double MinimumVelocity
        {
            get => minimumVelocity;
            set
            {
                if (!double.IsFinite(value) || value >= MaximumVelocity)
                    throw new ArgumentOutOfRangeException(nameof(value), "최저 유속은 최고 유속보다 작아야 합니다.");
                if (minimumVelocity.Equals(value)) return;
                minimumVelocity = value;
                RebuildImage();
            }
        }

        [Category("AFMS Data")]
        [DefaultValue(0.5D)]
        [Description("빨간색으로 표시할 최고 유속입니다. 단위는 m/s입니다.")]
        public double MaximumVelocity
        {
            get => maximumVelocity;
            set
            {
                if (!double.IsFinite(value) || value <= MinimumVelocity)
                    throw new ArgumentOutOfRangeException(nameof(value), "최고 유속은 최저 유속보다 커야 합니다.");
                if (maximumVelocity.Equals(value)) return;
                maximumVelocity = value;
                RebuildImage();
            }
        }

        /// <summary>표시할 유속계와 측선 위치를 지정합니다.</summary>
        public void SetData(
            MeasurementDataHub dataHub,
            VelocityMeasurement measurement,
            IEnumerable<Transect> transects)
        {
            ArgumentNullException.ThrowIfNull(dataHub);
            ArgumentNullException.ThrowIfNull(measurement);
            ArgumentNullException.ThrowIfNull(transects);

            if (measurementDataHub != null)
                measurementDataHub.Changed -= MeasurementDataHub_Changed;

            measurementDataHub = dataHub;
            sourceType = measurement.SourceType;
            deviceKey = measurement.DeviceKey;
            transectPositions.Clear();
            transectPositions.AddRange(transects
                .Where(item => item.No > 0 && double.IsFinite(item.CenterLeftBankDistance))
                .GroupBy(item => item.No)
                .Select(group => group.First())
                .OrderBy(item => item.CenterLeftBankDistance)
                .Select(item => new TransectPosition(item.No, item.CenterLeftBankDistance, item.Elevation)));

            measurementDataHub.Changed += MeasurementDataHub_Changed;
            RebuildImage();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            lock (imageSync)
            {
                if (renderedImage != null)
                    e.Graphics.DrawImage(renderedImage, ClientRectangle);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (measurementDataHub != null)
                    measurementDataHub.Changed -= MeasurementDataHub_Changed;
                lock (imageSync)
                {
                    renderedImage?.Dispose();
                    renderedImage = null;
                }
            }
            base.Dispose(disposing);
        }

        private void MeasurementDataHub_Changed(object? sender, MeasurementDataChangedEventArgs e)
        {
            if (IsDisposed || Disposing) return;
            if (IsHandleCreated && InvokeRequired)
            {
                try { BeginInvoke(RebuildImage); }
                catch (InvalidOperationException) { }
                return;
            }
            RebuildImage();
        }

        private void RebuildImage()
        {
            if (IsDisposed || Disposing) return;
            Bitmap next = BuildImage();
            Bitmap? previous;
            lock (imageSync)
            {
                previous = renderedImage;
                renderedImage = next;
            }
            previous?.Dispose();
            if (IsHandleCreated) Invalidate();
        }

        private Bitmap BuildImage()
        {
            Bitmap bitmap = new(RenderWidth, RenderHeight, PixelFormat.Format32bppPArgb);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            graphics.Clear(Color.White);

            using Font titleFont = new("맑은 고딕", 13F, FontStyle.Bold);
            using Font axisFont = new("맑은 고딕", 8.5F);
            using Font labelFont = new("맑은 고딕", 8F);
            using Font messageFont = new("맑은 고딕", 11F);
            using SolidBrush titleBrush = new(Color.FromArgb(31, 41, 55));
            using SolidBrush textBrush = new(Color.FromArgb(71, 85, 105));
            using Pen axisPen = new(Color.FromArgb(100, 116, 139), 1F);
            using Pen gridPen = new(Color.FromArgb(80, 255, 255, 255), 1F);

            graphics.DrawString("유속 시간분포", titleFont, titleBrush, 28F, 17F);

            RectangleF plot = new(82F, 65F, 400F, 740F);
            RectangleF legend = new(515F, 115F, 20F, 600F);
            IReadOnlyList<TimeDistributionSlot> slots = GetTimeDistributionSlots();
            IReadOnlyList<TransectPosition> positions = transectPositions.ToArray();

            if (slots.Count == 0 || positions.Count == 0)
            {
                string message = slots.Count == 0
                    ? "MeasurementDataHub에 표시할 유속 자료가 없습니다."
                    : "유속계에 설정된 측선 위치가 없습니다.";
                DrawCenteredMessage(graphics, plot, message, messageFont, textBrush);
                graphics.DrawRectangle(axisPen, plot.X, plot.Y, plot.Width, plot.Height);
                return bitmap;
            }

            double leftBound;
            double rightBound;
            if (positions.Count == 1)
            {
                leftBound = positions[0].Position - 0.5D;
                rightBound = positions[0].Position + 0.5D;
            }
            else
            {
                leftBound = positions[0].Position - ((positions[1].Position - positions[0].Position) / 2D);
                rightBound = positions[^1].Position + ((positions[^1].Position - positions[^2].Position) / 2D);
            }
            if (rightBound <= leftBound) rightBound = leftBound + 1D;

            DrawCells(graphics, plot, slots, positions, leftBound, rightBound);
            DrawGridAndAxes(graphics, plot, slots, positions, leftBound, rightBound, axisFont, labelFont, textBrush, axisPen, gridPen);
            DrawLegend(graphics, legend, labelFont, textBrush, axisPen);

            string range = $"{slots[0].SlotTime:yyyy-MM-dd HH:mm}  ~  {slots[^1].SlotTime:yyyy-MM-dd HH:mm}";
            SizeF rangeSize = graphics.MeasureString(range, labelFont);
            graphics.DrawString(range, labelFont, textBrush, plot.Right - rangeSize.Width, 30F);
            return bitmap;
        }

        private IReadOnlyList<TimeDistributionSlot> GetTimeDistributionSlots()
        {
            if (measurementDataHub == null) return [];
            IReadOnlyList<MeasurementSlot> hubSlots = measurementDataHub.GetSlots();
            if (hubSlots.Count == 0) return [];
            return hubSlots.Select(slot => new TimeDistributionSlot(
                slot.SlotTime,
                slot.MeasurementDevices.HydroMeters.FirstOrDefault(item =>
                    item.SourceType == sourceType && item.DeviceKey == deviceKey),
                slot.MeasurementDevices.WaterLevelGauge.IsValid
                    ? slot.MeasurementDevices.WaterLevelGauge.Level
                    : null)).ToArray();
        }

        private void DrawCells(
            Graphics graphics,
            RectangleF plot,
            IReadOnlyList<TimeDistributionSlot> slots,
            IReadOnlyList<TransectPosition> positions,
            double leftBound,
            double rightBound)
        {
            using SolidBrush brush = new(Color.White);
            for (int row = 0; row < slots.Count; row++)
            {
                float top = plot.Top + (plot.Height * row / slots.Count);
                float bottom = plot.Top + (plot.Height * (row + 1) / slots.Count);
                VelocityMeasurement? measurement = slots[row].Measurement;
                IReadOnlyDictionary<int, VelocityTransectMeasurement> values = measurement?.Transects
                    .ToDictionary(item => item.TransectNo)
                    ?? new Dictionary<int, VelocityTransectMeasurement>();

                for (int column = 0; column < positions.Count; column++)
                {
                    double cellLeft = column == 0
                        ? leftBound
                        : (positions[column - 1].Position + positions[column].Position) / 2D;
                    double cellRight = column == positions.Count - 1
                        ? rightBound
                        : (positions[column].Position + positions[column + 1].Position) / 2D;
                    float left = MapX(cellLeft, plot, leftBound, rightBound);
                    float right = MapX(cellRight, plot, leftBound, rightBound);
                    double? gaugeWaterLevel = slots[row].GaugeWaterLevel;
                    bool isMeasurableAtWaterLevel = sourceType != VideoVelocityMeasurement.SourceName ||
                        (gaugeWaterLevel is double level &&
                         double.IsFinite(positions[column].Elevation) &&
                         level > positions[column].Elevation);
                    Color color = isMeasurableAtWaterLevel &&
                                  values.TryGetValue(positions[column].Number, out VelocityTransectMeasurement? value) &&
                                  value.IsValid && double.IsFinite(value.Velocity)
                        ? GetVelocityColor(value.Velocity)
                        : Color.White;
                    brush.Color = color;
                    graphics.FillRectangle(brush, left, top, Math.Max(1F, right - left), Math.Max(1F, bottom - top));
                }
            }
        }

        private void DrawGridAndAxes(
            Graphics graphics,
            RectangleF plot,
            IReadOnlyList<TimeDistributionSlot> slots,
            IReadOnlyList<TransectPosition> positions,
            double leftBound,
            double rightBound,
            Font axisFont,
            Font labelFont,
            Brush textBrush,
            Pen axisPen,
            Pen gridPen)
        {
            int xStep = Math.Max(1, (int)Math.Ceiling(positions.Count / 10D));
            using StringFormat xFormat = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
            for (int index = 0; index < positions.Count; index++)
            {
                float x = MapX(positions[index].Position, plot, leftBound, rightBound);
                graphics.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                if (index % xStep != 0 && index != positions.Count - 1) continue;
                graphics.DrawLine(axisPen, x, plot.Bottom, x, plot.Bottom + 5F);
                string label = $"{positions[index].Number}\n{positions[index].Position:0.##}m";
                graphics.DrawString(label, labelFont, textBrush, new RectangleF(x - 34F, plot.Bottom + 7F, 68F, 36F), xFormat);
            }

            int timeStep = Math.Max(1, (int)Math.Ceiling(slots.Count / 8D));
            using StringFormat timeFormat = new() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            bool includesMultipleDates = slots[0].SlotTime.Date != slots[^1].SlotTime.Date;
            for (int index = 0; index < slots.Count; index++)
            {
                if (index % timeStep != 0 && index != slots.Count - 1) continue;
                float y = plot.Top + (plot.Height * (index + 0.5F) / slots.Count);
                graphics.DrawLine(axisPen, plot.Left - 5F, y, plot.Left, y);
                string label = slots[index].SlotTime.ToString(includesMultipleDates ? "MM-dd HH:mm" : "HH:mm");
                graphics.DrawString(label, labelFont, textBrush, new RectangleF(5F, y - 10F, plot.Left - 12F, 20F), timeFormat);
            }

            graphics.DrawRectangle(axisPen, plot.X, plot.Y, plot.Width, plot.Height);
            using StringFormat axisFormat = new() { Alignment = StringAlignment.Center };
            graphics.DrawString("측선 번호 / 좌안 기준 위치", axisFont, textBrush,
                new RectangleF(plot.Left, 858F, plot.Width, 22F), axisFormat);

            GraphicsState state = graphics.Save();
            graphics.TranslateTransform(17F, plot.Top + (plot.Height / 2F));
            graphics.RotateTransform(-90F);
            graphics.DrawString("시간", axisFont, textBrush, new RectangleF(-55F, -11F, 110F, 22F), axisFormat);
            graphics.Restore(state);
        }

        private void DrawLegend(Graphics graphics, RectangleF legend, Font font, Brush textBrush, Pen borderPen)
        {
            for (int y = 0; y < (int)legend.Height; y++)
            {
                double ratio = y / Math.Max(1D, legend.Height - 1D);
                double velocity = MaximumVelocity - ((MaximumVelocity - MinimumVelocity) * ratio);
                using Pen pen = new(GetVelocityColor(velocity));
                graphics.DrawLine(pen, legend.Left, legend.Top + y, legend.Right, legend.Top + y);
            }
            graphics.DrawRectangle(borderPen, legend.X, legend.Y, legend.Width, legend.Height);
            graphics.DrawString($"{MaximumVelocity:0.###}", font, textBrush, legend.Right + 7F, legend.Top - 8F);
            if (MinimumVelocity < 0D && MaximumVelocity > 0D)
            {
                float zeroY = legend.Top + (float)(legend.Height * MaximumVelocity / (MaximumVelocity - MinimumVelocity));
                graphics.DrawLine(borderPen, legend.Right, zeroY, legend.Right + 5F, zeroY);
                graphics.DrawString("0", font, textBrush, legend.Right + 7F, zeroY - 8F);
            }
            graphics.DrawString($"{MinimumVelocity:0.###}", font, textBrush, legend.Right + 7F, legend.Bottom - 8F);
            graphics.DrawString("m/s", font, textBrush, legend.Left - 1F, legend.Top - 25F);
            graphics.DrawString("자료 없음", font, textBrush, 505F, 755F);
            using SolidBrush missingBrush = new(Color.White);
            graphics.FillRectangle(missingBrush, 515F, 780F, 20F, 14F);
            graphics.DrawRectangle(borderPen, 515F, 780F, 20F, 14F);
        }

        private Color GetVelocityColor(double velocity)
        {
            double clamped = Math.Clamp(velocity, MinimumVelocity, MaximumVelocity);
            if (clamped <= 0D)
            {
                double denominator = 0D - MinimumVelocity;
                double ratio = denominator <= 0D ? 0D : (clamped - MinimumVelocity) / denominator;
                return Interpolate(Color.FromArgb(37, 99, 235), Color.FromArgb(250, 204, 21), ratio);
            }

            double positiveRange = MaximumVelocity;
            double positiveRatio = positiveRange <= 0D ? 1D : clamped / positiveRange;
            return Interpolate(Color.FromArgb(250, 204, 21), Color.FromArgb(220, 38, 38), positiveRatio);
        }

        private static Color Interpolate(Color from, Color to, double ratio)
        {
            ratio = Math.Clamp(ratio, 0D, 1D);
            return Color.FromArgb(
                (int)Math.Round(from.R + ((to.R - from.R) * ratio)),
                (int)Math.Round(from.G + ((to.G - from.G) * ratio)),
                (int)Math.Round(from.B + ((to.B - from.B) * ratio)));
        }

        private static float MapX(double value, RectangleF plot, double minimum, double maximum) =>
            plot.Left + (float)((value - minimum) / (maximum - minimum) * plot.Width);

        private static void DrawCenteredMessage(Graphics graphics, RectangleF area, string message, Font font, Brush brush)
        {
            using StringFormat format = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            graphics.DrawString(message, font, brush, area, format);
        }
    }
}
