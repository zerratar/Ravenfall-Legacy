using Assets.Scripts.Game;
using System;
using System.Text;
using UnityEngine;

public class FerryHandler : MonoBehaviour
{
    [NonSerialized] public PlayerFerryState State;

    private PlayerController player;
    private FerryController ferry;
    private GameManager gameManager;
    private IslandController destination;

    public bool OnFerry => isOnFerry || State == PlayerFerryState.Disembarking || State == PlayerFerryState.Embarked;//player.transform.parent?.GetComponentInParent<FerryController>();
    public bool Active => State > PlayerFerryState.None;
    public bool Embarking => State == PlayerFerryState.Embarking;
    public bool Disembarking => State == PlayerFerryState.Disembarking;
    public bool IsCaptain => OnFerry && ferry.IsCaptainPosition(_transform.parent);
    public IslandController Destination => destination;

    private float expTime = 2.5f;
    private float expTimer = 2.5f;
    private Transform lastFerryPoint;
    private bool hasReferences;
    private Transform _transform;
    private bool isOnFerry;

    private void Start()
    {
        EnsureReferences();
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (hasReferences) return;
        if (!ferry) ferry = FindAnyObjectByType<FerryController>();
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!player) player = GetComponent<PlayerController>();
        hasReferences = player && gameManager && ferry;
        this._transform = this.transform;
    }

    private void Update()
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        EnsureReferences();

        if (player.raidHandler.InRaid || player.dungeonHandler.InDungeon || player.duelHandler.InDuel)
        {
            return;
        }

        if (OnFerry)
        {
            player.Movement.Lock();
            player.Animations.SetCaptainState(IsCaptain);

            expTimer -= GameTime.deltaTime;
            if (expTimer <= 0f)
            {
                expTimer = expTime;
                player.AddExp(RavenNest.Models.Skill.Sailing);
            }

            if (ferry.state == FerryState.Docked
                && ferry.Island && destination
                && destination == ferry.Island)
            {
                BeginDisembark(ferry.Island);
            }
        }
        else
        {
            player.Animations.SetCaptainState(IsCaptain);
        }

        if (Embarking && !OnFerry)
        {
            if (!player.Island) return;
            if (!player.Island.DockingArea) return;
            if (!player.Island.DockingArea.OnDock(player))
            {
                player.SetDestination(player.Island.DockingArea.DockPosition);
            }
            else
            {
                player.Movement.Lock();

                if (ferry.Docked && ferry.Island == player.Island)
                {
                    AddPlayerToFerry();
                }
            }
        }
        else if (Disembarking)
        {
            if (!OnFerry)
            {
                RemovePlayerFromFerry(destination, false);
                return;
            }

            if (!ferry.Island)
            {
                return;
            }

            if (!ferry.Island.DockingArea) return;

            Disembark();
        }
    }

    private void Disembark()
    {
        if (!ferry.Island.DockingArea.OnDock(player))
        {
            if (ferry.Docked && destination)
            {
                // we have reached our target destination
                if (destination == ferry.Island)
                {
                    // disembark!
                    RemovePlayerFromFerry(destination, true);

                }

            }
            else if (ferry.Docked && !destination)
            {
                RemovePlayerFromFerry(ferry.Island);
            }
        }
        else
        {
            RemovePlayerFromFerry(ferry.Island);
        }
    }

    public void Cancel()
    {
        EnsureReferences();

        State = PlayerFerryState.None;
        player.SetDestination(player.Position);
        this.destination = null;
    }

    public void ClearDestination()
    {
        this.destination = null;
    }

    public void Embark(IslandController destination = null)
    {
        EnsureReferences();

        player.Animations.ResetAnimationStates();
        player.taskTarget = null;
        State = PlayerFerryState.Embarking;
        this.destination = destination;

        if (player.onsenHandler.InOnsen)
        {
            gameManager.Onsen.Leave(player);
        }
    }

    public void BeginDisembark(IslandController destination = null)
    {
        EnsureReferences();
        State = PlayerFerryState.Disembarking;
        this.destination = destination;
    }

    public bool RemoveFromFerry()
    {
        var wasOnFerry = OnFerry;
        var parent = player.transform.parent;
        var inShipPlayerPoint = parent && parent.CompareTag("ShipPlayerPoint");

        isOnFerry = false;
        State = PlayerFerryState.None;

        ClearDestination();

        if (wasOnFerry || inShipPlayerPoint)
        {
            player.transform.SetParent(null);

            ferry.AssignBestCaptain();

            return true;
        }

        return false;
    }

    private void RemovePlayerFromFerry(IslandController island, bool notifyPlayerOfDisembark = true)
    {
        try
        {
            if (player == null || !player || player.isDestroyed)
            {
                return;
            }

            if (!gameManager)
                gameManager = GameObject.FindAnyObjectByType<GameManager>();

            if (!gameManager)
            {
                Shinobytes.Debug.LogError("Unable to remove player from ferry, gameManager obj cannot be found.");
                return;
            }

            var onFerry = OnFerry;

            var targetIsland = island ?? gameManager.Islands.FindPlayerIsland(player) ?? ferry.Island;

            if (targetIsland == null || !targetIsland)
            {
                Shinobytes.Debug.LogError("Unable to remove player from ferry, we don't have a target island.");
                return;
            }

            if (RemoveFromFerry())
            {
                player.Movement.SetPosition(targetIsland.DockingArea.DockPosition, true, true);
            }

            State = PlayerFerryState.None;

            player.Animations.SetCaptainState(false);

            isOnFerry = false;
            player.Island = targetIsland;
            player.taskTarget = null;

            this.ClearDestination();

            var task = player.GetTask();
            if (task != TaskType.None)
            {
                player.GotoClosest(task);
            }

            if (notifyPlayerOfDisembark && !AdminControlData.ControlPlayers)
            {
                gameManager.RavenBot?.SendReply(player, Localization.MSG_FERRY_ARRIVED, player.Island.Identifier);
            }

            ferry.AssignBestCaptain();

        }
        catch (System.Exception exc)
        {
            var err = "Unable to remove player from ferry: ";
            if (exc is System.NullReferenceException nexc)
            {
                Shinobytes.Debug.LogError(err + nexc + ": " + GameUtilities.Validate(player, gameManager));
                return;
            }

            Shinobytes.Debug.LogError(err + exc);
        }
    }

    private void LateUpdate()
    {
        if (OnFerry && Time.frameCount % 4 == 0)
        {
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;
        }
    }

    public void AddPlayerToFerry(IslandController destination)
    {
        ClearDestination();
        AddPlayerToFerry();
        this.destination = destination;
    }

    public void AddPlayerToFerry()
    {
        EnsureReferences();
        if (!ferry) return;
        player.InterruptAction();

        // re-arrange players if this player should be the captain.

        //var currentCaptain = ferry.Captain;
        //if (canBeCaptain && currentCaptain)
        //{
        //    if (player.Stats.Sailing.MaxLevel > currentCaptain.Stats.Sailing.MaxLevel)
        //    {
        //        currentCaptain.Ferry.MoveToSailorPosition();
        //    }
        //}

        lastFerryPoint = ferry.GetNextPlayerPoint(false);
        if (lastFerryPoint)
        {
            player.Movement.Lock();
            State = PlayerFerryState.Embarked;
            player.transform.SetParent(lastFerryPoint);
            player.transform.localPosition = Vector3.zero;
            player.transform.rotation = lastFerryPoint.rotation;
            player.Island = null;
            //if (ferry.IsCaptainPosition(lastFerryPoint))
            //{
            //    ferry.SetCaptain(this.player);
            //}
            this.isOnFerry = true;
        }

        ferry.AssignBestCaptain();
    }

    public void MoveToSailorPosition()
    {
        EnsureReferences();
        if (!ferry) return;
        player.InterruptAction();

        lastFerryPoint = ferry.GetNextPlayerPoint(false);
        if (lastFerryPoint)
        {
            player.Movement.Lock();
            State = PlayerFerryState.Embarked;
            player.transform.SetParent(lastFerryPoint);
            player.transform.localPosition = Vector3.zero;
            player.transform.rotation = lastFerryPoint.rotation;
            player.Island = null;
            this.isOnFerry = true;
        }
    }

    public enum PlayerFerryState
    {
        None,
        Embarking,
        Embarked,
        Fishing,
        Disembarking
    }
}
