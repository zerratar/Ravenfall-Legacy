using UnityEngine;

/// <summary>
/// A river, lake or pond of fresh water in any shape (the Lumbridge test's River Lum, its sea and swamp pools): a saved
/// mesh drawn with the sea's own shader, tuned clear blue-green and calm. The mesh reaches in under the banks, so the
/// water's edge is wherever the ground rises out of it.
/// </summary>
/// <remarks>
/// As <see cref="SwampPool"/>: the drawn surface is a hidden child made whenever this is enabled and never saved; one
/// material for every river, made once; it follows potato mode (Ocean.Detailed).
/// </remarks>
[ExecuteAlways]
public class RiverWater : MonoBehaviour
{
    private const string SurfaceName = "River Water Surface";

    [Tooltip("The water's surface, in this object's space.")]
    [SerializeField] private Mesh mesh;

    private static Material material;
    private GameObject surface;

    public Mesh Mesh { get => mesh; set { mesh = value; Build(); } }

    private void OnEnable() => Build();

    private void OnDisable()
    {
        if (surface) DestroyNow(surface);
        surface = null;
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
        if (!mesh || !EnsureMaterial()) return;
        if (!surface)
        {
            surface = new GameObject(SurfaceName) { hideFlags = HideFlags.HideAndDontSave };
            surface.layer = gameObject.layer;
            surface.transform.SetParent(transform, false);
            surface.AddComponent<MeshFilter>();
            var r = surface.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
        surface.GetComponent<MeshFilter>().sharedMesh = mesh;
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = Vector3.one;
    }

    /// <summary>Fresh water: the sea's shader, blue-green and clear enough to see the bed near the banks, nearly still.</summary>
    private static bool EnsureMaterial()
    {
        if (material) return true;
        var shader = Shader.Find("Ravenfall/Ocean");
        if (!shader) return false;
        material = new Material(shader) { name = "River Water", hideFlags = HideFlags.DontSave };
        material.SetColor("_ScatterColor", new Color(0.05f, 0.17f, 0.2f));
        material.SetColor("_DeepColor", new Color(0.01f, 0.05f, 0.08f));
        material.SetVector("_Absorption", new Vector4(0.45f, 0.18f, 0.12f, 0f));
        material.SetFloat("_Murk", 1.5f);
        material.SetFloat("_DeepDepth", 1.6f);
        material.SetFloat("_SunScatter", 0.3f);
        material.SetFloat("_Refraction", 0.15f);
        material.SetFloat("_WaveHeight", 0.03f);
        material.SetFloat("_WaveSpeed", 0.4f);
        material.SetFloat("_RippleScale", 2f);
        material.SetFloat("_RippleStrength", 0.35f);
        material.SetFloat("_RippleSpeed", 0.5f);
        material.SetFloat("_Smoothness", 0.92f);
        material.SetFloat("_Reflectivity", 0.6f);
        material.SetFloat("_SunGlitter", 0.6f);
        material.SetFloat("_ShoreFoam", 0.35f);
        material.SetFloat("_ShoreFoamAmount", 0.08f);
        material.SetFloat("_CrestFoam", 0f);
        material.SetFloat("_Caustics", 0.3f);
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
