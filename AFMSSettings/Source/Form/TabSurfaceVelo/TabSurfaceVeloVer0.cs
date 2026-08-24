using AFMSDll;
using System;

namespace AFMSSettings
{
    public class TabSurfaceVeloVer0 : TabSurfaceBase
    {
        private const int MAX_ATTR_COUNT = 10;


        public AFMSNumberBox uiNumberMaxVi;
        public AFMSNumberBox uiNumberA;
        public AFMSNumberBox uiNumberB;

        public AFMSButton uiBtnAdd;

        public TabSurfaceVeloVer0()
        {
            Version = DiscVerSurfaceVelo.Ver00;

            uiDesc.Text = $"아래 입력란에서 ";
            uiDesc.Text += $"{QSurfaceVelo.VER1_ATTR_NODE1}, {QSurfaceVelo.VER1_ATTR_NODE2}, {QSurfaceVelo.VER1_ATTR_NODE3} ";
            uiDesc.Text += $"값을 입력한 후 추가 버튼을 눌러주세요.";

            SetupGridColumns();
            SetupAttrInput();
        }



        public override void SetupGridColumns()
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

        public void SetupAttrInput()
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

            uiNumberMaxVi = new AFMSNumberBox();
            uiNumberMaxVi.Dock = DockStyle.Fill;
            uiNumberMaxVi.InputType = AFMSNumericInputType.Double;
            uiNumberMaxVi.Hint = QSurfaceVelo.VER1_ATTR_NODE1;

            uiNumberA = new AFMSNumberBox();
            uiNumberA.Dock = DockStyle.Fill;
            uiNumberA.InputType = AFMSNumericInputType.Double;
            uiNumberA.Hint = QSurfaceVelo.VER1_ATTR_NODE2;

            uiNumberB = new AFMSNumberBox();
            uiNumberB.Dock = DockStyle.Fill;
            uiNumberB.InputType = AFMSNumericInputType.Double;
            uiNumberB.Hint = QSurfaceVelo.VER1_ATTR_NODE3;

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

        private double? GetInputValue(AFMSNumberBox numberBox, string name)
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

            MessageBox.Show($"{QSurfaceVelo.VER1_ATTR_NODE1} 값은 이전 값 {lastMaxVi:0.####}보다 커야 합니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (rowIndex >= 0) uiGrid.FirstDisplayedScrollingRowIndex = rowIndex;
        }

        private void ClearAttrInput()
        {
            uiNumberMaxVi.Text = string.Empty;
            uiNumberA.Text = string.Empty;
            uiNumberB.Text = string.Empty;
            uiNumberMaxVi.Focus();
        }
    }
}
