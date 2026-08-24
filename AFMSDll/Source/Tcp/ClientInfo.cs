using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed record ClientInfo(Guid Id, string RemoteEndPoint);

    public sealed record TcpPacket(byte Command, byte[] Data);
}
