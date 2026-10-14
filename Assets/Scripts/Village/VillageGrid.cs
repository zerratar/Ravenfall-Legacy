using System;
using UnityEngine;

/// <summary>
/// The grid over the whole buildable area while build mode is on. Buildings snap to a 1 m grid,
/// but the only grid shown was a 13 m patch round the building being moved, which said little
/// about where building is possible. This lays the grid over every part of the village a
/// building may stand on: a mesh draped over the ground, a line every metre and a stronger one
/// every five, fading out towards the edge of the village and left out over water.
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

    private MeshFilter filter;
    private MeshRenderer meshRenderer;
    private Material material;
    private Texture2D texture;
    private Vector3 builtCentre = new Vector3(float.NaN, 0f, 0f);
    private float builtRadius = float.NaN;

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
    /// Shows the grid over a circle of <paramref name="radius"/> round <paramref name="centre"/>.
    /// <paramref name="groundHeight"/> gives the terrain under a point, ignoring buildings, so the
    /// grid lies on the ground rather than over floors; ground below <paramref name="waterLevel"/>
    /// is water and left out.
    /// </summary>
    public void Show(Vector3 centre, float radius, float cellSize, Func<Vector3, float> groundHeight, float waterLevel)
    {
        if (!Mathf.Approximately(radius, builtRadius) || (Flat(centre) - Flat(builtCentre)).sqrMagnitude > 0.01f)
        {
            Build(centre, radius, cellSize, groundHeight, waterLevel);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Build(Vector3 centre, float radius, float cellSize, Func<Vector3, float> groundHeight, float waterLevel)
    {
        builtCentre = centre;
        builtRadius = radius;

        if (!material) material = MakeMaterial(cellSize);

        var count = Mathf.CeilToInt(radius * 2f / Spacing) + 1;
        var origin = new Vector3(centre.x - radius, 0f, centre.z - radius);
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

                var alpha = Mathf.Clamp01((radius - distance) / EdgeFade);
                if (height < waterLevel + 0.2f) alpha = 0f;
                inside[i] = alpha > 0f;
                colors[i] = new Color32(255, 255, 255, (byte)(alpha * 255f));
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
        if (texture) Destroy(texture);
        if (filter && filter.sharedMesh) Destroy(filter.sharedMesh);
    }
}
