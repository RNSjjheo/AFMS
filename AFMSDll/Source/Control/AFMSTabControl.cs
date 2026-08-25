using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    public class AFMSTabControl : _AFMSTabControlBase
    {
        private const int WS_BORDER = 0x00800000;
        private const int WS_EX_CLIENTEDGE = 0x00000200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;

        private Color _headerBackColor = Color.FromArgb(240, 244, 249);
        private Color _contentBackColor = Color.White;
        private Color _selectedBackColor = Color.White;
        private Color _selectedForeColor = Color.FromArgb(0, 157, 111);
        private Color _normalBackColor = Color.White;
        private Color _normalForeColor = Color.FromArgb(83, 100, 121);
        private Color _hoverBackColor = Color.White;
        private Color _selectedBorderColor = Color.FromArgb(0, 157, 111);

        private int _headerHeight = 34;
        private int _tabHeight = 25;
        private int _tabLeftMargin = 6;
        private int _tabTopMargin = 5;
        private int _tabGap = 4;
        private int _tabHorizontalPadding = 10;
        private int _iconTextGap = 5;
        private int _hoverIndex = -1;

        public AFMSTabControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateStyles();

            Appearance = TabAppearance.Buttons;
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Normal;
            Multiline = false;
            Padding = Point.Empty;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.White;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style &= ~WS_BORDER;
                cp.ExStyle &= ~WS_EX_CLIENTEDGE;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONUP || m.Msg == WM_LBUTTONDBLCLK)
            {
                Point point = GetMousePoint(m.LParam);

                if (point.Y >= 0 && point.Y < HeaderHeight)
                {
                    if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONDBLCLK)
                    {
                        int index = HitTestTab(point);

                        if (index >= 0 && index < TabPages.Count && SelectedIndex != index)
                        {
                            SelectedIndex = index;
                            Focus();
                        }
                    }

                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            base.WndProc(ref m);
        }

        private static Point GetMousePoint(IntPtr lParam)
        {
            long value = lParam.ToInt64();
            int x = unchecked((short)(value & 0xFFFF));
            int y = unchecked((short)((value >> 16) & 0xFFFF));
            return new Point(x, y);
        }

        public override Rectangle DisplayRectangle => new Rectangle(0, HeaderHeight, ClientSize.Width, Math.Max(0, ClientSize.Height - HeaderHeight));

        #region Properties

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HeaderBackColor
        {
            get => _headerBackColor;
            set { _headerBackColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ContentBackColor
        {
            get => _contentBackColor;
            set
            {
                _contentBackColor = value;
                ApplyTabPageBackColor();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedBackColor
        {
            get => _selectedBackColor;
            set { _selectedBackColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedForeColor
        {
            get => _selectedForeColor;
            set { _selectedForeColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color NormalBackColor
        {
            get => _normalBackColor;
            set { _normalBackColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color NormalForeColor
        {
            get => _normalForeColor;
            set { _normalForeColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverBackColor
        {
            get => _hoverBackColor;
            set { _hoverBackColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedBorderColor
        {
            get => _selectedBorderColor;
            set { _selectedBorderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(36)]
        public int HeaderHeight
        {
            get => _headerHeight;
            set
            {
                _headerHeight = Math.Max(TabHeight + TabTopMargin + 1, value);
                PerformLayout();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(25)]
        public int TabHeight
        {
            get => _tabHeight;
            set
            {
                _tabHeight = Math.Max(20, value);
                if (_headerHeight < _tabHeight + _tabTopMargin + 1) _headerHeight = _tabHeight + _tabTopMargin + 1;
                PerformLayout();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(6)]
        public int TabLeftMargin
        {
            get => _tabLeftMargin;
            set { _tabLeftMargin = Math.Max(0, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(5)]
        public int TabTopMargin
        {
            get => _tabTopMargin;
            set
            {
                _tabTopMargin = Math.Max(0, value);
                if (_headerHeight < _tabHeight + _tabTopMargin + 1) _headerHeight = _tabHeight + _tabTopMargin + 1;
                PerformLayout();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(4)]
        public int TabGap
        {
            get => _tabGap;
            set { _tabGap = Math.Max(0, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(10)]
        public int TabHorizontalPadding
        {
            get => _tabHorizontalPadding;
            set { _tabHorizontalPadding = Math.Max(0, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(5)]
        public int IconTextGap
        {
            get => _iconTextGap;
            set { _iconTextGap = Math.Max(0, value); Invalidate(); }
        }

        #endregion

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using SolidBrush contentBrush = new SolidBrush(ContentBackColor);
            using SolidBrush headerBrush = new SolidBrush(HeaderBackColor);

            e.Graphics.FillRectangle(contentBrush, ClientRectangle);
            e.Graphics.FillRectangle(headerBrush, new Rectangle(0, 0, Width, Math.Min(HeaderHeight, Height)));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            for (int i = 0; i < TabPages.Count; i++) DrawTab(e.Graphics, i);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int hoverIndex = HitTestTab(e.Location);
            if (_hoverIndex == hoverIndex) return;

            _hoverIndex = hoverIndex;
            Invalidate(new Rectangle(0, 0, Width, Math.Min(HeaderHeight, Height)));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_hoverIndex < 0) return;

            _hoverIndex = -1;
            Invalidate(new Rectangle(0, 0, Width, Math.Min(HeaderHeight, Height)));
        }

        private void DrawTab(Graphics g, int index)
        {
            Rectangle rect = GetCustomTabRectangle(index);
            if (rect.Width <= 0 || rect.Height <= 0 || rect.Left >= Width) return;

            bool selected = SelectedIndex == index;
            bool hover = !selected && _hoverIndex == index;
            TabPage page = TabPages[index];

            Color backColor = selected ? SelectedBackColor : hover ? HoverBackColor : NormalBackColor;
            Color foreColor = selected ? SelectedForeColor : NormalForeColor;
            Color borderColor = selected ? SelectedBorderColor : BorderColor;

            using GraphicsPath fillPath = AFMSRoundedDrawing.CreatePath(rect, BorderRadius);
            RectangleF borderRectangle = new RectangleF(rect.Left + 0.5F, rect.Top + 0.5F,
                Math.Max(0F, rect.Width - 1F), Math.Max(0F, rect.Height - 1F));
            using GraphicsPath borderPath = AFMSRoundedDrawing.CreatePath(borderRectangle, BorderRadius);
            using SolidBrush backBrush = new SolidBrush(backColor);
            using Pen borderPen = new Pen(borderColor, BorderThickness) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round };

            g.FillPath(backBrush, fillPath);

            SmoothingMode oldSmoothingMode = g.SmoothingMode;
            PixelOffsetMode oldPixelOffsetMode = g.PixelOffsetMode;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawPath(borderPen, borderPath);

            g.SmoothingMode = oldSmoothingMode;
            g.PixelOffsetMode = oldPixelOffsetMode;

            DrawTabContent(g, page, rect, foreColor);
        }

        private void DrawTabContent(Graphics g, TabPage page, Rectangle rect, Color foreColor)
        {
            Image? image = GetTabImage(page);
            Size textSize = TextRenderer.MeasureText(page.Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            int imageWidth = image?.Width ?? 0;
            int imageHeight = image?.Height ?? 0;
            int gap = image == null ? 0 : IconTextGap;
            int contentWidth = imageWidth + gap + textSize.Width;
            int x = rect.Left + Math.Max(TabHorizontalPadding, (rect.Width - contentWidth) / 2);

            if (image != null)
            {
                int imageY = rect.Top + ((rect.Height - imageHeight) / 2);
                g.DrawImage(image, new Rectangle(x, imageY, imageWidth, imageHeight));
                x += imageWidth + IconTextGap;
            }

            Rectangle textRect = new Rectangle(x, rect.Top, Math.Max(1, rect.Right - x - TabHorizontalPadding), rect.Height);
            TextRenderer.DrawText(g, page.Text, Font, textRect, foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        private Rectangle GetCustomTabRectangle(int index)
        {
            int x = TabLeftMargin;

            for (int i = 0; i < index; i++) x += GetTabWidth(TabPages[i]) + TabGap;

            return new Rectangle(x, TabTopMargin, GetTabWidth(TabPages[index]), TabHeight);
        }

        private int GetTabWidth(TabPage page)
        {
            Size textSize = TextRenderer.MeasureText(page.Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            Image? image = GetTabImage(page);

            int imageWidth = image?.Width ?? 0;
            int gap = image == null ? 0 : IconTextGap;

            return Math.Max(TabHeight, textSize.Width + imageWidth + gap + (TabHorizontalPadding * 2));
        }

        private int HitTestTab(Point point)
        {
            if (point.Y < 0 || point.Y >= HeaderHeight) return -1;

            for (int i = 0; i < TabPages.Count; i++)
            {
                Rectangle rect = GetCustomTabRectangle(i);
                if (rect.Contains(point)) return i;
            }

            return -1;
        }

        private Image? GetTabImage(TabPage page)
        {
            if (ImageList == null) return null;
            if (!string.IsNullOrEmpty(page.ImageKey) && ImageList.Images.ContainsKey(page.ImageKey)) return ImageList.Images[page.ImageKey];
            if (page.ImageIndex >= 0 && page.ImageIndex < ImageList.Images.Count) return ImageList.Images[page.ImageIndex];
            return null;
        }

        private void ApplyTabPageBackColor()
        {
            foreach (TabPage page in TabPages)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = ContentBackColor;
            }
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            if (e.Control is TabPage page)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = ContentBackColor;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyTabPageBackColor();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            ApplyTabPageBackColor();
            PerformLayout();
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            PerformLayout();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PerformLayout();
            Invalidate();
        }
    }
}
