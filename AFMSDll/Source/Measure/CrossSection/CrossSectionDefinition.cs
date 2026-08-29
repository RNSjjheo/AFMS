using System;
using System.Collections.Generic;

namespace AFMSDll
{
    public sealed class CrossSectionDefinition
    {
        private readonly (double LeftBankDistance, double Elevation)[] _points;

        public CrossSectionDefinition(int id, string description, double zeroPointElevation, IEnumerable<CrossSectionPoint> points)
        {
            ArgumentNullException.ThrowIfNull(points);

            if (!double.IsFinite(zeroPointElevation))
                throw new ArgumentOutOfRangeException(nameof(zeroPointElevation), "영점 표고는 유한한 값이어야 합니다.");

            List<(double LeftBankDistance, double Elevation)> pointValues = [];
            foreach (CrossSectionPoint point in points)
            {
                ArgumentNullException.ThrowIfNull(point);

                if (!double.IsFinite(point.LeftBankDistance) || !double.IsFinite(point.Elevation))
                {
                    throw new ArgumentException("단면 좌표는 유한한 값이어야 합니다.", nameof(points));
                }

                pointValues.Add((point.LeftBankDistance, point.Elevation));
            }

            Id = id;
            Description = description?.Trim() ?? string.Empty;
            ZeroPointElevation = zeroPointElevation;
            _points = pointValues.ToArray();
        }

        public int Id { get; }

        public string Description { get; }

        public double ZeroPointElevation { get; }

        public int PointCount => _points.Length;

        public CrossSectionPointCollection CreatePointCollection()
        {
            return CrossSectionPointBuilder.Build(_points, point => new CrossSectionPoint(point.LeftBankDistance, point.Elevation));
        }
    }
}
