using System.Collections.Generic;
using UnityEngine;

public interface IAttackable
{
    string Name { get; }
    bool GivesExperienceWhenKilled { get; }
    bool InCombat { get; }
    bool TakeDamage(IAttackable attacker, int damage);
    bool Heal(int amount);
    IReadOnlyList<IAttackable> GetAttackers();
    Skills GetStats();
    EquipmentStats GetEquipmentStats();
    int GetCombatStyle();
    Transform Target { get; }
    Transform Transform { get; }
    Vector3 Position { get; }
    float HealthBarOffset { get; }
    float GetHitRange();
    StatsModifiers GetModifiers();
}