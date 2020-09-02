using System.Collections.Generic;
using UnityEngine;

public interface IAttackable
{
    string Name { get; }
    bool GivesExperienceWhenKilled { get; }
    bool InCombat { get; }
    bool TakeDamage(IAttackable attacker, int damage);
    IReadOnlyList<IAttackable> GetAttackers();
    Skills GetStats();
    EquipmentStats GetEquipmentStats();
    int GetCombatStyle();
    Transform Target { get; }
    Transform Transform { get; }
    decimal GetExperience();
    float HealthBarOffset { get; }
}