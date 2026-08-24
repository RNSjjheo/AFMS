using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    [DefaultEvent(nameof(TextChanged))]
    public class AFMSNavigatorBox : UserControl
    {
        private readonly TextBox _textBox;

        private bool _leftHover;
        private bool _rightHover;
        private bool _leftPressed;
        private bool _rightPressed;

        private int _buttonWidth = 24;
        private int _borderRadius = 5;
        private float _borderThickness = 1F;

        private Color _borderColor = Color.FromArgb(225, 232, 240);
        private Color _buttonBackColor = Color.FromArgb(248, 252, 251);
        private Color _buttonHoverBackColor = Color.FromArgb(238, 249, 246);
        private Color _buttonPressedBackColor = Color.FromArgb(226, 244, 239);
        private Color _textBoxBackColor = Color.White;
        private Color _buttonForeColor = Color.FromArgb(0, 157, 111);

        public AFMSNavigatorBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            Size = new Size(280, 28);
            MinimumSize = new Size(120, 24);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center,
                BackColor = _textBoxBackColor,
                ForeColor = ForeColor
            };

            _textBox.TextChanged += (_, _) =>
            {
                base.OnTextChanged(EventArgs.Empty);
            };

            _textBox.GotFocus += (_, _) => Invalidate();
            _textBox.LostFocus += (_, _) => Invalidate();

            Controls.Add(_textBox);

            UpdateTextBoxLayout();
        }

        [Category("Action")]
        public event EventHandler? LeftButtonClick;

        [Category("Action")]
        public event EventHandler? RightButtonClick;

        [Category("AFMS Appearance")]
        [DefaultValue(28)]
        public int ButtonWidth
        {
            get => _buttonWidth;
            set
            {
                _buttonWidth = Math.Max(20, value);
                UpdateTextBoxLayout();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(5)]
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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ButtonBackColor
        {
            get => _buttonBackColor;
            set
            {
                _buttonBackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ButtonHoverBackColor
        {
            get => _buttonHoverBackColor;
            set
            {
                _buttonHoverBackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ButtonPressedBackColor
        {
            get => _buttonPressedBackColor;
            set
            {
                _buttonPressedBackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TextBoxBackColor
        {
            get => _textBoxBackColor;
            set
            {
                _textBoxBackColor = value;
                _textBox.BackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ButtonForeColor
        {
            get => _buttonForeColor;
            set
            {
                _buttonForeColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => _textBox.ReadOnly;
            set => _textBox.ReadOnly = value;
        }

        [Category("AFMS Behavior")]
        [DefaultValue(typeof(HorizontalAlignment), "Center")]
        public HorizontalAlignment TextAlign
        {
            get => _textBox.TextAlign;
            set => _textBox.TextAlign = value;
        }

        [Browsable(true)]
        [Bindable(true)]
        public override string Text
        {
            get => _textBox.Text;
            set
            {
                _textBox.Text = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        public TextBox InnerTextBox => _textBox;

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            if (_textBox == null)
                return;

            _textBox.Font = Font;
            UpdateTextBoxLayout();
            Invalidate();
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);

            if (_textBox == null)
                return;

            _textBox.ForeColor = ForeColor;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateTextBoxLayout();
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _textBox.Enabled = Enabled;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle leftRect = GetLeftButtonRectangle();
            Rectangle rightRect = GetRightButtonRectangle();

            float borderOffset = BorderThickness <= 1F ? 0.5F : BorderThickness / 2F;
            RectangleF outerRect = new RectangleF(borderOffset, borderOffset, Width - (borderOffset * 2F), Height - (borderOffset * 2F));

            using GraphicsPath outerPath = CreateRoundRectPath(outerRect, BorderRadius);
            using SolidBrush middleBrush = new SolidBrush(TextBoxBackColor);

            g.FillPath(middleBrush, outerPath);

            DrawButtonBackground(g, leftRect, true, _leftHover, _leftPressed);
            DrawButtonBackground(g, rightRect, false, _rightHover, _rightPressed);

            using Pen borderPen = new Pen(BorderColor, BorderThickness) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round };

            g.DrawPath(borderPen, outerPath);

            // 좌우 버튼과 TextBox 사이 구분선
            g.DrawLine(borderPen, leftRect.Right, 1, leftRect.Right, Height - 2);
            g.DrawLine(borderPen, rightRect.Left, 1, rightRect.Left, Height - 2);

            // 방향 아이콘
            DrawChevron(g, leftRect, false);
            DrawChevron(g, rightRect, true);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool newLeftHover = GetLeftButtonRectangle().Contains(e.Location);
            bool newRightHover = GetRightButtonRectangle().Contains(e.Location);

            if (_leftHover == newLeftHover && _rightHover == newRightHover)
                return;

            _leftHover = newLeftHover;
            _rightHover = newRightHover;

            Cursor = _leftHover || _rightHover ? Cursors.Hand : Cursors.IBeam;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            _leftHover = false;
            _rightHover = false;
            _leftPressed = false;
            _rightPressed = false;
            Cursor = Cursors.Default;

            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            if (GetLeftButtonRectangle().Contains(e.Location))
            {
                _leftPressed = true;
                Invalidate();
                return;
            }

            if (GetRightButtonRectangle().Contains(e.Location))
            {
                _rightPressed = true;
                Invalidate();
                return;
            }

            _textBox.Focus();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button != MouseButtons.Left)
                return;

            bool invokeLeft = _leftPressed && GetLeftButtonRectangle().Contains(e.Location);
            bool invokeRight = _rightPressed && GetRightButtonRectangle().Contains(e.Location);

            _leftPressed = false;
            _rightPressed = false;
            Invalidate();

            if (invokeLeft)
            {
                LeftButtonClick?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (invokeRight)
            {
                RightButtonClick?.Invoke(this, EventArgs.Empty);
            }
        }

        private void DrawChevron(Graphics g, Rectangle rect, bool right)
        {
            float centerX = rect.Left + rect.Width / 2F;
            float centerY = rect.Top + rect.Height / 2F;

            float width = 4F;
            float height = 7F;

            using Pen pen = new Pen(Enabled ? ButtonForeColor : SystemColors.GrayText,1.6F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            if (right)
            {
                g.DrawLine(pen, centerX - width / 2F, centerY - height / 2F, centerX + width / 2F, centerY);

                g.DrawLine(pen, centerX + width / 2F, centerY, centerX - width / 2F, centerY + height / 2F);
            }
            else
            {
                g.DrawLine(pen, centerX + width / 2F, centerY - height / 2F, centerX - width / 2F, centerY);

                g.DrawLine(pen, centerX - width / 2F, centerY, centerX + width / 2F, centerY + height / 2F);
            }
        }

        private void UpdateTextBoxLayout()
        {
            if (_textBox == null) return;

            int left = ButtonWidth + 8;
            int right = ButtonWidth + 8;
            int width = Math.Max(10, Width - left - right);
            int height = _textBox.PreferredHeight;
            int top = Math.Max(1, (Height - height) / 2);

            _textBox.Location = new Point(left, top);
            _textBox.Size = new Size(width, height);
            _textBox.Font = Font;
        }

        private Rectangle GetLeftButtonRectangle()
        {
            return new Rectangle(0, 0, ButtonWidth, Height - 1);
        }

        private Rectangle GetRightButtonRectangle()
        {
            return new Rectangle(Width - ButtonWidth, 0, ButtonWidth, Height - 1);
        }

        private void DrawButtonBackground(Graphics g, Rectangle rect, bool isLeft, bool hover, bool pressed)
        {
            Color fillColor = !Enabled
                ? ButtonBackColor
                : pressed
                    ? ButtonPressedBackColor
                    : hover
                        ? ButtonHoverBackColor
                        : ButtonBackColor;

            using SolidBrush brush = new SolidBrush(fillColor);
            using GraphicsPath path = isLeft
                ? CreateLeftSegmentPath(rect, BorderRadius)
                : CreateRightSegmentPath(rect, BorderRadius);

            g.FillPath(brush, path);
        }

        private static GraphicsPath CreateRoundRectPath(RectangleF rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float d = radius * 2F;

            path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }

        private static GraphicsPath CreateLeftSegmentPath(Rectangle rect, int radius)
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

            path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
            path.AddLine(rect.Left + r, rect.Top, rect.Right, rect.Top);
            path.AddLine(rect.Right, rect.Top, rect.Right, rect.Bottom);
            path.AddLine(rect.Right, rect.Bottom, rect.Left + r, rect.Bottom);
            path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }

        private static GraphicsPath CreateRightSegmentPath(Rectangle rect, int radius)
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

            path.AddLine(rect.Left, rect.Top, rect.Right - r, rect.Top);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddLine(rect.Right - r, rect.Bottom, rect.Left, rect.Bottom);
            path.CloseFigure();

            return path;
        }
    }
}