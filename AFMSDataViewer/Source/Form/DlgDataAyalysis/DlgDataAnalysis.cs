using System.Data;
using System.Globalization;
using AFMSDll;
using ScottPlot.WinForms;

namespace AFMSDataViewer
{
    internal abstract class DlgDataAnalysis : AFMSForm
    {
        protected abstract void ConfigureAnalysisTabs();
        protected readonly AFMSTabControl uiTabs;
        protected readonly MeasurementDataHub? measurementDataHub;
        protected readonly Tracking? linkedTracking;
        protected readonly double? minimumVelocity;
        protected readonly double? maximumVelocity;
        protected VelocityMeasurement? velocityMeasurement;
        private bool syncingTracking;

        public ChartMainType SourceChartType;
        public RealtimeChartSeries SelectedSeries { get; }
        public RealtimeChartPoint SelectedPoint { get; private set; }
        public int? TransectNo { get; }
        private readonly TableLayoutPanel uiTpMain;
        private readonly Tracking uiTracking;

        public DlgDataAnalysis(RealtimeChartSeries series, RealtimeChartPoint point, int? tranNo = null, MeasurementDataHub? hub = null,
            VelocityMeasurement? velocityMeasurement = null, double? min= null, double? max= null, Tracking? linkedTracking = null)
        {
            SelectedSeries = series;
            SelectedPoint = point;
            TransectNo = tranNo;
            this.measurementDataHub = hub;
            this.velocityMeasurement = velocityMeasurement;
            this.minimumVelocity = min;
            this.maximumVelocity = max;
            this.linkedTracking = linkedTracking;

            Text = $"데이터 분석 - {series.Name} ({point.Time:yyyy-MM-dd HH:mm})";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1000, 700);
            MinimumSize = new Size(760, 520);
            ShowMinimizeButton = false;
            ShowInfoButton = false;
            ShowInTaskbar = false;

            uiTpMain = new TableLayoutPanel();
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.RowStyles.Clear();
            uiTpMain.ColumnStyles.Clear();
            uiTpMain.RowCount = 2;
            uiTpMain.ColumnCount = 1;
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            uiTracking = new Tracking();
            uiTracking.Dock = DockStyle.Fill;

            uiTabs = new AFMSTabControl();
            uiTabs.Dock = DockStyle.Fill;
            uiTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            uiTabs.Font = new Font("Segoe UI", 9F);
            uiTabs.ItemSize = new Size(120, 40);
            uiTabs.Padding = new Point(12, 5);
            uiTabs.TabHeight = 40;
            uiTabs.TabSizingMode = AFMSTabSizingMode.Equal;
            uiTabs.EqualTabWidth = 120;
            uiTabs.BorderRadius = 5;
            uiTabs.SizeMode = TabSizeMode.Fixed;

            uiTpMain.Controls.Add(uiTabs, 0, 0);
            uiTpMain.Controls.Add(uiTracking, 0, 1);
            Controls.Add(uiTpMain);

            ConfigureAnalysisTabs();
            InitializeTracking();
        }

        private void InitializeTracking()
        {
            uiTracking.SelectedTimeChanged += UiTracking_SelectedTimeChanged;
            if (linkedTracking == null)
            {
                uiTracking.SetRange(SelectedPoint.Time, SelectedPoint.Time, SelectedPoint.Time);
                return;
            }

            linkedTracking.SelectedTimeChanged += LinkedTracking_SelectedTimeChanged;
            SyncFromLinkedTracking(linkedTracking.SelectedTime);
        }

        private void UiTracking_SelectedTimeChanged(object? sender, TrackingTimeChangedEventArgs e)
        {
            if (syncingTracking) return;

            if (linkedTracking != null)
            {
                syncingTracking = true;
                try { linkedTracking.SetSelectedTime(e.Time); }
                finally { syncingTracking = false; }
            }

            RefreshAnalysis(e.Time);
        }

        private void LinkedTracking_SelectedTimeChanged(object? sender, TrackingTimeChangedEventArgs e)
        {
            if (syncingTracking || IsDisposed) return;
            SyncFromLinkedTracking(e.Time);
        }

        private void SyncFromLinkedTracking(DateTime selectedTime)
        {
            if (linkedTracking == null) return;

            syncingTracking = true;
            try { uiTracking.SetRange(linkedTracking.RangeStart, linkedTracking.RangeEnd, selectedTime); }
            finally { syncingTracking = false; }

            RefreshAnalysis(selectedTime);
        }

        private void RefreshAnalysis(DateTime selectedTime)
        {
            DateTime slotTime = MeasurementDataHub.AlignToSlot(selectedTime);
            RealtimeChartPoint? point = SelectedSeries.Points
                .Where(item => MeasurementDataHub.AlignToSlot(item.Time) == slotTime)
                .MinBy(item => Math.Abs((item.Time - selectedTime).Ticks));

            if (SourceChartType != ChartMainType.Velocity) return;
            if (measurementDataHub == null) return;
            if (velocityMeasurement == null) return;

                VelocityMeasurement? measurement = measurementDataHub.GetVelocitySlots(slotTime, slotTime)
                    .SelectMany(slot => slot.Measurements)
                    .FirstOrDefault(item => item.SourceType == velocityMeasurement.SourceType && item.DeviceKey == velocityMeasurement.DeviceKey);
                if (measurement == null) return;

                velocityMeasurement = measurement;
                VelocityTransectMeasurement? selectedValue = measurement.Transects.FirstOrDefault(item => item.TransectNo == TransectNo);
                point = new RealtimeChartPoint(
                    measurement.Time,
                    selectedValue is { IsValid: true } ? selectedValue.Velocity : 0D,
                    selectedValue is not { IsValid: true });

            if (point != null) SelectedPoint = point;

            Text = $"데이터 분석 - {SelectedSeries.Name} ({slotTime:yyyy-MM-dd HH:mm})";
            RebuildAnalysisTabs();
        }

        private void RebuildAnalysisTabs()
        {
            /*
            TabPage[] updatedPages = CreateAnalysisPages();
            bool sameTabs = updatedPages.Length == uiTabs.TabPages.Count &&
                updatedPages.Select((page, index) => page.Text == uiTabs.TabPages[index].Text).All(matches => matches);

            if (!sameTabs)
            {
                int selectedIndex = uiTabs.SelectedIndex;
                TabPage[] oldPages = uiTabs.TabPages.Cast<TabPage>().ToArray();
                uiTabs.TabPages.Clear();
                uiTabs.TabPages.AddRange(updatedPages);
                foreach (TabPage page in oldPages) page.Dispose();
                if (selectedIndex >= 0 && selectedIndex < uiTabs.TabPages.Count)
                    uiTabs.SelectedIndex = selectedIndex;
                return;
            }

            for (int index = 0; index < updatedPages.Length; index++)
                ReplacePageContents(uiTabs.TabPages[index], updatedPages[index]);
            */
        }

        private static void ReplacePageContents(TabPage currentPage, TabPage updatedPage)
        {
            Control[] oldControls = currentPage.Controls.Cast<Control>().ToArray();
            Control[] updatedControls = updatedPage.Controls.Cast<Control>().ToArray();

            currentPage.SuspendLayout();
            try
            {
                updatedPage.Controls.Clear();
                currentPage.Controls.Clear();
                foreach (Control control in oldControls) control.Dispose();
                currentPage.Controls.AddRange(updatedControls);
            }
            finally
            {
                currentPage.ResumeLayout(true);
                updatedPage.Dispose();
            }
        }

        protected SectionContext LoadSectionContext()
        {
            MeasurementSlot? slot = measurementDataHub?.GetSlots(SelectedPoint.Time, SelectedPoint.Time).FirstOrDefault();
            CrossSectionPointCollection points = slot?.CrossSectionDefinition.CreatePointCollection()
                ?? new CrossSectionPointCollection();
            double? gaugeWaterLevel = slot?.MeasurementDevices.WaterLevelGauge.IsValid == true
                ? slot.MeasurementDevices.WaterLevelGauge.Level
                : null;
            double? meterWaterLevel = velocityMeasurement!.Transects
                .Select(item => item.AdditionalValues != null &&
                                item.AdditionalValues.TryGetValue("수위(m)", out double? value)
                    ? value
                    : null)
                .FirstOrDefault(value => value.HasValue);
            double? waterLevel = velocityMeasurement is VideoVelocityMeasurement
                ? gaugeWaterLevel
                : meterWaterLevel ?? gaugeWaterLevel;
            string message = string.Empty;

            if (points.Count == 0)
            {
                try { points = LoadCrossSection(SelectedPoint.Time); }
                catch (InvalidOperationException ex) { message = ex.Message; }
            }
            points.WaterLevel = waterLevel;

            TransectCollection transects = new();
            try { transects = LoadTransects(velocityMeasurement!.DeviceId, SelectedPoint.Time); }
            catch (InvalidOperationException ex)
            {
                message = string.IsNullOrWhiteSpace(message) ? ex.Message : $"{message} / {ex.Message}";
            }

            foreach (Transect transect in transects)
                transect.Elevation = double.NaN;
            if (points.Count >= 2 && transects.Count > 0)
            {
                try { transects.CalculateSectionAreas(points, waterLevel ?? 0D); }
                catch (ArgumentException ex)
                {
                    foreach (Transect transect in transects)
                        transect.Elevation = double.NaN;
                    message = string.IsNullOrWhiteSpace(message) ? ex.Message : $"{message} / {ex.Message}";
                }
            }

            return new SectionContext(points, transects, waterLevel, message);
        }

        private static CrossSectionPointCollection LoadCrossSection(DateTime time)
        {
            string date = time.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string clock = time.ToString("HHmmss", CultureInfo.InvariantCulture);
            string sql = $"SELECT FIRST 1 {FbtAFMSCrossSection.COL_ZERO_POINT_ELEVATION}," +
                         $" {FbtAFMSCrossSection.COL_POINT_DATA} FROM {FbtAFMSCrossSection.TABLE_NAME}" +
                         $" WHERE ({_FBTableBase.COL_MEASURE_DATE} < '{date}' OR" +
                         $" ({_FBTableBase.COL_MEASURE_DATE} = '{date}' AND {_FBTableBase.COL_MEASURE_TIME} <= '{clock}'))" +
                         $" ORDER BY {_FBTableBase.COL_MEASURE_DATE} DESC, {_FBTableBase.COL_MEASURE_TIME} DESC, {_FBTableBase.COL_ID} DESC";
            using FBDatabase database = FBProvider.Instance.CreateDatabase();
            DataTable table = database.Execute(sql, out string error);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException("단면 설정을 조회하지 못했습니다.");
            if (table.Rows.Count == 0) return new CrossSectionPointCollection();

            DataRow row = table.Rows[0];
            double zero = row[0] == DBNull.Value ? 0D : Convert.ToDouble(row[0], CultureInfo.InvariantCulture);
            string json = Convert.ToString(row[1], CultureInfo.InvariantCulture) ?? string.Empty;
            try { return CrossSectionPointBuilder.Build(json, zero); }
            catch (System.Text.Json.JsonException) { throw new InvalidOperationException("단면 설정 형식이 올바르지 않습니다."); }
        }

        private static TransectCollection LoadTransects(int deviceId, DateTime time)
        {
            if (deviceId <= 0) return new TransectCollection();
            string date = time.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string clock = time.ToString("HHmmss", CultureInfo.InvariantCulture);
            string sql = $"SELECT FIRST 1 {FbtAFMSHydroTransect.COL_DISTANCE_DATAS}" +
                         $" FROM {FbtAFMSHydroTransect.TABLE_NAME}" +
                         $" WHERE {FbtAFMSHydroTransect.COL_HYDRO_ID} = {deviceId}" +
                         $" AND ({_FBTableBase.COL_MEASURE_DATE} < '{date}' OR" +
                         $" ({_FBTableBase.COL_MEASURE_DATE} = '{date}' AND {_FBTableBase.COL_MEASURE_TIME} <= '{clock}'))" +
                         $" ORDER BY {_FBTableBase.COL_MEASURE_DATE} DESC, {_FBTableBase.COL_MEASURE_TIME} DESC, {_FBTableBase.COL_ID} DESC";
            using FBDatabase database = FBProvider.Instance.CreateDatabase();
            DataTable table = database.Execute(sql, out string error);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException("측선 설정을 조회하지 못했습니다.");
            if (table.Rows.Count == 0 || table.Rows[0][0] == DBNull.Value) return new TransectCollection();

            string json = Convert.ToString(table.Rows[0][0], CultureInfo.InvariantCulture) ?? string.Empty;
            if (TransectBuilder.TryBuild(json, out TransectCollection transects)) return transects;
            throw new InvalidOperationException("측선 설정 형식이 올바르지 않습니다.");
        }

        protected static string BuildSectionSummary(SectionContext context)
        {
            string level = context.WaterLevel.HasValue ? $"수위 {context.WaterLevel:N3} m" : "수위 자료 없음";
            string section = context.Points.Count > 0 ? $"단면점 {context.Points.Count}개" : "단면 자료 없음";
            string transects = context.Transects.Count > 0 ? $"측선 {context.Transects.Count}개" : "측선 정보 없음";
            string result = $"{level}   |   {section}   |   {transects}";
            return string.IsNullOrWhiteSpace(context.Message) ? result : $"{result}   |   {context.Message}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                uiTracking.SelectedTimeChanged -= UiTracking_SelectedTimeChanged;
                if (linkedTracking != null) linkedTracking.SelectedTimeChanged -= LinkedTracking_SelectedTimeChanged;
            }
            base.Dispose(disposing);
        }

        protected static double GetTransectPosition(TransectCollection transects, int transectNo) =>
            transects.FirstOrDefault(item => item.No == transectNo)?.CenterLeftBankDistance ?? transectNo;

        public static TabPage CreatePage(string text) => new(text)
        {
            BackColor = Color.White,
            Padding = new Padding(8)
        };

        public static void AddGridColumn(DataGridView grid, string name, string header, string format)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Format = format,
                    NullValue = "-"
                }
            });
        }
    }
}
