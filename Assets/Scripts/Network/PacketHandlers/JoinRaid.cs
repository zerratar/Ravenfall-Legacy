using UnityEngine;

public class RaidJoin : PacketHandler<Player>
{
    public RaidJoin(
        GameManager game,
        GameServer server,
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
                client.SendCommand(data.Username, "raid_join_failed", $"You cannot fight a raid boss during a war!");
                return;
            }

            if (player.Ferry.OnFerry)
            {
                client.SendCommand(data.Username, "raid_join_failed", $"You cannot join the raid while on the ferry.");
                return;
            }

            if (player.Ferry.Active)
            {
                player.Ferry.Disembark();
            }

            if (player.Arena.InArena && Game.Arena.Started)
            {
                client.SendCommand(data.Username, "raid_join_failed", $"You cannot join the raid while in the arena.");
                return;
            }

            if (player.Duel.InDuel)
            {
                client.SendCommand(data.Username, "raid_join_failed", $"You cannot join the raid while in a duel.");
                return;
            }

            var result = Game.Raid.CanJoin(player);
            switch (result)
            {
                case RaidJoinResult.NoActiveRaid:
                    client.SendCommand(data.Username, "raid_join_failed", $"There are no active raids at the moment.");
                    return;
                case RaidJoinResult.AlreadyJoined:
                    client.SendCommand(data.Username, "raid_join_failed", $"You have already joined the raid.");
                    return;
                case RaidJoinResult.MinHealthReached:
                    client.SendCommand(data.Username, "raid_join_failed", $"You can no longer join the raid.");
                    return;
            }

            Game.Arena.Leave(player);
            Game.Raid.Join(player);
            client.SendCommand(data.Username, "raid_join_success", $"You have joined the raid. Good luck!");
        }
        else
        {
            client.SendCommand(data.Username, "raid_join_failed", $"You have to !join the game before using this command.");
        }
    }
}