using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
///     What a tower boss does in its intro (TowerManager, the first time a stream fights it): one clip per boss model, on
///     the asset Resources/Tower/Boss Taunts. Most enemy controllers have no taunt of their own, so the clip plays over the
///     controller for a while (<see cref="Play"/>) and the controller takes over again after. A model with no taunt here
///     winds up a big swing instead: its attack, slowed down.
/// </summary>
[CreateAssetMenu(menuName = "Ravenfall/Tower Boss Taunts")]
public sealed class TowerBossTaunts : ScriptableObject
{
    [Serializable]
    public sealed class Taunt
    {
        /// <summary>The boss model's enemy definition id (RavenNest.Combat.TowerBoss.Model).</summary>
        public string Model;
        public AnimationClip Clip;
        public float Speed = 1f;

        /// <summary>How long it plays once; a looping clip goes on until it is stopped.</summary>
        public float Seconds => Clip ? Clip.length / Mathf.Max(0.05f, Speed) : 0f;
        public bool Loops => Clip && Clip.isLooping;
    }

    public Taunt[] Taunts = Array.Empty<Taunt>();

    private static TowerBossTaunts loaded;

    public static TowerBossTaunts Get()
    {
        if (!loaded) loaded = Resources.Load<TowerBossTaunts>("Tower/Boss Taunts");
        return loaded;
    }

    public Taunt For(string model)
    {
        if (Taunts == null || string.IsNullOrEmpty(model)) return null;
        foreach (var t in Taunts) if (t != null && t.Clip && string.Equals(t.Model, model, StringComparison.OrdinalIgnoreCase)) return t;
        return null;
    }

    /// <summary>
    ///     A taunt playing on a boss: <see cref="Stop"/> hands the boss back to its controller. Null when the boss has no
    ///     animator.
    /// </summary>
    public sealed class Playing
    {
        private PlayableGraph graph;
        private Animator animator;
        private float speedWas = 1f;
        private float ends = float.MaxValue;

        /// <summary>Played through: the controller can take the boss back.</summary>
        public bool Done => Time.time >= ends;

        public void Stop()
        {
            if (graph.IsValid()) graph.Destroy();
            if (animator) animator.speed = speedWas;
            animator = null;
        }

        internal static Playing Clip(Animator animator, AnimationClip clip, float speed)
        {
            var playing = new Playing { animator = animator, speedWas = animator.speed };
            var playable = AnimationPlayableUtilities.PlayClip(animator, clip, out playing.graph);
            playable.SetSpeed(Mathf.Max(0.05f, speed));
            if (!clip.isLooping) playing.ends = Time.time + clip.length / Mathf.Max(0.05f, speed);
            // the graph goes with the boss if nobody stops it first
            animator.gameObject.AddComponent<Owner>().Playing = playing;
            return playing;
        }

        internal static Playing Swing(Animator animator)
        {
            // no taunt of its own: its attack, slow, as a big wind-up
            var playing = new Playing { animator = animator, speedWas = animator.speed, ends = Time.time + 1.8f };
            foreach (var p in animator.parameters)
            {
                if (p.type != AnimatorControllerParameterType.Trigger || p.name.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) < 0) continue;
                animator.SetTrigger(p.name);
                animator.speed = 0.45f;
                break;
            }
            return playing;
        }
    }

    private sealed class Owner : MonoBehaviour
    {
        public Playing Playing;
        private void OnDestroy() => Playing?.Stop();
    }

    /// <summary>
    ///     The boss's death clip, straight away (its controller finishes a swing first), held on its last frame: its fall
    ///     in slow motion, as slow as <paramref name="slowest"/> but done within <paramref name="seconds"/> (a dragon's
    ///     long dramatic death plays at its own pace). Null when it has no animator or no death clip.
    /// </summary>
    public static Playing Fall(EnemyController boss, float slowest, float seconds)
    {
        var animator = boss ? boss.GetComponentInChildren<Animator>() : null;
        var controller = animator ? animator.runtimeAnimatorController : null;
        if (!controller) return null;
        AnimationClip death = null;
        foreach (var c in controller.animationClips)
        {
            if (!c) continue;
            var n = c.name.ToLowerInvariant();
            if (n.Contains("death") || n.Contains("die")) { death = c; break; }
        }
        if (!death) return null;
        var speed = Mathf.Clamp(death.length / Mathf.Max(0.5f, seconds), slowest, 1f);
        try
        {
            var movement = boss.GetComponent<EnemyMovementController>();
            if (movement) movement.StopHolding();
            if (!animator.enabled) animator.enabled = true;
            return Playing.Clip(animator, death, speed);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerBossTaunts.Fall: " + exc.Message);
            return null;
        }
    }

    /// <summary>The taunt for <paramref name="model"/> on <paramref name="boss"/>, or its slowed attack when it has none.</summary>
    public static Playing Play(EnemyController boss, string model)
    {
        if (!boss) return null;
        var animator = boss.GetComponentInChildren<Animator>();
        if (!animator || !animator.isActiveAndEnabled) return null;
        var taunt = Get() ? Get().For(model) : null;
        try
        {
            return taunt != null ? Playing.Clip(animator, taunt.Clip, taunt.Speed) : Playing.Swing(animator);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerBossTaunts: " + exc.Message);
            return null;
        }
    }
}
