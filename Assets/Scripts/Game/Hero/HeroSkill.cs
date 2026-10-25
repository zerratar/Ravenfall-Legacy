using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A hero skill (docs/hero-mode-design.md): its key slot, the style level that unlocks it, its mana and cooldown, and
/// what it does. 7a has the melee kit: Cleave, Leap slam, Whirlwind and War cry; ranged, magic and healing follow in
/// 7c. Damage multiplies the style's own (GameMath), so the character's levels and gear matter.
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
                hero.StartCoroutine(Later(0.35f, () => { foreach (var e in hero.EnemiesInArc(3.5f, 70f).ToList()) hero.Hit(e, 1.6f); }));
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
                var to = hero.AimPoint;
                var from = hero.Player.Position;
                var flat = to - from; flat.y = 0f;
                if (flat.magnitude > 9f) to = from + flat.normalized * 9f;
                if (!hero.TryGround(to, out var ground)) return false;
                hero.Face(ground);
                hero.Play("Hero-Leap", 0.05f);
                hero.StartCoroutine(hero.Move(ground, 0.5f, 2.5f, () =>
                {
                    hero.Play("Hero-Land", 0.05f);
                    foreach (var e in hero.EnemiesNear(ground, 3.5f).ToList())
                    {
                        hero.Hit(e, 2f);
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

    private static System.Collections.IEnumerator Later(float seconds, Action then)
    {
        yield return new WaitForSeconds(seconds);
        then();
    }
}
