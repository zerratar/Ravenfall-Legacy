using UnityEngine;

public class RaidJoin : ChatBotCommandHandler<string>
{
    public RaidJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string code, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player)
        {
            if (player.streamRaidHandler.InWar)
            {
                client.SendReply(gm, Localization.MSG_JOIN_RAID_WAR);
                return;
            }

            if (player.dungeonHandler.InDungeon)
            {
                client.SendReply(gm, Localization.MSG_JOIN_RAID_DUNGEON);
                return;
            }
            //if (player.ferryHandler.OnFerry)
            //{
            //    client.SendMessage(username, Localization.MSG_JOIN_RAID_FERRY);
            //    return;
            //}

            //if (player.ferryHandler.Active)
            //{
            //    player.ferryHandler.Disembark();
            //}

            if (player.arenaHandler.InArena && Game.Arena.Started)
            {
                client.SendReply(gm, Localization.MSG_JOIN_RAID_ARENA);
                return;
            }

            if (player.duelHandler.InDuel)
            {
                client.SendReply(gm, Localization.MSG_JOIN_RAID_DUEL);
                return;
            }

            var result = Game.Raid.CanJoin(player, code);
            switch (result)
            {
                case RaidJoinResult.NoActiveRaid:
                    client.SendReply(gm, Localization.MSG_JOIN_RAID_NO_RAID);
                    return;
                case RaidJoinResult.AlreadyJoined:
                    client.SendReply(gm, Localization.MSG_JOIN_RAID_ALREADY);
                    return;
                case RaidJoinResult.MinHealthReached:
                    client.SendReply(gm, Localization.MSG_JOIN_RAID_PAST_HEALTH);
                    return;
                case RaidJoinResult.WrongCode:
                    client.SendReply(gm, "You have used incorrect command for joining the raid. Use !raid [word seen on stream] to join.");
                    break;
            }



            Game.Arena.Leave(player);
            Game.Raid.Join(player);
            client.SendReply(gm, Localization.MSG_JOIN_RAID);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
        }
    }
}