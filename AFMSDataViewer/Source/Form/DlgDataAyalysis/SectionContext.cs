using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    public sealed record SectionContext(
        CrossSectionPointCollection Points,
        TransectCollection Transects,
        double? WaterLevel,
        string Message);

}
