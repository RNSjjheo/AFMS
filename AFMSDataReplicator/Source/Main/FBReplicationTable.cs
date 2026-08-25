using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace AFMSDataReplicator
{
    public class FBReplicationTable
    {
        private const int ReplicationBatchSize = 20;
        private static readonly TimeSpan SchemaValidationInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan SchemaValidationRetryInterval = TimeSpan.FromSeconds(30);

        private readonly FBDatabase sourceDb;
        private readonly FBDatabase targetDb;
        private DateTime _nextSchemaValidationUtc;
   
        public string TableName { get; private set; }
        public List<string> Columns { get; private set; } = new();
        public string SqlInsert { get; private set; } = "";
        public string ErrorMsg { get; private set; } = "";
        public DateTime LastDT;
        private FBCompareTable Compare;
        public FBSurveyTableValidationResult CompareResult;
        public List<string> Logs = new();
        public FBReplicationTable(FBDatabase sourceDatabase, FBDatabase targetDatabase, string tableName, DateTime lastDT)
        {
            sourceDb = sourceDatabase;
            targetDb = targetDatabase;
            TableName = tableName;
            LastDT = lastDT;

            Compare = new FBCompareTable(sourceDb, targetDb, TableName);

            CompareResult = Compare.Validate();
            SetNextSchemaValidationTime();
            
            if (!CompareResult.IsValid) return;

            Initialize();
        }

        public bool DropTargetForeignKeys()
        {
            const string CONSTRAINT_NAME = "CONSTRAINT_NAME";
            string sql = $"SELECT TRIM(RC.RDB$CONSTRAINT_NAME) AS {CONSTRAINT_NAME}";
            sql += "\n" + $"FROM RDB$RELATION_CONSTRAINTS RC";
            sql += "\n" + $"WHERE RC.RDB$RELATION_NAME = '{TableName.Replace("'", "''")}'";
            sql += "\n" + $"AND RC.RDB$CONSTRAINT_TYPE = 'FOREIGN KEY'";
            sql += "\n" + $"ORDER BY RC.RDB$CONSTRAINT_NAME";

            string error = targetDb.RunQuery(sql);

            Logs.Clear();

            if (!string.IsNullOrEmpty(error))
            {
                Logs.Add($"ERROR [{TableName}] 로컬 외래키 조회 실패");
                Logs.Add(error);
                return false;
            }

            List<string> constraints = targetDb.Results.Rows
                .Cast<DataRow>()
                .Select(row => row[CONSTRAINT_NAME]?.ToString()?.Trim() ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            if (constraints.Count == 0)
            {
                Logs.Add($"[{TableName}] 로컬 외래키 없음");
                return true;
            }

            foreach (string constraint in constraints)
            {
                string dropSql = $"ALTER TABLE {QuoteIdentifier(TableName)} DROP CONSTRAINT {QuoteIdentifier(constraint)}";
                error = targetDb.RunNonQuery(dropSql);

                if (!string.IsNullOrEmpty(error))
                {
                    Logs.Add($"ERROR [{TableName}] 로컬 외래키 삭제 실패 - {constraint}");
                    Logs.Add(error);
                    return false;
                }

                Logs.Add($"[{TableName}] 로컬 외래키 삭제 완료 - {constraint}");
            }

            return true;
        }

        private static string QuoteIdentifier(string identifier)
        {
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        private void Initialize()
        {
            if (string.IsNullOrWhiteSpace(TableName))
            {
                ErrorMsg = "Table name is empty.";
                return;
            }

            Columns = GetColumns();

            if (!string.IsNullOrEmpty(ErrorMsg)) return;

            if (Columns.Count == 0)
            {
                ErrorMsg = $"Table column not found: {TableName}";
                return;
            }

            SqlInsert = CreateInsertSql();
        }

        private List<string> GetColumns()
        {
            List<string> result = new();

            string query = $@"
SELECT TRIM(RF.RDB$FIELD_NAME) AS FIELD_NAME
FROM RDB$RELATION_FIELDS RF
WHERE RF.RDB$RELATION_NAME = '{TableName.ToUpperInvariant()}'
ORDER BY RF.RDB$FIELD_POSITION";

            string error = sourceDb.RunQuery(query);

            if (!string.IsNullOrEmpty(error))
            {
                ErrorMsg = error;
                return result;
            }

            foreach (DataRow row in sourceDb.Results.Rows)
            {
                string columnName = row["FIELD_NAME"]?.ToString()?.Trim() ?? "";

                if (!string.IsNullOrWhiteSpace(columnName)) result.Add(columnName);
            }

            return result;
        }

        private string CreateInsertSql()
        {
            if (Columns.Count == 0) return "";

            string columnText = string.Join(", ", Columns);
            string parameterText = string.Join(", ", Columns.Select(x => $"@{x}"));

            return $"INSERT INTO {TableName} (\n{columnText}\n) VALUES (\n{parameterText}\n)";
        }

        public bool Replicate()
        {
            Logs.Clear();

            if (!Validate(out string error)) return false;
            if (!TryGetNextRows(out DataTable rows, out error)) return false;

            bool replicated = false;

            foreach (DataRow row in rows.Rows)
            {
                if (!TryGetMeasureDateTime(row, out DateTime measureDT)) continue;

                // DB 오류 행을 건너뛰고 커서를 이동하면 해당 데이터가 영구 누락될 수 있으므로 중단한다.
                if (!TryReplicateRow(row, measureDT, out error)) break;

                LastDT = measureDT;
                replicated = true;
            }

            return replicated;
        }

        private bool Validate(out string error)
        {
            error = "";

            if (!string.IsNullOrEmpty(ErrorMsg))
            {
                Logs.Add($"ERROR [{TableName}] {ErrorMsg}");
                return false;
            }

            bool validatedNow = false;

            if (DateTime.UtcNow >= _nextSchemaValidationUtc)
            {
                CompareResult = Compare.Validate();
                SetNextSchemaValidationTime();
                validatedNow = true;
            }

            if (CompareResult.IsValid) return true;

            if (validatedNow)
            {
                Logs.Add($"ERROR [{TableName}] 원격/로컬 테이블 구조 검증 실패");

                foreach (FBSurveyDifference difference in CompareResult.Differences)
                {
                    Logs.Add($"{difference.Type}, {difference.ColumnName}, Remote={difference.RemoteValue}, Local={difference.LocalValue}");
                }
            }

            return false;
        }

        private void SetNextSchemaValidationTime()
        {
            TimeSpan interval = CompareResult.IsValid ? SchemaValidationInterval : SchemaValidationRetryInterval;
            _nextSchemaValidationUtc = DateTime.UtcNow.Add(interval);
        }

        private bool TryGetNextRows(out DataTable rows, out string error)
        {
            string date = LastDT.ToString("yyyyMMdd");
            string time = LastDT.ToString("HHmmss");

            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = TableName;
            query.First = ReplicationBatchSize;

            foreach (string column in Columns) query.Add(column);

            query.Where(_FBTableBase.COL_MEASURE_DATE, ">", date);
            query.OrWhereRaw($"({_FBTableBase.COL_MEASURE_DATE} = '{date}' AND {_FBTableBase.COL_MEASURE_TIME} > '{time}')");
            query.OrderBy(_FBTableBase.COL_MEASURE_DATE, _FBTableBase.COL_MEASURE_TIME);

            rows = sourceDb.Execute(query, out error);

            if (!string.IsNullOrEmpty(error))
            {
                Logs.Add($"ERROR [{TableName}] Source 데이터 조회 실패");
                Logs.Add(error);
                return false;
            }

            return true;
        }

        private bool TryGetMeasureDateTime(DataRow row, out DateTime measureDT)
        {
            string date = row[_FBTableBase.COL_MEASURE_DATE]?.ToString()?.Trim() ?? "";
            string time = row[_FBTableBase.COL_MEASURE_TIME]?.ToString()?.Trim() ?? "";

            if (DateTime.TryParseExact(date + time, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out measureDT) &&
                measureDT.Minute % 10 == 0 && measureDT.Second == 0) return true;

            Logs.Add($"ERROR [{TableName}] 측정시간 형식 또는 10분 정각 조건 오류 - MEASUREDATE={date}, MEASURETIME={time}");

            return false;
        }

        private bool TryReplicateRow(DataRow row, DateTime measureDT, out string error)
        {
            string date = measureDT.ToString("yyyyMMdd");
            string time = measureDT.ToString("HHmmss");

            error = IsTargetExists(date, time, out bool exists);

            if (!string.IsNullOrEmpty(error))
            {
                Logs.Add($"ERROR [{TableName}] Target 데이터 확인 실패");
                Logs.Add(error);
                return false;
            }

            if (exists) return true;

            QueryBuilderInsert query = CreateInsertQuery(row);

            targetDb.Execute(query, out error);

            if (!string.IsNullOrEmpty(error))
            {
                Logs.Add($"ERROR [{TableName}] 데이터 복제 실패 - MEASUREDATE={date}, MEASURETIME={time}");
                Logs.Add(error);
                return false;
            }

            Logs.Add($"[{TableName}] 복제 완료 {CreateDataLog(row)}");

            return true;
        }

        private QueryBuilderInsert CreateInsertQuery(DataRow row)
        {
            QueryBuilderInsert query = new QueryBuilderInsert();
            query.Table = TableName;

            foreach (string column in Columns)
            {
                object value = row[column];
                Type type = row.Table.Columns[column].DataType;

                query.Value(column, value, type);
            }

            return query;
        }

        private string CreateDataLog(DataRow row)
        {
            List<string> values = new();

            foreach (string column in Columns)
            {
                object value = row[column];
                string valueText = value == DBNull.Value ? "NULL" : value.ToString() ?? "";

                values.Add($"{column}={valueText}");
            }

            return string.Join(", ", values);
        }

        private string IsTargetExists(string measureDate, string measureTime, out bool exists)
        {
            exists = false;

            string sql = $"SELECT FIRST 1";
            sql += "\n" + $"{_FBTableBase.COL_MEASURE_DATE}";
            sql += "\n" + $"FROM {TableName}";
            sql += "\n" + $"WHERE  {_FBTableBase.COL_MEASURE_DATE}='{measureDate}'";
            sql += "\n" + $"AND  {_FBTableBase.COL_MEASURE_TIME}='{measureTime}'";

            string error = targetDb.RunQuery(sql);

            if (!string.IsNullOrEmpty(error)) return error;

            exists = targetDb.Results.Rows.Count > 0;

            return string.Empty;
        }

        private string GetTargetLastPosition(out string? lastDate, out string? lastTime)
        {
            lastDate = null;
            lastTime = null;

            string query = $@"
SELECT FIRST 1
MEASUREDATE,
MEASURETIME
FROM {TableName}
ORDER BY MEASUREDATE DESC, MEASURETIME DESC";

            string error = targetDb.RunQuery(query);

            if (!string.IsNullOrEmpty(error)) return error;

            if (targetDb.Results.Rows.Count == 0) return string.Empty;

            DataRow row = targetDb.Results.Rows[0];

            lastDate = row[_FBTableBase.COL_MEASURE_DATE]?.ToString()?.Trim();
            lastTime = row[_FBTableBase.COL_MEASURE_TIME]?.ToString()?.Trim();

            return string.Empty;
        }
    }
}
