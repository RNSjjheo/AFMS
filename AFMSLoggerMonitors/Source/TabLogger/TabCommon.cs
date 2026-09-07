using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSLoggerMonitors
{
    internal class TabCommon
    {
        public static readonly Color TextColor = DllColorHelper.HexToColor("#263442");
        public static readonly Color DescriptionColor = DllColorHelper.GetDescStrColor();
        public static readonly Color BorderColor = DllColorHelper.HexToColor("#D8E0E8");
        public static readonly Color ConnectedColor = DllColorHelper.HexToColor("#1B9A62");
        public static readonly Color DisconnectedColor = DllColorHelper.HexToColor("#CB4141");
    }
}
