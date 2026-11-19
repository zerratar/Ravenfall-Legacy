using UnityEngine;

/// <summary>
///     The storm over the Storm Spire's places (tower band 3): now and then a bolt comes down out of the clouds onto one
///     of the lightning rods (<see cref="Rods"/>) or somewhere far out over the clouds. A jagged bolt for a moment, a flash
///     of light over the place, and the thunder after it, later and softer the farther away it struck. Only while the
///     camera is at the place, and off in potato mode (the user: what costs frames or changes the look turns off there).
/// </summary>
public sealed class TowerLightning : MonoBehaviour
{
    /// <summary>The rods the near bolts strike (their tops). Without any, every bolt is a far one.</summary>
    public Transform[] Rods = new Transform[0];

    /// <summary>Seconds between bolts, at least and at most.</summary>
    public float MinGap = 7f, MaxGap = 18f;

    /// <summary>How far out the far bolts strike, and the camera's reach that keeps the storm going.</summary>
    public float FarRadius = 160f, CameraRadius = 220f;

    /// <summary>Where the bolts come from, above the place.</summary>
    public float CloudHeight = 70f;

    /// <summary>The share of bolts that strike a rod.</summary>
    [Range(0f, 1f)] public float RodShare = 0.4f;

    private const float BoltSeconds = 0.28f, FlashSeconds = 0.35f;

    private float nextBolt;
    private LineRenderer bolt;
    private Light flash;
    private AudioSource thunder;
    private float boltAt = -10f;
    private float thunderAt = -1f;
    private float thunderVolume;
    private static Material boltMaterial;

    private void OnEnable() => nextBolt = Time.time + Random.Range(2f, MaxGap);

    private void Update()
    {
        var cam = Camera.main;
        var game = GameManager.Instance;
        var calm = !cam || (game && game.PotatoMode) || (cam.transform.position - transform.position).sqrMagnitude > CameraRadius * CameraRadius;
        if (calm)
        {
            if (bolt) bolt.enabled = false;
            if (flash) flash.intensity = 0f;
            nextBolt = Mathf.Max(nextBolt, Time.time + 2f);
            return;
        }

        if (Time.time >= nextBolt)
        {
            nextBolt = Time.time + Random.Range(MinGap, MaxGap);
            Strike(cam.transform.position);
        }

        // the bolt flickers out, the flash fades twice (lightning comes in pulses)
        var t = Time.time - boltAt;
        if (bolt) bolt.enabled = t < BoltSeconds && (t < 0.08f || t > 0.13f);
        if (flash) flash.intensity = t < FlashSeconds ? Mathf.Lerp(2.6f, 0f, t / FlashSeconds) * (t < 0.1f || t > 0.16f ? 1f : 0.3f) : 0f;

        if (thunderAt > 0f && Time.time >= thunderAt)
        {
            thunderAt = -1f;
            var props = RavebridgeRevealProps.Get();
            if (props && props.Boom && thunder) thunder.PlayOneShot(props.Boom, GameSounds.Volume * thunderVolume);
        }
    }

    private void Strike(Vector3 eye)
    {
        Make();
        Vector3 ground;
        var near = Rods != null && Rods.Length > 0 && Random.value < RodShare;
        if (near)
        {
            var rod = Rods[Random.Range(0, Rods.Length)];
            ground = rod ? rod.position : transform.position;
        }
        else
        {
            var a = Random.value * Mathf.PI * 2f;
            var r = FarRadius * Random.Range(0.6f, 1f);
            ground = transform.position + new Vector3(Mathf.Cos(a) * r, Random.Range(-35f, -5f), Mathf.Sin(a) * r);
        }

        // a jagged line from the clouds down, with a side fork
        var top = ground + new Vector3(Random.Range(-12f, 12f), CloudHeight, Random.Range(-12f, 12f));
        const int Points = 14;
        bolt.positionCount = Points;
        for (var i = 0; i < Points; i++)
        {
            var u = i / (float)(Points - 1);
            var p = Vector3.Lerp(top, ground, u);
            if (i > 0 && i < Points - 1) p += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * (3.5f * Mathf.Sin(u * Mathf.PI) + 0.8f);
            bolt.SetPosition(i, p);
        }
        bolt.widthMultiplier = near ? 0.55f : 1.6f;
        boltAt = Time.time;

        flash.transform.rotation = Quaternion.LookRotation((ground - top).normalized + Vector3.down);
        var distance = Vector3.Distance(eye, ground);
        // sound travels at about 343 m/s; the far ones rumble softly
        thunderAt = Time.time + distance / 343f + 0.05f;
        thunderVolume = Mathf.Lerp(0.55f, 0.18f, Mathf.InverseLerp(20f, 250f, distance));
        thunder.pitch = Random.Range(0.5f, 0.7f);
    }

    private void Make()
    {
        if (bolt) return;
        if (!boltMaterial)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) shader = Shader.Find("Sprites/Default");
            boltMaterial = new Material(shader) { name = "Tower Lightning" };
            // bright enough to bloom
            var color = new Color(0.75f, 0.85f, 1f) * 4f;
            if (boltMaterial.HasProperty("_BaseColor")) boltMaterial.SetColor("_BaseColor", color);
            else boltMaterial.color = color;
        }
        var go = new GameObject("Bolt") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(transform, false);
        bolt = go.AddComponent<LineRenderer>();
        bolt.sharedMaterial = boltMaterial;
        bolt.useWorldSpace = true;
        bolt.numCornerVertices = 2;
        bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bolt.receiveShadows = false;
        bolt.enabled = false;

        var light = new GameObject("Flash") { hideFlags = HideFlags.DontSave };
        light.transform.SetParent(transform, false);
        flash = light.AddComponent<Light>();
        flash.type = LightType.Directional;
        flash.color = new Color(0.78f, 0.84f, 1f);
        flash.intensity = 0f;
        flash.shadows = LightShadows.None;

        thunder = gameObject.AddComponent<AudioSource>();
        thunder.spatialBlend = 0f;
        thunder.playOnAwake = false;
    }
}
