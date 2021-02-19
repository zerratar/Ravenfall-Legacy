using Assets.Scripts;
using Pathfinding;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class WalkyWalkyScript : MonoBehaviour
{
    [SerializeField] private float minDestinationDistance = 1f;
    [SerializeField] private float attackAnimationLength = 0.22f;
    [SerializeField] private float deathAnimationLength = 3f;
    [SerializeField] private EnemyController enemyController;

    private Animator animator;
    private NavMeshAgent navMeshAgent;
    //private AIPath aiPathAgent;

    private SkinnedMeshRenderer meshRenderer;
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

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        //aiPathAgent = GetComponent<AIPath>();
        //if (!aiPathAgent)
        //{
        navMeshAgent = GetComponent<NavMeshAgent>();
        //}

        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        moveAnimation = animator.parameters
            .Where(x => x.name.IndexOf("Walking", StringComparison.OrdinalIgnoreCase) >= 0 || x.name.IndexOf("Movement", StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault();

        hasMoveAnimation = moveAnimation != null;

        if (!enemyController) enemyController = GetComponent<EnemyController>();

        SetupDeathAnimation();
        SetupAttackAnimation();
        Lock();
    }

    private bool IsMoving()
    {
        if (!hasMoveAnimation)
            return false;

        if (navMeshAgent && navMeshAgent.enabled)
            return navMeshAgent.remainingDistance > minDestinationDistance;

        //if (aiPathAgent && aiPathAgent.enabled)
        //    return aiPathAgent.remainingDistance > minDestinationDistance;

        return false;
    }

    private void Update()
    {
        if (GameCache.Instance.IsAwaitingGameRestore) return;
        var moving = IsMoving();
        if (moveAnimation != null && lastMove != moving)
        {
            if (moveAnimation.type == AnimatorControllerParameterType.Float)
                animator.SetFloat(moveAnimation.name, moving ? 1f : 0);
            else
                animator.SetBool(moveAnimation.name, moving);
            lastMove = moving;
        }

        if (attackAnimationTimer > 0f)
            attackAnimationTimer -= Time.deltaTime;

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

        var agent = navMeshAgent;
        if (agent && agent.enabled)
        {
            agent.SetDestination(transform.position);
            agent.isStopped = true;
            agent.enabled = false;
        }
    }
    public void Unlock()
    {
        //if (aiPathAgent)
        //{
        //    aiPathAgent.enabled = true;
        //    return;
        //}

        var agent = navMeshAgent;
        if (agent)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }
    }

    public void SetDestination(Vector3 pos)
    {
        //if (aiPathAgent)
        //{
        //    aiPathAgent.destination = pos;
        //    return;
        //}

        var agent = navMeshAgent;

        if (agent && agent.isActiveAndEnabled && !agent.isOnNavMesh)
        {
            Debug.LogError(this.name + " is not on a navmesh!!!");
        }

        if (!agent || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        agent.SetDestination(pos);
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
        if (deathAnimations.Length > 0)
        {
            var index = UnityEngine.Random.Range(0, deathAnimations.Length);
            deathAnimation = deathAnimations[index];
            animator.SetBool(deathAnimation, true);
        }
        else
        {
            deathAnimationLength = 0f;
        }

        StartCoroutine(Hide());
    }

    private IEnumerator Hide()
    {
        yield return new WaitForSeconds(deathAnimationLength);
        if (meshRenderer) meshRenderer.enabled = false;
    }


    public void Attack(Action onAnimationTrigger)
    {
        if (attackAnimations.Length > 0)
        {
            attackAnimation = attackAnimations.Random();
            if (HasParameter(attackAnimation))
            {
                animator.SetTrigger(attackAnimation);
            }
        }
        attackAnimationTrigger = onAnimationTrigger;
        attackAnimationTimer = attackAnimationLength;
    }

    public void Revive()
    {
        if (meshRenderer) meshRenderer.enabled = true;
        if (!string.IsNullOrEmpty(deathAnimation))
        {
            animator.SetBool(deathAnimation, false);
        }

        if (HasParameter("revive"))
        {
            animator.SetTrigger("revive");
        }

        Unlock();
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