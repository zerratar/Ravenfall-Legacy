using System;
using RavenNest.Models;
using UnityEngine;
using Skill = RavenNest.Models.Skill;

/// <summary>
/// Executes attacks and heals for a single player: choosing the attack type, driving the swing
/// timing, and applying damage or healing when the animation lands.
///
/// <para>
/// Split out of PlayerController as a plain class, not a component, for the same reason as
/// PlayerProgression and PlayerHealthRegeneration. See docs/player-component-architecture.md.
/// </para>
///
/// <para>
/// The animation timing values deliberately stayed on PlayerController. They are [SerializeField]
/// and are set on the Player prefab, where healingAnimationTime is 2 while the code default is 3.
/// Moving the fields here would have dropped the prefab value and silently made healing half again
/// as slow for every player. They are read through the owner instead.
/// </para>
///
/// <para>
/// actionTimer and lastTrainedSkill are likewise shared with the task system, and
/// chompTreeAnimationTime with woodcutting. Untangling those is a later pass.
/// </para>
/// </summary>
public class PlayerCombat
{
    private readonly PlayerController owner;

    /// <summary>Timestamp of the last heal, used to rate limit healing. Combat only, so it moved.</summary>
    private float lastHeal;

    public PlayerCombat(PlayerController owner)
    {
        this.owner = owner;
    }

    public bool Attack(PlayerController player)
    {
        if (player == owner)
        {
            Shinobytes.Debug.LogError(player.PlayerName + ", You cant fight yourself :o");
            return false;
        }
        if (player == null || !player)
        {
            return false;
        }

        return AttackEntity(player, true);
    }
    public bool Attack(EnemyController enemy)
    {
        if (enemy == null || !enemy) return false;
        return AttackEntity(enemy);
    }

    public bool Heal(PlayerController target)
    {
        if (target == null || !target) return false;
        return AttackEntity(target);
    }

    private bool AttackEntity(IAttackable target, bool damageOnDraw = false)
    {
        owner.LastExecutedTaskTime = Time.time;
        if (target == null)
        {
            return false;
        }

        if (!owner || owner.isDestroyed)
        {
            return false;
        }
        var targetTransform = target.Transform;
        if (!targetTransform || target.Transform == null)
        {
            return false;
        }

        if (owner.Stats.IsDead)
        {
            return true;
        }

        owner.Target = targetTransform;
        var attackType = GetAttackType();
        owner.InCombat = true;
        owner.HealthRegeneration.ResetTimer();
        owner.ActionTimer = GetAttackAnimationTime(attackType);

        var hitTime = owner.ActionTimer / 2;

        if (owner.TrainingHealing)
        {
            if (Time.time - lastHeal < hitTime)
            {
                return true;
            }

            lastHeal = Time.time;
        }

        owner.Movement.Lock();

        owner.Equipment.ShowWeapon(attackType);

        var weapon = owner.Inventory.GetEquipmentOfCategory(ItemCategory.Weapon);
        var weaponAnim = owner.TrainingHealing ? 6 : owner.TrainingMagic ? 4 : owner.TrainingRanged ? 3 : weapon?.GetAnimation() ?? 0;
        var attackAnimation = owner.TrainingHealing || owner.TrainingMagic || owner.TrainingRanged ? 0 : weapon?.GetAnimationCount() ?? 4;

        if (!owner.playerAnimations.IsAttacking() || !owner.LastTrainedSkill.IsCombatSkill())
        {
            owner.LastTrainedSkill = owner.ActiveSkill.IsCombatSkill() ? owner.ActiveSkill : Skill.Attack;
            owner.playerAnimations.StartCombat(weaponAnim, owner.Equipment.HasShield);
            if (!damageOnDraw) return true;
        }

        owner.playerAnimations.Attack(weaponAnim, UnityEngine.Random.Range(0, attackAnimation), owner.Equipment.HasShield);

        owner._transform.LookAt(owner.Target);

        var startTime = Time.time;

        if (owner.TrainingHealing)
        {
            ActionSystem.Run(() => HealTarget(target, hitTime, startTime));
            return true;
            //StartCoroutine(HealTarget(target, hitTime, startTime));
        }

        ActionSystem.Run(() => DamageEnemy(target, hitTime, startTime));
        //StartCoroutine(DamageEnemy(target, hitTime, startTime));
        return true;
    }

    private float GetAttackAnimationTime(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Healing:
                return owner.HealingAnimationTime / owner.GetModifiers().CastSpeedMultiplier;
            case AttackType.Ranged:
                return owner.RangeAnimationTime / owner.GetModifiers().AttackSpeedMultiplier;
            case AttackType.Magic:
                return owner.MagicAnimationTime / owner.GetModifiers().CastSpeedMultiplier;
            default:
                return owner.AttackAnimationTime / owner.GetModifiers().AttackSpeedMultiplier;
        }
    }

    public AttackType GetAttackType()
    {
        if (owner.TrainingHealing) return AttackType.Healing;
        if (owner.TrainingRanged) return AttackType.Ranged;
        if (owner.TrainingMagic) return AttackType.Magic;
        return AttackType.Melee;
    }

    public bool HealTarget(IAttackable target, float hitTime, float startTime)
    {
        owner.LastExecutedTaskTime = Time.time;
        var delta = Time.time - startTime;
        if (delta < hitTime) return false;
        try
        {
            if (target == null || !target.Transform || target.GetStats().IsDead)
                return true;

            var maxHeal = GameMath.MaxHit(owner.Stats.Healing.MaxLevel, owner.EquipmentStats.BaseMagicPower);
            var heal = CalculateDamage(target);
            if (!target.Heal(heal))
                return true;

            owner.SessionStats.AddHealingDealt(heal);

            // allow for some variation in gains based on how high you heal.
            var state = ExpGainState.FullGain;
            var factor = (1 + (heal / maxHeal * 0.2)) *
                ((owner.raidHandler.InRaid || owner.dungeonHandler.InDungeon) ? 1.0 : owner.Chunk?.CalculateExpFactor(owner, out state) ?? 1.0);

            owner.SetExpGainState(state, owner.Stats.Healing);

            if (owner.AutoTrainTargetLevel <= 0 || owner.AutoTrainTargetLevel > owner.Stats.Healing.Level)
                owner.AddExp(Skill.Healing, factor);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to heal target: " + exc.Message);
        }
        finally
        {
            owner.InCombat = false;
        }
        return true;
    }

    public bool DamageEnemy(IAttackable enemy, float hitTime, float startTime)
    {
        owner.LastExecutedTaskTime = Time.time;
        var delta = Time.time - startTime;
        if (delta < hitTime) return false;
        if (enemy == null) return true;

        if (owner.TrainingRanged)
        {
            owner.effectHandler.DestroyProjectile();
        }

        var damage = CalculateDamage(enemy);

        owner.SessionStats.AddDamageDealt(damage);

        if (enemy == null || !enemy.TakeDamage(owner, damage))
            return true;

        owner.SessionStats.IncrementEnemiesKilled();
        //Statistics.TotalDamageDone += damage;

        var isPlayer = enemy is PlayerController playerController;
        var enemyController = enemy as EnemyController;

        try
        {
            var isMonster = enemyController != null;
            if (isMonster && owner.Island)
                owner.Island.Statistics.MonstersDefeated++;

            if (!enemy.GivesExperienceWhenKilled)
                return true;

            // give all attackers exp for the kill, not just the one who gives the killing blow.
            foreach (PlayerController player in enemy.GetAttackers())
            {
                if (player == null || !player || player.isDestroyed)
                    continue;

                //var combatExperience = enemy.GetExperience();
                var activeSkill = player.ActiveSkill;
                if (activeSkill.IsCombatSkill())
                {
                    //activeSkill = Skill.Health; // ALL
                    var state = ExpGainState.FullGain;
                    var factor = owner.dungeonHandler.InDungeon ? 1d : owner.Chunk?.CalculateExpFactor(player, out state) ?? 1d;

                    player.SetExpGainState(state, null);

                    if (isMonster)
                    {
                        factor *= System.Math.Max(1.0d, enemyController.ExpFactor);
                    }

                    if (player.AutoTrainTargetLevel <= 0 || player.AutoTrainTargetLevel > player.GetSkill(activeSkill).Level)
                        player.AddExp(activeSkill, factor);
                }
            }
        }
        finally
        {
            owner.InCombat = false;
        }
        return true;
    }


    public bool DamageTree(TreeController tree, float startTime)
    {
        try
        {
            owner.LastExecutedTaskTime = Time.time;
            var delta = Time.time - startTime;
            var actionTime = owner.ChompTreeAnimationTime / 2f;
            if (delta < actionTime)
                return false;

            var damage = CalculateDamage(tree);
            if (!tree.DoDamage(owner, damage))
                return true;

            owner.SessionStats.IncrementTreeCutDown();

            if (owner.Island)
                owner.Island.Statistics.TreesCutDown++;

            // give all attackers exp for the kill, not just the one who gives the killing blow.
            foreach (var player in tree.WoodCutters)
            {
                if (player == null || !player || player.isDestroyed)
                {
                    continue;
                }

                //++player.Statistics.TotalTreesCutDown;

                var factor = owner.Chunk.CalculateExpFactor(player, out var state);

                player.SetExpGainState(state, player.Stats.Woodcutting);

                if (player.AutoTrainTargetLevel <= 0 || player.AutoTrainTargetLevel > player.Stats.Woodcutting.Level)
                    player.AddExp(Skill.Woodcutting, factor);// tree.Experience);
                                                             //var amount = (int)(tree.Resource * Mathf.FloorToInt(player.Stats.Woodcutting.CurrentValue / 10f));
                                                             //player.Statistics.TotalWoodCollected += amount;
            }
            return true;
        }
        catch (Exception exc)
        {
            var pos = tree.Position;
            Shinobytes.Debug.LogError($"Unable to damage tree ({tree.name} at x{pos.x} y{pos.y} z{pos.z}): " + exc.Message);
            return false;
        }
    }

    private int CalculateDamage(IAttackable enemy)
    {
        if (owner == null || enemy == null) return 0;
        if (owner.TrainingHealing)
            return (int)GameMath.CalculateHealing(owner, enemy);

        if (owner.TrainingMagic)
            return (int)GameMath.CalculateMagicDamage(owner, enemy);

        if (owner.TrainingRanged)
            return (int)GameMath.CalculateRangedDamage(owner, enemy);

        return (int)GameMath.CalculateMeleeDamage(owner, enemy);
    }

    private int CalculateDamage(TreeController enemy)
    {
        return (int)GameMath.CalculateSkillDamage(owner.Stats.Woodcutting, enemy.Level);
    }
}
