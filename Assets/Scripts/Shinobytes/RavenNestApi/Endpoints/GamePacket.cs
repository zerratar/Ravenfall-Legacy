using Newtonsoft.Json;
using System;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace RavenNest.SDK.Endpoints
{
    public class GamePacket
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public object Data { get; set; }
        public Guid CorrelationId { get; set; }

        internal bool TryGetValue<T>(out T result)
        {
            if (Data is T res)
            {
                result = res;
                return true;
            }

            result = default;
            return false;
        }
    }

    public class PlayerGamePacketRef
    {
        public GamePacket Packet { get; set; }
        public string Key { get; set; }
        public Guid Sender { get; set; }
        public DateTime Created { get; set; }
        public int SendIndex { get; set; }
    }
}