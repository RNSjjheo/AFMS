using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    public sealed class AFMSBitmap
    {
        private readonly byte[] _svgData;
        private readonly int _width;
        private readonly int _height;

        public AFMSBitmap(byte[] svgData, int width, int height)
        {
            _svgData = svgData;
            _width = width;
            _height = height;
        }

        public Color? ShapeColor { get; set; } = null;

        public float BorderThickness { get; set; } = 0;

        public Color? BorderColor { get; set; } = Color.Transparent;

        public Bitmap ToBitmap()
        {
            return AFMSSvgHelper.ToBitmap(_svgData, _width, _height, ShapeColor, BorderThickness, BorderColor);
        }

        public static implicit operator Bitmap(AFMSBitmap value) => value?.ToBitmap();

        public static implicit operator Image(AFMSBitmap value) => value?.ToBitmap();
    }
}
