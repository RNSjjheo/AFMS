using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class CrossSectionPoint
    {
        public double LeftBankDistance;
        public double BedElevation;

        public CrossSectionPoint()
        {
        }

        public CrossSectionPoint(double leftbank, double elevation)
        {
            LeftBankDistance = leftbank;
            BedElevation = elevation;
        }
    }
}
