using UnityEngine;

public class RaidJoin : PacketHandler<EventJoinRequest>
{
    public RaidJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(EventJoinRequest data, GameClient client)
    {
        var username = data.Player.Username;
        var player = PlayerManager.GetPlayer(data.Player);
        if (player)
        {
            if (player.StreamRaid.InWar)
            {
                client.SendMessage(username, Localization.MSG_JOIN_RAID_WAR);
                return;
            }

            if (player.Ferry.OnFerry)
            {
                client.SendMessage(username, Localization.MSG_JOIN_RAID_FERRY);
                return;
            }

            if (player.Ferry.Active)
            {
                player.Ferry.Disembark();
            }

            if (player.Arena.InArena && Game.Arena.Started)
            {
                client.SendMessage(username, Localization.MSG_JOIN_RAID_ARENA);
                return;
            }

            if (player.Duel.InDuel)
            {
                client.SendMessage(username, Localization.MSG_JOIN_RAID_DUEL);
                return;
            }

            var result = Game.Raid.CanJoin(player, data.Code);
            switch (result)
            {
                case RaidJoinResult.NoActiveRaid:
                    client.SendMessage(username, Localization.MSG_JOIN_RAID_NO_RAID);
                    return;
                case RaidJoinResult.AlreadyJoined:
                    client.SendMessage(username, Localization.MSG_JOIN_RAID_ALREADY);
                    return;
                case RaidJoinResult.MinHealthReached:
                    client.SendMessage(username, Localization.MSG_JOIN_RAID_PAST_HEALTH);
                    return;
                case RaidJoinResult.WrongCode:
                    client.SendMessage(username, "You have used incorrect command for joining the raid. Use !raid [word seen on stream] to join.");
                    break;
            }

            if (player.Onsen.InOnsen)
            {
                Game.Onsen.Leave(player);
            }

            Game.Arena.Leave(player);
            Game.Raid.Join(player);
            client.SendMessage(username, Localization.MSG_JOIN_RAID);
        }
        else
        {
            client.SendMessage(username, Localization.MSG_NOT_PLAYING);
        }
    }
}