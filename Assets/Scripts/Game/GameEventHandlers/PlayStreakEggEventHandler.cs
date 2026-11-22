using RavenNest.Models;

/// <summary>
/// A character played days in a row and the server gave a pet egg (day 7, then every 14 days; docs/battle-pets-design.md).
/// The inventory feed brings the egg; this says it in chat. The same event with no days is Nest Finder's egg (the
/// Gathering skill tree, docs/trade-skills-design.md): found while gathering.
/// </summary>
public class PlayStreakEggEventHandler : GameEventHandler<PlayStreakEgg>
{
    public override void Handle(GameManager gameManager, PlayStreakEgg data)
    {
        if (data == null || string.IsNullOrEmpty(data.Egg)) return;
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        if (!player) return;
        if (data.Days <= 0) gameManager.RavenBot?.Announce(Localization.MSG_PET_NEST_EGG, player.PlayerName, data.Egg);
        else gameManager.RavenBot?.Announce(Localization.MSG_PET_STREAK_EGG, player.PlayerName, data.Days, data.Egg);
    }
}
