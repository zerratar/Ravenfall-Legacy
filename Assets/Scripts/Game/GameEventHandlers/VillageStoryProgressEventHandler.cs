using RavenNest.SDK.Endpoints;

/// <summary>
/// A step of the streamer's story line moved: a viewer found the story's thing (named in chat), or a step was done
/// and the NPC says what comes next.
/// </summary>
public class VillageStoryProgressEventHandler : GameEventHandler<VillageStoryProgressData>
{
    public override void Handle(GameManager gameManager, VillageStoryProgressData data)
    {
        var story = data?.Story;
        if (story == null)
        {
            return;
        }

        StoryLog.RefreshSoon();
        var bot = gameManager.RavenBot;
        if (!string.IsNullOrEmpty(data.FinderName) && data.StepDone < 0)
        {
            bot.Announce(Localization.MSG_STORY_FOUND, data.FinderName, StoryLog.Thing(story.Key), StoryLog.Number(story.Progress), StoryLog.Number(story.Amount));
            return;
        }

        if (data.StepDone >= 0)
        {
            bot.Announce(Localization.MSG_STORY_STEP, StoryLog.Name(story.Key), story.Step + 1, story.Steps, StoryLog.Line(story.Key, story.Step), StoryLog.Task(story));
        }
    }
}
