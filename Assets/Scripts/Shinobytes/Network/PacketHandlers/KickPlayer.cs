
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
        if (data.Username.Equals("afk", System.StringComparison.OrdinalIgnoreCase))
        {
            var kickedPlayerCount = 0;
            var players = PlayerManager
                .GetAllPlayers()
                .Where(x => x.GetTask() == TaskType.None)
                .ToList();

            foreach (var plr in players)
            {
                if (plr.Movement.IdleTime < 30f)
                    continue;

                if (plr.Onsen.InOnsen)
                    continue;

                if (plr.Raid.InRaid)
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

            if (!Game.Arena.Started && Game.Arena.Activated && playerToKick.Arena.InArena)
            {
                Game.Arena.Leave(playerToKick);
            }
            else if (playerToKick.Duel.InDuel || !Game.Arena.CanJoin(playerToKick, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
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