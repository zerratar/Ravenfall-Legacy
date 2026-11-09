using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
///     Plays a battle pet's clips straight from its model, without its animator controller: the pets come from enemies
///     and pet items whose controllers all have different states and parameters (docs/battle-pets-design.md, section 9).
///     The clips are found by name (idle, run or walk, attack, die, sleep) and cross faded in a small playable mixer.
///     A model without a clip for something simply keeps the clip it has, and code moves it instead (the pounce, the hop).
/// </summary>
public sealed class PetClipPlayer
{
    public enum Kind { Idle, Move, Attack, Hit, Down, Sleep }

    private readonly Dictionary<Kind, AnimationClip> clips = new Dictionary<Kind, AnimationClip>();
    private readonly List<AnimationClipPlayable> playables = new List<AnimationClipPlayable>();
    private readonly Dictionary<AnimationClip, int> inputs = new Dictionary<AnimationClip, int>();
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private int current = -1;
    private float[] weights;
    private float fadeSpeed = 8f;
    private float oneShotUntil;
    private Kind oneShotReturn;
    private Kind playing = Kind.Idle;

    public bool Valid => graph.IsValid();
    public bool Has(Kind kind) => clips.ContainsKey(kind);
    public float Length(Kind kind) => clips.TryGetValue(kind, out var c) && c ? c.length : 0f;

    /// <param name="bolt">A pet that fires bolts from afar: a projectile or spell clip suits it better than a bite.</param>
    public PetClipPlayer(Animator animator, AnimationClip[] available, bool bolt = false)
    {
        if (!animator || available == null || available.Length == 0) return;

        // "W Root" clips move the model themselves (the cute series has both); "In Place" ones are what code moves.
        // " to " clips are the moves between two states ("Rest to Idle"), not the states.
        Pick(Kind.Idle, available, new[] { "idle_a", "idle01", "idle_1", "idle" }, new[] { "sit", "eat", "lie", " to ", "happy" });
        Pick(Kind.Move, available, new[] { "run forward", "run_forward", "run", "walk forward", "walk_forward", "walk", "move", "jump_walk",
            "fly forward", "slither", "roll forward", "forward" }, new[] { "left", "right", "back", "w root", "attack", "dash" });
        if (bolt) Pick(Kind.Attack, available, new[] { "projectile attack", "projectile", "fireball", "cast spell", "spell" }, new[] { "w root", "upward", "trajectory", "low" });
        if (!clips.ContainsKey(Kind.Attack))
            Pick(Kind.Attack, available, new[] { "bite", "stab", "attack_01", "attack-r1", "attack", "fireball" }, new[] { "w root", "long range" });
        Pick(Kind.Hit, available, new[] { "damage_01", "damage", "gethit", "hit" }, new string[0]);
        Pick(Kind.Down, available, new[] { "die", "death" }, new string[0]);
        Pick(Kind.Sleep, available, new[] { "sleep" }, new[] { "begin", "end", "eyes" });
        if (!clips.ContainsKey(Kind.Idle) && clips.TryGetValue(Kind.Move, out var move)) clips[Kind.Idle] = move;
        if (clips.Count == 0) return;

        animator.applyRootMotion = false;
        animator.fireEvents = false; // the clips call their old scripts (footsteps, "Running"), which are gone
        animator.runtimeAnimatorController = null; // the graph drives it now
        graph = PlayableGraph.Create("BattlePet");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var output = AnimationPlayableOutput.Create(graph, "Pet", animator);
        var distinct = new List<AnimationClip>();
        foreach (var c in clips.Values) if (!distinct.Contains(c)) distinct.Add(c);
        mixer = AnimationMixerPlayable.Create(graph, distinct.Count);
        for (var i = 0; i < distinct.Count; ++i)
        {
            var p = AnimationClipPlayable.Create(graph, distinct[i]);
            p.SetApplyFootIK(false);
            graph.Connect(p, 0, mixer, i);
            playables.Add(p);
            inputs[distinct[i]] = i;
        }
        weights = new float[distinct.Count];
        output.SetSourcePlayable(mixer);
        graph.Play();
        Play(Kind.Idle, 0f);
    }

    private void Pick(Kind kind, AnimationClip[] available, string[] names, string[] avoid)
    {
        foreach (var name in names)
        {
            AnimationClip best = null;
            foreach (var clip in available)
            {
                if (!clip) continue;
                var n = clip.name.ToLowerInvariant();
                if (!n.Contains(name)) continue;
                var skip = false;
                foreach (var a in avoid) if (n.Contains(a)) { skip = true; break; }
                if (skip) continue;
                if (best == null || clip.name.Length < best.name.Length) best = clip;
            }
            if (best != null) { clips[kind] = best; return; }
        }
    }

    /// <summary>Loops a clip (fading over <paramref name="fade"/> seconds); a one shot playing finishes first unless forced.</summary>
    public void Play(Kind kind, float fade = 0.15f, bool force = false)
    {
        if (!Valid) return;
        if (!force && Time.time < oneShotUntil) { oneShotReturn = kind; return; }
        if (!clips.TryGetValue(kind, out var clip)) return;
        var index = inputs[clip];
        playing = kind;
        if (index == current) return;
        current = index;
        playables[index].SetTime(0);
        playables[index].SetSpeed(1);
        fadeSpeed = fade <= 0.001f ? 1000f : 1f / fade;
    }

    /// <summary>Plays a clip once, then goes back to <paramref name="then"/>; false when the model has no such clip.</summary>
    public bool PlayOnce(Kind kind, Kind then, float speed = 1f)
    {
        if (!Valid || !clips.TryGetValue(kind, out var clip)) return false;
        oneShotUntil = 0f;
        Play(kind, 0.08f, true);
        var p = playables[inputs[clip]];
        p.SetTime(0);
        p.SetSpeed(speed);
        oneShotUntil = Time.time + clip.length / Mathf.Max(0.1f, speed) * 0.95f;
        oneShotReturn = then;
        return true;
    }

    /// <summary>Plays a clip once and stays on its last frame (knocked out).</summary>
    public bool Hold(Kind kind)
    {
        if (!Valid || !clips.TryGetValue(kind, out var clip)) return false;
        oneShotUntil = float.MaxValue;
        Play(kind, 0.1f, true);
        var p = playables[inputs[clip]];
        p.SetTime(0);
        p.SetSpeed(1);
        p.SetDuration(clip.length);
        return true;
    }

    public void Release(Kind then)
    {
        oneShotUntil = 0f;
        foreach (var p in playables) p.SetDuration(double.MaxValue);
        Play(then, 0.2f, true);
    }

    public void Tick()
    {
        if (!Valid || weights == null) return;
        if (oneShotUntil > 0f && oneShotUntil < float.MaxValue && Time.time >= oneShotUntil)
        {
            oneShotUntil = 0f;
            Play(oneShotReturn, 0.15f, true);
        }
        var dt = Time.deltaTime * fadeSpeed;
        var total = 0f;
        for (var i = 0; i < weights.Length; ++i)
        {
            weights[i] = Mathf.MoveTowards(weights[i], i == current ? 1f : 0f, dt);
            total += weights[i];
        }
        for (var i = 0; i < weights.Length; ++i) mixer.SetInputWeight(i, total > 0 ? weights[i] / total : (i == current ? 1f : 0f));
    }

    public void Dispose()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
