using System;
using UnityEngine;

/// <summary>
/// The grid over the whole buildable area while build mode is on. Buildings snap to a 1 m grid,
/// but the only grid shown was a 13 m patch round the building being moved, which said little
/// about where building is possible. This lays the grid over every part of the village a
/// building may stand on: a mesh draped over the ground, a line every metre and a stronger one
/// every five, fading out towards the edge of the village and left out over water.
/// <para>
/// The edge of the area is drawn as a gold line on the ground. Past it, the ring the next town hall
/// tier opens carries on the grid, dimmer and cooler, with a fainter line at its edge, so it reads
/// as ground that is coming rather than ground that is open.
/// </para>
/// <para>
/// Only land joined to the plaza counts: the grid is flooded outward from the middle over dry
/// ground, so a circle that reaches across the water to the next island stops at the coast.
/// <see cref="IsOnVillageLand"/> answers the same for placing buildings.
/// </para>
/// </summary>
/// <remarks>
/// Built once and kept: the ground does not change, so entering build mode again only shows it.
/// A different village area (the town hall moved, which moves nothing here, or a new plot further
/// out) rebuilds it.
/// </remarks>
public class VillageGrid : MonoBehaviour
{
    // One vertex every this many metres. The lines come from the texture, so this only decides how
    // closely the mesh follows bumps in the ground.
    private const float Spacing = 1.5f;
    private const float Lift = 0.12f;
    private const float EdgeFade = 8f;
    private const int MajorEvery = 5;
    private const int PixelsPerCell = 48;

    private const float LineLift = 0.18f;
    private static readonly Color32 Open = new Color32(255, 255, 255, 255);
    private static readonly Color32 Locked = new Color32(185, 205, 255, 255);
    private static readonly Color EdgeColour = new Color(1f, 0.82f, 0.38f, 0.95f);
    private static readonly Color NextEdgeColour = new Color(0.62f, 0.72f, 0.9f, 0.55f);

    private MeshFilter filter;
    private MeshRenderer meshRenderer;
    private Material material;
    private Material lineMaterial;
    private Texture2D texture;
    private Vector3 builtCentre = new Vector3(float.NaN, 0f, 0f);
    private float builtRadius = float.NaN;
    private float builtNext = float.NaN;
    private readonly System.Collections.Generic.List<LineRenderer> lines = new System.Collections.Generic.List<LineRenderer>();

    // which grid points are land joined to the plaza, from the last build
    private bool[] reached;
    private int reachedCount;
    private Vector3 reachedOrigin;

    public static VillageGrid Create()
    {
        var go = new GameObject("Build Mode Village Grid");
        var grid = go.AddComponent<VillageGrid>();
        grid.filter = go.AddComponent<MeshFilter>();
        grid.meshRenderer = go.AddComponent<MeshRenderer>();
        grid.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        grid.meshRenderer.receiveShadows = false;
        go.SetActive(false);
        return grid;
    }

    /// <summary>
    /// Shows the grid over a circle of <paramref name="radius"/> round <paramref name="centre"/>,
    /// and dimmer out to <paramref name="next"/>, the next tier's area.
    /// <paramref name="groundHeight"/> gives the terrain under a point, ignoring buildings, so the
    /// grid lies on the ground rather than over floors; ground below <paramref name="waterLevel"/>
    /// is water and left out.
    /// </summary>
    public void Show(Vector3 centre, float radius, float next, float cellSize, Func<Vector3, float> groundHeight, float waterLevel)
    {
        next = Mathf.Max(next, radius);
        if (!Mathf.Approximately(radius, builtRadius) || !Mathf.Approximately(next, builtNext)
            || (Flat(centre) - Flat(builtCentre)).sqrMagnitude > 0.01f)
        {
            Build(centre, radius, next, cellSize, groundHeight, waterLevel);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Build(Vector3 centre, float radius, float next, float cellSize, Func<Vector3, float> groundHeight, float waterLevel)
    {
        builtCentre = centre;
        builtRadius = radius;
        builtNext = next;

        if (!material) material = MakeMaterial(cellSize);

        // the mesh reaches out to the next tier's edge; open ground inside the area, locked beyond
        var outer = next;
        var count = Mathf.CeilToInt(outer * 2f / Spacing) + 1;
        var origin = new Vector3(centre.x - outer, 0f, centre.z - outer);
        var vertices = new Vector3[count * count];
        var colors = new Color32[count * count];
        var uvs = new Vector2[count * count];
        var inside = new bool[count * count];

        for (var z = 0; z < count; ++z)
        {
            for (var x = 0; x < count; ++x)
            {
                var i = z * count + x;
                var flat = origin + new Vector3(x * Spacing, 0f, z * Spacing);
                var distance = (Flat(flat) - Flat(centre)).magnitude;
                var height = groundHeight != null ? groundHeight(flat) : PlacementUtility.FindGroundPoint(flat).y;
                vertices[i] = new Vector3(flat.x, height + Lift, flat.z);

                // texture coordinates in cells of the world, so the lines sit where the snapping does
                uvs[i] = new Vector2(flat.x / (cellSize * MajorEvery), flat.z / (cellSize * MajorEvery));

                // open up to the edge, then the next ring dimmer, fading out towards its far edge
                Color32 colour;
                if (distance <= radius) colour = Open;
                else
                {
                    colour = Locked;
                    colour.a = (byte)(Locked.a * Mathf.Clamp01((outer - distance) / EdgeFade));
                }
                if (height < waterLevel + 0.2f) colour.a = 0;
                inside[i] = colour.a > 0;
                colors[i] = colour;
            }
        }

        // Flood out from the middle over grid points on land: whatever the flood does not reach is
        // across water, another island, and left out.
        reached = new bool[count * count];
        reachedCount = count;
        reachedOrigin = origin;
        var queue = new System.Collections.Generic.Queue<int>();
        var start = (count / 2) * count + count / 2;
        if (inside[start])
        {
            reached[start] = true;
            queue.Enqueue(start);
        }
        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            var x = i % count;
            var z = i / count;
            for (var n = 0; n < 4; ++n)
            {
                var nx = x + (n == 0 ? 1 : n == 1 ? -1 : 0);
                var nz = z + (n == 2 ? 1 : n == 3 ? -1 : 0);
                if (nx < 0 || nz < 0 || nx >= count || nz >= count) continue;
                var j = nz * count + nx;
                if (reached[j] || !inside[j]) continue;
                reached[j] = true;
                queue.Enqueue(j);
            }
        }
        for (var i = 0; i < inside.Length; ++i)
        {
            if (inside[i] && !reached[i])
            {
                inside[i] = false;
                colors[i].a = 0;
            }
        }

        // only squares with a corner on buildable ground, so the mesh stops at the village edge
        var triangles = new System.Collections.Generic.List<int>(count * count * 6);
        for (var z = 0; z < count - 1; ++z)
        {
            for (var x = 0; x < count - 1; ++x)
            {
                var a = z * count + x;
                var b = a + 1;
                var c = a + count;
                var d = c + 1;
                if (!inside[a] && !inside[b] && !inside[c] && !inside[d]) continue;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        var mesh = filter.sharedMesh;
        if (!mesh)
        {
            mesh = new Mesh { name = "Village Grid" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            filter.sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors32 = colors;
        mesh.uv = uvs;
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        meshRenderer.sharedMaterial = material;

        var used = 0;
        used = DrawCircle(used, centre, radius, EdgeColour, 0.55f, groundHeight, waterLevel);
        if (next > radius + 0.5f) used = DrawCircle(used, centre, next, NextEdgeColour, 0.35f, groundHeight, waterLevel);
        for (var i = used; i < lines.Count; ++i) lines[i].gameObject.SetActive(false);
    }

    /// <summary>
    /// A circle on the ground, broken where it crosses water, as one line per stretch over land.
    /// Returns how many lines are in use.
    /// </summary>
    private int DrawCircle(int used, Vector3 centre, float radius, Color colour, float width, Func<Vector3, float> groundHeight, float waterLevel)
    {
        var steps = Mathf.Max(32, Mathf.CeilToInt(Mathf.PI * 2f * radius / 1.5f));
        var points = new System.Collections.Generic.List<Vector3>();
        var start = -1;
        // begin on water if there is any, so no stretch over land is split at the seam
        for (var k = 0; k < steps && start < 0; ++k)
        {
            if (!OnLand(k)) start = k;
        }

        var closed = start < 0;
        if (closed) start = 0;
        for (var n = 0; n <= steps; ++n)
        {
            var k = (start + n) % steps;
            if (OnLand(k) && !(closed && n == steps))
            {
                points.Add(At(k));
                continue;
            }

            if (closed && n == steps) break;
            if (points.Count >= 2) Line(used++, points, colour, width, false);
            points.Clear();
        }

        if (points.Count >= 2) Line(used++, points, colour, width, closed);
        return used;

        Vector3 FlatAt(int k)
        {
            var a = k / (float)steps * Mathf.PI * 2f;
            return new Vector3(centre.x + Mathf.Cos(a) * radius, 0f, centre.z + Mathf.Sin(a) * radius);
        }

        bool OnLand(int k)
        {
            var p = FlatAt(k);
            return IsOnVillageLand(p) && (groundHeight != null ? groundHeight(p) : PlacementUtility.FindGroundPoint(p).y) >= waterLevel + 0.2f;
        }

        Vector3 At(int k)
        {
            var p = FlatAt(k);
            p.y = (groundHeight != null ? groundHeight(p) : PlacementUtility.FindGroundPoint(p).y) + LineLift;
            return p;
        }
    }

    /// <summary>
    /// Whether a point is on land joined to the plaza, as far out as the grid was last built.
    /// True when no grid has been built, so nothing is refused for want of one.
    /// </summary>
    public bool IsOnVillageLand(Vector3 point)
    {
        if (reached == null) return true;
        var x = Mathf.RoundToInt((point.x - reachedOrigin.x) / Spacing);
        var z = Mathf.RoundToInt((point.z - reachedOrigin.z) / Spacing);
        if (x < 0 || z < 0 || x >= reachedCount || z >= reachedCount) return false;
        // the nearest grid point, or one beside it: the coast runs between grid points
        for (var dz = -1; dz <= 1; ++dz)
        {
            for (var dx = -1; dx <= 1; ++dx)
            {
                var nx = x + dx;
                var nz = z + dz;
                if (nx < 0 || nz < 0 || nx >= reachedCount || nz >= reachedCount) continue;
                if (reached[nz * reachedCount + nx]) return true;
            }
        }
        return false;
    }

    private void Line(int index, System.Collections.Generic.List<Vector3> points, Color colour, float width, bool loop)
    {
        if (!lineMaterial)
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Village Area Edge" };
            lineMaterial.renderQueue = 3001;
        }

        while (lines.Count <= index)
        {
            var go = new GameObject("Area Edge " + lines.Count);
            go.transform.SetParent(transform, false);
            var made = go.AddComponent<LineRenderer>();
            made.sharedMaterial = lineMaterial;
            made.useWorldSpace = true;
            made.numCornerVertices = 2;
            made.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            made.receiveShadows = false;
            lines.Add(made);
        }

        var line = lines[index];
        line.gameObject.SetActive(true);
        line.loop = loop;
        line.widthMultiplier = width;
        line.startColor = colour;
        line.endColor = colour;
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }

    /// <summary>
    /// A texture of five by five cells: a thin line on each cell edge and a stronger one on the
    /// fifth. Transparent between the lines, so the ground shows through.
    /// </summary>
    private Material MakeMaterial(float cellSize)
    {
        var size = PixelsPerCell * MajorEvery;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = "Village Grid Lines",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 8,
        };

        var minor = new Color(1f, 0.96f, 0.84f, 0.14f);
        var major = new Color(1f, 0.96f, 0.84f, 0.42f);
        var clear = new Color(1f, 1f, 1f, 0f);
        var pixels = new Color[size * size];
        for (var y = 0; y < size; ++y)
        {
            for (var x = 0; x < size; ++x)
            {
                var onMajor = x < 3 || y < 3;
                var onMinor = x % PixelsPerCell < 2 || y % PixelsPerCell < 2;
                pixels[y * size + x] = onMajor ? major : onMinor ? minor : clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true);

        // a plain alpha blended, vertex coloured shader: the vertex alpha fades the edge
        var shader = Shader.Find("Sprites/Default");
        var made = new Material(shader) { name = "Village Grid", mainTexture = texture };
        made.renderQueue = 3000;
        return made;
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private void OnDestroy()
    {
        if (material) Destroy(material);
        if (lineMaterial) Destroy(lineMaterial);
        if (texture) Destroy(texture);
        if (filter && filter.sharedMesh) Destroy(filter.sharedMesh);
    }
}
