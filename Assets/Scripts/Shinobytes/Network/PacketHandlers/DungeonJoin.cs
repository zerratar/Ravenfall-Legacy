using System;
using System.Data;

public class DungeonJoin : ChatBotCommandHandler<string>
{
    public DungeonJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        try
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (player == null)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendReply(gm, "Dungeon is unavailable during wars. Please wait for it to be over.");
                return;
            }

            //if (player.Ferry.OnFerry)
            //{
            //    client.SendMessage(username, "You cannot join the dungeon while on the ferry. Please !disembark before you can join.");
            //    return;
            //}

            //if (player.Ferry.Active)
            //{
            //    player.Ferry.Disembark();
            //}

            if (!Game.Dungeons.Active)
            {
                client.SendReply(gm, "No active dungeons available, sorry.");
                return;
            }

            if (Game.Dungeons.Started)
            {
                client.SendReply(gm, "The dungeon has already started. You will have to wait for the next one!");
                return;
            }

            var result = Game.Dungeons.CanJoin(player, data);

            switch (result)
            {
                case DungeonJoinResult.CanJoin:
                    Game.Dungeons.Join(player);
                    client.SendReply(gm, "You have joined the dungeon. Good luck!");
                    break;
                case DungeonJoinResult.AlreadyJoined:
                    client.SendReply(gm, "You have already joined the dungeon.");
                    break;
                case DungeonJoinResult.WrongCode:
                    client.SendReply(gm, "You have used incorrect command for joining the dungeon. Use !dungeon [word seen on stream] to join.");
                    break;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("DungeonJoin.Handle: " + exc);
        }
    }
}
