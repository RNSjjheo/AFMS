using AFMSDll;
using log4net;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace AFMSDataViewer
{
    public partial class InfoSysStatus : UserControl
    {
        private const int RefreshIntervalMilliseconds = 10_000;

        private static readonly ILog Log = LogManager.GetLogger("SYS");

        private readonly System.Windows.Forms.Timer _refreshTimer;
        private readonly RoundedTwoLabel _inputCard;
        private readonly RoundedTwoLabel _outputCard;
        private readonly RoundedTwoLabel _temperatureCard;

        public InfoSysStatus()
        {
            InitializeComponent();

            uiPnMain.HeaderText = "전원 정보";

            TableLayoutPanel content = uiPnMain.ContentLayout;
            content.ColumnStyles.Clear();
            content.RowStyles.Clear();
            content.ColumnCount = 3;
            content.RowCount = 1;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.Padding = new Padding(4);
            content.Margin = Padding.Empty;

            _inputCard = CreateCard("입력", "#FECED4", "#FFF1F2", "#DF2785");
            _outputCard = CreateCard("출력", "#C1DCFD", "#EFF6FF", "#1045E8");
            _temperatureCard = CreateCard("온도", "#FEEA96", "#FFFBEB", "#DE7A43");

            content.Controls.Add(_inputCard, 0, 0);
            content.Controls.Add(_outputCard, 1, 0);
            content.Controls.Add(_temperatureCard, 2, 0);

            _refreshTimer = new System.Windows.Forms.Timer { Interval = RefreshIntervalMilliseconds };
            _refreshTimer.Tick += (_, _) => ReadDatabase();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            ReadDatabase();
            _refreshTimer.Start();
        }

        private static RoundedTwoLabel CreateCard(string key, string borderColor, string backColor, string valueColor)
        {
            RoundedTwoLabel card = new RoundedTwoLabel(true)
            {
                Dock = DockStyle.Fill,
                BorderColor = DllColorHelper.HexToColor(borderColor),
                BackColor = DllColorHelper.HexToColor(backColor),
                ValueForeColor = DllColorHelper.HexToColor(valueColor),
                Key = key,
                Value = "-",
                Margin = new Padding(3, 2, 3, 2),
                ValueFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                KeyFont = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point)
            };

            return card;
        }

        public void ReadDatabase()
        {
            string sql = $"SELECT FIRST 1 {FbtVTHLOGGER.COL_DCCHARGE}, {FbtVTHLOGGER.COL_DCBATTERY}, {FbtVTHLOGGER.COL_TEMPERATURE}";
            sql += $" FROM {FbtVTHLOGGER.TABLE_NAME}";
            sql += $" ORDER BY {_FBTableBase.COL_MEASURE_DATE} DESC, {_FBTableBase.COL_MEASURE_TIME} DESC";

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(sql, out string error);

            if (!string.IsNullOrEmpty(error))
            {
                Log.Error($"VTHLogger 최신 전원 정보 조회 실패: {error}");
                return;
            }

            if (table.Rows.Count == 0)
            {
                ApplyValues("-", "-", "-");
                return;
            }

            DataRow row = table.Rows[0];
            ApplyValues(
                FormatMeasurement(row[FbtVTHLOGGER.COL_DCCHARGE], "V"),
                FormatMeasurement(row[FbtVTHLOGGER.COL_DCBATTERY], "V"),
                FormatMeasurement(row[FbtVTHLOGGER.COL_TEMPERATURE], "℃"));
        }

        private void ApplyValues(string input, string output, string temperature)
        {
            _inputCard.Value = input;
            _outputCard.Value = output;
            _temperatureCard.Value = temperature;
        }

        private static string FormatMeasurement(object value, string unit)
        {
            if (value == null || value == DBNull.Value) return "-";

            if (value is IConvertible)
            {
                try
                {
                    double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return $"{number:0.0#}{unit}";
                }
                catch (FormatException)
                {
                }
                catch (InvalidCastException)
                {
                }
                catch (OverflowException)
                {
                }
            }

            return $"{value}{unit}";
        }
    }
}
