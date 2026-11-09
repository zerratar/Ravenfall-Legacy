using RavenNest.SDK.Endpoints;
using RavenNest.Models;

/// <summary>
/// The stream quest ended on the server: done (the server paid the village and the helpers; the helpers' coins show
/// here too), out of time, or stopped. Chat hears how it went either way.
/// </summary>
public class StreamQuestEndedEventHandler : GameEventHandler<StreamQuestEnded>
{
    private const int TopHelpersNamed = 3;

    public override void Handle(GameManager gameManager, StreamQuestEnded data)
    {
        var quest = data?.Quest;
        if (quest == null)
        {
            return;
        }

        StreamQuests.Ended(quest);
        var helpers = data.Helpers ?? System.Array.Empty<StreamQuestHelper>();
        var bot = gameManager.RavenBot;

        switch (quest.State)
        {
            case StreamQuests.Done:
                {
                    long coins = 0;
                    foreach (var helper in helpers)
                    {
                        // only counted for the announcement: the server's coin update (ResourceUpdate) brings the new amount; changing it here too raced that update
                        coins += helper.Coins;
                    }

                    bot.Announce(Localization.MSG_STREAM_QUEST_DONE, StreamQuests.Text(quest), StreamQuests.VillageReward(quest),
                        helpers.Length, StreamQuests.Number(coins), StreamQuests.Helpers(helpers, TopHelpersNamed));
                    // the one who added the most may have found a pet egg
                    foreach (var helper in helpers)
                        if (!string.IsNullOrEmpty(helper.Egg)) bot.Announce(Localization.MSG_STREAM_QUEST_EGG, helper.Name, helper.Egg);
                    break;
                }

            case StreamQuests.OutOfTime:
                bot.Announce(Localization.MSG_STREAM_QUEST_OUT_OF_TIME, StreamQuests.Text(quest),
                    StreamQuests.Number(quest.Progress), StreamQuests.Number(quest.Amount));
                break;

            default:
                bot.Announce(Localization.MSG_STREAM_QUEST_STOPPED, StreamQuests.Text(quest));
                break;
        }
    }
}
