using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AFMSSettings
{
    internal class TabCrossSection : _TabBase
    {
        private const string HYDRO_NONE = "NONE";
        private const string COL_AREA_TRANSECT = "측선";
        private const string COL_AREA_VALUE = "단면적(m²)";
        private const string COL_DATA = "단면값";
        private const string COL_INDEX = "번호";
        private const string COL_DATE = "입력일자";
        private const string COL_DESC = "단면설명";
        private const string COL_ZERO_ELEV = "영점표고";
        private const string COL_POINT_CNT = "포인트";

        private sealed class HydroComboItem
        {
            public int Id { get; }
            public string Name { get; }
            public string TransectJson { get; }

            public HydroComboItem(int id, string name, string transectJson)
            {
                Id = id;
                Name = name;
                TransectJson = transectJson;
            }

            public override string ToString() => Name;
        }

        private sealed class TransectJsonData
        {
            [JsonPropertyName("transects")]
            public List<TransectJsonItem> Transects { get; set; } = new();
        }

        private sealed class TransectJsonItem
        {
            [JsonPropertyName("no")]
            public int No { get; set; }

            [JsonPropertyName("distance")]
            public double Distance { get; set; }
        }

        private AFMSDataGridView _uiGrid;
        private AFMSDataGridView _uiAreaGrid;
        private AFMSAreaChart _uiChart;
        private AFMSComboBox _uiHydroCombo;
        private AFMSNumberBox _uiWaterLevel;
        private AFMSLabel _uiTotalArea;
        private AFMSLabel _uiAreaStatus;
        private AFMSButtonGroup _uiChartRatio;
        private readonly TransectCollection _selectedTransects = new();
        private DataTable _uiGridData;
        private AFMSTextBox uiTeFilePath;
        private AFMSTextBox uiTeFileName;
        private AFMSTextBox uiTeFileResult;
        private AFMSNumberBox uiNoZeroLevel;
        private AFMSButton uiBtnSelect;
        private AFMSButton uiBtnInsert;
        private CrossSectionPointCollection? _selectedAreaData;
        private AFMSGuidePanel uiGuide;
        private TableLayoutPanel uiTpRigth;
        private int _lastSelectedId = -1;
        private bool _selectLastArea;

        public TabCrossSection()
        {
            Text = "단면설정";
            Desc = "csv 형태의 단면 자료를 입력합니다";

            SetupMainLayout();
            SetupFileLayout();
        }

        private void SetupMainLayout()
        {
            uiTpRigth = new TableLayoutPanel();
            uiTpRigth.Dock = DockStyle.Fill;
            uiTpRigth.RowStyles.Clear();
            uiTpRigth.ColumnStyles.Clear();
            uiTpRigth.RowCount = 3;
            uiTpRigth.ColumnCount = 1;
            uiTpRigth.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpRigth.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
            uiTpRigth.RowStyles.Add(new RowStyle(SizeType.Absolute, 125F));
            uiTpRigth.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpRigth.Padding = Padding.Empty;
            uiTpRigth.Margin = Padding.Empty;

            _uiChart = new AFMSAreaChart();
            _uiChart.Dock = DockStyle.Fill;

            AFMSPanel chartpanel = new AFMSPanel();
            chartpanel.Dock = DockStyle.Fill;
            chartpanel.Controls.Add(_uiChart);

            _uiGrid = new AFMSDataGridView();
            _uiGrid.Dock = DockStyle.Fill;
            _uiGrid.DataBindingComplete += BindingComplete;
            _uiGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _uiGrid.SelectionChanged += UiGrid_SelectionChanged;

            Control analysisPanel = SetupAnalysisControls();

            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Margin = Padding.Empty;
            mainLayout.Padding = Padding.Empty;
            mainLayout.ColumnCount = 2;
            mainLayout.RowCount = 2;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 105F));
            mainLayout.Controls.Add(chartpanel, 0, 0);
            mainLayout.Controls.Add(analysisPanel, 1, 0);
            mainLayout.SetRowSpan(analysisPanel, 2);

            Control commandPanel = CreateCommandPanel();
            mainLayout.Controls.Add(commandPanel, 0, 1);

            UpdateMapList();
            LoadHydroMeters();

            CtlMain = mainLayout;
            CtlSub = uiTpRigth;
        }

        private Control SetupAnalysisControls()
        {
            _uiHydroCombo = new AFMSComboBox();
            _uiHydroCombo.Dock = DockStyle.Fill;
            _uiHydroCombo.BorderColor = DllColorHelper.HexToColor("#017D43");
            _uiHydroCombo.BorderRadius = 6;
            _uiHydroCombo.BorderThickness = 1.5F;
            _uiHydroCombo.SelectedIndexChanged += HydroCombo_SelectedIndexChanged;

            _uiAreaGrid = new AFMSDataGridView();
            _uiAreaGrid.Dock = DockStyle.Fill;
            _uiAreaGrid.ReadOnly = true;
            _uiAreaGrid.AllowUserToAddRows = false;
            _uiAreaGrid.AllowUserToDeleteRows = false;
            _uiAreaGrid.AutoGenerateColumns = false;
            _uiAreaGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _uiAreaGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_AREA_TRANSECT,
                HeaderText = COL_AREA_TRANSECT,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            });
            _uiAreaGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_AREA_VALUE,
                HeaderText = COL_AREA_VALUE,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });

            Label title = new Label();
            title.Dock = DockStyle.Fill;
            title.Text = "측선별 단면적";
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            title.ForeColor = DllColorHelper.HexToColor("#017D43");

            _uiAreaStatus = new AFMSLabel();
            _uiAreaStatus.Dock = DockStyle.Fill;
            _uiAreaStatus.TextAlign = ContentAlignment.MiddleCenter;
            _uiAreaStatus.Text = "측선 정보 없음";

            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(5, 0, 0, 0);
            panel.ColumnCount = 1;
            panel.RowCount = 3;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            panel.Controls.Add(title, 0, 0);
            panel.Controls.Add(_uiAreaGrid, 0, 1);
            panel.Controls.Add(_uiAreaStatus, 0, 2);
            return panel;
        }

        private Control CreateCommandPanel()
        {
            _uiWaterLevel = new AFMSNumberBox();
            _uiWaterLevel.Dock = DockStyle.Fill;
            _uiWaterLevel.AllowNegative = true;
            _uiWaterLevel.InputType = AFMSNumericInputType.Double;
            _uiWaterLevel.Hint = "수위(m)";
            _uiWaterLevel.TextChanged += WaterLevel_TextChanged;

            _uiTotalArea = new AFMSLabel();
            _uiTotalArea.Dock = DockStyle.Fill;
            _uiTotalArea.TextAlign = ContentAlignment.MiddleCenter;
            _uiTotalArea.Bold = true;
            _uiTotalArea.FontSize = 12F;
            _uiTotalArea.NormalForeColor = DllColorHelper.HexToColor("#017D43");
            _uiTotalArea.Text = "전체 단면적  0.00 m²";

            _uiChartRatio = new AFMSButtonGroup();
            _uiChartRatio.Dock = DockStyle.Fill;
            _uiChartRatio.AddButton("화면 맞춤", AFMSChartAspectMode.Fit);
            _uiChartRatio.AddButton("X·Y 동일 비율", AFMSChartAspectMode.EqualScale);
            _uiChartRatio.SelectedIndexChanged += ChartRatio_SelectedIndexChanged;
            _uiChartRatio.SelectedIndex = 0;

            Control waterPanel = CreateLabeledControl("수위 / 전체 단면적", _uiWaterLevel, _uiTotalArea);
            Control hydroPanel = CreateLabeledControl("유속계 선택", _uiHydroCombo);
            Control ratioPanel = CreateLabeledControl("차트 비율", _uiChartRatio);

            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 5, 0, 0);
            panel.ColumnCount = 3;
            panel.RowCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(waterPanel, 0, 0);
            panel.Controls.Add(hydroPanel, 1, 0);
            panel.Controls.Add(ratioPanel, 2, 0);
            return panel;
        }

        private static Control CreateLabeledControl(string title, params Control[] controls)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.Text = title;
            label.TextAlign = ContentAlignment.MiddleLeft;

            TableLayoutPanel content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.Margin = new Padding(5, 0, 5, 0);
            content.ColumnCount = Math.Max(1, controls.Length);
            content.RowCount = 2;
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            for (int i = 0; i < content.ColumnCount; i++)
                content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / content.ColumnCount));
            content.Controls.Add(label, 0, 0);
            content.SetColumnSpan(label, content.ColumnCount);
            for (int i = 0; i < controls.Length; i++) content.Controls.Add(controls[i], i, 1);
            return content;
        }

        private void SetupFileLayout()
        {
            const float MAIN_ROW_H = 38;

            TableLayoutPanel backlp = new TableLayoutPanel();
            backlp.Dock = DockStyle.Fill;
            backlp.RowStyles.Clear();
            backlp.ColumnStyles.Clear();
            backlp.RowCount = 6;
            backlp.ColumnCount = 3;
            backlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            backlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            backlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            backlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            backlp.RowStyles.Add(new RowStyle(SizeType.Absolute, MAIN_ROW_H));
            backlp.RowStyles.Add(new RowStyle(SizeType.Absolute, MAIN_ROW_H));
            backlp.RowStyles.Add(new RowStyle(SizeType.Absolute, MAIN_ROW_H));
            backlp.RowStyles.Add(new RowStyle(SizeType.Absolute, MAIN_ROW_H));
            backlp.RowStyles.Add(new RowStyle(SizeType.Absolute, MAIN_ROW_H));
            backlp.Padding = Padding.Empty;
            backlp.Margin = Padding.Empty;

            uiBtnSelect = new AFMSButton();
            uiBtnSelect.Dock = DockStyle.Fill;
            uiBtnSelect.Text = "파일선택";
            uiBtnSelect.Click += BtnFileSelect_Click;

            uiBtnInsert = new AFMSButton();
            uiBtnInsert.Dock = DockStyle.Fill;
            uiBtnInsert.Text = "저장하기";
            uiBtnInsert.Enabled = false;
            uiBtnInsert.Click += BtnFileInsert_Click;

            uiTeFilePath = new AFMSTextBox();
            uiTeFilePath.Dock = DockStyle.Fill;
            uiTeFilePath.Enabled = false;

            uiGuide = new AFMSGuidePanel();
            uiGuide.Dock = DockStyle.Fill;
            uiGuide.BackColor = DllColorHelper.HexToColor("#FAFDFA");
            uiGuide.Margin = Padding.Empty;
            uiGuide.Add(GuideLevelType.Level0, "CSV 정의");
            uiGuide.Add(GuideLevelType.Level1, "첫행은 열정보(Distance, Elevation) 입니다.");
            uiGuide.Add(GuideLevelType.Level1, "이후 데이터는 단면 정보이며, Elevation은 해발 기준입니다.");
            uiGuide.Add(GuideLevelType.Level1, "열정보는 변경할수 없습니다.");
            uiGuide.Add(GuideLevelType.Level0, "유량 산정시 최종 단면으로 산정합니다.");

            AFMSLabel namedesc = new AFMSLabel();
            namedesc.Dock = DockStyle.Fill;
            namedesc.TextAlign = ContentAlignment.MiddleCenter;
            namedesc.Text = COL_DESC;

            uiTeFileResult = new AFMSTextBox();
            uiTeFileResult.Dock = DockStyle.Fill;
            uiTeFileResult.Enabled = false;
            uiTeFileResult.MaxLength = 20;

            AFMSLabel zerolevel = new AFMSLabel();
            zerolevel.Dock = DockStyle.Fill;
            zerolevel.TextAlign = ContentAlignment.MiddleCenter;
            zerolevel.Text = COL_ZERO_ELEV;

            uiNoZeroLevel = new AFMSNumberBox();
            uiNoZeroLevel.Dock = DockStyle.Fill;
            uiNoZeroLevel.Hint = "해발수위(El.m)";
            uiNoZeroLevel.TextAlign = HorizontalAlignment.Left;

            uiTeFileName = new AFMSTextBox();
            uiTeFileName.Dock = DockStyle.Fill;
            uiTeFileName.Enabled = true;
            uiTeFileName.MaxLength = 20;
            uiTeFileName.Hint = "단면 설명을 입력하세요.";

            backlp.Controls.Add(uiBtnSelect, 0, 1);
            backlp.Controls.Add(uiTeFilePath, 1, 1);
            backlp.Controls.Add(namedesc, 0, 2);
            backlp.Controls.Add(uiTeFileName, 1, 2);
            backlp.Controls.Add(zerolevel, 0, 3);
            backlp.Controls.Add(uiNoZeroLevel, 1, 3);
            backlp.Controls.Add(uiTeFileResult, 0, 4);
            backlp.Controls.Add(uiBtnInsert, 0, 5);

            backlp.SetColumnSpan(uiTeFilePath, 2);
            backlp.SetColumnSpan(uiTeFileName, 2);
            backlp.SetColumnSpan(uiNoZeroLevel, 2);
            backlp.SetColumnSpan(uiTeFileResult, 3);
            backlp.SetColumnSpan(uiBtnInsert, 3);

            uiTpRigth.Controls.Add(uiGuide, 0, 0);
            uiTpRigth.Controls.Add(_uiGrid, 0, 1);
            uiTpRigth.Controls.Add(backlp, 0, 2);
        }


        protected override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            _uiGrid.Columns[COL_DATA]?.Visible = false;
            _uiGrid.Columns[COL_INDEX]?.Width = 45;
            _uiGrid.Columns[COL_POINT_CNT]?.Width = 60;
            _uiGrid.Columns[COL_DATE]?.Width = 120;
            _uiGrid.Columns[COL_ZERO_ELEV]?.Width = 80;

            _uiGrid.Columns[COL_ZERO_ELEV]?.DefaultCellStyle.Format = "0.##' m'";

            foreach (DataGridViewRow row in _uiGrid.Rows)
            {
                if (row.IsNewRow) continue;

                object? dataValue = row.Cells[COL_DATA].Value;

                if (dataValue == null || dataValue == DBNull.Value)
                {
                    row.Tag = null;
                    continue;
                }

                try
                {
                    object? zeroValue = row.Cells[COL_ZERO_ELEV].Value;

                    if (zeroValue == null) continue;
                    if (zeroValue == DBNull.Value) continue;
                    if (!double.TryParse(zeroValue.ToString(), out double zeroEL)) continue;

                    CrossSectionPointCollection data = CrossSectionPointBuilder.Build(
                        dataValue.ToString() ?? string.Empty,
                        zeroEL);
                    row.Tag = data;
                }
                catch
                {
                    row.Tag = null;
                }
            }

            if (_uiGrid.Rows.Count == 0) return;

            _uiGrid.BeginInvoke((MethodInvoker)(() =>
            {
                if (_uiGrid.IsDisposed || _uiGrid.Rows.Count == 0) return;

                DataGridViewRow row = _uiGrid.Rows[_uiGrid.Rows.Count - 1];

                _lastSelectedId = -1;
                _uiGrid.ClearSelection();
                _uiGrid.CurrentCell = row.Cells[COL_INDEX];
                row.Selected = true;
            }));
        }

        private void UiGrid_SelectionChanged(object? sender, EventArgs e)
        {
            DataGridViewRow row = _uiGrid.CurrentRow;

            object? idValue = row.Cells[COL_INDEX].Value;

            if (idValue == null || idValue == DBNull.Value) return;
            if (!int.TryParse(idValue.ToString(), out int id)) return;
            if (_lastSelectedId == id) return;

            _lastSelectedId = id;

            if (row.Tag is not CrossSectionPointCollection data)
            {
                _uiChart.ClearData();
                _uiWaterLevel.Text = string.Empty;
                UpdateAreaGrid();
                return;
            }

            _uiChart.SetData(data);
            if (data.WaterLevel.HasValue) _uiWaterLevel.SetValue(data.WaterLevel.Value);
            else _uiWaterLevel.Text = string.Empty;
            UpdateAreaGrid();
        }

        private void WaterLevel_TextChanged(object? sender, EventArgs e)
        {
            CrossSectionPointCollection data = _uiChart.Data;
            data.WaterLevel = _uiWaterLevel.TryGetDouble(out double waterLevel) ? waterLevel : null;
            _uiChart.Invalidate();
            UpdateAreaGrid();
        }

        private void ChartRatio_SelectedIndexChanged(object? sender, EventArgs e)
        {
            AFMSChartAspectMode mode = _uiChartRatio.SelectedValue is AFMSChartAspectMode selected
                ? selected
                : AFMSChartAspectMode.Fit;
            _uiChart.SetAspectMode(mode);
        }

        private void UpdateAreaGrid()
        {
            _uiAreaGrid.Rows.Clear();

            CrossSectionPointCollection data = _uiChart.Data;
            if (!data.WaterLevel.HasValue || data.Count < 2)
            {
                _uiTotalArea.Text = "전체 단면적  0.00 m²";
                _uiAreaStatus.NormalForeColor = Color.FromArgb(90, 95, 105);
                _uiAreaStatus.Text = _selectedTransects.Count == 0 ? "측선 정보 없음" : "수위를 입력하세요";
                return;
            }

            double totalArea = data.Area;
            _uiTotalArea.Text = $"전체 단면적  {totalArea:N2} m²";

            if (_selectedTransects.Count == 0)
            {
                _uiAreaStatus.NormalForeColor = Color.FromArgb(90, 95, 105);
                _uiAreaStatus.Text = "측선 정보 없음";
                return;
            }

            try
            {
                _selectedTransects.CalculateSectionAreas(data, data.WaterLevel.Value);
                double transectAreaSum = _selectedTransects.Sum(transect => transect.SectionArea);

                int summaryIndex = _uiAreaGrid.Rows.Add("합계", transectAreaSum);
                _uiAreaGrid.Rows[summaryIndex].DefaultCellStyle.BackColor = Color.FromArgb(235, 248, 243);
                _uiAreaGrid.Rows[summaryIndex].DefaultCellStyle.ForeColor = DllColorHelper.HexToColor("#017D43");

                foreach (Transect transect in _selectedTransects.OrderBy(item => item.No))
                    _uiAreaGrid.Rows.Add(transect.No, transect.SectionArea);

                double tolerance = Math.Max(0.01, Math.Abs(totalArea) * 0.000001);
                bool matches = Math.Abs(totalArea - transectAreaSum) <= tolerance;
                _uiAreaStatus.NormalForeColor = matches
                    ? DllColorHelper.HexToColor("#017D43")
                    : Color.FromArgb(200, 60, 60);
                _uiAreaStatus.Text = matches ? "✓ 전체 단면적과 일치" : "⚠ 전체 단면적과 불일치";
            }
            catch (ArgumentException ex)
            {
                _uiAreaStatus.NormalForeColor = Color.FromArgb(200, 60, 60);
                _uiAreaStatus.Text = ex.Message;
            }
        }

        private void LoadHydroMeters()
        {
            const string COL_HYDRO_ID = "__HYDRO_ID";
            const string COL_HYDRO_NAME = "__HYDRO_NAME";
            const string COL_HYDRO_NO = "__HYDRO_NO";
            const string COL_TRANSECTS = "__TRANSECTS";

            _uiHydroCombo.ClearItems();
            _uiHydroCombo.Items.Add(HYDRO_NONE);

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
                    if (!TryReadTransectMarkers(json, out _)) continue;

                    int id = Convert.ToInt32(row[COL_HYDRO_ID]);
                    string deviceName = Convert.ToString(row[COL_HYDRO_NAME])?.Trim() ?? "유속계";
                    string deviceNo = row[COL_HYDRO_NO] == DBNull.Value
                        ? string.Empty
                        : $" #{Convert.ToInt32(row[COL_HYDRO_NO])}";
                    _uiHydroCombo.Items.Add(new HydroComboItem(id, $"{deviceName}{deviceNo}", json));
                }
            }

            _uiHydroCombo.SelectedIndex = 0;
        }

        private void HydroCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_uiHydroCombo.SelectedItem is not HydroComboItem hydro ||
                !TryReadTransects(hydro.TransectJson, out List<TransectJsonItem> transects))
            {
                _selectedTransects.Clear();
                _uiChart.ClearTransectMarkers();
                UpdateAreaGrid();
                return;
            }

            _selectedTransects.Clear();
            _selectedTransects.AddRange(transects.Select(item => new Transect
            {
                No = item.No,
                CenterLeftBankDistance = item.Distance
            }));
            List<AFMSChartTransectMarker> markers = transects
                .Select(item => new AFMSChartTransectMarker(item.No, item.Distance))
                .ToList();
            _uiChart.SetTransectMarkers(markers);
            UpdateAreaGrid();
        }

        private static bool TryReadTransectMarkers(string json, out List<AFMSChartTransectMarker> markers)
        {
            bool success = TryReadTransects(json, out List<TransectJsonItem> transects);
            markers = success
                ? transects.Select(item => new AFMSChartTransectMarker(item.No, item.Distance)).ToList()
                : new List<AFMSChartTransectMarker>();
            return success;
        }

        private static bool TryReadTransects(string json, out List<TransectJsonItem> transects)
        {
            transects = new List<TransectJsonItem>();

            try
            {
                TransectJsonData? data = JsonSerializer.Deserialize<TransectJsonData>(json);
                if (data == null || data.Transects.Count == 0) return false;

                foreach (TransectJsonItem transect in data.Transects.OrderBy(item => item.No))
                {
                    if (transect.No < 1 || !double.IsFinite(transect.Distance) || transect.Distance < 0.0) return false;
                    transects.Add(transect);
                }

                return transects.Count > 0;
            }
            catch (JsonException)
            {
                return false;
            }
        }
        
        private void UpdateMapList()
        {
            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSCrossSection.TABLE_NAME;

            query.AsAlias(FbtAFMSCrossSection.COL_ID, COL_INDEX);
            query.AsAlias(FbtAFMSCrossSection.SQL_MEASURE_DATETIME, COL_DATE);
            query.AsAlias(FbtAFMSCrossSection.COL_DESCRIPTION, COL_DESC);
            query.AsAlias(FbtAFMSCrossSection.COL_POINT_COUNT, COL_POINT_CNT);
            query.AsAlias(FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION, COL_ZERO_ELEV);
            query.AsAlias(FbtAFMSCrossSection.COL_POINT_DATA, COL_DATA);
            query.OrderBy(FbtAFMSCrossSection.COL_ID);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);

            if (!string.IsNullOrEmpty(error)) return;

            _uiGridData?.Dispose();
            _uiGridData = table.Copy();

            _uiGrid.DataSource = _uiGridData;
            _lastSelectedId = -1;
        }

        private void BtnFileInsert_Click(object? sender, EventArgs e)
        {
            if (_selectedAreaData == null || _selectedAreaData.Count == 0)
            {
                MessageBox.Show("저장할 단면 데이터가 없습니다.", "단면 데이터", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime now = DateTime.Now;
            CrossSectionPointCollection data = _selectedAreaData;

            QueryBuilderInsert query = new QueryBuilderInsert();
            query.Table = FbtAFMSCrossSection.TABLE_NAME;
            query.AutoIncrement = FbtAFMSCrossSection.COL_ID;

            query.Value(FbtAFMSCrossSection.COL_MEASURE_DATE, now.ToString("yyyyMMdd"));
            query.Value(FbtAFMSCrossSection.COL_MEASURE_TIME, now.ToString("HHmmss"));
            query.Value(FbtAFMSCrossSection.COL_DESCRIPTION, uiTeFileName.Text);
            query.Value(FbtAFMSCrossSection.COL_POINT_COUNT, data.Count);
            query.Value(FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION, uiNoZeroLevel.DoubleValue);
            query.Value(FbtAFMSCrossSection.COL_POINT_DATA, CrossSectionPointBuilder.GetJson(data));

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            db.Execute(query, out string error);

            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "DB 저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UpdateMapList();

            _selectedAreaData = null;
            uiTeFilePath.Text = string.Empty;
            uiTeFileName.Text = string.Empty;
            uiTeFileResult.Text = string.Empty;
            uiBtnInsert.Enabled = false;
        }

        private void BtnFileSelect_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "단면 CSV 파일 선택",
                Filter = "CSV 파일 (*.csv)|*.csv",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                RestoreDirectory = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            uiTeFilePath.Text = dialog.FileName;

            try
            {
                _selectedAreaData = AreaMapPointReader.Read(dialog.FileName);

                uiTeFileResult.Text = $"유효한 파일입니다. 포인트 {_selectedAreaData.Count}개 / CSV 구조 확인 완료";
                uiTeFileResult.ForeColor = Color.FromArgb(35, 130, 65);
                uiBtnInsert.Enabled = true;

                _uiChart.SetData(_selectedAreaData);
                _uiWaterLevel.Text = string.Empty;
                UpdateAreaGrid();
            }
            catch (UnauthorizedAccessException)
            {
                SetFileReadError("파일에 접근할 권한이 없습니다.");
            }
            catch (IOException ex)
            {
                SetFileReadError($"파일을 읽을 수 없습니다. {ex.Message}");
            }
            catch (Exception ex)
            {
                SetFileReadError($"파일 형식이 올바르지 않습니다. {ex.Message}");
            }
        }

        private void SetFileReadError(string message)
        {
            _selectedAreaData = null;
            uiTeFileResult.Text = message;
            uiTeFileResult.ForeColor = Color.FromArgb(200, 60, 60);
            uiTeFileName.Text = string.Empty;
            uiBtnInsert.Enabled = false;
        }

        protected override void ThisPageEntered(object? sender, EventArgs e)
        {
            UpdateMapList();
            LoadHydroMeters();
        }
    }
}
