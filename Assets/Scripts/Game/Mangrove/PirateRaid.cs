using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The pirates' raid on the Mangrove island (docs/banana-island.md): when the banana pile is big the pirates come for
/// it (or the broadcaster calls them with !pirates). Their ship anchors off the south beach and three waves row ashore
/// and go for the pile in the stilt village. Everyone on the island who trains combat fights them; the streamer can
/// take their own character by hand and defend in hero mode (click the bar; Esc gives it back). A pirate that reaches
/// the pile runs off with bananas. Held: the defenders share the pirates' chest (the server). Lost (no defenders left
/// standing, or too many got through): the pirates carry half the pile off.
/// </summary>
public sealed class PirateRaid : MonoBehaviour
{
    public const int Waves = 3;
    public const float WarningSeconds = 30f;
    public const float BreatherSeconds = 12f;
    public const float GiveUpAfter = 420f;
    public const float Cooldown = 1800f;
    /// <summary>The pile it takes for the pirates to come by themselves, and how often they think about it.</summary>
    public const float TemptingPile = 650f;
    private const float ThinkEvery = 300f;
    private const float ComeChance = 0.2f;
    private const int MostAlive = 14;
    /// <summary>Bananas a pirate runs off with when it reaches the pile; this many getting through loses the raid.</summary>
    private const float StolenEach = 25f;
    private const int ThroughToLose = 8;
    private const float PileReach = 3.5f;
    private const float LoseShare = 0.5f;

    public enum Phase { Coming, Fighting, Breather, Done }

    private static PirateRaid current;
    private static float nextThought;
    private static float nextAllowed;

    private IslandController island;
    private EnemyController template;
    private Chunk chunk;
    private Transform container;
    private readonly List<EnemyController> alive = new List<EnemyController>();
    private readonly List<EnemyController> pool = new List<EnemyController>();
    private readonly HashSet<PlayerController> defenders = new HashSet<PlayerController>();
    private Vector3 landing;
    private Vector3 pile;
    private GameObject ship;
    private Phase phase;
    private float phaseEnds;
    private float startedAt;
    private int wave;
    private int toSpawn;
    private float nextSpawn;
    private float nextOrders;
    private int through;
    private int kills;
    private PlayerController hero;
    private TaskType heroTaskBefore;
    private string heroArgumentBefore;

    public static bool Running => current && current.phase != Phase.Done;

    /// <summary>Hero mode may go on here: the streamer defending the island by hand (HeroController).</summary>
    public static bool Defending(PlayerController p) => Running && p && current.hero == p && p.Island == current.island;

    // ---- Coming ------------------------------------------------------------------------------------

    /// <summary>Now and then, with a tempting pile and people about, the pirates come by themselves (MonkeyTroop ticks this).</summary>
    public static void Think(IslandController island)
    {
        if (Time.time < nextThought) return;
        nextThought = Time.time + ThinkEvery;
        if (Running || Time.time < nextAllowed || BananaPile.Amount < TemptingPile) return;
        if (island.GetPlayers().Count(p => p && !p.IsBot) < 3) return;
        if (UnityEngine.Random.value < ComeChance) Begin(island, false);
    }

    public enum StartResult { Started, NotOpen, Running, Busy, NoPirates }

    /// <summary>!pirates (the broadcaster or a moderator), or the pirates' own idea.</summary>
    public static StartResult Begin(IslandController island = null, bool called = true)
    {
        var game = GameManager.Instance;
        island = island ? island : game ? game.Islands.Get(IslandController.MangroveIsland) : null;
        if (!island || island.Locked) return StartResult.NotOpen;
        if (Running) return StartResult.Running;
        if (TreasureHunt.Running || Regatta.Running) return StartResult.Busy;
        var template = island.GetComponentsInChildren<EnemyController>(true).FirstOrDefault(e => e && !e.name.StartsWith("Raid Pirate"));
        if (!template) return StartResult.NoPirates;

        var go = new GameObject("Pirate Raid");
        go.transform.SetParent(island.transform, false);
        current = go.AddComponent<PirateRaid>();
        current.island = island;
        current.template = template;
        current.chunk = template.GetComponentInParent<Chunk>();
        current.container = go.transform;
        current.Setup();
        return StartResult.Started;
    }

    private void Setup()
    {
        startedAt = Time.time;
        phase = Phase.Coming;
        phaseEnds = Time.time + WarningSeconds;
        // the pile: by the chief in the stilt village; the landing: the pirates' own beach on the south side
        var chief = IslandNpc.Find(MonkeyTroop.ChiefKey);
        pile = chief ? chief.transform.position : island.SpawnPosition;
        landing = template.transform.position;
        if (NavMesh.SamplePosition(landing, out var hit, 6f, NavMeshHelper.AreaMaskAt(landing))) landing = hit.position;
        MakeShip();

        MangroveFeed.Say(Localization.UI_PIRATES_COMING, "");
        var game = GameManager.Instance;
        game.RavenBot?.Announce(Localization.MSG_PIRATES_COMING);
        var banner = FindAnyObjectByType<Shinobytes.UI.NotificationScreenView>(FindObjectsInactive.Include);
        if (banner)
        {
            banner.Show(Localization.UI_PIRATES_TITLE, Localization.UI_PIRATES_COMING, Shinobytes.UI.NotificationScreenView.WithCommand(Localization.UI_PIRATES_JOIN, "!sail mangrove"));
            Invoke(nameof(HideBanner), 8f);
        }
    }

    private void HideBanner()
    {
        var banner = FindAnyObjectByType<Shinobytes.UI.NotificationScreenView>(FindObjectsInactive.Include);
        if (banner) banner.Hide();
    }

    /// <summary>The pirates' ship sails in and anchors off their beach.</summary>
    private void MakeShip()
    {
        var prefab = Resources.Load<GameObject>("Mangrove/Pirate Ship");
        if (!prefab) return;
        var away = landing - pile;
        away.y = 0f;
        away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.back;
        var anchor = landing + away * 45f;
        for (var r = 45f; r < 140f && !SeaChart.IsWater(anchor); r += 10f) anchor = landing + away * r;
        anchor.y = SeaChart.Sea;
        ship = Instantiate(prefab, anchor + away * 120f, Quaternion.LookRotation(-away), container);
        ship.AddComponent<SailIn>().Target = anchor;
    }

    private sealed class SailIn : MonoBehaviour
    {
        public Vector3 Target;
        private void Update()
        {
            var t = Mathf.Clamp01(Time.deltaTime * 0.35f);
            transform.position = Vector3.Lerp(transform.position, Target, t) + Vector3.up * (Mathf.Sin(Time.time * 0.8f) * 0.004f);
        }
    }

    // ---- The fight ---------------------------------------------------------------------------------

    private void Update()
    {
        if (phase == Phase.Done) return;
        alive.RemoveAll(e => !e);
        if (hero && (Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping || !hero.Controlled || hero.Island != island)) StopDefending();

        switch (phase)
        {
            case Phase.Coming:
            case Phase.Breather:
                if (Time.time >= phaseEnds) StartWave();
                break;
            case Phase.Fighting:
                SpawnSome();
                CountFallen();
                if (toSpawn == 0 && alive.Count == 0) WaveHeld();
                break;
        }

        if (Time.time >= nextOrders)
        {
            nextOrders = Time.time + 0.5f;
            GiveOrders();
        }
        if (phase != Phase.Done && (through >= ThroughToLose || Time.time - startedAt > GiveUpAfter)) End(false);
    }

    private void StartWave()
    {
        wave++;
        phase = Phase.Fighting;
        var defending = Defenders().Count();
        toSpawn = Mathf.Clamp(3 + wave * 2 + defending / 2, 4, 30);
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_WAVE, wave, Waves, toSpawn), "");
    }

    private void SpawnSome()
    {
        if (toSpawn <= 0 || alive.Count >= MostAlive || Time.time < nextSpawn) return;
        nextSpawn = Time.time + 0.8f;
        var captain = wave == Waves && toSpawn == 1;
        var offset = UnityEngine.Random.insideUnitCircle * 4f;
        var at = landing + new Vector3(offset.x, 0f, offset.y);
        if (NavMesh.SamplePosition(at, out var hit, 4f, NavMeshHelper.AreaMaskAt(landing))) at = hit.position;
        var enemy = Make(at, captain);
        if (enemy) { alive.Add(enemy); toSpawn--; }
    }

    /// <summary>A pirate sized to the defenders: about their average combat level, a captain much tougher.</summary>
    private EnemyController Make(Vector3 at, bool captain)
    {
        EnemyController enemy;
        if (pool.Count > 0) { enemy = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); enemy.gameObject.SetActive(true); }
        else
        {
            enemy = Instantiate(template, container);
            enemy.Stats = template.Stats * 1f;
            enemy.EquipmentStats = template.EquipmentStats;
        }
        if (!enemy) return null;
        enemy.name = captain ? "Raid Pirate Captain" : "Raid Pirate";
        var level = Mathf.Clamp(Mathf.RoundToInt(AverageCombatLevel() * (captain ? 1.1f : 0.75f)), 5, 900);
        enemy.Stats.Attack = new SkillStat(level);
        enemy.Stats.Strength = new SkillStat(level);
        enemy.Stats.Defense = new SkillStat(level);
        enemy.Stats.Health = new SkillStat(Mathf.Max(20, level * (captain ? 40 : 8) * (1f + wave * 0.15f)));
        enemy.transform.localScale = Vector3.one * (captain ? 1.35f : 1f);
        enemy.AutomaticRespawn = false;
        enemy.Hunting = true;
        enemy.AggroRange = 30f;
        enemy.ExpFactor = (captain ? 6d : 1.5d) * (1d + wave * 0.15d);
        enemy.SpawnAt(at);
        enemy.InCombat = true;
        enemy.SetDestination(pile);
        return enemy;
    }

    private float AverageCombatLevel()
    {
        var list = Defenders().ToList();
        if (list.Count == 0) return 20f;
        return (float)list.Average(p => (p.Stats.Attack.Level + p.Stats.Strength.Level + p.Stats.Defense.Level + p.Stats.Health.Level) / 4f);
    }

    private IEnumerable<PlayerController> Defenders() =>
        island.GetPlayers().Where(p => p && !p.Stats.IsDead && (p == hero || p.TrainingMelee || p.TrainingRanged || p.TrainingMagic || p.TrainingHealing));

    private void CountFallen()
    {
        for (var i = alive.Count - 1; i >= 0; --i)
        {
            var e = alive[i];
            if (!e.Stats.IsDead) continue;
            alive.RemoveAt(i);
            kills++;
            foreach (var a in e.GetAttackers()) if (a is PlayerController p && p) defenders.Add(p);
            StartCoroutine(PoolLater(e, 4f));
        }
    }

    private System.Collections.IEnumerator PoolLater(EnemyController e, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!e) yield break;
        e.gameObject.SetActive(false);
        pool.Add(e);
    }

    /// <summary>
    /// Twice a second: each pirate goes for the hero or a defender near it, otherwise for the pile, where it grabs
    /// bananas and runs; defenders training combat go for the nearest pirate.
    /// </summary>
    private void GiveOrders()
    {
        var defending = Defenders().ToList();
        foreach (var e in alive.ToList())
        {
            if (!e || e.Stats.IsDead) continue;
            if ((e.Position - pile).sqrMagnitude < PileReach * PileReach)
            {
                Steal(e);
                continue;
            }
            PlayerController target = null;
            if (hero && !hero.Stats.IsDead && Vector3.Distance(hero.Position, e.Position) < 25f) target = hero;
            else target = defending.Where(p => Vector3.Distance(p.Position, e.Position) < 14f).OrderBy(p => Vector3.Distance(p.Position, e.Position)).FirstOrDefault();
            if (target)
            {
                if (e.TargetPlayer != target) e.SetTarget(target);
                e.InCombat = true;
            }
            else
            {
                if (e.TargetPlayer) e.ClearTarget();
                e.SetDestination(pile);
            }
        }

        if (alive.Count == 0) return;
        foreach (var p in defending)
        {
            if (p == hero || p.Controlled || p.TrainingHealing) continue;
            if (p.taskTarget is EnemyController now && now && !now.Stats.IsDead && alive.Contains(now)) continue;
            var nearest = alive.Where(e => e && !e.Stats.IsDead).OrderBy(e => Vector3.Distance(e.Position, p.Position)).FirstOrDefault();
            if (!nearest || Vector3.Distance(nearest.Position, p.Position) > 70f) continue;
            p.Chunk = chunk;
            p.taskTarget = nearest;
            nearest.AddAttacker(p);
        }
    }

    private void Steal(EnemyController e)
    {
        through++;
        var stolen = Mathf.Min(StolenEach, BananaPile.Amount);
        BananaPile.Add(-stolen);
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_STOLE, Mathf.RoundToInt(stolen), through, ThroughToLose), "");
        alive.Remove(e);
        e.gameObject.SetActive(false);
        pool.Add(e);
    }

    private void WaveHeld()
    {
        if (wave >= Waves) { End(true); return; }
        phase = Phase.Breather;
        phaseEnds = Time.time + BreatherSeconds;
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_WAVE_HELD, wave, Waves), "");
    }

    private void End(bool held)
    {
        phase = Phase.Done;
        current = null;
        nextAllowed = Time.time + Cooldown;
        StopDefending();
        foreach (var e in alive) if (e) e.gameObject.SetActive(false);
        alive.Clear();
        var game = GameManager.Instance;
        if (held)
        {
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_HELD, kills), "");
            game.RavenBot?.Announce(Localization.MSG_PIRATES_HELD, kills);
            foreach (var p in island.GetPlayers()) if (p && !p.Stats.IsDead) p.Cheer();
            // the one who fought longest (the hero, or whoever helped most) first: they roll twice
            var paid = defenders.Where(p => p && !p.IsBot).OrderByDescending(p => p == hero).Select(p => p.Id).Distinct().ToArray();
            Pay(paid);
        }
        else
        {
            var lost = BananaPile.Lose(LoseShare);
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_LOST, Mathf.RoundToInt(lost)), "");
            game.RavenBot?.Announce(Localization.MSG_PIRATES_LOST, Mathf.RoundToInt(lost));
        }
        Destroy(gameObject, 6f);
    }

    private static async void Pay(Guid[] ids)
    {
        var game = GameManager.Instance;
        if (ids.Length == 0 || !game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        try
        {
            var rewards = await game.RavenNest.Mangrove.ChestAsync(RavenNest.SDK.Endpoints.MangroveApi.Pirates, ids);
            MangroveRewards.Apply(game, rewards, Localization.UI_PIRATES_PAID, Localization.MSG_TREASURE_RARE);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The pirates' chest could not be opened: " + exc.Message);
        }
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
        if (hero) StopDefending();
    }

    // ---- The streamer, by hand ---------------------------------------------------------------------

    /// <summary>The broadcaster's own character on the island, which can defend by hand.</summary>
    private PlayerController StreamersCharacter() =>
        island.GetPlayers().FirstOrDefault(p => p && p.IsBroadcaster && !p.Stats.IsDead);

    /// <summary>The bar was clicked: the streamer takes their character by hand as the hero, or gives it back.</summary>
    public static void BarClicked()
    {
        if (!Running) return;
        if (current.hero) { current.StopDefending(); return; }
        current.Defend();
    }

    public static bool CanDefend => Running && !current.hero && current.StreamersCharacter() && !RavebridgePlay.Playing;

    private void Defend()
    {
        var p = StreamersCharacter();
        var game = GameManager.Instance;
        if (!p || !game || RavebridgePlay.Playing) return;
        heroTaskBefore = p.GetTask();
        heroArgumentBefore = p.GetTaskArgument();
        p.taskTarget = null;
        p.SetDestination(p.Position);
        game.Camera.FollowControlled(p);
        p.EnableControl();
        hero = p;
        if (!HeroController.Begin(p)) { StopDefending(); return; }
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_HERO, p.Name), "");
    }

    private void StopDefending()
    {
        var p = hero;
        hero = null;
        if (!p) return;
        if (HeroController.Active && HeroController.Active.Player == p) HeroController.End();
        p.RevokeControl();
        p.taskTarget = null;
        if (heroTaskBefore != TaskType.None) p.SetTask(heroTaskBefore, heroArgumentBefore, true);
        var game = GameManager.Instance;
        if (game && game.Camera) game.Camera.StopFollowingControlled();
    }

    // ---- The bar -----------------------------------------------------------------------------------

    public static Shinobytes.UI.MangroveView.Status CurrentStatus()
    {
        if (!current) return null;
        var r = current;
        var hint = r.hero ? Localization.UI_PIRATES_BAR_HERO_STOP : CanDefend ? Localization.UI_PIRATES_BAR_HERO : Localization.UI_PIRATES_BAR_HELP;
        string title;
        switch (r.phase)
        {
            case Phase.Coming:
                title = string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_BAR_COMING, Mathf.Max(0, Mathf.CeilToInt(r.phaseEnds - Time.time)));
                break;
            case Phase.Breather:
                title = string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_BAR_BREATHER, r.wave + 1, Mathf.Max(0, Mathf.CeilToInt(r.phaseEnds - Time.time)));
                break;
            case Phase.Fighting:
                title = string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_BAR_WAVE, r.wave, Waves, r.alive.Count + r.toSpawn);
                break;
            default:
                return null;
        }
        return new Shinobytes.UI.MangroveView.Status
        {
            Glyph = "",
            Title = title,
            Line = string.Format(CultureInfo.InvariantCulture, Localization.UI_PIRATES_BAR_LINE, BananaPile.AmountText, r.through, ThroughToLose) + "  " + hint,
            Progress = 1f - (float)r.through / ThroughToLose,
        };
    }
}
