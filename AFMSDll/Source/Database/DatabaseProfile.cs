using FirebirdSql.Data.FirebirdClient;

namespace AFMSDll
{
    /// <summary>
    /// 하나의 Firebird DB 접속 정보를 나타냅니다.
    /// Database는 접속 대상 서버에서 인식할 수 있는 DB 경로입니다.
    /// </summary>
    public sealed class DatabaseProfile
    {
        public string Name { get; }
        public string ConnectionString { get; }

        public DatabaseProfile(string name, string dataSource, int port, string database, string userId, string pw, string charset = "UTF8", bool pooling = true, int timeout = 10)
        {
            Name = name;

            FbConnectionStringBuilder builder = new FbConnectionStringBuilder();

            builder.DataSource = dataSource;
            builder.Port = port;
            builder.Database = database;
            builder.UserID = userId;
            builder.Password = pw;
            builder.Charset = charset;
            builder.Pooling = pooling;
            builder.ConnectionTimeout = timeout;

            // Builder 자체를 보관하지 않고 문자열로 복사합니다.
            ConnectionString = builder.ConnectionString;
        }

        public DatabaseProfile(string name, FbConnectionStringBuilder builder)
        {
            Name = name;

            // 이후 원본 builder가 변경돼도 영향을 받지 않습니다.
            ConnectionString = builder.ConnectionString;
        }
    }
}