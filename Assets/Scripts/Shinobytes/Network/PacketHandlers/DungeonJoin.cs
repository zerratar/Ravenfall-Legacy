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

            if (player.InRavebridge)
            {
                client.SendReply(gm, Localization.MSG_RAVEBRIDGE_BLOCKED);
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendReply(gm, Localization.MSG_DUNGEON_JOIN_WAR);
                return;
            }

            //if (player.ferryHandler.OnFerry)
            //{
            //    client.SendMessage(username, "You cannot join the dungeon while on the ferry. Please !disembark before you can join.");
            //    return;
            //}

            //if (player.ferryHandler.Active)
            //{
            //    player.ferryHandler.Disembark();
            //}

            if (!Game.Dungeons.Active)
            {
                client.SendReply(gm, Localization.MSG_DUNGEON_JOIN_NO_DUNGEON);
                return;
            }

            if (Game.Dungeons.Started)
            {
                client.SendReply(gm, Localization.MSG_DUNGEON_JOIN_STARTED);
                return;
            }

            var result = Game.Dungeons.CanJoin(player, data);

            switch (result)
            {
                case DungeonJoinResult.CanJoin:
                    Game.Dungeons.Join(player);
                    client.SendReply(gm, Localization.MSG_DUNGEON_JOIN_SUCCESS);
                    break;
                case DungeonJoinResult.AlreadyJoined:
                    client.SendReply(gm, Localization.MSG_DUNGEON_JOIN_ALREADY);
                    break;
                case DungeonJoinResult.WrongCode:
                    client.SendReply(gm, Localization.MSG_DUNGEON_JOIN_WRONG_CODE);
                    return;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("DungeonJoin.Handle: " + exc);
        }
    }
}
