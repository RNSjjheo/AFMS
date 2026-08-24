using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    public class AFMSButton : Button
    {
        private Color _borderColor = Color.FromArgb(205, 211, 220);
        private Color _hoverBackColor = Color.FromArgb(247, 249, 252);
        private Color _pressedBackColor = Color.FromArgb(238, 242, 247);

        private float _borderThickness = 1F;
        private int _borderRadius = 6;

        private bool _mouseOver;
        private bool _mouseDown;

        public AFMSButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;

            BackColor = Color.White;
            ForeColor = Color.FromArgb(40, 43, 48);
            Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Size = new Size(90, 36);
            Cursor = Cursors.Hand;
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color NormalBackColor
        {
            get => BackColor;
            set
            {
                BackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverBackColor
        {
            get => _hoverBackColor;
            set { _hoverBackColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color PressedBackColor
        {
            get => _pressedBackColor;
            set { _pressedBackColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1F)]
        public float BorderThickness
        {
            get => _borderThickness;
            set { _borderThickness = Math.Max(0F, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(6)]
        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            Color parentBackColor = Parent?.BackColor ?? Color.White;

            using (SolidBrush parentBrush = new SolidBrush(parentBackColor)) g.FillRectangle(parentBrush, ClientRectangle);

            float offset = BorderThickness <= 1F ? 0.5F : BorderThickness / 2F;
            RectangleF rect = new RectangleF(offset, offset, Width - (offset * 2f) - 1f, Height - (offset * 2f) - 1f);

            using GraphicsPath path = CreateRoundPath(rect, BorderRadius);

            Color backColor = GetCurrentBackColor();

            using (SolidBrush backBrush = new SolidBrush(backColor)) g.FillPath(backBrush, path);

            if (BorderThickness > 0F)
            {
                using Pen borderPen = new Pen(BorderColor, BorderThickness);
                borderPen.Alignment = PenAlignment.Center;
                g.DrawPath(borderPen, path);
            }

            DrawContent(g);
        }

        private void DrawContent(Graphics g)
        {
            Color foreColor = Enabled ? ForeColor : SystemColors.GrayText;

            if (Image == null)
            {
                TextRenderer.DrawText(g, Text, Font, ClientRectangle, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                return;
            }

            if (TextImageRelation == TextImageRelation.ImageAboveText)
            {
                Size textSize = TextRenderer.MeasureText(g, Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                const int gap = 3;
                int totalHeight = Image.Height + gap + textSize.Height;
                int startY = (Height - totalHeight) / 2;
                int imageX = (Width - Image.Width) / 2;

                g.DrawImage(Image, imageX, startY, Image.Width, Image.Height);

                Rectangle textRect = new Rectangle(0, startY + Image.Height + gap, Width, textSize.Height);
                TextRenderer.DrawText(g, Text, Font, textRect, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                return;
            }

            int x = (Width - Image.Width) / 2;
            int y = (Height - Image.Height) / 2;

            g.DrawImage(Image, x, y, Image.Width, Image.Height);

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private Color GetCurrentBackColor()
        {
            if (!Enabled) return Color.FromArgb(245, 245, 245);
            if (_mouseDown) return PressedBackColor;
            if (_mouseOver) return HoverBackColor;

            return BackColor;
        }

        private static GraphicsPath CreateRoundPath(RectangleF rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rect.Width <= 0F || rect.Height <= 0F) return path;

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            float d = r * 2f;

            path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _mouseOver = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _mouseOver = false;
            _mouseDown = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);

            if (mevent.Button != MouseButtons.Left) return;

            _mouseDown = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);

            _mouseDown = false;
            Invalidate();
        }
    }
}