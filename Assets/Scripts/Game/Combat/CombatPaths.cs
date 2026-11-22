using System.Collections.Generic;
using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     The capstone paths in the game's fights (docs/capstone-paths-design.md): a combat tree ends in three capstones of
///     which a character learns one, and most of the eighteen new ones need a small rule of their own. The rules that
///     a single blow settles are in the shared library's CombatMath.Attack (Clean Kill, Blood Rage, Thin the Herd's
///     damage, poison on a crit); the ones that remember something between blows are its PathRules, whose memory
///     (a <see cref="PathState"/> a fighter) is kept here. This class carries out what they set off where the fight
///     runs: Aftershock's burst and Volley's second arrow, Shield of the Many's share of an ally's blow, Holy Fire's
///     strike, Guardian Angel's save, Wildfire's jumping burn, Crimson Pact's shield and Thin the Herd's rush.
///     What is drawn (a ring, a bolt) reuses the battle pets' little effects and is skipped with weapon effects off,
///     graphics off and in potato mode, like the other skill effects. Only with <see cref="CombatRules.Enabled"/>.
/// </summary>
public static class CombatPaths
{
    private static readonly Dictionary<PlayerController, PathState> states = new Dictionary<PlayerController, PathState>();
    private static readonly List<PlayerController> guardians = new List<PlayerController>();
    private static readonly List<PlayerController> angels = new List<PlayerController>();
    private static float nextScan;
    private static bool guarding;

    /// <summary>How often each rule has fired this session, every character together (to see the paths at work, as CombatSkillRunner.UsedCounts).</summary>
    public static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();

    private static void Count(string what) => Counts[what] = Counts.TryGetValue(what, out var n) ? n + 1 : 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Hook()
    {
        states.Clear();
        guardians.Clear();
        angels.Clear();
        Counts.Clear();
        nextScan = 0f;
        guarding = false;
        CombatRules.PathOf = StateOf;
        CombatRules.PathBlowLanded = OnBlow;
    }

    // the stats whose rules remember something: without one of them a fighter needs no memory
    private static readonly GearStat[] Stateful = { GearStat.FirstStrike, GearStat.ComboSpeed, GearStat.Aftershock, GearStat.Volley, GearStat.BlockRetort };

    /// <summary>A player's path memory, or null for a fighter whose build has nothing to remember (most of them).</summary>
    public static PathState StateOf(IAttackable fighter)
    {
        if (!(fighter is PlayerController player) || !player) return null;
        if (states.TryGetValue(player, out var state)) return state;
        var eq = player.EquipmentStats;
        if (eq == null || eq.Extras == null) return null;
        var any = false;
        foreach (var stat in Stateful) if (eq.Gear(stat) > 0) { any = true; break; }
        if (!any) return null;
        return Remember(player);
    }

    private static PathState Remember(PlayerController player)
    {
        if (states.Count > 256) Forget(); // players that left
        return states[player] = new PathState();
    }

    private static void Forget()
    {
        var gone = new List<PlayerController>();
        foreach (var p in states.Keys) if (!p || p.isDestroyed || p.Removed) gone.Add(p);
        foreach (var p in gone) states.Remove(p);
    }

    private static double Gear(PlayerController player, GearStat stat) => player && player.EquipmentStats != null ? player.EquipmentStats.Gear(stat) : 0;

    private static bool Alive(PlayerController p) => p && !p.isDestroyed && !p.Removed && p.Stats != null && !p.Stats.IsDead;

    private static bool Alive(EnemyController e) => e && e.Stats != null && !e.Stats.IsDead;

    // ---- what a blow set off --------------------------------------------------------------------------------------

    /// <summary>A blow with something to carry out (CombatRules): the first hit's word, Aftershock's burst, Volley's second arrow.</summary>
    private static void OnBlow(IAttackable attacker, IAttackable defender, AttackStyle style, AttackResult result, PathBlow blow)
    {
        if (!(attacker is PlayerController player) || !player) return;
        if (blow.First)
        {
            Count("first-blood");
            Word(defender, Localization.UI_FLOAT_FIRST_BLOOD, new Color(1f, 0.35f, 0.3f));
        }
        if (blow.Retorted)
        {
            Count("retribution");
            Word(defender, Localization.UI_FLOAT_RETRIBUTION, new Color(1f, 0.8f, 0.35f));
        }

        if (blow.Splash > 0)
        {
            // Aftershock: every other enemy around the attacker
            Count("aftershock");
            var reach = (float)PathRules.AftershockRadius;
            var struck = 0;
            foreach (var e in Near(player.Position, reach, player, defender as EnemyController))
            {
                CombatEffects.DealAs(player, e, blow.Splash);
                player.SessionStats.AddDamageDealt(blow.Splash);
                struck++;
            }
            if (struck > 0) Count("aftershock-struck");
            Ring(player.Position, reach, new Color(0.95f, 0.7f, 0.35f));
        }

        if (blow.Second > 0 && defender is EnemyController shot && shot)
        {
            // Volley: one more enemy near the target
            Count("volley");
            EnemyController second = null;
            var best = float.MaxValue;
            foreach (var e in Near(shot.Position, (float)PathRules.VolleyRadius, player, shot))
            {
                var d = (e.Position - shot.Position).sqrMagnitude;
                if (d < best) { best = d; second = e; }
            }
            if (second)
            {
                Count("volley-struck");
                Bolt(player.Position + Vector3.up * 1.2f, second.transform, new Color(0.75f, 1f, 0.5f));
                CombatEffects.DealAs(player, second, blow.Second);
                player.SessionStats.AddDamageDealt(blow.Second);
            }
        }
    }

    private static readonly List<EnemyController> near = new List<EnemyController>();

    private static List<EnemyController> Near(Vector3 center, float radius, PlayerController by, EnemyController not)
    {
        near.Clear();
        foreach (var e in EnemyController.Active)
        {
            if (!Alive(e) || e == not || e.Island != by.Island) continue;
            if ((e.Position - center).sqrMagnitude <= radius * radius) near.Add(e);
        }
        return near;
    }

    // ---- an enemy fell --------------------------------------------------------------------------------------------

    /// <summary>
    ///     An enemy fell to <paramref name="killer"/> (PlayerCombat.Defeated): it is a new target when it comes back,
    ///     Thin the Herd's rush starts for those who fought it, and Wildfire's burn moves on.
    /// </summary>
    public static void Defeated(PlayerController killer, IAttackable enemy)
    {
        if (!CombatRules.Enabled || enemy == null) return;
        foreach (var attacker in enemy.GetAttackers())
        {
            if (!(attacker is PlayerController p) || !p) continue;
            if (states.TryGetValue(p, out var state)) state.Forget(enemy);
            var rush = Gear(p, GearStat.KillRush);
            if (rush > 0)
            {
                Count("kill-rush");
                CombatEffects.AddModifier(p, CombatEffects.ModifierKind.DamageDealt, rush, (float)PathRules.RushSeconds);
            }
        }
        if (killer && states.TryGetValue(killer, out var own)) own.Forget(enemy);

        // Wildfire: the burn of the one who lit it jumps to an enemy near
        if (!(enemy is EnemyController dead) || !dead) return;
        var burn = CombatEffects.TakeBurn(dead, out var source);
        if (burn == null || !(source is PlayerController pyro) || !pyro) return;
        var spread = Gear(pyro, GearStat.BurnSpread);
        if (spread <= 0) return;
        EnemyController next = null;
        var best = float.MaxValue;
        foreach (var e in Near(dead.Position, (float)PathRules.BurnJumpRadius, pyro, dead))
        {
            var d = (e.Position - dead.Position).sqrMagnitude;
            if (d < best) { best = d; next = e; }
        }
        if (!next) return;
        Count("wildfire");
        var perSecond = PathRules.BurnJump(burn.DamagePerSecond, spread);
        CombatEffects.AddDamageOverTime(pyro, next, Element.Fire, perSecond * ElementRules.BurnSeconds, (float)ElementRules.BurnSeconds);
        Bolt(dead.Position + Vector3.up * 0.8f, next.transform, new Color(1f, 0.5f, 0.15f));
        Word(next, Localization.UI_FLOAT_BURNING, new Color(1f, 0.55f, 0.2f));
    }

    // ---- life steal, heals ----------------------------------------------------------------------------------------

    /// <summary>Life steal heals the attacker; with Crimson Pact what does not fit into its health becomes a shield.</summary>
    public static void LifeSteal(IAttackable attacker, int healed)
    {
        if (attacker == null || healed <= 0) return;
        if (attacker is PlayerController player && player && player.Stats != null)
        {
            var share = Gear(player, GearStat.LifeStealShield);
            if (share > 0)
            {
                var health = player.Stats.Health;
                var now = Time.time <= player.CombatShieldUntil ? player.CombatShield : 0;
                var after = PathRules.OverflowShield(healed, health.MaxValue - health.CurrentValue, now, health.MaxValue, share);
                if (after > now)
                {
                    Count("crimson-pact");
                    player.AddShield(after, (float)PathRules.OverflowShieldSeconds);
                }
            }
        }
        attacker.Heal(healed);
    }

    /// <summary>Holy Fire: a heal cast by <paramref name="healer"/> on <paramref name="healed"/> also strikes an enemy.</summary>
    public static void HealStruck(PlayerController healer, IAttackable healed, int amount)
    {
        if (!CombatRules.Enabled || amount <= 0 || !healer || healed == null) return;
        if (Gear(healer, GearStat.HealStrike) <= 0) return;
        // the enemy fighting the one healed, otherwise the nearest one to them
        var at = healed.Position;
        var patient = healed as PlayerController;
        EnemyController target = null;
        var best = float.MaxValue;
        foreach (var e in Near(at, (float)PathRules.AllyRadius, healer, null))
        {
            var d = (e.Position - at).sqrMagnitude - (patient && e.TargetPlayer == patient ? 10000f : 0f);
            if (d < best) { best = d; target = e; }
        }
        if (!target) return;
        var damage = PathRules.HealStrike(amount, CombatRules.Sheet(healer), CombatRules.Sheet(target));
        if (damage <= 0) return;
        Count("holy-fire");
        Bolt(healer.Position + Vector3.up * 1.4f, target.transform, new Color(1f, 0.92f, 0.55f));
        healer.SessionStats.AddDamageDealt(damage);
        CombatEffects.DealAs(healer, target, damage);
    }

    // ---- a blow on a player: a guardian's share, an angel's save -----------------------------------------------------

    // who has Shield of the Many and Guardian Angel: looked up every two seconds, not at every blow
    private static void Scan()
    {
        if (Time.time < nextScan) return;
        nextScan = Time.time + 2f;
        guardians.Clear();
        angels.Clear();
        var game = GameManager.Instance;
        if (!game || game.Players == null) return;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!p || p.EquipmentStats == null || p.EquipmentStats.Extras == null) continue;
            if (p.EquipmentStats.Extras[GearStat.AllyGuard] > 0) guardians.Add(p);
            if (p.EquipmentStats.Extras[GearStat.GuardianAngel] > 0) angels.Add(p);
        }
    }

    private static bool Close(PlayerController a, PlayerController b)
    {
        var reach = (float)PathRules.AllyRadius;
        return a.Island == b.Island && (a.Position - b.Position).sqrMagnitude <= reach * reach;
    }

    /// <summary>
    ///     Shield of the Many: of an enemy's blow on <paramref name="victim"/>, a guardian near takes its share in the
    ///     victim's place (after the guardian's own armor), while the guardian has more than 30% of its health.
    ///     Returns what is left for the victim.
    /// </summary>
    public static int Guard(PlayerController victim, IAttackable attacker, int damage)
    {
        if (guarding || damage <= 1 || !CombatRules.Enabled || !victim) return damage;
        Scan();
        if (guardians.Count == 0) return damage;
        foreach (var guardian in guardians)
        {
            if (guardian == victim || !Alive(guardian) || !Close(guardian, victim)) continue;
            var health = guardian.Stats.Health;
            if (health.MaxValue <= 0 || health.CurrentValue <= health.MaxValue * PathRules.GuardFloor) continue;
            var shared = PathRules.GuardShare(damage, Gear(guardian, GearStat.AllyGuard));
            if (shared <= 0) continue;
            Count("shield-of-the-many");
            guarding = true; // the guardian's share is nobody else's to share
            try { guardian.TakeDamage(attacker, PathRules.GuardTaken(shared, CombatRules.Sheet(guardian))); }
            finally { guarding = false; }
            return damage - shared;
        }
        return damage;
    }

    /// <summary>
    ///     Guardian Angel: <paramref name="victim"/> would die. A healer near whose angel is ready (once a minute) saves
    ///     them: the health they are healed for (the blow leaves them at 1), or 0 when nobody does.
    /// </summary>
    public static int Angel(PlayerController victim)
    {
        if (!CombatRules.Enabled || !victim || victim.Stats == null) return 0;
        Scan();
        if (angels.Count == 0) return 0;
        foreach (var healer in angels)
        {
            if (healer == victim || !Alive(healer) || !Close(healer, victim)) continue;
            var share = Gear(healer, GearStat.GuardianAngel);
            if (share <= 0) continue;
            if (!states.TryGetValue(healer, out var state)) state = Remember(healer);
            if (!PathRules.UseAngel(state, Time.time)) continue;
            Count("guardian-angel");
            Ring(victim.Position, 1.6f, new Color(1f, 0.95f, 0.7f), 0.9f);
            Word(victim, Localization.UI_FLOAT_GUARDIAN_ANGEL, new Color(1f, 0.95f, 0.7f));
            return PathRules.AngelHeal(victim.Stats.Health.MaxValue, share);
        }
        return 0;
    }

    // ---- what is seen: cheap, and off with weapon effects, graphics or in potato mode ----------------------------------

    private static bool Shown
    {
        get
        {
            var game = GameManager.Instance;
            return SettingsApplier.WeaponEffects && GraphicsToggler.GraphicsEnabled && !(game && game.PotatoMode);
        }
    }

    private static Material material;
    private static Shinobytes.UI.FloatingTextView floating;

    private static Material Material
    {
        get
        {
            if (material) return material;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = "Capstone path fx" };
            return material;
        }
    }

    /// <summary>A ring that grows and fades on the ground (the battle pets' ring).</summary>
    private static void Ring(Vector3 center, float radius, Color color, float seconds = 0.6f)
    {
        if (!Shown) return;
        var go = new GameObject("Path ring");
        go.transform.position = center + Vector3.up * 0.1f;
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = Material;
        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = 40;
        line.widthMultiplier = 0.12f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<PetRingFader>().Begin(line, radius, color, seconds);
    }

    /// <summary>A small bolt with a trail from a point to a fighter (the battle pets' bolt): only for the eye, the damage is dealt at once.</summary>
    private static void Bolt(Vector3 from, Transform target, Color color)
    {
        if (!Shown || !target) return;
        var go = new GameObject("Path bolt");
        go.transform.position = from;
        var trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = Material;
        trail.time = 0.25f;
        trail.startWidth = 0.22f;
        trail.endWidth = 0f;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<PetBoltMover>().Launch(target, Vector3.up * 0.8f, null);
    }

    private static void Word(IAttackable fighter, string word, Color color)
    {
        if (!SettingsApplier.EffectWords || !GraphicsToggler.GraphicsEnabled) return;
        if (!(fighter is Component c) || !c) return;
        if (!floating) floating = Object.FindAnyObjectByType<Shinobytes.UI.FloatingTextView>();
        if (floating) floating.AddEffect(c.transform, word, color);
    }
}
