using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Someone on an island the streamer can talk to while playing by hand (phase 6e of docs/ravebridge-design.md): the
/// cook, the priest, the miller, the goblins' generals. A click from the controlled character walks up to them and
/// opens their dialogue (ManualPlayerController.TalkTo, RavebridgeDialogue). Now and then they say one of their idle
/// lines in a speech bubble, like the tavern's regulars (TavernNpc).
/// </summary>
public sealed class IslandNpc : MonoBehaviour
{
    /// <summary>Who they are in the island quests' data (StreamingAssets/Ravebridge/quests.json): "cook", "priest".</summary>
    public string Key;
    public float TalkDistance = 3.5f;
    public float HeadHeight = 2.2f;

    /// <summary>Their idle lines, from the quests' data.</summary>
    public static Func<string, IReadOnlyList<string>> LinesFor;

    private const float LineSeconds = 6f;
    private const float FadeSeconds = 0.35f;
    private static readonly List<IslandNpc> all = new List<IslandNpc>();

    private TextMeshPro bubble;
    private float showLeft;
    private float shownFor;
    private float nextLine;

    public static IReadOnlyList<IslandNpc> All => all;

    public static IslandNpc Find(string key)
    {
        foreach (var npc in all) if (npc && npc.Key == key) return npc;
        return null;
    }

    private void OnEnable()
    {
        all.Add(this);
        nextLine = Time.time + UnityEngine.Random.Range(5f, 25f);
    }

    private void OnDisable()
    {
        all.Remove(this);
        showLeft = 0f;
        if (bubble) bubble.gameObject.SetActive(false);
    }

    /// <summary>Shows a line over their head for a few seconds.</summary>
    public void Say(string text, float seconds = LineSeconds)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (!bubble) bubble = MakeBubble();
        bubble.gameObject.SetActive(true);
        bubble.text = "<mark=#1b130ccc padding=\"14,14,8,8\">" + text + "</mark>";
        showLeft = seconds;
        shownFor = 0f;
        nextLine = Mathf.Max(nextLine, Time.time + seconds + UnityEngine.Random.Range(15f, 30f));
    }

    private void Update()
    {
        if (Time.time >= nextLine)
        {
            nextLine = Time.time + UnityEngine.Random.Range(30f, 60f);
            var lines = LinesFor?.Invoke(Key);
            // only where someone might hear it: a player on the same island
            if (lines != null && lines.Count > 0 && SomeoneNear()) Say(lines[UnityEngine.Random.Range(0, lines.Count)]);
        }

        if (!bubble || !bubble.gameObject.activeSelf) return;
        showLeft -= Time.deltaTime;
        shownFor += Time.deltaTime;
        if (showLeft <= 0f)
        {
            bubble.gameObject.SetActive(false);
            return;
        }

        var cam = Camera.main;
        bubble.transform.position = transform.position + Vector3.up * HeadHeight;
        if (cam) bubble.transform.rotation = cam.transform.rotation;
        bubble.alpha = Mathf.Min(Mathf.Clamp01(shownFor / FadeSeconds), Mathf.Clamp01(showLeft / FadeSeconds));
    }

    private bool SomeoneNear()
    {
        var cam = Camera.main;
        return cam && Vector3.Distance(cam.transform.position, transform.position) < 80f;
    }

    private TextMeshPro MakeBubble()
    {
        var go = new GameObject("Speech Bubble");
        go.transform.SetParent(transform, false);
        var text = go.AddComponent<TextMeshPro>();
        text.fontSize = 1.6f;
        text.alignment = TextAlignmentOptions.Bottom;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.sizeDelta = new Vector2(3.2f, 1.5f);
        text.rectTransform.pivot = new Vector2(0.5f, 0f);
        text.color = new Color(1f, 0.94f, 0.82f);
        text.outlineWidth = 0f;
        return text;
    }
}
