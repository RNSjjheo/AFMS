using AFMSDll;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSDataViewer
{
    public class ViewRealtime : TableLayoutPanel
    {
        private sealed record QueryPeriodOption(RealtimeQueryPeriod Period, string Text)
        {
            public TimeSpan Duration => TimeSpan.FromHours((int)Period);
            public override string ToString() => Text;
        }

        private const int LAYOUT_ICON_SIZE = 16;
        public AFMSPanel uiPnHeader;
        public MaximizableTableLayoutPanel uiTpField;
        public AFMSComboBox uiRangeCombo;
        public AFMSButtonGroup uiBtnTemp;

        private TableLayoutPanel uiTpTop;
        private ChartSelectPanel uiChart1;
        private ChartSelectPanel uiChart2;
        private ChartSelectPanel uiChart3;
        private ChartSelectPanel uiChart4;
        private DateTime selectedDateTime;
        private long lastTrackingSlotTicks;
        public Tracking uiTracking;
        private readonly MeasurementDataHub measurementDataHub;
        private readonly MeasurementRefreshService measurementRefreshService;

        public ViewRealtime(
            MeasurementDataHub measurementDataHub,
            MeasurementRefreshService measurementRefreshService)
        {
            ArgumentNullException.ThrowIfNull(measurementDataHub);
            ArgumentNullException.ThrowIfNull(measurementRefreshService);
            this.measurementDataHub = measurementDataHub;
            this.measurementRefreshService = measurementRefreshService;
            Dock = DockStyle.Fill;
            RowStyles.Clear();
            ColumnStyles.Clear();
            RowCount = 3;
            ColumnCount = 1;
            Margin = Padding.Empty;
            Padding = Padding.Empty;

            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

            uiPnHeader = new AFMSPanel();
            uiPnHeader.Dock = DockStyle.Fill;
            uiPnHeader.BackColor = DllColorHelper.HexToColor("#F0F4F9");
            uiPnHeader.Padding = new Padding(3, 2, 3, 2);
            uiPnHeader.Margin = Padding.Empty;
            uiPnHeader.BorderRadius = 5;
            uiPnHeader.BorderColor = DllColorHelper.GetCommonBorder();
            uiPnHeader.BorderThickness = 2;

            uiTpField = new MaximizableTableLayoutPanel();
            uiTpField.Dock = DockStyle.Fill;
            uiTpField.BackColor = Color.White;
            uiTpField.Margin = new Padding(0, 10, 0, 0);
            uiTpField.Padding = Padding.Empty;

            uiTracking = new Tracking();
            uiTracking.BackColor = Color.White;
            uiTracking.Margin = new Padding(0, 10, 0, 0);
            uiTracking.Padding = new Padding(3);
            uiTracking.BorderRadius = 5;
            uiTracking.BorderColor = DllColorHelper.GetCommonBorder();
            uiTracking.BorderThickness = 1;

            uiTpTop = new TableLayoutPanel();
            uiTpTop.Dock = DockStyle.Fill;
            uiTpTop.RowStyles.Clear();
            uiTpTop.ColumnStyles.Clear();
            uiTpTop.RowCount = 1;
            uiTpTop.ColumnCount = 3;
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            uiTpTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpTop.Margin = Padding.Empty;
            uiTpTop.Padding = Padding.Empty;

            uiRangeCombo = new AFMSComboBox
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(2),
                BorderRadius = 5,
                PlaceholderText = "조회 시간"
            };
            QueryPeriodOption defaultPeriod = new(RealtimeQueryPeriod.Hours6, "최근 6시간");
            uiRangeCombo.Items.Add(defaultPeriod);
            uiRangeCombo.Items.Add(new QueryPeriodOption(RealtimeQueryPeriod.Hours12, "최근 12시간"));
            uiRangeCombo.Items.Add(new QueryPeriodOption(RealtimeQueryPeriod.Hours24, "최근 24시간"));
            uiRangeCombo.SelectedItem = defaultPeriod;
            uiRangeCombo.SelectedIndexChanged += UiRangeCombo_SelectedIndexChanged;

            uiBtnTemp = new AFMSButtonGroup();
            uiBtnTemp.Dock = DockStyle.Fill;
            uiBtnTemp.SelectedBackColor = DllColorHelper.HexToColor("#ECFDF5");
            uiBtnTemp.Click += UiBtnTemp_Click;


            AFMSButtonGroupItem x1 = uiBtnTemp.AddButton(
                AFMSIcon.Get(AFMSIcons.Layout22Off, LAYOUT_ICON_SIZE),
                AFMSIcon.Get(AFMSIcons.Layout22On, LAYOUT_ICON_SIZE),
                MaximizableTableLayoutType.Layout2_2);
            AFMSButtonGroupItem x2 = uiBtnTemp.AddButton(
                AFMSIcon.Get(AFMSIcons.Layout21Off, LAYOUT_ICON_SIZE),
                AFMSIcon.Get(AFMSIcons.Layout21On, LAYOUT_ICON_SIZE),
                MaximizableTableLayoutType.Layout2_1);
            AFMSButtonGroupItem x3 = uiBtnTemp.AddButton(
                AFMSIcon.Get(AFMSIcons.Layout12Off, LAYOUT_ICON_SIZE),
                AFMSIcon.Get(AFMSIcons.Layout12On, LAYOUT_ICON_SIZE),
                MaximizableTableLayoutType.Layout1_2);
            AFMSButtonGroupItem x4 = uiBtnTemp.AddButton(
                AFMSIcon.Get(AFMSIcons.Layout11Off, LAYOUT_ICON_SIZE),
                AFMSIcon.Get(AFMSIcons.Layout11On, LAYOUT_ICON_SIZE),
                MaximizableTableLayoutType.Layout1_1);

            DateTime now = DateTime.Now;
            IReadOnlyList<MeasurementSlot> initialSlots = measurementDataHub.GetSlots();
            selectedDateTime = initialSlots.Count > 0
                ? initialSlots[^1].SlotTime
                : MeasurementDataHub.AlignToSlot(now);
            lastTrackingSlotTicks = selectedDateTime.Ticks;
            measurementDataHub.Changed += MeasurementDataHub_Changed;

            uiChart1 = CreateChartPanel();
            uiChart2 = CreateChartPanel();
            uiChart3 = CreateChartPanel();
            uiChart4 = CreateChartPanel();
            uiTracking.SelectedTimeChanged += UiTracking_SelectedTimeChanged;
            ApplyNavigatorRange();

            uiTpTop.Controls.Add(uiRangeCombo, 0, 0);
            uiTpTop.Controls.Add(uiBtnTemp, 2, 0);

            uiPnHeader.Controls.Add(uiTpTop);


            Controls.Add(uiPnHeader, 0, 0);
            Controls.Add(uiTpField, 0, 1);
            Controls.Add(uiTracking, 0, 2);

            uiBtnTemp.PerformClick(x2);
        }

        private void ApplyNavigatorRange()
        {
            DateTime start = selectedDateTime.Subtract(GetSelectedDuration());

            DateTime trackingTime = start.AddTicks(GetSelectedDuration().Ticks * 3 / 4);
            uiTracking?.SetRange(start, selectedDateTime, trackingTime);

            uiChart1?.SetTimeRange(start, selectedDateTime);
            uiChart2?.SetTimeRange(start, selectedDateTime);
            uiChart3?.SetTimeRange(start, selectedDateTime);
            uiChart4?.SetTimeRange(start, selectedDateTime);
        }

        private async void UiRangeCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            uiRangeCombo.Enabled = false;
            try
            {
                await measurementRefreshService.EnsureRetentionAsync(GetSelectedDuration());
                IReadOnlyList<MeasurementSlot> slots = measurementDataHub.GetSlots();
                if (slots.Count > 0) selectedDateTime = slots[^1].SlotTime;
                ApplyNavigatorRange();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    FindForm(),
                    $"조회 기간 데이터를 불러오지 못했습니다.\n{exception.Message}",
                    "데이터 조회 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed) uiRangeCombo.Enabled = true;
            }
        }

        private TimeSpan GetSelectedDuration() =>
            (uiRangeCombo.SelectedItem as QueryPeriodOption)?.Duration ??
            TimeSpan.FromHours((int)RealtimeQueryPeriod.Hours6);

        private void UiTracking_SelectedTimeChanged(object? sender, TrackingTimeChangedEventArgs e)
        {
            uiChart1.SetTrackingTime(e.Time);
            uiChart2.SetTrackingTime(e.Time);
            uiChart3.SetTrackingTime(e.Time);
            uiChart4.SetTrackingTime(e.Time);
        }

        private void MeasurementDataHub_Changed(object? sender, MeasurementDataChangedEventArgs e)
        {
            if (e.RangeEnd == DateTime.MinValue) return;

            long previousTicks;
            do
            {
                previousTicks = Interlocked.Read(ref lastTrackingSlotTicks);
                if (e.RangeEnd.Ticks <= previousTicks) return;
            }
            while (Interlocked.CompareExchange(
                ref lastTrackingSlotTicks, e.RangeEnd.Ticks, previousTicks) != previousTicks);

            if (IsDisposed) return;
            if (!IsHandleCreated)
            {
                selectedDateTime = e.RangeEnd;
                return;
            }

            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed) return;
                    selectedDateTime = e.RangeEnd;
                    ApplyNavigatorRange();
                }));
            }
            catch (InvalidOperationException)
            {
                // 컨트롤이 종료되는 동안 도착한 백그라운드 갱신은 무시합니다.
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            IReadOnlyList<MeasurementSlot> slots = measurementDataHub.GetSlots();
            if (slots.Count > 0)
            {
                selectedDateTime = slots[^1].SlotTime;
                Interlocked.Exchange(ref lastTrackingSlotTicks, selectedDateTime.Ticks);
            }

            ApplyNavigatorRange();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                uiRangeCombo.SelectedIndexChanged -= UiRangeCombo_SelectedIndexChanged;
                uiTracking.SelectedTimeChanged -= UiTracking_SelectedTimeChanged;
                measurementDataHub.Changed -= MeasurementDataHub_Changed;
            }

            base.Dispose(disposing);
        }

        private void UiBtnTemp_Click(object? sender, EventArgs e)
        {
            if (sender is not AFMSButtonGroup buttonGroup || buttonGroup.SelectedItem?.Tag is not MaximizableTableLayoutType layoutType) return;

            uiChart1.ResetToChartSelection();
            uiChart2.ResetToChartSelection();
            uiChart3.ResetToChartSelection();
            uiChart4.ResetToChartSelection();
            uiTpField.SetLayout(layoutType, uiChart1, uiChart2, uiChart3, uiChart4);
        }

        private ChartSelectPanel CreateChartPanel()
        {
            ChartSelectPanel panel = new ChartSelectPanel(measurementDataHub);
            panel.Dock = DockStyle.Fill;
            panel.BorderRadius = 5;
            panel.Padding = Padding.Empty;
            panel.Margin = Padding.Empty;
            panel.BackColor = DllColorHelper.HexToColor("#F0F4F9");
            panel.uiTpBtnArr.BackColor = panel.BackColor;
            panel.BorderColor = DllColorHelper.GetCommonBorder();
            panel.MaximizeRequested += (_, _) => uiTpField.ToggleMaximize(panel);

            return panel;
        }
    }
}
