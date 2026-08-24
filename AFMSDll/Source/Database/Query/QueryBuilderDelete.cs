using System;
using System.Collections.Generic;

namespace AFMSDll
{
    public class QueryBuilderDelete : QueryBuilderBase
    {
        private readonly List<string> _where = new List<string>();

        public QueryBuilderDelete Where(string column)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("WHERE 컬럼이 비어 있습니다.", nameof(column));

            _where.Add($"{column} = @{column}");
            return this;
        }

        public QueryBuilderDelete Where(string column, string parameter)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("WHERE 컬럼이 비어 있습니다.", nameof(column));
            if (string.IsNullOrWhiteSpace(parameter)) throw new ArgumentException("WHERE 파라미터가 비어 있습니다.", nameof(parameter));

            _where.Add($"{column} = @{parameter}");
            return this;
        }

        protected override string Build()
        {
            ValidateTable();

            if (_where.Count == 0) throw new InvalidOperationException("DELETE Query에는 WHERE 조건이 필요합니다.");

            string sql = $"DELETE FROM {Table}";
            sql += "\n" + $"WHERE {string.Join(" AND ", _where)}";

            return sql;
        }
    }
}
