using RavenNest.Models;
using RavenNest.SDK.Endpoints;

/// <summary>
/// A player finished a quest (the server counted it and paid the coins): the coins show here too, the experience
/// is added here (the game's skills are what get saved), and the player hears about it in chat. For a chain, every
/// step finished comes here too, with the chain at its next step; the last one pays out, gives the title, and may
/// unlock the chain's decor piece for the village, which the whole chat hears.
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

        var bot = gameManager.RavenBot;
        if (quest.Kind == 3 && quest.State == 1)
        {
            // a chain's step: on to the next
            var story = QuestChat.Story(quest.Key, quest.Step);
            bot.SendReply(player, Localization.MSG_QUEST_STEP_DONE, QuestChat.ChainName(quest.Key), quest.Step + 1, quest.Steps, story, QuestChat.Task(quest));
            QuestChat.Say(gameManager, player, story);
            return;
        }

        if (quest.RewardCoins > 0) player.Resources.Coins += quest.RewardCoins;
        if (quest.RewardExp > 0) player.GetSkill((Skill)quest.RewardSkill)?.AddExp(quest.RewardExp);

        switch (quest.Kind)
        {
            case 3:
                bot.SendReply(player, Localization.MSG_QUEST_CHAIN_COMPLETE, QuestChat.ChainName(quest.Key), QuestChat.Ending(quest.Key),
                    QuestChat.Reward(quest), QuestChat.TitleText(quest.RewardTitle));
                QuestChat.Say(gameManager, player, QuestChat.Ending(quest.Key));
                if (!string.IsNullOrEmpty(data.DecorUnlocked))
                {
                    var decor = gameManager.Village ? gameManager.Village.Decor : null;
                    bot.Announce(Localization.MSG_QUEST_DECOR_UNLOCKED, player.PlayerName, QuestChat.ChainName(quest.Key),
                        decor ? decor.NameOf(data.DecorUnlocked) : Localization.DecorName(data.DecorUnlocked, data.DecorUnlocked));
                    if (decor) _ = decor.LoadAsync();
                }
                break;
            case 2:
                bot.SendReply(player, Localization.MSG_QUEST_SEASONAL_COMPLETED, QuestChat.SeasonOf(quest), QuestChat.Text(quest), QuestChat.Reward(quest));
                break;
            case 1:
                bot.SendReply(player, Localization.MSG_QUEST_WEEKLY_COMPLETED, QuestChat.Text(quest), QuestChat.Reward(quest));
                break;
            default:
                bot.SendReply(player, Localization.MSG_QUEST_COMPLETED, QuestChat.Text(quest), QuestChat.Reward(quest), data.Streak);
                break;
        }
    }
}
