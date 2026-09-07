using AFMSDll;
using System.Runtime;

namespace AFMSLoggerMonitors
{
    internal sealed class TabLogger : TabPage
    {
        private readonly TableLayoutPanel uiTpMain = new TableLayoutPanel();
        private readonly PanelInfo uiInfo;
        private readonly PanelDiag uiDiag;
        private const int PADDING = 10;
        public string ServiceName;
        public TabLogger(LoggerKind kind)
        {
            BackColor = Color.White;
            Text = LoggerDefine.GetDeviceName(kind);

            uiTpMain.BackColor = DllColorHelper.HexToColor("#E9EEF3");
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.ColumnCount = 2;
            uiTpMain.RowCount = 2;
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
            uiInfo = new PanelInfo(kind);
            uiInfo.DeviceName = Text;
            uiInfo.ServiceName = LoggerDefine.GetSerivceName(kind);
            uiInfo.Dock = DockStyle.Fill;
            uiInfo.Margin = new Padding(PADDING);
            uiInfo.BorderThickness = 0;

            uiDiag =  new PanelDiag();
            uiDiag.Dock = DockStyle.Fill;
            uiDiag.Margin = new Padding(0, PADDING, PADDING, PADDING);
            uiDiag.BorderThickness = 0;

            uiTpMain.Controls.Add(uiInfo, 0, 0);
            uiTpMain.Controls.Add(uiDiag, 1, 0);

            Controls.Add(uiTpMain);
        }
    }
}
