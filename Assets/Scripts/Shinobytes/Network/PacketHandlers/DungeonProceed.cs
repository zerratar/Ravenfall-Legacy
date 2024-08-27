using Shinobytes.Linq;

public class DungeonProceed : ChatBotCommandHandler
{
    public DungeonProceed(GameManager game, RavenBotConnection server, PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (TryGetPlayer(gm, client, out var player))
        {
            if (Game.Dungeons && Game.Dungeons.Started && player.IsGameAdmin)
            {
                var dungeonManager = Game.Dungeons;
                var players = dungeonManager.GetPlayers();
                foreach (var enemy in dungeonManager.Dungeon.Room.Enemies)
                {
                    var randomPlayer = players.Random();
                    if (!enemy.Stats.IsDead)
                    {
                        enemy.TakeDamage(randomPlayer, enemy.Stats.Health.MaxLevel);
                    }
                }

                foreach (var plr in players)
                {
                    plr.ClearTarget();
                }

                Shinobytes.Debug.Log(player.Name + " used their admin powers to proceed in the dungeon.");
            }
        }
    }
}
