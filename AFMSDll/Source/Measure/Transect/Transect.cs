using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class Transect
    {
        public CrossSectionPointCollection AreaFull = new();
        public CrossSectionPointCollection AreaThis = new();
        public int Id { get; set; } = -1; // DB 고유 식별자
        public int No { get; set; }       // 사람이 사용하는 측선 번호 1부터 시작
        /// <summary>측선 담당 구간의 시작 위치(좌안 기준 거리)입니다.</summary>
        public double LeftBankDistance { get; set; }
        /// <summary>유속계 측선 정보에서 얻은 측선 중심 위치(좌안 기준 거리)입니다.</summary>
        public double CenterLeftBankDistance { get; set; }
        /// <summary>측선 담당 구간의 종료 위치(좌안 기준 거리)입니다.</summary>
        public double EndLeftBankDistance { get; set; }
        public double Elevation { get; set; }
        public double SurfaceWidth;

        public double SectionArea;
    }
}
