using System;
using System.Collections.Generic;

/// <summary>
/// The titles the players in the stream wear (phase 3 of docs/quests-design.md), for their name tags: asked of the
/// server now and then and soon after someone joins, and set at once when a player picks one with !title. The name
/// tags look at <see cref="Version"/> to know when to look again.
/// </summary>
public static class QuestTitles
{
    private const float RefreshSeconds = 60f;
    private const float AfterJoinSeconds = 3f;

    private static readonly Dictionary<Guid, string> titles = new Dictionary<Guid, string>();
    private static float nextRefresh;
    private static int lastPlayerCount = -1;
    private static bool refreshing;

    /// <summary>Grows whenever a title changes.</summary>
    public static int Version { get; private set; }

    /// <summary>The title's words for a character ("the Lumberjack"), or null.</summary>
    public static string TextFor(Guid characterId) =>
        titles.TryGetValue(characterId, out var key) && !string.IsNullOrEmpty(key) ? QuestChat.TitleText(key) : null;

    public static void Set(Guid characterId, string key)
    {
        if (string.IsNullOrEmpty(key)) titles.Remove(characterId);
        else titles[characterId] = key;
        Version++;
    }

    /// <summary>Asks the server when it is time, and soon after the number of players changed.</summary>
    public static void Tick(GameManager game, float now)
    {
        if (!game || game.RavenNest == null || !game.RavenNest.SessionStarted || game.Players == null) return;
        var count = game.Players.GetPlayerCount();
        if (count != lastPlayerCount)
        {
            lastPlayerCount = count;
            nextRefresh = Math.Min(nextRefresh, now + AfterJoinSeconds);
        }

        if (now < nextRefresh) return;
        nextRefresh = now + RefreshSeconds;
        _ = RefreshAsync(game);
    }

    private static async System.Threading.Tasks.Task RefreshAsync(GameManager game)
    {
        if (refreshing) return;
        refreshing = true;
        try
        {
            var result = await game.RavenNest.Quests.GetTitlesAsync();
            if (result?.CharacterIds == null || result.Titles == null) return;
            titles.Clear();
            for (var i = 0; i < result.CharacterIds.Length && i < result.Titles.Length; ++i) titles[result.CharacterIds[i]] = result.Titles[i];
            Version++;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The players' titles could not be loaded: " + exc.Message);
        }
        finally
        {
            refreshing = false;
        }
    }
}
