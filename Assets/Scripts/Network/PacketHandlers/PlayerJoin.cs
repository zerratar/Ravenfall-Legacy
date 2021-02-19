using System;

public class PlayerJoin : PacketHandler<Player>
{
    public PlayerJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(Player data, GameClient client)
    {
        try
        {
            var addPlayerRequest = data;
            if (Game.RavenNest.SessionStarted)
            {
                if (!Game.Items.Loaded)
                {
                    client.SendMessage(addPlayerRequest.Username, Localization.GAME_NOT_LOADED);
                    return;
                }

                if (Game.Players.Contains(addPlayerRequest.UserId))
                {
                    client.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_FAILED_ALREADY_PLAYING);
                    return;
                }

                Game.EventTriggerSystem.SendInput(addPlayerRequest.UserId, "join");

                var playerInfo = await Game.RavenNest.PlayerJoinAsync(
                    new RavenNest.Models.PlayerJoinData
                    {
                        Identifier = addPlayerRequest.Identifier,
                        Moderator = addPlayerRequest.IsModerator,
                        Subscriber = addPlayerRequest.IsSubscriber,
                        Vip = addPlayerRequest.IsVip,
                        UserId = addPlayerRequest.UserId,
                        UserName = addPlayerRequest.Username,
                    });

                if (playerInfo == null)
                {
                    client.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_FAILED, addPlayerRequest.Username);
                    return;
                }

                if (!playerInfo.Success)
                {
                    client.SendMessage(addPlayerRequest.Username, playerInfo.ErrorMessage);
                    return;
                }

                var player = Game.SpawnPlayer(playerInfo.Player, addPlayerRequest);
                if (player)
                {
                    player.PlayerNameHexColor = addPlayerRequest.Color;
                    client.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_WELCOME);

                    if (player.IsBroadcaster)
                        Game.EventTriggerSystem.TriggerEvent("join", TimeSpan.FromSeconds(1));
                    // receiver:cmd|arg1|arg2|arg3|
                }
                else
                {
                    client.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_FAILED_ALREADY_PLAYING);
                }
            }
            else
            {
                client.SendMessage(addPlayerRequest.Username, Localization.GAME_NOT_READY);
            }
        }
        catch (Exception exc)
        {
            Game.LogError(exc.ToString());
        }
    }
}