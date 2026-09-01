using AFMSDll;
using AFMSSediService;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Globalization;

namespace AFMSSediService
{
    internal sealed class RadsReplicationRepository
    {
        private static readonly string[] HeaderColumns =
        [
            "MEASUREDATE", "MEASURETIME", "HYDROKIND",
            "AVGVELOCITY", "MINVELOCITY", "MAXVELOCITY",
            .. Enumerable.Range(1, 40).Select(number => $"VALUE{number:00}"),
            "RAWDATA"
        ];

        private static readonly string[] CellColumns =
        [
            "MEASUREDATE", "MEASURETIME", "CELLNO",
            .. Enumerable.Range(1, 40).Select(number => $"VALUE{number:00}")
        ];

        private readonly string remoteConnectionString;
        private readonly string localConnectionString;
        private readonly int batchSize;
        private readonly string startTimestamp;

        public RadsReplicationRepository(RadsReplicationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            DatabaseProfile remoteProfile = new DatabaseProfile(
                "Remote RADS",
                options.RemoteHost,
                options.RemotePort,
                options.RemoteDatabase,
                options.UserId,
                options.Password,
                options.Charset,
                true,
                options.ConnectionTimeoutSeconds);

            remoteConnectionString = remoteProfile.ConnectionString;
            localConnectionString = FBProvider.Instance.ConnStrBuilder.ConnectionString;
            batchSize = Math.Clamp(options.BatchSize, 1, 1000);
            startTimestamp = options.StartTime.ToString(
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture);
        }

        public int ReplicateDevice(int deviceNumber)
        {
            string headerTable = GetHeaderTable(deviceNumber);
            string cellTable = GetCellTable(deviceNumber);
            string cursor = LoadLocalCursor(headerTable);
            if (string.CompareOrdinal(cursor, startTimestamp) < 0)
                cursor = startTimestamp;

            using FbConnection remote = new FbConnection(remoteConnectionString);
            remote.Open();

            DataTable headers = LoadRemoteHeaders(remote, headerTable, cursor);
            int replicated = 0;

            foreach (DataRow header in headers.Rows)
            {
                string measureDate = GetKey(header, "MEASUREDATE");
                string measureTime = GetKey(header, "MEASURETIME");
                DataTable cells = LoadRemoteCells(
                    remote,
                    cellTable,
                    measureDate,
                    measureTime);

                int expectedCellCount = GetExpectedCellCount(header);
                if (expectedCellCount > 0 && cells.Rows.Count != expectedCellCount)
                    break;

                SaveMeasurement(
                    headerTable,
                    cellTable,
                    header,
                    cells);
                replicated++;
            }

            return replicated;
        }

        private string LoadLocalCursor(string tableName)
        {
            using FbConnection connection = new FbConnection(localConnectionString);
            connection.Open();
            using FbCommand command = connection.CreateCommand();
            command.CommandText =
                $"SELECT MAX(MEASUREDATE || MEASURETIME) FROM {tableName}";
            object? value = command.ExecuteScalar();
            return value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }

        private DataTable LoadRemoteHeaders(
            FbConnection connection,
            string tableName,
            string cursor)
        {
            using FbCommand command = connection.CreateCommand();
            command.CommandText =
                $"SELECT FIRST {batchSize} {string.Join(", ", HeaderColumns)} " +
                $"FROM {tableName} " +
                "WHERE (MEASUREDATE || MEASURETIME) > @CURSOR " +
                "ORDER BY MEASUREDATE, MEASURETIME";
            command.Parameters.AddWithValue("@CURSOR", cursor);

            using FbDataReader reader = command.ExecuteReader();
            DataTable result = new DataTable();
            result.Load(reader);
            return result;
        }

        private static DataTable LoadRemoteCells(
            FbConnection connection,
            string tableName,
            string measureDate,
            string measureTime)
        {
            using FbCommand command = connection.CreateCommand();
            command.CommandText =
                $"SELECT {string.Join(", ", CellColumns)} FROM {tableName} " +
                "WHERE MEASUREDATE = @MEASUREDATE AND MEASURETIME = @MEASURETIME " +
                "ORDER BY CELLNO";
            command.Parameters.AddWithValue("@MEASUREDATE", measureDate);
            command.Parameters.AddWithValue("@MEASURETIME", measureTime);

            using FbDataReader reader = command.ExecuteReader();
            DataTable result = new DataTable();
            result.Load(reader);
            return result;
        }

        private void SaveMeasurement(
            string headerTable,
            string cellTable,
            DataRow header,
            DataTable cells)
        {
            using FbConnection connection = new FbConnection(localConnectionString);
            connection.Open();
            using FbTransaction transaction = connection.BeginTransaction();

            try
            {
                UpsertRow(connection, transaction, headerTable, HeaderColumns, header,
                    ["MEASUREDATE", "MEASURETIME"]);

                foreach (DataRow cell in cells.Rows)
                {
                    UpsertRow(connection, transaction, cellTable, CellColumns, cell,
                        ["MEASUREDATE", "MEASURETIME", "CELLNO"]);
                }

                transaction.Commit();
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                }

                throw;
            }
        }

        private static void UpsertRow(
            FbConnection connection,
            FbTransaction transaction,
            string tableName,
            IReadOnlyList<string> columns,
            DataRow row,
            IReadOnlyList<string> matchingColumns)
        {
            using FbCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"UPDATE OR INSERT INTO {tableName} ({string.Join(", ", columns)}) " +
                $"VALUES ({string.Join(", ", columns.Select(column => $"@{column}"))}) " +
                $"MATCHING ({string.Join(", ", matchingColumns)})";

            foreach (string column in columns)
                command.Parameters.AddWithValue($"@{column}", row[column]);

            command.ExecuteNonQuery();
        }

        private static int GetExpectedCellCount(DataRow header)
        {
            object value = header["VALUE02"];
            if (value == DBNull.Value) return 0;
            return Math.Max(0, Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        private static string GetKey(DataRow row, string columnName)
        {
            string value = Convert.ToString(
                row[columnName],
                CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"원격 RADS 자료의 {columnName} 값이 비어 있습니다.");
            return value;
        }

        private static string GetHeaderTable(int deviceNumber) =>
            deviceNumber switch
            {
                1 => "RHYDROMETER1",
                2 => "RHYDROMETER2",
                _ => throw new ArgumentOutOfRangeException(nameof(deviceNumber))
            };

        private static string GetCellTable(int deviceNumber) =>
            deviceNumber switch
            {
                1 => "RHYDROMETER1CELL",
                2 => "RHYDROMETER2CELL",
                _ => throw new ArgumentOutOfRangeException(nameof(deviceNumber))
            };
    }
}
