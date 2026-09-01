using FirebirdSql.Data.FirebirdClient;
using RnsLibrary;
using System;
using System.Collections.Generic;
using System.Data;

namespace AFMSDll
{
    public sealed class FBProvider
    {
        private static readonly FBProvider instance = new FBProvider();

        private readonly object syncRoot = new object();
        private string defaultConnectionString = string.Empty;
        private FBProvider()
        {
            // 외부에서 new AppConfig() 하지 못하게 막음
        }

        public static FBProvider Instance
        {
            get
            {
                return instance;
            }
        }

        // 기존 외부 호출과의 호환을 위한 복사본 기반 프로퍼티입니다.
        // 신규 코드는 Initialize()와 CreateDatabase()를 사용합니다.
        public FbConnectionStringBuilder ConnStrBuilder
        {
            get
            {
                lock (syncRoot)
                {
                    if (string.IsNullOrWhiteSpace(defaultConnectionString))
                    {
                        return new FbConnectionStringBuilder();
                    }

                    return new FbConnectionStringBuilder(defaultConnectionString);
                }
            }
            set
            {
                Initialize(value);
            }
        }

        public bool IsInitialized
        {
            get
            {
                lock (syncRoot)
                {
                    return !string.IsNullOrWhiteSpace(defaultConnectionString);
                }
            }
        }

        public void Initialize(FbConnectionStringBuilder connectionStringBuilder)
        {
            if (connectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(connectionStringBuilder));
            }

            Initialize(connectionStringBuilder.ConnectionString);
        }

        public void Initialize(DatabaseProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Initialize(profile.ConnectionString);
        }

        public void Initialize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("DB 연결 문자열이 비어 있습니다.", nameof(connectionString));
            }

            lock (syncRoot)
            {
                defaultConnectionString = connectionString;
            }
        }

        public FBDatabase CreateDatabase()
        {
            string connectionString;

            lock (syncRoot)
            {
                connectionString = defaultConnectionString;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("FBProvider가 초기화되지 않았습니다.");
            }

            return new FBDatabase(connectionString);
        }

        public FBDatabase CreateDatabase(DatabaseProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return new FBDatabase(profile.ConnectionString);
        }

        public FBDatabase CreateDatabase(string connectionString)
        {
            return new FBDatabase(connectionString);
        }


        public int GetNextID(string tablename)
        {
            string sql = $"SELECT COALESCE(MAX({_FBTableBase.COL_ID}), 0) + 1 AS {_FBTableBase.COL_ID} FROM {tablename}";
            using FBDatabase db = CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                try
                {
                    string strId = row[_FBTableBase.COL_ID].ToString();
                    int id = Convert.ToInt32(strId);
                    return id;
                }
                catch
                {

                }
            }

            return 1;
        }

        public List<string> CheckTables(bool isAFMS = true)
        {
            List<string> result = new List<string>();

            List<_FBTableBase> tables = isAFMS? CreateTableDefinitions() : CreateTableSediDefinitions();
            using FBDatabase db = CreateDatabase();

            foreach (_FBTableBase table in tables)
            {
                string tablename = table.GetTableName();

                if (ExistTable(tablename, result)) continue;

                string crateTableSql = table.GetCreateTableSql();

                if (crateTableSql == "")
                {
                    result.Add($"{tablename} 테이블은 생성 관리 예외 테이블입니다.");
                    continue;
                }
                else
                {
                    result.Add($"{tablename}을(를) 생성합니다...");
                }

                db.Execute(crateTableSql, out string errorMsg);
                if (errorMsg != string.Empty) result.Add(errorMsg);

                ExistTable(tablename, result);

                List<string>? defaultValueSql = table.GetDefaultInsertSql();
                if (defaultValueSql == null) continue;

                foreach (string sql in defaultValueSql)
                {
                    db.RunNonQuery(sql);
                }
            }

            foreach (_FBTableBase table in tables)
            {
                string log = table.CheckNewColumn(db);
                if(log != "") result.Add(log);
            }



            Sync();

            return result;
        }

        public void Sync()
        {
            FBHydorManger.SyncAdd();
            FBHydorManger.SyncRemove();
        }

        public bool IsExistExtraEmptyData()
        {
            string sql = $"SELECT COUNT(*) FROM {FbtSETUP.TABLE_NAME}";
            sql += $" WHERE {FbtSETUP.COL_PK1} = 100";

            using FBDatabase db = CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                int count = Convert.ToInt32(row[0].ToString());
                return count > 0;
            }

            return false;
        }

        public bool ExistTable(string tableName, List<string> result)
        {
            string sql = @"SELECT COUNT(*) FROM RDB$RELATIONS";
            sql += $" WHERE TRIM(RDB$RELATION_NAME) = '{tableName}'";
            sql += @" AND COALESCE(RDB$SYSTEM_FLAG, 0) = 0";
            sql += @" AND RDB$VIEW_BLR IS NULL;";

            using FBDatabase db = CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                int count = Convert.ToInt32(row[0].ToString());

                if (count > 0)
                {
                    result.Add($"{tableName} 테이블이 확인 완료");
                    return true;
                }
            }

            result.Add($"{tableName} 테이블이 확인 실패");
            return false;
        }

        private static List<_FBTableBase> CreateTableDefinitions()
        {
            List<_FBTableBase> tables = new List<_FBTableBase>();

            tables.Add(new FbtHYDROMETERVIDEO());
            tables.Add(new FbtHYDROMETERVIDEOCELL());
            tables.Add(new FbtHYDROMETERMPDS());
            tables.Add(new FbtHYDROMETERMPDSCELL());
            tables.Add(new FbtWATERLEVEL());
            tables.Add(new FbtVTHLOGGER());
            tables.Add(new FbtSETUP());
            tables.Add(new FbtAFMSCrossSection());
            tables.Add(new FbtAFMSHydroMeter());
            tables.Add(new FbtAFMSDischargeMethodConfig());
            tables.Add(new FbtAFMSDischargeTimeslot());
            tables.Add(new FbtAFMSDischargeResult());
            tables.Add(new FbtAFMSReplicatorSetting());
            tables.Add(new FbtAFMSHydroTransect());

            return tables;
        }

        private static List<_FBTableBase> CreateTableSediDefinitions()
        {
            List<_FBTableBase> tables = new List<_FBTableBase>();

            tables.Add(new FbtRPOINT());
            tables.Add(new FbtRHYDROMETER1());
            tables.Add(new FbtRHYDROMETER1CELL());
            tables.Add(new FbtRHYDROMETER2());
            tables.Add(new FbtRHYDROMETER2CELL());
            tables.Add(new FbtRSAND());
            tables.Add(new FbtRSANDDETAIL());
            tables.Add(new FbtRSANDPROFILE());
            tables.Add(new FbtSETUP());

            return tables;
        }


        public static FbConnectionStringBuilder SetFBConnStrBuilder()
        {
            FBDatabaseInfo info = new FBDatabaseInfo();
            RnsIni<DatabaseSetting> ini = new RnsIni<DatabaseSetting>(AFMSBuild.NAME);
            ini.Read(DatabaseSetting.DatabaseIP, out string address, "localhost");
            ini.Read(DatabaseSetting.DatabaseName, out string path, "D:\\RADS\\Database\\RADS.FDB");
            ini.ReadSilent(DatabaseSetting.DatabaseAccount, out string account, "rads");
            ini.ReadSilent(DatabaseSetting.DatabasePort, out string port, "3050");

            if (!int.TryParse(port, out int ppp))
            {
                ppp = 3050;
            }

            info.Address = address;
            info.Path = path;
            info.Account = account;
            info.Port = ppp;
            info.Pw = "rads2014";

            return FBConnectionString.GetConnectionString(info);
        }
    }
}
