namespace RavenNest.SDK.Endpoints
{
    public interface IGamePacketSerializer
    {
        byte[] Serialize(GamePacket packet);
        GamePacket Deserialize(byte[] data);
        GamePacket Deserialize(byte[] data, int length);

    }
}