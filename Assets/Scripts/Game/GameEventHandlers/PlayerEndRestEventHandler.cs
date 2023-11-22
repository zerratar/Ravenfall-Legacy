public class PlayerEndRestEventHandler : GameEventHandler<RavenNest.Models.PlayerId>
{
    public override void Handle(GameManager gameManager, RavenNest.Models.PlayerId data)
    {
        var player = gameManager.Players.GetPlayerById(data.Id);
        if (!player) return;

        if (!player.onsenHandler.InOnsen)
        {
            return;
        }

        gameManager.Onsen.Leave(player);
    }
}
