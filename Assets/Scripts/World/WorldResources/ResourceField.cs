using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The trees or gather spots of one chunk, as data: for each, its type, look, position, turn and size. No
/// GameObject per tree; <see cref="ResourceRenderer"/> draws every field instanced, in the editor as well as in
/// play mode, and the game's resource system keeps whether each one is standing or used up.
/// </summary>
/// <remarks>
/// Positions are in world space, on the ground where they were placed. Only this data is saved with the scene.
/// Put the field on the chunk it belongs to (or a child of it), so the chunk knows its trees. In play mode the
/// field hands out one <see cref="ResourceTarget"/> per instance and brings used up ones back when due; the
/// instances are not meant to change while the game runs.
/// </remarks>
[ExecuteAlways]
[DisallowMultipleComponent]
public class ResourceField : MonoBehaviour
{
    [Serializable]
    public struct Instance
    {
        /// <summary>Index into the field's <see cref="types"/>.</summary>
        public ushort type;
        /// <summary>Index into the type's variants.</summary>
        public byte variant;
        public Vector3 position;
        /// <summary>Turn about the up axis, in degrees.</summary>
        public float yaw;
        public float scale;

        public Matrix4x4 Matrix => Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one * (scale > 0f ? scale : 1f));
    }

    [SerializeField] private List<ResourceType> types = new List<ResourceType>();
    [SerializeField] private List<Instance> instances = new List<Instance>();

    // how tough and how quick to come back the things here are, when the chunk wants other than their types
    [Tooltip("Hits to cut a tree here down; 0 uses each type's own.")]
    [SerializeField, Min(0)] private int treeHealth;
    [Tooltip("Seconds until a tree or spot here is back, before the chunk's adaptive respawn speeds it up; 0 uses each type's own.")]
    [SerializeField, Min(0f)] private float respawnSeconds;

    // whether each instance is used up (a stump, or a gathered spot), kept by the game while it runs
    [NonSerialized] private bool[] depleted = Array.Empty<bool>();

    // the game's handles on the instances, made on first use, and the used up ones waiting to come back
    [NonSerialized] private ResourceTarget[] targets;
    [NonSerialized] private readonly List<ResourceTarget> respawning = new List<ResourceTarget>();
    [NonSerialized] private float nextRespawn = float.MaxValue;

    public IReadOnlyList<ResourceType> Types => types;

    /// <summary>Hits to cut a tree here down, or 0 for each type's own.</summary>
    public int TreeHealth
    {
        get => treeHealth;
        set => treeHealth = Mathf.Max(0, value);
    }

    /// <summary>Seconds until a used up instance is back, or 0 for each type's own.</summary>
    public float RespawnSeconds
    {
        get => respawnSeconds;
        set => respawnSeconds = Mathf.Max(0f, value);
    }
    public IReadOnlyList<Instance> Instances => instances;
    public int Count => instances.Count;

    /// <summary>Bumped on every change to the instances or types, so the renderer knows to rebuild.</summary>
    public int Version { get; private set; }

    public ResourceType TypeOf(int index) => index >= 0 && index < instances.Count && instances[index].type < types.Count ? types[instances[index].type] : null;

    public bool IsDepleted(int index) => index >= 0 && index < depleted.Length && depleted[index];

    /// <summary>
    /// One target per instance, for the chunk's tasks. Each knows its island and the level its chunk requires,
    /// which is the level a tree is chopped against.
    /// </summary>
    public IReadOnlyList<ResourceTarget> Targets
    {
        get
        {
            if (targets == null || targets.Length != instances.Count) MakeTargets();
            return targets;
        }
    }

    private void MakeTargets()
    {
        var chunk = GetComponentInParent<Chunk>(true);
        var island = GetComponentInParent<IslandController>(true);
        var level = chunk ? Mathf.Max(1, chunk.GetRequiredSkillLevel()) : 1;
        targets = new ResourceTarget[instances.Count];
        for (var i = 0; i < instances.Count; ++i) targets[i] = new ResourceTarget(this, i, TypeOf(i), instances[i].position, island, level);
        respawning.Clear();
        nextRespawn = float.MaxValue;
    }

    internal void QueueRespawn(ResourceTarget target)
    {
        respawning.Add(target);
        if (target.RespawnAt < nextRespawn) nextRespawn = target.RespawnAt;
    }

    private void Update()
    {
        // nothing to do until the next one is due
        if (respawning.Count == 0 || Time.time < nextRespawn) return;
        var now = Time.time;
        nextRespawn = float.MaxValue;
        for (var i = respawning.Count - 1; i >= 0; --i)
        {
            var target = respawning[i];
            if (target.RespawnAt <= now)
            {
                respawning.RemoveAt(i);
                target.Respawn();
            }
            else if (target.RespawnAt < nextRespawn)
            {
                nextRespawn = target.RespawnAt;
            }
        }
    }

    /// <summary>Marks an instance used up or back; the renderer shows its depleted look (a stump) or its own.</summary>
    public void SetDepleted(int index, bool value)
    {
        if (index < 0 || index >= instances.Count) return;
        if (depleted.Length != instances.Count) Array.Resize(ref depleted, instances.Count);
        depleted[index] = value;
        ResourceRenderer.SetDepleted(this, index, value);
    }

    /// <summary>The index of a type in this field, added if it is new.</summary>
    public ushort TypeIndex(ResourceType type)
    {
        var at = types.IndexOf(type);
        if (at < 0)
        {
            types.Add(type);
            at = types.Count - 1;
        }
        return (ushort)at;
    }

    public int Add(ResourceType type, int variant, Vector3 position, float yaw, float scale)
    {
        instances.Add(new Instance { type = TypeIndex(type), variant = (byte)Mathf.Clamp(variant, 0, 255), position = position, yaw = yaw, scale = scale });
        Changed();
        return instances.Count - 1;
    }

    public void Set(int index, Instance instance)
    {
        if (index < 0 || index >= instances.Count) return;
        instances[index] = instance;
        Changed();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= instances.Count) return;
        instances.RemoveAt(index);
        Changed();
    }

    /// <summary>Removes every instance that matches, in one change; returns how many went.</summary>
    public int RemoveAll(Predicate<Instance> match)
    {
        var removed = instances.RemoveAll(match);
        if (removed > 0) Changed();
        return removed;
    }

    public void Clear()
    {
        instances.Clear();
        Changed();
    }

    /// <summary>Call after changing the instances or types in another way, so they are drawn again.</summary>
    public void Changed()
    {
        Version++;
        if (depleted.Length != instances.Count) Array.Resize(ref depleted, instances.Count);
        ResourceRenderer.MarkDirty();
    }

    private void OnEnable()
    {
        if (depleted.Length != instances.Count) depleted = new bool[instances.Count];
        ResourceRenderer.Register(this);
    }

    private void OnDisable()
    {
        ResourceRenderer.Unregister(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // an undo, a paste or an inspector edit changed the data
        Version++;
        ResourceRenderer.MarkDirty();
    }
#endif
}
