using RavenNest.Models;
using RavenNest.SDK.Endpoints;

/// <summary>
/// A player finished a quest (the server counted it and paid the coins): the coins show here too, the experience
/// is added here (the game's skills are what get saved), and the player hears about it in chat.
/// </summary>
public class QuestCompletedEventHandler : GameEventHandler<QuestCompletedData>
{
    public override void Handle(GameManager gameManager, QuestCompletedData data)
    {
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        var quest = data.Quest;
        if (!player || quest == null)
        {
            return;
        }

        if (quest.RewardCoins > 0) player.Resources.Coins += quest.RewardCoins;
        if (quest.RewardExp > 0) player.GetSkill((Skill)quest.RewardSkill)?.AddExp(quest.RewardExp);

        gameManager.RavenBot.SendReply(player, quest.Kind == 1 ? Localization.MSG_QUEST_WEEKLY_COMPLETED : Localization.MSG_QUEST_COMPLETED,
            QuestChat.Text(quest), QuestChat.Reward(quest), data.Streak);
    }
}
