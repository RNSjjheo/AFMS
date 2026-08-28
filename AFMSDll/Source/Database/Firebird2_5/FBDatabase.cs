using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AFMSDll
{
    public class FBDatabase : IDisposable
    {
        private readonly string connectionString;
        private readonly object syncRoot = new object();
        private bool disposed;

        public string ErrorMsg;
        public DataTable Results { get; private set; }

        public FBDatabase(FbConnectionStringBuilder connectionStringBuilder)
            : this(connectionStringBuilder?.ConnectionString
                ?? throw new ArgumentNullException(nameof(connectionStringBuilder)))
        {
        }

        public FBDatabase(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("DB 연결 문자열이 비어 있습니다.", nameof(connectionString));
            }

            this.connectionString = connectionString;
            Results = new DataTable();
        }

        public DataTable Execute(string query, out string error)
        {
            return ExecuteCore(query, IsResultQuery(query), null, null, out error);
        }

        public DataTable Execute(QueryBuilderBase query, out string error)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return ExecuteCore(query.Sql, query.ReturnsRows, query.Row, query.Columns, out error);
        }

        public string RunQuery(string query)
        {
            Execute(query, out string error);
            return error;
        }

        public string RunNonQuery(string query)
        {
            ExecuteCore(query, false, null, null, out string error);
            return error;
        }

        private DataTable ExecuteCore(string query, bool returnsRows, DataRow? row, IReadOnlyList<string>? columns, out string error)
        {
            ThrowIfDisposed();

            DataTable table = new DataTable();
            error = string.Empty;

            lock (syncRoot)
            {
                ThrowIfDisposed();

                using FbConnection connection = new FbConnection(connectionString);

                try
                {
                    connection.Open();

                    using FbTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        using FbCommand command = new FbCommand(query, connection, transaction);

                        AddParameters(command, row, columns);

                        if (returnsRows)
                        {
                            using FbDataReader reader = command.ExecuteReader();
                            table.Load(reader);
                        }
                        else
                        {
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }

                        error = BuildErrorLog("Run", query, ex, row, columns);
                    }
                }
                catch (Exception ex)
                {
                    error = BuildErrorLog("Run", query, ex, row, columns);
                }
            }

            SetResults(table);
            return table;
        }

        private static void AddParameters(FbCommand command, DataRow? row, IReadOnlyList<string>? columns)
        {
            if (row == null || columns == null) return;

            foreach (string column in columns)
            {
                object value = row[column];
                command.Parameters.AddWithValue($"@{column}", value == DBNull.Value ? DBNull.Value : value);
            }
        }

        private void SetResults(DataTable table)
        {
            Results.Dispose();
            Results = table;
        }

        private static bool IsResultQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;

            string sql = query.TrimStart();
            return sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                   sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildErrorLog(string methodName, string query, Exception ex, DataRow? row, IReadOnlyList<string>? columns)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Error {methodName}: {ex}");
            sb.AppendLine("Error Query:");
            sb.AppendLine(query);

            if (row != null && columns != null)
            {
                sb.AppendLine("Parameters:");

                foreach (string column in columns)
                {
                    object value = row[column];
                    string text = value == DBNull.Value ? "NULL" : value?.ToString() ?? "";
                    sb.AppendLine($"@{column} = {text} ({(value == DBNull.Value ? "NULL" : value?.GetType().Name ?? "NULL")})");
                }
            }

            return sb.ToString();
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (disposed) return;

                Results.Dispose();
                disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(FBDatabase));
        }
    }
}
