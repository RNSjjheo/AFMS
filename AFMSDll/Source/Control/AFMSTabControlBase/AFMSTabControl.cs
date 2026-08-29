using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    public enum AFMSTabSizingMode
    {
        Fill,
        Equal,
        Individual
    }

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
        private Color _selectedForeColor = Color.FromArgb(0, 102, 255);
        private Color _normalBackColor = Color.Transparent;
        private Color _normalForeColor = Color.FromArgb(38, 44, 52);
        private Color _hoverBackColor = Color.FromArgb(247, 250, 253);
        private Color _selectedBorderColor = Color.FromArgb(0, 102, 255);

        private int _headerHeight = 34;
        private int _tabHeight = 25;
        private int _tabLeftMargin = 6;
        private int _tabTopMargin = 5;
        private int _tabGap = 4;
        private int _tabHorizontalPadding = 10;
        private int _iconTextGap = 5;
        private int _selectionIndicatorThickness = 3;
        private int _selectionIndicatorHorizontalInset = 12;
        private int _selectionIndicatorBottomOffset = 4;
        private int _equalTabWidth = 120;
        private AFMSTabSizingMode _tabSizingMode = AFMSTabSizingMode.Fill;
        private int _hoverIndex = -1;
        private int _pressedIndex = -1;

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
                bool inHeader = point.Y >= 0 && point.Y < HeaderHeight;

                if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONDBLCLK)
                {
                    if (!inHeader)
                    {
                        base.WndProc(ref m);
                        return;
                    }

                    int index = HitTestTab(point);
                    _pressedIndex = index;
                    Capture = index >= 0;
                    base.WndProc(ref m);
                    SelectTabFromHeader(index);
                    return;
                }

                int pressedIndex = _pressedIndex;
                int releasedIndex = inHeader ? HitTestTab(point) : -1;

                _pressedIndex = -1;
                Capture = false;

                if (inHeader || pressedIndex >= 0)
                {
                    base.WndProc(ref m);
                    if (releasedIndex >= 0 && (pressedIndex < 0 || pressedIndex == releasedIndex))
                        SelectTabFromHeader(releasedIndex);
                    return;
                }
            }

            base.WndProc(ref m);
        }

        private void SelectTabFromHeader(int index)
        {
            if (index < 0 || index >= TabPages.Count) return;

            if (SelectedIndex != index) SelectedIndex = index;
            TabPages[index].BringToFront();
            PerformLayout();
            Invalidate(new Rectangle(0, 0, Width, Math.Min(HeaderHeight, Height)));
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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("선택된 탭의 글자와 하단 표시선에 함께 적용할 색상입니다.")]
        public Color AccentColor
        {
            get => _selectedForeColor;
            set
            {
                _selectedForeColor = value;
                _selectedBorderColor = value;
                Invalidate();
            }
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

        [Category("AFMS Appearance")]
        [DefaultValue(3)]
        public int SelectionIndicatorThickness
        {
            get => _selectionIndicatorThickness;
            set { _selectionIndicatorThickness = Math.Max(1, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(12)]
        public int SelectionIndicatorHorizontalInset
        {
            get => _selectionIndicatorHorizontalInset;
            set { _selectionIndicatorHorizontalInset = Math.Max(0, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(4)]
        public int SelectionIndicatorBottomOffset
        {
            get => _selectionIndicatorBottomOffset;
            set { _selectionIndicatorBottomOffset = Math.Max(1, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(AFMSTabSizingMode.Fill)]
        [Description("탭 폭 배치 방식입니다. Fill은 전체 폭을 채우고, Equal은 동일 폭, Individual은 내용에 맞는 개별 폭을 사용합니다.")]
        public AFMSTabSizingMode TabSizingMode
        {
            get => _tabSizingMode;
            set { _tabSizingMode = value; PerformLayout(); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(120)]
        [Description("TabSizingMode가 Equal일 때 사용할 탭 폭입니다.")]
        public int EqualTabWidth
        {
            get => _equalTabWidth;
            set { _equalTabWidth = Math.Max(1, value); PerformLayout(); Invalidate(); }
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

            DrawHeaderBottomLine(e.Graphics);

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

            Color foreColor = selected ? SelectedForeColor : NormalForeColor;

            if (selected)
            {
                DrawSelectedTabBackground(g, rect);
            }
            else
            {
                Color backColor = hover ? HoverBackColor : NormalBackColor;
                DrawFlatTabBackground(g, rect, backColor);
            }

            DrawTabContent(g, page, rect, foreColor);

            if (selected) DrawSelectionIndicator(g, rect);
        }

        private void DrawHeaderBottomLine(Graphics g)
        {
            if (Width <= 0 || HeaderHeight <= 0 || HeaderHeight > Height) return;

            using Pen pen = new Pen(BorderColor, Math.Max(1F, BorderThickness));
            g.DrawLine(pen, 0, HeaderHeight - 1, Width, HeaderHeight - 1);
        }

        private void DrawSelectedTabBackground(Graphics g, Rectangle tabRect)
        {
            Rectangle rect = new Rectangle(tabRect.Left, tabRect.Top, tabRect.Width, Math.Max(1, HeaderHeight - tabRect.Top));

            using GraphicsPath fillPath = CreateTopRoundedPath(rect, BorderRadius);
            using GraphicsPath borderPath = CreateTopRoundedBorderPath(rect, BorderRadius);
            using SolidBrush backBrush = new SolidBrush(SelectedBackColor);
            using Pen borderPen = new Pen(BorderColor, Math.Max(1F, BorderThickness))
            {
                Alignment = PenAlignment.Center,
                LineJoin = LineJoin.Round
            };

            g.FillPath(backBrush, fillPath);
            g.DrawPath(borderPen, borderPath);

            // The selected tab visually joins the content surface below it.
            g.FillRectangle(backBrush, rect.Left + 1, HeaderHeight - 1, Math.Max(1, rect.Width - 2), 1);
        }

        private void DrawFlatTabBackground(Graphics g, Rectangle rect, Color color)
        {
            if (color.A == 0) return;

            using GraphicsPath path = CreateTopRoundedPath(rect, Math.Min(BorderRadius, 6));
            using SolidBrush brush = new SolidBrush(color);
            g.FillPath(brush, path);
        }

        private void DrawSelectionIndicator(Graphics g, Rectangle rect)
        {
            int maxInset = Math.Max(0, (rect.Width - 1) / 2);
            int inset = Math.Min(SelectionIndicatorHorizontalInset, maxInset);
            int width = Math.Max(1, rect.Width - (inset * 2));
            int y = Math.Max(rect.Top, HeaderHeight - SelectionIndicatorBottomOffset - SelectionIndicatorThickness);

            using SolidBrush brush = new SolidBrush(SelectedBorderColor);
            g.FillRectangle(brush, rect.Left + inset, y, width, SelectionIndicatorThickness);
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
            if (TabSizingMode == AFMSTabSizingMode.Fill && TabPages.Count > 0)
            {
                int gapWidth = TabGap * Math.Max(0, TabPages.Count - 1);
                int availableWidth = Math.Max(1, Width - (TabLeftMargin * 2) - gapWidth);
                int baseWidth = availableWidth / TabPages.Count;
                int remainder = availableWidth % TabPages.Count;
                int stretchedX = TabLeftMargin + (index * baseWidth) + Math.Min(index, remainder) + (index * TabGap);
                int width = baseWidth + (index < remainder ? 1 : 0);

                return new Rectangle(stretchedX, TabTopMargin, width, TabHeight);
            }

            if (TabSizingMode == AFMSTabSizingMode.Equal)
            {
                int equalX = TabLeftMargin + (index * (EqualTabWidth + TabGap));
                return new Rectangle(equalX, TabTopMargin, EqualTabWidth, TabHeight);
            }

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

        private static GraphicsPath CreateTopRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            int r = Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2));
            if (r <= 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            int diameter = r * 2;

            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180F, 90F);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270F, 90F);
            path.AddLine(rect.Right, rect.Top + r, rect.Right, rect.Bottom);
            path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
            path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + r);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath CreateTopRoundedBorderPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float left = rect.Left + 0.5F;
            float top = rect.Top + 0.5F;
            float right = rect.Right - 0.5F;
            float bottom = rect.Bottom - 0.5F;

            if (radius <= 0)
            {
                path.StartFigure();
                path.AddLine(left, bottom, left, top);
                path.AddLine(left, top, right, top);
                path.AddLine(right, top, right, bottom);
                return path;
            }

            float r = Math.Min(radius, Math.Min(rect.Width / 2F, rect.Height / 2F));
            if (r <= 0F)
            {
                path.StartFigure();
                path.AddLine(left, bottom, left, top);
                path.AddLine(left, top, right, top);
                path.AddLine(right, top, right, bottom);
                return path;
            }

            float diameter = r * 2F;

            path.StartFigure();
            path.AddLine(left, bottom, left, top + r);
            path.AddArc(left, top, diameter, diameter, 180F, 90F);
            path.AddLine(left + r, top, right - r, top);
            path.AddArc(right - diameter, top, diameter, diameter, 270F, 90F);
            path.AddLine(right, top + r, right, bottom);
            return path;
        }

        private int HitTestTab(Point point)
        {
            if (point.Y < 0 || point.Y >= HeaderHeight) return -1;

            for (int i = 0; i < TabPages.Count; i++)
            {
                Rectangle rect = GetCustomTabRectangle(i);
                if (point.X >= rect.Left && point.X < rect.Right) return i;
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
