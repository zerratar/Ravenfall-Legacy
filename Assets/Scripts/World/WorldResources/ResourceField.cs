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
/// Put the field on the chunk it belongs to (or a child of it), so the chunk knows its trees.
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

    // whether each instance is used up (a stump, or a gathered spot), kept by the game while it runs
    [NonSerialized] private bool[] depleted = Array.Empty<bool>();

    public IReadOnlyList<ResourceType> Types => types;
    public IReadOnlyList<Instance> Instances => instances;
    public int Count => instances.Count;

    /// <summary>Bumped on every change to the instances or types, so the renderer knows to rebuild.</summary>
    public int Version { get; private set; }

    public ResourceType TypeOf(int index) => index >= 0 && index < instances.Count && instances[index].type < types.Count ? types[instances[index].type] : null;

    public bool IsDepleted(int index) => index >= 0 && index < depleted.Length && depleted[index];

    /// <summary>Marks an instance used up or back; the renderer shows its depleted look (a stump) or its own.</summary>
    public void SetDepleted(int index, bool value)
    {
        if (index < 0 || index >= instances.Count) return;
        if (depleted.Length != instances.Count) Array.Resize(ref depleted, instances.Count);
        depleted[index] = value;
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
