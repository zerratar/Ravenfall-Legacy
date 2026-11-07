using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     One kind of enemy (combat v2, docs/combat-v2-design.md section 6). The data is RavenNest.Combat's
///     <see cref="EnemyDefinition"/>, shared with the simulator; this asset holds it in the project, and every enemy of
///     the kind points at it (EnemyController.Definition). Its levels replace what the scene has, so the numbers live
///     in one place and can no longer be lost from the scene file.
/// </summary>
[CreateAssetMenu(menuName = "Ravenfall/Enemy Definition", fileName = "Enemy")]
public class EnemyDefinitionAsset : ScriptableObject
{
    public EnemyDefinition Definition = new EnemyDefinition();

    /// <summary>Skills at the definition's levels, Health full.</summary>
    public Skills CreateSkills()
    {
        var d = Definition;
        var skills = new Skills();
        skills.Attack.Set(d.Attack, 0, false);
        skills.Strength.Set(d.Strength, 0, false);
        skills.Defense.Set(d.Defense, 0, false);
        skills.Health.Set(d.Health, 0, false);
        skills.Ranged.Set(d.Ranged, 0, false);
        skills.Magic.Set(d.Magic, 0, false);
        skills.Healing.Set(d.Healing, 0, false);
        return skills;
    }

    /// <summary>The definition's power and accuracy on its own style, and its armour, as equipment numbers.</summary>
    public EquipmentStats CreateEquipment()
    {
        var d = Definition;
        var eq = new EquipmentStats { BaseArmorPower = d.Armor };
        switch (d.Style)
        {
            case AttackStyle.Ranged:
                eq.BaseRangedPower = d.Power;
                eq.BaseRangedAim = d.Accuracy;
                break;
            case AttackStyle.Magic:
                eq.BaseMagicPower = d.Power;
                eq.BaseMagicAim = d.Accuracy;
                break;
            case AttackStyle.Healing:
                eq.BaseHealingPower = d.Power;
                break;
            default:
                eq.BaseWeaponPower = d.Power;
                eq.BaseWeaponAim = d.Accuracy;
                break;
        }
        return eq;
    }
}

/// <summary>A fighter with an enemy definition: the combat rules take its element, resistances and boss flag from it.</summary>
public interface IHasEnemyDefinition
{
    EnemyDefinition CombatDefinition { get; }
}
