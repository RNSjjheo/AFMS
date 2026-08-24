using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtVTHLOGGER : _FBTableBase
    {
        public const string TABLE_NAME = "RVTHLOGGER";

        public const string COL_VTHKIND = "VTHKIND";
        public const string COL_VOLT = "VOLT";
        public const string COL_TEMPERATURE = "TEMPERATURE";
        public const string COL_HUMIDITY = "HUMIDITY";
        public const string COL_DCCHARGE = "DCCHARGE";
        public const string COL_DCBATTERY = "DCBATTERY";
        public const string COL_VALUE01 = "VALUE01";
        public const string COL_VALUE02 = "VALUE02";
        public const string COL_VALUE03 = "VALUE03";
        public const string COL_VALUE04 = "VALUE04";
        public const string COL_VALUE05 = "VALUE05";
        public const string COL_RAWDATA = "RAWDATA";
        public const string COL_ = "VTHKIND";


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
