using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSLoggerMonitors
{
    internal static class TabCommon
    {
        public static readonly Color TextColor = DllColorHelper.HexToColor("#263442");
        public static readonly Color DescriptionColor = DllColorHelper.GetDescStrColor();
        public static readonly Color BorderColor = DllColorHelper.HexToColor("#D8E0E8");
        public static readonly Color ConnectedColor = DllColorHelper.HexToColor("#1B9A62");
        public static readonly Color DisconnectedColor = DllColorHelper.HexToColor("#CB4141");

        public const float TITILE_HIGTH = 30F;
        public const float TITILE_MARGIN = 5F;

        public static Label CreateLabel(string text, float fontSize, FontStyle fontStyle, Color foreColor)
        {
            return new Label
            {
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, fontSize, fontStyle, GraphicsUnit.Point),
                ForeColor = foreColor,
                Margin = Padding.Empty,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        public static Label CreateTitleLabel(string text)
        {
            return CreateLabel(text, 13F, FontStyle.Bold, TextColor);
           
        }

        public static void SetLabelText(Label label, string? value)
        {
            void UpdateText() => label.Text = value ?? string.Empty;

            if (label.InvokeRequired) label.BeginInvoke(UpdateText);
            else UpdateText();
        }
    }
}
