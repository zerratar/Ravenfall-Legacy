using UnityEngine;

/// <summary>
///     A shaft of sunlight pouring in through a window of a ruined hall (the Overgrown Halls and the Marsh, concept
///     docs/mockups/tower/concept-overgrown.jpg): a soft, glowing beam from the window's opening along the sun, widening a
///     little and fading out as it nears the floor, with a slow drift in its brightness like dust in the air. A mesh, a
///     generated texture and an additive material made here. Off in potato mode.
/// </summary>
/// <remarks>The object sits at the window's middle with its forward along the sunlight.</remarks>
[ExecuteAlways]
public sealed class TowerLightShaft : MonoBehaviour
{
    public float Width = 4f;
    public float Height = 10f;
    public float Length = 18f;
    /// <summary>How much wider and taller the beam is at its far end.</summary>
    public float Spread = 1.35f;
    public Color Tint = new Color(1f, 0.86f, 0.58f, 0.16f);

    private static Texture2D soft;
    private Material material;
    private MeshRenderer meshRenderer;
    private float phase;

    private void OnEnable()
    {
        // not ?? : GetComponent hands back Unity's fake null in the editor
        if (!TryGetComponent<MeshFilter>(out var filter)) filter = gameObject.AddComponent<MeshFilter>();
        if (!TryGetComponent(out meshRenderer)) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        filter.sharedMesh = Beam();
        if (!soft) soft = Soft();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        material = new Material(shader) { name = "Tower Light Shaft", hideFlags = HideFlags.DontSave };
        material.SetTexture("_BaseMap", soft);
        material.SetColor("_BaseColor", Tint);
        // additive, both sides, no depth writes: light adds to what is behind it
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
        phase = (transform.position.x * 0.37f + transform.position.z * 0.21f) % 6.28f;
    }

    private void OnDisable()
    {
        if (material) DestroyImmediate(material);
    }

    private void Update()
    {
        if (!material || !meshRenderer) return;
        var game = Application.isPlaying ? GameManager.Instance : null;
        var on = !(game && game.PotatoMode);
        if (meshRenderer.enabled != on) meshRenderer.enabled = on;
        if (!on) return;
        // dust drifting through it: the brightness breathes slowly, each shaft in its own time
        var t = Application.isPlaying ? Time.time : 0f;
        var c = Tint;
        c.a *= 0.8f + 0.2f * Mathf.Sin(t * 0.35f + phase) + 0.08f * Mathf.Sin(t * 1.3f + phase * 2f);
        material.SetColor("_BaseColor", c);
    }

    /// <summary>
    ///     The beam: four sides of a box from the window (z 0) to the floor (z Length), widening along it; uv.x runs across
    ///     each side (the soft texture fades its edges), the vertex alpha fades along it, and a little at the window too.
    /// </summary>
    private Mesh Beam()
    {
        const int Rows = 8;
        var verts = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var cols = new System.Collections.Generic.List<Color>();
        var tris = new System.Collections.Generic.List<int>();
        // the four sides, each as two corners of the rectangle at unit size
        var sides = new[] { (new Vector2(-1, -1), new Vector2(1, -1)), (new Vector2(1, -1), new Vector2(1, 1)), (new Vector2(1, 1), new Vector2(-1, 1)), (new Vector2(-1, 1), new Vector2(-1, -1)) };
        foreach (var (a, b) in sides)
        {
            var start = verts.Count;
            for (var i = 0; i <= Rows; i++)
            {
                var t = i / (float)Rows;
                var s = Mathf.Lerp(1f, Spread, t);
                var z = Length * t;
                var alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 6f)) * (1f - Mathf.SmoothStep(0f, 1f, t));
                foreach (var (corner, u) in new[] { (a, 0f), (b, 1f) })
                {
                    verts.Add(new Vector3(corner.x * Width * 0.5f * s, corner.y * Height * 0.5f * s, z));
                    uvs.Add(new Vector2(u, t));
                    cols.Add(new Color(1f, 1f, 1f, alpha));
                }
            }
            for (var i = 0; i < Rows; i++)
            {
                var v = start + i * 2;
                tris.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
            }
        }
        var m = new Mesh { name = "Tower Light Shaft", hideFlags = HideFlags.DontSave };
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetColors(cols);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    /// <summary>A soft band: bright in the middle across, fading to nothing at both edges.</summary>
    private static Texture2D Soft()
    {
        const int W = 64, H = 4;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "Light Shaft Soft", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color32[W * H];
        for (var x = 0; x < W; x++)
        {
            var a = Mathf.Pow(Mathf.Sin(Mathf.PI * (x + 0.5f) / W), 1.6f);
            for (var y = 0; y < H; y++) px[y * W + x] = new Color32(255, 255, 255, (byte)(255 * a));
        }
        tex.SetPixels32(px);
        tex.Apply(true);
        return tex;
    }
}
