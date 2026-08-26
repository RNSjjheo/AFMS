using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class Transect
    {
        public CrossSectionPointCollection AreaFull = new();
        public CrossSectionPointCollection AreaThis = new();
        public int Id;
        public double LeftBankDistance { get; set; }
        public double Elevation { get; set; }
        public double SurfaceWidth;

        public double SectionArea;
    }
}
