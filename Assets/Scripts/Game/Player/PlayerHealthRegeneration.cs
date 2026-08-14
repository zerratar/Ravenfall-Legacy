using UnityEngine;

/// <summary>
/// Passive out of combat health regeneration for a single player.
///
/// <para>
/// This is not the heal over time applied by status effects, which lives with the effect handling
/// and shares its own tick state. This is the slow refill that happens while a player is not
/// fighting.
/// </para>
///
/// <para>
/// Split out of PlayerController as a plain class rather than a component, for the same reason as
/// PlayerProgression: a player already carries 26 components and sessions reach a thousand players,
/// so every extra component is another Unity object the engine has to track.
/// </para>
///
/// <para>
/// This one is also the clearest candidate for a data oriented rewrite later. It runs for every
/// player every frame and touches nothing but a few floats and the health stat, so a single system
/// walking packed arrays would do the same work with far better cache behaviour than a thousand
/// objects each ticking themselves. Isolating the state here is the prerequisite for that, which is
/// why the timers now live in this class instead of on the controller.
/// </para>
/// </summary>
public class PlayerHealthRegeneration
{
    private readonly PlayerController player;

    /// <summary>Seconds spent out of combat, counted up towards <see cref="PlayerController.RegenTime"/>.</summary>
    private float regenTimer;

    /// <summary>Fractional health carried between frames, so slow regeneration is not rounded away.</summary>
    private float regenAmount;

    public PlayerHealthRegeneration(PlayerController player)
    {
        this.player = player;
    }

    /// <summary>
    /// Restarts the out of combat delay. Called when the player takes or deals damage.
    /// </summary>
    public void ResetTimer()
    {
        regenTimer = 0;
    }

    /// <summary>
    /// Advances regeneration by one frame. Driven from PlayerController rather than by an engine
    /// callback, so this costs a direct call rather than a managed to native transition.
    /// </summary>
    public void Poll()
    {
        try
        {
            if (player.isDestroyed || player.Removed)
            {
                // player removed.
                return;
            }
        }
        catch
        {
            // ignored
            return;
        }

        try
        {
            var stats = player.Stats;

            if ((player.Chunk?.ChunkType != TaskType.Fighting) && !player.InCombat)
            {
                regenTimer += GameTime.deltaTime;
            }

            if (regenTimer >= player.RegenTime)
            {
                var oldValue = stats.Health.CurrentValue;

                var amount = stats.Health.MaxLevel * player.RegenRate * GameTime.deltaTime;
                regenAmount += amount;
                var add = Mathf.FloorToInt(regenAmount);
                if (add > 0)
                {
                    var newValue = Mathf.Min(stats.Health.MaxLevel, stats.Health.CurrentValue + add);
                    stats.Health.CurrentValue = newValue;
                    regenAmount -= add;

                    var healthBar = player.HealthBar;
                    if (healthBar && healthBar != null && oldValue != newValue)
                    {
                        healthBar.UpdateHealth();
                    }
                }

                if (stats.Health.CurrentValue == stats.Health.MaxLevel)
                {
                    regenTimer = 0;
                }
            }
        }
        catch
        {
            // ignored
        }
    }
}
