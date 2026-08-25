using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    public class AFMSDataGridViewButtonColumn : DataGridViewColumn
    {
        private string _text = "";
        private Color _backColor = Color.FromArgb(2, 146, 93);
        private Color _hoverBackColor = Color.FromArgb(2, 127, 81);
        private Color _pressedBackColor = Color.FromArgb(2, 109, 70);
        private Color _foreColor = Color.White;
        private Color _borderColor = Color.Transparent;
        private float _borderThickness;
        private int _borderRadius = 4;
        private Padding _buttonMargin = new Padding(8, 7, 8, 7);

        public AFMSDataGridViewButtonColumn() : base(new AFMSDataGridViewButtonCell())
        {
            ReadOnly = true;
            SortMode = DataGridViewColumnSortMode.NotSortable;
            DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Text { get => _text; set { _text = value ?? ""; DataGridView?.InvalidateColumn(Index); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BackColor { get => _backColor; set { _backColor = value; DataGridView?.InvalidateColumn(Index); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HoverBackColor { get => _hoverBackColor; set { _hoverBackColor = value; DataGridView?.InvalidateColumn(Index); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color PressedBackColor { get => _pressedBackColor; set { _pressedBackColor = value; DataGridView?.InvalidateColumn(Index); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color ForeColor { get => _foreColor; set { _foreColor = value; DataGridView?.InvalidateColumn(Index); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BorderColor { get => _borderColor; set { _borderColor = value; DataGridView?.InvalidateColumn(Index); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public float BorderThickness { get => _borderThickness; set { _borderThickness = Math.Max(0F, value); DataGridView?.InvalidateColumn(Index); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int BorderRadius { get => _borderRadius; set { _borderRadius = Math.Max(0, value); DataGridView?.InvalidateColumn(Index); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Padding ButtonMargin { get => _buttonMargin; set { _buttonMargin = value; DataGridView?.InvalidateColumn(Index); } }

        public override object Clone()
        {
            AFMSDataGridViewButtonColumn clone = (AFMSDataGridViewButtonColumn)base.Clone();
            clone._text = _text;
            clone._backColor = _backColor;
            clone._hoverBackColor = _hoverBackColor;
            clone._pressedBackColor = _pressedBackColor;
            clone._foreColor = _foreColor;
            clone._borderColor = _borderColor;
            clone._borderThickness = _borderThickness;
            clone._borderRadius = _borderRadius;
            clone._buttonMargin = _buttonMargin;
            return clone;
        }
    }

    public class AFMSDataGridViewButtonCell : DataGridViewTextBoxCell
    {
        private bool _isHover;
        private bool _isPressed;

        protected override void OnMouseEnter(int rowIndex)
        {
            base.OnMouseEnter(rowIndex);
            _isHover = true;
            DataGridView?.InvalidateCell(ColumnIndex, rowIndex);
            if (DataGridView != null) DataGridView.Cursor = Cursors.Hand;
        }

        protected override void OnMouseLeave(int rowIndex)
        {
            base.OnMouseLeave(rowIndex);
            _isHover = false;
            _isPressed = false;
            DataGridView?.InvalidateCell(ColumnIndex, rowIndex);
            if (DataGridView != null) DataGridView.Cursor = Cursors.Default;
        }

        protected override void OnMouseDown(DataGridViewCellMouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _isPressed = true;
            DataGridView?.InvalidateCell(ColumnIndex, e.RowIndex);
        }

        protected override void OnMouseUp(DataGridViewCellMouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            DataGridView?.InvalidateCell(ColumnIndex, e.RowIndex);
        }

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates elementState,
            object? value, object? formattedValue, string? errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, "", errorText, cellStyle, advancedBorderStyle,
                paintParts & ~DataGridViewPaintParts.ContentForeground);

            if (OwningColumn is not AFMSDataGridViewButtonColumn column) return;

            Rectangle buttonRect = new Rectangle(cellBounds.X + column.ButtonMargin.Left, cellBounds.Y + column.ButtonMargin.Top,
                cellBounds.Width - column.ButtonMargin.Horizontal, cellBounds.Height - column.ButtonMargin.Vertical);
            if (buttonRect.Width <= 0 || buttonRect.Height <= 0) return;

            Color backColor = _isPressed ? column.PressedBackColor : _isHover ? column.HoverBackColor : column.BackColor;
            int radius = Math.Min(column.BorderRadius, Math.Min(buttonRect.Width, buttonRect.Height) / 2);

            SmoothingMode oldSmoothingMode = graphics.SmoothingMode;
            PixelOffsetMode oldPixelOffsetMode = graphics.PixelOffsetMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;

            using GraphicsPath path = CreateRoundedPath(buttonRect, radius);
            using SolidBrush brush = new SolidBrush(backColor);
            graphics.FillPath(brush, path);

            if (column.BorderThickness > 0F && column.BorderColor.A > 0)
            {
                RectangleF borderRect = new RectangleF(buttonRect.X + column.BorderThickness / 2F, buttonRect.Y + column.BorderThickness / 2F,
                    buttonRect.Width - column.BorderThickness, buttonRect.Height - column.BorderThickness);
                using GraphicsPath borderPath = CreateRoundedPath(borderRect, radius);
                using Pen pen = new Pen(column.BorderColor, column.BorderThickness);
                graphics.DrawPath(pen, borderPath);
            }

            TextRenderer.DrawText(graphics, column.Text, cellStyle.Font, buttonRect, column.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            graphics.SmoothingMode = oldSmoothingMode;
            graphics.PixelOffsetMode = oldPixelOffsetMode;
        }

        private static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0F)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float diameter = radius * 2F;
            RectangleF arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
            path.AddArc(arc, 180F, 90F);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270F, 90F);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0F, 90F);
            arc.X = rect.Left;
            path.AddArc(arc, 90F, 90F);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            return CreateRoundedPath(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height), radius);
        }
    }
}
