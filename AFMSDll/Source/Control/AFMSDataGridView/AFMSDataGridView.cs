using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    public class AFMSDataGridView : DataGridView
    {
        private sealed class AFMSCheckBoxColumnSetting
        {
            public string Text { get; set; } = string.Empty;
            public int HorizontalMargin { get; set; } = 5;
            public int VerticalMargin { get; set; } = 8;
        }

        private sealed class AFMSCheckBoxCellVisibilitySetting
        {
            public bool Visible { get; }

            public AFMSCheckBoxCellVisibilitySetting(bool visible)
            {
                Visible = visible;
            }
        }

        private sealed class AFMSMergedHeaderSetting
        {
            public string Text { get; }
            public List<string> ColumnNames { get; }

            public AFMSMergedHeaderSetting(string text, IEnumerable<string> columnNames)
            {
                Text = text ?? string.Empty;
                ColumnNames = new List<string>(columnNames);
            }
        }

        public class AFMSCheckBoxCheckedChangedEventArgs : EventArgs
        {
            public int RowIndex { get; }
            public int ColumnIndex { get; }
            public string ColumnName { get; }
            public bool Checked { get; }
            public AFMSCheckBox CheckBox { get; }

            public AFMSCheckBoxCheckedChangedEventArgs(int rowIndex, int columnIndex, string columnName, bool isChecked, AFMSCheckBox checkBox)
            {
                RowIndex = rowIndex;
                ColumnIndex = columnIndex;
                ColumnName = columnName;
                Checked = isChecked;
                CheckBox = checkBox;
            }
        }

        private readonly Dictionary<int, AFMSCheckBoxColumnSetting> _checkBoxColumns = new Dictionary<int, AFMSCheckBoxColumnSetting>();
        private readonly Dictionary<string, AFMSCheckBox> _checkBoxControls = new Dictionary<string, AFMSCheckBox>();
        private readonly Dictionary<string, bool> _checkBoxCellVisibility = new Dictionary<string, bool>();
        private readonly List<AFMSMergedHeaderSetting> _mergedHeaders = new List<AFMSMergedHeaderSetting>();
        private bool _syncingCheckBox;
        private bool _rebuildingCheckBoxes;
        private bool _updatingCheckBoxBounds;
        private bool _adjustingScrollBars;
        private bool _scrollBarUpdatePending;
        private VScrollBar? _attachedVerticalScrollBar;

        private Color _headerBackColor = Color.FromArgb(248, 249, 250);
        private Color _headerForeColor = Color.FromArgb(35, 35, 35);
        private Color _rowBackColor = Color.White;
        private Color _rowForeColor = Color.FromArgb(45, 45, 45);
        private Color _gridLineColor = Color.FromArgb(225, 228, 232);
        private Color _selectedBackColor = Color.FromArgb(242, 249, 244);
        private Color _selectedForeColor = Color.FromArgb(25, 90, 48);
        private Color _selectedBorderColor = Color.FromArgb(40, 150, 80);
        private Color _borderColor = Color.FromArgb(190, 198, 205);
        private Color _mergedHeaderLineColor = Color.FromArgb(225, 228, 232);
        private float _mergedHeaderLineThickness = 1F;
        private Color _afmsCheckBoxCheckedBorderColor = Color.FromArgb(53, 164, 93);

        private int _headerHeight = 30;
        private int _rowHeight = 30;
        private int _borderRadius = 8;
        private float _borderThickness = 1F;
        private float _afmsCheckBoxCheckedBorderThickness = 1.5F;
        private bool _showSelectedRowHighlight = true;
        private bool _showEmptyState = true;
        private string _emptyText = "데이터가 없습니다.";
        private Color _emptyTextColor = Color.FromArgb(175, 180, 185);
        private Color _emptyIconColor = Color.FromArgb(220, 223, 226);

        public event EventHandler<AFMSCheckBoxCheckedChangedEventArgs> AFMSCheckBoxCheckedChanged;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<int, int, bool>? AFMSCheckBoxCellVisibleEvaluator { get; set; }

        public AFMSDataGridView()
        {
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;

            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeColumns = false;
            AllowUserToResizeRows = false;
            RowHeadersVisible = false;
            MultiSelect = false;
            ReadOnly = true;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            AutoGenerateColumns = true;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            BackgroundColor = Color.White;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            EnableHeadersVisualStyles = false;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            AdvancedColumnHeadersBorderStyle.Left = DataGridViewAdvancedCellBorderStyle.None;
            AdvancedColumnHeadersBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;
            AdvancedColumnHeadersBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
            AdvancedColumnHeadersBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;

            AdvancedCellBorderStyle.Left = DataGridViewAdvancedCellBorderStyle.None;
            AdvancedCellBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;
            AdvancedCellBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
            AdvancedCellBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;

            ApplyAppearance();
        }

        #region AFMS Appearance

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HeaderBackColor
        {
            get => _headerBackColor;
            set { _headerBackColor = value; ApplyAppearance(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HeaderForeColor
        {
            get => _headerForeColor;
            set { _headerForeColor = value; ApplyAppearance(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color RowBackColor
        {
            get => _rowBackColor;
            set { _rowBackColor = value; ApplyAppearance(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color RowForeColor
        {
            get => _rowForeColor;
            set { _rowForeColor = value; ApplyAppearance(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color GridLineColor
        {
            get => _gridLineColor;
            set { _gridLineColor = value; ApplyAppearance(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color SelectedBackColor
        {
            get => _selectedBackColor;
            set { _selectedBackColor = value; ApplyAppearance(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color SelectedForeColor
        {
            get => _selectedForeColor;
            set { _selectedForeColor = value; ApplyAppearance(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color SelectedBorderColor
        {
            get => _selectedBorderColor;
            set { _selectedBorderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(30)]
        public int AFMSHeaderHeight
        {
            get => _headerHeight;
            set
            {
                _headerHeight = value < 1 ? 1 : value;
                UpdateColumnHeadersHeight();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(30)]
        public int AFMSRowHeight
        {
            get => _rowHeight;
            set
            {
                _rowHeight = value < 1 ? 1 : value;
                RowTemplate.Height = _rowHeight;
                foreach (DataGridViewRow row in Rows) row.Height = _rowHeight;
                UpdateAFMSCheckBoxBounds();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(8)]
        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = value < 0 ? 0 : value; UpdateInternalScrollBarBounds(); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1F)]
        public float BorderThickness
        {
            get => _borderThickness;
            set
            {
                _borderThickness = value < 0.5F ? 0.5F : value;
                UpdateInternalScrollBarBounds();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(true)]
        public bool ShowSelectedRowHighlight
        {
            get => _showSelectedRowHighlight;
            set
            {
                _showSelectedRowHighlight = value;
                ApplyAppearance();
                UpdateAFMSCheckBoxBackground();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(true)]
        public bool ShowEmptyState
        {
            get => _showEmptyState;
            set { _showEmptyState = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue("데이터가 없습니다.")]
        public string EmptyText
        {
            get => _emptyText;
            set { _emptyText = value ?? string.Empty; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color EmptyTextColor
        {
            get => _emptyTextColor;
            set { _emptyTextColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color EmptyIconColor
        {
            get => _emptyIconColor;
            set { _emptyIconColor = value; Invalidate(); }
        }

        [Category("AFMS CheckBox Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color CheckBoxCheckedBorderColor
        {
            get => _afmsCheckBoxCheckedBorderColor;
            set
            {
                _afmsCheckBoxCheckedBorderColor = value;
                ApplyAFMSCheckBoxAppearance();
            }
        }

        [Category("AFMS CheckBox Appearance")]
        [DefaultValue(1.5F)]
        public float CheckBoxCheckedBorderThickness
        {
            get => _afmsCheckBoxCheckedBorderThickness;
            set
            {
                _afmsCheckBoxCheckedBorderThickness = Math.Max(0F, value);
                ApplyAFMSCheckBoxAppearance();
            }
        }

        [Category("AFMS Merged Header")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color MergedHeaderLineColor
        {
            get => _mergedHeaderLineColor;
            set
            {
                _mergedHeaderLineColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Merged Header")]
        [DefaultValue(1F)]
        public float MergedHeaderLineThickness
        {
            get => _mergedHeaderLineThickness;
            set
            {
                _mergedHeaderLineThickness = Math.Max(0F, value);
                Invalidate();
            }
        }

        #endregion

        #region AFMS Merged Header

        [Browsable(false)]
        public bool HasMergedHeaders => _mergedHeaders.Count > 0;

        public void AddMergedHeader(string headerText, params string[] columnNames)
        {
            if (string.IsNullOrWhiteSpace(headerText)) throw new ArgumentException("병합 헤더의 제목을 입력해주세요.", nameof(headerText));
            if (columnNames == null || columnNames.Length < 2) throw new ArgumentException("병합 헤더에는 2개 이상의 컬럼이 필요합니다.", nameof(columnNames));

            List<DataGridViewColumn> columns = new List<DataGridViewColumn>();

            foreach (string columnName in columnNames)
            {
                if (string.IsNullOrWhiteSpace(columnName) || !Columns.Contains(columnName))
                    throw new ArgumentException($"컬럼 '{columnName}'을 찾을 수 없습니다.", nameof(columnNames));

                if (columns.Exists(column => string.Equals(column.Name, columnName, StringComparison.Ordinal)))
                    throw new ArgumentException($"컬럼 '{columnName}'이 중복 지정되었습니다.", nameof(columnNames));

                if (IsMergedHeaderColumn(columnName))
                    throw new ArgumentException($"컬럼 '{columnName}'은 이미 다른 병합 헤더에 포함되어 있습니다.", nameof(columnNames));

                columns.Add(Columns[columnName]);
            }

            columns.Sort((left, right) => left.DisplayIndex.CompareTo(right.DisplayIndex));

            for (int i = 1; i < columns.Count; i++)
            {
                if (columns[i].DisplayIndex != columns[i - 1].DisplayIndex + 1)
                    throw new ArgumentException("병합할 컬럼은 화면상 서로 연속되어 있어야 합니다.", nameof(columnNames));
            }

            _mergedHeaders.Add(new AFMSMergedHeaderSetting(headerText, columns.ConvertAll(column => column.Name)));
            UpdateColumnHeadersHeight();
            Invalidate();
        }

        public bool RemoveMergedHeader(string headerText)
        {
            int index = _mergedHeaders.FindIndex(header => string.Equals(header.Text, headerText, StringComparison.Ordinal));
            if (index < 0) return false;

            _mergedHeaders.RemoveAt(index);
            UpdateColumnHeadersHeight();
            Invalidate();

            return true;
        }

        public void ClearMergedHeaders()
        {
            if (_mergedHeaders.Count == 0) return;

            _mergedHeaders.Clear();
            UpdateColumnHeadersHeight();
            Invalidate();
        }

        private bool IsMergedHeaderColumn(string columnName)
        {
            foreach (AFMSMergedHeaderSetting header in _mergedHeaders)
            {
                if (header.ColumnNames.Exists(name => string.Equals(name, columnName, StringComparison.Ordinal))) return true;
            }

            return false;
        }

        private void UpdateColumnHeadersHeight()
        {
            ColumnHeadersHeight = _mergedHeaders.Count > 0 ? _headerHeight * 2 : _headerHeight;
        }

        #endregion

        #region AFMS CheckBox Column

        public void SetAFMSCheckBoxColumn(string columnName, string text = "", int horizontalMargin = 4, int verticalMargin = 7)
        {
            if (string.IsNullOrWhiteSpace(columnName) || !Columns.Contains(columnName)) return;
            SetAFMSCheckBoxColumn(Columns[columnName].Index, text, horizontalMargin, verticalMargin);
        }

        public void SetAFMSCheckBoxColumn(int columnIndex, string text = "", int horizontalMargin = 4, int verticalMargin = 7)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count) return;

            _checkBoxColumns[columnIndex] = new AFMSCheckBoxColumnSetting
            {
                Text = text ?? string.Empty,
                HorizontalMargin = Math.Max(0, horizontalMargin),
                VerticalMargin = Math.Max(0, verticalMargin)
            };

            Columns[columnIndex].ReadOnly = true;
            CreateAFMSCheckBoxes();
            UpdateAFMSCheckBoxBounds();
            InvalidateColumn(columnIndex);
        }

        public void RemoveAFMSCheckBoxColumn(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName) || !Columns.Contains(columnName)) return;
            RemoveAFMSCheckBoxColumn(Columns[columnName].Index);
        }

        public void RemoveAFMSCheckBoxColumn(int columnIndex)
        {
            if (!_checkBoxColumns.Remove(columnIndex)) return;
            RemoveAFMSCheckBoxes(columnIndex);
            InvalidateColumn(columnIndex);
        }

        public bool IsAFMSCheckBoxColumn(string columnName)
        {
            return !string.IsNullOrWhiteSpace(columnName) && Columns.Contains(columnName) && _checkBoxColumns.ContainsKey(Columns[columnName].Index);
        }

        public bool IsAFMSCheckBoxColumn(int columnIndex)
        {
            return _checkBoxColumns.ContainsKey(columnIndex);
        }

        public bool GetAFMSChecked(int rowIndex, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName) || !Columns.Contains(columnName)) return false;
            return GetAFMSChecked(rowIndex, Columns[columnName].Index);
        }

        public bool GetAFMSChecked(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= Rows.Count || !_checkBoxColumns.ContainsKey(columnIndex)) return false;
            return ToBoolean(Rows[rowIndex].Cells[columnIndex].Value);
        }

        public void SetAFMSChecked(int rowIndex, string columnName, bool value)
        {
            if (string.IsNullOrWhiteSpace(columnName) || !Columns.Contains(columnName)) return;
            SetAFMSChecked(rowIndex, Columns[columnName].Index, value);
        }

        public void SetAFMSChecked(int rowIndex, int columnIndex, bool value)
        {
            if (rowIndex < 0 || rowIndex >= Rows.Count || !_checkBoxColumns.ContainsKey(columnIndex)) return;

            _syncingCheckBox = true;
            Rows[rowIndex].Cells[columnIndex].Value = value;

            string key = GetCheckBoxKey(rowIndex, columnIndex);
            if (_checkBoxControls.TryGetValue(key, out AFMSCheckBox checkBox) && !checkBox.IsDisposed)
                checkBox.Checked = value;

            _syncingCheckBox = false;
        }

        public AFMSCheckBox GetAFMSCheckBox(int rowIndex, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName) || !Columns.Contains(columnName)) return null;
            return GetAFMSCheckBox(rowIndex, Columns[columnName].Index);
        }

        public AFMSCheckBox GetAFMSCheckBox(int rowIndex, int columnIndex)
        {
            _checkBoxControls.TryGetValue(GetCheckBoxKey(rowIndex, columnIndex), out AFMSCheckBox checkBox);
            return checkBox;
        }

        public void SetAFMSCheckBoxVisible(int rowIndex, string columnName, bool visible)
        {
            if (string.IsNullOrWhiteSpace(columnName) || !Columns.Contains(columnName)) return;
            SetAFMSCheckBoxVisible(rowIndex, Columns[columnName].Index, visible);
        }

        public void SetAFMSCheckBoxVisible(int rowIndex, int columnIndex, bool visible)
        {
            if (rowIndex < 0 || rowIndex >= Rows.Count || !_checkBoxColumns.ContainsKey(columnIndex)) return;

            string key = GetCheckBoxKey(rowIndex, columnIndex);
            _checkBoxCellVisibility[key] = visible;
            Rows[rowIndex].Cells[columnIndex].Tag = new AFMSCheckBoxCellVisibilitySetting(visible);

            if (_checkBoxControls.TryGetValue(key, out AFMSCheckBox checkBox) && !checkBox.IsDisposed)
                checkBox.Visible = visible;
            UpdateAFMSCheckBoxBounds();
        }

        public void ClearAFMSCheckBoxCellVisibility()
        {
            _checkBoxCellVisibility.Clear();

            foreach (DataGridViewRow row in Rows)
            {
                foreach (int columnIndex in _checkBoxColumns.Keys)
                {
                    if (columnIndex >= 0 && columnIndex < row.Cells.Count &&
                        row.Cells[columnIndex].Tag is AFMSCheckBoxCellVisibilitySetting)
                    {
                        row.Cells[columnIndex].Tag = null;
                    }
                }
            }

            UpdateAFMSCheckBoxBounds();
        }

        private void CreateAFMSCheckBoxes()
        {
            if (!IsHandleCreated && Rows.Count == 0) return;

            foreach (KeyValuePair<int, AFMSCheckBoxColumnSetting> pair in _checkBoxColumns)
            {
                int columnIndex = pair.Key;
                if (columnIndex < 0 || columnIndex >= Columns.Count) continue;

                for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++) CreateAFMSCheckBox(rowIndex, columnIndex, pair.Value);
            }
        }

        private void CreateAFMSCheckBox(int rowIndex, int columnIndex, AFMSCheckBoxColumnSetting setting)
        {
            string key = GetCheckBoxKey(rowIndex, columnIndex);
            if (_checkBoxControls.ContainsKey(key)) return;

            AFMSCheckBox checkBox = new AFMSCheckBox();
            checkBox.Text = setting.Text;
            checkBox.Checked = ToBoolean(Rows[rowIndex].Cells[columnIndex].Value);
            checkBox.Tag = new Point(columnIndex, rowIndex);
            checkBox.Margin = new Padding(0);
            checkBox.Visible = false;
            checkBox.BackColor = _rowBackColor;
            checkBox.CheckedBorderColor = _afmsCheckBoxCheckedBorderColor;
            checkBox.CheckedBorderThickness = _afmsCheckBoxCheckedBorderThickness;
            checkBox.CheckedChanged += AFMSCheckBox_CheckedChanged;

            Controls.Add(checkBox);
            _checkBoxControls[key] = checkBox;
        }

        private void AFMSCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_syncingCheckBox || sender is not AFMSCheckBox checkBox || checkBox.Tag is not Point cell) return;
            if (cell.Y < 0 || cell.Y >= Rows.Count || cell.X < 0 || cell.X >= Columns.Count) return;

            bool isChecked = checkBox.Checked;

            _syncingCheckBox = true;
            try
            {
                Rows[cell.Y].Cells[cell.X].Value = isChecked;
            }
            finally
            {
                _syncingCheckBox = false;
            }

            AFMSCheckBoxCheckedChanged?.Invoke(this, new AFMSCheckBoxCheckedChangedEventArgs(cell.Y, cell.X, Columns[cell.X].Name, isChecked, checkBox));
        }

        private void UpdateAFMSCheckBoxBounds()
        {
            if (!IsHandleCreated || _checkBoxColumns.Count == 0 ||
                _rebuildingCheckBoxes || _updatingCheckBoxBounds) return;

            _updatingCheckBoxBounds = true;

            try
            {
                CreateAFMSCheckBoxes();

                Rectangle displayArea = new Rectangle(0, ColumnHeadersHeight, ClientSize.Width, Math.Max(0, ClientSize.Height - ColumnHeadersHeight));

                foreach (KeyValuePair<string, AFMSCheckBox> pair in new List<KeyValuePair<string, AFMSCheckBox>>(_checkBoxControls))
                {
                    AFMSCheckBox checkBox = pair.Value;
                    if (checkBox.IsDisposed) continue;
                    if (checkBox.Tag is not Point cell || cell.Y < 0 || cell.Y >= Rows.Count || cell.X < 0 || cell.X >= Columns.Count)
                    {
                        checkBox.Visible = false;
                        continue;
                    }

                    if (!_checkBoxColumns.TryGetValue(cell.X, out AFMSCheckBoxColumnSetting setting) || !Columns[cell.X].Visible || !Rows[cell.Y].Visible)
                    {
                        checkBox.Visible = false;
                        continue;
                    }

                    if (!IsAFMSCheckBoxCellVisible(cell.Y, cell.X, pair.Key))
                    {
                        checkBox.Visible = false;
                        continue;
                    }

                    Rectangle cellRect = GetCellDisplayRectangle(cell.X, cell.Y, true);
                    Rectangle visibleRect = Rectangle.Intersect(cellRect, displayArea);

                    if (visibleRect.Width <= 0 || visibleRect.Height <= 0)
                    {
                        checkBox.Visible = false;
                        continue;
                    }

                    int x = cellRect.Left + setting.HorizontalMargin;
                    int y = cellRect.Top + setting.VerticalMargin;
                    int width = Math.Max(0, cellRect.Width - (setting.HorizontalMargin * 2));
                    int height = Math.Max(0, cellRect.Height - (setting.VerticalMargin * 2));

                    if (width <= 0 || height <= 0)
                    {
                        checkBox.Visible = false;
                        continue;
                    }

                    checkBox.BackColor = _showSelectedRowHighlight && Rows[cell.Y].Selected ? _selectedBackColor : _rowBackColor;
                    checkBox.Bounds = new Rectangle(x, y, width, height);
                    checkBox.Visible = true;
                    checkBox.BringToFront();
                    checkBox.Invalidate();
                }
            }
            finally
            {
                _updatingCheckBoxBounds = false;
            }
        }

        private bool IsAFMSCheckBoxCellVisible(int rowIndex, int columnIndex, string key)
        {
            if (_checkBoxCellVisibility.TryGetValue(key, out bool cellVisible) && !cellVisible) return false;

            if (Rows[rowIndex].Cells[columnIndex].Tag is AFMSCheckBoxCellVisibilitySetting visibility && !visibility.Visible)
                return false;

            return AFMSCheckBoxCellVisibleEvaluator?.Invoke(rowIndex, columnIndex) ?? true;
        }

        private void RemoveAFMSCheckBoxes(int columnIndex)
        {
            List<string> removeKeys = new List<string>();

            foreach (KeyValuePair<string, AFMSCheckBox> pair in _checkBoxControls)
            {
                if (pair.Value.Tag is Point cell && cell.X == columnIndex) removeKeys.Add(pair.Key);
            }

            foreach (string key in removeKeys)
            {
                AFMSCheckBox checkBox = _checkBoxControls[key];
                _checkBoxControls.Remove(key);
                checkBox.CheckedChanged -= AFMSCheckBox_CheckedChanged;
                Controls.Remove(checkBox);
                checkBox.Dispose();
            }
        }

        private void RebuildAFMSCheckBoxes()
        {
            if (_rebuildingCheckBoxes) return;

            _rebuildingCheckBoxes = true;

            try
            {
                List<AFMSCheckBox> controls = new List<AFMSCheckBox>(_checkBoxControls.Values);
                _checkBoxControls.Clear();

                foreach (AFMSCheckBox checkBox in controls)
                {
                    Controls.Remove(checkBox);
                    checkBox.CheckedChanged -= AFMSCheckBox_CheckedChanged;
                    checkBox.Dispose();
                }

                CreateAFMSCheckBoxes();
            }
            finally
            {
                _rebuildingCheckBoxes = false;
            }

            UpdateAFMSCheckBoxBounds();
        }

        private static bool ToBoolean(object value)
        {
            if (value == null || value == DBNull.Value) return false;
            if (value is bool boolValue) return boolValue;
            if (value is int intValue) return intValue != 0;

            string text = Convert.ToString(value)?.Trim();
            return string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "Y", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "YES", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCheckBoxKey(int rowIndex, int columnIndex)
        {
            return rowIndex + ":" + columnIndex;
        }

        #endregion

        private void UpdateInternalScrollBarBounds()
        {
            if (_adjustingScrollBars || !IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            _adjustingScrollBars = true;

            try
            {
                VScrollBar? vertical = null;
                HScrollBar? horizontal = null;

                foreach (Control control in Controls)
                {
                    if (control is VScrollBar vScrollBar) vertical = vScrollBar;
                    else if (control is HScrollBar hScrollBar) horizontal = hScrollBar;
                }

                AttachInternalVerticalScrollBar(vertical);

                int borderInset = Math.Max(1, (int)Math.Ceiling(_borderThickness));
                int radiusInset = Math.Max(borderInset, _borderRadius);

                if (vertical != null && vertical.Visible)
                {
                    int x = Math.Max(0, ClientSize.Width - vertical.Width - borderInset);
                    int y = ColumnHeadersVisible ? ColumnHeadersHeight : borderInset;
                    int bottom = ClientSize.Height - radiusInset;

                    if (horizontal != null && horizontal.Visible) bottom = Math.Min(bottom, horizontal.Top);

                    int height = Math.Max(0, bottom - y);
                    vertical.SetBounds(x, y, vertical.Width, height);
                }

                if (horizontal != null && horizontal.Visible)
                {
                    int x = radiusInset;
                    int right = ClientSize.Width - radiusInset;
                    if (vertical != null && vertical.Visible) right = Math.Min(right, vertical.Left);

                    int y = Math.Max(0, ClientSize.Height - horizontal.Height - radiusInset);
                    int width = Math.Max(0, right - x);
                    horizontal.SetBounds(x, y, width, horizontal.Height);
                }
            }
            finally
            {
                _adjustingScrollBars = false;
            }
        }

        private void AttachInternalVerticalScrollBar(VScrollBar? vertical)
        {
            if (ReferenceEquals(_attachedVerticalScrollBar, vertical)) return;

            if (_attachedVerticalScrollBar != null) _attachedVerticalScrollBar.ValueChanged -= InternalVerticalScrollBar_ValueChanged;

            _attachedVerticalScrollBar = vertical;

            if (_attachedVerticalScrollBar != null) _attachedVerticalScrollBar.ValueChanged += InternalVerticalScrollBar_ValueChanged;
        }

        private void InternalVerticalScrollBar_ValueChanged(object? sender, EventArgs e)
        {
            QueueInternalScrollBarBoundsUpdate();
        }

        private void QueueInternalScrollBarBoundsUpdate()
        {
            if (_scrollBarUpdatePending || !IsHandleCreated || IsDisposed || Disposing) return;

            _scrollBarUpdatePending = true;

            BeginInvoke(new Action(() =>
            {
                _scrollBarUpdatePending = false;

                if (!IsHandleCreated || IsDisposed || Disposing) return;

                UpdateInternalScrollBarBounds();
                Invalidate();
            }));
        }

        private void DrawScrollBarHeaderArea(Graphics g)
        {
            if (!ColumnHeadersVisible || ColumnHeadersHeight <= 0) return;

            foreach (Control control in Controls)
            {
                if (control is not VScrollBar vertical || !vertical.Visible) continue;

                int left = Math.Max(0, vertical.Left);
                int width = Math.Max(0, ClientSize.Width - left);
                if (width <= 0) return;

                Rectangle rect = new Rectangle(left, 0, width, ColumnHeadersHeight);
                using SolidBrush brush = new SolidBrush(_headerBackColor);
                g.FillRectangle(brush, rect);

                if (_gridLineColor.A > 0)
                {
                    using Pen pen = new Pen(_gridLineColor, 1F);
                    int y = ColumnHeadersHeight - 1;
                    g.DrawLine(pen, rect.Left, y, rect.Right, y);
                }

                return;
            }
        }

        private void ApplyAppearance()
        {
            GridColor = _gridLineColor;
            UpdateColumnHeadersHeight();
            RowTemplate.Height = _rowHeight;

            ColumnHeadersDefaultCellStyle.BackColor = _headerBackColor;
            ColumnHeadersDefaultCellStyle.ForeColor = _headerForeColor;
            ColumnHeadersDefaultCellStyle.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
            ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            ColumnHeadersDefaultCellStyle.SelectionBackColor = _headerBackColor;
            ColumnHeadersDefaultCellStyle.SelectionForeColor = _headerForeColor;
            ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            DefaultCellStyle.BackColor = _rowBackColor;
            DefaultCellStyle.ForeColor = _rowForeColor;
            DefaultCellStyle.Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular);
            DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DefaultCellStyle.SelectionBackColor = _showSelectedRowHighlight ? _selectedBackColor : _rowBackColor;
            DefaultCellStyle.SelectionForeColor = _showSelectedRowHighlight ? _selectedForeColor : _rowForeColor;
            DefaultCellStyle.Padding = new Padding(6, 3, 6, 3);

            RowsDefaultCellStyle.BackColor = _rowBackColor;
            AlternatingRowsDefaultCellStyle.BackColor = _rowBackColor;

            Invalidate();
        }

        private void UpdateAFMSCheckBoxBackground()
        {
            foreach (AFMSCheckBox checkBox in new List<AFMSCheckBox>(_checkBoxControls.Values))
            {
                if (checkBox.IsDisposed) continue;
                if (checkBox.Tag is not Point cell) continue;
                if (cell.Y < 0 || cell.Y >= Rows.Count) continue;

                checkBox.BackColor = _showSelectedRowHighlight && Rows[cell.Y].Selected ? _selectedBackColor : _rowBackColor;
                checkBox.Invalidate();
            }
        }

        private void ApplyAFMSCheckBoxAppearance()
        {
            foreach (AFMSCheckBox checkBox in new List<AFMSCheckBox>(_checkBoxControls.Values))
            {
                if (checkBox.IsDisposed) continue;
                checkBox.CheckedBorderColor = _afmsCheckBoxCheckedBorderColor;
                checkBox.CheckedBorderThickness = _afmsCheckBoxCheckedBorderThickness;
                checkBox.Invalidate();
            }
        }

        public void RefreshAFMSCheckBoxes()
        {
            CreateAFMSCheckBoxes();
            UpdateAFMSCheckBoxBounds();
            Invalidate();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing && _attachedVerticalScrollBar != null)
            {
                _attachedVerticalScrollBar.ValueChanged -= InternalVerticalScrollBar_ValueChanged;
                _attachedVerticalScrollBar = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnColumnAdded(DataGridViewColumnEventArgs e)
        {
            base.OnColumnAdded(e);
            e.Column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        protected override void OnColumnRemoved(DataGridViewColumnEventArgs e)
        {
            base.OnColumnRemoved(e);
            RebuildAFMSCheckBoxes();
            RemoveInvalidMergedHeaders();
        }

        protected override void OnRowsAdded(DataGridViewRowsAddedEventArgs e)
        {
            base.OnRowsAdded(e);
            CreateAFMSCheckBoxes();
            UpdateAFMSCheckBoxBounds();
            UpdateInternalScrollBarBounds();
        }

        protected override void OnRowsRemoved(DataGridViewRowsRemovedEventArgs e)
        {
            base.OnRowsRemoved(e);
            RebuildAFMSCheckBoxes();
            UpdateInternalScrollBarBounds();
        }

        protected override void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e)
        {
            base.OnDataBindingComplete(e);

            foreach (DataGridViewColumn column in Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
            foreach (DataGridViewRow row in Rows) row.Height = _rowHeight;

            RebuildAFMSCheckBoxes();
            UpdateInternalScrollBarBounds();
            QueueInternalScrollBarBoundsUpdate();
            ClearSelection();
        }

        protected override void OnCellValueChanged(DataGridViewCellEventArgs e)
        {
            base.OnCellValueChanged(e);

            if (_syncingCheckBox || e.RowIndex < 0 || e.ColumnIndex < 0 || !_checkBoxColumns.ContainsKey(e.ColumnIndex)) return;

            string key = GetCheckBoxKey(e.RowIndex, e.ColumnIndex);
            if (!_checkBoxControls.TryGetValue(key, out AFMSCheckBox checkBox) || checkBox.IsDisposed) return;

            _syncingCheckBox = true;
            checkBox.Checked = ToBoolean(Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
            _syncingCheckBox = false;
        }

        protected override void OnSelectionChanged(EventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateAFMSCheckBoxBackground();
            Invalidate();
        }

        protected override void OnScroll(ScrollEventArgs e)
        {
            base.OnScroll(e);
            UpdateAFMSCheckBoxBounds();
            QueueInternalScrollBarBoundsUpdate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateAFMSCheckBoxBounds();
            UpdateInternalScrollBarBounds();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            UpdateAFMSCheckBoxBounds();
            UpdateInternalScrollBarBounds();
        }

        protected override void OnColumnWidthChanged(DataGridViewColumnEventArgs e)
        {
            base.OnColumnWidthChanged(e);
            UpdateAFMSCheckBoxBounds();
        }

        protected override void OnColumnDisplayIndexChanged(DataGridViewColumnEventArgs e)
        {
            base.OnColumnDisplayIndexChanged(e);
            UpdateAFMSCheckBoxBounds();
        }

        protected override void OnRowHeightChanged(DataGridViewRowEventArgs e)
        {
            base.OnRowHeightChanged(e);
            UpdateAFMSCheckBoxBounds();
        }

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && _checkBoxColumns.ContainsKey(e.ColumnIndex))
            {
                bool selected = _showSelectedRowHighlight && Rows[e.RowIndex].Selected;
                Color backColor = selected ? _selectedBackColor : _rowBackColor;

                using (SolidBrush brush = new SolidBrush(backColor)) e.Graphics.FillRectangle(brush, e.CellBounds);

                if (!selected)
                {
                    using Pen pen = new Pen(_gridLineColor, 1F);
                    int y = e.CellBounds.Bottom - 1;
                    e.Graphics.DrawLine(pen, e.CellBounds.Left, y, e.CellBounds.Right, y);
                }

                e.Handled = true;
                return;
            }

            if (_showSelectedRowHighlight && e.RowIndex >= 0 && Rows[e.RowIndex].Selected)
            {
                e.Paint(e.CellBounds, e.PaintParts & ~DataGridViewPaintParts.Focus);
                e.Handled = true;
                return;
            }

            base.OnCellPainting(e);
        }

        protected override void OnRowPostPaint(DataGridViewRowPostPaintEventArgs e)
        {
            base.OnRowPostPaint(e);

            if (!_showSelectedRowHighlight || !Rows[e.RowIndex].Selected) return;

            Rectangle rect = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, e.RowBounds.Width - 1, e.RowBounds.Height - 1);

            using Pen pen = new Pen(_selectedBorderColor, 1);
            using SolidBrush brush = new SolidBrush(_selectedBorderColor);

            e.Graphics.DrawLine(pen, rect.Left, rect.Top, rect.Right, rect.Top);
            e.Graphics.DrawLine(pen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
            e.Graphics.FillRectangle(brush, rect.Left, rect.Top + 1, 4, rect.Height - 2);
        }

        private void RemoveInvalidMergedHeaders()
        {
            bool removed = false;

            for (int i = _mergedHeaders.Count - 1; i >= 0; i--)
            {
                AFMSMergedHeaderSetting header = _mergedHeaders[i];

                if (header.ColumnNames.Exists(columnName => !Columns.Contains(columnName)))
                {
                    _mergedHeaders.RemoveAt(i);
                    removed = true;
                }
            }

            if (!removed) return;

            UpdateColumnHeadersHeight();
            Invalidate();
        }

        private void DrawMergedHeaders(Graphics g)
        {
            if (_mergedHeaders.Count == 0 || Columns.Count == 0 || ColumnHeadersHeight <= 0) return;

            int topHeight = Math.Min(_headerHeight, ColumnHeadersHeight);
            int bottomHeight = Math.Max(0, ColumnHeadersHeight - topHeight);

            using SolidBrush backBrush = new SolidBrush(_headerBackColor);
            using Pen linePen = new Pen(_mergedHeaderLineColor, MergedHeaderLineThickness);

            HashSet<string> groupedColumns = new HashSet<string>(StringComparer.Ordinal);

            foreach (AFMSMergedHeaderSetting header in _mergedHeaders)
            {
                List<DataGridViewColumn> columns = GetVisibleMergedColumns(header);
                if (columns.Count == 0) continue;

                foreach (DataGridViewColumn column in columns) groupedColumns.Add(column.Name);

                Rectangle groupRect = GetMergedHeaderRectangle(columns, 0, topHeight);
                if (groupRect.Width > 0 && groupRect.Height > 0) DrawHeaderCell(g, groupRect, header.Text, backBrush, linePen);

                foreach (DataGridViewColumn column in columns)
                {
                    Rectangle columnRect = GetHeaderColumnRectangle(column);
                    Rectangle childRect = new Rectangle(columnRect.Left, topHeight, columnRect.Width, bottomHeight);
                    childRect = Rectangle.Intersect(childRect, new Rectangle(0, 0, ClientSize.Width, ColumnHeadersHeight));

                    if (childRect.Width <= 0 || childRect.Height <= 0) continue;
                    DrawHeaderCell(g, childRect, column.HeaderText, backBrush, linePen);
                }
            }

            foreach (DataGridViewColumn column in Columns)
            {
                if (!column.Visible || groupedColumns.Contains(column.Name)) continue;

                Rectangle rect = GetHeaderColumnRectangle(column);
                rect.Y = 0;
                rect.Height = ColumnHeadersHeight;
                rect = Rectangle.Intersect(rect, new Rectangle(0, 0, ClientSize.Width, ColumnHeadersHeight));

                if (rect.Width <= 0 || rect.Height <= 0) continue;
                DrawHeaderCell(g, rect, column.HeaderText, backBrush, linePen);
            }
        }

        private List<DataGridViewColumn> GetVisibleMergedColumns(AFMSMergedHeaderSetting header)
        {
            List<DataGridViewColumn> columns = new List<DataGridViewColumn>();

            foreach (string columnName in header.ColumnNames)
            {
                if (!Columns.Contains(columnName)) continue;

                DataGridViewColumn column = Columns[columnName];
                if (column.Visible) columns.Add(column);
            }

            columns.Sort((left, right) => left.DisplayIndex.CompareTo(right.DisplayIndex));
            return columns;
        }

        private Rectangle GetMergedHeaderRectangle(List<DataGridViewColumn> columns, int y, int height)
        {
            if (columns.Count == 0) return Rectangle.Empty;

            Rectangle first = GetHeaderColumnRectangle(columns[0]);
            Rectangle last = GetHeaderColumnRectangle(columns[columns.Count - 1]);
            int left = first.Left;
            int right = last.Right;

            Rectangle rect = new Rectangle(left, y, Math.Max(0, right - left), height);
            return Rectangle.Intersect(rect, new Rectangle(0, 0, ClientSize.Width, ColumnHeadersHeight));
        }

        private Rectangle GetHeaderColumnRectangle(DataGridViewColumn column)
        {
            return GetCellDisplayRectangle(column.Index, -1, true);
        }

        private void DrawHeaderCell(Graphics g, Rectangle rect, string text, SolidBrush backBrush, Pen linePen)
        {
            g.FillRectangle(backBrush, rect);

            Rectangle textRect = Rectangle.Inflate(rect, -4, 0);
            TextRenderer.DrawText(
                g,
                text ?? string.Empty,
                ColumnHeadersDefaultCellStyle.Font ?? Font,
                textRect,
                _headerForeColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);

            if (_mergedHeaderLineColor.A == 0 || MergedHeaderLineThickness <= 0F) return;

            int right = rect.Right - 1;
            int bottom = rect.Bottom - 1;

            g.DrawLine(linePen, rect.Left, rect.Top, right, rect.Top);
            g.DrawLine(linePen, rect.Left, rect.Top, rect.Left, bottom);
            g.DrawLine(linePen, right, rect.Top, right, bottom);
            g.DrawLine(linePen, rect.Left, bottom, right, bottom);
        }

        private void DrawEmptyState(Graphics g)
        {
            if (!ShowEmptyState || Rows.Count > 0 || string.IsNullOrEmpty(EmptyText)) return;

            int top = ColumnHeadersVisible ? ColumnHeadersHeight : 0;
            Rectangle dataRect = new Rectangle(0, top, ClientSize.Width, Math.Max(0, ClientSize.Height - top));
            if (dataRect.Width <= 0 || dataRect.Height <= 0) return;

            const int iconWidth = 28;
            const int iconHeight = 34;
            const int foldSize = 8;
            const int textGap = 8;

            Font textFont = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 8.5F, FontStyle.Regular);
            Size textSize = TextRenderer.MeasureText(EmptyText, textFont, Size.Empty, TextFormatFlags.NoPadding);

            int totalHeight = iconHeight + textGap + textSize.Height;
            int startY = dataRect.Top + Math.Max(0, (dataRect.Height - totalHeight) / 2);
            int iconX = dataRect.Left + Math.Max(0, (dataRect.Width - iconWidth) / 2);

            Rectangle iconRect = new Rectangle(iconX, startY, iconWidth, iconHeight);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using Pen iconPen = new Pen(EmptyIconColor, 1.5F);

            Point[] outline =
            {
                new Point(iconRect.Left + 1, iconRect.Top + 1),
                new Point(iconRect.Right - foldSize - 1, iconRect.Top + 1),
                new Point(iconRect.Right - 1, iconRect.Top + foldSize + 1),
                new Point(iconRect.Right - 1, iconRect.Bottom - 1),
                new Point(iconRect.Left + 1, iconRect.Bottom - 1)
            };

            using GraphicsPath documentPath = new GraphicsPath();
            documentPath.AddLines(outline);
            documentPath.CloseFigure();
            g.DrawPath(iconPen, documentPath);

            g.DrawLine(iconPen, iconRect.Right - foldSize - 1, iconRect.Top + 1, iconRect.Right - foldSize - 1, iconRect.Top + foldSize + 1);
            g.DrawLine(iconPen, iconRect.Right - foldSize - 1, iconRect.Top + foldSize + 1, iconRect.Right - 1, iconRect.Top + foldSize + 1);

            int lineLeft = iconRect.Left + 7;
            int lineRight = iconRect.Right - 7;
            int lineY = iconRect.Top + 17;
            g.DrawLine(iconPen, lineLeft, lineY, lineRight, lineY);
            g.DrawLine(iconPen, lineLeft, lineY + 5, lineRight, lineY + 5);
            g.DrawLine(iconPen, lineLeft, lineY + 10, lineRight - 4, lineY + 10);

            Rectangle textRect = new Rectangle(dataRect.Left, iconRect.Bottom + textGap, dataRect.Width, textSize.Height + 2);
            TextRenderer.DrawText(g, EmptyText, textFont, textRect, EmptyTextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            textFont.Dispose();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawScrollBarHeaderArea(e.Graphics);
            if (_mergedHeaders.Count > 0) DrawMergedHeaders(e.Graphics);
            DrawEmptyState(e.Graphics);

            if (ClientSize.Width <= 1 || ClientSize.Height <= 1) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color cornerColor = Parent?.BackColor ?? BackColor;
            RectangleF outerRect = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);
            RectangleF cornerRect = new RectangleF(0, 0, ClientSize.Width - 1F, ClientSize.Height - 1F);

            using (GraphicsPath cornerPath = new GraphicsPath(FillMode.Alternate))
            using (GraphicsPath roundPath = CreateRoundPath(cornerRect, _borderRadius))
            using (SolidBrush cornerBrush = new SolidBrush(cornerColor))
            {
                cornerPath.AddRectangle(outerRect);
                cornerPath.AddPath(roundPath, false);
                e.Graphics.FillPath(cornerBrush, cornerPath);
            }

            float inset = _borderThickness / 2F;
            RectangleF borderRect = new RectangleF(inset, inset, ClientSize.Width - _borderThickness, ClientSize.Height - _borderThickness);
            if (borderRect.Width <= 0 || borderRect.Height <= 0) return;

            float borderRadius = Math.Max(0F, _borderRadius - inset);
            using GraphicsPath borderPath = CreateRoundPath(borderRect, borderRadius);
            using Pen pen = new Pen(_borderColor, _borderThickness);
            e.Graphics.DrawPath(pen, borderPath);
        }

        private static GraphicsPath CreateRoundPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float diameter = radius * 2F;
            float maxDiameter = rect.Width < rect.Height ? rect.Width : rect.Height;
            if (diameter > maxDiameter) diameter = maxDiameter;

            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
