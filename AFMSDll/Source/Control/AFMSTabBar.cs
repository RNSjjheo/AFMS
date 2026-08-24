using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AFMSDll
{
    public class AFMSTabBarItem
    {
        public string Text { get; set; } = string.Empty;
        public Image? NormalImage { get; set; }
        public Image? SelectedImage { get; set; }

        [Browsable(false)]
        public Image? Image
        {
            get => NormalImage;
            set => NormalImage = value;
        }

        public bool Enabled { get; set; } = true;
        public int Width { get; set; }
        [Browsable(false)]
        public object? Tag { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    [ToolboxItem(true)]
    public class AFMSTabBar : Control
    {
        private readonly BindingList<AFMSTabBarItem> _items = new BindingList<AFMSTabBarItem>();

        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private bool _rightButtonHover;
        private Bitmap? _rightButtonImage;

        private Color _barBackColor = Color.FromArgb(240, 244, 249);
        private Color _selectedBackColor = Color.White;
        private Color _normalBackColor = Color.Transparent;
        private Color _hoverBackColor = Color.FromArgb(247, 250, 253);
        private Color _selectedForeColor = Color.FromArgb(5, 149, 105);
        private Color _normalForeColor = Color.FromArgb(100, 115, 135);
        private Color _disabledForeColor = Color.FromArgb(165, 172, 182);
        private Color _borderColor = Color.FromArgb(196, 206, 219);
        private Color _accentColor = Color.FromArgb(5, 149, 105);
        private Color _rightIconColor = Color.FromArgb(100, 115, 135);
        private Color _rightHoverBackColor = Color.FromArgb(226, 234, 242);

        private int _tabHeight = 35;
        private int _tabMinWidth = 120;
        private int _tabLeftMargin = 15;
        private int _tabGap = 4;
        private int _tabHorizontalPadding = 16;
        private int _iconSize = 16;
        private int _iconTextGap = 8;
        private int _cornerRadius = 5;
        private int _rightButtonWidth = 48;
        private FontStyle _selectedFontStyle = FontStyle.Bold;
        private int _letterSpacing;

        public AFMSTabBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

            Height = 48;
            MinimumSize = new Size(100, 40);
            BackColor = Color.Transparent;
            Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Cursor = Cursors.Default;

            _items.ListChanged += (_, _) =>
            {
                NormalizeSelectedIndex();
                Invalidate();
            };
        }

        [Category("AFMS Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public BindingList<AFMSTabBarItem> Items => _items;

        [Category("AFMS Behavior")]
        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int newValue = value;

                if (_items.Count == 0) newValue = -1;
                else newValue = Math.Max(-1, Math.Min(value, _items.Count - 1));

                if (_selectedIndex == newValue) return;

                _selectedIndex = newValue;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Browsable(false)]
        public AFMSTabBarItem? SelectedItem
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _items.Count) return null;
                return _items[_selectedIndex];
            }
        }

        [Category("AFMS Behavior")]
        [DefaultValue(true)]
        public bool RightButtonVisible { get; set; } = true;

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Description("우측 설정 버튼에 표시할 외부 Bitmap 이미지입니다. null이면 기본 기어 아이콘을 표시합니다.")]
        public Bitmap? RightButtonImage
        {
            get => _rightButtonImage;
            set
            {
                if (ReferenceEquals(_rightButtonImage, value)) return;

                _rightButtonImage?.Dispose();
                _rightButtonImage = value == null ? null : new Bitmap(value);
                Invalidate(GetRightButtonRectangle());
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BarBackColor
        {
            get => _barBackColor;
            set { _barBackColor = value; Invalidate(); }
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
        public Color NormalBackColor
        {
            get => _normalBackColor;
            set { _normalBackColor = value; Invalidate(); }
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
        public Color SelectedForeColor
        {
            get => _selectedForeColor;
            set { _selectedForeColor = value; Invalidate(); }
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
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DefaultValue(35)]
        public int TabHeight
        {
            get => _tabHeight;
            set
            {
                _tabHeight = Math.Max(24, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DefaultValue(120)]
        public int TabMinWidth
        {
            get => _tabMinWidth;
            set
            {
                _tabMinWidth = Math.Max(40, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(15)]
        [Description("첫 번째 탭이 시작되는 왼쪽 여백입니다.")]
        public int TabLeftMargin
        {
            get => _tabLeftMargin;
            set
            {
                _tabLeftMargin = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(4)]
        public int TabGap
        {
            get => _tabGap;
            set
            {
                _tabGap = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(16)]
        public int TabHorizontalPadding
        {
            get => _tabHorizontalPadding;
            set
            {
                _tabHorizontalPadding = Math.Max(4, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(16)]
        public int IconSize
        {
            get => _iconSize;
            set
            {
                _iconSize = Math.Max(8, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(8)]
        public int IconTextGap
        {
            get => _iconTextGap;
            set
            {
                _iconTextGap = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(5)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(48)]
        public int RightButtonWidth
        {
            get => _rightButtonWidth;
            set
            {
                _rightButtonWidth = Math.Max(28, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(FontStyle.Bold)]
        [Description("선택된 탭에 적용할 글꼴 스타일입니다. 글꼴 종류와 크기는 기본 Font 속성에서 설정합니다.")]
        public FontStyle SelectedFontStyle
        {
            get => _selectedFontStyle;
            set
            {
                if (_selectedFontStyle == value) return;
                _selectedFontStyle = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(0)]
        [Description("탭 텍스트의 자간(px)입니다. 음수는 자간을 줄이고 양수는 자간을 늘립니다.")]
        public int LetterSpacing
        {
            get => _letterSpacing;
            set
            {
                if (_letterSpacing == value) return;
                _letterSpacing = value;
                Invalidate();
            }
        }

        [Category("Action")]
        public event EventHandler? SelectedIndexChanged;

        [Category("Action")]
        public event EventHandler? RightButtonClick;

        public AFMSTabBarItem AddTab(string text, Image? image = null, object? tag = null)
        {
            return AddTab(text, image, null, tag);
        }

        public AFMSTabBarItem AddTab(string text, Image? normalImage, Image? selectedImage, object? tag = null)
        {
            AFMSTabBarItem item = new AFMSTabBarItem
            {
                Text = text,
                NormalImage = normalImage,
                SelectedImage = selectedImage,
                Tag = tag,
            };

            _items.Add(item);

            if (_selectedIndex < 0) SelectedIndex = 0;

            return item;
        }

        public void ClearTabs()
        {
            _items.Clear();
            SelectedIndex = -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rightButtonImage?.Dispose();
                _rightButtonImage = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            using SolidBrush brush = new SolidBrush(BarBackColor);
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawBottomLine(g);
            DrawTabs(g);
            DrawRightButton(g);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int oldHoverIndex = _hoverIndex;
            bool oldRightHover = _rightButtonHover;

            _hoverIndex = HitTestTab(e.Location);
            _rightButtonHover = RightButtonVisible && GetRightButtonRectangle().Contains(e.Location);

            if (oldHoverIndex != _hoverIndex || oldRightHover != _rightButtonHover) Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_hoverIndex == -1 && !_rightButtonHover) return;

            _hoverIndex = -1;
            _rightButtonHover = false;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button != MouseButtons.Left) return;

            if (RightButtonVisible && GetRightButtonRectangle().Contains(e.Location))
            {
                RightButtonClick?.Invoke(this, EventArgs.Empty);
                return;
            }

            int index = HitTestTab(e.Location);

            if (index < 0 || index >= _items.Count) return;
            if (!_items[index].Enabled) return;

            SelectedIndex = index;
        }

        private void DrawBottomLine(Graphics g)
        {
            if (Height <= 0 || Width <= 0) return;

            using SolidBrush brush = new SolidBrush(AccentColor);
            g.FillRectangle(brush, 0, Height - 1, Width, 1);
        }

        private void DrawTabs(Graphics g)
        {
            using Font selectedFont = new Font(Font, SelectedFontStyle);

            for (int i = 0; i < _items.Count; i++)
            {
                Rectangle rect = GetTabRectangle(i);

                if (rect.Right > GetTabsRightLimit()) break;

                AFMSTabBarItem item = _items[i];
                bool selected = i == SelectedIndex;
                bool hover = i == _hoverIndex && !selected && item.Enabled;

                DrawTabBackground(g, rect, selected, hover);
                DrawTabContent(g, rect, item, selected, selectedFont);
            }
        }

        private void DrawTabBackground(Graphics g, Rectangle rect, bool selected, bool hover)
        {
            Color backColor = selected ? SelectedBackColor : hover ? HoverBackColor : NormalBackColor;
            Color borderColor = selected ? AccentColor : BorderColor;

            using GraphicsPath fillPath = CreateTopRoundedPath(rect, CornerRadius);
            using GraphicsPath borderPath = CreateTopRoundedBorderPath(rect, CornerRadius);
            using SolidBrush brush = new SolidBrush(backColor);
            using Pen pen = new Pen(borderColor, 1F);

            if (backColor.A > 0) g.FillPath(brush, fillPath);

            SmoothingMode oldSmoothingMode = g.SmoothingMode;
            PixelOffsetMode oldPixelOffsetMode = g.PixelOffsetMode;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawPath(pen, borderPath);

            g.SmoothingMode = oldSmoothingMode;
            g.PixelOffsetMode = oldPixelOffsetMode;

            if (selected)
            {
                using SolidBrush bottomBrush = new SolidBrush(SelectedBackColor);
                g.FillRectangle(bottomBrush, rect.Left + 1, rect.Bottom - 1, Math.Max(1, rect.Width - 2), 1);
            }
        }

        private void DrawTabContent(Graphics g, Rectangle rect, AFMSTabBarItem item, bool selected, Font selectedFont)
        {
            Font drawFont = selected ? selectedFont : Font;
            Color foreColor = !item.Enabled ? _disabledForeColor : selected ? SelectedForeColor : NormalForeColor;
            Image? image = GetItemImage(item, selected);

            int textWidth = MeasureTextWidth(g, item.Text, drawFont);
            int imageWidth = image == null ? 0 : IconSize;
            int gap = image == null ? 0 : IconTextGap;
            int contentWidth = imageWidth + gap + textWidth;
            int x = rect.Left + Math.Max(0, (rect.Width - contentWidth) / 2);

            if (image != null)
            {
                int imageY = rect.Top + (rect.Height - IconSize) / 2;
                g.DrawImage(image, new Rectangle(x, imageY, IconSize, IconSize));
                x += IconSize + IconTextGap;
            }

            int textRight = rect.Right - Math.Max(2, (rect.Width - contentWidth) / 2);
            Rectangle textRect = new Rectangle(x, rect.Top, Math.Max(1, textRight - x + 2), rect.Height);
            DrawTabText(g, item.Text, drawFont, textRect, foreColor);
        }

        private static Image? GetItemImage(AFMSTabBarItem item, bool selected)
        {
            if (selected && item.SelectedImage != null) return item.SelectedImage;
            return item.NormalImage ?? item.SelectedImage;
        }

        private void DrawRightButton(Graphics g)
        {
            if (!RightButtonVisible) return;

            Rectangle rect = GetRightButtonRectangle();

            if (_rightButtonHover)
            {
                using SolidBrush hoverBrush = new SolidBrush(_rightHoverBackColor);
                g.FillRectangle(hoverBrush, rect);
            }

            if (RightButtonImage != null)
            {
                int size = Math.Min(20, Math.Min(rect.Width - 8, rect.Height - 8));
                int x = rect.Left + (rect.Width - size) / 2;
                int y = rect.Top + (rect.Height - size) / 2;

                g.DrawImage(RightButtonImage, new Rectangle(x, y, size, size));
                return;
            }

            DrawGearIcon(g, rect);
        }

        private void DrawGearIcon(Graphics g, Rectangle rect)
        {
            PointF center = new PointF(rect.Left + rect.Width / 2F, rect.Top + rect.Height / 2F);
            const float outerRadius = 8F;
            const float innerRadius = 3F;

            using Pen pen = new Pen(_rightIconColor, 2F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            for (int i = 0; i < 8; i++)
            {
                double angle = Math.PI * 2D * i / 8D;
                float x1 = center.X + (float)Math.Cos(angle) * 5F;
                float y1 = center.Y + (float)Math.Sin(angle) * 5F;
                float x2 = center.X + (float)Math.Cos(angle) * outerRadius;
                float y2 = center.Y + (float)Math.Sin(angle) * outerRadius;

                g.DrawLine(pen, x1, y1, x2, y2);
            }

            g.DrawEllipse(pen, center.X - 5F, center.Y - 5F, 10F, 10F);
            g.DrawEllipse(pen, center.X - innerRadius, center.Y - innerRadius, innerRadius * 2F, innerRadius * 2F);
        }

        private Rectangle GetTabRectangle(int index)
        {
            int x = TabLeftMargin;

            for (int i = 0; i < index; i++)
            {
                x += GetTabWidth(_items[i]) + TabGap;
            }

            int width = GetTabWidth(_items[index]);
            int y = Math.Max(0, Height - TabHeight);

            return new Rectangle(x, y, width, TabHeight);
        }

        private int GetTabWidth(AFMSTabBarItem item)
        {
            if (item.Width > 0) return item.Width;

            int normalTextWidth = MeasureTextWidth(item.Text, Font);

            using Font selectedFont = new Font(Font, SelectedFontStyle);
            int selectedTextWidth = MeasureTextWidth(item.Text, selectedFont);
            int textWidth = Math.Max(normalTextWidth, selectedTextWidth);

            bool hasImage = item.NormalImage != null || item.SelectedImage != null;
            int iconWidth = hasImage ? IconSize + IconTextGap : 0;

            return Math.Max(TabMinWidth, textWidth + iconWidth + (TabHorizontalPadding * 2));
        }

        private int MeasureTextWidth(string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            int width = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
            if (LetterSpacing == 0 || text.Length <= 1) return width;

            return Math.Max(1, width + ((text.Length - 1) * LetterSpacing));
        }

        private int MeasureTextWidth(Graphics g, string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            if (LetterSpacing == 0) return TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;

            IntPtr hdc = g.GetHdc();
            IntPtr hFont = font.ToHfont();
            IntPtr oldFont = IntPtr.Zero;

            try
            {
                oldFont = SelectObject(hdc, hFont);
                SetTextCharacterExtra(hdc, LetterSpacing);
                return GetTextExtentPoint32W(hdc, text, text.Length, out NativeSize size) ? Math.Max(1, size.Width) : MeasureTextWidth(text, font);
            }
            finally
            {
                SetTextCharacterExtra(hdc, 0);
                if (oldFont != IntPtr.Zero) SelectObject(hdc, oldFont);
                DeleteObject(hFont);
                g.ReleaseHdc(hdc);
            }
        }

        private void DrawTabText(Graphics g, string text, Font font, Rectangle rect, Color color)
        {
            if (string.IsNullOrEmpty(text) || rect.Width <= 0 || rect.Height <= 0) return;

            if (LetterSpacing == 0)
            {
                TextRenderer.DrawText(g, text, font, rect, color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                return;
            }

            IntPtr hdc = g.GetHdc();
            IntPtr hFont = font.ToHfont();
            IntPtr oldFont = IntPtr.Zero;

            try
            {
                oldFont = SelectObject(hdc, hFont);
                SetBkMode(hdc, TRANSPARENT);
                SetTextColor(hdc, ColorTranslator.ToWin32(color));
                SetTextCharacterExtra(hdc, LetterSpacing);

                NativeRect nativeRect = new NativeRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
                DrawTextW(hdc, text, text.Length, ref nativeRect, DT_LEFT | DT_VCENTER | DT_SINGLELINE | DT_END_ELLIPSIS | DT_NOPREFIX);
            }
            finally
            {
                SetTextCharacterExtra(hdc, 0);
                if (oldFont != IntPtr.Zero) SelectObject(hdc, oldFont);
                DeleteObject(hFont);
                g.ReleaseHdc(hdc);
            }
        }

        private const int TRANSPARENT = 1;
        private const uint DT_LEFT = 0x0000;
        private const uint DT_VCENTER = 0x0004;
        private const uint DT_SINGLELINE = 0x0020;
        private const uint DT_NOPREFIX = 0x0800;
        private const uint DT_END_ELLIPSIS = 0x8000;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeSize
        {
            public int Width;
            public int Height;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public NativeRect(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern int SetBkMode(IntPtr hdc, int mode);

        [DllImport("gdi32.dll")]
        private static extern uint SetTextColor(IntPtr hdc, int colorRef);

        [DllImport("gdi32.dll")]
        private static extern int SetTextCharacterExtra(IntPtr hdc, int charExtra);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTextExtentPoint32W(IntPtr hdc, string text, int textLength, out NativeSize size);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int DrawTextW(IntPtr hdc, string text, int textLength, ref NativeRect rect, uint format);

        private int HitTestTab(Point point)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                Rectangle rect = GetTabRectangle(i);

                if (rect.Right > GetTabsRightLimit()) break;
                if (rect.Contains(point)) return i;
            }

            return -1;
        }

        private int GetTabsRightLimit()
        {
            return RightButtonVisible ? Width - RightButtonWidth : Width;
        }

        private Rectangle GetRightButtonRectangle()
        {
            return new Rectangle(Math.Max(0, Width - RightButtonWidth), 0, Math.Min(RightButtonWidth, Width), Height - 1);
        }

        private void NormalizeSelectedIndex()
        {
            if (_items.Count == 0)
            {
                _selectedIndex = -1;
                return;
            }

            if (_selectedIndex >= _items.Count) _selectedIndex = _items.Count - 1;
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
            float d = r * 2F;

            path.StartFigure();
            path.AddLine(left, bottom, left, top + r);
            path.AddArc(left, top, d, d, 180F, 90F);
            path.AddLine(left + r, top, right - r, top);
            path.AddArc(right - d, top, d, d, 270F, 90F);
            path.AddLine(right, top + r, right, bottom);

            return path;
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
            int d = r * 2;

            path.AddArc(rect.Left, rect.Top, d, d, 180F, 90F);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270F, 90F);
            path.AddLine(rect.Right, rect.Top + r, rect.Right, rect.Bottom);
            path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
            path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + r);
            path.CloseFigure();

            return path;
        }
    }
}
