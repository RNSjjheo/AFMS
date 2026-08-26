using AFMSDll;
using AFMSSettings.Source.Form.Discharge;
using System.Data;
using System.Drawing;
using System.Text.Json;

namespace AFMSSettings
{
    internal sealed class TabDiscVelocityDistribution : _TabDischargeBase
    {
        private int _hydroId = -1;
        private readonly AFMSDataGridView _uiGridTransects;
        private TransectCollection _transects = new();

        public TabDiscVelocityDistribution() : base(true)
        {
            Text = "유속분포법";
            BackColor = Color.White;
            uiTpMain.ColumnStyles[0].Width = 68F;
            uiTpMain.ColumnStyles[1].Width = 32F;
            _uiGridTransects = CreateTransectGrid();
            CtlSub = CreateTransectPanel(_uiGridTransects);
            uiGridMain.SelectionChanged += UiGridMain_SelectionChanged;
        }

        public void SetHydroId(int hydroId)
        {
            if (_hydroId == hydroId) return;
            _hydroId = hydroId;
            string error = LoadData();
            if (!string.IsNullOrEmpty(error)) MessageBox.Show(error, "유속분포 설정 조회 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public string LoadData()
        {
            if (_hydroId < 0)
            {
                uiGridMain.DataSource = null;
                _uiGridTransects.Rows.Clear();
                _transects = new TransectCollection();
                return string.Empty;
            }

            string transectError = LoadTransects();
            if (!string.IsNullOrEmpty(transectError)) return transectError;

            QueryBuilderSelect query = new();
            query.Table = FbtAFMSDiscAttrVelocityDistribution.TABLE_NAME;
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_ID);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_DIS_VER);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_HYDRO_ID);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_PHI);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_HORIZONTAL_GRID_M);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_VERTICAL_GRID_M);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_MAX_VELOCITY_DEPTH_RATIO);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_FIT_MODE);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_MIN_POSITIVE_MEASUREMENTS);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_FLOW_CENTER_X);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_BETA_LEFT);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_BETA_RIGHT);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_TRANSECT_NOS);
            query.Where(FbtAFMSDiscAttrVelocityDistribution.COL_HYDRO_ID, "=", _hydroId);
            query.OrderBy(FbtAFMSDiscAttrVelocityDistribution.COL_ID);

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) return error;
            table.AddRowNo(COL_NO);
            uiGridMain.DataSource = table;
            return string.Empty;
        }

        public override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is not AFMSDataGridView grid) return;
            SetColumnVisible(grid, FbtAFMSDiscAttrVelocityDistribution.COL_ID, false);
            SetColumnVisible(grid, FbtAFMSDiscAttrVelocityDistribution.COL_DIS_VER, false);
            SetColumnVisible(grid, FbtAFMSDiscAttrVelocityDistribution.COL_HYDRO_ID, false);
            SetColumnVisible(grid, FbtAFMSDiscAttrVelocityDistribution.COL_TRANSECT_NOS, false);
            SetColumnStyle(grid, COL_NO, "No.", 9F);
            SetColumnStyle(grid, FbtAFMSDiscAttrVelocityDistribution.COL_PHI, "φ", 10F, "0.000");
            SetColumnStyle(grid, FbtAFMSDiscAttrVelocityDistribution.COL_HORIZONTAL_GRID_M, "횡격자", 10F, "0.00");
            SetColumnStyle(grid, FbtAFMSDiscAttrVelocityDistribution.COL_VERTICAL_GRID_M, "종격자", 10F, "0.00");
            SetColumnStyle(grid, FbtAFMSDiscAttrVelocityDistribution.COL_MAX_VELOCITY_DEPTH_RATIO, "최대유속 수심비", 13F, "0.00");
            SetColumnStyle(grid, FbtAFMSDiscAttrVelocityDistribution.COL_FIT_MODE, "적합 방식", 10F);
            SetColumnStyle(grid, FbtAFMSDiscAttrVelocityDistribution.COL_MIN_POSITIVE_MEASUREMENTS, "최소 측선", 9F);
            SetColumnStyle(grid, FbtAFMSDiscAttrVelocityDistribution.COL_FLOW_CENTER_X, "흐름 중심", 10F, "0.00");
            SetColumnStyle(grid, FbtAFMSDiscAttrVelocityDistribution.COL_BETA_LEFT, "좌 β", 8F, "0.00");
            SetColumnStyle(grid, FbtAFMSDiscAttrVelocityDistribution.COL_BETA_RIGHT, "우 β", 8F, "0.00");
            grid.ClearSelection();
            grid.CurrentCell = null;
            _uiGridTransects.Rows.Clear();
        }

        protected override void UiButtonInput_Click(object? sender, EventArgs e)
        {
            if (_hydroId < 0)
            {
                MessageBox.Show("유속계를 먼저 선택해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FormDischargeVelocityDistribution form = new();
            form.HydroId = _hydroId;
            form.SaveHandler = SaveConfig;
            if (form.ShowDialog(FindForm()) != DialogResult.OK || form.ResultConfig == null) return;
            string error = LoadData();
            if (!string.IsNullOrEmpty(error)) MessageBox.Show(error, "유속분포 설정 조회 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected override void _TabDischargeBase_Enter(object? sender, EventArgs e) => LoadData();

        private static string SaveConfig(FormDischargeVelocityDistribution.VelocityDistributionConfig config)
        {
            DateTime now = DateTime.Now;
            QueryBuilderInsert query = new();
            query.Table = FbtAFMSDiscAttrVelocityDistribution.TABLE_NAME;
            query.AutoIncrement = FbtAFMSDiscAttrVelocityDistribution.COL_ID;
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_MEASURE_DATE, now.ToString("yyyyMMdd"));
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_MEASURE_TIME, now.ToString("HHmmss"));
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_DIS_VER, config.DisVer);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_HYDRO_ID, config.HydroId);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_PHI, config.Phi);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_HORIZONTAL_GRID_M, config.HorizontalGridM);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_VERTICAL_GRID_M, config.VerticalGridM);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_MAX_VELOCITY_DEPTH_RATIO, config.MaxVelocityDepthRatio);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_FIT_MODE, (int)config.FitMode);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_MIN_POSITIVE_MEASUREMENTS, config.MinimumPositiveMeasurements);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_FLOW_CENTER_X, config.FlowCenterX);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_BETA_LEFT, config.BetaLeft);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_BETA_RIGHT, config.BetaRight);
            query.Value(FbtAFMSDiscAttrVelocityDistribution.COL_TRANSECT_NOS, JsonSerializer.Serialize(config.TransectNos));

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            db.Execute(query, out string error);
            return error;
        }

        private string LoadTransects()
        {
            _transects = new TransectCollection();
            if (_hydroId < 0) return string.Empty;

            QueryBuilderSelect query = new();
            query.Table = FbtAFMSHydroTransect.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSHydroTransect.COL_DISTANCE_DATAS);
            query.Where(FbtAFMSHydroTransect.COL_HYDRO_ID, "=", _hydroId);
            query.OrderByDesc(FbtAFMSHydroTransect.COL_ID);

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) return error;
            if (table.Rows.Count == 0) return string.Empty;

            string json = table.Rows[0][FbtAFMSHydroTransect.COL_DISTANCE_DATAS].ToText();
            if (!string.IsNullOrEmpty(json) && !TransectBuilder.TryBuild(json, out _transects))
                return "측선 설정을 읽을 수 없습니다.";
            return string.Empty;
        }

        private void UiGridMain_SelectionChanged(object? sender, EventArgs e)
        {
            _uiGridTransects.Rows.Clear();
            if (uiGridMain.CurrentRow?.DataBoundItem is not DataRowView rowView) return;

            string json = rowView.Row[FbtAFMSDiscAttrVelocityDistribution.COL_TRANSECT_NOS].ToText();
            List<int>? selectedNos;
            try
            {
                selectedNos = JsonSerializer.Deserialize<List<int>>(json);
            }
            catch (JsonException)
            {
                return;
            }

            if (selectedNos == null) return;
            foreach (int no in selectedNos)
            {
                Transect? transect = _transects.FirstOrDefault(item => item.No == no);
                _uiGridTransects.Rows.Add(no, transect == null ? "-" : $"{transect.CenterLeftBankDistance:0.##} m");
            }
            _uiGridTransects.ClearSelection();
        }

        private static AFMSDataGridView CreateTransectGrid()
        {
            AFMSDataGridView grid = new();
            grid.Dock = DockStyle.Fill;
            grid.Margin = Padding.Empty;
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ShowSelectedRowHighlight = false;
            grid.AFMSHeaderHeight = 36;
            grid.AFMSRowHeight = 38;
            grid.BorderRadius = 6;

            DataGridViewTextBoxColumn noColumn = new();
            noColumn.HeaderText = "측선 번호";
            noColumn.FillWeight = 45F;
            noColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DataGridViewTextBoxColumn distanceColumn = new();
            distanceColumn.HeaderText = "거리";
            distanceColumn.FillWeight = 55F;
            distanceColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns.Add(noColumn);
            grid.Columns.Add(distanceColumn);
            return grid;
        }

        private static Control CreateTransectPanel(AFMSDataGridView grid)
        {
            AFMSSectionPanel panel = new();
            panel.Dock = DockStyle.Fill;
            panel.SectionStyle = AFMSSectionStyle.FilledHeader;
            panel.HeaderText = "선택된 운영 측선";
            panel.HeaderHeight = 38;
            panel.HeaderBackColor = DllColorHelper.HexToColor("#F5F8F6");
            panel.HeaderColor = DllColorHelper.HexToColor("#244B37");
            panel.ContentLayout.Controls.Add(grid);
            return panel;
        }
    }
}
