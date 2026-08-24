using FirebirdSql.Data.FirebirdClient;
using RnsLibrary;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace AFMSDll
{
    public class FBConnectionString
    {
        public static FbConnectionStringBuilder GetConnectionString(FBDatabaseInfo info)
        {
            var connectionStringBuilder = new FbConnectionStringBuilder
            {
                DataSource = info.Address,
                Database = info.Path,
                UserID = info.Account,
                Password = info.Pw,
                Port = info.Port,
                Charset = "UTF8",
                Dialect = 3,
                Pooling = true
            };

            return connectionStringBuilder;
        }
    }
}
