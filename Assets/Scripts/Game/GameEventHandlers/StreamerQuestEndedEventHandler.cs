using System.Collections.Generic;
using System.Globalization;
using RavenNest.SDK.Endpoints;

/// <summary>
/// One of the streamer's quests ended: a stream goal done (the helpers' shares were paid on the server), every bounty
/// reward claimed, out of time, or stopped. What was not won has gone back to the streamer. Chat hears how it went.
/// </summary>
public class StreamerQuestEndedEventHandler : GameEventHandler<StreamerQuestEndedData>
{
    private const int TopHelpersNamed = 3;

    public override void Handle(GameManager gameManager, StreamerQuestEndedData data)
    {
        var quest = data?.Quest;
        if (quest == null)
        {
            return;
        }

        StreamerQuests.RefreshSoon();
        var bot = gameManager.RavenBot;
        var title = StreamerQuests.Title(quest);
        switch (quest.State)
        {
            case StreamerQuests.Done:
                {
                    var rewards = data.Rewards ?? System.Array.Empty<StreamerQuestRewardData>();
                    var top = new List<string>();
                    for (var i = 0; i < rewards.Length && i < TopHelpersNamed; ++i) top.Add(rewards[i].Name + " (" + StreamerQuests.Number(rewards[i].Amount) + ")");
                    bot.Announce(Localization.MSG_STREAMER_GOAL_DONE, title, rewards.Length, top.Count > 0 ? string.Join(", ", top) : Localization.UI_QUEST_NONE);
                    break;
                }

            case StreamerQuests.AllClaimed:
                bot.Announce(Localization.MSG_STREAMER_BOUNTY_ALL_CLAIMED, title);
                break;

            case StreamerQuests.OutOfTime:
                {
                    var how = quest.Kind == StreamerQuests.Goal
                        ? StreamerQuests.Number(quest.Progress) + "/" + StreamerQuests.Number(quest.Amount)
                        : string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_CLAIMED, quest.Claimed, quest.Winners);
                    bot.Announce(Localization.MSG_STREAMER_QUEST_OUT_OF_TIME, title, how);
                    break;
                }

            default:
                bot.Announce(Localization.MSG_STREAMER_QUEST_STOPPED, title);
                break;
        }
    }
}
