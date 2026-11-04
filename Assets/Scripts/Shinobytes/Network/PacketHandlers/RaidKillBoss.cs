using Shinobytes.Linq;

public class RaidKillBoss : ChatBotCommandHandler
{
    public RaidKillBoss(GameManager game, RavenBotConnection server, PlayerManager playerManager) : base(game, server, playerManager)
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

            if (Game.Raid && Game.Raid.Started && player.IsGameAdmin)
            {
                var raidManager = Game.Raid;
                var randomPlayer = raidManager.Raiders.Random();
                if (raidManager.Boss && randomPlayer)
                {
                    raidManager.Boss.Enemy.TakeDamage(randomPlayer, raidManager.Boss.Enemy.Stats.Health.CurrentValue);
                    Shinobytes.Debug.Log(player.Name + " used their admin powers to kill the raid boss.");
                }
            }
        }
    }
}
