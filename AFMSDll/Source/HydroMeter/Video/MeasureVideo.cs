using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class MeasureVideo
    {
        public const string KEY_DEVICE_TYPE = "DeviceType";
        public const string KEY_DEVICE_KIND = "DeviceKind";
        public const string KEY_DATE_TIME = "DateTime";
        public const string KEY_STATUS = "Status";
        public const string KEY_INTERVAL = "Interval";
        public const string KEY_WATER_LEVEL = "W";
        public const string KEY_AREA = "A";
        public const string KEY_AREA_UNCERATAINLY = "Au";
        public const string KEY_VELOCITY = "V";
        public const string KEY_VELO_UNCERATAINLY = "Vu";
        public const string KEY_DISCHARGE = "Q";
        public const string KEY_DISC_UNCERATAINLY = "Qu";
        public const string KEY_CELL_COUNT = "CellCount";
        public const string KEY_CELL_LENGTH = "CellLength";
        private bool disposedValue;

        public int Id { get; set; }
        public string SiteCode { get; set; } = "";
        public HydroVideoType DeviceType { get; set; } = HydroVideoType.NONE;

        public DateTime Datetime { get; set; }
        public VideoMeasureStatus Status { get; set; }

        public int Interval { get; set; }       // 측정간격, 단위: 초
        public double WaterLevel { get; set; }           // 수위, 단위: m
        public double Area { get; set; }           // 단면적
        public double AreaUncertainty { get; set; }           // 단면적 불확도
        public double Velocity { get; set; }           // 평균 유속
        public double VeloUncertainty { get; set; }       // 유속 불확도/인증값
        public double Disc { get; set; }           // 유량
        public double DiscUncertainty { get; set; }       // 유량 불확도/인증값
        public int CellCount { get; set; }
        public double CellLength { get; set; }

        public List<MeasureVideoCell> Cells { get; set; } = new List<MeasureVideoCell>();
    }

    public class MeasureVideoCell
    {
        public int Id { get; set; }
        public int VideoId { get; set; }
        public int No { get; set; }
        public double Velocity { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double Uncertainty { get; set; }
    }
}
