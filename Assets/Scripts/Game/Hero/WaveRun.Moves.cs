using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The waves' own moves (docs/hero-mode-design.md, "After the design review"): seen coming on the ground, so the roll
/// and stepping aside matter. Warriors and skeleton knights lunge along a line, ghosts loose a bolt along one, bosses
/// slam a circle round them; at half health a boss calls for help, at a quarter it is enraged. A stun from the hero
/// breaks a move off before it lands; a boss is stunned half as long and then shrugs stuns off for a while.
/// </summary>
public partial class WaveRun
{
    public enum Move { None, Bolt, Lunge, Slam }

    // a share of the hero's health at wave 1 (growing with the waves as the blows do), and how long each is seen coming
    private const float BoltShare = 0.025f, LungeShare = 0.035f, SlamShare = 0.18f;
    private const float BoltWindup = 0.8f, LungeWindup = 0.7f, SlamWindup = 1.2f, EnragedSlamWindup = 0.9f;
    private const float BoltRange = 18f, BoltSpeed = 11f, BoltWidth = 1.1f;
    private const float LungeDistance = 7f, LungeSeconds = 0.3f, LungeWidth = 1.4f;
    private const float SlamRadius = 4.5f;
    private const float BossStunShare = 0.5f, BossStunRest = 6f;
    private const float EnragedSpeed = 1.35f, EnragedBlows = 1.3f;
    private const float EliteHits = 4f, EliteBlows = 1.6f, EliteScale = 1.25f;
    public const int MostElitesPerWave = 2;

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int LineId = Shader.PropertyToID("_Line");
    private static Material telegraphTemplate;

    private sealed class Telegraph
    {
        public Move Move;
        public EnemyController By;
        public Vector3 From, Direction;
        public float Started, HitAt, Ends;
        public GameObject Decal;
        public Material Material;
        public GameObject Bolt;
        public bool Landed, Struck;
    }

    private readonly List<Telegraph> telegraphs = new List<Telegraph>();
    private readonly Dictionary<EnemyController, float> nextMove = new Dictionary<EnemyController, float>();
    private readonly Dictionary<EnemyController, float> stunRest = new Dictionary<EnemyController, float>();
    private readonly HashSet<EnemyController> elites = new HashSet<EnemyController>();
    private readonly HashSet<EnemyController> hunters = new HashSet<EnemyController>(); // go for viewers (GiveOrders)
    private readonly HashSet<string> elitesBy = new HashSet<string>();
    private int elitesThisWave;
    private bool bossAdds, bossEnraged;
    private float special; // a move's share of the hero's health, for the blow it is about to land (BlowFor)

    /// <summary>
    /// A stun from the hero's skills: as long as asked on most enemies; on a boss half as long, and then not again for a
    /// while (frost nova, snare and leap in turn locked one down most of the time). It breaks off a move being wound up.
    /// </summary>
    public static void Stun(EnemyController e, float seconds)
    {
        if (!e) return;
        var run = Current;
        if (run && run.alive.TryGetValue(e, out var kind) && kind.Boss)
        {
            if (run.stunRest.TryGetValue(e, out var rest) && Time.time < rest) return;
            seconds *= BossStunShare;
            run.stunRest[e] = Time.time + seconds + BossStunRest;
        }
        e.StunnedUntil = Mathf.Max(e.StunnedUntil, Time.time + seconds);
        if (run) run.Interrupt(e);
    }

    /// <summary>Waves past the fifteenth hit harder, 5% a wave on top, so a long run ends in a fight, not a stroll.</summary>
    private float Steep => Wave > 15 ? Mathf.Pow(1.05f, Wave - 15) : 1f;

    /// <summary>What an enemy's blows and moves are worth beyond its kind: an elite's, an enraged boss's.</summary>
    private float Fierce(EnemyController e) => (elites.Contains(e) ? EliteBlows : 1f) * (e == Boss && bossEnraged ? EnragedBlows : 1f);

    // ---- Moves -------------------------------------------------------------------------------------------------

    private void UpdateMoves()
    {
        var now = Time.time;
        if (Hero && !Downed)
        {
            foreach (var pair in alive.ToList())
            {
                var e = pair.Key;
                var kind = pair.Value;
                if (!e || e.Stats.IsDead || kind.Move == Move.None || now < e.StunnedUntil || Winding(e)) continue;
                if (!nextMove.TryGetValue(e, out var at)) { nextMove[e] = now + UnityEngine.Random.Range(2f, 4f); continue; }
                if (now < at || e.TargetPlayer != Hero) continue;
                var distance = Flat(Hero.Position - e.Position).magnitude;
                switch (kind.Move)
                {
                    case Move.Bolt:
                        if (distance < 4f || distance > BoltRange) continue;
                        Begin(Move.Bolt, e, BoltWindup);
                        nextMove[e] = now + UnityEngine.Random.Range(4.5f, 6.5f);
                        break;
                    case Move.Lunge:
                        if (distance < 3f || distance > LungeDistance + 1f) continue;
                        Begin(Move.Lunge, e, LungeWindup);
                        nextMove[e] = now + UnityEngine.Random.Range(6f, 9f);
                        break;
                    case Move.Slam:
                        if (distance > SlamRadius + 1.5f) continue;
                        Begin(Move.Slam, e, e == Boss && bossEnraged ? EnragedSlamWindup : SlamWindup);
                        nextMove[e] = now + (e == Boss && bossEnraged ? UnityEngine.Random.Range(4f, 5f) : UnityEngine.Random.Range(7f, 10f));
                        break;
                }
            }
        }
        UpdateTelegraphs(now);
        UpdateBoss();
    }

    private bool Winding(EnemyController e)
    {
        foreach (var t in telegraphs) if (t.By == e && !t.Landed) return true;
        return false;
    }

    /// <summary>The enemy stops, faces the hero and shows on the ground what is coming.</summary>
    private void Begin(Move move, EnemyController e, float windup)
    {
        var now = Time.time;
        var to = Flat(Hero.Position - e.Position);
        var direction = to.sqrMagnitude > 0.01f ? to.normalized : Flat(e.transform.forward).normalized;
        var movement = e.GetComponent<EnemyMovementController>();
        if (movement) movement.Stop();
        e.transform.rotation = Quaternion.LookRotation(direction);
        // held still while it winds up (and while a lunge carries it)
        e.StunnedUntil = Mathf.Max(e.StunnedUntil, now + windup + (move == Move.Lunge ? LungeSeconds : 0.15f));

        var t = new Telegraph { Move = move, By = e, From = e.Position, Direction = direction, Started = now, HitAt = now + windup };
        switch (move)
        {
            case Move.Bolt:
                t.Decal = Decal(t.From, direction, BoltRange, BoltWidth * 1.6f, true, out t.Material);
                t.Ends = t.HitAt + BoltRange / BoltSpeed;
                break;
            case Move.Lunge:
                t.Decal = Decal(t.From, direction, LungeDistance + 1f, LungeWidth * 2f, true, out t.Material);
                t.Ends = t.HitAt + LungeSeconds;
                break;
            default:
                t.Decal = Decal(t.From, direction, SlamRadius * 2f, SlamRadius * 2f, false, out t.Material);
                t.Ends = t.HitAt + 0.15f;
                break;
        }
        telegraphs.Add(t);
    }

    private void UpdateTelegraphs(float now)
    {
        for (var i = telegraphs.Count - 1; i >= 0; --i)
        {
            var t = telegraphs[i];
            var e = t.By;
            var gone = !e || e.Stats.IsDead || State == Phase.Over;
            if (!t.Landed)
            {
                if (gone) { Drop(i); continue; }
                if (t.Material) t.Material.SetFloat(ProgressId, Mathf.Clamp01((now - t.Started) / Mathf.Max(0.01f, t.HitAt - t.Started)));
                if (now < t.HitAt) continue;
                t.Landed = true;
                Land(t);
            }
            switch (t.Move)
            {
                case Move.Bolt: FlyBolt(t, now); break;
                case Move.Lunge: if (!gone) Carry(t); break;
            }
            if (now >= t.Ends) Drop(i);
        }
    }

    /// <summary>The move goes off: the swing, the bolt loosed, the slam's blow.</summary>
    private void Land(Telegraph t)
    {
        var e = t.By;
        var movement = e.GetComponent<EnemyMovementController>();
        if (movement) movement.Attack(() => { });
        switch (t.Move)
        {
            case Move.Bolt:
                var prefab = HeroEffects.Get ? HeroEffects.Get.Smite : null;
                if (prefab) t.Bolt = Instantiate(prefab, t.From + Vector3.up * 1.2f, Quaternion.LookRotation(t.Direction));
                GameSounds.Play(SoundKind.GoblinAttack, e.Position + Vector3.up, 0.6f, 0f, Hero);
                break;
            case Move.Lunge:
                GameSounds.Play(SoundKind.GoblinAttack, e.Position + Vector3.up, 0.7f, 0f, Hero);
                break;
            case Move.Slam:
                HeroEffects.Burst(HeroEffects.Get?.MeteorBlast, t.From + Vector3.up * 0.2f, 2.4f, 2f);
                GameSounds.Play(SoundKind.RockImpact, t.From, 1f, 0f, Hero);
                var game = GameManager.Instance;
                if (game && game.Camera && game.Camera.HeroView) game.Camera.HeroView.Shake(0.4f, 0.3f);
                if (Hero && Flat(Hero.Position - t.From).magnitude <= SlamRadius) HitHero(e, SlamShare);
                break;
        }
    }

    /// <summary>A bolt flies straight on along its line; the hero standing in its way is hit.</summary>
    private void FlyBolt(Telegraph t, float now)
    {
        var travelled = Mathf.Min(BoltRange, (now - t.HitAt) * BoltSpeed);
        var at = t.From + t.Direction * travelled;
        if (t.Bolt) t.Bolt.transform.position = at + Vector3.up * 1.2f;
        if (t.Struck || !Hero || !t.By) return;
        if (Flat(Hero.Position - at).magnitude > BoltWidth) return;
        t.Struck = true;
        HitHero(t.By, BoltShare);
        if (t.Bolt) Destroy(t.Bolt);
        t.Ends = now;
    }

    /// <summary>A lunge carries the enemy along its line; the hero in its path is hit once.</summary>
    private void Carry(Telegraph t)
    {
        var e = t.By;
        var agent = e.GetComponent<NavMeshAgent>();
        if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.Move(t.Direction * (LungeDistance / LungeSeconds) * Time.deltaTime);
        if (t.Struck || !Hero || Flat(Hero.Position - e.Position).magnitude > LungeWidth) return;
        t.Struck = true;
        HitHero(e, LungeShare);
    }

    /// <summary>A move's blow on the hero, through the hero's own guard, roll and downing (HeroController.AdjustTaken).</summary>
    private void HitHero(EnemyController by, float share)
    {
        if (!Hero || Downed || !by) return;
        special = share;
        try { Hero.TakeDamage(by, 1); }
        finally { special = 0f; }
    }

    /// <summary>A stun broke the enemy's move off before it landed.</summary>
    private void Interrupt(EnemyController e)
    {
        for (var i = telegraphs.Count - 1; i >= 0; --i)
            if (telegraphs[i].By == e && !telegraphs[i].Landed) Drop(i);
    }

    private void Drop(int index)
    {
        var t = telegraphs[index];
        if (t.Decal) Destroy(t.Decal);
        if (t.Material) Destroy(t.Material);
        if (t.Bolt) Destroy(t.Bolt);
        telegraphs.RemoveAt(index);
    }

    private void DropMoves()
    {
        for (var i = telegraphs.Count - 1; i >= 0; --i) Drop(i);
        nextMove.Clear();
        stunRest.Clear();
    }

    /// <summary>
    /// What is coming, on the ground (the Hero Telegraph shader, Resources/HeroTelegraph): a line from the enemy, or a
    /// circle round it, filling up until the blow lands.
    /// </summary>
    private GameObject Decal(Vector3 at, Vector3 direction, float length, float width, bool line, out Material material)
    {
        material = null;
        if (!telegraphTemplate) telegraphTemplate = Resources.Load<Material>("HeroTelegraph");
        if (!telegraphTemplate) return null;
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Telegraph";
        Destroy(go.GetComponent<Collider>());
        if (container) go.transform.SetParent(container, true);
        var renderer = go.GetComponent<MeshRenderer>();
        material = new Material(telegraphTemplate) { hideFlags = HideFlags.DontSave };
        material.SetFloat(LineId, line ? 1f : 0f);
        material.SetFloat(ProgressId, 0f);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var ground = at + Vector3.up * 0.07f;
        if (line)
        {
            // the quad's own up (its uv's v) runs along the line, from the enemy out
            go.transform.SetPositionAndRotation(ground + direction * (length * 0.5f), Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f));
            go.transform.localScale = new Vector3(width, length, 1f);
        }
        else
        {
            go.transform.SetPositionAndRotation(ground, Quaternion.Euler(90f, 0f, 0f));
            go.transform.localScale = new Vector3(length, length, 1f);
        }
        return go;
    }

    // ---- The boss's phases ---------------------------------------------------------------------------------------

    /// <summary>At half health the boss calls for help, at a quarter it is enraged: faster, slamming sooner and harder.</summary>
    private void UpdateBoss()
    {
        var b = Boss;
        if (!b || b.Stats.IsDead) return;
        var health = b.Stats.Health;
        var share = health.MaxLevel > 0 ? health.CurrentValue / (float)health.MaxLevel : 1f;
        if (!bossAdds && share <= 0.5f)
        {
            bossAdds = true;
            var count = 2 + Mathf.Min(4, Fighters() / 3);
            var kinds = Kinds.Where(k => k.From <= Mathf.Max(1, Wave - 2)).ToArray();
            for (var i = 0; i < count; i++)
            {
                var kind = kinds[UnityEngine.Random.Range(0, kinds.Length)];
                var at = Around(b.Position, 5f);
                var add = Make(kind, at);
                if (!add) continue;
                alive[add] = kind;
                HeroEffects.Burst(HeroEffects.Get?.Arrival, at + Vector3.up * 0.8f, 1.3f, 2f);
            }
            HeroHelpers.Say(string.Format(Localization.UI_HERO_FEED_BOSS_ADDS, BossName));
        }
        if (!bossEnraged && share <= 0.25f)
        {
            bossEnraged = true;
            if (walkSpeed.TryGetValue(b, out var walk)) walkSpeed[b] = walk * EnragedSpeed;
            var agent = b.GetComponent<NavMeshAgent>();
            if (agent) agent.speed *= EnragedSpeed;
            HeroEffects.Burst(HeroEffects.Get?.Arrival, b.Position + Vector3.up, 3f, 2f);
            HeroHelpers.Say(string.Format(Localization.UI_HERO_FEED_BOSS_ENRAGED, BossName));
            Changed?.Invoke();
        }
    }

    /// <summary>The boss's phase for the HUD: 0 fighting, 1 called for help, 2 enraged.</summary>
    public int BossPhase => bossEnraged ? 2 : bossAdds ? 1 : 0;

    // ---- Elites (!hero elite) --------------------------------------------------------------------------------

    public enum EliteResult { Sent, NotFighting, Enough, Already }

    /// <summary>Whether a viewer may send an elite now (two a wave, one each).</summary>
    public EliteResult CanSendElite(string viewer)
    {
        if (State != Phase.Fighting || Downed) return EliteResult.NotFighting;
        if (elitesThisWave >= MostElitesPerWave) return EliteResult.Enough;
        if (elitesBy.Contains(viewer)) return EliteResult.Already;
        return EliteResult.Sent;
    }

    /// <summary>
    /// A viewer paid for an elite: the newest kind of the wave, four times as tough, hitting harder, from a gate, and
    /// it drops from the bosses' table.
    /// </summary>
    public EliteResult SendElite(string viewer)
    {
        var can = CanSendElite(viewer);
        if (can != EliteResult.Sent) return can;
        var kind = Kinds.Where(k => k.From <= Wave).OrderBy(k => k.From).Last();
        var at = SpawnPoint();
        var enemy = Make(kind, at, true);
        if (!enemy) return EliteResult.NotFighting;
        alive[enemy] = kind;
        elitesThisWave++;
        elitesBy.Add(viewer);
        HeroEffects.Burst(HeroEffects.Get?.Arrival, at + Vector3.up * 0.8f, 2f, 2f);
        HeroHelpers.Say(string.Format(Localization.UI_HERO_FEED_ELITE, viewer, enemy.name));
        Changed?.Invoke();
        return EliteResult.Sent;
    }

    // ---- Boons (chat votes in the breather) ----------------------------------------------------------------------

    /// <summary>A boon chat can vote the hero between waves; they stack for the rest of the run.</summary>
    public sealed class Boon
    {
        public string Key;
        public Func<string> Name;
    }

    public static readonly Boon[] Boons =
    {
        new Boon { Key = "steel", Name = () => Localization.UI_BOON_STEEL },
        new Boon { Key = "wells", Name = () => Localization.UI_BOON_WELLS },
        new Boon { Key = "hide", Name = () => Localization.UI_BOON_HIDE },
        new Boon { Key = "wind", Name = () => Localization.UI_BOON_WIND },
        new Boon { Key = "glass", Name = () => Localization.UI_BOON_GLASS },
        new Boon { Key = "swift", Name = () => Localization.UI_BOON_SWIFT },
        new Boon { Key = "blood", Name = () => Localization.UI_BOON_BLOOD },
    };

    private readonly List<string> boons = new List<string>();
    private readonly Dictionary<string, int> boonVotes = new Dictionary<string, int>();

    /// <summary>The three boons on offer in this breather; null when there is no vote.</summary>
    public Boon[] Offer { get; private set; }

    /// <summary>The votes for each boon on offer, in its order.</summary>
    public int BoonVotes(int index) => boonVotes.Values.Count(v => v == index);

    private int Has(string key) => boons.Count(b => b == key);

    private static int Stacks(string key) => Current ? Current.Has(key) : 0;

    public static float BoonDamage => 1f + Stacks("steel") * 0.15f + Stacks("glass") * 0.3f;
    public static float BoonTaken => Mathf.Max(0.4f, 1f - Stacks("hide") * 0.12f + Stacks("glass") * 0.2f);
    public static float BoonMana => 1f + Stacks("wells") * 0.4f;
    public static float BoonRoll => Stacks("swift") * 0.6f;
    public static float BoonLeech => Stacks("blood") * 3f;
    public static int BoonPotions => Stacks("wind");

    /// <summary>A wave held: three boons to vote on in the breather (!hero 1, 2 or 3); chat hears what they are.</summary>
    private void OfferBoons()
    {
        boonVotes.Clear();
        Offer = Boons.OrderBy(_ => Roll()).Take(3).ToArray();
        GameManager.Instance?.RavenBot?.Announce(Localization.MSG_HERO_BOONS, Cleared, Hero ? Hero.Name : "?", Offer[0].Name(), Offer[1].Name(), Offer[2].Name());
        Changed?.Invoke();
    }

    /// <summary>!hero 1, 2 or 3 from a viewer on the island, during the breather; the last vote counts.</summary>
    public bool VoteBoon(PlayerController viewer, int choice)
    {
        if (Offer == null || State != Phase.Breather || !viewer || !viewer.InRavebridge || viewer == Hero) return false;
        if (choice < 1 || choice > Offer.Length) return false;
        boonVotes[viewer.Id.ToString()] = choice - 1;
        Changed?.Invoke();
        return true;
    }

    /// <summary>The next wave starts: the boon with the most votes (a draw, or none, picks one of the best at random).</summary>
    private void ChooseBoon()
    {
        if (Offer == null) return;
        var counts = Enumerable.Range(0, Offer.Length).Select(BoonVotes).ToArray();
        var most = counts.Max();
        var best = Enumerable.Range(0, Offer.Length).Where(i => counts[i] == most).ToList();
        var boon = Offer[best[UnityEngine.Random.Range(0, best.Count)]];
        Offer = null;
        boonVotes.Clear();
        boons.Add(boon.Key);
        if (boon.Key == "wind" && HeroController.Active) HeroController.Active.RefillPotions();
        HeroHelpers.Say(string.Format(most > 0 ? Localization.UI_HERO_FEED_BOON : Localization.UI_HERO_FEED_BOON_RANDOM, boon.Name()));
    }

    /// <summary>The boons taken so far this run, for the HUD.</summary>
    public IEnumerable<string> BoonNames => boons.Select(k => Boons.First(b => b.Key == k).Name());

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
}
