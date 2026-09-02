using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public abstract class _FBTableBase
    {
        public const string COL_ID = "ID";
        public const string COL_MEASURE_DATE = "MeasureDate";
        public const string COL_MEASURE_TIME = "MeasureTime";
        public const string SQL_MEASURE_DATETIME = "(" + COL_MEASURE_DATE + " || ' ' || " + COL_MEASURE_TIME + ")";
        public abstract string GetTableName();
        public abstract string GetCreateTableSql();

        public virtual string CheckNewColumn(FBDatabase db)
        {
            return "";
        }

        public virtual string CheckNewIndexes(FBDatabase db)
        {
            return "";
        }

        public virtual List<string>? GetDefaultInsertSql()
        {
            return null;
        }

        public virtual List<string>? GetExampleSql()
        {
            return null;
        }

        public bool HasColumn(FBDatabase db, string columnName)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (string.IsNullOrWhiteSpace(columnName)) throw new ArgumentException("컬럼명이 없습니다.", nameof(columnName));

            string tableName = GetTableName().Replace("'", "''");
            string fieldName = columnName.Replace("'", "''");

            string sql = "SELECT COUNT(*)";
            sql += "\n" + "FROM RDB$RELATION_FIELDS";
            sql += "\n" + $"WHERE UPPER(TRIM(RDB$RELATION_NAME)) = UPPER('{tableName}')";
            sql += "\n" + $"AND UPPER(TRIM(RDB$FIELD_NAME)) = UPPER('{fieldName}')";

            db.RunQuery(sql);

            if (db.Results.Rows.Count == 0) return false;

            return Convert.ToInt32(db.Results.Rows[0][0]) > 0;
        }

        public string AddColumn(FBDatabase db, string columnName, string columnType)
        {
            string tableName = GetTableName().Replace("'", "''");

            string sql = $"ALTER TABLE {tableName} ADD {columnName} {columnType}";
            return db.RunQuery(sql);
        }

        protected string EnsureIndex(FBDatabase db, string indexName, params string[] columns)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentException("인덱스명이 없습니다.", nameof(indexName));
            if (columns == null || columns.Length == 0 || columns.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("인덱스 컬럼이 없습니다.", nameof(columns));

            string escapedIndexName = indexName.Replace("'", "''");
            string sql = "SELECT COUNT(*) FROM RDB$INDICES";
            sql += $" WHERE UPPER(TRIM(RDB$INDEX_NAME)) = UPPER('{escapedIndexName}')";
            string error = db.RunQuery(sql);
            if (!string.IsNullOrEmpty(error)) return error;
            if (db.Results.Rows.Count > 0 && Convert.ToInt32(db.Results.Rows[0][0]) > 0) return string.Empty;

            sql = $"CREATE INDEX {indexName} ON {GetTableName()} ({string.Join(", ", columns)})";
            return db.RunNonQuery(sql);
        }

        protected static string GetEnumCheckClause<TEnum>(string columnName)
            where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("CHECK 제약조건 컬럼명이 없습니다.", nameof(columnName));

            string values = string.Join(", ", Enum.GetNames<TEnum>().Select(value => $"'{value.Replace("'", "''")}'"));

            return $"CONSTRAINT CK_{columnName} CHECK ({columnName} IN ({values}))";
        }
    }
}
