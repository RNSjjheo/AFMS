using AFMSDll;
using FirebirdSql.Data.FirebirdClient;
using log4net;
using RnsLibrary;
using System.Data;
using System.Globalization;

namespace AFMSDataReplicator
{
    public class ReplicationWorker() : BackgroundService
    {
        private static readonly TimeSpan SettingsRefreshInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan InitializationRetryInterval = TimeSpan.FromSeconds(30);
        private static readonly ILog Log = LogManager.GetLogger("SYS");

        private sealed record ReplicationSettingSnapshot(
            int Id,
            DateTime BeginDateTime,
            string TargetIp,
            bool EnableVth,
            bool EnableLevel);

        private FBDatabase? _dbRemote;
        private FBDatabase? _dbLocal;
        private ReplicationSettingSnapshot? _currentSetting;
        private DateTime _nextSettingsRefreshUtc = DateTime.MinValue;
        private readonly List<FBReplicationTable> _tables = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime pretime = DateTime.Now;
            TimeSpan diff = DateTime.Now - pretime;

            Log.Info("START Worker");

            bool copyed = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= _nextSettingsRefreshUtc)
                {
                    try
                    {
                        RefreshSettingsAndTables();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("복제 설정 및 테이블 초기화 중 예외가 발생했습니다. 재시도합니다.", ex);
                    }

                    _nextSettingsRefreshUtc = DateTime.UtcNow.Add(NeedsInitializationRetry()
                        ? InitializationRetryInterval
                        : SettingsRefreshInterval);
                }

                copyed = false;

                foreach (FBReplicationTable table in _tables)
                {
                    if (table.Replicate()) copyed = true;

                    PrintReplicateLog(table);
                }

                if(!copyed) await Task.Delay(5000, stoppingToken);

                diff = DateTime.Now - pretime;

                if (diff.TotalSeconds < 60) continue;

                pretime = DateTime.Now;
                foreach (FBReplicationTable table in _tables)
                {
                    Log.Info($"[{table.TableName}] 모니터링중~ {table.LastDT.ToString("yyyy-MM-dd HH:mm:ss")}");
                }
            }
        }

        private bool RefreshSettingsAndTables()
        {
            string sql = $"SELECT FIRST 1 ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_ID}, ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_MEASURE_DATE}, ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_MEASURE_TIME}, ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_ENABLE_VTH}, ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_ENABLE_LEVEL}, ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_TARGET_IP}";
            sql += $"\n" + $"FROM {FbtAFMSReplicatorSetting.TABLE_NAME}";
            sql += $"\n" + $"ORDER BY {FbtAFMSReplicatorSetting.COL_ID} DESC";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            DataTable settings = db.Execute(sql, out string error);

            if (!string.IsNullOrEmpty(error))
            {
                Log.Error($"복제 설정 조회 실패{Environment.NewLine}{error}");
                return false;
            }

            if (settings.Rows.Count == 0)
            {
                Log.Error("복제 설정이 없습니다.");
                return false;
            }

            DataRow row = settings.Rows[0];
            string beginText = $"{row[FbtAFMSReplicatorSetting.COL_MEASURE_DATE]} {row[FbtAFMSReplicatorSetting.COL_MEASURE_TIME]}";
            string targetIp = row[FbtAFMSReplicatorSetting.COL_TARGET_IP]?.ToString()?.Trim() ?? string.Empty;

            if (!DateTime.TryParseExact(beginText, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime beginDateTime))
            {
                Log.Error($"복제 시작 시각 형식이 올바르지 않습니다. {beginText}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetIp))
            {
                Log.Error("원격 DB IP가 설정되지 않았습니다.");
                return false;
            }

            ReplicationSettingSnapshot setting;

            try
            {
                setting = new ReplicationSettingSnapshot(
                    Convert.ToInt32(row[FbtAFMSReplicatorSetting.COL_ID]),
                    beginDateTime,
                    targetIp,
                    Convert.ToInt32(row[FbtAFMSReplicatorSetting.COL_ENABLE_VTH]) != 0,
                    Convert.ToInt32(row[FbtAFMSReplicatorSetting.COL_ENABLE_LEVEL]) != 0);
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
            {
                Log.Error($"복제 설정 값이 올바르지 않습니다. {ex.Message}");
                return false;
            }

            if (_currentSetting != setting)
            {
                ResetReplicationResources();
                _currentSetting = setting;
                Log.Info($"복제 설정을 적용합니다. ID={setting.Id}, Target={setting.TargetIp}, VTH={setting.EnableVth}, Level={setting.EnableLevel}");
            }

            if (_dbRemote == null || _dbLocal == null)
            {
                FbConnectionStringBuilder remoteConnection = SetFBConnStrBuilder(setting.TargetIp);
                _dbRemote = new FBDatabase(remoteConnection);
                _dbLocal = FBProvider.Instance.CreateDatabase();
            }

            EnsureReplicationTables(setting);
            return !NeedsInitializationRetry();
        }

        private void EnsureReplicationTables(ReplicationSettingSnapshot setting)
        {
            if (_dbRemote == null || _dbLocal == null) return;

            if (setting.EnableVth && !ContainsTable(FbtVTHLOGGER.TABLE_NAME))
            {
                InsertValidTable(new FBReplicationTable(_dbRemote, _dbLocal, FbtVTHLOGGER.TABLE_NAME, setting.BeginDateTime));
            }

            if (setting.EnableLevel && !ContainsTable(FbtWATERLEVEL.TABLE_NAME))
            {
                InsertValidTable(new FBReplicationTable(_dbRemote, _dbLocal, FbtWATERLEVEL.TABLE_NAME, setting.BeginDateTime));
            }
        }

        private bool NeedsInitializationRetry()
        {
            if (_currentSetting == null || _dbRemote == null || _dbLocal == null) return true;
            if (_currentSetting.EnableVth && !ContainsTable(FbtVTHLOGGER.TABLE_NAME)) return true;
            if (_currentSetting.EnableLevel && !ContainsTable(FbtWATERLEVEL.TABLE_NAME)) return true;
            return false;
        }

        private bool ContainsTable(string tableName)
        {
            return _tables.Exists(table => string.Equals(table.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        }

        private void InsertValidTable(FBReplicationTable table)
        {
            Log.Info($"[{table.TableName}] 데이터 복제 유효성 확인");

            bool foreignKeysDropped = table.DropTargetForeignKeys();
            PrintReplicateLog(table);

            if (!foreignKeysDropped)
            {
                Log.Info($"[{table.TableName}] 로컬 외래키를 제거하지 못해 복제를 시작하지 않습니다.");
                return;
            }

            if (!table.CompareResult.IsValid || !string.IsNullOrEmpty(table.ErrorMsg))
            {
                foreach (FBSurveyDifference diff in table.CompareResult.Differences)
                {
                    Log.Info($"[{table.TableName}] {diff.Type.ToString()}, {diff.ColumnName}, {diff.LocalValue}");
                }
                if (!string.IsNullOrEmpty(table.ErrorMsg)) Log.Error($"[{table.TableName}] {table.ErrorMsg}");
                Log.Info($"[{table.TableName}] 유효성 확인 실패");
                return;
            }

            Log.Info($"[{table.TableName}] 유효성 확인 성공");

            UpdateLastDT(table);

            Log.Info($"[{table.TableName}] 복제 시작 마지막 데이터: {table.LastDT.ToString("yyyyMMdd HHmmss")}");


            _tables.Add(table);
        }

        private void UpdateLastDT(FBReplicationTable table)
        {
            const string MAX_DATETIME = "MAX_DATETIME";

            string sql = $"SELECT";
            sql += $"\n" + $"MAX({_FBTableBase.COL_MEASURE_DATE} || ' ' || {_FBTableBase.COL_MEASURE_TIME}) AS {MAX_DATETIME}";
            sql += $"\n" + $"FROM {table.TableName}";
            sql += $"\n" + $"ORDER BY {MAX_DATETIME} DESC";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                try
                {
                    string saveStr = row[MAX_DATETIME].ToString();

                    DateTime saveDT = DateTime.ParseExact(saveStr, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture);

                    if (saveDT > table.LastDT) table.LastDT = saveDT;
                }
                catch
                { 
                
                }
            }
        }

        private void PrintReplicateLog(FBReplicationTable table)
        {
            if (table.Logs.Count == 0) return;

            foreach (string log in table.Logs)
            {
                Log.Info($"[{table.TableName}] {log}");
            }
        }

        public FbConnectionStringBuilder SetFBConnStrBuilder(string remoteip)
        {
            FBDatabaseInfo info = new FBDatabaseInfo();
            RnsIni<DatabaseSetting> ini = new RnsIni<DatabaseSetting>(AFMSBuild.NAME);
            ini.Read(DatabaseSetting.DatabaseName, out string path, "D:\\RADS\\Database\\RADS.FDB");
            ini.ReadSilent(DatabaseSetting.DatabaseAccount, out string account, "rads");
            ini.ReadSilent(DatabaseSetting.DatabasePort, out string port, "3050");

            if (!int.TryParse(port, out int ppp))
            {
                ppp = 3050;
            }

            info.Address = remoteip;
            info.Path = path;
            info.Account = account;
            info.Port = ppp;
            info.Pw = "rads2014";

            return FBConnectionString.GetConnectionString(info);
        }

        private void ResetReplicationResources()
        {
            _tables.Clear();
            _dbRemote?.Dispose();
            _dbLocal?.Dispose();
            _dbRemote = null;
            _dbLocal = null;
        }

        public override void Dispose()
        {
            ResetReplicationResources();
            base.Dispose();
        }
    }
}
