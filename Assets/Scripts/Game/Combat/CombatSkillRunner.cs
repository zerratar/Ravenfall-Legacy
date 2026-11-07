using System.Collections.Generic;
using RavenNest.Combat;
using RavenNest.Models;
using UnityEngine;

/// <summary>
///     Uses a player's actives by themselves (docs/combat-skills-design.md, section 3): when a swing or heal would land,
///     the loadout is checked top first, and the first active that is off cooldown, has the gear it needs and whose use
///     rule fits is used instead of the basic attack. A short shared pause keeps a character from firing everything at
///     once. Its name floats up over the character. Combat v2 only (CombatRules.Enabled); bots use it too.
/// </summary>
public sealed class CombatSkillRunner
{
    private readonly PlayerController owner;
    private readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();
    private readonly List<IAttackable> targets = new List<IAttackable>();
    private float pauseUntil;
    private static Shinobytes.UI.FloatingTextView floating;

    /// <summary>How often each active has been used this session, every character together (to see the trees at work).</summary>
    public static readonly Dictionary<string, int> UsedCounts = new Dictionary<string, int>();

    public CombatSkillRunner(PlayerController owner) => this.owner = owner;

    /// <summary>Seconds until an active can be used again, 0 when ready (for !skills).</summary>
    public float CooldownLeft(string id) => readyAt.TryGetValue(id, out var t) ? Mathf.Max(0f, t - Time.time) : 0f;

    /// <summary>Tries the loadout at the moment a basic attack (or heal) would land on <paramref name="target"/>; true when an active was used instead.</summary>
    public bool TryUse(IAttackable target)
    {
        if (!CombatRules.Enabled || owner == null || owner.SkillBuild == null || owner.Stats == null || owner.Stats.IsDead) return false;
        if (Time.time < pauseUntil || owner.SkillBuild.Loadout.Count == 0) return false;

        foreach (var node in owner.SkillBuild.Actives())
        {
            var active = node.Active;
            if (CooldownLeft(node.Id) > 0f || !HasGear(active.Needs) || !RuleFits(active, target)) continue;
            Resolve(active, target);
            if (targets.Count == 0) continue;

            Run(node, active, target);
            readyAt[node.Id] = Time.time + (float)active.Cooldown;
            pauseUntil = Time.time + (float)SkillTrees.SharedPause;
            ShowName(node.Name);
            UsedCounts[node.Id] = UsedCounts.TryGetValue(node.Id, out var used) ? used + 1 : 1;
            return true;
        }
        return false;
    }

    // ---- what a skill needs ---------------------------------------------------------------------------------

    private bool HasGear(SkillGear needs)
    {
        var inventory = owner.Inventory;
        switch (needs)
        {
            case SkillGear.Shield: return owner.Equipment != null && owner.Equipment.HasShield;
            case SkillGear.MeleeWeapon: return inventory.GetMeleeWeapon() != null;
            case SkillGear.Bow: return inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedBow) != null;
            case SkillGear.Staff: return inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedStaff) != null;
            default: return true;
        }
    }

    private static float HealthShare(IAttackable f)
    {
        var s = f?.GetStats();
        return s == null || s.Health.MaxValue <= 0 ? 1f : (float)s.Health.CurrentValue / s.Health.MaxValue;
    }

    private static bool IsBoss(IAttackable target) =>
        target is EnemyController e && e && (e.IsRaidBoss || e.IsDungeonBoss || (e.Definition && e.Definition.Definition != null && e.Definition.Definition.IsBoss));

    private bool RuleFits(ActiveSkill active, IAttackable target)
    {
        var radius = active.Radius > 0 ? (float)active.Radius : 6f;
        switch (active.Use)
        {
            case SkillUse.InFight: return Enemy(target);
            case SkillUse.OnBoss: return Enemy(target) && IsBoss(target);
            case SkillUse.TargetLow: return Enemy(target) && HealthShare(target) < 0.2f;
            case SkillUse.SelfHurt: return HealthShare(owner) < 0.5f;
            case SkillUse.EnemiesNear:
                {
                    var center = active.Targeting == SkillTargeting.AroundTarget && Enemy(target) ? target.Transform.position : owner.Position;
                    return CountEnemies(center, Mathf.Max(radius, 4f)) >= 3;
                }
            case SkillUse.AllyHurt:
                foreach (var ally in Allies(radius)) if (HealthShare(ally) < 0.6f) return true;
                return false;
            case SkillUse.AllyHarmed:
                foreach (var ally in Allies(radius)) if (CombatEffects.HasHarmful(ally)) return true;
                return false;
            case SkillUse.EnemyOnAlly:
                foreach (var e in EnemyController.Active)
                    if (Near(e, owner.Position, radius) && e.TargetPlayer && e.TargetPlayer != owner && !e.TargetPlayer.Stats.IsDead) return true;
                return false;
            default: return false;
        }
    }

    private static bool Enemy(IAttackable target) => target is EnemyController e && e && e.Stats != null && !e.Stats.IsDead;

    private bool Near(EnemyController e, Vector3 center, float radius) =>
        e && e.Stats != null && !e.Stats.IsDead && e.Island == owner.Island && (e.Position - center).sqrMagnitude <= radius * radius;

    private int CountEnemies(Vector3 center, float radius)
    {
        var n = 0;
        foreach (var e in EnemyController.Active) if (Near(e, center, radius)) n++;
        return n;
    }

    private IEnumerable<PlayerController> Allies(float radius)
    {
        var game = owner.GameManager;
        if (!game || game.Players == null) yield break;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!p || p.Stats == null || p.Stats.IsDead || p.Island != owner.Island) continue;
            if ((p.Position - owner.Position).sqrMagnitude > radius * radius) continue;
            yield return p;
        }
    }

    // ---- who a skill reaches --------------------------------------------------------------------------------

    private void Resolve(ActiveSkill active, IAttackable target)
    {
        targets.Clear();
        var radius = active.Radius > 0 ? (float)active.Radius : 6f;
        var max = Mathf.Max(1, active.MaxTargets);
        switch (active.Targeting)
        {
            case SkillTargeting.Self:
                targets.Add(owner);
                break;
            case SkillTargeting.Target:
                if (Enemy(target)) targets.Add(target);
                break;
            case SkillTargeting.LowestAlly:
                {
                    PlayerController lowest = null;
                    var least = 2f;
                    foreach (var ally in Allies(radius))
                    {
                        var share = HealthShare(ally);
                        if (active.Use == SkillUse.AllyHarmed ? CombatEffects.HasHarmful(ally) && share < least : share < least) { least = share; lowest = ally; }
                    }
                    if (lowest) targets.Add(lowest);
                    break;
                }
            case SkillTargeting.Allies:
                {
                    var list = new List<PlayerController>(Allies(radius));
                    list.Sort((a, b) => HealthShare(a).CompareTo(HealthShare(b)));
                    for (var i = 0; i < list.Count && targets.Count < max; ++i) targets.Add(list[i]);
                    break;
                }
            case SkillTargeting.Cone:
                {
                    var forward = owner.transform.forward;
                    if (Enemy(target)) targets.Add(target);
                    foreach (var e in EnemyController.Active)
                    {
                        if (targets.Count >= max) break;
                        if (!Near(e, owner.Position, radius) || targets.Contains(e)) continue;
                        if (Vector3.Dot(forward, (e.Position - owner.Position).normalized) > 0.3f) targets.Add(e);
                    }
                    break;
                }
            case SkillTargeting.Around:
                foreach (var e in EnemyController.Active)
                {
                    if (targets.Count >= max) break;
                    if (Near(e, owner.Position, radius)) targets.Add(e);
                }
                break;
            case SkillTargeting.AroundTarget:
                {
                    if (!Enemy(target)) break;
                    targets.Add(target);
                    var center = target.Transform.position;
                    foreach (var e in EnemyController.Active)
                    {
                        if (targets.Count >= max) break;
                        if (Near(e, center, radius) && !targets.Contains(e)) targets.Add(e);
                    }
                    break;
                }
            case SkillTargeting.Chain:
                {
                    if (!Enemy(target)) break;
                    targets.Add(target);
                    var from = target.Transform.position;
                    while (targets.Count < max)
                    {
                        EnemyController next = null;
                        var best = radius * radius;
                        foreach (var e in EnemyController.Active)
                        {
                            if (!Near(e, from, radius) || targets.Contains(e)) continue;
                            var d = (e.Position - from).sqrMagnitude;
                            if (d < best) { best = d; next = e; }
                        }
                        if (!next) break;
                        targets.Add(next);
                        from = next.Position;
                    }
                    break;
                }
            case SkillTargeting.FarthestEnemy:
                {
                    EnemyController far = null;
                    var most = -1f;
                    foreach (var e in EnemyController.Active)
                    {
                        if (!Near(e, owner.Position, radius)) continue;
                        var d = (e.Position - owner.Position).sqrMagnitude;
                        if (d > most) { most = d; far = e; }
                    }
                    if (far) targets.Add(far);
                    else if (Enemy(target)) targets.Add(target);
                    break;
                }
        }
    }

    // ---- what a skill does ------------------------------------------------------------------------------------

    private void Run(SkillNode node, ActiveSkill active, IAttackable target)
    {
        var ccBonus = 1f + (float)owner.EquipmentStats.Gear(GearStat.CrowdControlDuration);
        foreach (var effect in active.Effects)
        {
            foreach (var t in targets)
            {
                if (t == null || (t is Object o && !o)) continue;
                var seconds = (float)effect.Seconds;
                switch (effect.Kind)
                {
                    case SkillEffectKind.Damage:
                        for (var hit = 0; hit < Mathf.Max(1, effect.Hits); ++hit)
                        {
                            var result = CombatRules.Strike(owner, t, active.Style, new AttackOptions { Multiplier = effect.Amount, Element = effect.Element, ArmorIgnored = effect.ArmorIgnored });
                            if (result == null) break;
                            owner.SessionStats.AddDamageDealt(result.Total);
                            if (t.TakeDamage(owner, result.Total))
                            {
                                if (t is EnemyController) owner.Combat.Defeated(t);
                                break;
                            }
                        }
                        break;
                    case SkillEffectKind.DamageOverTime:
                        CombatEffects.AddDamageOverTime(owner, t, effect.Element, CombatRules.TypicalHit(owner, active.Style) * effect.Amount, seconds);
                        break;
                    case SkillEffectKind.Heal:
                        t.Heal(CombatRules.SkillHeal(owner, effect.Amount));
                        break;
                    case SkillEffectKind.HealOverTime:
                        CombatEffects.AddHealOverTime(owner, t, CombatRules.SkillHeal(owner, effect.Amount), seconds);
                        break;
                    case SkillEffectKind.HealPercent:
                        {
                            var s = t.GetStats();
                            if (s != null) t.Heal(Mathf.RoundToInt((float)(s.Health.MaxValue * effect.Amount)));
                            break;
                        }
                    case SkillEffectKind.Shield:
                        if (t is PlayerController shielded) shielded.AddShield(CombatRules.SkillHeal(owner, effect.Amount), seconds);
                        break;
                    case SkillEffectKind.DamageTaken:
                        CombatEffects.AddModifier(t, CombatEffects.ModifierKind.DamageTaken, effect.Amount, seconds);
                        break;
                    case SkillEffectKind.DamageDealt:
                        CombatEffects.AddModifier(t, CombatEffects.ModifierKind.DamageDealt, effect.Amount, seconds);
                        break;
                    case SkillEffectKind.AttackSpeed:
                        CombatEffects.AddModifier(t, CombatEffects.ModifierKind.AttackSpeed, effect.Amount, seconds);
                        break;
                    case SkillEffectKind.Taunt:
                        if (t is EnemyController taunted) taunted.Taunt(owner, seconds);
                        break;
                    case SkillEffectKind.Stun:
                        CombatEffects.CrowdControl(owner, t, StatusEffectKind.Stun, seconds * ccBonus);
                        break;
                    case SkillEffectKind.Freeze:
                        CombatEffects.CrowdControl(owner, t, StatusEffectKind.Freeze, seconds * ccBonus);
                        break;
                    case SkillEffectKind.Root:
                        CombatEffects.CrowdControl(owner, t, StatusEffectKind.Root, seconds * ccBonus);
                        break;
                    case SkillEffectKind.Cleanse:
                        CombatEffects.Cleanse(t);
                        break;
                    case SkillEffectKind.CrowdControlImmunity:
                        CombatEffects.ImmuneToCrowdControl(t, seconds);
                        break;
                }
            }
        }
    }

    private void ShowName(string name)
    {
        if (!GraphicsToggler.GraphicsEnabled) return;
        if (!floating) floating = Object.FindAnyObjectByType<Shinobytes.UI.FloatingTextView>();
        if (floating) floating.AddText(owner.transform, name);
    }
}
