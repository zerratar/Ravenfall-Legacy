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
    private const float ScreenShare = 0.04f;
    private const float MinHeight = 0.35f;
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
        var tall = Fit(marker, cam, size);
        var bob = Mathf.Sin(Time.time * 2.2f + transform.position.x) * 0.08f * tall;
        marker.transform.position = transform.position + Vector3.up * (height + bob);
        if (cam) marker.transform.rotation = cam.transform.rotation;
    }

    /// <summary>
    /// Sizes a mark to a modest share of the screen whatever the distance: never taller than maxHeight metres (seen from
    /// afar), never smaller than MinHeight (close up, where a fixed size would loom over the head). Returns its height.
    /// </summary>
    public static float Fit(SpriteRenderer mark, Camera cam, float maxHeight)
    {
        var sprite = mark.sprite;
        if (!sprite) return maxHeight;
        var tall = maxHeight;
        if (cam)
        {
            var distance = Vector3.Distance(cam.transform.position, mark.transform.position);
            var view = cam.orthographic ? cam.orthographicSize * 2f : 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            tall = Mathf.Clamp(view * ScreenShare, Mathf.Min(MinHeight, maxHeight), maxHeight);
        }
        var parent = mark.transform.parent ? mark.transform.parent.lossyScale.y : 1f;
        mark.transform.localScale = Vector3.one * tall / Mathf.Max(0.0001f, sprite.bounds.size.y * parent);
        return tall;
    }

    private SpriteRenderer MakeMarker()
    {
        var go = new GameObject("Quest Mark");
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }}
