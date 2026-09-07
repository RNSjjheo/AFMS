using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSLoggerMonitors
{
    internal class PanelDiag : AFMSPanel
    {
        private TableLayoutPanel uiTpMain;
        private TableLayoutPanel uiTpRow0;
        private TableLayoutPanel uiTpRow1;
        private DiagCard uiDcStartTime;
        private DiagCard uiDcMemoryUse;
        private DiagCard uiDcLastMeas;
        private Label uiLbTitle;
        private Label uiLbVersion;
        public PanelDiag()
        {
            DoubleBuffered = true;
            BackColor = Color.White;

            uiTpMain = new TableLayoutPanel();
            uiTpMain.BackColor = Color.Transparent;
            uiTpMain.ColumnCount = 1;
            uiTpMain.RowCount = 3;
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.Margin = Padding.Empty;
            uiTpMain.Padding = new Padding(0, 0, 0, 0);
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, TabCommon.TITILE_HIGTH));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, TabCommon.TITILE_MARGIN));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiLbTitle = TabCommon.CreateTitleLabel("운영 정보");
            uiLbVersion = TabCommon.CreateLabel("-.-.-.-", 9F, FontStyle.Regular, TabCommon.DescriptionColor);
            uiLbVersion.TextAlign = ContentAlignment.MiddleRight;

            uiDcStartTime = CreateCard("시작시간");
            uiDcStartTime.Margin = new Padding(0, 0, 5, 0);
            uiDcStartTime.Value = "-";

            uiDcMemoryUse = CreateCard("메모리 사용량");
            uiDcMemoryUse.Margin = new Padding(5, 0, 5, 0);
            uiDcMemoryUse.Value = "-";

            uiDcLastMeas = CreateCard("최근 수집 시간");
            uiDcLastMeas.Margin = new Padding(5, 0, 5, 0);
            uiDcLastMeas.Value = "-";

            uiTpRow0 = CreateTableLayout(2);
            uiTpRow0.Controls.Add(uiLbTitle, 0, 0);
            uiTpRow0.Controls.Add(uiLbVersion,1, 0);

            uiTpRow1 = CreateTableLayout(3);
            uiTpRow1.Controls.Add(uiDcStartTime, 0, 0);
            uiTpRow1.Controls.Add(uiDcMemoryUse, 1, 0);
            uiTpRow1.Controls.Add(uiDcLastMeas, 2, 0);

            uiTpMain.Controls.Add(uiTpRow0, 0, 0);
            uiTpMain.Controls.Add(uiTpRow1, 0, 2);

            Controls.Add(uiTpMain);
        }

        public void UpdateDiagnostics(LoggerDiagnostics diagnostics)
        {
            string versionText = string.IsNullOrWhiteSpace(diagnostics.ProgramVersion) ? string.Empty : $"v{diagnostics.ProgramVersion}";
            TabCommon.SetLabelText(uiLbVersion, versionText);
            uiDcStartTime.Value = diagnostics.ServiceStartTime.ToString("yyyy/MM/dd HH:mm:ss");
            uiDcMemoryUse.Value = $"{diagnostics.MemoryUsageBytes / 1024.0 / 1024.0:0.00} MB";
            uiDcLastMeas.Value = diagnostics.LastMeasurementTime?.ToString("yyyy/MM/dd HH:mm:ss") ?? "-";
        }

        public void SetDisconnected()
        {
            TabCommon.SetLabelText(uiLbVersion, "-.-.-.-");
            uiDcStartTime.Value = "-";
        }


        private TableLayoutPanel CreateTableLayout(int colCount)
        {
            TableLayoutPanel result =  new TableLayoutPanel();
            result.RowCount = 1;
            result.ColumnCount = colCount;
            result.Dock = DockStyle.Fill;
            result.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            for (int i = 0; i < colCount; i++)
            {
                result.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F/ colCount));
            }

            return result;
        }

        private static DiagCard CreateCard(string title)
        {
            DiagCard result = new DiagCard();
            result.Dock = DockStyle.Fill;
            result.Title = title;

            return result;
        }
    }
}
