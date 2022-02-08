using System.Collections.Generic;

public class ReceivedRedeemables : OverlayPacketHandler<List<RavenNest.Models.RedeemableItem>>
{
    public override void Handle(Overlay overlay, List<RavenNest.Models.RedeemableItem> data)
    {
        overlay.ItemManager.SetRedeemableItems(data);
        overlay.OnRedeemablesLoaded(data);
    }
}
