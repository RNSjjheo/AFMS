using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace AFMSDll
{
    public class FbtAFMSDischargeConfig : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DISCHARGE_CONFIG";
        public const string COL_DEVICE_TYPE = "DEVICE_TYPE";
        public const string COL_DEVICE_ID = "DEVICE_ID";
        public const string COL_DISCHARGE_METHOD = "DISCHARGE_METHOD";
        public const string COL_METHOD_CONFIG_ID = "METHOD_CONFIG_ID";
        public const string COL_ENABLED = "ENABLED";
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
            sql += "\n" + $"{COL_DEVICE_TYPE} VARCHAR(30) NOT NULL,";
            sql += "\n" + $"{COL_DEVICE_ID} INTEGER NOT NULL,";
            sql += "\n" + $"{COL_DISCHARGE_METHOD} VARCHAR(32) NOT NULL,";
            sql += "\n" + $"{COL_METHOD_CONFIG_ID} INTEGER,";
            sql += "\n" + $"{COL_ENABLED} INTEGER DEFAULT 1 NOT NULL,";
            sql += "\n" + $"CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY({COL_ID})";
            sql += "\n" + ")";

            return sql;
        }

        public override string CheckNewColumn(FBDatabase db)
        {
            string result;

            if (!HasColumn(db, COL_DEVICE_TYPE))
            {
                result = AddColumn(db, COL_DEVICE_TYPE, "VARCHAR(30)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_DEVICE_ID))
            {
                result = AddColumn(db, COL_DEVICE_ID, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_DISCHARGE_METHOD))
            {
                result = AddColumn(db, COL_DISCHARGE_METHOD, "VARCHAR(32)");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_METHOD_CONFIG_ID))
            {
                result = AddColumn(db, COL_METHOD_CONFIG_ID, "INTEGER");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            if (!HasColumn(db, COL_ENABLED))
            {
                result = AddColumn(db, COL_ENABLED, "INTEGER DEFAULT 1");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            return "";
        }

    }
}
