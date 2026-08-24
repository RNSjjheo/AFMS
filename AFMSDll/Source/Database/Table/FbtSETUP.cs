using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtSETUP : _FBTableBase
    {
        public const string TABLE_NAME = "RSETUP";
        public const string COL_PK1 = "PK1";
        public const string COL_PK2 = "PK2";
        public const string COL_VALUE01 = "VALUE01";
        public const string COL_VALUE02 = "VALUE02";
        public const string COL_VALUE05 = "VALUE05";
        public const string COL_VALUE11 = "VALUE11";
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
