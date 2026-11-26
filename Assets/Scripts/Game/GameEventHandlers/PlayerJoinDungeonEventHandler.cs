using RavenNest.Models;

public class PlayerJoinDungeonEventHandler : GameEventHandler<PlayerId>
{
    public override void Handle(GameManager gameManager, PlayerId data)
    {
        var player = gameManager.Players.GetPlayerById(data.Id);
        if (!player) return;
        if (player.InRavebridge) return;
        // anyone held where they are stays, but a climber may choose the dungeon: they leave the tower when it starts
        if (player.HeldForEvents && !player.Climbing) return;

        if (gameManager.StreamRaid.IsWar)
            return;

        if (!gameManager.Dungeons.Active)
            return;

        if (gameManager.Dungeons.Started)
            return;

        if (gameManager.Dungeons.CanJoin(player) == DungeonJoinResult.CanJoin)
            gameManager.Dungeons.Join(player);
    }
}

