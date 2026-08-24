using AFMSDll;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Text.Json;

namespace AFMSExtraMonitor
{
    public partial class FormMain : Form
    {
        private const string COLUMN_DATETIME = "Datetime";
        private const string COLUMN_OWNER = "Owner";
        private const string COLUMN_MESSAGE = "Message";

        private readonly TcpReconnectClient _tcpClient;
        private static readonly JsonSerializerOptions JsonOptions =  new JsonSerializerOptions{IncludeFields = true,PropertyNameCaseInsensitive = true};
        private LoggerProperty PropertyItem;
        private DataTable _dtLive;
        private DataTable _dtFull;
        public FormMain()
        {
            InitializeComponent();
            this.Text = "유속 수집 모듈(확장)";

            PropertyItem = new LoggerProperty();
            propertyGrid1.SelectedObject = PropertyItem;
            propertyGrid1.PropertySort = PropertySort.Categorized;

            _tcpClient =  new TcpReconnectClient(serverAddress: "127.0.0.1", serverPort: 8003, clientId: "AFMSExtraMonitor");
            _tcpClient.ConnectionChanged += TcpClient_ConnectionChanged;
            _tcpClient.JsonReceived += TcpClient_JsonReceived;

            _dtLive = SetupDatatable(uiGridLive);
            _dtFull = SetupDatatable(uiGridFull);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _tcpClient.Start();
        }

        private void TcpClient_ConnectionChanged(bool connected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>TcpClient_ConnectionChanged(connected)));
                return;
            }
        }

        private void TcpClient_JsonReceived(string json)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>TcpClient_JsonReceived(json)));
                return;
            }

            _PacketBase basePacket = JsonSerializer.Deserialize<_PacketBase>(json)!;

            switch (basePacket.JsonType)
            {
                case JsonPacketType.Diagnotics:
                    Diagnotics? diag = JsonSerializer.Deserialize<Diagnotics>(json, JsonOptions);

                    PropertyItem.SetDiag(diag);
                    propertyGrid1.Refresh();
                    break;

                case JsonPacketType.ViewerLogMsg:
                    ViewLogMsg logmsg = JsonSerializer.Deserialize<ViewLogMsg>(json, JsonOptions);

                    InsertData(uiGridLive, true, logmsg);
                    InsertData(uiGridFull, false, logmsg);
                    break;

            }
        }

        private void InsertData(DataGridView grid, bool islive, ViewLogMsg logmsg)
        {
            DataTable dt = grid.DataSource as DataTable;
            DataRow fullrow = dt.Rows.Add();
            fullrow[COLUMN_DATETIME] = logmsg.SendingTime.ToString("HH:mm:ss");
            fullrow[COLUMN_OWNER] = logmsg.LogHost;
            fullrow[COLUMN_MESSAGE] = logmsg.LogMsg;

            if (islive)
            {
                FixGridScroll();
            }
            else
            {
                if (dt.Rows.Count > 500)
                {
                    dt.Rows.RemoveAt(0);
                }
            }

            grid.Refresh();
        }

        private bool FixGridScroll()
        {
            var vScroll = uiGridLive.Controls.OfType<VScrollBar>().FirstOrDefault();
            if (vScroll != null && vScroll.Visible)
            {
                _dtLive.Rows.RemoveAt(0);
                FixGridScroll();
                return true;
            }
            else
            {
                return false;
            }
        }

        public DataTable SetupDatatable(DataGridView grid)
        {
            DataTable dt = new DataTable();

            grid.Columns.Clear();
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoGenerateColumns = true;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.ReadOnly = true;
            grid.RowTemplate.Height = 25;
            dt.Columns.Clear();
            dt.Columns.Add(COLUMN_DATETIME, typeof(string));
            dt.Columns.Add(COLUMN_OWNER, typeof(string));
            dt.Columns.Add(COLUMN_MESSAGE, typeof(string));

            grid.DataSource = dt;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns[COLUMN_DATETIME]!.Width = 70;
            grid.Columns[COLUMN_DATETIME]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns[COLUMN_OWNER]!.Width = 50;
            grid.Columns[COLUMN_OWNER]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns[COLUMN_MESSAGE]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            grid.DataSource = dt;

            return dt;
        }

    }
}
