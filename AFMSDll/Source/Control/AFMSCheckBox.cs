using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    public class AFMSCheckBox : CheckBox
    {
        private Color _CheckedBorderColor = Color.FromArgb(53, 164, 93);
        private Color _borderColor = Color.FromArgb(214, 220, 226);
        private Color _CheckedBackColor = Color.White;
        private Color _UncheckedBackColor = Color.White;
        private Color _CheckColor = Color.White;
        private Color _CheckBoxColor = Color.FromArgb(38, 151, 76);
        private Color _UncheckedBoxBorderColor = Color.FromArgb(205, 212, 220);
        private Color _TextColor = Color.FromArgb(55, 55, 55);
        private int _borderRadius = 6;
        private float _CheckedBorderThickness = 1F;
        private float _borderThickness = 1F;
        private bool _MouseOver;

        public AFMSCheckBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            AutoSize = false;
            Size = new Size(86, 34);
            BackColor = Color.White;
            ForeColor = _TextColor;
            Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            Cursor = Cursors.Hand;

            UpdateRoundRegion();
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color CheckedBorderColor
        {
            get => _CheckedBorderColor;
            set { _CheckedBorderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color CheckedBackColor
        {
            get => _CheckedBackColor;
            set { _CheckedBackColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color UncheckedBackColor
        {
            get => _UncheckedBackColor;
            set { _UncheckedBackColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color CheckBoxColor
        {
            get => _CheckBoxColor;
            set { _CheckBoxColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color CheckColor
        {
            get => _CheckColor;
            set { _CheckColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color UncheckedBoxBorderColor
        {
            get => _UncheckedBoxBorderColor;
            set { _UncheckedBoxBorderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color TextColor
        {
            get => _TextColor;
            set { _TextColor = value; ForeColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(6)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                UpdateRoundRegion();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1F)]
        public float BorderThickness
        {
            get => _borderThickness;
            set { _borderThickness = Math.Max(0F, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1.5F)]
        public float CheckedBorderThickness
        {
            get => _CheckedBorderThickness;
            set { _CheckedBorderThickness = Math.Max(0F, value); Invalidate(); }
        }


        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            pevent.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            DrawBackground(e.Graphics);
            DrawCheckBox(e.Graphics);
            DrawText(e.Graphics);
        }

        private void DrawBackground(Graphics g)
        {
            float borderThickness = Checked ? CheckedBorderThickness : BorderThickness;
            float offset = borderThickness / 2F;
            RectangleF rect = new RectangleF(offset, offset, Math.Max(0F, Width - borderThickness), Math.Max(0F, Height - borderThickness));
            if (rect.Width <= 0F || rect.Height <= 0F) return;

            Color backColor = Checked ? CheckedBackColor : UncheckedBackColor;
            Color borderColor = Checked ? CheckedBorderColor : BorderColor;

            if (_MouseOver && !Checked) borderColor = Color.FromArgb(185, 195, 205);

            using GraphicsPath path = CreateRoundPath(rect, Math.Max(0F, BorderRadius - offset));
            using SolidBrush brush = new SolidBrush(backColor);
            using Pen pen = new Pen(borderColor, borderThickness) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round };

            g.FillPath(brush, path);
            if (borderThickness > 0F) g.DrawPath(pen, path);
        }

        private void DrawCheckBox(Graphics g)
        {
            const int boxSize = 14;

            float x = 10F;
            float y = (Height - boxSize) / 2F;
            RectangleF rect = new RectangleF(x + 0.5F, y + 0.5F, boxSize - 1F, boxSize - 1F);

            using GraphicsPath path = CreateRoundPath(rect, 3F);

            if (Checked)
            {
                using SolidBrush brush = new SolidBrush(CheckBoxColor);
                g.FillPath(brush, path);

                using Pen checkPen = new Pen(CheckColor, 1.8F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                PointF p1 = new PointF(x + 3.5F, y + 7F);
                PointF p2 = new PointF(x + 6F, y + 9.5F);
                PointF p3 = new PointF(x + 10.5F, y + 4.5F);

                g.DrawLines(checkPen, new[] { p1, p2, p3 });
            }
            else
            {
                using SolidBrush brush = new SolidBrush(Color.White);
                using Pen pen = new Pen(UncheckedBoxBorderColor, 1F);

                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
        }

        private void DrawText(Graphics g)
        {
            const int checkBoxRight = 10 + 14;
            const int textGap = 7;

            Rectangle textRect = new Rectangle(checkBoxRight + textGap, 0, Math.Max(0, Width - checkBoxRight - textGap - 6), Height);
            TextRenderer.DrawText(g, Text, Font, textRect, TextColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private void UpdateRoundRegion()
        {
            if (Width <= 0 || Height <= 0) return;

            using GraphicsPath path = CreateRoundPath(new RectangleF(0, 0, Width, Height), BorderRadius);
            Region oldRegion = Region;
            Region = new Region(path);
            oldRegion?.Dispose();
        }

        private static GraphicsPath CreateRoundPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0F)
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRoundRegion();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRoundRegion();
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _MouseOver = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _MouseOver = false;
            Invalidate();
        }
    }
}
