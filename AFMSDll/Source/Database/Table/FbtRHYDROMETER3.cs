using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRHYDROMETER3 : FbtRHYDROMETER
    {
        public const string TABLE_NAME = "RHYDROMETER3";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }
    }
}
