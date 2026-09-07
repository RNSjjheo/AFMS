using AFMSDll;

namespace AFMSLoggerMonitors
{
    public partial class FormMain : AFMSForm
    {
        private AFMSTabBar uiTabControl;
        private TableLayoutPanel uiTpMain;
        public FormMain()
        {
            InitializeComponent();
            this.Text = "Total Logger Monitoring";
            ClientSize = new Size(800, 450);

            uiTpMain = new TableLayoutPanel();
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.RowStyles.Clear();
            uiTpMain.ColumnStyles.Clear();
            uiTpMain.RowCount = 2;
            uiTpMain.ColumnCount = 1;
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent,100F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
