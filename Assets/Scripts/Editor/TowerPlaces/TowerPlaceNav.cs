using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
///     Rebakes a tower scene's walking ground (its NavMeshSurface, as the builders bake it: tools/tower/*-build.cs) into
///     the same NavMesh.asset, so the scene keeps pointing at it and nothing but the asset changes. The gates of a
///     floor of rooms (tools/tower/tower-rooms-route.cs) are left out of the bake as they were when the builder baked:
///     their wall pieces and seals carve the navmesh at run time instead. The asset is in a gitignored folder: commit it
///     with git add -f.
///     Objects added by hand block walking: before a bake each one with a mesh and no collider gets a box collider round
///     its mesh (<see cref="FitColliders"/>), as the builders give their own wall pieces one. The navmesh is baked from
///     colliders, and the owner's walls in the crypt and the frost vault (raw pack prefabs, no collider) were walked
///     through (playtest 2026-11-26).
/// </summary>
public static class TowerPlaceNav
{
    public static bool Rebake(Scene scene, StringBuilder log)
    {
        FitColliders(scene, log);
        var surfaces = new List<NavMeshSurface>();
        foreach (var root in scene.GetRootGameObjects()) surfaces.AddRange(root.GetComponentsInChildren<NavMeshSurface>(true));
        if (surfaces.Count == 0)
        {
            log?.AppendLine("no NavMeshSurface in " + scene.name);
            return false;
        }
        // what the builders baked without: the gates added after them (their colliders and their obstacles' cut)
        var off = new List<Collider>();
        var obstacles = new List<NavMeshObstacle>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var gate in root.GetComponentsInChildren<TowerGate>(true))
            {
                foreach (var c in gate.GetComponentsInChildren<Collider>(true)) if (c.enabled) { c.enabled = false; off.Add(c); }
                foreach (var o in gate.GetComponentsInChildren<NavMeshObstacle>(true)) if (o.enabled) { o.enabled = false; obstacles.Add(o); }
            }
        var ok = true;
        try
        {
            foreach (var surface in surfaces)
            {
                var old = surface.navMeshData;
                var path = old ? AssetDatabase.GetAssetPath(old) : "";
                if (string.IsNullOrEmpty(path)) path = System.IO.Path.GetDirectoryName(scene.path).Replace('\\', '/') + "/" + scene.name + "/NavMesh.asset";
                surface.BuildNavMesh();
                var fresh = surface.navMeshData;
                if (!fresh) { log?.AppendLine("the bake gave nothing for " + surface.name); ok = false; continue; }
                var asset = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
                if (asset && asset != fresh)
                {
                    // into the asset the scene points at: same file, same id
                    EditorUtility.CopySerialized(fresh, asset);
                    EditorUtility.SetDirty(asset);
                    surface.RemoveData();
                    surface.navMeshData = asset;
                    surface.AddData();
                    Object.DestroyImmediate(fresh);
                }
                else if (!asset)
                {
                    AssetDatabase.CreateAsset(fresh, path);
                    surface.navMeshData = fresh;
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                log?.AppendLine("baked " + surface.name + " into " + path + ", bounds " + surface.navMeshData.sourceBounds.size.ToString("F0"));
            }
            AssetDatabase.SaveAssets();
        }
        finally
        {
            foreach (var c in off) if (c) c.enabled = true;
            foreach (var o in obstacles) if (o) o.enabled = true;
        }
        return ok;
    }

    /// <summary>
    ///     Every object added by hand (the change list's added ones, saved or not) that has a mesh and no collider gets
    ///     a BoxCollider round its meshes, on the added object itself. One under a gate also gets a carving
    ///     NavMeshObstacle of that box, as the route's own wall pieces beside a doorway have: the bake leaves everything
    ///     under a gate out, so a collider alone would not keep anyone off it (the owner's walls round the doorways). A
    ///     copy of a built piece keeps the copy's own collider. The change list does not keep components, so this runs
    ///     before every bake, also after a rebuild puts the edits back (<see cref="TowerPlaceEdits.AfterBuild"/>).
    ///     Returns how many objects it changed.
    /// </summary>
    public static int FitColliders(Scene scene, StringBuilder log)
    {
        var added = 0;
        var file = TowerPlaceEdits.Compute(scene, false);
        foreach (var a in file.added)
        {
            var go = TowerPlaceEdits.FindObject(scene, a.id);
            if (!go || go.GetComponent<TowerGate>()) continue;
            var underGate = go.GetComponentInParent<TowerGate>(true);
            var hasCollider = go.GetComponentsInChildren<Collider>(true).Length > 0;
            var hasObstacle = go.GetComponentsInChildren<NavMeshObstacle>(true).Length > 0;
            if (hasCollider && (!underGate || hasObstacle)) continue;
            var root = go.transform;
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var any = false;
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.sharedMesh || !filter.GetComponent<MeshRenderer>()) continue;
                var b = filter.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var local = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                    any = true;
                }
            }
            if (!any) continue;
            var center = (min + max) * 0.5f;
            var size = Vector3.Max(max - min, Vector3.one * 0.1f);
            if (!hasCollider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = center;
                box.size = size;
            }
            if (underGate && !hasObstacle)
            {
                var obstacle = go.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = center;
                obstacle.size = Vector3.Max(size, new Vector3(0.5f, 0.5f, 0.5f));
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            added++;
        }
        if (added > 0) log?.AppendLine("a box collider (and under a gate a carving obstacle) on " + added + " object(s) added by hand, so they block walking");
        return added;
    }
}
