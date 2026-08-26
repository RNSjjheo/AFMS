using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class CrossSection
    {
        public int Id { get; set; } = -1;
        public string Description { get; set; } = string.Empty;
        public double ZeroPointElevation { get; set; }
        public CrossSectionPointCollection Points { get; } = new();
        public TransectCollection Transects { get; } = new();

        public void CalculateTransectAreas(double waterLevel)
        {
            Points.WaterLevel = waterLevel;
            Transects.CalculateSectionAreas(Points, waterLevel);
        }
    }
}
