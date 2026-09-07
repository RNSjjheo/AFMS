using AFMSDll;

namespace AFMSLoggerMonitors
{
    public partial class FormMain : AFMSForm
    {
        private readonly AFMSTabControl uiTabCtl;
        private readonly TabLogger uiLoggerVideo;

        public FormMain()
        {
            InitializeComponent();
            ConfigureWindow();

            uiTabCtl = CreateLoggerTabControl();

            uiLoggerVideo = new TabLogger(LoggerKind.VideoHydrosem);
            uiTabCtl.TabPages.Add(uiLoggerVideo);
            Controls.Add(uiTabCtl);
        }

        private void ConfigureWindow()
        {
            Text = "Total Logger Monitoring";
            Size = new Size(800, 600);
            TitleBarColor = DllColorHelper.HexToColor("#1E4B73");
            TitleBarHeight = 40;
            ShowIcon = true;
            ShowTitleBarIcon = true;
            BorderRadius = 5;
            ResizeBorderWidth = 0;

            using Bitmap titleBarImage = AFMSIcon.Get(AFMSIcons.LoggerMonitor, 32);
            TitleBarImage = titleBarImage;
        }

        private static AFMSTabControl CreateLoggerTabControl()
        {
            return new AFMSTabControl
            {
                BorderRadius = 5,
                Dock = DockStyle.Fill,
                EqualTabWidth = 100,
                SelectionIndicatorBottomOffset = 0,
                SelectionIndicatorHorizontalInset = 0,
                SizeMode = TabSizeMode.Fixed,
                TabHeight = 35,
                TabSizingMode = AFMSTabSizingMode.Equal
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
