using AFMSDll;

namespace AFMSLoggerMonitors
{
    public partial class FormMain : AFMSForm
    {
        private AFMSTabControl uiTabCtl;
        private TabLogger uiLoggerVideo;
        public FormMain()
        {
            InitializeComponent();
            this.Text = "Total Logger Monitoring";

            this.Width = 800;
            this.Height = 600;

            uiTabCtl = new AFMSTabControl();
            uiTabCtl.Dock = DockStyle.Fill;
            uiTabCtl.TabHeight = 40;
            uiTabCtl.TabSizingMode = AFMSTabSizingMode.Equal;
            uiTabCtl.EqualTabWidth = 120;
            uiTabCtl.BorderRadius = 5;
            uiTabCtl.SizeMode = TabSizeMode.Fixed;
            uiTabCtl.SelectionIndicatorBottomOffset = 0;
            uiTabCtl.SelectionIndicatorHorizontalInset = 0;

            uiLoggerVideo = new TabLogger(LoggerKind.VideoHydrosem);

            uiTabCtl.TabPages.Add(uiLoggerVideo);

            Controls.Add(uiTabCtl);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
