using RavenNest.Models;
using UnityEngine;

public class PlayerJoinArenaEventHandler : GameEventHandler<PlayerId>
{
    public override void Handle(GameManager gameManager, PlayerId data)
    {
        var player = gameManager.Players.GetPlayerById(data.Id);
        if (player == null)
            return;

        if (gameManager.StreamRaid.IsWar)
            return;

        if (gameManager.Arena.Island != player.Island)
            return;

        if (player.ferryHandler.OnFerry)
            return;

        if (player.ferryHandler.Active)
            player.ferryHandler.BeginDisembark();

        if (!gameManager.Arena.CanJoin(player, out var alreadyJoined, out var alreadyStarted)
            || player.raidHandler.InRaid
            || player.duelHandler.InDuel)
            return;

        gameManager.Arena.Join(player);

        Shinobytes.Debug.Log($"PlayerJoinArenaEventHandler " + data.Id);
    }
}

