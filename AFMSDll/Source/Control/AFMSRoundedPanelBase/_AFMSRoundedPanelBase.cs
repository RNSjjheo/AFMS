using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    public abstract class _AFMSRoundedPanelBase : Panel, IAFMSRoundedControl
    {
        private int _borderRadius = 8;
        private float _borderThickness = 0.5F;
        private Color _borderColor = Color.FromArgb(190, 198, 205);

        protected _AFMSRoundedPanelBase()
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
        [DefaultValue(8)]
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

            AFMSRoundedDrawing.SetHighQuality(e.Graphics);
            if (BorderRadius <= 0 && BorderThickness <= 0)
            {
                using SolidBrush brush = new SolidBrush(BackColor);
                e.Graphics.FillRectangle(brush, ClientRectangle);
                return;
            }

            e.Graphics.Clear(GetVisibleParentBackColor());
            RectangleF backgroundRectangle = new RectangleF(0.5F, 0.5F, ClientSize.Width - 1F, ClientSize.Height - 1F);
            using GraphicsPath backgroundPath = AFMSRoundedDrawing.CreatePath(backgroundRectangle, BorderRadius);
            using SolidBrush backgroundBrush = new SolidBrush(BackColor);
            e.Graphics.FillPath(backgroundBrush, backgroundPath);
        }

        private Color GetVisibleParentBackColor()
        {
            // Clearing a buffered control with Color.Transparent leaves transparent
            // pixels that WinForms can composite as black. Walk past transparent
            // layout containers and use the first background that is actually shown.
            Control? ancestor = Parent;
            while (ancestor != null)
            {
                if (ancestor.BackColor.A > 0) return ancestor.BackColor;
                ancestor = ancestor.Parent;
            }

            return SystemColors.Control;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (BorderThickness <= 0F || ClientSize.Width <= BorderThickness || ClientSize.Height <= BorderThickness) return;

            AFMSRoundedDrawing.SetHighQuality(e.Graphics);
            RectangleF borderRectangle = AFMSRoundedDrawing.GetBorderRectangle(ClientSize, BorderThickness);
            if (borderRectangle.Width <= 0F || borderRectangle.Height <= 0F) return;

            float offset = BorderThickness <= 1F ? 0.5F : BorderThickness / 2F;
            using GraphicsPath borderPath = AFMSRoundedDrawing.CreatePath(borderRectangle, Math.Max(0F, BorderRadius - offset));
            using Pen borderPen = new Pen(GetDrawBorderColor(), BorderThickness) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round };
            e.Graphics.DrawPath(borderPen, borderPath);
        }
    }
}
