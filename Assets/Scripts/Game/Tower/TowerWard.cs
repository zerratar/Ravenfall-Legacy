using UnityEngine;

/// <summary>
///     The light that seals a door of a tower part's route (a child of <see cref="TowerGate.Seal"/>): a curtain of light
///     across the doorway, from the floor to the top of the door, and a strip of the same light on the floor before it.
///     It is what says "this way is shut" from the game's camera, high up and far off: the thorns or the ice that stand
///     in it are the place's own, the curtain is the same at every sealed door, so it is learned once. It rises slowly,
///     like cold flame. A mesh, a generated texture and an additive material made here (as <see cref="TowerLightShaft"/>
///     does). In potato mode it stands still; it stays on, because it tells what the game does.
/// </summary>
/// <remarks>The object sits on the floor in the middle of the doorway, its back (-z) to the room the group comes from.</remarks>
[ExecuteAlways]
public sealed class TowerWard : MonoBehaviour
{
    public float Width = 3.4f;
    public float Height = 3.6f;

    /// <summary>Metres of floor before the door that glow.</summary>
    public float Apron = 1.8f;

    public Color Tint = new Color(0.45f, 1f, 0.62f, 0.6f);

    private static Texture2D flame;
    private Material material;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private float phase;

    private void OnEnable()
    {
        // not ?? : GetComponent hands back Unity's fake null in the editor
        if (!TryGetComponent<MeshFilter>(out var filter)) filter = gameObject.AddComponent<MeshFilter>();
        if (!TryGetComponent(out meshRenderer)) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        filter.hideFlags = HideFlags.DontSave;
        meshRenderer.hideFlags = HideFlags.DontSave;
        filter.sharedMesh = mesh = Curtain();
        if (!flame) flame = Flame();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        material = new Material(shader) { name = "Tower Ward", hideFlags = HideFlags.DontSave };
        material.SetTexture("_BaseMap", flame);
        material.SetColor("_BaseColor", Tint);
        // additive, both sides, no depth writes: light adds to the door behind it
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_BLENDMODE_ADD");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        phase = (transform.position.x * 0.31f + transform.position.z * 0.17f) % 6.28f;
    }

    private void OnDisable()
    {
        if (material) DestroyImmediate(material);
        if (mesh) DestroyImmediate(mesh);
    }

    private void Update()
    {
        if (!material) return;
        var game = Application.isPlaying ? GameManager.Instance : null;
        if (!Application.isPlaying || (game && game.PotatoMode)) return;
        // the light climbs the door and breathes a little, each door in its own time
        var t = Time.time;
        material.SetTextureOffset("_BaseMap", new Vector2(0f, -t * 0.11f));
        var c = Tint;
        c.a *= 0.88f + 0.12f * Mathf.Sin(t * 1.1f + phase);
        material.SetColor("_BaseColor", c);
    }

    /// <summary>
    ///     The curtain (x across the door, y up) and the strip on the floor before it (toward -z): uv.x runs across
    ///     (the texture fades the sides), uv.y up the door and out along the floor; the vertex alpha thins the curtain
    ///     toward its top and the strip toward its end.
    /// </summary>
    private Mesh Curtain()
    {
        const int Rows = 6;
        var verts = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var colors = new System.Collections.Generic.List<Color>();
        var tris = new System.Collections.Generic.List<int>();
        void Sheet(System.Func<float, Vector3> left, System.Func<float, Vector3> right, float uvLength, System.Func<float, float> alpha)
        {
            var first = verts.Count;
            for (var r = 0; r <= Rows; r++)
            {
                var k = r / (float)Rows;
                verts.Add(left(k)); verts.Add(right(k));
                uvs.Add(new Vector2(0f, k * uvLength)); uvs.Add(new Vector2(1f, k * uvLength));
                var a = alpha(k);
                colors.Add(new Color(1f, 1f, 1f, a)); colors.Add(new Color(1f, 1f, 1f, a));
                if (r == Rows) break;
                var i = first + r * 2;
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
                tris.Add(i + 1); tris.Add(i + 2); tris.Add(i + 3);
            }
        }
        var half = Width * 0.5f;
        // the curtain: full at the floor, thinner toward the top of the door, gone a little above it
        Sheet(k => new Vector3(-half, k * Height, 0f), k => new Vector3(half, k * Height, 0f), Height / Width, k => Mathf.Lerp(1f, 0.5f, k) * Mathf.Clamp01((1f - k) * 6f));
        // the strip on the floor: wider than the door, fading out toward the room
        var wide = half + 0.5f;
        Sheet(k => new Vector3(-Mathf.Lerp(half, wide, k), 0.04f, -k * Apron), k => new Vector3(Mathf.Lerp(half, wide, k), 0.04f, -k * Apron), Apron / Width, k => 0.85f * (1f - k) * (1f - k));
        var m = new Mesh { name = "Tower Ward", hideFlags = HideFlags.DontSave };
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetColors(colors);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    /// <summary>
    ///     Tongues of light: soft at both sides, in upright streaks that differ along the door; tiles upward, so it can
    ///     climb without a seam.
    /// </summary>
    private static Texture2D Flame()
    {
        const int W = 64, H = 64;
        var texture = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "Tower Ward", hideFlags = HideFlags.DontSave, wrapModeU = TextureWrapMode.Clamp, wrapModeV = TextureWrapMode.Repeat };
        var pixels = new Color32[W * H];
        for (var y = 0; y < H; y++)
        {
            var v = y / (float)H * Mathf.PI * 2f;
            for (var x = 0; x < W; x++)
            {
                var u = x / (float)(W - 1);
                var edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.16f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / 0.16f));
                // whole waves up the texture, so the top row meets the bottom row
                var streak = 0.5f + 0.25f * Mathf.Sin(v + u * 19f) + 0.15f * Mathf.Sin(2f * v - u * 31f + 1.7f) + 0.1f * Mathf.Sin(3f * v + u * 47f + 0.6f);
                var a = edge * Mathf.Clamp01(0.45f + 0.75f * streak);
                pixels[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        return texture;
    }
}
