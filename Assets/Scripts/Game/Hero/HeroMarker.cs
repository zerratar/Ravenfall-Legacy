using UnityEngine;

/// <summary>
/// Where a click sent the hero (HeroController): a ring on the ground that grows and fades (the Hero Marker shader,
/// Resources/HeroMarker). Gold for walking there, red under an enemy to attack. A few are reused.
/// </summary>
public class HeroMarker : MonoBehaviour
{
    private const float Seconds = 0.5f;
    private const int Pooled = 4;

    public static readonly Color Walk = new Color(1f, 0.82f, 0.35f, 1f);
    public static readonly Color Attack = new Color(1f, 0.25f, 0.2f, 1f);

    private static readonly HeroMarker[] pool = new HeroMarker[Pooled];
    private static int next;
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    private Material material;
    private float started;

    public static void Show(Vector3 at, Color color, float size = 1.4f)
    {
        var marker = pool[next];
        if (!marker)
        {
            var template = Resources.Load<Material>("HeroMarker");
            if (!template) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Hero marker";
            Destroy(go.GetComponent<Collider>());
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            marker = go.AddComponent<HeroMarker>();
            marker.material = new Material(template) { hideFlags = HideFlags.DontSave };
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = marker.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            pool[next] = marker;
        }
        next = (next + 1) % Pooled;
        marker.transform.position = at + Vector3.up * 0.06f;
        marker.transform.localScale = Vector3.one * size;
        marker.material.SetColor(ColorId, color);
        marker.material.SetFloat(ProgressId, 0f);
        marker.started = Time.time;
        marker.gameObject.SetActive(true);
    }

    private void Update()
    {
        var t = (Time.time - started) / Seconds;
        if (t >= 1f) { gameObject.SetActive(false); return; }
        material.SetFloat(ProgressId, t);
    }

    private void OnDestroy()
    {
        if (material) Destroy(material);
    }
}
