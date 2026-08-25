using AFMSDll;
using System;

namespace AFMSSettings
{
    public abstract class TabSurfaceBase : TabPage
    {
        public const string ATTR_FORMAT = "0.00";
        public abstract void SetupGridColumns();

        public DiscVerSurfaceVelo Version;
        public AFMSDataGridView uiGrid;
        public TableLayoutPanel uiTpMainRow;
        public Label uiDesc;
        public AFMSMathLabel uiLbExample;

        public AFMSNumberBox uiNumberCellMin;
        public AFMSNumberBox uiNumberCellMax;
        public AFMSNumberBox uiNumberVst;
        public AFMSNumberBox uiNumberVindex;

        private AFMSSectionPanel uiGpRange;
        private AFMSSectionPanel uiGpUncert;
        public TabSurfaceBase()
        {
            uiTpMainRow = new TableLayoutPanel();
            uiTpMainRow.Dock = DockStyle.Fill;
            uiTpMainRow.Margin = Padding.Empty;
            uiTpMainRow.Padding = Padding.Empty;
            uiTpMainRow.ColumnCount = 1;
            uiTpMainRow.RowCount = 5;
            uiTpMainRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));

            uiDesc = new Label();
            uiDesc.Dock = DockStyle.Fill;
            uiDesc.Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular);
            uiDesc.ForeColor = DllColorHelper.HexToColor("#69737D");
            uiDesc.TextAlign = ContentAlignment.MiddleLeft;
            uiDesc.Padding = Padding.Empty;
            uiDesc.Margin = Padding.Empty;

            uiLbExample = QSurfaceVelo.GetExample(Version);
            uiLbExample.Dock = DockStyle.Fill;
            uiLbExample.Font = new Font("맑은 고딕", 12F, FontStyle.Regular);
            uiLbExample.ForeColor = Color.Black;
            uiLbExample.TextAlign = ContentAlignment.MiddleCenter;
            uiLbExample.Padding = Padding.Empty;
            uiLbExample.Margin = Padding.Empty;

            uiGrid = new AFMSDataGridView();
            uiGrid.Dock = DockStyle.Fill;
            uiGrid.Margin = Padding.Empty;
            uiGrid.AutoGenerateColumns = false;
            uiGrid.ReadOnly = true;
            uiGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
            uiGrid.AllowUserToAddRows = false;
            uiGrid.AllowUserToDeleteRows = false;
            uiGrid.ShowSelectedRowHighlight = true;
            uiGrid.AFMSHeaderHeight = 36;
            uiGrid.AFMSRowHeight = 34;
            uiGrid.BorderRadius = 6;
            uiGrid.BorderThickness = 1F;
            uiGrid.BorderColor = DllColorHelper.GetCommonBorder();
            uiGrid.Columns.Clear();
            uiGrid.Rows.Clear();
            uiGrid.CellDoubleClick += UiGrid_CellDoubleClick;

            TableLayoutPanel rangelayout = new TableLayoutPanel();
            rangelayout.Dock = DockStyle.Fill;
            rangelayout.Margin = Padding.Empty;
            rangelayout.Padding = Padding.Empty;
            rangelayout.ColumnCount = 2;
            rangelayout.RowCount = 1;
            rangelayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            rangelayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            rangelayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiGpRange = GreateHeaderGroupBox();
            uiGpRange.HeaderText = "입력범위";
            uiGpRange.Padding = new Padding(8, 4, 8, 6);
            uiGpRange.Margin = new Padding(0, 0, 4, 0);

            uiGpUncert = GreateHeaderGroupBox();
            uiGpUncert.HeaderText = "불확도";
            uiGpUncert.Padding = new Padding(8, 4, 8, 6);
            uiGpUncert.Margin = new Padding(4, 0, 0, 0);

            rangelayout.Controls.Add(uiGpRange, 0, 0);
            rangelayout.Controls.Add(uiGpUncert, 1, 0);

            uiTpMainRow.Controls.Add(rangelayout, 0, 0);
            uiTpMainRow.Controls.Add(uiLbExample, 0, 1);
            uiTpMainRow.Controls.Add(uiDesc, 0, 2);
            uiTpMainRow.Controls.Add(uiGrid, 0, 3);
            Controls.Add(uiTpMainRow);

            SetupInputRange();
            SetupUncertainty();
        }

        private void SetupInputRange()
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

            Label labelSeparator = new Label();
            labelSeparator.Dock = DockStyle.Fill;
            labelSeparator.Padding = Padding.Empty;
            labelSeparator.Margin = Padding.Empty;
            labelSeparator.Text = "~";
            labelSeparator.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 11F, FontStyle.Regular);
            labelSeparator.ForeColor = DllColorHelper.HexToColor("#4E5963");
            labelSeparator.TextAlign = ContentAlignment.MiddleCenter;

            Label lbMin = CreateRangeLabel();
            Label lbMax = CreateRangeLabel();
            uiNumberCellMin = CreateRangeNumberBox();
            uiNumberCellMax = CreateRangeNumberBox();

            uiNumberCellMin.Minimum = 1;
            uiNumberCellMin.SetValue(1);

            lbMin.Text = "MIN";
            lbMax.Text = "MAX";

            layout.Controls.Add(lbMin, 0, 0);
            layout.Controls.Add(uiNumberCellMin, 1, 0);
            layout.Controls.Add(labelSeparator, 1, 1);
            layout.Controls.Add(lbMax, 0, 2);
            layout.Controls.Add(uiNumberCellMax, 1, 2);

            uiGpRange.Controls.Add(layout);
        }

        private void SetupUncertainty()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnCount = 2;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            AFMSMathLabel mathVst = CreateUncertaintyMathLabel("st");
            AFMSMathLabel mathVidx = CreateUncertaintyMathLabel("index");

            uiNumberVst = CreateUncertaintyNumberBox();
            uiNumberVindex = CreateUncertaintyNumberBox();

            layout.Controls.Add(mathVst, 0, 0);
            layout.Controls.Add(uiNumberVst, 1, 0);
            layout.Controls.Add(mathVidx, 0, 2);
            layout.Controls.Add(uiNumberVindex, 1, 2);

            uiGpUncert.Controls.Add(layout);
        }

        private static AFMSMathLabel CreateUncertaintyMathLabel(string subscript)
        {
            AFMSMathLabel math = new AFMSMathLabel();
            math.Dock = DockStyle.Fill;
            math.Margin = Padding.Empty;
            math.Padding = Padding.Empty;
            math.TextAlign = ContentAlignment.MiddleCenter;
            math.Font = new Font("Cambria Math", 11F, FontStyle.Italic);

            math.AddVariable("u");
            math.AddText("(");
            math.AddVariable("v");
            math.Add(subscript, AFMSMathTextType.Subscript);
            math.AddText(")");

            return math;
        }

        protected virtual string GridRowNumberColumnName => "No.";

        protected virtual void OnGridRowDeleted(int rowIndex)
        {
        }

        private void UiGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= uiGrid.Rows.Count) return;

            DataGridViewRow row = uiGrid.Rows[e.RowIndex];
            string rowNo = GetGridRowNumber(row, e.RowIndex);

            DialogResult result = MessageBox.Show(
                $"{rowNo}번 항목을 삭제하시겠습니까?",
                "항목 삭제",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return;

            uiGrid.Rows.RemoveAt(e.RowIndex);
            RenumberGridRows();
            OnGridRowDeleted(e.RowIndex);
        }

        private string GetGridRowNumber(DataGridViewRow row, int rowIndex)
        {
            if (uiGrid.Columns.Contains(GridRowNumberColumnName))
            {
                object? value = row.Cells[GridRowNumberColumnName].Value;
                if (value != null && !string.IsNullOrWhiteSpace(Convert.ToString(value))) return Convert.ToString(value)!;
            }

            return (rowIndex + 1).ToString();
        }

        protected void RenumberGridRows()
        {
            if (!uiGrid.Columns.Contains(GridRowNumberColumnName)) return;

            for (int i = 0; i < uiGrid.Rows.Count; i++) uiGrid.Rows[i].Cells[GridRowNumberColumnName].Value = i + 1;

            uiGrid.ClearSelection();
        }

        private AFMSSectionPanel GreateHeaderGroupBox()
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
            item.Margin = new Padding(0, 5, 0, 5);

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

        private static AFMSNumberBox CreateUncertaintyNumberBox()
        {
            AFMSNumberBox numberBox = new AFMSNumberBox();
            numberBox.Dock = DockStyle.Fill;
            numberBox.Margin = new Padding(4, 3, 0, 3);
            numberBox.InputType = AFMSNumericInputType.Double;
            numberBox.AllowNegative = false;
            numberBox.BorderColor = DllColorHelper.HexToColor("#CDD7D1");
            numberBox.FocusBorderColor = DllColorHelper.HexToColor("#02925D");
            numberBox.BorderRadius = 6;
            numberBox.TextAlign = HorizontalAlignment.Center;

            return numberBox;
        }

        private static Label CreateRangeLabel()
        {
            Label item = new Label();
            item.Dock = DockStyle.Fill;
            item.TextAlign = ContentAlignment.MiddleCenter;
            item.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Regular);
            item.ForeColor = Color.FromArgb(100, 108, 116);

            return item;
        }


        public override string ToString()
        {
            switch (Version)
            {
                case DiscVerSurfaceVelo.Ver00:
                    return "Type1";
                default:
                    return "정의되지 않음";
            }
        }
    }
}
