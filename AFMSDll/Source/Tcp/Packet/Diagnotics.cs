using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class Diagnotics :_PacketBase
    {
        public double MemoryUsage;
        public double MemoryUsageMin;
        public double MemoryUsageMax;

        public string SiteCode;
        public string LoggerVersion;
        public string LoggerBuild;
        public int WebPort;
        public string WebPath;
        public string StartTime;

        public double VideoMeasVelo;
        public string VideoMeasDate;
        public string VideoMeasTime;
        public int VideoMeasCellCnt;
        public double VideoMeasCellLen;
        public double VideoMeasCert;

        public string MPDSPort;
        public string MPDSRFInfo;
        public int MPDSMeasCnt;
        public string MPDSMeasDate;
        public string MPDSMeasTime;
        public double MPDSDevVolt;
        public double MPDSWaterLvl;
        public int MPDSRFRssi;
        public double MPDSSimpleVelo;


        public Diagnotics()        
        {
            JsonType = JsonPacketType.Diagnotics;

            MemoryUsage = 0;
            MemoryUsageMin = 0;
            MemoryUsageMax = 0;

            SiteCode = "";

            WebPort = 0;
            WebPath = "";

            MPDSPort = "";
            MPDSRFInfo = "";
            MPDSMeasCnt = 0;
            MPDSMeasDate = "";
            MPDSMeasTime = "";
            MPDSRFRssi = 0;

    }
}
}
