using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class CrossSection
    {
        public CrossSectionPointCollection Points { get; } = new();
        public TransectCollection Transects { get; } = new();
    }
}
