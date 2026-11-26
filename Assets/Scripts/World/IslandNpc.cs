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

    /// <summary>At this far from the camera a speech bubble has its own size; further off it grows (to 3 times), so it stays
    /// readable, and closer it shrinks (to a third, a camera right by the character played by hand).</summary>
    private const float BubbleDistance = 14f;
    private float bubbleScale = 1f;

    /// <summary>
    /// The least size of a line's letters on screen, in pixels of a 1080 high picture (more on a taller one): readable on
    /// stream from wherever the camera is (playtest 2026-11-26: over a spread-out tower town, the tower camera 46 m up,
    /// the lines were a few pixels high). Up close the size above wins, so a line never looms over the character.
    /// </summary>
    private const float MinLinePixels = 20f;
    /// <summary>Two people this close never speak at once: from the tower camera's height their lines lay over each other.</summary>
    private const float QuietNear = 30f;
    private const float BubbleFontSize = 1.6f;
    /// <summary>A world text's letters stand this many metres high per point of its font size (TextMeshPro's 3D text).</summary>
    private const float MetresPerPoint = 0.1f;
    private static TMP_FontAsset boldFont;
    private static bool boldFontLooked;
    /// <summary>The bubbles' one material: the font's own with a soft dark shadow under the letters, drawn over the world.</summary>
    private static Material bubbleMaterial;
    private static readonly List<IslandNpc> all = new List<IslandNpc>();

    private TextMeshPro bubble;
    private float showLeft;
    private float shownFor;
    private float nextLine;

    public static IReadOnlyList<IslandNpc> All => all;

    /// <summary>The gap between the head and the mark, and between the mark and a speech bubble (metres).</summary>
    private const float MarkGap = 0.12f;

    /// <summary>The mark over their head is showing: its middle and its height now (it is sized to the screen).</summary>
    public bool MarkShown => marker && marker.gameObject.activeSelf;
    public Vector3 MarkCenter { get; private set; }
    public float MarkHeight { get; private set; }

    /// <summary>Just over their head: the "Talk to" prompt floats here when there is no mark.</summary>
    public Vector3 HeadTop => transform.position + Vector3.up * HeadHeight;

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

    /// <summary>Talked to (the dialogue box): no idle lines over their head meanwhile, and the one showing goes.</summary>
    public bool Hushed
    {
        get => hushed;
        set
        {
            hushed = value;
            if (value && bubble) bubble.gameObject.SetActive(false);
        }
    }
    private bool hushed;

    /// <summary>Shows a line over their head for a few seconds.</summary>
    public void Say(string text, float seconds = LineSeconds)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (!bubble) bubble = MakeBubble();
        bubble.gameObject.SetActive(true);
        // cream letters on a soft dark shadow of their own (a box behind them came out a dull gray-olive in the world's
        // light and post effects, and the letters with it: hard to read on snow, playtest 2026-11-26)
        bubble.text = text;
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
            // only where someone might hear it: a player on the same island; one at a time among neighbors (wait a little)
            if (lines != null && lines.Count > 0 && !hushed && SomeoneNear())
            {
                if (NeighborSpeaking()) nextLine = Time.time + UnityEngine.Random.Range(4f, 9f);
                else Say(lines[UnityEngine.Random.Range(0, lines.Count)]);
            }
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
        // over the mark (which stays where it is), else over the head
        bubble.transform.position = HeadTop + Vector3.up * (MarkShown ? MarkGap + MarkHeight + MarkGap : MarkGap);
        // the NPC's own scale (a dwarf is a small human) must not shrink the words
        var distance = cam ? Vector3.Distance(cam.transform.position, bubble.transform.position) : BubbleDistance;
        bubbleScale = cam ? Mathf.Clamp(distance / BubbleDistance, 0.32f, 3f) : 1f;
        if (cam && !cam.orthographic)
        {
            // never under MinLinePixels on screen: the height the view covers at that distance, shared out over 1080
            var view = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            bubbleScale = Mathf.Max(bubbleScale, MinLinePixels / 1080f * view / (BubbleFontSize * MetresPerPoint));
        }
        bubble.transform.localScale = Vector3.one * bubbleScale / Mathf.Max(0.0001f, transform.lossyScale.y);
        if (cam) bubble.transform.rotation = cam.transform.rotation;
        bubble.alpha = Mathf.Min(Mathf.Clamp01(shownFor / FadeSeconds), Mathf.Clamp01(showLeft / FadeSeconds));
    }

    /// <summary>
    /// The mark over their head: looked up twice a second, bobbing gently, turned to the camera. Its foot stays just over
    /// the head at any distance (it is sized to the screen, so a fixed middle drifted off the head close up), and a speech
    /// bubble goes over it rather than pushing it up.
    /// </summary>
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
        var tall = WorldMark.Fit(marker, cam, MarkSize);
        var bob = Mathf.Sin(Time.time * 2.2f + transform.position.x) * 0.08f * tall;
        MarkHeight = tall;
        MarkCenter = HeadTop + Vector3.up * (MarkGap + tall * 0.5f);
        marker.transform.position = MarkCenter + Vector3.up * bob;
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

    private bool NeighborSpeaking()
    {
        foreach (var other in all)
        {
            if (!other || other == this || !other.bubble || !other.bubble.gameObject.activeSelf) continue;
            if ((other.transform.position - transform.position).sqrMagnitude < QuietNear * QuietNear) return true;
        }
        return false;
    }

    private bool SomeoneNear()
    {
        var cam = Camera.main;
        return cam && Vector3.Distance(cam.transform.position, transform.position) < 80f;
    }

    /// <summary>The bold Rodin (FOT-NewRodin Pro B SDF) as loaded for the world's signboards and the tower's stone words; looked up once.</summary>
    private static TMP_FontAsset BoldFont()
    {
        if (boldFontLooked) return boldFont;
        boldFontLooked = true;
        foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (font && font.name == "FOT-NewRodin Pro B SDF")
            {
                boldFont = font;
                break;
            }
        }
        return boldFont;
    }

    private TextMeshPro MakeBubble()
    {
        var go = new GameObject("Speech Bubble");
        go.transform.SetParent(transform, false);
        var text = go.AddComponent<TextMeshPro>();
        // the bold Rodin of the game's other world words (stream readable), else the default font made bold
        var bold = BoldFont();
        if (bold) text.font = bold;
        else text.fontStyle = FontStyles.Bold;
        if (!bubbleMaterial && text.font && text.font.material)
        {
            bubbleMaterial = new Material(text.font.material) { name = "Speech Bubble", hideFlags = HideFlags.DontSave };
            bubbleMaterial.EnableKeyword("UNDERLAY_ON");
            bubbleMaterial.SetColor("_UnderlayColor", new Color(0.06f, 0.04f, 0.02f, 0.9f));
            bubbleMaterial.SetFloat("_UnderlayDilate", 0.3f);
            bubbleMaterial.SetFloat("_UnderlaySoftness", 0.6f);
            // over everything, as a name tag is: grown for a far camera, a line reached through the roof or the wall
            // in front of its speaker and was cut off there
            bubbleMaterial.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
            bubbleMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay;
        }
        if (bubbleMaterial) text.fontSharedMaterial = bubbleMaterial;
        text.fontSize = BubbleFontSize;
        text.alignment = TextAlignmentOptions.Bottom;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.sizeDelta = new Vector2(3.2f, 1.5f);
        text.rectTransform.pivot = new Vector2(0.5f, 0f);
        text.color = new Color(1f, 0.94f, 0.82f);
        text.outlineWidth = 0f;
        return text;
    }
}
