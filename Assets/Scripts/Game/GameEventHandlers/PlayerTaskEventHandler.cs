public class PlayerTaskEventHandler : GameEventHandler<RavenNest.Models.PlayerTask>
{
    public override void Handle(GameManager gameManager, RavenNest.Models.PlayerTask data)
    {
        var player = gameManager.Players.GetPlayerById(data.PlayerId);
        if (!player) return;

        player.SetTask(data.Task, new string[] { data.TaskArgument });
    }
}
