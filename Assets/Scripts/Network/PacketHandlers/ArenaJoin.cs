using System;

public class ArenaJoin : PacketHandler<Player>
{
    public ArenaJoin(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        try
        {
            var player = PlayerManager.GetPlayer(data);
            if (player == null)
            {
                client.SendCommand(data.Username,
                    "arena_join_failed",
                    "You are not currently playing, use the !join to start playing.");

                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendCommand(data.Username, "arena_join_failed",
                    "Arena is unavailable during wars. Please wait for it to be over.");
                return;
            }

            if (Game.Arena.Island != player.Island)
            {
                client.SendCommand(data.Username, "arena_failed", $"There is no arena on the island you're on.");
                return;
            }

            if (player.Ferry.OnFerry)
            {
                client.SendCommand(data.Username, "arena_failed", $"You cannot join the arena while on the ferry.");
                return;
            }

            if (player.Ferry.Active)
            {
                player.Ferry.Disembark();
            }

            if (!Game.Arena.CanJoin(player, out var alreadyJoined, out var alreadyStarted)
                || player.Raid.InRaid
                || player.Duel.InDuel)
            {
                var errorMessage = "";
                if (player.Duel.InDuel)
                {
                    errorMessage = "You are currently fighting in a duel.";
                }
                else if (player.Raid.InRaid)
                {
                    errorMessage = "You are currently fighting in the raid.";
                }
                else if (alreadyJoined)
                {
                    errorMessage = "You have already joined the arena.";
                }
                else if (alreadyStarted)
                {
                    errorMessage = "You cannot join as the arena has already started.";
                }
                else
                {
                    errorMessage = "You cannot join the arena at this time.";
                }

                client.SendCommand(data.Username, "arena_join_failed", errorMessage);
                return;
            }

            Game.Arena.Join(player);

            client.SendCommand(player.PlayerName, "arena_join_success", $"You have joined the arena. Good luck!");
        }
        catch (Exception exc)
        {
            Game.LogError(exc.ToString());
        }
    }
}