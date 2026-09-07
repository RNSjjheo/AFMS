using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSLoggerMonitors
{
    internal class PanelDiag : AFMSPanel
    {
        private TableLayoutPanel uiTpMain;
        public PanelDiag()
        {
            DoubleBuffered = true;
            BackColor = Color.White;

            uiTpMain = new TableLayoutPanel();
            uiTpMain.BackColor = Color.Transparent;
            uiTpMain.ColumnCount = 1;
            uiTpMain.RowCount = 5;
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.Margin = Padding.Empty;
            uiTpMain.Padding = Padding.Empty;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 1F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
        }

        private static Label CreateLabel(string text, float fontSize, FontStyle fontStyle, Color foreColor)
        {
            return new Label
            {
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, fontSize, fontStyle, GraphicsUnit.Point),
                ForeColor = foreColor,
                Margin = Padding.Empty,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }
    }
}
