using System;
using System.Globalization;
using System.Threading.Tasks;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;

/// <summary>
/// The streamer's own quests (phase 4 of docs/quests-design.md): bounties (the first few viewers to reach the target
/// each get the reward) and a stream goal (everyone together, the reward shared). The streamer posts them from the
/// quest window; the server sets the reward aside and counts. This keeps what the server last said, asks again now
/// and then (StreamQuestView drives that, with the stream quest), posts and stops, and words them.
/// </summary>
public static class StreamerQuests
{
    public const int Bounty = 1, Goal = 2;
    public const int Running = 1, Done = 2, OutOfTime = 3, Stopped = 4, AllClaimed = 5;

    private const float ActiveRefreshSeconds = 10f;
    private const float IdleRefreshSeconds = 30f;

    private static bool refreshing;
    private static float nextRefresh;

    /// <summary>The stream's streamer quests as the server last sent them, with what can be set aside.</summary>
    public static StreamerQuestListData Latest { get; private set; }

    /// <summary>The running stream goal the streamer wrote, or null.</summary>
    public static StreamerQuestData RunningGoal
    {
        get
        {
            if (Latest?.Quests == null) return null;
            foreach (var q in Latest.Quests) if (q.Kind == StreamerQuests.Goal && q.State == Running) return q;
            return null;
        }
    }

    public static event Action Changed;

    public static void Tick(GameManager game, float now)
    {
        if (now < nextRefresh || !game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        var running = Latest?.Quests != null && Latest.Quests.Length > 0;
        nextRefresh = now + (running ? ActiveRefreshSeconds : IdleRefreshSeconds);
        _ = RefreshAsync(game);
    }

    public static void RefreshSoon() => nextRefresh = 0f;

    public static async Task RefreshAsync(GameManager game)
    {
        if (refreshing || !game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        refreshing = true;
        try
        {
            var list = await game.RavenNest.Quests.GetStreamerAsync();
            if (list != null)
            {
                Latest = list;
                Changed?.Invoke();
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The streamer quests could not be loaded: " + exc.Message);
        }
        finally
        {
            refreshing = false;
        }
    }

    /// <summary>Posts a quest and tells chat about it.</summary>
    public static async Task<StreamerQuestResultData> PostAsync(GameManager game, StreamerQuestRequestData request)
    {
        var result = await game.RavenNest.Quests.PostStreamerAsync(request);
        if (result != null && result.Ok && result.Quest != null) Announce(game, result.Quest);
        RefreshSoon();
        await RefreshAsync(game);
        return result;
    }

    /// <summary>Stops a quest; the server's StreamerQuestEnded tells chat.</summary>
    public static async Task<StreamerQuestResultData> StopAsync(GameManager game, Guid questId)
    {
        var result = await game.RavenNest.Quests.StopStreamerAsync(questId);
        await RefreshAsync(game);
        return result;
    }

    private static void Announce(GameManager game, StreamerQuestData q)
    {
        var title = string.IsNullOrEmpty(q.Description) ? Title(q) : Title(q) + ": " + q.Description;
        if (q.Kind == Goal)
            game.RavenBot.Announce(Localization.MSG_STREAMER_GOAL_POSTED, title, Text(q), Duration(q.Minutes), Reward(q));
        else
            game.RavenBot.Announce(Localization.MSG_STREAMER_BOUNTY_POSTED, title, Text(q), q.Winners, Reward(q), Duration(q.Minutes));
    }

    // ---- Wording ---------------------------------------------------------------------------------------

    /// <summary>The streamer's name for it, or what it asks when it has none.</summary>
    public static string Title(StreamerQuestData q) => string.IsNullOrEmpty(q?.Name) ? Text(q) : q.Name;

    /// <summary>"Catch 200 fish", from the objective, skill and amount.</summary>
    public static string Text(StreamerQuestData q) => Text(q?.Objective, q?.Skill ?? -1, q?.Amount ?? 0);

    public static string Text(string objective, int skill, long amount)
    {
        var n = Number(amount);
        switch (objective)
        {
            case "gather":
                switch ((Skill)skill)
                {
                    case Skill.Woodcutting: return string.Format(Localization.UI_QUEST_GATHER_WOOD, n);
                    case Skill.Mining: return string.Format(Localization.UI_QUEST_GATHER_ORE, n);
                    case Skill.Fishing: return string.Format(Localization.UI_QUEST_GATHER_FISH, n);
                    case Skill.Farming: return string.Format(Localization.UI_QUEST_GATHER_WHEAT, n);
                    default: return string.Format(Localization.UI_QUEST_GATHER_HERBS, n);
                }
            case "kill": return string.Format(Localization.UI_QUEST_KILL, n);
            case "craft": return string.Format(Localization.UI_QUEST_CRAFT, n);
            case "cook": return string.Format(Localization.UI_QUEST_COOK, n);
            case "brew": return string.Format(Localization.UI_QUEST_BREW, n);
            case "levels": return string.Format(Localization.UI_STREAM_QUEST_LEVELS, n);
            case "raid": return string.Format(amount == 1 ? Localization.UI_QUEST_RAID_ONE : Localization.UI_QUEST_RAID, n);
            case "dungeon": return string.Format(amount == 1 ? Localization.UI_QUEST_DUNGEON_ONE : Localization.UI_QUEST_DUNGEON, n);
            default: return objective ?? string.Empty;
        }
    }

    /// <summary>"5,000 coins, 3 Raw Shrimp and 100 loyalty points" (each, for a bounty; shared, for a goal).</summary>
    public static string Reward(StreamerQuestData q) => Reward(q.RewardCoins, q.RewardItemName, q.RewardItemAmount, q.RewardLoyalty);

    public static string Reward(long coins, string itemName, long items, long loyalty)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (coins > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_COINS, Number(coins)));
        if (items > 0 && !string.IsNullOrEmpty(itemName)) parts.Add(Number(items) + " " + itemName);
        if (loyalty > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_LOYALTY, Number(loyalty)));
        if (parts.Count == 0) return Localization.UI_QUEST_NONE;
        if (parts.Count == 1) return parts[0];
        return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + " " + Localization.UI_STREAMER_QUEST_AND + " " + parts[parts.Count - 1];
    }

    /// <summary>"2 hours", "30 minutes", "3 days".</summary>
    public static string Duration(int minutes)
    {
        if (minutes >= 24 * 60 && minutes % (24 * 60) == 0) return string.Format(CultureInfo.InvariantCulture, minutes == 24 * 60 ? Localization.UI_STREAMER_QUEST_DAY : Localization.UI_STREAMER_QUEST_DAYS, minutes / (24 * 60));
        if (minutes >= 60 && minutes % 60 == 0) return string.Format(CultureInfo.InvariantCulture, minutes == 60 ? Localization.UI_STREAMER_QUEST_HOUR : Localization.UI_STREAMER_QUEST_HOURS, minutes / 60);
        return string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_MINUTES, minutes);
    }

    /// <summary>The time left, as the stream quest says it.</summary>
    public static string TimeLeft(StreamerQuestData q) => StreamQuests.TimeLeft(new StreamQuestData { Ends = q.Ends });

    public static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
