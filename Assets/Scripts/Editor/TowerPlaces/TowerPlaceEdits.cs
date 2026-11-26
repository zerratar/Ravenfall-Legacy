using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
///     The owner's hand edits of a tower place, kept through a rebuild (docs/tower-places-editor.md).
///     <para>Every object a builder makes gets a fixed id (<see cref="Stamp"/>, into the scene's
///     <see cref="TowerPlaceIds"/>, with how it stood when built). When a tower scene is saved, what differs from that is
///     written to tools/tower/layouts/&lt;Scene&gt;.edits.json: objects moved, turned, scaled, put under another parent,
///     renamed, hidden or shown; objects deleted; objects added (a prefab by its GUID, a duplicate of an object of the
///     place by that object's id, anything else as an empty object), with where they stand. Parts of a prefab copy (a
///     wall piece of a house preset) count too, by the copy's id and the part's place in the prefab. A builder calls
///     <see cref="AfterBuild"/> at its end: ids for the new scene, the list put back, the walking ground baked again when
///     anything changed. An edit that cannot be put back (its object is no longer built) is kept in the file under
///     "unapplied" and shown in the window, never dropped silently.</para>
///     <para>Not covered: terrain sculpting and painting, materials, a component's own values (a light's color, a
///     collider's size, a script's fields), the order of children, the inside of an added copy, and anything outside the
///     scene file (meshes, prefabs). An object added from scratch (not a prefab and not a duplicate) comes back as an
///     empty object with its name and place only.</para>
/// </summary>
public static class TowerPlaceEdits
{
    public const string Folder = "tools/tower/layouts";

    public static string FileFor(string sceneName) => Folder + "/" + sceneName + ".edits.json";

    // ---- the change list ----------------------------------------------------------------------------------------

    [Serializable]
    public sealed class Edit
    {
        /// <summary>Only under "unapplied": what it was ("changed", "deleted" or "added").</summary>
        public string what = "";
        public string id = "";
        public string name = "";
        public string rename = "";
        public string parent = "";
        public string prefab = "";
        public string guid = "";
        public string copyOf = "";
        public float[] position = new float[0];
        public float[] rotation = new float[0];
        public float[] scale = new float[0];
        public float[] built = new float[0];
        /// <summary>"on" or "off" when it was shown or hidden (for an added one: how it is), else empty.</summary>
        public string active = "";
        /// <summary>For the owner: what this edit does not bring back, or why it was not put back.</summary>
        public string note = "";

        public bool HasTransform => position != null && position.Length >= 3;
    }

    [Serializable]
    public sealed class EditFile
    {
        public string scene = "";
        public string about = "";
        public List<Edit> changed = new List<Edit>();
        public List<Edit> deleted = new List<Edit>();
        public List<Edit> added = new List<Edit>();
        public List<Edit> unapplied = new List<Edit>();

        public int Count => changed.Count + deleted.Count + added.Count + unapplied.Count;
    }

    public static EditFile Load(string sceneName)
    {
        var path = FileFor(sceneName);
        try
        {
            if (File.Exists(path)) return JsonUtility.FromJson<EditFile>(File.ReadAllText(path)) ?? new EditFile { scene = sceneName };
        }
        catch (Exception e) { Debug.LogWarning("Tower Places: could not read " + path + ": " + e.Message); }
        return new EditFile { scene = sceneName };
    }

    private static string Line(Edit e)
    {
        var sb = new StringBuilder("{");
        void S(string key, string value, bool always = false)
        {
            if (!always && string.IsNullOrEmpty(value)) return;
            if (sb.Length > 1) sb.Append(", ");
            sb.Append('"').Append(key).Append("\": ").Append(TowerPlaceJson.Str(value));
        }
        void V(string key, float[] v)
        {
            if (v == null || v.Length < 3) return;
            sb.Append(", \"").Append(key).Append("\": ").Append(TowerPlaceJson.Vec(new Vector3(v[0], v[1], v[2])));
        }
        S("what", e.what);
        S("id", e.id, true);
        S("name", e.name);
        S("rename", e.rename);
        S("parent", e.parent);
        S("prefab", e.prefab);
        S("guid", e.guid);
        S("copyOf", e.copyOf);
        V("position", e.position);
        V("rotation", e.rotation);
        V("scale", e.scale);
        S("active", e.active);
        V("built", e.built);
        S("note", e.note);
        return sb.Append('}').ToString();
    }

    public static void Save(string sceneName, EditFile file)
    {
        var path = FileFor(sceneName);
        if (file.Count == 0 && !File.Exists(path)) return;
        Directory.CreateDirectory(Folder);
        var sb = new StringBuilder();
        sb.Append("{\n  \"scene\": ").Append(TowerPlaceJson.Str(sceneName)).Append(",\n");
        sb.Append("  \"about\": ").Append(TowerPlaceJson.Str("The owner's hand edits of " + sceneName + ", written by Ravenfall > Tower Places when the scene is saved and put back after a rebuild (docs/tower-places-editor.md). Positions, turns (degrees) and scales are local, under the parent.")).Append(",\n");
        void List(string key, List<Edit> list, bool last)
        {
            sb.Append("  \"").Append(key).Append("\": [");
            if (list.Count == 0) sb.Append("]");
            else
            {
                sb.Append('\n');
                for (var i = 0; i < list.Count; i++) sb.Append("    ").Append(Line(list[i])).Append(i + 1 < list.Count ? ",\n" : "\n");
                sb.Append("  ]");
            }
            sb.Append(last ? "\n" : ",\n");
        }
        List("changed", file.changed, false);
        List("deleted", file.deleted, false);
        List("added", file.added, false);
        List("unapplied", file.unapplied, true);
        sb.Append("}\n");
        File.WriteAllText(path, sb.ToString());
    }

    // ---- ids --------------------------------------------------------------------------------------------------------

    public static TowerPlaceIds Registry(Scene scene, bool create)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var ids = root.GetComponent<TowerPlaceIds>();
            if (ids) return ids;
        }
        if (!create) return null;
        var go = new GameObject(TowerPlaceIds.ObjectName);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.tag = "EditorOnly";
        go.transform.SetAsLastSibling();
        return go.AddComponent<TowerPlaceIds>();
    }

    private static string Clean(string name) => name.Replace('/', '|').Replace('>', ')');

    private static readonly Regex CopySuffix = new Regex(@"(\s*\(\d+\)|\s*\(Clone\))+$");

    internal static string BaseName(string name) => CopySuffix.Replace(name, "");

    /// <summary>A part of a prefab copy: it comes from the prefab (not the copy's root, not added to it by hand).</summary>
    internal static bool IsInner(GameObject go)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(go)) return false;
        if (PrefabUtility.IsOutermostPrefabInstanceRoot(go)) return false;
        if (PrefabUtility.IsAddedGameObjectOverride(go)) return false;
        return PrefabUtility.GetCorrespondingObjectFromSource(go) != null;
    }

    /// <summary>
    ///     A character or monster (an Animator): only the whole of it counts, not its bones (a copied monster's pose is
    ///     not the same from one build to the next).
    /// </summary>
    private static bool Rigged(GameObject go) => go.GetComponent<Animator>();

    private static bool Skip(GameObject go) =>
        (go.hideFlags & (HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy)) != 0 || go.GetComponent<TowerPlaceIds>();

    /// <summary>What a scene's objects are, for one piece of work (built fresh each time: objects come and go).</summary>
    private sealed class Ctx
    {
        public Scene Scene;
        public TowerPlaceIds Ids;
        public readonly Dictionary<string, int> ById = new Dictionary<string, int>();
        public readonly Dictionary<GameObject, int> ByObject = new Dictionary<GameObject, int>();
        public readonly Dictionary<string, TowerPlaceIds.Inner> Inners = new Dictionary<string, TowerPlaceIds.Inner>();
        public readonly Dictionary<Transform, string> Segs = new Dictionary<Transform, string>();
        public readonly Dictionary<GameObject, Dictionary<string, GameObject>> InnerMaps = new Dictionary<GameObject, Dictionary<string, GameObject>>();

        public Ctx(Scene scene, TowerPlaceIds ids)
        {
            Scene = scene;
            Ids = ids;
            Reindex();
        }

        public void Reindex()
        {
            ById.Clear(); ByObject.Clear(); Inners.Clear();
            if (!Ids) return;
            for (var i = 0; i < Ids.Entries.Count; i++)
            {
                var e = Ids.Entries[i];
                if (!string.IsNullOrEmpty(e.Id)) ById[e.Id] = i;
                if (e.Object) ByObject[e.Object] = i;
            }
            foreach (var n in Ids.Inners) if (!string.IsNullOrEmpty(n.Id)) Inners[n.Id] = n;
        }

        public string Seg(Transform t)
        {
            if (Segs.TryGetValue(t, out var s)) return s;
            var n = 0;
            if (t.parent)
            {
                var p = t.parent;
                var idx = t.GetSiblingIndex();
                for (var i = 0; i < idx; i++) if (p.GetChild(i).name == t.name) n++;
            }
            else if (t.gameObject.scene.IsValid())
            {
                foreach (var r in t.gameObject.scene.GetRootGameObjects())
                {
                    if (r.transform == t) break;
                    if (r.name == t.name) n++;
                }
            }
            s = n == 0 ? Clean(t.name) : Clean(t.name) + "#" + n;
            Segs[t] = s;
            return s;
        }

        // the path of a prefab asset's object under the asset's root
        public string AssetPath(Transform t, Transform root)
        {
            var parts = new List<string>();
            while (t && t != root)
            {
                parts.Add(Seg(t));
                t = t.parent;
            }
            if (t != root) return null;
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>The id of an object as it is now: its entry's, or (a part of a prefab copy) its copy's plus its place in the prefab.</summary>
        public string IdOf(Transform t)
        {
            if (!t) return "";
            if (ByObject.TryGetValue(t.gameObject, out var i)) return Ids.Entries[i].Id;
            if (!IsInner(t.gameObject)) return null;
            var anchor = t.parent;
            while (anchor && IsInner(anchor.gameObject)) anchor = anchor.parent;
            if (!anchor || !ByObject.TryGetValue(anchor.gameObject, out var a)) return null;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            var root = PrefabUtility.GetCorrespondingObjectFromSource(anchor.gameObject);
            if (!src || !root) return null;
            var rel = AssetPath(src.transform, root.transform);
            return rel == null ? null : Ids.Entries[a].Id + ">" + rel;
        }

        public GameObject Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (ById.TryGetValue(id, out var i)) return Ids.Entries[i].Object;
            var cut = id.IndexOf('>');
            if (cut < 0) return null;
            var anchor = Resolve(id.Substring(0, cut));
            if (!anchor) return null;
            if (!InnerMaps.TryGetValue(anchor, out var map))
            {
                map = new Dictionary<string, GameObject>();
                var root = PrefabUtility.GetCorrespondingObjectFromSource(anchor);
                if (root)
                {
                    foreach (var t in anchor.GetComponentsInChildren<Transform>(true))
                    {
                        if (t == anchor.transform || !IsInner(t.gameObject)) continue;
                        var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                        var rel = src ? AssetPath(src.transform, root.transform) : null;
                        if (rel != null && !map.ContainsKey(rel)) map[rel] = t.gameObject;
                    }
                }
                InnerMaps[anchor] = map;
            }
            return map.TryGetValue(id.Substring(cut + 1), out var go) ? go : null;
        }

        public string UniqueId(string id)
        {
            if (!ById.ContainsKey(id)) return id;
            for (var n = 2; ; n++) if (!ById.ContainsKey(id + "~" + n)) return id + "~" + n;
        }

        public int AddEntry(TowerPlaceIds.Entry e)
        {
            Ids.Entries.Add(e);
            var i = Ids.Entries.Count - 1;
            ById[e.Id] = i;
            if (e.Object) ByObject[e.Object] = i;
            return i;
        }
    }

    private static TowerPlaceIds.Entry Baseline(GameObject go, string id, string parentId)
    {
        var t = go.transform;
        var source = "";
        if (PrefabUtility.IsOutermostPrefabInstanceRoot(go) || (PrefabUtility.IsPartOfPrefabInstance(go) && PrefabUtility.IsAddedGameObjectOverride(go) && PrefabUtility.GetNearestPrefabInstanceRoot(go) == go))
        {
            var asset = PrefabUtility.GetCorrespondingObjectFromSource(go);
            var guid = asset ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)) : "";
            if (!string.IsNullOrEmpty(guid)) source = "prefab:" + guid;
        }
        return new TowerPlaceIds.Entry
        {
            Object = go, Id = id, ParentId = parentId ?? "", Source = source, Name = go.name,
            Position = t.localPosition, Rotation = t.localRotation, Scale = t.localScale, Active = go.activeSelf,
        };
    }

    private static bool Same(Vector3 a, Vector3 b, float tolerance = 0.001f) => (a - b).sqrMagnitude <= tolerance * tolerance;
    private static bool Same(Quaternion a, Quaternion b) => Quaternion.Angle(a, b) < 0.05f;

    /// <summary>
    ///     Gives every object of the scene that has no id yet one: its place in the hierarchy (names, "#n" for the n-th
    ///     of a name under one parent), with how it stands now as its baseline. Objects that have an id keep it and their
    ///     baseline; one made again where an object of the same id was (a rerun of tools/tower/tower-rooms-route.cs)
    ///     takes that id. Objects added by hand (registered when the scene was saved) are left as they are, and so is
    ///     what is inside them. Returns how many got an id.
    /// </summary>
    public static int Stamp(Scene scene, string by)
    {
        var ids = Registry(scene, true);
        var ctx = new Ctx(scene, ids);
        var stamped = 0;
        void Walk(Transform t, string parentPath, string parentId, bool insideAdded)
        {
            var go = t.gameObject;
            if (Skip(go)) return;
            var path = parentPath == null ? ctx.Seg(t) : parentPath + "/" + ctx.Seg(t);
            string myId;
            if (IsInner(go))
            {
                myId = insideAdded ? null : ctx.IdOf(t);
                if (myId != null && !ctx.Inners.ContainsKey(myId))
                {
                    // the builder left it different from the prefab: that is its baseline
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (src && (!Same(t.localPosition, src.transform.localPosition) || !Same(t.localRotation, src.transform.localRotation) || !Same(t.localScale, src.transform.localScale) || go.activeSelf != src.activeSelf))
                    {
                        var inner = new TowerPlaceIds.Inner { Id = myId, Position = t.localPosition, Rotation = t.localRotation, Scale = t.localScale, Active = go.activeSelf };
                        ids.Inners.Add(inner);
                        ctx.Inners[myId] = inner;
                    }
                }
            }
            else if (ctx.ByObject.TryGetValue(go, out var known))
            {
                var e = ids.Entries[known];
                myId = e.Id;
                if (e.Added && !string.IsNullOrEmpty(e.Source)) insideAdded = true;
            }
            else if (insideAdded) myId = null;
            else
            {
                var entry = Baseline(go, path, parentId);
                if (ctx.ById.TryGetValue(path, out var old) && !ids.Entries[old].Object && !ids.Entries[old].Added)
                {
                    ids.Entries[old] = entry;
                    ctx.ByObject[go] = old;
                }
                else
                {
                    entry.Id = ctx.UniqueId(path);
                    ctx.AddEntry(entry);
                }
                myId = entry.Id;
                stamped++;
            }
            if (Rigged(go)) return;
            foreach (Transform c in t) Walk(c, path, myId ?? parentId, insideAdded);
        }
        foreach (var root in scene.GetRootGameObjects()) Walk(root.transform, null, "", false);
        if (stamped > 0 || string.IsNullOrEmpty(ids.Stamped))
            ids.Stamped = DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) + " by " + by + " (" + ids.Entries.Count + " objects, " + ids.Inners.Count + " prefab parts built differently from their prefab)";
        EditorUtility.SetDirty(ids);
        EditorSceneManager.MarkSceneDirty(scene);
        return stamped;
    }

    // ---- what differs ---------------------------------------------------------------------------------------------

    private static float[] Arr(Vector3 v) => new[] { v.x, v.y, v.z };

    private static Edit TransformEdit(Transform t, string id, Vector3 built)
    {
        return new Edit
        {
            id = id, name = t.name,
            position = Arr(t.localPosition), rotation = Arr(t.localEulerAngles), scale = Arr(t.localScale),
            built = Arr(built),
        };
    }

    /// <summary>
    ///     The scene's hand edits now. With <paramref name="register"/>, objects added by hand get their id in the
    ///     registry (when the scene is saved), so the next save knows them; without it they are listed with a
    ///     provisional id.
    /// </summary>
    public static EditFile Compute(Scene scene, bool register)
    {
        var file = new EditFile { scene = scene.name };
        var ids = Registry(scene, false);
        if (!ids) return file;
        var ctx = new Ctx(scene, ids);
        var takenNow = new HashSet<string>();

        void Walk(Transform t, string parentPath, bool insideAdded)
        {
            var go = t.gameObject;
            if (Skip(go)) return;
            var path = parentPath == null ? ctx.Seg(t) : parentPath + "/" + ctx.Seg(t);
            if (IsInner(go))
            {
                var id = insideAdded ? null : ctx.IdOf(t);
                if (id != null)
                {
                    Vector3 p, s; Quaternion r; bool active;
                    if (ctx.Inners.TryGetValue(id, out var inner)) { p = inner.Position; r = inner.Rotation; s = inner.Scale; active = inner.Active; }
                    else
                    {
                        var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
                        p = src.transform.localPosition; r = src.transform.localRotation; s = src.transform.localScale; active = src.activeSelf;
                    }
                    var moved = !Same(t.localPosition, p) || !Same(t.localRotation, r) || !Same(t.localScale, s);
                    if (moved || go.activeSelf != active)
                    {
                        var e = moved ? TransformEdit(t, id, p) : new Edit { id = id, name = go.name };
                        if (go.activeSelf != active) e.active = go.activeSelf ? "on" : "off";
                        file.changed.Add(e);
                    }
                }
            }
            else if (ctx.ByObject.TryGetValue(go, out var i))
            {
                var e = ids.Entries[i];
                if (e.Added)
                {
                    var added = AddedEdit(ctx, t, e.Id, e.Source);
                    if (!string.IsNullOrEmpty(e.ParentId) && !ctx.Resolve(e.ParentId)) added.parent = e.ParentId;
                    file.added.Add(added);
                    if (!string.IsNullOrEmpty(e.Source)) insideAdded = true;
                }
                else
                {
                    var parentId = t.parent ? ctx.IdOf(t.parent) : "";
                    var reparented = parentId != null && parentId != (e.ParentId ?? "");
                    var moved = !Same(t.localPosition, e.Position) || !Same(t.localRotation, e.Rotation) || !Same(t.localScale, e.Scale);
                    var renamed = go.name != e.Name;
                    if (moved || reparented || renamed || go.activeSelf != e.Active)
                    {
                        var edit = moved || reparented ? TransformEdit(t, e.Id, e.Position) : new Edit { id = e.Id, name = e.Name };
                        edit.name = e.Name;
                        if (reparented) edit.parent = parentId;
                        if (renamed) edit.rename = go.name;
                        if (go.activeSelf != e.Active) edit.active = go.activeSelf ? "on" : "off";
                        file.changed.Add(edit);
                    }
                }
            }
            else if (!insideAdded)
            {
                // added by hand
                var source = AddedSource(ctx, go);
                var id = ctx.UniqueId("+" + path);
                while (takenNow.Contains(id)) id += "+";
                takenNow.Add(id);
                if (register)
                {
                    var entry = Baseline(go, id, "");
                    entry.Added = true;
                    entry.Source = source;
                    ctx.AddEntry(entry);
                }
                file.added.Add(AddedEdit(ctx, t, id, source));
                if (!string.IsNullOrEmpty(source)) insideAdded = true;
            }
            if (Rigged(go)) return;
            foreach (Transform c in t) Walk(c, path, insideAdded);
        }
        foreach (var root in scene.GetRootGameObjects()) Walk(root.transform, null, false);

        // deleted: the topmost only (deleting a parent takes its children)
        var gone = new HashSet<string>();
        foreach (var e in ids.Entries) if (!e.Object && !e.Added) gone.Add(e.Id);
        foreach (var e in ids.Entries)
        {
            if (e.Object || e.Added || gone.Contains(e.ParentId ?? "")) continue;
            file.deleted.Add(new Edit { id = e.Id, name = e.Name, built = Arr(e.Position) });
        }
        // parts deleted from prefab copies
        foreach (var e in ids.Entries)
        {
            if (!e.Object || !PrefabUtility.IsOutermostPrefabInstanceRoot(e.Object)) continue;
            List<RemovedGameObject> removed;
            try { removed = PrefabUtility.GetRemovedGameObjects(e.Object); }
            catch { continue; }
            var root = PrefabUtility.GetCorrespondingObjectFromSource(e.Object);
            foreach (var r in removed)
            {
                if (!r.assetGameObject || !root) continue;
                var rel = ctx.AssetPath(r.assetGameObject.transform, root.transform);
                if (rel != null) file.deleted.Add(new Edit { id = e.Id + ">" + rel, name = r.assetGameObject.name, built = Arr(r.assetGameObject.transform.localPosition) });
            }
        }
        if (register)
        {
            // an object added by hand and deleted again is no edit at all
            var before = ids.Entries.Count;
            ids.Entries.RemoveAll(e => e.Added && !e.Object);
            if (ids.Entries.Count != before) EditorUtility.SetDirty(ids);
        }
        return file;
    }

    private static string AddedSource(Ctx ctx, GameObject go)
    {
        if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
        {
            var asset = PrefabUtility.GetCorrespondingObjectFromSource(go);
            var guid = asset ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)) : "";
            if (!string.IsNullOrEmpty(guid)) return "prefab:" + guid;
        }
        // a duplicate (Ctrl+D) of an object of the place: the same name without its " (1)" and the same mesh, the nearest
        var name = BaseName(go.name);
        var mesh = go.GetComponent<MeshFilter>() ? go.GetComponent<MeshFilter>().sharedMesh : null;
        string best = null;
        var bestD = float.MaxValue;
        foreach (var e in ctx.Ids.Entries)
        {
            if (!e.Object || e.Object == go || BaseName(e.Object.name) != name) continue;
            var otherMesh = e.Object.GetComponent<MeshFilter>() ? e.Object.GetComponent<MeshFilter>().sharedMesh : null;
            if (otherMesh != mesh) continue;
            if (mesh == null && e.Object.transform.childCount != go.transform.childCount) continue;
            var d = (e.Object.transform.position - go.transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = e.Id; }
        }
        return best != null ? "copy:" + best : "";
    }

    private static Edit AddedEdit(Ctx ctx, Transform t, string id, string source)
    {
        var e = TransformEdit(t, id, Vector3.zero);
        e.built = new float[0];
        e.parent = t.parent ? ctx.IdOf(t.parent) ?? "" : "";
        e.active = t.gameObject.activeSelf ? "on" : "off";
        if (source.StartsWith("prefab:"))
        {
            e.guid = source.Substring(7);
            e.prefab = AssetDatabase.GUIDToAssetPath(e.guid);
        }
        else if (source.StartsWith("copy:")) e.copyOf = source.Substring(5);
        else e.note = "made from scratch: comes back as an empty object with this name and place (its components are not kept)";
        return e;
    }

    /// <summary>
    ///     Writes the scene's change list (when it has edits or had a file): registers what was added by hand, lists what
    ///     differs from what was built, and keeps the edits of the old file that point at nothing any more under
    ///     "unapplied". Called as a tower scene saves.
    /// </summary>
    public static EditFile Write(Scene scene, string sceneName)
    {
        var ids = Registry(scene, false);
        if (!ids) return null;
        var file = Compute(scene, true);
        var old = Load(sceneName);
        var ctx = new Ctx(scene, ids);
        bool Known(string id) => ctx.ById.ContainsKey(id) || (id.Contains(">") && ctx.Resolve(id));
        foreach (var e in old.changed) if (!Known(e.id)) { e.what = "changed"; file.unapplied.Add(e); }
        foreach (var e in old.deleted) if (!Known(e.id) && !file.deleted.Any(d => d.id == e.id)) { e.what = "deleted"; file.unapplied.Add(e); }
        foreach (var e in old.unapplied) if (!Known(e.id) && !file.unapplied.Any(u => u.id == e.id && u.what == e.what)) file.unapplied.Add(e);
        Save(sceneName, file);
        return file;
    }

    // ---- putting them back ----------------------------------------------------------------------------------------

    public sealed class ApplyResult
    {
        public int Applied;
        public int Unapplied;
        public readonly StringBuilder Log = new StringBuilder();
    }

    private static Vector3 V(float[] a, Vector3 fallback) => TowerPlaceJson.ToVec(a, fallback);

    /// <summary>
    ///     Puts the change list back on the scene: added objects first (made again, or found by their id), then the
    ///     changed ones, then the deleted ones. An edit whose object cannot be found, or whose object is no longer the one
    ///     it was made on (another name, or built somewhere else while another object of that name was built where it
    ///     was), is left out and kept under "unapplied". Running it twice changes nothing more.
    /// </summary>
    public static ApplyResult Reapply(Scene scene, string sceneName)
    {
        var result = new ApplyResult();
        var file = Load(sceneName);
        if (file.changed.Count + file.deleted.Count + file.added.Count + file.unapplied.Count == 0) return result;
        var ids = Registry(scene, true);
        if (ids.Entries.Count == 0) Stamp(scene, "Reapply");
        var ctx = new Ctx(scene, ids);
        var place = scene.GetRootGameObjects().Select(g => g.GetComponent<TowerPlace>()).FirstOrDefault(p => p);
        var unapplied = new List<Edit>();
        void Miss(Edit e, string what, string why)
        {
            e.what = what;
            e.note = why;
            unapplied.Add(e);
            result.Unapplied++;
            result.Log.AppendLine("  not put back: " + what + " " + e.id + " (" + why + ")");
        }

        // the object an edit was made on: by its id, checked against its name and where it was built
        GameObject Find(Edit e)
        {
            var go = ctx.Resolve(e.id);
            if (!ctx.ById.TryGetValue(e.id, out var i)) return go;
            var entry = ids.Entries[i];
            var built = V(e.built, entry.Position);
            var sameName = string.IsNullOrEmpty(e.name) || entry.Name == e.name;
            if (sameName && Same(entry.Position, built, 0.25f)) return go;
            // the builder changed: another object of that name built where this one was?
            for (var k = 0; k < ids.Entries.Count; k++)
            {
                var other = ids.Entries[k];
                if (other.Added || !other.Object || other.Name != e.name || other.ParentId != entry.ParentId) continue;
                if (Same(other.Position, built, 0.05f)) return other.Object;
            }
            if (!sameName) return null;
            result.Log.AppendLine("  note: " + e.id + " was built " + (entry.Position - built).magnitude.ToString("0.0") + " m from where it was when edited");
            return go;
        }

        // added
        foreach (var a in file.added)
        {
            var parent = string.IsNullOrEmpty(a.parent) ? null : ctx.Resolve(a.parent);
            if (!string.IsNullOrEmpty(a.parent) && !parent) result.Log.AppendLine("  note: the parent of added " + a.id + " is gone; it goes under the place's root");
            var parentT = parent ? parent.transform : place ? place.transform : null;
            var go = ctx.Resolve(a.id);
            var source = "";
            if (!go)
            {
                if (!string.IsNullOrEmpty(a.guid) || !string.IsNullOrEmpty(a.prefab))
                {
                    var path = !string.IsNullOrEmpty(a.guid) ? AssetDatabase.GUIDToAssetPath(a.guid) : a.prefab;
                    if (string.IsNullOrEmpty(path)) path = a.prefab;
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!asset) { Miss(a, "added", "its prefab is missing: " + a.prefab); continue; }
                    go = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                    source = "prefab:" + AssetDatabase.AssetPathToGUID(path);
                }
                else if (!string.IsNullOrEmpty(a.copyOf))
                {
                    var original = ctx.Resolve(a.copyOf);
                    if (!original) { Miss(a, "added", "what it was a copy of is no longer built: " + a.copyOf); continue; }
                    go = Object.Instantiate(original, parentT);
                    source = "copy:" + a.copyOf;
                }
                else
                {
                    go = new GameObject();
                    SceneManager.MoveGameObjectToScene(go, scene);
                }
                var entry = Baseline(go, a.id, "");
                entry.Added = true;
                entry.Source = source;
                ctx.AddEntry(entry);
            }
            go.transform.SetParent(parentT, false);
            // a parent that is not built yet (a gate, before tools/tower/tower-rooms-route.cs runs) is remembered, so the
            // next save keeps it and the next reapply puts the object under it
            if (ctx.ById.TryGetValue(a.id, out var ai))
            {
                var ae = ids.Entries[ai];
                ae.ParentId = !string.IsNullOrEmpty(a.parent) && !parent ? a.parent : "";
                ids.Entries[ai] = ae;
            }
            if (!string.IsNullOrEmpty(a.name)) go.name = a.name;
            go.transform.localPosition = V(a.position, go.transform.localPosition);
            go.transform.localEulerAngles = V(a.rotation, go.transform.localEulerAngles);
            go.transform.localScale = V(a.scale, go.transform.localScale);
            if (a.active == "on" || a.active == "off") go.SetActive(a.active == "on");
            result.Applied++;
        }

        // changed
        foreach (var c in file.changed.Concat(file.unapplied.Where(u => u.what == "changed")))
        {
            var go = Find(c);
            if (!go) { Miss(c, "changed", ctx.ById.ContainsKey(c.id) || ctx.Resolve(c.id) ? "another object has that id now" : "no object has that id"); continue; }
            var t = go.transform;
            if (!string.IsNullOrEmpty(c.parent))
            {
                var p = ctx.Resolve(c.parent);
                if (p) t.SetParent(p.transform, false);
                else result.Log.AppendLine("  note: the new parent of " + c.id + " is gone; it stays where it was built");
            }
            if (!string.IsNullOrEmpty(c.rename)) go.name = c.rename;
            if (c.HasTransform)
            {
                t.localPosition = V(c.position, t.localPosition);
                t.localEulerAngles = V(c.rotation, t.localEulerAngles);
                t.localScale = V(c.scale, t.localScale);
            }
            if (c.active == "on" || c.active == "off") go.SetActive(c.active == "on");
            result.Applied++;
        }

        // deleted
        foreach (var d in file.deleted.Concat(file.unapplied.Where(u => u.what == "deleted")))
        {
            if (ctx.ById.TryGetValue(d.id, out var i) && !ids.Entries[i].Object) { result.Applied++; continue; }
            var go = Find(d);
            if (!go) { Miss(d, "deleted", "no object has that id"); continue; }
            if (IsInner(go))
            {
                try { Object.DestroyImmediate(go); }
                catch (Exception) { go.SetActive(false); result.Log.AppendLine("  note: " + d.id + " is part of a prefab copy and could not be deleted; it is hidden instead"); }
            }
            else Object.DestroyImmediate(go);
            result.Applied++;
        }

        foreach (var u in file.unapplied.Where(u => u.what == "added")) Miss(u, "added", u.note);
        if (result.Applied > 0 || result.Unapplied > 0)
        {
            EditorUtility.SetDirty(ids);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        return result;
    }

    /// <summary>
    ///     For the builders (tools/tower/*-build.cs, tower-rooms-route.cs), just before they save: every object gets its
    ///     fixed id, then the owner's hand edits go back on, and when anything was put back the walking ground is baked
    ///     again. <paramref name="scenePath"/> names the scene (a new one has no path yet).
    /// </summary>
    public static void AfterBuild(Scene scene, string scenePath, StringBuilder log)
    {
        var name = Path.GetFileNameWithoutExtension(scenePath);
        try
        {
            var stamped = Stamp(scene, name + "'s builder");
            log?.AppendLine("ids: " + stamped + " new, " + Registry(scene, false).Entries.Count + " in all");
            var result = Reapply(scene, name);
            if (result.Applied + result.Unapplied > 0)
            {
                log?.AppendLine("hand edits (" + FileFor(name) + "): " + result.Applied + " put back, " + result.Unapplied + " not");
                log?.Append(result.Log);
                if (result.Applied > 0) TowerPlaceNav.Rebake(scene, log);
            }
        }
        catch (Exception e) { log?.AppendLine("ERROR putting the hand edits back: " + e); }
    }

    /// <summary>The object an id names in the open scene: an entry's, a part of a prefab copy, or (a listed id of something added since the last save) by its place.</summary>
    public static GameObject FindObject(Scene scene, string id)
    {
        var ids = Registry(scene, false);
        if (!ids || string.IsNullOrEmpty(id)) return null;
        var ctx = new Ctx(scene, ids);
        var go = ctx.Resolve(id);
        if (go || !id.StartsWith("+")) return go;
        var path = id.Substring(1).TrimEnd('+');
        var tilde = path.LastIndexOf('~');
        if (tilde > 0 && int.TryParse(path.Substring(tilde + 1), out _)) path = path.Substring(0, tilde);
        GameObject found = null;
        void Walk(Transform t, string parentPath)
        {
            if (found) return;
            var p = parentPath == null ? ctx.Seg(t) : parentPath + "/" + ctx.Seg(t);
            if (p == path) { found = t.gameObject; return; }
            if (!path.StartsWith(p + "/")) return;
            foreach (Transform c in t) Walk(c, p);
        }
        foreach (var root in scene.GetRootGameObjects()) Walk(root.transform, null);
        return found;
    }

    // ---- one edit undone ------------------------------------------------------------------------------------------

    /// <summary>Undoes one edit in the open scene (the change list follows when the scene is saved). False, with why, when it cannot.</summary>
    public static bool Revert(Scene scene, Edit edit, string kind, out string message)
    {
        message = null;
        var ids = Registry(scene, false);
        if (!ids) { message = "The place has no ids."; return false; }
        var ctx = new Ctx(scene, ids);
        Undo.RecordObject(ids, "Revert tower place edit");
        switch (kind)
        {
            case "added":
            {
                var go = ctx.Resolve(edit.id);
                if (go)
                {
                    // what the builder made and was put under it goes back to its own parent first
                    foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    {
                        if (t == go.transform || !ctx.ByObject.TryGetValue(t.gameObject, out var k) || ids.Entries[k].Added) continue;
                        var back = ctx.Resolve(ids.Entries[k].ParentId);
                        Undo.SetTransformParent(t, back ? back.transform : null, "Revert tower place edit");
                        t.localPosition = ids.Entries[k].Position; t.localRotation = ids.Entries[k].Rotation; t.localScale = ids.Entries[k].Scale;
                    }
                    Undo.DestroyObjectImmediate(go);
                }
                ids.Entries.RemoveAll(e => e.Id == edit.id);
                message = "Removed " + edit.name + ".";
                break;
            }
            case "changed":
            {
                var go = ctx.Resolve(edit.id);
                if (!go) { message = "It is not in the scene."; return false; }
                var t = go.transform;
                if (ctx.ById.TryGetValue(edit.id, out var i))
                {
                    var e = ids.Entries[i];
                    var parent = string.IsNullOrEmpty(e.ParentId) ? null : ctx.Resolve(e.ParentId);
                    Undo.SetTransformParent(t, parent ? parent.transform : null, "Revert tower place edit");
                    if (!parent) SceneManager.MoveGameObjectToScene(go, scene);
                    Undo.RecordObject(t, "Revert tower place edit");
                    Undo.RecordObject(go, "Revert tower place edit");
                    t.localPosition = e.Position; t.localRotation = e.Rotation; t.localScale = e.Scale;
                    go.name = e.Name;
                    go.SetActive(e.Active);
                }
                else if (ctx.Inners.TryGetValue(edit.id, out var inner))
                {
                    Undo.RecordObject(t, "Revert tower place edit");
                    Undo.RecordObject(go, "Revert tower place edit");
                    t.localPosition = inner.Position; t.localRotation = inner.Rotation; t.localScale = inner.Scale;
                    go.SetActive(inner.Active);
                }
                else
                {
                    PrefabUtility.RevertObjectOverride(t, InteractionMode.UserAction);
                    PrefabUtility.RevertObjectOverride(go, InteractionMode.UserAction);
                }
                message = "Put " + go.name + " back as it was built.";
                break;
            }
            case "deleted":
                if (!RestoreDeleted(scene, ctx, edit, out message)) return false;
                break;
            case "unapplied":
            {
                var file = Load(scene.name);
                file.unapplied.RemoveAll(u => u.id == edit.id && u.what == edit.what);
                Save(scene.name, file);
                message = "Forgot that edit.";
                return true;
            }
        }
        EditorUtility.SetDirty(ids);
        EditorSceneManager.MarkSceneDirty(scene);
        return true;
    }

    // a deleted object comes back from the scene as it is in git (HEAD), or as a new copy of its prefab
    private static bool RestoreDeleted(Scene scene, Ctx ctx, Edit edit, out string message)
    {
        message = null;
        var cut = edit.id.IndexOf('>');
        if (cut >= 0)
        {
            // a part of a prefab copy: the copy's removal is reverted
            var anchor = ctx.Resolve(edit.id.Substring(0, cut));
            if (!anchor) { message = "Its prefab copy is not in the scene."; return false; }
            var root = PrefabUtility.GetCorrespondingObjectFromSource(anchor);
            foreach (var r in PrefabUtility.GetRemovedGameObjects(anchor))
            {
                if (r.assetGameObject && root && ctx.AssetPath(r.assetGameObject.transform, root.transform) == edit.id.Substring(cut + 1))
                {
                    r.Revert(InteractionMode.UserAction);
                    message = "Brought " + edit.name + " back.";
                    return true;
                }
            }
            message = "Unity does not list it as removed from its prefab copy.";
            return false;
        }
        if (!ctx.ById.TryGetValue(edit.id, out var i)) { message = "No object has that id."; return false; }
        var entry = ctx.Ids.Entries[i];
        var parent = string.IsNullOrEmpty(entry.ParentId) ? null : ctx.Resolve(entry.ParentId);
        GameObject made = null;
        var from = "";

        // from git: the scene as committed, opened beside this one for a moment
        var temp = "Assets/Temp/TowerPlaces/" + scene.name + "-HEAD.unity";
        try
        {
            if (ExtractFromGit(scene.path, temp))
            {
                AssetDatabase.ImportAsset(temp, ImportAssetOptions.ForceSynchronousImport);
                var other = EditorSceneManager.OpenScene(temp, OpenSceneMode.Additive);
                try
                {
                    var otherIds = Registry(other, false);
                    var original = otherIds ? otherIds.Entries.FirstOrDefault(e => e.Id == edit.id).Object : null;
                    if (original)
                    {
                        if (PrefabUtility.IsOutermostPrefabInstanceRoot(original))
                        {
                            // a prefab copy stays one, with the changes the builder made to it
                            made = (GameObject)PrefabUtility.InstantiatePrefab(PrefabUtility.GetCorrespondingObjectFromSource(original), scene);
                            var mods = PrefabUtility.GetPropertyModifications(original);
                            if (mods != null) PrefabUtility.SetPropertyModifications(made, mods);
                        }
                        else made = Object.Instantiate(original);
                        SceneManager.MoveGameObjectToScene(made, scene);
                        from = "git";
                    }
                }
                finally { EditorSceneManager.CloseScene(other, true); }
            }
        }
        catch (Exception e) { Debug.LogWarning("Tower Places: could not bring it back from git: " + e.Message); }
        finally
        {
            AssetDatabase.DeleteAsset(temp);
            SceneManager.SetActiveScene(scene);
        }
        if (!made && !string.IsNullOrEmpty(entry.Source) && entry.Source.StartsWith("prefab:"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(entry.Source.Substring(7)));
            if (asset)
            {
                made = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                from = "its prefab (changes the builder made to that copy, other than where it stands, are not back)";
            }
        }
        if (!made)
        {
            message = "It cannot be brought back here (it is not in the scene in git and is not a prefab copy). A rebuild of the place brings it back.";
            return false;
        }
        Undo.RegisterCreatedObjectUndo(made, "Bring back deleted object");
        made.transform.SetParent(parent ? parent.transform : null, false);
        made.transform.localPosition = entry.Position;
        made.transform.localRotation = entry.Rotation;
        made.transform.localScale = entry.Scale;
        made.name = entry.Name;
        made.SetActive(entry.Active);
        entry.Object = made;
        ctx.Ids.Entries[i] = entry;
        message = "Brought " + entry.Name + " back from " + from + ".";
        return true;
    }

    private static bool ExtractFromGit(string scenePath, string target)
    {
        var root = Path.GetFullPath(".");
        string Run(string args, string input = null, string toFile = null)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = input != null,
            };
            using (var p = Process.Start(psi))
            {
                if (input != null) { p.StandardInput.Write(input); p.StandardInput.Close(); }
                if (toFile != null)
                {
                    using (var f = File.Create(toFile)) p.StandardOutput.BaseStream.CopyTo(f);
                    p.WaitForExit(60000);
                    return p.ExitCode == 0 ? "" : null;
                }
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(60000);
                return p.ExitCode == 0 ? output : null;
            }
        }
        var pointer = Run("cat-file -p HEAD:\"" + scenePath + "\"");
        if (pointer == null) return false;
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        if (pointer.StartsWith("version https://git-lfs"))
            return Run("lfs smudge", pointer, target) != null && new FileInfo(target).Length > 1000;
        File.WriteAllText(target, pointer);
        return true;
    }
}

/// <summary>Writes a tower scene's change list whenever it is saved, from the Tower Places window or with Ctrl+S.</summary>
[InitializeOnLoad]
internal static class TowerPlaceEditsOnSave
{
    static TowerPlaceEditsOnSave()
    {
        EditorSceneManager.sceneSaving += (scene, path) =>
        {
            if (!path.Replace('\\', '/').StartsWith(TowerPlaceCatalog.SceneFolder + "/") || path.Contains("/Temp/")) return;
            try { TowerPlaceEdits.Write(scene, Path.GetFileNameWithoutExtension(path)); }
            catch (Exception e) { Debug.LogError("Tower Places: could not write the change list of " + path + ": " + e); }
        };
    }
}
