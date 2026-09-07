using AFMSDll;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace AFMSLoggerMonitors
{
    internal sealed class PanelInfo : AFMSPanel
    {
        private readonly Label uiDeviceName;
        private readonly Label uiDeviceDesc;
        private readonly Label uiServiceName;

        private readonly Label uiConnectionInfo;
        private readonly StatusIndicator uiServiceStatus;
        private readonly StatusIndicator uiTcpStatus;
        private readonly TableLayoutPanel uiTpMain;
        private readonly TableLayoutPanel uiTpStatus;

        [Category("AFMS Data")]
        [DefaultValue("영상유속계")]
        public string DeviceName
        {
            get => uiDeviceName.Text;
            set => TabCommon.SetLabelText(uiDeviceName, value);
        }

        [Category("AFMS Data")]
        [DefaultValue("AFMSLoggerVideoHydorsem")]
        public string DeviceDesc
        {
            get => uiDeviceDesc.Text;
            set => TabCommon.SetLabelText(uiDeviceDesc, value);
        }

        [Category("AFMS Data")]
        [DefaultValue("AFMSLoggerVideoHydorsem")]
        public string ServiceName
        {
            get => uiServiceName.Text;
            set => TabCommon.SetLabelText(uiServiceName, value);
        }

        [Category("AFMS Data")]
        [DefaultValue("127.0.0.1:8004")]
        public string ConnectionInfo
        {
            get => uiConnectionInfo.Text;
            set => TabCommon.SetLabelText(uiConnectionInfo, value);
        }


        public PanelInfo(LoggerKind kind)
        {
            DoubleBuffered = true;
            BackColor = Color.White;

            uiTpMain = new TableLayoutPanel();
            uiTpMain.BackColor = Color.Transparent;
            uiTpMain.ColumnCount = 3;
            uiTpMain.RowCount = 5;
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.Margin = Padding.Empty;
            uiTpMain.Padding = Padding.Empty;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, TabCommon.TITILE_HIGTH));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, TabCommon.TITILE_MARGIN));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));

            uiDeviceName = TabCommon.CreateTitleLabel("영상유속계");
            uiDeviceName.Text = LoggerDefine.GetDeviceName(kind);
            uiDeviceName.Margin = Padding.Empty;

            uiTpStatus = new TableLayoutPanel();
            uiTpStatus.BackColor = Color.Transparent;
            uiTpStatus.Dock = DockStyle.Fill;
            uiTpStatus.Margin = Padding.Empty;
            uiTpStatus.Padding = Padding.Empty;
            uiTpStatus.ColumnCount = 2;
            uiTpStatus.RowCount = 1;
            uiTpStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiTpStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiTpStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiServiceStatus = new StatusIndicator("서비스 실행", true) { Margin = Padding.Empty };
            uiTcpStatus = new StatusIndicator("TCP 연결", true) { Margin = Padding.Empty };
            uiTpStatus.Controls.Add(uiServiceStatus, 0, 0);
            uiTpStatus.Controls.Add(uiTcpStatus, 1, 0);

            Label deviceDesc = TabCommon.CreateLabel("설명", 9F, FontStyle.Regular, TabCommon.DescriptionColor);
            Label serviceLabel = TabCommon.CreateLabel("서비스", 9F, FontStyle.Regular, TabCommon.DescriptionColor);
            Label connectionLabel = TabCommon.CreateLabel("연결 정보", 9F, FontStyle.Regular, TabCommon.DescriptionColor);

            uiServiceName = TabCommon.CreateLabel("", 9F, FontStyle.Bold, TabCommon.TextColor);
            uiServiceName.Text = LoggerDefine.GetSerivceName(kind);

            uiDeviceDesc = TabCommon.CreateLabel("", 9F, FontStyle.Bold, TabCommon.TextColor);
            uiDeviceDesc.Text = LoggerDefine.GetDescription(kind);

            uiConnectionInfo = TabCommon.CreateLabel("127.0.0.1:8004", 9F, FontStyle.Bold, TabCommon.TextColor);

            uiTpMain.Controls.Add(uiDeviceName, 0, 0);
            uiTpMain.SetColumnSpan(uiDeviceName, 2);
            uiTpMain.Controls.Add(uiTpStatus, 2, 0);

            uiTpMain.Controls.Add(deviceDesc, 0, 2);
            uiTpMain.Controls.Add(uiDeviceDesc, 1, 2);
            uiTpMain.SetColumnSpan(uiDeviceDesc, 2);

            uiTpMain.Controls.Add(serviceLabel, 0, 3);
            uiTpMain.Controls.Add(uiServiceName, 1, 3);
            uiTpMain.SetColumnSpan(uiServiceName, 2);

            uiTpMain.Controls.Add(connectionLabel, 0, 4);
            uiTpMain.Controls.Add(uiConnectionInfo, 1, 4);
            uiTpMain.SetColumnSpan(uiConnectionInfo, 2);

            Controls.Add(uiTpMain);
        }


        public void SetServiceState(bool isRunning)
        {
            SetStatus(uiServiceStatus, isRunning, isRunning ? "서비스 실행" : "서비스 중지");
        }

        public void SetTcpConnection(bool connected)
        {
            SetStatus(uiTcpStatus, connected, connected ? "TCP 연결" : "TCP 끊김");
        }

        private static void SetStatus(StatusIndicator indicator, bool active, string text)
        {
            void UpdateStatus()
            {
                indicator.Active = active;
                indicator.Text = text;
            }

            if (indicator.InvokeRequired) indicator.BeginInvoke(UpdateStatus);
            else UpdateStatus();
        }
    }
}
