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

    public override async void Handle(PlayerTaskRequest task, GameClient client)//(Packet packet)
    {
        try
        {
            //var task = JsonConvert.DeserializeObject<PlayerTaskRequest>(packet.JsonData);
            var player = PlayerManager.GetPlayer(task.Player);
            if (player == null || !player)
            {
                // player is not in game, try to add the player.
                //player = await Game.Players.JoinAsync(task.Player, client, true);
                //if (player == null)
                {
                    client.SendMessage(task.Player.Username, Localization.MSG_NOT_PLAYING);
                    return;
                }
            }

            player.SetTask(task.Task, task.Arguments);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
    }
}