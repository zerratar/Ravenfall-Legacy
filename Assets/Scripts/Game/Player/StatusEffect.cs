using System;
using RavenNest.Models;

public class StatusEffect
{
    private CharacterStatusEffect effect;
    public CharacterStatusEffect Effect
    {
        get => effect;
        set
        {
            effect = value;
            TimeLeft = effect.TimeLeft;
        }
    }

    public float Amount => Effect.Amount;
    public StatusEffectType Type => Effect.Type;
    public double Duration => Effect.Duration;
    public double TimeLeft;
    public DateTime LastUpdateUtc;
    public bool Expired
    {
        get
        {
            if (TimeLeft <= 0)
                return true;

            var effectHasBeenApplied = LastUpdateUtc > DateTime.UnixEpoch;
            var oneTimeEffect = Effect.Type == StatusEffectType.TeleportToIsland ||
                                Effect.Type == StatusEffectType.Heal ||
                                Effect.Type == StatusEffectType.Damage;

            if (effectHasBeenApplied && oneTimeEffect)
                return true;

            return false;
        }
    }
}
