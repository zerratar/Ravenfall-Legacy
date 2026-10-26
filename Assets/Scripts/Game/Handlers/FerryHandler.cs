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
    public bool IsCaptain => OnFerry && !ownBoat && ferry.IsCaptainPosition(_transform.parent);
    public IslandController Destination => destination;

    /// <summary>
    /// Aboard their own boat (docs/own-boats.md, phase 2) rather than the ferry. Everything that asks <see cref="OnFerry"/>
    /// or <see cref="Active"/> sees them at sea the same way.
    /// </summary>
    public bool OnOwnBoat => ownBoat;
    public OwnBoat OwnBoat => ownBoat;

    /// <summary>With a boat equipped (and the sea chart to sail by), !sail takes it instead of the ferry.</summary>
    public bool HasOwnBoat => player && player.Inventory != null && player.Inventory.GetEquipmentBySlot(ItemSlot.Boat) != null && SeaChart.Ready;

    private OwnBoat ownBoat;

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

    public void Poll()
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        EnsureReferences();

        if (player.raidHandler.InRaid || player.dungeonHandler.InDungeon || player.duelHandler.InDuel)
        {
            player.Animations.SetCaptainState(false);
            return;
        }

        if (ownBoat)
        {
            PollOwnBoat();
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

                if (HasOwnBoat && LaunchOwnBoat(player.Island))
                {
                    return;
                }

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

        // already out on their own boat: a new course, no need to go ashore first
        if (ownBoat)
        {
            this.destination = destination;
            State = PlayerFerryState.Embarked;
            SetOwnBoatCourse();
            return;
        }

        player.Animations.ResetAnimationStates();
        player.taskTarget = null;
        State = PlayerFerryState.Embarking;
        this.destination = destination;

        if (player.onsenHandler.InOnsen)
        {
            player.onsenHandler.Exit();
        }
    }

    public void BeginDisembark(IslandController destination = null)
    {
        EnsureReferences();
        State = PlayerFerryState.Disembarking;
        this.destination = destination;

        // on their own boat: it makes for that island, or the nearest one, and lands them there
        if (ownBoat)
        {
            if (!destination && SeaChart.TryGetNearestDock(ownBoat.SeaPosition, IsSailable, out var dock))
                this.destination = gameManager.Islands.Find(dock.Island);
            SetOwnBoatCourse();
        }
    }

    public bool RemoveFromFerry()
    {
        // taken off their own boat (a raid, a dungeon, a teleport): the boat is put away, its place kept so that being
        // put back at sea afterwards (AddPlayerToFerry) finds them where they were
        if (ownBoat)
        {
            OwnBoatLog.Set(player.Id, ownBoat);
            PutAwayOwnBoat(false);
            State = PlayerFerryState.None;
            ClearDestination();
            return true;
        }

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
                player.Movement.SetPosition(targetIsland.DockingArea.DockPosition);
                player.Movement.Unlock();
            }


            State = PlayerFerryState.None;

            player.Animations.SetCaptainState(false);

            isOnFerry = false;
            player.Island = targetIsland;
            player.taskTarget = null;

            player.playerAnimations.ResetAnimationStates();

            this.ClearDestination();

            var task = player.GetTask();
            if (task != TaskType.None)
            {
                player.GotoClosest(task);
            }

            if (notifyPlayerOfDisembark && !AdminControlData.ControlPlayers && !gameManager.HasMessageFilter("Disembark"))
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
            player.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    public void AddPlayerToFerry(IslandController destination)
    {
        ClearDestination();
        if (HasOwnBoat)
        {
            this.destination = destination;
            LaunchOwnBoatAtSea();
            return;
        }
        AddPlayerToFerry();
        this.destination = destination;
    }

    public void AddPlayerToFerry()
    {
        EnsureReferences();
        // put back at sea (a restart, the end of a raid): on their own boat if they have one
        if (HasOwnBoat && LaunchOwnBoatAtSea()) return;
        if (!ferry) return;
        player.InterruptAction();
        player.playerAnimations.ResetAnimationStates();
        // re-arrange players if this player should be the captain.

        //var currentCaptain = ferry.Captain;
        //if (canBeCaptain && currentCaptain)
        //{
        //    if (player.Stats.Sailing.MaxLevel > currentCaptain.Stats.Sailing.MaxLevel)
        //    {
        //        currentCaptain.Ferry.MoveToSailorPosition();
        //    }
        //}

        player.Animations.ResetAnimationStates();
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

    // ---- Own boats (docs/own-boats.md, phase 2) --------------------------------------------------------------

    private float ownBoatSaveTimer;

    private void PollOwnBoat()
    {
        player.Movement.Lock();
        // Sailing exp while the boat moves; none lying at anchor
        if (ownBoat.Moving)
        {
            expTimer -= GameTime.deltaTime;
            if (expTimer <= 0f)
            {
                expTimer = expTime;
                player.AddExp(RavenNest.Models.Skill.Sailing);
            }
        }

        if (SeaFishing.IsFishing(player)) FishOverTheSide();

        ownBoatSaveTimer -= GameTime.deltaTime;
        if (ownBoatSaveTimer <= 0f)
        {
            ownBoatSaveTimer = 5f;
            OwnBoatLog.Set(player.Id, ownBoat);
        }
    }

    /// <summary>
    /// !fish out at sea: the own boat drops anchor where it is (no longer bound anywhere) so its owner can fish over the
    /// side. False when not on an own boat.
    /// </summary>
    public bool FishAtSea()
    {
        if (!ownBoat) return false;
        ownBoat.Anchor();
        destination = null;
        State = PlayerFerryState.Embarked;
        seaFishTimer = 1f;
        return true;
    }

    private float seaFishTimer;

    /// <summary>A cast every few seconds, as at a fishing spot: the animation, the splash and Fishing exp.</summary>
    private void FishOverTheSide()
    {
        seaFishTimer -= GameTime.deltaTime;
        if (seaFishTimer > 0f) return;
        seaFishTimer = 3f;
        player.FishAtSea(ownBoat.transform.position + ownBoat.transform.right * 3f);
    }

    private bool IsSailable(string island)
    {
        var i = gameManager ? gameManager.Islands.Find(island) : null;
        return i && i.Sailable;
    }

    /// <summary>From the dock of the island they stand on: the boat comes alongside and they step aboard.</summary>
    private bool LaunchOwnBoat(IslandController from)
    {
        var item = player.Inventory.GetEquipmentBySlot(ItemSlot.Boat);
        if (item == null || !from || !SeaChart.TryGetDock(from.Identifier, out var dock)) return false;
        Board(OwnBoat.Launch(player, BoatItems.TierOf(item.Item), dock.Position, dock.Heading + 180f));
        SetOwnBoatCourse();
        if (!AdminControlData.ControlPlayers && !gameManager.HasMessageFilter("Disembark"))
        {
            gameManager.RavenBot?.SendReply(player, destination ? Localization.MSG_OWN_BOAT_SAILING : Localization.MSG_OWN_BOAT_WANDERING, item.Item.Name, destination ? destination.Identifier : "");
        }
        return true;
    }

    /// <summary>Back aboard out at sea: where the boat was before a restart, or by the ferry if it is not known.</summary>
    private bool LaunchOwnBoatAtSea()
    {
        EnsureReferences();
        var item = player.Inventory.GetEquipmentBySlot(ItemSlot.Boat);
        if (item == null) return false;
        Vector3 at; float heading;
        if (OwnBoatLog.TryGet(player.Id, out var saved))
        {
            at = new Vector3(saved.X, SeaChart.Sea, saved.Z);
            heading = saved.Heading;
            if (!destination && !string.IsNullOrEmpty(saved.Destination)) destination = gameManager.Islands.Find(saved.Destination);
        }
        else
        {
            at = ferry ? ferry.transform.position : player.Position;
            heading = ferry ? ferry.transform.eulerAngles.y : 0f;
        }
        if (ownBoat) PutAwayOwnBoat(false);
        Board(OwnBoat.Launch(player, BoatItems.TierOf(item.Item), at, heading));
        SetOwnBoatCourse();
        return true;
    }

    private void Board(OwnBoat boat)
    {
        ownBoat = boat;
        boat.Arrived += OnOwnBoatArrived;
        player.InterruptAction();
        player.taskTarget = null;
        player.Animations.ResetAnimationStates();
        player.Movement.Lock();
        player.transform.SetParent(boat.Seat);
        player.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        player.Island = null;
        isOnFerry = true;
        State = PlayerFerryState.Embarked;
        // rowing sits; the bigger boats are steered standing
        if (boat.Tier == BoatItems.Tier.Rowboat) player.Animations.Sit();
        else player.Animations.SetCaptainState(true);
        ownBoatSaveTimer = 0f;
    }

    private void SetOwnBoatCourse()
    {
        if (!ownBoat) return;
        if (destination && ownBoat.SailTo(destination.Identifier)) return;
        if (destination)
        {
            // no way there over the chart (an island with no dock point): wander rather than sit still
            Shinobytes.Debug.LogWarning(player.Name + "'s boat has no way to " + destination.Identifier + ", wandering instead.");
            destination = null;
        }
        if (State == PlayerFerryState.Disembarking)
        {
            // nowhere to land: stay aboard
            State = PlayerFerryState.Embarked;
        }
        ownBoat.Wander();
    }

    private void OnOwnBoatArrived(OwnBoat boat)
    {
        var island = gameManager.Islands.Find(boat.Destination);
        if (!island || !island.DockingArea)
        {
            boat.Wander();
            return;
        }
        LandOwnBoat(island);
    }

    /// <summary>Steps ashore on the island's dock; the boat is put away until they sail again.</summary>
    private void LandOwnBoat(IslandController island)
    {
        PutAwayOwnBoat(true);
        player.Movement.SetPosition(island.DockingArea.DockPosition);
        player.Movement.Unlock();
        State = PlayerFerryState.None;
        player.Island = island;
        player.taskTarget = null;
        player.playerAnimations.ResetAnimationStates();
        ClearDestination();

        var task = player.GetTask();
        if (task != TaskType.None) player.GotoClosest(task);

        if (!AdminControlData.ControlPlayers && !gameManager.HasMessageFilter("Disembark"))
        {
            gameManager.RavenBot?.SendReply(player, Localization.MSG_FERRY_ARRIVED, island.Identifier);
        }
    }

    private void PutAwayOwnBoat(bool forget)
    {
        if (!ownBoat) return;
        ownBoat.Arrived -= OnOwnBoatArrived;
        if (player && player.transform.parent == ownBoat.Seat) player.transform.SetParent(null);
        player.Animations.SetCaptainState(false);
        player.Animations.ClearOnsenAnimations();
        Destroy(ownBoat.gameObject);
        ownBoat = null;
        isOnFerry = false;
        if (forget) OwnBoatLog.Remove(player.Id);
    }

    private void OnDestroy()
    {
        // the player left the game at sea: the boat goes with them, its place kept for when they come back
        if (ownBoat)
        {
            OwnBoatLog.Set(player.Id, ownBoat);
            OwnBoatLog.Flush(true);
            Destroy(ownBoat.gameObject);
        }
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
}

public enum PlayerFerryState
{
    None,
    Embarking,
    Embarked,
    Fishing,
    Disembarking
}
