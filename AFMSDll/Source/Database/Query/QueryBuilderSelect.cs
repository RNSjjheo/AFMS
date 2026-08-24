using System;
using System.Globalization;
using System.Collections.Generic;

namespace AFMSDll
{
    public class QueryBuilderSelect : QueryBuilderBase
    {
        private const string MAIN_ALIAS = "A";
        private const string JOIN_B_ALIAS = "B";

        internal override bool ReturnsRows => true;

        private readonly List<string> _columns = new List<string>();
        private readonly List<(string Logic, string Condition)> _where = new List<(string Logic, string Condition)>();
        private readonly List<string> _orderBy = new List<string>();
        private int _first;

        public QueryLeftJoin LeftJoinB { get; } = new QueryLeftJoin(JOIN_B_ALIAS, MAIN_ALIAS);

        public int First
        {
            get => _first;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "First는 0 이상이어야 합니다.");
                _first = value;
            }
        }

        public QueryBuilderSelect Add(string column)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("SELECT 컬럼이 비어 있습니다.", nameof(column));

            _columns.Add(Qualify(column, MAIN_ALIAS));
            return this;
        }

        public QueryBuilderSelect AddB(string column)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("SELECT 컬럼이 비어 있습니다.", nameof(column));

            _columns.Add(Qualify(column, JOIN_B_ALIAS));
            return this;
        }

        public QueryBuilderSelect AsAlias(string column, string alias)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("SELECT 컬럼이 비어 있습니다.", nameof(column));
            if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("별칭이 비어 있습니다.", nameof(alias));

            _columns.Add($"{Qualify(column, MAIN_ALIAS)} AS \"{alias.Replace("\"", "\"\"")}\"");
            return this;
        }

        public QueryBuilderSelect AsAliasB(string column, string alias)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("SELECT 컬럼이 비어 있습니다.", nameof(column));
            if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("별칭이 비어 있습니다.", nameof(alias));

            _columns.Add($"{Qualify(column, JOIN_B_ALIAS)} AS \"{alias.Replace("\"", "\"\"")}\"");
            return this;
        }

        public QueryBuilderSelect Where(string column, string op, string value)
        {
            return AddWhere("AND", column, op, FormatValue(value));
        }

        public QueryBuilderSelect Where(string column, string op, int value)
        {
            return AddWhere("AND", column, op, value.ToString(CultureInfo.InvariantCulture));
        }

        public QueryBuilderSelect Where(string column, string op, double value)
        {
            return AddWhere("AND", column, op, value.ToString(CultureInfo.InvariantCulture));
        }

        public QueryBuilderSelect OrWhere(string column, string op, string value)
        {
            return AddWhere("OR", column, op, FormatValue(value));
        }

        public QueryBuilderSelect OrWhere(string column, string op, int value)
        {
            return AddWhere("OR", column, op, value.ToString(CultureInfo.InvariantCulture));
        }

        public QueryBuilderSelect OrWhere(string column, string op, double value)
        {
            return AddWhere("OR", column, op, value.ToString(CultureInfo.InvariantCulture));
        }

        public QueryBuilderSelect WhereRaw(string condition)
        {
            return AddWhereRaw("AND", condition);
        }

        public QueryBuilderSelect OrWhereRaw(string condition)
        {
            return AddWhereRaw("OR", condition);
        }

        public QueryBuilderSelect OrderBy(params string[] columns)
        {
            if (columns == null || columns.Length == 0) throw new ArgumentException("ORDER BY 컬럼이 지정되지 않았습니다.", nameof(columns));

            foreach (string column in columns)
            {
                if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("ORDER BY 컬럼이 비어 있습니다.", nameof(columns));
                _orderBy.Add(Qualify(column, MAIN_ALIAS));
            }

            return this;
        }

        public QueryBuilderSelect OrderByDesc(string column)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("ORDER BY 컬럼이 비어 있습니다.", nameof(column));

            _orderBy.Add($"{Qualify(column, MAIN_ALIAS)} DESC");
            return this;
        }

        protected override string Build()
        {
            ValidateTable();

            if (_columns.Count == 0) throw new InvalidOperationException("SELECT할 컬럼이 지정되지 않았습니다.");

            string sql = "SELECT";
            if (_first > 0) sql += $" FIRST {_first}";
            sql += $" {string.Join(", ", _columns)}";
            sql += "\n" + $"FROM {Table} {MAIN_ALIAS}";

            string leftJoinB = LeftJoinB.Build();
            if (!string.IsNullOrEmpty(leftJoinB)) sql += "\n" + leftJoinB;

            if (_where.Count > 0)
            {
                string where = _where[0].Condition;

                for (int i = 1; i < _where.Count; i++) where += $" {_where[i].Logic} {_where[i].Condition}";

                sql += "\n" + $"WHERE {where}";
            }

            if (_orderBy.Count > 0) sql += "\n" + $"ORDER BY {string.Join(", ", _orderBy)}";

            return sql;
        }

        private static string Qualify(string column, string alias)
        {
            string value = column.Trim();

            if (value.Contains('.') || value.Contains('(') || value.Contains(' ') || value.Contains("'") || value.Contains('"')) return value;

            return $"{alias}.{value}";
        }

        private QueryBuilderSelect AddWhere(string logic, string column, string op, string value)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("WHERE 컬럼이 비어 있습니다.", nameof(column));

            ValidateOperator(op);
            _where.Add((logic, $"{column} {op} {value}"));
            return this;
        }

        private QueryBuilderSelect AddWhereRaw(string logic, string condition)
        {
            if (string.IsNullOrWhiteSpace(condition)) throw new ArgumentException("WHERE 조건이 비어 있습니다.", nameof(condition));

            _where.Add((logic, condition));
            return this;
        }

        private static void ValidateOperator(string op)
        {
            string[] validOperators = { "=", "!=", "<>", ">", ">=", "<", "<=" };

            if (Array.IndexOf(validOperators, op) < 0) throw new ArgumentException($"지원하지 않는 비교 연산자입니다. {op}", nameof(op));
        }

        private static string FormatValue(string value)
        {
            return $"'{value.Replace("'", "''")}'";
        }

        public sealed class QueryLeftJoin
        {
            private readonly string _alias;
            private readonly string _mainAlias;
            private readonly List<string> _conditions = new List<string>();

            internal QueryLeftJoin(string alias, string mainAlias)
            {
                _alias = alias;
                _mainAlias = mainAlias;
            }

            public string Table { get; set; } = string.Empty;

            public QueryLeftJoin Add(string column, string op, string mainColumn)
            {
                if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("JOIN 컬럼이 비어 있습니다.", nameof(column));
                if (string.IsNullOrWhiteSpace(mainColumn)) throw new ArgumentException("메인 테이블 컬럼이 비어 있습니다.", nameof(mainColumn));

                ValidateJoinOperator(op);
                _conditions.Add($"{_alias}.{column} {op} {_mainAlias}.{mainColumn}");
                return this;
            }

            public QueryLeftJoin AddRaw(string condition)
            {
                if (string.IsNullOrWhiteSpace(condition)) throw new ArgumentException("JOIN 조건이 비어 있습니다.", nameof(condition));

                _conditions.Add(condition);
                return this;
            }

            internal string Build()
            {
                if (string.IsNullOrWhiteSpace(Table)) return string.Empty;
                if (_conditions.Count == 0) throw new InvalidOperationException($"{_alias} JOIN 조건이 지정되지 않았습니다.");

                return $"LEFT JOIN {Table} {_alias} ON {string.Join(" AND ", _conditions)}";
            }

            private static void ValidateJoinOperator(string op)
            {
                string[] validOperators = { "=", "!=", "<>", ">", ">=", "<", "<=" };

                if (Array.IndexOf(validOperators, op) < 0) throw new ArgumentException($"지원하지 않는 JOIN 비교 연산자입니다. {op}", nameof(op));
            }
        }
    }
}
