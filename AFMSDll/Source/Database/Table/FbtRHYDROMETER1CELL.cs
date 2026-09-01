using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRHYDROMETER1CELL : FbtRHYDROMETERCELL
    {
        public const string TABLE_NAME = "RHYDROMETER1CELL";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }
    }
}
