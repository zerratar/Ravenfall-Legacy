using UnityEngine;

/// <summary>
/// One of the silly scenes placed in the world (docs/easter-eggs.md): a sheep on the Ironhill anvil, a seagull with a
/// sandwich on Home, a skeleton still fishing. Made by tools/easter-eggs/world-gags.cs into the local prefab
/// Resources/EasterEggs/World Gags, which <see cref="SillySurprises"/> puts in the world at the start, so MainWorld
/// itself never changes. The pieces under this object show while the Silly surprises setting is on (never in potato
/// mode) and, for a special day's scene, only on that day.
/// </summary>
public sealed class WorldGag : MonoBehaviour
{
    [Tooltip("What it is, for the docs and the debug tools: \"ironhill-sheep\".")]
    public string Key;

    [Tooltip("Empty: every day. \"MM-DD\": that day only (\"04-01\"). \"fri13\": every Friday the 13th.")]
    public string Day;

    [Tooltip("Optional: the island (RavenNest.Models.Island name) to put it under, so it hides and shows with the island (Ravebridge).")]
    public string Island;

    [Tooltip("Sign texts, and the Localization field each one shows (UI_GAG_SIGN_...).")]
    public TMPro.TextMeshPro[] Texts;
    public string[] TextKeys;

    private bool? shown;

    /// <summary>Whether this scene belongs to the given day.</summary>
    public bool IsDay(System.DateTime now)
    {
        if (string.IsNullOrEmpty(Day)) return true;
        if (Day == "fri13") return now.DayOfWeek == System.DayOfWeek.Friday && now.Day == 13;
        return Day == now.ToString("MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Shows or hides what is under this object (the object itself stays on, for an island that hides it).</summary>
    public void Show(bool on)
    {
        if (shown == on) return;
        shown = on;
        foreach (Transform child in transform) child.gameObject.SetActive(on);
        if (on) Word();
    }

    /// <summary>The signs in the game's language.</summary>
    public void Word()
    {
        if (Texts == null || TextKeys == null) return;
        for (var i = 0; i < Texts.Length && i < TextKeys.Length; ++i)
        {
            if (!Texts[i] || string.IsNullOrEmpty(TextKeys[i])) continue;
            var field = typeof(Localization).GetField(TextKeys[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field?.GetValue(null) is string text && !string.IsNullOrEmpty(text)) Texts[i].text = text;
        }
    }
}
