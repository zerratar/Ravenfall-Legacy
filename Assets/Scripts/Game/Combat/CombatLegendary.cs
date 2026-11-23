using System.Collections.Generic;
using RavenNest.Combat;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
///     The Legendaries' powers in the game's fights (docs/legendary-design.md, section 5). The server decides who has
///     a Legendary and its rank (the inventory item's tag); the numbers and rules are the shared library's
///     (RavenNest.Combat.Legendary, LegendaryRules), the same the simulator uses. This carries them out where the fight
///     runs: Soul Harvest's heal on a kill, Dread's fear, Bat Swarm's bites, Iron Aegis's shields (its thorns are in
///     CombatMath) and Thunder Shot's blast. What is drawn reuses the capstone paths' little effects and follows the
///     Weapon effects setting and potato mode; the words follow Effect words. Only with <see cref="CombatRules.Enabled"/>.
/// </summary>
public static class CombatLegendary
{
    private static readonly Dictionary<PlayerController, LegendaryState> states = new Dictionary<PlayerController, LegendaryState>();

    /// <summary>How often each power has fired this session (to see them at work in tests, as CombatPaths.Counts).</summary>
    public static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();

    private static void Count(string what) => Counts[what] = Counts.TryGetValue(what, out var n) ? n + 1 : 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Hook()
    {
        states.Clear();
        Counts.Clear();
        CombatRules.LegendaryBefore = Before;
        CombatRules.LegendaryBlowLanded = OnBlow;
    }

    /// <summary>The rank of a worn Legendary with this power, 0 when none is worn.</summary>
    public static int Rank(PlayerController player, LegendaryPower power)
    {
        var worn = player && player.EquipmentStats != null ? player.EquipmentStats.Legendaries : null;
        if (worn == null) return 0;
        foreach (var w in worn) if (w.Info.Power == power) return w.Rank;
        return 0;
    }

    private static LegendaryState StateOf(PlayerController player)
    {
        if (states.TryGetValue(player, out var s)) return s;
        if (states.Count > 256)
        {
            var gone = new List<PlayerController>();
            foreach (var p in states.Keys) if (!p || p.isDestroyed || p.Removed) gone.Add(p);
            foreach (var p in gone) states.Remove(p);
        }
        return states[player] = new LegendaryState();
    }

    private static bool IsBoss(IAttackable target)
    {
        if (target is EnemyController e && e && (e.IsRaidBoss || e.IsDungeonBoss)) return true;
        var sheet = CombatRules.Sheet(target);
        return sheet != null && sheet.IsBoss;
    }

    // ---- before and after a blow ------------------------------------------------------------------------------------

    /// <summary>Thunder Shot: the third shot that hits is a blast (more damage).</summary>
    private static AttackOptions Before(IAttackable attacker, AttackStyle style, AttackOptions options, bool basic)
    {
        if (!basic || style != AttackStyle.Ranged || !(attacker is PlayerController player) || !player) return options;
        var rank = Rank(player, LegendaryPower.ThunderShot);
        if (rank <= 0 || !LegendaryRules.BlastNext(StateOf(player))) return options;
        var v = Legendary.Values(LegendaryPower.ThunderShot, rank);
        options.Multiplier = (options.Multiplier > 0 ? options.Multiplier : 1) * (1 + v.BlastBonus);
        return options;
    }

    private static void OnBlow(IAttackable attacker, IAttackable defender, AttackStyle style, AttackResult result, bool basic)
    {
        if (!CombatRules.Enabled || result == null || !(attacker is PlayerController player) || !player) return;
        if (player.EquipmentStats?.Legendaries == null || player.EquipmentStats.Legendaries.Count == 0) return;
        var now = Time.time;

        // Iron Aegis: a shield on the wielder and every ally near, every 10 s while it fights
        var aegis = Rank(player, LegendaryPower.IronAegis);
        if (aegis > 0 && LegendaryRules.Aegis(StateOf(player), now)) Aegis(player, aegis);

        if (!result.Hit || !(defender is EnemyController enemy) || !enemy) return;

        // Dread: the enemy freezes in fear (bosses shrug it off)
        var dread = style == AttackStyle.Melee ? Rank(player, LegendaryPower.Dread) : 0;
        if (dread > 0 && enemy.Stats != null && !enemy.Stats.IsDead)
        {
            var v = Legendary.Values(LegendaryPower.Dread, dread);
            if (LegendaryRules.Feared(v, IsBoss(enemy), Random.value) && CombatEffects.CrowdControl(player, enemy, StatusEffectKind.Stun, (float)v.FearSeconds))
            {
                Count("dread");
                CombatPaths.Word(enemy, Localization.UI_FLOAT_FEARED, new Color(1f, 0.45f, 0.25f));
                CombatPaths.Ring(enemy.Position, 1.4f, new Color(1f, 0.35f, 0.15f), 0.5f);
            }
        }

        // Bat Swarm: now and then a spell sends bats at the enemies near the target
        var swarm = style == AttackStyle.Magic ? Rank(player, LegendaryPower.BatSwarm) : 0;
        if (swarm > 0)
        {
            var v = Legendary.Values(LegendaryPower.BatSwarm, swarm);
            var bite = LegendaryRules.Swarm(StateOf(player), v, result.Total, now);
            if (bite > 0)
            {
                Count("bat-swarm");
                var bitten = 0;
                foreach (var e in Nearest(enemy.Position, (float)LegendaryRules.SwarmRadius, player, null, v.SwarmBites))
                {
                    CombatPaths.Bolt(player.Position + Vector3.up * 1.6f, e.transform, new Color(0.7f, 0.42f, 1f));
                    CombatEffects.DealAs(player, e, bite);
                    player.SessionStats.AddDamageDealt(bite);
                    bitten++;
                }
                if (bitten > 0) CombatPaths.Word(player, Localization.UI_FLOAT_BAT_SWARM, new Color(0.75f, 0.5f, 1f));
            }
        }

        // Thunder Shot: the third shot's blast knocks the target back, staggers it and hits enemies around it
        var cannon = basic && style == AttackStyle.Ranged ? Rank(player, LegendaryPower.ThunderShot) : 0;
        if (cannon > 0 && LegendaryRules.ShotHit(StateOf(player)))
        {
            Count("thunder-shot");
            var v = Legendary.Values(LegendaryPower.ThunderShot, cannon);
            var splash = LegendaryRules.BlastSplash(v, result.Total);
            var boss = IsBoss(enemy);
            foreach (var e in Nearest(enemy.Position, (float)LegendaryRules.BlastRadius, player, enemy, LegendaryRules.BlastTargets))
            {
                CombatEffects.DealAs(player, e, splash);
                player.SessionStats.AddDamageDealt(splash);
            }
            if (!boss && enemy.Stats != null && !enemy.Stats.IsDead)
            {
                CombatEffects.CrowdControl(player, enemy, StatusEffectKind.Stun, (float)v.StaggerSeconds);
                LegendaryKnockback.Push(enemy, player.Position, (float)LegendaryRules.Knockback);
            }
            CombatPaths.Ring(enemy.Position, (float)LegendaryRules.BlastRadius, new Color(1f, 0.78f, 0.3f), 0.45f);
            CombatPaths.Word(enemy, Localization.UI_FLOAT_THUNDER_SHOT, new Color(1f, 0.8f, 0.35f));
        }
    }

    private static void Aegis(PlayerController wielder, int rank)
    {
        if (wielder.Stats == null || wielder.Stats.Health == null) return;
        var v = Legendary.Values(LegendaryPower.IronAegis, rank);
        var shield = LegendaryRules.AegisShield(v, wielder.Stats.Health.MaxValue);
        if (shield <= 0) return;
        Count("iron-aegis");
        var reach = (float)LegendaryRules.AegisRadius;
        var game = GameManager.Instance;
        if (!game || game.Players == null) return;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!p || p.isDestroyed || p.Removed || p.Stats == null || p.Stats.IsDead || p.Island != wielder.Island) continue;
            if ((p.Position - wielder.Position).sqrMagnitude > reach * reach) continue;
            p.AddShield(shield, (float)LegendaryRules.AegisSeconds);
        }
        CombatPaths.Ring(wielder.Position, reach, new Color(0.5f, 1f, 0.7f), 0.7f);
        CombatPaths.Word(wielder, Localization.UI_FLOAT_IRON_AEGIS, new Color(0.55f, 1f, 0.72f));
    }

    // ---- an enemy fell ----------------------------------------------------------------------------------------------

    /// <summary>An enemy fell (PlayerCombat.Defeated): Soul Harvest heals every one who struck it and wears a Soulreaver.</summary>
    public static void Defeated(PlayerController killer, IAttackable enemy)
    {
        if (!CombatRules.Enabled || enemy == null) return;
        var healed = new HashSet<PlayerController>();
        foreach (var attacker in enemy.GetAttackers())
        {
            if (attacker is PlayerController p && p) Harvest(p, healed);
        }
        if (killer) Harvest(killer, healed);
    }

    private static void Harvest(PlayerController player, HashSet<PlayerController> healed)
    {
        if (!healed.Add(player) || player.Stats == null || player.Stats.IsDead) return;
        var rank = Rank(player, LegendaryPower.SoulHarvest);
        if (rank <= 0) return;
        var amount = LegendaryRules.KillHeal(Legendary.Values(LegendaryPower.SoulHarvest, rank), player.Stats.Health.MaxValue);
        if (amount <= 0) return;
        Count("soul-harvest");
        player.Heal(amount);
        CombatPaths.Word(player, Localization.UI_FLOAT_SOUL_HARVEST, new Color(0.62f, 0.85f, 1f));
    }

    // ---- helpers ----------------------------------------------------------------------------------------------------

    private static readonly List<EnemyController> near = new List<EnemyController>();

    /// <summary>Up to <paramref name="count"/> living enemies within <paramref name="radius"/>, nearest first.</summary>
    private static List<EnemyController> Nearest(Vector3 center, float radius, PlayerController by, EnemyController not, int count)
    {
        near.Clear();
        foreach (var e in EnemyController.Active)
        {
            if (!e || e.Stats == null || e.Stats.IsDead || e == not || e.Island != by.Island) continue;
            if ((e.Position - center).sqrMagnitude <= radius * radius) near.Add(e);
        }
        near.Sort((a, b) => (a.Position - center).sqrMagnitude.CompareTo((b.Position - center).sqrMagnitude));
        if (near.Count > count) near.RemoveRange(count, near.Count - count);
        return near;
    }
}

/// <summary>Thunder Shot's knockback: the enemy slides back along the navmesh over a fifth of a second.</summary>
public sealed class LegendaryKnockback : MonoBehaviour
{
    private Vector3 direction;
    private float left;
    private float speed;
    private NavMeshAgent agent;

    public static void Push(EnemyController enemy, Vector3 from, float distance)
    {
        if (!enemy || distance <= 0) return;
        var agent = enemy.GetComponent<NavMeshAgent>();
        if (!agent || !agent.enabled || !agent.isOnNavMesh) return;
        var dir = enemy.Position - from;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) dir = enemy.transform.forward;
        var k = enemy.GetComponent<LegendaryKnockback>();
        if (!k) k = enemy.gameObject.AddComponent<LegendaryKnockback>();
        k.agent = agent;
        k.direction = dir.normalized;
        k.left = distance;
        k.speed = distance / 0.2f;
        k.enabled = true;
    }

    private void Update()
    {
        if (left <= 0 || !agent || !agent.enabled || !agent.isOnNavMesh)
        {
            enabled = false;
            return;
        }
        var step = Mathf.Min(left, speed * Time.deltaTime);
        agent.Move(direction * step);
        left -= step;
    }
}
