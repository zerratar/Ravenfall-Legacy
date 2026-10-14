using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A kind of tree or gather spot: how it looks, how it changes when used, and how it behaves. The things
/// themselves are not GameObjects; a <see cref="ResourceField"/> holds where each one stands, and
/// <see cref="ResourceRenderer"/> draws them all instanced.
/// </summary>
/// <remarks>
/// A tree's level is not set here: it is the level its chunk requires, so a chunk's trees always match
/// what players need to train there. Experience comes from the player, island and chunk, as it always has.
/// The looks are baked meshes, one per level of detail, made from objects or prefabs by Ravenfall &gt; World
/// Resources &gt; Create Tree Type From Selection (or Gather). A field can make its trees tougher or slower to
/// come back than the type, as some chunks do.
/// </remarks>
[CreateAssetMenu(menuName = "Ravenfall/World Resource Type", fileName = "Resource Type")]
public class ResourceType : ScriptableObject
{
    public enum ResourceKind { Tree, Gatherable }

    /// <summary>One level of detail of a look: a mesh, its materials (one per submesh), and how far it shows.</summary>
    [Serializable]
    public class Lod
    {
        public Mesh mesh;
        public Material[] materials = Array.Empty<Material>();
        [Tooltip("Shown up to this distance from the camera, in metres at a 60 degree field of view; 0 for no limit. Past the last level, nothing is drawn.")]
        public float maxDistance;
    }

    /// <summary>A way the resource can look, picked per instance; or the look once it is used up.</summary>
    [Serializable]
    public class Look
    {
        public string name;
        [Tooltip("How often the brush picks this look, against the others.")]
        public float weight = 1f;
        public Lod[] lods = Array.Empty<Lod>();

        public bool IsEmpty => lods == null || lods.Length == 0 || lods[0] == null || !lods[0].mesh;
    }

    public ResourceKind kind = ResourceKind.Tree;

    [Tooltip("The looks an instance can have; each instance has one, picked when it is placed.")]
    public Look[] variants = Array.Empty<Look>();

    [Tooltip("The look once cut down or gathered: the stump of a tree. Empty for a gather spot that vanishes until it regrows.")]
    public Look depleted = new Look { name = "Depleted" };

    [Header("Gameplay")]
    [Tooltip("Hits to cut a tree down. A gather spot is used up in one gather.")]
    [Min(1)] public int health = 15;
    [Tooltip("Seconds until it is back, before the chunk's adaptive respawn speeds it up.")]
    [Min(1f)] public float respawnSeconds = 20f;
    [Tooltip("How close a player must stand to chop or gather, in metres.")]
    [Min(0.5f)] public float actionDistance = 3f;
    [Tooltip("Gather spots: whether the player kneels to gather.")]
    public bool kneelToGather = true;

    [Header("Drawing")]
    public ShadowCastingMode castShadows = ShadowCastingMode.On;

    [Header("Brush")]
    public bool randomYaw = true;
    public Vector2 scaleRange = new Vector2(0.9f, 1.1f);

    /// <summary>Raised when the type changes in the editor, so fields using it are drawn again.</summary>
    public static event Action<ResourceType> Changed;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Changed?.Invoke(this);
    }
#endif

    /// <summary>A look picked by the brush weights, from a 0 to 1 random value.</summary>
    public int PickVariant(float random)
    {
        if (variants == null || variants.Length == 0) return 0;
        var total = 0f;
        foreach (var v in variants) total += Mathf.Max(0f, v.weight);
        if (total <= 0f) return Mathf.Clamp((int)(random * variants.Length), 0, variants.Length - 1);
        var at = random * total;
        for (var i = 0; i < variants.Length; ++i)
        {
            at -= Mathf.Max(0f, variants[i].weight);
            if (at <= 0f) return i;
        }
        return variants.Length - 1;
    }
}
