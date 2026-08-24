using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace AFMSSettings
{
    internal class TabAreaManage : _TabBase
    {
        private const string COL_DATA = "단면값";
        private const string COL_INDEX = "번호";
        private const string COL_DATE = "입력일자";
        private const string COL_DESC = "단면설명";
        private const string COL_ZERO_ELEV = "영점표고";
        private const string COL_POINT_CNT = "포인트";

        private AFMSDataGridView _uiGrid;
        private AFMSAreaChart _uiChart;
        private DataTable _uiGridData;
        private AFMSTextBox uiTeFilePath;
        private AFMSTextBox uiTeFileName;
        private AFMSTextBox uiTeFileResult;
        private AFMSNumberBox uiNoZeroLevel;
        private AFMSButton uiBtnSelect;
        private AFMSButton uiBtnInsert;
        private AreaPointDatas? _selectedAreaData;
        private AFMSGuidePanel uiGuide;
        private TableLayoutPanel uiTpRigth;
        private int _lastSelectedId = -1;
        private bool _selectLastArea;

        public TabAreaManage()
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
            _uiChart.MouseUp += AreaChart_MouseUp;

            UpdateMapList();

            CtlMain = chartpanel;
            CtlSub = uiTpRigth;
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
                    AreaPointDatas data = new AreaPointDatas();
                    data.Convert(dataValue.ToString() ?? string.Empty);

                    object? zeroValue = row.Cells[COL_ZERO_ELEV].Value;

                    if (zeroValue == null) continue;
                    if (zeroValue == DBNull.Value) continue;
                    if (!double.TryParse(zeroValue.ToString(), out double zeroEL)) continue;

                    data.ZeroPointEL = zeroEL;
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

            if (row.Tag is not AreaPointDatas data)
            {
                _uiChart.ClearData();
                return;
            }

            _uiChart.SetData(data);
        }

        private void AreaChart_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            AreaPointDatas data = _uiChart.Data;

            if (data.Count < 2) return;

            Point screenPoint = _uiChart.PointToScreen(e.Location);

            FormAreaPopup popup = new FormAreaPopup(_uiChart, data);
            popup.StartPosition = FormStartPosition.Manual;
            popup.Location = new Point(screenPoint.X + 5, screenPoint.Y + 5);
            popup.ShowDialog(this);
        }
        
        private void UpdateMapList()
        {
            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSAreaMapPoint.TABLE_NAME;

            query.AsAlias(FbtAFMSAreaMapPoint.COL_ID, COL_INDEX);
            query.AsAlias(FbtAFMSAreaMapPoint.SQL_MEASURE_DATETIME, COL_DATE);
            query.AsAlias(FbtAFMSAreaMapPoint.COL_MAP_NAME, COL_DESC);
            query.AsAlias(FbtAFMSAreaMapPoint.COL_POINT_COUNT, COL_POINT_CNT);
            query.AsAlias(FbtAFMSAreaMapPoint.COL_ZERO_POINT_ELEVATION, COL_ZERO_ELEV);
            query.AsAlias(FbtAFMSAreaMapPoint.COL_MAP_DATA, COL_DATA);
            query.OrderBy(FbtAFMSAreaMapPoint.COL_ID);

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
            AreaPointDatas data = _selectedAreaData;

            QueryBuilderInsert query = new QueryBuilderInsert();
            query.Table = FbtAFMSAreaMapPoint.TABLE_NAME;
            query.AutoIncrement = FbtAFMSAreaMapPoint.COL_ID;

            query.Value(FbtAFMSAreaMapPoint.COL_MEASURE_DATE, now.ToString("yyyyMMdd"));
            query.Value(FbtAFMSAreaMapPoint.COL_MEASURE_TIME, now.ToString("HHmmss"));
            query.Value(FbtAFMSAreaMapPoint.COL_MAP_NAME, uiTeFileName.Text);
            query.Value(FbtAFMSAreaMapPoint.COL_POINT_COUNT, data.Count);
            query.Value(FbtAFMSAreaMapPoint.COL_ZERO_POINT_ELEVATION, uiNoZeroLevel.DoubleValue);
            query.Value(FbtAFMSAreaMapPoint.COL_MAP_DATA, data.GetJson());

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
        }
    }
}
