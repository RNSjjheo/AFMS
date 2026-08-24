using System;
using System.Collections.Generic;
using System.Data;

namespace AFMSDll
{
    public abstract class QueryBuilderBase
    {
        public string Table { get; set; } = string.Empty;
        public string Sql => Build();

        internal virtual bool ReturnsRows => false;
        internal virtual DataRow? Row => null;
        internal virtual IReadOnlyList<string>? Columns => null;

        public override string ToString()
        {
            return Sql;
        }

        protected abstract string Build();

        protected void ValidateTable()
        {
            if (string.IsNullOrWhiteSpace(Table)) throw new InvalidOperationException("Table이 지정되지 않았습니다.");
        }
    }
}
