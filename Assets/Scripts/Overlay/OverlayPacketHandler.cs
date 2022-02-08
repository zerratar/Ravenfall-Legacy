using System;

public abstract class OverlayPacketHandler<TData> : IOverlayPacketHandler
{
    public void Handle(Overlay overlay, OverlayPacket packet)
    {
        try
        {
            this.Handle(overlay, packet.GetValue<TData>());
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Failed to handle overlay packet: " + exc);
        }
    }

    public abstract void Handle(Overlay overlay, TData data);
}
