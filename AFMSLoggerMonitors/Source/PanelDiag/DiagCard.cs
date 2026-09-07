using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AFMSLoggerMonitors
{
    internal class DiagCard : AFMSPanel
    {
        private TableLayoutPanel uiTpMain = new TableLayoutPanel();
        private Label uiLbTitle;
        private Label uiLbValue;
        [Category("AFMS Data")]
        [DefaultValue("영상유속계")]
        public string Title
        {
            get => uiLbTitle.Text;
            set => TabCommon.SetLabelText(uiLbTitle, value);
        }

        [Category("AFMS Data")]
        [DefaultValue("AFMSLoggerVideoHydorsem")]
        public string Value
        {
            get => uiLbValue.Text;
            set => TabCommon.SetLabelText(uiLbValue, value);
        }

        public DiagCard()
        {
            this.BackColor = DllColorHelper.HexToColor("#F0F4F7");
            this.BorderColor = this.BackColor;

            uiTpMain = new TableLayoutPanel();
            uiTpMain.RowCount = 2;
            uiTpMain.ColumnCount = 1;
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            uiLbTitle = TabCommon.CreateLabel("설명", 8F, FontStyle.Regular, TabCommon.DescriptionColor);
            uiLbValue = TabCommon.CreateLabel("", 11F, FontStyle.Bold, TabCommon.TextColor);
            uiLbValue.Padding = Padding.Empty;
            uiLbValue.TextAlign = ContentAlignment.MiddleCenter;

            uiTpMain.Controls.Add(uiLbTitle, 0, 0);
            uiTpMain.Controls.Add(uiLbValue, 0, 1);

            Controls.Add(uiTpMain);
        }
    }
}
