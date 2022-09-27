public class PlayerTaskEventHandler : GameEventHandler<RavenNest.Models.PlayerTask>
{
    public override void Handle(GameManager gameManager, RavenNest.Models.PlayerTask data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player) return;

        player.SetTask(data.Task, new string[] { data.TaskArgument });
    }
}
