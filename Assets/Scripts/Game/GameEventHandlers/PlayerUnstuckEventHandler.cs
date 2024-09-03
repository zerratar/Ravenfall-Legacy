using RavenNest.Models;

public class PlayerUnstuckEventHandler : GameEventHandler<PlayerUnstuckMessage>
{
    public override void Handle(GameManager gameManager, PlayerUnstuckMessage data)
    {
        if (data.Ids == null || data.Ids.Length == 0) return;
        foreach (var id in data.Ids)
        {
            var player = gameManager.Players.GetPlayerById(id);
            if (!player) continue;
            player.Unstuck(true,0);
        }
    }
}
