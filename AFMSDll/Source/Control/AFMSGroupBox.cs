using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    public class AFMSGroupBox : GroupBox
    {
        private Color _borderColor = Color.FromArgb(218, 224, 232);
        private Color _titleColor = Color.FromArgb(1, 125, 67);
        private float _borderThickness = 1F;
        private int _borderRadius = 5;
        private int _titleHorizontalPadding = 8;
        private int _titleLeftMargin = 8;

        public AFMSGroupBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            BackColor = Color.White;
            ForeColor = Color.FromArgb(45, 45, 45);
            Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Bold, GraphicsUnit.Point);
            Padding = new Padding(12, 18, 12, 12);
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color TitleColor
        {
            get => _titleColor;
            set
            {
                _titleColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1F)]
        public float BorderThickness
        {
            get => _borderThickness;
            set
            {
                _borderThickness = Math.Max(0.5F, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(8)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(8)]
        public int TitleHorizontalPadding
        {
            get => _titleHorizontalPadding;
            set
            {
                _titleHorizontalPadding = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(8)]
        public int TitleLeftMargin
        {
            get => _titleLeftMargin;
            set
            {
                _titleLeftMargin = Math.Max(0, value);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            using (SolidBrush backBrush = new SolidBrush(BackColor)) e.Graphics.FillRectangle(backBrush, ClientRectangle);

            Size titleSize = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding);
            int titleTop = 0;
            int borderTop = Math.Max(1, titleSize.Height / 2);

            float inset = BorderThickness / 2F;
            RectangleF borderRect = new RectangleF(inset, borderTop + inset, ClientSize.Width - BorderThickness, ClientSize.Height - borderTop - BorderThickness);

            if (borderRect.Width > 0 && borderRect.Height > 0)
            {
                using GraphicsPath borderPath = CreateRoundPath(borderRect, Math.Max(0F, BorderRadius - inset));
                using Pen borderPen = new Pen(BorderColor, BorderThickness);
                borderPen.Alignment = PenAlignment.Center;
                e.Graphics.DrawPath(borderPen, borderPath);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                int titleX = TitleLeftMargin;
                int titleWidth = titleSize.Width + (TitleHorizontalPadding * 2);

                Rectangle titleBackRect = new Rectangle(titleX, titleTop, titleWidth, titleSize.Height + 1);
                using (SolidBrush titleBackBrush = new SolidBrush(BackColor)) e.Graphics.FillRectangle(titleBackBrush, titleBackRect);

                Rectangle titleTextRect = new Rectangle(titleX + TitleHorizontalPadding, titleTop, titleSize.Width, titleSize.Height);
                TextRenderer.DrawText(e.Graphics, Text, Font, titleTextRect, TitleColor,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Invalidate();
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            Invalidate();
        }

        private static GraphicsPath CreateRoundPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0F)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float diameter = radius * 2F;
            float maxDiameter = Math.Min(rect.Width, rect.Height);
            if (diameter > maxDiameter) diameter = maxDiameter;

            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180F, 90F);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270F, 90F);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0F, 90F);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90F, 90F);
            path.CloseFigure();

            return path;
        }
    }
}
