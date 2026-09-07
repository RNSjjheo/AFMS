using AFMSDll;

namespace AFMSLoggerMonitors
{
    internal sealed class PanelLog : AFMSPanel
    {
        private const int MaxLines = 2000;
        private readonly RichTextBox logTextBox;

        public PanelLog()
        {
            BackColor = Color.White;
            BorderThickness = 0;
            logTextBox = new RichTextBox
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                DetectUrls = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                WordWrap = false
            };
            Controls.Add(logTextBox);
        }

        public void Append(ViewLogMsg message)
        {
            void AppendCore()
            {
                string line = $"[{message.LogTime:yy-MM-dd HH:mm:ss.ff} {message.LogHost,-16}] {message.LogMsg}{Environment.NewLine}";
                logTextBox.AppendText(line);
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
    }
}
