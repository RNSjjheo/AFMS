using AFMSDll;
using System.Runtime.InteropServices;

namespace AFMSLoggerMonitors
{
    internal sealed class PanelLogging : AFMSPanel
    {
        private const int MaxLines = 2000;
        private const int EmGetScrollPosition = 0x04DD;
        private const int EmSetScrollPosition = 0x04DE;
        private static readonly TimeSpan AutoScrollResumeDelay = TimeSpan.FromMinutes(3);
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
        private readonly AutoScrollButton uiBtnAutoScroll;
        private readonly System.Windows.Forms.Timer autoScrollTimer;
        private bool suppressScrollEvents;
        private DateTime lastUserScrollTimeUtc;

        public PanelLogging()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            BorderThickness = 0;

            uiTpMain = new TableLayoutPanel();

            uiTpMain.BackColor = Color.Transparent;
            uiTpMain.ColumnCount = 2;
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.Margin = Padding.Empty;
            uiTpMain.Padding = Padding.Empty;
            uiTpMain.RowCount = 3;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, TabCommon.TITILE_HIGTH));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, TabCommon.TITILE_MARGIN));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiLbTitle = TabCommon.CreateTitleLabel("로그 메시지");

            logTextBox = new RichTextBox();
            logTextBox.BackColor = LogBackgroundColor;
            logTextBox.BorderStyle = BorderStyle.None;
            logTextBox.DetectUrls = false;
            logTextBox.Dock = DockStyle.Fill;
            logTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            logTextBox.ForeColor = MessageColor;
            logTextBox.HideSelection = false;
            logTextBox.Margin = Padding.Empty;
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = RichTextBoxScrollBars.Both;
            logTextBox.WordWrap = false;
            logTextBox.VScroll += LogTextBox_UserScrolled;
            logTextBox.HScroll += LogTextBox_UserScrolled;
            logTextBox.MouseWheel += LogTextBox_MouseWheel;

            uiBtnAutoScroll = new AutoScrollButton();
            uiBtnAutoScroll.Click += UiBtnAutoScroll_Click;

            autoScrollTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            autoScrollTimer.Tick += AutoScrollTimer_Tick;
            autoScrollTimer.Start();

            uiTpMain.Controls.Add(uiLbTitle, 0, 0);
            uiTpMain.Controls.Add(uiBtnAutoScroll, 1, 0);
            uiTpMain.Controls.Add(logTextBox, 0, 2);
            uiTpMain.SetColumnSpan(logTextBox, 2);
            Controls.Add(uiTpMain);
        }

        public void Append(ViewLogMsg message)
        {
            void AppendCore()
            {
                Point scrollPosition = GetScrollPosition();
                suppressScrollEvents = true;
                string level = message.LogLevel.ToUpperInvariant();
                string normalizedMessage = message.LogMsg.Replace("\r\n", "\n").Replace('\r', '\n');
                string[] lines = normalizedMessage.Split('\n');

                try
                {
                    for (int index = 0; index < lines.Length; index++)
                    {
                        AppendPart(index == 0 ? $"{message.LogTime:HH:mm:ss.fff}    " : new string(' ', 16), TimeColor);
                        AppendPart(index == 0 ? $"{level,-8}" : new string(' ', 8), GetLevelColor(level));
                        AppendPart(lines[index] + Environment.NewLine, MessageColor);
                    }

                    TrimOldLines();
                    if (uiBtnAutoScroll.AutoScrollEnabled) ScrollToEnd();
                    else SetScrollPosition(scrollPosition);
                }
                finally
                {
                    suppressScrollEvents = false;
                }
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

        private void TrimOldLines()
        {
            if (logTextBox.Lines.Length <= MaxLines) return;
            int removeThrough = logTextBox.GetFirstCharIndexFromLine(logTextBox.Lines.Length - MaxLines);
            logTextBox.Select(0, removeThrough);
            logTextBox.SelectedText = string.Empty;
        }

        private void LogTextBox_UserScrolled(object? sender, EventArgs e)
        {
            if (!suppressScrollEvents) DisableAutoScroll();
        }

        private void LogTextBox_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!suppressScrollEvents) DisableAutoScroll();
        }

        private void UiBtnAutoScroll_Click(object? sender, EventArgs e)
        {
            if (uiBtnAutoScroll.AutoScrollEnabled) DisableAutoScroll();
            else EnableAutoScroll();
        }

        private void AutoScrollTimer_Tick(object? sender, EventArgs e)
        {
            if (!uiBtnAutoScroll.AutoScrollEnabled && DateTime.UtcNow - lastUserScrollTimeUtc >= AutoScrollResumeDelay) EnableAutoScroll();
        }

        private void DisableAutoScroll()
        {
            uiBtnAutoScroll.AutoScrollEnabled = false;
            lastUserScrollTimeUtc = DateTime.UtcNow;
        }

        private void EnableAutoScroll()
        {
            uiBtnAutoScroll.AutoScrollEnabled = true;
            suppressScrollEvents = true;
            try
            {
                ScrollToEnd();
            }
            finally
            {
                suppressScrollEvents = false;
            }
        }

        private void ScrollToEnd()
        {
            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.SelectionLength = 0;
            logTextBox.ScrollToCaret();
        }

        private Point GetScrollPosition()
        {
            Point position = Point.Empty;
            if (logTextBox.IsHandleCreated) SendMessage(logTextBox.Handle, EmGetScrollPosition, IntPtr.Zero, ref position);
            return position;
        }

        private void SetScrollPosition(Point position)
        {
            if (logTextBox.IsHandleCreated) SendMessage(logTextBox.Handle, EmSetScrollPosition, IntPtr.Zero, ref position);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                autoScrollTimer.Stop();
                autoScrollTimer.Tick -= AutoScrollTimer_Tick;
                autoScrollTimer.Dispose();
                logTextBox.VScroll -= LogTextBox_UserScrolled;
                logTextBox.HScroll -= LogTextBox_UserScrolled;
                logTextBox.MouseWheel -= LogTextBox_MouseWheel;
                uiBtnAutoScroll.Click -= UiBtnAutoScroll_Click;
            }

            base.Dispose(disposing);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wordParameter, ref Point longParameter);
    }
}
