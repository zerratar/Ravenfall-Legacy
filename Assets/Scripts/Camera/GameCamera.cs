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
    private bool canObserveNextPlayer = true;
    private bool allowJoinObserve = true;
    private bool forcedFreeCamera;

    public static Quaternion Rotation;

    public PlayerDetails Observer => playerObserver;
    public bool ForcedFreeCamera => forcedFreeCamera;
    public GameCameraType State => state;

    public IslandController CurrentlyObservedIsland => islandObserveCamera.Island;

    public bool AllowJoinObserve
    {
        get => !forcedFreeCamera && allowJoinObserve && !gameManager.Raid.Started;
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


            var leftCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (//state != GameCameraType.Free &&
                leftCtrl && Input.GetKey(KeyCode.Tab))
            {
                EnableFreeCamera();
                return;
            }
            else if (leftCtrl && Input.GetKeyDown(KeyCode.I))
            {
                ObserveNextIsland();
                return;
            }
            else if (canObserveNextPlayer && Input.GetKeyDown(KeyCode.Tab))
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

    public bool ObserveNextIsland()
    {
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
        state = GameCameraType.Raid;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        islandObserver.Observe(null, 0);
        //orbitCamera.TargetTransform = raidManager?.Boss?.transform;
    }

    public void EnableArenaCamera()
    {
        if (forcedFreeCamera) return;
        state = GameCameraType.Arena;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        islandObserver.Observe(null, 0);
        //orbitCamera.TargetTransform = null;
    }

    public void EnableDungeonCamera()
    {
        if (forcedFreeCamera) return;
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
        if (!CanObservePlayer(player)) return;

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
        freeCamera.enabled = false;
        orbitCamera.enabled = false;
        focusTargetCamera.enabled = true;
        islandObserveCamera.enabled = false;
        if (transform) focusTargetCamera.Target = transform;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanObservePlayer(PlayerController player)
    {
        return !forcedFreeCamera && !gameManager.StreamRaid.Started || !gameManager.StreamRaid.IsWar || player.streamRaidHandler.InWar;
    }

    private bool CanObserveIsland(int totalPlayerCount, IslandController island)
    {
        if (island.AllowRaidWar) return false;

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
    Island
}
