using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    public class AFMSLabel : AFMSPanel
    {
        private readonly Label _label;
        private Color _normalForeColor = Color.Black;
        private Color _disabledForeColor = Color.FromArgb(150, 155, 165);
        private float _characterSpacing = 0F;
        private float _fontSize = 9F;
        private bool _bold;

        public AFMSLabel()
        {
            Size = new Size(120, 34);
            MinimumSize = new Size(30, 24);
            Padding = new Padding(8, 3, 8, 3);

            BorderRadius = 6;
            BorderThickness = 0.5F;
            BorderColor = Color.FromArgb(205, 211, 220);
            BackColor = Color.White;
            ForeColor = Color.Black;

            _label = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = Color.Black,
                Font = new System.Drawing.Font("Segoe UI", _fontSize, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                UseCompatibleTextRendering = false
            };

            Controls.Add(_label);
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get => _label?.Text ?? base.Text;
            set
            {
                if (_label == null) base.Text = value;
                else _label.Text = value ?? string.Empty;
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(ContentAlignment.MiddleLeft)]
        public ContentAlignment TextAlign
        {
            get => _label.TextAlign;
            set => _label.TextAlign = value;
        }

        [Category("AFMS Appearance")]
        [DefaultValue(9F)]
        public float FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = Math.Max(1F, value);
                UpdateLabelFont();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(false)]
        public bool Bold
        {
            get => _bold;
            set
            {
                _bold = value;
                UpdateLabelFont();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color NormalForeColor
        {
            get => _normalForeColor;
            set
            {
                _normalForeColor = value;
                UpdateLabelForeColor();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color DisabledForeColor
        {
            get => _disabledForeColor;
            set
            {
                _disabledForeColor = value;
                UpdateLabelForeColor();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(0F)]
        public float CharacterSpacing
        {
            get => _characterSpacing;
            set
            {
                _characterSpacing = value;
                Invalidate();
            }
        }

        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;

                if (_label != null)
                {
                    _fontSize = value.Size;
                    _bold = value.Bold;
                    _label.Font = value;
                }

                Invalidate();
            }
        }

        public override Color ForeColor
        {
            get => base.ForeColor;
            set
            {
                base.ForeColor = value;
                _normalForeColor = value;

                if (_label != null) _label.ForeColor = Enabled ? value : DisabledForeColor;

                Invalidate();
            }
        }

        [Browsable(false)]
        public Label InnerLabel => _label;

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            UpdateLabelForeColor();
        }

        private void UpdateLabelFont()
        {
            FontStyle style = Bold ? FontStyle.Bold : FontStyle.Regular;
            System.Drawing.Font font = new System.Drawing.Font("Segoe UI", FontSize, style, GraphicsUnit.Point);

            base.Font = font;
            if (_label != null) _label.Font = font;

            Invalidate();
        }

        private void UpdateLabelForeColor()
        {
            Color color = Enabled ? NormalForeColor : DisabledForeColor;

            base.ForeColor = color;
            if (_label != null) _label.ForeColor = color;

            Invalidate();
        }

        private void DrawSpacedText(Graphics g, string text, Font font, Brush brush, Rectangle rect)
        {
            if (string.IsNullOrEmpty(text)) return;

            float totalWidth = 0F;
            float[] widths = new float[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                widths[i] = g.MeasureString(text[i].ToString(), font, PointF.Empty, StringFormat.GenericTypographic).Width;
                totalWidth += widths[i];

                if (i < text.Length - 1) totalWidth += CharacterSpacing;
            }

            float x = rect.Left;
            float y = rect.Top + (rect.Height - font.Height) / 2F;

            for (int i = 0; i < text.Length; i++)
            {
                g.DrawString(text[i].ToString(), font, brush, x, y, StringFormat.GenericTypographic);
                x += widths[i] + CharacterSpacing;
            }
        }
    }
}