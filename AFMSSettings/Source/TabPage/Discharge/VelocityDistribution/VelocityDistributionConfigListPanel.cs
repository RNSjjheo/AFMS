using AFMSDll;
using System.Data;
using System.Text.Json;

namespace AFMSSettings
{
    internal sealed class VelocityDistributionConfigListPanel : UserControl
    {
        public event EventHandler<IReadOnlyList<int>>? SelectedTransectNosChanged;

        private const string COL_NO = "NO";
        
        private int _hydroId = -1;
        public AFMSDataGridView Grid { get; }

        public VelocityDistributionConfigListPanel()
        {
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
            BackColor = Color.White;

            Grid = new AFMSDataGridView();
            Grid.Dock = DockStyle.Fill;
            Grid.Margin = Padding.Empty;
            Grid.AFMSHeaderHeight = 42;
            Grid.AFMSRowHeight = 54;
            Grid.BorderRadius = 8;
            Grid.DataBindingComplete += Grid_DataBindingComplete;
            Grid.SelectionChanged += Grid_SelectionChanged;
            Controls.Add(Grid);
        }


        public void SetHydroId(int hydroId)
        {
            _hydroId = hydroId;
        }

        public string LoadData()
        {
            if (_hydroId < 0)
            {
                Grid.DataSource = null;
                SelectedTransectNosChanged?.Invoke(this, Array.Empty<int>());
                return string.Empty;
            }

            QueryBuilderSelect query = new();
            query.Table = FbtAFMSDiscAttrVelocityDistribution.TABLE_NAME;
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_ID);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_DIS_VER);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_HYDRO_ID);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_PHI);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_HORIZONTAL_GRID_M);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_VERTICAL_GRID_M);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_MAX_VELOCITY_DEPTH_RATIO);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_MIN_POSITIVE_MEASUREMENTS);
            query.Add(FbtAFMSDiscAttrVelocityDistribution.COL_TRANSECT_NOS);
            query.Where(FbtAFMSDiscAttrVelocityDistribution.COL_HYDRO_ID, "=", _hydroId);
            query.OrderBy(FbtAFMSDiscAttrVelocityDistribution.COL_ID);

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) return error;

            table.AddRowNo(COL_NO);
            Grid.DataSource = table;
            return string.Empty;
        }

        public string SaveConfig(FormDischargeVelocityDistribution.VelocityDistributionConfig config)
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
            if (!string.IsNullOrEmpty(error)) return error;
            return DischargeMethodConfigStore.Save(
                MeasurementDeviceType.VelocityMeter, config.HydroId, DischargeMethod.VeloDist,
                config, "유속분포법 설정");
        }

        public void ClearSelection()
        {
            Grid.ClearSelection();
            Grid.CurrentCell = null;
            SelectedTransectNosChanged?.Invoke(this, Array.Empty<int>());
        }

        private void Grid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            SetColumnVisible(FbtAFMSDiscAttrVelocityDistribution.COL_ID, false);
            SetColumnVisible(FbtAFMSDiscAttrVelocityDistribution.COL_DIS_VER, false);
            SetColumnVisible(FbtAFMSDiscAttrVelocityDistribution.COL_HYDRO_ID, false);
            SetColumnVisible(FbtAFMSDiscAttrVelocityDistribution.COL_TRANSECT_NOS, false);
            SetColumnStyle(COL_NO, "No.", 9F);
            SetColumnStyle(FbtAFMSDiscAttrVelocityDistribution.COL_PHI, "φ", 10F, "0.000");
            SetColumnStyle(FbtAFMSDiscAttrVelocityDistribution.COL_HORIZONTAL_GRID_M, "횡격자", 10F, "0.00");
            SetColumnStyle(FbtAFMSDiscAttrVelocityDistribution.COL_VERTICAL_GRID_M, "종격자", 10F, "0.00");
            SetColumnStyle(FbtAFMSDiscAttrVelocityDistribution.COL_MAX_VELOCITY_DEPTH_RATIO, "최대유속 수심비", 13F, "0.00");
            SetColumnStyle(FbtAFMSDiscAttrVelocityDistribution.COL_MIN_POSITIVE_MEASUREMENTS, "최소 측선", 9F);
            ClearSelection();
        }

        private void Grid_SelectionChanged(object? sender, EventArgs e)
        {
            if (Grid.CurrentRow?.DataBoundItem is not DataRowView rowView)
            {
                SelectedTransectNosChanged?.Invoke(this, Array.Empty<int>());
                return;
            }

            string json = rowView.Row[FbtAFMSDiscAttrVelocityDistribution.COL_TRANSECT_NOS].ToText();
            try
            {
                List<int> values = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
                SelectedTransectNosChanged?.Invoke(this, values);
            }
            catch (JsonException)
            {
                SelectedTransectNosChanged?.Invoke(this, Array.Empty<int>());
            }
        }

        private void SetColumnVisible(string name, bool visible)
        {
            if (Grid.Columns.Contains(name)) Grid.Columns[name].Visible = visible;
        }

        private void SetColumnStyle(string name, string headerText, float fillWeight, string format = "")
        {
            if (!Grid.Columns.Contains(name)) return;
            DataGridViewColumn column = Grid.Columns[name];
            column.HeaderText = headerText;
            column.FillWeight = fillWeight;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (!string.IsNullOrEmpty(format)) column.DefaultCellStyle.Format = format;
        }
    }
}
