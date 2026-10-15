using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The sea: a sheet of water at this object's height reaching the horizon, drawn with <c>Ravenfall/Ocean</c>.
/// </summary>
/// <remarks>
/// <para>The sheet is a disc of rings that grow geometrically from the centre out, and it is moved under whichever
/// camera is about to draw, so the water near the eye is dense enough for the swell to lift it and the far water is a
/// few coarse rings. The waves are functions of world position, so moving the sheet does not move the sea. The shader
/// keeps the far rings at the far plane rather than letting it cut them off, so the sea reaches the horizon whatever
/// the view distance setting.</para>
/// <para>Detailed water (the setting) adds refraction, caustics, whitecaps and the light through the crests; without
/// it, or in potato mode, the same sea is drawn with the cheap parts only.</para>
/// <para>Ported from FF3Unity's sea.</para>
/// </remarks>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class Ocean : MonoBehaviour
{
    private const string SurfaceName = "Ocean surface (generated)";
    private static readonly int NormalMapId = Shader.PropertyToID("_NormalMap");
    private static readonly int FoamTexId = Shader.PropertyToID("_FoamTex");

    [Tooltip("The ocean shader, Ravenfall/Ocean. Referenced here so builds include it.")]
    public Shader OceanShader;

    [Tooltip("How far the water reaches from the eye, metres.")]
    public float Radius = 30000f;

    [Tooltip("Rings from the centre out. More rings, finer near water for the swell to lift.")]
    [Range(48, 400)] public int Rings = 160;

    [Tooltip("Vertices round each ring.")]
    [Range(32, 512)] public int Segments = 160;

    [Tooltip("The first ring's radius, metres: how close the finest water is to the eye.")]
    public float Inner = 0.5f;

    private GameObject surface;
    private MeshRenderer surfaceRenderer;
    private Mesh mesh;
    private Material material;
    private bool dirty;

    /// <summary>The setting: the full water, or only its cheap parts.</summary>
    public static bool Detailed { get; set; } = true;

    /// <summary>Cheap water for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    /// <summary>The material drawn, made from <see cref="OceanShader"/>.</summary>
    public Material Material => material;

    private static readonly int[] WaveIds =
    {
        Shader.PropertyToID("_WaveA"), Shader.PropertyToID("_WaveB"), Shader.PropertyToID("_WaveC"), Shader.PropertyToID("_WaveD")
    };
    private static readonly int WaveHeightId = Shader.PropertyToID("_WaveHeight");
    private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");

    /// <summary>The waves' groups, on average: the shader varies them across the sea with a slow noise, 0.1 to 1.6.</summary>
    private const float AverageGroup = 0.6f;

    private static Ocean active;

    private void OnEnable()
    {
        Build();
        RenderPipelineManager.beginCameraRendering += Recentre;
        active = this;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= Recentre;
        Tear();
        if (active == this) active = null;
    }

    /// <summary>
    /// How the sea lies under a hull just now, from the same four waves the shader draws, so a boat can ride them: how
    /// far the water is above its rest level there (metres, the mean of the bow, the stern and both sides) and which
    /// way the water under it faces. Sampled at the hull's ends and sides, so a big ship rides the long waves and the
    /// short chop under it evens out. Near the drawn sea, not exact: the waves' slow grouping is taken as its average.
    /// False with no sea.
    /// </summary>
    public static bool Float(Vector3 centre, Vector3 forward, Vector3 right, float length, float width, out float heave, out Vector3 up)
    {
        heave = 0f;
        up = Vector3.up;
        if (!active || !active.material) return false;
        var m = active.material;
        var t = Time.timeSinceLevelLoad * m.GetFloat(WaveSpeedId);
        var height = m.GetFloat(WaveHeightId);
        var halfLength = forward * (length * 0.5f);
        var halfWidth = right * (width * 0.5f);
        var bow = Height(m, centre + halfLength, t, height);
        var stern = Height(m, centre - halfLength, t, height);
        var port = Height(m, centre - halfWidth, t, height);
        var starboard = Height(m, centre + halfWidth, t, height);
        heave = (bow + stern + port + starboard) * 0.25f;
        var alongSlope = (bow - stern) / Mathf.Max(length, 0.1f);
        var acrossSlope = (starboard - port) / Mathf.Max(width, 0.1f);
        up = (Vector3.up - forward * alongSlope - right * acrossSlope).normalized;
        return true;
    }

    // the water's height at a point: the shader's Gerstner sum, its vertical part (Ocean.shader, Gerstner)
    private static float Height(Material m, Vector3 at, float t, float height)
    {
        var sum = 0f;
        foreach (var id in WaveIds)
        {
            var w = m.GetVector(id);
            var d = new Vector2(w.x, w.y).normalized;
            var steep = w.z * height * AverageGroup;
            var k = 2f * Mathf.PI / Mathf.Max(w.w, 0.5f);
            var c = Mathf.Sqrt(9.8f / k);
            var f = k * (d.x * at.x + d.y * at.z - c * t);
            sum += steep / k * Mathf.Sin(f);
        }
        return sum;
    }

    private void OnValidate()
    {
        Radius = Mathf.Max(Radius, 10f);
        Inner = Mathf.Clamp(Inner, 0.05f, Radius * 0.01f);
        dirty = true; // objects may not be made from OnValidate; the next Update rebuilds
    }

    private void Update()
    {
        if (!surface || dirty)
        {
            dirty = false;
            Build();
        }
        if (material)
        {
            var simple = !Detailed || Suppressed;
            if (material.IsKeywordEnabled("_OCEAN_SIMPLE") != simple)
            {
                if (simple) material.EnableKeyword("_OCEAN_SIMPLE");
                else material.DisableKeyword("_OCEAN_SIMPLE");
            }
        }
    }

    /// <summary>Makes the sheet, or remakes it after a setting changed.</summary>
    public void Build()
    {
        // the child is not saved with the scene and not listed in the hierarchy; anything left from before goes
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c.name == SurfaceName && c.gameObject != surface) DestroyNow(c.gameObject);
        }

        if (!surface)
        {
            surface = new GameObject(SurfaceName) { hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy };
            surface.layer = gameObject.layer;
            surface.transform.SetParent(transform, false);
            surface.AddComponent<MeshFilter>();
            surfaceRenderer = surface.AddComponent<MeshRenderer>();
            surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off;
            surfaceRenderer.receiveShadows = true;
            surfaceRenderer.lightProbeUsage = LightProbeUsage.Off;
            surfaceRenderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            surfaceRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
#if UNITY_EDITOR
            // a click on the water in the Scene view would otherwise select a sheet that is not in the hierarchy
            UnityEditor.SceneVisibilityManager.instance.DisablePicking(surface, false);
#endif
        }
        // the sheet is level and unscaled whatever this object is
        surface.transform.rotation = Quaternion.identity;
        var s = transform.lossyScale;
        surface.transform.localScale = new Vector3(1f / Mathf.Max(s.x, 1e-4f), 1f / Mathf.Max(s.y, 1e-4f), 1f / Mathf.Max(s.z, 1e-4f));

        if (mesh) DestroyNow(mesh);
        mesh = Disc(Rings, Segments, Inner, Radius);
        surface.GetComponent<MeshFilter>().sharedMesh = mesh;

        if (!OceanShader) OceanShader = Shader.Find("Ravenfall/Ocean");
        if (!OceanShader)
        {
            Debug.LogError("Ocean: the Ravenfall/Ocean shader is missing; the sea is not drawn.");
            surfaceRenderer.enabled = false;
            return;
        }
        if (!material || material.shader != OceanShader)
        {
            if (material) DestroyNow(material);
            material = new Material(OceanShader) { name = "Ocean", hideFlags = HideFlags.DontSave };
        }
        material.SetTexture(NormalMapId, WaterTextures.Normals);
        material.SetTexture(FoamTexId, WaterTextures.Noise);
        surfaceRenderer.enabled = true;
        surfaceRenderer.sharedMaterial = material;
    }

    private void Recentre(ScriptableRenderContext context, Camera camera)
    {
        if (!surface || !camera) return;
        if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
        var eye = camera.transform.position;
        surface.transform.position = new Vector3(eye.x, transform.position.y, eye.z);
        // well under the sea (down in a dungeon) the water's underside would fill the top of the view
        if (surfaceRenderer) surfaceRenderer.enabled = eye.y > transform.position.y - 20f;
    }

    private void Tear()
    {
        if (surface) DestroyNow(surface);
        if (mesh) DestroyNow(mesh);
        if (material) DestroyNow(material);
        surface = null;
        surfaceRenderer = null;
        mesh = null;
        material = null;
    }

    private static void DestroyNow(Object o)
    {
        if (!o) return;
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }

    /// <summary>
    /// A flat disc: a centre vertex, then rings of vertices whose radii grow geometrically from
    /// <paramref name="inner"/> to <paramref name="outer"/>, so each ring is about as far from the last as it is wide
    /// on screen. Normals up; no UVs, the shader works in world space. Bounds tall enough for the swell.
    /// </summary>
    public static Mesh Disc(int rings, int segments, float inner, float outer)
    {
        rings = Mathf.Max(rings, 2);
        segments = Mathf.Max(segments, 3);
        inner = Mathf.Max(inner, 0.01f);
        outer = Mathf.Max(outer, inner * 2f);
        var count = 1 + rings * segments;
        var v = new Vector3[count];
        var n = new Vector3[count];
        v[0] = Vector3.zero;
        n[0] = Vector3.up;
        var growth = Mathf.Pow(outer / inner, 1f / (rings - 1));
        for (var r = 0; r < rings; r++)
        {
            var radius = inner * Mathf.Pow(growth, r);
            for (var s = 0; s < segments; s++)
            {
                var a = s / (float)segments * Mathf.PI * 2f;
                var i = 1 + r * segments + s;
                v[i] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                n[i] = Vector3.up;
            }
        }
        var tri = new int[segments * 3 + (rings - 1) * segments * 6];
        var t = 0;
        for (var s = 0; s < segments; s++)
        {
            var s1 = (s + 1) % segments;
            tri[t++] = 0; tri[t++] = 1 + s1; tri[t++] = 1 + s;
        }
        for (var r = 0; r < rings - 1; r++)
        {
            int a0 = 1 + r * segments, b0 = a0 + segments;
            for (var s = 0; s < segments; s++)
            {
                var s1 = (s + 1) % segments;
                tri[t++] = a0 + s; tri[t++] = b0 + s1; tri[t++] = b0 + s;
                tri[t++] = a0 + s; tri[t++] = a0 + s1; tri[t++] = b0 + s1;
            }
        }
        var m = new Mesh { name = "Ocean disc", indexFormat = count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16, hideFlags = HideFlags.DontSave };
        m.SetVertices(v);
        m.SetNormals(n);
        m.SetTriangles(tri, 0, false);
        m.bounds = new Bounds(Vector3.zero, new Vector3(outer * 2f, 20f, outer * 2f));
        return m;
    }
}
