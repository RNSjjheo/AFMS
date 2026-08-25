using AFMSDll;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public abstract class TabMidSectionBase : TabPage
    {
        public int Version;
        public TableLayoutPanel uiTpMainRow;
        public AFMSNumberBox uiNumberCellMin;
        public AFMSNumberBox uiNumberCellMax;
        public AFMSNumberBox uiNumberConversionFactor;
        public AFMSMathLabel uiLbExample;
        public Label uiDesc;

        private AFMSSectionPanel uiGpRange;
        private AFMSSectionPanel uiGpConversion;

        protected TabMidSectionBase()
        {
            BackColor = Color.White;
            Padding = Padding.Empty;
            Margin = Padding.Empty;

            uiTpMainRow = new TableLayoutPanel();
            uiTpMainRow.Dock = DockStyle.Fill;
            uiTpMainRow.Margin = Padding.Empty;
            uiTpMainRow.Padding = Padding.Empty;
            uiTpMainRow.ColumnCount = 1;
            uiTpMainRow.RowCount = 4;
            uiTpMainRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 125F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiGpRange = CreateHeaderGroupBox();
            uiGpRange.HeaderText = "입력범위";
            uiGpRange.Margin = new Padding(0, 5, 0, 5);

            uiGpConversion = CreateHeaderGroupBox();
            uiGpConversion.HeaderText = "환산계수";
            uiGpConversion.Margin = new Padding(0, 5, 0, 5);

            uiLbExample = new AFMSMathLabel();
            uiLbExample.Dock = DockStyle.Fill;
            uiLbExample.Margin = new Padding(0, 5, 0, 5);
            uiLbExample.Padding = Padding.Empty;
            uiLbExample.Font = new Font("Cambria Math", 16F, FontStyle.Italic);
            uiLbExample.ForeColor = Color.Black;
            uiLbExample.TextAlign = ContentAlignment.MiddleCenter;

            AFMSPanel formulaPanel = new AFMSPanel();
            formulaPanel.Dock = DockStyle.Fill;
            formulaPanel.Margin = new Padding(0, 5, 0, 5);
            formulaPanel.Padding = Padding.Empty;
            formulaPanel.BackColor = Color.White;
            formulaPanel.BorderColor = DllColorHelper.GetCommonBorder();
            formulaPanel.BorderThickness = 1F;
            formulaPanel.BorderRadius = 7;
            formulaPanel.Controls.Add(uiLbExample);

            uiDesc = new Label();
            uiDesc.Dock = DockStyle.Fill;
            uiDesc.Margin = Padding.Empty;
            uiDesc.Padding = new Padding(8, 0, 0, 0);
            uiDesc.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Regular);
            uiDesc.ForeColor = DllColorHelper.HexToColor("#667085");
            uiDesc.TextAlign = ContentAlignment.MiddleLeft;
            uiDesc.Text = "ⓘ  입력 범위와 환산계수를 설정한 후 저장 버튼을 눌러주세요.";

            uiTpMainRow.Controls.Add(uiGpRange, 0, 0);
            uiTpMainRow.Controls.Add(uiGpConversion, 0, 1);
            uiTpMainRow.Controls.Add(formulaPanel, 0, 2);
            uiTpMainRow.Controls.Add(uiDesc, 0, 3);
            Controls.Add(uiTpMainRow);

            SetupInputRange();
            SetupConversionFactor();
        }

        private void SetupInputRange()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = new Padding(8, 4, 8, 6);
            layout.ColumnCount = 2;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            Label lbMin = CreateLabel("MIN");
            Label lbMax = CreateLabel("MAX");
            Label separator = CreateLabel("~");
            separator.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 11F, FontStyle.Regular);

            uiNumberCellMin = CreateRangeNumberBox();
            uiNumberCellMax = CreateRangeNumberBox();
            uiNumberCellMin.Minimum = 1;
            uiNumberCellMax.Minimum = 1;
            uiNumberCellMin.SetValue(1);

            layout.Controls.Add(lbMin, 0, 0);
            layout.Controls.Add(uiNumberCellMin, 1, 0);
            layout.Controls.Add(separator, 1, 1);
            layout.Controls.Add(lbMax, 0, 2);
            layout.Controls.Add(uiNumberCellMax, 1, 2);

            uiGpRange.Controls.Add(layout);
        }

        private void SetupConversionFactor()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = new Padding(8, 10, 8, 8);
            layout.ColumnCount = 3;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
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

            layout.Controls.Add(uiNumberConversionFactor, 1, 1);
            uiGpConversion.Controls.Add(layout);
        }

        protected void SetFormulaType1()
        {
            uiLbExample.ClearMath();
            uiLbExample.AddVariable("Q");
            uiLbExample.AddText(" = ");
            uiLbExample.AddVariable("A");
            uiLbExample.Add("m", AFMSMathTextType.Subscript);
            uiLbExample.AddText("(");
            uiLbExample.AddVariable("h");
            uiLbExample.AddText(") × ");
            uiLbExample.AddVariable("V");
            uiLbExample.Add("m", AFMSMathTextType.Subscript);
        }

        private static AFMSSectionPanel CreateHeaderGroupBox()
        {
            AFMSSectionPanel item = new AFMSSectionPanel();
            item.SectionStyle = AFMSSectionStyle.OutlineTitle;
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

        public override string ToString()
        {
            return Version == 0 ? "Type1" : "정의되지 않음";
        }
    }
}
