using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Skill = RavenNest.Models.Skill;

/// <summary>
/// Owns experience gain for a single player: the multiplier stack, how much experience an action is
/// worth, and how that experience is distributed across skills.
///
/// <para>
/// Split out of PlayerController, which had grown to ~3,700 lines. This is deliberately a plain
/// class rather than a MonoBehaviour: a player already carries 26 components, and with sessions of
/// a thousand players every extra component is another Unity object to allocate, track and destroy.
/// A plain class owned by the controller costs one managed object and no engine overhead.
/// </para>
///
/// <para>
/// It holds a back reference to its owner for now. That is intentional for this first pass - the
/// aim was to move the logic without changing behaviour, so PlayerController's public API is
/// unchanged and every existing call site still works. Narrowing this to only the state it needs
/// is a later pass, and is what will eventually make experience gain testable without a live
/// player in a scene.
/// </para>
/// </summary>
public class PlayerProgression
{
    private readonly PlayerController player;

    public PlayerProgression(PlayerController player)
    {
        this.player = player;
    }

    /// <summary>
    /// Experience multiplier granted by subscription or Patreon tier, whichever is higher.
    /// </summary>
    public double GetTierExpMultiplier()
    {
        var tierMulti = TwitchEventManager.TierExpMultis[player.GameManager.SessionSettings.SubscriberTier];
        var subMulti = (player.IsSubscriber || player.GameManager.PlayerBoostRequirement > 0) ? tierMulti : 0;
        var multi = subMulti;
        if (player.PatreonTier > 0)
        {
            var patreonMulti = TwitchEventManager.TierExpMultis[player.PatreonTier];
            if (patreonMulti > multi)
            {
                return patreonMulti;
            }
        }
        return multi;
    }

    /// <summary>
    /// The full multiplier stack for a skill: tier, active global boost, village bonus and rested.
    /// </summary>
    public double GetExpMultiplier(Skill skill)
    {
        var tierSub = GetTierExpMultiplier();
        var multi = (float)tierSub;
        var boost = player.GameManager.Boost;
        if (boost.Active)
            multi += boost.Multiplier;

        multi += player.GameManager.Village.GetExpBonusBySkill(skill);
        multi = Math.Max(1, multi);
        if (player.Rested.ExpBoost > 1 && player.Rested.RestedTime > 0)
        {
            var rexp = (float)player.Rested.ExpBoost;
            multi = Mathf.Max(rexp * multi, rexp);
        }

        return multi;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetMultiplierFactor() => 1;

    /// <summary>
    /// Per-chunk scaling, which is how crowding on a training spot reduces gain.
    /// </summary>
    public double GetExpFactor(out ExpGainState state)
    {
        var skill = player.ActiveSkill;
        state = ExpGainState.FullGain;
        if (skill == Skill.Sailing || skill == Skill.Healing) return 1;
        return player.Chunk?.CalculateExpFactor(player, out state) ?? 1d;
    }

    /// <summary>
    /// How much experience one action in this skill is worth right now.
    /// </summary>
    public double GetExperience(Skill skill, double factor)
    {
        // check if we are training all. If so, take the avg level + 1
        // we can't take the min level as if you have super high str and low the rest.
        // exp gain shouldnt be super low. Don't want to punish those players
        // we can't take the max level either as it would mean that you can focus leveling on
        // one skill first and then gain the rest super quickly. using avg will still
        // benefit the player but not as much that it can be abused.
        // It will be more beneficial if the player have similar level on each skill (ATK,DEF,STR)
        int nextLevel = skill == Skill.Health || skill == Skill.Melee
            ? ((int)((player.GetSkill(Skill.Attack).Level + player.GetSkill(Skill.Defense).Level + player.GetSkill(Skill.Strength).Level) / 3f)) + 1
            : player.GetSkill(skill).Level + 1;

        var xp = GameMath.Exp.CalculateExperience(nextLevel, skill, factor, GetExpMultiplier(skill), GetMultiplierFactor());

        return xp * Mathf.Max(1, player.GetModifiers().ExpMultiplier);
    }

    /// <summary>
    /// Adds experience to the currently trained skill. Returns false when nothing is being trained.
    /// </summary>
    public bool AddExp(double factor = 1)
    {
        var skill = player.ActiveSkill;
        if (skill != Skill.None)
        {
            AddExp(skill, factor);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds experience to a specific skill.
    ///
    /// <para>
    /// Combat skills also feed a third of the experience into Health. Training Health or Melee
    /// spreads the experience across Attack, Defense and Strength, skipping any that have already
    /// reached the auto-train target level and redistributing their share to the remaining ones.
    /// </para>
    /// </summary>
    public void AddExp(Skill skill, double factor = 1)
    {
        try
        {
            if (factor == 0)
            {
                return;
            }

            var stats = player.Stats;
            var stat = stats.GetSkill(skill);
            if (stat == null)
                return;

            var exp = GetExperience(skill, factor);
            var autoTrainTargetLevel = player.AutoTrainTargetLevel;

            if (skill.IsCombatSkill())
            {
                if (stats.Health.AddExp(exp / 3d, out var hpLevels))
                    player.CelebrateSkillLevelUp(Skill.Health, hpLevels);

                if (skill == Skill.Health || skill == Skill.Melee)
                {
                    var each = exp / 3d;
                    var left = 3d;

                    if (autoTrainTargetLevel <= 0 || autoTrainTargetLevel > stats.Attack.Level)
                    {
                        if (stats.Attack.AddExp(each, out var a))
                            player.CelebrateSkillLevelUp(Skill.Attack, a);
                    }
                    else
                    {
                        each = exp / --left;
                    }

                    if (autoTrainTargetLevel <= 0 || autoTrainTargetLevel > stats.Defense.Level)
                    {
                        if (stats.Defense.AddExp(each, out var b))
                            player.CelebrateSkillLevelUp(Skill.Defense, b);
                    }
                    else
                    {
                        each = exp / --left;
                    }

                    if (autoTrainTargetLevel <= 0 || autoTrainTargetLevel > stats.Strength.Level)
                        if (stats.Strength.AddExp(each, out var c))
                            player.CelebrateSkillLevelUp(Skill.Strength, c);

                    return;
                }
            }

            if (stat.Type == Skill.Slayer || stat.Type == Skill.Sailing ||
                stat.Type == Skill.Health || stat.Type == Skill.Melee ||
                autoTrainTargetLevel <= 0 || autoTrainTargetLevel > stat.Level)
            {
                if (stat.AddExp(exp, out var atkLvls))
                {
                    player.CelebrateSkillLevelUp(skill, atkLvls);
                }
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to add exp to " + player.PlayerName + " training '" + skill + "': " + exc);
        }
    }
}
