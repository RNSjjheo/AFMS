using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    [DefaultProperty(nameof(BackColor))]
    public class AFMSPanel : Panel
    {
        private int _borderRadius = 8;
        private float _borderThickness = 0.5F;
        private Color _borderColor = Color.FromArgb(190, 198, 205);

        public AFMSPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

            DoubleBuffered = true;
            base.BorderStyle = BorderStyle.None;
            BackColor = Color.White;
            Padding = new Padding(8);
        }

        [Category("AFMS Appearance")]
        [Description("모서리의 라운딩 크기입니다.")]
        [DefaultValue(12)]
        public virtual int BorderRadius
        {
            get => _borderRadius;
            set
            {
                int newValue = Math.Max(0, value);
                if (_borderRadius == newValue) return;

                _borderRadius = newValue;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [Description("외곽선 두께입니다. 0이면 외곽선을 표시하지 않습니다.")]
        [DefaultValue(0.5F)]
        public virtual float BorderThickness
        {
            get => _borderThickness;
            set
            {
                float newValue = Math.Max(0F, value);
                if (Math.Abs(_borderThickness - newValue) < 0.001F) return;

                _borderThickness = newValue;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Description("외곽선 색상입니다.")]
        public virtual Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor == value) return;

                _borderColor = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DefaultValue(BorderStyle.None)]
        public new BorderStyle BorderStyle
        {
            get => BorderStyle.None;
            set => base.BorderStyle = BorderStyle.None;
        }

        protected virtual Color GetDrawBorderColor()
        {
            return BorderColor;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (ClientSize.Width <= 1 || ClientSize.Height <= 1) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            // 라운드와 테두리를 모두 사용하지 않으면 일반 Panel처럼 전체 영역을 채운다.
            if (BorderRadius <= 0 && BorderThickness <= 0)
            {
                using SolidBrush brush = new SolidBrush(BackColor);
                e.Graphics.FillRectangle(brush, ClientRectangle);
                return;
            }

            Color parentBackColor = Parent?.BackColor ?? SystemColors.Control;
            e.Graphics.Clear(parentBackColor);

            RectangleF backgroundRectangle = new RectangleF(0.5F, 0.5F, ClientSize.Width - 1F, ClientSize.Height - 1F);

            using GraphicsPath backgroundPath = CreateRoundedPath(backgroundRectangle, BorderRadius);
            using SolidBrush backgroundBrush = new SolidBrush(BackColor);

            e.Graphics.FillPath(backgroundBrush, backgroundPath);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (BorderThickness <= 0) return;
            if (ClientSize.Width <= BorderThickness || ClientSize.Height <= BorderThickness) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            float offset = BorderThickness <= 1F ? 0.5F : BorderThickness / 2F;
            RectangleF borderRectangle = new RectangleF(offset, offset, ClientSize.Width - (offset * 2F),
                ClientSize.Height - (offset * 2F));

            if (borderRectangle.Width <= 0 || borderRectangle.Height <= 0) return;

            float borderRadius = Math.Max(0F, BorderRadius - offset);

            using GraphicsPath borderPath = CreateRoundedPath(borderRectangle, borderRadius);
            using Pen borderPen = new Pen(GetDrawBorderColor(), BorderThickness)
            {
                Alignment = PenAlignment.Center,
                LineJoin = LineJoin.Round
            };

            e.Graphics.DrawPath(borderPen, borderPath);
        }

        protected static GraphicsPath CreateRoundedPath(RectangleF rectangle, float radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0F)
            {
                path.AddRectangle(rectangle);
                path.CloseFigure();
                return path;
            }

            float maximumRadius = Math.Min(rectangle.Width, rectangle.Height) / 2F;
            radius = Math.Min(radius, maximumRadius);
            float diameter = radius * 2F;

            RectangleF arc = new RectangleF(rectangle.X, rectangle.Y, diameter, diameter);

            path.AddArc(arc, 180F, 90F);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270F, 90F);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0F, 90F);
            arc.X = rectangle.Left;
            path.AddArc(arc, 90F, 90F);
            path.CloseFigure();

            return path;
        }
    }
}
