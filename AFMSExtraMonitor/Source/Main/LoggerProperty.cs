using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AFMSExtraMonitor
{
    [TypeConverter(typeof(PropertyOrderConverter))]
    internal class LoggerProperty
    {
        private const string GATEGORY_LOGGER = "1.프로그램";
        private const string GATEGORY_SITE = "​​​2.​​​​​​​​​​​사이트정보";
        private const string GATEGORY_SYSTEM = "3.시스템";
        private const string GATEGORY_VIDEO = "4.영상유속계";
        private const string GATEGORY_MPDS = "5.전자파표면유속계";

        [PropertyOrder(1)]
        [Category(GATEGORY_LOGGER)]
        [DisplayName("버전 정보")]
        [ReadOnly(true)]
        public string LoggerVersion { get; set; } = "";

        [PropertyOrder(2)]
        [Category(GATEGORY_LOGGER)]
        [DisplayName("빌드 날짜")]
        [ReadOnly(true)]
        public string LoggerBuildDate { get; set; } = "";

        [PropertyOrder(3)]
        [Category(GATEGORY_LOGGER)]
        [DisplayName("빌드 시간")]
        [ReadOnly(true)]
        public string LoggerBuildTime { get; set; } = "";

        [PropertyOrder(10)]
        [Category(GATEGORY_SITE)]
        [DisplayName("SiteCode")]
        [ReadOnly(true)]
        public string SiteCode { get; set; } = "";



        [PropertyOrder(20)]
        [Category(GATEGORY_SYSTEM)]
        [DisplayName("메모리최대")]
        [ReadOnly(true)]
        public string MemoryBytesMax { get; set; } = "";

        [PropertyOrder(21)]
        [Category(GATEGORY_SYSTEM)]
        [DisplayName("메모리사용")]
        [ReadOnly(true)]
        public string MemoryBytes { get; set; } = "";

        [PropertyOrder(22)]
        [Category(GATEGORY_SYSTEM)]
        [DisplayName("메모리최소")]
        [ReadOnly(true)]
        public string MemoryBytesMin { get; set; } = "";


        [PropertyOrder(23)]
        [Category(GATEGORY_SYSTEM)]
        [DisplayName("실행 날짜")]
        [ReadOnly(true)]
        public string StartedDate { get; set; } = "";


        [PropertyOrder(24)]
        [Category(GATEGORY_SYSTEM)]
        [DisplayName("실행 시간")]
        [ReadOnly(true)]
        public string StartedTime { get; set; } = "";

        [PropertyOrder(30)]
        [Category(GATEGORY_VIDEO)]
        [DisplayName("측정 날짜")]
        [ReadOnly(true)]
        public string VideoLastDate { get; set; } = "";

        [PropertyOrder(31)]
        [Category(GATEGORY_VIDEO)]
        [DisplayName("측정 시간")]
        [ReadOnly(true)]
        public string VideoLastTime { get; set; } = "";

        [PropertyOrder(32)]
        [Category(GATEGORY_VIDEO)]
        [DisplayName("측정 유속")]
        [ReadOnly(true)]
        public string VidoeVelocity { get; set; } = "";

        [PropertyOrder(33)]
        [Category(GATEGORY_VIDEO)]
        [DisplayName("CELL 수")]
        [ReadOnly(true)]
        public string VideoCellCnt { get; set; } = "";

        [PropertyOrder(34)]
        [Category(GATEGORY_VIDEO)]
        [DisplayName("CELL 크기")]
        [ReadOnly(true)]
        public string VideoCellLen { get; set; } = "";

        [PropertyOrder(35)]
        [Category(GATEGORY_VIDEO)]
        [DisplayName("유속 불확도")]
        [ReadOnly(true)]
        public string VideoMeasCert { get; set; } = "";


        [PropertyOrder(40)]
        [Category(GATEGORY_MPDS)]
        [DisplayName("COM 포트")]
        [ReadOnly(true)]
        public string MPDSPort { get; set; } = "";

        [PropertyOrder(41)]
        [Category(GATEGORY_MPDS)]
        [DisplayName("통신 주소")]
        [ReadOnly(true)]
        public string MPDSAddress { get; set; } = "";

        [PropertyOrder(42)]
        [Category(GATEGORY_MPDS)]
        [DisplayName("측정 날짜")]
        [ReadOnly(true)]
        public string MPDSMeasDate { get; set; } = "";

        [PropertyOrder(43)]
        [Category(GATEGORY_MPDS)]
        [DisplayName("측정 시간")]
        [ReadOnly(true)]
        public string MPDSMeasTime { get; set; } = "";

        [PropertyOrder(44)]
        [Category(GATEGORY_MPDS)]
        [DisplayName("컬렉터전압")]
        [ReadOnly(true)]
        public string MPDSCollectorVolt { get; set; } = "";

        [PropertyOrder(45)]
        [Category(GATEGORY_MPDS)]
        [DisplayName("컬렉터신호")]
        [ReadOnly(true)]
        public string MPDSCollectorRssi { get; set; } = "";

        [PropertyOrder(46)]
        [Category(GATEGORY_MPDS)]
        [DisplayName("메인 수위")]
        [ReadOnly(true)]
        public string MPDSMeasWaterLvl { get; set; } = "";

        [PropertyOrder(47)]
        [Category(GATEGORY_MPDS)]
        [DisplayName("장치 개수")]
        [ReadOnly(true)]
        public string MPDSMeasCellCnt { get; set; } = "";

        [PropertyOrder(47)]
        [Category(GATEGORY_MPDS)]
        [DisplayName("임의 평균")]
        [ReadOnly(true)]
        public string MPDSSimpleVelo { get; set; } = "";

        public void SetDiag(Diagnotics diag)
        {
            SiteCode = diag.SiteCode;
            LoggerVersion = diag.LoggerVersion;

            if (DateTime.TryParse(diag.LoggerBuild, out DateTime dtBuild))
            {
                LoggerBuildDate = dtBuild.ToString("yyyy-MM-dd");
                LoggerBuildTime = dtBuild.ToString("HH:mm:ss");
            }
            else
            {
                LoggerBuildDate = diag.LoggerBuild;
                LoggerBuildTime = diag.LoggerBuild;
            }

            if (DateTime.TryParse(diag.StartTime, out DateTime dtStart))
            {
                StartedDate = dtStart.ToString("yyyy-MM-dd");
                StartedTime = dtStart.ToString("HH:mm:ss");
            }
            else
            {
                StartedDate = diag.LoggerBuild;
                StartedTime = diag.LoggerBuild;
            }

            VideoLastDate = diag.VideoMeasDate;
            VideoLastTime = diag.VideoMeasTime;
            VidoeVelocity = diag.VideoMeasVelo.ToString("0.000m/sec");
            VideoCellCnt = diag.VideoMeasCellCnt.ToString();
            VideoCellLen = diag.VideoMeasCellLen.ToString("0.00m");
            VideoMeasCert = diag.VideoMeasCert.ToString("0.00%");

            MemoryBytes = diag.MemoryUsage.ToString("0.00MB");
            MemoryBytesMax = diag.MemoryUsageMax.ToString("0.00MB");
            MemoryBytesMin = diag.MemoryUsageMin.ToString("0.00MB");

            MPDSPort = diag.MPDSPort;
            MPDSAddress = diag.MPDSRFInfo;
            MPDSMeasDate = diag.MPDSMeasDate;
            MPDSMeasTime = diag.MPDSMeasTime;
            MPDSMeasWaterLvl = diag.MPDSWaterLvl.ToString("0.00m");
            MPDSCollectorVolt = diag.MPDSDevVolt.ToString("0.00V");
            MPDSCollectorRssi = diag.MPDSRFRssi.ToString();
            MPDSMeasCellCnt = diag.MPDSMeasCnt.ToString();
            MPDSSimpleVelo = diag.MPDSSimpleVelo.ToString("0.000m/sec");
        }
    }
}
