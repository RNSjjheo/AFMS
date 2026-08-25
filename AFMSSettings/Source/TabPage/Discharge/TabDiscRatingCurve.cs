using AFMSDll;
using AFMSSettings.Source.Form.Discharge;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class TabDiscRatingCurve : _TabDischargeBase
    {
        public sealed class RatingCurveCoefficient
        {
            public double MaxWaterLevel { get; set; }
            public double A { get; set; }
            public double B { get; set; }
            public double H0 { get; set; }
        }

        public sealed class RatingCurveConfig
        {
            public int Id { get; set; } = -1;
            public int DisVer { get; set; }
            public List<RatingCurveCoefficient> Coefficients { get; set; } = new();
        }

        private readonly AFMSDataGridView _uiGridDetail;
        private readonly Label _uiCurveName;

        public TabDiscRatingCurve() : base(true)
        {
            Text = "수위-유량곡선";
            BackColor = Color.White;
            uiTpMain.ColumnStyles[0].Width = 45F;
            uiTpMain.ColumnStyles[1].Width = 55F;
            uiGridMain.AutoGenerateColumns = true;
            uiGridMain.AFMSHeaderHeight = 34;
            uiGridMain.AFMSRowHeight = 34;
            uiGridMain.BorderRadius = 6;
            uiGridMain.SelectionChanged += UiGridMain_SelectionChanged;
            CtlSub = CreateDetailPanel(out _uiCurveName, out _uiGridDetail);
        }

        public override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is not AFMSDataGridView grid) return;
            SetColumnVisible(grid, FbtAFMSDiscAttrRatingCurve.COL_ID, false);
            SetColumnVisible(grid, FbtAFMSDiscAttrRatingCurve.COL_DIS_VER, false);
            SetColumnVisible(grid, FbtAFMSDiscAttrRatingCurve.COL_DIS_ATTR, false);
            SetColumnStyle(grid, COL_NO, "No.", 14F);
            SetColumnStyle(grid, FbtAFMSDiscAttrRatingCurve.COL_COEFF_COUNT, "구간수", 18F);
            SetColumnStyle(grid, _FBTableBase.COL_MEASURE_DATE, "등록일", 26F);
            grid.ClearSelection();
            grid.CurrentCell = null;
        }

        protected override void UiButtonInput_Click(object? sender, EventArgs e)
        {
            using FormDischargeRatingCurve form = new FormDischargeRatingCurve();
            form.SaveHandler = SaveConfig;
            if (form.ShowDialog(FindForm()) != DialogResult.OK) return;
            string error = LoadData();
            if (!string.IsNullOrEmpty(error)) MessageBox.Show(error, "관계곡선 조회 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected override void _TabDischargeBase_Enter(object? sender, EventArgs e) => LoadData();

        public string LoadData()
        {
            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSDiscAttrRatingCurve.TABLE_NAME;
            query.Add(FbtAFMSDiscAttrRatingCurve.COL_ID);
            query.Add(FbtAFMSDiscAttrRatingCurve.COL_DIS_VER);
            query.Add(FbtAFMSDiscAttrRatingCurve.COL_COEFF_COUNT);
            query.Add(FbtAFMSDiscAttrRatingCurve.COL_DIS_ATTR);
            query.Add(_FBTableBase.COL_MEASURE_DATE);
            query.OrderBy(FbtAFMSDiscAttrRatingCurve.COL_ID);
            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) return error;
            table.AddRowNo(COL_NO);
            uiGridMain.DataSource = table;
            ClearSelectionAndDetail();
            return string.Empty;
        }

        public void ClearSelectionAndDetail()
        {
            uiGridMain.ClearSelection();
            uiGridMain.CurrentCell = null;
            _uiCurveName.Text = "곡선을 선택해주세요.";
            _uiGridDetail.Rows.Clear();
        }

        private void UiGridMain_SelectionChanged(object? sender, EventArgs e)
        {
            if (uiGridMain.CurrentRow?.DataBoundItem is not DataRowView rowView) return;
            DataRow row = rowView.Row;
            _uiGridDetail.Rows.Clear();
            string json = Convert.ToString(row[FbtAFMSDiscAttrRatingCurve.COL_DIS_ATTR]) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(json)) return;
            List<RatingCurveCoefficient>? items;
            try
            {
                items = JsonSerializer.Deserialize<List<RatingCurveCoefficient>>(json);
            }
            catch (JsonException)
            {
                _uiCurveName.Text += " (상세 데이터 오류)";
                return;
            }
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                RatingCurveCoefficient item = items[i];
                _uiGridDetail.Rows.Add(i + 1, item.MaxWaterLevel, item.A, item.B, item.H0);
            }
        }

        private static string SaveConfig(RatingCurveConfig config)
        {
            DateTime now = DateTime.Now;
            QueryBuilderInsert query = new QueryBuilderInsert();
            query.Table = FbtAFMSDiscAttrRatingCurve.TABLE_NAME;
            query.AutoIncrement = FbtAFMSDiscAttrRatingCurve.COL_ID;
            query.Value(_FBTableBase.COL_MEASURE_DATE, now.ToString("yyyyMMdd"));
            query.Value(_FBTableBase.COL_MEASURE_TIME, now.ToString("HHmmss"));
            query.Value(FbtAFMSDiscAttrRatingCurve.COL_DIS_VER, config.DisVer);
            query.Value(FbtAFMSDiscAttrRatingCurve.COL_COEFF_COUNT, config.Coefficients.Count);
            query.Value(FbtAFMSDiscAttrRatingCurve.COL_DIS_ATTR, JsonSerializer.Serialize(config.Coefficients));
            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            db.Execute(query, out string error);
            return error;
        }

        private static AFMSSectionPanel CreateDetailPanel(out Label curveName, out AFMSDataGridView grid)
        {
            AFMSSectionPanel panel = new AFMSSectionPanel
            {
                Dock = DockStyle.Fill, SectionStyle = AFMSSectionStyle.FilledHeader,
                HeaderText = "수위-유량 관계곡선 상세", HeaderHeight = 38,
                HeaderBackColor = DllColorHelper.HexToColor("#F5F8F6"),
                HeaderColor = DllColorHelper.HexToColor("#244B37"),
                HeaderLineColor = Color.FromArgb(225, 229, 235)
            };
            TableLayoutPanel layout = panel.ContentLayout;
            layout.ColumnStyles.Clear(); layout.RowStyles.Clear();
            layout.ColumnCount = 1; layout.RowCount = 3; layout.Padding = new Padding(14, 12, 14, 14);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            curveName = new Label { Dock = DockStyle.Fill, Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 10F, FontStyle.Bold), ForeColor = DllColorHelper.HexToColor("#138052"), TextAlign = ContentAlignment.MiddleLeft };
            Label formula = new Label { Dock = DockStyle.Fill, Text = "Q = a(h − h₀)ᵇ", Font = new Font("Cambria Math", 17F, FontStyle.Italic), TextAlign = ContentAlignment.MiddleCenter };
            grid = new AFMSDataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true, AllowUserToAddRows = false, AFMSHeaderHeight = 34, AFMSRowHeight = 32, BorderRadius = 6 };
            AddDetailColumn(grid, "NO", "No.", 12F);
            AddDetailColumn(grid, "MAX_H", "최대 수위", 26F, "0.000");
            AddDetailColumn(grid, "A", "a", 20F, "0.0000");
            AddDetailColumn(grid, "B", "b", 20F, "0.0000");
            AddDetailColumn(grid, "H0", "h₀", 22F, "0.000");
            layout.Controls.Add(curveName, 0, 0); layout.Controls.Add(formula, 0, 1); layout.Controls.Add(grid, 0, 2);
            return panel;
        }

        private static void AddDetailColumn(DataGridView grid, string name, string header, float weight, string format = "")
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = weight };
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.DefaultCellStyle.Format = format;
            grid.Columns.Add(column);
        }
    }
}
