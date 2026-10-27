using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Someone on an island the streamer can talk to while playing by hand (phase 6e of docs/ravebridge-design.md): the
/// cook, the priest, the miller, the goblins' generals. A click from the controlled character walks up to them and
/// opens their dialogue (ManualPlayerController.TalkTo, RavebridgeDialogue). Now and then they say one of their idle
/// lines in a speech bubble, like the tavern's regulars (TavernNpc). Over their head floats a mark for the character
/// playing (<see cref="MarkFor"/>): what they have to give or take, as in docs/mockups/quest-markers.jpg.
/// </summary>
public sealed class IslandNpc : MonoBehaviour
{
    /// <summary>Who they are in the island quests' data (StreamingAssets/Ravebridge/quests.json): "cook", "priest".</summary>
    public string Key;
    public float TalkDistance = 3.5f;
    public float HeadHeight = 2.2f;

    /// <summary>Their idle lines, from the quests' data.</summary>
    public static Func<string, IReadOnlyList<string>> LinesFor;

    /// <summary>The marks, least to most useful: the most useful one is shown.</summary>
    public enum Mark { None, Talk, Waiting, Repeatable, Available, Ready }

    /// <summary>Which mark an NPC shows now, from the island quests (RavebridgeQuests.MarkFor).</summary>
    public static Func<string, Mark> MarkFor;

    /// <summary>
    /// This NPC's own lines and mark, for one outside the island quests (the Ironhill dwarf, from the streamer's
    /// stories); when set they are used instead of <see cref="LinesFor"/> and <see cref="MarkFor"/>.
    /// </summary>
    public Func<IReadOnlyList<string>> Lines;
    public Func<Mark> MarkSource;

    /// <summary>The tallest a mark gets (metres), seen from afar; closer it shrinks, see WorldMark.Fit.</summary>
    private const float MarkSize = 1.1f;
    private const float MarkEvery = 0.5f;
    private static readonly Dictionary<Mark, Sprite> markSprites = new Dictionary<Mark, Sprite>();
    private SpriteRenderer marker;
    private Mark shownMark;
    private float nextMark;

    private const float LineSeconds = 6f;
    private const float FadeSeconds = 0.35f;

    /// <summary>Up to this far from the camera a speech bubble keeps its size; further off it grows (to 3 times), so it stays readable.</summary>
    private const float BubbleDistance = 14f;
    private float bubbleScale = 1f;
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
        UpdateMark();

        if (Time.time >= nextLine)
        {
            nextLine = Time.time + UnityEngine.Random.Range(30f, 60f);
            var lines = Lines != null ? Lines() : LinesFor?.Invoke(Key);
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
        // the NPC's own scale (a dwarf is a small human) must not shrink the words
        bubbleScale = cam ? Mathf.Clamp(Vector3.Distance(cam.transform.position, bubble.transform.position) / BubbleDistance, 1f, 3f) : 1f;
        bubble.transform.localScale = Vector3.one * bubbleScale / Mathf.Max(0.0001f, transform.lossyScale.y);
        if (cam) bubble.transform.rotation = cam.transform.rotation;
        bubble.alpha = Mathf.Min(Mathf.Clamp01(shownFor / FadeSeconds), Mathf.Clamp01(showLeft / FadeSeconds));
    }

    /// <summary>The mark over their head: looked up twice a second, bobbing gently, turned to the camera, out of the way of a speech bubble.</summary>
    private void UpdateMark()
    {
        if (Time.time >= nextMark)
        {
            nextMark = Time.time + MarkEvery;
            var mark = !SomeoneNear() ? Mark.None : MarkSource != null ? MarkSource() : MarkFor != null ? MarkFor(Key) : Mark.None;
            if (mark != shownMark)
            {
                shownMark = mark;
                var sprite = SpriteFor(mark);
                if (sprite && !marker) marker = MakeMarker();
                if (marker)
                {
                    marker.sprite = sprite;
                    marker.gameObject.SetActive(sprite);
                }
            }
        }

        if (!marker || !marker.gameObject.activeSelf) return;
        var cam = Camera.main;
        var talking = bubble && bubble.gameObject.activeSelf;
        var bob = Mathf.Sin(Time.time * 2.2f + transform.position.x) * 0.08f * WorldMark.Fit(marker, cam, MarkSize);
        marker.transform.position = transform.position + Vector3.up * (HeadHeight + (talking ? 1.9f * bubbleScale : 0.55f) + bob);
        if (cam) marker.transform.rotation = cam.transform.rotation;
    }

    private static Sprite SpriteFor(Mark mark)
    {
        if (mark == Mark.None) return null;
        if (markSprites.TryGetValue(mark, out var sprite)) return sprite;
        // drawn with the Image Generator; Assets/World/Markers/Resources/QuestMarkers
        sprite = Resources.Load<Sprite>("QuestMarkers/" + mark.ToString().ToLowerInvariant());
        markSprites[mark] = sprite;
        return sprite;
    }

    private SpriteRenderer MakeMarker()
    {
        var go = new GameObject("Quest Mark");
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
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
