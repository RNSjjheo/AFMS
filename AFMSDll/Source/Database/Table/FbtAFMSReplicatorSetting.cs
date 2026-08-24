using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FbtAFMSReplicatorSetting : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_REPLICATOR_SETTING";
        public const string COL_TARGET_IP = "TargetIp";
        public const string COL_ENABLE_VTH = "EnableVth";
        public const string COL_ENABLE_LEVEL = "EnableLevel";
        public const string COL_BEGIN_DT = "BeginDT";

        public override string GetTableName()
        {
            return TABLE_NAME;
        }

        public override string GetCreateTableSql()
        {
            string sql = $"CREATE TABLE {TABLE_NAME} (";
            sql += "\n" + $"{COL_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_DATE} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_MEASURE_TIME} VARCHAR(8) NOT NULL,";
            sql += "\n" + $"{COL_TARGET_IP} VARCHAR(30) NOT NULL,";
            sql += "\n" + $"{COL_ENABLE_VTH} SMALLINT NOT NULL,";       //Firebird 2.5까지는 Boolean 타입이 없다
            sql += "\n" + $"{COL_ENABLE_LEVEL} SMALLINT NOT NULL,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + $")";

            return sql;
        }

        public override List<string> GetDefaultInsertSql()
        {
            List<string> result = [];

            string sql = $"INSERT INTO {TABLE_NAME}(";
            sql += $"\n" + $"{COL_ID}, {COL_MEASURE_DATE}, {COL_MEASURE_TIME}, ";
            sql += $"\n" + $"{COL_TARGET_IP}, {COL_ENABLE_VTH}, {COL_ENABLE_LEVEL}";
            sql += $"\n" + $") Values (";
            sql += $"\n" + $"1, '20260801', '000000', '192.168.10.13', 0, 0";
            sql += $"\n" + $")";

            result.Add(sql);

            return result;
        }

        public override List<string> GetExampleSql()
        {
            List<string> result = [];

            string sql = $"UPDATE {TABLE_NAME} SET";
            sql += $"\n" + $"{COL_ID} = '1',";
            sql += $"\n" + $"{COL_MEASURE_DATE} = '{DateTime.Now.ToString("yyyyMMdd")}',";
            sql += $"\n" + $"{COL_MEASURE_TIME} = '{DateTime.Now.ToString("HHmmss")}',";
            sql += $"\n" + $"{COL_TARGET_IP} = '192.168.10.13',";
            sql += $"\n" + $"{COL_ENABLE_VTH} = 1,";
            sql += $"\n" + $"{COL_ENABLE_LEVEL} = 1,";
            sql += $"\n" + $"WHERE ID = 1";

            result.Add(sql);

            return result;
        }
    }
}
