using System;
using System.Runtime.CompilerServices;
using FlatKit;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class GameCamera : MonoBehaviour
{
    private const float ObserverJumpTimer = 10f;

    [SerializeField] private FreeCamera freeCamera;
    [SerializeField] private MouseOrbitCamera orbitCamera;
    [SerializeField] private FocusTargetCamera focusTargetCamera;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RaidManager raidManager;
    [SerializeField] private PlayerObserveCamera observeCamera;
    [SerializeField] private GameObject arena;

    [SerializeField] private PlayerDetails playerObserver;

    [SerializeField] private float potatoModeFarClipDistance = 500f;
    private PostProcessLayer postProcessingLayer;
    private Camera camera;
    private float farClipDistance;

    private float observeNextPlayerTimer = ObserverJumpTimer;
    private int observedPlayerIndex;

    private GameCameraType state = GameCameraType.Free;
    private bool allowJoinObserve = true;
    private bool forcedFreeCamera;

    public PlayerDetails Observer => playerObserver;

    public bool AllowJoinObserve
    {
        get => !forcedFreeCamera && allowJoinObserve && !gameManager.Raid.Started;
        private set => allowJoinObserve = value;
    }

    // Start is called before the first frame updateF
    void Start()
    {
        if (!freeCamera) freeCamera = GetComponent<FreeCamera>();
        if (!orbitCamera) orbitCamera = GetComponent<MouseOrbitCamera>();
        if (!focusTargetCamera) focusTargetCamera = GetComponent<FocusTargetCamera>();

        if (!playerObserver) playerObserver = gameManager.ObservedPlayerDetails;

        playerObserver.gameObject.SetActive(false);
        postProcessingLayer = GetComponent<PostProcessLayer>();
        camera = GetComponent<Camera>();
        farClipDistance = camera.farClipPlane;
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

        if (gameManager.PotatoMode)
        {
            camera.farClipPlane = potatoModeFarClipDistance;
            if (postProcessingLayer)
                postProcessingLayer.enabled = false;
        }
        else
        {
            camera.farClipPlane = farClipDistance;
            if (postProcessingLayer)
                postProcessingLayer.enabled = true;
        }

        try
        {
            if (forcedFreeCamera)
            {
                return;
            }

            if (state != GameCameraType.Free &&
                Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Tab))
            {
                EnableFreeCamera();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
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
                    var camPoint = gameManager.Dungeons.Dungeon.Room.CameraPoint;

                    EnableFocusTargetCamera(camPoint);
                    //transform.position = camPoint.position;
                    //transform.rotation = camPoint.rotation;
                }
            }
            else if (state == GameCameraType.Arena)
            {
                EnableFocusTargetCamera(arena?.transform);
            }
            else if (state == GameCameraType.Raid)
            {
                EnableFocusTargetCamera(raidManager?.Boss?.transform);
            }
            else if (state == GameCameraType.Observe)
            {
                AllowJoinObserve = true;
                observeNextPlayerTimer -= Time.deltaTime;
                if (observeNextPlayerTimer <= 0)
                {
                    ObserveNextPlayer();
                }
            }
        }
        catch (Exception exc)
        {
            Debug.LogWarning(exc.ToString());
        }
    }

    private void EnableFreeCamera()
    {
        state = GameCameraType.Free;

        playerObserver.Observe(null, 0);
        observeCamera.ObservePlayer(null);

        orbitCamera.targetTransform = null;
        freeCamera.enabled = true;
        orbitCamera.enabled = false;
        focusTargetCamera.enabled = false;

        AllowJoinObserve = false;
    }

    public void ForceFreeCamera()
    {
        forcedFreeCamera = true;
        freeCamera.SlowMotion = true;
        EnableFreeCamera();
    }

    public void ReleaseFreeCamera()
    {
        freeCamera.SlowMotion = false;
        forcedFreeCamera = false;
    }

    public bool ObserveNextPlayer()
    {
        var playerCount = playerManager.GetPlayerCount(true);
        if (playerCount == 0)
        {
            return true;
        }

        state = GameCameraType.Observe;
        focusTargetCamera.enabled = false;
        PlayerController player;

        while (true)
        {
            observedPlayerIndex = (observedPlayerIndex + 1) % playerCount;
            player = playerManager.GetPlayerByIndex(observedPlayerIndex);
            if (!player) return true;
            if (CanObservePlayer(player)) break;
        }

        ObservePlayer(player);
        return false;
    }

    public void EnableRaidCamera()
    {
        if (forcedFreeCamera) return;
        state = GameCameraType.Raid;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        orbitCamera.targetTransform = null;
    }

    public void EnableArenaCamera()
    {
        if (forcedFreeCamera) return;
        state = GameCameraType.Arena;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        orbitCamera.targetTransform = null;
    }

    public void EnableDungeonCamera()
    {
        if (forcedFreeCamera) return;
        state = GameCameraType.Dungeon;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        orbitCamera.targetTransform = null;
    }

    public void DisableFocusCamera()
    {
        if (forcedFreeCamera) return;
        if (state != GameCameraType.Observe)
            ObserveNextPlayer();
    }

    public void DisableDungeonCamera()
    {
        if (forcedFreeCamera) return;
        ObserveNextPlayer();
    }

    public void ObservePlayer(PlayerController player)
    {
        if (forcedFreeCamera) return;
        if (!player) return;
        var subMultiplier = player.IsSubscriber ? 3f : 1f;
        ObservePlayer(player, ObserverJumpTimer * subMultiplier);
    }

    public void ObservePlayer(PlayerController player, float time)
    {
        if (forcedFreeCamera) return;
        if (!CanObservePlayer(player)) return;

        observeNextPlayerTimer = time;
        observeCamera.ObservePlayer(player);
        playerObserver.Observe(player, time);
        freeCamera.enabled = false;
        orbitCamera.targetTransform = player.transform;
        orbitCamera.enabled = true;
    }

    public void EnsureObserverCamera()
    {
        observeCamera.UpdatePlayerLayer();
    }

    private void EnableFocusTargetCamera(Transform transform)
    {
        if (forcedFreeCamera) return;
        freeCamera.enabled = false;
        orbitCamera.enabled = false;
        focusTargetCamera.enabled = true;
        if (transform) focusTargetCamera.Target = transform;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanObservePlayer(PlayerController player)
    {
        return !forcedFreeCamera && !gameManager.StreamRaid.Started || !gameManager.StreamRaid.IsWar || player.StreamRaid.InWar;
    }
}

public enum GameCameraType
{
    Free,
    Observe,
    Arena,
    Raid,
    Dungeon
}
