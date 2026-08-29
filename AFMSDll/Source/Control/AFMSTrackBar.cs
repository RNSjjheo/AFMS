using System.Collections;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace AFMSDll
{
    public enum AFMSTrackBarSortOrder
    {
        None,
        Ascending,
        Descending
    }

    [ToolboxItem(true)]
    [DefaultEvent(nameof(ValueChanged))]
    [DefaultProperty(nameof(Value))]
    public class AFMSTrackBar : Control
    {
        private sealed record DataItem(object? Value, int OriginalIndex);

        private readonly List<DataItem> _items = new();
        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private int _smallChange = 1;
        private int _largeChange = 10;
        private int _trackHeight = 5;
        private int _handleSize = 18;
        private Color _trackColor = Color.FromArgb(0, 168, 117);
        private Color _handleColor = Color.FromArgb(0, 168, 117);
        private IList? _dataSource;
        private string? _sortMember;
        private AFMSTrackBarSortOrder _sortOrder;
        private IComparer<object?>? _sortComparer;
        private bool _dragging;

        public AFMSTrackBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);

            DoubleBuffered = true;
            TabStop = true;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(200, 24);
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int Minimum
        {
            get => _minimum;
            set
            {
                EnsureNumericMode(nameof(Minimum));
                int newValue = Math.Min(value, int.MaxValue - 1);
                if (_minimum == newValue) return;
                _minimum = newValue;
                if (_maximum < _minimum) _maximum = _minimum;
                SetValue(_value, false);
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(100)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                EnsureNumericMode(nameof(Maximum));
                int newValue = Math.Max(value, _minimum);
                if (_maximum == newValue) return;
                _maximum = newValue;
                SetValue(_value, false);
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int Value
        {
            get => _value;
            set => SetValue(value, false);
        }

        [Category("Behavior")]
        [DefaultValue(1)]
        public int SmallChange
        {
            get => _smallChange;
            set => _smallChange = Math.Max(1, value);
        }

        [Category("Behavior")]
        [DefaultValue(10)]
        public int LargeChange
        {
            get => _largeChange;
            set => _largeChange = Math.Max(1, value);
        }

        [Category("Data")]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList? DataSource
        {
            get => _dataSource;
            set
            {
                object? previousItem = SelectedItem;
                bool hadSelection = SelectedIndex >= 0;
                _dataSource = value;
                RebuildItems(previousItem, hadSelection);
            }
        }

        [Category("Data")]
        [DefaultValue(null)]
        public string? SortMember
        {
            get => _sortMember;
            set
            {
                string? normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                if (string.Equals(_sortMember, normalized, StringComparison.Ordinal)) return;
                _sortMember = normalized;
                RefreshDataSource();
            }
        }

        [Category("Data")]
        [DefaultValue(AFMSTrackBarSortOrder.None)]
        public AFMSTrackBarSortOrder SortOrder
        {
            get => _sortOrder;
            set
            {
                if (_sortOrder == value) return;
                _sortOrder = value;
                RefreshDataSource();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IComparer<object?>? SortComparer
        {
            get => _sortComparer;
            set
            {
                if (ReferenceEquals(_sortComparer, value)) return;
                _sortComparer = value;
                RefreshDataSource();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _dataSource == null || _items.Count == 0 ? -1 : _value;
            set
            {
                if (_dataSource == null)
                    throw new InvalidOperationException("SelectedIndex를 사용하려면 DataSource를 먼저 설정해야 합니다.");
                if (value < 0 || value >= _items.Count)
                    throw new ArgumentOutOfRangeException(nameof(value));
                SetValue(value, false);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? SelectedItem
        {
            get => SelectedIndex >= 0 ? _items[SelectedIndex].Value : null;
            set
            {
                if (_dataSource == null)
                    throw new InvalidOperationException("SelectedItem을 사용하려면 DataSource를 먼저 설정해야 합니다.");

                int index = FindItemIndex(value);
                if (index < 0)
                    throw new ArgumentException("DataSource에서 지정한 항목을 찾을 수 없습니다.", nameof(value));
                SetValue(index, false);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedText => GetItemText(SelectedItem);

        [Category("AFMS Appearance")]
        [DefaultValue(5)]
        public int TrackHeight
        {
            get => _trackHeight;
            set
            {
                int newValue = Math.Max(1, value);
                if (_trackHeight == newValue) return;
                _trackHeight = newValue;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(18)]
        public int HandleSize
        {
            get => _handleSize;
            set
            {
                int newValue = Math.Max(4, value);
                if (_handleSize == newValue) return;
                _handleSize = newValue;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TrackColor
        {
            get => _trackColor;
            set
            {
                if (_trackColor == value) return;
                _trackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HandleColor
        {
            get => _handleColor;
            set
            {
                if (_handleColor == value) return;
                _handleColor = value;
                Invalidate();
            }
        }

        [Category("Action")]
        public event EventHandler? ValueChanged;

        [Category("Action")]
        public event EventHandler? Scroll;

        [Category("Action")]
        public event EventHandler? SelectedItemChanged;

        public string GetItemText(object? item) => item?.ToString() ?? string.Empty;

        public void RefreshDataSource()
        {
            if (_dataSource == null) return;
            object? previousItem = SelectedItem;
            bool hadSelection = SelectedIndex >= 0;
            RebuildItems(previousItem, hadSelection);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AFMSRoundedDrawing.SetHighQuality(e.Graphics);

            RectangleF trackBounds = GetTrackBounds();
            if (trackBounds.Width <= 0F || trackBounds.Height <= 0F) return;

            Color trackColor = Enabled ? TrackColor : ToDisabledColor(TrackColor);
            Color handleColor = Enabled ? HandleColor : ToDisabledColor(HandleColor);

            using GraphicsPath trackPath = AFMSRoundedDrawing.CreatePath(trackBounds, trackBounds.Height / 2F);
            using SolidBrush trackBrush = new(trackColor);
            e.Graphics.FillPath(trackBrush, trackPath);

            float handleCenterX = GetHandleCenterX(trackBounds);
            float handleTop = (ClientSize.Height - HandleSize) / 2F;
            using SolidBrush handleBrush = new(handleColor);
            e.Graphics.FillEllipse(handleBrush, handleCenterX - (HandleSize / 2F),
                handleTop, HandleSize, HandleSize);

            if (Focused && ShowFocusCues)
            {
                Rectangle focusBounds = ClientRectangle;
                focusBounds.Inflate(-1, -1);
                ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled || e.Button != MouseButtons.Left || !HasSelectableValue()) return;

            Focus();
            _dragging = true;
            Capture = true;
            SetValueFromPosition(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging) SetValueFromPosition(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            _dragging = false;
            Capture = false;
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture) _dragging = false;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!Enabled || e.Delta == 0 || !HasSelectableValue()) return;

            SetValue(_value + (e.Delta > 0 ? SmallChange : -SmallChange), true);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            return key is Keys.Left or Keys.Right or Keys.Up or Keys.Down or
                Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!HasSelectableValue()) return;

            int? target = e.KeyCode switch
            {
                Keys.Left or Keys.Down => _value - SmallChange,
                Keys.Right or Keys.Up => _value + SmallChange,
                Keys.PageDown => _value - LargeChange,
                Keys.PageUp => _value + LargeChange,
                Keys.Home => Minimum,
                Keys.End => Maximum,
                _ => null
            };

            if (!target.HasValue) return;
            SetValue(target.Value, true);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void RebuildItems(object? previousItem, bool hadSelection)
        {
            int previousValue = _value;
            _items.Clear();

            if (_dataSource == null)
            {
                _minimum = 0;
                _maximum = 100;
                _value = Math.Clamp(previousValue, _minimum, _maximum);
                Invalidate();
                if (previousValue != _value) ValueChanged?.Invoke(this, EventArgs.Empty);
                if (hadSelection) SelectedItemChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            for (int index = 0; index < _dataSource.Count; index++)
                _items.Add(new DataItem(_dataSource[index], index));

            SortItems();
            _minimum = 0;
            _maximum = Math.Max(0, _items.Count - 1);

            int selectedIndex = hadSelection ? FindItemIndex(previousItem) : -1;
            _value = selectedIndex >= 0 ? selectedIndex : 0;
            Invalidate();

            if (previousValue != _value) ValueChanged?.Invoke(this, EventArgs.Empty);
            SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SortItems()
        {
            if (SortOrder == AFMSTrackBarSortOrder.None ||
                (SortComparer == null && string.IsNullOrEmpty(SortMember))) return;

            int direction = SortOrder == AFMSTrackBarSortOrder.Ascending ? 1 : -1;
            _items.Sort((left, right) =>
            {
                int result = SortComparer != null
                    ? SortComparer.Compare(left.Value, right.Value)
                    : CompareSortMember(left.Value, right.Value);
                return result == 0
                    ? left.OriginalIndex.CompareTo(right.OriginalIndex)
                    : Math.Sign(result) * direction;
            });
        }

        private int CompareSortMember(object? left, object? right)
        {
            object? leftValue = GetSortMemberValue(left);
            object? rightValue = GetSortMemberValue(right);
            if (ReferenceEquals(leftValue, rightValue)) return 0;
            if (leftValue == null) return -1;
            if (rightValue == null) return 1;

            if (leftValue is IComparable comparable)
                return comparable.CompareTo(rightValue);

            return string.Compare(leftValue.ToString(), rightValue.ToString(), StringComparison.CurrentCulture);
        }

        private object? GetSortMemberValue(object? item)
        {
            if (item == null) return null;
            PropertyDescriptor? property = TypeDescriptor.GetProperties(item).Find(SortMember!, true);
            if (property == null)
                throw new InvalidOperationException(
                    $"'{item.GetType().Name}' 형식에서 정렬 속성 '{SortMember}'을(를) 찾을 수 없습니다.");
            return property.GetValue(item);
        }

        private int FindItemIndex(object? item)
        {
            int index = _items.FindIndex(entry => ReferenceEquals(entry.Value, item));
            return index >= 0
                ? index
                : _items.FindIndex(entry => Equals(entry.Value, item));
        }

        private void SetValueFromPosition(int x)
        {
            RectangleF trackBounds = GetTrackBounds();
            if (trackBounds.Width <= 0F) return;

            double ratio = Math.Clamp((x - trackBounds.Left) / trackBounds.Width, 0D, 1D);
            int value = Minimum + (int)Math.Round((Maximum - Minimum) * ratio);
            SetValue(value, true);
        }

        private void SetValue(int value, bool userInitiated)
        {
            int clamped = Math.Clamp(value, Minimum, Maximum);
            if (_value == clamped) return;

            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
            if (_dataSource != null) SelectedItemChanged?.Invoke(this, EventArgs.Empty);
            if (userInitiated) Scroll?.Invoke(this, EventArgs.Empty);
        }

        private RectangleF GetTrackBounds()
        {
            float handleRadius = HandleSize / 2F;
            float left = Padding.Left + handleRadius;
            float right = ClientSize.Width - Padding.Right - handleRadius;
            float height = Math.Min(TrackHeight, Math.Max(1, ClientSize.Height - Padding.Vertical));
            float top = (ClientSize.Height - height) / 2F;
            return new RectangleF(left, top, Math.Max(0F, right - left), height);
        }

        private float GetHandleCenterX(RectangleF trackBounds)
        {
            int range = Maximum - Minimum;
            double ratio = range <= 0 ? 0D : (Value - Minimum) / (double)range;
            return trackBounds.Left + (trackBounds.Width * (float)ratio);
        }

        private bool HasSelectableValue() => _dataSource == null || _items.Count > 0;

        private void EnsureNumericMode(string propertyName)
        {
            if (_dataSource != null)
                throw new InvalidOperationException(
                    $"DataSource가 설정된 동안에는 {propertyName}을(를) 직접 변경할 수 없습니다.");
        }

        private static Color ToDisabledColor(Color color)
        {
            return Color.FromArgb(color.A,
                (color.R + SystemColors.Control.R * 2) / 3,
                (color.G + SystemColors.Control.G * 2) / 3,
                (color.B + SystemColors.Control.B * 2) / 3);
        }
    }
}
