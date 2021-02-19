using System;
using System.Linq;

public class PlayerTaskEventHandler : GameEventHandler<RavenNest.Models.PlayerTask>
{
    protected override void Handle(GameManager gameManager, RavenNest.Models.PlayerTask data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player) return;


        if (player.Ferry && player.Ferry.Active)
            player.Ferry.Disembark();

        if (gameManager.Arena && gameManager.Arena.HasJoined(player) && !gameManager.Arena.Leave(player))
            return;

        if (player.Duel.InDuel)
            return;

        gameManager.Raid.Leave(player);

        if (gameManager.Arena.HasJoined(player))
            gameManager.Arena.Leave(player);

        var taskArgs = data.TaskArgument;
        var task = data.Task;

        player.SetTask(task, taskArgs);
    }
}

