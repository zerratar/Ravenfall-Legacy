using UnityEngine;

/// <summary>
///     A place in the tower that is loaded as its own scene next to MainWorld (docs/tower-design.md, section 8): a
///     floor (<see cref="TowerFloor"/>) or a town (<see cref="TowerTown"/>). Where players arrive, how far it reaches
///     (the camera inside it gets its look) and its light and fog.
/// </summary>
public abstract class TowerPlace : MonoBehaviour
{
    public Transform SpawnPoint;

    /// <summary>The camera within this distance of the place's centre (on the ground plane) sees its look.</summary>
    public float Radius = 90f;

    /// <summary>
    ///     How far below the place's centre the haze floor lies (TowerManager): just under the ground for most places, far
    ///     down for one high in the sky (the Storm Spire's cliffs drop into the clouds).
    /// </summary>
    public float HazeDepth = 3f;

    [Header("Look")]
    public Color SkyAmbient = new Color(0.45f, 0.55f, 0.50f);
    public Color EquatorAmbient = new Color(0.35f, 0.40f, 0.30f);
    public Color GroundAmbient = new Color(0.15f, 0.14f, 0.10f);
    public Color FogColor = new Color(0.55f, 0.65f, 0.55f);
    public float FogDensity = 0.012f;
    public Color SunColor = new Color(1f, 0.95f, 0.85f);
    public float SunIntensity = 1.1f;
    public Vector3 SunAngles = new Vector3(50f, -30f, 0f);

    [Header("Clouds")]
    /// <summary>
    ///     A sea of the world's own volumetric clouds under the place (one high in the sky), instead of none: the same
    ///     clouds as over the islands, so the style does not change from place to place. Heights are from the centre.
    /// </summary>
    public bool CloudSea;
    public float CloudSeaTop = -40f;
    public float CloudSeaBottom = -100f;
    [Range(0f, 1f)] public float CloudSeaCoverage = 0.62f;
    /// <summary>How big a puff is, metres (the sky's are 340; a place seen close up wants smaller).</summary>
    public float CloudSeaPuff = 70f;
    /// <summary>0 for white cumulus, 1 for a storm's heavy grey.</summary>
    [Range(0f, 1f)] public float CloudSeaDark;
    /// <summary>How bright the cloud sea is against the sky's clouds (a place's light is often dimmer than the world's).</summary>
    public float CloudSeaBrightness = 1.5f;

    /// <summary>
    ///     The low-poly cloud meshes round the place: the stand-in while the volumetric clouds are off (the setting or
    ///     potato mode), hidden while they draw.
    /// </summary>
    public GameObject MeshClouds;

    /// <summary>
    ///     The place's life (Ambience, with its setting and off in potato mode): snowflakes, fireflies, butterflies and
    ///     gulls a second round the camera's focus. All none by default.
    /// </summary>
    public float Snow;
    public float Fireflies;
    public float Butterflies;
    public float Gulls;

    /// <summary>How stormy it is, 0 to 1: lightning in the clouds and thunder (WeatherEffects), none by default.</summary>
    [Range(0f, 1f)] public float Storm;

    public Vector3 Centre => transform.position;

    public Vector3 Spawn => SpawnPoint ? SpawnPoint.position : transform.position;

    public bool Contains(Vector3 point)
    {
        var d = point - Centre;
        d.y = 0f;
        return d.sqrMagnitude <= Radius * Radius;
    }
}
