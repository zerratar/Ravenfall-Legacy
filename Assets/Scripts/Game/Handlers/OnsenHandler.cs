using RavenNest.Models;
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

    private bool hasSetTransform;
    private Transform transformInternal;

    public bool IsAutoResting { get; set; }

    //public int AutoRestCost { get; set; } = 500;

    public bool AutoRestAvailable
    {
        get
        {
            var rested = player.Rested;

            return rested.AutoRestTarget.HasValue &&
            rested.AutoRestStart.HasValue &&
            rested.AutoRestTarget.Value > 0 &&
            // player cannot be on or waiting for the ferry, in dungeon, in raid, duel or arena
            !player.ferryHandler.OnFerry &&
            player.ferryHandler.State != PlayerFerryState.Embarking &&
            !player.dungeonHandler.InDungeon &&
            !player.raidHandler.InRaid &&
            !player.duelHandler.InDuel &&
            !player.arenaHandler.InArena &&
            !player.streamRaidHandler.InWar;
        }
    }

    public bool ShouldAutoRest
    {
        get
        {
            var rested = player.Rested;
            var autoRestStart = rested.AutoRestStart.Value;
            var restedMinutes = rested.RestedTime / 60;
            return restedMinutes <= rested.AutoRestStart
                && !InOnsen // if we are not already resting
                && player.GameManager.Onsen.RestingAreaAvailable(player.Island) &&
                player.Resources.Coins >= player.GameManager.SessionSettings.AutoRestCost;
        }
    }

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
        if (AutoRestAvailable)
        {
            var restedMinutes = rested.RestedTime / 60;
            if (ShouldAutoRest)
            {
                IsAutoResting = true;
                player.GameManager.Onsen.Join(player);

                return;
            }
            else if (InOnsen && restedMinutes >= rested.AutoRestTarget)
            {
                player.onsenHandler.Exit();
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

        if (!hasSetTransform)
        {
            hasSetTransform = true;
            transformInternal = this.transform;
        }

        var parent = transformInternal.parent;
        if (!parent || activeOnsen == null)
        {
            this.InOnsen = false;
            return;
        }

        if (parent.GetInstanceID() != onsenParentID)
        {
            this.InOnsen = false;
            return;
        }

        if (IsAutoResting)
        {
            var before = (int)rested.AutoRestTime;
            rested.AutoRestTime += GameTime.deltaTime;
            var after = (int)rested.AutoRestTime;
            if (after > before)
            {
                var delta = after - before;
                player.Resources.Coins -= delta * player.GameManager.SessionSettings.AutoRestCost;
            }
        }
        else
        {
            rested.AutoRestTime = 0;
        }

        rested.RestedTime += GameTime.deltaTime * RestedGainFactor;
        if (rested.RestedTime >= CharacterRestedState.RestedTimeMax)
        {
            rested.RestedTime = CharacterRestedState.RestedTimeMax;
        }

        if (this.InOnsen)
        {
            // force player position
            transformInternal.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            player.ClearTarget();
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
        player.ClearTarget();

        player.Island = player.GameManager.Islands.FindPlayerIsland(player);
        if (player.Island != null)
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
            player.transform.SetParent(null);
            player.teleportHandler.Teleport(prevOnsen.EntryPoint, true);
        }

        prevOnsen.UpdateDetailsLabel();
        InOnsen = false;
    }

    internal void ClearAutoRest()
    {
        var rested = player.Rested;
        rested.AutoRestTarget = null;
        rested.AutoRestStart = null;
    }

    internal bool SetAutoRest(int autoRestStart, int autoRestStop)
    {
        // reverse if they are set in the wrong order
        if (autoRestStart > autoRestStop)
        {
            var tmp = autoRestStart;
            autoRestStart = autoRestStop;
            autoRestStop = tmp;
        }

        if (autoRestStart == autoRestStop)
        {
            return false;
        }

        var rested = player.Rested;
        rested.AutoRestStart = autoRestStart;
        rested.AutoRestTarget = autoRestStop;
        return true;
    }
}
