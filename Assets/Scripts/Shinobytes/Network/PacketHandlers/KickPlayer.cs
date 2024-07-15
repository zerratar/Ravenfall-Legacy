
using System.Linq;

public class KickPlayer : ChatBotCommandHandler<User>
{
    public KickPlayer(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(User data, GameMessage gm, GameClient client)
    {
        var user = gm.Sender;
        if (!user.IsModerator && !user.IsBroadcaster)
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (!player)
            {
                return;
            }

            if (!player.IsGameAdmin && !player.IsGameModerator)
            {
                return;
            }
        }

        if (data.Username.Equals("afk", System.StringComparison.OrdinalIgnoreCase))
        {
            var kickedPlayerCount = 0;
            var players = PlayerManager
                .GetAllPlayers()
                .Where(x => (!x.ferryHandler.OnFerry && x.GetTask() == TaskType.None) || x.IsAfk)
                .ToList();

            foreach (var plr in players)
            {
                var isAfk = plr.IsAfk;
                if (isAfk)
                {
                    ++kickedPlayerCount;
                    Game.QueueRemovePlayer(plr);
                    continue;
                }

                if (plr.Movement.IdleTime < 30f)
                    continue;

                if (plr.onsenHandler.InOnsen)
                    continue;

                if (plr.raidHandler.InRaid)
                    continue;

                if (plr.ferryHandler.OnFerry)
                    continue;

                if (Game.Dungeons.JoinedDungeon(plr))
                    continue;

                if (!Game.Arena.CanJoin(plr, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
                    continue;

                if (plr.duelHandler.InDuel)
                    continue;

                ++kickedPlayerCount;
                Game.RemovePlayer(plr);
            }

            client.SendReply(gm, "{kickedPlayerCount} players was kicked from the game.", kickedPlayerCount.ToString());
            return;
        }

        var playerToKick = PlayerManager.GetPlayer(data);
        if (playerToKick)
        {
            if (Game.Dungeons.JoinedDungeon(playerToKick))
            {
                Game.Dungeons.Remove(playerToKick);
            }

            if (!Game.Arena.Started && Game.Arena.Activated && playerToKick.arenaHandler.InArena)
            {
                Game.Arena.Leave(playerToKick);
            }
            else if (playerToKick.duelHandler.InDuel || !Game.Arena.CanJoin(playerToKick, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
            {
                Game.QueueRemovePlayer(playerToKick);
            }
            else
            {
                Game.RemovePlayer(playerToKick);
            }

            client.SendReply(gm, "{player} was kicked from the game.", playerToKick.PlayerName);
        }
        else
        {
            var similarPlayerName = PlayerManager.GetAllPlayers().FirstOrDefault(x => x.Name.ToLower().StartsWith(data.Username));
            if (similarPlayerName != null)
            {
                client.SendReply(gm, "No players with the name '{player}' is playing. Did you mean " + similarPlayerName.Name + "?", data.Username);
            }
            else
            {
                client.SendReply(gm, "No players with the name '{player}' is playing.", data.Username);
            }
        }
    }
}