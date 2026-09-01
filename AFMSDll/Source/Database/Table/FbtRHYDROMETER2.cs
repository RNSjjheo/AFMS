using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRHYDROMETER2 : FbtRHYDROMETER
    {
        public const string TABLE_NAME = "RHYDROMETER2";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }
    }
}
