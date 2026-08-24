using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AFMSDll
{
    public enum MaximizableTableLayoutType
    {
        Layout2_2 = 1,
        Layout2_1 = 2,
        Layout1_2 = 3,
        Layout1_1 = 4
    }

    [ToolboxItem(true)]
    public class MaximizableTableLayoutPanel : TableLayoutPanel
    {
        private Control? _maximizedControl;
        private readonly Dictionary<Control, LayoutSnapshot> _snapshots = new();

        public MaximizableTableLayoutPanel()
        {
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            SetGridSize(2, 2);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsMaximized => _maximizedControl != null;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control? MaximizedControl => _maximizedControl;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MaximizableTableLayoutType LayoutType { get; private set; } = MaximizableTableLayoutType.Layout2_2;

        public void SetLayout(MaximizableTableLayoutType layoutType, params Control[] controls)
        {
            ArgumentNullException.ThrowIfNull(controls);

            int requiredCount = GetRequiredControlCount(layoutType);
            if (controls.Length < requiredCount) throw new ArgumentException($"{layoutType} 레이아웃에는 최소 {requiredCount}개의 컨트롤이 필요합니다.", nameof(controls));

            for (int i = 0; i < requiredCount; i++)
            {
                if (controls[i] == null) throw new ArgumentException($"controls[{i}]가 null입니다.", nameof(controls));

                for (int j = 0; j < i; j++)
                {
                    if (ReferenceEquals(controls[i], controls[j])) throw new ArgumentException("동일한 컨트롤을 두 번 이상 배치할 수 없습니다.", nameof(controls));
                }
            }

            if (IsMaximized) Restore();

            SuspendLayout();

            try
            {
                Controls.Clear();
                const int s = 5;

                switch (layoutType)
                {
                    case MaximizableTableLayoutType.Layout2_2:
                        ConfigureGrid(2, 2);
                        AddLayoutControl(controls[0], 0, 0);
                        AddLayoutControl(controls[1], 1, 0);
                        AddLayoutControl(controls[2], 0, 1);
                        AddLayoutControl(controls[3], 1, 1);

                        controls[0].Margin = new Padding(0, 0, s, s);
                        controls[1].Margin = new Padding(s, 0, 0, s);
                        controls[2].Margin = new Padding(0, s, s, 0);
                        controls[3].Margin = new Padding(s, s, 0, 0);

                        break;

                    case MaximizableTableLayoutType.Layout2_1:
                        ConfigureGrid(2, 2);
                        AddLayoutControl(controls[0], 0, 0);
                        AddLayoutControl(controls[1], 1, 0);
                        AddLayoutControl(controls[2], 0, 1, 2, 1);

                        controls[0].Margin = new Padding(0, 0, s, s);
                        controls[1].Margin = new Padding(s, 0, 0, s);
                        controls[2].Margin = new Padding(0, s, 0, 0);

                        break;

                    case MaximizableTableLayoutType.Layout1_2:
                        ConfigureGrid(2, 2);
                        AddLayoutControl(controls[0], 0, 0, 2, 1);
                        AddLayoutControl(controls[1], 0, 1);
                        AddLayoutControl(controls[2], 1, 1);

                        controls[0].Margin = new Padding(0, 0, 0, s);
                        controls[1].Margin = new Padding(0, s, s, 0);
                        controls[2].Margin = new Padding(s, s, 0, 0);

                        break;

                    case MaximizableTableLayoutType.Layout1_1:
                        ConfigureGrid(1, 2);
                        AddLayoutControl(controls[0], 0, 0);
                        AddLayoutControl(controls[1], 0, 1);
                        controls[0].Margin = new Padding(0, 0, 0, s);
                        controls[1].Margin = new Padding(0, s, 0, 0);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(layoutType));
                }

                LayoutType = layoutType;
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        public void SetGridSize(int columnCount, int rowCount)
        {
            if (columnCount < 1) throw new ArgumentOutOfRangeException(nameof(columnCount), "열 개수는 1 이상이어야 합니다.");
            if (rowCount < 1) throw new ArgumentOutOfRangeException(nameof(rowCount), "행 개수는 1 이상이어야 합니다.");
            if (IsMaximized) Restore();

            ValidateExistingControlsFit(columnCount, rowCount);

            SuspendLayout();

            try
            {
                ConfigureGrid(columnCount, rowCount);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        public void AddControl(Control control, int column, int row, int columnSpan = 1, int rowSpan = 1)
        {
            ArgumentNullException.ThrowIfNull(control);
            if (IsMaximized) Restore();
            if (Controls.Contains(control)) throw new InvalidOperationException("이미 추가된 컨트롤입니다.");

            ValidatePlacement(null, column, row, columnSpan, rowSpan);

            SuspendLayout();

            try
            {
                control.Dock = DockStyle.Fill;
                Controls.Add(control, column, row);
                SetColumnSpan(control, columnSpan);
                SetRowSpan(control, rowSpan);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        public void SetControlLayout(Control control, int column, int row, int columnSpan = 1, int rowSpan = 1)
        {
            ArgumentNullException.ThrowIfNull(control);
            if (!Controls.Contains(control)) throw new ArgumentException("현재 TableLayoutPanel의 자식 컨트롤이 아닙니다.", nameof(control));
            if (IsMaximized) Restore();

            ValidatePlacement(control, column, row, columnSpan, rowSpan);

            SuspendLayout();

            try
            {
                SetCellPosition(control, new TableLayoutPanelCellPosition(column, row));
                SetColumnSpan(control, columnSpan);
                SetRowSpan(control, rowSpan);
                control.Dock = DockStyle.Fill;
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        public void SetControlSpan(Control control, int columnSpan, int rowSpan)
        {
            ArgumentNullException.ThrowIfNull(control);
            if (!Controls.Contains(control)) throw new ArgumentException("현재 TableLayoutPanel의 자식 컨트롤이 아닙니다.", nameof(control));

            TableLayoutPanelCellPosition position = GetCellPosition(control);
            SetControlLayout(control, position.Column, position.Row, columnSpan, rowSpan);
        }

        public void ToggleMaximize(Control target)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (ReferenceEquals(_maximizedControl, target))
            {
                Restore();
                return;
            }

            if (IsMaximized) Restore();
            Maximize(target);
        }

        public void Maximize(Control target)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (!Controls.Contains(target)) throw new ArgumentException("대상 컨트롤이 현재 TableLayoutPanel에 없습니다.", nameof(target));
            if (IsMaximized) Restore();

            SuspendLayout();

            try
            {
                _snapshots.Clear();

                foreach (Control control in Controls)
                {
                    _snapshots[control] = new LayoutSnapshot
                    {
                        Position = GetCellPosition(control),
                        ColumnSpan = GetColumnSpan(control),
                        RowSpan = GetRowSpan(control),
                        Visible = control.Visible,
                        Dock = control.Dock,
                        Margin = control.Margin
                    };

                    control.Visible = ReferenceEquals(control, target);
                }

                SetCellPosition(target, new TableLayoutPanelCellPosition(0, 0));
                SetColumnSpan(target, ColumnCount);
                SetRowSpan(target, RowCount);
                target.Dock = DockStyle.Fill;
                target.Margin = Padding.Empty;
                target.Visible = true;
                target.BringToFront();
                _maximizedControl = target;
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        public void Restore()
        {
            if (_maximizedControl == null) return;

            SuspendLayout();

            try
            {
                foreach (Control control in _snapshots.Keys)
                {
                    if (!control.IsDisposed && Controls.Contains(control)) control.Visible = false;
                }

                foreach (KeyValuePair<Control, LayoutSnapshot> pair in _snapshots)
                {
                    Control control = pair.Key;
                    LayoutSnapshot snapshot = pair.Value;
                    if (control.IsDisposed || !Controls.Contains(control)) continue;

                    SetCellPosition(control, snapshot.Position);
                    SetColumnSpan(control, snapshot.ColumnSpan);
                    SetRowSpan(control, snapshot.RowSpan);
                    control.Dock = snapshot.Dock;
                    control.Margin = snapshot.Margin;
                }

                foreach (KeyValuePair<Control, LayoutSnapshot> pair in _snapshots)
                {
                    Control control = pair.Key;
                    if (control.IsDisposed || !Controls.Contains(control)) continue;
                    control.Visible = pair.Value.Visible;
                }

                _snapshots.Clear();
                _maximizedControl = null;
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private static int GetRequiredControlCount(MaximizableTableLayoutType layoutType)
        {
            return layoutType switch
            {
                MaximizableTableLayoutType.Layout2_2 => 4,
                MaximizableTableLayoutType.Layout2_1 => 3,
                MaximizableTableLayoutType.Layout1_2 => 3,
                MaximizableTableLayoutType.Layout1_1 => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(layoutType))
            };
        }

        private void ConfigureGrid(int columnCount, int rowCount)
        {
            ColumnCount = columnCount;
            RowCount = rowCount;
            ColumnStyles.Clear();
            RowStyles.Clear();

            float columnPercent = 100F / columnCount;
            float rowPercent = 100F / rowCount;

            for (int column = 0; column < columnCount; column++) ColumnStyles.Add(new ColumnStyle(SizeType.Percent, columnPercent));
            for (int row = 0; row < rowCount; row++) RowStyles.Add(new RowStyle(SizeType.Percent, rowPercent));
        }

        private void AddLayoutControl(Control control, int column, int row, int columnSpan = 1, int rowSpan = 1)
        {
            control.Dock = DockStyle.Fill;
            control.Visible = true;
            Controls.Add(control, column, row);
            SetColumnSpan(control, columnSpan);
            SetRowSpan(control, rowSpan);
        }

        private void ValidatePlacement(Control? controlToIgnore, int column, int row, int columnSpan, int rowSpan)
        {
            if (column < 0 || column >= ColumnCount) throw new ArgumentOutOfRangeException(nameof(column));
            if (row < 0 || row >= RowCount) throw new ArgumentOutOfRangeException(nameof(row));
            if (columnSpan < 1) throw new ArgumentOutOfRangeException(nameof(columnSpan));
            if (rowSpan < 1) throw new ArgumentOutOfRangeException(nameof(rowSpan));
            if (column + columnSpan > ColumnCount) throw new ArgumentException("ColumnSpan이 전체 열 범위를 초과합니다.");
            if (row + rowSpan > RowCount) throw new ArgumentException("RowSpan이 전체 행 범위를 초과합니다.");

            foreach (Control existingControl in Controls)
            {
                if (ReferenceEquals(existingControl, controlToIgnore)) continue;

                TableLayoutPanelCellPosition position = GetCellPosition(existingControl);
                if (position.Column < 0 || position.Row < 0) continue;

                int existingColumnSpan = GetColumnSpan(existingControl);
                int existingRowSpan = GetRowSpan(existingControl);
                bool overlaps = column < position.Column + existingColumnSpan && column + columnSpan > position.Column && row < position.Row + existingRowSpan && row + rowSpan > position.Row;

                if (overlaps) throw new InvalidOperationException($"배치 영역이 '{existingControl.Name}' 컨트롤과 겹칩니다.");
            }
        }

        private void ValidateExistingControlsFit(int newColumnCount, int newRowCount)
        {
            foreach (Control control in Controls)
            {
                TableLayoutPanelCellPosition position = GetCellPosition(control);
                if (position.Column < 0 || position.Row < 0) continue;

                int columnSpan = GetColumnSpan(control);
                int rowSpan = GetRowSpan(control);
                bool exceedsColumns = position.Column + columnSpan > newColumnCount;
                bool exceedsRows = position.Row + rowSpan > newRowCount;

                if (exceedsColumns || exceedsRows) throw new InvalidOperationException($"'{control.Name}' 컨트롤의 위치 또는 Span이 변경하려는 그리드 범위를 초과합니다.");
            }
        }

        private sealed class LayoutSnapshot
        {
            public TableLayoutPanelCellPosition Position { get; init; }
            public int ColumnSpan { get; init; } = 1;
            public int RowSpan { get; init; } = 1;
            public bool Visible { get; init; }
            public DockStyle Dock { get; init; }
            public Padding Margin { get; init; }
        }
    }
}
