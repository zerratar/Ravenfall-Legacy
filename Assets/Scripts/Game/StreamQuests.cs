using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;

/// <summary>
/// The stream quest (phase 2 of docs/quests-design.md): one goal everyone in the stream works on together, picked by
/// the streamer from three offers a day. The server counts it; this keeps what it last said (<see cref="Latest"/>),
/// asks again now and then (StreamQuestView drives that), starts and stops, and words it for the window and for chat.
/// </summary>
public static class StreamQuests
{
    public const int Offered = 0, Running = 1, Done = 2, OutOfTime = 3, Stopped = 4;

    private const float ActiveRefreshSeconds = 5f;
    private const float IdleRefreshSeconds = 30f;

    private static bool refreshing;
    private static float nextRefresh;

    /// <summary>The stream quest as the server last sent it: the running one and today's offers.</summary>
    public static StreamQuestStateData Latest { get; private set; }

    /// <summary>The running quest, or null.</summary>
    public static StreamQuestData Active => Latest?.Active != null && Latest.Active.State == Running ? Latest.Active : null;

    public static event Action Changed;

    /// <summary>Asks the server when it is time (more often while a quest runs).</summary>
    public static void Tick(GameManager game, float now)
    {
        if (now < nextRefresh || !game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        nextRefresh = now + (Active != null ? ActiveRefreshSeconds : IdleRefreshSeconds);
        _ = RefreshAsync(game);
    }

    /// <summary>Asks at the next tick, after something changed.</summary>
    public static void RefreshSoon() => nextRefresh = 0f;

    public static async Task RefreshAsync(GameManager game)
    {
        if (refreshing || !game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        refreshing = true;
        try
        {
            var state = await game.RavenNest.Quests.GetStreamAsync();
            if (state != null) Set(state);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The stream quest could not be loaded: " + exc.Message);
        }
        finally
        {
            refreshing = false;
        }
    }

    /// <summary>Starts today's offer in that slot and tells chat.</summary>
    public static async Task<StreamQuestResultData> StartAsync(GameManager game, int slot)
    {
        var result = await game.RavenNest.Quests.StartStreamAsync(slot);
        if (result != null && result.Ok && result.Quest != null)
        {
            Set(new StreamQuestStateData { Active = result.Quest, Offers = Latest?.Offers ?? Array.Empty<StreamQuestData>() });
            game.RavenBot.Announce(Localization.MSG_STREAM_QUEST_STARTED, Text(result.Quest), result.Quest.Minutes, Reward(result.Quest));
        }
        RefreshSoon();
        return result;
    }

    /// <summary>Stops the running quest; the server's StreamQuestEnded tells chat.</summary>
    public static async Task<StreamQuestResultData> StopAsync(GameManager game)
    {
        var result = await game.RavenNest.Quests.StopStreamAsync();
        if (result != null && result.Ok)
        {
            Set(new StreamQuestStateData { Active = null, Offers = Latest?.Offers ?? Array.Empty<StreamQuestData>() });
        }
        RefreshSoon();
        return result;
    }

    /// <summary>The quest ended on the server: no longer running here either.</summary>
    public static void Ended(StreamQuestData quest)
    {
        if (Latest?.Active != null && quest != null && Latest.Active.Id == quest.Id)
        {
            Set(new StreamQuestStateData { Active = null, Offers = Latest.Offers });
        }
        RefreshSoon();
    }

    private static void Set(StreamQuestStateData state)
    {
        Latest = state;
        Changed?.Invoke();
    }

    // ---- Wording ---------------------------------------------------------------------------------------

    /// <summary>"Chop 600 wood", "Defeat 300 enemies", "Gain 12 levels".</summary>
    public static string Text(StreamQuestData q)
    {
        if (q == null) return string.Empty;
        var amount = Number(q.Amount);
        switch (q.Objective)
        {
            case "gather":
                switch ((Skill)q.Skill)
                {
                    case Skill.Woodcutting: return string.Format(Localization.UI_QUEST_GATHER_WOOD, amount);
                    case Skill.Mining: return string.Format(Localization.UI_QUEST_GATHER_ORE, amount);
                    case Skill.Fishing: return string.Format(Localization.UI_QUEST_GATHER_FISH, amount);
                    case Skill.Farming: return string.Format(Localization.UI_QUEST_GATHER_WHEAT, amount);
                    default: return string.Format(Localization.UI_QUEST_GATHER_HERBS, amount);
                }
            case "kill": return string.Format(Localization.UI_QUEST_KILL, amount);
            case "make": return string.Format(Localization.UI_STREAM_QUEST_MAKE, amount);
            case "levels": return string.Format(Localization.UI_STREAM_QUEST_LEVELS, amount);
            case "raid": return string.Format(q.Amount == 1 ? Localization.UI_QUEST_RAID_ONE : Localization.UI_QUEST_RAID, amount);
            case "dungeon": return string.Format(q.Amount == 1 ? Localization.UI_QUEST_DUNGEON_ONE : Localization.UI_QUEST_DUNGEON, amount);
            default: return q.Key;
        }
    }

    /// <summary>"2,200 coins and 600 wood for the village, 9,000 coins shared by the helpers".</summary>
    public static string Reward(StreamQuestData q) =>
        string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_REWARD, Number(q.RewardVillageCoins), ResourcePart(q), Number(q.RewardHelperCoins));

    /// <summary>What the village gets: "2,200 coins and 600 wood".</summary>
    public static string VillageReward(StreamQuestData q) =>
        string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_VILLAGE_REWARD, Number(q.RewardVillageCoins), ResourcePart(q));

    private static string ResourcePart(StreamQuestData q) =>
        q.RewardResource > 0 && q.RewardResourceAmount > 0
            ? string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_REWARD_AND, Number(q.RewardResourceAmount), ResourceName(q.RewardResource))
            : string.Empty;

    public static string ResourceName(int resource)
    {
        switch (resource)
        {
            case 1: return Localization.UI_STREAM_QUEST_WOOD;
            case 2: return Localization.UI_STREAM_QUEST_ORE;
            case 3: return Localization.UI_STREAM_QUEST_FISH;
            case 4: return Localization.UI_STREAM_QUEST_WHEAT;
            default: return string.Empty;
        }
    }

    /// <summary>"12:34" until the quest ends, "1:02:03" past an hour, "3d 4h" past a day.</summary>
    public static string TimeLeft(StreamQuestData q)
    {
        var left = EndsUtc(q) - DateTime.UtcNow;
        if (left < TimeSpan.Zero) left = TimeSpan.Zero;
        if (left.TotalDays >= 1) return string.Format(CultureInfo.InvariantCulture, "{0}d {1}h", (int)left.TotalDays, left.Hours);
        return left.TotalHours >= 1
            ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", (int)left.TotalHours, left.Minutes, left.Seconds)
            : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", left.Minutes, left.Seconds);
    }

    public static int MinutesLeft(StreamQuestData q) => Math.Max(0, (int)Math.Ceiling((EndsUtc(q) - DateTime.UtcNow).TotalMinutes));

    /// <summary>The server's times are UTC, with or without the zone in the text.</summary>
    private static DateTime EndsUtc(StreamQuestData q) => q.Ends.Kind == DateTimeKind.Local ? q.Ends.ToUniversalTime() : DateTime.SpecifyKind(q.Ends, DateTimeKind.Utc);

    /// <summary>"Ada (120), Bob (80)".</summary>
    public static string Helpers(IReadOnlyList<StreamQuestHelperData> helpers, int count)
    {
        if (helpers == null || helpers.Count == 0) return Localization.UI_QUEST_NONE;
        var names = new List<string>();
        for (var i = 0; i < helpers.Count && i < count; ++i)
        {
            names.Add(string.Format(CultureInfo.InvariantCulture, "{0} ({1})", helpers[i].Name, Number(helpers[i].Amount)));
        }
        return string.Join(", ", names);
    }

    public static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
