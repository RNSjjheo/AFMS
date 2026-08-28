using AFMSDll;
using System.Data;

namespace AFMSSettings
{
    internal sealed class CrossSectionManagePanel : TableLayoutPanel
    {
        private const string COL_DATA = "단면값";
        private const string COL_INDEX = "번호";
        private const string COL_DATE = "입력일자";
        private const string COL_DESC = "단면설명";
        private const string COL_ZERO_ELEV = "영점표고";
        private const string COL_POINT_CNT = "포인트";

        private readonly AFMSDataGridView _grid;
        private readonly AFMSTextBox _filePath;
        private readonly AFMSTextBox _description;
        private readonly AFMSTextBox _fileResult;
        private readonly AFMSNumberBox _zeroLevel;
        private readonly AFMSButton _selectButton;
        private readonly AFMSButton _saveButton;
        private CrossSectionPointCollection? _selectedData;
        private DataTable? _gridData;
        private int _lastSelectedId = -1;
        private bool _bindingUpdatePending;

        public event EventHandler<CrossSectionDataEventArgs>? CrossSectionSelected;

        public CrossSectionManagePanel()
        {
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            ColumnCount = 1;
            RowCount = 3;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 125F));
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AFMSGuidePanel guide = CreateGuide();
            _grid = new AFMSDataGridView
            {
                Dock = DockStyle.Fill
            };
            _grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.DataBindingComplete += Grid_DataBindingComplete;
            _grid.SelectionChanged += Grid_SelectionChanged;

            Control filePanel = CreateFilePanel(
                out _filePath, out _description, out _fileResult, out _zeroLevel,
                out _selectButton, out _saveButton);

            Controls.Add(guide, 0, 0);
            Controls.Add(_grid, 0, 1);
            Controls.Add(filePanel, 0, 2);
        }

        public void RefreshList()
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

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) return;

            _gridData?.Dispose();
            _gridData = table.Copy();
            _grid.DataSource = _gridData;
            _lastSelectedId = -1;
        }

        private static AFMSGuidePanel CreateGuide()
        {
            AFMSGuidePanel guide = new AFMSGuidePanel
            {
                Dock = DockStyle.Fill,
                BackColor = DllColorHelper.HexToColor("#FAFDFA"),
                Margin = Padding.Empty
            };
            guide.Add(GuideLevelType.Level0, "CSV 정의");
            guide.Add(GuideLevelType.Level1, "첫행은 열정보(Distance, Elevation) 입니다.");
            guide.Add(GuideLevelType.Level1, "이후 데이터는 단면 정보이며, Elevation은 해발 기준입니다.");
            guide.Add(GuideLevelType.Level1, "열정보는 변경할수 없습니다.");
            guide.Add(GuideLevelType.Level0, "유량 산정시 최종 단면으로 산정합니다.");
            return guide;
        }

        private Control CreateFilePanel(
            out AFMSTextBox filePath, out AFMSTextBox description, out AFMSTextBox fileResult,
            out AFMSNumberBox zeroLevel, out AFMSButton selectButton, out AFMSButton saveButton)
        {
            const float ROW_HEIGHT = 38F;
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 3,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 5; i++) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_HEIGHT));

            selectButton = new AFMSButton { Dock = DockStyle.Fill, Text = "파일선택" };
            selectButton.Click += SelectButton_Click;
            saveButton = new AFMSButton { Dock = DockStyle.Fill, Text = "저장하기", Enabled = false };
            saveButton.Click += SaveButton_Click;
            filePath = new AFMSTextBox { Dock = DockStyle.Fill, Enabled = false };
            description = new AFMSTextBox
            {
                Dock = DockStyle.Fill,
                MaxLength = 20,
                Hint = "단면 설명을 입력하세요."
            };
            fileResult = new AFMSTextBox { Dock = DockStyle.Fill, Enabled = false, MaxLength = 20 };
            zeroLevel = new AFMSNumberBox
            {
                Dock = DockStyle.Fill,
                Hint = "해발수위(El.m)",
                TextAlign = HorizontalAlignment.Left
            };

            panel.Controls.Add(selectButton, 0, 1);
            panel.Controls.Add(filePath, 1, 1);
            panel.Controls.Add(CreateLabel(COL_DESC), 0, 2);
            panel.Controls.Add(description, 1, 2);
            panel.Controls.Add(CreateLabel(COL_ZERO_ELEV), 0, 3);
            panel.Controls.Add(zeroLevel, 1, 3);
            panel.Controls.Add(fileResult, 0, 4);
            panel.Controls.Add(saveButton, 0, 5);
            panel.SetColumnSpan(filePath, 2);
            panel.SetColumnSpan(description, 2);
            panel.SetColumnSpan(zeroLevel, 2);
            panel.SetColumnSpan(fileResult, 3);
            panel.SetColumnSpan(saveButton, 3);
            return panel;
        }

        private static AFMSLabel CreateLabel(string text)
        {
            return new AFMSLabel
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = text
            };
        }

        private void Grid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (_bindingUpdatePending || _grid.IsDisposed || !_grid.IsHandleCreated) return;

            _bindingUpdatePending = true;
            _grid.BeginInvoke((MethodInvoker)(() =>
            {
                _bindingUpdatePending = false;
                if (_grid.IsDisposed) return;
                ApplyGridBinding();
            }));
        }

        private void ApplyGridBinding()
        {
            if (!HasRequiredColumns()) return;

            _grid.Columns[COL_DATA].Visible = false;
            _grid.Columns[COL_INDEX].Width = 45;
            _grid.Columns[COL_POINT_CNT].Width = 60;
            _grid.Columns[COL_DATE].Width = 120;
            _grid.Columns[COL_ZERO_ELEV].Width = 80;
            _grid.Columns[COL_ZERO_ELEV].DefaultCellStyle.Format = "0.##' m'";

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                object? dataValue = row.Cells[COL_DATA].Value;
                object? zeroValue = row.Cells[COL_ZERO_ELEV].Value;

                if (dataValue == null || dataValue == DBNull.Value || zeroValue == null || zeroValue == DBNull.Value ||
                    !double.TryParse(zeroValue.ToString(), out double zeroElevation))
                {
                    row.Tag = null;
                    continue;
                }

                try
                {
                    row.Tag = CrossSectionPointBuilder.Build(dataValue.ToString() ?? string.Empty, zeroElevation);
                }
                catch
                {
                    row.Tag = null;
                }
            }

            if (_grid.Rows.Count == 0) return;

            DataGridViewRow selectedRow = _grid.Rows[_grid.Rows.Count - 1];
            _lastSelectedId = -1;
            _grid.ClearSelection();
            _grid.CurrentCell = selectedRow.Cells[COL_INDEX];
            selectedRow.Selected = true;
        }

        private void Grid_SelectionChanged(object? sender, EventArgs e)
        {
            if (!HasRequiredColumns()) return;

            DataGridViewRow? row = _grid.CurrentRow;
            if (row == null) return;

            object? idValue = row.Cells[COL_INDEX].Value;
            if (idValue == null || idValue == DBNull.Value || !int.TryParse(idValue.ToString(), out int id) ||
                _lastSelectedId == id) return;

            _lastSelectedId = id;
            CrossSectionSelected?.Invoke(this, new CrossSectionDataEventArgs(row.Tag as CrossSectionPointCollection));
        }

        private bool HasRequiredColumns()
        {
            return _grid.Columns.Contains(COL_DATA) &&
                   _grid.Columns.Contains(COL_INDEX) &&
                   _grid.Columns.Contains(COL_DATE) &&
                   _grid.Columns.Contains(COL_ZERO_ELEV) &&
                   _grid.Columns.Contains(COL_POINT_CNT);
        }

        private void SelectButton_Click(object? sender, EventArgs e)
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

            _filePath.Text = dialog.FileName;
            try
            {
                _selectedData = AreaMapPointReader.Read(dialog.FileName);
                _fileResult.Text = $"유효한 파일입니다. 포인트 {_selectedData.Count}개 / CSV 구조 확인 완료";
                _fileResult.ForeColor = Color.FromArgb(35, 130, 65);
                _saveButton.Enabled = true;
                CrossSectionSelected?.Invoke(this, new CrossSectionDataEventArgs(_selectedData));
            }
            catch (UnauthorizedAccessException)
            {
                SetFileError("파일에 접근할 권한이 없습니다.");
            }
            catch (IOException ex)
            {
                SetFileError($"파일을 읽을 수 없습니다. {ex.Message}");
            }
            catch (Exception ex)
            {
                SetFileError($"파일 형식이 올바르지 않습니다. {ex.Message}");
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (_selectedData == null || _selectedData.Count == 0)
            {
                MessageBox.Show("저장할 단면 데이터가 없습니다.", "단면 데이터", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime now = DateTime.Now;
            QueryBuilderInsert query = new QueryBuilderInsert();
            query.Table = FbtAFMSCrossSection.TABLE_NAME;
            query.AutoIncrement = FbtAFMSCrossSection.COL_ID;
            query.Value(FbtAFMSCrossSection.COL_MEASURE_DATE, now.ToString("yyyyMMdd"));
            query.Value(FbtAFMSCrossSection.COL_MEASURE_TIME, now.ToString("HHmmss"));
            query.Value(FbtAFMSCrossSection.COL_DESCRIPTION, _description.Text);
            query.Value(FbtAFMSCrossSection.COL_POINT_COUNT, _selectedData.Count);
            query.Value(FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION, _zeroLevel.DoubleValue);
            query.Value(FbtAFMSCrossSection.COL_POINT_DATA, CrossSectionPointBuilder.GetJson(_selectedData));

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "DB 저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RefreshList();
            _selectedData = null;
            _filePath.Text = string.Empty;
            _description.Text = string.Empty;
            _fileResult.Text = string.Empty;
            _saveButton.Enabled = false;
        }

        private void SetFileError(string message)
        {
            _selectedData = null;
            _fileResult.Text = message;
            _fileResult.ForeColor = Color.FromArgb(200, 60, 60);
            _description.Text = string.Empty;
            _saveButton.Enabled = false;
        }
    }

    internal sealed class CrossSectionDataEventArgs : EventArgs
    {
        public CrossSectionPointCollection? Data { get; }

        public CrossSectionDataEventArgs(CrossSectionPointCollection? data)
        {
            Data = data;
        }
    }
}
