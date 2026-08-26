using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    public class AFMSCheckBox : _AFMSCheckBoxBase
    {
        private Color _CheckedBorderColor = Color.FromArgb(53, 164, 93);
        private Color _CheckedBackColor = Color.White;
        private Color _UncheckedBackColor = Color.White;
        private Color _CheckColor = Color.White;
        private Color _CheckBoxColor = Color.FromArgb(38, 151, 76);
        private Color _UncheckedBoxBorderColor = Color.FromArgb(205, 212, 220);
        private Color _TextColor = Color.FromArgb(55, 55, 55);
        private float _CheckedBorderThickness = 1F;
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
            AFMSRoundedDrawing.SetHighQuality(e.Graphics);
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

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

            using GraphicsPath path = AFMSRoundedDrawing.CreatePath(rect, Math.Max(0F, BorderRadius - offset));
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

            using GraphicsPath path = AFMSRoundedDrawing.CreatePath(rect, 3F);

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

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            if (!IsDisposed && !Disposing) Invalidate();
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
