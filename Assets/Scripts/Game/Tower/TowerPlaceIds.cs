using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     The fixed ids of what a tower place's builder made (tools/tower/*-build.cs), and how each thing stood when it was
///     built: the baseline that Ravenfall > Tower Places compares the scene with to write the owner's hand edits to
///     tools/tower/layouts/&lt;Scene&gt;.edits.json, which a rebuild puts back (docs/tower-places-editor.md).
///     <para>One of these per tower scene, on a root object of its own tagged EditorOnly, so a player build leaves it
///     out. An id is the object's place in the hierarchy when it was stamped (names, with "#n" for the n-th of the same
///     name under one parent), so a rebuild by the same script gives every object the same id. The ids live here and
///     not on the objects, so a copy made with Ctrl+D is a new object rather than a second one with the same id.</para>
///     <para>Nothing in the game reads this.</para>
/// </summary>
public sealed class TowerPlaceIds : MonoBehaviour
{
    public const string ObjectName = "Tower Place Ids (editor only)";

    [Serializable]
    public struct Entry
    {
        /// <summary>The object (null once it was deleted by hand).</summary>
        public GameObject Object;
        public string Id;
        /// <summary>Its parent's id when it was built (a part of a prefab copy has "&gt;" in its id), empty at the scene's root.</summary>
        public string ParentId;
        /// <summary>
        ///     What it is a copy of: "prefab:GUID" for a prefab copy, "copy:ID" for a duplicate of another object (added by
        ///     hand), empty for anything else.
        /// </summary>
        public string Source;
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool Active;
        /// <summary>Added by hand (its id is the change list's), not made by the builder: no baseline.</summary>
        public bool Added;
    }

    /// <summary>
    ///     A part of a prefab copy (a wall piece of a house preset) that the builder left different from the prefab: its
    ///     own baseline. Every other part's baseline is the prefab's.
    /// </summary>
    [Serializable]
    public struct Inner
    {
        public string Id;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool Active;
    }

    public List<Entry> Entries = new List<Entry>();
    public List<Inner> Inners = new List<Inner>();

    /// <summary>When the ids were stamped, and by what.</summary>
    public string Stamped = "";
}
