using UnityEngine;

/// <summary>
/// A still pool of murky water (Ravebridge's swamp): a level disc of the sea's own shader, tuned brown-green, calm and
/// shallow, at this object's height. The ground under it is dug into a shallow bowl (tools/ravebridge/ravebridge-swamp.cs),
/// so the water has depth for its murk and foam, and its shore is wherever the ground rises out of it, not a circle.
/// </summary>
/// <remarks>
/// As <see cref="Ocean"/>: the disc is a hidden child made whenever the pool is enabled or changed and never saved, so
/// the scene keeps only the sizes. One material for every pool, made once; it follows potato mode (Ocean.Detailed).
/// </remarks>
[ExecuteAlways]
public class SwampPool : MonoBehaviour
{
    private const string SurfaceName = "Swamp Pool Surface";

    [Tooltip("Half the pool's length along this object's x, in metres; the disc reaches past the shore and hides under the ground.")]
    [SerializeField, Min(0.5f)] private float radiusX = 6f;

    [Tooltip("Half the pool's length along this object's z, in metres.")]
    [SerializeField, Min(0.5f)] private float radiusZ = 4f;

    private static Material material;
    private static Mesh disc;
    private GameObject surface;

    public float RadiusX { get => radiusX; set { radiusX = Mathf.Max(0.5f, value); Build(); } }
    public float RadiusZ { get => radiusZ; set { radiusZ = Mathf.Max(0.5f, value); Build(); } }

    private void OnEnable() => Build();

    private void OnDisable()
    {
        if (surface) DestroyNow(surface);
        surface = null;
    }

    private void OnValidate()
    {
        radiusX = Mathf.Max(0.5f, radiusX);
        radiusZ = Mathf.Max(0.5f, radiusZ);
        if (surface) surface.transform.localScale = new Vector3(radiusX, 1f, radiusZ);
    }

    private void Update()
    {
        if (!material) return;
        var simple = !Ocean.Detailed || Ocean.Suppressed;
        if (material.IsKeywordEnabled("_OCEAN_SIMPLE") != simple)
        {
            if (simple) material.EnableKeyword("_OCEAN_SIMPLE");
            else material.DisableKeyword("_OCEAN_SIMPLE");
        }
    }

    public void Build()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c.name == SurfaceName && c.gameObject != surface) DestroyNow(c.gameObject);
        }
        if (!EnsureMaterial()) return;
        if (!disc) disc = Ocean.Disc(8, 32, 0.08f, 1f);
        if (!surface)
        {
            surface = new GameObject(SurfaceName) { hideFlags = HideFlags.HideAndDontSave };
            surface.layer = gameObject.layer;
            surface.transform.SetParent(transform, false);
            surface.AddComponent<MeshFilter>().sharedMesh = disc;
            var r = surface.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = new Vector3(radiusX, 1f, radiusZ);
    }

    /// <summary>The swamp's water: the sea's shader, brown-green and thick, nearly still, a faint wash at the edge.</summary>
    private static bool EnsureMaterial()
    {
        if (material) return true;
        var shader = Shader.Find("Ravenfall/Ocean");
        if (!shader) return false;
        material = new Material(shader) { name = "Swamp Water", hideFlags = HideFlags.DontSave };
        material.SetColor("_ScatterColor", new Color(0.09f, 0.12f, 0.06f));
        material.SetColor("_DeepColor", new Color(0.025f, 0.04f, 0.025f));
        material.SetVector("_Absorption", new Vector4(0.9f, 0.6f, 0.9f, 0f));
        material.SetFloat("_Murk", 4f);
        material.SetFloat("_DeepDepth", 0.6f);
        material.SetFloat("_SunScatter", 0.2f);
        material.SetFloat("_Refraction", 0.1f);
        material.SetFloat("_WaveHeight", 0.02f);
        material.SetFloat("_WaveSpeed", 0.3f);
        material.SetFloat("_RippleScale", 2.5f);
        material.SetFloat("_RippleStrength", 0.25f);
        material.SetFloat("_RippleSpeed", 0.3f);
        material.SetFloat("_Smoothness", 0.9f);
        material.SetFloat("_Reflectivity", 0.5f);
        material.SetFloat("_SunGlitter", 0.4f);
        material.SetFloat("_ShoreFoam", 0.3f);
        material.SetFloat("_ShoreFoamAmount", 0.06f);
        material.SetFloat("_CrestFoam", 0f);
        material.SetFloat("_Caustics", 0f);
        material.SetTexture("_NormalMap", WaterTextures.Normals);
        material.SetTexture("_FoamTex", WaterTextures.Noise);
        return true;
    }

    private static void DestroyNow(Object o)
    {
        if (!o) return;
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }
}
