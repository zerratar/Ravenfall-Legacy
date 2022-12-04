using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts;
using RavenNest.Models;
using RavenNest.SDK;
using RavenNest.SDK.Endpoints;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using RavenNestPlayer = RavenNest.Models.Player;
using Debug = Shinobytes.Debug;

using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using Shinobytes.Linq;
using UnityEngine.AI;

public class GameManager : MonoBehaviour, IGameManager
{
    [SerializeField] private GameCamera gameCamera;
    [SerializeField] private RavenBot commandServer;

    [SerializeField] private PlayerDetails playerObserver;
    [SerializeField] private PlayerList playerList;
    [SerializeField] private GameSettings settings;

    [SerializeField] private TwitchEventManager subEventManager;
    [SerializeField] private DropEventManager dropEventManager;

    [SerializeField] private BATimer boostTimer;
    [SerializeField] private PlayerSearchHandler playerSearchHandler;
    [SerializeField] private GameMenuHandler menuHandler;

    [SerializeField] private string accessKey;

    [SerializeField] private FerryProgress ferryProgress;
    [SerializeField] private GameObject exitView;
    [SerializeField] private MusicManager musicManager;

    [SerializeField] private IslandManager islandManager;
    [SerializeField] private DungeonManager dungeonManager;

    [SerializeField] private VillageManager villageManager;
    [SerializeField] private OnsenManager onsen;

    [SerializeField] private PlayerLogoManager playerLogoManager;
    [SerializeField] private ServerNotificationManager serverNotificationManager;
    [SerializeField] private LoginHandler loginHandler;

    [SerializeField] private GameObject gameReloadMessage;

    [SerializeField] private TavernHandler tavern;

    [SerializeField] private DayNightCycle dayNightCycle;


    [SerializeField] private GameObject gameReloadUIPanel;

    [SerializeField] private Volume postProcessingEffects;

    private readonly ConcurrentDictionary<GameEventType, IGameEventHandler> gameEventHandlers = new();
    private readonly ConcurrentDictionary<Type, IGameEventHandler> typedGameEventHandlers = new();

    private readonly ConcurrentQueue<GameEvent> gameEventQueue = new ConcurrentQueue<GameEvent>();
    private readonly Queue<PlayerController> playerKickQueue = new Queue<PlayerController>();
    private readonly ConcurrentDictionary<string, LoadingState> loadingStates
        = new ConcurrentDictionary<string, LoadingState>();

    private readonly GameEventManager events = new GameEventManager();
    private IoCContainer ioc;
    private FerryController ferryController;
    private ChunkManager chunkManager;
    private PlayerManager playerManager;
    private CraftingManager craftingManager;
    private RaidManager raidManager;
    private StreamRaidManager streamRaidManager;
    private ArenaController arenaController;
    private ItemManager itemManager;

    private int lastButtonIndex = 0;
    public float playerRequestTime = 1f;
    private float playerRequestInterval = 1f;

    public string ServerAddress;

    public bool UsePostProcessingEffects = true;
    public GraphicsToggler Graphics;
    public RavenNestClient RavenNest;
    public ClanManager Clans => clanManager ?? (clanManager = new ClanManager(this));

    public VillageManager Village => villageManager;
    public PlayerLogoManager PlayerLogo => playerLogoManager;
    public TwitchSubscriberBoost Boost => subEventManager.CurrentBoost;
    public TwitchEventManager Twitch => subEventManager;
    public MusicManager Music => musicManager;
    public ChunkManager Chunks => chunkManager;
    public IslandManager Islands => islandManager;
    public PlayerDetails ObservedPlayerDetails => playerObserver;
    public PlayerManager Players => playerManager;
    public ItemManager Items => itemManager;
    public CraftingManager Crafting => craftingManager;
    public ArenaController Arena => arenaController;
    public RaidManager Raid => raidManager;
    public StreamRaidManager StreamRaid => streamRaidManager;
    public DungeonManager Dungeons => dungeonManager;
    public RavenBotConnection RavenBot => commandServer?.Connection;
    public RavenBot RavenBotController => commandServer;
    public FerryController Ferry => ferryController;
    public DropEventManager DropEvent => dropEventManager;
    public GameCamera Camera => gameCamera;
    public GameEventManager Events => events;
    public PlayerList PlayerList => playerList;
    public ServerNotificationManager ServerNotifications => serverNotificationManager;

    private EventTriggerSystem eventTriggerSystem;
    public EventTriggerSystem EventTriggerSystem => eventTriggerSystem ?? (eventTriggerSystem = ioc.Resolve<EventTriggerSystem>());
    public OnsenManager Onsen => onsen;
    public TavernHandler Tavern => tavern;
    public Overlay Overlay => overlay;
    public bool IsSaving => saveCounter > 0;

    private float updateSessionInfoTime = 5f;
    private float saveFrequency = 5f;
    private int saveCounter;

    private RavenNest.SDK.UnityLogger logger;
    private bool gameSessionActive;
    private float expBoostTimerUpdate = 0.5f;
    private float streamerRaidTimer;
    private bool streamerRaidWar;
    private string streamerRaid;
    private TextMeshProUGUI exitViewText;
    private string exitViewTextFormat;
    private DateTime lastGameEventRecevied;
    private bool isReloadingScene;
    private float savingPlayersTime = 0f;
    private float savingPlayersTimeDuration = 60000f;
    private float uptimeSaveTimerInterval = 5f;
    private float uptimeSaveTimer = 5f;

    private int spawnedBots;
    private bool useManualExpMultiplierCheck;

    private bool potatoMode;
    private bool forcedPotatoMode;
    private DateTime dungeonStartTime;
    private DateTime raidStartTime;
    private ClanManager clanManager;

    private float lastServerTimeUpdateFloat;
    private DateTime lastServerTimeUpdateDateTime;
    private DateTime serverTime;

    private readonly TimeSpan dungeonStartCooldown = TimeSpan.FromMinutes(10);
    private readonly TimeSpan raidStartCooldown = TimeSpan.FromMinutes(10);
    private NameTagManager nametagManager;

    public StreamLabel uptimeLabel;
    public StreamLabel villageBoostLabel;
    public StreamLabel playerCountLabel;

    private Permissions permissions = new Permissions();
    public Permissions Permissions
    {
        get { return permissions; }
        set
        {
            permissions = value;
            if (value != null)
            {
                AdminControlData.IsAdmin = permissions.IsAdministrator;
            }
        }
    }
    public bool LogoCensor { get; set; }
    public bool AlertExpiredStateCacheInChat { get; set; } = true;
    public PlayerItemDropMessageSettings ItemDropMessageSettings { get; set; }
    public int PlayerBoostRequirement { get; set; } = 0;

    public bool PotatoMode
    {
        get => forcedPotatoMode || potatoMode;
        set => potatoMode = value;
    }

    public bool RealtimeDayNightCycle
    {
        get => dayNightCycle?.UseRealTime ?? false; set
        {
            if (dayNightCycle)
                dayNightCycle.UseRealTime = value;
        }
    }

    public bool AutoPotatoMode { get; set; }
    public bool IsLoaded { get; private set; }
    public bool DungeonStartEnabled { get; internal set; } = true;
    public bool RaidStartEnabled { get; internal set; } = true;
    public NameTagManager NameTags => nametagManager;
    public StreamLabels StreamLabels { get; private set; }

    private Overlay overlay;

    public bool RequireCodeForDungeonOrRaid;
    private GameCache.LoadStateResult gameCacheStateFileLoadResult;
    private bool stateFileStatusReported;

    void Awake()
    {
        //Physics.autoSimulation = false;

        overlay = gameObject.AddComponent<Overlay>();
        if (!settings) settings = GetComponent<GameSettings>();
        this.StreamLabels = new StreamLabels(settings);
        gameReloadUIPanel.SetActive(false);
        Overlay.CheckIfGame();
        GameSystems.Awake();
    }

    public void SaveNow()
    {
        playerRequestTime = 0.001f;
    }

    // Start is called before the first frame update   
    void Start()
    {

#if DEBUG
        Application.SetStackTraceLogType(LogType.Assert | LogType.Error | LogType.Exception | LogType.Log | LogType.Warning, StackTraceLogType.Full);
#else 
        Application.SetStackTraceLogType(LogType.Log | LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Error | LogType.Exception, StackTraceLogType.ScriptOnly);
#endif

        this.SetupStreamLabels();

        GameCache.IsAwaitingGameRestore = false;
        ioc = GetComponent<IoCContainer>();

        gameReloadMessage.SetActive(false);
        if (!Graphics) Graphics = FindObjectOfType<GraphicsToggler>();
        if (!nametagManager) nametagManager = FindObjectOfType<NameTagManager>();
        if (!dayNightCycle) dayNightCycle = GetComponent<DayNightCycle>();
        if (!onsen) onsen = GetComponent<OnsenManager>();
        if (!loginHandler) loginHandler = FindObjectOfType<LoginHandler>();
        if (!dropEventManager) dropEventManager = GetComponent<DropEventManager>();
        if (!ferryProgress) ferryProgress = FindObjectOfType<FerryProgress>();
        if (!gameCamera) gameCamera = FindObjectOfType<GameCamera>();

        if (!playerLogoManager) playerLogoManager = GetComponent<PlayerLogoManager>();
        if (!villageManager) villageManager = FindObjectOfType<VillageManager>();

        if (!subEventManager) subEventManager = GetComponent<TwitchEventManager>();
        if (!subEventManager) subEventManager = gameObject.AddComponent<TwitchEventManager>();

        if (!commandServer) commandServer = GetComponent<RavenBot>();
        if (!islandManager) islandManager = GetComponent<IslandManager>();
        if (!itemManager) itemManager = GetComponent<ItemManager>();
        if (!playerManager) playerManager = GetComponent<PlayerManager>();
        if (!chunkManager) chunkManager = GetComponent<ChunkManager>();
        if (!craftingManager) craftingManager = GetComponent<CraftingManager>();
        if (!raidManager) raidManager = GetComponent<RaidManager>();
        if (!streamRaidManager) streamRaidManager = GetComponent<StreamRaidManager>();
        if (!arenaController) arenaController = FindObjectOfType<ArenaController>();

        if (!ferryController) ferryController = FindObjectOfType<FerryController>();
        if (!musicManager) musicManager = GetComponent<MusicManager>();

        RegisterGameEventHandler<ItemAddEventHandler>(GameEventType.ItemAdd);
        RegisterGameEventHandler<ResourceUpdateEventHandler>(GameEventType.ResourceUpdate);
        RegisterGameEventHandler<ServerMessageEventHandler>(GameEventType.ServerMessage);

        RegisterGameEventHandler<PermissionChangedEventHandler>(GameEventType.PermissionChange);
        RegisterGameEventHandler<VillageInfoEventHandler>(GameEventType.VillageInfo);
        RegisterGameEventHandler<VillageLevelUpEventHandler>(GameEventType.VillageLevelUp);

        RegisterGameEventHandler<ClanLevelChangedEventHandler>(GameEventType.ClanLevelChanged);
        RegisterGameEventHandler<ClanSkillLevelChangedEventHandler>(GameEventType.ClanSkillLevelChanged);
        RegisterGameEventHandler<ServerTimeEventHandler>(GameEventType.ServerTime);

        RegisterGameEventHandler<PlayerRestedUpdateEventHandler>(GameEventType.PlayerRestedUpdate);

        RegisterGameEventHandler<PubSubTokenReceivedEventHandler>(GameEventType.PubSubToken);

        RegisterGameEventHandler<ExpMultiplierEventHandler>(GameEventType.ExpMultiplier);

        RegisterGameEventHandler<PlayerRemoveEventHandler>(GameEventType.PlayerRemove);
        RegisterGameEventHandler<PlayerAddEventHandler>(GameEventType.PlayerAdd);
        RegisterGameEventHandler<PlayerExpUpdateEventHandler>(GameEventType.PlayerExpUpdate);
        RegisterGameEventHandler<PlayerJoinArenaEventHandler>(GameEventType.PlayerJoinArena);
        RegisterGameEventHandler<PlayerJoinDungeonEventHandler>(GameEventType.PlayerJoinDungeon);
        RegisterGameEventHandler<PlayerJoinRaidEventHandler>(GameEventType.PlayerJoinRaid);
        RegisterGameEventHandler<PlayerNameUpdateEventHandler>(GameEventType.PlayerNameUpdate);
        RegisterGameEventHandler<PlayerTaskEventHandler>(GameEventType.PlayerTask);


        RegisterGameEventHandler<StreamerWarRaidEventHandler>(GameEventType.WarRaid);
        RegisterGameEventHandler<StreamerRaidEventHandler>(GameEventType.Raid);
        RegisterGameEventHandler<PlayerAppearanceEventHandler>(GameEventType.PlayerAppearance);
        RegisterGameEventHandler<ItemBuyEventHandler>(GameEventType.ItemBuy);
        RegisterGameEventHandler<ItemSellEventHandler>(GameEventType.ItemSell);

        commandServer.Initialize(this);

        LoadGameSettings();

        musicManager.PlayBackgroundMusic();

        this.EventTriggerSystem.SourceTripped += OnSourceTripped;

        gameCacheStateFileLoadResult = GameCache.LoadState();

        GameSystems.Start();
    }

    private void OnSourceTripped(object sender, EventTriggerSystem.SysEventStats e)
    {
        if (!RavenNest.Authenticated || !RavenNest.WebSocket.IsReady)
        {
            return;
        }

        RavenNest.WebSocket.UpdatePlayerEventStatsAsync(e);
    }

    private void SetupStreamLabels()
    {
        uptimeLabel = StreamLabels.Register("uptime", () => Time.realtimeSinceStartup.ToString());
        villageBoostLabel = StreamLabels.Register("village-boost", () =>
        {
            var bonuses = Village.GetExpBonuses();

            var value = bonuses.Where(x => x.Bonus > 0)
             .GroupBy(x => x.SlotType)
             .Where(x => x.Key != TownHouseSlotType.Empty && x.Key != TownHouseSlotType.Undefined)
             .Select(x => $"{x.Key} {x.Value.Sum(y => y.Bonus)}%");


            return string.Join(", ", value);
        });
        playerCountLabel = StreamLabels.Register("online-player-count", () => playerManager.GetPlayerCount().ToString());
    }

    private void LoadGameSettings()
    {
        var settings = PlayerSettings.Instance;

        TwitchEventManager.AnnouncementTimersSeconds = settings.ExpMultiplierAnnouncements ?? TwitchEventManager.AnnouncementTimersSeconds;

        UsePostProcessingEffects = settings.PostProcessing.GetValueOrDefault();
        AutoPotatoMode = settings.AutoPotatoMode.GetValueOrDefault();
        PotatoMode = settings.PotatoMode.GetValueOrDefault();
        RealtimeDayNightCycle = settings.RealTimeDayNightCycle.GetValueOrDefault();
        PlayerBoostRequirement = settings.PlayerBoostRequirement.GetValueOrDefault();
        AlertExpiredStateCacheInChat = settings.AlertExpiredStateCacheInChat.GetValueOrDefault();
        ItemDropMessageSettings = (PlayerItemDropMessageSettings)settings.ItemDropMessageType.GetValueOrDefault((int)ItemDropMessageSettings);
        PlayerList.Bottom = settings.PlayerListSize.GetValueOrDefault(PlayerList.Bottom);
        PlayerList.Scale = settings.PlayerListScale.GetValueOrDefault(PlayerList.Scale);
        Raid.Notifications.volume = settings.RaidHornVolume.GetValueOrDefault(Raid.Notifications.volume);
        Music.volume = settings.MusicVolume.GetValueOrDefault(Music.volume);

        OrbitCamera.RotationSpeed = settings.CameraRotationSpeed.GetValueOrDefault(OrbitCamera.RotationSpeed);
        SettingsMenuView.SetResolutionScale(settings.DPIScale.GetValueOrDefault(1f));
    }

    internal void OnSessionStart()
    {
        commandServer.UpdateSessionInfo();

        if (RavenBot.UseRemoteBot && RavenBot.IsConnectedToRemote)
        {
            RavenBot.SendSessionOwner(this.RavenNest.TwitchUserId, this.RavenNest.TwitchUserName, this.RavenNest.SessionId);
        }
    }

    private void RegisterGameEventHandler<T>(GameEventType type) where T : IGameEventHandler, new()
    {
        var value = new T();
        var targetType = typeof(T);
        gameEventHandlers[type] = value;
        typedGameEventHandlers[targetType] = value;
    }
    private IGameEventHandler GetEventHandler(GameEventType type)
    {
        if (gameEventHandlers.TryGetValue(type, out var handler))
        {
            return handler;
        }

        return null;
    }

    private IGameEventHandler<T> GetEventHandler<T>()
    {
        // a little bit more expensive, but it will do.
        var targetType = typeof(T);
        if (typedGameEventHandlers.TryGetValue(targetType, out var handler) && handler is IGameEventHandler<T> typed)
        {
            return typed;
        }

        foreach (var value in gameEventHandlers.Values)
        {
            var t = value as IGameEventHandler<T>;
            if (t != null)
            {
                typedGameEventHandlers[targetType] = t;
                return t;
            }
        }

        return default;
    }

    public void SaveStateAndShutdownGame(bool activateTempLogin = true)
    {
        if (activateTempLogin)
        {
            if (!loginHandler) return;
            loginHandler.ActivateTempAutoLogin();
        }

        SavePlayerStates();
        Application.Quit();
    }
    public void SavePlayerStates()
    {
        GameCache.SavePlayersState(this.playerManager.GetAllPlayers());

        //var gc = GameCache;
        //var players = ;

        //gc.SetPlayersState(players);
        //gc.BuildState();
        //gc.SaveState();
    }

    public void ReloadScene()
    {
        RavenNest.WebSocket.Close();
        RavenBot.Stop(false);
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }

    public void ReloadGame()
    {
        if (!loginHandler) return;

        isReloadingScene = true;
        loginHandler.ActivateTempAutoLogin();

        SavePlayerStates();

        GameCache.IsAwaitingGameRestore = true;

        RavenNest.WebSocket.Close();
        RavenNest.Tcp.Dispose();
        RavenBot.Dispose();

        gameReloadMessage.SetActive(true);

        ReloadScene();
    }

    private IEnumerator RestoreGameState(GameCacheState state)
    {
        GameCache.IsAwaitingGameRestore = false;

        gameReloadUIPanel.SetActive(true);
        try
        {
            if (state.Players == null || state.Players.Count == 0)
                yield break;

            yield return UnityEngine.Resources.UnloadUnusedAssets();

            Shinobytes.Debug.Log("Restoring game state with " + state.Players.Count + " players.");

            var waitTime = 0;
            // if we got disconnected or something
            while ((!RavenNest.Authenticated || !RavenNest.SessionStarted || (!RavenNest.WebSocket.IsReady && !RavenNest.Tcp.Connected)) && waitTime < 100)
            {
                yield return new WaitForSeconds(1);
                waitTime++;
            }

            // still not connected? retry later.
            if (!RavenNest.Authenticated || !RavenNest.SessionStarted || (!RavenNest.WebSocket.IsReady && !RavenNest.Tcp.Connected))
            {
                Shinobytes.Debug.LogWarning("No conneciton to server when trying to restore players. Retrying");
                GameCache.SetState(state);
                yield break;
            }

            // RestoreAsync

            yield return Players.RestoreAsync(state.Players);
        }
        finally
        {
            gameReloadUIPanel.SetActive(false);
        }

        //foreach (var player in state.Players)
        //{
        //    if (player.TwitchUser == null || string.IsNullOrEmpty(player.TwitchUser.UserId))
        //    {
        //        Debug.LogError("Unable to restore character, TwitchUser is null or missing UserId.");
        //        yield return null;
        //    }


        //    yield return Players.JoinAsync(player.TwitchUser, RavenBot.ActiveClient, false, false, player.CharacterId);
        //}

        //SavePlayerStates();
    }

    // Update is called once per frame
    void Update()
    {
        if (isReloadingScene)
        {
            return;
        }

        GameSystems.Update();

        //if (Input.GetKeyDown(KeyCode.L))
        //{
        //    (new StreamerRaidEventHandler()).Handle(this, JsonConvert.SerializeObject(new StreamRaidInfo()
        //    {
        //        RaiderUserId = "72424639",
        //        RaiderUserName = "zerratar",
        //        Players = new List<UserCharacter>
        //        {
        //            new UserCharacter()
        //            {
        //                CharacterId = new Guid("0d4e920d-64a7-4b98-9c78-01dd1913824b"),
        //                UserId ="72424639",
        //                Username = "zerratar"
        //            }
        //        }
        //    }));
        //}

        if (AutoPotatoMode)
        {
            forcedPotatoMode = !Application.isFocused;
        }

        var currentQualityLevel = QualitySettings.GetQualityLevel();
        if (PotatoMode)
        {
            if (currentQualityLevel != 0)
            {
                QualitySettings.SetQualityLevel(0);
            }

            DisablePostProcessingEffects();
        }
        else
        {

            if (currentQualityLevel != 1)
            {
                QualitySettings.SetQualityLevel(1);
            }

            EnablePostProcessingEffects();
        }

        UpdateIntegrityCheck();

        if (Input.GetKeyDown(KeyCode.F11) && RavenBot.UseRemoteBot && !RavenBot.IsConnectedToLocal)
        {
            RavenBot.Disconnect(BotConnectionType.Remote);
            return;
        }

        var f12Down = Input.GetKeyDown(KeyCode.F12);
        if (f12Down && Input.GetKey(KeyCode.LeftShift))
        {
            RavenNest.Tcp.Enabled = !RavenNest.Tcp.Enabled;
            return;
        }
        else if (f12Down)
        {
            RavenNest.WebSocket.Reconnect();
            RavenNest.Tcp.Disconnect();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            ReloadGame();
            return;
        }

        if (UpdateExitView())
        {
            return;
        }

        if (Input.GetKeyUp(KeyCode.Escape) && !menuHandler.Visible && !playerSearchHandler.Visible)
        {
            menuHandler.Show();
        }

        UpdateApiCommunication();

        if (RavenNest == null || !RavenNest.Authenticated)
        {
            return;
        }

        if (!UpdateGameEvents())
        {
            return;
        }

        ExpMultiplierChecker.RunAsync(this);

        if (GameCache.IsAwaitingGameRestore
            && RavenNest.Authenticated
            && RavenNest.SessionStarted
            && RavenNest.WebSocket.IsReady
            && Items.Loaded)
        {
            GameCache.IsAwaitingGameRestore = false;
            var reloadState = GameCache.GetReloadState();
            if (reloadState != null)
            {
                StartCoroutine(RestoreGameState(reloadState.Value));
                return;
            }
        }
        else if (
             RavenNest.Authenticated &&
             RavenNest.SessionStarted &&
             RavenBot.IsConnected &&
            !stateFileStatusReported && gameCacheStateFileLoadResult == GameCache.LoadStateResult.Expired)
        {
            if (AlertExpiredStateCacheInChat)
            {
                RavenBot.Announce("Player restore state file has expired. No players has been added back.");
            }

            stateFileStatusReported = true;
        }

        if (uptimeSaveTimer > 0)
            uptimeSaveTimer -= Time.deltaTime;

        if (uptimeSaveTimer <= 0)
        {
            uptimeSaveTimer = uptimeSaveTimerInterval;
            uptimeLabel.Update();
        }

        UpdateExpBoostTimer();

        UpdatePlayerKickQueue();

        HandleKeyDown();

        UpdateChatBotCommunication();
    }

    public void EnablePostProcessingEffects()
    {
        if (!UsePostProcessingEffects)
        {
            DisablePostProcessingEffects();
            return;
        }

        postProcessingEffects.weight = 0.625f;
    }
    public void DisablePostProcessingEffects()
    {
        postProcessingEffects.weight = 0f;
    }

    private void UpdateIntegrityCheck()
    {
        IntegrityCheck.Update();
    }

    private bool UpdateGameEvents()
    {
        if (gameEventQueue.TryDequeue(out var ge))
        {
            HandleGameEvent(ge);
        }

        return true;
    }


    public bool UpdateExitView()
    {
        if (streamerRaidTimer <= 0)
        {
            return false;
        }

        if (exitViewText && exitView.activeSelf)
        {
            streamerRaidTimer -= Time.deltaTime;
            exitViewText.text = string.Format(
                exitViewTextFormat,
                streamerRaid,
                Mathf.FloorToInt(streamerRaidTimer)
            );

            if (streamerRaidTimer <= 0f)
            {
                //Application.Quit();
                SaveStateAndShutdownGame(false);
            }

            return true;
        }
        return false;
    }

    internal void BeginStreamerRaid(string username, bool war)
    {
        exitView.SetActive(true);
        streamerRaidTimer = 15f;
        streamerRaidWar = war;
        streamerRaid = username;

        exitViewText = exitView.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        exitViewTextFormat = exitViewText.text;
    }

    public void SetLoadingState(string key, LoadingState state)
    {
        loadingStates[key] = state;
        IsLoaded = loadingStates.All(x => x.Value == LoadingState.Loaded);
    }

    public void OnAuthenticated()
    {
        //ShowFerryProgress();
    }

    public void ShowFerryProgress()
    {
        ferryProgress.gameObject.SetActive(true);
    }

    internal void SetTimeOfDay(int totalTime, int freezeTime)
    {
        dayNightCycle.SetTimeOfDay(totalTime, freezeTime);
    }
    public void QueueRemovePlayer(PlayerController player)
    {
        playerKickQueue.Enqueue(player);
        player.OnKicked();
    }
    public void RemovePlayer(PlayerController player, bool notifyServer = true)
    {
        if (player.Dungeon.InDungeon)
        {
            dungeonManager.Remove(player);
        }

        if (player.Raid.InRaid)
        {
            raidManager.Leave(player);
        }


        if (player.Ferry.OnFerry)
        {
            player.Ferry.RemoveFromFerry();
        }

        if (notifyServer)
        {
            RavenNest.PlayerRemoveAsync(player);
        }

        player.Removed = true;
        playerList.RemovePlayer(player);
        playerManager.Remove(player);

        if (gameCamera.Observer != null && gameCamera.Observer.ObservedPlayer == player)
        {
            gameCamera.ObservePlayer(null);
        }

        villageBoostLabel.Update();
        playerCountLabel.Update();
        SavePlayerStates();
        UpdatePathfindingIterations();
    }

    public void UpdatePathfindingIterations()
    {
        var qualitySettingsIndex = PlayerSettings.Instance.PathfindingQualitySettings.GetValueOrDefault(1);
        var min = SettingsMenuView.PathfindingQualityMin[qualitySettingsIndex];
        var max = SettingsMenuView.PathfindingQualityMax[qualitySettingsIndex];
        var value = playerManager.GetPlayerCount() * 2;
        NavMesh.pathfindingIterationsPerFrame = Mathf.Min(Mathf.Max(min, value), max);
    }

    public async void SpawnManyBotPlayers(int count)
    {
        for (var i = 0; i < count; ++i)
        {
            await SpawnBotPlayer();
            await Task.Delay(10);
        }
    }

    public async Task SpawnBotPlayerOnFerry()
    {
        BotPlayerGenerator.Instance.NextOnFerry = true;
        var playerInfo = await GenerateBotInfoAsync();
        var player = await Players.JoinAsync(playerInfo, RavenBot.ActiveClient, false, true);
        if (player)
            await player.EquipBestItemsAsync();
        ++spawnedBots;
    }

    public async Task SpawnBotPlayer()
    {
        var playerInfo = await GenerateBotInfoAsync();
        var player = await Players.JoinAsync(playerInfo, RavenBot.ActiveClient, false, true);
        if (player)
            await player.EquipBestItemsAsync();
        ++spawnedBots;
    }

    private async Task<TwitchPlayerInfo> GenerateBotInfoAsync()
    {
        var id = Random.Range(10000, 99999);
        var userId = "#" + id;

        while (playerManager.GetPlayerByUserId(userId))
        {
            id = Random.Range(10000, 99999);
            userId = "#" + id;
            await Task.Delay(5);
        }

        var userName = "Bot" + id;
        return new TwitchPlayerInfo(userId, userName, userName, "#ffffff", false, false, false, false, "1");
    }

    public PlayerController SpawnPlayer(
        RavenNest.Models.Player playerDefinition,
        TwitchPlayerInfo streamUser = null,
        StreamRaidInfo raidInfo = null,
        bool isGameRestore = false)
    {
        if (!chunkManager)
        {
            Debug.LogError("No chunk manager available!");
            return null;
        }

        var starter = chunkManager.GetStarterChunk();
        if (starter == null)
        {
            Debug.LogError("No starter chunk available!");
            return null;
        }

        var spawnPoint = starter.GetPlayerSpawnPoint();
        //var startIsland = playerDefinition?.State?.Island;
        //var island = Islands.Find(startIsland);
        //if (island != null)
        //{
        //    spawnPoint = island.SpawnPosition;
        //}

        var vector3 = Random.insideUnitSphere + (Vector3.up * 2f);
        var player = playerManager.Spawn(spawnPoint + vector3, playerDefinition, streamUser, raidInfo);

        if (!player)
        {
            Debug.LogError("Can't spawn player, player is already playing.");
            return null;
        }

        playerList.AddPlayer(player);

        if (!isGameRestore)
        {
            Village.TownHouses.EnsureAssignPlayerRows(Players.GetPlayerCount());

            playerCountLabel.Update();
            villageBoostLabel.Update();

            if (player && gameCamera && gameCamera.AllowJoinObserve)
                gameCamera.ObservePlayer(player);
        }

        if (dropEventManager.IsActive)
            player.BeginItemDropEvent();

        UpdatePathfindingIterations();
        return player;
    }

    public void PostGameRestore()
    {
        Village.TownHouses.EnsureAssignPlayerRows(Players.GetPlayerCount());
        playerCountLabel.Update();
        villageBoostLabel.Update();

        var player = playerManager.LastAddedPlayer;
        if (player && gameCamera && gameCamera.AllowJoinObserve)
            gameCamera.ObservePlayer(player);
    }

    public void HandleGameEvents(EventList gameEvents)
    {
        lastGameEventRecevied = DateTime.UtcNow;

        foreach (var ge in gameEvents.Events)
        {
            gameEventQueue.Enqueue(ge);
        }
    }

    public void HandleGameEvent(GameEvent gameEvent)
    {
        if (gameEvent == null)
            return;

        var handler = GetEventHandler((GameEventType)gameEvent.Type);
        if (handler != null)
        {
            handler.Handle(this, gameEvent.Data);
            return;
        }
    }

    public void HandleGameEvent<TEventData>(TEventData gameEvent)
    {
        if (gameEvent == null)
            return;

        var handler = GetEventHandler<TEventData>();
        if (handler != null)
        {
            handler.Handle(this, gameEvent);
            return;
        }
    }

    internal async Task<PlayerController> AddPlayerByCharacterIdAsync(Guid characterId, StreamRaidInfo raiderInfo)
    {
        var playerInfo = await RavenNest.PlayerJoinAsync(new PlayerJoinData
        {
            CharacterId = characterId
        });

        if (playerInfo == null || !playerInfo.Success)
        {
            Shinobytes.Debug.LogError("Unable to add character with id: " + characterId + " " + (playerInfo != null ? playerInfo.ErrorMessage : ""));
            return null;
        }
        return SpawnPlayer(playerInfo.Player, raidInfo: raiderInfo);
    }

    //internal async Task<PlayerController> AddPlayerByUserIdAsync(string userId, StreamRaidInfo raiderInfo)
    //{
    //    var playerInfo = await RavenNest.PlayerJoinAsync(new PlayerJoinData
    //    {
    //        UserId = userId,
    //        UserName = "",
    //        Identifier = "1"
    //    });

    //    if (playerInfo == null || !playerInfo.Success)
    //    {

    //        return null;
    //    }

    //    return SpawnPlayer(playerInfo.Player, raidInfo: raiderInfo);
    //}

    private void OnApplicationQuit()
    {
        RavenBot.Dispose();

        StopRavenNestSession();

        Debug.Log("Application ending after " + Time.time + " seconds");
    }

    private void HandleRavenNestConnection()
    {
        var client = RavenNest;
        if (logger == null)
            logger = new RavenNest.SDK.UnityLogger();

        if (client == null)
        {
            client = new RavenNestClient(logger, this,
            new ProductionRavenNestStreamSettings()
            //new StagingRavenNestStreamSettings()
            //new LocalRavenNestStreamSettings()
            //new UnsecureLocalRavenNestStreamSettings()
            );

            RavenNest = client;
        }

        if (client != null)
        {
            //ravenNest.Update();
            if (!string.IsNullOrEmpty(RavenNest.ServerAddress))
                ServerAddress = RavenNest.ServerAddress;
        }
    }

    public async Task<bool> RavenNestLoginAsync(string username, string password)
    {
        if (RavenNest == null) return false;
        if (RavenNest.Authenticated) return true;
        return await RavenNest.LoginAsync(username, password);
    }

    private void RavenNestUpdate()
    {
        if (RavenNest.HasActiveRequest)
        {
            return;
        }

        if (RavenNest.BadClientVersion)
        {
            return;
        }

        if (RavenNest.AwaitingSessionStart)
        {
            return;
        }

        if (RavenNest.Authenticated && !RavenNest.SessionStarted)
        {
            RavenNest.StartSession(Ravenfall.Version, accessKey);
            //if (await ravenNest.StartSessionAsync(Ravenfall.Version, accessKey, false))
            //{
            //    gameSessionActive = true;
            //    lastGameEventRecevied = DateTime.UtcNow;
            //}
        }

        if (!RavenNest.Authenticated || !RavenNest.SessionStarted)
        {
            return;
        }

        if (!gameSessionActive)
        {
            gameSessionActive = true;
            lastGameEventRecevied = DateTime.UtcNow;
        }

        if (updateSessionInfoTime > 0)
        {
            updateSessionInfoTime -= Time.deltaTime;
        }

        if (RavenBot.UseRemoteBot && !RavenBot.IsConnectedToRemote && !RavenBot.IsConnectedToLocal)
        {
            RavenBot.Connect(BotConnectionType.Remote);

            if (RavenNest.Authenticated && RavenNest.SessionStarted && RavenBot.UseRemoteBot && RavenBot.IsConnectedToRemote)
            {
                commandServer.UpdateSessionInfo();
            }
        }

        if (RavenNest.Authenticated && RavenNest.SessionStarted)
        {
            if (Players.LoadingPlayers)
            {
                return;
            }

            if (playerRequestTime >= 0)
            {
                playerRequestTime -= Time.deltaTime;
                if (playerRequestTime < 0f)
                {
                    SendPlayerRequests();
                    playerRequestTime = Players.LoadingPlayers ? playerRequestInterval * 3 : playerRequestInterval;
                }
            }
        }

        if (updateSessionInfoTime <= 0f)
        {
            updateSessionInfoTime = saveFrequency;
            //await SavePlayersAsync();

            if (RavenNest.Authenticated && RavenNest.SessionStarted && RavenBot.UseRemoteBot && RavenBot.IsConnectedToRemote)
            {
                commandServer.UpdateSessionInfo();
            }
        }

        if (savingPlayersTime > 0)
        {
            savingPlayersTime -= Time.deltaTime;
        }
    }

    public void StopRavenNestSession()
    {
        if (gameSessionActive)
        {
            SavePlayers();

            RavenNest.Terminate();
        }
    }

    private void SendPlayerRequests()
    {
        try
        {
            var players = playerManager.GetAllPlayers();
            var now = Time.realtimeSinceStartup;
            var failCount = 0;

            for (var playerIndex = 0; playerIndex < players.Count; ++playerIndex)
            {
                var player = players[playerIndex];
                if (player.IsBot)
                    continue;
                if (player.RequestQueue.TryDequeue(out var req))
                {
                    if (!req.Invoke())
                    {
                        failCount++;
                    }
                }

                player.UpdateRequestQueue();
            }

            if (failCount > 0)
            {
                Debug.LogError("Failed to send player requests for " + failCount + " out of " + players.Count + " players.");
            }
            var elapsed = Time.realtimeSinceStartup - now;
            //Shinobytes.Debug.Log("SendPlayerRequests: " + players.Count + " players, took " + (elapsed * 1000) + "ms");
        }
        catch (Exception exc)
        {
            Debug.LogError(exc);
        }
    }

    private void SavePlayers()
    {
        try
        {
            if (savingPlayersTime > 0) return;
            var players = playerManager.GetAllPlayers();
            var failedToSave = new List<PlayerController>();
            if (players.Count == 0) return;
            //Debug.Log($"Saving {players.Count} players...");
            try
            {
                savingPlayersTime = savingPlayersTimeDuration;
                // Save using websocket
                foreach (var player in players)
                {
                    RavenNest.SavePlayer(player);
                    //if (await ravenNest.SavePlayerAsync(player))
                    //{
                    //    player.SavedSucceseful();
                    //}
                    //else
                    //{
                    //    player.FailedToSave();
                    //    failedToSave.Add(player);
                    //}
                    //await Task.Delay(5);
                }
            }
            catch (Exception exc)
            {
                Debug.LogError(exc);
            }

            //if (failedToSave.Count > 0)
            //{
            //    await SavePlayersUsingHTTP(failedToSave);
            //}
        }
        catch (Exception exc)
        {
            Debug.LogError(exc);
        }

        savingPlayersTime = 0;
    }

    //private async Task SavePlayersUsingHTTP(IReadOnlyList<PlayerController> players)
    //{
    //    Debug.LogWarning($"Fallbacking to HTTP Endpoint Saving {players.Count} players...");
    //    var states = players
    //        .Select(x => x.BuildPlayerState())
    //        .ToArray();

    //    // fall back to HTTPS Post Save
    //    var batchSize = 20;
    //    for (var i = 0; i < states.Length;)
    //    {
    //        var toUpdate = states.Skip(i * batchSize).Take(batchSize).ToArray();
    //        var remaining = states.Length - i;
    //        i += remaining < batchSize ? remaining : batchSize;

    //        var result = await ravenNest.Players.UpdateManyAsync(toUpdate);
    //        if (result == null)
    //        {
    //            Debug.LogWarning($"Saving gave null result. Data may not have been saved.");
    //            continue;
    //        }

    //        for (var playerIndex = 0; playerIndex < result.Length; ++playerIndex)
    //        {
    //            if (players.Count <= playerIndex)
    //            {
    //                Debug.LogWarning($"Player at index {playerIndex} did not exist ingame. Skipping");
    //                continue;
    //            }

    //            var playerResult = new { Player = players[playerIndex], Successeful = result[playerIndex] };
    //            if (playerResult.Successeful)
    //            {
    //                playerResult.Player.SavedSucceseful();
    //            }
    //            else
    //            {
    //                playerResult.Player.FailedToSave();
    //                Debug.LogWarning($"{playerResult.Player.Name} was not saved. In another session?");
    //            }
    //        }

    //        await Task.Delay(1000);
    //    }
    //}

    private void UpdateChatBotCommunication()
    {
        if (RavenBot == null || !RavenBot.IsBound)
        {
            return;
        }

        RavenBot.HandleNextPacket(this, RavenBot, playerManager);
    }

    private IEnumerator TestSubsAndCheers()
    {
        const string userIdZerratar = "72424639";
        const string userIdAbby = "39575045";

        string[] randomUserIds = new string[] {
            "559852513",
            "244495308",
            "269415137",
            "51961033",
            "21747441",
            "38809039",
            "119132738",
            "97907194",
            "120241807",
            "244444961"
        };


        // Test cheer bits, not in game
        {
            Shinobytes.Debug.Log("Test 1: Cheering bits without being in game");
            var cheer = new TwitchCheer(userIdZerratar, "zerratar", "Zerratar", true, true, true, 10);
            RavenNest.EnqueueLoyaltyUpdate(cheer);
            Twitch.OnCheer(cheer);
        }

        // Subscriber, no Receiver, not ingame
        yield return new WaitForSecondsRealtime(1.0f);
        {
            Shinobytes.Debug.Log("Test 2: Subscribe, no receiver, not in game");
            var sub_noReceiver = new TwitchSubscription(userIdZerratar, "zerratar", "Zerratar", null, true, true, 1, true);
            RavenNest.EnqueueLoyaltyUpdate(sub_noReceiver);
            Twitch.OnSubscribe(sub_noReceiver);
        }


        // Gift sub to player not in game (neither players in game)
        yield return new WaitForSecondsRealtime(1.0f);
        {
            Shinobytes.Debug.Log("Test 3: Gift sub, neither in game");
            var sub_notInGame = new TwitchSubscription(userIdZerratar, "zerratar", "Zerratar", userIdAbby, true, true, 1, true);
            RavenNest.EnqueueLoyaltyUpdate(sub_notInGame);
            Twitch.OnSubscribe(sub_notInGame);
        }

        // Subscriber, no Receiver, in game
        yield return new WaitForSecondsRealtime(1.0f);
        {
            Shinobytes.Debug.Log("Test 4: Subscribe, no receiver, in game");
            Shinobytes.Debug.Log(" -1/2-: Adding player....");

            Players.JoinAsync(
                new TwitchPlayerInfo(userIdZerratar, "zerratar", "Zerratar", "", true, true, false, true, "1")
                , null, false);

            while (!Players.Contains(userIdZerratar))
            {
                yield return new WaitForSecondsRealtime(0.5f);
            }

            Shinobytes.Debug.Log(" -2/2-: Subscribe....");
            var sub_noReceiver = new TwitchSubscription(userIdZerratar, "zerratar", "Zerratar", null, true, true, 1, true);
            RavenNest.EnqueueLoyaltyUpdate(sub_noReceiver);
            Twitch.OnSubscribe(sub_noReceiver);
        }

        // Test cheer bits, in game
        yield return new WaitForSecondsRealtime(1.0f);
        {
            Shinobytes.Debug.Log("Test 5: Cheering bits when in game");
            var cheer = new TwitchCheer(userIdZerratar, "zerratar", "Zerratar", true, true, true, 10);
            RavenNest.EnqueueLoyaltyUpdate(cheer);
            Twitch.OnCheer(cheer);
        }

        // Gift sub to a player in game (gifter not in game)
        yield return new WaitForSecondsRealtime(1.0f);
        {
            Shinobytes.Debug.Log("Test 6: Gift sub when not in game to a player");
            var sub_notInGame = new TwitchSubscription(userIdAbby, "abbycottontail", "AbbyCottontail", userIdZerratar, true, true, 1, true);
            RavenNest.EnqueueLoyaltyUpdate(sub_notInGame);
            Twitch.OnSubscribe(sub_notInGame);
        }

        // Gift 10 sub to random players
        yield return new WaitForSecondsRealtime(1.0f);
        {
            Shinobytes.Debug.Log("Test 7: Gift " + randomUserIds.Length + " subs");
            for (var i = 0; i < randomUserIds.Length; ++i)
            {
                Shinobytes.Debug.Log(" Gift " + (i + 1) + "/" + randomUserIds.Length);
                var sub_notInGame = new TwitchSubscription(userIdAbby, "abbycottontail", "AbbyCottontail", randomUserIds[i], true, true, 1, true);
                RavenNest.EnqueueLoyaltyUpdate(sub_notInGame);
                Twitch.OnSubscribe(sub_notInGame);
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }

        // Gift sub to a player in game (both in game)
        yield return new WaitForSecondsRealtime(1.0f);
        {
            Shinobytes.Debug.Log("Test 8: Gift sub when both in game");
            Shinobytes.Debug.Log(" -1/2-: Adding player....");
            Players.JoinAsync(
                new TwitchPlayerInfo(userIdAbby, "abbycottontail", "AbbyCottontail", "", true, true, false, true, "1")
                , null, false);

            while (!Players.Contains(userIdAbby))
            {
                yield return new WaitForSecondsRealtime(0.5f);
            }

            Shinobytes.Debug.Log(" -2/2-: Gift sub....");
            var sub_notInGame = new TwitchSubscription(userIdAbby, "abbycottontail", "AbbyCottontail", userIdZerratar, true, true, 1, true);
            RavenNest.EnqueueLoyaltyUpdate(sub_notInGame);
            Twitch.OnSubscribe(sub_notInGame);
        }
    }

    //private IslandController lastIslandToggle = null;
    private void HandleKeyDown()
    {
        var isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var isControlDown = isShiftDown || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (Input.GetKeyUp(KeyCode.F2))
        {
            Camera.Observer.ToggleVisibility();
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            LogoCensor = !LogoCensor;
        }

        if (Input.GetKeyUp(KeyCode.I))
        {
            playerList.ToggleExpRate();
        }

        if (isControlDown && Input.GetKeyUp(KeyCode.F))
        {
            playerSearchHandler.Show();
        }

        // Since we are no longer using concurrent dictionary to track state
        // we can't have streamers triggering save players
        //if (isControlDown && Input.GetKeyUp(KeyCode.S))
        //{
        //    SavePlayers();
        //}

        if (Permissions.IsAdministrator)
        {

            //if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyUp(KeyCode.Comma))
            //{
            //    StartCoroutine(TestSubsAndCheers());
            //    return;
            //}

            if (isControlDown && Input.GetKeyUp(KeyCode.C))
            {
                Twitch.OnCheer(new TwitchCheer("72424639", "zerratar", "Zerratar", true, true, true, 10));
                return;
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.X))
            {
                Twitch.OnSubscribe(new TwitchSubscription("72424639", "zerratar", "Zerratar", null, true, true, 1, true));
                return;
            }


            if (isControlDown && Input.GetKeyUp(KeyCode.R))
            {
                var players = Players.GetAllGameAdmins();
                Dungeons.Dungeon.RewardItemDrops(players);
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.O))
            {
                var elapsed = DateTime.UtcNow - dungeonStartTime;
                if (elapsed > dungeonStartCooldown || Permissions.IsAdministrator)
                {
                    dungeonStartTime = DateTime.UtcNow;
                    Dungeons.ActivateDungeon();
                }
                else
                {
                    var timeLeft = dungeonStartCooldown - elapsed;
                    RavenBot.Announce("You have to wait {cooldown} seconds before you can start another raid.", timeLeft.TotalSeconds.ToString());
                }
            }

            //if (isControlDown && Input.GetKeyUp(KeyCode.R))
            //{
            //    var elapsed = DateTime.UtcNow - raidStartTime;
            //    if (elapsed > raidStartCooldown || Permissions.IsAdministrator)
            //    {
            //        raidStartTime = DateTime.UtcNow;
            //        raidManager.StartRaid("streamer");
            //    }
            //    else
            //    {
            //        var timeLeft = raidStartCooldown - elapsed;
            //        RavenBot.Announce("You have to wait {cooldown} seconds before you can start another raid.", timeLeft.TotalSeconds.ToString());
            //    }
            //}

            //if (isControlDown && Input.GetKeyUp(KeyCode.Y))
            //{
            //    RavenNest.EnqueueLoyaltyUpdate(new TwitchCheer("72424639", "zerratar", "zerratar", true, true, true, 200));
            //}

            //if (isControlDown && Input.GetKeyUp(KeyCode.T))
            //{
            //    RavenNest.EnqueueLoyaltyUpdate(new TwitchSubscription("72424639", "zerratar", "zerratar", "72424639", true, true, 1, false));
            //}
        }

        //if (Permissions.IsAdministrator || Application.isEditor)
        //{
        //    //if (isControlDown && Input.GetKeyDown(KeyCode.KeypadPlus))
        //    //{
        //    //    var adminPlayer = this.Players.GetAllPlayers().FirstOrDefault(x => x.IsGameAdmin);
        //    //    if (adminPlayer != null)
        //    //    {
        //    //        var st = itemManager.GetItems().FirstOrDefault(x => x.Category == ItemCategory.StreamerToken);
        //    //        if (st != null)
        //    //        {
        //    //            adminPlayer.PickupItem(st);
        //    //        }
        //    //    }
        //    //}
        //    //if (isControlDown && Input.GetKeyDown(KeyCode.Delete))
        //    //{
        //    //    subEventManager.Reset();
        //    //}
        //    //if (isControlDown && Input.GetKeyUp(KeyCode.A))
        //    //{
        //    //    subEventManager.Activate();
        //    //}
        //    //if (isControlDown && Input.GetKeyUp(KeyCode.C))
        //    //{
        //    //    Twitch.OnSubscribe(new TwitchSubscription(null, null, null, null, false, false, -1, true));
        //    //}
        //}
    }

#if DEBUG



    private void OnGUI()
    {
        if (!Permissions.IsAdministrator)
        {
            return;
        }

        int buttonWidth = 150;
        int buttonMarginY = 5;
        int buttonHeight = 40;
        int buttonStartY = (Screen.height / 2) - (lastButtonIndex * (buttonHeight / 2));//60;

        Rect GetButtonRect(int i) => new Rect(Screen.width - buttonWidth - 20, buttonStartY + ((buttonHeight + buttonMarginY) * i), buttonWidth, buttonHeight);

        var buttonIndex = 0;

        if (GUI.Button(GetButtonRect(buttonIndex++), AdminControlData.NoChatBotMessages ? "Chat Notifications OFF" : "Chat Notifications ON"))
        {
            AdminControlData.NoChatBotMessages = !AdminControlData.NoChatBotMessages;
        }

        if (AdminControlData.ControlPlayers)
        {
            if (GUI.Button(GetButtonRect(buttonIndex++), "Control Bots"))
            {
                AdminControlData.ControlPlayers = false;
            }
        }
        else
        {
            if (GUI.Button(GetButtonRect(buttonIndex++), "Control Players"))
            {
                AdminControlData.ControlPlayers = true;
            }
        }

        if (raidManager.Started)
        {
            if (GUI.Button(GetButtonRect(buttonIndex++), "Kill raid boss"))
            {
                var randomPlayer = raidManager.Raiders.Random();
                if (raidManager.Boss && randomPlayer)
                {
                    raidManager.Boss.Enemy.TakeDamage(randomPlayer, raidManager.Boss.Enemy.Stats.Health.CurrentValue);
                }
            }
        }
        else
        {

            if (GUI.Button(GetButtonRect(buttonIndex++), "Start Raid"))
            {
                raidManager.StartRaid();
            }

        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Toggle Dungeon"))
        {
            dungeonManager.ToggleDungeon();
        }

        if (dungeonManager.Active)
        {
            if (GUI.Button(GetButtonRect(buttonIndex++), "Advance to next room"))
            {
                var players = dungeonManager.GetPlayers();
                foreach (var enemy in dungeonManager.Dungeon.Room.Enemies)
                {
                    var randomPlayer = players.Random();
                    if (!enemy.Stats.IsDead)
                    {
                        enemy.TakeDamage(randomPlayer, enemy.Stats.Health.Level);
                    }
                }

                foreach (var player in players)
                {
                    player.ClearTarget();
                }
            }

            if (!dungeonManager.Started)
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Start dungeon now"))
                {
                    dungeonManager.ForceStartDungeon();
                }
            }

            if (GUI.Button(GetButtonRect(buttonIndex++), "Damage Boss (-25% HP)"))
            {
                var randomPlayer = dungeonManager.GetPlayers().Random();
                if (randomPlayer && dungeonManager.Boss)
                {
                    var boss = dungeonManager.Boss.Enemy;
                    boss.TakeDamage(randomPlayer, (int)(boss.Stats.Health.Level * 0.25f));
                }
            }


            if (GUI.Button(GetButtonRect(buttonIndex++), "Kill Boss (-100% HP)"))
            {
                var randomPlayer = dungeonManager.GetPlayers().Random();
                if (randomPlayer && dungeonManager.Boss)
                {
                    var boss = dungeonManager.Boss.Enemy;
                    boss.TakeDamage(randomPlayer, boss.Stats.Health.Level);
                }
            }

        }

        if (!AdminControlData.TeleportationVisible && !AdminControlData.BotTrainingVisible && !AdminControlData.ControlPlayers)
        {
            if (AdminControlData.BotSpawnVisible)
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 300 Bots"))
                {
                    SpawnManyBotPlayers(300);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 200 Bots"))
                {
                    SpawnManyBotPlayers(200);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 100 Bots"))
                {
                    SpawnManyBotPlayers(100);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 10 Bots"))
                {
                    SpawnManyBotPlayers(10);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn a bot"))
                {
                    SpawnBotPlayer();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn a bot on ferry"))
                {
                    SpawnBotPlayerOnFerry();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "<< Back"))
                {
                    AdminControlData.BotSpawnVisible = false;
                }

            }
            else
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn Bots"))
                {
                    AdminControlData.BotSpawnVisible = true;
                }
            }
        }

        if (!AdminControlData.TeleportationVisible && !AdminControlData.BotSpawnVisible && (spawnedBots > 0 || playerManager.GetPlayerCount() > 0))
        {
            if (AdminControlData.BotTrainingVisible)
            {

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Combat"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask("fighting", new string[] { (new string[] { "all", "strength", "attack", "defense", "ranged", "magic" }).Random() });
                    }
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Healing"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask("fighting", new string[] { (new string[] { "healing" }).Random() });
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Fishing"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask("fishing", new string[0]);
                    }
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Woodcutting"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask("woodcutting", new string[0]);
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Mining"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask((new string[] { "mining" }).Random(), new string[0]);
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Farming"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask((new string[] { "farming" }).Random(), new string[0]);
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Crafting"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask((new string[] { "Crafting" }).Random(), new string[0]);
                    }
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Gathering"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask((new string[] { "fishing", "mining", "farming", "crafting", "cooking", "woodcutting" }).Random(), new string[0]);
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Healing 50/Combat 50"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var c = bots.Count / 2;
                    var i = 0;
                    foreach (var bot in bots)
                    {
                        var task = "fighting";
                        var subTask = (new string[] { "all", "strength", "attack", "defense", "ranged", "magic", }).Random();
                        if (i++ <= c)
                        {
                            subTask = "healing";
                        }
                        bot.SetTask(task, new string[] { subTask });
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Random"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        var s = (new string[] { "fishing", "mining", "farming", "crafting", "cooking", "woodcutting", "fighting" }).Random();
                        if (s == "fighting")
                        {
                            bot.SetTask(s, new string[] { (new string[] { "all", "strength", "attack", "defense", "ranged", "magic", "healing" }).Random() });
                        }
                        else
                        {
                            bot.SetTask(s, new string[0]);
                        }
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Rest"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        Onsen.Join(bot);
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "<< Back"))
                {
                    AdminControlData.BotTrainingVisible = false;
                }
            }
            else
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Training"))
                {
                    AdminControlData.BotTrainingVisible = true;
                }
            }
        }

        if (!AdminControlData.BotSpawnVisible && !AdminControlData.BotTrainingVisible && (spawnedBots > 0 || playerManager.GetPlayerCount() > 0))
        {
            if (AdminControlData.TeleportationVisible)
            {

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Home"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Home");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Away"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Away");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Ironhill"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Ironhill");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Kyo"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Kyo");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Heim"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Heim");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Home"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Home");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Away"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Away");
                    foreach (var bot in bots) bot.Teleporter.Teleport(island.SpawnPosition);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Ironhill"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Ironhill");
                    foreach (var bot in bots) bot.Teleporter.Teleport(island.SpawnPosition);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Kyo"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Kyo");
                    foreach (var bot in bots) bot.Teleporter.Teleport(island.SpawnPosition);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Heim"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Heim");
                    foreach (var bot in bots) bot.Teleporter.Teleport(island.SpawnPosition);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "<< Back"))
                {
                    AdminControlData.TeleportationVisible = false;
                }
            }
            else
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Traveling"))
                {
                    AdminControlData.TeleportationVisible = true;
                }
            }
        }
        //if (GUI.Button(GetButtonRect(buttonIndex++), dungeonManager.Active ? "Stop Dungeon" : "Start Dungeon"))
        //{
        //    dungeonManager.ToggleDungeon();
        //}
        //if (GUI.Button(GetButtonRect(buttonIndex++), "Start Raid"))
        //{
        //    raidManager.StartRaid("admin");
        //}
        //if (this.raidManager.Started)
        //{
        //    if (GUI.Button(GetButtonRect(buttonIndex++), "Join Raid"))
        //    {
        //        var bots = this.playerManager.GetAllBots();
        //        foreach (var bot in bots)
        //        {
        //            Raid.Join(bot);
        //        }
        //    }
        //}
        //if (this.dungeonManager.Active)
        //{
        //    if (GUI.Button(GetButtonRect(buttonIndex++), "Join Dungeon"))
        //    {
        //        var bots = this.playerManager.GetAllBots();
        //        foreach (var bot in bots)
        //        {
        //            dungeonManager.Join(bot);
        //        }
        //    }
        //}
        //if (GUI.Button(GetButtonRect(buttonIndex++), "Start Stop 100 Dungeons"))
        //{
        //    StartCoroutine(ToggleManyDungeons(100));
        //}


        lastButtonIndex = buttonIndex;
    }
#endif

    private IEnumerator ToggleManyDungeons(int count)
    {
        for (var i = 0; i < count; ++i)
        {
            dungeonManager.ToggleDungeon();
            yield return null;
        }
    }

    private void UpdatePlayerKickQueue()
    {
        if (playerKickQueue.Count <= 0)// || !Arena || Arena.Started)
        {
            return;
        }

        var player = playerKickQueue.Dequeue();
        //if (player.Duel.InDuel)
        //{
        //    playerKickQueue.Enqueue(player);
        //    return;
        //}

        RemovePlayer(player);
    }

    private void UpdateExpBoostTimer()
    {
        if (!boostTimer) return;

        expBoostTimerUpdate -= Time.deltaTime;
        if (expBoostTimerUpdate > 0f) return;

        boostTimer.SetActive(subEventManager.CurrentBoost.Active);


        if (subEventManager.CurrentBoost.Active)
        {
            var duration = subEventManager.Duration ?? TimeSpan.Zero;
            var remainingTime = subEventManager.TimeLeft ?? TimeSpan.Zero;
            var secondsLeft = (float)remainingTime.TotalSeconds;
            var secondsLeftInt = Mathf.FloorToInt(secondsLeft);
            var secondsTotal = (float)duration.TotalSeconds;
            var timeLeft = $"{secondsLeftInt} sec";
            if (secondsLeftInt > 3600)
            {
                timeLeft = $"{Mathf.FloorToInt(secondsLeftInt / 3600f)} hours";
                var minutes = secondsLeftInt / 60;
                minutes = (int)(minutes % 60);
                if (minutes > 0)
                {
                    timeLeft += " " + (int)+minutes + "min";
                }
            }
            else if (secondsLeftInt > 60)
                timeLeft = $"{Mathf.FloorToInt(secondsLeftInt / 60f)} mins";
            boostTimer.SetSubscriber(subEventManager.CurrentBoost.LastSubscriber, !subEventManager.CurrentBoost.LastSubscriber.Contains(" "));
            boostTimer.SetText($"EXP Multiplier x{subEventManager.CurrentBoost.Multiplier} - {timeLeft}");
            boostTimer.SetTime(secondsLeft, secondsTotal);
        }

        expBoostTimerUpdate = 0.5f;
    }

    private void UpdateApiCommunication()
    {
        HandleRavenNestConnection();

        if (RavenNest != null)
        {
            RavenNestUpdate();
        }
    }
    internal void UpdateServerTime(DateTime timeUtc)
    {
        if (Permissions.IsAdministrator)
            return;

        var f0 = lastServerTimeUpdateFloat;
        var dt0 = lastServerTimeUpdateDateTime;
        var st0 = serverTime;
        var now = DateTime.UtcNow;

        var f1 = lastServerTimeUpdateFloat = Time.realtimeSinceStartup;
        var dt1 = lastServerTimeUpdateDateTime = DateTime.UtcNow;
        var st1 = serverTime = timeUtc;
        var margin = 3600;

        if (f0 > 0.0001f && dt0 != DateTime.MinValue && st0 != DateTime.MinValue)
        {
            var nowDelta = now - timeUtc;
            if (nowDelta > TimeSpan.FromSeconds(margin))
            {
                var fd = f1 - f0;
                var dtd = dt1 - dt0;
                var std = st1 - st0;

                var us = (long)fd;
                var cs = (long)dtd.TotalSeconds;
                var ss = (long)std.TotalSeconds;

                if (Math.Abs(cs - ss) > margin || Math.Abs(us - cs) > margin || Math.Abs(us - ss) > margin)
                {
                    RavenNest.WebSocket.SyncTimeAsync(nowDelta, now, timeUtc);
                    //RavenNest.Desynchronized = true;

                    // Things will not be saved properly.
                }
            }
        }

    }
}