using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AFMSDll
{

    public enum JsonPacketType
    {
        Heartbeat = 0x00,
        ViewerLogMsg = 0x10,
        Diagnotics = 0x50
    }

    public class _PacketBase
    {
        private bool disposedValue;

        [JsonPropertyOrder(-3)]
        public JsonPacketType JsonType { get; set; }
        [JsonPropertyOrder(-2)]
        public string ClientId { get; set;  }

        [JsonPropertyOrder(-1)]
        public DateTime SendingTime { get; set; }


        public string GetJsonString()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            };

            return JsonSerializer.Serialize(
                this,
                GetType(),
                options);
        }
    }
}
