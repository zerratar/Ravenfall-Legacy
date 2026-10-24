using System;
using UnityEngine;

/// <summary>
/// One piece of village decor: one of the island's own trees, bushes or flowers (it has a default key, given once by
/// tools/village-rebuild/decor-defaults.cs), or a piece the streamer placed (it has the server's id).
/// See docs/village-decor-design.md.
/// </summary>
public class VillageDecorItem : MonoBehaviour
{
    [Tooltip("The island piece's key, the same on the server (VillageDecorDefaults). Empty for a placed piece.")]
    [SerializeField] private string defaultKey;

    [Tooltip("What it is: a key of the decor price list (VillageDecorManager.Pieces on the server).")]
    [SerializeField] private string piece;

    private bool homeKnown;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float radius = -1f;

    public string DefaultKey => defaultKey;
    public string Piece => piece;
    public bool IsDefault => !string.IsNullOrEmpty(defaultKey);

    /// <summary>The server's id for a placed piece, or for an island piece's change once it has one.</summary>
    public Guid Id { get; set; }

    /// <summary>An island piece the village took away.</summary>
    public bool Removed { get; set; }

    /// <summary>A small piece (flowers, grass) under a road or building, hidden until that moves away.</summary>
    public bool Covered { get; set; }

    /// <summary>Flowers, grass, small stones: never in the way of anything.</summary>
    public bool Small { get; set; }

    public void Setup(string defaultKey, string piece)
    {
        this.defaultKey = defaultKey;
        this.piece = piece;
    }

    /// <summary>Where an island piece stands as shipped, to go back to when the village's changes are applied again.</summary>
    public void RememberHome()
    {
        if (homeKnown) return;
        homeKnown = true;
        homePosition = transform.position;
        homeRotation = transform.rotation;
        // measured now, while it is shown: a hidden renderer has no bounds
        _ = Radius;
    }

    public void GoHome()
    {
        if (!homeKnown) return;
        transform.SetPositionAndRotation(homePosition, homeRotation);
    }

    /// <summary>How far it reaches from its middle on the ground, from its renderers, for picking and for what it is in the way of.</summary>
    public float Radius
    {
        get
        {
            if (radius >= 0f) return radius;
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return radius = 0.5f;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            // the trunk and the lower crown, not the whole canopy: a house may stand under a tree's branches' edge
            return radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.5f, 0.3f, 4f);
        }
    }

    public void UpdateVisible()
    {
        var visible = !Removed && !Covered;
        if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
    }
}
