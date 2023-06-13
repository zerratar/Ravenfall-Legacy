public class PlayerBeginRestEventHandler : GameEventHandler<RavenNest.Models.PlayerId>
{
    public override void Handle(GameManager gameManager, RavenNest.Models.PlayerId data)
    {
        var player = gameManager.Players.GetPlayerById(data.Id);
        if (!player) return;

        if (player.Ferry.Embarking)
        {
            player.Ferry.Cancel();
        }

        if (player.Duel.InDuel || player.Arena.InArena || player.StreamRaid.InWar
            || player.Dungeon.InDungeon || player.Raid.InRaid || player.Onsen.InOnsen)
        {
            return;
        }

        gameManager.Onsen.Join(player);
    }
}
