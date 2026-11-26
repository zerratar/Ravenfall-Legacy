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
public sealed class TowerFloor : TowerPlace
{
    /// <summary>
    ///     The floors that are loaded now: the part the climb is on and the cleared parts players train on, each in its
    ///     own place (TowerManager's areas, docs/tower-worlds-proposal.md section 13). TowerManager knows which is which.
    /// </summary>
    public static readonly List<TowerFloor> All = new List<TowerFloor>();

    /// <summary>Hidden monsters to copy, one per look (their EnemyController.Definition id).</summary>
    public Transform Templates;

    /// <summary>Where the floor's monsters stand: every child is one spot.</summary>
    public Transform Spawns;

    /// <summary>Where the boss waits during an attempt.</summary>
    public Transform BossPoint;

    /// <summary>
    ///     A floor of rooms (docs/tower-design.md section 22), or null for one arena: then the monsters stand on the
    ///     route's rooms' spots instead of <see cref="Spawns"/>, and the group fights its way from room to room.
    /// </summary>
    public TowerRoute Route;

    private readonly Dictionary<EnemyController, int> zones = new Dictionary<EnemyController, int>();

    /// <summary>The route's arrival (the room the group is in) on a floor of rooms.</summary>
    public override Vector3 Spawn => Route && Route.Valid ? Route.Arrival : base.Spawn;

    private readonly List<EnemyController> enemies = new List<EnemyController>();
    private readonly Dictionary<EnemyController, TowerEnemy> kinds = new Dictionary<EnemyController, TowerEnemy>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<EnemyController> adds = new List<EnemyController>();

    /// <summary>The monsters the boss called during an attempt (still standing or not).</summary>
    public IReadOnlyList<EnemyController> Adds => adds;

    public IReadOnlyList<EnemyController> Enemies => enemies;

    /// <summary>The floor it is filled for, or null (the test scene keeps the monsters it was saved with).</summary>
    public TowerFloorDefinition Definition { get; private set; }

    /// <summary>The boss of an attempt, while there is one.</summary>
    public EnemyController Boss { get; private set; }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        // tree tops between the camera and the group fade (an open part's trees; an indoor one has none)
        if (!GetComponent<TowerCanopyFade>()) gameObject.AddComponent<TowerCanopyFade>();
        enemies.Clear();
        foreach (var e in GetComponentsInChildren<EnemyController>(true))
            if (!Templates || !e.transform.IsChildOf(Templates)) enemies.Add(e);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    /// <summary>
    ///     The pressure the monsters standing now were made for (RavenNest.Combat.TowerPressure): the players on the part
    ///     against the group it is made for, times their strength. 1 until someone asks for another.
    /// </summary>
    public double Pressure { get; private set; } = 1;

    /// <summary>The spots monsters stand on: the layout's, or the rooms' on a floor of rooms.</summary>
    public IEnumerable<Transform> Spots()
    {
        if (Route && Route.Valid)
        {
            foreach (var zone in Route.Zones)
            {
                if (!zone || !zone.Spawns) continue;
                for (var i = 0; i < zone.Spawns.childCount; ++i) yield return zone.Spawns.GetChild(i);
            }
            yield break;
        }
        if (!Spawns) yield break;
        for (var i = 0; i < Spawns.childCount; ++i) yield return Spawns.GetChild(i);
    }

    /// <summary>
    ///     The part's monsters follow the fight's pressure: those that stand untouched take the new numbers now, the
    ///     others when they come back (<see cref="EnemyController.Respawning"/>). They come back sooner under a crowd.
    /// </summary>
    public void SetPressure(double pressure)
    {
        if (Definition == null || System.Math.Abs(pressure - Pressure) < 0.02) return;
        Pressure = pressure;
        scaled.Clear();
        foreach (var e in enemies)
        {
            if (!e || e.Stats == null || e.Stats.IsDead || e.InCombat || e.Stats.Health.CurrentValue < e.Stats.Health.MaxValue) continue;
            Rescale(e);
        }
    }

    private readonly Dictionary<string, EnemyDefinition> scaled = new Dictionary<string, EnemyDefinition>();

    // one of the part's monsters at the pressure of the moment
    private EnemyDefinition Scaled(TowerEnemy kind)
    {
        if (!scaled.TryGetValue(kind.Id, out var definition)) scaled[kind.Id] = definition = Tower.Enemy(Definition, kind, Pressure);
        return definition;
    }

    private float RespawnSeconds => (float)TowerPressure.RespawnSeconds(Definition != null ? Definition.Floor : 1, Pressure);

    private void Rescale(EnemyController enemy)
    {
        if (Definition == null || !kinds.TryGetValue(enemy, out var kind)) return;
        enemy.ApplyRuntimeDefinition(Scaled(kind), kind.Name, RespawnSeconds);
    }

    /// <summary>
    ///     Fills the floor for <paramref name="floor"/>: its monsters on the spots, at its level, with the band's element.
    ///     Monsters of the floor before go first. Returns false when the scene has no templates or spots (the test floor).
    /// </summary>
    public bool Apply(TowerFloorDefinition floor)
    {
        scaled.Clear();
        if (Route && Route.Valid) return ApplyRoute(floor);
        if (floor == null || !Templates || !Spawns || Spawns.childCount == 0) return false;
        Clear();
        Definition = floor;

        var made = new Dictionary<string, EnemyDefinition>();
        for (var i = 0; i < Spawns.childCount; ++i)
        {
            var kind = floor.Enemies[i % floor.Enemies.Length];
            var template = Template(kind.Model);
            if (!template) continue;
            if (!made.TryGetValue(kind.Id, out var definition)) made[kind.Id] = definition = Scaled(kind);
            var spot = Spawns.GetChild(i);
            var enemy = Make(template, spot.position, spot.rotation, kind.Scale > 0 ? kind.Scale : 1f);
            enemy.ApplyRuntimeDefinition(definition, kind.Name, RespawnSeconds);
            enemy.Respawning = Rescale;
            enemy.TowerMonster = true;
            enemies.Add(enemy);
            kinds[enemy] = kind;
        }
        return true;
    }

    /// <summary>
    ///     A floor of rooms: every room's monsters at once (the ones ahead stand waiting behind their gates), the kinds
    ///     dealt out over all the spots in route order as on a one-room floor. Only the hold's monsters come back after
    ///     they fall; the rooms before it stay cleared. The route starts again at its first room.
    /// </summary>
    private bool ApplyRoute(TowerFloorDefinition floor)
    {
        if (floor == null || !Templates || floor.Enemies.Length == 0) return false;
        Clear();
        Definition = floor;
        Route.ResetRoute();

        var made = new Dictionary<string, EnemyDefinition>();
        var i = 0;
        for (var z = 0; z < Route.Zones.Length; z++)
        {
            var zone = Route.Zones[z];
            if (!zone || !zone.Spawns) continue;
            for (var s = 0; s < zone.Spawns.childCount; s++, i++)
            {
                var kind = floor.Enemies[i % floor.Enemies.Length];
                var template = Template(kind.Model);
                if (!template) continue;
                if (!made.TryGetValue(kind.Id, out var definition)) made[kind.Id] = definition = Scaled(kind);
                var spot = zone.Spawns.GetChild(s);
                var enemy = Make(template, spot.position, spot.rotation, kind.Scale > 0 ? kind.Scale : 1f);
                enemy.ApplyRuntimeDefinition(definition, kind.Name, RespawnSeconds);
                enemy.Respawning = Rescale;
                enemy.AutomaticRespawn = z >= Route.Hold;
                enemy.TowerMonster = true;
                enemies.Add(enemy);
                kinds[enemy] = kind;
                zones[enemy] = z;
            }
        }
        return true;
    }

    /// <summary>The room of the route an enemy stands in, or -1 (a one-room floor, the boss, its helpers).</summary>
    public int ZoneOf(EnemyController enemy) => enemy && zones.TryGetValue(enemy, out var z) ? z : -1;

    /// <summary>The monsters of a room still standing.</summary>
    public int AliveIn(int zone)
    {
        var count = 0;
        foreach (var kv in zones)
            if (kv.Value == zone && kv.Key && kv.Key.Stats != null && !kv.Key.Stats.IsDead) count++;
        return count;
    }

    /// <summary>A room's gate opened: its monsters are worth walking to again (a path tried while it was shut failed).</summary>
    public void ZoneOpened(int zone)
    {
        foreach (var kv in zones) if (kv.Value <= zone && kv.Key) kv.Key.IsUnreachable = false;
    }

    /// <summary>Someone in the tower is on this floor (not in the town).</summary>
    public bool AnyoneOn()
    {
        var game = GameManager.Instance;
        if (!game || game.Players == null) return false;
        foreach (var p in game.Players.GetAllPlayers())
            if (p && p.towerHandler != null && p.towerHandler.InTower && p.towerHandler.Floor == this) return true;
        return false;
    }

    // ---- Monsters notice players (the owner's watch of the bot playtest, 2026-11-26: they felt dead) ---------------

    /// <summary>How far a part's monster sees a player and goes for them: shorter on the first floor, so it stays relaxed.</summary>
    public static float NoticeMeters(int floor) => floor <= 1 ? 5.5f : 9f;

    /// <summary>
    ///     The most monsters on one player before another goes for them (the ones the player fights count): one on the
    ///     first floor, three higher up.
    /// </summary>
    public static int NoticeCap(int floor) => floor <= 1 ? 1 : 3;

    private const float NoticeSeconds = 0.5f;
    private float nextNotice;
    private readonly List<PlayerController> noticeable = new List<PlayerController>();
    private readonly Dictionary<PlayerController, int> onPlayer = new Dictionary<PlayerController, int>();

    private void Update()
    {
        if (Definition == null || Time.time < nextNotice) return;
        nextNotice = Time.time + NoticeSeconds;
        Notice();
    }

    /// <summary>
    ///     An idle monster that sees a player within <see cref="NoticeMeters"/> walks over and attacks, as dungeon
    ///     monsters do, unless that player already has <see cref="NoticeCap"/> monsters on them. Not players in a boss
    ///     attempt or recovering from one, and not while everyone gathers for the boss; a monster on a player who went
    ///     into one of those goes back to its spot. Never through a wall or a shut gate, nor from a room not reached.
    /// </summary>
    private void Notice()
    {
        var game = GameManager.Instance;
        if (!game || game.Players == null) return;
        noticeable.Clear();
        onPlayer.Clear();
        var rallying = Route && Route.Valid && Route.Rallying;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!p || p.isDestroyed || p.Removed || p.towerHandler == null || !p.towerHandler.InTower || p.towerHandler.Floor != this) continue;
            if (p.Stats.IsDead || rallying || p.towerHandler.InBossFight || p.towerHandler.SatOut) continue;
            noticeable.Add(p);
            onPlayer[p] = 0;
        }

        for (var i = 0; i < enemies.Count; ++i)
        {
            var e = enemies[i];
            if (!e || !e.TowerMonster || e.Stats == null || e.Stats.IsDead || !e.InCombat || !e.TargetPlayer) continue;
            if (onPlayer.TryGetValue(e.TargetPlayer, out var n)) onPlayer[e.TargetPlayer] = n + 1;
            else e.LoseInterest();
        }
        if (noticeable.Count == 0) return;

        var reach = NoticeMeters(Definition.Floor);
        var cap = NoticeCap(Definition.Floor);
        for (var i = 0; i < enemies.Count; ++i)
        {
            var e = enemies[i];
            if (!e || !e.TowerMonster || e.InCombat || e.IsUnreachable || e.Stats == null || e.Stats.IsDead || !e.gameObject.activeInHierarchy) continue;
            if (Route && zones.TryGetValue(e, out var zone) && !Route.Open(zone)) continue;
            var at = e.transform.position;
            PlayerController seen = null;
            var nearest = reach * reach;
            for (var k = 0; k < noticeable.Count; ++k)
            {
                var p = noticeable[k];
                if (onPlayer[p] >= cap) continue;
                var to = p.Position - at;
                if (Mathf.Abs(to.y) > 3f || to.sqrMagnitude >= nearest) continue;
                nearest = to.sqrMagnitude;
                seen = p;
            }
            if (!seen || UnityEngine.AI.NavMesh.Raycast(at, seen.Position, out _, UnityEngine.AI.NavMesh.AllAreas)) continue;
            e.Notice(seen);
            onPlayer[seen]++;
        }
    }

    /// <summary>The pressure the boss of the attempt was made for (its helpers come by it too).</summary>
    public double BossPressure { get; private set; } = 1;

    /// <summary>
    ///     The part's guardian at its spot, full health, facing the arena's middle, with the numbers the fight's
    ///     <paramref name="pressure"/> asks for (1: the fixed numbers for the suggested group).
    /// </summary>
    public EnemyController SpawnBoss(double pressure = 1)
    {
        DespawnBoss();
        if (Definition == null) return null;
        var b = Definition.Boss;
        var template = Template(b.Model);
        if (!template) return null;
        var point = BossPoint ? BossPoint : transform;
        Boss = Make(template, point.position, point.rotation, b.Scale);
        Boss.MergeDamage = true;
        Boss.AutomaticRespawn = false;
        BossPressure = pressure;
        Boss.ApplyRuntimeDefinition(Tower.Boss(Definition, pressure), b.Name, 0f);
        // its fall is shown (TowerManager): the body stays until it is despawned
        var movement = Boss.GetComponent<EnemyMovementController>();
        if (movement) movement.KeepBody = true;
        return Boss;
    }

    /// <summary>
    ///     The boss before the fight begins, when the group changed during its countdown or intro: the numbers for the
    ///     pressure the fight starts with, full health.
    /// </summary>
    public void RescaleBoss(double pressure)
    {
        if (!Boss || Definition == null || System.Math.Abs(pressure - BossPressure) < 0.02) return;
        BossPressure = pressure;
        Boss.ApplyRuntimeDefinition(Tower.Boss(Definition, pressure), Definition.Boss.Name, 0f);
    }

    /// <summary>
    ///     The boss calls help: the floor's first monster around it, at the floor's level, no respawn; how many and how
    ///     tough by the fight's pressure (<paramref name="count"/> is what the boss calls for the suggested group). They
    ///     go with the boss when the attempt ends.
    /// </summary>
    public int SpawnAdds(int count)
    {
        if (Definition == null || !Boss || count <= 0) return 0;
        var kind = Definition.Enemies[0];
        var template = Template(kind.Model);
        if (!template) return 0;
        count = TowerPressure.Helpers(count, Definition.Floor, BossPressure);
        var definition = Tower.Enemy(Definition, kind, BossPressure, true);
        var made = 0;
        for (var i = 0; i < count; ++i)
        {
            var angle = (i + 0.5f) / count * Mathf.PI * 2f;
            var radius = 3f + Boss.transform.localScale.x * 1.2f;
            var spot = Boss.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 4f, UnityEngine.AI.NavMesh.AllAreas)) spot = hit.position;
            var enemy = Make(template, spot, Quaternion.LookRotation(Boss.transform.position - spot), kind.Scale > 0 ? kind.Scale : 1f);
            enemy.AutomaticRespawn = false;
            enemy.ApplyRuntimeDefinition(definition, kind.Name, 0f);
            adds.Add(enemy);
            made++;
        }
        return made;
    }

    /// <summary>The boss's helpers still standing.</summary>
    public int AliveAdds()
    {
        var count = 0;
        foreach (var a in adds) if (a && a.Stats != null && !a.Stats.IsDead) count++;
        return count;
    }

    /// <summary>The nearest of the boss's helpers still standing, or null.</summary>
    public EnemyController NearestAdd(Vector3 from)
    {
        EnemyController best = null;
        var bestDistance = float.MaxValue;
        foreach (var e in adds)
        {
            if (!e || e.Stats == null || e.Stats.IsDead) continue;
            var d = (e.transform.position - from).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = e; }
        }
        return best;
    }

    /// <summary>The boss and its helpers go; the boss after <paramref name="bossDelay"/> seconds (its fall is shown).</summary>
    public void DespawnBoss(float bossDelay = 0f)
    {
        foreach (var a in adds)
        {
            if (!a) continue;
            spawned.Remove(a.gameObject);
            Destroy(a.gameObject);
        }
        adds.Clear();
        if (Boss)
        {
            spawned.Remove(Boss.gameObject);
            if (bossDelay > 0f) Destroy(Boss.gameObject, bossDelay);
            else Destroy(Boss.gameObject);
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
            // on a floor of rooms, only the rooms the group has reached: the ones ahead wait behind their gates
            if (Route && zones.TryGetValue(e, out var zone) && !Route.Open(zone)) continue;
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
        // under a crowd its damage numbers add up into one (a guardian's always do: SpawnBoss)
        enemy.MergeWhenCrowded = true;
        return enemy;
    }

    private void Clear()
    {
        DespawnBoss();
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        kinds.Clear();
        zones.Clear();
        enemies.Clear();
        scaled.Clear();
    }
}
