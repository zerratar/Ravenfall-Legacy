using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What happens on the Mangrove island, as short lines on screen (MangroveView): a monkey ran off with someone's fish, a
/// treasure map turned up, a boat won the regatta. Never in chat (docs/twitch-message-budget.md); the few lines worth
/// chat (a treasure found, the pirates coming, the regatta's winner) are posted where they happen.
/// </summary>
public static class MangroveFeed
{
    public const int Kept = 5;
    public const float LineSeconds = 10f;

    public struct Line
    {
        public string Text;
        public string Glyph;
        public float At;
    }

    private static readonly List<Line> lines = new List<Line>();

    public static IReadOnlyList<Line> Lines => lines;
    public static event Action Changed;

    /// <summary>A line in the feed, with a Font Awesome glyph before it (a paw by default).</summary>
    public static void Say(string text, string glyph = "")
    {
        if (string.IsNullOrEmpty(text)) return;
        lines.Add(new Line { Text = text, Glyph = glyph, At = Time.unscaledTime });
        while (lines.Count > Kept) lines.RemoveAt(0);
        Changed?.Invoke();
    }
}
