using RavenNest.Models;

/// <summary>
/// A character played days in a row and the server gave a pet egg (day 7, then every 14 days; docs/battle-pets-design.md).
/// The inventory feed brings the egg; this says it in chat.
/// </summary>
public class PlayStreakEggEventHandler : GameEventHandler<PlayStreakEgg>
{
    public override void Handle(GameManager gameManager, PlayStreakEgg data)
    {
        if (data == null || string.IsNullOrEmpty(data.Egg)) return;
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        if (!player) return;
        gameManager.RavenBot?.Announce(Localization.MSG_PET_STREAK_EGG, player.PlayerName, data.Days, data.Egg);
    }
}
