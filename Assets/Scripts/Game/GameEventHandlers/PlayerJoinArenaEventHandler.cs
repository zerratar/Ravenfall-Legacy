using RavenNest.Models;
using UnityEngine;

public class PlayerJoinArenaEventHandler : GameEventHandler<PlayerId>
{
    protected override void Handle(GameManager gameManager, PlayerId data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (player == null)
            return;

        if (gameManager.StreamRaid.IsWar)
            return;

        if (gameManager.Arena.Island != player.Island)
            return;

        if (player.Ferry.OnFerry)
            return;

        if (player.Ferry.Active)
            player.Ferry.Disembark();

        if (!gameManager.Arena.CanJoin(player, out var alreadyJoined, out var alreadyStarted)
            || player.Raid.InRaid
            || player.Duel.InDuel)
            return;

        gameManager.Arena.Join(player);

        GameManager.Log($"PlayerJoinArenaEventHandler " + data.UserId);
    }
}

