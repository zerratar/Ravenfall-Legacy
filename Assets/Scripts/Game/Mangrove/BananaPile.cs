using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// The Mangrove island's banana pile (docs/banana-island.md): the monkeys slowly eat it, players gathering on the island
/// add to it, and viewers feed it real bananas (!banana feed). The sign over the island shows how it stands, and the
/// monkeys' mood follows it: well fed they rarely steal and the island gives a little more exp; hungry they steal often.
/// The stream's own island, so the game keeps it (states/mangrove-bananas.json), not the server; time the game was
/// closed eats into it too, but never more than half.
/// </summary>
public static class BananaPile
{
    public enum Mood { Insufficient, Barely, Sufficient, Abundant }

    public const float Most = 1000f;
    public const float Start = 400f;
    /// <summary>The monkeys eat one every this many seconds (600 an hour).</summary>
    public const float EatEvery = 6f;
    public const float PerGather = 1f;
    public const float PerBanana = 5f;
    public const float PerGoldenBanana = 250f;
    /// <summary>The extra exp on the island while the pile is abundant (on top of the island's own bonus).</summary>
    public const float AbundantBonus = 0.05f;

    private const float SaveEvery = 60f;

    private class State
    {
        public float Amount = Start;
        public DateTime SavedUtc = DateTime.UtcNow;
    }

    private static JsonStore<State> store;
    private static State state;
    private static float nextSave;

    public static event Action Changed;

    public static float Amount
    {
        get { Load(); return state.Amount; }
    }

    public static Mood Current => MoodOf(Amount);

    public static Mood MoodOf(float amount) =>
        amount >= 700f ? Mood.Abundant : amount >= 300f ? Mood.Sufficient : amount >= 100f ? Mood.Barely : Mood.Insufficient;

    /// <summary>How often the monkeys snatch something from someone training on the island (seconds).</summary>
    public static float StealEvery => Current switch
    {
        Mood.Abundant => 720f,
        Mood.Sufficient => 360f,
        Mood.Barely => 180f,
        _ => 90f,
    };

    public static string SignText => Current switch
    {
        Mood.Abundant => Localization.UI_BANANAS_ABUNDANT,
        Mood.Sufficient => Localization.UI_STORY_BANANAS_SIGN_DONE,
        Mood.Barely => Localization.UI_BANANAS_BARELY,
        _ => Localization.UI_BANANAS_INSUFFICIENT,
    };

    public static string MoodName => Current switch
    {
        Mood.Abundant => Localization.UI_BANANAS_MOOD_ABUNDANT,
        Mood.Sufficient => Localization.UI_BANANAS_MOOD_SUFFICIENT,
        Mood.Barely => Localization.UI_BANANAS_MOOD_BARELY,
        _ => Localization.UI_BANANAS_MOOD_INSUFFICIENT,
    };

    public static string AmountText => Mathf.FloorToInt(Amount).ToString("N0", CultureInfo.InvariantCulture);

    public static void Add(float bananas)
    {
        if (bananas == 0f) return;
        Load();
        var before = Current;
        state.Amount = Mathf.Clamp(state.Amount + bananas, 0f, Most);
        if (Current != before) Changed?.Invoke();
    }

    /// <summary>The pirates carry off this share of the pile (a raid lost).</summary>
    public static float Lose(float share)
    {
        Load();
        var lost = state.Amount * Mathf.Clamp01(share);
        Add(-lost);
        return lost;
    }

    /// <summary>The monkeys eat, and the pile is saved now and then (MonkeyTroop calls this every frame).</summary>
    public static void Tick(float deltaTime)
    {
        Load();
        Add(-deltaTime / EatEvery);
        if (Time.unscaledTime < nextSave) return;
        nextSave = Time.unscaledTime + SaveEvery;
        Save();
    }

    public static void Save()
    {
        if (state == null || store == null) return;
        try
        {
            state.SavedUtc = DateTime.UtcNow;
            store.Save(state);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The banana pile could not be saved: " + exc.Message);
        }
    }

    private static void Load()
    {
        if (state != null) return;
        try
        {
            store = JsonStore<State>.Create("mangrove-bananas");
            state = store.Get() ?? new State();
            // eaten while the game was closed, but the monkeys leave at least half
            var away = (float)(DateTime.UtcNow - state.SavedUtc).TotalSeconds;
            if (away > 0f) state.Amount = Mathf.Max(state.Amount * 0.5f, state.Amount - away / EatEvery);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The banana pile could not be loaded: " + exc.Message);
            state = new State();
        }
        nextSave = Time.unscaledTime + SaveEvery;
    }
}
