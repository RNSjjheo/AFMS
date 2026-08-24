using System;
using System.Collections.Generic;

namespace AFMSDll
{
    public class QueryBuilderUpdate : QueryBuilderBase
    {
        private readonly List<string> _columns = new List<string>();
        private readonly List<string> _where = new List<string>();

        public QueryBuilderUpdate Set(string column)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("UPDATE할 컬럼이 비어 있습니다.", nameof(column));
            if (_columns.Contains(column)) throw new InvalidOperationException($"이미 추가된 UPDATE 컬럼입니다. {column}");

            _columns.Add(column);
            return this;
        }

        public QueryBuilderUpdate Where(string column)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("WHERE 컬럼이 비어 있습니다.", nameof(column));

            _where.Add($"{column} = @{column}");
            return this;
        }

        public QueryBuilderUpdate Where(string column, string parameter)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("WHERE 컬럼이 비어 있습니다.", nameof(column));
            if (string.IsNullOrWhiteSpace(parameter)) throw new ArgumentException("WHERE 파라미터가 비어 있습니다.", nameof(parameter));

            _where.Add($"{column} = @{parameter}");
            return this;
        }

        protected override string Build()
        {
            ValidateTable();

            if (_columns.Count == 0) throw new InvalidOperationException("UPDATE할 컬럼이 지정되지 않았습니다.");
            if (_where.Count == 0) throw new InvalidOperationException("UPDATE Query에는 WHERE 조건이 필요합니다.");

            List<string> sets = new List<string>();
            foreach (string column in _columns) sets.Add($"{column} = @{column}");

            string sql = $"UPDATE {Table}";
            sql += "\n" + $"SET {string.Join(", ", sets)}";
            sql += "\n" + $"WHERE {string.Join(" AND ", _where)}";

            return sql;
        }
    }
}
