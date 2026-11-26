using UnityEngine;
using UnityEngine.AI;

/// <summary>
///     A swing for a model that has no attack clip (the home bunny of Nibbles, Snowball and Zap; the cow of the
///     Moo-tant): a short lunge at its target and back, its blow landing at the far end, holding a pose the model has
///     meanwhile (the bunny's jump up, the cow's run). Without it such a boss swung every 2.5 s while it stood in its
///     idle (bot playtest 2026-11-26). Added on the first swing; switched off between lunges, so it costs nothing then.
/// </summary>
public sealed class EnemyLunge : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private string pose;
    private Vector3 from;
    private Vector3 direction;
    private float reach;
    private float hop;
    private float time;
    private float peak;
    private float length;
    private bool jump;

    /// <summary>The bool the model holds during a lunge, or null: the first of these it has.</summary>
    private static readonly string[] Poses = { "isJumping_Up", "isRunning" };

    public static void Play(EnemyMovementController movement, Animator animator, Transform target, float hitAt)
    {
        if (!movement || !target) return;
        var lunge = movement.GetComponent<EnemyLunge>();
        if (!lunge)
        {
            lunge = movement.gameObject.AddComponent<EnemyLunge>();
            lunge.Setup(animator);
        }
        lunge.Begin(target.position, hitAt, false);
    }

    /// <summary>A jump on the spot that comes down at <paramref name="landsIn"/> seconds: a slam's windup.</summary>
    public static void Jump(EnemyMovementController movement, Animator animator, float landsIn)
    {
        if (!movement) return;
        var lunge = movement.GetComponent<EnemyLunge>();
        if (!lunge)
        {
            lunge = movement.gameObject.AddComponent<EnemyLunge>();
            lunge.Setup(animator);
        }
        lunge.Begin(movement.transform.position + movement.transform.forward, landsIn, true);
    }

    private void Setup(Animator a)
    {
        animator = a;
        agent = GetComponent<NavMeshAgent>();
        if (!a) return;
        foreach (var name in Poses)
        {
            foreach (var p in a.parameters)
            {
                if (p.name != name || p.type != AnimatorControllerParameterType.Bool) continue;
                pose = name;
                break;
            }
            if (pose != null) break;
        }
    }

    private void Begin(Vector3 target, float hitAt, bool jump)
    {
        // one at a time: a new swing starts from where the last one began
        if (enabled && time < length) transform.position = from;
        var to = target - transform.position;
        to.y = 0f;
        var distance = to.magnitude;
        if (distance < 0.05f) return;
        var scale = Mathf.Max(0.2f, transform.lossyScale.x);
        from = transform.position;
        direction = to / distance;
        transform.rotation = Quaternion.LookRotation(direction);
        // a good part of the way to the target, never into it; a jumper leaves the ground, the rest lean in. A jump (a
        // slam) goes up and comes down hard where it stood
        reach = jump ? 0f : Mathf.Clamp((distance - 0.5f) * 0.45f, 0.15f, 0.9f * scale);
        hop = jump ? (pose == "isJumping_Up" ? 0.5f : 0.25f) * scale : pose == "isJumping_Up" ? 0.22f * scale : 0f;
        this.jump = jump;
        peak = Mathf.Clamp(hitAt, 0.15f, jump ? 1.2f : 0.6f);
        length = peak + (jump ? 0.15f : 0.3f);
        time = 0f;
        if (pose != null && animator && animator.enabled) animator.SetBool(pose, true);
        enabled = true;
    }

    private void LateUpdate()
    {
        // the agent took it over (its target walked off): it moves from here
        if (agent && agent.enabled)
        {
            End(false);
            return;
        }
        time += Time.deltaTime;
        if (jump)
        {
            // up and hanging a moment, then down fast onto the blow
            var rise = time > peak ? 0f
                : time <= peak * 0.55f ? Mathf.Sin(time / (peak * 0.55f) * Mathf.PI * 0.5f)
                : 1f - Mathf.Pow(Mathf.Clamp01((time - peak * 0.55f) / (peak * 0.45f)), 2f);
            transform.position = from + Vector3.up * (hop * rise);
            if (time >= length) End(true);
            return;
        }
        // out fast to the blow, back a little slower
        var k = time <= peak ? Mathf.Sin(time / peak * Mathf.PI * 0.5f) : 1f - Mathf.SmoothStep(0f, 1f, (time - peak) / (length - peak));
        transform.position = from + direction * (reach * k) + Vector3.up * (hop * Mathf.Sin(Mathf.Clamp01(time / length) * Mathf.PI));
        if (time >= length) End(true);
    }

    private void End(bool home)
    {
        if (home) transform.position = from;
        if (pose != null && animator && animator.enabled) animator.SetBool(pose, false);
        time = length;
        enabled = false;
    }

    // switched off in the middle (the object hidden, despawned): back where it began
    private void OnDisable()
    {
        if (time < length) End(!(agent && agent.enabled));
    }
}
