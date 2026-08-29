using AFMSDll;
using log4net;
using RnsLibrary;
using System.Windows;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Forms;
using AFMSDataViewer.Properties;
using System.Diagnostics;

namespace AFMSDataViewer
{
    public partial class FormMain : AFMSForm
    {
        private const string PROCESS_NAME = "AFMSDataViewer";
        private const string TAB_HEADEL_REALTIME = "실시간 측정";
        private const string TAB_HEADEL_HISTORY= "데이터 조회";
        private static readonly ILog Log = LogManager.GetLogger("SYS");

        public const float MAIN_LAYOUT_COL1 = 260F;
        public const float MAIN_LAYOUT_COL3 = 300F;

        private AFMSTabBarItem _TabRealtime;
        private AFMSTabBarItem _TabHistory;
        public ViewRealtime _ViewRealtime;

        public FormMain(MeasurementDataHub measurementDataHub)
        {
            ArgumentNullException.ThrowIfNull(measurementDataHub);
            RnsLog.Init(Environment.UserInteractive, PROCESS_NAME, 100, 0);
            RnsLog.Start();
            RnsLog.AppenderInfo();
            RnsLog.ShowVersion();
            FBProvider.Instance.Initialize(FBProvider.SetFBConnStrBuilder());
            List<string> dbtablelog = FBProvider.Instance.CheckTables();

            InitializeComponent();

            Log.Info($"===========================================================");
            Log.Info($"= {PROCESS_NAME}");
            Log.Info($"= 버전: {AFMSBuild.GetVersion()} ");
            Log.Info($"= 빌드: {AFMSBuild.GetBuildDate()} ");
            Log.Info($"===========================================================");

            this.Width = 1260;
            this.Height = 768;
            this.BackColor = Color.White;

            uiTpMain.Padding = Padding.Empty;
            uiTpMain.ColumnStyles[0].Width = MAIN_LAYOUT_COL1;

            afmsTabBar1.RightButtonImage = AFMSIcon.Get(AFMSIcons.Setting, 24);
            afmsTabBar1.RightButtonClick += AfmsTabBar1_RightButtonClick;

            _ViewRealtime = new ViewRealtime(measurementDataHub);
            _ViewRealtime.uiPnHeader.BorderRadius = 5;
            _ViewRealtime.uiPnHeader.BorderColor = DllColorHelper.HexToColor("#E3E9F1");
            _ViewRealtime.uiPnHeader.BorderThickness = 1;

            uiSysInfo.Margin = new Padding(10);
            uiSysInfo.uiPnMain.BorderRadius = 5;
            uiSysInfo.uiPnMain.BorderColor = _ViewRealtime.uiPnHeader.BorderColor;
            uiSysInfo.uiPnMain.BorderThickness = 2;

            afmsTabBar1.Margin = Padding.Empty;
            afmsTabBar1.Padding = Padding.Empty;
            afmsTabBar1.SelectedIndexChanged += AfmsTabBar1_SelectedIndexChanged;
            afmsTabBar1.Font = new System.Drawing.Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, System.Drawing.FontStyle.Bold);
            afmsTabBar1.LetterSpacing = -1;

            const int imagesize = 20;

            _TabRealtime = afmsTabBar1.AddTab(TAB_HEADEL_REALTIME, AFMSIcon.Get(AFMSIcons.MeasureOff, imagesize), AFMSIcon.Get(AFMSIcons.MeasureOn, imagesize));
            _TabRealtime.Width = 115;
            _TabRealtime.Tag = _ViewRealtime;

            _TabHistory = afmsTabBar1.AddTab(TAB_HEADEL_HISTORY, AFMSIcon.Get(AFMSIcons.SearchOff, imagesize), AFMSIcon.Get(AFMSIcons.SearchOn, imagesize));
            _TabHistory.Width = 115;
            //_TabHistory.Tag = _ViewRealtime;

            uiPnMain.Controls.Add(_TabRealtime.Tag as System.Windows.Forms.Control);
            uiPnMain.Margin = new Padding(0, 10, 10, 10);

            uiSysInfo.ReadDatabase();

            Shown += FormMain_Shown;
        }

        private void FormMain_Shown(object? sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                RefreshMainLayout();
            }));
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ApplicationDiag.IsRunning = false;
            base.OnFormClosed(e);
        }

        private void RefreshMainLayout()
        {
            SuspendLayout();

            try
            {
                PerformLayout();

                _ViewRealtime.PerformLayout();
                _ViewRealtime.uiTpField.PerformLayout();

                _ViewRealtime.uiTpField.Invalidate(true);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void AfmsTabBar1_SelectedIndexChanged(object? sender, EventArgs e)
        {
            AFMSTabBarItem tab = afmsTabBar1.SelectedItem;

            if (tab == null) return;
            if (uiPnMain.Controls.Count != 0) uiPnMain.Controls.Clear();
            if (tab.Tag is not System.Windows.Forms.Control content) return;

            content.Dock = DockStyle.Fill;
            uiPnMain.Controls.Add(content);
        }

        private void AfmsTabBar1_RightButtonClick(object? sender, EventArgs e)
        {
            string settingsPath = Path.Combine(AppContext.BaseDirectory, "AFMSSettings.exe");
            if (!File.Exists(settingsPath))
            {
                Log.Error($"설정 프로그램을 찾을 수 없습니다: {settingsPath}");
                System.Windows.Forms.MessageBox.Show(
                    this,
                    $"설정 프로그램을 찾을 수 없습니다.\n{settingsPath}",
                    "설정 실행 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = settingsPath,
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                Log.Error("설정 프로그램 실행에 실패했습니다.", exception);
                System.Windows.Forms.MessageBox.Show(
                    this,
                    $"설정 프로그램을 실행하지 못했습니다.\n{exception.Message}",
                    "설정 실행 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
