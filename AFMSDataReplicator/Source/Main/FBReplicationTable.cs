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
        private readonly FBDatabase sourceDb;
        private readonly FBDatabase targetDb;
   
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
            
            if (!CompareResult.IsValid) return;

            Initialize();
        }

        public void CheckForeignKey()
        {
            const string CONSTRAINT_NAME = "CONSTRAINT_NAME";
            const string CONSTRAINT_TYPE = "CONSTRAINT_TYPE";
            const string INDEX_NAME = "INDEX_NAME";
            string sql = $"SELECT  TRIM(RC.RDB$CONSTRAINT_NAME) AS {CONSTRAINT_NAME},";
            sql += "\n" + $"TRIM(RC.RDB$CONSTRAINT_TYPE) AS {CONSTRAINT_TYPE},";
            sql += "\n" + $"TRIM(RC.RDB$INDEX_NAME) AS {INDEX_NAME}";
            sql += "\n" + $"FROM RDB$RELATION_CONSTRAINTS RC";
            sql += "\n" + $"WHERE RC.RDB$RELATION_NAME = '{TableName}'\r\nORDER BY RC.RDB${CONSTRAINT_NAME}";

            targetDb.RunQuery(sql);

            Logs.Clear();
            foreach (DataRow row in targetDb.Results.Rows)
            {
                string log = "";
                log += row[CONSTRAINT_NAME].ToString() + ", ";
                log += row[CONSTRAINT_TYPE].ToString() + ", ";
                log += row[INDEX_NAME].ToString();

                Logs.Add(log);

                if (row[CONSTRAINT_TYPE].ToString() == "FOREIGN KEY")
                {
                    Logs.Add($"외래키 삭제 => ALTER TABLE {TableName} DROP CONSTRAINT {row[CONSTRAINT_NAME].ToString()}");
                }
            }
        }

        private string GetColumnInfos(FBDatabase database, out List<FBColumnInfo> columns)
        {
            columns = new List<FBColumnInfo>();

            string query = $@"
SELECT
    TRIM(RF.RDB$FIELD_NAME) AS FIELD_NAME,
    COALESCE(F.RDB$FIELD_TYPE, 0) AS FIELD_TYPE,
    COALESCE(F.RDB$FIELD_SUB_TYPE, 0) AS FIELD_SUB_TYPE,
    COALESCE(F.RDB$FIELD_LENGTH, 0) AS FIELD_LENGTH,
    COALESCE(F.RDB$FIELD_SCALE, 0) AS FIELD_SCALE,
    COALESCE(F.RDB$CHARACTER_LENGTH, 0) AS CHAR_LENGTH_VALUE
FROM RDB$RELATION_FIELDS RF
JOIN RDB$FIELDS F ON F.RDB$FIELD_NAME = RF.RDB$FIELD_SOURCE
WHERE RF.RDB$RELATION_NAME = '{TableName.ToUpperInvariant()}'
ORDER BY RF.RDB$FIELD_POSITION";

            string error = database.RunQuery(query);

            if (!string.IsNullOrEmpty(error))
                return error;

            foreach (DataRow row in database.Results.Rows)
            {
                FBColumnInfo column = new()
                {
                    Name = row["FIELD_NAME"]?.ToString()?.Trim() ?? "",
                    FieldType = GetInt(row, "FIELD_TYPE"),
                    FieldSubType = GetInt(row, "FIELD_SUB_TYPE"),
                    FieldLength = GetInt(row, "FIELD_LENGTH"),
                    FieldScale = GetInt(row, "FIELD_SCALE"),
                    CharacterLength = GetInt(row, "CHAR_LENGTH_VALUE")
                };

                columns.Add(column);
            }

            return string.Empty;
        }

        private static int GetInt(DataRow row, string columnName)
        {
            if (row[columnName] == DBNull.Value)
                return 0;

            return Convert.ToInt32(row[columnName]);
        }

        public string DiagnoseTargetDatabase()
        {
            string error = targetDb.RunQuery("SELECT 1 AS TEST_VALUE FROM RDB$DATABASE");

            if (!string.IsNullOrEmpty(error))
                return $"Target DB connection failed.{Environment.NewLine}{error}";

            error = GetColumnInfos(sourceDb, out List<FBColumnInfo> sourceColumns);

            if (!string.IsNullOrEmpty(error))
                return error;

            error = GetColumnInfos(targetDb, out List<FBColumnInfo> targetColumns);

            if (!string.IsNullOrEmpty(error))
                return error;

            if (sourceColumns.Count == 0)
                return $"Source table does not exist: {TableName}";

            if (targetColumns.Count == 0)
                return $"Target table does not exist: {TableName}";

            if (sourceColumns.Count != targetColumns.Count)
                return $"Column count mismatch: {TableName} - Source={sourceColumns.Count}, Target={targetColumns.Count}";

            for (int i = 0; i < sourceColumns.Count; i++)
            {
                FBColumnInfo source = sourceColumns[i];
                FBColumnInfo target = targetColumns[i];

                if (!source.Name.Equals(target.Name, StringComparison.OrdinalIgnoreCase))
                    return $"Column name mismatch: {TableName} - Source={source.Name}, Target={target.Name}";

                if (source.FieldType != target.FieldType)
                    return $"Column type mismatch: {TableName}.{source.Name}";

                if (source.FieldSubType != target.FieldSubType)
                    return $"Column subtype mismatch: {TableName}.{source.Name}";

                if (source.FieldScale != target.FieldScale)
                    return $"Column scale mismatch: {TableName}.{source.Name}";

                if (source.CharacterLength != target.CharacterLength)
                    return $"Column length mismatch: {TableName}.{source.Name}";
            }

            return string.Empty;
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
            if (!TryGetNextRow(out DataRow? row, out error)) return false;
            if (row == null) return false;
            if (!TryGetMeasureDateTime(row, out DateTime measureDT)) return false;
            if (!TryReplicateRow(row, measureDT, out error)) return false;

            LastDT = measureDT;

            return true;
        }

        private bool Validate(out string error)
        {
            error = "";

            if (!string.IsNullOrEmpty(ErrorMsg))
            {
                Logs.Add($"ERROR [{TableName}] {ErrorMsg}");
                return false;
            }

            error = DiagnoseTargetDatabase();

            if (string.IsNullOrEmpty(error)) return true;

            Logs.Add($"ERROR [{TableName}] Target DB 진단 실패");
            Logs.Add(error);

            return false;
        }

        private bool TryGetNextRow(out DataRow? row, out string error)
        {
            row = null;

            string date = LastDT.ToString("yyyyMMdd");
            string time = LastDT.ToString("HHmmss");

            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = TableName;
            query.First = 1;

            foreach (string column in Columns) query.Add(column);

            query.Where(_FBTableBase.COL_MEASURE_DATE, ">", date);
            query.OrWhereRaw($"({_FBTableBase.COL_MEASURE_DATE} = '{date}' AND {_FBTableBase.COL_MEASURE_TIME} > '{time}')");
            query.OrderBy(_FBTableBase.COL_MEASURE_DATE, _FBTableBase.COL_MEASURE_TIME);

            DataTable table = sourceDb.Execute(query, out error);

            if (!string.IsNullOrEmpty(error))
            {
                Logs.Add($"ERROR [{TableName}] Source 데이터 조회 실패");
                Logs.Add(error);
                return false;
            }

            if (table.Rows.Count == 0) return true;

            row = table.Rows[0];

            return true;
        }

        private bool TryGetMeasureDateTime(DataRow row, out DateTime measureDT)
        {
            string date = row[_FBTableBase.COL_MEASURE_DATE]?.ToString()?.Trim() ?? "";
            string time = row[_FBTableBase.COL_MEASURE_TIME]?.ToString()?.Trim() ?? "";

            if (DateTime.TryParseExact(date + time, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out measureDT)) return true;

            Logs.Add($"ERROR [{TableName}] 측정시간 형식 오류 - MEASUREDATE={date}, MEASURETIME={time}");

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