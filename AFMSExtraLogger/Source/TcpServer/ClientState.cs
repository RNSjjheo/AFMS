using AFMSDll;
using System.Diagnostics;

namespace AFMSExtraLogger
{ 
    public class ClientState
    {
        public DateTime TimeConnected;
        public DateTime TimeLastHeartbeat;

        public ClientState(ClientInfo client)
        {
            Client = client;

        }

        public ClientInfo Client { get; }

        public string? DeviceId { get; set; }
    }

}
