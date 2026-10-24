using RavenNest.SDK.Endpoints;

/// <summary>The stream's tavern shelves changed on the server: a listing made, edited, bought from, taken down or expired.</summary>
public class StreamerShopUpdatedEventHandler : GameEventHandler<StreamerShopInfo>
{
    public override void Handle(GameManager gameManager, StreamerShopInfo data)
    {
        TavernShop.Apply(data);
    }
}
