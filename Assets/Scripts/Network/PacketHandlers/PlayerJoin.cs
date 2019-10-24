using System;
using Newtonsoft.Json;


public class PlayerJoin : PacketHandler
{
    public PlayerJoin(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(Packet packet)
    {
        try
        {
            var addPlayerRequest = JsonConvert.DeserializeObject<Player>(packet.JsonData);

            if (Game.RavenNest.SessionStarted)
            {
                if (!Game.Items.Loaded)
                {
                    packet.Client.SendCommand(addPlayerRequest.Username,
                        "join_failed",
                        "Game has not finished loading yet, try again soon!");

                    return;
                }

                if (Game.Players.Contains(addPlayerRequest.UserId))
                {
                    packet.Client.SendCommand(addPlayerRequest.Username, "join_failed", "You're already playing!");
                    return;
                }

                var playerInfo = await Game.RavenNest.PlayerJoinAsync(addPlayerRequest.UserId, addPlayerRequest.Username);
                if (playerInfo == null)
                {
                    packet.Client.SendCommand(addPlayerRequest.Username,
                        "join_failed",
                        "Failed create or find a player with the username " + addPlayerRequest.Username);
                    return;
                }

                var player = Game.SpawnPlayer(playerInfo, addPlayerRequest);
                if (player)
                {
                    player.PlayerNameHexColor = addPlayerRequest.Color;
                    packet.Client.SendCommand(addPlayerRequest.Username, "join_success", "Welcome to the game!");                    
                    // receiver:cmd|arg1|arg2|arg3|
                }
                else
                {
                    packet.Client.SendCommand(addPlayerRequest.Username, "join_failed", "You're already playing!");
                }
            }
            else
            {
                packet.Client.SendCommand(addPlayerRequest.Username, "join_failed", "Game is not ready yet!");
            }
        }
        catch (Exception exc)
        {
            Game.LogError(exc.ToString());
        }
    }
}