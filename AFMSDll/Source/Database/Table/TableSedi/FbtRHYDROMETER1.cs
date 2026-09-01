using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRHYDROMETER1 : FbtRHYDROMETER
    {
        public const string TABLE_NAME = "RHYDROMETER1";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }
    }
}
