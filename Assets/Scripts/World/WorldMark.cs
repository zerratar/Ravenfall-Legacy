using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A quest mark floating over something in the world that is not an island NPC: the Ironhill runestone, the monkey
/// chief's mangrove island, the tavern's quest giver. The same marks and look as <see cref="IslandNpc"/>'s
/// (docs/mockups/quest-markers.jpg): a gold ! for something to begin, a gold ? where the story goes on, a grey ? while
/// it is under way. Looked up twice a second from its source, bobbing gently, turned to the camera.
/// </summary>
public sealed class WorldMark : MonoBehaviour
{
    private const float Size = 1.2f;
    private const float Every = 0.5f;
    private static readonly Dictionary<IslandNpc.Mark, Sprite> sprites = new Dictionary<IslandNpc.Mark, Sprite>();

    private Func<IslandNpc.Mark> source;
    private float height;
    private float size = Size;
    private SpriteRenderer marker;
    private IslandNpc.Mark shown = IslandNpc.Mark.None;
    private float next;

    /// <summary>Puts a mark over a thing (or changes its source), the given height above it, size metres tall.</summary>
    public static WorldMark Attach(GameObject host, float height, Func<IslandNpc.Mark> source, float size = Size)
    {
        if (!host) return null;
        var mark = host.GetComponent<WorldMark>() ?? host.AddComponent<WorldMark>();
        mark.source = source;
        mark.height = height;
        mark.size = size;
        mark.next = 0f;
        if (mark.marker) mark.marker.transform.localScale = mark.LocalScale();
        return mark;
    }

    public static Sprite SpriteFor(IslandNpc.Mark mark)
    {
        if (mark == IslandNpc.Mark.None) return null;
        if (sprites.TryGetValue(mark, out var sprite)) return sprite;
        // drawn with the Image Generator; Assets/World/Markers/Resources/QuestMarkers
        sprite = Resources.Load<Sprite>("QuestMarkers/" + mark.ToString().ToLowerInvariant());
        sprites[mark] = sprite;
        return sprite;
    }

    private void Update()
    {
        var cam = Camera.main;
        if (Time.time >= next)
        {
            next = Time.time + Every;
            var mark = source != null ? source() : IslandNpc.Mark.None;
            if (mark != shown)
            {
                shown = mark;
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
        var bob = Mathf.Sin(Time.time * 2.2f + transform.position.x) * 0.08f * size;
        marker.transform.position = transform.position + Vector3.up * (height + bob);
        if (cam) marker.transform.rotation = cam.transform.rotation;
    }

    private SpriteRenderer MakeMarker()
    {
        var go = new GameObject("Quest Mark");
        go.transform.SetParent(transform, false);
        go.transform.localScale = LocalScale();
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private Vector3 LocalScale() => Vector3.one * size / Mathf.Max(0.0001f, transform.lossyScale.x);
}
