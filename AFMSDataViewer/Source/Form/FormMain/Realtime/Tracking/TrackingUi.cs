using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AFMSDataViewer
{
    public class TrackingUi : AFMSPanel
    {
        private float TIME_LAYOUT_WIDTH = 120F;
        private TableLayoutPanel _tpMain;
        private Panel _pnBar1;
        private Panel _pnBar2;
        private Label _lbTimeStart;
        private Label _lbTimeFinish;
        private DateTime _rangeStart;
        private bool _settingRange;
        public AFMSTrackBar CtlItem;

        public event EventHandler<TrackingTimeChangedEventArgs>? SelectedTimeChanged;

        public DateTime SelectedTime =>
            _rangeStart + TimeSpan.FromTicks(MeasurementDataHub.SlotInterval.Ticks * CtlItem.Value);

        public TrackingUi()
        {
            Dock = DockStyle.Fill;
            Padding = Padding.Empty;
            Margin = Padding.Empty;
            BackColor = DllColorHelper.HexToColor("#EAFDF6");

            _tpMain = new TableLayoutPanel();
            _tpMain.Dock = DockStyle.Fill;
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

            CtlItem = new AFMSTrackBar();
            CtlItem.Dock = DockStyle.Fill;
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
            item.BackColor = DllColorHelper.GetCommonBorder();

            return item;
        }

        private Label CreateLabel()
        {
            Label item = new Label();
            item.Dock = DockStyle.Fill;
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

        private void RaiseSelectedTimeChanged() =>
            SelectedTimeChanged?.Invoke(this, new TrackingTimeChangedEventArgs(SelectedTime));
    }
}
