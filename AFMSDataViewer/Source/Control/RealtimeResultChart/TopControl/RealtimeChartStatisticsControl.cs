namespace AFMSDataViewer
{
    /// <summary>차트 하단에 최소, 평균, 최대 값을 한 줄로 표시합니다.</summary>
    public sealed class RealtimeChartStatisticsControl : TableLayoutPanel
    {
        public const int FixedHeight = 30;
        private static readonly Color AccentColor = Color.FromArgb(37, 99, 235);
        private static readonly Color CaptionColor = Color.FromArgb(100, 116, 139);
        private static readonly Color ValueColor = Color.FromArgb(30, 41, 59);
        private static readonly Color DividerColor = Color.FromArgb(225, 234, 242);

        private readonly Label minimumValue;
        private readonly Label averageValue;
        private readonly Label maximumValue;

        public RealtimeChartStatisticsControl()
        {
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
            Padding = new Padding(0, 0, 0, 0);
            BackColor = Color.FromArgb(250, 252, 255);
            RowCount = 1;
            ColumnCount = 3;
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));

            minimumValue = AddStatistic(0, "최소");
            averageValue = AddStatistic(1, "평균");
            maximumValue = AddStatistic(2, "최대");
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        public void SetValues(string minimum, string average, string maximum)
        {
            minimumValue.Text = minimum;
            averageValue.Text = average;
            maximumValue.Text = maximum;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using Pen pen = new(DividerColor);
            e.Graphics.DrawLine(pen, 0, 0, Width, 0);
            int contentWidth = Math.Max(0, Width - Padding.Horizontal);
            int firstDivider = Padding.Left + contentWidth / 3;
            int secondDivider = Padding.Left + contentWidth * 2 / 3;
            e.Graphics.DrawLine(pen, firstDivider, 7, firstDivider, Height - 7);
            e.Graphics.DrawLine(pen, secondDivider, 7, secondDivider, Height - 7);
        }

        private Label AddStatistic(int column, string caption)
        {
            TableLayoutPanel item = new()
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                BackColor = Color.Transparent,
                RowCount = 1,
                ColumnCount = 5
            };
            item.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            item.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            item.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            item.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            item.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            item.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Label dot = CreateLabel("●", AccentColor, FontStyle.Regular);
            dot.Font = new Font("맑은 고딕", 7F);
            dot.Margin = new Padding(0, 0, 6, 0);
            Label title = CreateLabel(caption, CaptionColor, FontStyle.Regular);
            title.Margin = new Padding(0, 0, 10, 0);
            Label value = CreateLabel("-", ValueColor, FontStyle.Bold);

            item.Controls.Add(dot, 1, 0);
            item.Controls.Add(title, 2, 0);
            item.Controls.Add(value, 3, 0);
            Controls.Add(item, column, 0);
            return value;
        }

        private static Label CreateLabel(string text, Color color, FontStyle style) => new()
        {
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Margin = Padding.Empty,
            Text = text,
            ForeColor = color,
            BackColor = Color.Transparent,
            Font = new Font("맑은 고딕", 8.5F, style),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
