public class PlayerEndRestEventHandler : GameEventHandler<RavenNest.Models.PlayerId>
{
    public override void Handle(GameManager gameManager, RavenNest.Models.PlayerId data)
    {
        var player = gameManager.Players.GetPlayerById(data.Id);
        if (!player) return;

        if (player.towerHandler != null && player.towerHandler.Resting)
        {
            player.towerHandler.StopResting();
            return;
        }

        if (!player.onsenHandler.InOnsen)
        {
            return;
        }
        player.onsenHandler.Exit();
    }
}
