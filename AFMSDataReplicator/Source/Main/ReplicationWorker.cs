using AFMSDll;
using FirebirdSql.Data.FirebirdClient;
using log4net;
using Microsoft.VisualBasic.Logging;
using RnsLibrary;
using System.Data;
using System.Drawing.Interop;
using System.Globalization;
using System.Security.Cryptography.Xml;

namespace AFMSDataReplicator
{
    public class ReplicationWorker() : BackgroundService
    {
        private static readonly ILog Log = LogManager.GetLogger("SYS");
        private FBDatabase _dbRemote;
        private FBDatabase _dbLocal;
        private ReplicatorInfo RepVthInfo = new ReplicatorInfo();
        private ReplicatorInfo RepLevInfo = new ReplicatorInfo();
        private FbConnectionStringBuilder RemoteStrBuilder = null!;
        private List<FBReplicationTable> _tables = new();
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime pretime = DateTime.Now;
            TimeSpan diff = DateTime.Now - pretime;

            List<string> logs = [];
            Log.Info("START Worker");

            SetInfo();

            bool copyed = false;

            while (!stoppingToken.IsCancellationRequested)
            {
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

        private void SetInfo()
        {
            string sql = $"SELECT FIRST 1 ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_MEASURE_DATE}, ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_MEASURE_TIME}, ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_ENABLE_VTH}, ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_ENABLE_LEVEL}, ";
            sql += $"\n" + $"{FbtAFMSReplicatorSetting.COL_TARGET_IP}";
            sql += $"\n" + $"FROM {FbtAFMSReplicatorSetting.TABLE_NAME}";
            sql += $"\n" + $"ORDER BY {FbtAFMSReplicatorSetting.COL_ID} DESC";

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                string begindate = row[FbtAFMSReplicatorSetting.COL_MEASURE_DATE].ToString();
                string begintime = row[FbtAFMSReplicatorSetting.COL_MEASURE_TIME].ToString();
                string targetip = row[FbtAFMSReplicatorSetting.COL_TARGET_IP].ToString();

                string begin = begindate + " " + begintime;
                DateTime saveDT = DateTime.ParseExact(begin, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture);

                RemoteStrBuilder = SetFBConnStrBuilder(targetip);

                _dbRemote = new FBDatabase(RemoteStrBuilder);
                _dbLocal = new FBDatabase(FBProvider.Instance.ConnStrBuilder);

                if (row[FbtAFMSReplicatorSetting.COL_ENABLE_VTH].ToString() != "0")
                {
                    FBReplicationTable table = new FBReplicationTable(_dbRemote, _dbLocal, FbtVTHLOGGER.TABLE_NAME, saveDT);

                    InsertValidTable(table);
                }

                if (row[FbtAFMSReplicatorSetting.COL_ENABLE_LEVEL].ToString() != "0")
                {
                    FBReplicationTable table  = new FBReplicationTable(_dbRemote, _dbLocal, FbtWATERLEVEL.TABLE_NAME, saveDT);

                    InsertValidTable(table);
                }
            }
        }

        private void InsertValidTable(FBReplicationTable table)
        {
            Log.Info($"[{table.TableName}] 데이터 복제 유효성 확인");

            table.CheckForeignKey();
            PrintReplicateLog(table);

            if (!table.CompareResult.IsValid)
            {
                foreach (FBSurveyDifference diff in table.CompareResult.Differences)
                {
                    Log.Info($"[{table.TableName}] {diff.Type.ToString()}, {diff.ColumnName}, {diff.LocalValue}");
                }
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

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
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
    }
}
