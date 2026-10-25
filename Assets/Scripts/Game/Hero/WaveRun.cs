using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A wave run (docs/hero-mode-design.md, 7b): the hero holds Ravebridge against waves of enemies, started as a quest
/// (Captain Hargrove's "Hold the line", RavebridgeQuests' defend step) and, once that is done, again for the score.
///
/// After a short count, each wave comes in from the two gates nearest the hero (tools/hero/hero-places.cs; a ring
/// around the hero when every gate is far): goblins first, then warriors, ghosts, skeletons and skeleton knights,
/// more of them each wave, and every fifth wave a boss. They go for the hero. Between waves a breather refills some
/// health and mana. The run is a fair fight at any level: an enemy's health is a number of the hero's own average
/// hits, and its blow a share of the hero's health (HeroController.AdjustTaken), both growing with the waves.
///
/// At zero health the hero is downed: chat has 20 seconds to get the hero back up with !rb revive (a share of the
/// viewers on the island), or a healer standing close does it. Otherwise the run is over: the score is the waves
/// held, the best is kept per character, and chat hears it. Viewers fighting on the island are sent at the waves.
/// </summary>
public class WaveRun : MonoBehaviour
{
    public enum Phase { Gathering, Fighting, Breather, Over }

    private const float GatherSeconds = 8f;
    private const float BreatherSeconds = 12f;
    private const float DownedSeconds = 20f;
    private const float HealerReviveSeconds = 5f;
    private const int MaxAlive = 12;
    private const float SpawnInterval = 0.7f;
    private const float BlowCooldown = 1f;

    /// <summary>A kind of wave enemy: the rig's variants it shows as, the wave it starts coming at, how many of the
    /// hero's hits it takes and how much of the hero's health its blow takes (both at wave 1).</summary>
    private sealed class Kind
    {
        public Func<string> Name;
        public string[] Variants;
        public int From;
        public float Hits;
        public float Blow;
        public float Scale = 1f;
        public bool Boss;
    }

    private static readonly Kind[] Kinds =
    {
        new Kind { Name = () => Localization.UI_WAVE_GOBLIN, Variants = new[] { "Character_Goblin_Male", "Character_Goblin_Female" }, From = 1, Hits = 2.5f, Blow = 0.008f },
        new Kind { Name = () => Localization.UI_WAVE_WARRIOR, Variants = new[] { "Character_Goblin_Warrior_Male", "Character_Goblin_Warrior_Female" }, From = 3, Hits = 4f, Blow = 0.012f, Scale = 1.05f },
        new Kind { Name = () => Localization.UI_WAVE_GHOST, Variants = new[] { "Character_Ghost_01", "Character_Ghost_02" }, From = 7, Hits = 3f, Blow = 0.014f },
        new Kind { Name = () => Localization.UI_WAVE_SKELETON, Variants = new[] { "Character_Skeleton_Soldier_01", "Character_Skeleton_Soldier_02", "Character_Skeleton_Slave_01" }, From = 6, Hits = 5f, Blow = 0.013f },
        new Kind { Name = () => Localization.UI_WAVE_SKELETON_KNIGHT, Variants = new[] { "Character_Skeleton_Knight" }, From = 11, Hits = 7f, Blow = 0.017f, Scale = 1.1f },
    };

    // every fifth wave, in turn
    private static readonly Kind[] Bosses =
    {
        new Kind { Name = () => Localization.UI_WAVE_BOSS_CHIEF, Variants = new[] { "Character_Goblin_WarChief" }, Hits = 30f, Blow = 0.045f, Scale = 1.8f, Boss = true },
        new Kind { Name = () => Localization.UI_WAVE_BOSS_GOLEM, Variants = new[] { "Character_Rock_Golem" }, Hits = 40f, Blow = 0.055f, Scale = 1.5f, Boss = true },
        new Kind { Name = () => Localization.UI_WAVE_BOSS_SOUL, Variants = new[] { "Character_Tormented_Soul" }, Hits = 36f, Blow = 0.05f, Scale = 1.7f, Boss = true },
        new Kind { Name = () => Localization.UI_WAVE_BOSS_KNIGHT, Variants = new[] { "Character_Skeleton_Knight" }, Hits = 50f, Blow = 0.065f, Scale = 2.1f, Boss = true },
    };

    public static WaveRun Current { get; private set; }

    /// <summary>How a run ended (kept after the run itself is gone).</summary>
    public sealed class Result
    {
        public string Hero;
        public int Goal, Cleared, Kills, Best;
        public bool NewBest, Abandoned;
        public float StartedAt, EndedAt;
    }

    /// <summary>The last run's end, for the HUD to show for a while.</summary>
    public static Result Last { get; private set; }

    public static event Action Changed;

    public PlayerController Hero { get; private set; }
    public Phase State { get; private set; }
    public int Wave { get; private set; }
    public int Cleared { get; private set; }
    public int Goal { get; private set; }
    public int Kills { get; private set; }
    public float StartedAt { get; private set; }
    public float PhaseEndsAt { get; private set; }
    public bool Downed { get; private set; }
    public float DownedUntil { get; private set; }
    public int Votes => votes.Count;
    public int VotesNeeded { get; private set; }
    public float HealerProgress => healerTime / HealerReviveSeconds;
    public int Best { get; private set; }
    public bool NewBest { get; private set; }
    public bool Abandoned { get; private set; }
    public EnemyController Boss { get; private set; }
    public string BossName { get; private set; }
    public int Left => toSpawn.Count + alive.Count;

    private readonly Dictionary<EnemyController, Kind> alive = new Dictionary<EnemyController, Kind>();
    private readonly Dictionary<EnemyController, float> lastBlow = new Dictionary<EnemyController, float>();
    private readonly Queue<Kind> toSpawn = new Queue<Kind>();
    private readonly List<EnemyController> pool = new List<EnemyController>();
    private readonly List<(EnemyController Enemy, float At)> fallen = new List<(EnemyController, float)>();
    private readonly HashSet<string> votes = new HashSet<string>();
    private Action<WaveRun> finished;
    private EnemyController template;
    private Chunk chunk;
    private Transform container;
    private Transform[] gates = Array.Empty<Transform>();
    private float nextSpawn;
    private int nextGate;
    private float nextOrders;
    private float healerTime;
    private PlayerController lastHealer;
    private int revives;

    /// <summary>Starts a run for the hero (in hero mode on Ravebridge). Goal 0 is endless; finished hears the end.</summary>
    public static WaveRun Begin(PlayerController hero, int goal, Action<WaveRun> finished)
    {
        if (Current || !hero || !hero.InRavebridge || !hero.Island) return null;
        var run = new GameObject("Wave run").AddComponent<WaveRun>();
        if (!run.Prepare(hero))
        {
            Destroy(run.gameObject);
            return null;
        }
        run.Goal = goal;
        run.finished = finished;
        run.StartedAt = Time.time;
        run.Best = PlayerPrefs.GetInt(BestKey(hero), 0);
        run.SetPhase(Phase.Gathering, GatherSeconds);
        Current = run;
        Last = null;
        GameManager.Instance?.RavenBot?.Announce(Localization.MSG_WAVE_STARTED, hero.Name);
        Changed?.Invoke();
        return run;
    }

    public static int BestOf(PlayerController hero) => hero ? PlayerPrefs.GetInt(BestKey(hero), 0) : 0;

    private static string BestKey(PlayerController hero) => "rf_hero_best_" + hero.Id;

    /// <summary>The island's goblins give the template (the rig with every variant) and the chunk the waves belong to,
    /// so the hero's attack and the viewers' training work on them as on any enemy.</summary>
    private bool Prepare(PlayerController hero)
    {
        Hero = hero;
        foreach (var c in hero.Island.GetComponentsInChildren<Chunk>(true))
        {
            if (c.Type != TaskType.Fighting) continue;
            var goblin = c.GetComponentsInChildren<EnemyController>(true).FirstOrDefault(e => e.transform.Find("Character_Goblin_WarChief"));
            if (!goblin) continue;
            chunk = c;
            template = goblin;
            break;
        }
        if (!chunk || !template) return false;
        container = new GameObject("Waves").transform;
        container.SetParent(chunk.transform, false);
        // the gates (tools/hero/hero-places.cs), under the island
        var group = hero.Island.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Wave gates");
        gates = group ? group.Cast<Transform>().ToArray() : Array.Empty<Transform>();
        return true;
    }

    private void SetPhase(Phase phase, float seconds)
    {
        State = phase;
        PhaseEndsAt = Time.time + seconds;
        Changed?.Invoke();
    }

    // ---- The run ------------------------------------------------------------------------------------------

    private void Update()
    {
        if (State == Phase.Over) return;
        var hero = HeroController.Active;
        if (!Hero || !Hero.InRavebridge || !hero || hero.Player != Hero) { Finish(true); return; }

        PoolFallen();
        if (Downed) { UpdateDowned(); }

        switch (State)
        {
            case Phase.Gathering:
            case Phase.Breather:
                if (Time.time >= PhaseEndsAt) StartWave();
                break;
            case Phase.Fighting:
                Spawn();
                CountFallen();
                if (toSpawn.Count == 0 && alive.Count == 0) WaveCleared();
                break;
        }

        if (Time.time >= nextOrders)
        {
            nextOrders = Time.time + 0.5f;
            GiveOrders();
        }
    }

    private void StartWave()
    {
        Wave++;
        toSpawn.Clear();
        Boss = null;
        BossName = null;
        var viewers = Others().Count();
        var count = Mathf.Min(40, 3 + Wave * 2 + Mathf.Min(10, viewers / 2));
        if (Wave % 5 == 0)
        {
            toSpawn.Enqueue(Bosses[(Wave / 5 - 1) % Bosses.Length]);
            count /= 2;
        }
        var kinds = Kinds.Where(k => k.From <= Wave).ToArray();
        for (var i = 0; i < count; i++)
        {
            // the newest kind is rarer than the ones before it
            var k = kinds[Mathf.Min(kinds.Length - 1, Mathf.FloorToInt(Mathf.Pow(UnityEngine.Random.value, 1.6f) * kinds.Length))];
            toSpawn.Enqueue(k);
        }
        nextSpawn = Time.time;
        SetPhase(Phase.Fighting, 0f);
    }

    private void WaveCleared()
    {
        Cleared = Wave;
        var hero = HeroController.Active;
        if (hero)
        {
            hero.Refill(0.25f, 40f);
            if (Wave % 5 == 0) hero.RefillPotions();
        }
        if (Goal > 0 && Cleared >= Goal)
        {
            Finish(false);
            return;
        }
        SetPhase(Phase.Breather, BreatherSeconds);
    }

    private void Spawn()
    {
        if (toSpawn.Count == 0 || alive.Count >= MaxAlive || Time.time < nextSpawn) return;
        nextSpawn = Time.time + SpawnInterval;
        var kind = toSpawn.Dequeue();
        var at = SpawnPoint();
        var enemy = Make(kind, at);
        if (!enemy) return;
        alive[enemy] = kind;
        if (kind.Boss)
        {
            Boss = enemy;
            BossName = kind.Name();
            Changed?.Invoke();
        }
    }

    /// <summary>The next of the two gates nearest the hero, or a place around the hero when every gate is far.</summary>
    private Vector3 SpawnPoint()
    {
        var from = Hero.Position;
        var near = gates.Where(g => g).OrderBy(g => Vector3.Distance(g.position, from)).Where(g => Vector3.Distance(g.position, from) >= 12f).Take(2).ToArray();
        if (near.Length > 0 && Vector3.Distance(near[0].position, from) <= 70f)
        {
            var gate = near[nextGate++ % near.Length];
            return Around(gate.position, 3f);
        }
        for (var i = 0; i < 12; i++)
        {
            var angle = UnityEngine.Random.value * Mathf.PI * 2f;
            var p = from + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * UnityEngine.Random.Range(22f, 30f);
            if (NavMesh.SamplePosition(p, out var hit, 4f, NavMeshHelper.AreaMaskAt(from))) return hit.position;
        }
        return Around(from, 15f);
    }

    private static Vector3 Around(Vector3 center, float radius)
    {
        var offset = UnityEngine.Random.insideUnitCircle * radius;
        var p = center + new Vector3(offset.x, 0f, offset.y);
        return NavMesh.SamplePosition(p, out var hit, radius + 2f, NavMeshHelper.AreaMaskAt(center)) ? hit.position : center;
    }

    /// <summary>A wave enemy of a kind: a fallen one again, or a new copy of the goblin rig. Its health is the kind's
    /// hits of the hero's own average hit, growing with the wave.</summary>
    private EnemyController Make(Kind kind, Vector3 at)
    {
        EnemyController enemy;
        if (pool.Count > 0)
        {
            enemy = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
        }
        else
        {
            enemy = Instantiate(template, container);
            enemy.Stats = template.Stats * 1f;
            enemy.EquipmentStats = template.EquipmentStats;
        }
        if (!enemy) return null;

        var variant = kind.Variants[UnityEngine.Random.Range(0, kind.Variants.Length)];
        foreach (Transform part in enemy.transform)
            if (part.name.StartsWith("Character_")) part.gameObject.SetActive(part.name == variant);
        enemy.name = kind.Name();
        enemy.transform.localScale = Vector3.one * kind.Scale;
        enemy.AutomaticRespawn = false;
        enemy.Hunting = true;
        enemy.AggroRange = 60f;
        enemy.ExpFactor = (kind.Boss ? 5d : 1d) * (1d + Wave * 0.1d);

        var hero = HeroController.Active;
        var hit = hero ? hero.ExpectedHit(enemy) : 10f;
        var hits = kind.Hits * (1f + Wave * 0.06f) * (kind.Boss ? 1f + Others().Count() * 0.1f : 1f);
        enemy.Stats.Health = new SkillStat((float)Mathf.Max(5, Mathf.RoundToInt(hit * hits)));
        enemy.SpawnAt(at);
        enemy.InCombat = true;
        enemy.SetTarget(Hero);
        lastBlow.Remove(enemy);
        return enemy;
    }

    private void CountFallen()
    {
        List<EnemyController> dead = null;
        foreach (var e in alive.Keys)
        {
            if (e && !e.Stats.IsDead) continue;
            (dead ??= new List<EnemyController>()).Add(e);
        }
        if (dead == null) return;
        foreach (var e in dead)
        {
            alive.Remove(e);
            if (!e) continue;
            Kills++;
            if (e == Boss) { Boss = null; Changed?.Invoke(); }
            fallen.Add((e, Time.time + 3.5f));
        }
    }

    /// <summary>The fallen go back to the pool once their death has played.</summary>
    private void PoolFallen()
    {
        for (var i = fallen.Count - 1; i >= 0; i--)
        {
            if (Time.time < fallen[i].At) continue;
            var e = fallen[i].Enemy;
            fallen.RemoveAt(i);
            if (!e) continue;
            e.gameObject.SetActive(false);
            pool.Add(e);
        }
    }

    /// <summary>
    /// Twice a second: the waves go for the hero (or, while the hero is down, for a viewer close by), and viewers
    /// training combat on the island go for the nearest wave enemy.
    /// </summary>
    private void GiveOrders()
    {
        var others = Others().ToList();
        foreach (var e in alive.Keys)
        {
            if (!e || e.Stats.IsDead) continue;
            var target = Hero;
            var taunter = HeroController.Active ? HeroController.Active.TauntedBy(e) : null;
            if (taunter) target = taunter;
            else if (Downed)
            {
                var close = others.Where(p => Vector3.Distance(p.Position, e.Position) < 30f).OrderBy(p => Vector3.Distance(p.Position, e.Position)).FirstOrDefault();
                if (close) target = close;
            }
            if (e.TargetPlayer != target) e.SetTarget(target);
            e.InCombat = true;
        }

        if (alive.Count == 0) return;
        foreach (var p in others)
        {
            if (p.Stats.IsDead || p.TrainingHealing || !(p.TrainingMelee || p.TrainingRanged || p.TrainingMagic)) continue;
            if (p.taskTarget is EnemyController current && current && !current.Stats.IsDead && alive.ContainsKey(current)) continue;
            var nearest = alive.Keys.Where(e => e && !e.Stats.IsDead).OrderBy(e => Vector3.Distance(e.Position, p.Position)).FirstOrDefault();
            if (!nearest || Vector3.Distance(nearest.Position, p.Position) > 45f) continue;
            p.Chunk = chunk;
            p.taskTarget = nearest;
            nearest.AddAttacker(p);
        }
    }

    /// <summary>The viewers on Ravebridge besides the hero.</summary>
    private IEnumerable<PlayerController> Others()
    {
        var game = GameManager.Instance;
        if (!game || game.Players == null) yield break;
        foreach (var p in game.Players.GetAllPlayers())
            if (p && p != Hero && p.InRavebridge && !p.isDestroyed) yield return p;
    }

    // ---- The hero's side ----------------------------------------------------------------------------------

    public bool Owns(EnemyController enemy) => enemy && alive.ContainsKey(enemy);

    /// <summary>A wave enemy's blow on the hero: a share of the hero's health, once a second at most per enemy.</summary>
    public int BlowFor(EnemyController enemy)
    {
        if (!alive.TryGetValue(enemy, out var kind)) return 0;
        if (lastBlow.TryGetValue(enemy, out var last) && Time.time - last < BlowCooldown) return 0;
        lastBlow[enemy] = Time.time;
        var share = kind.Blow * (1f + Wave * 0.03f) * UnityEngine.Random.Range(0.7f, 1.2f);
        return Mathf.Max(1, Mathf.RoundToInt(Hero.Stats.Health.MaxLevel * share));
    }

    /// <summary>The hero falls: chat has a while to get the hero back up.</summary>
    public void Down()
    {
        if (Downed || State == Phase.Over) return;
        Downed = true;
        votes.Clear();
        healerTime = 0f;
        // each revive makes the next one harder: more votes, less time
        var viewers = Others().Count();
        VotesNeeded = Mathf.Clamp(Mathf.CeilToInt(viewers * 0.3f) + revives, 1, Mathf.Max(1, Mathf.Min(viewers, 5 + revives)));
        var seconds = Mathf.Max(8f, DownedSeconds - revives * 4f);
        DownedUntil = Time.time + (viewers > 0 ? seconds : 4f);
        if (Hero.Animations) Hero.Animations.Death();
        GameManager.Instance?.RavenBot?.Announce(Localization.MSG_WAVE_DOWN, Hero.Name, VotesNeeded, Mathf.RoundToInt(seconds));
        Changed?.Invoke();
    }

    public enum VoteResult { Counted, Revived, NotDown, NotThere, Already }

    /// <summary>!rb revive from a viewer on the island.</summary>
    public VoteResult Vote(PlayerController viewer)
    {
        if (!Downed) return VoteResult.NotDown;
        if (!viewer || !viewer.InRavebridge || viewer == Hero) return VoteResult.NotThere;
        if (!votes.Add(viewer.Id.ToString())) return VoteResult.Already;
        Changed?.Invoke();
        if (votes.Count < VotesNeeded) return VoteResult.Counted;
        Revive();
        return VoteResult.Revived;
    }

    private void UpdateDowned()
    {
        // a healer standing close brings the hero back by themselves
        var healer = Others().FirstOrDefault(p => p.TrainingHealing && !p.Stats.IsDead && Vector3.Distance(p.Position, Hero.Position) < 12f);
        if (healer) lastHealer = healer;
        healerTime = healer ? healerTime + Time.deltaTime : Mathf.Max(0f, healerTime - Time.deltaTime);
        if (healerTime >= HealerReviveSeconds) { Revive(); return; }
        if (Time.time >= DownedUntil) Finish(false, true);
    }

    private void Revive()
    {
        if (!Downed) return;
        Downed = false;
        revives++;
        // the ones who brought the hero back
        var game = GameManager.Instance;
        if (game && game.Players != null)
        {
            foreach (var p in game.Players.GetAllPlayers())
                if (p && votes.Contains(p.Id.ToString())) HeroHelpers.Revive(p);
        }
        if (votes.Count == 0 && lastHealer) HeroHelpers.Revive(lastHealer);
        HeroHelpers.Say(string.Format(Localization.UI_HERO_FEED_REVIVED, Hero.Name));
        Hero.Heal(Hero.Stats.Health.MaxLevel / 2);
        if (Hero.Animations) Hero.Animations.Revive();
        HeroController.Active?.Invulnerable(2.5f);
        HeroEffects.Burst(HeroEffects.Get?.Angel, Hero.Position + Vector3.up, 2f, 2.5f);
        GameManager.Instance?.RavenBot?.Announce(Localization.MSG_WAVE_REVIVED, Hero.Name);
        Changed?.Invoke();
    }

    // ---- The end --------------------------------------------------------------------------------------------

    /// <summary>Ends the run: the hero left (abandoned), fell, or reached the goal.</summary>
    public void Finish(bool abandoned, bool fell = false)
    {
        if (State == Phase.Over) return;
        State = Phase.Over;
        Abandoned = abandoned;
        foreach (var e in alive.Keys.Concat(fallen.Select(f => f.Enemy)).Concat(pool).ToList())
            if (e) Destroy(e.gameObject);
        alive.Clear();
        fallen.Clear();
        pool.Clear();
        if (container) Destroy(container.gameObject);

        if (Hero)
        {
            if (Downed && Hero.Animations) Hero.Animations.Revive();
            Downed = false;
            Hero.Heal(Hero.Stats.Health.MaxLevel);
            if (Cleared > Best)
            {
                Best = Cleared;
                NewBest = true;
                PlayerPrefs.SetInt(BestKey(Hero), Best);
                PlayerPrefs.Save();
            }
            if (!abandoned)
                GameManager.Instance?.RavenBot?.Announce(NewBest ? Localization.MSG_WAVE_OVER_BEST : Localization.MSG_WAVE_OVER, Hero.Name, Cleared, Kills);
        }

        Current = null;
        Last = new Result
        {
            Hero = Hero ? Hero.Name : null, Goal = Goal, Cleared = Cleared, Kills = Kills, Best = Best,
            NewBest = NewBest, Abandoned = abandoned, StartedAt = StartedAt, EndedAt = Time.time,
        };
        try { finished?.Invoke(this); }
        catch (Exception exc) { Shinobytes.Debug.LogError("Wave run end: " + exc); }
        Changed?.Invoke();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
        if (container) Destroy(container.gameObject);
    }
}
