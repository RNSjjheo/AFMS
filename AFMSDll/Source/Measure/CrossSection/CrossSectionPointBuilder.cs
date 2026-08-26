using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public static class CrossSectionPointBuilder
    {
        public static CrossSectionPointCollection Build(IEnumerable<CrossSectionPoint> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            CrossSectionPointCollection result = new();
            result.AddRange(source);
            return result;
        }

        public static CrossSectionPointCollection Build<T>(IEnumerable<T> source, Func<T, CrossSectionPoint> converter)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(converter);

            CrossSectionPointCollection result = new();

            foreach (T item in source)
            {
                CrossSectionPoint point = converter(item) ?? throw new InvalidOperationException("CrossSectionPoint 변환 결과가 null입니다.");
                result.Add(point);
            }

            return result;
        }
    }
}
