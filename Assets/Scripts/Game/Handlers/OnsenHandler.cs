using RavenNest.Models;
using System;
using UnityEngine;

public class OnsenHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private OnsenController activeOnsen;
    private OnsenPositionType positionType;
    private int onsenParentID;

    public bool InOnsen;
    public Vector3 EntryPoint => activeOnsen.EntryPoint;

    public const double RestedGainFactor = 2.0;
    public const double RestedDrainFactor = 1.0;

    public void Poll()
    {
        var rested = player.Rested;
        if (rested.RestedTime > 0)
        {
            if (rested.ExpBoost == 0)
                rested.ExpBoost = 2;
        }
        else if (rested.ExpBoost > 0)
        {
            rested.ExpBoost = 0;
        }

        // check if we have auto rest on
        if (player.Rested.AutoRestTarget.HasValue && 
            player.Rested.AutoRestStart.HasValue && 
            player.Rested.AutoRestTarget.Value > 0 &&
            !player.dungeonHandler.InDungeon && !player.raidHandler.InRaid &&
            !player.duelHandler.InDuel && !player.arenaHandler.InArena && !player.streamRaidHandler.InWar)
        {
            var autoRestTarget = player.Rested.AutoRestTarget.Value;
            var autoRestStart = player.Rested.AutoRestStart.Value;
            var restedMinutes = player.Rested.RestedTime / 60;
            if (restedMinutes <= player.Rested.AutoRestStart && !InOnsen)
            {
                player.GameManager.Onsen.Join(player);
                return;
            }
            else if (InOnsen && restedMinutes >= player.Rested.AutoRestTarget)
            {
                player.GameManager.Onsen.Leave(player);
                return;
            }
        }

        if (!InOnsen)
        {


            if (rested.RestedTime > 0)
            {
                rested.RestedTime -= GameTime.deltaTime * RestedDrainFactor;
            }
            return;
        }

        if (!this.transform.parent || activeOnsen == null || player.InCombat)
        {
            this.InOnsen = false;
            return;
        }

        if (this.transform.parent.GetInstanceID() != onsenParentID)
        {
            this.InOnsen = false;
            return;
        }

        rested.RestedTime += GameTime.deltaTime * RestedGainFactor;
        if (rested.RestedTime >= CharacterRestedState.RestedTimeMax)
        {
            rested.RestedTime = CharacterRestedState.RestedTimeMax;
        }

        if (this.InOnsen)
        {
            // force player position
            player.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    public void Enter(OnsenController onsen)
    {
        var spot = onsen.GetNextAvailableSpot();
        if (spot == null)
        {
            player.GameManager.RavenBot.SendReply(player, Localization.MSG_ONSEN_FULL);
            return;
        }

        var target = spot.Target;

        player.taskTarget = null;
        player.Movement.Lock();
        // player.teleportHandler.Teleport(target.position);
        player._transform.SetParent(target);
        player._transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        player.InCombat = false;
        player.ClearAttackers();
        player.Island = player.GameManager.Islands.FindPlayerIsland(player);
        if (player.Island)
        {
            player.Island.AddPlayer(player);
        }

        // used for determing which animation to use
        this.positionType = spot.Type;
        this.activeOnsen = onsen;
        switch (positionType)
        {
            case OnsenPositionType.Sitting:
                player.Animations.Sit();
                break;

            case OnsenPositionType.Swimming:
                player.Animations.Swim();
                break;

            case OnsenPositionType.Meditating:
                player.Animations.Meditate();
                break;

            case OnsenPositionType.Sleeping:
                player.Animations.Sleep();
                break;
        }

        this.onsenParentID = target.GetInstanceID();

        InOnsen = true;
        onsen.UpdateDetailsLabel();
    }

    public void Exit()
    {
        var prevOnsen = activeOnsen;
        activeOnsen = null;

        player.Animations.ClearOnsenAnimations();
        onsenParentID = -1;

        if (InOnsen)
        {
            player.Movement.Unlock();
            player.transform.SetParent(null);
            player.teleportHandler.Teleport(prevOnsen.EntryPoint, true, true);
        }

        prevOnsen.UpdateDetailsLabel();
        InOnsen = false;
    }

    internal void ClearAutoRest()
    {
        player.Rested.AutoRestTarget = null;
        player.Rested.AutoRestStart = null;
    }

    internal void SetAutoRest(int autoRestStart, int autoRestStop)
    {
        player.Rested.AutoRestStart = autoRestStart;
        player.Rested.AutoRestTarget = autoRestStop;
    }
}
