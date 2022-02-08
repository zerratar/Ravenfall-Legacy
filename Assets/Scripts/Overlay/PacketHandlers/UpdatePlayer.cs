using System;

public class UpdatePlayer : OverlayPacketHandler<OverlayPlayer>
{
    public override void Handle(Overlay overlay, OverlayPlayer data)
    {
        if (!overlay.ItemManager.Loaded)
        {
            // we are not ready to spawn players yet.
            // and since we will get a nother UpdatePlayer later on, its fine if we ignore it now.
            return;
        }

        var wasSpawn = overlay.Players.AddOrUpdate(data, out var player);
        if (wasSpawn)
        {
            overlay.OnShowPlayer(data, player);
        }
        else
        {
            overlay.OnPlayerUpdated(data, player);
        }
    }
}


public class ClearPlayer : OverlayPacketHandler<string>
{
    public override void Handle(Overlay overlay, string data)
    {
        overlay.OnClearDisplayedPlayer();
    }
}
