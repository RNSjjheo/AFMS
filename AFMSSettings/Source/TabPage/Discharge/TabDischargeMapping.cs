using AFMSDll;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class TabDischargeMapping : _TabDischargeBase
    {
        private const string COL_DEVICE_TYPE = "DEVICE_TYPE";
        private const string COL_DEVICE_ID = "DEVICE_ID";
        private const string COL_TRANSECT_COUNT = "TRANSECT_COUNT";
        private const string COL_ROW_NO = "ROW_NO";
        private const string COL_DEVICE_KIND = "DEVICE_KIND";
        private const string COL_DEVICE_NAME = "DEVICE_NAME";
        private const int SYSTEM_WATER_LEVEL_DEVICE_ID = 0;

        private readonly List<DischargeMethod> _methods = new();
        private readonly Dictionary<string, Dictionary<DischargeMethod, bool>> _originalValues = new();
        private readonly Dictionary<string, int> _methodConfigIds = new();
        private readonly HashSet<int> _surfaceConfiguredHydroIds = new();
        private readonly HashSet<int> _midSectionConfiguredHydroIds = new();
        private readonly HashSet<int> _velocityDistributionConfiguredHydroIds = new();
        private readonly Dictionary<int, int> _transectCounts = new();
        private readonly Dictionary<int, int> _latestTransectConfigIds = new();
        private readonly HashSet<int> _staleHydroIds = new();
        private bool _hasRatingCurveConfig;
        private AFMSGuidePanel uiGuide;

        public TabDischargeMapping() : base(false)
        {
            Text = "유량 산정 선택";
            BackColor = Color.White;
            Padding = Padding.Empty;

            foreach (DischargeMethod method in Enum.GetValues(typeof(DischargeMethod)))
                if (method != DischargeMethod.None) _methods.Add(method);

            SetupLayout();
            SetupGrid();
            SetupMethodCheckBoxes();

            uiTpMain.SetRowSpan(uiGridMain, 2);
            uiTpMain.SetRowSpan(uiGuide, 2);
            uiTpMain.Controls.Remove(uiButtonInput);
        }

        public bool HasChanges => uiGridMain.Rows.Cast<DataGridViewRow>()
            .Any(row => !row.IsNewRow && IsRowChanged(row));

        public void LoadData()
        {
            uiGridMain.Rows.Clear();
            uiGridMain.ClearAFMSCheckBoxCellVisibility();
            _originalValues.Clear();

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            LoadMethodAvailability(db);
            Dictionary<string, bool> configured = LoadLatestSelections(db);

            QueryBuilderSelect query = new();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.Add(FbtAFMSHydroMeter.COL_ID);
            query.Add(FbtAFMSHydroMeter.COL_DEVICE_NAME);
            query.Add(FbtAFMSHydroMeter.COL_DEVICE_NO);
            query.Add(FbtAFMSHydroMeter.COL_TRANSECT_CNT);
            query.OrderBy(FbtAFMSHydroMeter.COL_ID);

            DataTable hydros = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) return;

            int rowNo = 1;
            foreach (DataRow row in hydros.Rows)
            {
                int id = Convert.ToInt32(row[FbtAFMSHydroMeter.COL_ID]);
                int transectCount = row[FbtAFMSHydroMeter.COL_TRANSECT_CNT] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(row[FbtAFMSHydroMeter.COL_TRANSECT_CNT]);
                string name = Convert.ToString(row[FbtAFMSHydroMeter.COL_DEVICE_NAME])?.Trim() ?? $"유속계 {id}";

                AddDeviceRow(rowNo++, MeasurementDeviceType.VelocityMeter, id, transectCount, "유속계", name, configured);
            }

            AddDeviceRow(rowNo, MeasurementDeviceType.WaterLevelGauge, SYSTEM_WATER_LEVEL_DEVICE_ID,
                0, "수위계", "시스템 수위계", configured);

            uiGridMain.ClearSelection();
            uiGridMain.RefreshAFMSCheckBoxes();
        }

        public string SaveChanges(out int savedCount)
        {
            savedCount = 0;

            foreach (DataGridViewRow row in uiGridMain.Rows)
            {
                if (row.IsNewRow || !IsRowChanged(row)) continue;

                MeasurementDeviceType deviceType = GetDeviceType(row);
                int deviceId = Convert.ToInt32(row.Cells[COL_DEVICE_ID].Value);
                string deviceKey = GetDeviceKey(deviceType, deviceId);
                Dictionary<DischargeMethod, bool> original = _originalValues[deviceKey];
                bool rowSaved = false;

                foreach (DischargeMethod method in _methods)
                {
                    bool enabled = GetCurrentValue(row, method);
                    if (original.TryGetValue(method, out bool oldValue) && oldValue == enabled) continue;

                    int id = FBProvider.Instance.GetNextID(FbtAFMSDischargeConfig.TABLE_NAME);
                    DateTime now = DateTime.Now;
                    string methodConfigKey = GetMethodKey(deviceType, deviceId, method);
                    string methodConfigId = _methodConfigIds.TryGetValue(methodConfigKey, out int configId)
                        ? configId.ToString()
                        : "NULL";

                    string sql = $"INSERT INTO {FbtAFMSDischargeConfig.TABLE_NAME} (";
                    sql += $"{FbtAFMSDischargeConfig.COL_ID}, {FbtAFMSDischargeConfig.COL_MEASURE_DATE}, ";
                    sql += $"{FbtAFMSDischargeConfig.COL_MEASURE_TIME}, {FbtAFMSDischargeConfig.COL_DEVICE_TYPE}, ";
                    sql += $"{FbtAFMSDischargeConfig.COL_DEVICE_ID}, {FbtAFMSDischargeConfig.COL_DISCHARGE_METHOD}, ";
                    sql += $"{FbtAFMSDischargeConfig.COL_METHOD_CONFIG_ID}, {FbtAFMSDischargeConfig.COL_ENABLED}) VALUES (";
                    sql += $"{id}, '{now:yyyyMMdd}', '{now:HHmmss}', '{deviceType}', {deviceId}, '{method}', ";
                    sql += $"{methodConfigId}, {(enabled ? 1 : 0)})";

                    using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
                    string error = db.RunNonQuery(sql);
                    if (!string.IsNullOrEmpty(error)) return error;

                    original[method] = enabled;
                    rowSaved = true;
                }

                if (rowSaved) savedCount++;
            }

            return string.Empty;
        }

        private void AddDeviceRow(
            int rowNo,
            MeasurementDeviceType deviceType,
            int deviceId,
            int transectCount,
            string deviceKind,
            string deviceName,
            Dictionary<string, bool> configured)
        {
            int rowIndex = uiGridMain.Rows.Add();
            DataGridViewRow row = uiGridMain.Rows[rowIndex];
            row.Cells[COL_DEVICE_TYPE].Value = deviceType.ToString();
            row.Cells[COL_DEVICE_ID].Value = deviceId;
            row.Cells[COL_TRANSECT_COUNT].Value = transectCount;
            row.Cells[COL_ROW_NO].Value = rowNo;
            row.Cells[COL_DEVICE_KIND].Value = deviceKind;
            row.Cells[COL_DEVICE_NAME].Value = deviceName;

            Dictionary<DischargeMethod, bool> original = new();
            foreach (DischargeMethod method in _methods)
            {
                bool available = IsMethodAvailable(deviceType, deviceId, transectCount, method);
                bool enabled = available && configured.TryGetValue(GetMethodKey(deviceType, deviceId, method), out bool value) && value;
                string columnName = GetGridMethodColumnName(method);

                row.Cells[columnName].Value = enabled;
                uiGridMain.SetAFMSCheckBoxVisible(rowIndex, columnName, available);
                original[method] = enabled;
            }

            _originalValues[GetDeviceKey(deviceType, deviceId)] = original;
        }

        private Dictionary<string, bool> LoadLatestSelections(FBDatabase db)
        {
            Dictionary<string, bool> result = new();
            string sql = $"SELECT C.{FbtAFMSDischargeConfig.COL_DEVICE_TYPE}, C.{FbtAFMSDischargeConfig.COL_DEVICE_ID},";
            sql += $" C.{FbtAFMSDischargeConfig.COL_DISCHARGE_METHOD}, C.{FbtAFMSDischargeConfig.COL_ENABLED}";
            sql += $" FROM {FbtAFMSDischargeConfig.TABLE_NAME} C WHERE C.{FbtAFMSDischargeConfig.COL_ID} = (";
            sql += $"SELECT MAX(C2.{FbtAFMSDischargeConfig.COL_ID}) FROM {FbtAFMSDischargeConfig.TABLE_NAME} C2";
            sql += $" WHERE C2.{FbtAFMSDischargeConfig.COL_DEVICE_TYPE} = C.{FbtAFMSDischargeConfig.COL_DEVICE_TYPE}";
            sql += $" AND C2.{FbtAFMSDischargeConfig.COL_DEVICE_ID} = C.{FbtAFMSDischargeConfig.COL_DEVICE_ID}";
            sql += $" AND C2.{FbtAFMSDischargeConfig.COL_DISCHARGE_METHOD} = C.{FbtAFMSDischargeConfig.COL_DISCHARGE_METHOD})";

            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error)) return result;

            foreach (DataRow row in table.Rows)
            {
                if (!Enum.TryParse(Convert.ToString(row[FbtAFMSDischargeConfig.COL_DEVICE_TYPE]), true, out MeasurementDeviceType type) ||
                    !Enum.TryParse(Convert.ToString(row[FbtAFMSDischargeConfig.COL_DISCHARGE_METHOD]), true, out DischargeMethod method)) continue;

                int deviceId = Convert.ToInt32(row[FbtAFMSDischargeConfig.COL_DEVICE_ID]);
                result[GetMethodKey(type, deviceId, method)] = Convert.ToInt32(row[FbtAFMSDischargeConfig.COL_ENABLED]) == 1;
            }

            return result;
        }

        private void LoadMethodAvailability(FBDatabase db)
        {
            _surfaceConfiguredHydroIds.Clear();
            _midSectionConfiguredHydroIds.Clear();
            _velocityDistributionConfiguredHydroIds.Clear();
            _methodConfigIds.Clear();
            _transectCounts.Clear();
            _latestTransectConfigIds.Clear();
            _staleHydroIds.Clear();

            LoadLatestMethodConfigIds(db, FbtAFMSDiscAttrSurfaceVelo.TABLE_NAME,
                FbtAFMSDiscAttrSurfaceVelo.COL_HYDRO_ID, DischargeMethod.SurfaceVelo, _surfaceConfiguredHydroIds);
            LoadLatestMethodConfigIds(db, FbtAFMSDiscAttrMidSection.TABLE_NAME,
                FbtAFMSDiscAttrMidSection.COL_HYDRO_ID, DischargeMethod.MidSection, _midSectionConfiguredHydroIds);
            LoadLatestMethodConfigIds(db, FbtAFMSDiscAttrVelocityDistribution.TABLE_NAME,
                FbtAFMSDiscAttrVelocityDistribution.COL_HYDRO_ID, DischargeMethod.VeloDist, _velocityDistributionConfiguredHydroIds);

            string ratingSql = $"SELECT FIRST 1 {FbtAFMSDiscAttrRatingCurve.COL_ID} FROM {FbtAFMSDiscAttrRatingCurve.TABLE_NAME} ORDER BY {FbtAFMSDiscAttrRatingCurve.COL_ID} DESC";
            DataTable rating = db.Execute(ratingSql, out string ratingError);
            _hasRatingCurveConfig = string.IsNullOrEmpty(ratingError) && rating.Rows.Count > 0;
            if (_hasRatingCurveConfig)
            {
                _methodConfigIds[GetMethodKey(MeasurementDeviceType.WaterLevelGauge,
                    SYSTEM_WATER_LEVEL_DEVICE_ID, DischargeMethod.RatingCurve)] = Convert.ToInt32(rating.Rows[0][0]);
            }

            string transectSql = $"SELECT A.{FbtAFMSHydroTransect.COL_ID}, A.{FbtAFMSHydroTransect.COL_HYDRO_ID}, A.{FbtAFMSHydroTransect.COL_TRANSECT_COUNT}";
            transectSql += $" FROM {FbtAFMSHydroTransect.TABLE_NAME} A WHERE A.{FbtAFMSHydroTransect.COL_ID} = (";
            transectSql += $"SELECT MAX(B.{FbtAFMSHydroTransect.COL_ID}) FROM {FbtAFMSHydroTransect.TABLE_NAME} B";
            transectSql += $" WHERE B.{FbtAFMSHydroTransect.COL_HYDRO_ID} = A.{FbtAFMSHydroTransect.COL_HYDRO_ID})";
            DataTable transects = db.Execute(transectSql, out string transectError);
            if (string.IsNullOrEmpty(transectError))
            {
                foreach (DataRow row in transects.Rows)
                {
                    int hydroId = Convert.ToInt32(row[FbtAFMSHydroTransect.COL_HYDRO_ID]);
                    _transectCounts[hydroId] = Convert.ToInt32(row[FbtAFMSHydroTransect.COL_TRANSECT_COUNT]);
                    _latestTransectConfigIds[hydroId] = Convert.ToInt32(row[FbtAFMSHydroTransect.COL_ID]);
                }
            }
            LoadStaleHydroIds(db);
        }

        private void LoadLatestMethodConfigIds(FBDatabase db, string tableName, string hydroColumn,
            DischargeMethod method, HashSet<int> configuredIds)
        {
            string sql = $"SELECT A.{FbtAFMSDischargeConfig.COL_ID}, A.{hydroColumn} FROM {tableName} A";
            sql += $" WHERE A.{FbtAFMSDischargeConfig.COL_ID} = (SELECT MAX(B.{FbtAFMSDischargeConfig.COL_ID})";
            sql += $" FROM {tableName} B WHERE B.{hydroColumn} = A.{hydroColumn})";
            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error)) return;

            foreach (DataRow row in table.Rows)
            {
                int hydroId = Convert.ToInt32(row[hydroColumn]);
                configuredIds.Add(hydroId);
                _methodConfigIds[GetMethodKey(MeasurementDeviceType.VelocityMeter, hydroId, method)] = Convert.ToInt32(row[0]);
            }
        }

        private void LoadStaleHydroIds(FBDatabase db)
        {
            Dictionary<int, HashSet<DischargeMethod>> freshMethods = new();
            string sql = $"SELECT C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID}, C.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD}, C.{FbtAFMSDischargeMethodConfig.COL_TRANSECT_CONFIG_ID}";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C";
            sql += $" WHERE C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE} = '{MeasurementDeviceType.VelocityMeter}'";
            sql += $" AND C.{FbtAFMSDischargeMethodConfig.COL_ID} = (SELECT MAX(C2.{FbtAFMSDischargeMethodConfig.COL_ID})";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C2";
            sql += $" WHERE C2.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE} = C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE}";
            sql += $" AND C2.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID} = C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID}";
            sql += $" AND C2.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD} = C.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD})";

            DataTable table = db.Execute(sql, out string error);
            if (string.IsNullOrEmpty(error))
            {
                foreach (DataRow row in table.Rows)
                {
                    int hydroId = Convert.ToInt32(row[FbtAFMSDischargeMethodConfig.COL_DEVICE_ID]);
                    int transectId = row[FbtAFMSDischargeMethodConfig.COL_TRANSECT_CONFIG_ID] == DBNull.Value
                        ? -1 : Convert.ToInt32(row[FbtAFMSDischargeMethodConfig.COL_TRANSECT_CONFIG_ID]);
                    if (!_latestTransectConfigIds.TryGetValue(hydroId, out int latestId) || transectId != latestId)
                    {
                        _staleHydroIds.Add(hydroId);
                        continue;
                    }
                    if (!Enum.TryParse(Convert.ToString(row[FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD]), out DischargeMethod method)) continue;
                    if (!freshMethods.TryGetValue(hydroId, out HashSet<DischargeMethod>? methods))
                    {
                        methods = new HashSet<DischargeMethod>();
                        freshMethods[hydroId] = methods;
                    }
                    methods.Add(method);
                }
            }

            IEnumerable<int> configuredHydros = _surfaceConfiguredHydroIds
                .Union(_midSectionConfiguredHydroIds)
                .Union(_velocityDistributionConfiguredHydroIds);
            foreach (int hydroId in configuredHydros)
            {
                int required = (_surfaceConfiguredHydroIds.Contains(hydroId) ? 1 : 0) +
                               (_midSectionConfiguredHydroIds.Contains(hydroId) ? 1 : 0) +
                               (_velocityDistributionConfiguredHydroIds.Contains(hydroId) ? 1 : 0);
                if (!freshMethods.TryGetValue(hydroId, out HashSet<DischargeMethod>? methods) || methods.Count < required)
                    _staleHydroIds.Add(hydroId);
            }
        }

        private bool IsMethodAvailable(MeasurementDeviceType type, int deviceId, int transectCount, DischargeMethod method)
        {
            if (type == MeasurementDeviceType.WaterLevelGauge)
                return method == DischargeMethod.RatingCurve && _hasRatingCurveConfig;
            if (type != MeasurementDeviceType.VelocityMeter || method == DischargeMethod.RatingCurve) return false;
            if (_staleHydroIds.Contains(deviceId)) return false;

            return method switch
            {
                DischargeMethod.SurfaceVelo => _surfaceConfiguredHydroIds.Contains(deviceId),
                DischargeMethod.MidSection => _midSectionConfiguredHydroIds.Contains(deviceId) &&
                    _transectCounts.TryGetValue(deviceId, out int count) && count == transectCount && count > 0,
                DischargeMethod.VeloDist => _velocityDistributionConfiguredHydroIds.Contains(deviceId),
                _ => false
            };
        }

        private bool IsRowChanged(DataGridViewRow row)
        {
            MeasurementDeviceType type = GetDeviceType(row);
            int deviceId = Convert.ToInt32(row.Cells[COL_DEVICE_ID].Value);
            if (!_originalValues.TryGetValue(GetDeviceKey(type, deviceId), out Dictionary<DischargeMethod, bool>? original)) return true;

            return _methods.Any(method => original.GetValueOrDefault(method) != GetCurrentValue(row, method));
        }

        private bool GetCurrentValue(DataGridViewRow row, DischargeMethod method)
        {
            MeasurementDeviceType type = GetDeviceType(row);
            int deviceId = Convert.ToInt32(row.Cells[COL_DEVICE_ID].Value);
            int transectCount = Convert.ToInt32(row.Cells[COL_TRANSECT_COUNT].Value ?? 0);
            return IsMethodAvailable(type, deviceId, transectCount, method) &&
                   uiGridMain.GetAFMSChecked(row.Index, GetGridMethodColumnName(method));
        }

        private void SetupLayout()
        {
            uiGuide = new AFMSGuidePanel
            {
                Dock = DockStyle.Fill,
                BackColor = DllColorHelper.HexToColor("#FAFDFA"),
                Title = "설정 안내"
            };
            uiGuide.Add(GuideLevelType.Level0, "유속계에는 유속 기반 산정법만 표시됩니다.");
            uiGuide.Add(GuideLevelType.Level0, "수위계에는 수위-유량 관계법만 표시됩니다.");
            uiGuide.Add(GuideLevelType.Level0, "변경된 설정은 유량 서비스를 재시작한 후 반영됩니다.");
            uiGuide.Add(GuideLevelType.Level0, "최신 측선이 입력되면 해당 유속계의 모든 유량 산정법을 다시 설정해야 합니다.");
            CtlSub = uiGuide;
        }

        private void SetupGrid()
        {
            uiGridMain.AutoGenerateColumns = false;
            uiGridMain.ShowCellToolTips = false;
            uiGridMain.Columns.Clear();
            uiGridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = COL_DEVICE_TYPE, Visible = false });
            uiGridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = COL_DEVICE_ID, Visible = false });
            uiGridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = COL_TRANSECT_COUNT, Visible = false });
            uiGridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = COL_ROW_NO, HeaderText = "번호", FillWeight = 9F, ReadOnly = true });
            uiGridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = COL_DEVICE_KIND, HeaderText = "장비 유형", FillWeight = 14F, ReadOnly = true });
            uiGridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = COL_DEVICE_NAME, HeaderText = "측정장비", FillWeight = 25F, ReadOnly = true });
            foreach (DischargeMethod method in _methods) uiGridMain.Columns.Add(CreateMethodColumn(method));
            uiGridMain.AFMSHeaderHeight = 42;
            uiGridMain.AFMSRowHeight = 54;
            uiGridMain.BorderRadius = 8;
        }

        private DataGridViewTextBoxColumn CreateMethodColumn(DischargeMethod method) => new()
        {
            Name = GetGridMethodColumnName(method),
            HeaderText = string.Empty,
            FillWeight = 52F / Math.Max(1, _methods.Count),
            ReadOnly = true,
            Tag = method,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };

        private void SetupMethodCheckBoxes()
        {
            foreach (DischargeMethod method in _methods)
                uiGridMain.SetAFMSCheckBoxColumn(GetGridMethodColumnName(method), EnumPaser.GetKorString(method));
            uiGridMain.AFMSCheckBoxCellVisibleEvaluator = IsMethodCheckBoxVisible;
        }

        private bool IsMethodCheckBoxVisible(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= uiGridMain.Rows.Count ||
                uiGridMain.Columns[columnIndex].Tag is not DischargeMethod method) return false;
            DataGridViewRow row = uiGridMain.Rows[rowIndex];
            return IsMethodAvailable(GetDeviceType(row), Convert.ToInt32(row.Cells[COL_DEVICE_ID].Value),
                Convert.ToInt32(row.Cells[COL_TRANSECT_COUNT].Value ?? 0), method);
        }

        private static MeasurementDeviceType GetDeviceType(DataGridViewRow row) =>
            Enum.TryParse(Convert.ToString(row.Cells[COL_DEVICE_TYPE].Value), out MeasurementDeviceType type)
                ? type
                : MeasurementDeviceType.None;

        private static string GetDeviceKey(MeasurementDeviceType type, int id) => $"{type}:{id}";
        private static string GetMethodKey(MeasurementDeviceType type, int id, DischargeMethod method) => $"{type}:{id}:{method}";
        private static string GetGridMethodColumnName(DischargeMethod method) => $"METHOD_{(int)method}";

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible || uiGridMain == null || !IsHandleCreated) return;
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed || !Visible) return;
                uiGridMain.PerformLayout();
                uiGridMain.RefreshAFMSCheckBoxes();
            }));
        }

        public override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e) { }
        protected override void UiButtonInput_Click(object? sender, EventArgs e) { }
        protected override void _TabDischargeBase_Enter(object? sender, EventArgs e) => LoadData();
    }
}
