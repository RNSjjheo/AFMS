using Svg;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AFMSDll
{
    public static class AFMSSvgHelper
    {
        /// <summary>
        /// Resource의 SVG(byte[])를 Bitmap으로 변환합니다.
        /// 배경은 투명으로 처리하며, shapeColor는 SVG 도형 자체의 색상입니다.
        /// borderThickness와 borderColor는 Bitmap 외곽선이 아니라 SVG 도형의 외곽선입니다.
        /// </summary>
        public static Bitmap ToBitmap(byte[] svgData, int width, int height, Color? shapeColor = null, float borderThickness = 0, Color? borderColor = null, float? cornerRadius = null)
        {
            return ToBitmap(svgData, width, height, shapeColor, Color.Transparent, borderThickness, borderColor, cornerRadius);
        }

        /// <summary>
        /// Resource의 SVG(byte[])를 Bitmap으로 변환합니다.
        /// 도형 색상, 배경 색상, 도형 외곽선 굵기/색상을 각각 지정할 수 있습니다.
        /// borderThickness는 최종 Bitmap 기준 픽셀 단위입니다.
        /// </summary>
        public static Bitmap ToBitmap(byte[] svgData, int width, int height, Color? shapeColor, Color? backgroundColor, float borderThickness, Color? borderColor, float? cornerRadius = null)
        {
            if (svgData == null || svgData.Length == 0)
                throw new ArgumentException("SVG 데이터가 없습니다.", nameof(svgData));

            ValidateSize(width, height, borderThickness, cornerRadius);

            using MemoryStream stream = new MemoryStream(svgData);
            XDocument document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            ApplySvgStyle(document, width, height, shapeColor, borderThickness, borderColor, cornerRadius);

            SvgDocument svgDocument = SvgDocument.FromSvg<SvgDocument>(document.ToString(SaveOptions.DisableFormatting));
            return CreateBitmap(svgDocument, width, height, backgroundColor);
        }

        /// <summary>
        /// SVG 문자열을 Bitmap으로 변환합니다.
        /// 배경은 투명으로 처리하며, shapeColor는 SVG 도형 자체의 색상입니다.
        /// borderThickness와 borderColor는 Bitmap 외곽선이 아니라 SVG 도형의 외곽선입니다.
        /// </summary>
        public static Bitmap ToBitmap(string svgText, int width, int height, Color? shapeColor = null, float borderThickness = 0, Color? borderColor = null, float? cornerRadius = null)
        {
            return ToBitmap(svgText, width, height, shapeColor, Color.Transparent, borderThickness, borderColor, cornerRadius);
        }

        /// <summary>
        /// SVG 문자열을 Bitmap으로 변환합니다.
        /// 도형 색상, 배경 색상, 도형 외곽선 굵기/색상을 각각 지정할 수 있습니다.
        /// borderThickness는 최종 Bitmap 기준 픽셀 단위입니다.
        /// </summary>
        public static Bitmap ToBitmap(string svgText, int width, int height, Color? shapeColor, Color? backgroundColor, float borderThickness, Color? borderColor, float? cornerRadius = null)
        {
            if (string.IsNullOrWhiteSpace(svgText))
                throw new ArgumentException("SVG 데이터가 없습니다.", nameof(svgText));

            ValidateSize(width, height, borderThickness, cornerRadius);

            XDocument document = XDocument.Parse(svgText, LoadOptions.PreserveWhitespace);
            ApplySvgStyle(document, width, height, shapeColor, borderThickness, borderColor, cornerRadius);

            SvgDocument svgDocument = SvgDocument.FromSvg<SvgDocument>(document.ToString(SaveOptions.DisableFormatting));
            return CreateBitmap(svgDocument, width, height, backgroundColor);
        }

        private static void ValidateSize(int width, int height, float borderThickness, float? cornerRadius)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            if (borderThickness < 0)
                throw new ArgumentOutOfRangeException(nameof(borderThickness));

            if (cornerRadius < 0)
                throw new ArgumentOutOfRangeException(nameof(cornerRadius));
        }

        private static Bitmap CreateBitmap(SvgDocument svgDocument, int width, int height, Color? backgroundColor)
        {
            Bitmap result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using Graphics g = Graphics.FromImage(result);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            g.Clear(backgroundColor ?? Color.Transparent);

            using Bitmap svgBitmap = svgDocument.Draw(width, height);
            g.DrawImage(svgBitmap, new Rectangle(0, 0, width, height));

            return result;
        }

        private static void ApplySvgStyle(XDocument document, int bitmapWidth, int bitmapHeight, Color? shapeColor, float borderThickness, Color? borderColor, float? cornerRadius)
        {
            XElement? root = document.Root;

            if (root == null)
                throw new InvalidDataException("유효한 SVG 문서가 아닙니다.");

            if (cornerRadius.HasValue)
            {
                XElement? backgroundRect = root.Elements()
                    .FirstOrDefault(element => element.Name.LocalName.Equals("rect", StringComparison.OrdinalIgnoreCase));
                if (backgroundRect != null)
                {
                    float svgRadius = ConvertPixelToSvgUnit(root, bitmapWidth, bitmapHeight, cornerRadius.Value);
                    string radiusText = svgRadius.ToString("0.###", CultureInfo.InvariantCulture);
                    backgroundRect.SetAttributeValue("rx", radiusText);
                    backgroundRect.SetAttributeValue("ry", radiusText);
                }
            }

            string? shapeColorText = shapeColor.HasValue ? ToSvgColor(shapeColor.Value) : null;
            string borderColorText = ToSvgColor(borderColor ?? shapeColor ?? Color.Black);
            float svgBorderThickness = borderThickness > 0 ? ConvertPixelToSvgUnit(root, bitmapWidth, bitmapHeight, borderThickness) : 0;

            foreach (XElement element in root.DescendantsAndSelf().Where(IsDrawableElement))
            {
                string elementName = element.Name.LocalName;
                string? fill = GetEffectiveStyleValue(element, "fill");
                string? stroke = GetEffectiveStyleValue(element, "stroke");

                bool fillIsNone = string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase);
                bool strokeIsNone = string.Equals(stroke, "none", StringComparison.OrdinalIgnoreCase);
                bool isStrokeOnlyElement = elementName.Equals("line", StringComparison.OrdinalIgnoreCase) || elementName.Equals("polyline", StringComparison.OrdinalIgnoreCase);

                if (shapeColorText != null)
                {
                    if (!fillIsNone && !isStrokeOnlyElement)
                        SetStyleValue(element, "fill", shapeColorText);

                    if (borderThickness <= 0 && !strokeIsNone && stroke != null)
                        SetStyleValue(element, "stroke", shapeColorText);

                    if (fill == null && stroke == null && !isStrokeOnlyElement)
                        SetStyleValue(element, "fill", shapeColorText);

                    if (isStrokeOnlyElement && borderThickness <= 0)
                        SetStyleValue(element, "stroke", shapeColorText);
                }

                if (borderThickness > 0)
                {
                    SetStyleValue(element, "stroke", borderColorText);
                    SetStyleValue(element, "stroke-width", svgBorderThickness.ToString("0.###", CultureInfo.InvariantCulture));
                    SetStyleValue(element, "stroke-linejoin", "round");
                    SetStyleValue(element, "stroke-linecap", "round");
                }
            }
        }

        private static bool IsDrawableElement(XElement element)
        {
            switch (element.Name.LocalName.ToLowerInvariant())
            {
                case "path":
                case "rect":
                case "circle":
                case "ellipse":
                case "polygon":
                case "polyline":
                case "line":
                    return true;

                default:
                    return false;
            }
        }

        private static string? GetEffectiveStyleValue(XElement element, string propertyName)
        {
            string? styleValue = GetStyleValue(element, propertyName);

            if (!string.IsNullOrWhiteSpace(styleValue))
                return styleValue;

            return element.Attribute(propertyName)?.Value;
        }

        private static string? GetStyleValue(XElement element, string propertyName)
        {
            string? style = element.Attribute("style")?.Value;

            if (string.IsNullOrWhiteSpace(style))
                return null;

            foreach (string item in style.Split(';'))
            {
                int separatorIndex = item.IndexOf(':');

                if (separatorIndex <= 0)
                    continue;

                string name = item.Substring(0, separatorIndex).Trim();

                if (!name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                return item.Substring(separatorIndex + 1).Trim();
            }

            return null;
        }

        private static void SetStyleValue(XElement element, string propertyName, string value)
        {
            element.SetAttributeValue(propertyName, value);

            string? style = element.Attribute("style")?.Value;
            string[] items = string.IsNullOrWhiteSpace(style) ? Array.Empty<string>() : style.Split(';');
            bool replaced = false;

            for (int i = 0; i < items.Length; i++)
            {
                int separatorIndex = items[i].IndexOf(':');

                if (separatorIndex <= 0)
                    continue;

                string name = items[i].Substring(0, separatorIndex).Trim();

                if (!name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                items[i] = propertyName + ":" + value;
                replaced = true;
            }

            string newStyle = string.Join(";", items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

            if (!replaced)
            {
                if (!string.IsNullOrWhiteSpace(newStyle))
                    newStyle += ";";

                newStyle += propertyName + ":" + value;
            }

            element.SetAttributeValue("style", newStyle);
        }

        private static float ConvertPixelToSvgUnit(XElement root, int bitmapWidth, int bitmapHeight, float pixelThickness)
        {
            XAttribute? viewBoxAttribute = root.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals("viewBox", StringComparison.OrdinalIgnoreCase));

            if (viewBoxAttribute == null)
                return pixelThickness;

            string[] parts = viewBoxAttribute.Value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 4)
                return pixelThickness;

            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float viewBoxWidth) || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float viewBoxHeight))
                return pixelThickness;

            if (viewBoxWidth <= 0 || viewBoxHeight <= 0)
                return pixelThickness;

            float unitPerPixelX = viewBoxWidth / bitmapWidth;
            float unitPerPixelY = viewBoxHeight / bitmapHeight;

            return pixelThickness * Math.Max(unitPerPixelX, unitPerPixelY);
        }

        private static string ToSvgColor(Color color)
        {
            if (color.A == 0)
                return "none";

            if (color.A == 255)
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            double alpha = color.A / 255.0;
            return string.Format(CultureInfo.InvariantCulture, "rgba({0},{1},{2},{3:0.###})", color.R, color.G, color.B, alpha);
        }
    }
}
