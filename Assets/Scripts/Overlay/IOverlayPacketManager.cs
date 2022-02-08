using System;

public interface IOverlayPacketManager
{
    event EventHandler<OverlayPacket> PacketHandled;
    void Register<T>(string name = null) where T : IOverlayPacketHandler, new();
    bool TryGet(string name, out IOverlayPacketHandler packetHandler);
    bool TryHandle(OverlayPacket packet);
}