using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public static class DllColorHelper
    {
        public static Color HexToColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
                throw new ArgumentException("색상값이 비어 있습니다.", nameof(hexColor));

            try
            {
                return ColorTranslator.FromHtml(hexColor);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"올바르지 않은 색상값입니다: {hexColor}",nameof(hexColor),ex);
            }
        }

        public static Color GetDescStrColor()
        {
            return HexToColor("#64758E");
        }

        public static Color GetCommonBorder()
        {
            return HexToColor("#E3E9F1");
        }
    }
}
