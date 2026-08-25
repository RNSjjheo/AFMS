using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;

namespace AFMSDll
{
    public enum AFMSNumericInputType
    {
        Integer,
        Double
    }

    [ToolboxItem(true)]
    public class AFMSNumberBox : AFMSTextBox
    {
        private AFMSNumericInputType _inputType = AFMSNumericInputType.Double;
        private bool _allowNegative = true;
        private bool _internalTextChange;
        private int _decimalPlaces = -1;
        private double? _minimum;
        private double? _maximum;

        public AFMSNumberBox()
        {
            Size = new System.Drawing.Size(180, 34);
            TextAlign = HorizontalAlignment.Right;
            Multiline = false;
        }

        [Category("AFMS Behavior")]
        [DefaultValue(AFMSNumericInputType.Double)]
        public AFMSNumericInputType InputType
        {
            get => _inputType;
            set
            {
                _inputType = value;
                NormalizeText();
            }
        }

        [Category("AFMS Behavior")]
        [DefaultValue(true)]
        public bool AllowNegative
        {
            get => _allowNegative;
            set
            {
                _allowNegative = value;
                NormalizeText();
            }
        }

        [Category("AFMS Behavior")]
        [Description("Double 입력 시 허용하고 표시할 소수 자릿수입니다. -1이면 제한하지 않습니다.")]
        [DefaultValue(-1)]
        public int DecimalPlaces
        {
            get => _decimalPlaces;
            set
            {
                _decimalPlaces = Math.Max(-1, Math.Min(15, value));
                NormalizeText();
            }
        }

        [Category("AFMS Behavior")]
        [DefaultValue(null)]
        public double? Minimum
        {
            get => _minimum;
            set
            {
                if (value.HasValue && _maximum.HasValue && value.Value > _maximum.Value)
                    throw new ArgumentOutOfRangeException(nameof(Minimum), "Minimum은 Maximum보다 클 수 없습니다.");

                _minimum = value;
                NormalizeText();
            }
        }

        [Category("AFMS Behavior")]
        [DefaultValue(null)]
        public double? Maximum
        {
            get => _maximum;
            set
            {
                if (value.HasValue && _minimum.HasValue && value.Value < _minimum.Value)
                    throw new ArgumentOutOfRangeException(nameof(Maximum), "Maximum은 Minimum보다 작을 수 없습니다.");

                _maximum = value;
                NormalizeText();
            }
        }

        [Browsable(false)]
        public int? IntValue
        {
            get
            {
                if (int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) return value;
                return null;
            }
        }

        [Browsable(false)]
        public double? DoubleValue
        {
            get
            {
                if (double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) return value;
                return null;
            }
        }

        public void SetValue(int value)
        {
            SetNormalizedValue(value);
        }

        public void SetValue(double value)
        {
            SetNormalizedValue(value);
        }

        public bool TryGetInt(out int value)
        {
            return int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetDouble(out double value)
        {
            return double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        protected override void OnEditorKeyPress(KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;

            string candidate = Editor.Text.Remove(Editor.SelectionStart, Editor.SelectionLength)
                .Insert(Editor.SelectionStart, e.KeyChar.ToString());
            e.Handled = FilterText(candidate) != candidate;
        }

        protected override void OnEditorTextChanged(EventArgs e)
        {
            if (_internalTextChange) return;

            string filtered = FilterText(Editor.Text);

            if (filtered != Editor.Text)
            {
                int selectionStart = Math.Min(Editor.SelectionStart, filtered.Length);

                _internalTextChange = true;
                Editor.Text = filtered;
                Editor.SelectionStart = selectionStart;
                _internalTextChange = false;
            }

            base.OnEditorTextChanged(e);
        }

        protected override void OnEditorLostFocus(EventArgs e)
        {
            NormalizeText();
            base.OnEditorLostFocus(e);
        }

        private string FilterText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            char[] buffer = new char[text.Length];
            int length = 0;
            bool decimalAdded = false;
            int decimalDigitCount = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];

                if (char.IsDigit(ch))
                {
                    if (decimalAdded && DecimalPlaces >= 0 && decimalDigitCount >= DecimalPlaces) continue;
                    buffer[length++] = ch;
                    if (decimalAdded) decimalDigitCount++;
                    continue;
                }

                if (ch == '-' && AllowNegative && length == 0)
                {
                    buffer[length++] = ch;
                    continue;
                }

                if (ch == '.' && InputType == AFMSNumericInputType.Double && DecimalPlaces != 0 && !decimalAdded)
                {
                    buffer[length++] = ch;
                    decimalAdded = true;
                }
            }

            return new string(buffer, 0, length);
        }

        private void NormalizeText()
        {
            if (string.IsNullOrWhiteSpace(Text) || Text == "-" || Text == "." || Text == "-.")
            {
                Text = string.Empty;
                return;
            }

            if (InputType == AFMSNumericInputType.Integer)
            {
                if (TryGetInt(out int intValue)) SetNormalizedValue(intValue);
                else Text = string.Empty;

                return;
            }

            if (TryGetDouble(out double doubleValue))
            {
                if (!AllowNegative && doubleValue < 0) Text = string.Empty;
                else SetNormalizedValue(doubleValue);
            }
            else
            {
                Text = string.Empty;
            }
        }

        private void SetNormalizedValue(double value)
        {
            if (!AllowNegative && value < 0) value = 0;
            if (Minimum.HasValue && value < Minimum.Value) value = Minimum.Value;
            if (Maximum.HasValue && value > Maximum.Value) value = Maximum.Value;

            _internalTextChange = true;

            if (InputType == AFMSNumericInputType.Integer)
                Text = Convert.ToInt32(Math.Truncate(value)).ToString(CultureInfo.InvariantCulture);
            else if (DecimalPlaces >= 0)
                Text = value.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture);
            else
                Text = value.ToString(CultureInfo.InvariantCulture);

            _internalTextChange = false;
        }
    }
}
