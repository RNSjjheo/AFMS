using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    public static class AFMSRoundedDrawing
    {
        public static void SetHighQuality(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
        }

        public static GraphicsPath CreatePath(RectangleF rectangle, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rectangle.Width <= 0F || rectangle.Height <= 0F) return path;

            float actualRadius = Math.Min(Math.Max(0F, radius), Math.Min(rectangle.Width, rectangle.Height) / 2F);
            if (actualRadius <= 0F)
            {
                path.AddRectangle(rectangle);
                path.CloseFigure();
                return path;
            }

            float diameter = actualRadius * 2F;
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180F, 90F);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270F, 90F);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0F, 90F);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90F, 90F);
            path.CloseFigure();
            return path;
        }

        public static RectangleF GetBorderRectangle(Size clientSize, float borderThickness)
        {
            float offset = borderThickness <= 1F ? 0.5F : borderThickness / 2F;
            return new RectangleF(offset, offset, Math.Max(0F, clientSize.Width - (offset * 2F)), Math.Max(0F, clientSize.Height - (offset * 2F)));
        }

        public static void ApplyRegion(Control control, float radius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;

            using GraphicsPath path = CreatePath(new RectangleF(0F, 0F, control.Width, control.Height), radius);
            Region? oldRegion = control.Region;
            control.Region = new Region(path);
            oldRegion?.Dispose();
        }
    }
}
