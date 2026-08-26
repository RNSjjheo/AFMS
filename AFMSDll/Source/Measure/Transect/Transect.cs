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
        public double PosX;
        public double Width;
        public double Level;
        public double SectionArea;
    }
}
