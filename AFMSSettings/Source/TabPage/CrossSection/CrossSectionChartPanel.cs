using AFMSDll;
using System.Data;

namespace AFMSSettings
{
    internal sealed class CrossSectionChartPanel : TableLayoutPanel
    {
        private const string HYDRO_NONE = "NONE";
        private const string COL_TRANSECT = "측선";
        private const string COL_AREA = "단면적(m²)";

        private sealed class HydroComboItem
        {
            public string Name { get; }
            public string TransectJson { get; }

            public HydroComboItem(string name, string transectJson)
            {
                Name = name;
                TransectJson = transectJson;
            }

            public override string ToString() => Name;
        }

        private readonly AFMSAreaChart _chart;
        private readonly AFMSDataGridView _areaGrid;
        private readonly AFMSComboBox _hydroCombo;
        private readonly AFMSNumberBox _waterLevel;
        private readonly AFMSLabel _totalArea;
        private readonly AFMSButtonGroup _chartRatio;
        private readonly TransectCollection _selectedTransects = new();

        public CrossSectionChartPanel()
        {
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            ColumnCount = 2;
            RowCount = 2;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));

            _chart = new AFMSAreaChart { Dock = DockStyle.Fill };
            AFMSPanel chartPanel = new AFMSPanel { Dock = DockStyle.Fill };
            chartPanel.Controls.Add(_chart);

            _hydroCombo = new AFMSComboBox
            {
                Dock = DockStyle.Fill,
                BorderColor = DllColorHelper.HexToColor("#017D43"),
                BorderRadius = 6,
                BorderThickness = 1.5F
            };
            _hydroCombo.SelectedIndexChanged += HydroCombo_SelectedIndexChanged;

            _areaGrid = CreateAreaGrid();

            Control areaPanel = CreateAreaPanel();
            Control commandPanel = CreateCommandPanel(out _waterLevel, out _totalArea, out _chartRatio);

            Controls.Add(chartPanel, 0, 0);
            Controls.Add(areaPanel, 1, 0);
            SetRowSpan(areaPanel, 2);
            Controls.Add(commandPanel, 0, 1);
        }

        public void SetData(CrossSectionPointCollection? data)
        {
            _chart.SetData(data);

            if (data?.WaterLevel.HasValue == true) _waterLevel.SetValue(data.WaterLevel.Value);
            else _waterLevel.Text = string.Empty;

            UpdateAreas();
        }

        public void RefreshHydroMeters()
        {
            const string COL_HYDRO_ID = "__HYDRO_ID";
            const string COL_HYDRO_NAME = "__HYDRO_NAME";
            const string COL_HYDRO_NO = "__HYDRO_NO";
            const string COL_TRANSECTS = "__TRANSECTS";

            _hydroCombo.ClearItems();
            _hydroCombo.Items.Add(HYDRO_NONE);

            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.AsAlias(FbtAFMSHydroMeter.COL_ID, COL_HYDRO_ID);
            query.AsAlias(FbtAFMSHydroMeter.COL_DEVICE_NAME, COL_HYDRO_NAME);
            query.AsAlias(FbtAFMSHydroMeter.COL_DEVICE_NO, COL_HYDRO_NO);
            query.AsAliasB(FbtAFMSHydroTransect.COL_DISTANCE_DATAS, COL_TRANSECTS);
            query.LeftJoinB.Table = FbtAFMSHydroTransect.TABLE_NAME;
            query.LeftJoinB.Add(FbtAFMSHydroTransect.COL_HYDRO_ID, "=", FbtAFMSHydroMeter.COL_ID);
            query.LeftJoinB.AddRaw(
                $"B.{FbtAFMSHydroTransect.COL_ID} = (" +
                $"SELECT MAX(B2.{FbtAFMSHydroTransect.COL_ID}) " +
                $"FROM {FbtAFMSHydroTransect.TABLE_NAME} B2 " +
                $"WHERE B2.{FbtAFMSHydroTransect.COL_HYDRO_ID} = A.{FbtAFMSHydroMeter.COL_ID})");
            query.OrderBy(FbtAFMSHydroMeter.COL_ID);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);

            if (string.IsNullOrEmpty(error))
            {
                foreach (DataRow row in table.Rows)
                {
                    if (row[COL_HYDRO_ID] == DBNull.Value || row[COL_TRANSECTS] == DBNull.Value) continue;

                    string json = Convert.ToString(row[COL_TRANSECTS])?.Trim() ?? string.Empty;
                    if (!TransectBuilder.TryBuild(json, out _)) continue;

                    string deviceName = Convert.ToString(row[COL_HYDRO_NAME])?.Trim() ?? "유속계";
                    string deviceNo = row[COL_HYDRO_NO] == DBNull.Value
                        ? string.Empty
                        : $" #{Convert.ToInt32(row[COL_HYDRO_NO])}";
                    _hydroCombo.Items.Add(new HydroComboItem($"{deviceName}{deviceNo}", json));
                }
            }

            _hydroCombo.SelectedIndex = 0;
        }

        private AFMSDataGridView CreateAreaGrid()
        {
            AFMSDataGridView grid = new AFMSDataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false
            };
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_TRANSECT,
                HeaderText = COL_TRANSECT,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_AREA,
                HeaderText = COL_AREA,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });
            return grid;
        }

        private Control CreateAreaPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 0, 0),
                ColumnCount = 1,
                RowCount = 1
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(_areaGrid, 0, 0);
            return panel;
        }

        private Control CreateCommandPanel(
            out AFMSNumberBox waterLevel, out AFMSLabel totalArea, out AFMSButtonGroup chartRatio)
        {
            waterLevel = new AFMSNumberBox
            {
                Dock = DockStyle.Fill,
                AllowNegative = true,
                InputType = AFMSNumericInputType.Double,
                Hint = "수위(m)"
            };
            waterLevel.TextChanged += WaterLevel_TextChanged;

            totalArea = new AFMSLabel
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Bold = true,
                FontSize = 12F,
                NormalForeColor = DllColorHelper.HexToColor("#017D43"),
                Text = "전체 단면적  0.00 m²"
            };

            chartRatio = new AFMSButtonGroup { Dock = DockStyle.Fill };
            chartRatio.AddButton("화면 맞춤", AFMSChartAspectMode.Fit);
            chartRatio.AddButton("X·Y 동일 비율", AFMSChartAspectMode.EqualScale);
            chartRatio.SelectedIndexChanged += ChartRatio_SelectedIndexChanged;
            chartRatio.SelectedIndex = 0;

            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 0),
                ColumnCount = 3,
                RowCount = 1
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            panel.Controls.Add(CreateSectionPanel("수위 / 전체 단면적", waterLevel, totalArea), 0, 0);
            panel.Controls.Add(CreateSectionPanel("유속계 선택", _hydroCombo), 1, 0);
            panel.Controls.Add(CreateSectionPanel("차트 비율", chartRatio), 2, 0);
            return panel;
        }

        private static AFMSSectionPanel CreateSectionPanel(string title, params Control[] controls)
        {
            AFMSSectionPanel section = new AFMSSectionPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 5, 0),
                HeaderText = title,
                SectionStyle = AFMSSectionStyle.OutlineTitle,
                HeaderHeight = 32,
                BorderRadius = 6,
                BorderThickness = 1F,
                ContentPadding = new Padding(6)
            };

            TableLayoutPanel content = section.ContentLayout;
            content.Controls.Clear();
            content.ColumnStyles.Clear();
            content.RowStyles.Clear();
            content.ColumnCount = Math.Max(1, controls.Length);
            content.RowCount = 1;
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            for (int i = 0; i < content.ColumnCount; i++)
                content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / content.ColumnCount));
            for (int i = 0; i < controls.Length; i++)
            {
                controls[i].Margin = new Padding(i == 0 ? 0 : 3, 0, i == controls.Length - 1 ? 0 : 3, 0);
                content.Controls.Add(controls[i], i, 0);
            }

            return section;
        }

        private void WaterLevel_TextChanged(object? sender, EventArgs e)
        {
            _chart.Data.WaterLevel = _waterLevel.TryGetDouble(out double waterLevel) ? waterLevel : null;
            _chart.Invalidate();
            UpdateAreas();
        }

        private void ChartRatio_SelectedIndexChanged(object? sender, EventArgs e)
        {
            AFMSChartAspectMode mode = _chartRatio.SelectedValue is AFMSChartAspectMode selected
                ? selected
                : AFMSChartAspectMode.Fit;
            _chart.SetAspectMode(mode);
        }

        private void HydroCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_hydroCombo.SelectedItem is not HydroComboItem hydro ||
                !TransectBuilder.TryBuild(hydro.TransectJson, out TransectCollection transects))
            {
                _selectedTransects.Clear();
                _chart.ClearTransectMarkers();
                UpdateAreas();
                return;
            }

            _selectedTransects.Clear();
            _selectedTransects.AddRange(transects);
            _chart.SetTransectMarkers(transects.Select(transect =>
                new AFMSChartTransectMarker(transect.No, transect.CenterLeftBankDistance)));
            UpdateAreas();
        }

        private void UpdateAreas()
        {
            _areaGrid.Rows.Clear();
            CrossSectionPointCollection data = _chart.Data;

            if (!data.WaterLevel.HasValue || data.Count < 2)
            {
                _totalArea.Text = "전체 단면적  0.00 m²";
                return;
            }

            double totalArea = data.Area;
            _totalArea.Text = $"{totalArea:N2} m²";

            if (_selectedTransects.Count == 0) return;

            try
            {
                _selectedTransects.CalculateSectionAreas(data, data.WaterLevel.Value);
                double sum = _selectedTransects.Sum(transect => transect.SectionArea);
                int summaryIndex = _areaGrid.Rows.Add("합계", sum);
                _areaGrid.Rows[summaryIndex].DefaultCellStyle.BackColor = Color.FromArgb(235, 248, 243);
                _areaGrid.Rows[summaryIndex].DefaultCellStyle.ForeColor = DllColorHelper.HexToColor("#017D43");

                foreach (Transect transect in _selectedTransects.OrderBy(item => item.No))
                    _areaGrid.Rows.Add(transect.No, transect.SectionArea);
            }
            catch (ArgumentException ex)
            {
            }
        }

    }
}
