
using System.Linq;

public class KickPlayer : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public KickPlayer(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
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
            else if (player.Duel.InDuel || !Game.Arena.CanJoin(player, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
            {
                Game.QueueRemovePlayer(player);
            }
            else
            {
                Game.RemovePlayer(player);
            }

            client.SendMessage(data.Username, "{player} was kicked from the game.", player.PlayerName);
        }
        else
        {
            var similarPlayerName = PlayerManager.GetAllPlayers().FirstOrDefault(x => x.Name.ToLower().StartsWith(data.Username));
            if (similarPlayerName != null)
            {
                client.SendMessage(data.Username, "No players with the name '{player}' is playing. Did you mean " + similarPlayerName.Name + "?", data.Username);
            }
            else
            {
                client.SendMessage(data.Username, "No players with the name '{player}' is playing.", data.Username);
            }
        }
    }
}