using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    [DefaultProperty(nameof(HeaderText))]
    public class AFMSHeaderGroupBox : Panel
    {
        private string _headerText = string.Empty;
        private Color _headerColor = Color.FromArgb(2, 146, 93);
        private Color _headerLineColor = Color.FromArgb(226, 232, 236);
        private Color _borderColor = Color.FromArgb(220, 228, 224);
        private float _borderThickness = 1F;
        private float _headerLineThickness = 1F;
        private int _borderRadius = 8;
        private int _headerHeight = 48;
        private int _headerHorizontalPadding = 16;
        private int _headerBarWidth = 3;
        private int _headerBarHeight = 18;
        private int _headerBarTextGap = 10;

        public AFMSHeaderGroupBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.White;
            Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Bold, GraphicsUnit.Point);
            Padding = new Padding(12, 8, 12, 12);
            BorderStyle = BorderStyle.None;
        }

        [Category("AFMS Header")]
        [DefaultValue("")]
        public string HeaderText
        {
            get => _headerText;
            set
            {
                _headerText = value ?? string.Empty;
                Invalidate();
            }
        }

        [Category("AFMS Header")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HeaderColor
        {
            get => _headerColor;
            set
            {
                _headerColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Header")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HeaderLineColor
        {
            get => _headerLineColor;
            set
            {
                _headerLineColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Header")]
        [DefaultValue(48)]
        public int HeaderHeight
        {
            get => _headerHeight;
            set
            {
                _headerHeight = Math.Max(24, value);
                PerformLayout();
                Invalidate();
            }
        }

        [Category("AFMS Header")]
        [DefaultValue(16)]
        public int HeaderHorizontalPadding
        {
            get => _headerHorizontalPadding;
            set
            {
                _headerHorizontalPadding = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("AFMS Header")]
        [DefaultValue(3)]
        public int HeaderBarWidth
        {
            get => _headerBarWidth;
            set
            {
                _headerBarWidth = Math.Max(1, value);
                Invalidate();
            }
        }

        [Category("AFMS Header")]
        [DefaultValue(18)]
        public int HeaderBarHeight
        {
            get => _headerBarHeight;
            set
            {
                _headerBarHeight = Math.Max(1, value);
                Invalidate();
            }
        }

        [Category("AFMS Header")]
        [DefaultValue(10)]
        public int HeaderBarTextGap
        {
            get => _headerBarTextGap;
            set
            {
                _headerBarTextGap = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("AFMS Header")]
        [DefaultValue(1F)]
        public float HeaderLineThickness
        {
            get => _headerLineThickness;
            set
            {
                _headerLineThickness = Math.Max(0F, value);
                PerformLayout();
                Invalidate();
            }
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
        [DefaultValue(1F)]
        public float BorderThickness
        {
            get => _borderThickness;
            set
            {
                _borderThickness = Math.Max(0F, value);
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

        [Browsable(false)]
        public Rectangle HeaderRectangle => new Rectangle(0, 0, ClientSize.Width, Math.Min(HeaderHeight, ClientSize.Height));

        [Browsable(false)]
        public override Rectangle DisplayRectangle
        {
            get
            {
                int lineHeight = HeaderLineThickness > 0F ? (int)Math.Ceiling(HeaderLineThickness) : 0;
                int x = Padding.Left;
                int y = HeaderHeight + lineHeight + Padding.Top;
                int width = Math.Max(0, ClientSize.Width - Padding.Horizontal);
                int height = Math.Max(0, ClientSize.Height - y - Padding.Bottom);
                return new Rectangle(x, y, width, height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            float inset = BorderThickness > 0F ? BorderThickness / 2F : 0F;
            RectangleF outerRect = new RectangleF(inset, inset, Math.Max(0F, ClientSize.Width - BorderThickness), Math.Max(0F, ClientSize.Height - BorderThickness));
            float radius = Math.Max(0F, Math.Min(BorderRadius, Math.Min(outerRect.Width, outerRect.Height) / 2F) - inset);

            using GraphicsPath outerPath = CreateRoundPath(outerRect, radius);
            using SolidBrush backBrush = new SolidBrush(BackColor);
            e.Graphics.FillPath(backBrush, outerPath);

            DrawHeader(e.Graphics);

            if (BorderThickness > 0F)
            {
                using Pen borderPen = new Pen(BorderColor, BorderThickness) { Alignment = PenAlignment.Center };
                e.Graphics.DrawPath(borderPen, outerPath);
            }
        }

        protected override void OnPaddingChanged(EventArgs e)
        {
            base.OnPaddingChanged(e);
            PerformLayout();
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

        private void DrawHeader(Graphics g)
        {
            int headerHeight = Math.Min(HeaderHeight, ClientSize.Height);
            if (headerHeight <= 0) return;

            int barHeight = Math.Min(HeaderBarHeight, Math.Max(1, headerHeight - 8));
            float barX = HeaderHorizontalPadding;
            float barY = (headerHeight - barHeight) / 2F;

            using GraphicsPath barPath = CreateRoundPath(new RectangleF(barX, barY, HeaderBarWidth, barHeight), HeaderBarWidth / 2F);
            using SolidBrush headerBrush = new SolidBrush(HeaderColor);
            g.FillPath(headerBrush, barPath);

            int textX = HeaderHorizontalPadding + HeaderBarWidth + HeaderBarTextGap;
            Rectangle textRect = new Rectangle(textX, 0, Math.Max(0, ClientSize.Width - textX - HeaderHorizontalPadding), headerHeight);

            TextRenderer.DrawText(g, HeaderText, Font, textRect, HeaderColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            if (HeaderLineThickness <= 0F) return;

            float lineY = HeaderHeight + (HeaderLineThickness / 2F);
            float lineLeft = HeaderHorizontalPadding;
            float lineRight = ClientSize.Width - HeaderHorizontalPadding;

            if (lineRight <= lineLeft) return;

            using Pen linePen = new Pen(HeaderLineColor, HeaderLineThickness);
            g.DrawLine(linePen, lineLeft, lineY, lineRight, lineY);
        }

        private static GraphicsPath CreateRoundPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rect.Width <= 0F || rect.Height <= 0F) return path;

            if (radius <= 0F)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float r = Math.Min(radius, Math.Min(rect.Width / 2F, rect.Height / 2F));
            float diameter = r * 2F;

            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180F, 90F);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270F, 90F);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0F, 90F);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90F, 90F);
            path.CloseFigure();

            return path;
        }
    }
}
