using SqlParser.Ast;
using System;

public class PlayerTask : ChatBotCommandHandler<PlayerTaskRequest>
{
    public PlayerTask(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerTaskRequest task, GameMessage gm, GameClient client)
    {
        try
        {
            //var task = JsonConvert.DeserializeObject<PlayerTaskRequest>(packet.JsonData);
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (player == null || !player)
            {
                // player is not in game, try to add the player.
                //player = await Game.Players.JoinAsync(task.Player, client, true);
                //if (player == null)
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }

            var taskArgument = string.Empty;
            var targetLevel = 0;
            if (task.Arguments != null && task.Arguments.Length > 0)
            {
                taskArgument = task.Arguments[0];
                if (task.Arguments.Length > 1 && int.TryParse(task.Arguments[1], out var lv))
                {
                    targetLevel = lv;
                }
            }

            player.SetTask(task.Task, taskArgument);
            player.AutoTrainTargetLevel = targetLevel;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("PlayerTask.Handle: " + exc);
        }
    }
}