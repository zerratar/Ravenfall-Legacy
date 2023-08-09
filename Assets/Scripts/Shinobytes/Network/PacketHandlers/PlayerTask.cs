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
                {
                    client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                    return;
                }
            }
            
            var arg = task.Arguments != null && task.Arguments.Length > 0 ? task.Arguments[0] : null;
            player.SetTask(task.Task, arg);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
    }
}