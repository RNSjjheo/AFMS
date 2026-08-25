using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    [DefaultEvent(nameof(SelectedIndexChanged))]
    public class AFMSComboBox : _AFMSComboBoxBase
    {
        private readonly List<object> _Items = new List<object>();
        private int _SelectedIndex = -1;
        private string _PlaceholderText = "선택";
        private Color _HoverBorderColor = Color.FromArgb(190, 200, 214);
        private Color _ForeColor = Color.FromArgb(84, 98, 121);
        private Color _ArrowColor = Color.FromArgb(84, 98, 121);
        private Color _DropDownBackColor = Color.White;
        private Color _DropDownForeColor = Color.FromArgb(60, 70, 85);
        private Color _SelectedBackColor = Color.FromArgb(235, 248, 243);
        private Color _SelectedForeColor = Color.FromArgb(40, 90, 70);
        private int _HorizontalPadding = 14;
        private int _ArrowAreaWidth = 30;
        private int _DropDownItemHeight = 32;
        private int _DropDownMaxItems = 8;
        private bool _Hover;
        private ToolStripDropDown? _DropDown;
        private ListBox? _ListBox;

        public AFMSComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.White;
            Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Size = new Size(130, 30);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        [Category("AFMS Appearance")]
        [DefaultValue("선택")]
        public string PlaceholderText
        {
            get => _PlaceholderText;
            set { _PlaceholderText = value ?? string.Empty; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverBorderColor
        {
            get => _HoverBorderColor;
            set { _HoverBorderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor
        {
            get => _ForeColor;
            set { _ForeColor = value; base.ForeColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ArrowColor
        {
            get => _ArrowColor;
            set { _ArrowColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color DropDownBackColor
        {
            get => _DropDownBackColor;
            set { _DropDownBackColor = value; }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color DropDownForeColor
        {
            get => _DropDownForeColor;
            set { _DropDownForeColor = value; }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedBackColor
        {
            get => _SelectedBackColor;
            set { _SelectedBackColor = value; }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedForeColor
        {
            get => _SelectedForeColor;
            set { _SelectedForeColor = value; }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(14)]
        public int HorizontalPadding
        {
            get => _HorizontalPadding;
            set { _HorizontalPadding = Math.Max(0, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(30)]
        public int ArrowAreaWidth
        {
            get => _ArrowAreaWidth;
            set { _ArrowAreaWidth = Math.Max(18, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(32)]
        public int DropDownItemHeight
        {
            get => _DropDownItemHeight;
            set { _DropDownItemHeight = Math.Max(20, value); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(8)]
        public int DropDownMaxItems
        {
            get => _DropDownMaxItems;
            set { _DropDownMaxItems = Math.Max(1, value); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList Items => new ItemCollection(this);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _SelectedIndex;
            set
            {
                int newIndex = value;
                if (newIndex < -1) newIndex = -1;
                if (newIndex >= _Items.Count) newIndex = _Items.Count - 1;
                if (_SelectedIndex == newIndex) return;
                _SelectedIndex = newIndex;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? SelectedItem
        {
            get => _SelectedIndex >= 0 && _SelectedIndex < _Items.Count ? _Items[_SelectedIndex] : null;
            set => SelectedIndex = value == null ? -1 : _Items.IndexOf(value);
        }

        [Browsable(false)]
        public override string Text
        {
            get => SelectedItem?.ToString() ?? string.Empty;
            set
            {
                if (string.IsNullOrEmpty(value)) { SelectedIndex = -1; return; }
                for (int i = 0; i < _Items.Count; i++)
                {
                    if (string.Equals(_Items[i]?.ToString(), value, StringComparison.Ordinal)) { SelectedIndex = i; return; }
                }
            }
        }

        public event EventHandler? SelectedIndexChanged;

        public void Add(object item)
        {
            _Items.Add(item);
            Invalidate();
        }

        public void AddRange(params object[] items)
        {
            if (items == null) return;
            _Items.AddRange(items);
            Invalidate();
        }

        public void ClearItems()
        {
            _Items.Clear();
            SelectedIndex = -1;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            AFMSRoundedDrawing.SetHighQuality(e.Graphics);
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

            float inset = BorderThickness > 0F ? BorderThickness / 2F : 0F;
            RectangleF rect = new RectangleF(inset, inset, Math.Max(0F, ClientSize.Width - BorderThickness), Math.Max(0F, ClientSize.Height - BorderThickness));
            float radius = Math.Max(0F, Math.Min(BorderRadius, rect.Height / 2F) - inset);

            using GraphicsPath path = AFMSRoundedDrawing.CreatePath(rect, radius);
            using SolidBrush backBrush = new SolidBrush(BackColor);
            using Pen borderPen = new Pen(_Hover || Focused || (_DropDown?.Visible ?? false) ? HoverBorderColor : BorderColor, BorderThickness) { Alignment = PenAlignment.Center };
            e.Graphics.FillPath(backBrush, path);
            if (BorderThickness > 0F) e.Graphics.DrawPath(borderPen, path);

            string displayText = SelectedItem?.ToString() ?? PlaceholderText;
            Rectangle textRect = new Rectangle(HorizontalPadding, 0, Math.Max(0, Width - HorizontalPadding - ArrowAreaWidth), Height);
            TextRenderer.DrawText(e.Graphics, displayText, Font, textRect, ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            DrawArrow(e.Graphics);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _Hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _Hover = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            ToggleDropDown();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.F4) { ToggleDropDown(); e.Handled = true; return; }
            if (e.KeyCode == Keys.Down && _Items.Count > 0) { SelectedIndex = Math.Min(_Items.Count - 1, SelectedIndex + 1); e.Handled = true; return; }
            if (e.KeyCode == Keys.Up && _Items.Count > 0) { SelectedIndex = Math.Max(0, SelectedIndex <= 0 ? 0 : SelectedIndex - 1); e.Handled = true; }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        private void ToggleDropDown()
        {
            if (_DropDown != null && !_DropDown.IsDisposed && _DropDown.Visible)
            {
                CloseDropDown();
                return;
            }

            ShowDropDown();
        }

        private void ShowDropDown()
        {
            if (_Items.Count == 0) return;

            _ListBox = new ListBox();
            _ListBox.BorderStyle = BorderStyle.None;
            _ListBox.DrawMode = DrawMode.OwnerDrawFixed;
            _ListBox.ItemHeight = DropDownItemHeight;
            _ListBox.BackColor = DropDownBackColor;
            _ListBox.ForeColor = DropDownForeColor;
            _ListBox.Font = Font;
            _ListBox.IntegralHeight = false;
            _ListBox.Items.AddRange(_Items.ToArray());
            _ListBox.SelectedIndex = SelectedIndex;
            _ListBox.DrawItem += ListBox_DrawItem;
            _ListBox.MouseClick += ListBox_MouseClick;
            _ListBox.KeyDown += ListBox_KeyDown;

            int visibleCount = Math.Min(_Items.Count, DropDownMaxItems);
            int dropDownHeight = Math.Max(DropDownItemHeight, visibleCount * DropDownItemHeight + 2);
            _ListBox.Size = new Size(Math.Max(Width, 80), dropDownHeight);

            ToolStripControlHost host = new ToolStripControlHost(_ListBox) { AutoSize = false, Margin = Padding.Empty, Padding = Padding.Empty, Size = _ListBox.Size };
            _DropDown = new ToolStripDropDown { AutoSize = false, Padding = new Padding(1), Margin = Padding.Empty, BackColor = BorderColor, Size = new Size(_ListBox.Width + 2, _ListBox.Height + 2) };
            _DropDown.Items.Add(host);
            _DropDown.Closed += DropDown_Closed;
            _DropDown.Show(this, new Point(0, Height + 2));
            _ListBox.Focus();
            Invalidate();
        }

        private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            if (e.Index < 0 || e.Index >= listBox.Items.Count) return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using SolidBrush backBrush = new SolidBrush(selected ? SelectedBackColor : DropDownBackColor);
            e.Graphics.FillRectangle(backBrush, e.Bounds);
            Rectangle textRect = new Rectangle(e.Bounds.Left + HorizontalPadding, e.Bounds.Top, Math.Max(0, e.Bounds.Width - HorizontalPadding * 2), e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, listBox.Items[e.Index]?.ToString() ?? string.Empty, Font, textRect, selected ? SelectedForeColor : DropDownForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private void ListBox_MouseClick(object? sender, MouseEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            int index = listBox.IndexFromPoint(e.Location);
            if (index < 0) return;

            SelectedIndex = index;
            CloseDropDown();
        }

        private void CloseDropDown()
        {
            ToolStripDropDown dropDown = _DropDown;
            if (dropDown == null || dropDown.IsDisposed) return;

            dropDown.Close();
        }

        private void ListBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            if (e.KeyCode == Keys.Enter && listBox.SelectedIndex >= 0) { SelectedIndex = listBox.SelectedIndex; _DropDown?.Close(); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape) { _DropDown?.Close(); e.Handled = true; }
        }

        private void DropDown_Closed(object? sender, ToolStripDropDownClosedEventArgs e)
        {
            ToolStripDropDown? closedDropDown = sender as ToolStripDropDown;
            ListBox? closedListBox = _ListBox;

            if (closedListBox != null)
            {
                closedListBox.DrawItem -= ListBox_DrawItem;
                closedListBox.MouseClick -= ListBox_MouseClick;
                closedListBox.KeyDown -= ListBox_KeyDown;
            }

            if (ReferenceEquals(_DropDown, closedDropDown)) _DropDown = null;
            if (ReferenceEquals(_ListBox, closedListBox)) _ListBox = null;

            if (!IsDisposed && !Disposing)
            {
                Focus();
                Invalidate();
            }

            if (closedDropDown == null) return;

            if (IsHandleCreated && !IsDisposed && !Disposing)
            {
                BeginInvoke(new Action(() =>
                {
                    if (!closedDropDown.IsDisposed) closedDropDown.Dispose();
                    if (closedListBox != null && !closedListBox.IsDisposed) closedListBox.Dispose();
                }));
            }
        }

        private void DrawArrow(Graphics g)
        {
            int centerX = Width - (ArrowAreaWidth / 2);
            int centerY = Height / 2;
            int halfWidth = 4;
            int halfHeight = 2;
            bool opened = _DropDown?.Visible ?? false;

            Point p1 = opened ? new Point(centerX - halfWidth, centerY + halfHeight) : new Point(centerX - halfWidth, centerY - halfHeight);
            Point p2 = opened ? new Point(centerX, centerY - halfHeight) : new Point(centerX, centerY + halfHeight);
            Point p3 = opened ? new Point(centerX + halfWidth, centerY + halfHeight) : new Point(centerX + halfWidth, centerY - halfHeight);

            using Pen pen = new Pen(ArrowColor, 1.7F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(pen, new[] { p1, p2, p3 });
        }

        private sealed class ItemCollection : IList
        {
            private readonly AFMSComboBox _Owner;
            public ItemCollection(AFMSComboBox owner) { _Owner = owner; }
            public int Add(object value) { _Owner._Items.Add(value); _Owner.Invalidate(); return _Owner._Items.Count - 1; }
            public void Clear() { _Owner._Items.Clear(); _Owner.SelectedIndex = -1; _Owner.Invalidate(); }
            public bool Contains(object value) => _Owner._Items.Contains(value);
            public int IndexOf(object value) => _Owner._Items.IndexOf(value);
            public void Insert(int index, object value) { _Owner._Items.Insert(index, value); _Owner.Invalidate(); }
            public bool IsFixedSize => false;
            public bool IsReadOnly => false;
            public void Remove(object value) { int index = _Owner._Items.IndexOf(value); if (index >= 0) RemoveAt(index); }
            public void RemoveAt(int index) { if (index < 0 || index >= _Owner._Items.Count) return; _Owner._Items.RemoveAt(index); if (_Owner.SelectedIndex == index) _Owner.SelectedIndex = -1; else if (_Owner.SelectedIndex > index) _Owner._SelectedIndex--; _Owner.Invalidate(); }
            public object this[int index] { get => _Owner._Items[index]; set { _Owner._Items[index] = value; _Owner.Invalidate(); } }
            public void CopyTo(Array array, int index) => ((ICollection)_Owner._Items).CopyTo(array, index);
            public int Count => _Owner._Items.Count;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public IEnumerator GetEnumerator() => _Owner._Items.GetEnumerator();
        }
    }
}
