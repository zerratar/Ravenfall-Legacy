﻿using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private SyntyPlayerAppearance appearance;
    private Animator defaultAnimator;
    private Animator activeAnimator;
    private PlayerAnimationState animationState;
    private float idleTimer;

    public bool IsMoving;
    private bool hasAnimator;

    public float CastSpeedMultiplier = 1;
    public float MovementSpeedMultiplier = 1;
    public float AttackSpeedMultiplier = 1;
    public float LastTrigger;
    public PlayerAnimationState State => animationState;

    bool isCaptain;
    private float last_MovementSpeedMultiplier;
    private float last_CastSpeedMultiplier;
    private float last_AttackSpeedMultiplier;


    // Use this for initialization
    void Start()
    {
        if (appearance == null)
            appearance = GetComponent<SyntyPlayerAppearance>();
    }

    // Update is called once per frame
    public void Poll()
    {
        if (!EnsureAnimator())
        {
            return;
        }

        idleTimer += GameTime.deltaTime;

        if (animationState != PlayerAnimationState.Idle)
        {
            idleTimer = 0f;
        }

        if (idleTimer >= 5f)
        {
            TriggerBored();
            idleTimer = 0f;
        }

        if (MovementSpeedMultiplier > 0 && last_MovementSpeedMultiplier != MovementSpeedMultiplier)
        {
            activeAnimator.SetFloat("MovementSpeedMultiplier", MovementSpeedMultiplier);
            last_MovementSpeedMultiplier = MovementSpeedMultiplier;
        }

        if (CastSpeedMultiplier > 0 && last_CastSpeedMultiplier != CastSpeedMultiplier)
        {
            activeAnimator.SetFloat("CastSpeedMultiplier", CastSpeedMultiplier);
            last_CastSpeedMultiplier = CastSpeedMultiplier;
        }

        if (AttackSpeedMultiplier > 0 && last_AttackSpeedMultiplier != AttackSpeedMultiplier)
        {
            activeAnimator.SetFloat("AttackSpeedMultiplier", AttackSpeedMultiplier);
            last_AttackSpeedMultiplier = AttackSpeedMultiplier;
        }
    }

    #region General

    public void ResetActiveAnimator()
    {
        activeAnimator = defaultAnimator;
    }

    public void SetActiveAnimator(Animator animator)
    {
        activeAnimator = animator;
        for (var i = 0; i < defaultAnimator.parameterCount; ++i)
        {
            var parameter = defaultAnimator.parameters[i];
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                    activeAnimator.SetBool(parameter.name, defaultAnimator.GetBool(parameter.name));
                    break;
                case AnimatorControllerParameterType.Float:
                    activeAnimator.SetFloat(parameter.name, defaultAnimator.GetFloat(parameter.name));
                    break;
                case AnimatorControllerParameterType.Int:
                    activeAnimator.SetInteger(parameter.name, defaultAnimator.GetInteger(parameter.name));
                    break;
                case AnimatorControllerParameterType.Trigger:
                    activeAnimator.SetBool(parameter.name, defaultAnimator.GetBool(parameter.name));
                    break;
            }
        }
        //activeAnimator.playbackTime = defaultAnimator.playbackTime;
        ResetAnimationStates();
    }

    private void SetTrigger(string trigger)
    {
        activeAnimator.SetTrigger(trigger);
        if (activeAnimator != defaultAnimator)
            defaultAnimator.SetTrigger(trigger);
        LastTrigger = GameTime.time;
    }

    private void SetInteger(string key, int value)
    {
        activeAnimator.SetInteger(key, value);
        if (activeAnimator != defaultAnimator)
            defaultAnimator.SetInteger(key, value);
    }

    private void SetBool(string key, bool value)
    {
        activeAnimator.SetBool(key, value);
        if (activeAnimator != defaultAnimator)
            defaultAnimator.SetBool(key, value);
    }
    public void ForceCheer()
    {
        if (!EnsureAnimator()) return;
        SetTrigger("ForceCheer");
    }

    public void Death()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Dead;
        SetBool("Dead", true);
        SetTrigger("Died");
        StopMoving(true);
    }

    public void SetCaptainState(bool isCaptain)
    {
        if ((this.isCaptain == isCaptain) &&
            (isCaptain && animationState == PlayerAnimationState.Captain)
            || (!isCaptain && animationState != PlayerAnimationState.Captain)
            || !EnsureAnimator())
            return;

        if (isCaptain)
        {
            animationState = PlayerAnimationState.Captain;
            StopMoving(true);
        }

        if (this.isCaptain != isCaptain)
        {
            SetBool("Captain", isCaptain);
            this.isCaptain = isCaptain;
        }
    }

    public void Revive()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Dead", false);
        StopMoving(true);
    }

    private void TriggerBored()
    {
        if (!EnsureAnimator()) return;
        if (animationState == PlayerAnimationState.Idle)
            SetTrigger("Bored");
    }
    public void Cheer()
    {
        if (!EnsureAnimator()) return;
        SetTrigger("Cheer");
    }
    internal void Sit()
    {
        if (!EnsureAnimator()) return;
        SetBool("Sitting", true);
    }
    internal void Swim()
    {
        if (!EnsureAnimator()) return;
        SetBool("Swimming", true);
    }
    internal void Meditate()
    {
        if (!EnsureAnimator()) return;
        SetBool("Meditating", true);
    }

    internal void Sleep()
    {
        if (!EnsureAnimator()) return;
        SetBool("Sleeping", true);
    }
    internal void ClearOnsenAnimations()
    {
        if (!EnsureAnimator()) return;
        SetBool("Sitting", false);
        SetBool("Swimming", false);
        SetBool("Meditating", false);
        SetBool("Sleeping", false);
    }

    public void StartMoving(bool force = false)
    {
        if ((!force && IsMoving) || !EnsureAnimator()) return;
        SetBool("Moving", true);
        IsMoving = true;
    }
    public void StopMoving(bool force = false)
    {
        if ((!force && !IsMoving) || !EnsureAnimator()) return;
        SetBool("Moving", false);
        IsMoving = false;
    }
    #endregion

    #region Combat
    public void StartCombat(int weapon, bool hasShield)
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Attacking;
        ResetAnimationStates();
        SetTrigger("Start");
        SetInteger("Weapon", weapon);
        SetBool("Attacking", true);
        SetBool("Shield", hasShield);
    }

    public bool IsAttacking()
    {
        if (!EnsureAnimator()) return false;
        return animationState == PlayerAnimationState.Attacking ||
               activeAnimator.GetBool("Attacking");
    }

    public void Attack(int weapon, int action = 0, bool hasShield = false)
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Attacking;
        SetInteger("Weapon", weapon);
        SetInteger("Action", action);
        SetBool("Attacking", true);
        SetBool("Shield", hasShield);
        SetTrigger("Attack");
    }

    public void EndCombat()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Attacking", false);
    }
    #endregion

    #region Mining
    public void StartMining()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        activeAnimator.SetTrigger("Start");
        Mine();
    }
    public void Mine()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Mining;
        activeAnimator.SetBool("Mining", true);
    }
    public void EndMining()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        activeAnimator.SetBool("Mining", false);
    }
    #endregion

    #region Woodcutting
    public void StartWoodcutting()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();

        animationState = PlayerAnimationState.Woodcutting;

        SetTrigger("Start");
        SetBool("Woodcutting", true);
    }

    public void Chop(int animation)
    {
        if (!EnsureAnimator()) return;
        SetBool("Woodcutting", true);
        SetInteger("Action", animation);
        SetTrigger("Chop");
    }

    public void EndWoodcutting()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Woodcutting", false);
    }
    #endregion

    #region Fishing
    public void StartFishing()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        SetTrigger("Start");
        Fish();
    }

    public void Fish()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Fishing;
        SetBool("Fishing", true);
    }

    public void EndFishing()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Fishing", false);
    }
    #endregion

    #region Crafting
    public void StartCrafting()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        SetTrigger("Start");
        Craft();
    }

    public void Craft()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Crafting;
        SetBool("Crafting", true);
    }

    public void EndCrafting()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Crafting", false);
    }

    #endregion

    #region Alchemy
    public void StartBrewing()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        animationState = PlayerAnimationState.Brewing;
        SetTrigger("Start");
        SetBool("Brewing", true);
    }

    public void Brew()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Brewing;
        SetBool("Brewing", true);
    }


    public void EndBrewing()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Brewing", false);
    }
    #endregion

    #region Farming
    public void StartFarming()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        animationState = PlayerAnimationState.Farming;
        SetTrigger("Start");
        SetBool("Farming", true);
    }

    public void EndFarming()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Farming", false);
    }
    #endregion

    #region Gathering
    public void StartGathering(bool kneeling = true)
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        animationState = PlayerAnimationState.Gathering;
        SetInteger("Action", kneeling ? 0 : 1);
        SetTrigger("Start");
        SetBool("Gathering", true);
    }

    public void Gather(bool kneeling = true)
    {
        if (!EnsureAnimator()) return;
        SetBool("Gathering", true);
        SetInteger("Action", kneeling ? 0 : 1);
        SetTrigger("Start");
        SetTrigger("Gather");
    }

    public void EndGathering()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Gathering", false);
    }
    #endregion

    #region Cooking
    public void StartCooking()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        animationState = PlayerAnimationState.Cooking;
        SetTrigger("Start");
        SetBool("Cooking", true);
    }

    public void EndCooking()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Cooking", false);
    }
    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EnsureAnimator()
    {
        if (hasAnimator)
        {
            return true;
        }

        if (appearance == null)
            appearance = GetComponent<SyntyPlayerAppearance>();

        if (appearance is MonoBehaviour behaviour)
        {
            //  && !appearance.AppearanceUpdated()
            if (activeAnimator) return true;
            if (!behaviour || !behaviour.transform) return false;
            activeAnimator = behaviour.transform.GetComponentInChildren<Animator>();
            defaultAnimator = activeAnimator;
        }
        hasAnimator = activeAnimator != null;
        return activeAnimator;
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
        EndGathering();
        EndBrewing();
    }

}

public enum PlayerAnimationState
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
    Captain,
    Gathering,
    Brewing,
    Brewing_Fail,
    Brewing_Success
}
