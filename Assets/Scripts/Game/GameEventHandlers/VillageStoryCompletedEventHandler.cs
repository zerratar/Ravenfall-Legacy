using RavenNest.SDK.Endpoints;

/// <summary>
/// The streamer's story line is told to its end: chat hears the ending and what the village unlocked, the helpers'
/// coins (paid on the server) show here too, the story's decor piece appears in build mode, and the streamer's new
/// title can be worn.
/// </summary>
public class VillageStoryCompletedEventHandler : GameEventHandler<VillageStoryCompletedData>
{
    public override void Handle(GameManager gameManager, VillageStoryCompletedData data)
    {
        var story = data?.Story;
        if (story == null)
        {
            return;
        }

        long coins = 0;
        var helpers = data.Helpers ?? System.Array.Empty<StreamQuestHelperData>();
        foreach (var helper in helpers)
        {
            coins += helper.Coins;
            var player = gameManager.Players.GetPlayerById(helper.CharacterId);
            if (player && helper.Coins > 0) player.Resources.Coins += helper.Coins;
        }

        gameManager.RavenBot.Announce(Localization.MSG_STORY_DONE, StoryLog.Name(story.Key), StoryLog.Ending(story.Key),
            StoryLog.Unlocks(data.Unlocks), helpers.Length, StoryLog.Number(coins));

        var decor = gameManager.Village ? gameManager.Village.Decor : null;
        if (!string.IsNullOrEmpty(data.DecorUnlocked) && decor) _ = decor.LoadAsync();
        StoryLog.RefreshSoon();
    }
}
