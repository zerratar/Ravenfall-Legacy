using Newtonsoft.Json;
using System;

namespace RavenNest.SDK.Endpoints
{
    internal class PartialGamePacket
    {
        private readonly IGamePacketSerializer packetSerializer;
        private byte[] array;
        private int count;

        public PartialGamePacket(IGamePacketSerializer packetSerializer, byte[] array, int count)
        {
            this.packetSerializer = packetSerializer;
            this.array = array;
            this.count = count;
        }

        internal void Append(byte[] array, int count)
        {
            var tmpArray = new byte[this.count + count];
            Array.Copy(this.array, 0, tmpArray, 0, this.count);
            Array.Copy(array, 0, tmpArray, this.count - 1, count);
            this.array = tmpArray;
        }

        internal GamePacket Build()
        {
            return packetSerializer.Deserialize(array);
        }
    }
}