using UnityEngine;

public class RaidJoin : PacketHandler<Player>
{
    public RaidJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (player)
        {
            if (player.StreamRaid.InWar)
            {
                client.SendMessage(data.Username, Localization.MSG_JOIN_RAID_WAR);
                return;
            }

            if (player.Ferry.OnFerry)
            {
                client.SendMessage(data.Username, Localization.MSG_JOIN_RAID_FERRY);
                return;
            }

            if (player.Ferry.Active)
            {
                player.Ferry.Disembark();
            }

            if (player.Arena.InArena && Game.Arena.Started)
            {
                client.SendMessage(data.Username, Localization.MSG_JOIN_RAID_ARENA);
                return;
            }

            if (player.Duel.InDuel)
            {
                client.SendMessage(data.Username, Localization.MSG_JOIN_RAID_DUEL);
                return;
            }

            var result = Game.Raid.CanJoin(player);
            switch (result)
            {
                case RaidJoinResult.NoActiveRaid:
                    client.SendMessage(data.Username, Localization.MSG_JOIN_RAID_NO_RAID);
                    return;
                case RaidJoinResult.AlreadyJoined:
                    client.SendMessage(data.Username, Localization.MSG_JOIN_RAID_ALREADY);
                    return;
                case RaidJoinResult.MinHealthReached:
                    client.SendMessage(data.Username, Localization.MSG_JOIN_RAID_PAST_HEALTH);
                    return;
            }

            Game.Arena.Leave(player);
            Game.Raid.Join(player);
            client.SendMessage(data.Username, Localization.MSG_JOIN_RAID);
        }
        else
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
        }
    }
}