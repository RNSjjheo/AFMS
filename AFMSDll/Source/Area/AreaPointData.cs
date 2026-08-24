using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

namespace AFMSDll
{
    public class AreaPoint
    {
        public int Index;
        public double Dist;
        public double Elev;

        public AreaPoint(int index, double dist, double elev)
        {
            Index = index;
            Dist = dist;
            Elev = elev;
        }
    }

    public class AreaPointDatas : List<AreaPoint>
    {

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true
        };
        private double _Area;
        private double _ZeroPointEL { get; set; }
        public double? WaterLevel { get; set; }

        //[Browsable(false)]
        //public AreaPointDatas Data => this;

        public double Area 
        {
            get => (GetCrossSectionArea());
            
        }

        public double ZeroPointEL
        {
            get => _ZeroPointEL;
            set
            {
                _ZeroPointEL = value;
                ApplyZeroPointOffset();
            }
                 
        }

        public void ClearWaterLevel()
        {
            WaterLevel = null;
        }

        private double GetCrossSectionArea()
        {
            if (!WaterLevel.HasValue) return 0.0;
            if (Count < 2) return 0.0;

            double waterLevel = WaterLevel.Value;
            double area = 0.0;

            List<AreaPoint> points = this.OrderBy(x => x.Dist).ToList();

            for (int i = 0; i < points.Count - 1; i++)
            {
                AreaPoint p1 = points[i];
                AreaPoint p2 = points[i + 1];

                double width = p2.Dist - p1.Dist;

                if (width <= 0) continue;

                double h1 = waterLevel - p1.Elev;
                double h2 = waterLevel - p2.Elev;

                // 두 점 모두 수위 이상이면 물이 없는 구간
                if (h1 <= 0 && h2 <= 0) continue;

                // 두 점 모두 수위 아래이면 사다리꼴 면적
                if (h1 >= 0 && h2 >= 0)
                {
                    area += (h1 + h2) * width / 2.0;
                    continue;
                }

                // 첫 번째 점만 수위 아래
                if (h1 > 0)
                {
                    double wetWidth = width * h1 / (h1 - h2);
                    area += h1 * wetWidth / 2.0;
                    continue;
                }

                // 두 번째 점만 수위 아래
                if (h2 > 0)
                {
                    double wetWidth = width * h2 / (h2 - h1);
                    area += h2 * wetWidth / 2.0;
                }
            }

            return area;
        }

        public string GetJson()
        {
            AreaPointDataJson data = new AreaPointDataJson
            {
                WaterLevel = WaterLevel,
                Points = this
            };

            return JsonSerializer.Serialize(data, JsonOptions);
        }

        public void Convert(string jsonStr)
        {
            Clear();
            WaterLevel = null;

            if (string.IsNullOrWhiteSpace(jsonStr)) return;

            try
            {
                AreaPointDataJson? data = JsonSerializer.Deserialize<AreaPointDataJson>(jsonStr, JsonOptions);

                if (data != null && data.Points != null)
                {
                    WaterLevel = data.WaterLevel;
                    AddRange(data.Points);
                    return;
                }
            }
            catch (JsonException)
            {
            }

            AreaPointDatas? oldData = JsonSerializer.Deserialize<AreaPointDatas>(jsonStr, JsonOptions);

            if (oldData == null) return;

            AddRange(oldData);
            ApplyZeroPointOffset();
        }

        private void ApplyZeroPointOffset()
        {
            foreach (AreaPoint point in this)
            {
                point.Elev = point.Elev - _ZeroPointEL;
            }
        }

        private sealed class AreaPointDataJson
        {
            public double? WaterLevel { get; set; }
            public List<AreaPoint> Points { get; set; } = new List<AreaPoint>();
        }
    }
}