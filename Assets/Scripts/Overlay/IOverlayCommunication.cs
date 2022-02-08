namespace Assets.Scripts.Overlay
{
    public interface IOverlayCommunication
    {
        void Send(OverlayPacket data, bool replaceExistingOfSameType = false);
        bool TryRead(out OverlayPacket packet);
    }
}
