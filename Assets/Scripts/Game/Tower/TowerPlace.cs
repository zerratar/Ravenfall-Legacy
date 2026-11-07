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

    [Header("Look")]
    public Color SkyAmbient = new Color(0.45f, 0.55f, 0.50f);
    public Color EquatorAmbient = new Color(0.35f, 0.40f, 0.30f);
    public Color GroundAmbient = new Color(0.15f, 0.14f, 0.10f);
    public Color FogColor = new Color(0.55f, 0.65f, 0.55f);
    public float FogDensity = 0.012f;
    public Color SunColor = new Color(1f, 0.95f, 0.85f);
    public float SunIntensity = 1.1f;
    public Vector3 SunAngles = new Vector3(50f, -30f, 0f);

    public Vector3 Centre => transform.position;

    public Vector3 Spawn => SpawnPoint ? SpawnPoint.position : transform.position;

    public bool Contains(Vector3 point)
    {
        var d = point - Centre;
        d.y = 0f;
        return d.sqrMagnitude <= Radius * Radius;
    }
}
