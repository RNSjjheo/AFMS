using AFMSDll;
using log4net;
using System.Collections;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AFMSExtraLogger
{
    public class DiagnosticsOwner: Diagnotics
    {
        private static readonly ILog Log = LogManager.GetLogger("DIAG");
        private static readonly DiagnosticsOwner instance = new DiagnosticsOwner();
        private static readonly object lockObj = new object();

        public static DiagnosticsOwner Instance
        {
            get
            {
                return instance;
            }
        }

        public void UpdateCurrentProcessMemoryMB()
        {
            using Process process = Process.GetCurrentProcess();

            // 최신 값으로 갱신
            process.Refresh();

            // 실제 메모리 사용량, byte 단위
            long memoryBytes = process.WorkingSet64;

            lock (lockObj)
            {
                // MB 변환
                MemoryUsage = memoryBytes / 1024.0 / 1024.0;

                if (MemoryUsageMax < MemoryUsage)
                {
                    MemoryUsageMax = MemoryUsage;
                }

                if (MemoryUsageMin > MemoryUsage || MemoryUsageMin == 0)
                {
                    MemoryUsageMin = MemoryUsage;
                }
            }

            TcpBrocastBuffer.Insert((Diagnotics)this);
        }
    }
}
