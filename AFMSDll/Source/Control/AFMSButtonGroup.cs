using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    public class AFMSButtonGroupItem
    {
        public string Text { get; set; } = string.Empty;
        public Image? Image { get; set; }
        public Image? SelectedImage { get; set; }
        public object? Tag { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    [ToolboxItem(true)]
    [DefaultEvent(nameof(SelectedIndexChanged))]
    public class AFMSButtonGroup : Control
    {
        private readonly List<AFMSButtonGroupItem> _items = new List<AFMSButtonGroupItem>();

        private int _selectedIndex = -1;
        private int _borderRadius = 5;
        private float _borderThickness = 1F;

        private Color _selectedBackColor = Color.FromArgb(5, 149, 105);
        private Color _selectedForeColor = Color.White;
        private Color _normalBackColor = Color.White;
        private Color _normalForeColor = Color.FromArgb(100, 116, 139);
        private Color _borderColor = Color.FromArgb(226, 232, 239);

        public AFMSButtonGroup()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.Selectable,
                true);

            Size = new Size(90, 28);
            MinimumSize = new Size(30, 22);

            Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public event EventHandler? SelectedIndexChanged;

        #region Properties

        [Browsable(false)]
        public IReadOnlyList<AFMSButtonGroupItem> Items => _items;

        [Category("AFMS Behavior")]
        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int newValue;

                if (_items.Count == 0)
                    newValue = -1;
                else
                    newValue = Math.Max(0, Math.Min(_items.Count - 1, value));

                if (_selectedIndex == newValue)
                    return;

                _selectedIndex = newValue;

                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Browsable(false)]
        public AFMSButtonGroupItem? SelectedItem
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
                    return null;

                return _items[_selectedIndex];
            }
        }

        [Browsable(false)]
        public string SelectedText => SelectedItem?.Text ?? string.Empty;

        [Browsable(false)]
        public object? SelectedValue => SelectedItem?.Tag;

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedBackColor
        {
            get => _selectedBackColor;
            set
            {
                _selectedBackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedForeColor
        {
            get => _selectedForeColor;
            set
            {
                _selectedForeColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color NormalBackColor
        {
            get => _normalBackColor;
            set
            {
                _normalBackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color NormalForeColor
        {
            get => _normalForeColor;
            set
            {
                _normalForeColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1F)]
        public float BorderThickness
        {
            get => _borderThickness;
            set
            {
                _borderThickness = Math.Max(0F, value);
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(5)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        #endregion

        #region Items

        public AFMSButtonGroupItem AddButton(string text, object? tag = null)
        {
            AFMSButtonGroupItem item = new AFMSButtonGroupItem
            {
                Text = text,
                Tag = tag
            };

            _items.Add(item);

            if (_selectedIndex < 0)
                _selectedIndex = 0;

            Invalidate();

            return item;
        }

        public AFMSButtonGroupItem AddButton(Image image, object? tag = null)
        {
            ArgumentNullException.ThrowIfNull(image);

            AFMSButtonGroupItem item = new AFMSButtonGroupItem
            {
                Image = image,
                Tag = tag
            };

            _items.Add(item);
            if (_selectedIndex < 0) _selectedIndex = 0;
            Invalidate();
            return item;
        }

        public AFMSButtonGroupItem AddButton(Image image, Image selectedImage, object? tag = null)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(selectedImage);

            AFMSButtonGroupItem item = new AFMSButtonGroupItem
            {
                Image = image,
                SelectedImage = selectedImage,
                Tag = tag
            };

            _items.Add(item);
            if (_selectedIndex < 0) _selectedIndex = 0;
            Invalidate();
            return item;
        }

        public bool RemoveButton(AFMSButtonGroupItem item)
        {
            int index = _items.IndexOf(item);

            if (index < 0)
                return false;

            _items.RemoveAt(index);

            if (_items.Count == 0)
            {
                _selectedIndex = -1;
            }
            else if (_selectedIndex >= _items.Count)
            {
                _selectedIndex = _items.Count - 1;
            }

            Invalidate();
            return true;
        }

        public void ClearButtons()
        {
            _items.Clear();
            _selectedIndex = -1;
            Invalidate();
        }

        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_items.Count == 0)
                return;

            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float borderOffset = BorderThickness <= 1F ? 0.5F : BorderThickness / 2F;
            RectangleF outerRect = new RectangleF(borderOffset, borderOffset, Width - (borderOffset * 2F), Height - (borderOffset * 2F));

            using (GraphicsPath outerPath = CreateRoundRectPath(outerRect, BorderRadius))
            using (SolidBrush normalBrush = new SolidBrush(NormalBackColor))
            {
                g.FillPath(normalBrush, outerPath);
            }

            DrawSelectedBackground(g);
            DrawBorder(g);
            DrawTexts(g);
        }

        private void DrawSelectedBackground(Graphics g)
        {
            if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
                return;

            Rectangle rect = GetSegmentRectangle(_selectedIndex);

            using SolidBrush brush = new SolidBrush(SelectedBackColor);

            // 버튼이 하나뿐이면 네 모서리 모두 둥글게
            if (_items.Count == 1)
            {
                float borderOffset = BorderThickness <= 1F ? 0.5F : BorderThickness / 2F;
                RectangleF selectedRect = new RectangleF(borderOffset, borderOffset, Width - (borderOffset * 2F), Height - (borderOffset * 2F));

                using GraphicsPath path = CreateRoundRectPath(selectedRect, BorderRadius);
                g.FillPath(brush, path);

                return;
            }

            // 첫 번째 버튼
            if (_selectedIndex == 0)
            {
                using GraphicsPath path = CreateLeftSegmentPath(rect, BorderRadius);
                g.FillPath(brush, path);

                return;
            }

            // 마지막 버튼
            if (_selectedIndex == _items.Count - 1)
            {
                using GraphicsPath path = CreateRightSegmentPath(rect, BorderRadius);
                g.FillPath(brush, path);

                return;
            }

            // 중간 버튼
            g.FillRectangle(brush, rect);
        }

        private void DrawBorder(Graphics g)
        {
            float borderOffset = BorderThickness <= 1F ? 0.5F : BorderThickness / 2F;
            RectangleF outerRect = new RectangleF(borderOffset, borderOffset, Width - (borderOffset * 2F), Height - (borderOffset * 2F));

            using Pen pen = new Pen(BorderColor, BorderThickness) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round };
            using GraphicsPath path = CreateRoundRectPath(outerRect, BorderRadius);

            g.DrawPath(pen, path);

            // 버튼 사이 구분선
            for (int i = 0; i < _items.Count - 1; i++)
            {
                int x = GetSegmentRectangle(i).Right;

                g.DrawLine(pen, x, 1, x, Height - 2);
            }
        }

        private void DrawTexts(Graphics g)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                Rectangle rect = GetSegmentRectangle(i);
                Color foreColor = i == SelectedIndex ? SelectedForeColor : NormalForeColor;

                Image? image = i == SelectedIndex
                    ? _items[i].SelectedImage ?? _items[i].Image
                    : _items[i].Image;
                if (image != null)
                {
                    int width = Math.Min(image.Width, Math.Max(1, rect.Width - 10));
                    int height = Math.Min(image.Height, Math.Max(1, rect.Height - 10));
                    Rectangle imageRect = new(
                        rect.Left + (rect.Width - width) / 2,
                        rect.Top + (rect.Height - height) / 2,
                        width,
                        height);
                    g.DrawImage(image, imageRect);
                    continue;
                }

                TextRenderer.DrawText(
                    g,
                    _items[i].Text,
                    Font,
                    rect,
                    foreColor,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPadding);
            }
        }

        private Rectangle GetSegmentRectangle(int index)
        {
            if (_items.Count == 0 || index < 0 || index >= _items.Count)
                return Rectangle.Empty;

            // 나머지 픽셀까지 균등하게 분배
            int left = Width * index / _items.Count;
            int right = Width * (index + 1) / _items.Count;

            return new Rectangle(left, 0, right - left, Height);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            Focus();

            for (int i = 0; i < _items.Count; i++)
            {
                if (!GetSegmentRectangle(i).Contains(e.Location))
                    continue;

                SelectedIndex = i;
                break;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_items.Count == 0)
                return;

            if (e.KeyCode == Keys.Left)
            {
                SelectedIndex = Math.Max(0, SelectedIndex - 1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                SelectedIndex = Math.Min(_items.Count - 1, SelectedIndex + 1);
                e.Handled = true;
            }
        }

        private GraphicsPath CreateRoundRectPath(RectangleF rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float r = Math.Min(radius, Math.Min(rect.Width / 2F, rect.Height / 2F));
            float diameter = r * 2F;

            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();

            return path;
        }

        private GraphicsPath CreateLeftSegmentPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float r = Math.Min(radius, Math.Min(rect.Width / 2F, rect.Height / 2F));
            float diameter = r * 2F;

            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddLine(rect.Left + r, rect.Top, rect.Right, rect.Top);
            path.AddLine(rect.Right, rect.Top, rect.Right, rect.Bottom);
            path.AddLine(rect.Right, rect.Bottom, rect.Left + r, rect.Bottom);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();

            return path;
        }

        private GraphicsPath CreateRightSegmentPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float r = Math.Min(radius, Math.Min(rect.Width / 2F, rect.Height / 2F));
            float diameter = r * 2F;

            path.AddLine(rect.Left, rect.Top, rect.Right - r, rect.Top);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddLine(rect.Right - r, rect.Bottom, rect.Left, rect.Bottom);

            path.CloseFigure();

            return path;
        }

        public void PerformClick(AFMSButtonGroupItem item)
        {
            if (item == null)
                return;

            int index = _items.IndexOf(item);

            if (index < 0)
                return;

            SelectedIndex = index;
            OnClick(EventArgs.Empty);
        }
    }
}
