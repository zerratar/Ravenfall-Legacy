using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     The root of a tower floor scene (docs/tower-design.md, section 8): where players arrive, how far the floor
///     reaches (the camera inside it gets the floor's look), the floor's light and fog, and its enemies. A floor scene is
///     loaded next to MainWorld, far away from everything in it, and unloaded when the last player leaves.
/// </summary>
public sealed class TowerFloor : MonoBehaviour
{
    /// <summary>The floor that is loaded now, or null.</summary>
    public static TowerFloor Loaded { get; private set; }

    public Transform SpawnPoint;

    /// <summary>The camera within this distance of the floor's centre (on the ground plane) sees the floor's look.</summary>
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

    private readonly List<EnemyController> enemies = new List<EnemyController>();

    public Vector3 Centre => transform.position;

    public Vector3 Spawn => SpawnPoint ? SpawnPoint.position : transform.position;

    public IReadOnlyList<EnemyController> Enemies => enemies;

    public bool Contains(Vector3 point)
    {
        var d = point - Centre;
        d.y = 0f;
        return d.sqrMagnitude <= Radius * Radius;
    }

    private void OnEnable()
    {
        Loaded = this;
        enemies.Clear();
        enemies.AddRange(GetComponentsInChildren<EnemyController>(true));
    }

    private void OnDisable()
    {
        if (Loaded == this) Loaded = null;
    }

    /// <summary>The nearest enemy still standing to <paramref name="from"/>, or null.</summary>
    public EnemyController NearestEnemy(Vector3 from)
    {
        EnemyController best = null;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < enemies.Count; ++i)
        {
            var e = enemies[i];
            if (!e || e.Stats == null || e.Stats.IsDead || e.IsUnreachable) continue;
            var d = (e.transform.position - from).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = e;
            }
        }
        return best;
    }

    public int AliveEnemies()
    {
        var count = 0;
        for (var i = 0; i < enemies.Count; ++i)
            if (enemies[i] && enemies[i].Stats != null && !enemies[i].Stats.IsDead) count++;
        return count;
    }
}
