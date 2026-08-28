using AFMSDll;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AFMSDataViewer
{
    public partial class InfoDevDetail : UserControl
    {
        private static readonly ILog Log = LogManager.GetLogger("SYS");
        private const int HEIGTH_MID_HEADER = 28;
        private const int HEIGTH_DATA_NODE = 30;
        private const int HEIGTH_EMPEY_LINE = 2;
        private TableLayoutPanel uiTpMain;
        private List<DeviceInfoDetail> MidPowers = new();
        private List<DeviceInfoDetail> MidLevels = new();
        private List<DeviceInfoDetail> MidVelos = new();
        private DeviceInfoDetail MidVTH;
        private DeviceInfoDetail MidLvl;

        public InfoDevDetail()
        {
            InitializeComponent();

            uiPnMain.HeaderText = "장비 정보";
            uiTpMain = uiPnMain.ContentLayout;
            uiTpMain.RowStyles.Clear();
            uiTpMain.ColumnStyles.Clear();
            uiTpMain.RowCount = 4;
            uiTpMain.ColumnCount = 1;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, HEIGTH_MID_HEADER + HEIGTH_DATA_NODE + HEIGTH_EMPEY_LINE));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, HEIGTH_MID_HEADER + HEIGTH_DATA_NODE + HEIGTH_EMPEY_LINE));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, HEIGTH_MID_HEADER + HEIGTH_DATA_NODE + HEIGTH_EMPEY_LINE));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.Margin = Padding.Empty;
            uiTpMain.Padding = Padding.Empty;

            MidVTH = new DeviceInfoDetail();
            MidVTH.uiKey.Text = "VTHLogger";
            MidPowers.Add(MidVTH);

            MidLvl = new DeviceInfoDetail();
            MidLvl.uiKey.Text = "RnsWater";
            MidLevels.Add(MidLvl);

            CreateMidHeader(0, MidPowers, "전원감시");
            CreateMidHeader(1, MidLevels, "수위계");
            CreateMidHeader(2, MidVelos, "유속계");
        }

        private void CreateMidHeader(int midIndex, List<DeviceInfoDetail> panels, string mid)
        {
            Control? previous = uiTpMain.GetControlFromPosition(0, midIndex);
            if (previous != null)
            {
                uiTpMain.Controls.Remove(previous);
                previous.Dispose();
            }

            uiTpMain.RowStyles[midIndex].Height = HEIGTH_MID_HEADER;
            uiTpMain.RowStyles[midIndex].Height += (HEIGTH_DATA_NODE * panels.Count);
            uiTpMain.RowStyles[midIndex].Height += (midIndex != 2) ? HEIGTH_EMPEY_LINE : 0;

            AFMSLabel label = new AFMSLabel();
            label.Dock = DockStyle.Fill;
            label.BackColor = Color.White;
            label.BorderThickness = 0;
            label.BorderRadius = 0;
            label.BorderStyle = BorderStyle.None;
            label.Margin = Padding.Empty;
            label.Padding = new Padding(12, 0, 0, 0);
            label.CharacterSpacing = -0.5F;
            label.TextAlign = ContentAlignment.BottomLeft;
            label.Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);
            label.ForeColor = Color.Black;
            label.Text = mid;

            Panel space = new Panel();
            space.Dock = DockStyle.Fill;
            space.BackColor = DllColorHelper.HexToColor("#F0F4F9");
            space.Padding = Padding.Empty;
            space.Margin = new Padding(12, 0, 12, 0);

            TableLayoutPanel tpMid = new TableLayoutPanel();
            tpMid.Dock = DockStyle.Fill;
            tpMid.BackColor = Color.Transparent;
            tpMid.Padding = Padding.Empty;
            tpMid.Margin = Padding.Empty;
            tpMid.RowStyles.Clear();
            tpMid.ColumnStyles.Clear();
            tpMid.RowCount = 1 + panels.Count + 1;
            tpMid.ColumnCount = 1;
            tpMid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tpMid.RowStyles.Add(new RowStyle(SizeType.Absolute, HEIGTH_MID_HEADER));
            foreach (DeviceInfoDetail panel in panels)
            {
                tpMid.RowStyles.Add(new RowStyle(SizeType.Absolute, HEIGTH_DATA_NODE));
            }
            tpMid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tpMid.RowStyles.Add(new RowStyle(SizeType.Absolute, HEIGTH_EMPEY_LINE));
            tpMid.Controls.Add(label, 0, 0);

            int rowid = 1;
            foreach(DeviceInfoDetail panel in panels)
            {
                panel.Padding = label.Padding;
                tpMid.Controls.Add(panel, 0, rowid++);
            }

            if(midIndex !=2) tpMid.Controls.Add(space, 0, panels.Count + 1);
            uiTpMain.Controls.Add(tpMid, 0, midIndex);
        }

        private void UpdatePowers()
        {
            string sql = "SELECT";
            sql += "\n" + $"{FbtSETUP.COL_VALUE05}";
            sql += "\n" + $"FROM {FbtSETUP.TABLE_NAME}";
            sql += "\n" + $"WHERE {FbtSETUP.COL_PK1} = 20";
            sql += "\n" + $"AND {FbtSETUP.COL_PK2} = 2";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                MidVTH.uiValue.Text = row[FbtSETUP.COL_VALUE05].ToString();
                return;
            }
        }

        private void UpdateLevels()
        {
            string sql = "SELECT";
            sql += "\n" + $"{FbtSETUP.COL_VALUE01}, ";
            sql += "\n" + $"{FbtSETUP.COL_VALUE02} ";
            sql += "\n" + $"FROM {FbtSETUP.TABLE_NAME}";
            sql += "\n" + $"WHERE {FbtSETUP.COL_PK1} = 10";
            sql += "\n" + $"AND {FbtSETUP.COL_PK2} = 4";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                MidLvl.uiValue.Text = row[FbtSETUP.COL_VALUE02].ToString();
                return;
            }
        }

        private void UpdateVelocities()
        {
            string sql = $"SELECT {FbtAFMSHydroMeter.COL_ID}, " +
                $"{FbtAFMSHydroMeter.COL_DEVICE_NAME}, {FbtAFMSHydroMeter.COL_COMM_CONFIG} " +
                $"FROM {FbtAFMSHydroMeter.TABLE_NAME} ORDER BY {FbtAFMSHydroMeter.COL_ID}";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            DataTable table = db.Execute(sql, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                Log.Error($"유속계 장비 정보 조회 실패: {error}");
                return;
            }

            MidVelos.Clear();
            foreach (DataRow row in table.Rows)
            {
                string deviceName = Convert.ToString(row[FbtAFMSHydroMeter.COL_DEVICE_NAME])?.Trim() ?? string.Empty;
                Enum.TryParse(deviceName, true, out HydroMeterType meterType);

                DeviceInfoDetail dev = new DeviceInfoDetail();
                dev.uiKey.Text = EnumPaser.GetKorString(meterType);
                dev.uiValue.Text = Convert.ToString(row[FbtAFMSHydroMeter.COL_COMM_CONFIG])?.Trim() ?? string.Empty;
                dev.ColumnStyles[0].Width = 75F;
                dev.ColumnStyles[1].Width = 0F;
                dev.uiDesc.Visible = false;
                MidVelos.Add(dev);
            }

            CreateMidHeader(2, MidVelos, "유속계");
        }

        public void ReadDatabase()
        {
            UpdatePowers();
            UpdateLevels();
            UpdateVelocities();
        }
    }
}
