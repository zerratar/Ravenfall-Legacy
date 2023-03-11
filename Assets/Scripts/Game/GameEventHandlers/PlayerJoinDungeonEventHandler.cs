﻿using RavenNest.Models;

public class PlayerJoinDungeonEventHandler : GameEventHandler<PlayerId>
{
    public override void Handle(GameManager gameManager, PlayerId data)
    {
        var player = gameManager.Players.GetPlayerById(data.Id);
        if (!player) return;

        if (gameManager.StreamRaid.IsWar)
            return;
        if (player.Ferry.OnFerry)
            return;


        if (player.Ferry.Active)
            player.Ferry.Disembark();

        if (!gameManager.Dungeons.Active)
            return;

        if (gameManager.Dungeons.Started)
            return;

        if (gameManager.Dungeons.CanJoin(player) == DungeonJoinResult.CanJoin)
            gameManager.Dungeons.Join(player);
    }
}

