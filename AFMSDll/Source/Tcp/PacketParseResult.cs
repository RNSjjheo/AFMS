using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public enum PacketParseResult
    {
        NeedMoreData,
        PacketReceived,
        InvalidData
    }
}
