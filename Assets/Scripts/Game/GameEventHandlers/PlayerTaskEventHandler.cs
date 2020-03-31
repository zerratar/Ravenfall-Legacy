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

        var type = Enum
            .GetValues(typeof(TaskType))
            .Cast<TaskType>()
            .FirstOrDefault(x => x.ToString().Equals(data.Task, StringComparison.InvariantCultureIgnoreCase));

        gameManager.Raid.Leave(player);

        if (gameManager.Arena.HasJoined(player))
            gameManager.Arena.Leave(player);

        var taskArgs = new string[] { data.TaskArgument };

        player.SetTaskArguments(taskArgs);
        player.GotoClosest(type);
    }
}

