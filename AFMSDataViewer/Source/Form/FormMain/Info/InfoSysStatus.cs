using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AFMSDataViewer
{
    public partial class InfoSysStatus : UserControl
    {
        public Label uiLbDCInput;
        public Label uiLbDCOutput;
        public Label uiLbTemp;

        public RoundedTwoLabel uiTlInput;
        public RoundedTwoLabel uiTlOutput;
        public RoundedTwoLabel uiTlTemp;
        public RoundedTwoLabel uiTIProgLogger;
        public RoundedTwoLabel uiTIProgSender;

        private const int PADDING_RL = 6;
        private const int PADDING_TB = 6;
        public InfoSysStatus()
        {
            InitializeComponent();
            Padding gategorypadding = new Padding(3);

            uiTlInput = new RoundedTwoLabel(true);
            uiTlInput.Dock = DockStyle.Fill;
            uiTlInput.BorderColor = DllColorHelper.HexToColor("#FECED4");
            uiTlInput.BackColor = DllColorHelper.HexToColor("#FFF1F2");
            uiTlInput.ValueForeColor = DllColorHelper.HexToColor("#DF2785");
            uiTlInput.Key = "입력";
            uiTlInput.Value = "23.7V";

            uiTlOutput = new RoundedTwoLabel(true);
            uiTlOutput.Dock = DockStyle.Fill;
            uiTlOutput.BorderColor = DllColorHelper.HexToColor("#C1DCFD");
            uiTlOutput.BackColor = DllColorHelper.HexToColor("#EFF6FF");
            uiTlOutput.ValueForeColor = DllColorHelper.HexToColor("#1045E8");
            uiTlOutput.Key = "출력";
            uiTlOutput.Value = "22.6V";
            
            uiTlTemp = new RoundedTwoLabel(true);
            uiTlTemp.Dock = DockStyle.Fill;
            uiTlTemp.BorderColor = DllColorHelper.HexToColor("#FEEA96");
            uiTlTemp.BackColor = DllColorHelper.HexToColor("#FFFBEB");
            uiTlTemp.ValueForeColor = DllColorHelper.HexToColor("#DE7A43");
            uiTlTemp.Key = "온도";
            uiTlTemp.Value = "32.7℃";

            uiTIProgLogger = new RoundedTwoLabel(false);
            uiTIProgLogger.Dock = DockStyle.Fill;
            uiTIProgLogger.BorderColor = DllColorHelper.HexToColor("#E2E8F0");
            uiTIProgLogger.BackColor = DllColorHelper.HexToColor("#FFFFFF");
            uiTIProgLogger.ValueForeColor = DllColorHelper.HexToColor("#ED7C95");
            uiTIProgLogger.Key = "DataLogger";
            uiTIProgLogger.Value = "00:00";

            uiTIProgSender = new RoundedTwoLabel(false);
            uiTIProgSender.Dock = DockStyle.Fill;
            uiTIProgSender.BorderColor = DllColorHelper.HexToColor("#E2E8F0");
            uiTIProgSender.BackColor = DllColorHelper.HexToColor("#FFFFFF");
            uiTIProgSender.ValueForeColor = DllColorHelper.HexToColor("#ED7C95");
            uiTIProgSender.Key = "DataSender";
            uiTIProgSender.Value = "00:00";

            CommonTwoLabelSize(uiTlInput);
            CommonTwoLabelSize(uiTlOutput);
            CommonTwoLabelSize(uiTlTemp);
            CommonTwoLabelSize(uiTIProgLogger);
            CommonTwoLabelSize(uiTIProgSender);

            tableLayoutPanel1.BorderStyle = BorderStyle.None;
            tableLayoutPanel1.Margin = gategorypadding;
            tableLayoutPanel1.Padding = Padding.Empty;

            tableLayoutPanel2.Controls.Add(uiTlInput, 0, 0);
            tableLayoutPanel2.Controls.Add(uiTlOutput, 1, 0);
            tableLayoutPanel2.Controls.Add(uiTlTemp, 2, 0);
            tableLayoutPanel2.Margin = Padding.Empty;

            tableLayoutPanel3.Controls.Add(uiTIProgLogger, 0, 0);
            tableLayoutPanel3.Controls.Add(uiTIProgSender, 1, 0);
            tableLayoutPanel3.Margin = Padding.Empty;

            Thread diagprocess = new Thread(RunDiagProcess)
            {
                IsBackground = true,
                Name = "AFMSDataViewer.SystemStatus"
            };
            diagprocess.Start();
        }

        private void CommonTwoLabelSize(RoundedTwoLabel tl)
        {
            tl.ValueFont = new Font(uiTlTemp.ValueFont.FontFamily, 9F, FontStyle.Bold);
            tl.KeyFont = new Font(uiTlTemp.ValueFont.FontFamily, 8F, FontStyle.Regular);
        }

        private void QueryVTHData()
        {
            string sql = $"SELECT ";
        }

        private void RunDiagProcess()
        {
            DateTime pretime = DateTime.Now.AddMinutes(10);
            TimeSpan diff = DateTime.Now - pretime;

            while (ApplicationDiag.IsRunning)
            {
                Thread.Sleep(50);
            
                if(diff.TotalSeconds>10)
                {
                    QueryVTHData();
                }
            }
        }


    }
}
