using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtWATERLEVEL : _FBTableBase
    {
        public const string TABLE_NAME = "RWATERLEVEL";
        public const string COL_WATER_KIND = "WATERKIND";
        public const string COL_AVG_WATER_LEVEL = "AVGWATERLEVEL";
        public const string COL_MIN_WATER_LEVEL = "MINWATERLEVEL";
        public const string COL_MAX_WATER_LEVEL = "MAXWATERLEVEL";
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
