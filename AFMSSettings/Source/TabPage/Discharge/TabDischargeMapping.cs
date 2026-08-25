using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class TabDischargeMapping : _TabDischargeBase
    {
        private const string COL_HYDRO_ID = "HYDRO_ID";
        private const string COL_ROW_NO = "ROW_NO";
        private const string COL_FLOWMETER = "FLOWMETER";

        private readonly List<DischargeMethod> _DischargeMethods = new List<DischargeMethod>();
        private readonly Dictionary<int, Dictionary<DischargeMethod, bool>> _OriginalValues = new Dictionary<int, Dictionary<DischargeMethod, bool>>();
        private readonly HashSet<int> _SurfaceConfiguredHydroIds = new HashSet<int>();
        private readonly HashSet<int> _MidSectionConfiguredHydroIds = new HashSet<int>();
        private readonly Dictionary<int, TransectSetting> _TransectSettings = new Dictionary<int, TransectSetting>();
        private bool _HasRatingCurveConfig;

        private sealed class TransectSetting
        {
            public int Count { get; set; }
            public string DistanceDatas { get; set; } = string.Empty;
        }

        private AFMSGuidePanel uiGuide;

        public TabDischargeMapping()
            : base(false)
        {
            Text = "유량 산정 선택";
            BackColor = Color.White;
            Padding = Padding.Empty;

            SetupDischargeMethods();
            SetupLayout();
            SetupGrid();
            SetupMethodCheckBoxes();

            uiTpMain.SetRowSpan(uiGridMain, 2);
            uiTpMain.SetRowSpan(uiGuide, 2);

            uiTpMain.Controls.Remove(uiButtonInput);
        }

        public bool HasChanges
        {
            get
            {
                foreach (DataGridViewRow row in uiGridMain.Rows)
                {
                    if (!row.IsNewRow && IsRowChanged(row)) return true;
                }

                return false;
            }
        }

        private void AddGridRow(DataRow dataRow, int rowNo)
        {
            int hydroId = Convert.ToInt32(dataRow[COL_HYDRO_ID]);
            int transectCount = dataRow[FbtAFMSHydroMeter.COL_TRANSECT_CNT] == DBNull.Value
                ? 0
                : Convert.ToInt32(dataRow[FbtAFMSHydroMeter.COL_TRANSECT_CNT]);
            string deviceName = Convert.ToString(dataRow[FbtAFMSHydroMeter.COL_DEVICE_NAME])?.Trim() ?? string.Empty;

            int rowIndex = uiGridMain.Rows.Add();
            DataGridViewRow gridRow = uiGridMain.Rows[rowIndex];

            gridRow.Cells[COL_HYDRO_ID].Value = hydroId;
            gridRow.Cells[FbtAFMSHydroMeter.COL_TRANSECT_CNT].Value = transectCount;
            gridRow.Cells[COL_ROW_NO].Value = rowNo;
            gridRow.Cells[COL_FLOWMETER].Value = deviceName;

            Dictionary<DischargeMethod, bool> original = new Dictionary<DischargeMethod, bool>();

            foreach (DischargeMethod method in _DischargeMethods)
            {
                string dbColumn = FbtAFMSDischargeConfig.GetMethodColumn(method);
                bool isAvailable = IsMethodAvailable(hydroId, transectCount, method);
                bool isChecked = isAvailable && dataRow[dbColumn] != DBNull.Value && Convert.ToInt32(dataRow[dbColumn]) == 1;
                string gridColumn = GetGridMethodColumnName(method);

                gridRow.Cells[gridColumn].Value = isChecked;
                uiGridMain.SetAFMSCheckBoxVisible(rowIndex, gridColumn, isAvailable);
                original[method] = isChecked;
            }

            _OriginalValues[hydroId] = original;
        }
        public void LoadData()
        {
            uiGridMain.ClearAFMSCheckBoxCellVisibility();
            uiGridMain.Rows.Clear();
            _OriginalValues.Clear();

            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;

            query.AsAlias(FbtAFMSHydroMeter.COL_ID, COL_HYDRO_ID);
            query.Add(FbtAFMSHydroMeter.COL_DEVICE_NAME);
            query.Add(FbtAFMSHydroMeter.COL_DEVICE_NO);
            query.Add(FbtAFMSHydroMeter.COL_TRANSECT_CNT);

            foreach (DischargeMethod method in _DischargeMethods) query.AddB(FbtAFMSDischargeConfig.GetMethodColumn(method));

            query.LeftJoinB.Table = FbtAFMSDischargeConfig.TABLE_NAME;
            query.LeftJoinB.AddRaw(
                $"B.{FbtAFMSDischargeConfig.COL_ID} = (" +
                $"SELECT MAX(B2.{FbtAFMSDischargeConfig.COL_ID}) " +
                $"FROM {FbtAFMSDischargeConfig.TABLE_NAME} B2 " +
                $"WHERE B2.{FbtAFMSDischargeConfig.COL_HYDRO_ID} = A.{FbtAFMSHydroMeter.COL_ID})");
            query.OrderBy(FbtAFMSHydroMeter.COL_ID);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            LoadMethodAvailability(db);
            DataTable table = db.Execute(query.Sql, out string error);

            if (!string.IsNullOrEmpty(error)) return;

            int rowNo = 1;

            foreach (DataRow row in table.Rows) AddGridRow(row, rowNo++);

            uiGridMain.ClearSelection();
        }

        private void LoadMethodAvailability(FBDatabase db)
        {
            _SurfaceConfiguredHydroIds.Clear();
            _MidSectionConfiguredHydroIds.Clear();
            _TransectSettings.Clear();
            _HasRatingCurveConfig = false;

            LoadConfiguredHydroIds(db, FbtAFMSDiscAttrSurfaceVelo.TABLE_NAME, FbtAFMSDiscAttrSurfaceVelo.COL_HYDRO_ID, _SurfaceConfiguredHydroIds);
            LoadConfiguredHydroIds(db, FbtAFMSDiscAttrMidSection.TABLE_NAME, FbtAFMSDiscAttrMidSection.COL_HYDRO_ID, _MidSectionConfiguredHydroIds);
            LoadLatestTransectSettings(db);

            string ratingSql = $"SELECT FIRST 1 {FbtAFMSDiscAttrRatingCurve.COL_ID} FROM {FbtAFMSDiscAttrRatingCurve.TABLE_NAME}";
            DataTable ratingTable = db.Execute(ratingSql, out string ratingError);
            _HasRatingCurveConfig = string.IsNullOrEmpty(ratingError) && ratingTable.Rows.Count > 0;
        }

        private static void LoadConfiguredHydroIds(FBDatabase db, string tableName, string hydroIdColumn, HashSet<int> target)
        {
            DataTable table = db.Execute($"SELECT DISTINCT {hydroIdColumn} FROM {tableName}", out string error);
            if (!string.IsNullOrEmpty(error)) return;

            foreach (DataRow row in table.Rows)
            {
                if (row[hydroIdColumn] != DBNull.Value) target.Add(Convert.ToInt32(row[hydroIdColumn]));
            }
        }

        private void LoadLatestTransectSettings(FBDatabase db)
        {
            string sql =
                $"SELECT A.{FbtAFMSHydroTransect.COL_HYDRO_ID}, A.{FbtAFMSHydroTransect.COL_TRANSECT_COUNT}, A.{FbtAFMSHydroTransect.COL_DISTANCE_DATAS} " +
                $"FROM {FbtAFMSHydroTransect.TABLE_NAME} A " +
                $"WHERE A.{FbtAFMSHydroTransect.COL_ID} = (" +
                $"SELECT MAX(B.{FbtAFMSHydroTransect.COL_ID}) FROM {FbtAFMSHydroTransect.TABLE_NAME} B " +
                $"WHERE B.{FbtAFMSHydroTransect.COL_HYDRO_ID} = A.{FbtAFMSHydroTransect.COL_HYDRO_ID})";

            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error)) return;

            foreach (DataRow row in table.Rows)
            {
                if (row[FbtAFMSHydroTransect.COL_HYDRO_ID] == DBNull.Value) continue;

                int hydroId = Convert.ToInt32(row[FbtAFMSHydroTransect.COL_HYDRO_ID]);
                _TransectSettings[hydroId] = new TransectSetting
                {
                    Count = row[FbtAFMSHydroTransect.COL_TRANSECT_COUNT] == DBNull.Value ? 0 : Convert.ToInt32(row[FbtAFMSHydroTransect.COL_TRANSECT_COUNT]),
                    DistanceDatas = Convert.ToString(row[FbtAFMSHydroTransect.COL_DISTANCE_DATAS])?.Trim() ?? string.Empty
                };
            }
        }

        private bool IsMethodAvailable(int hydroId, int transectCount, DischargeMethod method)
        {
            return method switch
            {
                DischargeMethod.SurfaceVelo => _SurfaceConfiguredHydroIds.Contains(hydroId),
                DischargeMethod.MidSection => _MidSectionConfiguredHydroIds.Contains(hydroId) && HasValidTransectSetting(hydroId, transectCount),
                DischargeMethod.RatingCurve => _HasRatingCurveConfig,
                _ => false
            };
        }

        private bool HasValidTransectSetting(int hydroId, int expectedCount)
        {
            if (expectedCount <= 0 || !_TransectSettings.TryGetValue(hydroId, out TransectSetting setting) ||
                setting.Count != expectedCount || string.IsNullOrWhiteSpace(setting.DistanceDatas)) return false;

            try
            {
                using JsonDocument document = JsonDocument.Parse(setting.DistanceDatas);
                if (!document.RootElement.TryGetProperty("transects", out JsonElement transects) ||
                    transects.ValueKind != JsonValueKind.Array || transects.GetArrayLength() != expectedCount) return false;

                double previousDistance = double.NegativeInfinity;
                int index = 0;

                foreach (JsonElement transect in transects.EnumerateArray())
                {
                    index++;
                    if (!transect.TryGetProperty("no", out JsonElement no) || no.GetInt32() != index ||
                        !transect.TryGetProperty("distance", out JsonElement distanceElement) || !distanceElement.TryGetDouble(out double distance) ||
                        distance <= previousDistance) return false;

                    previousDistance = distance;
                }

                return index == expectedCount;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public string SaveChanges(out int savedCount)
        {
            savedCount = 0;

            foreach (DataGridViewRow row in uiGridMain.Rows)
            {
                if (row.IsNewRow || !IsRowChanged(row)) continue;

                int hydroId = Convert.ToInt32(row.Cells[COL_HYDRO_ID].Value);
                int id = FBProvider.Instance.GetNextID(FbtAFMSDischargeConfig.TABLE_NAME);
                DateTime now = DateTime.Now;
                List<string> columns = new List<string> { FbtAFMSDischargeConfig.COL_ID, FbtAFMSDischargeConfig.COL_MEASURE_DATE, FbtAFMSDischargeConfig.COL_MEASURE_TIME, FbtAFMSDischargeConfig.COL_HYDRO_ID };
                List<string> values = new List<string> { id.ToString(), $"'{now:yyyyMMdd}'", $"'{now:HHmmss}'", hydroId.ToString() };

                foreach (DischargeMethod method in _DischargeMethods)
                {
                    columns.Add(FbtAFMSDischargeConfig.GetMethodColumn(method));
                    values.Add(GetCurrentValue(row, method) ? "1" : "0");
                }

                string sql = $"INSERT INTO {FbtAFMSDischargeConfig.TABLE_NAME} ({string.Join(", ", columns)})";
                sql += "\n" + $"VALUES ({string.Join(", ", values)})";

                using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
                string error = db.RunNonQuery(sql);
                if (!string.IsNullOrEmpty(error)) return error;

                _OriginalValues[hydroId] = CaptureCurrentValues(row);
                savedCount++;
            }

            return string.Empty;
        }

        private void SetupDischargeMethods()
        {
            _DischargeMethods.Clear();
            foreach (DischargeMethod method in Enum.GetValues(typeof(DischargeMethod))) if (method != DischargeMethod.None) _DischargeMethods.Add(method);
        }

        private void SetupLayout()
        {
            uiGuide = new AFMSGuidePanel();
            uiGuide.Dock = DockStyle.Fill;
            uiGuide.BackColor = DllColorHelper.HexToColor("#FAFDFA");
            uiGuide.Title = "설정 안내";
            uiGuide.Add(GuideLevelType.Level0, "유속계별 설정이 1개 이상 등록된 유량산정법만 표시됩니다.");
            uiGuide.Add(GuideLevelType.Level0, "중간단면적법은 측선 수와 실제 측선 거리 설정이 모두 일치해야 표시됩니다.");
            uiGuide.Add(GuideLevelType.Level0, "체크 변경만으로 DB에 저장되지 않습니다.");
            uiGuide.Add(GuideLevelType.Level0, "상단의 저장 버튼을 누르면 변경된 유속계만 새 설정 이력으로 저장됩니다.");

            CtlSub = uiGuide;
        }

        private void SetupGrid()
        {
            uiGridMain.AutoGenerateColumns = false;
            uiGridMain.ShowCellToolTips = false;
            uiGridMain.Columns.Clear();

            DataGridViewTextBoxColumn colHydroId = new DataGridViewTextBoxColumn { Name = COL_HYDRO_ID, Visible = false };
            DataGridViewTextBoxColumn colTransectCount = new DataGridViewTextBoxColumn { Name = FbtAFMSHydroMeter.COL_TRANSECT_CNT, Visible = false };
            DataGridViewTextBoxColumn colNo = new DataGridViewTextBoxColumn { Name = COL_ROW_NO, HeaderText = "번호", FillWeight = 10F, ReadOnly = true };
            DataGridViewTextBoxColumn colFlowmeter = new DataGridViewTextBoxColumn { Name = COL_FLOWMETER, HeaderText = "유속계", FillWeight = 25F, ReadOnly = true };
            colNo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colFlowmeter.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colFlowmeter.DefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            colFlowmeter.DefaultCellStyle.Padding = new Padding(18, 0, 8, 0);

            uiGridMain.Columns.Add(colHydroId);
            uiGridMain.Columns.Add(colTransectCount);
            uiGridMain.Columns.Add(colNo);
            uiGridMain.Columns.Add(colFlowmeter);
            foreach (DischargeMethod method in _DischargeMethods) uiGridMain.Columns.Add(CreateMethodColumn(method));

            uiGridMain.AFMSHeaderHeight = 42;
            uiGridMain.AFMSRowHeight = 54;
            uiGridMain.BorderRadius = 8;
        }

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

        private DataGridViewTextBoxColumn CreateMethodColumn(DischargeMethod method)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = GetGridMethodColumnName(method),
                HeaderText = string.Empty,
                FillWeight = 65F / Math.Max(1, _DischargeMethods.Count),
                ReadOnly = true,
                Tag = method,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
        }

        private void SetupMethodCheckBoxes()
        {
            foreach (DischargeMethod method in _DischargeMethods) uiGridMain.SetAFMSCheckBoxColumn(GetGridMethodColumnName(method), EnumPaser.GetKorString(method));
        }

        private bool IsRowChanged(DataGridViewRow row)
        {
            int hydroId = Convert.ToInt32(row.Cells[COL_HYDRO_ID].Value);
            if (!_OriginalValues.TryGetValue(hydroId, out Dictionary<DischargeMethod, bool> original)) return true;

            foreach (DischargeMethod method in _DischargeMethods)
            {
                bool oldValue = original.TryGetValue(method, out bool value) && value;
                if (oldValue != GetCurrentValue(row, method)) return true;
            }

            return false;
        }

        private Dictionary<DischargeMethod, bool> CaptureCurrentValues(DataGridViewRow row)
        {
            Dictionary<DischargeMethod, bool> values = new Dictionary<DischargeMethod, bool>();
            foreach (DischargeMethod method in _DischargeMethods) values[method] = GetCurrentValue(row, method);
            return values;
        }

        private bool GetCurrentValue(DataGridViewRow row, DischargeMethod method)
        {
            int hydroId = Convert.ToInt32(row.Cells[COL_HYDRO_ID].Value);
            int transectCount = Convert.ToInt32(row.Cells[FbtAFMSHydroMeter.COL_TRANSECT_CNT].Value ?? 0);
            if (!IsMethodAvailable(hydroId, transectCount, method)) return false;

            return uiGridMain.GetAFMSChecked(row.Index, GetGridMethodColumnName(method));
        }

        private static string GetGridMethodColumnName(DischargeMethod method)
        {
            return $"METHOD_{(int)method}";
        }

        public override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected override void UiButtonInput_Click(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        protected override void _TabDischargeBase_Enter(object? sender, EventArgs e)
        {
            LoadData();
        }
    }
}
