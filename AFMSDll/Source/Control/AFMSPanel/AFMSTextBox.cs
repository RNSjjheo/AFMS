using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    public class AFMSTextBox : AFMSPanel
    {
        private readonly TextBox _textBox;
        private Color _focusBorderColor = Color.FromArgb(86, 135, 255);
        private int _horizontalPadding = 10;
        private int _verticalPadding = 6;
        private bool _focused;

        public AFMSTextBox()
        {
            Padding = Padding.Empty;
            Size = new Size(220, 34);
            MinimumSize = new Size(40, 28);
            BorderRadius = 6;
            BorderThickness = 1;
            BorderColor = Color.FromArgb(205, 211, 220);
            BackColor = Color.White;
            Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            ForeColor = Color.FromArgb(45, 48, 54);

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = BackColor,
                ForeColor = ForeColor,
                Font = Font,
                Multiline = false
            };

            _textBox.GotFocus += TextBox_GotFocus;
            _textBox.LostFocus += TextBox_LostFocus;
            _textBox.KeyPress += TextBox_KeyPress;
            _textBox.TextChanged += TextBox_TextChanged;

            Controls.Add(_textBox);
            UpdateTextBoxBounds();
        }

        [Browsable(false)]
        protected TextBox Editor => _textBox;

        [Browsable(false)]
        public TextBox InnerTextBox => _textBox;

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color FocusBorderColor
        {
            get => _focusBorderColor;
            set
            {
                _focusBorderColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color FillColor
        {
            get => BackColor;
            set
            {
                BackColor = value;
                _textBox.BackColor = value;
                Invalidate();
            }
        }


        [Category("AFMS Appearance")]
        [DefaultValue(10)]
        public int HorizontalPaddingSize
        {
            get => _horizontalPadding;
            set
            {
                _horizontalPadding = Math.Max(2, value);
                UpdateTextBoxBounds();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(6)]
        public int VerticalPaddingSize
        {
            get => _verticalPadding;
            set
            {
                _verticalPadding = Math.Max(2, value);
                UpdateTextBoxBounds();
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
        [DefaultValue(false)]
        public bool Multiline
        {
            get => _textBox.Multiline;
            set
            {
                _textBox.Multiline = value;
                UpdateTextBoxBounds();
                Invalidate();
            }
        }

        [Category("AFMS Behavior")]
        [Description("값을 입력하기 전에 표시할 안내 문구입니다.")]
        [DefaultValue("")]
        public string Hint
        {
            get => _textBox.PlaceholderText;
            set => _textBox.PlaceholderText = value ?? string.Empty;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PlaceholderText
        {
            get => Hint;
            set => Hint = value;
        }

        [Category("AFMS Behavior")]
        [DefaultValue(HorizontalAlignment.Left)]
        public HorizontalAlignment TextAlign
        {
            get => _textBox.TextAlign;
            set => _textBox.TextAlign = value;
        }

        [Category("AFMS Behavior")]
        [DefaultValue(false)]
        public bool UseSystemPasswordChar
        {
            get => _textBox.UseSystemPasswordChar;
            set => _textBox.UseSystemPasswordChar = value;
        }

        [Category("AFMS Behavior")]
        [DefaultValue(32767)]
        public int MaxLength
        {
            get => _textBox.MaxLength;
            set => _textBox.MaxLength = Math.Max(0, value);
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get => _textBox?.Text ?? base.Text;
            set
            {
                if (_textBox == null) base.Text = value;
                else _textBox.Text = value ?? string.Empty;
            }
        }

        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                if (_textBox != null) _textBox.Font = value;
                UpdateTextBoxBounds();
                Invalidate();
            }
        }

        public override Color ForeColor
        {
            get => base.ForeColor;
            set
            {
                base.ForeColor = value;
                if (_textBox != null) _textBox.ForeColor = value;
                Invalidate();
            }
        }

        protected override Color GetDrawBorderColor()
        {
            return _focused ? FocusBorderColor : BorderColor;
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            _textBox.Focus();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateTextBoxBounds();
            Invalidate();
        }

        protected virtual void OnEditorKeyPress(KeyPressEventArgs e)
        {
        }

        protected virtual void OnEditorTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
        }

        protected virtual void OnEditorLostFocus(EventArgs e)
        {
        }

        protected void UpdateTextBoxBounds()
        {
            if (_textBox == null) return;

            int left = Math.Max(_horizontalPadding, BorderRadius + 3);
            int right = left;
            int width = Math.Max(1, Width - left - right);

            if (_textBox.Multiline)
            {
                int top = _verticalPadding;
                int height = Math.Max(1, Height - (_verticalPadding * 2));
                _textBox.SetBounds(left, top, width, height);
                return;
            }

            int preferredHeight = _textBox.PreferredHeight;
            int y = Math.Max(0, (Height - preferredHeight) / 2);
            _textBox.SetBounds(left, y, width, preferredHeight);
        }

        private void TextBox_GotFocus(object? sender, EventArgs e)
        {
            _focused = true;
            Invalidate();
        }

        private void TextBox_LostFocus(object? sender, EventArgs e)
        {
            _focused = false;
            OnEditorLostFocus(e);
            Invalidate();
        }

        private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            OnEditorKeyPress(e);
        }

        private void TextBox_TextChanged(object? sender, EventArgs e)
        {
            OnEditorTextChanged(e);
        }
    }
}