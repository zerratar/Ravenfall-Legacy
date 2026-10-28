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

    /// <summary>Aboard someone else's own boat as their guest (docs/own-boats.md, "Guests aboard").</summary>
    public bool IsGuest => guestRiding;
    public OwnBoat GuestBoat => guestBoat;
    /// <summary>The own boat they are aboard, theirs or as a guest.</summary>
    public OwnBoat Boat => ownBoat ? ownBoat : guestBoat;

    /// <summary>How far a guest must sail for the trip to count towards the owner's Ferryman titles.</summary>
    private const float GuestTripMetres = 150f;

    private OwnBoat guestBoat;
    private bool guestRiding;
    private Guid guestOwnerId;
    private Vector3 guestLastSea;
    private float guestSailed;
    // taken off as a guest by a raid or a dungeon: where they come back to, ashore (the ride is over)
    private IslandController guestDropIsland;

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
        BoatCrews.Tick();

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

        if (guestRiding)
        {
            PollGuest();
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

            // coming along in someone's boat: to the dock with them, where their boat takes them aboard; if they no
            // longer set out, neither does the guest
            var crewOwner = BoatCrews.OwnerOf(player);
            var waitingForOwner = crewOwner && (crewOwner.ferryHandler.Embarking || crewOwner.ferryHandler.OnOwnBoat);
            if (crewOwner && !waitingForOwner)
            {
                Cancel();
                return;
            }

            if (!player.Island.DockingArea.OnDock(player))
            {
                player.SetDestination(player.Island.DockingArea.DockPosition);
            }
            else
            {
                player.Movement.Lock();

                if (waitingForOwner)
                {
                    return;
                }

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

        // a guest does not steer: the owner decides where the boat goes
        if (guestRiding)
        {
            var owner = guestBoat ? guestBoat.Owner : null;
            Tell(Localization.MSG_BOAT_PASSENGER, owner ? owner.Name : "");
            return;
        }

        // already out on their own boat: a new course, no need to go ashore first
        if (ownBoat)
        {
            this.destination = destination;
            State = PlayerFerryState.Embarked;
            SetOwnBoatCourse();
            return;
        }

        // was to come along in someone's boat, but sails off on their own
        LeaveCrewAshore();

        player.Animations.ResetAnimationStates();
        player.taskTarget = null;
        State = PlayerFerryState.Embarking;
        this.destination = destination;

        if (player.onsenHandler.InOnsen)
        {
            player.onsenHandler.Exit();
        }

        CallCrew();
    }

    public void BeginDisembark(IslandController destination = null)
    {
        EnsureReferences();

        // a guest gets off at the nearest dock, the boat sailing on without them
        if (guestRiding)
        {
            LeaveBoatAsGuest();
            return;
        }

        // waiting to come along: does not, after all
        if (LeaveCrewAshore())
        {
            Cancel();
            return;
        }

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
        // a guest taken off (a raid, a dungeon): the ride is over; put back "at sea" afterwards, they come back ashore
        // at the dock nearest to where they left the boat
        if (guestRiding)
        {
            guestDropIsland = NearestIsland(guestBoat ? guestBoat.SeaPosition : guestLastSea);
            GuestAshore(null, false);
            return true;
        }

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
        if (TryLandDroppedGuest()) return;
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
        if (TryLandDroppedGuest()) return;
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
        // Sailing exp while the boat moves, a tenth more for each guest carried; none lying at anchor
        if (ownBoat.Moving)
        {
            expTimer -= GameTime.deltaTime;
            if (expTimer <= 0f)
            {
                expTimer = expTime;
                player.AddExp(RavenNest.Models.Skill.Sailing, SailingSpeeds.OwnerExpFactor(ownBoat.Guests.Count));
            }

            // the anchor came up while they fished: back to the helm (they kept the fishing pose, sailing on)
            if (player.playerAnimations.State == PlayerAnimationState.Fishing)
            {
                player.Animations.ResetAnimationStates();
                HelmPose(ownBoat);
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
        // a guest's line goes in whenever the owner lies at anchor
        if (guestRiding)
        {
            if (guestBoat && guestBoat.State != OwnBoat.Course.Anchored)
                Tell(Localization.MSG_BOAT_WAIT_ANCHOR, guestBoat.Owner ? guestBoat.Owner.Name : "");
            seaFishTimer = 1f;
            return true;
        }

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
        // the owner fishes over the starboard side, guests over the other
        var boat = Boat;
        player.FishAtSea(boat.transform.position + boat.transform.right * (guestRiding ? -3f : 3f));
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
        BoardCrew(from);
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
        HelmPose(boat);
        ownBoatSaveTimer = 0f;
    }

    /// <summary>Rowing sits; the bigger boats are steered standing.</summary>
    private void HelmPose(OwnBoat boat)
    {
        if (boat.Tier == BoatItems.Tier.Rowboat) player.Animations.Sit();
        else player.Animations.SetCaptainState(true);
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
        CrewAshore(island);
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
        // its guests first: they sit in it, and would go with it
        CrewAshore(null);
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
        // a guest leaving the game frees their seat (and the trip counts, if it was one)
        if (guestRiding)
        {
            if (guestBoat) guestBoat.FreeSeat(player);
            TellServerAshore(guestOwnerId, guestSailed >= GuestTripMetres);
            guestRiding = false;
        }
        BoatCrews.Remove(player);

        // the player left the game at sea: the boat goes with them, its place kept for when they come back. Its guests
        // are lifted out first, or they would go with it; each lands at the nearest dock on its own (PollGuest)
        if (ownBoat)
        {
            foreach (var guest in ownBoat.Guests) if (guest) guest.transform.SetParent(null);
            OwnBoatLog.Set(player.Id, ownBoat);
            OwnBoatLog.Flush(true);
            Destroy(ownBoat.gameObject);
        }
    }

    // ---- Guests aboard (docs/own-boats.md, "Guests aboard") ---------------------------------------------------

    /// <summary>The owner sets out: everyone coming along on this island walks to the dock with them.</summary>
    private void CallCrew()
    {
        foreach (var guest in Copy(BoatCrews.GuestsOf(player)))
        {
            if (guest.Island == player.Island && !guest.ferryHandler.Active) guest.ferryHandler.FollowOwner();
        }
    }

    /// <summary>To the dock with the owner, to board their boat when it comes alongside.</summary>
    public void FollowOwner()
    {
        EnsureReferences();
        if (guestRiding || ownBoat) return;
        if (player.onsenHandler.InOnsen) player.onsenHandler.Exit();
        player.Animations.ResetAnimationStates();
        player.taskTarget = null;
        destination = null;
        State = PlayerFerryState.Embarking;
    }

    /// <summary>The boat came alongside: everyone coming along on that island climbs aboard; anyone not there misses it.</summary>
    private void BoardCrew(IslandController from)
    {
        foreach (var guest in Copy(BoatCrews.GuestsOf(player)))
        {
            var f = guest.ferryHandler;
            var free = guest.Island == from && !f.ownBoat && !f.guestRiding && !(f.OnFerry && !f.Embarking)
                && !guest.raidHandler.InRaid && !guest.dungeonHandler.InDungeon && !guest.duelHandler.InDuel
                && !guest.arenaHandler.InArena && !guest.streamRaidHandler.InWar && !guest.InRavebridge;
            if (free && f.BoardAsGuest(ownBoat)) continue;
            BoatCrews.Leave(guest);
            if (f.State == PlayerFerryState.Embarking) f.Cancel();
            f.Tell(Localization.MSG_BOAT_MISSED, player.Name);
        }
    }

    /// <summary>Takes a seat in the owner's boat; false when there is none.</summary>
    public bool BoardAsGuest(OwnBoat boat)
    {
        EnsureReferences();
        if (!boat || !boat.Owner || ownBoat || guestRiding) return false;
        var seat = boat.TakeSeat(player);
        if (!seat) return false;
        if (player.onsenHandler.InOnsen) player.onsenHandler.Exit();

        guestBoat = boat;
        guestRiding = true;
        guestOwnerId = boat.Owner.Id;
        guestLastSea = boat.SeaPosition;
        guestSailed = 0f;
        guestDropIsland = null;

        player.InterruptAction();
        player.taskTarget = null;
        player.Animations.ResetAnimationStates();
        player.Movement.Lock();
        player.transform.SetParent(seat);
        player.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        player.Island = null;
        isOnFerry = true;
        State = PlayerFerryState.Embarked;
        destination = null;
        expTimer = expTime;
        SitAboard();

        var game = gameManager;
        var guestId = player.Id;
        var ownerId = guestOwnerId;
        TellServer(() => game.RavenNest.Boats.GuestAboardAsync(ownerId, guestId));
        var item = BoatCrews.BoatOf(boat.Owner);
        Tell(Localization.MSG_BOAT_GUEST_BOARDED, boat.Owner.Name, item != null ? item.Item.Name : "");
        return true;
    }

    private void SitAboard()
    {
        if (guestBoat && guestBoat.GuestsSit) player.Animations.Sit();
    }

    private void PollGuest()
    {
        // the boat is gone (its owner left the game): ashore at the dock nearest to where it was
        if (!guestBoat)
        {
            var island = NearestIsland(guestLastSea);
            GuestAshore(island, true);
            if (!island) AddPlayerToFerry();
            return;
        }

        player.Movement.Lock();
        var sea = guestBoat.SeaPosition;
        guestSailed += Vector3.Distance(sea, guestLastSea);
        guestLastSea = sea;

        // Sailing exp while the boat moves, as for its owner
        if (guestBoat.Moving)
        {
            expTimer -= GameTime.deltaTime;
            if (expTimer <= 0f)
            {
                expTimer = expTime;
                player.AddExp(RavenNest.Models.Skill.Sailing);
            }

            // the anchor came up while they fished: back to the oars
            if (player.playerAnimations.State == PlayerAnimationState.Fishing)
            {
                player.Animations.ResetAnimationStates();
                SitAboard();
            }
        }

        if (SeaFishing.IsFishing(player)) FishOverTheSide();
    }

    /// <summary>!boat leave or !disembark as a guest: off at the nearest dock, the boat sailing on.</summary>
    public void LeaveBoatAsGuest()
    {
        if (!guestRiding) return;
        var owner = guestBoat ? guestBoat.Owner : null;
        var island = NearestIsland(guestBoat ? guestBoat.SeaPosition : guestLastSea);
        GuestAshore(island, true);
        if (!island) AddPlayerToFerry();
        if (owner) owner.ferryHandler.Tell(Localization.MSG_BOAT_GUEST_LEFT, player.Name);
    }

    /// <summary>Off the boat: at the island's dock when one is given, otherwise wherever whoever takes them puts them.</summary>
    public void GuestAshore(IslandController island, bool notify)
    {
        if (!guestRiding) return;
        var boat = guestBoat;
        if (boat) boat.FreeSeat(player);
        if (player.transform.parent && player.transform.parent.name.StartsWith("GuestSeat", StringComparison.Ordinal))
            player.transform.SetParent(null);

        guestBoat = null;
        guestRiding = false;
        isOnFerry = false;
        State = PlayerFerryState.None;
        ClearDestination();
        player.Animations.SetCaptainState(false);
        player.Animations.ClearOnsenAnimations();
        BoatCrews.Leave(player);
        TellServerAshore(guestOwnerId, guestSailed >= GuestTripMetres);

        if (!island || !island.DockingArea) return;
        player.Movement.SetPosition(island.DockingArea.DockPosition);
        player.Movement.Unlock();
        player.Island = island;
        player.taskTarget = null;
        player.playerAnimations.ResetAnimationStates();
        var task = player.GetTask();
        if (task != TaskType.None) player.GotoClosest(task);
        if (notify) Tell(Localization.MSG_FERRY_ARRIVED, island.Identifier);
    }

    /// <summary>The owner's boat is put away: its guests step ashore (at that island, or the nearest dock) and the crew ends.</summary>
    private void CrewAshore(IslandController island)
    {
        if (!ownBoat) return;
        if (ownBoat.Guests.Count > 0)
        {
            var at = island ? island : NearestIsland(ownBoat.SeaPosition);
            foreach (var guest in Copy(ownBoat.Guests))
            {
                guest.ferryHandler.GuestAshore(at, true);
                if (!at) guest.ferryHandler.AddPlayerToFerry();
            }
        }
        BoatCrews.Disband(player);
    }

    /// <summary>Was to come along in someone's boat and is not now; false if they were in no crew.</summary>
    private bool LeaveCrewAshore()
    {
        var owner = BoatCrews.OwnerOf(player);
        if (!owner || guestRiding) return false;
        BoatCrews.Leave(player);
        Tell(Localization.MSG_BOAT_LEFT_CREW, owner.Name);
        owner.ferryHandler.Tell(Localization.MSG_BOAT_GUEST_LEFT, player.Name);
        return true;
    }

    /// <summary>!boat leave ashore: no longer coming along (and no longer walking to the dock for it).</summary>
    public bool QuitCrew()
    {
        if (!LeaveCrewAshore()) return false;
        if (State == PlayerFerryState.Embarking) Cancel();
        return true;
    }

    /// <summary>Back from a raid or dungeon that took them off a boat as a guest: ashore, not out at sea.</summary>
    private bool TryLandDroppedGuest()
    {
        var island = guestDropIsland;
        guestDropIsland = null;
        if (!island || !island.DockingArea) return false;
        player.Movement.SetPosition(island.DockingArea.DockPosition);
        player.Movement.Unlock();
        player.Island = island;
        State = PlayerFerryState.None;
        ClearDestination();
        return true;
    }

    private IslandController NearestIsland(Vector3 sea) =>
        gameManager && SeaChart.TryGetNearestDock(sea, IsSailable, out var dock) ? gameManager.Islands.Find(dock.Island) : null;

    private static PlayerController[] Copy(System.Collections.Generic.IReadOnlyList<PlayerController> players)
    {
        var copy = new PlayerController[players.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = players[i];
        return System.Array.FindAll(copy, p => p);
    }

    /// <summary>A line to this player in chat, kept quiet as the ferry's own lines are.</summary>
    public void Tell(string format, params object[] args)
    {
        EnsureReferences();
        if (AdminControlData.ControlPlayers || !gameManager || gameManager.RavenBot == null) return;
        gameManager.RavenBot.SendReply(player, format, args);
    }

    private void TellServerAshore(Guid ownerId, bool trip)
    {
        if (ownerId == Guid.Empty || !player) return;
        var game = gameManager;
        // the game closing: its session is already over, and the server would only refuse it
        if (!game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        var guestId = player.Id;
        TellServer(() => game.RavenNest.Boats.GuestAshoreAsync(ownerId, guestId, trip));
    }

    private static async void TellServer(Func<System.Threading.Tasks.Task<bool>> call)
    {
        try
        {
            await call();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Telling the server about a boat guest failed: " + exc.Message);
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
