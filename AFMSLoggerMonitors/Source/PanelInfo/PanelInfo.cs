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
            set => SetLabelText(uiDeviceName, value);
        }

        [Category("AFMS Data")]
        [DefaultValue("AFMSLoggerVideoHydorsem")]
        public string DeviceDesc
        {
            get => uiDeviceDesc.Text;
            set => SetLabelText(uiDeviceDesc, value);
        }

        [Category("AFMS Data")]
        [DefaultValue("AFMSLoggerVideoHydorsem")]
        public string ServiceName
        {
            get => uiServiceName.Text;
            set => SetLabelText(uiServiceName, value);
        }

        [Category("AFMS Data")]
        [DefaultValue("127.0.0.1:8004")]
        public string ConnectionInfo
        {
            get => uiConnectionInfo.Text;
            set => SetLabelText(uiConnectionInfo, value);
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
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));

            uiDeviceName = CreateLabel("영상유속계", 14F, FontStyle.Bold, TabCommon.TextColor);
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

            uiServiceStatus = new StatusIndicator("서비스 실행", true) { Margin = new Padding(0, 0, 18, 0) };
            uiTcpStatus = new StatusIndicator("TCP 연결", true) { Margin = Padding.Empty };
            uiTpStatus.Controls.Add(uiServiceStatus, 0, 0);
            uiTpStatus.Controls.Add(uiTcpStatus, 1, 0);

            Label deviceDesc = CreateLabel("설명", 9F, FontStyle.Regular, TabCommon.DescriptionColor);
            Label serviceLabel = CreateLabel("서비스", 9F, FontStyle.Regular, TabCommon.DescriptionColor);
            Label connectionLabel = CreateLabel("연결 정보", 9F, FontStyle.Regular, TabCommon.DescriptionColor);

            uiServiceName = CreateLabel("", 9F, FontStyle.Regular, TabCommon.TextColor);
            uiServiceName.Text = LoggerDefine.GetSerivceName(kind);

            uiDeviceDesc = CreateLabel("", 9F, FontStyle.Regular, TabCommon.TextColor);
            uiDeviceDesc.Text = LoggerDefine.GetDescription(kind);

            uiConnectionInfo = CreateLabel("127.0.0.1:8004", 9F, FontStyle.Regular, TabCommon.TextColor);

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

        private static Label CreateLabel(string text, float fontSize, FontStyle fontStyle, Color foreColor)
        {
            return new Label
            {
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, fontSize, fontStyle, GraphicsUnit.Point),
                ForeColor = foreColor,
                Margin = Padding.Empty,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static void SetLabelText(Label label, string? value)
        {
            void UpdateText() => label.Text = value ?? string.Empty;

            if (label.InvokeRequired) label.BeginInvoke(UpdateText);
            else UpdateText();
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
