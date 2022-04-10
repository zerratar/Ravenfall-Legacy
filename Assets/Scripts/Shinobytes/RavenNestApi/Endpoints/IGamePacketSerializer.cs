using System.Collections.Generic;
using System.IO;

namespace RavenNest.SDK.Endpoints
{
    public interface IGamePacketSerializer
    {
        byte[] Serialize(GamePacket packet);
        byte[] SerializeMany(IReadOnlyList<GamePacket> packetsToSend);
        void Serialize(BinaryWriter stream, GamePacket packet);

        GamePacket Deserialize(byte[] data);
        GamePacket Deserialize(byte[] data, int length);
    }
}