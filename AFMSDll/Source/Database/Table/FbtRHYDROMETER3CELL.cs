using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FbtRHYDROMETER3CELL : FbtRHYDROMETERCELL
    {
        public const string TABLE_NAME = "RHYDROMETER3CELL";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }
    }
}
