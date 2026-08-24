using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    public enum GuideLevelType
    {
        Level0 = 0,
        Level1,
        Level2,
        Level3
    }

    [ToolboxItem(true)]
    public class AFMSGuidePanel : UserControl
    {
        private readonly GuideHeader _Header;
        private readonly FlowLayoutPanel _ItemPanel;

        private Color _borderColor = Color.FromArgb(190, 198, 205);
        private Color _TitleForeColor = Color.FromArgb(45, 45, 45);
        private Color _ItemForeColor = Color.FromArgb(75, 75, 75);
        private Color _IconColor = Color.FromArgb(30, 145, 70);
        private Color _BulletColor = Color.FromArgb(75, 75, 75);

        private float _borderThickness = 1F;
        private int _borderRadius = 8;
        private int _ItemSpacing = 2;
        private int _LevelIndentSpaceCount = 2;

        public AFMSGuidePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            Padding = new Padding(15, 5, 18, 14);

            _Header = new GuideHeader();
            _Header.Dock = DockStyle.Top;
            _Header.Height = 32;
            _Header.Font = Font;
            _Header.Title = "설정 안내";
            _Header.TitleForeColor = _TitleForeColor;
            _Header.IconColor = _IconColor;

            _ItemPanel = new FlowLayoutPanel();
            _ItemPanel.Dock = DockStyle.Fill;
            _ItemPanel.FlowDirection = FlowDirection.TopDown;
            _ItemPanel.WrapContents = false;
            _ItemPanel.AutoScroll = true;
            _ItemPanel.BackColor = Color.Transparent;
            _ItemPanel.Padding = new Padding(0, 4, 0, 0);

            Controls.Add(_ItemPanel);
            Controls.Add(_Header);

            Size = new Size(300, 330);
        }

        [Category("AFMS Appearance")]
        [DefaultValue("설정 안내")]
        public string Title
        {
            get => _Header.Title;
            set { _Header.Title = value ?? string.Empty; _Header.Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1F)]
        public float BorderThickness
        {
            get => _borderThickness;
            set { _borderThickness = Math.Max(0F, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(8)]
        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); UpdateRegion(); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color TitleForeColor
        {
            get => _TitleForeColor;
            set { _TitleForeColor = value; _Header.TitleForeColor = value; _Header.Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color ItemForeColor
        {
            get => _ItemForeColor;
            set
            {
                _ItemForeColor = value;

                foreach (Control control in _ItemPanel.Controls)
                {
                    if (control is GuideItem item) item.ItemForeColor = value;
                }

                _ItemPanel.Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BulletColor
        {
            get => _BulletColor;
            set
            {
                _BulletColor = value;

                foreach (Control control in _ItemPanel.Controls)
                {
                    if (control is GuideItem item) item.BulletColor = value;
                }

                _ItemPanel.Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color IconColor
        {
            get => _IconColor;
            set { _IconColor = value; _Header.IconColor = value; _Header.Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(12)]
        public int ItemSpacing
        {
            get => _ItemSpacing;
            set
            {
                _ItemSpacing = Math.Max(0, value);
                UpdateItemWidths();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(2)]
        public int LevelIndentSpaceCount
        {
            get => _LevelIndentSpaceCount;
            set
            {
                _LevelIndentSpaceCount = Math.Max(0, value);

                foreach (Control control in _ItemPanel.Controls)
                {
                    if (control is not GuideItem item) continue;
                    item.IndentSpaceCount = _LevelIndentSpaceCount;
                    item.UpdateHeight();
                    item.Invalidate();
                }
            }
        }

        public void Add(GuideLevelType level, string text)
        {
            GuideItem item = new GuideItem();
            item.Level = level;
            item.IndentSpaceCount = LevelIndentSpaceCount;
            item.Text = text ?? string.Empty;
            item.Font = Font;
            item.ItemForeColor = ItemForeColor;
            item.BulletColor = BulletColor;
            item.Margin = new Padding(0, 0, 0, ItemSpacing);

            _ItemPanel.Controls.Add(item);

            UpdateItemWidths();
        }

        public void Add(GuideLevelType level, params string[] items)
        {
            if (items == null) return;

            foreach (string item in items) Add(level, item);
        }

        public void Clear()
        {
            _ItemPanel.Controls.Clear();
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _ItemPanel.Controls.Count) return;

            Control control = _ItemPanel.Controls[index];
            _ItemPanel.Controls.RemoveAt(index);
            control.Dispose();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            if (_Header == null || _ItemPanel == null) return;

            _Header.Font = Font;

            foreach (Control control in _ItemPanel.Controls) control.Font = Font;

            UpdateItemWidths();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
            UpdateItemWidths();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (BorderThickness <= 0F || ClientSize.Width <= BorderThickness || ClientSize.Height <= BorderThickness) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            float offset = BorderThickness / 2F;
            RectangleF rect = new RectangleF(offset, offset, ClientSize.Width - BorderThickness, ClientSize.Height - BorderThickness);
            float radius = Math.Max(0F, BorderRadius - offset);

            using GraphicsPath path = CreateRoundedPath(rect, radius);
            using Pen pen = new Pen(BorderColor, BorderThickness) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round };

            e.Graphics.DrawPath(pen, path);
        }

        private void UpdateItemWidths()
        {
            if (_ItemPanel == null || _ItemPanel.ClientSize.Width <= 0) return;

            int width = Math.Max(20, _ItemPanel.ClientSize.Width - 8);

            foreach (Control control in _ItemPanel.Controls)
            {
                if (control is not GuideItem item) continue;

                item.Width = width;
                item.Margin = new Padding(0, 0, 0, ItemSpacing);
                item.UpdateHeight();
            }
        }

        private void UpdateRegion()
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0) return;

            using GraphicsPath path = CreateRoundedPath(new RectangleF(0, 0, Width, Height), BorderRadius);

            Region oldRegion = Region;
            Region = new Region(path);
            oldRegion?.Dispose();
        }

        private static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float diameter = Math.Min(radius * 2F, Math.Min(rect.Width, rect.Height));

            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private class GuideHeader : Control
        {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public string Title { get; set; } = string.Empty;
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public Color TitleForeColor { get; set; } = Color.FromArgb(45, 45, 45);
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public Color IconColor { get; set; } = Color.FromArgb(30, 145, 70);

            public GuideHeader()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                const int iconSize = 19;
                int iconX = 0;
                int iconY = (Height - iconSize) / 2;

                using SolidBrush iconBrush = new SolidBrush(IconColor);
                e.Graphics.FillEllipse(iconBrush, iconX, iconY, iconSize, iconSize);

                using Font iconFont = new Font("Segoe UI", 9F, FontStyle.Bold);
                Rectangle iconRect = new Rectangle(iconX, iconY - 1, iconSize, iconSize + 1);

                TextRenderer.DrawText(e.Graphics, "i", iconFont, iconRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                using Font titleFont = new Font(Font, FontStyle.Bold);
                Rectangle titleRect = new Rectangle(iconX + iconSize + 10, 0, Math.Max(0, Width - iconSize - 10), Height);

                TextRenderer.DrawText(e.Graphics, Title, titleFont, titleRect, TitleForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
        }

        private class GuideItem : Control
        {
            private const int BULLET_WIDTH = 16;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public GuideLevelType Level { get; set; } = GuideLevelType.Level0;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public int IndentSpaceCount { get; set; } = 2;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public Color ItemForeColor { get; set; } = Color.FromArgb(75, 75, 75);
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public Color BulletColor { get; set; } = Color.FromArgb(75, 75, 75);

            public GuideItem()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

                BackColor = Color.Transparent;
                Height = 30;
            }

            protected override void OnTextChanged(EventArgs e)
            {
                base.OnTextChanged(e);
                UpdateHeight();
                Invalidate();
            }

            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                UpdateHeight();
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                UpdateHeight();
            }

            public void UpdateHeight()
            {
                int indentWidth = GetIndentWidth();
                if (Width <= indentWidth + BULLET_WIDTH) return;

                int textWidth = Math.Max(1, Width - indentWidth - BULLET_WIDTH);
                Size size = TextRenderer.MeasureText(Text, Font, new Size(textWidth, int.MaxValue), TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                Height = Math.Max(Font.Height + 4, size.Height + 4);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                string bullet = Level switch
                {
                    GuideLevelType.Level0 => "•",
                    GuideLevelType.Level1 => "-",
                    GuideLevelType.Level2 => "",
                    GuideLevelType.Level3 => "",
                    _ => ""
                };

                int indentWidth = GetIndentWidth();
                Rectangle bulletRect = new Rectangle(indentWidth, 0, BULLET_WIDTH - 4, Font.Height + 6);
                Rectangle textRect = new Rectangle(indentWidth + BULLET_WIDTH, 0, Math.Max(0, Width - indentWidth - BULLET_WIDTH), Height);

                if (!string.IsNullOrEmpty(bullet)) TextRenderer.DrawText(e.Graphics, bullet, Font, bulletRect, BulletColor, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ItemForeColor, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            }

            private int GetIndentWidth()
            {
                int spaceCount = Math.Max(0, (int)Level * IndentSpaceCount);
                if (spaceCount == 0) return 0;
                return TextRenderer.MeasureText(new string(' ', spaceCount), Font, new Size(int.MaxValue, Font.Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
            }
        }
    }
}