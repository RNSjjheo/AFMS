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
        private const int SYSTEM_WATER_LEVEL_DEVICE_ID = 0;

        private sealed class DeviceRowState
        {
            public required MeasurementDeviceType DeviceType { get; init; }
            public required int DeviceId { get; init; }
            public Dictionary<DischargeMethod, bool> Original { get; } = new();
            public Dictionary<DischargeMethod, AFMSCheckBox> CheckBoxes { get; } = new();
        }

        private readonly List<DischargeMethod> _methods = new();
        private readonly Dictionary<string, Dictionary<DischargeMethod, bool>> _originalValues = new();
        private readonly Dictionary<string, int> _methodConfigIds = new();
        private readonly HashSet<int> _surfaceConfiguredHydroIds = new();
        private readonly HashSet<int> _midSectionConfiguredHydroIds = new();
        private readonly HashSet<int> _velocityDistributionConfiguredHydroIds = new();
        private readonly Dictionary<int, int> _transectCounts = new();
        private readonly Dictionary<int, int> _latestTransectConfigIds = new();
        private readonly HashSet<int> _staleHydroIds = new();
        private readonly List<DeviceRowState> _deviceRows = new();
        private bool _hasRatingCurveConfig;
        private AFMSGuidePanel uiGuide;
        private Panel uiDeviceListHost;
        private TableLayoutPanel uiDeviceTable;

        public TabDischargeMapping() : base(false)
        {
            Text = "유량 산정 선택";
            BackColor = Color.White;
            Padding = Padding.Empty;

            foreach (DischargeMethod method in Enum.GetValues(typeof(DischargeMethod)))
                if (method != DischargeMethod.None) _methods.Add(method);

            SetupLayout();
            SetupDeviceList();

            uiTpMain.SetRowSpan(uiDeviceListHost, 2);
            uiTpMain.SetRowSpan(uiGuide, 2);
            uiTpMain.Controls.Remove(uiButtonInput);
        }

        public bool HasChanges => _deviceRows.Any(IsRowChanged);

        public void LoadData()
        {
            ResetDeviceRows();
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

        }

        public string SaveChanges(out int savedCount)
        {
            savedCount = 0;

            foreach (DeviceRowState row in _deviceRows)
            {
                if (!IsRowChanged(row)) continue;

                MeasurementDeviceType deviceType = row.DeviceType;
                int deviceId = row.DeviceId;
                string deviceKey = GetDeviceKey(deviceType, deviceId);
                Dictionary<DischargeMethod, bool> original = _originalValues[deviceKey];
                bool rowSaved = false;

                foreach (DischargeMethod method in _methods)
                {
                    bool enabled = GetCurrentValue(row, method);
                    if (original.TryGetValue(method, out bool oldValue) && oldValue == enabled) continue;

                    string error = DischargeMethodConfigStore.SetEnabled(deviceType, deviceId, method, enabled);
                    if (!string.IsNullOrEmpty(error)) return error;

                    original[method] = enabled;
                    row.Original[method] = enabled;
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
            DeviceRowState state = new() { DeviceType = deviceType, DeviceId = deviceId };
            Dictionary<DischargeMethod, bool> original = new();
            FlowLayoutPanel panel = CreateMethodPanel();
            foreach (DischargeMethod method in _methods)
            {
                bool available = IsMethodAvailable(deviceType, deviceId, transectCount, method);
                bool enabled = available && configured.TryGetValue(GetMethodKey(deviceType, deviceId, method), out bool value) && value;
                original[method] = enabled;
                if (!available) continue;

                AFMSCheckBox checkBox = new()
                {
                    Text = EnumPaser.GetKorString(method),
                    Checked = enabled,
                    AutoSize = true,
                    Margin = new Padding(4, 7, 4, 7)
                };
                panel.Controls.Add(checkBox);
                state.CheckBoxes[method] = checkBox;
            }

            string key = GetDeviceKey(deviceType, deviceId);
            _originalValues[key] = original;
            foreach ((DischargeMethod method, bool enabled) in original) state.Original[method] = enabled;
            _deviceRows.Add(state);

            int tableRow = uiDeviceTable.RowCount++;
            uiDeviceTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            uiDeviceTable.Controls.Add(CreateCellLabel(rowNo.ToString()), 0, tableRow);
            uiDeviceTable.Controls.Add(CreateCellLabel(deviceKind), 1, tableRow);
            uiDeviceTable.Controls.Add(CreateCellLabel(deviceName), 2, tableRow);
            uiDeviceTable.Controls.Add(panel, 3, tableRow);
        }

        private Dictionary<string, bool> LoadLatestSelections(FBDatabase db)
        {
            Dictionary<string, bool> result = new();
            string sql = $"SELECT C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE}, C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID},";
            sql += $" C.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD}, C.{FbtAFMSDischargeMethodConfig.COL_ENABLED}";
            sql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C WHERE C.{FbtAFMSDischargeMethodConfig.COL_ID} = (";
            sql += $"SELECT MAX(C2.{FbtAFMSDischargeMethodConfig.COL_ID}) FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C2";
            sql += $" WHERE C2.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE} = C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE}";
            sql += $" AND C2.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID} = C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID}";
            sql += $" AND C2.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD} = C.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD})";

            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error)) return result;

            foreach (DataRow row in table.Rows)
            {
                if (!Enum.TryParse(Convert.ToString(row[FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE]), true, out MeasurementDeviceType type) ||
                    !Enum.TryParse(Convert.ToString(row[FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD]), true, out DischargeMethod method)) continue;

                int deviceId = Convert.ToInt32(row[FbtAFMSDischargeMethodConfig.COL_DEVICE_ID]);
                result[GetMethodKey(type, deviceId, method)] = Convert.ToInt32(row[FbtAFMSDischargeMethodConfig.COL_ENABLED]) == 1;
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
            _hasRatingCurveConfig = false;

            string methodSql = $"SELECT C.{FbtAFMSDischargeMethodConfig.COL_ID}, C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE}, C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID}, C.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD}";
            methodSql += $" FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C WHERE C.{FbtAFMSDischargeMethodConfig.COL_ID} = (SELECT MAX(C2.{FbtAFMSDischargeMethodConfig.COL_ID}) FROM {FbtAFMSDischargeMethodConfig.TABLE_NAME} C2";
            methodSql += $" WHERE C2.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE} = C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE}";
            methodSql += $" AND C2.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID} = C.{FbtAFMSDischargeMethodConfig.COL_DEVICE_ID}";
            methodSql += $" AND C2.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD} = C.{FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD})";
            DataTable methodConfigs = db.Execute(methodSql, out string methodError);
            if (string.IsNullOrEmpty(methodError))
            {
                foreach (DataRow row in methodConfigs.Rows)
                {
                    if (!Enum.TryParse(row[FbtAFMSDischargeMethodConfig.COL_DEVICE_TYPE].ToText(), out MeasurementDeviceType type) ||
                        !Enum.TryParse(row[FbtAFMSDischargeMethodConfig.COL_DISCHARGE_METHOD].ToText(), out DischargeMethod method)) continue;
                    int deviceId = Convert.ToInt32(row[FbtAFMSDischargeMethodConfig.COL_DEVICE_ID]);
                    _methodConfigIds[GetMethodKey(type, deviceId, method)] = Convert.ToInt32(row[FbtAFMSDischargeMethodConfig.COL_ID]);
                    if (type == MeasurementDeviceType.WaterLevelGauge && method == DischargeMethod.RatingCurve) _hasRatingCurveConfig = true;
                    if (type != MeasurementDeviceType.VelocityMeter) continue;
                    if (method == DischargeMethod.SurfaceVelo) _surfaceConfiguredHydroIds.Add(deviceId);
                    if (method == DischargeMethod.MidSection) _midSectionConfiguredHydroIds.Add(deviceId);
                    if (method == DischargeMethod.VeloDist) _velocityDistributionConfiguredHydroIds.Add(deviceId);
                }
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

        private bool IsRowChanged(DeviceRowState row)
        {
            return _methods.Any(method => row.Original.GetValueOrDefault(method) != GetCurrentValue(row, method));
        }

        private static bool GetCurrentValue(DeviceRowState row, DischargeMethod method)
        {
            return row.CheckBoxes.TryGetValue(method, out AFMSCheckBox? checkBox) && checkBox.Checked;
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

        private void SetupDeviceList()
        {
            uiDeviceTable = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                RowCount = 1,
                Dock = DockStyle.Top,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = Color.White
            };
            uiDeviceTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9F));
            uiDeviceTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            uiDeviceTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            uiDeviceTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            uiDeviceTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            uiDeviceTable.Controls.Add(CreateHeaderLabel("번호"), 0, 0);
            uiDeviceTable.Controls.Add(CreateHeaderLabel("장비 유형"), 1, 0);
            uiDeviceTable.Controls.Add(CreateHeaderLabel("측정장비"), 2, 0);
            uiDeviceTable.Controls.Add(CreateHeaderLabel("산정법", ContentAlignment.MiddleLeft), 3, 0);

            uiDeviceListHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            uiDeviceListHost.Controls.Add(uiDeviceTable);
            CtlMain = uiDeviceListHost;
        }

        private static FlowLayoutPanel CreateMethodPanel() => new()
        {
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(4, 0, 0, 0)
        };

        private static Label CreateHeaderLabel(string text, ContentAlignment alignment = ContentAlignment.MiddleCenter) => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = alignment,
            Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Bold),
            ForeColor = DllColorHelper.HexToColor("#244B37"),
            BackColor = DllColorHelper.HexToColor("#F7F9F8"),
            Margin = Padding.Empty
        };

        private static Label CreateCellLabel(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.White,
            Margin = Padding.Empty
        };

        private void ResetDeviceRows()
        {
            _deviceRows.Clear();
            while (uiDeviceTable.RowCount > 1)
            {
                int row = uiDeviceTable.RowCount - 1;
                foreach (Control control in uiDeviceTable.Controls.Cast<Control>()
                             .Where(control => uiDeviceTable.GetRow(control) == row).ToArray())
                {
                    uiDeviceTable.Controls.Remove(control);
                    control.Dispose();
                }
                uiDeviceTable.RowStyles.RemoveAt(row);
                uiDeviceTable.RowCount--;
            }
        }

        private static string GetDeviceKey(MeasurementDeviceType type, int id) => $"{type}:{id}";
        private static string GetMethodKey(MeasurementDeviceType type, int id, DischargeMethod method) => $"{type}:{id}:{method}";

        public override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e) { }
        protected override void UiButtonInput_Click(object? sender, EventArgs e) { }
        protected override void _TabDischargeBase_Enter(object? sender, EventArgs e) => LoadData();
    }
}
