using System.Drawing;
using System.Runtime.InteropServices;

namespace AFMSDll
{
    public enum AFMSIcons
    {
        ArrowDown,
        ArrowLeft,
        ArrowRight,
        Calendar,
        Export,
        FlowRate,
        FlowRateSelect,
        FlowVelocity,
        FlowVelocitySelect,
        Layout11Off,
        Layout11On,
        Layout12Off,
        Layout12On,
        Layout21Off,
        Layout21On,
        Layout22Off,
        Layout22On,
        MeasureOff,
        MeasureOn,
        SearchOff,
        SearchOn,
        Setting,
        TitlebarClose,
        TitlebarCloseW,
        TitlebarMax,
        TitlebarMaxW,
        TitlebarMinW,
        Vth,
        VthSelect,
        WaterLevel,
        WaterLevelSelect,
        ToggleOff,
        ToggleOffW,
        ToggleOn,
        ToggleOnW
    }

    public static class AFMSIcon
    {
        public static Bitmap Get(AFMSIcons iconType, int size) => Get(iconType, size, size);

        /// <summary>SVG 최상위 배경 사각형의 모서리 반경을 최종 Bitmap 픽셀 단위로 지정합니다.</summary>
        public static Bitmap Get(AFMSIcons iconType, int size, float cornerRadius) =>
            Get(iconType, size, size, cornerRadius);

        public static Bitmap Get(AFMSIcons iconType, int width, int height)
        {
            return Get(iconType, width, height, null);
        }

        /// <summary>SVG 최상위 배경 사각형의 모서리 반경을 최종 Bitmap 픽셀 단위로 지정합니다.</summary>
        public static Bitmap Get(AFMSIcons iconType, int width, int height, float? cornerRadius)
        {
            string resource;

            switch (iconType)
            {
                case AFMSIcons.ArrowDown:
                    resource = Res.icon_arrow_down;
                    break;
                case AFMSIcons.ArrowLeft:
                    resource = Res.icon_arrow_left;
                    break;
                case AFMSIcons.ArrowRight:
                    resource = Res.icon_arrow_right;
                    break;
                case AFMSIcons.Calendar:
                    resource = Res.icon_calender;
                    break;
                case AFMSIcons.Export:
                    resource = Res.icon_export;
                    break;
                case AFMSIcons.FlowRate:
                    resource = Res.icon_flowRate;
                    break;
                case AFMSIcons.FlowRateSelect:
                    resource = Res.icon_flowRate_select;
                    break;
                case AFMSIcons.FlowVelocity:
                    resource = Res.icon_flowVelocity;
                    break;
                case AFMSIcons.FlowVelocitySelect:
                    resource = Res.icon_flowVelocity_select;
                    break;
                case AFMSIcons.Layout11Off:
                    resource = Res.icon_layout11_off;
                    break;
                case AFMSIcons.Layout11On:
                    resource = Res.icon_layout11_on;
                    break;
                case AFMSIcons.Layout12Off:
                    resource = Res.icon_layout12_off;
                    break;
                case AFMSIcons.Layout12On:
                    resource = Res.icon_layout12_on;
                    break;
                case AFMSIcons.Layout21Off:
                    resource = Res.icon_layout21_off;
                    break;
                case AFMSIcons.Layout21On:
                    resource = Res.icon_layout21_on;
                    break;
                case AFMSIcons.Layout22Off:
                    resource = Res.icon_layout22_off;
                    break;
                case AFMSIcons.Layout22On:
                    resource = Res.icon_layout22_on;
                    break;
                case AFMSIcons.MeasureOff:
                    resource = Res.icon_measure_off;
                    break;
                case AFMSIcons.MeasureOn:
                    resource = Res.icon_measure_on;
                    break;
                case AFMSIcons.SearchOff:
                    resource = Res.icon_search_off;
                    break;
                case AFMSIcons.SearchOn:
                    resource = Res.icon_search_on;
                    break;
                case AFMSIcons.Setting:
                    resource = Res.icon_setting;
                    break;
                case AFMSIcons.TitlebarClose:
                    resource = Res.icon_titlebar_close;
                    break;
                case AFMSIcons.TitlebarCloseW:
                    resource = Res.icon_titlebar_close_w;
                    break;
                case AFMSIcons.TitlebarMax:
                    resource = Res.icon_titlebar_max;
                    break;
                case AFMSIcons.TitlebarMaxW:
                    resource = Res.icon_titlebar_max_w;
                    break;
                case AFMSIcons.TitlebarMinW:
                    resource = Res.icon_titlebar_min_w;
                    break;
                case AFMSIcons.Vth:
                    resource = Res.icon_vth;
                    break;
                case AFMSIcons.VthSelect:
                    resource = Res.icon_vth_select;
                    break;
                case AFMSIcons.WaterLevel:
                    resource = Res.icon_waterLevel;
                    break;
                case AFMSIcons.WaterLevelSelect:
                    resource = Res.icon_waterLevel_select;
                    break;
                case AFMSIcons.ToggleOff:
                    resource = Res.toggle_off;
                    break;
                case AFMSIcons.ToggleOffW:
                    resource = Res.toggle_off_w;
                    break;
                case AFMSIcons.ToggleOn:
                    resource = Res.toggle_on;
                    break;
                case AFMSIcons.ToggleOnW:
                    resource = Res.toggle_on_w;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(iconType), iconType, null);
            }

            return AFMSSvgHelper.ToBitmap(resource, width, height, cornerRadius: cornerRadius);
        }

        public static Icon GetIcon(AFMSIcons iconType, int size)
        {
            using Bitmap bitmap = Get(iconType, size);
            return ToIcon(bitmap);
        }

        private static Icon ToIcon(Bitmap bitmap)
        {
            IntPtr iconHandle = bitmap.GetHicon();
            try
            {
                using Icon temporaryIcon = Icon.FromHandle(iconHandle);
                return (Icon)temporaryIcon.Clone();
            }
            finally
            {
                DestroyIcon(iconHandle);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr iconHandle);
    }
}
