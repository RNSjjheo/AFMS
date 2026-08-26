using System;
using System.Collections.Generic;
using System.Linq;

namespace AFMSDll
{
    public class TransectCollection : List<Transect>
    {
        /// <summary>
        /// 측선 중심 위치를 기준으로 담당 구간을 나누고 구간별 단면적을 계산합니다.
        /// 첫 측선은 좌안(0)에서 시작하고 마지막 측선은 단면의 우안에서 끝납니다.
        /// </summary>
        public void CalculateSectionAreas(CrossSectionPointCollection crossSectionPoints, double waterLevel)
        {
            ArgumentNullException.ThrowIfNull(crossSectionPoints);

            if (!double.IsFinite(waterLevel))
                throw new ArgumentOutOfRangeException(nameof(waterLevel), "수위는 유한한 값이어야 합니다.");
            if (crossSectionPoints.Count < 2)
                throw new ArgumentException("단면적 계산에는 두 개 이상의 단면 좌표가 필요합니다.", nameof(crossSectionPoints));
            if (Count == 0) return;

            List<CrossSectionPoint> section = crossSectionPoints
                .OrderBy(point => point.LeftBankDistance)
                .ToList();
            List<Transect> transects = this
                .OrderBy(transect => transect.CenterLeftBankDistance)
                .ToList();

            Validate(section, transects);
            double sectionEnd = section[^1].LeftBankDistance;

            for (int i = 0; i < transects.Count; i++)
            {
                Transect transect = transects[i];
                double start = i == 0
                    ? 0.0
                    : (transects[i - 1].CenterLeftBankDistance + transect.CenterLeftBankDistance) / 2.0;
                double end = i == transects.Count - 1
                    ? sectionEnd
                    : (transect.CenterLeftBankDistance + transects[i + 1].CenterLeftBankDistance) / 2.0;

                transect.LeftBankDistance = start;
                transect.EndLeftBankDistance = end;
                transect.SurfaceWidth = end - start;
                transect.Elevation = InterpolateElevation(section, transect.CenterLeftBankDistance);
                transect.AreaFull = BuildSection(section, 0.0, end, waterLevel);
                transect.AreaThis = BuildSection(section, start, end, waterLevel);
                transect.SectionArea = transect.AreaThis.Area;
            }
        }

        private static void Validate(List<CrossSectionPoint> section, List<Transect> transects)
        {
            double sectionStart = section[0].LeftBankDistance;
            double sectionEnd = section[^1].LeftBankDistance;

            if (sectionStart > 0.0 || sectionEnd <= 0.0)
                throw new ArgumentException("단면 좌표에는 좌안 거리 0과 그보다 큰 우안 거리가 포함되어야 합니다.");

            for (int i = 0; i < transects.Count; i++)
            {
                double center = transects[i].CenterLeftBankDistance;
                if (!double.IsFinite(center) || center < 0.0 || center > sectionEnd)
                    throw new ArgumentOutOfRangeException(nameof(transects), "측선 중심 위치가 단면 범위를 벗어났습니다.");
                if (i > 0 && transects[i - 1].CenterLeftBankDistance == center)
                    throw new ArgumentException("측선 중심 위치가 중복되어 있습니다.", nameof(transects));
            }
        }

        private static CrossSectionPointCollection BuildSection(
            List<CrossSectionPoint> section, double start, double end, double waterLevel)
        {
            CrossSectionPointCollection result = new CrossSectionPointCollection { WaterLevel = waterLevel };
            result.Add(new CrossSectionPoint(start, InterpolateElevation(section, start, useLastExactPoint: true)));
            result.AddRange(section.Where(point =>
                point.LeftBankDistance > start && point.LeftBankDistance < end));
            result.Add(new CrossSectionPoint(end, InterpolateElevation(section, end, useLastExactPoint: false)));
            return result;
        }

        private static double InterpolateElevation(
            List<CrossSectionPoint> section, double distance, bool useLastExactPoint = true)
        {
            IEnumerable<CrossSectionPoint> exactPoints = section.Where(point => point.LeftBankDistance == distance);
            CrossSectionPoint? exact = useLastExactPoint ? exactPoints.LastOrDefault() : exactPoints.FirstOrDefault();
            if (exact != null) return exact.Elevation;

            for (int i = 0; i < section.Count - 1; i++)
            {
                CrossSectionPoint left = section[i];
                CrossSectionPoint right = section[i + 1];
                if (right.LeftBankDistance <= left.LeftBankDistance) continue;
                if (distance <= left.LeftBankDistance || distance >= right.LeftBankDistance) continue;

                double ratio = (distance - left.LeftBankDistance) /
                               (right.LeftBankDistance - left.LeftBankDistance);
                return left.Elevation + ((right.Elevation - left.Elevation) * ratio);
            }

            throw new ArgumentOutOfRangeException(nameof(distance), "보간 위치가 단면 범위를 벗어났습니다.");
        }
    }
}
