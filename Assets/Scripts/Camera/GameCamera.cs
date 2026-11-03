using Shinobytes.Linq;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class GameCamera : MonoBehaviour
{
    private const float ObserverJumpTimer = 10f;

    [SerializeField] private FreeCamera freeCamera;
    [SerializeField] private OrbitCamera orbitCamera;
    [SerializeField] private FocusTargetCamera focusTargetCamera;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RaidManager raidManager;
    [SerializeField] private PlayerObserveCamera observeCamera;
    [SerializeField] private IslandObserveCamera islandObserveCamera;
    [SerializeField] private GameObject arena;

    [SerializeField] private PlayerDetails playerObserver;
    [SerializeField] private IslandDetails islandObserver;

    [SerializeField] private float potatoModeFarClipDistance = 500f;
    private PostProcessLayer postProcessingLayer;

    private Camera _camera;

    public float FarClipDistance;

    public float MaxFarClipDistance;
    public float MinFarClipDistance;

    private float observeNextPlayerTimer = ObserverJumpTimer;
    private float observeNextIslandTimer = ObserverJumpTimer;
    private int observedPlayerIndex;
    private int observedIslandIndex;

    private GameCameraType state = GameCameraType.Free;

    // the character the streamer plays by hand (Ravebridge): followed until control ends
    private PlayerController controlled;
    private HeroCamera heroCamera;
    // the regatta's director (RaceCamera) while a race is shown
    private RaceCamera raceCamera;

    /// <summary>Hero mode's camera while it follows the hero (HeroController), else null.</summary>
    public HeroCamera HeroView => heroCamera && heroCamera.enabled ? heroCamera : null;
    private bool canObserveNextPlayer = true;
    private bool allowJoinObserve = true;
    private bool forcedFreeCamera;

    public static Quaternion Rotation;

    public PlayerDetails Observer => playerObserver;
    public bool ForcedFreeCamera => forcedFreeCamera;
    public GameCameraType State => state;

    /// <summary>
    /// True while the free camera is actually the one in control. <see cref="State"/> is not a
    /// reliable signal for this: it starts as Free and the automatic observe path never changes it.
    /// </summary>
    public bool IsFreeCameraActive => freeCamera && freeCamera.isActiveAndEnabled;

    public IslandController CurrentlyObservedIsland => islandObserveCamera.Island;

    public bool AllowJoinObserve
    {
        get => !forcedFreeCamera && allowJoinObserve && !gameManager.Raid.Started && state != GameCameraType.Race;
        private set => allowJoinObserve = value;
    }

    // Start is called before the first frame updateF
    void Start()
    {
        if (!freeCamera) freeCamera = GetComponent<FreeCamera>();
        if (!orbitCamera) orbitCamera = GetComponent<OrbitCamera>();
        if (!focusTargetCamera) focusTargetCamera = GetComponent<FocusTargetCamera>();
        if (!islandObserveCamera) islandObserveCamera = GetComponent<IslandObserveCamera>();
        if (!playerObserver) playerObserver = gameManager.ObservedPlayerDetails;

        playerObserver.gameObject.SetActive(false);
        postProcessingLayer = GetComponent<PostProcessLayer>();
        _camera = GetComponent<Camera>();
        FarClipDistance = _camera.farClipPlane;
        MaxFarClipDistance = FarClipDistance * 2f;
        MinFarClipDistance = FarClipDistance * 0.05f;

    }

    internal void OnSessionStart()
    {
        if (PlayerSettings.Instance.EnableIslandCameraOnStart == true)
        {
            AllowJoinObserve = false;
            ObserveNextIsland();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager == null ||
            gameManager.RavenNest == null ||
            !gameManager.RavenNest.Authenticated ||
            !gameManager.IsLoaded)
        {
            return;
        }

        LookAt.HasGameCameraRotation = true;
        Rotation = LookAt.GameCameraRotation = transform.rotation;

        // the tilt shift keeps the observed player in its sharp band; with the free camera there is no one to follow
        var observed = !IsFreeCameraActive && playerObserver ? playerObserver.ObservedPlayer : null;
        TiltShift.FocusPoint = observed ? observed.Position + Vector3.up : (Vector3?)null;


        if (gameManager.PotatoMode)
        {
            _camera.farClipPlane = potatoModeFarClipDistance;
            if (postProcessingLayer)
                postProcessingLayer.enabled = false;
        }
        else
        {
            _camera.farClipPlane = FarClipDistance;
            if (postProcessingLayer)
                postProcessingLayer.enabled = true;
        }

        try
        {
            if (forcedFreeCamera)
            {
                return;
            }

            if (controlled)
            {
                if (!HeroView) SetOrbitCameraTarget(controlled.transform);
                return;
            }

            // a cutscene moves the camera itself; no camera key takes it
            if (state == GameCameraType.Cinematic) return;


            // the regatta: the race camera key gives the race camera back (from by hand, or from a player or an island
            // watched meanwhile); the free camera key takes the race camera by hand without leaving the race
            var building = VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive;
            if (Regatta.Running && !building && Keybinds.Pressed(Keybinds.Action.RaceCamera))
            {
                if (state == GameCameraType.Race) raceCamera.ReturnToRace();
                else FollowRace(Regatta.Current, asked: true);
                return;
            }
            if (state == GameCameraType.Race && Keybinds.Held(Keybinds.Action.FreeCamera))
            {
                raceCamera.TakeByHand();
                return;
            }

            if (Keybinds.Held(Keybinds.Action.FreeCamera))
            {
                EnableFreeCamera();
                return;
            }
            else if (Keybinds.Pressed(Keybinds.Action.NextIsland))
            {
                ObserveNextIsland();
                return;
            }
            else if (canObserveNextPlayer && Keybinds.Pressed(Keybinds.Action.NextPlayer))
            {
                ObserveNextPlayer();
                return;
            }

            if (state == GameCameraType.Dungeon)
            {
                //freeCamera.enabled = false;
                //orbitCamera.enabled = false;
                //focusTargetCamera.enabled = false;
                if (gameManager.Dungeons.Started)
                {
                    var camPoint = gameManager.Dungeons.Dungeon.CameraPoint;

                    SetOrbitCameraTarget(camPoint);

                    //EnableFocusTargetCamera(camPoint);
                    //transform.position = camPoint.position;
                    //transform.rotation = camPoint.rotation;
                }
            }
            else if (state == GameCameraType.Arena)
            {
                //EnableFocusTargetCamera(arena?.transform);
                SetOrbitCameraTarget(arena?.transform);
            }
            else if (state == GameCameraType.Raid)
            {
                SetOrbitCameraTarget(raidManager?.Boss?.transform);
            }
            else if (state == GameCameraType.Race)
            {
                // the race is over or its camera was turned off
                if (!raceCamera || !raceCamera.enabled || !raceCamera.Race) StopRace();
            }
            else if (state == GameCameraType.Observe)
            {
                AllowJoinObserve = true;
                observeNextPlayerTimer -= GameTime.deltaTime;
                if (observeNextPlayerTimer <= 0)
                {
                    ObserveNextPlayer();
                }
            }
            else if (state == GameCameraType.Island)
            {
                AllowJoinObserve = false;

                // check if we are currently observing a player
                // if so, then don't allow the camera to switch to the next island
                if (observeCamera.HasTarget && playerObserver.IsObservingPlayer)
                {
                    return;
                }


                observeNextIslandTimer -= GameTime.deltaTime;
                if (observeNextIslandTimer <= 0)
                {
                    ObserveNextIsland();
                }
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("GameCamera->Update: " + exc.Message);
        }
    }

    private void SetOrbitCameraTarget(Transform transform)
    {
        orbitCamera.enabled = true;
        orbitCamera.TargetTransform = transform;
    }

    private void EnableFreeCamera()
    {
        LeaveRace();
        state = GameCameraType.Free;
        canObserveNextPlayer = false;

        var position = this.transform.position;
        var rotation = this.transform.rotation;

        orbitCamera.TargetTransform = null;
        freeCamera.enabled = true;
        orbitCamera.enabled = false;
        islandObserveCamera.enabled = false;
        focusTargetCamera.enabled = false;

        playerObserver.Observe(null, 0);
        islandObserver.Observe(null, 0);

        observeCamera.ObservePlayer(null);


        AllowJoinObserve = false;

        this.transform.position = position;
        this.transform.rotation = rotation;

        StartCoroutine(SetTransform(position, rotation));
    }

    private IEnumerator SetTransform(Vector3 pos, Quaternion rot)
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        this.transform.position = pos;
        this.transform.rotation = rot;
        canObserveNextPlayer = true;
    }

    public void ForceFreeCamera(bool slowMOtion = true)
    {
        forcedFreeCamera = true;
        freeCamera.SlowMotion = slowMOtion;
        EnableFreeCamera();
    }

    public void ReleaseFreeCamera()
    {
        freeCamera.SlowMotion = false;
        forcedFreeCamera = false;
    }

    /// <summary>
    /// The camera stays on a character played by hand (ManualPlayerController) until StopFollowingControlled: hero mode's
    /// camera (HeroCamera), in or out of hero mode, so it looks at the character's head rather than its feet and walls in
    /// the way are hidden or slipped in front of (the orbit camera went through buildings).
    /// </summary>
    public void FollowControlled(PlayerController player)
    {
        if (!player) return;
        observeCamera.ObservePlayer(player);
        islandObserver.Observe(null, 0);
        playerObserver.Observe(player, float.MaxValue);
        FollowHero(player);
    }

    /// <summary>Hero mode: the top-down or over-the-shoulder camera on the hero (HeroCamera).</summary>
    public void FollowHero(PlayerController player)
    {
        if (!player) return;
        LeaveRace();
        controlled = player;
        state = GameCameraType.Controlled;
        if (!heroCamera) heroCamera = GetComponent<HeroCamera>();
        if (!heroCamera) heroCamera = gameObject.AddComponent<HeroCamera>();
        orbitCamera.enabled = false;
        freeCamera.enabled = false;
        islandObserveCamera.enabled = false;
        focusTargetCamera.enabled = false;
        heroCamera.enabled = true;
        heroCamera.Follow(player);
    }

    /// <summary>Hero mode ends: the character played by hand keeps the same camera (FollowControlled).</summary>
    public void StopHeroView()
    {
        if (controlled) return;
        if (heroCamera) heroCamera.enabled = false;
    }

    public void StopFollowingControlled()
    {
        if (heroCamera) heroCamera.enabled = false;
        var player = controlled;
        controlled = null;
        orbitCamera.Manual = false;
        if (player) ObservePlayer(player, ObserveEvent.Automatic);
        else ObserveNextPlayer();
    }

    public bool ObserveNextIsland()
    {
        if (controlled) return true;
        LeaveRace();
        if (forcedFreeCamera || gameManager.StreamRaid.IsWar)
            return true;

        var settings = PlayerSettings.Instance;
        if (settings.EnableIslandCameraOnStart == false)
        {
            settings.EnableIslandCameraOnStart = true;
            PlayerSettings.Save();
        }

        var totalPlayerCount = playerManager.GetPlayerCountOnIslands();

        state = GameCameraType.Island;
        var allIslands = gameManager.Islands.All;
        var islandCount = allIslands.Length;
        IslandController island = null;
        for (var i = 0; i < islandCount; i++)
        {
            observedIslandIndex = (observedIslandIndex + 1) % islandCount;
            island = allIslands[observedIslandIndex];
            if (!island) return true;

            if (CanObserveIsland(totalPlayerCount, island))
            {
                break;
            }
        }
        if (!island) return true;
        ObserveIsland(island);
        return false;
    }

    public bool ObserveNextPlayer()
    {
        if (controlled) return true;
        LeaveRace();
        var playerCount = playerManager.GetPlayerCount(true);
        if (playerCount == 0)
        {
            return true;
        }

        var settings = PlayerSettings.Instance;
        if (settings.EnableIslandCameraOnStart == true)
        {
            settings.EnableIslandCameraOnStart = false;
            PlayerSettings.Save();
        }

        state = GameCameraType.Observe;
        focusTargetCamera.enabled = false;
        PlayerController player = null;

        for (var i = 0; i < playerCount; i++)
        {
            observedPlayerIndex = (observedPlayerIndex + 1) % playerCount;
            player = playerManager.GetPlayerByIndex(observedPlayerIndex);
            if (!player) return true;
            if (CanObservePlayer(player)) break;
        }
        if (!player) return true;
        ObservePlayer(player);
        return false;
    }

    public void EnableRaidCamera()
    {
        if (forcedFreeCamera) return;
        LeaveRace();
        state = GameCameraType.Raid;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        islandObserver.Observe(null, 0);
        //orbitCamera.TargetTransform = raidManager?.Boss?.transform;
    }

    public void EnableArenaCamera()
    {
        if (forcedFreeCamera) return;
        LeaveRace();
        state = GameCameraType.Arena;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        islandObserver.Observe(null, 0);
        //orbitCamera.TargetTransform = null;
    }

    public void EnableDungeonCamera()
    {
        if (forcedFreeCamera) return;
        LeaveRace();
        state = GameCameraType.Dungeon;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        islandObserver.Observe(null, 0);
        //orbitCamera.TargetTransform = null;
    }

    public void DisableFocusCamera()
    {
        if (forcedFreeCamera) return;
        if (state != GameCameraType.Observe)
        {
            if (PlayerSettings.Instance.EnableIslandCameraOnStart == true)
            {
                ObserveNextIsland();
            }
            else
            {
                ObserveNextPlayer();
            }
        }
    }

    public void DisableDungeonCamera()
    {
        if (forcedFreeCamera) return;
        if (PlayerSettings.Instance.EnableIslandCameraOnStart == true)
        {
            ObserveNextIsland();
        }
        else
        {
            ObserveNextPlayer();
        }
    }
    public void EnsureObserverCamera()
    {
        observeCamera.UpdatePlayerLayer();
    }


    public void ObservePlayer(PlayerController player)
    {
        if (!player) return;
        if (forcedFreeCamera) return;
        ObservePlayer(player, ObserveEvent.Automatic);
    }

    public void ObserveIsland(IslandController island)
    {
        if (!island) return;
        if (forcedFreeCamera) return;
        LeaveRace();
        playerObserver.Observe(null, 0);
        observeNextIslandTimer = PlayerSettings.Instance.IslandObserveSeconds;
        islandObserver.Observe(island, observeNextIslandTimer);
        islandObserveCamera.ObserveIsland(island);
        islandObserveCamera.enabled = true;
        freeCamera.enabled = false;
        orbitCamera.enabled = false;
    }

    public void ObservePlayer(PlayerController player, ObserveEvent @event, int data = 1)
    {
        // a cheer or a sub does not take the camera off the race (a join does not either: AllowJoinObserve); clicking a
        // player, !observe and the keys do
        if (state == GameCameraType.Race && @event != ObserveEvent.Automatic) return;
        var settings = PlayerSettings.Instance;
        var times = settings.PlayerObserveSeconds;
        var observeTime = times.Default;

        if (player.IsVip)
        {
            observeTime = Mathf.Max(observeTime, times.Vip);
        }

        if (player.IsModerator)
        {
            observeTime = Mathf.Max(observeTime, times.Moderator);
        }

        if (player.IsSubscriber)
        {
            observeTime = Mathf.Max(observeTime, times.Subscriber);
        }

        if (player.IsBroadcaster)
        {
            observeTime = Mathf.Max(observeTime, times.Broadcaster);
        }

        var eventTime = 0f;
        switch (@event)
        {
            case ObserveEvent.Bits:
                eventTime = times.OnCheeredBits;
                break;

            case ObserveEvent.Subscription:
                eventTime = times.OnSubcription;
                break;
        }

        ObservePlayer(player, Mathf.Max(observeTime, eventTime));
    }

    private void ObservePlayer(PlayerController player, float time)
    {
        if (forcedFreeCamera) return;
        if (controlled) return;
        if (!CanObservePlayer(player)) return;
        LeaveRace();

        observeNextPlayerTimer = time;
        observeCamera.ObservePlayer(player);
        islandObserver.Observe(null, 0);

        playerObserver.Observe(player, time);
        freeCamera.enabled = false;
        orbitCamera.TargetTransform = player.transform;
        orbitCamera.enabled = true;
        islandObserveCamera.enabled = false;
    }

    public void EnableFocusTargetCamera(Transform transform)
    {
        if (forcedFreeCamera) return;
        LeaveRace();
        freeCamera.enabled = false;
        orbitCamera.enabled = false;
        focusTargetCamera.enabled = true;
        islandObserveCamera.enabled = false;
        if (transform) focusTargetCamera.Target = transform;
    }

    /// <summary>
    /// The regatta's own camera (RaceCamera), when the setting allows and nothing bigger has the camera: a raid, a
    /// dungeon, the arena, a war, a character played by hand or the free camera. False when it did not take over.
    /// </summary>
    /// <param name="asked">The streamer asked for it (the race camera key or button): the setting and the free camera do not stop it.</param>
    public bool FollowRace(Regatta race, bool asked = false)
    {
        if (!race || forcedFreeCamera || controlled) return false;
        if (!asked && (!SettingsApplier.RaceCamera || IsFreeCameraActive)) return false;
        if (state == GameCameraType.Raid || state == GameCameraType.Dungeon || state == GameCameraType.Arena) return false;
        if (gameManager.StreamRaid.IsWar || gameManager.Raid.Started || gameManager.Dungeons.Started) return false;
        if (!raceCamera) raceCamera = GetComponent<RaceCamera>();
        if (!raceCamera) raceCamera = gameObject.AddComponent<RaceCamera>();
        state = GameCameraType.Race;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        islandObserver.Observe(null, 0);
        freeCamera.enabled = false;
        orbitCamera.enabled = false;
        islandObserveCamera.enabled = false;
        focusTargetCamera.enabled = false;
        raceCamera.Follow(race);
        raceCamera.enabled = true;
        return true;
    }

    /// <summary>
    /// A cutscene takes the camera (RavebridgeReveal): no other camera moves it and no camera key works until
    /// <see cref="StopCinematic"/>; the cutscene sets the camera's transform itself, in LateUpdate. False when something
    /// bigger has it: a raid, a dungeon, the arena, a war, a character played by hand or the forced free camera.
    /// </summary>
    public bool StartCinematic()
    {
        if (forcedFreeCamera || controlled) return false;
        if (state == GameCameraType.Raid || state == GameCameraType.Dungeon || state == GameCameraType.Arena) return false;
        if (gameManager.StreamRaid.IsWar || gameManager.Raid.Started || gameManager.Dungeons.Started) return false;
        LeaveRace();
        state = GameCameraType.Cinematic;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        islandObserver.Observe(null, 0);
        freeCamera.enabled = false;
        orbitCamera.enabled = false;
        islandObserveCamera.enabled = false;
        focusTargetCamera.enabled = false;
        return true;
    }

    /// <summary>The cutscene is over: the camera goes on to <paramref name="island"/>, or to the islands as usual.</summary>
    public void StopCinematic(IslandController island = null)
    {
        if (state != GameCameraType.Cinematic) return;
        state = GameCameraType.Free;
        if (island)
        {
            state = GameCameraType.Island;
            ObserveIsland(island);
        }
        else if (PlayerSettings.Instance.EnableIslandCameraOnStart == true || ObserveNextPlayer()) ObserveNextIsland();
    }

    /// <summary>The race camera while it shows a race (by hand or not), else null: the race's UI asks it.</summary>
    public RaceCamera RaceCamera => state == GameCameraType.Race ? raceCamera : null;

    /// <summary>The "Return to race camera" button: back to the director, or to the race from whatever is watched.</summary>
    public void ReturnToRace()
    {
        if (state == GameCameraType.Race) raceCamera.ReturnToRace();
        else if (Regatta.Running) FollowRace(Regatta.Current, asked: true);
    }

    /// <summary>The race is over: back to the islands or the players, as before it.</summary>
    public void StopRace()
    {
        var showing = state == GameCameraType.Race;
        // taken by hand when it ended: the streamer keeps the camera where they put it, as the free camera
        if (showing && raceCamera && raceCamera.Manual)
        {
            LeaveRace();
            EnableFreeCamera();
            return;
        }
        LeaveRace();
        if (!showing || forcedFreeCamera) return;
        // nobody to watch: the islands then
        if (PlayerSettings.Instance.EnableIslandCameraOnStart == true || ObserveNextPlayer()) ObserveNextIsland();
    }

    private void LeaveRace()
    {
        if (raceCamera) raceCamera.enabled = false;
        if (state == GameCameraType.Race) state = GameCameraType.Free;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanObservePlayer(PlayerController player)
    {
        return !forcedFreeCamera && !gameManager.StreamRaid.Started || !gameManager.StreamRaid.IsWar || player.streamRaidHandler.InWar;
    }

    private bool CanObserveIsland(int totalPlayerCount, IslandController island)
    {
        if (island.AllowRaidWar) return false;
        if (island.EventOnly && island.GetPlayers().Count == 0) return false;

        if (PlayerSettings.Instance.CanObserveEmptyIslands.GetValueOrDefault())
        {
            return true;
        }

        // if we dont have any players, then all islands can be observed
        if (totalPlayerCount == 0)
            return true;

        // when we have players, we should only observe the island with players on.
        var plrCount = island.GetPlayerCount();

        return plrCount > 0;
    }
}

public enum ObserveEvent
{
    Automatic,
    Bits,
    Subscription
}

public enum GameCameraType
{
    Free,
    Observe,
    Arena,
    Raid,
    Dungeon,
    Island,
    Controlled,
    Race,
    Cinematic
}
