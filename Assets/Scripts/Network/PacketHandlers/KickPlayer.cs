
using System.Linq;

public class KickPlayer : PacketHandler<Player>
{
    public KickPlayer(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        if (data.Username.Equals("afk", System.StringComparison.OrdinalIgnoreCase))
        {
            var kickedPlayerCount = 0;
            var players = PlayerManager
                .GetAllPlayers()
                .Where(x => x.GetTask() == TaskType.None)
                .ToList();

            foreach (var plr in players)
            {
                if (plr.IdleTime < 30f)
                    continue;

                if (plr.Ferry.OnFerry)
                    continue;

                if (Game.Dungeons.JoinedDungeon(plr))
                    continue;

                if (!Game.Arena.CanJoin(plr, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
                    continue;

                if (plr.Duel.InDuel)
                    continue;

                ++kickedPlayerCount;
                Game.RemovePlayer(plr);
            }

            client.SendCommand(data.Username, "kick_success", $"{kickedPlayerCount} players was kicked from the game.");
            return;
        }

        var player = PlayerManager.GetPlayer(data);
        if (player)
        {
            if (Game.Dungeons.JoinedDungeon(player))
            {
                Game.Dungeons.Remove(player);
            }

            if (!Game.Arena.CanJoin(player, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
            {
                client.SendCommand(data.Username, "kick_failed", $"{player.PlayerName} cannot be kicked as they are participating in the arena that has already started and may break it. Player has been queued up to be kicked after the arena has been finished.");
                Game.QueueRemovePlayer(player);
                return;
            }

            if (player.Duel.InDuel)
            {
                client.SendCommand(data.Username, "kick_failed", $"{player.PlayerName} cannot be kicked while fighting a duel. Player has been queued up to be kicked after the duel has been finished.");
                Game.QueueRemovePlayer(player);
                return;
            }

            Game.RemovePlayer(player);
            client.SendCommand(data.Username, "kick_success", $"{player.PlayerName} was kicked from the game.");
        }
        else
        {
            client.SendCommand(data.Username, "kick_failed", $"No players with the name '{data.Username}' is playing.");
        }
    }
}