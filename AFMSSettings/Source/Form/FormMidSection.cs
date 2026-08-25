using AFMSDll;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class FormMidSection : AFMSForm
    {
        public sealed class MidSectionConfig
        {
            public int HydroId { get; set; } = -1;
            public int DisVer { get; set; }
            public int CellMin { get; set; }
            public int CellMax { get; set; }
            public double ConversionFactor { get; set; }
        }

        private TableLayoutPanel uiTpMain;
        private AFMSComboBox uiCbVersion;
        private AFMSNumberBox uiNumberCellMin;
        private AFMSNumberBox uiNumberCellMax;
        private AFMSNumberBox uiNumberConversionFactor;
        private AFMSButton uiButtonSave;
        private AFMSButton uiButtonCancel;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int HydroId { get; set; } = -1;

        public MidSectionConfig? ResultConfig { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Func<MidSectionConfig, string>? SaveHandler { get; set; }

        public FormMidSection()
        {
            Text = "중간단면적법 설정 입력";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            ShowMinimizeButton = false;
            ShowMaximizeButton = false;
            ShowInfoButton = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            ClientSize = new Size(480, 620);
            Padding = new Padding(18);

            SetupMainLayout();
            SetupVersion();
            SetupInputRange();
            SetupConversionFactor();
            SetupFormula();
            SetupGuide();
            SetupButtons();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SetDefaultCellRange();
        }

        private void SetupMainLayout()
        {
            uiTpMain = new TableLayoutPanel();
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.Margin = Padding.Empty;
            uiTpMain.Padding = new Padding(0, 4, 0, 0);
            uiTpMain.ColumnCount = 1;
            uiTpMain.RowCount = 6;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 135F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 85F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            Controls.Add(uiTpMain);
        }

        private void SetupVersion()
        {
            uiCbVersion = new AFMSComboBox();
            uiCbVersion.Dock = DockStyle.Right;
            uiCbVersion.Width = 200;
            uiCbVersion.Margin = new Padding(0, 0, 0, 6);
            uiCbVersion.BorderRadius = 6;
            uiCbVersion.BorderColor = DllColorHelper.GetCommonBorder();
            uiCbVersion.Items.Add("Type1");
            uiCbVersion.SelectedIndex = 0;

            uiTpMain.Controls.Add(uiCbVersion, 0, 0);
        }

        private void SetupInputRange()
        {
            AFMSSectionPanel group = CreateHeaderGroupBox("입력 범위");
            group.Margin = new Padding(0, 4, 0, 6);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = new Padding(8, 4, 8, 4);
            layout.ColumnCount = 5;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label lbMin = CreateInputLabel("MIN");
            Label lbMax = CreateInputLabel("MAX");
            Label lbSeparator = CreateInputLabel("~");

            uiNumberCellMin = CreateRangeNumberBox();
            uiNumberCellMax = CreateRangeNumberBox();
            uiNumberCellMin.Minimum = 1;
            uiNumberCellMax.Minimum = 1;
            uiNumberCellMin.SetValue(1);

            layout.Controls.Add(lbMin, 0, 0);
            layout.Controls.Add(uiNumberCellMin, 1, 0);
            layout.Controls.Add(lbSeparator, 2, 0);
            layout.Controls.Add(lbMax, 3, 0);
            layout.Controls.Add(uiNumberCellMax, 4, 0);

            group.Controls.Add(layout);
            uiTpMain.Controls.Add(group, 0, 1);
        }

        private void SetupConversionFactor()
        {
            AFMSSectionPanel group = CreateHeaderGroupBox("환산계수");
            group.Margin = new Padding(0, 6, 0, 6);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = new Padding(8, 8, 8, 4);
            layout.ColumnCount = 3;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiNumberConversionFactor = new AFMSNumberBox();
            uiNumberConversionFactor.Dock = DockStyle.Fill;
            uiNumberConversionFactor.Margin = new Padding(0, 5, 0, 5);
            uiNumberConversionFactor.InputType = AFMSNumericInputType.Double;
            uiNumberConversionFactor.AllowNegative = false;
            uiNumberConversionFactor.Minimum = 0;
            uiNumberConversionFactor.BorderColor = DllColorHelper.HexToColor("#CDD7D1");
            uiNumberConversionFactor.FocusBorderColor = DllColorHelper.HexToColor("#02925D");
            uiNumberConversionFactor.BorderRadius = 6;
            uiNumberConversionFactor.TextAlign = HorizontalAlignment.Center;
            uiNumberConversionFactor.SetValue(1.0);

            layout.Controls.Add(uiNumberConversionFactor, 1, 0);
            group.Controls.Add(layout);
            uiTpMain.Controls.Add(group, 0, 2);
        }

        private void SetupFormula()
        {
            AFMSPanel panel = new AFMSPanel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 6, 0, 6);
            panel.Padding = Padding.Empty;
            panel.BackColor = Color.White;
            panel.BorderColor = DllColorHelper.GetCommonBorder();
            panel.BorderThickness = 1F;
            panel.BorderRadius = 7;

            AFMSMathLabel formula = new AFMSMathLabel();
            formula.Dock = DockStyle.Fill;
            formula.Margin = Padding.Empty;
            formula.Padding = Padding.Empty;
            formula.Font = new Font("Cambria Math", 16F, FontStyle.Italic);
            formula.ForeColor = Color.Black;
            formula.TextAlign = ContentAlignment.MiddleCenter;
            formula.AddVariable("Q");
            formula.AddText(" = ");
            formula.AddVariable("A");
            formula.Add("m", AFMSMathTextType.Subscript);
            formula.AddText("(");
            formula.AddVariable("h");
            formula.AddText(") × ");
            formula.AddVariable("V");
            formula.Add("m", AFMSMathTextType.Subscript);

            panel.Controls.Add(formula);
            uiTpMain.Controls.Add(panel, 0, 3);
        }

        private void SetupGuide()
        {
            Label guide = new Label();
            guide.Dock = DockStyle.Fill;
            guide.Margin = Padding.Empty;
            guide.Padding = new Padding(8, 0, 0, 0);
            guide.Text = "ⓘ  입력 범위와 환산계수를 설정한 후 저장 버튼을 눌러주세요.";
            guide.TextAlign = ContentAlignment.MiddleLeft;
            guide.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Regular);
            guide.ForeColor = DllColorHelper.HexToColor("#667085");

            uiTpMain.Controls.Add(guide, 0, 4);
        }

        private void SetupButtons()
        {
            uiButtonSave = CreateButton("저장", true);
            uiButtonCancel = CreateButton("취소", false);
            uiButtonSave.Click += UiButtonSave_Click;
            uiButtonCancel.Click += UiButtonCancel_Click;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnCount = 4;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Controls.Add(uiButtonCancel, 2, 0);
            layout.Controls.Add(uiButtonSave, 3, 0);

            CancelButton = uiButtonCancel;
            uiTpMain.Controls.Add(layout, 0, 5);
        }

        private void SetDefaultCellRange()
        {
            if (HydroId < 0) return;

            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.Add(FbtAFMSHydroMeter.COL_TRANSECT_CNT);
            query.Where(FbtAFMSHydroMeter.COL_ID, "=", HydroId);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);

            if (!string.IsNullOrEmpty(error) || table.Rows.Count == 0) return;

            object value = table.Rows[0][FbtAFMSHydroMeter.COL_TRANSECT_CNT];
            if (value == DBNull.Value) return;

            int transectCount = Convert.ToInt32(value);
            if (transectCount < 1) return;

            uiNumberCellMin.Maximum = transectCount;
            uiNumberCellMax.Maximum = transectCount;
            if (!uiNumberCellMax.IntValue.HasValue) uiNumberCellMax.SetValue(transectCount);
        }

        private void UiButtonSave_Click(object? sender, EventArgs e)
        {
            MidSectionConfig? config = CreateConfig();
            if (config == null) return;

            if (SaveHandler != null)
            {
                string error = SaveHandler(config);
                if (!string.IsNullOrEmpty(error))
                {
                    MessageBox.Show(error, "저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            ResultConfig = config;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UiButtonCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private MidSectionConfig? CreateConfig()
        {
            if (!uiNumberCellMin.IntValue.HasValue || !uiNumberCellMax.IntValue.HasValue)
            {
                MessageBox.Show("MIN과 MAX를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            if (uiNumberCellMin.IntValue.Value > uiNumberCellMax.IntValue.Value)
            {
                MessageBox.Show("MIN은 MAX보다 클 수 없습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            if (!uiNumberConversionFactor.DoubleValue.HasValue)
            {
                MessageBox.Show("환산계수를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                uiNumberConversionFactor.Focus();
                return null;
            }

            MidSectionConfig config = new MidSectionConfig();
            config.HydroId = HydroId;
            config.DisVer = uiCbVersion.SelectedIndex < 0 ? 0 : uiCbVersion.SelectedIndex;
            config.CellMin = uiNumberCellMin.IntValue.Value;
            config.CellMax = uiNumberCellMax.IntValue.Value;
            config.ConversionFactor = uiNumberConversionFactor.DoubleValue.Value;

            return config;
        }

        private static AFMSSectionPanel CreateHeaderGroupBox(string headerText)
        {
            AFMSSectionPanel group = new AFMSSectionPanel();
            group.Dock = DockStyle.Fill;
            group.HeaderText = headerText;
            group.HeaderColor = DllColorHelper.HexToColor("#02925D");
            group.HeaderHeight = 42;
            group.HeaderHorizontalPadding = 14;
            group.HeaderBarWidth = 3;
            group.HeaderBarHeight = 18;
            group.HeaderBarTextGap = 10;
            group.HeaderLineColor = DllColorHelper.GetCommonBorder();
            group.HeaderLineThickness = 1F;
            group.BorderRadius = 7;
            group.BorderThickness = 1F;
            group.BorderColor = DllColorHelper.GetCommonBorder();
            group.BackColor = Color.White;
            group.Padding = new Padding(12, 6, 12, 10);
            return group;
        }

        private static AFMSNumberBox CreateRangeNumberBox()
        {
            AFMSNumberBox numberBox = new AFMSNumberBox();
            numberBox.Dock = DockStyle.Fill;
            numberBox.Margin = new Padding(0, 8, 0, 8);
            numberBox.InputType = AFMSNumericInputType.Integer;
            numberBox.AllowNegative = false;
            numberBox.BorderColor = DllColorHelper.HexToColor("#CDD7D1");
            numberBox.FocusBorderColor = DllColorHelper.HexToColor("#02925D");
            numberBox.BorderRadius = 6;
            numberBox.TextAlign = HorizontalAlignment.Center;
            return numberBox;
        }

        private static Label CreateInputLabel(string text)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.Margin = Padding.Empty;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Regular);
            label.ForeColor = DllColorHelper.HexToColor("#4E5963");
            return label;
        }

        private static AFMSButton CreateButton(string text, bool primary)
        {
            AFMSButton button = new AFMSButton();
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(8, 0, 0, 0);
            button.Text = text;
            button.BorderRadius = 5;

            if (primary)
            {
                button.BackColor = DllColorHelper.HexToColor("#02925D");
                button.HoverBackColor = DllColorHelper.HexToColor("#027F51");
                button.PressedBackColor = DllColorHelper.HexToColor("#026D46");
                button.ForeColor = Color.White;
                button.BorderThickness = 0F;
            }
            else
            {
                button.BackColor = Color.White;
                button.HoverBackColor = DllColorHelper.HexToColor("#F3F6F4");
                button.PressedBackColor = DllColorHelper.HexToColor("#E7ECE9");
                button.ForeColor = DllColorHelper.HexToColor("#4C5751");
                button.BorderColor = DllColorHelper.HexToColor("#C9D2CD");
                button.BorderThickness = 1F;
            }

            return button;
        }
    }
}
