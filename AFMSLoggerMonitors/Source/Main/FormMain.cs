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
            using Font titleBarFont = new Font(Font.FontFamily, 10.5F, FontStyle.Bold, GraphicsUnit.Point);

            Text = "통합 수집 서비스 모니터링";
            Size = new Size(800, 600);
            TitleBarColor = DllColorHelper.HexToColor("#1E4B73");
            TitleBarHeight = 44;
            TitleBarIconSize = 32;
            TitleBarFont = titleBarFont;
            ShowIcon = true;
            Icon = AFMSIcon.GetIcon(AFMSIcons.LoggerMonitor, 32);
            ShowTitleBarIcon = true;
            BorderRadius = 10;

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
