using AFMSDll;
namespace AFMSLoggerMonitors
{
    internal sealed class TabLogger : TabPage
    {
        private readonly TableLayoutPanel uiTpMain = new TableLayoutPanel();
        private readonly PanelInfo uiInfo;
        private readonly PanelDiag uiDiag;
        private readonly ServiceStatusWorker serviceStatusWorker;
        private readonly LoggerTcpClient loggerTcpClient;
        private const int PADDING = 12;
        public string ServiceName { get; }

        public TabLogger(LoggerKind kind) : this(kind, "127.0.0.1", GetDefaultMonitoringPort(kind))
        {
        }

        public TabLogger(LoggerKind kind, string monitoringHost, int monitoringPort)
        {
            BackColor = Color.White;
            Text = LoggerDefine.GetDeviceName(kind);
            ServiceName = LoggerDefine.GetSerivceName(kind);

            uiTpMain.BackColor = DllColorHelper.HexToColor("#E9EEF3");
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.ColumnCount = 2;
            uiTpMain.RowCount = 2;
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
            uiInfo = new PanelInfo(kind);
            uiInfo.DeviceName = Text;
            uiInfo.ServiceName = ServiceName;
            uiInfo.ConnectionInfo = $"{monitoringHost}:{monitoringPort}";
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

            serviceStatusWorker = new ServiceStatusWorker(new[] { ServiceName }, TimeSpan.FromSeconds(10));
            serviceStatusWorker.StatusChecked += ServiceStatusWorker_StatusChecked;

            loggerTcpClient = new LoggerTcpClient(monitoringHost, monitoringPort, TimeSpan.FromSeconds(10));
            loggerTcpClient.ConnectionChanged += LoggerTcpClient_ConnectionChanged;
            uiInfo.SetTcpConnection(false);
        }

        public void SetServiceState(ServiceRunState state)
        {
            uiInfo.SetServiceState(state);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            serviceStatusWorker.Start();
            loggerTcpClient.Start();
        }

        private void ServiceStatusWorker_StatusChecked(object? sender, ServiceStatusCheckedEventArgs e)
        {
            if (string.Equals(ServiceName, e.ServiceName, StringComparison.OrdinalIgnoreCase)) SetServiceState(e.State);
        }

        private void LoggerTcpClient_ConnectionChanged(object? sender, TcpConnectionChangedEventArgs e)
        {
            uiInfo.SetTcpConnection(e.Connected);
        }

        private static int GetDefaultMonitoringPort(LoggerKind kind)
        {
            return kind switch
            {
                LoggerKind.VideoHydrosem => 8004,
                _ => throw new NotSupportedException($"{kind} 장비의 기본 모니터링 포트가 지정되지 않았습니다.")
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                serviceStatusWorker.StatusChecked -= ServiceStatusWorker_StatusChecked;
                serviceStatusWorker.Dispose();
                loggerTcpClient.ConnectionChanged -= LoggerTcpClient_ConnectionChanged;
                loggerTcpClient.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
