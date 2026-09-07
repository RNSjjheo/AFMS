using AFMSDll;
using RnsLibrary;
using System.Data;

namespace AFMSLoggerVideoHydorsem
{
    public class Configuration
    {
        private static readonly Configuration instance = new Configuration();

        public static Configuration Instance
        {
            get
            {
                return instance;
            }
        }

        public string SiteCode { get; private set; } = "unknown";
        public int WebPort { get; private set; }
        public string WebPath { get; private set; } = "upload";

        public void Setup()
        {
            SiteCode = SetSiteCode();
            WebPort = SetWebPort();
            WebPath = SetWebVisionPath();
        }

        private string SetSiteCode()
        {
            string sitecode = "unknown";
            string sql = $"SELECT {FbtSETUP.COL_VALUE01} FROM {FbtSETUP.TABLE_NAME}";
            sql += $" WHERE {FbtSETUP.COL_PK1}='1' AND {FbtSETUP.COL_PK2}='1'";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                object? val = row[FbtSETUP.COL_VALUE01];
                if (val == null || val == DBNull.Value) continue;
                sitecode = val?.ToString() ?? "unknown";
            }

            return sitecode;
        }

        private int SetWebPort()
        {
            int port = 0;
            string s = "";
            RnsIni<WebConfig> webconfig = new RnsIni<WebConfig>(AFMSBuild.NAME);
            webconfig.Read(WebConfig.WebPort, out s, "8000");

            // s가 null일 수 있으므로 안전하게 기본값 적용
            if (string.IsNullOrWhiteSpace(s)) s = "8000";

            if (!int.TryParse(s, out int n)) port = 8000;
            else port = n;

            return port;
        }

        private string SetWebVisionPath()
        {
            string path = "";
            RnsIni<WebConfig> webconfig = new RnsIni<WebConfig>(AFMSBuild.NAME);
            webconfig.Read(WebConfig.WebVisionPath, out path, "upload");

            // path가 null일 수 있으니 기본값 사용
            return path ?? "upload";
        }
    }
}
