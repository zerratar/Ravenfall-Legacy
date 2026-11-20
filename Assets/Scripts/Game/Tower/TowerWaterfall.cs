using UnityEngine;

/// <summary>
///     A waterfall spilling off a floating island's rim (Galewharf, tower band 3): a strip that curves out over the edge
///     and falls, with a streaky water texture scrolling down it and fading out long before the clouds. Synty's own
///     waterfall shader does not draw in this pipeline (a grey slab), so this one is made here: a mesh, a generated
///     texture and an unlit transparent material. The scrolling stops in potato mode; it costs nothing else.
/// </summary>
[ExecuteAlways]
public sealed class TowerWaterfall : MonoBehaviour
{
    public float Width = 2.6f;
    public float Height = 34f;
    /// <summary>How far it arcs out over the rim before it falls straight.</summary>
    public float Reach = 2.2f;
    public float Speed = 0.9f;
    public Color Tint = new Color(0.82f, 0.92f, 1f, 0.85f);

    private static Texture2D streaks;
    private Material material;
    private MeshRenderer meshRenderer;

    private void OnEnable() => Build();

    private void OnDisable()
    {
        if (material) DestroyImmediate(material);
    }

    private void Build()
    {
        // not ?? : GetComponent hands back Unity's fake null in the editor
        if (!TryGetComponent<MeshFilter>(out var filter)) filter = gameObject.AddComponent<MeshFilter>();
        if (!TryGetComponent(out meshRenderer)) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        filter.sharedMesh = Strip();
        if (!streaks) streaks = Streaks();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        material = new Material(shader) { name = "Tower Waterfall", hideFlags = HideFlags.DontSave };
        material.SetTexture("_BaseMap", streaks);
        material.SetColor("_BaseColor", Tint);
        // transparent, both sides, no shadows
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void Update()
    {
        if (!material) return;
        var game = Application.isPlaying ? GameManager.Instance : null;
        if (game && game.PotatoMode) return;
        var t = Application.isPlaying ? Time.time : (float)(System.DateTime.Now.TimeOfDay.TotalSeconds % 1000.0);
        material.SetTextureOffset("_BaseMap", new Vector2(0f, t * Speed));
    }

    /// <summary>The strip: local +z is out over the rim, it arcs out and falls down -y; uv.y runs down it.</summary>
    private Mesh Strip()
    {
        const int Rows = 24;
        var vertices = new Vector3[(Rows + 1) * 2];
        var uv = new Vector2[vertices.Length];
        var colors = new Color[vertices.Length];
        for (var i = 0; i <= Rows; i++)
        {
            var t = i / (float)Rows;
            // out over the edge first (a quarter circle), then straight down, widening a little as it falls
            var arc = Mathf.Clamp01(t * 6f);
            var z = Reach * Mathf.Sin(arc * Mathf.PI * 0.5f);
            var y = -Height * t;
            var w = Width * (1f + 0.6f * t) * 0.5f;
            vertices[i * 2] = new Vector3(-w, y, z);
            vertices[i * 2 + 1] = new Vector3(w, y, z);
            uv[i * 2] = new Vector2(0f, t * Height / Width * 0.5f);
            uv[i * 2 + 1] = new Vector2(1f, t * Height / Width * 0.5f);
            // it thins into spray and fades well above the clouds
            var a = Mathf.Clamp01(1f - Mathf.InverseLerp(0.55f, 1f, t));
            colors[i * 2] = colors[i * 2 + 1] = new Color(1f, 1f, 1f, a);
        }
        var triangles = new int[Rows * 6];
        for (var i = 0; i < Rows; i++)
        {
            var a = i * 2;
            triangles[i * 6] = a; triangles[i * 6 + 1] = a + 2; triangles[i * 6 + 2] = a + 1;
            triangles[i * 6 + 3] = a + 1; triangles[i * 6 + 4] = a + 2; triangles[i * 6 + 5] = a + 3;
        }
        var m = new Mesh { name = "Tower Waterfall", vertices = vertices, uv = uv, colors = colors, triangles = triangles, hideFlags = HideFlags.DontSave };
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Vertical streaks of water, lighter and darker, tiling up and down.</summary>
    private static Texture2D Streaks()
    {
        const int W = 64, H = 128;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "Waterfall Streaks", wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave };
        var px = new Color32[W * H];
        var rnd = new System.Random(7);
        var lanes = new float[W];
        for (var x = 0; x < W; x++) lanes[x] = (float)rnd.NextDouble();
        for (var y = 0; y < H; y++)
            for (var x = 0; x < W; x++)
            {
                // streaks along y, broken now and then; the edges of the strip fade
                var streak = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.35f + lanes[x] * 5f, y * 0.06f + lanes[x] * 10f);
                var edge = Mathf.Sin(Mathf.PI * (x + 0.5f) / W);
                var v = Mathf.Clamp01(streak);
                px[y * W + x] = new Color32((byte)(200 + 55 * v), (byte)(220 + 35 * v), 255, (byte)(255 * Mathf.Clamp01(edge * 1.4f) * (0.45f + 0.55f * v)));
            }
        tex.SetPixels32(px);
        tex.Apply(true);
        return tex;
    }
}
