using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AFMSDataViewer
{
    public class TrackingUi : AFMSPanel
    {
        private const float TIME_LAYOUT_WIDTH = 120F;
        private const string BACKGROUND_COLOR = "#99E0D2";
        private const string BORDER_COLOR = "#99E0D2";
        private const string TRACK_COLOR = "#0DAC7E";
        private const string HANDLE_COLOR = "#0DAC7E";
        private const float BORDER_THICKNESS = 1F;
        private const int BORDER_RADIUS = 5;
        private const int TRACK_HEIGHT = 2;
        private const int HANDLE_SIZE = 12;

        private TableLayoutPanel _tpMain;
        private Panel _pnBar1;
        private Panel _pnBar2;
        private Label _lbTimeStart;
        private Label _lbTimeFinish;
        private DateTime _rangeStart;
        private DateTime _rangeEnd;
        private bool _settingRange;
        public AFMSTrackBar CtlItem;

        public event EventHandler<TrackingTimeChangedEventArgs>? SelectedTimeChanged;

        public DateTime RangeStart => _rangeStart;
        public DateTime RangeEnd => _rangeEnd;
        public DateTime SelectedTime =>
            _rangeStart + TimeSpan.FromTicks(MeasurementDataHub.SlotInterval.Ticks * CtlItem.Value);

        public TrackingUi()
        {
            Dock = DockStyle.Fill;
            Padding = Padding.Empty;
            Margin = Padding.Empty;
            BackColor = DllColorHelper.HexToColor(BACKGROUND_COLOR);
            BorderColor = DllColorHelper.HexToColor(BORDER_COLOR);
            BorderThickness = BORDER_THICKNESS;
            BorderRadius = BORDER_RADIUS;

            _tpMain = new TableLayoutPanel();
            _tpMain.Dock = DockStyle.Fill;
            _tpMain.BackColor = Color.Transparent;
            _tpMain.RowStyles.Clear();
            _tpMain.ColumnStyles.Clear();
            _tpMain.ColumnCount = 5;
            _tpMain.RowCount = 1;
            _tpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _tpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TIME_LAYOUT_WIDTH));
            _tpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1F));
            _tpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _tpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1F));
            _tpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TIME_LAYOUT_WIDTH));
            _tpMain.Padding = Padding.Empty;
            _tpMain.Margin = Padding.Empty;

            _pnBar1 = CreatePanel();
            _pnBar2 = CreatePanel();
            _lbTimeStart = CreateLabel();
            _lbTimeFinish = CreateLabel();

            CtlItem = new AFMSTrackBar
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                TrackColor = DllColorHelper.HexToColor(TRACK_COLOR),
                HandleColor = DllColorHelper.HexToColor(HANDLE_COLOR),
                TrackHeight = TRACK_HEIGHT,
                HandleSize = HANDLE_SIZE
            };
            CtlItem.ValueChanged += (_, _) =>
            {
                if (!_settingRange)
                    RaiseSelectedTimeChanged();
            };

            _tpMain.Controls.Add(_lbTimeStart, 0, 0);
            _tpMain.Controls.Add(_pnBar1, 1, 0);
            _tpMain.Controls.Add(CtlItem, 2, 0);
            _tpMain.Controls.Add(_pnBar2, 3, 0);
            _tpMain.Controls.Add(_lbTimeFinish, 4, 0);

            Controls.Add(_tpMain);
        }

        private Panel CreatePanel()
        {
            Panel item = new Panel();
            item.Dock = DockStyle.Fill;
            item.BackColor = DllColorHelper.HexToColor(BORDER_COLOR);

            return item;
        }

        private Label CreateLabel()
        {
            Label item = new Label();
            item.Dock = DockStyle.Fill;
            item.BackColor = Color.Transparent;
            item.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            item.TextAlign = ContentAlignment.MiddleCenter;

            return item;
        }

        public void SetRange(DateTime start, DateTime end, DateTime? selected = null)
        {
            if (start > end)
                throw new ArgumentException("트래킹 시작 시각은 종료 시각보다 늦을 수 없습니다.");

            _lbTimeStart.Text = start.ToString("yyyy-MM-dd HH:mm");
            _lbTimeFinish.Text = end.ToString("yyyy-MM-dd HH:mm");
            _rangeStart = start;
            _rangeEnd = end;

            int maximum = (int)Math.Min(int.MaxValue,
                Math.Max(0, (end - start).Ticks / MeasurementDataHub.SlotInterval.Ticks));
            DateTime selectedTime = selected ?? end;
            int value = (int)Math.Clamp(
                (selectedTime - start).Ticks / MeasurementDataHub.SlotInterval.Ticks,
                0L,
                maximum);

            _settingRange = true;
            try
            {
                CtlItem.Minimum = 0;
                CtlItem.Maximum = maximum;
                CtlItem.SmallChange = 1;
                CtlItem.LargeChange = Math.Max(1, maximum / 12);
                CtlItem.Value = value;
            }
            finally
            {
                _settingRange = false;
            }

            RaiseSelectedTimeChanged();

        }

        public void SetSelectedTime(DateTime selectedTime)
        {
            if (_rangeStart > _rangeEnd)
                throw new InvalidOperationException("트래킹 시간 범위가 설정되지 않았습니다.");

            int maximum = (int)Math.Min(int.MaxValue,
                Math.Max(0, (_rangeEnd - _rangeStart).Ticks / MeasurementDataHub.SlotInterval.Ticks));
            int value = (int)Math.Clamp(
                (selectedTime - _rangeStart).Ticks / MeasurementDataHub.SlotInterval.Ticks,
                0L,
                maximum);

            _settingRange = true;
            try { CtlItem.Value = value; }
            finally { _settingRange = false; }

            RaiseSelectedTimeChanged();
        }

        private void RaiseSelectedTimeChanged() =>
            SelectedTimeChanged?.Invoke(this, new TrackingTimeChangedEventArgs(SelectedTime));
    }
}
