using System.Collections.Generic;
using System.Linq;

namespace AFMSDll
{
    public class CrossSectionPointCollection : List<CrossSectionPoint>
    {
        public double? WaterLevel { get; set; }

        public double Area => GetCrossSectionArea();

        public void ClearWaterLevel()
        {
            WaterLevel = null;
        }

        private double GetCrossSectionArea()
        {
            if (!WaterLevel.HasValue || Count < 2) return 0.0;

            double waterLevel = WaterLevel.Value;
            double area = 0.0;
            List<CrossSectionPoint> points = this.OrderBy(point => point.X).ToList();

            for (int i = 0; i < points.Count - 1; i++)
            {
                CrossSectionPoint p1 = points[i];
                CrossSectionPoint p2 = points[i + 1];
                double width = p2.X - p1.X;

                if (width <= 0) continue;

                double h1 = waterLevel - p1.Y;
                double h2 = waterLevel - p2.Y;

                if (h1 <= 0 && h2 <= 0) continue;

                if (h1 >= 0 && h2 >= 0)
                {
                    area += (h1 + h2) * width / 2.0;
                    continue;
                }

                if (h1 > 0)
                {
                    double wetWidth = width * h1 / (h1 - h2);
                    area += h1 * wetWidth / 2.0;
                    continue;
                }

                if (h2 > 0)
                {
                    double wetWidth = width * h2 / (h2 - h1);
                    area += h2 * wetWidth / 2.0;
                }
            }

            return area;
        }
    }
}
