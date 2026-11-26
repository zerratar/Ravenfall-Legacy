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
/// </summary>
public static class TowerPlaceNav
{
    public static bool Rebake(Scene scene, StringBuilder log)
    {
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
}
