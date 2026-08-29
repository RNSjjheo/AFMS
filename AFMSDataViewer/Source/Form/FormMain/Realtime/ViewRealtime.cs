using AFMSDll;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSDataViewer
{
    public class ViewRealtime : TableLayoutPanel
    {
        private const int LAYOUT_ICON_SIZE = 16;
        public AFMSPanel uiPnHeader;
        public MaximizableTableLayoutPanel uiTpField;
        public AFMSButtonGroup uiBtnGroups;
        public AFMSButtonGroup uiBtnTemp;
        public AFMSNavigatorBox uiNavigator;

        private TableLayoutPanel uiTpTop;
        private ChartSelectPanel uiChart1;
        private ChartSelectPanel uiChart2;
        private ChartSelectPanel uiChart3;
        private ChartSelectPanel uiChart4;
        private DateTime selectedDateTime;
        private long lastTrackingSlotTicks;
        public Tracking uiTracking;
        private readonly MeasurementDataHub measurementDataHub;

        public ViewRealtime(MeasurementDataHub measurementDataHub)
        {
            ArgumentNullException.ThrowIfNull(measurementDataHub);
            this.measurementDataHub = measurementDataHub;
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
            uiTpTop.ColumnCount = 4;
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            uiTpTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpTop.Margin = Padding.Empty;
            uiTpTop.Padding = Padding.Empty;

            uiBtnGroups = new AFMSButtonGroup();
            uiBtnGroups.Dock = DockStyle.Fill;
            uiBtnGroups.AddButton("12H");
            uiBtnGroups.AddButton("24H");
            uiBtnGroups.AddButton("1주일");
            uiBtnGroups.SelectedIndexChanged += (_, _) => ApplyNavigatorRange();

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

            uiNavigator = new AFMSNavigatorBox();
            uiNavigator.Dock = DockStyle.Fill;
            uiNavigator.ReadOnly = true;
            uiNavigator.LeftButtonClick += (_, _) => MoveSelectedDate(-1);
            uiNavigator.RightButtonClick += (_, _) => MoveSelectedDate(1);
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

            uiTpTop.Controls.Add(uiBtnGroups, 0, 0);
            uiTpTop.Controls.Add(uiNavigator, 1, 0);
            uiTpTop.Controls.Add(uiBtnTemp, 3, 0);

            uiPnHeader.Controls.Add(uiTpTop);


            Controls.Add(uiPnHeader, 0, 0);
            Controls.Add(uiTpField, 0, 1);
            Controls.Add(uiTracking, 0, 2);

            uiBtnTemp.PerformClick(x2);
        }

        private void MoveSelectedDate(int direction)
        {
            selectedDateTime = selectedDateTime.AddTicks(GetSelectedDuration().Ticks * direction);
            ApplyNavigatorRange();
        }

        private void ApplyNavigatorRange()
        {
            if (uiNavigator == null) return;
            uiNavigator.Text = selectedDateTime.ToString("yyyy-MM-dd HH:mm");

            DateTime start = selectedDateTime.Subtract(GetSelectedDuration());

            DateTime trackingTime = start.AddTicks(GetSelectedDuration().Ticks * 3 / 4);
            uiTracking?.SetRange(start, selectedDateTime, trackingTime);

            uiChart1?.SetTimeRange(start, selectedDateTime);
            uiChart2?.SetTimeRange(start, selectedDateTime);
            uiChart3?.SetTimeRange(start, selectedDateTime);
            uiChart4?.SetTimeRange(start, selectedDateTime);
        }

        private TimeSpan GetSelectedDuration() => uiBtnGroups.SelectedIndex switch
        {
            1 => TimeSpan.FromHours(24),
            2 => TimeSpan.FromDays(7),
            _ => TimeSpan.FromHours(12)
        };

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
