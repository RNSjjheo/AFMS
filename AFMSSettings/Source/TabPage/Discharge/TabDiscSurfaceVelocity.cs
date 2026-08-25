using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Drawing;
using System.Windows.Forms;
using AFMSSettings.Source.Form.Discharge;

namespace AFMSSettings
{
    public class TabDiscSurfaceVelocity : _TabDischargeBase
    {
        private const string COL_COEFF_COUNT = "COEFF_COUNT";

        public sealed class SurfaceVelocityCoefficient
        {
            public double? MaxVi { get; set; }
            public double? A { get; set; }
            public double? C { get; set; }
        }

        public sealed class SurfaceVelocityConfig
        {
            public int Id { get; set; } = -1;
            public int DisVer { get; set; }
            public int HydroId { get; set; } = -1;
            public int CellMin { get; set; }
            public int CellMax { get; set; }
            public int CoeffCount { get; set; }
            public double? UcertVst { get; set; }
            public double? UcertVindex { get; set; }
            public List<SurfaceVelocityCoefficient> Coefficients { get; } = new List<SurfaceVelocityCoefficient>();
        }

        private int _hydroId = -1;

        private AFMSSectionPanel uiPanelDetail;
        private AFMSDataGridView uiGridCoefficient;
        private Panel uiPanelExample;

        public event Action<SurfaceVelocityConfig>? ConfigInputCompleted;

        public TabDiscSurfaceVelocity()
            : base(true)
        {
            Text = "지표유속법";
            BackColor = Color.White;

            SetupListGrid();
            SetupDetailPanel();
            ClearDetail();
        }

        private void SetupListGrid()
        {
            uiGridMain.AutoGenerateColumns = true;
            uiGridMain.Columns.Clear();
            uiGridMain.AFMSHeaderHeight = 24;
            uiGridMain.AFMSRowHeight = 34;
            uiGridMain.BorderRadius = 6;
            uiGridMain.MergedHeaderLineColor = Color.FromArgb(245, 246, 248);
            uiGridMain.MergedHeaderLineThickness = 0.5F;
            uiGridMain.DataBindingComplete += BindingComplete;
            uiGridMain.SelectionChanged += UiGridList_SelectionChanged;

            CtlMain = uiGridMain;
        }

        private void SetupDetailPanel()
        {
            uiPanelDetail = CreateCategoryPanel("지표유속 설정 상세");
            uiPanelDetail.Padding = Padding.Empty;
            uiPanelDetail.Margin = Padding.Empty;

            uiPanelExample = new Panel();
            uiPanelExample.Dock = DockStyle.Fill;
            uiPanelExample.Margin = Padding.Empty;
            uiPanelExample.Padding = Padding.Empty;
            uiPanelExample.BackColor = Color.Transparent;

            SetupCoefficientGrid();

            TableLayoutPanel layout = uiPanelDetail.ContentLayout;
            layout.ColumnStyles.Clear();
            layout.RowStyles.Clear();
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.Padding = new Padding(14, 12, 14, 14);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            layout.Controls.Add(uiPanelExample, 0, 0);
            layout.Controls.Add(uiGridCoefficient, 0, 1);

            CtlSub = uiPanelDetail;
        }

        private void SetupCoefficientGrid()
        {
            uiGridCoefficient = CreateGrid(false);
            uiGridCoefficient.AutoGenerateColumns = false;

            DataGridViewTextBoxColumn colNo = new DataGridViewTextBoxColumn();
            colNo.Name = "NO";
            colNo.HeaderText = "No.";
            colNo.FillWeight = 18F;
            colNo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            DataGridViewTextBoxColumn colMaxVi = new DataGridViewTextBoxColumn();
            colMaxVi.Name = "MAX_VI";
            colMaxVi.HeaderText = QSurfaceVelo.VER1_ATTR_NODE1;
            colMaxVi.FillWeight = 28F;
            colMaxVi.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colMaxVi.DefaultCellStyle.Format = "0.00";

            DataGridViewTextBoxColumn colA = new DataGridViewTextBoxColumn();
            colA.Name = "COEFF_A";
            colA.HeaderText = QSurfaceVelo.VER1_ATTR_NODE2;
            colA.FillWeight = 27F;
            colA.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colA.DefaultCellStyle.Format = "0.00";

            DataGridViewTextBoxColumn colC = new DataGridViewTextBoxColumn();
            colC.Name = "COEFF_C";
            colC.HeaderText = QSurfaceVelo.VER1_ATTR_NODE3;
            colC.FillWeight = 27F;
            colC.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colC.DefaultCellStyle.Format = "0.00";

            uiGridCoefficient.Columns.Add(colNo);
            uiGridCoefficient.Columns.Add(colMaxVi);
            uiGridCoefficient.Columns.Add(colA);
            uiGridCoefficient.Columns.Add(colC);
        }

        public override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is not AFMSDataGridView grid) return;

            grid.ClearMergedHeaders();

            SetColumnVisible(grid, FbtAFMSDiscAttrSurfaceVelo.COL_ID, false);
            SetColumnVisible(grid, FbtAFMSDiscAttrSurfaceVelo.COL_DIS_VER, false);
            SetColumnVisible(grid, FbtAFMSDiscAttrSurfaceVelo.COL_HYDRO_ID, false);
            SetColumnVisible(grid, FbtAFMSDiscAttrSurfaceVelo.COL_DIS_ATTR, false);

            SetColumnStyle(grid, COL_NO, "No.", 12F);
            SetColumnStyle(grid, FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MIN, "MIN", 18F);
            SetColumnStyle(grid, FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MAX, "MAX", 18F);
            SetColumnStyle(grid, FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_ST, "Vst", 18F, "0.00");
            SetColumnStyle(grid, FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_INDEX, "Vindex", 20F, "0.00");
            SetColumnStyle(grid, COL_COEFF_COUNT, "구간수", 14F);

            if (grid.Columns.Contains(FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MIN) && grid.Columns.Contains(FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MAX))
                grid.AddMergedHeader("분석범위", FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MIN, FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MAX);

            grid.ClearSelection();
            grid.CurrentCell = null;
        }



        private void UiGridList_SelectionChanged(object? sender, EventArgs e)
        {
            if (uiGridMain.CurrentRow?.DataBoundItem is not DataRowView rowView)
            {
                ClearDetail();
                return;
            }

            ShowDetail(CreateConfig(rowView.Row));
        }

        protected override void UiButtonInput_Click(object? sender, EventArgs e)
        {
            if (_hydroId < 0)
            {
                MessageBox.Show("유속계를 먼저 선택해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FormDischargeSurfaceVelo form = new FormDischargeSurfaceVelo();
            form.HydroId = _hydroId;
            form.SaveHandler = SaveConfig;

            if (form.ShowDialog(FindForm()) != DialogResult.OK || form.ResultConfig == null) return;

            SurfaceVelocityConfig config = form.ResultConfig;
            string error = LoadData();
            if (!string.IsNullOrEmpty(error)) MessageBox.Show(error, "지표유속 설정 조회 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ConfigInputCompleted?.Invoke(config);
        }

        public int HydroId => _hydroId;

        public void ClearSelectionAndDetail()
        {
            uiGridMain.ClearSelection();
            uiGridMain.CurrentCell = null;
            ClearDetail();
        }

        public void SetHydroId(int hydroId)
        {
            if (_hydroId == hydroId) return;

            _hydroId = hydroId;
            string error = LoadData();
            if (!string.IsNullOrEmpty(error)) MessageBox.Show(error, "지표유속 설정 조회 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public string LoadData()
        {
            if (_hydroId < 0)
            {
                uiGridMain.DataSource = null;
                ClearDetail();
                return string.Empty;
            }

            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSDiscAttrSurfaceVelo.TABLE_NAME;

            query.Add(FbtAFMSDiscAttrSurfaceVelo.COL_ID);
            query.Add(FbtAFMSDiscAttrSurfaceVelo.COL_DIS_VER);
            query.Add(FbtAFMSDiscAttrSurfaceVelo.COL_HYDRO_ID);
            query.Add(FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MIN);
            query.Add(FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MAX);
            query.Add(FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_ST);
            query.Add(FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_INDEX);
            query.AsAlias(FbtAFMSDiscAttrSurfaceVelo.COL_COEFF_COUNT, COL_COEFF_COUNT);
            query.Add(FbtAFMSDiscAttrSurfaceVelo.COL_DIS_ATTR);

            query.Where(FbtAFMSDiscAttrSurfaceVelo.COL_HYDRO_ID, "=", _hydroId);
            query.OrderBy(FbtAFMSDiscAttrSurfaceVelo.COL_ID);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);

            if (!string.IsNullOrEmpty(error)) return error;

            table.AddRowNo(COL_NO);

            uiGridMain.DataSource = table.AddRowNo(COL_NO);

            return string.Empty;

        }

        private static SurfaceVelocityConfig CreateConfig(DataRow row)
        {
            SurfaceVelocityConfig config = new SurfaceVelocityConfig();
            config.Id = Convert.ToInt32(row[FbtAFMSDiscAttrSurfaceVelo.COL_ID]);
            config.DisVer = Convert.ToInt32(row[FbtAFMSDiscAttrSurfaceVelo.COL_DIS_VER]);
            config.HydroId = Convert.ToInt32(row[FbtAFMSDiscAttrSurfaceVelo.COL_HYDRO_ID]);
            config.CellMin = Convert.ToInt32(row[FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MIN]);
            config.CellMax = Convert.ToInt32(row[FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MAX]);
            config.UcertVst = row[FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_ST] == DBNull.Value ? null : Convert.ToDouble(row[FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_ST]);
            config.UcertVindex = row[FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_INDEX] == DBNull.Value ? null : Convert.ToDouble(row[FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_INDEX]);

            LoadAttributes(config, GetString(row, FbtAFMSDiscAttrSurfaceVelo.COL_DIS_ATTR));
            return config;
        }

        private static string GetString(DataRow row, string columnName)
        {
            if (row[columnName] == DBNull.Value) return string.Empty;
            return Convert.ToString(row[columnName])?.Trim() ?? string.Empty;
        }

        private string SaveConfig(SurfaceVelocityConfig config)
        {
            DateTime now = DateTime.Now;

            QueryBuilderInsert query = new QueryBuilderInsert();
            query.Table = FbtAFMSDiscAttrSurfaceVelo.TABLE_NAME;
            query.AutoIncrement = FbtAFMSDiscAttrSurfaceVelo.COL_ID;

            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_MEASURE_DATE, now.ToString("yyyyMMdd"));
            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_MEASURE_TIME, now.ToString("HHmmss"));
            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_DIS_VER, config.DisVer);
            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_HYDRO_ID, config.HydroId);
            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MIN, config.CellMin);
            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_CELL_RANGE_MAX, config.CellMax);
            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_ST, config.UcertVst);
            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_UCERT_V_INDEX, config.UcertVindex);
            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_COEFF_COUNT, config.CoeffCount);
            query.Value(FbtAFMSDiscAttrSurfaceVelo.COL_DIS_ATTR, SerializeAttributes(config));

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            db.Execute(query, out string error);

            return error;
        }

        private static string SerializeAttributes(SurfaceVelocityConfig config)
        {
            List<Dictionary<string, double?>> attrs = new List<Dictionary<string, double?>>();

            foreach (SurfaceVelocityCoefficient coefficient in config.Coefficients)
            {
                attrs.Add(new Dictionary<string, double?>
                {
                    [QSurfaceVelo.VER1_ATTR_NODE1] = coefficient.MaxVi,
                    [QSurfaceVelo.VER1_ATTR_NODE2] = coefficient.A,
                    [QSurfaceVelo.VER1_ATTR_NODE3] = coefficient.C
                });
            }

            return JsonSerializer.Serialize(attrs);
        }

        private static void LoadAttributes(SurfaceVelocityConfig config, string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                List<Dictionary<string, double?>>? attrs = JsonSerializer.Deserialize<List<Dictionary<string, double?>>>(json);
                if (attrs == null) return;

                foreach (Dictionary<string, double?> attr in attrs)
                {
                    attr.TryGetValue(QSurfaceVelo.VER1_ATTR_NODE1, out double? maxVi);
                    attr.TryGetValue(QSurfaceVelo.VER1_ATTR_NODE2, out double? a);
                    attr.TryGetValue(QSurfaceVelo.VER1_ATTR_NODE3, out double? c);
                    config.Coefficients.Add(new SurfaceVelocityCoefficient { MaxVi = maxVi, A = a, C = c });
                }
            }
            catch (JsonException)
            {
            }
        }

        private void ShowDetail(SurfaceVelocityConfig config)
        {
            UpdateExample((DiscVerSurfaceVelo)config.DisVer);
            uiGridCoefficient.Rows.Clear();

            for (int i = 0; i < config.Coefficients.Count; i++)
            {
                SurfaceVelocityCoefficient coefficient = config.Coefficients[i];
                uiGridCoefficient.Rows.Add(i + 1, coefficient.MaxVi, coefficient.A, coefficient.C);
            }

            uiGridCoefficient.ClearSelection();
        }

        private void UpdateExample(DiscVerSurfaceVelo version)
        {
            uiPanelExample.Controls.Clear();

            AFMSMathLabel example = QSurfaceVelo.GetExample(version);
            example.Dock = DockStyle.Fill;
            example.Margin = Padding.Empty;
            example.Padding = Padding.Empty;
            example.TextAlign = ContentAlignment.MiddleCenter;

            uiPanelExample.Controls.Add(example);
        }

        private void ClearDetail()
        {
            uiPanelExample.Controls.Clear();
            uiGridCoefficient.Rows.Clear();
        }

        private static AFMSSectionPanel CreateCategoryPanel(string title)
        {
            AFMSSectionPanel panel = new AFMSSectionPanel();
            panel.Dock = DockStyle.Fill;
            panel.SectionStyle = AFMSSectionStyle.FilledHeader;
            panel.HeaderText = title;
            panel.HeaderHeight = 38;
            panel.HeaderBackColor = DllColorHelper.HexToColor("#F5F8F6");
            panel.HeaderColor = DllColorHelper.HexToColor("#244B37");
            panel.HeaderLineColor = Color.FromArgb(225, 229, 235);
            return panel;
        }

        private static AFMSDataGridView CreateGrid(bool editable)
        {
            AFMSDataGridView grid = new AFMSDataGridView();
            grid.Dock = DockStyle.Fill;
            grid.Margin = Padding.Empty;
            grid.ReadOnly = !editable;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ShowSelectedRowHighlight = true;
            grid.AFMSHeaderHeight = 32;
            grid.AFMSRowHeight = 34;
            grid.BorderRadius = 6;
            grid.BorderThickness = 1F;
            grid.BorderColor = DllColorHelper.HexToColor("#D7DDD9");

            return grid;
        }

        protected override void _TabDischargeBase_Enter(object? sender, EventArgs e)
        {
            LoadData();
        }
    }
}
