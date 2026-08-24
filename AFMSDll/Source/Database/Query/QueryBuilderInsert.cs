using System;
using System.Collections.Generic;
using System.Data;

namespace AFMSDll
{
    public class QueryBuilderInsert : QueryBuilderBase
    {
        private readonly List<string> _columns = new List<string>();
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();
        private string _autoIncrement = string.Empty;

        public string AutoIncrement
        {
            get => _autoIncrement;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _autoIncrement = string.Empty;
                    return;
                }

                if (_columns.Contains(value)) throw new InvalidOperationException($"이미 INSERT 컬럼으로 추가된 컬럼입니다. {value}");

                _autoIncrement = value;
            }
        }

        internal override IReadOnlyList<string>? Columns => _columns;

        internal override DataRow? Row
        {
            get
            {
                DataTable table = new DataTable();

                foreach (string column in _columns) table.Columns.Add(column, _types[column]);

                DataRow row = table.NewRow();
                foreach (string column in _columns) row[column] = _values[column];
                table.Rows.Add(row);

                return row;
            }
        }

        public QueryBuilderInsert Value(string column, int value)
        {
            return AddValue(column, value, typeof(int));
        }

        public QueryBuilderInsert Value(string column, int? value)
        {
            return AddValue(column, value.HasValue ? (object)value.Value : DBNull.Value, typeof(int));
        }

        public QueryBuilderInsert Value(string column, double value)
        {
            return AddValue(column, value, typeof(double));
        }

        public QueryBuilderInsert Value(string column, double? value)
        {
            return AddValue(column, value.HasValue ? (object)value.Value : DBNull.Value, typeof(double));
        }

        public QueryBuilderInsert Value(string column, string? value)
        {
            return AddValue(column, value ?? (object)DBNull.Value, typeof(string));
        }

        public QueryBuilderInsert Value(string column, object value, Type type)
        {
            return AddValue(column, value, type);
        }

        protected override string Build()
        {
            ValidateTable();

            if (_columns.Count == 0 && string.IsNullOrEmpty(_autoIncrement))
                throw new InvalidOperationException("INSERT할 컬럼이 지정되지 않았습니다.");

            List<string> columns = new List<string>();
            List<string> values = new List<string>();

            if (!string.IsNullOrEmpty(_autoIncrement))
            {
                columns.Add(_autoIncrement);
                values.Add($"(SELECT COALESCE(MAX({_autoIncrement}), 0) + 1 FROM {Table})");
            }

            foreach (string column in _columns)
            {
                columns.Add(column);
                values.Add($"@{column}");
            }

            string sql = $"INSERT INTO {Table} ({string.Join(", ", columns)})";
            sql += "\n" + $"VALUES ({string.Join(", ", values)})";

            return sql;
        }

        private QueryBuilderInsert AddValue(string column, object value, Type type)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("INSERT할 컬럼이 비어 있습니다.", nameof(column));

            if (string.Equals(_autoIncrement, column, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"자동증가 컬럼은 Value()로 추가할 수 없습니다. {column}");

            if (_columns.Contains(column)) throw new InvalidOperationException($"이미 추가된 INSERT 컬럼입니다. {column}");

            _columns.Add(column);
            _values.Add(column, value);
            _types.Add(column, type);

            return this;
        }
    }
}
