using RavenNest.SDK.Endpoints;

/// <summary>
/// A player reached an achievement's tier (the server counts them): the player hears which, and the title it gave.
/// The hidden ones announce themselves as secrets.
/// </summary>
public class AchievementReachedEventHandler : GameEventHandler<AchievementReachedData>
{
    public override void Handle(GameManager gameManager, AchievementReachedData data)
    {
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        if (!player)
        {
            return;
        }

        var secret = data.Key == "wrong" || data.Key == "quitter";
        gameManager.RavenBot.SendReply(player, secret ? Localization.MSG_ACHIEVEMENT_SECRET : Localization.MSG_ACHIEVEMENT_REACHED,
            QuestChat.AchievementName(data.Key), QuestChat.Medal(data.Tier), QuestChat.TitleText(data.Title));
    }
}
