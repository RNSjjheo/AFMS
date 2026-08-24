using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AFMSDll
{
    public class FBExample
    {
        public string SqlInsert { get; private set; } = "";
        public string SqlUpdate { get; private set; } = "";
        public string ErrorMsg { get; private set; } = "";

        public FBExample(FBDatabase database, string tablename)
        {
            if (string.IsNullOrWhiteSpace(tablename))
            {
                ErrorMsg = "Table name is empty.";
                return;
            }

            List<string> columns = GetColumns(database, tablename);

            if (!string.IsNullOrEmpty(ErrorMsg) || columns.Count == 0) return;

            List<string> primaryKeys = GetPrimaryKeys(database, tablename);

            if (!string.IsNullOrEmpty(ErrorMsg)) return;

            SqlInsert = CreateInsertSql(tablename, columns);
            SqlUpdate = CreateUpdateSql(tablename, columns, primaryKeys);
        }

        private List<string> GetColumns(FBDatabase database, string tablename)
        {
            List<string> result = new();

            string query = $@"
SELECT TRIM(RF.RDB$FIELD_NAME) AS FIELD_NAME
FROM RDB$RELATION_FIELDS RF
WHERE RF.RDB$RELATION_NAME = '{tablename.ToUpperInvariant()}'
ORDER BY RF.RDB$FIELD_POSITION";

            string error = database.RunQuery(query);

            if (!string.IsNullOrEmpty(error))
            {
                ErrorMsg = error;
                return result;
            }

            foreach (DataRow row in database.Results.Rows)
            {
                string columnName = row["FIELD_NAME"]?.ToString()?.Trim() ?? "";

                if (!string.IsNullOrWhiteSpace(columnName))
                    result.Add(columnName);
            }

            return result;
        }

        private List<string> GetPrimaryKeys(FBDatabase database, string tablename)
        {
            List<string> result = new();

            string query = $@"
SELECT TRIM(S.RDB$FIELD_NAME) AS FIELD_NAME
FROM RDB$RELATION_CONSTRAINTS RC
JOIN RDB$INDEX_SEGMENTS S ON S.RDB$INDEX_NAME = RC.RDB$INDEX_NAME
WHERE RC.RDB$RELATION_NAME = '{tablename.ToUpperInvariant()}'
AND RC.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'
ORDER BY S.RDB$FIELD_POSITION";

            string error = database.RunQuery(query);

            if (!string.IsNullOrEmpty(error))
            {
                ErrorMsg = error;
                return result;
            }

            foreach (DataRow row in database.Results.Rows)
            {
                string columnName = row["FIELD_NAME"]?.ToString()?.Trim() ?? "";

                if (!string.IsNullOrWhiteSpace(columnName))
                    result.Add(columnName);
            }

            return result;
        }

        private static string CreateInsertSql(string tablename, List<string> columns)
        {
            if (columns.Count == 0)
                return "";

            string columnText = string.Join(", ", columns);
            string valueText = string.Join(", ", columns.Select(x => $"@{x}"));

            return $"INSERT INTO {tablename} \n({columnText})\n VALUES \n({valueText})";
        }

        private static string CreateUpdateSql(string tablename, List<string> columns, List<string> primaryKeys)
        {
            if (columns.Count == 0 || primaryKeys.Count == 0) return "";

            List<string> updateColumns = columns.Where(x => !primaryKeys.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();

            if (updateColumns.Count == 0) return "";

            string setText = string.Join(", ", updateColumns.Select(x => $"{x} = @{x}"));
            string whereText = string.Join(" AND ", primaryKeys.Select(x => $"{x} = @{x}"));

            return $"UPDATE {tablename} SET \n{setText}\n WHERE {whereText}";
        }
    }
}