using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    public class AFMSForm : Form
    {
        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int CS_DROPSHADOW = 0x00020000;

        private readonly Button _btnMinimize;
        private readonly Button _btnMaximize;
        private readonly Button _btnClose;
        private readonly System.Windows.Forms.Timer _modalClickTimer;
        private readonly System.Windows.Forms.Timer _shakeTimer;

        private static readonly int[] SHAKE_OFFSETS = { -6, 6, -5, 5, -3, 3, 0 };

        private bool _modalMouseDown;
        private int _shakeIndex;
        private Point _shakeOrigin;

        private Color _titleBarColor = DllColorHelper.HexToColor("#059569");
        private Color _titleForeColor = Color.FromArgb(220, 225, 233);
        private Color _titleButtonForeColor = Color.FromArgb(210, 216, 225);
        private Color _titleButtonHoverColor = Color.FromArgb(58, 66, 84);
        private Color _closeButtonHoverColor = Color.FromArgb(196, 43, 28);
        private Color _contentBackColor = Color.White;
        private Color _windowBorderColor = Color.FromArgb(143, 156, 150);
        private Color _inactiveWindowBorderColor = Color.FromArgb(190, 199, 195);
        private Image? _titleBarImage;
        private bool _showTitleBarIcon = true;

        private int _titleBarHeight = 31;
        private int _windowBorderThickness = 1;
        private int _borderRadius = 0;
        private int _resizeBorderWidth = 6;
        private bool _showInfoButton = true;
        private bool _showMinimizeButton = true;
        private bool _showMaximizeButton = true;
        private bool _showWindowShadow = true;
        private bool _windowActive = true;

        public AFMSForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = _contentBackColor;
            MinimumSize = new Size(400, 250);
            DoubleBuffered = true;
            ResizeRedraw = true;
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            ShowIcon = false;

            _btnMinimize = CreateTitleButton();
            _btnMaximize = CreateTitleButton();
            _btnClose = CreateTitleButton();

            _modalClickTimer = new System.Windows.Forms.Timer();
            _modalClickTimer.Interval = 30;
            _modalClickTimer.Tick += ModalClickTimer_Tick;

            _shakeTimer = new System.Windows.Forms.Timer();
            _shakeTimer.Interval = 25;
            _shakeTimer.Tick += ShakeTimer_Tick;

            _btnMinimize.Paint += BtnMinimize_Paint;
            _btnMaximize.Paint += BtnMaximize_Paint;
            _btnClose.Paint += BtnClose_Paint;

            _btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;
            _btnMaximize.Click += (s, e) => ToggleMaximize();
            _btnClose.Click += (s, e) => Close();

            _btnClose.MouseEnter += (s, e) => _btnClose.BackColor = _closeButtonHoverColor;
            _btnClose.MouseLeave += (s, e) => _btnClose.BackColor = _titleBarColor;

            Controls.Add(_btnMinimize);
            Controls.Add(_btnMaximize);
            Controls.Add(_btnClose);

            LayoutChrome();
        }

        public override Rectangle DisplayRectangle
        {
            get
            {
                int border = WindowBorderThickness;
                return new Rectangle(border, TitleBarHeight, Math.Max(0, ClientSize.Width - (border * 2)), Math.Max(0, ClientSize.Height - TitleBarHeight - border));
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (ShowWindowShadow) cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(36)]
        public int TitleBarHeight
        {
            get => _titleBarHeight;
            set
            {
                _titleBarHeight = Math.Max(28, value);
                LayoutChrome();
                PerformLayout();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TitleBarColor
        {
            get => _titleBarColor;
            set
            {
                _titleBarColor = value;
                ApplyTitleButtonColors();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TitleForeColor
        {
            get => _titleForeColor;
            set { _titleForeColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ContentBackColor
        {
            get => _contentBackColor;
            set
            {
                _contentBackColor = value;
                BackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color WindowBorderColor
        {
            get => _windowBorderColor;
            set { _windowBorderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color InactiveWindowBorderColor
        {
            get => _inactiveWindowBorderColor;
            set { _inactiveWindowBorderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(null)]
        public Image? TitleBarImage
        {
            get => _titleBarImage;
            set
            {
                Image? previous = _titleBarImage;
                _titleBarImage = value == null ? null : new Bitmap(value);
                previous?.Dispose();
                Invalidate(new Rectangle(0, 0, ClientSize.Width, TitleBarHeight));
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(true)]
        public bool ShowTitleBarIcon
        {
            get => _showTitleBarIcon;
            set
            {
                _showTitleBarIcon = value;
                Invalidate(new Rectangle(0, 0, ClientSize.Width, TitleBarHeight));
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1)]
        public int WindowBorderThickness
        {
            get => _windowBorderThickness;
            set
            {
                _windowBorderThickness = Math.Max(0, Math.Min(4, value));
                LayoutChrome();
                PerformLayout();
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
                UpdateFormRegion();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(true)]
        public bool ShowWindowShadow
        {
            get => _showWindowShadow;
            set
            {
                if (_showWindowShadow == value) return;
                _showWindowShadow = value;
                if (IsHandleCreated) RecreateHandle();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(true)]
        public bool ShowInfoButton
        {
            get => _showInfoButton;
            set
            {
                _showInfoButton = value;
                LayoutChrome();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(true)]
        public bool ShowMinimizeButton
        {
            get => _showMinimizeButton;
            set
            {
                if (_showMinimizeButton == value) return;

                _showMinimizeButton = value;
                LayoutChrome();
                Invalidate(new Rectangle(0, 0, ClientSize.Width, TitleBarHeight));
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(true)]
        public bool ShowMaximizeButton
        {
            get => _showMaximizeButton;
            set
            {
                if (_showMaximizeButton == value) return;

                _showMaximizeButton = value;

                if (!value && WindowState == FormWindowState.Maximized) WindowState = FormWindowState.Normal;

                LayoutChrome();
                Invalidate(new Rectangle(0, 0, ClientSize.Width, TitleBarHeight));
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(6)]
        public int ResizeBorderWidth
        {
            get => _resizeBorderWidth;
            set => _resizeBorderWidth = Math.Max(1, value);
        }

        [Category("Action")]
        public event EventHandler InfoButtonClick;

        protected virtual void OnInfoButtonClick(EventArgs e)
        {
            InfoButtonClick?.Invoke(this, e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            int border = WindowBorderThickness;
            Color borderColor = _windowActive ? WindowBorderColor : InactiveWindowBorderColor;

            using (SolidBrush borderBrush = new SolidBrush(borderColor)) e.Graphics.FillRectangle(borderBrush, ClientRectangle);

            Rectangle bodyRect = new Rectangle(border, TitleBarHeight, Math.Max(0, ClientSize.Width - (border * 2)), Math.Max(0, ClientSize.Height - TitleBarHeight - border));
            if (bodyRect.Width > 0 && bodyRect.Height > 0)
            {
                using SolidBrush bodyBrush = new SolidBrush(ContentBackColor);
                e.Graphics.FillRectangle(bodyBrush, bodyRect);
            }

            Rectangle titleRect = new Rectangle(border, border, Math.Max(0, ClientSize.Width - (border * 2)), Math.Max(0, TitleBarHeight - border));
            if (titleRect.Width > 0 && titleRect.Height > 0)
            {
                using SolidBrush titleBrush = new SolidBrush(TitleBarColor);
                e.Graphics.FillRectangle(titleBrush, titleRect);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            int border = WindowBorderThickness;
            int iconSize = 16;
            int iconX = border + 16;
            int iconY = border + ((TitleBarHeight - border - iconSize) / 2);

            if (ShowTitleBarIcon && ShowIcon && (_titleBarImage != null || Icon != null))
            {
                Rectangle iconRect = new Rectangle(iconX, iconY, iconSize, iconSize);
                if (_titleBarImage != null)
                    e.Graphics.DrawImage(_titleBarImage, iconRect);
                else
                    e.Graphics.DrawIcon(Icon!, iconRect);
                iconX += iconSize + 8;
            }

            int titleButtonsLeft = GetTitleButtonsLeft();
            Rectangle titleRect = new Rectangle(iconX, border, Math.Max(0, titleButtonsLeft - iconX - 8), Math.Max(0, TitleBarHeight - border));
            TextRenderer.DrawText(e.Graphics, Text, Font, titleRect, TitleForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            if (WindowBorderThickness <= 0 || ClientSize.Width <= 1 || ClientSize.Height <= 1) return;

            float inset = WindowBorderThickness / 2F;
            RectangleF rect = new RectangleF(inset, inset, ClientSize.Width - WindowBorderThickness, ClientSize.Height - WindowBorderThickness);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            Color borderColor = _windowActive ? WindowBorderColor : InactiveWindowBorderColor;
            float radius = WindowState == FormWindowState.Maximized ? 0F : Math.Max(0F, BorderRadius - inset);

            using GraphicsPath path = CreateRoundPath(rect, radius);
            using Pen pen = new Pen(borderColor, WindowBorderThickness);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate(new Rectangle(0, 0, ClientSize.Width, TitleBarHeight));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChrome();
            UpdateFormRegion();
            Invalidate();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateFormRegion();

            if (Modal)
            {
                _modalMouseDown = Control.MouseButtons != MouseButtons.None;
                _modalClickTimer.Start();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _modalClickTimer.Stop();
            _shakeTimer.Stop();

            base.OnFormClosed(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            _windowActive = true;
            Invalidate();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            _windowActive = false;
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
            {
                base.WndProc(ref m);

                Point point = PointToClient(new Point((short)((long)m.LParam & 0xFFFF), (short)(((long)m.LParam >> 16) & 0xFFFF)));
                int hit = GetResizeHitTest(point);

                if (hit != HTCLIENT)
                {
                    m.Result = (IntPtr)hit;
                    return;
                }

                if (point.Y >= 0 && point.Y < TitleBarHeight && !IsPointOnTitleButton(point))
                {
                    m.Result = (IntPtr)HTCAPTION;
                    return;
                }

                return;
            }

            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Maximized)
            {
                base.WndProc(ref m);

                Point point = PointToClient(new Point((short)((long)m.LParam & 0xFFFF), (short)(((long)m.LParam >> 16) & 0xFFFF)));
                if (point.Y >= 0 && point.Y < TitleBarHeight && !IsPointOnTitleButton(point)) m.Result = (IntPtr)HTCAPTION;

                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            Point point = PointToClient(MousePosition);

            if (ShowMaximizeButton && point.Y >= 0 && point.Y < TitleBarHeight && !IsPointOnTitleButton(point))
            {
                ToggleMaximize();
                return;
            }

            base.OnDoubleClick(e);
        }

        private void ModalClickTimer_Tick(object? sender, EventArgs e)
        {
            bool mouseDown = Control.MouseButtons != MouseButtons.None;

            if (mouseDown && !_modalMouseDown)
            {
                Point point = Cursor.Position;

                if (!Bounds.Contains(point)) StartShake();
            }

            _modalMouseDown = mouseDown;
        }

        private void StartShake()
        {
            if (_shakeTimer.Enabled || WindowState != FormWindowState.Normal) return;

            _shakeOrigin = Location;
            _shakeIndex = 0;
            _shakeTimer.Start();
        }

        private void ShakeTimer_Tick(object? sender, EventArgs e)
        {
            if (_shakeIndex >= SHAKE_OFFSETS.Length)
            {
                Location = _shakeOrigin;
                _shakeTimer.Stop();
                return;
            }

            Location = new Point(_shakeOrigin.X + SHAKE_OFFSETS[_shakeIndex], _shakeOrigin.Y);
            _shakeIndex++;
        }

        private void BtnMinimize_Paint(object sender, PaintEventArgs e)
        {
            Button button = (Button)sender;

            using (Pen pen = new Pen(button.ForeColor, 1f))
            {
                int cx = button.ClientSize.Width / 2;
                int cy = button.ClientSize.Height / 2;

                e.Graphics.DrawLine(pen, cx - 5, cy + 3, cx + 5, cy + 3);
            }
        }

        private void BtnMaximize_Paint(object sender, PaintEventArgs e)
        {
            Button button = (Button)sender;

            using (Pen pen = new Pen(button.ForeColor, 1f))
            {
                int cx = button.ClientSize.Width / 2;
                int cy = button.ClientSize.Height / 2;

                Rectangle rect = new Rectangle(cx - 4, cy - 4, 8, 8);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void BtnClose_Paint(object sender, PaintEventArgs e)
        {
            Button button = (Button)sender;

            using (Pen pen = new Pen(button.ForeColor, 1f))
            {
                int cx = button.ClientSize.Width / 2;
                int cy = button.ClientSize.Height / 2;

                e.Graphics.DrawLine(pen, cx - 4, cy - 4, cx + 4, cy + 4);
                e.Graphics.DrawLine(pen, cx + 4, cy - 4, cx - 4, cy + 4);
            }
        }

        private Button CreateTitleButton()
        {
            Button button = new Button
            {
                Text = "",
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                UseVisualStyleBackColor = false,
                BackColor = _titleBarColor,
                ForeColor = _titleButtonForeColor,
                Cursor = Cursors.Default
            };

            button.FlatAppearance.BorderSize = 0;
            button.MouseEnter += TitleButtonMouseEnter;
            button.MouseLeave += TitleButtonMouseLeave;

            return button;
        }

        private void TitleButtonMouseEnter(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, _btnClose)) return;
            ((Button)sender).BackColor = _titleButtonHoverColor;
        }

        private void TitleButtonMouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackColor = _titleBarColor;
        }

        private void ApplyTitleButtonColors()
        {
            if (_btnMinimize == null) return;

            _btnMinimize.BackColor = _titleBarColor;
            _btnMaximize.BackColor = _titleBarColor;
            _btnClose.BackColor = _titleBarColor;

            _btnMinimize.ForeColor = _titleButtonForeColor;
            _btnMaximize.ForeColor = _titleButtonForeColor;
            _btnClose.ForeColor = _titleButtonForeColor;
        }

        private void UpdateFormRegion()
        {
            if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            Region oldRegion = Region;

            if (WindowState == FormWindowState.Maximized || BorderRadius <= 0)
            {
                Region = null;
                oldRegion?.Dispose();
                return;
            }

            RectangleF rect = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);
            using GraphicsPath path = CreateRoundPath(rect, BorderRadius);
            Region = new Region(path);
            oldRegion?.Dispose();
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

        private void LayoutChrome()
        {
            if (_btnClose == null) return;

            const int buttonWidth = 32;
            int border = WindowBorderThickness;
            int buttonHeight = Math.Max(1, TitleBarHeight - border);
            int right = ClientSize.Width - border;

            _btnClose.Visible = true;
            _btnMaximize.Visible = ShowMaximizeButton;
            _btnMinimize.Visible = ShowMinimizeButton;

            right -= buttonWidth;
            _btnClose.SetBounds(right, border, buttonWidth, buttonHeight);

            if (ShowMaximizeButton)
            {
                right -= buttonWidth;
                _btnMaximize.SetBounds(right, border, buttonWidth, buttonHeight);
            }

            if (ShowMinimizeButton)
            {
                right -= buttonWidth;
                _btnMinimize.SetBounds(right, border, buttonWidth, buttonHeight);
            }

            _btnClose.BringToFront();
            if (ShowMaximizeButton) _btnMaximize.BringToFront();
            if (ShowMinimizeButton) _btnMinimize.BringToFront();
        }

        private int GetTitleButtonsLeft()
        {
            int left = _btnClose.Left;

            if (ShowMaximizeButton) left = Math.Min(left, _btnMaximize.Left);
            if (ShowMinimizeButton) left = Math.Min(left, _btnMinimize.Left);

            return left;
        }

        private void ToggleMaximize()
        {
            if (!ShowMaximizeButton) return;

            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            _btnMaximize.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _modalClickTimer.Stop();
                _shakeTimer.Stop();

                _modalClickTimer.Tick -= ModalClickTimer_Tick;
                _shakeTimer.Tick -= ShakeTimer_Tick;

                _modalClickTimer.Dispose();
                _shakeTimer.Dispose();
                _titleBarImage?.Dispose();
                _titleBarImage = null;
            }

            base.Dispose(disposing);
        }

        private int GetResizeHitTest(Point point)
        {
            bool left = point.X >= 0 && point.X < ResizeBorderWidth;
            bool right = point.X < ClientSize.Width && point.X >= ClientSize.Width - ResizeBorderWidth;
            bool top = point.Y >= 0 && point.Y < ResizeBorderWidth;
            bool bottom = point.Y < ClientSize.Height && point.Y >= ClientSize.Height - ResizeBorderWidth;

            if (left && top) return HTTOPLEFT;
            if (right && top) return HTTOPRIGHT;
            if (left && bottom) return HTBOTTOMLEFT;
            if (right && bottom) return HTBOTTOMRIGHT;
            if (left) return HTLEFT;
            if (right) return HTRIGHT;
            if (top) return HTTOP;
            if (bottom) return HTBOTTOM;

            return HTCLIENT;
        }

        private bool IsPointOnTitleButton(Point point)
        {
            if (_btnClose.Visible && _btnClose.Bounds.Contains(point)) return true;
            if (_btnMaximize.Visible && _btnMaximize.Bounds.Contains(point)) return true;
            if (_btnMinimize.Visible && _btnMinimize.Bounds.Contains(point)) return true;

            return false;
        }
    }
}
