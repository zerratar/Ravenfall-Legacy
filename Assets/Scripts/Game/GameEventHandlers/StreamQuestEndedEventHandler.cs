using RavenNest.SDK.Endpoints;

/// <summary>
/// The stream quest ended on the server: done (the server paid the village and the helpers; the helpers' coins show
/// here too), out of time, or stopped. Chat hears how it went either way.
/// </summary>
public class StreamQuestEndedEventHandler : GameEventHandler<StreamQuestEndedData>
{
    private const int TopHelpersNamed = 3;

    public override void Handle(GameManager gameManager, StreamQuestEndedData data)
    {
        var quest = data?.Quest;
        if (quest == null)
        {
            return;
        }

        StreamQuests.Ended(quest);
        var helpers = data.Helpers ?? System.Array.Empty<StreamQuestHelperData>();
        var bot = gameManager.RavenBot;

        switch (quest.State)
        {
            case StreamQuests.Done:
                {
                    long coins = 0;
                    foreach (var helper in helpers)
                    {
                        coins += helper.Coins;
                        var player = gameManager.Players.GetPlayerById(helper.CharacterId);
                        if (player && helper.Coins > 0) player.Resources.Coins += helper.Coins;
                    }

                    bot.Announce(Localization.MSG_STREAM_QUEST_DONE, StreamQuests.Text(quest), StreamQuests.VillageReward(quest),
                        helpers.Length, StreamQuests.Number(coins), StreamQuests.Helpers(helpers, TopHelpersNamed));
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
