using Shinobytes.Linq;

public class DungeonKillBoss : ChatBotCommandHandler
{
    public DungeonKillBoss(GameManager game, RavenBotConnection server, PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (TryGetPlayer(gm, client, out var player))
        {
            // a game administrator's tool for a stuck fight: anyone else was answered with silence
            if (!player.IsGameAdmin)
            {
                client.SendReply(gm, Localization.MSG_ADMIN_ONLY_SKIP);
                return;
            }

            if (Game.Dungeons && Game.Dungeons.Started && player.IsGameAdmin)
            {
                var dungeonManager = Game.Dungeons;
                var randomPlayer = dungeonManager.GetPlayers().Random();
                if (randomPlayer && dungeonManager.Boss)
                {
                    var boss = dungeonManager.Boss.Enemy;
                    boss.TakeDamage(randomPlayer, boss.Stats.Health.MaxValue);
                    Shinobytes.Debug.Log(player.Name + " used their admin powers to kill the dungeon boss.");
                }
            }
        }
    }
}
