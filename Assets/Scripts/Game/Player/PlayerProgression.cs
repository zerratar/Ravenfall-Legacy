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

    // ---- Training away from the islands -------------------------------------------------------------------------
    //
    // In a calm place (a tower town, Ravebridge) a player keeps training their skill at the rate they had on their
    // island, so waiting there costs nothing (Zerratar 2026-11-08). The rate is learned from the experience the player
    // really earned per second while training on an island (the sum of the exp factors, so level, boosts and multipliers
    // apply as usual when it is paid); until a few minutes are known, a typical island rate per skill is used.

    private sealed class TrainingRate
    {
        public double Factor;
        public float Seconds;
    }

    private readonly System.Collections.Generic.Dictionary<Skill, TrainingRate> rates = new System.Collections.Generic.Dictionary<Skill, TrainingRate>();
    private double awayPending;
    private double awayEarned;
    private float awayWindow;
    private bool applyingAway;

    /// <summary>The seconds of history after which the learned rate halves (so it follows the player's current spot).</summary>
    private const float RateMemorySeconds = 600f;
    /// <summary>The seconds of history needed before the learned rate is used instead of the typical one.</summary>
    private const float RateTrustSeconds = 90f;
    /// <summary>How often the calm place pays what the island would have.</summary>
    private const float AwayPayInterval = 5f;

    private TrainingRate Rate(Skill skill)
    {
        if (!rates.TryGetValue(skill, out var rate)) rates[skill] = rate = new TrainingRate();
        return rate;
    }

    /// <summary>Training on an island as usual (or sailing on the ferry): the rate is learned from it.</summary>
    public bool TrainingNormally
    {
        get
        {
            var p = player;
            if (!p || p.IsAway || p.InRavebridge || p.raidHandler.InRaid || p.onsenHandler.InOnsen || p.duelHandler.InDuel
                || p.arenaHandler.InArena || p.streamRaidHandler.InWar || p.Stats.IsDead) return false;
            if (p.ferryHandler.OnFerry) return p.ActiveSkill == Skill.Sailing;
            return p.Island != null;
        }
    }

    /// <summary>Called every frame: time spent training on an island counts toward the rate of the trained skill.</summary>
    public void TrackTraining(float deltaTime)
    {
        var skill = player.ActiveSkill;
        if (skill == Skill.None || !TrainingNormally) return;
        var rate = Rate(skill);
        rate.Seconds += deltaTime;
        if (rate.Seconds > RateMemorySeconds)
        {
            rate.Seconds *= 0.5f;
            rate.Factor *= 0.5;
        }
    }

    /// <summary>Exp factors per second the player earns training this skill on their island.</summary>
    public double IslandRate(Skill skill)
    {
        if (rates.TryGetValue(skill, out var rate) && rate.Seconds >= RateTrustSeconds) return rate.Factor / rate.Seconds;
        return TypicalRate(skill);
    }

    /// <summary>
    ///     Exp factors per second on an island at a fitting spot, measured with bots (2026-11-08): an action every 2 to 3
    ///     seconds, not every one paying, and the chunk's factor.
    /// </summary>
    public static double TypicalRate(Skill skill)
    {
        switch (skill)
        {
            case Skill.Woodcutting: return 0.17; // a share of every tree felled
            case Skill.Mining: return 0.45;
            case Skill.Fishing: return 0.30;
            case Skill.Farming: return 0.30;
            case Skill.Gathering: return 0.30;
            case Skill.Crafting:
            case Skill.Cooking:
            case Skill.Alchemy: return 0.30;
            case Skill.Sailing: return 0.30;
            case Skill.Healing: return 0.30;
            default: return 0.20; // the combat skills: a share of every kill
        }
    }

    /// <summary>
    ///     Called every frame in a calm place: every few seconds the experience the island would have given is paid, less
    ///     what the player earned in that skill there anyway (Ravebridge has its own fights), never less than nothing.
    /// </summary>
    public void TrainAway(float deltaTime)
    {
        var skill = player.ActiveSkill;
        if (skill == Skill.None) { awayPending = awayEarned = 0; awayWindow = 0; return; }
        awayPending += IslandRate(skill) * deltaTime;
        awayWindow += deltaTime;
        if (awayWindow < AwayPayInterval) return;

        var give = awayPending - awayEarned;
        awayPending = awayEarned = 0;
        awayWindow = 0;
        if (give <= 0) return;
        var target = player.AutoTrainTargetLevel;
        if (target > 0 && skill != Skill.Health && skill != Skill.Melee && target <= player.GetSkill(skill).Level) return;
        applyingAway = true;
        try { AddExp(skill, give); }
        finally { applyingAway = false; }
    }

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

        // and the village's perks from the streamer's story lines (StoryLog)
        return xp * Mathf.Max(1, player.GetModifiers().ExpMultiplier) * StoryLog.ExpFactor(skill, player.Island);
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

            // the training rate on the island, and what a calm place already gave (see TrainAway)
            if (!applyingAway)
            {
                if (TrainingNormally) Rate(skill).Factor += factor;
                else if (skill == player.ActiveSkill) awayEarned += factor;
            }

            var stats = player.Stats;
            var stat = stats.GetSkill(skill);
            if (stat == null)
                return;

            var exp = GetExperience(skill, factor);
            var autoTrainTargetLevel = player.AutoTrainTargetLevel;

            // a battle pet out shares it: the pet and Taming get a quarter (docs/battle-pets-design.md)
            player.BattlePets.Earned(skill, exp);

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
