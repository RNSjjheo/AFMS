using AFMSDll;
namespace AFMSLoggerMonitors
{
    internal sealed class PanelLogging : AFMSPanel
    {
        private const int MaxLines = 2000;
        private static readonly Color LogBackgroundColor = DllColorHelper.HexToColor("#111B25");
        private static readonly Color TimeColor = DllColorHelper.HexToColor("#7D8D9E");
        private static readonly Color MessageColor = DllColorHelper.HexToColor("#C5CED7");
        private static readonly Color InfoColor = DllColorHelper.HexToColor("#55C6A9");
        private static readonly Color DebugColor = DllColorHelper.HexToColor("#61AFEF");
        private static readonly Color WarnColor = DllColorHelper.HexToColor("#E5C07B");
        private static readonly Color ErrorColor = DllColorHelper.HexToColor("#E06C75");
        private readonly TableLayoutPanel uiTpMain;
        private readonly RichTextBox logTextBox;
        private readonly Label uiLbTitle;

        public PanelLogging()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            BorderThickness = 0;

            uiTpMain = new TableLayoutPanel
            {
                BackColor = Color.Transparent,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                RowCount = 3
            };
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, TabCommon.TITILE_HIGTH));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, TabCommon.TITILE_MARGIN));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiLbTitle = TabCommon.CreateTitleLabel("로그 메시지");
            logTextBox = new RichTextBox
            {
                BackColor = LogBackgroundColor,
                BorderStyle = BorderStyle.None,
                DetectUrls = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = MessageColor,
                HideSelection = false,
                Margin = Padding.Empty,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = false
            };

            uiTpMain.Controls.Add(uiLbTitle, 0, 0);
            uiTpMain.Controls.Add(logTextBox, 0, 2);
            Controls.Add(uiTpMain);
        }

        public void Append(ViewLogMsg message)
        {
            void AppendCore()
            {
                string level = message.LogLevel.ToUpperInvariant();
                string normalizedMessage = message.LogMsg.Replace("\r\n", "\n").Replace('\r', '\n');
                string[] lines = normalizedMessage.Split('\n');

                for (int index = 0; index < lines.Length; index++)
                {
                    AppendPart(index == 0 ? $"{message.LogTime:HH:mm:ss.fff}    " : new string(' ', 16), TimeColor);
                    AppendPart(index == 0 ? $"{level,-8}" : new string(' ', 8), GetLevelColor(level));
                    AppendPart(lines[index] + Environment.NewLine, MessageColor);
                }

                if (logTextBox.Lines.Length > MaxLines)
                {
                    int removeThrough = logTextBox.GetFirstCharIndexFromLine(logTextBox.Lines.Length - MaxLines);
                    logTextBox.Select(0, removeThrough);
                    logTextBox.SelectedText = string.Empty;
                }
                logTextBox.SelectionStart = logTextBox.TextLength;
                logTextBox.ScrollToCaret();
            }

            if (logTextBox.InvokeRequired) logTextBox.BeginInvoke(AppendCore);
            else AppendCore();
        }

        private void AppendPart(string text, Color color)
        {
            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.SelectionLength = 0;
            logTextBox.SelectionColor = color;
            logTextBox.AppendText(text);
        }

        private static Color GetLevelColor(string level)
        {
            return level switch
            {
                "DEBUG" => DebugColor,
                "INFO" => InfoColor,
                "WARN" => WarnColor,
                "ERROR" => ErrorColor,
                "FATAL" => ErrorColor,
                _ => MessageColor
            };
        }
    }
}
