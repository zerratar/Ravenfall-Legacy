using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>The hero's combat styles; Tab switches between them (HeroController).</summary>
public enum HeroStyle
{
    Melee,
    Ranged,
    Magic,
    Healing,
}

/// <summary>
/// A hero skill (docs/hero-mode-design.md): its key slot, the style level that unlocks it, its mana and cooldown, and
/// what it does. Each style has a kit of four. Damage multiplies the style's own (GameMath, HeroController.Hit), so
/// the character's levels and gear matter; the looks come from HeroEffects.
/// </summary>
public sealed class HeroSkill
{
    public string Key;
    public Func<string> Name;
    public Func<string> Description;
    public string Icon; // a Font Awesome glyph
    public int UnlockLevel;
    public float Mana;
    public float Cooldown;
    public Func<HeroController, bool> Run;

    public const float SpinInterval = 0.4f;
    public const float SpinRadius = 3f;
    public const float SpinMultiplier = 0.7f;

    public static IReadOnlyList<HeroSkill> For(HeroStyle style)
    {
        switch (style)
        {
            case HeroStyle.Ranged: return Ranged;
            case HeroStyle.Magic: return Magic;
            case HeroStyle.Healing: return Healing;
            default: return Melee;
        }
    }

    /// <summary>The style's icon for the HUD.</summary>
    public static string StyleIcon(HeroStyle style)
    {
        switch (style)
        {
            case HeroStyle.Ranged: return ""; // bow-arrow
            case HeroStyle.Magic: return ""; // hat-wizard
            case HeroStyle.Healing: return ""; // staff-snake
            default: return ""; // swords
        }
    }

    public static readonly HeroSkill[] Melee =
    {
        new HeroSkill
        {
            Key = "cleave", Name = () => Localization.UI_HERO_CLEAVE, Description = () => Localization.UI_HERO_CLEAVE_HINT, Icon = "",
            UnlockLevel = 1, Mana = 15f, Cooldown = 4f,
            Run = hero =>
            {
                // a wide arc in front, toward the cursor
                hero.Face(hero.AimPoint);
                hero.Play("Hero-Cleave", 0.05f);
                hero.StartCoroutine(Later(0.35f, () => { foreach (var e in hero.EnemiesInArc(3.5f, 70f).ToList()) hero.Strike(e, 1.6f); }));
                return true;
            },
        },
        new HeroSkill
        {
            Key = "leap", Name = () => Localization.UI_HERO_LEAP, Description = () => Localization.UI_HERO_LEAP_HINT, Icon = "",
            UnlockLevel = 30, Mana = 25f, Cooldown = 8f,
            Run = hero =>
            {
                // jump to the cursor (9 m at most): damage and a stun where the hero lands
                var to = hero.Within(hero.AimPoint, 9f);
                if (!hero.TryGround(to, out var ground)) return false;
                hero.Face(ground);
                hero.Play("Hero-Leap", 0.05f);
                hero.StartCoroutine(hero.Move(ground, 0.5f, 2.5f, () =>
                {
                    hero.Play("Hero-Land", 0.05f);
                    foreach (var e in hero.EnemiesNear(ground, 3.5f).ToList())
                    {
                        hero.Strike(e, 2f);
                        e.StunnedUntil = Time.time + 1.5f;
                    }
                }));
                return true;
            },
        },
        new HeroSkill
        {
            Key = "whirlwind", Name = () => Localization.UI_HERO_WHIRLWIND, Description = () => Localization.UI_HERO_WHIRLWIND_HINT, Icon = "",
            UnlockLevel = 100, Mana = 35f, Cooldown = 12f,
            Run = hero => { hero.StartSpin(3f); return true; },
        },
        new HeroSkill
        {
            Key = "warcry", Name = () => Localization.UI_HERO_WARCRY, Description = () => Localization.UI_HERO_WARCRY_HINT, Icon = "",
            UnlockLevel = 250, Mana = 30f, Cooldown = 20f,
            Run = hero =>
            {
                // enemies around turn on the hero, who takes half the damage for a while
                hero.Play("Hero-WarCry");
                foreach (var e in hero.EnemiesNear(hero.Player.Position, 10f))
                {
                    e.SetTarget(hero.Player);
                    e.InCombat = true;
                }
                hero.Guard(6f);
                return true;
            },
        },
    };

    public static readonly HeroSkill[] Ranged =
    {
        new HeroSkill
        {
            Key = "multishot", Name = () => Localization.UI_HERO_MULTISHOT, Description = () => Localization.UI_HERO_MULTISHOT_HINT, Icon = "",
            UnlockLevel = 1, Mana = 15f, Cooldown = 4f,
            Run = hero =>
            {
                // five arrows in a cone toward the cursor; each hits the first enemy in its line
                hero.Face(hero.AimPoint);
                hero.Play("Hero-Shoot", 0.05f);
                var forward = hero.Forward;
                var taken = new HashSet<EnemyController>();
                for (var i = -2; i <= 2; i++)
                {
                    var direction = Quaternion.Euler(0f, i * 12f, 0f) * forward;
                    var target = hero.Closest(hero.Player.Position, 14f, direction, 8f, taken);
                    if (target) taken.Add(target);
                    var end = hero.Player.Position + direction * 14f;
                    hero.Shoot(HeroEffects.Get?.Arrow, target, end, 30f, 0.6f, e => hero.Strike(e, 1f));
                }
                return true;
            },
        },
        new HeroSkill
        {
            Key = "arrowrain", Name = () => Localization.UI_HERO_ARROWRAIN, Description = () => Localization.UI_HERO_ARROWRAIN_HINT, Icon = "",
            UnlockLevel = 30, Mana = 25f, Cooldown = 9f,
            Run = hero =>
            {
                // arrows fall on the cursor's place for three seconds, hurting everything under them
                var center = hero.Within(hero.AimPoint, 16f);
                hero.Face(center);
                hero.Play("Hero-Volley", 0.05f);
                hero.StartCoroutine(Repeat(3f, 0.15f, _ =>
                {
                    var spot = center + Flat(UnityEngine.Random.insideUnitSphere) * 3.5f;
                    hero.StartCoroutine(HeroEffects.Fly(HeroEffects.Get?.Arrow, spot + new Vector3(-1.5f, 14f, -1.5f), () => spot, 28f, 0f, end => HeroEffects.Burst(HeroEffects.Get?.ArrowRain, end, 1.2f, 1.5f)));
                }));
                hero.StartCoroutine(Repeat(3f, 0.5f, _ => { foreach (var e in hero.EnemiesNear(center, 3.8f).ToList()) hero.Strike(e, 0.5f); }, 0.5f));
                return true;
            },
        },
        new HeroSkill
        {
            Key = "snare", Name = () => Localization.UI_HERO_SNARE, Description = () => Localization.UI_HERO_SNARE_HINT, Icon = "",
            UnlockLevel = 100, Mana = 25f, Cooldown = 12f,
            Run = hero =>
            {
                // a trap thrown at the cursor: enemies inside are held in place for four seconds
                var center = hero.Within(hero.AimPoint, 14f);
                hero.Face(center);
                hero.Play("Hero-Shoot", 0.05f);
                hero.StartCoroutine(HeroEffects.Fly(null, hero.CastPoint, () => center, 20f, 0f, at =>
                {
                    hero.StartCoroutine(Repeat(4f, 0.5f, _ =>
                    {
                        HeroEffects.Burst(HeroEffects.Get?.Snare, center, 1.6f, 1.5f);
                        foreach (var e in hero.EnemiesNear(center, 3f)) e.StunnedUntil = Mathf.Max(e.StunnedUntil, Time.time + 0.7f);
                    }));
                }));
                return true;
            },
        },
        new HeroSkill
        {
            Key = "rapidfire", Name = () => Localization.UI_HERO_RAPIDFIRE, Description = () => Localization.UI_HERO_RAPIDFIRE_HINT, Icon = "",
            UnlockLevel = 250, Mana = 30f, Cooldown = 18f,
            Run = hero => { hero.StartRapidFire(5f); return true; },
        },
    };

    public static readonly HeroSkill[] Magic =
    {
        new HeroSkill
        {
            Key = "fireball", Name = () => Localization.UI_HERO_FIREBALL, Description = () => Localization.UI_HERO_FIREBALL_HINT, Icon = "",
            UnlockLevel = 1, Mana = 15f, Cooldown = 3f,
            Run = hero =>
            {
                // a ball of fire toward the cursor that bursts on the first enemy (or where the cursor is)
                hero.Face(hero.AimPoint);
                hero.Play("Hero-Cast", 0.05f);
                var target = hero.Closest(hero.Player.Position, 16f, hero.Forward, 15f);
                var end = hero.Within(hero.AimPoint, 16f) + Vector3.up;
                hero.Shoot(HeroEffects.Get?.Fireball, target, end, 22f, 0f, null, at =>
                {
                    foreach (var e in hero.EnemiesNear(at, 2.5f).ToList()) hero.Strike(e, e == target ? 2f : 1f);
                }, 1.5f);
                return true;
            },
        },
        new HeroSkill
        {
            Key = "frostnova", Name = () => Localization.UI_HERO_FROSTNOVA, Description = () => Localization.UI_HERO_FROSTNOVA_HINT, Icon = "",
            UnlockLevel = 30, Mana = 25f, Cooldown = 10f,
            Run = hero =>
            {
                // a ring of frost around the hero: everything near is hurt and frozen for three seconds
                hero.Play("Hero-CastArea", 0.05f);
                HeroEffects.Burst(HeroEffects.Get?.FrostNova, hero.Player.Position + Vector3.up * 0.3f, 3.5f, 2.5f);
                foreach (var e in hero.EnemiesNear(hero.Player.Position, 6f).ToList())
                {
                    hero.Strike(e, 1f);
                    e.StunnedUntil = Time.time + 3f;
                }
                return true;
            },
        },
        new HeroSkill
        {
            Key = "chain", Name = () => Localization.UI_HERO_CHAIN, Description = () => Localization.UI_HERO_CHAIN_HINT, Icon = "",
            UnlockLevel = 100, Mana = 30f, Cooldown = 8f,
            Run = hero =>
            {
                // lightning to the enemy toward the cursor, then on to four more close by, a little weaker each jump
                var first = hero.EnemyNearCursor(4f) ?? hero.Closest(hero.Player.Position, 14f, hero.Forward, 40f);
                if (!first) return false;
                hero.Face(first.Position);
                hero.Play("Hero-Cast", 0.05f);
                hero.StartCoroutine(Chain(hero, hero.CastPoint, first, 1.6f, 4, new HashSet<EnemyController>()));
                return true;
            },
        },
        new HeroSkill
        {
            Key = "meteor", Name = () => Localization.UI_HERO_METEOR, Description = () => Localization.UI_HERO_METEOR_HINT, Icon = "",
            UnlockLevel = 250, Mana = 40f, Cooldown = 16f,
            Run = hero =>
            {
                // a short wait, then a meteor falls on the cursor's place
                var center = hero.Within(hero.AimPoint, 18f);
                if (hero.TryGround(center, out var ground)) center = ground;
                hero.Face(center);
                hero.Play("Hero-Buff", 0.05f);
                hero.StartCoroutine(HeroEffects.Fly(HeroEffects.Get?.Meteor, center + new Vector3(-6f, 22f, -6f), () => center, 24f, 0f, at =>
                {
                    HeroEffects.Burst(HeroEffects.Get?.MeteorBlast, center, 3f, 3f);
                    foreach (var e in hero.EnemiesNear(center, 4.5f).ToList()) hero.Strike(e, 3f);
                }, 3f));
                return true;
            },
        },
    };

    public static readonly HeroSkill[] Healing =
    {
        new HeroSkill
        {
            Key = "mend", Name = () => Localization.UI_HERO_MEND, Description = () => Localization.UI_HERO_MEND_HINT, Icon = "",
            UnlockLevel = 1, Mana = 20f, Cooldown = 5f,
            Run = hero =>
            {
                // heals the hero, and the viewers in front of the hero
                hero.Play("Hero-Buff", 0.05f);
                hero.HealAlly(hero.Player, 1.5f);
                foreach (var p in hero.AlliesNear(hero.Player.Position, 10f))
                {
                    if (p == hero.Player) continue;
                    var to = Flat(p.Position - hero.Player.Position);
                    if (Vector3.Angle(hero.Forward, to) <= 70f) hero.HealAlly(p, 1f);
                }
                return true;
            },
        },
        new HeroSkill
        {
            Key = "smite", Name = () => Localization.UI_HERO_SMITE, Description = () => Localization.UI_HERO_SMITE_HINT, Icon = "",
            UnlockLevel = 30, Mana = 20f, Cooldown = 5f,
            Run = hero =>
            {
                // holy light on the enemy at the cursor, twice as hard on the undead
                var target = hero.EnemyNearCursor(4f) ?? hero.Closest(hero.Player.Position, 14f, hero.Forward, 40f);
                if (!target) return false;
                hero.Face(target.Position);
                hero.Play("Hero-Cast", 0.05f);
                hero.Shoot(HeroEffects.Get?.Smite, target, target.Position, 26f, 0f, e => hero.Strike(e, HeroController.IsUndead(e) ? 5f : 2.5f), null, 1.5f);
                return true;
            },
        },
        new HeroSkill
        {
            Key = "sanctuary", Name = () => Localization.UI_HERO_SANCTUARY, Description = () => Localization.UI_HERO_SANCTUARY_HINT, Icon = "",
            UnlockLevel = 100, Mana = 35f, Cooldown = 16f,
            Run = hero =>
            {
                // a holy circle at the cursor for six seconds: allies inside heal, enemies inside burn
                var center = hero.Within(hero.AimPoint, 14f);
                if (hero.TryGround(center, out var ground)) center = ground;
                hero.Play("Hero-CastArea", 0.05f);
                hero.StartCoroutine(Repeat(6f, 1f, _ =>
                {
                    HeroEffects.Burst(HeroEffects.Get?.Sanctuary, center + Vector3.up * 0.2f, 2.2f, 2f);
                    foreach (var p in hero.AlliesNear(center, 4.5f)) hero.HealAlly(p, 0.5f, false);
                    foreach (var e in hero.EnemiesNear(center, 4.5f).ToList()) hero.Strike(e, 0.6f);
                }));
                return true;
            },
        },
        new HeroSkill
        {
            Key = "angel", Name = () => Localization.UI_HERO_ANGEL, Description = () => Localization.UI_HERO_ANGEL_HINT, Icon = "",
            UnlockLevel = 250, Mana = 50f, Cooldown = 60f,
            Run = hero =>
            {
                // everyone near is healed in full, and the next blow that would fell the hero leaves it standing
                hero.Play("Hero-Buff", 0.05f);
                HeroEffects.Burst(HeroEffects.Get?.Angel, hero.Player.Position + Vector3.up, 2.5f, 3f);
                foreach (var p in hero.AlliesNear(hero.Player.Position, 12f)) hero.HealAlly(p, 100f, p != hero.Player);
                hero.GuardianAngel();
                return true;
            },
        },
    };

    /// <summary>Lightning from one enemy to the next close one, weaker each jump.</summary>
    private static System.Collections.IEnumerator Chain(HeroController hero, Vector3 from, EnemyController target, float multiplier, int jumpsLeft, HashSet<EnemyController> struck)
    {
        while (target)
        {
            struck.Add(target);
            var t = target;
            var done = false;
            yield return HeroEffects.Fly(HeroEffects.Get?.Lightning, from, () => t ? t.Position + Vector3.up : from, 45f, 0f, at => done = true);
            if (!done || !t) yield break;
            hero.Strike(t, multiplier);
            if (jumpsLeft-- <= 0) yield break;
            from = t.Position + Vector3.up;
            multiplier *= 0.85f;
            target = hero.Closest(t.Position, 7f, Vector3.zero, 180f, struck);
        }
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    private static System.Collections.IEnumerator Later(float seconds, Action then)
    {
        yield return new WaitForSeconds(seconds);
        then();
    }

    /// <summary>Something every interval for a while (the first right away, or after a delay).</summary>
    private static System.Collections.IEnumerator Repeat(float seconds, float interval, Action<int> step, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        var end = Time.time + seconds - delay;
        for (var i = 0; Time.time < end; i++)
        {
            step(i);
            yield return new WaitForSeconds(interval);
        }
    }
}
