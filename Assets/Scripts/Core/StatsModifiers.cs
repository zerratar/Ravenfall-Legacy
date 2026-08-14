public class StatsModifiers
{
    public StatsModifiers()
    {
        Reset();
    }

    // Base stats
    public float DodgeChance;
    public float ExpMultiplier;
    public float MovementSpeedMultiplier;
    public float AttackSpeedMultiplier;
    public float CastSpeedMultiplier;
    public float StrengthMultiplier;
    public float DefenseMultiplier;
    public float RangedPowerMultiplier;
    public float MagicPowerMultiplier;
    public float HealingPowerMultiplier;
    public float AttackPowerMultiplier;
    public float HitChanceMultiplier;

    // New additions
    public float CriticalHitChance;
    public float CriticalHitDamage;


    // Attack attributes
    public float AttackAttributePoisonEffect;
    public float AttackAttributeBleedingEffect;
    public float AttackAttributeBurningEffect;
    public float AttackAttributeHealthStealEffect;

    // Damage over time effects
    public float PoisonEffect;  // Represents damage per tick, you can handle application separately
    public float BleedingEffect;  // Represents damage per tick
    public float BurningEffect;  // Represents damage per tick

    public void Reset()
    {
        DodgeChance = 0;
        ExpMultiplier = 1;
        MovementSpeedMultiplier = 1;
        AttackSpeedMultiplier = 1;
        CastSpeedMultiplier = 1;
        StrengthMultiplier = 1;
        DefenseMultiplier = 1;
        RangedPowerMultiplier = 1;
        MagicPowerMultiplier = 1;
        HealingPowerMultiplier = 1;
        AttackPowerMultiplier = 1;
        HitChanceMultiplier = 1;

        // Reset new additions
        CriticalHitChance = 0;

        AttackAttributePoisonEffect = 0;
        AttackAttributeBleedingEffect = 0;
        AttackAttributeBurningEffect = 0;
        AttackAttributeHealthStealEffect = 0;

        PoisonEffect = 0;
        BleedingEffect = 0;
        BurningEffect = 0;
    }
}
