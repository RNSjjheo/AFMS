using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

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
        public TrackBar CtlItem;
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

            CtlItem = new TrackBar();
            CtlItem.Dock = DockStyle.Fill;

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
    }
}
