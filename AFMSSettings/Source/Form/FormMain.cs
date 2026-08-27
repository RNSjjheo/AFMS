using AFMSDll;
using log4net;
using Microsoft.VisualBasic.Logging;
using RnsLibrary;

namespace AFMSSettings
{
    public partial class FormMain : AFMSForm
    {
        private const string PROCESS_NAME = "AFMSSettings";
        private static readonly ILog Log = LogManager.GetLogger("SYS");

        public FormMain()
        {
            InitializeComponent();
            ShowIcon = true;
            Icon = AFMSIcon.SettingIcon(32, 32, Color.White);
            this.Width = 1260;
            this.Height = 620;

            RnsLog.Init(Environment.UserInteractive, PROCESS_NAME, 100, 0);
            RnsLog.Start();
            RnsLog.AppenderInfo();
            RnsLog.ShowVersion();

            string programPath = Environment.ProcessPath ?? throw new InvalidOperationException("현재 프로그램 경로를 확인할 수 없습니다.");

            FBProvider.Instance.ConnStrBuilder = FBProvider.SetFBConnStrBuilder();
            List<string> dbtablelog = FBProvider.Instance.CheckTables();

            Log.Info($"===========================================================");
            Log.Info($"= {PROCESS_NAME}");
            Log.Info($"= 버전: {AFMSBuild.GetVersion()} ");
            Log.Info($"= 빌드: {AFMSBuild.GetBuildDate()} ");
            Log.Info($"===========================================================");

            foreach (string log in dbtablelog)
            {
                Log.Info(log);
            }

            this.Text = "설정";


            afmsTabControl1.TabHeight = 40;
            afmsTabControl1.TabPages.Add(new TabHydroMeter());
            afmsTabControl1.TabPages.Add(new TabCrossSection());
            afmsTabControl1.TabPages.Add(new TabDischarge());
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
