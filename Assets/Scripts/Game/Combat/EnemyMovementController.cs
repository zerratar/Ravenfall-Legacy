using Assets.Scripts;
using Shinobytes.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovementController : MonoBehaviour
{
    public float minDestinationDistance = 1f;
    public float attackAnimationLength = 0.22f;
    public float deathAnimationLength = 3f;
    public EnemyController enemyController;
    public NavMeshAgent navMeshAgent;

    private Animator animator;
    private DungeonBossController dungeonBoss;
    //private AIPath aiPathAgent;

    private Renderer[] renderers;
    private LODGroup lodGroup;

    private bool hasMoveAnimation;
    private AnimatorControllerParameter moveAnimation;


    private string[] attackAnimations;
    private string attackAnimation;

    private string[] deathAnimations;
    private string deathAnimation;

    private Action attackAnimationTrigger;
    private float attackAnimationTimer;

    /// <summary>A blow is wound up and has not come down yet.</summary>
    public bool Swinging => attackAnimationTimer > 0f;

    /// <summary>
    ///     The body stays after the death has played, until whoever made it destroys it (a tower boss: its fall is shown
    ///     in slow motion, TowerManager).
    /// </summary>
    [NonSerialized] public bool KeepBody;

    private Coroutine holding;

    /// <summary>Someone else holds the kept body's pose now (a clip played over the controller): the animator stays on.</summary>
    public void StopHolding()
    {
        if (holding != null) StopCoroutine(holding);
        holding = null;
    }

    private ConcurrentDictionary<string, AnimatorControllerParameter> parameters
        = new ConcurrentDictionary<string, AnimatorControllerParameter>();

    private float lastMove;
    [HideInInspector] public bool isMovingInternal;

    /// <summary>Wandering at a walk: a float move parameter gets 0.5 (the Humanoid Enemy's walk) rather than 1 (its run).</summary>
    [NonSerialized] public bool Ambling;

    /// <summary>
    ///     Holds its fighting stance between swings whatever its target does (a tower boss for its whole fight). Without
    ///     it the stance follows the fight: on while it has someone to fight, off when it goes back to idling.
    /// </summary>
    [NonSerialized] public bool CombatStance;

    // the Humanoid Enemy's Combat float: its Idle blends from the relaxed idle (0) to the fighting stance (1), so every
    // swing comes back to the stance instead of the plain idle (seen on every humanoid tower boss, 2026-11-26)
    private static readonly int CombatId = Animator.StringToHash("Combat");
    private bool hasCombatStance;
    private float stance;
    private float stanceSet = -1f;

    internal Vector3 Destination;

    private NavMeshPath currentPath;

    /// <summary>
    /// Set on enemies placed in the scene, whose agent is saved disabled by EnemyAgentsWaitForNavMesh: the
    /// world's navmesh is added by its NavMeshSurface once the scene has loaded, after Unity creates the agents,
    /// so enabled ones logged "Failed to create agent because there is no valid NavMesh" on every start.
    /// </summary>
    [HideInInspector] public bool EnableAgentOnStart;

    private void Start()
    {
        // by Start every OnEnable has run, the NavMeshSurface's included
        if (navMeshAgent) navMeshAgent.areaMask = NavMeshHelper.AreaMaskAt(transform.position);
        if (EnableAgentOnStart && navMeshAgent) navMeshAgent.enabled = true;
        EnableAgentOnStart = false;
    }

    private void OnDestroy()
    {
        parameters.Clear();
        parameters = null;
        enemyController = null;
        navMeshAgent = null;
        animator = null;
        attackAnimations = null;
        attackAnimation = null;
        deathAnimations = null;
        deathAnimation = null;
        attackAnimationTrigger = null;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        //aiPathAgent = GetComponent<AIPath>();
        //if (!aiPathAgent)
        //{
        navMeshAgent = GetComponent<NavMeshAgent>();
        //}

        renderers = GetComponentsInChildren<Renderer>().Where(x => x.enabled && x.gameObject.activeInHierarchy).ToArray();

        lodGroup = GetComponent<LODGroup>();

        moveAnimation = animator.parameters
            .Where(x => x.name.IndexOf("Walking", StringComparison.OrdinalIgnoreCase) >= 0 || x.name.IndexOf("Movement", StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault()
            // rabbits hop about (their Walking state plays on isJumping)
            ?? animator.parameters.FirstOrDefault(x => x.name == "isJumping");

        hasMoveAnimation = moveAnimation != null;
        foreach (var p in animator.parameters)
            if (p.nameHash == CombatId && p.type == AnimatorControllerParameterType.Float) hasCombatStance = true;

        if (!enemyController) enemyController = GetComponent<EnemyController>();
        if (!dungeonBoss) dungeonBoss = GetComponent<DungeonBossController>();

        SetupDeathAnimation();
        SetupAttackAnimation();
        //Lock();
    }

    private bool IsMoving()
    {
        if (!hasMoveAnimation)
            return false;

        if (navMeshAgent && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            return navMeshAgent.remainingDistance > minDestinationDistance;

        return false;
    }

    private void OnBecameVisible()
    {
        animator.enabled = true;
        lastMove = -1f; // a disabled animator lost its parameters: set the move again
        stanceSet = -1f;
    }
    private void OnBecameInvisible()
    {
        animator.enabled = false;
    }

    public void Poll()
    {
        if (GameCache.IsAwaitingGameRestore) return;

        // at the end of its path it stops: only Lock ended the walk, so a wander or the way back to the spawn left
        // the enemy walking on the spot. The path goes too, or the agent (stopping distance 0) circles the point,
        // gliding in its idle pose; nor may it coast on once the walk has stopped
        if (isMovingInternal && navMeshAgent && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh
            && !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.1f)
        {
            Stop();
        }

        var move = isMovingInternal ? (Ambling ? 0.5f : 1f) : 0f;

        if (moveAnimation != null && lastMove != move && animator.enabled)
        {
            if (moveAnimation.type == AnimatorControllerParameterType.Float)
                animator.SetFloat(moveAnimation.name, move);
            else
                animator.SetBool(moveAnimation.name, move > 0f);
            lastMove = move;
        }

        if (hasCombatStance && animator.enabled) UpdateStance();

        if (attackAnimationTimer > 0f)
            attackAnimationTimer -= GameTime.deltaTime;

        if (attackAnimationTimer < 0f)
        {
            attackAnimationTrigger?.Invoke();
            attackAnimationTimer = 0f;
        }
    }

    // into the stance and out of it over a quarter of a second, so a swing's target changing does not flicker it
    private void UpdateStance()
    {
        var e = enemyController;
        var fighting = e && e.Stats != null && !e.Stats.IsDead && (CombatStance || (e.InCombat && e.Target));
        stance = Mathf.MoveTowards(stance, fighting ? 1f : 0f, GameTime.deltaTime * 4f);
        if (stance == stanceSet) return;
        animator.SetFloat(CombatId, stance);
        stanceSet = stance;
    }

    /// <summary>Stands still where it is, the agent left on (Lock turns it off).</summary>
    public void Stop()
    {
        isMovingInternal = false;
        if (navMeshAgent && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
            navMeshAgent.velocity = Vector3.zero;
        }
    }

    public void Lock()
    {
        EnableAgentOnStart = false; // locked before Start: stays locked
        //if (aiPathAgent)
        //{
        //    aiPathAgent.destination = transform.position;
        //    aiPathAgent.enabled = false;
        //    return;
        //}

        if (!navMeshAgent) navMeshAgent = GetComponent<NavMeshAgent>();
        var agent = navMeshAgent;

        Destination = transform.position;

        if (agent)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(transform.position);
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }
        this.isMovingInternal = false;
    }
    public void Unlock(bool adjustToNavMesh = false)
    {
        //if (aiPathAgent)
        //{
        //    aiPathAgent.enabled = true;
        //    return;
        //}

        if (!navMeshAgent) navMeshAgent = GetComponent<NavMeshAgent>();
        var agent = navMeshAgent;
        if (agent)
        {
            NavMeshHelper.UseAreasAt(agent, transform.position);
            if (adjustToNavMesh && !agent.isOnNavMesh)
            {
                AdjustPlayerPositionToNavmesh();
            }

            agent.enabled = true;
            agent.isStopped = false;
        }
    }

    public void AdjustPlayerPositionToNavmesh()
    {
        if (navMeshAgent.isOnNavMesh) return;
        NavMeshHit closestHit;
        if (NavMesh.SamplePosition(gameObject.transform.position, out closestHit, 500f, NavMeshHelper.AreaMaskAt(gameObject.transform.position)))
        {
            var pos = closestHit.position;

            gameObject.transform.position = pos;

            var agent = navMeshAgent;
            if (agent)
            {
                agent.Warp(pos);
            }
        }
    }
    internal void SetPosition(Vector3 position)
    {
        if (!navMeshAgent) navMeshAgent = GetComponent<NavMeshAgent>();
        var agent = navMeshAgent;
        if (agent) agent.areaMask = NavMeshHelper.AreaMaskAt(position);
        if (agent && agent.enabled)
        {
            agent.Warp(position);
        }
        else
        {
            this.transform.position = position;
        }
    }


    public bool SetDestination(Vector3 pos)
    {
        Unlock();

        if (!navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh)
        {
            return true;
        }

        if (Destination == pos)
        {
            return true;
        }

        if (currentPath == null)
            currentPath = new NavMeshPath();

        if (navMeshAgent.CalculatePath(pos, currentPath) && currentPath.status == NavMeshPathStatus.PathComplete)
        {
            Destination = pos;
            navMeshAgent.SetPath(currentPath);
            this.isMovingInternal = true;
            return true;
        }

        return false;
    }

    private void SetupAttackAnimation()
    {
        if (!animator)
        {
            return;
        }

        attackAnimations = animator.parameters
            .Where(x => x.name.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0 && x.name != "AttackType")
            .Select(x => x.name)
            .ToArray();

        // a controller with no attack clip at all (the home bunny has no attack trigger, the cow's goes nowhere): its
        // swings are lunges (EnemyLunge)
        var controller = animator.runtimeAnimatorController;
        if (controller)
            foreach (var clip in controller.animationClips)
                if (clip && clip.name.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0) { hasAttackClip = true; break; }
    }

    private bool hasAttackClip;

    /// <summary>Its controller has an attack clip to play (else its swings are lunges, EnemyLunge).</summary>
    public bool HasAttackClip => hasAttackClip;

    private void SetupDeathAnimation()
    {
        if (!animator)
        {
            return;
        }

        deathAnimations = animator.parameters
            .Where(x => x.name.Contains("isDead") || x.name.Contains("Die") || x.name.Contains("DeathTrigger"))
            .Select(x => x.name)
            .ToArray();
    }

    public void Die()
    {
        Lock();
        PlayDeathAnimation();
    }

    private void PlayDeathAnimation()
    {
        if (deathAnimations.Length > 0 && animator.enabled)
        {
            var index = UnityEngine.Random.Range(0, deathAnimations.Length);
            deathAnimation = deathAnimations[index];
            // the Humanoid Enemy controller's Die is a trigger: SetBool on it does nothing and the enemy just vanished
            if (IsTrigger(deathAnimation)) animator.SetTrigger(deathAnimation);
            else animator.SetBool(deathAnimation, true);
        }
        else
        {
            deathAnimationLength = 0f;
        }

        if (this.isActiveAndEnabled)
        {
            if (KeepBody) holding = StartCoroutine(Hold());
            else StartCoroutine(Hide());
        }
    }

    private bool IsTrigger(string parameter)
    {
        foreach (var p in animator.parameters)
            if (p.name == parameter) return p.type == AnimatorControllerParameterType.Trigger;
        return false;
    }

    /// <summary>Takes the death back: a trigger is reset, a bool cleared.</summary>
    private void ClearDeath()
    {
        if (string.IsNullOrEmpty(deathAnimation)) return;
        if (IsTrigger(deathAnimation)) animator.ResetTrigger(deathAnimation);
        else animator.SetBool(deathAnimation, false);
    }

    /// <summary>
    /// Hidden once the death has played: the death clip may start late (an attack finishes first) and the animator
    /// leaves it by itself, so this follows the animator rather than a fixed time (deathAnimationLength plus two
    /// seconds at most). Without a death animation, after deathAnimationLength.
    /// </summary>
    private IEnumerator Hide()
    {
        var waited = 0f;
        var sawDeath = false;
        var limit = deathAnimationLength + 2f;
        while (waited < limit && deathAnimations != null && deathAnimations.Length > 0 && animator && animator.enabled)
        {
            var dying = Playing(animator.GetCurrentAnimatorClipInfo(0));
            if (dying) sawDeath = true;
            else if (sawDeath) break;
            // done when the death is near its end or the animator starts to move on from it
            if (dying && (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f || (animator.IsInTransition(0) && !Playing(animator.GetNextAnimatorClipInfo(0))))) break;
            waited += Time.deltaTime;
            yield return null;
        }
        if (!sawDeath && waited < deathAnimationLength) yield return new WaitForSeconds(deathAnimationLength - waited);
        SetVisibility(false);
    }

    /// <summary>
    /// The body that stays (<see cref="KeepBody"/>) lies still where it fell: the animator would leave the death by itself
    /// and stand it up again, so it stops once the death is near its end.
    /// </summary>
    private IEnumerator Hold()
    {
        var waited = 0f;
        var sawDeath = false;
        while (waited < 15f && animator && animator.enabled)
        {
            var dying = Playing(animator.GetCurrentAnimatorClipInfo(0));
            if (dying) sawDeath = true;
            if (dying && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.92f) break;
            if (sawDeath && (!dying || (animator.IsInTransition(0) && !Playing(animator.GetNextAnimatorClipInfo(0))))) break;
            waited += Time.deltaTime;
            yield return null;
        }
        if (animator) animator.enabled = false;
        holding = null;
    }

    private static bool Playing(AnimatorClipInfo[] clips)
    {
        foreach (var c in clips)
            if (c.clip && c.clip.name.IndexOf("death", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }


    public void Attack(Action onAnimationTrigger)
    {
        if (attackAnimations.Length > 0 && animator.enabled)
        {
            attackAnimation = attackAnimations.Random();
            if (HasParameter(attackAnimation))
            {
                animator.SetTrigger(attackAnimation);
            }

            if (dungeonBoss)
            {
                animator.SetInteger("AttackType", UnityEngine.Random.Range(0, 4));
            }
        }
        attackAnimationTrigger = onAnimationTrigger;
        attackAnimationTimer = attackAnimationLength;

        // nothing to play: a lunge at the target instead, its blow at the far end
        if (!hasAttackClip && animator && animator.enabled && enemyController && enemyController.Target && !CombatEffects.CannotMove(enemyController))
            EnemyLunge.Play(this, animator, enemyController.Target, attackAnimationLength);
    }

    /// <summary>
    ///     The swing alone, with no blow behind it (a tower boss winding up its slam or spit): its attack clip, or for a
    ///     model with none a jump on the spot (<paramref name="slam"/>) or a lunge at its target, coming down
    ///     <see cref="SwingLead"/> seconds later.
    /// </summary>
    public void PlaySwing(bool slam)
    {
        if (!animator || !animator.enabled) return;
        if (hasAttackClip)
        {
            if (attackAnimations != null && attackAnimations.Length > 0)
            {
                var swing = attackAnimations.Random();
                if (HasParameter(swing)) animator.SetTrigger(swing);
                if (dungeonBoss) animator.SetInteger("AttackType", UnityEngine.Random.Range(0, 4));
            }
            return;
        }
        if (slam) EnemyLunge.Jump(this, animator, SwingLead(true));
        else if (enemyController && enemyController.Target) EnemyLunge.Play(this, animator, enemyController.Target, SwingLead(false));
    }

    /// <summary>Seconds from <see cref="PlaySwing"/> to its blow: the attack clip's, or the jump's (a slam) and the lunge's.</summary>
    public float SwingLead(bool slam) => hasAttackClip ? Mathf.Clamp(attackAnimationLength, 0.25f, 0.9f) : slam ? 0.75f : Mathf.Clamp(attackAnimationLength, 0.25f, 0.6f);

    public void ResetVisibility()
    {
        SetVisibility(true);
        if (animator.enabled)
        {
            ClearDeath();
        }
    }

    public void Revive()
    {
        SetVisibility(true);
        if (animator.enabled)
        {
            ClearDeath();

            if (HasParameter("revive"))
            {
                animator.SetTrigger("revive");
            }
        }

        Unlock();
    }

    /// <summary>The renderers to show and hide again, after the rig's shown variant changed (hero mode's waves turn
    /// a goblin rig into a skeleton or a warchief after it was made).</summary>
    public void RefreshRenderers() => renderers = GetComponentsInChildren<Renderer>(false);

    private void SetVisibility(bool value)
    {
        if (lodGroup)
        {
            // lodGroup.enabled = value;
            foreach (var lod in lodGroup.GetLODs())
            {
                foreach (var renderer in lod.renderers)
                {
                    renderer.enabled = value;
                }
            }
        }
        if (renderers != null && renderers.Length > 0)
        {
            foreach (var mr in renderers)
            {
                mr.enabled = value;
            }
        }
    }

    private bool HasParameter(string attackAnimation)
    {
        if (!animator) return false;
        if (this.parameters.TryGetValue(attackAnimation, out var p))
        {
            if (p == null) return false;
            return true;
        }
        parameters[attackAnimation] = animator.parameters.FirstOrDefault(x => x.name == attackAnimation);
        return parameters[attackAnimation] != null;
    }

}