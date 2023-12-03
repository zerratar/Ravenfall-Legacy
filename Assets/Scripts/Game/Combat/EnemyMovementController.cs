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

    private ConcurrentDictionary<string, AnimatorControllerParameter> parameters
        = new ConcurrentDictionary<string, AnimatorControllerParameter>();

    private bool lastMove;
    [HideInInspector] public bool isMovingInternal;

    internal Vector3 Destination;

    private NavMeshPath currentPath;

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
            .FirstOrDefault();

        hasMoveAnimation = moveAnimation != null;

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
    }
    private void OnBecameInvisible()
    {
        animator.enabled = false;
    }

    private void Update()
    {
        if (GameCache.IsAwaitingGameRestore) return;

        var moving = isMovingInternal;//IsMoving();

        if (moveAnimation != null && lastMove != moving && animator.enabled)
        {
            if (moveAnimation.type == AnimatorControllerParameterType.Float)
                animator.SetFloat(moveAnimation.name, moving ? 1f : 0);
            else
                animator.SetBool(moveAnimation.name, moving);
            lastMove = moving;
        }

        if (attackAnimationTimer > 0f)
            attackAnimationTimer -= GameTime.deltaTime;

        if (attackAnimationTimer < 0f)
        {
            attackAnimationTrigger?.Invoke();
            attackAnimationTimer = 0f;
        }
    }

    public void Lock()
    {
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
    public void Unlock()
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
            agent.enabled = true;
            agent.isStopped = false;
        }
    }

    internal void SetPosition(Vector3 position)
    {
        if (!navMeshAgent) navMeshAgent = GetComponent<NavMeshAgent>();
        var agent = navMeshAgent;
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
    }

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
            animator.SetBool(deathAnimation, true);
        }
        else
        {
            deathAnimationLength = 0f;
        }

        if (this.isActiveAndEnabled)
        {
            StartCoroutine(Hide());
        }
    }

    private IEnumerator Hide()
    {
        yield return new WaitForSeconds(deathAnimationLength);
        SetVisibility(false);
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
    }

    public void ResetVisibility()
    {
        SetVisibility(true);
        if (animator.enabled)
        {
            if (!string.IsNullOrEmpty(deathAnimation))
            {
                animator.SetBool(deathAnimation, false);
            }
        }
    }

    public void Revive()
    {
        SetVisibility(true);
        if (animator.enabled)
        {
            if (!string.IsNullOrEmpty(deathAnimation))
            {
                animator.SetBool(deathAnimation, false);
            }

            if (HasParameter("revive"))
            {
                animator.SetTrigger("revive");
            }
        }

        Unlock();
    }

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