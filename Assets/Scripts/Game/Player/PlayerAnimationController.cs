using System;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private IPlayerAppearance appearance;
    private Animator animator;
    private PlayerAnimationState animationState;
    private float idleTimer;

    // Use this for initialization
    void Start()
    {
        if (appearance == null)
            appearance = (IPlayerAppearance)GetComponent<SyntyPlayerAppearance>() ?? GetComponent<PlayerAppearance>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!EnsureAnimator())
        {
            return;
        }

        idleTimer += Time.deltaTime;

        if (animationState != PlayerAnimationState.Idle)
        {
            idleTimer = 0f;
        }

        if (idleTimer >= 5f)
        {
            TriggerBored();
            idleTimer = 0f;
        }
    }

    #region General

    public void ForceCheer()
    {
        if (!EnsureAnimator()) return;
        animator.SetTrigger("ForceCheer");
    }

    public void Death()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Dead;
        animator.SetBool("Dead", true);
    }

    public void SetCaptainState(bool isCaptain)
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Captain;
        animator.SetBool("Captain", isCaptain);
    }

    public void Revive()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        animator.SetBool("Dead", false);
    }

    private void TriggerBored()
    {
        if (!EnsureAnimator()) return;
        if (animationState == PlayerAnimationState.Idle)
            animator.SetTrigger("Bored");
    }

    public void Cheer()
    {
        if (!EnsureAnimator()) return;
        animator.SetTrigger("Cheer");
    }

    public void StartMoving()
    {
        if (!EnsureAnimator()) return;
        animator.SetBool("Moving", true);
    }

    public void StopMoving()
    {
        if (!EnsureAnimator()) return;
        animator.SetBool("Moving", false);
    }
    #endregion

    #region Combat
    public void StartCombat(int weapon)
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Attacking;
        ResetAnimationStates();
        animator.SetTrigger("Start");
        animator.SetInteger("Weapon", weapon);
        animator.SetBool("Attacking", true);
    }

    public bool IsAttacking()
    {
        if (!EnsureAnimator()) return false;
        return animationState == PlayerAnimationState.Attacking ||
               animator.GetBool("Attacking");
    }

    public void Attack(int weapon, int action = 0)
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Attacking;
        animator.SetInteger("Weapon", weapon);
        animator.SetInteger("Action", action);
        animator.SetBool("Attacking", true);
        animator.SetTrigger("Attack");
    }

    public void EndCombat()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        animator.SetBool("Attacking", false);
    }
    #endregion

    #region Mining
    public void StartMining()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        animator.SetTrigger("Start");
        Mine();
    }
    public void Mine()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Mining;
        animator.SetBool("Mining", true);
    }
    public void EndMining()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        animator.SetBool("Mining", false);
    }
    #endregion

    #region Woodcutting
    public void StartWoodcutting()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Woodcutting;
        ResetAnimationStates();
        animator.SetTrigger("Start");
        animator.SetBool("Woodcutting", true);
    }

    public void Chop(int animation)
    {
        if (!EnsureAnimator()) return;
        animator.SetBool("Woodcutting", true);
        animator.SetInteger("Action", animation);
        animator.SetTrigger("Chop");
    }

    public void EndWoodcutting()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        animator.SetBool("Woodcutting", false);
    }
    #endregion

    #region Fishing
    public void StartFishing()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        animator.SetTrigger("Start");
        Fish();
    }

    public void Fish()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Fishing;
        animator.SetBool("Fishing", true);
    }

    public void EndFishing()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        animator.SetBool("Fishing", false);
    }
    #endregion

    #region Crafting
    public void StartCrafting()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        animator.SetTrigger("Start");
        Craft();
    }

    public void Craft()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Crafting;
        animator.SetBool("Crafting", true);
    }

    public void EndCrafting()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        animator.SetBool("Crafting", false);
    }
    #endregion

    #region Farming
    public void StartFarming()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Farming;
        ResetAnimationStates();
        animator.SetTrigger("Start");
        animator.SetBool("Farming", true);
    }

    public void EndFarming()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        animator.SetBool("Farming", false);
    }
    #endregion

    #region Farming
    public void StartCooking()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Cooking;
        ResetAnimationStates();
        animator.SetTrigger("Start");
        animator.SetBool("Cooking", true);
    }

    public void EndCooking()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        animator.SetBool("Cooking", false);
    }
    #endregion

    private bool EnsureAnimator()
    {
        if (appearance is MonoBehaviour behaviour)
        {
            //  && !appearance.AppearanceUpdated()
            if (animator) return true;
            if (!behaviour || !behaviour.transform) return false;
            animator = behaviour.transform.GetComponentInChildren<Animator>();
        }

        return animator;
    }

    public void ResetAnimationStates()
    {
        EndCombat();
        EndCooking();
        EndCrafting();
        EndFarming();
        EndFishing();
        EndMining();
        EndWoodcutting();
    }
}

internal enum PlayerAnimationState
{
    Idle,
    Dead,
    Attacking,
    Mining,
    Crafting,
    Woodcutting,
    Cooking,
    Fishing,
    Farming,
    Drinking,
    Eating,
    Captain
}
