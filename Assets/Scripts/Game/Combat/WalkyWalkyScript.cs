using Assets.Scripts;
using System;
using System.Collections;
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
    private SkinnedMeshRenderer meshRenderer;
    private bool hasMoveAnimation;
    private AnimatorControllerParameter moveAnimation;


    private string[] attackAnimations;
    private string attackAnimation;

    private string[] deathAnimations;
    private string deathAnimation;

    private Action attackAnimationTrigger;
    private float attackAnimationTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        moveAnimation = animator.parameters
            .Where(x => x.name.IndexOf("Walking", StringComparison.OrdinalIgnoreCase) >= 0 || x.name.IndexOf("Movement", StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault();

        hasMoveAnimation = moveAnimation != null;

        if (!enemyController) enemyController = GetComponent<EnemyController>();

        SetupDeathAnimation();
        SetupAttackAnimation();
    }
    private void Update()
    {
        if (GameCache.Instance.IsAwaitingGameRestore) return;
        var moving = navMeshAgent && navMeshAgent.enabled && hasMoveAnimation && navMeshAgent.remainingDistance > minDestinationDistance;
        if (moveAnimation != null)
        {
            if (moveAnimation.type == AnimatorControllerParameterType.Float)
                animator.SetFloat(moveAnimation.name, moving ? 1f : 0);
            else
                animator.SetBool(moveAnimation.name, moving);
        }

        if (attackAnimationTimer > 0f)
            attackAnimationTimer -= Time.deltaTime;

        if (attackAnimationTimer < 0f)
        {
            attackAnimationTrigger?.Invoke();
            attackAnimationTimer = 0f;
        }
    }

    private void SetupAttackAnimation()
    {
        if (!animator)
        {
            return;
        }

        attackAnimations = animator.parameters
            .Where(x => x.name.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0)
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

    private void Lock()
    {
        if (enemyController)
        {
            enemyController.Lock();
        }
        else
        {
            navMeshAgent.SetDestination(this.transform.position);
            navMeshAgent.enabled = false;
        }
    }

    private void Unlock()
    {
        if (enemyController)
        {
            enemyController.Unlock();
        }
        else
        {
            navMeshAgent.enabled = true;
        }
    }

    public void Attack(Action onAnimationTrigger)
    {
        if (attackAnimations.Length > 0)
        {
            attackAnimation = attackAnimations.Random();
            if (animator)
                animator.SetTrigger(attackAnimation);
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

        if (animator.parameters.FirstOrDefault(x => x.name == "revive") != null)
        {
            animator.SetTrigger("revive");
        }

        Unlock();
    }

    //private void DamageAnimationTrigger()
    //{
    //    attackAnimationTrigger?.Invoke();
    //}
}