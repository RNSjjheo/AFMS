using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    internal sealed class SediAdvmRecord
    {
        public int Number { get; set; } // No_ADVM
        public int Type { get; set; } // ADVMType
        public double Ssc { get; set; } // SSC
        public double Sediment { get; set; } // Sedment (RADX 원본 철자)
        public double TotalSediment { get; set; } // TotalSed
        public int StartCell { get; set; } // StartCell
        public int EndCell { get; set; } // EndSell (RADX 원본 철자)
        public string Decision { get; set; } = string.Empty; // Dec_ADVM
        public double WaterTemperature { get; set; } // Temp_Water
        public double Depth { get; set; } // Depth_ADVM
        public double Pitch { get; set; } // Pitch
        public double Roll { get; set; } // Roll
        public int CellCount { get; set; } // WN
        public int CellSize { get; set; } // WS (cm)
        public int PingCount { get; set; } // WP
        public int Frequency { get; set; } // WF
        public int FirstCellDistance { get; set; } // DIS1 (cm)
        public int LastCellDistance { get; set; } // DIS2 (cm)

        /// <summary>
        /// No_Cell, V_EW, V_NS, E1, E2 열이 WN만큼 반복된 값이다.
        /// </summary>
        public List<SediCellRecord> Cells { get; } = [];

        public bool HasExpectedCellCount =>
            CellCount >= 0 && Cells.Count == CellCount;
    }
}
