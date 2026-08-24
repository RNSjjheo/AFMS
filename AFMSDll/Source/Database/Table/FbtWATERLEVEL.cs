using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtWATERLEVEL : _FBTableBase
    {
        public const string TABLE_NAME = "RWATERLEVEL";
        public override string GetCreateTableSql()
        {
            return "";
        }

        public override string GetTableName()
        {
            return TABLE_NAME;
        }
    }
}
