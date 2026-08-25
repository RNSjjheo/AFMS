using AFMSDll;
using AFMSDll.Source.Measure.Discharge;
using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AFMSSettings.Source.Form.Discharge
{
    public sealed class RatingCurveVer0Control : UserControl
    {
        private const int MAX_SECTION_COUNT = 10;
        private const string COL_NO = "NO";
        private const string COL_MAX_H = QRatingCurve.VER1_ATTR_MAX_H;
        private const string COL_NODE1 = QRatingCurve.VER1_ATTR_NODE1;
        private const string COL_NODE2 = QRatingCurve.VER1_ATTR_NODE2;
        private const string COL_NODE3 = QRatingCurve.VER1_ATTR_NODE3;

        private readonly AFMSDataGridView _uiGrid;
        private readonly AFMSNumberBox _uiMaxWaterLevel;
        private readonly AFMSNumberBox _uiA;
        private readonly AFMSNumberBox _uiB;
        private readonly AFMSNumberBox _uiC;

        public RatingCurveVer0Control()
        {
            BackColor = Color.White;

            TableLayoutPanel main = new TableLayoutPanel();
            main.Dock = DockStyle.Fill;
            main.Margin = Padding.Empty;
            main.ColumnCount = 1;
            main.RowCount = 3;
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            Controls.Add(main);

            AFMSMathLabel formula = QRatingCurve.GetExample(DiscVerRatingCurve.Ver00);
            formula.Dock = DockStyle.Fill;
            formula.TextAlign = ContentAlignment.MiddleCenter;

            _uiGrid = CreateGrid();
            _uiGrid.CellDoubleClick += UiGrid_CellDoubleClick;
            SetupGridColumns();

            _uiMaxWaterLevel = CreateNumberBox("최대 수위");
            _uiA = CreateNumberBox(COL_NODE1);
            _uiB = CreateNumberBox(COL_NODE2);
            _uiC = CreateNumberBox(COL_NODE3);
            _uiMaxWaterLevel.InnerTextBox.KeyDown += UiMaxWaterLevel_KeyDown;

            main.Controls.Add(formula, 0, 0);
            main.Controls.Add(_uiGrid, 0, 1);
            main.Controls.Add(CreateInputPanel(), 0, 2);
        }

        public DiscVerRatingCurve Version => DiscVerRatingCurve.Ver00;

        public bool TryCreateConfig(out TabDiscRatingCurve.RatingCurveConfig config)
        {
            config = new TabDiscRatingCurve.RatingCurveConfig();

            if (_uiGrid.Rows.Count == 0)
            {
                MessageBox.Show("관계곡선 구간을 한 개 이상 추가해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            config.DisVer = (int)Version;

            foreach (DataGridViewRow row in _uiGrid.Rows)
            {
                config.Coefficients.Add(new TabDiscRatingCurve.RatingCurveCoefficient
                {
                    MaxWaterLevel = Convert.ToDouble(row.Cells[COL_MAX_H].Value),
                    A = Convert.ToDouble(row.Cells[COL_NODE1].Value),
                    B = Convert.ToDouble(row.Cells[COL_NODE2].Value),
                    C = Convert.ToDouble(row.Cells[COL_NODE3].Value)
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
            row.ColumnCount = 5;
            for (int i = 0; i < 4; i++) row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));

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
            row.Controls.Add(_uiC, 3, 0);
            row.Controls.Add(add, 4, 0);
            panel.Controls.Add(row);
            return panel;
        }

        private void Add_Click(object? sender, EventArgs e)
        {
            AFMSNumberBox[] inputs = [_uiMaxWaterLevel, _uiA, _uiB, _uiC];
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
            if (_uiGrid.Rows.Count > 0 && maxH <= Convert.ToDouble(_uiGrid.Rows[^1].Cells[COL_MAX_H].Value))
            {
                MessageBox.Show("최대 수위는 이전 구간보다 커야 합니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _uiGrid.Rows.Add(_uiGrid.Rows.Count + 1, maxH, _uiA.DoubleValue, _uiB.DoubleValue, _uiC.DoubleValue);
            foreach (AFMSNumberBox input in inputs) input.Text = string.Empty;
        }

        private void UiMaxWaterLevel_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!e.Control || e.KeyCode != Keys.V) return;

            string clipboardText;
            try
            {
                if (!Clipboard.ContainsText()) return;
                clipboardText = Clipboard.GetText(TextDataFormat.UnicodeText);
            }
            catch (ExternalException)
            {
                MessageBox.Show("클립보드 데이터를 읽을 수 없습니다. 다시 시도해주세요.", "붙여넣기 오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            bool multipleCells = clipboardText.IndexOfAny(['\t', '\r', '\n']) >= 0;
            if (!multipleCells) return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            if (!TryParseExcelValues(clipboardText, out double[] values))
            {
                MessageBox.Show(
                    "Excel에서 최대 수위, a, b, c 순서의 숫자 셀 4개를 복사해주세요.",
                    "붙여넣기 형식 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _uiMaxWaterLevel.SetValue(values[0]);
            _uiA.SetValue(values[1]);
            _uiB.SetValue(values[2]);
            _uiC.SetValue(values[3]);
            _uiC.Focus();
        }

        private static bool TryParseExcelValues(string text, out double[] values)
        {
            values = Array.Empty<double>();
            string normalized = text.TrimEnd('\r', '\n');
            string[] rows = normalized.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            string[] cells;

            if (rows.Length == 1)
            {
                cells = rows[0].Split('\t');
            }
            else if (rows.Length == 4)
            {
                cells = new string[4];
                for (int i = 0; i < rows.Length; i++)
                {
                    if (rows[i].Contains('\t')) return false;
                    cells[i] = rows[i];
                }
            }
            else
            {
                return false;
            }

            if (cells.Length != 4) return false;

            values = new double[4];
            for (int i = 0; i < cells.Length; i++)
            {
                string cell = cells[i].Trim();
                if (double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out values[i])) continue;
                if (double.TryParse(cell, NumberStyles.Float, CultureInfo.CurrentCulture, out values[i])) continue;
                values = Array.Empty<double>();
                return false;
            }

            return true;
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
            for (int i = 0; i < _uiGrid.Rows.Count; i++) _uiGrid.Rows[i].Cells[COL_NO].Value = i + 1;
            _uiGrid.ClearSelection();
        }

        private void SetupGridColumns()
        {
            AddColumn(COL_NO, "No.", 12F);
            AddColumn(COL_MAX_H, "최대 수위 (m)", 26F, "0.000");
            AddColumn(COL_NODE1, COL_NODE1, 20F, "0.0000");
            AddColumn(COL_NODE2, COL_NODE2, 20F, "0.0000");
            AddColumn(COL_NODE3, COL_NODE3, 22F, "0.0000");
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
