using System;
using System.Collections.Concurrent;

public class OverlayPacketManager : IOverlayPacketManager
{
    private ConcurrentDictionary<string, IOverlayPacketHandler> packetHandlers
        = new ConcurrentDictionary<string, IOverlayPacketHandler>();

    private Overlay overlay;

    public event EventHandler<OverlayPacket> PacketHandled;
    public OverlayPacketManager(Overlay overlay)
    {
        this.overlay = overlay;
    }

    public void Register<T>(string name = null) where T : IOverlayPacketHandler, new()
    {
        if (string.IsNullOrEmpty(name))
        {
            name = typeof(T).Name;
        }

        packetHandlers[name] = new T();
    }

    public bool TryGet(string name, out IOverlayPacketHandler packetHandler)
    {
        return packetHandlers.TryGetValue(name, out packetHandler);
    }

    public bool TryHandle(OverlayPacket packet)
    {
        if (TryGet(packet.Name, out var handler))
        {
            handler.Handle(overlay, packet);
            if (PacketHandled != null)
            {
                PacketHandled.Invoke(this, packet);
            }
            return true;
        }
        return false;
    }
}
