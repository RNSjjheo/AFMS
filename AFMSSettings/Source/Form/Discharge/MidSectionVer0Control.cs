using AFMSDll;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public sealed class MidSectionVer0Control : UserControl
    {
        private readonly AFMSNumberBox _uiNumberCellMin;
        private readonly AFMSNumberBox _uiNumberCellMax;
        private readonly AFMSNumberBox _uiNumberConversionFactor;

        public MidSectionVer0Control()
        {
            BackColor = Color.White;
            Margin = Padding.Empty;
            Padding = Padding.Empty;

            TableLayoutPanel mainLayout = CreateMainLayout();
            Controls.Add(mainLayout);

            TableLayoutPanel settingsLayout = CreateSettingsLayout();
            AFMSSectionPanel rangeGroup = CreateHeaderGroupBox("입력범위");
            AFMSSectionPanel conversionGroup = CreateHeaderGroupBox("환산계수");

            _uiNumberCellMin = CreateRangeNumberBox();
            _uiNumberCellMax = CreateRangeNumberBox();
            _uiNumberConversionFactor = CreateConversionFactorNumberBox();

            rangeGroup.ContentLayout.Controls.Add(CreateInputRangeLayout());
            conversionGroup.ContentLayout.Controls.Add(CreateConversionFactorLayout());

            rangeGroup.Margin = new Padding(0, 5, 4, 5);
            conversionGroup.Margin = new Padding(4, 5, 0, 5);
            settingsLayout.Controls.Add(rangeGroup, 0, 0);
            settingsLayout.Controls.Add(conversionGroup, 1, 0);

            mainLayout.Controls.Add(settingsLayout, 0, 0);
            mainLayout.Controls.Add(CreateFormulaLabel(), 0, 1);
            mainLayout.Controls.Add(CreateGuideLabel(), 0, 2);

            _uiNumberCellMin.Minimum = 1;
            _uiNumberCellMax.Minimum = 1;
            _uiNumberCellMin.SetValue(1);
            _uiNumberConversionFactor.SetValue(0.85);
        }

        public DiscVerMidSection Version => DiscVerMidSection.Ver00;

        public void SetCellRangeMaximum(int maximum)
        {
            if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));

            _uiNumberCellMin.Maximum = maximum;
            _uiNumberCellMax.Maximum = maximum;
            _uiNumberCellMax.SetValue(maximum);
        }

        public bool TryCreateConfig(int hydroId, out FormDischargeMidSection.MidSectionConfig config)
        {
            config = new FormDischargeMidSection.MidSectionConfig();

            if (!_uiNumberCellMin.IntValue.HasValue || !_uiNumberCellMax.IntValue.HasValue)
            {
                MessageBox.Show("MIN과 MAX를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (_uiNumberCellMin.IntValue.Value > _uiNumberCellMax.IntValue.Value)
            {
                MessageBox.Show("MIN은 MAX보다 클 수 없습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (!_uiNumberConversionFactor.DoubleValue.HasValue)
            {
                MessageBox.Show("환산계수를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _uiNumberConversionFactor.Focus();
                return false;
            }

            config.HydroId = hydroId;
            config.DisVer = (int)Version;
            config.CellMin = _uiNumberCellMin.IntValue.Value;
            config.CellMax = _uiNumberCellMax.IntValue.Value;
            config.ConversionFactor = Math.Round(
                _uiNumberConversionFactor.DoubleValue.Value,
                2,
                MidpointRounding.AwayFromZero);
            return true;
        }

        private TableLayoutPanel CreateInputRangeLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnCount = 2;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            Label separator = CreateLabel("~");
            separator.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 11F, FontStyle.Regular);

            layout.Controls.Add(CreateLabel("MIN"), 0, 0);
            layout.Controls.Add(_uiNumberCellMin, 1, 0);
            layout.Controls.Add(separator, 1, 1);
            layout.Controls.Add(CreateLabel("MAX"), 0, 2);
            layout.Controls.Add(_uiNumberCellMax, 1, 2);
            return layout;
        }

        private TableLayoutPanel CreateConversionFactorLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label factorLabel = CreateLabel("k");
            factorLabel.Font = new Font("Cambria Math", 11F, FontStyle.Italic);

            layout.Controls.Add(factorLabel, 0, 0);
            layout.Controls.Add(_uiNumberConversionFactor, 1, 0);
            return layout;
        }

        private static TableLayoutPanel CreateMainLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return layout;
        }

        private static TableLayoutPanel CreateSettingsLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return layout;
        }

        private static AFMSMathLabel CreateFormulaLabel()
        {
            AFMSMathLabel label = new AFMSMathLabel();
            label.Dock = DockStyle.Fill;
            label.Margin = Padding.Empty;
            label.Padding = Padding.Empty;
            label.Font = new Font("Cambria Math", 16F, FontStyle.Italic);
            label.ForeColor = Color.Black;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.AddVariable("Q");
            label.AddText(" = ");
            label.AddVariable("A");
            label.Add("m", AFMSMathTextType.Subscript);
            label.AddText("(");
            label.AddVariable("h");
            label.AddText(") × ");
            label.AddVariable("V");
            label.Add("m", AFMSMathTextType.Subscript);
            return label;
        }

        private static Label CreateGuideLabel()
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.Margin = Padding.Empty;
            label.Padding = new Padding(8, 0, 0, 0);
            label.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Regular);
            label.ForeColor = DllColorHelper.HexToColor("#667085");
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Text = "ⓘ  입력 범위와 환산계수를 설정한 후 저장 버튼을 눌러주세요.";
            return label;
        }

        private static AFMSSectionPanel CreateHeaderGroupBox(string headerText)
        {
            AFMSSectionPanel item = new AFMSSectionPanel();
            item.SectionStyle = AFMSSectionStyle.OutlineTitle;
            item.HeaderText = headerText;
            item.HeaderColor = DllColorHelper.HexToColor("#02925D");
            item.HeaderHeight = 40;
            item.HeaderHorizontalPadding = 14;
            item.HeaderBarWidth = 3;
            item.HeaderBarHeight = 18;
            item.HeaderBarTextGap = 10;
            item.HeaderLineColor = DllColorHelper.GetCommonBorder();
            item.HeaderLineThickness = 1F;
            item.BorderRadius = 7;
            item.BorderThickness = 1F;
            item.BorderColor = DllColorHelper.GetCommonBorder();
            item.BackColor = Color.White;
            item.Dock = DockStyle.Fill;
            item.Padding = new Padding(14, 4, 14, 8);
            item.ContentPadding = new Padding(8, 4, 8, 6);
            return item;
        }

        private static AFMSNumberBox CreateRangeNumberBox()
        {
            AFMSNumberBox numberBox = new AFMSNumberBox();
            numberBox.Dock = DockStyle.Fill;
            numberBox.InputType = AFMSNumericInputType.Integer;
            numberBox.AllowNegative = false;
            numberBox.BorderColor = DllColorHelper.HexToColor("#CDD7D1");
            numberBox.FocusBorderColor = DllColorHelper.HexToColor("#02925D");
            numberBox.BorderRadius = 6;
            numberBox.TextAlign = HorizontalAlignment.Center;
            return numberBox;
        }

        private static AFMSNumberBox CreateConversionFactorNumberBox()
        {
            AFMSNumberBox numberBox = new AFMSNumberBox();
            numberBox.Dock = DockStyle.Fill;
            numberBox.Margin = new Padding(0, 22, 0, 22);
            numberBox.InputType = AFMSNumericInputType.Double;
            numberBox.AllowNegative = false;
            numberBox.Minimum = 0;
            numberBox.DecimalPlaces = 2;
            numberBox.BorderColor = DllColorHelper.HexToColor("#CDD7D1");
            numberBox.FocusBorderColor = DllColorHelper.HexToColor("#02925D");
            numberBox.BorderRadius = 6;
            numberBox.TextAlign = HorizontalAlignment.Center;
            return numberBox;
        }

        private static Label CreateLabel(string text)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.Margin = Padding.Empty;
            label.Padding = Padding.Empty;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Regular);
            label.ForeColor = Color.FromArgb(100, 108, 116);
            return label;
        }
    }
}
