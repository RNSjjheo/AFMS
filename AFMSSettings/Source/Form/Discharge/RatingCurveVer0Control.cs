using AFMSDll;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings.Source.Form.Discharge
{
    public sealed class RatingCurveVer0Control : UserControl
    {
        private const int MAX_SECTION_COUNT = 10;

        private readonly TextBox _uiCurveName;
        private readonly AFMSDataGridView _uiGrid;
        private readonly AFMSNumberBox _uiMaxWaterLevel;
        private readonly AFMSNumberBox _uiA;
        private readonly AFMSNumberBox _uiB;
        private readonly AFMSNumberBox _uiH0;
        private readonly AFMSNumberBox _uiUncertainty;

        public RatingCurveVer0Control()
        {
            BackColor = Color.White;

            TableLayoutPanel main = new TableLayoutPanel();
            main.Dock = DockStyle.Fill;
            main.Margin = Padding.Empty;
            main.ColumnCount = 1;
            main.RowCount = 5;
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            Controls.Add(main);

            _uiCurveName = new TextBox();
            _uiCurveName.Dock = DockStyle.Fill;
            _uiCurveName.Margin = new Padding(8, 12, 0, 10);
            _uiCurveName.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 10F);

            TableLayoutPanel nameLayout = new TableLayoutPanel();
            nameLayout.Dock = DockStyle.Fill;
            nameLayout.ColumnCount = 2;
            nameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            nameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            nameLayout.Controls.Add(CreateLabel("곡선명"), 0, 0);
            nameLayout.Controls.Add(_uiCurveName, 1, 0);

            Label formula = CreateLabel("Q = a(h − h₀)ᵇ");
            formula.Font = new Font("Cambria Math", 17F, FontStyle.Italic);

            Label guide = CreateLabel("ⓘ  적용 수위의 최댓값이 작은 구간부터 순서대로 입력해주세요.");
            guide.TextAlign = ContentAlignment.MiddleLeft;
            guide.ForeColor = DllColorHelper.HexToColor("#667085");

            _uiGrid = CreateGrid();
            _uiGrid.CellDoubleClick += UiGrid_CellDoubleClick;
            SetupGridColumns();

            _uiMaxWaterLevel = CreateNumberBox("최대 수위");
            _uiA = CreateNumberBox("a");
            _uiB = CreateNumberBox("b");
            _uiH0 = CreateNumberBox("h₀");
            _uiUncertainty = CreateNumberBox("불확도(%)");

            main.Controls.Add(nameLayout, 0, 0);
            main.Controls.Add(formula, 0, 1);
            main.Controls.Add(guide, 0, 2);
            main.Controls.Add(_uiGrid, 0, 3);
            main.Controls.Add(CreateInputPanel(), 0, 4);
        }

        public DiscVerRatingCurve Version => DiscVerRatingCurve.Ver00;

        public bool TryCreateConfig(out TabDiscRatingCurve.RatingCurveConfig config)
        {
            config = new TabDiscRatingCurve.RatingCurveConfig();
            string name = _uiCurveName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("곡선명을 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _uiCurveName.Focus();
                return false;
            }

            if (_uiGrid.Rows.Count == 0)
            {
                MessageBox.Show("관계곡선 구간을 한 개 이상 추가해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            config.DisVer = (int)Version;
            config.CurveName = name;
            foreach (DataGridViewRow row in _uiGrid.Rows)
            {
                config.Coefficients.Add(new TabDiscRatingCurve.RatingCurveCoefficient
                {
                    MaxWaterLevel = Convert.ToDouble(row.Cells["MAX_H"].Value),
                    A = Convert.ToDouble(row.Cells["A"].Value),
                    B = Convert.ToDouble(row.Cells["B"].Value),
                    H0 = Convert.ToDouble(row.Cells["H0"].Value),
                    Uncertainty = Convert.ToDouble(row.Cells["UNCERTAINTY"].Value)
                });
            }
            return true;
        }

        private Control CreateInputPanel()
        {
            AFMSPanel panel = new AFMSPanel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 5, 0, 5);
            panel.Padding = new Padding(5);
            panel.BackColor = DllColorHelper.HexToColor("#F8FBF9");
            panel.BorderColor = DllColorHelper.HexToColor("#DCE8E0");
            panel.BorderThickness = 1F;
            panel.BorderRadius = 7;

            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.ColumnCount = 6;
            for (int i = 0; i < 5; i++) row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));

            AFMSButton add = new AFMSButton();
            add.Dock = DockStyle.Fill;
            add.Margin = new Padding(5, 0, 0, 0);
            add.Text = "추가";
            add.BorderRadius = 5;
            add.BackColor = DllColorHelper.HexToColor("#02925D");
            add.ForeColor = Color.White;
            add.BorderThickness = 0F;
            add.Click += Add_Click;

            row.Controls.Add(_uiMaxWaterLevel, 0, 0);
            row.Controls.Add(_uiA, 1, 0);
            row.Controls.Add(_uiB, 2, 0);
            row.Controls.Add(_uiH0, 3, 0);
            row.Controls.Add(_uiUncertainty, 4, 0);
            row.Controls.Add(add, 5, 0);
            panel.Controls.Add(row);
            return panel;
        }

        private void Add_Click(object? sender, EventArgs e)
        {
            AFMSNumberBox[] inputs = [_uiMaxWaterLevel, _uiA, _uiB, _uiH0, _uiUncertainty];
            foreach (AFMSNumberBox input in inputs)
            {
                if (input.DoubleValue.HasValue) continue;
                MessageBox.Show($"{input.Hint} 값을 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                input.Focus();
                return;
            }

            if (_uiGrid.Rows.Count >= MAX_SECTION_COUNT)
            {
                MessageBox.Show($"최대 {MAX_SECTION_COUNT}개 구간까지 입력할 수 있습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            double maxH = _uiMaxWaterLevel.DoubleValue!.Value;
            if (_uiGrid.Rows.Count > 0 && maxH <= Convert.ToDouble(_uiGrid.Rows[^1].Cells["MAX_H"].Value))
            {
                MessageBox.Show("최대 수위는 이전 구간보다 커야 합니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _uiGrid.Rows.Add(_uiGrid.Rows.Count + 1, maxH, _uiA.DoubleValue, _uiB.DoubleValue, _uiH0.DoubleValue, _uiUncertainty.DoubleValue);
            foreach (AFMSNumberBox input in inputs) input.Text = string.Empty;
        }

        private void UiGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _uiGrid.Rows.Count) return;
            DialogResult result = MessageBox.Show(
                $"{e.RowIndex + 1}번 구간을 삭제하시겠습니까?",
                "구간 삭제",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;

            _uiGrid.Rows.RemoveAt(e.RowIndex);
            for (int i = 0; i < _uiGrid.Rows.Count; i++) _uiGrid.Rows[i].Cells["NO"].Value = i + 1;
            _uiGrid.ClearSelection();
        }

        private void SetupGridColumns()
        {
            AddColumn("NO", "No.", 12F);
            AddColumn("MAX_H", "최대 수위 (m)", 22F, "0.000");
            AddColumn("A", "a", 16F, "0.0000");
            AddColumn("B", "b", 16F, "0.0000");
            AddColumn("H0", "h₀ (m)", 18F, "0.000");
            AddColumn("UNCERTAINTY", "불확도 (%)", 20F, "0.00");
        }

        private void AddColumn(string name, string header, float weight, string format = "")
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = header;
            column.FillWeight = weight;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.DefaultCellStyle.Format = format;
            _uiGrid.Columns.Add(column);
        }

        private static AFMSDataGridView CreateGrid()
        {
            AFMSDataGridView grid = new AFMSDataGridView();
            grid.Dock = DockStyle.Fill;
            grid.Margin = Padding.Empty;
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AFMSHeaderHeight = 34;
            grid.AFMSRowHeight = 32;
            grid.BorderRadius = 6;
            return grid;
        }

        private static AFMSNumberBox CreateNumberBox(string hint)
        {
            AFMSNumberBox box = new AFMSNumberBox();
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(2, 0, 2, 0);
            box.InputType = AFMSNumericInputType.Double;
            box.Hint = hint;
            return box;
        }

        private static Label CreateLabel(string text)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.Margin = Padding.Empty;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F);
            return label;
        }
    }
}
