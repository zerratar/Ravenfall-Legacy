using System.Collections.Generic;

public class ReceivedItems : OverlayPacketHandler<List<RavenNest.Models.Item>>
{
    public override void Handle(Overlay overlay, List<RavenNest.Models.Item> data)
    {
        overlay.ItemManager.SetItems(data);
        overlay.OnItemsLoaded(data);
    }
}
