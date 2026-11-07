using System.Collections.Generic;
using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     The root of a tower floor scene (docs/tower-design.md, section 8): where players arrive, how far the floor
///     reaches (the camera inside it gets the floor's look), the floor's light and fog, and its enemies. A floor scene is
///     loaded next to MainWorld, far away from everything in it, and unloaded when the last player leaves.
///     A layout scene serves several floors: it holds one hidden template of every monster its floors use
///     (<see cref="Templates"/>, matched by their enemy definition id), the spots monsters stand on
///     (<see cref="Spawns"/>) and the boss's spot; <see cref="Apply"/> fills it for one floor from RavenNest.Combat.Tower.
/// </summary>
public sealed class TowerFloor : MonoBehaviour
{
    /// <summary>The floor that is loaded now, or null.</summary>
    public static TowerFloor Loaded { get; private set; }

    public Transform SpawnPoint;

    /// <summary>Hidden monsters to copy, one per look (their EnemyController.Definition id).</summary>
    public Transform Templates;

    /// <summary>Where the floor's monsters stand: every child is one spot.</summary>
    public Transform Spawns;

    /// <summary>Where the boss waits during an attempt.</summary>
    public Transform BossPoint;

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
    private readonly Dictionary<EnemyController, TowerEnemy> kinds = new Dictionary<EnemyController, TowerEnemy>();
    private readonly List<GameObject> spawned = new List<GameObject>();

    public Vector3 Centre => transform.position;

    public Vector3 Spawn => SpawnPoint ? SpawnPoint.position : transform.position;

    public IReadOnlyList<EnemyController> Enemies => enemies;

    /// <summary>The floor it is filled for, or null (the test scene keeps the monsters it was saved with).</summary>
    public TowerFloorDefinition Definition { get; private set; }

    /// <summary>The boss of an attempt, while there is one.</summary>
    public EnemyController Boss { get; private set; }

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
        foreach (var e in GetComponentsInChildren<EnemyController>(true))
            if (!Templates || !e.transform.IsChildOf(Templates)) enemies.Add(e);
    }

    private void OnDisable()
    {
        if (Loaded == this) Loaded = null;
    }

    /// <summary>
    ///     Fills the floor for <paramref name="floor"/>: its monsters on the spots, at its level, with the band's element.
    ///     Monsters of the floor before go first. Returns false when the scene has no templates or spots (the test floor).
    /// </summary>
    public bool Apply(TowerFloorDefinition floor)
    {
        if (floor == null || !Templates || !Spawns || Spawns.childCount == 0) return false;
        Clear();
        Definition = floor;

        var made = new Dictionary<string, EnemyDefinition>();
        for (var i = 0; i < Spawns.childCount; ++i)
        {
            var kind = floor.Enemies[i % floor.Enemies.Length];
            var template = Template(kind.Model);
            if (!template) continue;
            if (!made.TryGetValue(kind.Id, out var definition)) made[kind.Id] = definition = Tower.Enemy(floor, kind);
            var spot = Spawns.GetChild(i);
            var enemy = Make(template, spot.position, spot.rotation, 1f);
            enemy.ApplyRuntimeDefinition(definition, kind.Name, Tower.EnemyRespawnSeconds);
            enemies.Add(enemy);
            kinds[enemy] = kind;
        }
        return true;
    }

    /// <summary>The boss of the floor at its spot, full health, facing the arena's middle.</summary>
    public EnemyController SpawnBoss()
    {
        DespawnBoss();
        if (Definition == null) return null;
        var b = Definition.Boss;
        var template = Template(b.Model);
        if (!template) return null;
        var point = BossPoint ? BossPoint : transform;
        Boss = Make(template, point.position, point.rotation, b.Scale);
        Boss.AutomaticRespawn = false;
        Boss.ApplyRuntimeDefinition(Tower.Boss(Definition), b.Name, 0f);
        return Boss;
    }

    public void DespawnBoss()
    {
        if (Boss)
        {
            spawned.Remove(Boss.gameObject);
            Destroy(Boss.gameObject);
        }
        Boss = null;
    }

    /// <summary>Which of the floor's monsters an enemy is, or null (the boss, or a monster of the test floor).</summary>
    public TowerEnemy KindOf(EnemyController enemy) => enemy && kinds.TryGetValue(enemy, out var kind) ? kind : null;

    /// <summary>The nearest enemy still standing to <paramref name="from"/>, or null.</summary>
    public EnemyController NearestEnemy(Vector3 from)
    {
        EnemyController best = null;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < enemies.Count; ++i)
        {
            var e = enemies[i];
            if (!e || e.Stats == null || e.Stats.IsDead || e.IsUnreachable || !e.gameObject.activeInHierarchy) continue;
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

    private EnemyController Template(string model)
    {
        if (!Templates) return null;
        foreach (var e in Templates.GetComponentsInChildren<EnemyController>(true))
            if (e.Definition && e.Definition.Definition != null && e.Definition.Definition.Id == model) return e;
        return null;
    }

    private EnemyController Make(EnemyController template, Vector3 position, Quaternion rotation, float scale)
    {
        // the template is hidden: the copy wakes up (registers, takes the asset's numbers) when it is shown, at its spot
        var go = Instantiate(template.gameObject, position, rotation, transform);
        go.name = template.name;
        go.transform.localScale = template.transform.localScale * scale;
        go.SetActive(true);
        spawned.Add(go);
        var enemy = go.GetComponent<EnemyController>();
        enemy.Island = null;
        return enemy;
    }

    private void Clear()
    {
        DespawnBoss();
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        kinds.Clear();
        enemies.Clear();
    }
}
