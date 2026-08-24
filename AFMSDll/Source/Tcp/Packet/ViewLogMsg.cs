using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class ViewLogMsg: _PacketBase
    {
        public string LogHost;
        public string LogMsg;
        public ViewLogMsg()
        {
            JsonType = JsonPacketType.ViewerLogMsg;
        }
    }
}
