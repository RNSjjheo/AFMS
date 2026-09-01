using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRHYDROMETER2CELL : FbtRHYDROMETERCELL
    {
        public const string TABLE_NAME = "RHYDROMETER2CELL";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }
    }
}
