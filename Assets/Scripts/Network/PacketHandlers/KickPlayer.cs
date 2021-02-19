
using System.Linq;

public class KickPlayer : PacketHandler<Player>
{
    public KickPlayer(
        GameManager game,
        RavenBotConnection server,
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

            client.SendMessage(data.Username, "{kickedPlayerCount} players was kicked from the game.", kickedPlayerCount.ToString());
            return;
        }

        var player = PlayerManager.GetPlayer(data);
        if (player)
        {
            if (Game.Dungeons.JoinedDungeon(player))
            {
                Game.Dungeons.Remove(player);
            }

            if (!Game.Arena.Started && Game.Arena.Activated && player.Arena.InArena)
            {
                Game.Arena.Leave(player);
            }
            else if (!Game.Arena.CanJoin(player, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
            {
                client.SendMessage(data.Username, "{player} cannot be kicked as they are participating in the arena that has already started and may break it. Player has been queued up to be kicked after the arena has been finished.", player.PlayerName);
                Game.QueueRemovePlayer(player);
                return;
            }

            if (player.Duel.InDuel)
            {
                client.SendMessage(data.Username, "{player} cannot be kicked while fighting a duel. Player has been queued up to be kicked after the duel has been finished.", player.PlayerName);
                Game.QueueRemovePlayer(player);
                return;
            }

            Game.RemovePlayer(player);
            client.SendMessage(data.Username, "{player} was kicked from the game.", player.PlayerName);
        }
        else
        {
            client.SendMessage(data.Username, "No players with the name '{player}' is playing.", data.Username);
        }
    }
}