using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class CrossSection
    {
        public CrossSectionPointCollection Points { get; } = new();
        public TransectCollection Transects { get; } = new();

        public void CalculateTransectAreas(double waterLevel)
        {
            Points.WaterLevel = waterLevel;
            Transects.CalculateSectionAreas(Points, waterLevel);
        }
    }
}
