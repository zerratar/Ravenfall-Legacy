using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Roads laid into a scene by hand or by a build script, drawn the same way as the village roads the streamer draws
///     (<see cref="RoadMesh"/> and the road shader's materials in Resources/Roads): the tower's floors and towns use
///     them for their paths. The meshes are made when the scene wakes up and are never saved with it; the points are.
///     Points are in this object's space, on the ground plane; the road follows whatever ground is under it.
/// </summary>
[ExecuteAlways]
public sealed class SceneRoads : MonoBehaviour
{
    [Serializable]
    public sealed class Road
    {
        /// <summary>The road type, as the village's: 0 dirt, 1 gravel, 2 cobbles, 3 paving, 4 mossy flagstones.</summary>
        public int Type;
        public float Width = 2.5f;
        public List<Vector2> Points = new List<Vector2>();
        /// <summary>The width along the road, one per point (or empty for 1 all along).</summary>
        public List<float> Widths = new List<float>();
        public float StartRound = 1f;
        public float EndRound = 1f;
    }

    public List<Road> Roads = new List<Road>();

    /// <summary>The layers the ground is looked for on (terrain and walkable meshes).</summary>
    public LayerMask Ground = ~0;

    private readonly List<GameObject> made = new List<GameObject>();
    private static readonly Dictionary<int, Material[]> materials = new Dictionary<int, Material[]>();

    private void OnEnable() => Rebuild();

    private void OnDisable() => Clear();

    /// <summary>Makes every road's mesh again (after the points changed).</summary>
    public void Rebuild()
    {
        Clear();
        for (var i = 0; i < Roads.Count; ++i)
        {
            var road = Roads[i];
            if (road == null || road.Points == null || road.Points.Count < 2) continue;
            var go = new GameObject("Road " + i) { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
            go.transform.SetParent(transform, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mesh = new Mesh { name = "Scene Road", hideFlags = HideFlags.DontSave };
            var samples = RoadMesh.Sample(road.Points);
            var widths = road.Widths != null && road.Widths.Count == road.Points.Count ? RoadMesh.SampleWidths(road.Points, road.Widths) : null;
            // the better road a little higher, so where two meet the better one lies on top
            RoadMesh.Build(mesh, samples, road.Width, widths, GroundHeight, RoadMesh.Lift + Mathf.Clamp(road.Type, 0, 5) * 0.012f, road.StartRound, road.EndRound);
            filter.sharedMesh = mesh;
            renderer.sharedMaterials = MaterialsFor(road.Type);
            made.Add(go);
        }
    }

    private void Clear()
    {
        foreach (var go in made)
        {
            if (!go) continue;
            var filter = go.GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh) DestroyNow(filter.sharedMesh);
            DestroyNow(go);
        }
        made.Clear();
    }

    private static void DestroyNow(UnityEngine.Object o)
    {
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }

    /// <summary>The ground's height under a point of this object's space, in this object's space.</summary>
    private float GroundHeight(Vector3 local)
    {
        var world = transform.TransformPoint(new Vector3(local.x, 0f, local.z));
        var y = world.y;
        var found = false;
        foreach (var terrain in Terrain.activeTerrains)
        {
            if (!terrain || terrain.gameObject.scene != gameObject.scene) continue;
            var p = terrain.transform.position;
            var size = terrain.terrainData.size;
            if (world.x < p.x || world.z < p.z || world.x > p.x + size.x || world.z > p.z + size.z) continue;
            y = terrain.SampleHeight(world) + p.y;
            found = true;
            break;
        }
        if (!found && Physics.Raycast(world + Vector3.up * 50f, Vector3.down, out var hit, 200f, Ground, QueryTriggerInteraction.Ignore)) y = hit.point.y;
        return transform.InverseTransformPoint(new Vector3(world.x, y, world.z)).y;
    }

    /// <summary>
    ///     The road type's materials as the village draws them: Resources/Roads/Road_&lt;type&gt;, and a copy that draws
    ///     only the middle a queue later (so where two roads meet, each middle lies over the other's fringe).
    /// </summary>
    public static Material[] MaterialsFor(int type)
    {
        if (materials.TryGetValue(type, out var existing) && existing[0]) return existing;
        var asset = UnityEngine.Resources.Load<Material>("Roads/Road_" + type) ?? UnityEngine.Resources.Load<Material>("Roads/Road_0");
        if (!asset) return new Material[0];
        Material[] set;
        if (asset.HasProperty("_Core"))
        {
            var core = new Material(asset) { name = asset.name + " Middle", hideFlags = HideFlags.DontSave };
            core.SetFloat("_Core", 1f);
            core.renderQueue = asset.renderQueue + 1;
            set = new[] { asset, core };
        }
        else set = new[] { asset };
        materials[type] = set;
        return set;
    }
}
