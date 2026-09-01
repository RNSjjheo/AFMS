using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    /// <summary>
    /// RADX SED 행의 반복 셀 블록을 표현한다.
    /// </summary>
    internal sealed class SediCellRecord
    {
        public int Number { get; set; } // No_Cell
        public int VelocityEastWest { get; set; } // V_EW (mm/s)
        public int VelocityNorthSouth { get; set; } // V_NS (mm/s)
        public int Echo1 { get; set; } // E1
        public int Echo2 { get; set; } // E2
    }
}
