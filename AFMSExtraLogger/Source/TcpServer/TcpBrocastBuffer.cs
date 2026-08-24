using AFMSDll;
using System.Collections;

namespace AFMSExtraLogger
{
    public static class TcpBrocastBuffer
    {
        private static Queue<_PacketBase> _Queue = new Queue<_PacketBase>();
        private static readonly object lockObj = new object();
        public static _PacketBase GetDequeue()
        {
            lock (lockObj)
            {
                if (_Queue.TryDequeue(out _PacketBase temp))
                {
                    return temp;
                }
            }

            return null;
        }

        public static void Insert(_PacketBase temp)
        {
            lock (lockObj)
            {
                _Queue.Enqueue(temp);
            }
        }

        public static void WriteLog(string host, string msg)
        {
            ViewLogMsg log = new ViewLogMsg();
            log.LogHost = host;
            log.LogMsg = msg;

            lock (lockObj)
            {
                _Queue.Enqueue(log);
            }
        }
    }
}
