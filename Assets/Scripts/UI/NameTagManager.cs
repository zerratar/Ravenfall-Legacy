using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

// after every NameTag's LateUpdate, so the tags are laid out where they are drawn this frame
[DefaultExecutionOrder(100)]
public class NameTagManager : MonoBehaviour
{
    //private readonly object mutex = new object();
    private readonly List<NameTag> nameTags = new List<NameTag>();

    [SerializeField] private GameObject nameTagPrefab;
    [SerializeField] private PlayerLogoManager logoManager;

    private bool? nameTagsWasEnabled = null;
    private readonly List<(NameTag Tag, Rect Rect, float Priority)> layout = new List<(NameTag, Rect, float)>();
    private readonly List<Rect> placed = new List<Rect>();
    private Camera layoutCamera;
    internal bool NameTagsEnabled;
    public PlayerLogoManager LogoManager => logoManager;

    private void Awake()
    {
        if (!logoManager) logoManager = FindAnyObjectByType<PlayerLogoManager>();
    }

    private void Update()
    {
        if (nameTagsWasEnabled != NameTagsEnabled)
        {
            foreach (var nt in nameTags)
            {
                nt.gameObject.SetActive(NameTagsEnabled);
            }
        }

        nameTagsWasEnabled = NameTagsEnabled;
    }

    /// <summary>
    /// Keeps the tags readable when players crowd together (a dungeon room, a raid boss, the
    /// tavern door): each frame the tags are placed on screen in turn, the followed player first,
    /// then the streamer, then nearest the camera first, and a tag that would cover one already
    /// placed fades out until there is room for it again. Before this a crowd's names piled into
    /// one unreadable smear.
    /// </summary>
    private void LateUpdate()
    {
        if (!NameTagsEnabled || !GraphicsToggler.GraphicsEnabled || nameTags.Count == 0) return;
        if (!layoutCamera || !layoutCamera.isActiveAndEnabled) layoutCamera = Camera.main;
        if (!layoutCamera) return;

        var gm = GameManager.Instance;
        var observed = gm && gm.Camera && !gm.Camera.IsFreeCameraActive && gm.ObservedPlayerDetails ? gm.ObservedPlayerDetails.ObservedPlayer : null;
        var cameraPosition = layoutCamera.transform.position;
        var deltaTime = Time.unscaledDeltaTime;

        layout.Clear();
        foreach (var tag in nameTags)
        {
            if (!tag || !tag.isActiveAndEnabled) continue;
            if (!tag.TryGetScreenRect(layoutCamera, out var rect))
            {
                tag.FadeTo(1f, deltaTime);
                continue;
            }

            var player = tag.TargetPlayer;
            float priority;
            if (player && player == observed) priority = float.MinValue;
            else if (player && player.IsBroadcaster) priority = -1e9f;
            else
            {
                // a tag already showing keeps its place against a slightly nearer one, so two
                // players walking side by side do not swap tags back and forth
                priority = (tag.transform.position - cameraPosition).sqrMagnitude;
                if (tag.Alpha > 0.5f) priority *= 0.6f;
            }

            layout.Add((tag, rect, priority));
        }

        layout.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        placed.Clear();
        foreach (var (tag, rect, _) in layout)
        {
            // a little overlap is fine, the edges of the glyph boxes are mostly empty
            var inner = new Rect(rect.x + rect.width * 0.05f, rect.y + rect.height * 0.12f, rect.width * 0.9f, rect.height * 0.76f);
            var free = true;
            for (var i = 0; i < placed.Count; i++)
            {
                if (placed[i].Overlaps(inner)) { free = false; break; }
            }

            if (free) placed.Add(inner);
            tag.FadeTo(free ? 1f : 0f, deltaTime);
        }
    }

    public void Remove(PlayerController player)
    {
        //lock (mutex)
        {
            var nameTag = nameTags.FirstOrDefault(x => x.TargetPlayer == player);
            if (nameTag)
            {
                Destroy(nameTag.gameObject);
                nameTags.Remove(nameTag);
            }
        }
    }

    public void Remove(NameTag nameTag)
    {
        if (nameTag)
        {
            Destroy(nameTag.gameObject);
            nameTags.Remove(nameTag);
        }
    }

    public void Remove(Transform transform)
    {
        //lock (mutex)
        {
            var nameTag = nameTags.FirstOrDefault(x => x.TargetTransform.GetEntityId() == transform.GetEntityId());
            if (nameTag)
            {
                Destroy(nameTag.gameObject);
                nameTags.Remove(nameTag);
            }
        }
    }

    public NameTag Add(Transform transform)
    {
        if (!transform) return null;
        if (!nameTagPrefab) return null;
        var targetName = transform.name;
        //lock (mutex)
        //{
        //    if (nameTags.Any(x => x.TargetTransform.GetInstanceID() == transform.GetInstanceID()))
        //    {
        //        Shinobytes.Debug.LogWarning($"{targetName} already have an assigned name tag.");
        //        return null;
        //    }
        //}
        return AddNameTag(null, targetName, transform);
    }

    public NameTag Get(PlayerController player)
    {
        if (!player) return null;
        return nameTags.FirstOrDefault(x => x.TargetPlayer?.Id == player.Id);
    }

    public NameTag Add(PlayerController player)
    {
        if (!player) return null;
        if (!nameTagPrefab) return null;

        if (nameTags.Any(x => x.TargetPlayer == player))
        {
            Shinobytes.Debug.LogWarning($"{player.PlayerName} already have an assigned name tag.");
            return null;
        }

        var targetName = player.PlayerName;
        var targetTransform = player.Transform;

        return AddNameTag(player, targetName, targetTransform);
    }

    private NameTag AddNameTag(PlayerController player, string targetName, Transform targetTransform)
    {
        var obj = Instantiate(nameTagPrefab, transform);
        if (!obj)
        {
            Shinobytes.Debug.LogError($"Failed to add a nametag for {targetName}");
            return null;
        }

        var nameTag = obj.GetComponent<NameTag>();
        nameTag.TargetPlayer = player;
        nameTag.TargetTransform = targetTransform;
        nameTag.Manager = this;
        nameTag.HasTargetPlayer = true;

        nameTags.Add(nameTag);

        nameTag.gameObject.SetActive(NameTagsEnabled);

        return nameTag;
    }
}
