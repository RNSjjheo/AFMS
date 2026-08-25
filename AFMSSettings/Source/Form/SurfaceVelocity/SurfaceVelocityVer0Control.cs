using AFMSDll;
using System;

namespace AFMSSettings
{
    public sealed class SurfaceVelocityVer0Control : UserControl
    {
        public const string ATTR_FORMAT = "0.00";
        private const int MAX_ATTR_COUNT = 10;

        public DiscVerSurfaceVelo Version => DiscVerSurfaceVelo.Ver00;
        public AFMSDataGridView uiGrid;
        public TableLayoutPanel uiTpMainRow;
        public Label uiDesc;
        public AFMSMathLabel uiLbExample;

        public AFMSNumberBox uiNumberCellMin;
        public AFMSNumberBox uiNumberCellMax;
        public AFMSNumberBox uiNumberVst;
        public AFMSNumberBox uiNumberVindex;
        public AFMSNumberBox uiNumberMaxVi;
        public AFMSNumberBox uiNumberA;
        public AFMSNumberBox uiNumberB;
        public AFMSButton uiBtnAdd;

        private AFMSSectionPanel uiGpRange;
        private AFMSSectionPanel uiGpUncert;
        public SurfaceVelocityVer0Control()
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
            uiGpRange.Padding = Padding.Empty;
            uiGpRange.ContentPadding = new Padding(8, 4, 8, 6);
            uiGpRange.Margin = new Padding(0, 0, 4, 0);

            uiGpUncert = GreateHeaderGroupBox();
            uiGpUncert.HeaderText = "불확도";
            uiGpUncert.Padding = Padding.Empty;
            uiGpUncert.ContentPadding = new Padding(8, 4, 8, 6);
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

            uiDesc.Text = $"아래 입력란에서 {QSurfaceVelo.VER1_ATTR_NODE1}, " +
                $"{QSurfaceVelo.VER1_ATTR_NODE2}, {QSurfaceVelo.VER1_ATTR_NODE3} 값을 입력한 후 추가 버튼을 눌러주세요.";

            SetupGridColumns();
            SetupAttrInput();
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

            uiGpRange.ContentLayout.Controls.Add(layout);
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

            uiGpUncert.ContentLayout.Controls.Add(layout);
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

        private const string GridRowNumberColumnName = "No.";

        private void OnGridRowDeleted(int rowIndex)
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


        public void SetCellRangeMaximum(int maximum)
        {
            if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));

            uiNumberCellMin.Maximum = maximum;
            uiNumberCellMax.Maximum = maximum;
            uiNumberCellMax.SetValue(maximum);
        }

        public bool TryCreateConfig(int hydroId, out TabDiscSurfaceVelocity.SurfaceVelocityConfig config)
        {
            config = new TabDiscSurfaceVelocity.SurfaceVelocityConfig();

            if (!uiNumberCellMin.IntValue.HasValue || !uiNumberCellMax.IntValue.HasValue)
            {
                MessageBox.Show("측선 MIN과 측선 MAX를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (uiNumberCellMin.IntValue.Value > uiNumberCellMax.IntValue.Value)
            {
                MessageBox.Show("측선 MIN은 측선 MAX보다 클 수 없습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (!uiNumberVst.DoubleValue.HasValue)
            {
                MessageBox.Show("Vst 불확도 값을 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                uiNumberVst.Focus();
                return false;
            }

            if (!uiNumberVindex.DoubleValue.HasValue)
            {
                MessageBox.Show("Vindex 불확도 값을 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                uiNumberVindex.Focus();
                return false;
            }

            if (uiGrid.Rows.Count == 0)
            {
                MessageBox.Show("Max Vi, a, b 값을 한 구간 이상 추가해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            foreach (DataGridViewRow row in uiGrid.Rows)
            {
                if (!TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE1, out _) ||
                    !TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE2, out _) ||
                    !TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE3, out _))
                {
                    MessageBox.Show(
                        $"{row.Index + 1}번 구간에 빈 데이터가 있습니다. Max Vi, a, b 값을 모두 입력해주세요.",
                        "입력 확인",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return false;
                }
            }

            config.HydroId = hydroId;
            config.DisVer = (int)Version;
            config.CellMin = uiNumberCellMin.IntValue.Value;
            config.CellMax = uiNumberCellMax.IntValue.Value;
            config.UcertVst = uiNumberVst.DoubleValue.Value;
            config.UcertVindex = uiNumberVindex.DoubleValue.Value;

            foreach (DataGridViewRow row in uiGrid.Rows)
            {
                TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE1, out double maxVi);
                TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE2, out double a);
                TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE3, out double b);

                config.Coefficients.Add(new TabDiscSurfaceVelocity.SurfaceVelocityCoefficient
                {
                    MaxVi = maxVi,
                    A = a,
                    C = b
                });
            }

            config.CoeffCount = config.Coefficients.Count;
            return true;
        }

        private void SetupGridColumns()
        {
            DataGridViewTextBoxColumn colNo = new DataGridViewTextBoxColumn();
            colNo.Name = "No.";
            colNo.HeaderText = "No.";
            colNo.FillWeight = 18F;
            colNo.ReadOnly = true;
            colNo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            DataGridViewTextBoxColumn colMaxVi = new DataGridViewTextBoxColumn();
            colMaxVi.Name = QSurfaceVelo.VER1_ATTR_NODE1;
            colMaxVi.HeaderText = QSurfaceVelo.VER1_ATTR_NODE1;
            colMaxVi.FillWeight = 28F;
            colMaxVi.ReadOnly = true;
            colMaxVi.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colMaxVi.DefaultCellStyle.Format = ATTR_FORMAT;

            DataGridViewTextBoxColumn colA = new DataGridViewTextBoxColumn();
            colA.Name = QSurfaceVelo.VER1_ATTR_NODE2;
            colA.HeaderText = QSurfaceVelo.VER1_ATTR_NODE2;
            colA.FillWeight = 27F;
            colA.ReadOnly = true;
            colA.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colA.DefaultCellStyle.Format = ATTR_FORMAT;

            DataGridViewTextBoxColumn colC = new DataGridViewTextBoxColumn();
            colC.Name = QSurfaceVelo.VER1_ATTR_NODE3;
            colC.HeaderText = QSurfaceVelo.VER1_ATTR_NODE3;
            colC.FillWeight = 27F;
            colC.ReadOnly = true;
            colC.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colC.DefaultCellStyle.Format = ATTR_FORMAT;

            uiGrid.Columns.Add(colNo);
            uiGrid.Columns.Add(colMaxVi);
            uiGrid.Columns.Add(colA);
            uiGrid.Columns.Add(colC);
        }

        private void SetupAttrInput()
        {
            AFMSPanel panel = new AFMSPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(5);
            panel.Margin = new Padding(0, 5, 0, 5);
            panel.BackColor = DllColorHelper.HexToColor("#F8FBF9");
            panel.BorderColor = DllColorHelper.HexToColor("#DCE8E0");
            panel.BorderThickness = 1F;
            panel.BorderRadius = 7;

            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.Margin = new Padding(3);
            row.Padding = Padding.Empty;
            row.ColumnCount = 7;
            row.RowCount = 1;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiNumberMaxVi = CreateAttributeNumberBox(QSurfaceVelo.VER1_ATTR_NODE1);
            uiNumberA = CreateAttributeNumberBox(QSurfaceVelo.VER1_ATTR_NODE2);
            uiNumberB = CreateAttributeNumberBox(QSurfaceVelo.VER1_ATTR_NODE3);

            uiBtnAdd = new AFMSButton();
            uiBtnAdd.Dock = DockStyle.Fill;
            uiBtnAdd.Text = "추가";
            uiBtnAdd.BorderRadius = 5;
            uiBtnAdd.BackColor = DllColorHelper.HexToColor("#02925D");
            uiBtnAdd.HoverBackColor = DllColorHelper.HexToColor("#027F51");
            uiBtnAdd.PressedBackColor = DllColorHelper.HexToColor("#026D46");
            uiBtnAdd.ForeColor = Color.White;
            uiBtnAdd.BorderThickness = 0;
            uiBtnAdd.Click += UiBtnAdd_Click;

            row.Controls.Add(uiNumberMaxVi, 0, 0);
            row.Controls.Add(uiNumberA, 2, 0);
            row.Controls.Add(uiNumberB, 4, 0);
            row.Controls.Add(uiBtnAdd, 6, 0);

            panel.Controls.Add(row);
            uiTpMainRow.Controls.Add(panel, 0, 4);
        }

        private void UiBtnAdd_Click(object? sender, EventArgs e)
        {
            double? maxVi = GetInputValue(uiNumberMaxVi, QSurfaceVelo.VER1_ATTR_NODE1);
            if (!maxVi.HasValue) return;

            double? a = GetInputValue(uiNumberA, QSurfaceVelo.VER1_ATTR_NODE2);
            if (!a.HasValue) return;

            double? b = GetInputValue(uiNumberB, QSurfaceVelo.VER1_ATTR_NODE3);
            if (!b.HasValue) return;

            if (uiGrid.Rows.Count >= MAX_ATTR_COUNT)
            {
                MessageBox.Show($"최대 {MAX_ATTR_COUNT}개까지 입력할 수 있습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateMaxVi(maxVi.Value)) return;

            AddGridRow(maxVi.Value, a.Value, b.Value);
            ClearAttrInput();
        }

        private static double? GetInputValue(AFMSNumberBox numberBox, string name)
        {
            if (numberBox.DoubleValue.HasValue) return numberBox.DoubleValue.Value;

            MessageBox.Show($"{name} 값을 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            numberBox.Focus();
            return null;
        }

        private bool ValidateMaxVi(double maxVi)
        {
            if (uiGrid.Rows.Count == 0) return true;

            DataGridViewRow lastRow = uiGrid.Rows[uiGrid.Rows.Count - 1];
            double lastMaxVi = Convert.ToDouble(lastRow.Cells[QSurfaceVelo.VER1_ATTR_NODE1].Value);
            if (maxVi > lastMaxVi) return true;

            MessageBox.Show(
                $"{QSurfaceVelo.VER1_ATTR_NODE1} 값은 이전 값 {lastMaxVi:0.####}보다 커야 합니다.",
                "입력 확인",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            uiNumberMaxVi.Focus();
            return false;
        }

        private void AddGridRow(double maxVi, double a, double b)
        {
            int rowIndex = uiGrid.Rows.Add();
            DataGridViewRow row = uiGrid.Rows[rowIndex];
            row.Cells["No."].Value = rowIndex + 1;
            row.Cells[QSurfaceVelo.VER1_ATTR_NODE1].Value = maxVi;
            row.Cells[QSurfaceVelo.VER1_ATTR_NODE2].Value = a;
            row.Cells[QSurfaceVelo.VER1_ATTR_NODE3].Value = b;

            uiGrid.ClearSelection();
            row.Selected = true;
            uiGrid.FirstDisplayedScrollingRowIndex = rowIndex;
        }

        private void ClearAttrInput()
        {
            uiNumberMaxVi.Text = string.Empty;
            uiNumberA.Text = string.Empty;
            uiNumberB.Text = string.Empty;
            uiNumberMaxVi.Focus();
        }

        private static bool TryGetGridDouble(DataGridViewRow row, string columnName, out double value)
        {
            value = 0;
            object? cellValue = row.Cells[columnName].Value;
            if (cellValue == null || cellValue == DBNull.Value) return false;

            string text = Convert.ToString(cellValue)?.Trim() ?? string.Empty;
            return !string.IsNullOrEmpty(text) && double.TryParse(text, out value);
        }

        private static AFMSNumberBox CreateAttributeNumberBox(string hint)
        {
            AFMSNumberBox numberBox = new AFMSNumberBox();
            numberBox.Dock = DockStyle.Fill;
            numberBox.InputType = AFMSNumericInputType.Double;
            numberBox.Hint = hint;
            return numberBox;
        }
    }
}
