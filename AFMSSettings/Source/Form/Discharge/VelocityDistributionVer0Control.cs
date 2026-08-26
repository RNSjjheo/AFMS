using AFMSDll;
using AFMSSettings.Source.Form.Discharge;
using System.Drawing;

namespace AFMSSettings
{
    public sealed class VelocityDistributionVer0Control : UserControl
    {
        private sealed class FitModeOption
        {
            public FitModeOption(VelocityDistributionFitMode value, string text)
            {
                Value = value;
                Text = text;
            }

            public VelocityDistributionFitMode Value { get; }
            public string Text { get; }
            public override string ToString() => Text;
        }

        private readonly AFMSNumberBox _phi = CreateNumberBox(3);
        private readonly AFMSNumberBox _horizontalGrid = CreateNumberBox(2);
        private readonly AFMSNumberBox _verticalGrid = CreateNumberBox(2);
        private readonly AFMSNumberBox _maxVelocityDepthRatio = CreateNumberBox(2);
        private readonly AFMSNumberBox _minimumPositiveCount = CreateIntegerBox();
        private readonly AFMSNumberBox _flowCenterX = CreateNumberBox(2);
        private readonly AFMSNumberBox _betaLeft = CreateNumberBox(2);
        private readonly AFMSNumberBox _betaRight = CreateNumberBox(2);
        private readonly AFMSComboBox _fitMode = new();
        private readonly TableLayoutPanel _transectLayout = new();
        private readonly List<(int No, AFMSCheckBox CheckBox)> _transectChecks = new();

        public VelocityDistributionVer0Control()
        {
            BackColor = Color.White;
            Margin = Padding.Empty;
            Padding = Padding.Empty;

            TableLayoutPanel main = new();
            main.Dock = DockStyle.Fill;
            main.Margin = Padding.Empty;
            main.ColumnCount = 2;
            main.RowCount = 1;
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AFMSSectionPanel settings = CreateSectionPanel();
            AFMSSectionPanel transects = CreateSectionPanel();
            settings.Margin = new Padding(0, 5, 5, 5);
            transects.Margin = new Padding(5, 5, 0, 5);

            settings.ContentLayout.Controls.Add(CreateSettingsLayout());
            transects.ContentLayout.Controls.Add(CreateTransectHost());
            main.Controls.Add(settings, 0, 0);
            main.Controls.Add(transects, 1, 0);
            Controls.Add(main);

            _fitMode.AddRange(
                new FitModeOption(VelocityDistributionFitMode.AutoAsymmetric, "흐름 중심·좌우 β 자동"),
                new FitModeOption(VelocityDistributionFitMode.AutoCommonBeta, "최심점·공통 β 자동"),
                new FitModeOption(VelocityDistributionFitMode.Manual, "수동 설정"));
            _fitMode.SelectedIndex = 0;
            _fitMode.SelectedIndexChanged += FitMode_SelectedIndexChanged;

            _phi.Minimum = 0.501;
            _phi.Maximum = 0.999;
            _horizontalGrid.Minimum = 0.01;
            _verticalGrid.Minimum = 0.01;
            _maxVelocityDepthRatio.Minimum = 0.0;
            _maxVelocityDepthRatio.Maximum = 0.999;
            _minimumPositiveCount.Minimum = 1;
            _flowCenterX.Minimum = 0.0;
            _betaLeft.Minimum = 0.01;
            _betaRight.Minimum = 0.01;

            _phi.SetValue(0.667);
            _horizontalGrid.SetValue(1.0);
            _verticalGrid.SetValue(0.25);
            _maxVelocityDepthRatio.SetValue(0.0);
            _minimumPositiveCount.SetValue(2);
            _betaLeft.SetValue(1.0);
            _betaRight.SetValue(1.0);
            UpdateManualInputState();
        }

        public DiscVerVelocityDistribution Version => DiscVerVelocityDistribution.Ver00;

        public void SetTransects(IEnumerable<Transect> transects)
        {
            _transectChecks.Clear();
            _transectLayout.Controls.Clear();
            _transectLayout.RowStyles.Clear();

            int row = 0;
            foreach (Transect transect in transects.OrderBy(item => item.No))
            {
                AFMSCheckBox checkBox = new();
                checkBox.Dock = DockStyle.Fill;
                checkBox.Margin = Padding.Empty;
                checkBox.Text = $"측선 {transect.No}";
                checkBox.Checked = true;

                Label distance = new();
                distance.Dock = DockStyle.Fill;
                distance.Margin = Padding.Empty;
                distance.Text = $"{transect.CenterLeftBankDistance:0.##} m";
                distance.TextAlign = ContentAlignment.MiddleRight;
                distance.ForeColor = DllColorHelper.HexToColor("#69737D");

                _transectLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
                _transectLayout.Controls.Add(checkBox, 0, row);
                _transectLayout.Controls.Add(distance, 1, row);
                _transectChecks.Add((transect.No, checkBox));
                row++;
            }
        }

        public bool TryCreateConfig(int hydroId, out FormDischargeVelocityDistribution.VelocityDistributionConfig config)
        {
            config = new FormDischargeVelocityDistribution.VelocityDistributionConfig();

            if (!_phi.DoubleValue.HasValue || !_horizontalGrid.DoubleValue.HasValue ||
                !_verticalGrid.DoubleValue.HasValue || !_maxVelocityDepthRatio.DoubleValue.HasValue ||
                !_minimumPositiveCount.IntValue.HasValue)
            {
                MessageBox.Show("유속분포 설정값을 모두 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            List<int> selectedNos = _transectChecks.Where(item => item.CheckBox.Checked).Select(item => item.No).ToList();
            if (selectedNos.Count < _minimumPositiveCount.IntValue.Value)
            {
                MessageBox.Show("선택한 운영 측선 수가 최소 양의 유속 측선 수보다 적습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            VelocityDistributionFitMode fitMode = GetFitMode();
            if (fitMode == VelocityDistributionFitMode.Manual &&
                (!_flowCenterX.DoubleValue.HasValue || !_betaLeft.DoubleValue.HasValue || !_betaRight.DoubleValue.HasValue))
            {
                MessageBox.Show("수동 적합값을 모두 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            config.HydroId = hydroId;
            config.DisVer = (int)Version;
            config.Phi = _phi.DoubleValue.Value;
            config.HorizontalGridM = _horizontalGrid.DoubleValue.Value;
            config.VerticalGridM = _verticalGrid.DoubleValue.Value;
            config.MaxVelocityDepthRatio = _maxVelocityDepthRatio.DoubleValue.Value;
            config.FitMode = fitMode;
            config.MinimumPositiveMeasurements = _minimumPositiveCount.IntValue.Value;
            config.TransectNos.AddRange(selectedNos);

            if (fitMode == VelocityDistributionFitMode.Manual)
            {
                config.FlowCenterX = _flowCenterX.DoubleValue;
                config.BetaLeft = _betaLeft.DoubleValue;
                config.BetaRight = _betaRight.DoubleValue;
            }

            return true;
        }

        private Control CreateSettingsLayout()
        {
            TableLayoutPanel layout = new();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.ColumnCount = 2;
            layout.RowCount = 6;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            for (int i = 1; i < 6; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 4 ? 32F : 68F));

            AFMSMathLabel example = QVelocityDistribution.GetExample();
            example.Dock = DockStyle.Fill;
            example.TextAlign = ContentAlignment.MiddleCenter;
            example.Font = new Font("Cambria Math", 15F, FontStyle.Regular);
            layout.Controls.Add(example, 0, 0);
            layout.SetColumnSpan(example, 2);

            layout.Controls.Add(CreateField("엔트로피 계수 φ", _phi), 0, 1);
            layout.Controls.Add(CreateField("적합 방식", _fitMode), 1, 1);
            layout.Controls.Add(CreateField("횡방향 격자 간격 (m)", _horizontalGrid), 0, 2);
            layout.Controls.Add(CreateField("수직방향 격자 간격 (m)", _verticalGrid), 1, 2);
            layout.Controls.Add(CreateField("최대유속 발생 수심비", _maxVelocityDepthRatio), 0, 3);
            layout.Controls.Add(CreateField("최소 양의 유속 측선 수", _minimumPositiveCount), 1, 3);

            Label manualTitle = new();
            manualTitle.Dock = DockStyle.Fill;
            manualTitle.Text = "수동 적합값";
            manualTitle.TextAlign = ContentAlignment.MiddleLeft;
            manualTitle.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 10F, FontStyle.Regular);
            manualTitle.ForeColor = DllColorHelper.HexToColor("#02925D");
            layout.Controls.Add(manualTitle, 0, 4);
            layout.SetColumnSpan(manualTitle, 2);

            layout.Controls.Add(CreateField("흐름 중심 위치 (m)", _flowCenterX), 0, 5);
            TableLayoutPanel betaLayout = new();
            betaLayout.Dock = DockStyle.Fill;
            betaLayout.Margin = Padding.Empty;
            betaLayout.ColumnCount = 2;
            betaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            betaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            betaLayout.Controls.Add(CreateField("좌측 β", _betaLeft), 0, 0);
            betaLayout.Controls.Add(CreateField("우측 β", _betaRight), 1, 0);
            layout.Controls.Add(betaLayout, 1, 5);
            return layout;
        }

        private Control CreateTransectHost()
        {
            Panel host = new();
            host.Dock = DockStyle.Fill;
            host.AutoScroll = true;

            _transectLayout.Dock = DockStyle.Top;
            _transectLayout.AutoSize = true;
            _transectLayout.ColumnCount = 2;
            _transectLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            _transectLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            host.Controls.Add(_transectLayout);
            return host;
        }

        private static AFMSSectionPanel CreateSectionPanel()
        {
            AFMSSectionPanel panel = new();
            panel.Dock = DockStyle.Fill;
            panel.SectionStyle = AFMSSectionStyle.OutlineTitle;
            panel.HeaderText = string.Empty;
            panel.HeaderHeight = 8;
            panel.BorderColor = DllColorHelper.GetCommonBorder();
            panel.BorderThickness = 1F;
            panel.BackColor = Color.White;
            panel.ContentPadding = new Padding(14, 8, 14, 12);
            return panel;
        }

        private static Control CreateField(string labelText, Control input)
        {
            TableLayoutPanel layout = new();
            layout.Dock = DockStyle.Fill;
            layout.Margin = new Padding(4);
            layout.RowCount = 2;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label label = new();
            label.Dock = DockStyle.Fill;
            label.Text = labelText;
            label.TextAlign = ContentAlignment.BottomLeft;
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 3, 0, 0);
            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(input, 0, 1);
            return layout;
        }

        private static AFMSNumberBox CreateNumberBox(int decimalPlaces)
        {
            AFMSNumberBox box = new();
            box.InputType = AFMSNumericInputType.Double;
            box.AllowNegative = false;
            box.DecimalPlaces = decimalPlaces;
            box.BorderColor = DllColorHelper.GetCommonBorder();
            box.FocusBorderColor = DllColorHelper.HexToColor("#02925D");
            box.BorderRadius = 6;
            return box;
        }

        private static AFMSNumberBox CreateIntegerBox()
        {
            AFMSNumberBox box = CreateNumberBox(0);
            box.InputType = AFMSNumericInputType.Integer;
            return box;
        }

        private VelocityDistributionFitMode GetFitMode()
        {
            return _fitMode.SelectedItem is FitModeOption option
                ? option.Value
                : VelocityDistributionFitMode.AutoAsymmetric;
        }

        private void FitMode_SelectedIndexChanged(object? sender, EventArgs e) => UpdateManualInputState();

        private void UpdateManualInputState()
        {
            bool enabled = GetFitMode() == VelocityDistributionFitMode.Manual;
            _flowCenterX.Enabled = enabled;
            _betaLeft.Enabled = enabled;
            _betaRight.Enabled = enabled;
        }
    }
}
