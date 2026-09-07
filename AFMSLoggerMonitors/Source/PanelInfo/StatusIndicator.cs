using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text;

namespace AFMSLoggerMonitors
{
    public class StatusIndicator : Control
    {
        private bool active;

        public StatusIndicator(string text, bool active)
        {
            AutoSize = true;
            DoubleBuffered = true;
            Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Bold, GraphicsUnit.Point);
            ForeColor = TabCommon.TextColor;
            BackColor = Color.White;
            MinimumSize = new Size(80, 28);
            Text = text;
            Dock = DockStyle.Fill;
            this.active = active;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Active
        {
            get => active;
            set
            {
                if (active == value) return;
                active = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using SolidBrush statusBrush = new SolidBrush(Active ? TabCommon.ConnectedColor : TabCommon.DisconnectedColor);
            e.Graphics.FillEllipse(statusBrush, 0, (Height - 8) / 2, 8, 8);

            Rectangle textRect = new Rectangle(15, 0, Math.Max(0, Width - 15), Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Size measured = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            Size = new Size(Math.Max(MinimumSize.Width, measured.Width + 16), MinimumSize.Height);
            Invalidate();
        }
    }
}
