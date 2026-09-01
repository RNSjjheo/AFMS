using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    internal sealed class SediSedimentRecord
    {
        public string StationCode { get; set; } = string.Empty; // St_code
        public string MeasurementTime { get; set; } = string.Empty; // YYYYMMDDhhmm
        public string OverallDecision { get; set; } = string.Empty; // Deci_All
        public string VthDecision { get; set; } = string.Empty; // Deci_VTH
        public int Ac { get; set; } // AC
        public double DcCharge { get; set; } // DC_Charge
        public double DcBattery { get; set; } // DC_Battery
        public double SystemTemperature { get; set; } // Temp_Sys
        public double SystemHumidity { get; set; } // Hr_Sys
        public string WaterLevelDecision { get; set; } = string.Empty; // Deci_WL
        public double WaterDepth { get; set; } // WaterDepth
        public double WaterLevel { get; set; } // WaterLevel
        public double WaterLevelOffset { get; set; } // WL_Offset
        public double Salinity { get; set; } // Salinity

        /// <summary>
        /// RADX가 출력한 ADVM 블록 목록. 현재 RADX는 하층 1번과 상층 2번을 출력할 수 있다.
        /// </summary>
        public List<SediAdvmRecord> Advms { get; } = [];
    }
}
