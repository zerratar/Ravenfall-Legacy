using RavenNest.Models;

public class ResourceUpdateEventHandler : GameEventHandler<ResourceUpdate>
{
    public override void Handle(GameManager gameManager, ResourceUpdate data)
    {
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        if (!player)
        {
            return;
        }

        player.Resources.Coins = data.CoinsAmount;
    }
}
