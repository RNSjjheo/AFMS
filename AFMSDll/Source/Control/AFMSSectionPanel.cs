using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    public enum AFMSSectionStyle
    {
        OutlineTitle,
        AccentHeader,
        FilledHeader
    }

    public enum AFMSHeaderBarPosition
    {
        Left,
        Right
    }

    [ToolboxItem(true)]
    [DefaultProperty(nameof(HeaderText))]
    public class AFMSSectionPanel : Panel
    {
        private const int HEADER_TEXT_RENDER_MARGIN = 6;

        private readonly Label _headerLabel;
        private readonly TableLayoutPanel _contentLayout;
        private RectangleF _headerBarRectangle;
        private Rectangle _headerTextRectangle;
        private Rectangle _headerTextBackgroundRectangle;

        private AFMSSectionStyle _sectionStyle = AFMSSectionStyle.AccentHeader;
        private AFMSHeaderBarPosition _headerBarPosition = AFMSHeaderBarPosition.Left;
        private string _headerText = string.Empty;
        private Color _headerColor = Color.FromArgb(2, 146, 93);
        private Color _headerBackColor = Color.FromArgb(245, 248, 246);
        private Color _headerLineColor = Color.FromArgb(220, 228, 224);
        private Color _borderColor = Color.FromArgb(205, 211, 220);
        private float _borderThickness;
        private float _headerLineThickness = 1F;
        private int _borderRadius;
        private int _headerHeight = 40;
        private int _headerHorizontalPadding = 14;
        private int _headerBarWidth = 3;
        private int _headerBarHeight = 18;
        private int _headerBarTextGap = 10;
        private int _titleHorizontalPadding = 8;
        private int _titleLeftMargin = 8;
        private Padding _contentPadding = new Padding(10, 8, 10, 8);

        public AFMSSectionPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

            DoubleBuffered = true;
            base.BorderStyle = BorderStyle.None;
            BackColor = Color.White;
            Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Bold, GraphicsUnit.Point);
            Padding = Padding.Empty;

            _headerLabel = new Label();
            _headerLabel.AutoSize = false;
            _headerLabel.AutoEllipsis = true;
            _headerLabel.BackColor = Color.Transparent;
            _headerLabel.Font = Font;
            _headerLabel.ForeColor = HeaderColor;
            _headerLabel.TextAlign = ContentAlignment.MiddleLeft;
            _headerLabel.UseCompatibleTextRendering = false;

            _contentLayout = new TableLayoutPanel();
            _contentLayout.BackColor = Color.Transparent;
            _contentLayout.ColumnCount = 1;
            _contentLayout.RowCount = 1;
            _contentLayout.Margin = Padding.Empty;
            _contentLayout.Padding = ContentPadding;
            _contentLayout.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Controls.Add(_contentLayout);

            ApplyStyleDefaults(AFMSSectionStyle.AccentHeader);
            LayoutInternalControls();
        }

        [Category("AFMS Section")]
        [DefaultValue(AFMSSectionStyle.AccentHeader)]
        public AFMSSectionStyle SectionStyle
        {
            get => _sectionStyle;
            set
            {
                if (_sectionStyle == value) return;

                ApplyStyleDefaults(value);
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DefaultValue(AFMSHeaderBarPosition.Left)]
        public AFMSHeaderBarPosition HeaderBarPosition
        {
            get => _headerBarPosition;
            set
            {
                if (_headerBarPosition == value) return;

                _headerBarPosition = value;
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DefaultValue("")]
        public string HeaderText
        {
            get => _headerText;
            set
            {
                _headerText = value ?? string.Empty;
                _headerLabel.Text = _headerText;
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DefaultValue(ContentAlignment.MiddleLeft)]
        public ContentAlignment HeaderTextAlign
        {
            get => _headerLabel.TextAlign;
            set
            {
                _headerLabel.TextAlign = value;
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HeaderColor
        {
            get => _headerColor;
            set
            {
                _headerColor = value;
                _headerLabel.ForeColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HeaderBackColor
        {
            get => _headerBackColor;
            set
            {
                _headerBackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Section")]
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

        [Category("AFMS Section")]
        [DefaultValue(40)]
        public int HeaderHeight
        {
            get => _headerHeight;
            set
            {
                _headerHeight = Math.Max(20, value);
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DefaultValue(14)]
        public int HeaderHorizontalPadding
        {
            get => _headerHorizontalPadding;
            set
            {
                _headerHorizontalPadding = Math.Max(0, value);
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DefaultValue(3)]
        public int HeaderBarWidth
        {
            get => _headerBarWidth;
            set
            {
                _headerBarWidth = Math.Max(1, value);
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
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

        [Category("AFMS Section")]
        [DefaultValue(10)]
        public int HeaderBarTextGap
        {
            get => _headerBarTextGap;
            set
            {
                _headerBarTextGap = Math.Max(0, value);
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DefaultValue(1F)]
        public float HeaderLineThickness
        {
            get => _headerLineThickness;
            set
            {
                _headerLineThickness = Math.Max(0F, value);
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DefaultValue(8)]
        public int TitleHorizontalPadding
        {
            get => _titleHorizontalPadding;
            set
            {
                _titleHorizontalPadding = Math.Max(0, value);
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Section")]
        [DefaultValue(8)]
        public int TitleLeftMargin
        {
            get => _titleLeftMargin;
            set
            {
                _titleLeftMargin = Math.Max(0, value);
                LayoutInternalControls();
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
        [DefaultValue(0F)]
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
        [DefaultValue(0)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("AFMS Layout")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Padding ContentPadding
        {
            get => _contentPadding;
            set
            {
                _contentPadding = value;
                _contentLayout.Padding = value;
                PerformLayout();
            }
        }

        [Category("AFMS Layout")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TableLayoutPanel ContentLayout => _contentLayout;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label HeaderLabel => _headerLabel;

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

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DefaultValue(BorderStyle.None)]
        public new BorderStyle BorderStyle
        {
            get => BorderStyle.None;
            set => base.BorderStyle = BorderStyle.None;
        }

        public void ApplyStyleDefaults(AFMSSectionStyle style)
        {
            _sectionStyle = style;

            switch (style)
            {
                case AFMSSectionStyle.OutlineTitle:
                    _headerHeight = 28;
                    _borderRadius = 5;
                    _borderThickness = 1F;
                    _headerLineThickness = 0F;
                    _contentPadding = new Padding(12, 8, 12, 12);
                    break;
                case AFMSSectionStyle.FilledHeader:
                    _headerHeight = 32;
                    _borderRadius = 8;
                    _borderThickness = 0.5F;
                    _headerLineThickness = 1F;
                    _contentPadding = new Padding(10, 8, 10, 8);
                    break;
                default:
                    _headerHeight = 40;
                    _borderRadius = 0;
                    _borderThickness = 0F;
                    _headerLineThickness = 1F;
                    _contentPadding = new Padding(14, 12, 14, 14);
                    break;
            }

            if (_contentLayout != null) _contentLayout.Padding = _contentPadding;
            LayoutInternalControls();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInternalControls();
        }

        protected override void OnPaddingChanged(EventArgs e)
        {
            base.OnPaddingChanged(e);
            PerformLayout();
            LayoutInternalControls();
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_headerLabel == null) return;

            _headerLabel.Font = Font;
            LayoutInternalControls();
            Invalidate();
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            if (_headerLabel == null) return;

            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (ClientSize.Width <= 1 || ClientSize.Height <= 1) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            Color parentBackColor = Parent?.BackColor ?? SystemColors.Control;
            e.Graphics.Clear(parentBackColor);

            RectangleF outerRectangle = GetOuterRectangle();
            if (outerRectangle.Width <= 0F || outerRectangle.Height <= 0F) return;

            using GraphicsPath outerPath = CreateRoundedPath(outerRectangle, BorderRadius);
            using SolidBrush backBrush = new SolidBrush(BackColor);
            e.Graphics.FillPath(backBrush, outerPath);

            if (SectionStyle != AFMSSectionStyle.FilledHeader) return;

            GraphicsState state = e.Graphics.Save();
            e.Graphics.SetClip(outerPath);
            using SolidBrush headerBrush = new SolidBrush(HeaderBackColor);
            e.Graphics.FillRectangle(headerBrush, 0F, 0F, ClientSize.Width, Math.Min(HeaderHeight, ClientSize.Height));
            e.Graphics.Restore(state);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (ClientSize.Width <= 1 || ClientSize.Height <= 1) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            DrawHeaderDecoration(e.Graphics);
            DrawBorder(e.Graphics);
            DrawHeaderText(e.Graphics);
        }

        private void LayoutInternalControls()
        {
            if (_headerLabel == null || _contentLayout == null) return;

            int lineHeight = HeaderLineThickness > 0F ? (int)Math.Ceiling(HeaderLineThickness) : 0;
            int contentTop = HeaderHeight + lineHeight;

            if (SectionStyle == AFMSSectionStyle.OutlineTitle)
            {
                Size titleSize = MeasureHeaderText();
                int titleWidth = titleSize.Width + HEADER_TEXT_RENDER_MARGIN + (TitleHorizontalPadding * 2);
                _headerLabel.SetBounds(TitleLeftMargin, 0, Math.Max(0, titleWidth), HeaderHeight);
                _headerLabel.Padding = new Padding(TitleHorizontalPadding, 0, TitleHorizontalPadding, 0);
                _headerTextBackgroundRectangle = _headerLabel.Bounds;
                _headerTextRectangle = new Rectangle(TitleLeftMargin + TitleHorizontalPadding, 0,
                    Math.Max(0, titleWidth - (TitleHorizontalPadding * 2)), HeaderHeight);
            }
            else if (SectionStyle == AFMSSectionStyle.AccentHeader)
            {
                Size titleSize = MeasureHeaderText();
                int availableWidth = Math.Max(0, ClientSize.Width - (HeaderHorizontalPadding * 2));
                int measuredTextWidth = titleSize.Width + HEADER_TEXT_RENDER_MARGIN;
                int textWidth = Math.Min(measuredTextWidth, Math.Max(0, availableWidth - HeaderBarWidth - HeaderBarTextGap));
                int groupWidth = HeaderBarWidth + HeaderBarTextGap + textWidth;
                int groupLeft = GetAlignedHeaderGroupLeft(groupWidth);
                int barHeight = Math.Min(HeaderBarHeight, Math.Max(1, HeaderHeight - 8));
                float barY = (HeaderHeight - barHeight) / 2F;

                if (HeaderBarPosition == AFMSHeaderBarPosition.Left)
                {
                    _headerBarRectangle = new RectangleF(groupLeft, barY, HeaderBarWidth, barHeight);
                    _headerLabel.SetBounds(groupLeft + HeaderBarWidth + HeaderBarTextGap, 0, textWidth, HeaderHeight);
                }
                else
                {
                    _headerLabel.SetBounds(groupLeft, 0, textWidth, HeaderHeight);
                    _headerBarRectangle = new RectangleF(groupLeft + textWidth + HeaderBarTextGap, barY, HeaderBarWidth, barHeight);
                }

                _headerLabel.Padding = Padding.Empty;
                _headerTextBackgroundRectangle = Rectangle.Empty;
                _headerTextRectangle = _headerLabel.Bounds;
            }
            else
            {
                _headerBarRectangle = RectangleF.Empty;
                _headerLabel.SetBounds(HeaderHorizontalPadding, 0, Math.Max(0, ClientSize.Width - (HeaderHorizontalPadding * 2)), HeaderHeight);
                _headerLabel.Padding = Padding.Empty;
                _headerTextBackgroundRectangle = Rectangle.Empty;
                _headerTextRectangle = _headerLabel.Bounds;
            }

            int contentInset = BorderThickness > 0F ? (int)Math.Ceiling(BorderThickness) : 0;
            int contentWidth = Math.Max(0, ClientSize.Width - (contentInset * 2));
            int contentHeight = Math.Max(0, ClientSize.Height - contentTop - contentInset);
            _contentLayout.SetBounds(contentInset, contentTop, contentWidth, contentHeight);
        }

        private void DrawHeaderDecoration(Graphics graphics)
        {
            if (SectionStyle == AFMSSectionStyle.AccentHeader)
            {
                using GraphicsPath barPath = CreateRoundedPath(_headerBarRectangle, HeaderBarWidth / 2F);
                using SolidBrush barBrush = new SolidBrush(HeaderColor);
                graphics.FillPath(barBrush, barPath);
            }

            if (SectionStyle == AFMSSectionStyle.OutlineTitle || HeaderLineThickness <= 0F) return;

            float lineY = HeaderHeight + (HeaderLineThickness / 2F);
            float lineLeft = SectionStyle == AFMSSectionStyle.AccentHeader ? HeaderHorizontalPadding : 1F;
            float lineRight = SectionStyle == AFMSSectionStyle.AccentHeader ? ClientSize.Width - HeaderHorizontalPadding : ClientSize.Width - 1F;
            if (lineRight <= lineLeft) return;

            using Pen linePen = new Pen(HeaderLineColor, HeaderLineThickness);
            graphics.DrawLine(linePen, lineLeft, lineY, lineRight, lineY);
        }

        private void DrawBorder(Graphics graphics)
        {
            if (BorderThickness <= 0F) return;

            RectangleF outerRectangle = GetOuterRectangle();
            if (outerRectangle.Width <= 0F || outerRectangle.Height <= 0F) return;

            using GraphicsPath borderPath = CreateRoundedPath(outerRectangle, BorderRadius);
            using Pen borderPen = new Pen(BorderColor, BorderThickness) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round };
            graphics.DrawPath(borderPen, borderPath);
        }

        private void DrawHeaderText(Graphics graphics)
        {
            if (string.IsNullOrEmpty(HeaderText) || _headerTextRectangle.Width <= 0 || _headerTextRectangle.Height <= 0) return;

            if (SectionStyle == AFMSSectionStyle.OutlineTitle && !_headerTextBackgroundRectangle.IsEmpty)
            {
                using SolidBrush titleBackBrush = new SolidBrush(BackColor);
                graphics.FillRectangle(titleBackBrush, _headerTextBackgroundRectangle);
            }

            TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter;
            flags |= GetHorizontalTextFormatFlags();
            TextRenderer.DrawText(graphics, HeaderText, Font, _headerTextRectangle, HeaderColor, flags);
        }

        private TextFormatFlags GetHorizontalTextFormatFlags()
        {
            switch (HeaderTextAlign)
            {
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    return TextFormatFlags.Right;
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    return TextFormatFlags.HorizontalCenter;
                default:
                    return TextFormatFlags.Left;
            }
        }

        private Size MeasureHeaderText()
        {
            return TextRenderer.MeasureText(HeaderText, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        private int GetAlignedHeaderGroupLeft(int groupWidth)
        {
            int left = HeaderHorizontalPadding;
            int right = Math.Max(left, ClientSize.Width - HeaderHorizontalPadding);

            switch (HeaderTextAlign)
            {
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    return Math.Max(left, right - groupWidth);
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    return Math.Max(left, left + ((right - left - groupWidth) / 2));
                default:
                    return left;
            }
        }

        private RectangleF GetOuterRectangle()
        {
            float inset = BorderThickness > 0F ? BorderThickness / 2F : 0F;
            float top = SectionStyle == AFMSSectionStyle.OutlineTitle ? HeaderHeight / 2F : inset;
            float width = Math.Max(0F, ClientSize.Width - (inset * 2F));
            float height = Math.Max(0F, ClientSize.Height - top - inset);
            return new RectangleF(inset, top, width, height);
        }

        private static GraphicsPath CreateRoundedPath(RectangleF rectangle, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rectangle.Width <= 0F || rectangle.Height <= 0F) return path;

            if (radius <= 0F)
            {
                path.AddRectangle(rectangle);
                path.CloseFigure();
                return path;
            }

            float actualRadius = Math.Min(radius, Math.Min(rectangle.Width, rectangle.Height) / 2F);
            float diameter = actualRadius * 2F;

            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180F, 90F);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270F, 90F);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0F, 90F);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90F, 90F);
            path.CloseFigure();
            return path;
        }
    }
}
