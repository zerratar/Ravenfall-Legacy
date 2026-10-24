using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A tavern regular who talks: every half a minute or so a short line in a speech bubble over their head, picked
/// from their role's lines (<see cref="Localization"/>, separated by |). The quest giver also answers !quest, a
/// teaser for the quests that come in a later update: no tasks yet.
/// </summary>
/// <remarks>
/// The bubble is a world space text with a dark marked background, turned to the camera, fading in and out. The NPCs
/// only run while the tavern is open, as it is switched off otherwise.
/// </remarks>
public sealed class TavernNpc : MonoBehaviour
{
    public enum NpcRole { Guest, Barkeep, QuestGiver, Shopkeeper, GhostBarkeep }

    [SerializeField] private NpcRole role;
    [SerializeField] private float headHeight = 2.2f;

    private const float LineSeconds = 6f;
    private const float FadeSeconds = 0.35f;
    private static readonly List<TavernNpc> all = new List<TavernNpc>();

    private TextMeshPro bubble;
    private float showLeft;
    private float shownFor;
    private float nextLine;

    public NpcRole Role => role;

    /// <summary>The first NPC in the tavern with the given role, or null.</summary>
    public static TavernNpc Find(NpcRole role)
    {
        foreach (var npc in all) if (npc && npc.role == role) return npc;
        return null;
    }

    private void OnEnable()
    {
        all.Add(this);
        nextLine = Time.time + Random.Range(5f, 20f);
    }

    private void OnDisable()
    {
        all.Remove(this);
        showLeft = 0f;
        if (bubble) bubble.gameObject.SetActive(false);
    }

    /// <summary>Shows a line over the NPC's head for a few seconds.</summary>
    public void Say(string text, float seconds = LineSeconds)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (!bubble) bubble = MakeBubble();
        bubble.gameObject.SetActive(true);
        bubble.text = "<mark=#1b130ccc padding=\"14,14,8,8\">" + text + "</mark>";
        showLeft = seconds;
        shownFor = 0f;
        // a line said on demand pushes the next idle one back
        nextLine = Mathf.Max(nextLine, Time.time + seconds + Random.Range(15f, 30f));
    }

    private void Update()
    {
        if (Time.time >= nextLine)
        {
            nextLine = Time.time + Random.Range(28f, 55f);
            var lines = Lines();
            if (lines.Length > 0) Say(lines[Random.Range(0, lines.Length)]);
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
        bubble.transform.position = transform.position + Vector3.up * headHeight;
        if (cam) bubble.transform.rotation = cam.transform.rotation;
        var fadeIn = Mathf.Clamp01(shownFor / FadeSeconds);
        var fadeOut = Mathf.Clamp01(showLeft / FadeSeconds);
        bubble.alpha = Mathf.Min(fadeIn, fadeOut);
    }

    private string[] Lines()
    {
        var text = role switch
        {
            NpcRole.Barkeep => Localization.UI_TAVERN_BARKEEP_LINES,
            NpcRole.GhostBarkeep => Localization.UI_TAVERN_GHOST_BARKEEP_LINES,
            NpcRole.QuestGiver => Localization.UI_TAVERN_QUEST_GIVER_LINES,
            NpcRole.Shopkeeper => Localization.UI_TAVERN_SHOPKEEPER_LINES,
            _ => Localization.UI_TAVERN_GUEST_LINES,
        };
        return string.IsNullOrEmpty(text) ? new string[0] : text.Split('|');
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
